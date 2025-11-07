using UnityEngine;
using System.Collections.Generic;
using Mediapipe.Tasks.Vision.PoseLandmarker;

/// <summary>
/// Main controller that integrates MediaPipe pose tracking with SMPLX character animation.
/// Handles the complete pipeline from landmark detection to joint rotation application.
/// </summary>
public class MediaPipeSMPLXController : MonoBehaviour
{
    #region Inspector Configuration

    [Header("SMPLX Character")]
    [Tooltip("Root transform of the SMPLX character")]
    public Transform smplxRoot;

    [Tooltip("Array of SMPLX joint transforms (55 joints in hierarchical order)")]
    public Transform[] smplxJoints = new Transform[55];

    [Header("MediaPipe Configuration")]
    [Tooltip("Reference to MediaPipe PoseLandmarker")]
    public PoseLandmarker poseLandmarker;

    [Tooltip("Minimum confidence for pose detection")]
    [Range(0f, 1f)]
    public float minDetectionConfidence = 0.5f;

    [Tooltip("Minimum confidence for pose tracking")]
    [Range(0f, 1f)]
    public float minTrackingConfidence = 0.5f;

    [Header("Processing Settings")]
    public LandmarkPreprocessor.FilterSettings preprocessorSettings = new LandmarkPreprocessor.FilterSettings();
    public SMPLXJointCalculator.CalculatorSettings calculatorSettings = new SMPLXJointCalculator.CalculatorSettings();
    public SMPLXRotationSolver.SolverSettings solverSettings = new SMPLXRotationSolver.SolverSettings();

    [Header("Runtime Options")]
    [Tooltip("Apply tracking to SMPLX character")]
    public bool enableTracking = true;

    [Tooltip("Show debug visualization")]
    public bool showDebugVisualization = true;

    [Tooltip("Gizmo size for landmark visualization")]
    public float gizmoSize = 0.02f;

    #endregion

    #region Private Fields

    private LandmarkPreprocessor preprocessor;
    private SMPLXJointCalculator jointCalculator;
    private SMPLXRotationSolver rotationSolver;

    private Vector3[] mediapipeLandmarks;
    private float[] landmarkVisibility;
    private Vector3[] smplxJointPositions;
    private Quaternion[] smplxJointRotations;

