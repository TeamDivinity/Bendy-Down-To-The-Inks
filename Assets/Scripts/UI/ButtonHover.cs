using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System.Collections;
[RequireComponent(typeof(Graphic))]
public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Настройки")]
    [SerializeField, Tooltip("Длительность анимации")] private float hoverDuration = 0.2f;
    [SerializeField, Range(1f, 1.3f), Tooltip("Множитель масштаба при наведении")] private float hoverScale = 1.08f;
    [SerializeField] private bool useUnscaledTime = true;
    [Header("Цели (автопоиск если пусто)")]
    [Tooltip("Объект, чья прозрачность будет меняться (обычно дочерний TMP)")]
    [SerializeField] private Graphic targetAlphaObject;
    [Tooltip("Объект, который будет масштабироваться (обычно корень кнопки)")]
    [SerializeField] private RectTransform targetScaleObject;
    [Header("🔊 Звук")]
    [SerializeField, Tooltip("Звук при наведении на кнопку")] private AudioClip hoverSound;
    [SerializeField, Range(0f, 10f), Tooltip("Громкость звука")] private float soundVolume = 0.8f;
    private Coroutine animCoroutine;
    private float initialAlpha;
    private Vector3 initialScale;
    private AudioSource audioSource;
    private void Start()
    {
        if (targetAlphaObject == null) targetAlphaObject = GetComponentInChildren<TMP_Text>();
        if (targetAlphaObject == null) targetAlphaObject = GetComponentInChildren<Image>();
        if (targetScaleObject == null) targetScaleObject = GetComponent<RectTransform>();
        if (targetAlphaObject != null) initialAlpha = targetAlphaObject.color.a;
        initialScale = targetScaleObject != null ? targetScaleObject.localScale : Vector3.one;
        var graphic = GetComponent<Graphic>();
        if (graphic != null) graphic.raycastTarget = true;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.ignoreListenerPause = true; 
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Animate(true));
        if (hoverSound != null && audioSource != null)
        {
            audioSource.volume = soundVolume;
            audioSource.PlayOneShot(hoverSound);
        }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (animCoroutine != null) StopCoroutine(animCoroutine);
        animCoroutine = StartCoroutine(Animate(false));
    }
    private IEnumerator Animate(bool entering)
    {
        float elapsed = 0f;
        float startAlpha = targetAlphaObject != null ? targetAlphaObject.color.a : initialAlpha;
        Vector3 startScale = targetScaleObject != null ? targetScaleObject.localScale : initialScale;
        float targetAlpha = entering ? 1f : initialAlpha;
        Vector3 targetScale = entering ? initialScale * hoverScale : initialScale;
        while (elapsed < hoverDuration)
        {
            elapsed += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / hoverDuration);
            if (targetAlphaObject != null)
            {
                Color c = targetAlphaObject.color;
                c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                targetAlphaObject.color = c;
            }
            if (targetScaleObject != null)
            {
                targetScaleObject.localScale = Vector3.Lerp(startScale, targetScale, t);
            }
            yield return null;
        }
        if (targetAlphaObject != null)
        {
            Color c = targetAlphaObject.color;
            c.a = targetAlpha;
            targetAlphaObject.color = c;
        }
        if (targetScaleObject != null) targetScaleObject.localScale = targetScale;
        animCoroutine = null;
    }
}