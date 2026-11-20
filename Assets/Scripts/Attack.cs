using UnityEngine;
using System.Collections;

[RequireComponent(typeof(AudioSource))]
public class PlayerAttack : MonoBehaviour
{
    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 3f;
    [SerializeField] private float attackCooldown = 0.5f;
    [SerializeField] private LayerMask hitLayers = ~0;
    [SerializeField] private int attackDamage = 10;

    [Header("State")]
    [SerializeField] private bool hasWeapon; // Управляй этим флагом из системы подбора
    public bool HasWeapon { get => hasWeapon; set => hasWeapon = value; }
    public bool CanAttack => hasWeapon && Time.time >= nextAttackTime;

    [Header("References")]
    [SerializeField] private Animation legacyAnimation;
    [SerializeField] private string attackAnimationName = "Attack";

    [Header("Camera Shake")]
    [SerializeField] private Transform cameraToShake;
    [SerializeField] private float shakeDuration = 0.12f;
    [SerializeField] private float shakeIntensity = 0.06f;
    [SerializeField] private AnimationCurve shakeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Audio")]
    [SerializeField] private AudioClip[] hitSounds;
    [SerializeField] private AudioClip[] missSounds;
    [SerializeField] [Range(0f, 1f)] private float attackSoundVolume = 1f;

    [Header("VFX")]
    [SerializeField] private GameObject hitEffectPrefab;

    private AudioSource audioSource;
    private float nextAttackTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (cameraToShake == null) cameraToShake = Camera.main?.transform;
        if (legacyAnimation == null) legacyAnimation = GetComponent<Animation>();
    }

    private void Update()
    {
        if (!hasWeapon || Pause.IsPaused) return;

        if (Input.GetMouseButtonDown(0) && Time.time >= nextAttackTime)
        {
            PerformAttack();
        }
    }

    private void PerformAttack()
    {
        nextAttackTime = Time.time + attackCooldown;

        // 1. Анимация
        if (legacyAnimation != null && !string.IsNullOrEmpty(attackAnimationName))
            legacyAnimation.Play(attackAnimationName);

        // 2. Рейкаст + определение попадания
        bool didHit = TryPerformRaycast();

        // 3. Звук
        PlayAttackSound(didHit);

        // 4. Тряска камеры (только при попадании)
        if (didHit)
            StartCoroutine(ShakeCameraCoroutine());
    }

    private bool TryPerformRaycast()
    {
        Transform aimSource = cameraToShake ?? Camera.main?.transform;
        if (aimSource == null) return false;

        if (Physics.Raycast(aimSource.position, aimSource.forward, out RaycastHit hit, attackRange, hitLayers))
        {
            if (hit.collider.TryGetComponent<IDamageable>(out IDamageable target))
                target.TakeDamage(attackDamage);

            if (hitEffectPrefab != null)
                Instantiate(hitEffectPrefab, hit.point, Quaternion.LookRotation(hit.normal));
                
            return true;
        }
        return false;
    }

    private void PlayAttackSound(bool didHit)
    {
        if (audioSource == null) return;
        
        AudioClip[] clips = didHit ? hitSounds : missSounds;
        if (clips == null || clips.Length == 0) return;

        audioSource.PlayOneShot(clips[UnityEngine.Random.Range(0, clips.Length)], attackSoundVolume);
    }

    private IEnumerator ShakeCameraCoroutine()
    {
        Vector3 originalPos = cameraToShake.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float t = elapsed / shakeDuration;
            float intensity = shakeCurve.Evaluate(t);

            cameraToShake.localPosition = originalPos + new Vector3(
                UnityEngine.Random.Range(-1f, 1f) * shakeIntensity * intensity,
                UnityEngine.Random.Range(-1f, 1f) * shakeIntensity * intensity,
                0f
            );

            elapsed += Time.deltaTime;
            yield return null;
        }

        cameraToShake.localPosition = originalPos;
    }
}

public interface IDamageable { void TakeDamage(int amount); }