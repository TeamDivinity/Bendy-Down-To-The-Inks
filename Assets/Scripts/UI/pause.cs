using UnityEngine;
using UnityEngine.UI; // (не обязательно, но оставлено)
using System.Collections;

public class Pause : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup pausePanel;
    [SerializeField] private CanvasGroup buttonsPanel;
    [SerializeField] private Material dissolveMaterial;
    [SerializeField] private AudioClip pauseClip;
    [SerializeField] private Buttons buttons;  // <-- новое поле

    [Header("Settings")]
    [SerializeField] private float dissolveFadeDuration = 0.3f;
    [SerializeField] private float buttonsFadeDuration = 0.3f;
    [SerializeField] private float dissolveStartValue = 0.5f;
    [SerializeField] private float dissolveEndValue = 0f;
    [SerializeField] private float buttonsActivateThreshold = 0.7f;

    public static bool IsPaused { get; private set; }
    public static Pause Instance { get; private set; }

    private Coroutine fadeCoroutine;
    private AudioSource _audioSource;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (pausePanel == null)
            pausePanel = GetComponentInChildren<CanvasGroup>();

        if (pausePanel == null)
        {
            Debug.LogError("[Pause] CanvasGroup не найден!");
            enabled = false;
            return;
        }

        SetupAudioSource();
        ResetPauseState();
    }

    private void SetupAudioSource()
    {
        if (pauseClip == null) return;
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
        _audioSource.ignoreListenerPause = true;
        _audioSource.outputAudioMixerGroup = null;
    }

    private void ResetPauseState()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        SetDissolveState(dissolveStartValue, false, false);
        SetButtonsAlpha(0f, false, false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Если открыты настройки – просто закрываем их, паузу не трогаем
            if (buttons != null && buttons.IsSettingsOpen())
            {
                buttons.ToggleSettings();
            }
            else
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        if (fadeCoroutine != null)
            StopCoroutine(fadeCoroutine);

        IsPaused = !IsPaused;

        if (IsPaused && pauseClip != null && _audioSource != null)
            _audioSource.PlayOneShot(pauseClip);

        Time.timeScale = IsPaused ? 0f : 1f;
        AudioListener.pause = IsPaused;

        Cursor.lockState = IsPaused ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = IsPaused;

        float targetDissolve = IsPaused ? dissolveEndValue : dissolveStartValue;
        float targetButtonsAlpha = IsPaused ? 1f : 0f;

        fadeCoroutine = StartCoroutine(FadeBoth(targetDissolve, targetButtonsAlpha));
    }

    private IEnumerator FadeBoth(float targetDissolve, float targetButtonsAlpha)
    {
        float startDissolve = dissolveMaterial?.GetFloat("_DissolveAmount") ?? dissolveStartValue;
        float startButtonsAlpha = buttonsPanel?.alpha ?? 0f;
        float elapsedDissolve = 0f;
        float elapsedButtons = 0f;

        while (elapsedDissolve < dissolveFadeDuration || elapsedButtons < buttonsFadeDuration)
        {
            float delta = Time.unscaledDeltaTime;

            if (elapsedDissolve < dissolveFadeDuration)
            {
                elapsedDissolve += delta;
                float t = Mathf.Clamp01(elapsedDissolve / dissolveFadeDuration);
                float currentDissolve = Mathf.Lerp(startDissolve, targetDissolve, t);
                bool mainReady = IsPaused
                    ? (currentDissolve <= Mathf.Lerp(dissolveStartValue, dissolveEndValue, buttonsActivateThreshold))
                    : false;
                SetDissolveState(currentDissolve, mainReady, mainReady);
            }

            if (buttonsPanel != null && elapsedButtons < buttonsFadeDuration)
            {
                elapsedButtons += delta;
                float t = Mathf.Clamp01(elapsedButtons / buttonsFadeDuration);
                float currentButtonsAlpha = Mathf.Lerp(startButtonsAlpha, targetButtonsAlpha, t);
                bool buttonsReady = currentButtonsAlpha >= buttonsActivateThreshold;
                SetButtonsAlpha(currentButtonsAlpha, buttonsReady, buttonsReady);
            }

            yield return null;
        }

        SetDissolveState(targetDissolve, IsPaused, IsPaused);
        SetButtonsAlpha(targetButtonsAlpha, IsPaused, IsPaused);
        fadeCoroutine = null;
    }

    private void SetDissolveState(float dissolveAmount, bool interactable, bool blocksRaycasts)
    {
        dissolveMaterial?.SetFloat("_DissolveAmount", dissolveAmount);
        if (pausePanel != null)
        {
            pausePanel.interactable = interactable;
            pausePanel.blocksRaycasts = blocksRaycasts;
            pausePanel.alpha = 1f;
        }
    }

    private void SetButtonsAlpha(float alpha, bool interactable, bool blocksRaycasts)
    {
        if (buttonsPanel != null)
        {
            buttonsPanel.alpha = alpha;
            buttonsPanel.interactable = interactable;
            buttonsPanel.blocksRaycasts = blocksRaycasts;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }
}