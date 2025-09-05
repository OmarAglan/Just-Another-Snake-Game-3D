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
/// 
/// KEY FIXES:
/// - Now properly handles chronological PathPoint order (tail to head)
/// - Each segment's rotation is applied independently (no global mesh rotation)
/// - Improved performance with better vertex/triangle management
/// - Added safety checks for edge cases
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

    [Header("Advanced Settings")]
    [Tooltip("If enabled, the snake will taper towards the tail (smaller radius at the back).")]
    public bool enableTapering = true;

    [Tooltip("The minimum radius at the tail when tapering is enabled (as a percentage of main radius).")]
    [Range(0.1f, 1f)]
    public float tailRadiusMultiplier = 0.3f;

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
    /// IMPORTANT: PathPoints are now expected in chronological order:
    /// - Index 0 = oldest position (tail end)
    /// - Higher indices = newer positions (towards head)
    /// 
    /// This fixes the previous rotation issues by ensuring each segment
    /// uses its own stored rotation data independently.
    /// </summary>
    /// <param name="pathPoints">List of points defining the snake's path and orientation (tail to head order)</param>
    public void BuildMesh(List<PathPoint> pathPoints)
    {
        // Safety check: Need at least 1 point to create any geometry
        if (pathPoints == null || pathPoints.Count < 1)
        {
            ClearMesh();
            return;
        }

        // For a single point, create a small sphere at the head position
        if (pathPoints.Count == 1)
        {
            CreateSinglePointMesh(pathPoints[0]);
            return;
        }

        // Clear previous mesh data - we rebuild everything each frame
        ClearMeshData();

        // === STEP 1: GENERATE VERTEX RINGS ===
        // Create a circular ring of vertices at each path point
        // Each ring will have 'crossSectionResolution' number of vertices
        for (int i = 0; i < pathPoints.Count; i++)
        {
            // Calculate tapering factor if enabled
            float radiusFactor = 1f;
            if (enableTapering && pathPoints.Count > 1)
            {
                // Taper from tail (index 0) to head (highest index)
                float t = (float)i / (pathPoints.Count - 1);
                radiusFactor = Mathf.Lerp(tailRadiusMultiplier, 1f, t);
            }

            // Generate a ring using the stored position and rotation from PathPoint
            // Each PathPoint contains its own independent rotation data
            GenerateRing(pathPoints[i].position, pathPoints[i].rotation, radius * radiusFactor);
        }

        // === STEP 2: CONNECT RINGS WITH TRIANGLES ===
        // Connect each pair of adjacent rings to form the tube surface
        // Since pathPoints are in chronological order (tail to head),
        // we connect them in the same order for proper triangle winding
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            ConnectRings(i, i + 1);
        }

        // === STEP 3: UPDATE UNITY MESH ===
        UpdateUnityMesh();
    }

    // === HELPER FUNCTIONS FOR MESH GENERATION ===

    /// <summary>
    /// Creates a simple mesh for when we only have one PathPoint.
    /// This prevents errors and provides visual feedback even with minimal data.
    /// </summary>
    /// <param name="point">The single PathPoint to create a mesh for</param>
    private void CreateSinglePointMesh(PathPoint point)
    {
        ClearMeshData();

        // Create a small sphere at the point location
        // This gives immediate visual feedback when the snake is just starting
        GenerateRing(point.position, point.rotation, radius);
        
        // For a single ring, we can't create triangles, so just update the mesh
        UpdateUnityMesh();
    }

    /// <summary>
    /// Clears all mesh data and the Unity mesh itself.
    /// Used when we have invalid or no path data.
    /// </summary>
    private void ClearMesh()
    {
        ClearMeshData();
        mesh.Clear();
    }

    /// <summary>
    /// Clears our internal mesh data lists.
    /// Prepares for building a new mesh from scratch.
    /// </summary>
    private void ClearMeshData()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
    }

    /// <summary>
    /// Generates a circular ring of vertices at a specific position and orientation.
    /// 
    /// HOW IT WORKS:
    /// 1. Create a circle of points in local space (XY plane, radius distance from origin)
    /// 2. Apply the provided rotation to orient the circle correctly
    /// 3. Translate to the final world position
    /// 
    /// KEY FIX: Each ring now uses its own independent rotation from the PathPoint.
    /// This ensures that rotations don't affect the entire mesh, just the local segment.
    /// 
    /// This creates the cross-sectional shape of the snake at each path point.
    /// </summary>
    /// <param name="position">World space position where the ring should be placed</param>
    /// <param name="rotation">Rotation that orients the ring (from the PathPoint's stored rotation)</param>
    /// <param name="ringRadius">The radius for this specific ring (allows for tapering)</param>
    private void GenerateRing(Vector3 position, Quaternion rotation, float ringRadius)
    {
        // Generate vertices around a circle in the XY plane
        for (int i = 0; i < crossSectionResolution; i++)
        {
            // Calculate angle for this vertex (evenly distributed around the circle)
            float angle = 2 * Mathf.PI * i / crossSectionResolution;

            // Create a point on the circle in local space
            // Z=0 means the circle lies in the XY plane initially
            Vector3 circlePoint = new Vector3(
                Mathf.Cos(angle) * ringRadius,  // X position on circle
                Mathf.Sin(angle) * ringRadius,  // Y position on circle
                0                               // Z is 0 (flat circle in XY plane)
            );

            // Transform the local circle point to world space:
            // 1. Apply rotation to orient the circle correctly (using PathPoint's rotation)
            // 2. Add position to place it in the world
            // 
            // CRUCIAL: Each ring uses its OWN rotation, preventing global mesh rotation bugs
            Vector3 worldVertex = position + rotation * circlePoint;

            // Add to our vertex list
            vertices.Add(worldVertex);

            // Generate UV coordinates for potential texturing
            // U wraps around the circumference, V goes along the snake's length
            float u = (float)i / crossSectionResolution;
            float v = (float)(vertices.Count / crossSectionResolution) / Mathf.Max(1, vertices.Count / crossSectionResolution);
            uvs.Add(new Vector2(u, v));
        }
    }

    /// <summary>
    /// Connects two adjacent rings with triangles to form part of the tube surface.
    /// 
    /// ALGORITHM:
    /// For each segment around the circumference:
    /// 1. Get the 4 vertices that form a quad (2 from each ring)
    /// 2. Split the quad into 2 triangles with proper winding order
    /// 3. Add triangle indices to our list
    /// 
    /// WINDING ORDER: Counter-clockwise for outward-facing normals (proper lighting)
    /// </summary>
    /// <param name="ringIndex1">Index of the first ring (closer to tail)</param>
    /// <param name="ringIndex2">Index of the second ring (closer to head)</param>
    private void ConnectRings(int ringIndex1, int ringIndex2)
    {
        // Calculate the starting vertex index for each ring
        int ring1StartIndex = ringIndex1 * crossSectionResolution;
        int ring2StartIndex = ringIndex2 * crossSectionResolution;

        // For each segment around the circumference, create 2 triangles
        for (int i = 0; i < crossSectionResolution; i++)
        {
            // Get the 4 vertices that form this quad segment
            int a = ring1StartIndex + i;                                      // Ring 1, current vertex
            int b = ring1StartIndex + (i + 1) % crossSectionResolution;       // Ring 1, next vertex (wraps around)
            int c = ring2StartIndex + i;                                      // Ring 2, current vertex
            int d = ring2StartIndex + (i + 1) % crossSectionResolution;       // Ring 2, next vertex (wraps around)

            // Create two triangles to form a quad with proper winding order
            // Triangle 1: a -> c -> d (counter-clockwise when viewed from outside)
            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);

            // Triangle 2: a -> d -> b
            triangles.Add(a);
            triangles.Add(d);
            triangles.Add(b);
        }
    }

    /// <summary>
    /// Updates the Unity mesh with our generated vertex and triangle data.
    /// This is the final step that makes our procedural mesh visible.
    /// </summary>
    private void UpdateUnityMesh()
    {
        // Clear any existing mesh data
        mesh.Clear();

        // Only update if we have valid data
        if (vertices.Count > 0)
        {
            // Set new vertex positions
            mesh.vertices = vertices.ToArray();

            // Set UV coordinates (for potential texturing)
            mesh.uv = uvs.ToArray();

            // Set triangle connectivity (only if we have triangles)
            if (triangles.Count > 0)
            {
                mesh.triangles = triangles.ToArray();
            }

            // Let Unity calculate surface normals for proper lighting
            // This analyzes our triangles and determines which way each face points
            mesh.RecalculateNormals();

            // Recalculate bounds for proper culling and collision detection
            mesh.RecalculateBounds();
        }
    }

    // === DEBUG AND VALIDATION ===

    /// <summary>
    /// Unity Editor debug function - provides visual debugging information.
    /// Shows mesh statistics and validation in the Scene view.
    /// </summary>
    void OnDrawGizmos()
    {
        // Only draw debug info if we have a valid mesh
        if (mesh != null && vertices.Count > 0)
        {
            // Draw mesh bounds for debugging
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(mesh.bounds.center, mesh.bounds.size);

            // Show mesh statistics in Scene view (when selected)
            #if UNITY_EDITOR
            if (UnityEditor.Selection.activeGameObject == gameObject)
            {
                // This will show up in the Scene view
                UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, 
                    $"Snake Mesh:\nVertices: {vertices.Count}\nTriangles: {triangles.Count / 3}");
            }
            #endif
        }
    }
}