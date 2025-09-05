using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// A custom data structure that represents a single point along the snake's path.
/// 
/// WHY USE A STRUCT?
/// - More memory efficient than a class for simple data
/// - Contains both position AND rotation data for proper mesh generation
/// - Immutable by design (good for path tracking)
/// 
/// USAGE:
/// Each PathPoint represents where a segment of the snake's body should be positioned
/// and how it should be oriented. This is crucial for creating smooth curved snakes.
/// </summary>
public struct PathPoint
{
    /// <summary>World space position of this path segment</summary>
    public Vector3 position;

    /// <summary>Rotation/orientation of this path segment (handles curves and turns)</summary>
    public Quaternion rotation;

    /// <summary>
    /// Constructor for creating new PathPoints with cleaner syntax.
    /// Example: new PathPoint(transform.position, transform.rotation)
    /// </summary>
    /// <param name="pos">World position for this path point</param>
    /// <param name="rot">Rotation for this path point</param>
    public PathPoint(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
    }
}

/// <summary>
/// Controls the snake's movement, input handling, and body tracking system.
/// 
/// CORE RESPONSIBILITIES:
/// 1. Handle player input for steering the snake
/// 2. Move the snake's head continuously forward
/// 3. Track the snake's path by creating PathPoints at regular intervals
/// 4. Communicate with SnakeMeshGenerator to create the visual body
/// 
/// HOW THE SNAKE SYSTEM WORKS:
/// - The snake head (this GameObject) moves continuously forward
/// - As it moves, we record PathPoints at regular distance intervals
/// - These PathPoints are sent to SnakeMeshGenerator to create the tubular body mesh
/// - The body automatically follows the exact path the head took
/// 
/// KEY INSIGHT: The snake's "body" is just a visual representation of where 
/// the head has been. This creates smooth, natural movement patterns.
/// 
/// FIXED ISSUES:
/// - PathPoints now added to END of list (proper chronological order)
/// - Snake head position tracked separately from body segments
/// - Rotation properly isolated per segment instead of affecting entire mesh
/// </summary>
public class SnakeController : MonoBehaviour
{
    // === MOVEMENT CONFIGURATION ===
    // These settings control how the snake moves and responds to player input

    [Header("Movement Settings")]
    [Tooltip("The forward speed of the snake's head in units per second. Higher = faster snake.")]
    public float moveSpeed = 5f;

    [Tooltip("The speed at which the snake turns in degrees per second. Higher = more responsive turning.")]
    public float turnSpeed = 180f;

    // === BODY GENERATION SETTINGS ===
    // These control how the snake's body is created and how detailed it is

    [Header("Body Settings")]
    [Tooltip("How far the head must travel before adding a new body segment. Smaller = smoother body, but more expensive.")]
    public float segmentLength = 0.25f;

    [Tooltip("Maximum length of the snake's body in segments. Prevents infinite growth and performance issues.")]
    public int maxBodySegments = 100;

    // === INTERNAL STATE TRACKING ===
    // These variables track the snake's current state and path history

    /// <summary>
    /// Complete history of where the snake's head has been.
    /// Index 0 = oldest position (tail), higher indices = newer positions (towards head)
    /// Each PathPoint contains both position and rotation data for proper mesh generation.
    /// 
    /// IMPORTANT: This is the REVERSE of the previous implementation!
    /// We now add to the END of the list, maintaining proper chronological order.
    /// </summary>
    private List<PathPoint> pathPoints = new List<PathPoint>();

    /// <summary>
    /// Tracks how far the head has moved since we last added a PathPoint.
    /// When this reaches 'segmentLength', we add a new point and reset this counter.
    /// This ensures consistent spacing between body segments regardless of framerate.
    /// </summary>
    private float distanceSinceLastSegment = 0f;

    /// <summary>
    /// The previous position of the snake head, used for distance calculations.
    /// This allows us to track exactly how far the head has moved each frame.
    /// </summary>
    private Vector3 lastRecordedPosition;

    // === COMPONENT REFERENCES ===
    // References to other components this script needs to communicate with

    /// <summary>
    /// Reference to the SnakeMeshGenerator that creates the visual snake body.
    /// We send our pathPoints to this component to generate the tubular mesh.
    /// </summary>
    private SnakeMeshGenerator snakeMeshGenerator;

    // === INITIALIZATION ===

