using UnityEngine;

public class TextureScroller : MonoBehaviour
{
    [Header("Настройки")]
    [Tooltip("Скорость движения текстуры по оси X")]
    public float speedX = 0.5f;

    private Renderer rend;
    private Vector2 offset;

    void Start()
    {
        // Получаем компонент Renderer с этого объекта
        rend = GetComponent<Renderer>();
    }

    void Update()
    {
        // Получаем текущее смещение текстуры
        offset = rend.material.mainTextureOffset;

        // Увеличиваем смещение по X на скорость * время (для плавности)
        offset.y -= speedX * Time.deltaTime;

        // Применяем новое смещение к материалу
        rend.material.mainTextureOffset = offset;
    }
}