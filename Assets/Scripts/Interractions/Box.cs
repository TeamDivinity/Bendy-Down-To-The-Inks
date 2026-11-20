using UnityEngine;

public class AnimatedInteractable : MonoBehaviour, IInteractable
{
    [Header("Настройки цели")]
    public Transform animatedTarget;

    [Header("Настройки анимации")]
    public AnimationClip openClip;
    public AnimationClip closeClip;

    [Header("Параметры")]
    public float animationSpeed = 1f;

    [Header("Настройки звука")]
    public AudioClip openSound;
    public AudioClip closeSound;

    [Header("Настройки после открытия")]
    [Tooltip("Если true, после открытия сундук больше не будет реагировать на взаимодействие")]
    public bool disableInteractionAfterOpen = true;

    [Tooltip("Слой, на который перейдёт объект после открытия. \nДля пропуска рейкастов используйте 'Ignore Raycast'")]
    public string layerAfterOpen = "Default";

    private Animation anim;
    private AudioSource audioSource;
    private bool isOpen = false;
    private bool canInteract = true;
    private string openClipName;
    private string closeClipName;

    void Start()
    {
        Transform target = animatedTarget != null ? animatedTarget : transform;
        anim = target.GetComponent<Animation>();
        if (anim == null)
            anim = target.gameObject.AddComponent<Animation>();

        // Настройка клипа открытия
        if (openClip != null)
        {
            openClipName = "Open_" + openClip.name;
            if (anim[openClipName] == null)
                anim.AddClip(openClip, openClipName);

            AnimationState openState = anim[openClipName];
            openState.wrapMode = WrapMode.ClampForever;
            openState.speed = animationSpeed;
        }

        // Настройка клипа закрытия
        if (closeClip != null)
        {
            closeClipName = "Close_" + closeClip.name;
            if (anim[closeClipName] == null)
                anim.AddClip(closeClip, closeClipName);

            AnimationState closeState = anim[closeClipName];
            closeState.wrapMode = WrapMode.ClampForever;
            closeState.speed = animationSpeed;
        }

        // Начальное состояние (закрыто)
        if (closeClip != null)
        {
            anim.Play(closeClipName);
            anim[closeClipName].normalizedTime = 1f;
            anim.Sample();
            anim.Stop();
        }
        else if (openClip != null)
        {
            anim.Play(openClipName);
            anim[openClipName].normalizedTime = 0f;
            anim.Sample();
            anim.Stop();
        }

        // Настройка аудио
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }
    }

    public void OnInteract()
    {
        if (!canInteract || isOpen) return;

        isOpen = true;

        // Анимация открытия
        if (openClip != null)
        {
            anim.Play(openClipName);
            anim[openClipName].normalizedTime = 0f;
            anim[openClipName].speed = Mathf.Abs(animationSpeed);
        }

        // Звук открытия
        if (openSound != null)
            audioSource.PlayOneShot(openSound);

        // Логика после открытия
        if (disableInteractionAfterOpen)
        {
            canInteract = false;
            // Если ваша система поиска интерактивных объектов использует GetComponent<IInteractable>(),
            // раскомментируйте строку ниже, чтобы полностью отключить компонент:
            // this.enabled = false;
        }

        // Смена слоя
        int newLayer = LayerMask.NameToLayer(layerAfterOpen);
        if (newLayer != -1)
        {
            gameObject.layer = newLayer;
        }
    }

    public bool IsOpen() => isOpen;
}