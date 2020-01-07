using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [HideInInspector]
    public PlayerDirection direction;

    public float stepLength = 0.1f;
    public float movementFraqancy = 0.1f;
    public GameObject tailPrefap;
    public GameObject pauseMenu;

    private List<Vector3> deltaPositon;
    private List<Rigidbody> nodes;
    private Rigidbody mainBody;
    private Rigidbody headBody;
    private Transform tr;
    private float counter;
    private bool move;
    private bool createNodeAtTail;

    // Start is called before the first frame update
    void Awake()
    {
        tr = transform;
        mainBody = GetComponent<Rigidbody>();

        IntSnakeNodes();
        IntPlayer();

        deltaPositon = new List<Vector3>()
        {
            new Vector3(-stepLength, 0f), // -dx .... Left
            new Vector3(0f, stepLength), // dy ...... Up
            new Vector3(stepLength, 0f), // dx ..... Right
            new Vector3(0f, -stepLength) // -dy ..... Down
        };
    }

    // Update is called once per frame
    void Update()
    {
        CheckMovementFraquncy();
    }

    void FixedUpdate()
    {
        if (move)
        {
            move = false;

            Move();
        }
    }

    void IntSnakeNodes()
    {
        nodes = new List<Rigidbody>();
        nodes.Add(tr.GetChild(0).GetComponent<Rigidbody>());
        nodes.Add(tr.GetChild(1).GetComponent<Rigidbody>());
        nodes.Add(tr.GetChild(2).GetComponent<Rigidbody>());

        headBody = nodes[0];
    }

    void SetDirectionRandom()
    {
        direction = (PlayerDirection)Random.Range(0, (int)PlayerDirection.COUNT);
    }

    void IntPlayer()
    {
        SetDirectionRandom();

        switch (direction)
        {
            case PlayerDirection.RIGHT:
                nodes[1].position = nodes[0].position - new Vector3(Metrics.node, 0f, 0f);
                nodes[2].position = nodes[0].position - new Vector3(Metrics.node * 2f, 0f, 0f);
                break;
            case PlayerDirection.LEFT:
                nodes[1].position = nodes[0].position + new Vector3(Metrics.node, 0f, 0f);
                nodes[2].position = nodes[0].position + new Vector3(Metrics.node * 2f, 0f, 0f);
                break;
            case PlayerDirection.UP:
                nodes[1].position = nodes[0].position - new Vector3(0f, Metrics.node, 0f);
                nodes[2].position = nodes[0].position - new Vector3(0f, Metrics.node * 2f, 0f);
                break;
            case PlayerDirection.DOWN:
                nodes[1].position = nodes[0].position - new Vector3(0f, Metrics.node, 0f);
                nodes[2].position = nodes[0].position - new Vector3(0f, Metrics.node * 2f, 0f);
                break;
        }
    }

    void Move()
    {
        Vector3 parentPos = headBody.position;
        Vector3 prevPos;

        mainBody.position = mainBody.position + deltaPositon[(int)direction];
        headBody.position = headBody.position + deltaPositon[(int)direction];

        for (int i = 1; i < nodes.Count; i++)
        {
            prevPos = nodes[i].position;
            nodes[i].position = parentPos;
            parentPos = prevPos;
        }

        //Create New Body Part if snake ate friut
        if (createNodeAtTail)
        {
            createNodeAtTail = false;
            GameObject newNode = Instantiate(tailPrefap, nodes[nodes.Count - 1].position, Quaternion.identity);
            newNode.transform.SetParent(transform, true);
            nodes.Add(newNode.GetComponent<Rigidbody>());
        }
    }
    void CheckMovementFraquncy()
    {
        counter += Time.deltaTime;

        if (counter >= movementFraqancy)
        {
            counter = 0;
            move = true;
        }
    }

    public void SetInputDirection(PlayerDirection dir)
    {
        if (dir == PlayerDirection.UP && direction == PlayerDirection.DOWN ||
            dir == PlayerDirection.DOWN && direction == PlayerDirection.UP ||
            dir == PlayerDirection.RIGHT && direction == PlayerDirection.LEFT ||
            dir == PlayerDirection.LEFT && direction == PlayerDirection.RIGHT)
        {
            return;
        }

        direction = dir;
        ForceMove();
    }

    void ForceMove()
    {
        counter = 0;
        move = false;
        Move();
    }

    void OnTriggerEnter(Collider target)
    {
        if (target.tag == Tags.fruit)
        {
            target.gameObject.SetActive(false);
            createNodeAtTail = true;
            GamePlayController.intsance.IncraseScore();
            AuidoManager.instance.PlayPickUpSound();
        }
        if (target.tag == Tags.bomb || target.tag == Tags.tail)
        {
            AuidoManager.instance.PlayDeadSound();
            Time.timeScale = 0f;
            Debug.Log("Hit Tail");
            pauseMenu.SetActive(true);
        }
        if (target.tag == Tags.wall)
        {
            AuidoManager.instance.PlayHitWallSound();
            AuidoManager.instance.PlayDeadSound();
            Time.timeScale = 0f;
            Debug.Log("Hit Wall");
            pauseMenu.SetActive(true);
        }
    }
}
