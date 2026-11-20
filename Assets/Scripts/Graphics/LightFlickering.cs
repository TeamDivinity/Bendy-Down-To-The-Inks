using UnityEngine;

[RequireComponent(typeof(Light))]
public class FastLightFlicker : MonoBehaviour
{
    [Header("Настройки мерцания")]
    [Range(0f, 5f)] public float minIntensity = 0f;
    [Range(0f, 5f)] public float maxIntensity = 3f;
    [Range(0.01f, 1f)] public float baseInterval = 0.1f;
    [Range(0f, 0.5f)] public float intervalDeviation = 0.03f;

    [Header("Эмиссия")]
    [Tooltip("Объект, материал которого будет мигать в такт свету")]
    public GameObject targetEmissiveObject;

    private Light lightSource;
    private Material matInstance;
    private Color originalEmission;
    private bool hasEmission;

    private float timer = 0f;
    private float currentInterval = 0f;

    void Start()
    {
        lightSource = GetComponent<Light>();
        if (lightSource == null)
        {
            Debug.LogError("Компонент Light не найден!", this);
            enabled = false;
            return;
        }

        if (targetEmissiveObject != null)
        {
            var renderer = targetEmissiveObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                // Создаём инстанс материала только для этого объекта
                matInstance = renderer.material;
                
                // Сохраняем исходный цвет эмиссии из материала
                if (matInstance.HasProperty("_EmissionColor"))
                {
                    originalEmission = matInstance.GetColor("_EmissionColor");
                    hasEmission = true;
                }
            }
        }
        SetNewInterval();
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= currentInterval)
        {
            lightSource.intensity = Random.Range(minIntensity, maxIntensity);

            // Синхронное переключение эмиссии в такт свету
            if (hasEmission)
            {
                // Включаем, если интенсивность выше середины диапазона
                bool isOn = lightSource.intensity >= (minIntensity + maxIntensity) * 0.5f;
                matInstance.SetColor("_EmissionColor", isOn ? originalEmission : Color.black);

                // Управление ключевым словом для корректной работы GI
                if (isOn) matInstance.EnableKeyword("_EMISSION");
                else      matInstance.DisableKeyword("_EMISSION");
            }

            timer = 0f;
            SetNewInterval();
        }
    }

    void SetNewInterval()
    {
        currentInterval = Mathf.Max(0.01f, baseInterval + Random.Range(-intervalDeviation, intervalDeviation));
    }
}