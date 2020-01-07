using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeactivePickUps : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Invoke("Deactived", Random.Range(3f, 6f));
    }

    void Deactived()
    {
        gameObject.SetActive(false);
    }
}
