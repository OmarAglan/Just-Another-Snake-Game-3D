using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenu : MonoBehaviour
{
    public void EnterGame(string level)
    {
        SceneManager.LoadScene(level);
        Time.timeScale = 1;
    }

    public void ExitButton()
    {
        Application.Quit();
    }
}
