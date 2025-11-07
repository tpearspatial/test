using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Calculates SMPLX joint positions from MediaPipe landmarks using the mapping configuration.
/// Handles direct mapping, interpolation, and estimation of joint positions.
/// </summary>
public class SMPLXJointCalculator
{
    #region Configuration

    [System.Serializable]
    public class CalculatorSettings
    {
        [Tooltip("Use estimated positions for hand joints when hand tracker is unavailable")]
        public bool estimateHandJoints = true;

        [Tooltip("Default finger curl amount (0-1) when estimating")]
        [Range(0f, 1f)]
        public float defaultFingerCurl = 0.3f;

        [Tooltip("Default finger spread amount (0-1) when estimating")]
        [Range(0f, 1f)]
        public float defaultFingerSpread = 0.1f;

        [Tooltip("Average finger length ratio relative to hand size")]
        public float fingerLengthRatio = 0.15f;
    }

    #endregion

    #region Fields

    private CalculatorSettings settings;
    private Vector3[] smplxJointPositions;
    private MediaPipeToSMPLXMapper.JointMapping[] mappingTable;

    #endregion

    #region Initialization

    public SMPLXJointCalculator(CalculatorSettings settings = null)
    {
        this.settings = settings ?? new CalculatorSettings();
        this.smplxJointPositions = new Vector3[55]; // SMPLX has 55 joints
        this.mappingTable = MediaPipeToSMPLXMapper.GetMappingTable();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Calculate all SMPLX joint positions from processed MediaPipe landmarks
    /// </summary>
    /// <param name="mpLandmarks">Processed MediaPipe landmarks (33 positions)</param>
    /// <param name="handLandmarksLeft">Optional left hand landmarks (21 positions)</param>
    /// <param name="handLandmarksRight">Optional right hand landmarks (21 positions)</param>
    /// <returns>Array of 55 SMPLX joint positions</returns>
    public Vector3[] CalculateJointPositions(
        Vector3[] mpLandmarks,
        Vector3[] handLandmarksLeft = null,
        Vector3[] handLandmarksRight = null)
    {
        if (mpLandmarks == null || mpLandmarks.Length != 33)
        {
            Debug.LogError("Invalid MediaPipe landmarks. Expected 33 positions.");
            return smplxJointPositions;
        }

        // Process each joint according to its mapping type
        foreach (var mapping in mappingTable)
        {
            Vector3 position = CalculateJointPosition(
                mapping,
                mpLandmarks,
                handLandmarksLeft,
                handLandmarksRight
            );

            smplxJointPositions[mapping.smplxIndex] = position;
        }

        return smplxJointPositions;
    }

    /// <summary>
    /// Calculate a single joint position based on its mapping
    /// </summary>
    private Vector3 CalculateJointPosition(
        MediaPipeToSMPLXMapper.JointMapping mapping,
        Vector3[] mpLandmarks,
        Vector3[] handLandmarksLeft,
        Vector3[] handLandmarksRight)
    {
        switch (mapping.mappingType)
        {
            case MediaPipeToSMPLXMapper.MappingType.Direct:
                return CalculateDirectMapping(mapping, mpLandmarks);

            case MediaPipeToSMPLXMapper.MappingType.Interpolated:
                return CalculateInterpolatedMapping(mapping, mpLandmarks);

            case MediaPipeToSMPLXMapper.MappingType.Estimated:
                return CalculateEstimatedMapping(mapping, mpLandmarks);

            case MediaPipeToSMPLXMapper.MappingType.HandTracker:
                return CalculateHandTrackerMapping(
                    mapping,
                    mpLandmarks,
                    handLandmarksLeft,
                    handLandmarksRight
                );

            case MediaPipeToSMPLXMapper.MappingType.FaceMesh:
                return CalculateFaceMeshMapping(mapping, mpLandmarks);

            default:
                return Vector3.zero;
        }
    }

    #endregion

    #region Mapping Calculations

    /// <summary>
    /// Direct one-to-one mapping from MediaPipe landmark
    /// </summary>
    private Vector3 CalculateDirectMapping(
        MediaPipeToSMPLXMapper.JointMapping mapping,
        Vector3[] mpLandmarks)
    {
        if (mapping.mediapipeIndices.Length == 0)
            return Vector3.zero;

        int mpIndex = mapping.mediapipeIndices[0];
        return mpLandmarks[mpIndex];
    }

    /// <summary>
    /// Interpolated position from multiple MediaPipe landmarks
    /// </summary>
    private Vector3 CalculateInterpolatedMapping(
        MediaPipeToSMPLXMapper.JointMapping mapping,
        Vector3[] mpLandmarks)
    {
        if (mapping.mediapipeIndices.Length == 0 || mapping.weights == null)
            return Vector3.zero;

        Vector3 result = Vector3.zero;

        for (int i = 0; i < mapping.mediapipeIndices.Length; i++)
        {
            int mpIndex = mapping.mediapipeIndices[i];
            float weight = mapping.weights[i];
            result += mpLandmarks[mpIndex] * weight;
        }

        return result;
    }

    /// <summary>
    /// Estimated position for joints without direct tracking (e.g., basic finger joints)
    /// </summary>
    private Vector3 CalculateEstimatedMapping(
        MediaPipeToSMPLXMapper.JointMapping mapping,
        Vector3[] mpLandmarks)
    {
        // Get wrist and finger base positions
        int jointIndex = mapping.smplxIndex;

        // Determine if left or right hand
        bool isLeftHand = jointIndex >= 25 && jointIndex <= 39;
        int wristIdx = isLeftHand ? MediaPipeToSMPLXMapper.MP_LEFT_WRIST : MediaPipeToSMPLXMapper.MP_RIGHT_WRIST;

        Vector3 wristPos = mpLandmarks[wristIdx];

        // Estimate finger joint positions based on default poses
        if (jointIndex >= 31 && jointIndex <= 33) // Left pinky
        {
            int pinkyBaseIdx = MediaPipeToSMPLXMapper.MP_LEFT_PINKY;
            return EstimateFingerJoint(wristPos, mpLandmarks[pinkyBaseIdx], jointIndex - 31);
        }
        else if (jointIndex >= 37 && jointIndex <= 39) // Left thumb
        {
            int thumbBaseIdx = MediaPipeToSMPLXMapper.MP_LEFT_THUMB;
            return EstimateFingerJoint(wristPos, mpLandmarks[thumbBaseIdx], jointIndex - 37);
        }
        else if (jointIndex >= 46 && jointIndex <= 48) // Right pinky
        {
            int pinkyBaseIdx = MediaPipeToSMPLXMapper.MP_RIGHT_PINKY;
            return EstimateFingerJoint(wristPos, mpLandmarks[pinkyBaseIdx], jointIndex - 46);
        }
        else if (jointIndex >= 52 && jointIndex <= 54) // Right thumb
        {
            int thumbBaseIdx = MediaPipeToSMPLXMapper.MP_RIGHT_THUMB;
            return EstimateFingerJoint(wristPos, mpLandmarks[thumbBaseIdx], jointIndex - 52);
        }

        // Default: use wrist position
        return wristPos;
    }

    /// <summary>
    /// Estimate finger joint position from wrist and finger base
    /// </summary>
    private Vector3 EstimateFingerJoint(Vector3 wristPos, Vector3 fingerBase, int jointIndex)
    {
        Vector3 direction = (fingerBase - wristPos).normalized;
        float baseDistance = Vector3.Distance(wristPos, fingerBase);

        // Estimate joint positions along the finger direction
        float[] distances = new float[] { 0.3f, 0.6f, 0.9f }; // Proximal, intermediate, distal
        float distanceMultiplier = distances[Mathf.Clamp(jointIndex, 0, 2)];

        // Apply finger curl
        Vector3 curlOffset = Vector3.down * (settings.defaultFingerCurl * 0.02f * (jointIndex + 1));

        return fingerBase + direction * (baseDistance * distanceMultiplier * settings.fingerLengthRatio) + curlOffset;
    }

    /// <summary>
    /// Map from detailed hand tracker data (21 landmarks per hand)
    /// </summary>
    private Vector3 CalculateHandTrackerMapping(
        MediaPipeToSMPLXMapper.JointMapping mapping,
        Vector3[] mpLandmarks,
        Vector3[] handLandmarksLeft,
        Vector3[] handLandmarksRight)
    {
        int jointIndex = mapping.smplxIndex;

        // Determine if left or right hand
        bool isLeftHand = jointIndex >= 25 && jointIndex <= 39;
        Vector3[] handLandmarks = isLeftHand ? handLandmarksLeft : handLandmarksRight;

        // If hand landmarks not available, fall back to estimation
        if (handLandmarks == null || handLandmarks.Length != 21)
        {
            if (settings.estimateHandJoints)
                return CalculateEstimatedMapping(mapping, mpLandmarks);
            else
                return Vector3.zero;
        }

        // Map SMPLX finger joints to MediaPipe hand landmarks
        int handLandmarkIndex = GetHandLandmarkIndex(jointIndex, isLeftHand);

        if (handLandmarkIndex >= 0 && handLandmarkIndex < 21)
            return handLandmarks[handLandmarkIndex];

        return Vector3.zero;
    }

    /// <summary>
    /// Map SMPLX finger joint index to MediaPipe hand landmark index
    /// </summary>
    private int GetHandLandmarkIndex(int smplxJointIndex, bool isLeftHand)
    {
        // MediaPipe Hand landmarks:
        // 0: WRIST
        // 1-4: THUMB (CMC, MCP, IP, TIP)
        // 5-8: INDEX (MCP, PIP, DIP, TIP)
        // 9-12: MIDDLE (MCP, PIP, DIP, TIP)
        // 13-16: RING (MCP, PIP, DIP, TIP)
        // 17-20: PINKY (MCP, PIP, DIP, TIP)

        if (isLeftHand)
        {
            // Left hand mapping
            if (smplxJointIndex >= 25 && smplxJointIndex <= 27) // left_index
                return 5 + (smplxJointIndex - 25); // MP indices 5-7
            if (smplxJointIndex >= 28 && smplxJointIndex <= 30) // left_middle
                return 9 + (smplxJointIndex - 28); // MP indices 9-11
            if (smplxJointIndex >= 31 && smplxJointIndex <= 33) // left_pinky
                return 17 + (smplxJointIndex - 31); // MP indices 17-19
            if (smplxJointIndex >= 34 && smplxJointIndex <= 36) // left_ring
                return 13 + (smplxJointIndex - 34); // MP indices 13-15
            if (smplxJointIndex >= 37 && smplxJointIndex <= 39) // left_thumb
                return 1 + (smplxJointIndex - 37); // MP indices 1-3
        }
        else
        {
            // Right hand mapping (same structure)
            if (smplxJointIndex >= 40 && smplxJointIndex <= 42) // right_index
                return 5 + (smplxJointIndex - 40);
            if (smplxJointIndex >= 43 && smplxJointIndex <= 45) // right_middle
                return 9 + (smplxJointIndex - 43);
            if (smplxJointIndex >= 46 && smplxJointIndex <= 48) // right_pinky
                return 17 + (smplxJointIndex - 46);
            if (smplxJointIndex >= 49 && smplxJointIndex <= 51) // right_ring
                return 13 + (smplxJointIndex - 49);
            if (smplxJointIndex >= 52 && smplxJointIndex <= 54) // right_thumb
                return 1 + (smplxJointIndex - 52);
        }

        return -1;
    }

    /// <summary>
    /// Calculate position from face mesh data (jaw, eyes)
    /// </summary>
    private Vector3 CalculateFaceMeshMapping(
        MediaPipeToSMPLXMapper.JointMapping mapping,
        Vector3[] mpLandmarks)
    {
        // Without face mesh, use approximations from pose landmarks
        int jointIndex = mapping.smplxIndex;

        if (jointIndex == MediaPipeToSMPLXMapper.SMPLX_JAW)
        {
            // Estimate jaw from mouth landmarks
            Vector3 mouthLeft = mpLandmarks[MediaPipeToSMPLXMapper.MP_MOUTH_LEFT];
            Vector3 mouthRight = mpLandmarks[MediaPipeToSMPLXMapper.MP_MOUTH_RIGHT];
            Vector3 mouthCenter = (mouthLeft + mouthRight) * 0.5f;

            // Offset slightly down for jaw position
            return mouthCenter + Vector3.down * 0.02f;
        }

        // Default: interpolate from mapping
        return CalculateInterpolatedMapping(mapping, mpLandmarks);
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Get calculated position for a specific SMPLX joint
    /// </summary>
    public Vector3 GetJointPosition(int smplxIndex)
    {
        if (smplxIndex < 0 || smplxIndex >= 55)
            return Vector3.zero;

        return smplxJointPositions[smplxIndex];
    }

    /// <summary>
    /// Calculate bone direction between parent and child joints
    /// </summary>
    public Vector3 GetBoneDirection(int parentIndex, int childIndex)
    {
        Vector3 parentPos = GetJointPosition(parentIndex);
        Vector3 childPos = GetJointPosition(childIndex);

        return (childPos - parentPos).normalized;
    }

    /// <summary>
    /// Calculate bone length between parent and child joints
    /// </summary>
    public float GetBoneLength(int parentIndex, int childIndex)
    {
        Vector3 parentPos = GetJointPosition(parentIndex);
        Vector3 childPos = GetJointPosition(childIndex);

        return Vector3.Distance(parentPos, childPos);
    }

    /// <summary>
    /// Get all calculated joint positions
    /// </summary>
    public Vector3[] GetAllJointPositions()
    {
        return smplxJointPositions;
    }

    #endregion
}
