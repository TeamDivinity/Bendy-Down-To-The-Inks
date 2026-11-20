using UnityEngine;
using System.Collections;

public class ValveInteractable : MonoBehaviour, IInteractable
{
    [Header("Финальное состояние")]
    [Tooltip("Слой, который будет установлен на этом объекте после финального прокручивания")]
    public string finalLayer = "Used";

    [Header("Настройки вращения")]
    public Transform valveTransform;
    public Vector3 rotationAxis = Vector3.up;
    public float rotationDegrees = 90f;
    public float rotationDuration = 1f;

    [Header("Доп. движение")]
    public Transform objectToMove;
    public float targetHeight = 2f;
    public float moveDuration = 1f;

    [Header("Звуки")]
    public AudioClip valveSound;
    public AudioClip moveSound;
    public AudioClip pickupSound;

    private AudioSource _audioSource;
    private RandomPlacement _randomPlacement;

    private bool _isAnimating = false;
    public bool IsInstalled { get; private set; }
    private bool _isFinalized = false;

    private void Awake()
    {
        if (valveTransform == null) valveTransform = transform;

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0.5f;

        _randomPlacement = GetComponent<RandomPlacement>();
    }

    public void OnInteract()
    {
        if (_isAnimating || _isFinalized) return;

        if (_randomPlacement != null && !_randomPlacement.IsInstalled && !_randomPlacement.IsPickedUp)
        {
            _randomPlacement.PickUp();
            PlaySound(pickupSound);
            IsInstalled = false;
            return;
        }

        if (IsInstalled)
        {
            StartCoroutine(AnimateValveRotation());
        }
    }

    public void TryInstallValve()
    {
        if (_randomPlacement == null)
        {
            IsInstalled = true;
            StartCoroutine(AnimateValveRotation());
            return;
        }

        if (!_randomPlacement.IsPickedUp || _randomPlacement.IsInstalled) return;

        _randomPlacement.Install();
        IsInstalled = true;
    }

    private IEnumerator AnimateValveRotation()
    {
        _isAnimating = true;
        PlaySound(valveSound);

        Quaternion startRot = valveTransform.localRotation;
        Quaternion targetRot = startRot * Quaternion.AngleAxis(rotationDegrees, rotationAxis.normalized);
        float elapsed = 0f;

        while (elapsed < rotationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / rotationDuration);
            float smoothT = EaseInOutCubic(t);

            valveTransform.localRotation = Quaternion.Slerp(startRot, targetRot, smoothT);
            yield return null;
        }

        valveTransform.localRotation = targetRot;

        if (objectToMove != null)
        {
            PlaySound(moveSound);
            Vector3 startPos = objectToMove.position;
            Vector3 targetPos = new Vector3(startPos.x, targetHeight, startPos.z);
            elapsed = 0f;

            while (elapsed < moveDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / moveDuration);
                float smoothT = EaseInOutCubic(t);

                objectToMove.position = Vector3.Lerp(startPos, targetPos, smoothT);
                yield return null;
            }

            objectToMove.position = targetPos;
        }

        _isAnimating = false;
        Finalize();
    }

    private void Finalize()
    {
        if (_isFinalized) return;

        _isFinalized = true;

        if (!string.IsNullOrEmpty(finalLayer))
        {
            int layerIndex = LayerMask.NameToLayer(finalLayer);
            if (layerIndex != -1)
                gameObject.layer = layerIndex;
        }
    }

    private float EaseInOutCubic(float t) =>
        t < 0.5f ? 4 * t * t * t : 1 - Mathf.Pow(-2 * t + 2, 3) / 2f;

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
            _audioSource.PlayOneShot(clip);
    }
}