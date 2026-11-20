using UnityEngine;
using UnityEngine.SceneManagement;


public class NewGameButton : MonoBehaviour
{
    public void changeScene(int scene)
    {
        SceneManager.LoadScene(scene);
    }
    public void hideCursor()
    {
        Cursor.visible = false;
    }
}