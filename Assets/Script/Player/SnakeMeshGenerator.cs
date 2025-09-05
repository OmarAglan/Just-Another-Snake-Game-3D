using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Generates a procedural 3D mesh for a snake's body using a series of path points.
/// Creates a tubular mesh by generating circular cross-sections along the snake's path
/// and connecting them with triangles to form a smooth, continuous body.
/// 
/// This component requires a MeshFilter and MeshRenderer to display the generated mesh.
/// The snake's body is built from PathPoint data that includes both position and rotation
/// information, allowing for proper orientation along curved paths.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SnakeMeshGenerator : MonoBehaviour
{
    // === CONFIGURATION SETTINGS ===
    // These parameters control the visual appearance and quality of the snake mesh

    [Header("Mesh Settings")]
    [Tooltip("The radius of the snake's body. Larger values create a thicker snake.")]
    [Range(0.01f, 2f)]
    public float radius = 0.5f;

    [Tooltip("The number of vertices that make up the circular cross-section of the body. Higher values = smoother curves but more expensive.")]
    [Range(3, 32)]
    public int crossSectionResolution = 8;

    // === MESH DATA STORAGE ===
    // These hold the actual mesh data that gets sent to Unity's rendering system

    /// <summary>The Unity Mesh object that will be rendered</summary>
    private Mesh mesh;

    /// <summary>List of all vertex positions in world space</summary>
    private List<Vector3> vertices = new List<Vector3>();

    /// <summary>List of triangle indices that define which vertices form triangles</summary>
    private List<int> triangles = new List<int>();

    /// <summary>UV coordinates for texture mapping (currently unused but prepared for future texturing)</summary>
    private List<Vector2> uvs = new List<Vector2>();

    /// <summary>Reference to the MeshFilter component that displays our generated mesh</summary>
    private MeshFilter meshFilter;

    /// <summary>
    /// Initialize the mesh system when the GameObject is created.
    /// This runs before Start() and ensures our mesh is ready to use.
    /// </summary>
    void Awake()
    {
        // Get the required MeshFilter component (guaranteed to exist due to RequireComponent)
        meshFilter = GetComponent<MeshFilter>();

        // Create a new empty mesh and give it a descriptive name for debugging
        mesh = new Mesh();
        mesh.name = "Snake Body Mesh";

        // Assign our mesh to the MeshFilter so Unity can render it
        meshFilter.mesh = mesh;
    }

    // === MAIN MESH GENERATION FUNCTION ===

    /// <summary>
    /// Builds the complete snake mesh from a series of path points.
    /// 
    /// ALGORITHM OVERVIEW:
    /// 1. For each PathPoint, generate a circular ring of vertices
    /// 2. Connect adjacent rings with triangles to form a tube
    /// 3. Update the Unity mesh with the new geometry
    /// 
    /// PathPoint struct should contain:
    /// - Vector3 position: Where this segment of the snake is located
    /// - Quaternion rotation: How this segment is oriented (handles curves and turns)
    /// </summary>
    /// <param name="pathPoints">List of points defining the snake's path and orientation</param>
    public void BuildMesh(List<PathPoint> pathPoints)
    {
        // Safety check: Need at least 2 points to create any geometry
        if (pathPoints.Count < 2)
        {
            mesh.Clear(); // Clear any existing mesh data
            return;
        }

        // Clear previous mesh data - we rebuild everything each frame
        vertices.Clear();
        triangles.Clear();

        // === STEP 1: GENERATE VERTEX RINGS ===
        // Create a circular ring of vertices at each path point
        // Each ring will have 'crossSectionResolution' number of vertices
        for (int i = 0; i < pathPoints.Count; i++)
        {
            // Generate a ring using the stored position and rotation from PathPoint
            // No need to calculate direction - PathPoint already contains the correct orientation
            GenerateRing(pathPoints[i].position, pathPoints[i].rotation);
        }

        // === STEP 2: CONNECT RINGS WITH TRIANGLES ===
        // Connect each pair of adjacent rings to form the tube surface
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            // Calculate the starting index for vertices in each ring
            int currentRingStartIndex = i * crossSectionResolution;
            int nextRingStartIndex = (i + 1) * crossSectionResolution;

            // For each segment of the ring, create 2 triangles (forming a quad)
            for (int j = 0; j < crossSectionResolution; j++)
            {
                // Get the 4 vertices that form this quad segment
                int a = currentRingStartIndex + j;                              // Current ring, current vertex
                int b = currentRingStartIndex + (j + 1) % crossSectionResolution; // Current ring, next vertex (wraps around)
                int c = nextRingStartIndex + j;                                 // Next ring, current vertex
                int d = nextRingStartIndex + (j + 1) % crossSectionResolution;    // Next ring, next vertex (wraps around)

                // Create two triangles to form a quad
                // Triangle 1: a -> d -> c (counter-clockwise for correct normals)
                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(c);

                // Triangle 2: a -> b -> d
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(d);
            }
        }

        // === STEP 3: UPDATE UNITY MESH ===
        mesh.Clear();                           // Remove old data
        mesh.vertices = vertices.ToArray();     // Set new vertex positions
        mesh.triangles = triangles.ToArray();   // Set new triangle connectivity
        mesh.RecalculateNormals();              // Let Unity calculate surface normals for lighting
    }

    // === HELPER FUNCTION FOR VERTEX GENERATION ===

    /// <summary>
    /// Generates a circular ring of vertices at a specific position and orientation.
    /// 
    /// HOW IT WORKS:
    /// 1. Create a circle of points in local space (XY plane, radius distance from origin)
    /// 2. Apply the provided rotation to orient the circle correctly
    /// 3. Translate to the final world position
    /// 
    /// This creates the cross-sectional shape of the snake at each path point.
    /// </summary>
    /// <param name="position">World space position where the ring should be placed</param>
    /// <param name="rotation">Rotation that orients the ring (typically points along the snake's direction)</param>
    private void GenerateRing(Vector3 position, Quaternion rotation)
    {
        // Generate vertices around a circle in the XY plane
        for (int i = 0; i < crossSectionResolution; i++)
        {
            // Calculate angle for this vertex (evenly distributed around the circle)
            float angle = 2 * Mathf.PI * i / crossSectionResolution;

            // Create a point on the circle in local space
            // Z=0 means the circle lies in the XY plane initially
            Vector3 circlePoint = new Vector3(
                Mathf.Cos(angle) * radius,  // X position on circle
                Mathf.Sin(angle) * radius,  // Y position on circle
                0                           // Z is 0 (flat circle in XY plane)
            );

            // Transform the local circle point to world space:
            // 1. Apply rotation to orient the circle correctly
            // 2. Add position to place it in the world
            Vector3 worldVertex = position + rotation * circlePoint;

            // Add to our vertex list
            vertices.Add(worldVertex);
        }
    }
}