using UnityEngine;
using System.Collections.Generic;

public class RandomPlacement : MonoBehaviour
{
    [Header("Точки спавна")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("Настройки отображения")]
    public bool startHidden = false;

    public Vector3 OriginalPosition { get; private set; }
    public Quaternion OriginalRotation { get; private set; }
    public bool IsPickedUp { get; private set; }
    public bool IsInstalled { get; private set; }

    private Collider _collider;
    private Renderer[] _renderers;
    private OutlineFx.OutlineFx[] _outlines;

    private void Awake()
    {
        OriginalPosition = transform.position;
        OriginalRotation = transform.rotation;

        _collider = GetComponent<Collider>();
        _renderers = GetComponentsInChildren<Renderer>();
        _outlines = GetComponentsInChildren<OutlineFx.OutlineFx>();

        if (startHidden)
            SetVisibility(false);
    }

    private void Start()
    {
        if (spawnPoints == null || spawnPoints.Count == 0)
        {
            IsInstalled = true;
            return;
        }

        int index = Random.Range(0, spawnPoints.Count);
        Transform point = spawnPoints[index];
        transform.SetPositionAndRotation(point.position, point.rotation);
    }

    public void PickUp()
    {
        if (IsPickedUp || IsInstalled) return;

        SetVisibility(false);
        IsPickedUp = true;
    }

    public void Install()
    {
        if ((!IsPickedUp && !startHidden) || IsInstalled) return;

        transform.SetPositionAndRotation(OriginalPosition, OriginalRotation);
        SetVisibility(true);

        IsPickedUp = false;
        IsInstalled = true;
    }

    private void SetVisibility(bool visible)
    {
        if (_collider != null)
            _collider.enabled = visible;

        foreach (var renderer in _renderers)
            if (renderer != null) renderer.enabled = visible;

        foreach (var outline in _outlines)
            if (outline != null) outline.enabled = visible;
    }
}