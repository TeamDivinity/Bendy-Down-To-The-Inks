using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class DynamicFocusSystem : MonoBehaviour
{
    [Header("Raycasting")]
    [SerializeField] private LayerMask focusableLayers = ~0;
    [SerializeField] private float maxFocusDistance = 100f;
    [SerializeField] private float raycastRadius = 0.1f;
    
    [Header("Focus Behavior")]
    [SerializeField] private float focusSpeed = 5f;
    [SerializeField] private float defaultFocusDistance = 10f;
    
    [Header("Depth of Field Settings")]
    [SerializeField] private float minAperture = 1.4f;
    [SerializeField] private float maxAperture = 16f;
    [SerializeField] private float currentAperture = 5.6f;
    [SerializeField] private float focalLength = 50f;
    
    [Header("References")]
    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private Transform focusOrigin;
    
    private DepthOfField depthOfField;
    private float targetFocusDistance;
    private float currentFocusDistance;
    private Camera mainCamera;

    private void Start()
    {
        mainCamera = Camera.main;
        
        if (focusOrigin == null)
            focusOrigin = mainCamera.transform;
        
        if (postProcessVolume == null)
            postProcessVolume = FindFirstObjectByType<Volume>();
        
        if (postProcessVolume != null && postProcessVolume.profile.TryGet(out depthOfField))
        {
            depthOfField.mode.Override(DepthOfFieldMode.Bokeh);
            depthOfField.active = true;
        }
        else
        {
            Debug.LogError("DynamicFocusSystem: No Depth of Field effect found in volume profile.");
        }
        
        currentFocusDistance = defaultFocusDistance;
        targetFocusDistance = defaultFocusDistance;
    }

    private void Update()
    {
        if (depthOfField == null) return;
        
        UpdateFocusTarget();
        SmoothFocusTransition();
        ApplyDepthOfFieldSettings();
    }

    private void UpdateFocusTarget()
    {
        Ray ray = new Ray(focusOrigin.position, focusOrigin.forward);
        
        if (Physics.SphereCast(ray, raycastRadius, out RaycastHit hit, maxFocusDistance, focusableLayers))
        {
            targetFocusDistance = hit.distance;
        }
        else
        {
            targetFocusDistance = defaultFocusDistance;
        }
    }

    private void SmoothFocusTransition()
    {
        currentFocusDistance = Mathf.Lerp(currentFocusDistance, targetFocusDistance, focusSpeed * Time.deltaTime);
    }

    private void ApplyDepthOfFieldSettings()
    {
        depthOfField.focusDistance.Override(currentFocusDistance);
        depthOfField.aperture.Override(currentAperture);
        depthOfField.focalLength.Override(focalLength);
    }

    public void SetAperture(float aperture)
    {
        currentAperture = Mathf.Clamp(aperture, minAperture, maxAperture);
    }

    public void FocusOnObject(Transform target)
    {
        if (target != null)
        {
            float distance = Vector3.Distance(focusOrigin.position, target.position);
            targetFocusDistance = distance;
        }
    }

    public void FocusOnPoint(Vector3 worldPoint)
    {
        float distance = Vector3.Distance(focusOrigin.position, worldPoint);
        targetFocusDistance = distance;
    }

    private void OnDrawGizmosSelected()
    {
        Transform origin = focusOrigin != null ? focusOrigin : transform;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawRay(origin.position, origin.forward * currentFocusDistance);
        Gizmos.DrawWireSphere(origin.position + origin.forward * currentFocusDistance, 0.2f);
    }
}