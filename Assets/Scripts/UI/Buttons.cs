using UnityEngine;

public class Buttons : MonoBehaviour
{
    [Header("Меню")]
    [SerializeField] private CanvasGroup settingsMenuGroup;
    [SerializeField] private CanvasGroup currentMenuGroup;
    [SerializeField] private bool settingsOpenAtStart = false;

    private void Awake() => SetSettingsOpen(settingsOpenAtStart);

    public void Resume() => Pause.Instance?.TogglePause();

    public void ToggleSettings()
    {
        bool isOpen = IsSettingsOpen();
        SetSettingsOpen(!isOpen);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary> Публичный метод – проверяет, открыто ли сейчас меню настроек </summary>
    public bool IsSettingsOpen()
    {
        return settingsMenuGroup != null && settingsMenuGroup.alpha > 0.5f;
    }

    private void SetSettingsOpen(bool open)
    {
        if (settingsMenuGroup != null)
        {
            settingsMenuGroup.alpha = open ? 1f : 0f;
            settingsMenuGroup.interactable = open;
            settingsMenuGroup.blocksRaycasts = open;
        }

        if (currentMenuGroup != null)
        {
            currentMenuGroup.alpha = open ? 0f : 1f;
            currentMenuGroup.interactable = !open;
            currentMenuGroup.blocksRaycasts = !open;
        }
    }
}