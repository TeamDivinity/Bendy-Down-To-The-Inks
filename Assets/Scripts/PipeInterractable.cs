using UnityEngine;

public class PipeInteractable : MonoBehaviour, IInteractable
{
    [Header("Ссылка на вентиль")]
    [Tooltip("Перетащи сюда объект с ValveInteractable")]
    public ValveInteractable valveInteractable;
    
    [Header("Настройки")]
    [Tooltip("Слой, который будет установлен на трубе после успешной установки")]
    public string installedLayer = "Installed";
    
    [Header("Звуки")]
    public AudioClip installSound;
    public AudioClip notPickedUpSound;
    public AudioClip alreadyInstalledSound;

    private AudioSource _audioSource;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0.5f;
    }

    public void OnInteract()
    {
        if (valveInteractable == null)
        {
            Debug.LogError("[PipeInteractable] valveInteractable не назначен в инспекторе!");
            return;
        }

        var randomPlacement = valveInteractable.GetComponent<RandomPlacement>();
        
        // Проверяем состояние вентиля
        if (randomPlacement != null && !randomPlacement.IsPickedUp)
        {
            PlaySound(notPickedUpSound);
            Debug.Log("Сначала нужно найти и подобрать вентиль!");
            return;
        }

        if (valveInteractable.IsInstalled)
        {
            PlaySound(alreadyInstalledSound);
            return;
        }

        // Устанавливаем вентиль
        valveInteractable.TryInstallValve();
        
        // Проверяем, успешно ли установился
        if (valveInteractable.IsInstalled)
        {
            // Меняем слой трубы
            if (!string.IsNullOrEmpty(installedLayer))
            {
                gameObject.layer = LayerMask.NameToLayer(installedLayer);
            }
            PlaySound(installSound);
            Debug.Log("Труба: вентиль установлен, слой изменён!");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && _audioSource != null)
            _audioSource.PlayOneShot(clip);
    }
}