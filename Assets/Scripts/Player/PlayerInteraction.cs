using UnityEngine;
using System.Collections.Generic;
public class InteractionController : MonoBehaviour
{
    [Header("Настройки")]
    public float interactRange = 3f;
    public KeyCode interactKey = KeyCode.E;
    public Camera playerCamera;
    [Header("Фильтр слоёв")]
    public LayerMask interactableLayers = ~0;
    [Header("Звук наведения")]
    public AudioClip hoverSound;
    private AudioSource audioSource;
    private List<OutlineFx.OutlineFx> _currentHoveredOutlines;
    void Start()
    {
        if (playerCamera == null)
            playerCamera = Camera.main;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
    }
    void Update()
    {
        HandleHover();
        if (Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }
    void HandleHover()
    {
        RaycastHit hit;
        List<OutlineFx.OutlineFx> newOutlines = new List<OutlineFx.OutlineFx>();
        Vector3 rayOrigin = playerCamera.transform.position + playerCamera.transform.forward * 0.1f;
        float rayLength = interactRange - 0.1f;
        if (Physics.Raycast(rayOrigin, playerCamera.transform.forward, out hit, rayLength, interactableLayers))
        {
            hit.collider.transform.GetComponentsInChildren<OutlineFx.OutlineFx>(newOutlines);
        }
        bool hoverChanged = false;
        if (_currentHoveredOutlines == null || newOutlines.Count == 0)
        {
            hoverChanged = (_currentHoveredOutlines == null && newOutlines.Count > 0) ||
                           (_currentHoveredOutlines != null && newOutlines.Count == 0);
        }
        else if (_currentHoveredOutlines.Count != newOutlines.Count)
        {
            hoverChanged = true;
        }
        else
        {
            for (int i = 0; i < newOutlines.Count; i++)
            {
                if (_currentHoveredOutlines[i] != newOutlines[i])
                {
                    hoverChanged = true;
                    break;
                }
            }
        }
        if (_currentHoveredOutlines != null)
        {
            foreach (var o in _currentHoveredOutlines)
                if (o != null) o.Alpha = 0f;
        }
        if (newOutlines.Count > 0)
        {
            foreach (var o in newOutlines)
                if (o != null) o.Alpha = 1f;
            if (hoverChanged && hoverSound != null)
            {
                audioSource.PlayOneShot(hoverSound);
            }
        }
        _currentHoveredOutlines = newOutlines.Count > 0 ? newOutlines : null;
    }
    void Interact()
    {
        RaycastHit hit;
        Vector3 rayOrigin = playerCamera.transform.position + playerCamera.transform.forward * 0.1f;
        float rayLength = interactRange - 0.1f;
        if (Physics.Raycast(rayOrigin, playerCamera.transform.forward, out hit, rayLength, interactableLayers))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            if (interactable != null)
            {
                interactable.OnInteract();
            }
        }
    }
}