    /// <summary>
    /// Initialize component references during object creation.
    /// 
    /// WHY AWAKE() VS START()?
    /// - Awake(): Get component references, initialize private variables
    /// - Start(): Perform initial setup that depends on other objects being ready
    /// 
    /// Awake() is called before Start() and is ideal for internal setup.
    /// </summary>
    void Awake()
    {
        // Get the SnakeMeshGenerator component on the same GameObject
        // This component will handle creating the visual snake body mesh
        snakeMeshGenerator = GetComponent<SnakeMeshGenerator>();

        // NOTE: If this fails, make sure SnakeMeshGenerator is attached to the same GameObject!
    }

    // <summary>
    /// Perform initial setup when the game begins.
    /// 
    /// WHAT HAPPENS HERE:
    /// 1. Create the first PathPoint at the snake's starting position
    /// 2. Initialize the position tracking system
    /// 3. Generate the initial mesh so the snake is visible immediately
    /// 
    /// This ensures the snake has a body from the very first frame and
    /// connects properly to the head from the start.
    /// </summary>
    void Start()
    {
        // Create the initial PathPoint using the snake head's starting position and rotation
        // This becomes the foundation of our path tracking system
        pathPoints.Add(new PathPoint(transform.position, transform.rotation));

        // Initialize our position tracking system
        lastRecordedPosition = transform.position;

        // Generate the initial mesh with current head position included
        // This ensures immediate connectivity between body and head
        if (snakeMeshGenerator != null)
        {
            List<PathPoint> initialPathWithHead = new List<PathPoint>(pathPoints);
            initialPathWithHead.Add(new PathPoint(transform.position, transform.rotation));
            snakeMeshGenerator.BuildMesh(initialPathWithHead);
        }
    }

    // === MAIN UPDATE LOOP ===

    /// <summary>
    /// Main game loop - called every frame.
    /// 
    /// EXECUTION ORDER MATTERS:
    /// 1. Handle input first (affects rotation)
    /// 2. Move and track path second (uses the updated rotation)
    /// 
    /// This ensures the snake's movement feels responsive and smooth.
    /// </summary>
    void Update()
    {
        // Process player input and update snake head rotation
        HandleInput();

        // Move the snake head and update the path tracking system
        MoveAndTrackPath();
    }

    // === INPUT HANDLING ===

    /// <summary>
    /// Process player input and rotate the snake head accordingly.
    /// 
    /// INPUT MAPPING:
    /// - A/Left Arrow: Turn left (-1.0)
    /// - D/Right Arrow: Turn right (+1.0)
    /// - Controller: Left stick horizontal axis
    /// 
    /// FRAMERATE INDEPENDENCE:
    /// We multiply by Time.deltaTime to ensure consistent turning speed
    /// regardless of the game's framerate (60 FPS vs 120 FPS, etc.)
    /// </summary>
    private void HandleInput()
    {
        // Get horizontal input (-1 to +1 range)
        // Unity automatically handles keyboard, gamepad, and other input devices
        float horizontalInput = Input.GetAxis("Horizontal");

        // Rotate around the snake's local up-axis (Y-axis in most cases)
        // Vector3.up = (0, 1, 0) in world space
        // 
        // MATH BREAKDOWN:
        // - horizontalInput: Direction and intensity of turn (-1 to +1)
        // - turnSpeed: Maximum degrees per second
        // - Time.deltaTime: Time since last frame (makes it framerate-independent)
        transform.Rotate(Vector3.up, horizontalInput * turnSpeed * Time.deltaTime);
    }

    // === MOVEMENT AND PATH TRACKING ===

