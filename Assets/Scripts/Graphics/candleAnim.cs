using UnityEngine;

[RequireComponent(typeof(Transform))]
public class RandomScaleChanger : MonoBehaviour
{
    [Header("Диапазон размера")]
    public Vector3 minScale = new Vector3(0.5f, 0.5f, 0.5f);
    public Vector3 maxScale = new Vector3(1.5f, 1.5f, 1.5f);

    [Header("Диапазон скорости изменения (ед/сек)")]
    public float minSpeed = 1f;
    public float maxSpeed = 3f;

    [Header("Диапазон паузы между изменениями (сек)")]
    public float minInterval = 1f;
    public float maxInterval = 4f;

    // Приватные поля для внутренней логики
    private Transform tf;
    private Vector3 targetScale;
    private float currentSpeed;
    private float waitTimer;
    private bool isWaiting = true;

    private void Awake()
    {
        // Кешируем Transform, чтобы не вызывать GetComponent каждый кадр
        tf = transform;
        SetNewTarget();
        // Начинаем с паузы
        waitTimer = Random.Range(minInterval, maxInterval);
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (isWaiting)
        {
            waitTimer -= dt;
            if (waitTimer <= 0f)
            {
                isWaiting = false; // Начинаем плавное изменение
            }
        }
        else
        {
            // Плавное движение к цели с заданной скоростью
            tf.localScale = Vector3.MoveTowards(tf.localScale, targetScale, currentSpeed * dt);

            // Проверка достижения цели без дорогостоящего Sqrt
            if (Vector3.SqrMagnitude(tf.localScale - targetScale) < 0.0001f)
            {
                isWaiting = true;
                waitTimer = Random.Range(minInterval, maxInterval);
                SetNewTarget(); // Готовим следующие параметры
            }
        }
    }

    private void SetNewTarget()
    {
        targetScale = new Vector3(
            Random.Range(minScale.x, maxScale.x),
            Random.Range(minScale.y, maxScale.y),
            Random.Range(minScale.z, maxScale.z)
        );
        currentSpeed = Random.Range(minSpeed, maxSpeed);
    }
}