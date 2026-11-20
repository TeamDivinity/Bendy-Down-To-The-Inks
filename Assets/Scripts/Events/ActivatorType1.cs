using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

/// <summary>
/// Универсальный активатор: при взаимодействии активирует указанные объекты
/// и (опционально) передаёт вызов другим IInteractable на этом же объекте.
/// </summary>
[RequireComponent(typeof(Collider))]
public class InteractActivator : MonoBehaviour, IInteractable
{
    [Header("🎯 Целевые объекты")]
    [Tooltip("Список объектов, состояние которых нужно изменить")]
    public List<GameObject> targets = new List<GameObject>();

    [Header("⚙️ Режим активации")]
    [Tooltip("Activate = включить, Deactivate = выключить, Toggle = переключить")]
    public ActivationMode mode = ActivationMode.Activate;

    [Header("🔗 Передача взаимодействия")]
    [Tooltip("Если на объекте есть другие IInteractable (напр. Lever), вызвать их после активации")]
    public bool forwardInteraction = true;

    [Header("🔊 События (опционально)")]
    public UnityEvent onActivated;

    public enum ActivationMode
    {
        Activate,
        Deactivate,
        Toggle
    }

    private List<IInteractable> _cachedInteractables;

    void Start()
    {
        // Кэшируем другие IInteractable на этом же объекте для передачи вызова
        if (forwardInteraction)
        {
            var components = GetComponents<IInteractable>();
            _cachedInteractables = new List<IInteractable>();
            foreach (var comp in components)
            {
                // Не добавляем себя, проверяем на null
                if (comp != null && (UnityEngine.Object)comp != (UnityEngine.Object)this)
                {
                    _cachedInteractables.Add(comp);
                }
            }
        }
    }

    public void OnInteract()
    {
        // 1. Применяем действие к целевым объектам
        foreach (var target in targets)
        {
            if (target != null)
            {
                switch (mode)
                {
                    case ActivationMode.Activate:
                        if (!target.activeSelf) target.SetActive(true);
                        break;
                    case ActivationMode.Deactivate:
                        if (target.activeSelf) target.SetActive(false);
                        break;
                    case ActivationMode.Toggle:
                        target.SetActive(!target.activeSelf);
                        break;
                }
            }
        }

        // 2. Вызываем подключенные UnityEvents
        onActivated?.Invoke();

        // 3. Передаём взаимодействие дальше (например, логика рычага)
        if (forwardInteraction && _cachedInteractables != null)
        {
            foreach (var interactable in _cachedInteractables)
            {
                if (interactable != null)
                {
                    interactable.OnInteract();
                }
            }
        }
    }
}