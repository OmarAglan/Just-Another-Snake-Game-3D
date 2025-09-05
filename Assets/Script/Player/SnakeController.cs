using UnityEngine;

// This directive ensures that the GameObject this script is on
// also has a MeshFilter and MeshRenderer component. We'll need these later.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SnakeController : MonoBehaviour
{
    // --- PUBLIC SETTINGS ---
    // These variables will appear in the Unity Inspector, allowing us to tweak them.
    
    [Header("Movement Settings")] // Adds a nice title in the Inspector for organization.
    [Tooltip("The forward speed of the snake's head in units per second.")]
    public float moveSpeed = 5f;

    [Tooltip("The speed at which the snake turns in degrees per second.")]
    public float turnSpeed = 180f;


    // --- PRIVATE VARIABLES ---
    // These are for internal use by the script.

    // We will add more variables here in the next steps.


    // The Start() method is called once when the script instance is being loaded.
    void Start()
    {
        // We will add initialization code here later.
    }

    // The Update() method is called once per frame. It's the ideal place for movement logic.
    void Update()
    {
        // Handle player input for turning first.
        HandleInput();

        // Move the snake's head forward.
        MoveSnake();
    }

    /// <summary>
    /// Checks for player input (left/right keys) and rotates the snake head accordingly.
    /// </summary>
    private void HandleInput()
    {
        // GetAxis("Horizontal") gets input from A/D keys, left/right arrow keys, or a controller stick.
        // It returns a value between -1 (for left) and +1 (for right).
        float horizontalInput = Input.GetAxis("Horizontal");

        // We will rotate the snake around its own up-axis (Vector3.up).
        // The rotation amount is the input value multiplied by our turn speed.
        // We multiply by Time.deltaTime to make the rotation smooth and independent of the frame rate.
        // If we didn't use Time.deltaTime, the snake would turn much faster on computers with a higher FPS.
        transform.Rotate(Vector3.up, horizontalInput * turnSpeed * Time.deltaTime);
    }

    /// <summary>
    /// Moves the snake's head forward in the direction it is currently facing.
    /// </summary>
    private void MoveSnake()
    {
        // transform.forward is a built-in property that always points in the object's "forward" direction.
        // We move the snake by this direction, scaled by our moveSpeed.
        // Again, we multiply by Time.deltaTime to ensure consistent speed across different frame rates.
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
    }
}