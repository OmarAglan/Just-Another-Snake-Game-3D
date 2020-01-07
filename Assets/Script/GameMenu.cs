using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameMenu : MonoBehaviour
{
    public AudioClip MainMenu;

    public void Awake()
    {
       Invoke("PlayMusic",1f);   
    }

    public void EnterGame(string level)
    {
        SceneManager.LoadScene(level);
        StopCoroutine("PlayMusic");
    }

    public void ExitButton()
    {
        Application.Quit();
    }

    IEnumerator PlayMusic()
    {
       AudioSource.PlayClipAtPoint(MainMenu, transform.position);
       return null;
    }
}
