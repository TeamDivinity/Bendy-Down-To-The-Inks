using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SettingsController : MonoBehaviour
{
    [Header("Чувствительность мыши")]
    [SerializeField] private Slider mouseSensitivitySlider;
    [SerializeField] private PlayerController playerController;
    [SerializeField] private float mouseSensitivityMin = 0.1f;
    [SerializeField] private float mouseSensitivityMax = 10f;
    [SerializeField] private float mouseSensitivityDefault = 2f;

    [Header("Яркость (Shadows V в Split Toning)")]
    [SerializeField] private Slider brightnessSlider;
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private float brightnessMin = 0f;
    [SerializeField] private float brightnessMax = 1f;
    [SerializeField] private float brightnessDefault = 0.5f;

    [Header("Громкость SFX")]
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private AudioMixer mainMixer;                // Ссылка на ваш Audio Mixer
    [SerializeField] private string sfxExposedParameter = "SFXVolume"; // Имя exposed-параметра
    [SerializeField] private float sfxMinDB = -80f;               // Мин. громкость в дБ (тишина)
    [SerializeField] private float sfxMaxDB = 0f;                 // Макс. громкость в дБ
    [SerializeField] private float sfxDefaultDB = 0f;             // Громкость по умолчанию

    [Header("Громкость Ambience")]
    [SerializeField] private Slider ambienceVolumeSlider;
    [SerializeField] private string ambienceExposedParameter = "AmbienceVolume";
    [SerializeField] private float ambienceMinDB = -80f;
    [SerializeField] private float ambienceMaxDB = 0f;
    [SerializeField] private float ambienceDefaultDB = -6f;       // По умолчанию чуть тише

    private SplitToning splitToning;

    private void Start()
    {
        // Split Toning
        if (postProcessVolume != null && postProcessVolume.profile != null)
        {
            if (!postProcessVolume.profile.TryGet(out splitToning))
            {
                splitToning = postProcessVolume.profile.Add<SplitToning>(true);
            }
            splitToning.shadows.overrideState = true;
        }

        // Мышь
        if (mouseSensitivitySlider != null)
        {
            float defaultVal = Mathf.InverseLerp(mouseSensitivityMin, mouseSensitivityMax, mouseSensitivityDefault);
            mouseSensitivitySlider.value = defaultVal;
            mouseSensitivitySlider.onValueChanged.AddListener(v => ApplyMouseSensitivity(v));
            ApplyMouseSensitivity(defaultVal);
        }

        // Яркость
        if (brightnessSlider != null)
        {
            float defaultVal = Mathf.InverseLerp(brightnessMin, brightnessMax, brightnessDefault);
            brightnessSlider.value = defaultVal;
            brightnessSlider.onValueChanged.AddListener(v => ApplyBrightness(v));
            ApplyBrightness(defaultVal);
        }

        // SFX
        if (sfxVolumeSlider != null && mainMixer != null)
        {
            float defaultVal = Mathf.InverseLerp(sfxMinDB, sfxMaxDB, sfxDefaultDB);
            sfxVolumeSlider.value = defaultVal;
            sfxVolumeSlider.onValueChanged.AddListener(v => ApplySFXVolume(v));
            ApplySFXVolume(defaultVal);
        }

        // Ambience
        if (ambienceVolumeSlider != null && mainMixer != null)
        {
            float defaultVal = Mathf.InverseLerp(ambienceMinDB, ambienceMaxDB, ambienceDefaultDB);
            ambienceVolumeSlider.value = defaultVal;
            ambienceVolumeSlider.onValueChanged.AddListener(v => ApplyAmbienceVolume(v));
            ApplyAmbienceVolume(defaultVal);
        }
    }

    // ---------- Методы применения ----------

    private void ApplyMouseSensitivity(float t)
    {
        if (playerController != null)
            playerController.mouseSensitivity = Mathf.Lerp(mouseSensitivityMin, mouseSensitivityMax, t);
    }

    private void ApplyBrightness(float t)
    {
        if (splitToning != null)
        {
            float v = Mathf.Lerp(brightnessMin, brightnessMax, t);
            splitToning.shadows.value = new Color(v, v, v);
        }
    }

    private void ApplySFXVolume(float t)
    {
        if (mainMixer != null)
        {
            float db = Mathf.Lerp(sfxMinDB, sfxMaxDB, t);
            mainMixer.SetFloat(sfxExposedParameter, db);
        }
    }

    private void ApplyAmbienceVolume(float t)
    {
        if (mainMixer != null)
        {
            float db = Mathf.Lerp(ambienceMinDB, ambienceMaxDB, t);
            mainMixer.SetFloat(ambienceExposedParameter, db);
        }
    }

    // ---------- Методы сброса ----------

    public void ResetMouseSensitivity()
    {
        float val = Mathf.InverseLerp(mouseSensitivityMin, mouseSensitivityMax, mouseSensitivityDefault);
        mouseSensitivitySlider.value = val;
        ApplyMouseSensitivity(val);
    }

    public void ResetBrightness()
    {
        float val = Mathf.InverseLerp(brightnessMin, brightnessMax, brightnessDefault);
        brightnessSlider.value = val;
        ApplyBrightness(val);
    }

    public void ResetSFXVolume()
    {
        float val = Mathf.InverseLerp(sfxMinDB, sfxMaxDB, sfxDefaultDB);
        sfxVolumeSlider.value = val;
        ApplySFXVolume(val);
    }

    public void ResetAmbienceVolume()
    {
        float val = Mathf.InverseLerp(ambienceMinDB, ambienceMaxDB, ambienceDefaultDB);
        ambienceVolumeSlider.value = val;
        ApplyAmbienceVolume(val);
    }
}