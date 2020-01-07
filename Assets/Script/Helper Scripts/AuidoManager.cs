using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AuidoManager : MonoBehaviour
{
    public static AuidoManager instance;

    public AudioClip pickUpSound, deadSound , bombSound, wallSound;
    void Awake()
    {
        MakeInstance();
    }

    void MakeInstance()
    {
        if (instance == null)
        {
            instance = this;
        }
    }

    public void PlayPickUpSound()
    {
        AudioSource.PlayClipAtPoint(pickUpSound, transform.position);
    }
    public void PlayDeadSound()
    {
        AudioSource.PlayClipAtPoint(deadSound, transform.position);
    }
    public void PlayHitWallSound()
    {
        AudioSource.PlayClipAtPoint(wallSound, transform.position);
    }
    public void PlayBombSound()
    {
        AudioSource.PlayClipAtPoint(bombSound, transform.position);
    }
}
