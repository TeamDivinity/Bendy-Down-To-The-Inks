using UnityEngine;

public class GENTPipe : MonoBehaviour, IInteractable
{
    [Header("Прикрепление к игроку")]
    [Tooltip("Родительский объект, к которому прикрепится труба (например, слот для предмета в руке)")]
    public Transform attachParent;
    
    [Tooltip("Локальное смещение относительно родителя")]
    public Vector3 attachPosition = Vector3.zero;
    
    [Tooltip("Local Rotation")]
    public Vector3 attachRotation = Vector3.zero;
    
    [Tooltip("Новый слой объекта после взятия (например, 'HeldItem')")]
    public int targetLayer = 0;
    
    [Header("Настройки поведения")]
    [Tooltip("Отключать ли коллайдер после взятия")]
    public bool disableColliderOnPickup = true;
    
    [Tooltip("Отключать ли этот скрипт после взятия")]
    public bool disableScriptOnPickup = true;
    
    [Tooltip("Звук взятия (опционально)")]
    public AudioClip pickupSound;
    
    [Header("Интеграция с боевой системой")]
    [Tooltip("Ссылка на компонент атаки игрока. После подбора включит возможность атаковать.")]
    [SerializeField] private PlayerAttack playerAttack;
    
    private AudioSource audioSource;
    private Collider objectCollider;
    private bool isTaken = false;
    
    private void Awake()
    {
        objectCollider = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
    }
    
    public void OnInteract()
    {
        if (isTaken || attachParent == null) return;
        
        isTaken = true;
        
        // 1. Прикрепляем трубу
        transform.SetParent(attachParent);
        transform.localPosition = attachPosition;
        transform.localRotation = Quaternion.Euler(attachRotation);
        
        // 2. Меняем слой
        SetLayerRecursively(gameObject, targetLayer);
        
        // 3. Дополнительные действия
        if (disableColliderOnPickup && objectCollider != null)
            objectCollider.enabled = false;
        
        if (pickupSound != null && audioSource != null)
            audioSource.PlayOneShot(pickupSound);
        
        if (playerAttack != null)
            playerAttack.HasWeapon = true;
        
        // 5. Отключаем скрипт, если нужно
        if (disableScriptOnPickup)
            enabled = false;
    }
    
    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
            SetLayerRecursively(child.gameObject, newLayer);
    }
}