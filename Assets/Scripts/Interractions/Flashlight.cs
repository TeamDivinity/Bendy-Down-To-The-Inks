using UnityEngine;

public class Flashlight : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private Light flashlightLight; // Ссылка на компонент Light
    [SerializeField] private bool isLightOn = true; // Начальное состояние (включен по умолчанию)

    [Header("Controls")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F; // Кнопка включения

    [Header("Sound Settings")]
    [SerializeField] private AudioClip toggleSound; // Звук щелчка
    [SerializeField] private float soundVolume = 1f; // Громкость звука

    [Header("Optional")]
    [SerializeField] private AudioSource audioSource; // Можно указать свой AudioSource или использовать встроенный

    private void Start()
    {
        // Если Light не назначен, пробуем найти его на этом объекте
        if (flashlightLight == null)
        {
            flashlightLight = GetComponent<Light>();
        }

        // Если AudioSource не назначен, создаём его на объекте
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Устанавливаем начальное состояние света
        if (flashlightLight != null)
        {
            flashlightLight.enabled = isLightOn;
        }
    }

    private void Update()
    {
        // Проверка нажатия кнопки
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleLight();
        }
    }

    public void ToggleLight()
    {
        if (flashlightLight == null)
        {
            Debug.LogWarning("Flashlight: Light component not found!");
            return;
        }

        // Переключаем состояние
        isLightOn = !isLightOn;
        flashlightLight.enabled = isLightOn;

        // Проигрываем звук
        PlayToggleSound();

        // Опционально: вывод в консоль для отладки
        // Debug.Log("Flashlight: " + (isLightOn ? "ON" : "OFF"));
    }

    public void TurnOn()
    {
        if (!isLightOn)
        {
            ToggleLight();
        }
    }

    public void TurnOff()
    {
        if (isLightOn)
        {
            ToggleLight();
        }
    }

    private void PlayToggleSound()
    {
        if (toggleSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(toggleSound, soundVolume);
        }
    }

    // Публичные методы для доступа к состоянию из других скриптов
    public bool IsLightOn()
    {
        return isLightOn;
    }

    public void SetLightState(bool state)
    {
        if (state != isLightOn)
        {
            ToggleLight();
        }
    }
}