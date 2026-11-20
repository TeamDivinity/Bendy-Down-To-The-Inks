using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[RequireComponent(typeof(AudioSource))]
public class AudioRotateInteractable : MonoBehaviour, IInteractable
{
    [Header("Настройки звука")]
    [SerializeField] private AudioClip interactionSound;
    [Header("Настройки слоя")]
    [Tooltip("Введите точное имя слоя (например: IgnoreRaycast, Default, Water). Регистр важен!")]
    [SerializeField] private string targetLayerName = "IgnoreRaycast";
    private int targetLayerIndex;
    [Header("Элементы вращения")]
    [Tooltip("Объекты, которые будут вращаться вокруг локальной оси Z, пока играет звук")]
    [SerializeField] private List<Transform> spinningElements = new List<Transform>();
    [SerializeField] private float rotationSpeed = 360f; 
    private AudioSource _audioSource;
    private bool _isPlaying = false;
    private Dictionary<GameObject, int> _originalLayers = new Dictionary<GameObject, int>();
    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
        targetLayerIndex = LayerMask.NameToLayer(targetLayerName);
        if (targetLayerIndex == -1)
        {
            Debug.LogError($"[{name}] Слой '{targetLayerName}' не найден! Проверьте точное имя в Edit -> Project Settings -> Tags and Layers. Временно используется Default (0).", this);
            targetLayerIndex = 0;
        }
    }
    public void OnInteract()
    {
        if (_isPlaying || interactionSound == null) return;
        _isPlaying = true;
        SaveOriginalLayers();
        SetLayers(targetLayerIndex);
        _audioSource.PlayOneShot(interactionSound);
        StartCoroutine(WaitForSoundCompletion());
    }
    private void SaveOriginalLayers()
    {
        _originalLayers.Clear();
        var allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            _originalLayers[t.gameObject] = t.gameObject.layer;
        }
    }
    private void SetLayers(int newLayer)
    {
        var allTransforms = GetComponentsInChildren<Transform>(true);
        foreach (var t in allTransforms)
        {
            t.gameObject.layer = newLayer;
        }
    }
    private void RestoreLayers()
    {
        foreach (var kvp in _originalLayers)
        {
            if (kvp.Key != null)
                kvp.Key.layer = kvp.Value;
        }
    }
    private IEnumerator WaitForSoundCompletion()
    {
        yield return new WaitForSeconds(interactionSound.length);
        OnPlaybackFinished();
    }
    private void OnPlaybackFinished()
    {
        _isPlaying = false;
        RestoreLayers();
        _originalLayers.Clear();
    }
    private void Update()
    {
        if (!_isPlaying || spinningElements.Count == 0) return;
        float deltaRotation = rotationSpeed * Time.deltaTime;
        foreach (var element in spinningElements)
        {
            if (element != null)
                element.Rotate(Vector3.forward * deltaRotation, Space.Self);
        }
    }
}