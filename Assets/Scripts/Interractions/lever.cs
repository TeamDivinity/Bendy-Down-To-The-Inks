using UnityEngine;
using System.Collections;

public class Lever : MonoBehaviour, IInteractable
{
    [Header("Анимации")]
    public AnimationClip pullDownClip;
    public AnimationClip pullUpClip;

    [Header("Состояние")]
    public bool isPulled = false;

    [Header("Звук")]
    public AudioClip leverSound;
    [Range(0f, 1f)] public float soundVolume = 1f;

    [Header("Лампочка")]
    public Renderer lightRenderer;
    public int materialIndex = 1;
    public Color lightOnColor = Color.white;
    public float lightIntensity = 1f;
    private Color lightOffColor = Color.black;

    private Animation anim;
    private AudioSource audioSource;
    private bool isBusy = false;
    private bool currentState = false;
    private bool wantLightOn = false;

    private AnimationState downState;
    private AnimationState upState;
    private const string DOWN_NAME = "Lever_Down";
    private const string UP_NAME = "Lever_Up";

    void Start()
    {
        // 1. Animation компонент
        anim = GetComponent<Animation>();
        if (!anim) anim = gameObject.AddComponent<Animation>();
        anim.playAutomatically = false;

        // 2. Добавление клипов
        if (pullDownClip != null)
        {
            anim.AddClip(pullDownClip, DOWN_NAME);
            downState = anim[DOWN_NAME];
            if (downState != null) downState.wrapMode = WrapMode.Once;
        }

        if (pullUpClip != null)
        {
            anim.AddClip(pullUpClip, UP_NAME);
            upState = anim[UP_NAME];
            if (upState != null) upState.wrapMode = WrapMode.Once;
        }

        // 3. Audio
        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
        audioSource.clip = leverSound;
        audioSource.volume = soundVolume;

        // 4. Начальное состояние
        currentState = isPulled;
        wantLightOn = currentState;
        UpdateLight();

        // Применяем начальную позу без проигрывания
        if (currentState && downState != null)
        {
            anim.Play(DOWN_NAME);
            downState.normalizedTime = 1f;
            anim.Sample();
            anim.Stop(DOWN_NAME);
        }
        else if (!currentState && upState != null)
        {
            anim.Play(UP_NAME);
            upState.normalizedTime = 1f;
            anim.Sample();
            anim.Stop(UP_NAME);
        }
    }

    public void OnInteract()
    {
        if (isBusy) return;

        bool target = !currentState;
        string clipName = target ? DOWN_NAME : UP_NAME;
        AnimationState state = target ? downState : upState;

        if (state == null)
        {
            Debug.LogWarning($"[Lever] Анимация '{clipName}' не найдена! Проверь назначение клипов.");
            return;
        }

        currentState = target;
        isBusy = true;

        // Логика света
        if (currentState)
            wantLightOn = true; // Включится ПОСЛЕ анимации
        else
        {
            wantLightOn = false;
            UpdateLight(); // Выключится СРАЗУ
        }

        if (leverSound) audioSource.PlayOneShot(leverSound, soundVolume);
        StartCoroutine(PlayAnimRoutine(clipName, state));
    }

    private IEnumerator PlayAnimRoutine(string name, AnimationState state)
    {
        anim.Stop(name); // Сброс на случай залипания
        anim.Play(name);
        state.normalizedTime = 0f;
        state.speed = 1f;

        // Ждём точное время анимации
        yield return new WaitForSeconds(state.length);

        anim.Stop(name);
        isBusy = false;

        if (wantLightOn) UpdateLight();
    }

    private void UpdateLight()
    {
        if (!lightRenderer) return;

        Material[] mats = lightRenderer.materials;
        if (materialIndex < 0 || materialIndex >= mats.Length)
        {
            Debug.LogError($"[Lever] MaterialIndex {materialIndex} вне диапазона! Всего материалов: {mats.Length}");
            return;
        }

        Color target = wantLightOn ? lightOnColor : lightOffColor;
        mats[materialIndex].SetColor("_EmissionColor", target * lightIntensity);

        if (wantLightOn) mats[materialIndex].EnableKeyword("_EMISSION");
        else mats[materialIndex].DisableKeyword("_EMISSION");

        // 🔑 Критически важно: присваиваем изменённый массив обратно
        lightRenderer.materials = mats;
    }

    public bool IsPulled() => currentState;
}