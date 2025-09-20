using UnityEngine;
using System.Collections.Generic;

public struct PathPoint
{
    public Vector3 position;
    public Quaternion rotation;
    
    public PathPoint(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
    }
}

public class SnakeController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float turnSpeed = 180f;
    
    [Header("Body Settings")]
    public float segmentLength = 0.25f;
    public int maxBodySegments = 100;
    
    [Header("Head Settings")]
    public GameObject headModel;  // Reference to the head mesh
    public float headOffset = 0.5f; // How far forward the head is from the body
    
    private List<PathPoint> pathPoints = new List<PathPoint>();
    private float distanceSinceLastSegment = 0f;
    private Vector3 lastRecordedPosition;
    private SnakeMeshGenerator snakeMeshGenerator;
    
    void Awake()
{
    snakeMeshGenerator = GetComponent<SnakeMeshGenerator>();
    // Remove head creation - we'll add eyes directly to the transform
    CreateEyesOnly();
}
    
    void Start()
    {
        pathPoints.Add(new PathPoint(transform.position, transform.rotation));
        lastRecordedPosition = transform.position;
        
        if (snakeMeshGenerator != null)
        {
            List<PathPoint> initialPathWithHead = new List<PathPoint>(pathPoints);
            // Don't add head position to mesh - head is separate now
            snakeMeshGenerator.BuildMesh(initialPathWithHead);
        }
    }
    
    void Update()
    {
        HandleInput();
        MoveAndTrackPath();
        UpdateHeadPosition();
    }
    
    private void HandleInput()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        transform.Rotate(Vector3.up, horizontalInput * turnSpeed * Time.deltaTime);
    }
    
