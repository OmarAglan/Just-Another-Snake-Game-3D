using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private PlayerController playerController;
    private int horizontal = 0;
    private int vertical = 0;

    private Vector3 fp;   //First touch position
    private Vector3 lp;   //Last touch position
    private float dragDistance;  //minimum distance for a swipe to be registered

    [HideInInspector]
    public bool up;
    [HideInInspector]
    public bool down;
    [HideInInspector]
    public bool left;
    [HideInInspector]
    public bool right;

    public enum Axis
    {
        horizontal,
        vertical
    }

    void Awake()
    {
        playerController = GetComponent<PlayerController>();
        dragDistance = Screen.height * 15 / 100; //dragDistance is 15% height of the screen
    }

    void Update()
    {
        horizontal = 0;
        vertical = 0;

        GetKayboardInput();
        SetMovement();

        //Swipe Controlls 
        if (Input.touchCount == 1) // user is touching the screen with a single touch
        {
            Touch touch = Input.GetTouch(0); // get the touch
            if (touch.phase == TouchPhase.Began) //check for the first touch
            {
                fp = touch.position;
                lp = touch.position;
            }
            else if (touch.phase == TouchPhase.Moved) // update the last position based on where they moved
            {
                lp = touch.position;
            }
            else if (touch.phase == TouchPhase.Ended) //check if the finger is removed from the screen
            {
                lp = touch.position;  //last touch position. Ommitted if you use list

                //Check if drag distance is greater than 20% of the screen height
                if (Mathf.Abs(lp.x - fp.x) > dragDistance || Mathf.Abs(lp.y - fp.y) > dragDistance)
                {//It's a drag
                 //check if the drag is vertical or horizontal
                    if (Mathf.Abs(lp.x - fp.x) > Mathf.Abs(lp.y - fp.y))
                    {   //If the horizontal movement is greater than the vertical movement...
                        if ((lp.x > fp.x))  //If the movement was to the right)
                        {   //Right swipe
                            Debug.Log("Right Swipe");
                            left = false;
                            right = true;
                        }
                        else
                        {   //Left swipe
                            Debug.Log("Left Swipe");
                            right = false;
                            left = true;
                        }
                    }
                    else
                    {   //the vertical movement is greater than the horizontal movement
                        if (lp.y > fp.y)  //If the movement was up
                        {   //Up swipe
                            Debug.Log("Up Swipe");
                            down = false;
                            up = true;
                        }
                        else
                        {   //Down swipe
                            Debug.Log("Down Swipe");
                            up = false;
                            down = true;
                        }
                    }
                }
            }
        }
    }

    void GetKayboardInput()
    {
        horizontal = GetAxisRaw(Axis.horizontal);
        vertical = GetAxisRaw(Axis.vertical);

        if (horizontal != 0)
        {
            vertical = 0;
        }
    }

    void SetMovement()
    {
        if (vertical != 0)
        {
            playerController.SetInputDirection((vertical == 1) ?
            PlayerDirection.UP : PlayerDirection.DOWN);

        }
        else if (horizontal != 0)
        {
            playerController.SetInputDirection((horizontal == 1) ?
            PlayerDirection.RIGHT : PlayerDirection.LEFT);
        }
    }

    int GetAxisRaw(Axis axis)
    {
        if (axis == Axis.horizontal)
        {
           // bool left = leftTouch.Pressed;
           //bool right = rightTouch.Pressed;

            if (left == true)
            {
                right = false;
                return -1;
            }

            if (right == true)
            {
                left = false;
                return 1;
            }
            return 0;
        }
        else if (axis == Axis.vertical)
        {
            //bool up = upTouch.Pressed;
            //bool down = downTouch.Pressed;

            if (up == true)
            {
                down = false;
                return 1;
            }

            if (down == true)
            {
                up = false;
                return -1;
            }
            return 0;
        }
        return 0;
    }
}
