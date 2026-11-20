using UnityEngine;
[RequireComponent(typeof(AudioSource))]
public class DestroyableInteractable : MonoBehaviour, IInteractable
{
    [Header("Настройки удаления")]
    public AudioClip destroySound;
    [Tooltip("Скрывать объект визуально сразу после нажатия")]
    public bool hideImmediately = true;
    private AudioSource _audioSource;
    void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.loop = false;
    }
    public void OnInteract()
    {
        if (destroySound != null && _audioSource != null)
        {
            _audioSource.PlayOneShot(destroySound);
        }
        if (hideImmediately)
        {
            Renderer[] allRenderers = GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in allRenderers)
            {
                renderer.enabled = false;
            }
            Collider[] allColliders = GetComponentsInChildren<Collider>(true);
            foreach (var collider in allColliders)
            {
                collider.enabled = false;
            }
            enabled = false;
        }
        float destroyDelay = (destroySound != null) ? destroySound.length : 0.1f;
        Destroy(gameObject, destroyDelay);
    }
}