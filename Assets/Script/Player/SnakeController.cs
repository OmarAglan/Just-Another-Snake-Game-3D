using UnityEngine;
using System.Collections.Generic;

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

    [Header("Body Settings")]
    [Tooltip("How far the head must travel to add a new body segment.")]
    public float segmentLength = 0.25f;

    // --- PRIVATE VARIABLES ---

    // This list will store the positions of our snake's body segments, from head to tail.
    private List<Vector3> pathPoints = new List<Vector3>();
    // This tracks the distance the head has moved since the last point was added.
    private float distanceSinceLastSegment = 0f;


    // The Start() method is called once when the script instance is being loaded.
    void Start()
    {
        // When the game starts, we need to create an initial body.
        // We will add the head's starting position to the path.
        pathPoints.Add(transform.position);
    }

    // The Update() method is called once per frame. It's the ideal place for movement logic.
    void Update()
    {
        // Handle player input for turning first.
        HandleInput();

        // Move the snake's head forward and Track its path.
        MoveAndTrackPath();
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
    /// Moves the snake's head and updates the path tracking data.
    /// Renamed from MoveSnake() to be more descriptive.
    /// </summary>
    private void MoveAndTrackPath()
    {
        // Store the head's position *before* we move it.
        Vector3 oldHeadPosition = transform.position;

        // Move the snake's head forward.
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // Calculate the distance moved in this frame.
        float distanceMoved = Vector3.Distance(oldHeadPosition, transform.position);
        // Add this distance to our tracker.
        distanceSinceLastSegment += distanceMoved;

        // Check if the tracker has exceeded our desired segment length.
        if (distanceSinceLastSegment >= segmentLength)
        {
            // If it has, add a new point to the beginning of our path list.
            // We use Insert(0, ...) to add to the front, because this is where the head is.
            pathPoints.Insert(0, transform.position);

            // Reset the distance tracker. We subtract segmentLength instead of setting to 0
            // to carry over any extra distance. This makes segment spacing more accurate.
            distanceSinceLastSegment -= segmentLength;
        }
    }

    /// <summary>
    /// This is a special Unity function that is called in the editor.
    /// It allows us to draw visual helpers (Gizmos) in the Scene view to debug our code.
    /// </summary>
    void OnDrawGizmos()
    {
        // If our path has points in it, let's draw them.
        if (pathPoints != null && pathPoints.Count > 0)
        {
            // Loop through all the points in our path.
            for (int i = 0; i < pathPoints.Count; i++)
            {
                // Set the color of the Gizmo.
                Gizmos.color = Color.yellow;
                // Draw a small sphere at each path point.
                Gizmos.DrawSphere(pathPoints[i], 0.1f);

                // If it's not the last point in the list, draw a line to the next one.
                if (i < pathPoints.Count - 1)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(pathPoints[i], pathPoints[i + 1]);
                }
            }
        }
    }
}