using UnityEngine;

public class LookAtAudioPlayer : MonoBehaviour
{
    [Header("Настройки обзора")]
    public float checkRange = 3f;
    public LayerMask checkLayers = ~0;
    public Camera playerCamera;

    [Header("Звук")]
    public AudioClip clip;
    [Range(0f, 10f)] public float volume = 10f;

    [Header("Условия")]
    public bool requireActive = true;

    private AudioSource audioSource;
    private bool hasPlayed = false;

    void Start()
    {
        if (playerCamera == null) playerCamera = Camera.main;

        audioSource = GetComponent<AudioSource>();
        if (!audioSource)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f;
        }
    }

    void Update()
    {
        if (hasPlayed || clip == null || !gameObject.activeInHierarchy) return;
        if (requireActive && !gameObject.activeSelf) return;

        if (CheckLook())
        {
            audioSource.PlayOneShot(clip, volume);
            hasPlayed = true;
        }
    }

    bool CheckLook()
    {
        if (playerCamera == null) return false;

        Vector3 origin = playerCamera.transform.position + playerCamera.transform.forward * 0.1f;
        float length = checkRange - 0.1f;

        if (Physics.Raycast(origin, playerCamera.transform.forward, out RaycastHit hit, length, checkLayers))
        {
            return hit.collider != null && 
                   (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform));
        }
        return false;
    }
}