    private bool isInitialized = false;
    private float lastUpdateTime;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        Initialize();
    }

    void Update()
    {
        if (!enableTracking || !isInitialized)
            return;

        // Update time tracking
        float currentTime = Time.time;
        float deltaTime = currentTime - lastUpdateTime;
        lastUpdateTime = currentTime;
    }

    void LateUpdate()
    {
        // Apply rotations in LateUpdate to ensure smooth animation
        if (enableTracking && isInitialized && smplxJointRotations != null)
        {
            ApplyRotationsToSMPLX();
        }
    }

    #endregion

    #region Initialization

    private void Initialize()
    {
        // Initialize processing components
        preprocessor = new LandmarkPreprocessor(preprocessorSettings);
        jointCalculator = new SMPLXJointCalculator(calculatorSettings);
        rotationSolver = new SMPLXRotationSolver(solverSettings);

        // Initialize data arrays
        mediapipeLandmarks = new Vector3[33];
        landmarkVisibility = new float[33];
        smplxJointPositions = new Vector3[55];
        smplxJointRotations = new Quaternion[55];

        // Validate SMPLX joint array
        if (smplxJoints == null || smplxJoints.Length != 55)
        {
            Debug.LogError("SMPLX joints array must contain exactly 55 transforms!");
            return;
        }

        // Validate that all joints are assigned
        for (int i = 0; i < 55; i++)
        {
            if (smplxJoints[i] == null)
            {
                Debug.LogWarning($"SMPLX joint at index {i} is not assigned!");
            }
        }

        lastUpdateTime = Time.time;
        isInitialized = true;

        Debug.Log("MediaPipe SMPLX Controller initialized successfully");
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Process MediaPipe pose detection result and update SMPLX character
    /// Call this from your MediaPipe callback
    /// </summary>
    /// <param name="result">PoseLandmarker result containing detected poses</param>
    public void ProcessPoseResult(PoseLandmarkerResult result)
    {
        if (!isInitialized || result == null)
            return;

        // Check if pose was detected
        if (result.poseWorldLandmarks == null || result.poseWorldLandmarks.Count == 0)
        {
            Debug.LogWarning("No pose detected in frame");
            return;
        }

        // Get first detected pose
        var worldLandmarks = result.poseWorldLandmarks[0];

        if (worldLandmarks.Count != 33)
        {
            Debug.LogError($"Invalid landmark count: {worldLandmarks.Count}, expected 33");
            return;
        }

        // Convert MediaPipe landmarks to Vector3 array
        Vector3[] rawLandmarks = new Vector3[33];
        float[] visibility = new float[33];

        for (int i = 0; i < 33; i++)
        {
            var landmark = worldLandmarks[i];
            rawLandmarks[i] = new Vector3(landmark.X, landmark.Y, landmark.Z);
            visibility[i] = landmark.Visibility;
        }

        // Process through pipeline
        UpdateFromLandmarks(rawLandmarks, visibility);
    }

    /// <summary>
    /// Update SMPLX character from raw MediaPipe landmarks
    /// </summary>
    /// <param name="rawLandmarks">Raw 3D landmarks from MediaPipe</param>
    /// <param name="visibility">Visibility scores for each landmark</param>
    public void UpdateFromLandmarks(Vector3[] rawLandmarks, float[] visibility)
    {
        if (!isInitialized)
            return;

        // Step 1: Preprocess landmarks (smoothing, filtering, coordinate transformation)
        mediapipeLandmarks = preprocessor.ProcessLandmarks(
            rawLandmarks,
            visibility,
            Time.time
        );

        // Step 2: Calculate SMPLX joint positions from MediaPipe landmarks
        smplxJointPositions = jointCalculator.CalculateJointPositions(
            mediapipeLandmarks,
            null, // Hand landmarks left (optional)
            null  // Hand landmarks right (optional)
        );

        // Step 3: Solve joint rotations from positions
        smplxJointRotations = rotationSolver.SolveRotations(
            smplxJointPositions,
            smplxRoot
        );

        // Store visibility for debugging
        landmarkVisibility = visibility;
    }

    /// <summary>
    /// Reset tracking state (useful when tracking is lost)
    /// </summary>
    public void ResetTracking()
    {
        if (preprocessor != null)
            preprocessor.Reset();

        if (rotationSolver != null)
            rotationSolver.Reset();

        Debug.Log("Tracking reset");
    }

    /// <summary>
    /// Update settings at runtime
    /// </summary>
    public void UpdateSettings()
    {
        if (preprocessor != null)
            preprocessor.UpdateSettings(preprocessorSettings);
    }

    #endregion

    #region Apply to SMPLX

    private void ApplyRotationsToSMPLX()
    {
        if (smplxJoints == null || smplxJointRotations == null)
            return;

        for (int i = 0; i < 55; i++)
        {
            if (smplxJoints[i] == null)
                continue;

            // Apply local rotation
            smplxJoints[i].localRotation = smplxJointRotations[i];
        }

        // Optional: Update root position from pelvis
        if (smplxRoot != null && smplxJointPositions != null)
        {
            Vector3 pelvisPos = smplxJointPositions[MediaPipeToSMPLXMapper.SMPLX_PELVIS];
            smplxRoot.position = pelvisPos;
        }
    }

    #endregion

    #region Auto-Setup Helpers

    /// <summary>
    /// Automatically find and assign SMPLX joints by name
    /// Call this in Editor or at runtime if joints follow standard naming
    /// </summary>
    [ContextMenu("Auto-Assign SMPLX Joints")]
    public void AutoAssignSMPLXJoints()
    {
        if (smplxRoot == null)
        {
            Debug.LogError("SMPLX root not assigned!");
            return;
        }

        smplxJoints = new Transform[55];
        var mappingTable = MediaPipeToSMPLXMapper.GetMappingTable();

        int foundCount = 0;

        foreach (var mapping in mappingTable)
        {
            // Search for joint by name in hierarchy
            Transform joint = FindChildRecursive(smplxRoot, mapping.smplxName);

            if (joint != null)
            {
                smplxJoints[mapping.smplxIndex] = joint;
                foundCount++;
            }
            else
            {
                Debug.LogWarning($"Could not find joint: {mapping.smplxName}");
            }
        }

        Debug.Log($"Auto-assigned {foundCount}/55 SMPLX joints");
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        // Direct child search
        Transform result = parent.Find(name);
        if (result != null)
            return result;

        // Recursive search
        foreach (Transform child in parent)
        {
            result = FindChildRecursive(child, name);
            if (result != null)
                return result;
        }

        return null;
    }

    #endregion

    #region Debug Visualization

    void OnDrawGizmos()
    {
        if (!showDebugVisualization || !isInitialized)
            return;

        // Draw MediaPipe landmarks
        if (mediapipeLandmarks != null)
        {
            Gizmos.color = Color.green;
            for (int i = 0; i < mediapipeLandmarks.Length; i++)
            {
                float visibility = landmarkVisibility != null && i < landmarkVisibility.Length ?
                                   landmarkVisibility[i] : 1f;

                if (visibility > 0.5f)
                {
                    Vector3 worldPos = smplxRoot != null ?
                                      smplxRoot.TransformPoint(mediapipeLandmarks[i]) :
                                      mediapipeLandmarks[i];
                    Gizmos.DrawSphere(worldPos, gizmoSize);
                }
            }
        }

        // Draw SMPLX joint positions
        if (smplxJointPositions != null)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < smplxJointPositions.Length; i++)
            {
                Vector3 worldPos = smplxRoot != null ?
                                  smplxRoot.TransformPoint(smplxJointPositions[i]) :
                                  smplxJointPositions[i];
                Gizmos.DrawSphere(worldPos, gizmoSize * 1.5f);
            }
        }

        // Draw bones
        DrawBones();
    }

    private void DrawBones()
    {
        if (smplxJointPositions == null)
            return;

        Gizmos.color = Color.yellow;
        var parentIndices = MediaPipeToSMPLXMapper.GetParentIndices();

        for (int i = 0; i < 55; i++)
        {
            int parentIndex = parentIndices[i];
            if (parentIndex < 0)
                continue;

            Vector3 childPos = smplxRoot != null ?
                              smplxRoot.TransformPoint(smplxJointPositions[i]) :
                              smplxJointPositions[i];
            Vector3 parentPos = smplxRoot != null ?
                               smplxRoot.TransformPoint(smplxJointPositions[parentIndex]) :
                               smplxJointPositions[parentIndex];

            Gizmos.DrawLine(parentPos, childPos);
        }
    }

    #endregion

    #region Public Accessors

    /// <summary>
    /// Get current MediaPipe landmarks
    /// </summary>
    public Vector3[] GetMediaPipeLandmarks() => mediapipeLandmarks;

    /// <summary>
    /// Get current SMPLX joint positions
    /// </summary>
    public Vector3[] GetSMPLXJointPositions() => smplxJointPositions;

    /// <summary>
    /// Get current SMPLX joint rotations
    /// </summary>
    public Quaternion[] GetSMPLXJointRotations() => smplxJointRotations;

    /// <summary>
    /// Check if tracking is active and valid
    /// </summary>
    public bool IsTrackingActive()
    {
        if (!isInitialized || !enableTracking)
            return false;

        // Check if we have recent landmark data
        if (preprocessor == null)
            return false;

        float avgVisibility = preprocessor.GetAverageVisibility();
        return avgVisibility > 0.3f;
    }

    #endregion
}
