using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

public class IntroManager : MonoBehaviour
{
    public int waitTime;
    void Start()
    {
        StartCoroutine(WaitForLevel());
    }
    IEnumerator WaitForLevel()
    {
        yield return new WaitForSeconds(waitTime);
        SceneManager.LoadScene(1);
    }
}