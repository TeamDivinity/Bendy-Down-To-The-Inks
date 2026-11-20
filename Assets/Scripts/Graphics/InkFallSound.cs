using UnityEngine;
using System.Collections;
[RequireComponent(typeof(ParticleSystem))]
[RequireComponent(typeof(AudioSource))]
public class ParticleSoundOnSpawn : MonoBehaviour
{
    [Header("Настройки звука")]
    [Tooltip("Звук, который будет проигрываться")]
    public AudioClip spawnSound;
    [Tooltip("Задержка перед проигрыванием звука (в секундах)")]
    [Min(0f)]
    public float spawnDelay = 0f;
    [Tooltip("Громкость звука")]
    [Range(0f, 1f)]
    public float volume = 1f;
    [Header("Ограничения (Важно для производительности)")]
    [Tooltip("Максимальное количество звуков, которые могут проигрываться одновременно")]
    public int maxConcurrentSounds = 10;
    private ParticleSystem ps;
    private AudioSource audioSource;
    private int previousParticleCount = 0;
    private int currentActiveSounds = 0;
    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        audioSource = GetComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.loop = false;
    }
    void Update()
    {
        if (spawnSound == null) return;
        int currentCount = ps.particleCount;
        if (currentCount > previousParticleCount)
        {
            int newParticlesCount = currentCount - previousParticleCount;
            for (int i = 0; i < newParticlesCount; i++)
            {
                if (currentActiveSounds < maxConcurrentSounds)
                {
                    StartCoroutine(PlaySoundDelayed());
                    currentActiveSounds++;
                }
            }
        }
        previousParticleCount = currentCount;
    }
    private IEnumerator PlaySoundDelayed()
    {
        if (spawnDelay > 0f)
        {
            yield return new WaitForSeconds(spawnDelay);
        }
        if (audioSource != null && spawnSound != null)
        {
            audioSource.PlayOneShot(spawnSound, volume);
        }
        yield return new WaitForSeconds(spawnSound.length);
        currentActiveSounds--;
    }
    void OnDisable()
    {
        currentActiveSounds = 0;
        StopAllCoroutines();
    }
}