private void MoveAndTrackPath()
{
    transform.position += transform.forward * moveSpeed * Time.deltaTime;
    
    float distanceMoved = Vector3.Distance(lastRecordedPosition, transform.position);
    distanceSinceLastSegment += distanceMoved;
    
    if (distanceSinceLastSegment >= segmentLength)
    {
        // Add path point at current position (no offset)
        pathPoints.Add(new PathPoint(transform.position, transform.rotation));
        
        lastRecordedPosition = transform.position;
        distanceSinceLastSegment -= segmentLength;
        
        while (pathPoints.Count > maxBodySegments)
        {
            pathPoints.RemoveAt(0);
        }
    }
    
    // IMPORTANT: Always include current head position for seamless connection
    if (snakeMeshGenerator != null)
    {
        List<PathPoint> meshPoints = new List<PathPoint>(pathPoints);
        // Add current position as the head of the mesh
        meshPoints.Add(new PathPoint(transform.position, transform.rotation));
        snakeMeshGenerator.BuildMesh(meshPoints);
    }
}
private void CreateEyesOnly()
{
    // Just create eyes that float on the mesh head
    // Left eye
    GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    leftEye.name = "Left Eye";
    leftEye.transform.SetParent(transform);
    leftEye.transform.localPosition = new Vector3(-0.2f, 0.2f, 0.3f);
    leftEye.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
    Destroy(leftEye.GetComponent<Collider>());
    
    // Right eye  
    GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    rightEye.name = "Right Eye";
    rightEye.transform.SetParent(transform);
    rightEye.transform.localPosition = new Vector3(0.2f, 0.2f, 0.3f);
    rightEye.transform.localScale = new Vector3(0.15f, 0.15f, 0.15f);
    Destroy(rightEye.GetComponent<Collider>());
    
    // Eye materials
    Material eyeMat = new Material(Shader.Find("Standard"));
    eyeMat.color = Color.white;
    leftEye.GetComponent<Renderer>().material = eyeMat;
    rightEye.GetComponent<Renderer>().material = eyeMat;
}
    
    private void UpdateHeadPosition()
    {
        if (headModel != null)
        {
            // Keep head aligned with controller
            headModel.transform.localPosition = Vector3.zero;
            headModel.transform.localRotation = Quaternion.identity;
        }
    }
    
    private void CreateDefaultHead()
{
    // Create a head GameObject
    headModel = new GameObject("Snake Head");
    headModel.transform.SetParent(transform);
    headModel.transform.localPosition = Vector3.zero;
    headModel.transform.localRotation = Quaternion.identity;
    
    // Create the main head shape - ADJUSTED SCALE
    GameObject headMesh = GameObject.CreatePrimitive(PrimitiveType.Sphere);
    headMesh.transform.SetParent(headModel.transform);
    headMesh.transform.localPosition = new Vector3(0, 0, 0.2f); // Moved slightly forward
    headMesh.transform.localScale = new Vector3(0.9f, 0.9f, 1.1f); // Better proportions
    
    // Rest of the method stays the same...
    Destroy(headMesh.GetComponent<Collider>());
    CreateEyes();
    
    Renderer headRenderer = headMesh.GetComponent<Renderer>();
    Material headMat = new Material(Shader.Find("Standard"));
    headMat.color = new Color(0.2f, 0.7f, 0.3f);
    headRenderer.material = headMat;
}
    
    private void CreateEyes()
    {
        // Left eye
        GameObject leftEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftEye.name = "Left Eye";
        leftEye.transform.SetParent(headModel.transform);
        leftEye.transform.localPosition = new Vector3(-0.25f, 0.2f, 0.4f);
        leftEye.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        Destroy(leftEye.GetComponent<Collider>());
        
        // Left eye pupil
        GameObject leftPupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        leftPupil.name = "Left Pupil";
        leftPupil.transform.SetParent(leftEye.transform);
        leftPupil.transform.localPosition = new Vector3(0, 0, 0.5f);
        leftPupil.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Destroy(leftPupil.GetComponent<Collider>());
        
        // Right eye
        GameObject rightEye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightEye.name = "Right Eye";
        rightEye.transform.SetParent(headModel.transform);
        rightEye.transform.localPosition = new Vector3(0.25f, 0.2f, 0.4f);
        rightEye.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
        Destroy(rightEye.GetComponent<Collider>());
        
        // Right eye pupil
        GameObject rightPupil = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        rightPupil.name = "Right Pupil";
        rightPupil.transform.SetParent(rightEye.transform);
        rightPupil.transform.localPosition = new Vector3(0, 0, 0.5f);
        rightPupil.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        Destroy(rightPupil.GetComponent<Collider>());
        
        // Eye materials
        Material eyeWhiteMat = new Material(Shader.Find("Standard"));
        eyeWhiteMat.color = Color.white;
        leftEye.GetComponent<Renderer>().material = eyeWhiteMat;
        rightEye.GetComponent<Renderer>().material = eyeWhiteMat;
        
        Material pupilMat = new Material(Shader.Find("Standard"));
        pupilMat.color = Color.black;
        leftPupil.GetComponent<Renderer>().material = pupilMat;
        rightPupil.GetComponent<Renderer>().material = pupilMat;
    }
    
    void OnDrawGizmos()
    {
        if (pathPoints != null && pathPoints.Count > 0)
        {
            for (int i = 0; i < pathPoints.Count; i++)
            {
                PathPoint currentPoint = pathPoints[i];
                
                float t = pathPoints.Count > 1 ? (float)i / (pathPoints.Count - 1) : 1f;
                Gizmos.color = Color.Lerp(new Color(0, 0.3f, 0), Color.green, t);
                Gizmos.DrawSphere(currentPoint.position, 0.1f);
                
                Gizmos.color = Color.blue;
                Vector3 forwardDirection = currentPoint.rotation * Vector3.forward;
                Gizmos.DrawRay(currentPoint.position, forwardDirection * 0.5f);
                
                if (i < pathPoints.Count - 1)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(currentPoint.position, pathPoints[i + 1].position);
                }
            }
        }
        
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(transform.position, 0.15f);
        Gizmos.color = Color.cyan;
        Gizmos.DrawRay(transform.position, transform.forward * 1f);
    }
}