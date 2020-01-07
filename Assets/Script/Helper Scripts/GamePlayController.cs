using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class GamePlayController : MonoBehaviour
{
    public static GamePlayController intsance;

    public GameObject fruitPickUp, BombPickUP;

    private float minX = -4.25f, maxX = 4.25f, minY = -2.26f, maxY = 2.26f;
    private float zPos = 5.8f;

    private Text scoreText;
    private int scoreCount;
    void Awake()
    {
        MakeIntsance();
    }

    void Start()
    {
        scoreText = GameObject.Find("Score").GetComponent<Text>();

        Invoke("StartSpwaing", 0.5f);
    }

    void MakeIntsance()
    {
        if (intsance == null)
        {
            intsance = this;
        }
    }

    void StartSpwaing()
    {
        StartCoroutine(SpawnPickUps());
    }

    public void CancelSpwaing()
    {
        CancelInvoke("StartSpwaing");
    }

    IEnumerator SpawnPickUps()
    {
        yield return new WaitForSeconds(Random.Range(1f, 3f));

        if (Random.Range(0, 10) >= 2)
        {
            Instantiate(fruitPickUp, new Vector3(Random.Range(minX,maxX),
                Random.Range(minY,maxY), zPos), Quaternion.identity);
        }
        else
        {
            Instantiate(BombPickUP, new Vector3(Random.Range(minX, maxX),
            Random.Range(minY, maxY), zPos), Quaternion.identity);
            AuidoManager.instance.PlayBombSound();
        }

        Invoke("StartSpwaing", 0f);
    }

    public void IncraseScore()
    {
        scoreCount++;
        scoreText.text = "Score: " + scoreCount;
    }
}
