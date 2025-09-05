using UnityEngine;
using System.Collections.Generic;

// This script now requires the components needed for rendering a mesh.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SnakeMeshGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    [Tooltip("The radius of the snake's body.")]
    [Range(0.01f, 2f)] // A slider makes it easy to adjust in the Inspector.
    public float radius = 0.5f;

    [Tooltip("The number of vertices that make up the circular cross-section of the body.")]
    [Range(3, 32)] // A snake body needs at least 3 sides (a triangle).
    public int crossSectionResolution = 8;

    // --- Private Mesh Data ---
    private Mesh mesh;
    // These lists will store the calculated data for our mesh.
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>(); // For textures later.

    // --- Component References ---
    // A reference to the MeshFilter component on this GameObject.
    private MeshFilter meshFilter;


    /// <summary>
    /// Awake is called when the script instance is being loaded.
    /// It's the ideal place to get references to other components.
    /// </summary>
    void Awake()
    {
        // Get the MeshFilter component.
        meshFilter = GetComponent<MeshFilter>();

        // Create a new Mesh object and assign it to the MeshFilter.
        // This mesh is currently empty, but we now have a place to put our generated data.
        mesh = new Mesh();
        mesh.name = "Snake Body Mesh";
        meshFilter.mesh = mesh;
    }

    /// <summary>
    /// This is the main public function that will be called by the SnakeController.
    /// It takes the snake's path data and generates the 3D mesh from it.
    /// </summary>
    /// <param name="pathPoints">The list of points representing the snake's spine.</param>
    public void BuildMesh(List<Vector3> pathPoints)
    {
        // If we have fewer than 2 points, we can't form a body segment.
        if (pathPoints.Count < 2)
        {
            mesh.Clear(); // Clear the mesh if the snake is too short.
            return;
        }

        // --- Step 1: Clear old data ---
        vertices.Clear();
        triangles.Clear();
        // uvs.Clear(); // We'll handle UVs for textures in a later phase.

        // --- Step 2 & 3: Generate Vertices for each path point ---
        // Loop through the path, creating a ring of vertices at each point.
        for (int i = 0; i < pathPoints.Count; i++)
        {
            Vector3 currentPoint = pathPoints[i];

            // Determine the direction of this segment.
            // If it's the first point, it looks towards the next one.
            // Otherwise, it looks backwards from the previous one for a smoother transition.
            Vector3 direction = (i < pathPoints.Count - 1) ? (pathPoints[i + 1] - currentPoint).normalized : (currentPoint - pathPoints[i - 1]).normalized;

            // Generate one ring of vertices.
            GenerateRing(currentPoint, direction);
        }

        // --- Step 4: Generate Triangles to connect the rings ---
        // We loop through the segments of the snake body. A segment is the space between two path points.
        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            // Each ring has 'crossSectionResolution' number of vertices.
            // The index of the first vertex of the current ring is (i * crossSectionResolution).
            int currentRingStartIndex = i * crossSectionResolution;
            // The index of the first vertex of the next ring.
            int nextRingStartIndex = (i + 1) * crossSectionResolution;

            // Loop through the vertices of the current ring.
            for (int j = 0; j < crossSectionResolution; j++)
            {
                // Find the indices of the four vertices that form a quad.
                int a = currentRingStartIndex + j;
                int b = currentRingStartIndex + (j + 1) % crossSectionResolution; // Use modulo to wrap around the last vertex.
                int c = nextRingStartIndex + j;
                int d = nextRingStartIndex + (j + 1) % crossSectionResolution;

                // Create the two triangles that form the quad.
                // Triangle 1: (a, d, c)
                triangles.Add(a);
                triangles.Add(d);
                triangles.Add(c);

                // Triangle 2: (a, b, d)
                triangles.Add(a);
                triangles.Add(b);
                triangles.Add(d);
            }
        }

        // --- Step 5: Apply the data to the mesh ---
        mesh.Clear(); // Clear any existing mesh data.
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        // RecalculateNormals is crucial for lighting. It calculates how light should
        // bounce off the surface, otherwise the mesh will look flat or black.
        mesh.RecalculateNormals();
    }

    /// <summary>
    /// Generates a ring of vertices at a specific point along the path.
    /// </summary>
    /// <param name="position">The center of the ring.</param>
    /// <param name="direction">The forward direction of the snake at this point.</param>
    private void GenerateRing(Vector3 position, Vector3 direction)
    {
        // To create a circle of vertices, we need an "up" and a "right" direction
        // that are perpendicular to the "forward" direction.

        // This calculates the orientation of the ring.
        Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up);

        // Loop to create each vertex in the ring.
        for (int i = 0; i < crossSectionResolution; i++)
        {
            // Calculate the angle for this vertex.
            float angle = 2 * Mathf.PI * i / crossSectionResolution;

            // Find the point on a 2D circle using Sin and Cos.
            // Note: We use (y, z) or (x, y) depending on the orientation we want. (Cos(angle), Sin(angle)) is standard.
            Vector3 circlePoint = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

            // Rotate the circle point to align with the snake's direction and add it to the vertices list.
            vertices.Add(position + rotation * circlePoint);
        }
    }
}