using UnityEngine;
using System.Collections;
public class PuzzleDoor : MonoBehaviour
{
    [Header("Последовательность рычагов")]
    public Lever[] sequence;
    [Header("Настройки двери")]
    public float openHeight = 4f;
    public float moveDuration = 2f;
    public AnimationCurve movementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [Header("Звук")]
    public AudioClip doorOpenSound;     
    public float soundVolume = 1f;      
    private Vector3 closedPosition;
    private Vector3 targetPosition;
    private bool[] previousLeverStates;
    private bool resetRequired = false;
    private int currentStep = 0;
    private Collider doorCollider;
    private AudioSource audioSource;
    void Start()
    {
        closedPosition = transform.position;
        targetPosition = closedPosition + Vector3.up * openHeight;
        doorCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null && doorOpenSound != null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 1f; 
        }
        if (audioSource != null)
        {
            audioSource.clip = doorOpenSound;
            audioSource.volume = soundVolume;
        }
        if (sequence != null && sequence.Length > 0)
        {
            previousLeverStates = new bool[sequence.Length];
            for (int i = 0; i < sequence.Length; i++)
            {
                if (sequence[i] != null)
                    previousLeverStates[i] = sequence[i].IsPulled();
            }
        }
    }
    void Update()
    {
        CheckLeverStates();
    }
    void CheckLeverStates()
    {
        if (sequence == null || previousLeverStates == null) return;
        for (int i = 0; i < sequence.Length; i++)
        {
            if (sequence[i] == null) continue;
            bool currentState = sequence[i].IsPulled();
            if (currentState != previousLeverStates[i])
            {
                if (currentState)
                    HandleLeverPulled(i);
                else
                    HandleLeverRaised(i);
                previousLeverStates[i] = currentState;
            }
        }
    }
    void HandleLeverPulled(int leverIndex)
    {
        if (resetRequired) return;
        if (leverIndex == currentStep)
        {
            currentStep++;
            if (currentStep >= sequence.Length)
                StartCoroutine(OpenDoorCoroutine());
        }
        else if (leverIndex > currentStep)
        {
            ResetSequence();
        }
    }
    void HandleLeverRaised(int leverIndex)
    {
        if (resetRequired && AreAllLeversRaised())
        {
            resetRequired = false;
            currentStep = 0;
        }
    }
    bool AreAllLeversRaised()
    {
        for (int i = 0; i < sequence.Length; i++)
        {
            if (sequence[i] != null && sequence[i].IsPulled())
                return false;
        }
        return true;
    }
    void ResetSequence()
    {
        currentStep = 0;
        resetRequired = true;
    }
    IEnumerator OpenDoorCoroutine()
    {
        if (audioSource != null && doorOpenSound != null)
        {
            audioSource.PlayOneShot(doorOpenSound, soundVolume);
        }
        Vector3 startPosition = transform.position;
        float elapsedTime = 0f;
        while (elapsedTime < moveDuration)
        {
            elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsedTime / moveDuration);
            transform.position = Vector3.Lerp(startPosition, targetPosition, movementCurve.Evaluate(progress));
            yield return null;
        }
        transform.position = targetPosition;
        if (doorCollider != null)
            doorCollider.enabled = false;
        enabled = false;
    }
    public void ResetDoor()
    {
        StopAllCoroutines();
        transform.position = closedPosition;
        if (doorCollider != null)
            doorCollider.enabled = true;
        resetRequired = false;
        currentStep = 0;
        enabled = true;
        if (sequence != null && previousLeverStates != null)
        {
            for (int i = 0; i < sequence.Length; i++)
            {
                if (sequence[i] != null)
                    previousLeverStates[i] = sequence[i].IsPulled();
            }
        }
    }
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 worldTarget = transform.position + Vector3.up * openHeight;
        Gizmos.DrawLine(transform.position, worldTarget);
        Gizmos.DrawWireSphere(worldTarget, 0.3f);
    }
}