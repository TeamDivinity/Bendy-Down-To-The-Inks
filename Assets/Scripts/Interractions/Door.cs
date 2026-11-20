using UnityEngine;
public class Door : MonoBehaviour, IInteractable
{
    [Header("Настройки двери")]
    public float openAngle = 90f;
    public float smoothSpeed = 2f;
    public float stopThreshold = 0.1f;
    public enum RotationAxis { X, Y, Z }
    public RotationAxis axis = RotationAxis.Y;
    [Header("Настройки звука")] 
    public AudioClip openSound;
    public AudioClip closeSound;
    private bool isOpen = false;
    private Quaternion closedRotation;
    private Quaternion openRotation;
    private Transform myTransform;
    private AudioSource audioSource; 
    void Start()
    {
        myTransform = transform;
        closedRotation = myTransform.localRotation;
        Vector3 axisVector = Vector3.zero;
        if (axis == RotationAxis.X) axisVector = Vector3.right;
        else if (axis == RotationAxis.Y) axisVector = Vector3.up;
        else if (axis == RotationAxis.Z) axisVector = Vector3.forward;
        openRotation = closedRotation * Quaternion.AngleAxis(openAngle, axisVector);
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        enabled = false; 
    }
    public void OnInteract()
    {
        Toggle();
    }
    public void Toggle()
    {
        isOpen = !isOpen;
        enabled = true; 
        if (audioSource != null)
        {
            if (isOpen)
            {
                if (openSound != null)
                {
                    audioSource.PlayOneShot(openSound);
                }
            }
            else
            {
                if (closeSound != null)
                {
                    audioSource.PlayOneShot(closeSound);
                }
            }
        }
    }
    void Update()
    {
        Quaternion targetRotation = isOpen ? openRotation : closedRotation;
        myTransform.localRotation = Quaternion.Slerp(
            myTransform.localRotation, 
            targetRotation, 
            Time.deltaTime * smoothSpeed
        );
        float angleDiff = Quaternion.Angle(myTransform.localRotation, targetRotation);
        if (angleDiff < stopThreshold)
        {
            myTransform.localRotation = targetRotation;
            enabled = false; 
        }
    }
}