    /// <summary>
    /// Moves the snake head forward and manages the path tracking system.
    /// 
    /// ALGORITHM OVERVIEW:
    /// 1. Move snake head forward
    /// 2. Calculate distance moved this frame
    /// 3. Add to cumulative distance tracker
    /// 4. If we've moved far enough, create a new PathPoint
    /// 5. Manage body length by removing old segments
    /// 6. ALWAYS include current head position in mesh generation
    /// 7. Update the visual mesh
    /// 
    /// KEY FIX: The mesh now ALWAYS connects to the current head position,
    /// preventing gaps between the body and the moving head.
    /// 
    /// WHY TRACK DISTANCE?
    /// This ensures body segments are evenly spaced regardless of:
    /// - Framerate variations
    /// - Speed changes
    /// - Direction changes
    /// </summary>
    private void MoveAndTrackPath()
    {
        // === STEP 1: MOVE THE SNAKE HEAD ===
        // Move forward in the direction the snake is facing
        // transform.forward is the local Z-axis direction (blue arrow in Scene view)
        transform.position += transform.forward * moveSpeed * Time.deltaTime;

        // === STEP 2: CALCULATE MOVEMENT DISTANCE ===
        // Measure how far we actually moved since the last recorded position
        float distanceMoved = Vector3.Distance(lastRecordedPosition, transform.position);

        // Add this distance to our cumulative tracker
        distanceSinceLastSegment += distanceMoved;

        // === STEP 3: CHECK IF WE NEED A NEW PATH POINT ===
        // Have we moved far enough to warrant adding a new body segment?
        if (distanceSinceLastSegment >= segmentLength)
        {
            // === STEP 4: CREATE NEW PATH POINT ===
            // CRUCIAL FIX: Add to the END of the list (chronological order)
            // This ensures index 0 = oldest (tail), highest index = newest (head)
            pathPoints.Add(new PathPoint(lastRecordedPosition, transform.rotation));

            // Update our position tracking for the next distance calculation
            lastRecordedPosition = transform.position;

            // Reset distance tracker, but keep any "overflow" distance
            // This prevents accumulation errors and maintains consistent spacing
            distanceSinceLastSegment -= segmentLength;

            // === STEP 5: MANAGE BODY LENGTH ===
            // Remove old segments to prevent infinite growth and maintain performance
            while (pathPoints.Count > maxBodySegments)
            {
                // Remove the oldest segment (index 0 = tail end)
                pathPoints.RemoveAt(0);
            }
        }

        // === STEP 6: CREATE CURRENT HEAD PATH POINTS ===
        // CRITICAL FIX: Always include the current head position in the mesh
        // This ensures the snake body connects seamlessly to the moving head
        List<PathPoint> currentPathWithHead = new List<PathPoint>(pathPoints);
        
        // Add the current head position as the newest path point
        // This creates a seamless connection from body to head
        currentPathWithHead.Add(new PathPoint(transform.position, transform.rotation));

        // === STEP 7: UPDATE VISUAL REPRESENTATION ===
        // Tell the mesh generator to rebuild the snake body with current head included
        if (snakeMeshGenerator != null)
        {
            snakeMeshGenerator.BuildMesh(currentPathWithHead);
        }
    }

    // === DEBUG VISUALIZATION ===

    /// <summary>
    /// Unity Editor debug function - draws visual helpers in the Scene view.
    /// 
    /// WHAT IT DRAWS:
    /// - Green spheres: Each PathPoint position (tail to head gradient)
    /// - Blue rays: Direction each PathPoint is facing (rotation visualization)
    /// - White lines: Connections between consecutive PathPoints
    /// - Red sphere: Current head position
    /// - Yellow line: Connection from last path point to current head
    /// 
    /// HOW TO VIEW:
    /// 1. Select this GameObject in the Hierarchy
    /// 2. Look in the Scene view (not Game view)
    /// 3. The Gizmos will be visible when the object is selected
    /// 
    /// DEBUGGING TIPS:
    /// - If spheres are too close together: Increase segmentLength
    /// - If blue rays point wrong direction: Check rotation calculations
    /// - If lines are jagged: Snake might be moving too fast or turning too sharply
    /// - Green spheres should get brighter from tail to head
    /// - Yellow line should always connect the body to the red head sphere
    /// </summary>
    void OnDrawGizmos()
    {
        // Safety check: Only draw if we have path data
        if (pathPoints != null && pathPoints.Count > 0)
        {
            // Draw visualization for each PathPoint
            for (int i = 0; i < pathPoints.Count; i++)
            {
                PathPoint currentPoint = pathPoints[i];

                // === DRAW POSITION MARKER WITH GRADIENT ===
                // Color gradient from dark green (tail) to bright green (head)
                float t = pathPoints.Count > 1 ? (float)i / (pathPoints.Count - 1) : 1f;
                Gizmos.color = Color.Lerp(new Color(0, 0.3f, 0), Color.green, t);
                Gizmos.DrawSphere(currentPoint.position, 0.1f);

                // === DRAW DIRECTION INDICATOR ===
                // Show which way this path segment is facing
                Gizmos.color = Color.blue;
                Vector3 forwardDirection = currentPoint.rotation * Vector3.forward;
                Gizmos.DrawRay(currentPoint.position, forwardDirection * 0.5f);

                // === DRAW CONNECTION TO NEXT POINT ===
                // Show the path continuity between segments
                if (i < pathPoints.Count - 1)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(currentPoint.position, pathPoints[i + 1].position);
                }
            }

            // === DRAW CONNECTION TO CURRENT HEAD ===
            // Show the critical connection from last path point to current head position
            if (pathPoints.Count > 0)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(pathPoints[pathPoints.Count - 1].position, transform.position);
            }
        }

        // === DRAW CURRENT HEAD POSITION ===
        // Show where the actual snake head is right now
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.15f);

        // === DRAW HEAD DIRECTION ===
        // Show which way the head is facing
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 1f);
    }
}