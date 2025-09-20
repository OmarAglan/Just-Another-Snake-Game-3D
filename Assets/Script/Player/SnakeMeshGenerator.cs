using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SnakeMeshGenerator : MonoBehaviour
{
    [Header("Mesh Settings")]
    [Range(0.01f, 2f)]
    public float radius = 0.5f;
    
    [Range(3, 32)]
    public int crossSectionResolution = 8;
    
    [Header("Advanced Settings")]
    public bool enableTapering = true;
    
    [Range(0.1f, 1f)]
    public float tailRadiusMultiplier = 0.3f;

    private Mesh mesh;
    private List<Vector3> vertices = new List<Vector3>();
    private List<int> triangles = new List<int>();
    private List<Vector2> uvs = new List<Vector2>();
    private MeshFilter meshFilter;

    void Awake()
    {
        meshFilter = GetComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.name = "Snake Body Mesh";
        meshFilter.mesh = mesh;
    }

    public void BuildMesh(List<PathPoint> pathPoints)
    {
        if (pathPoints == null || pathPoints.Count < 1)
        {
            ClearMesh();
            return;
        }

        if (pathPoints.Count == 1)
        {
            CreateSinglePointMesh(pathPoints[0]);
            return;
        }

        ClearMeshData();

        // CRITICAL FIX: Convert world space positions to local space
        for (int i = 0; i < pathPoints.Count; i++)
        {
            float radiusFactor = 1f;
            if (enableTapering && pathPoints.Count > 1)
            {
                float t = (float)i / (pathPoints.Count - 1);
                radiusFactor = Mathf.Lerp(tailRadiusMultiplier, 1f, t);
            }

            // Convert world position to local space relative to this transform
            Vector3 localPosition = transform.InverseTransformPoint(pathPoints[i].position);
            Quaternion localRotation = Quaternion.Inverse(transform.rotation) * pathPoints[i].rotation;
            
            GenerateRing(localPosition, localRotation, radius * radiusFactor);
        }

        for (int i = 0; i < pathPoints.Count - 1; i++)
        {
            ConnectRings(i, i + 1);
        }

        UpdateUnityMesh();
    }

    private void CreateSinglePointMesh(PathPoint point)
    {
        ClearMeshData();
        Vector3 localPosition = transform.InverseTransformPoint(point.position);
        Quaternion localRotation = Quaternion.Inverse(transform.rotation) * point.rotation;
        GenerateRing(localPosition, localRotation, radius);
        UpdateUnityMesh();
    }

    private void ClearMesh()
    {
        ClearMeshData();
        mesh.Clear();
    }

    private void ClearMeshData()
    {
        vertices.Clear();
        triangles.Clear();
        uvs.Clear();
    }

    private void GenerateRing(Vector3 position, Quaternion rotation, float ringRadius)
{
    // Check if this is near the head (last few rings)
    int totalRings = vertices.Count / crossSectionResolution;
    int maxRings = pathPoints?.Count ?? 0;
    bool isNearHead = maxRings > 0 && totalRings >= maxRings - 3;
    
    // Make the head area slightly larger
    float headBulge = isNearHead ? 1.2f : 1.0f;
    float actualRadius = ringRadius * headBulge;
    
    for (int i = 0; i < crossSectionResolution; i++)
    {
        float angle = 2 * Mathf.PI * i / crossSectionResolution;
        
        Vector3 circlePoint = new Vector3(
            Mathf.Cos(angle) * actualRadius,
            Mathf.Sin(angle) * actualRadius,
            0
        );
        
        Vector3 localVertex = position + rotation * circlePoint;
        vertices.Add(localVertex);
        
        float u = (float)i / crossSectionResolution;
        float v = (float)(vertices.Count / crossSectionResolution) / Mathf.Max(1, vertices.Count / crossSectionResolution);
        uvs.Add(new Vector2(u, v));
    }
}

    private void ConnectRings(int ringIndex1, int ringIndex2)
    {
        int ring1StartIndex = ringIndex1 * crossSectionResolution;
        int ring2StartIndex = ringIndex2 * crossSectionResolution;

        for (int i = 0; i < crossSectionResolution; i++)
        {
            int a = ring1StartIndex + i;
            int b = ring1StartIndex + (i + 1) % crossSectionResolution;
            int c = ring2StartIndex + i;
            int d = ring2StartIndex + (i + 1) % crossSectionResolution;

            triangles.Add(a);
            triangles.Add(c);
            triangles.Add(d);

            triangles.Add(a);
            triangles.Add(d);
            triangles.Add(b);
        }
    }

    private void UpdateUnityMesh()
    {
        mesh.Clear();

        if (vertices.Count > 0)
        {
            mesh.vertices = vertices.ToArray();
            mesh.uv = uvs.ToArray();

            if (triangles.Count > 0)
            {
                mesh.triangles = triangles.ToArray();
            }

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
        }
    }
}