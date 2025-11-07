using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;

/// <summary>
/// Core mapping configuration and data structures for MediaPipe to SMPLX conversion.
/// This class defines the mapping relationships between MediaPipe's 33 landmarks
/// and SMPLX's 55 joints.
/// </summary>
public class MediaPipeToSMPLXMapper
{
    #region MediaPipe Landmark Indices

    // Head Region (0-10)
    public const int MP_NOSE = 0;
    public const int MP_LEFT_EYE_INNER = 1;
    public const int MP_LEFT_EYE = 2;
    public const int MP_LEFT_EYE_OUTER = 3;
    public const int MP_RIGHT_EYE_INNER = 4;
    public const int MP_RIGHT_EYE = 5;
    public const int MP_RIGHT_EYE_OUTER = 6;
    public const int MP_LEFT_EAR = 7;
    public const int MP_RIGHT_EAR = 8;
    public const int MP_MOUTH_LEFT = 9;
    public const int MP_MOUTH_RIGHT = 10;

    // Upper Body (11-22)
    public const int MP_LEFT_SHOULDER = 11;
    public const int MP_RIGHT_SHOULDER = 12;
    public const int MP_LEFT_ELBOW = 13;
    public const int MP_RIGHT_ELBOW = 14;
    public const int MP_LEFT_WRIST = 15;
    public const int MP_RIGHT_WRIST = 16;
    public const int MP_LEFT_PINKY = 17;
    public const int MP_RIGHT_PINKY = 18;
    public const int MP_LEFT_INDEX = 19;
    public const int MP_RIGHT_INDEX = 20;
    public const int MP_LEFT_THUMB = 21;
    public const int MP_RIGHT_THUMB = 22;

    // Lower Body (23-32)
    public const int MP_LEFT_HIP = 23;
    public const int MP_RIGHT_HIP = 24;
    public const int MP_LEFT_KNEE = 25;
    public const int MP_RIGHT_KNEE = 26;
    public const int MP_LEFT_ANKLE = 27;
    public const int MP_RIGHT_ANKLE = 28;
    public const int MP_LEFT_HEEL = 29;
    public const int MP_RIGHT_HEEL = 30;
    public const int MP_LEFT_FOOT_INDEX = 31;
    public const int MP_RIGHT_FOOT_INDEX = 32;

    #endregion

    #region SMPLX Joint Indices

    // Body Joints (0-21)
    public const int SMPLX_PELVIS = 0;
    public const int SMPLX_LEFT_HIP = 1;
    public const int SMPLX_RIGHT_HIP = 2;
    public const int SMPLX_SPINE1 = 3;
    public const int SMPLX_LEFT_KNEE = 4;
    public const int SMPLX_RIGHT_KNEE = 5;
    public const int SMPLX_SPINE2 = 6;
    public const int SMPLX_LEFT_ANKLE = 7;
    public const int SMPLX_RIGHT_ANKLE = 8;
    public const int SMPLX_SPINE3 = 9;
    public const int SMPLX_LEFT_FOOT = 10;
    public const int SMPLX_RIGHT_FOOT = 11;
    public const int SMPLX_NECK = 12;
    public const int SMPLX_LEFT_COLLAR = 13;
    public const int SMPLX_RIGHT_COLLAR = 14;
    public const int SMPLX_HEAD = 15;
    public const int SMPLX_LEFT_SHOULDER = 16;
    public const int SMPLX_RIGHT_SHOULDER = 17;
    public const int SMPLX_LEFT_ELBOW = 18;
    public const int SMPLX_RIGHT_ELBOW = 19;
    public const int SMPLX_LEFT_WRIST = 20;
    public const int SMPLX_RIGHT_WRIST = 21;

    // Face Joints (22-24)
    public const int SMPLX_JAW = 22;
    public const int SMPLX_LEFT_EYE = 23;
    public const int SMPLX_RIGHT_EYE = 24;

    // Left Hand Joints (25-39)
    public const int SMPLX_LEFT_INDEX1 = 25;
    public const int SMPLX_LEFT_INDEX2 = 26;
    public const int SMPLX_LEFT_INDEX3 = 27;
    public const int SMPLX_LEFT_MIDDLE1 = 28;
    public const int SMPLX_LEFT_MIDDLE2 = 29;
    public const int SMPLX_LEFT_MIDDLE3 = 30;
    public const int SMPLX_LEFT_PINKY1 = 31;
    public const int SMPLX_LEFT_PINKY2 = 32;
    public const int SMPLX_LEFT_PINKY3 = 33;
    public const int SMPLX_LEFT_RING1 = 34;
    public const int SMPLX_LEFT_RING2 = 35;
    public const int SMPLX_LEFT_RING3 = 36;
    public const int SMPLX_LEFT_THUMB1 = 37;
    public const int SMPLX_LEFT_THUMB2 = 38;
    public const int SMPLX_LEFT_THUMB3 = 39;

    // Right Hand Joints (40-54)
    public const int SMPLX_RIGHT_INDEX1 = 40;
    public const int SMPLX_RIGHT_INDEX2 = 41;
    public const int SMPLX_RIGHT_INDEX3 = 42;
    public const int SMPLX_RIGHT_MIDDLE1 = 43;
    public const int SMPLX_RIGHT_MIDDLE2 = 44;
    public const int SMPLX_RIGHT_MIDDLE3 = 45;
    public const int SMPLX_RIGHT_PINKY1 = 46;
    public const int SMPLX_RIGHT_PINKY2 = 47;
    public const int SMPLX_RIGHT_PINKY3 = 48;
    public const int SMPLX_RIGHT_RING1 = 49;
    public const int SMPLX_RIGHT_RING2 = 50;
    public const int SMPLX_RIGHT_RING3 = 51;
    public const int SMPLX_RIGHT_THUMB1 = 52;
    public const int SMPLX_RIGHT_THUMB2 = 53;
    public const int SMPLX_RIGHT_THUMB3 = 54;

    #endregion

    #region Mapping Types

    public enum MappingType
    {
        Direct,         // One-to-one correspondence
        Interpolated,   // Calculated from multiple landmarks
        Estimated,      // Approximated or requires additional data
        HandTracker,    // Requires MediaPipe Hand Landmarker
        FaceMesh        // Requires MediaPipe Face Mesh
    }

    [System.Serializable]
    public struct JointMapping
    {
        public int smplxIndex;
        public string smplxName;
        public MappingType mappingType;
        public int[] mediapipeIndices;  // Source landmark indices
        public float[] weights;          // Interpolation weights (if applicable)
        public string description;

        public JointMapping(int smplxIdx, string name, MappingType type,
                           int[] mpIndices, float[] wts = null, string desc = "")
        {
            smplxIndex = smplxIdx;
            smplxName = name;
            mappingType = type;
            mediapipeIndices = mpIndices;
            weights = wts;
            description = desc;
        }
    }

    #endregion

    #region Mapping Definitions

    private static JointMapping[] mappingTable;

    public static JointMapping[] GetMappingTable()
    {
        if (mappingTable != null) return mappingTable;

        mappingTable = new JointMapping[]
        {
            // Body Joints
            new JointMapping(SMPLX_PELVIS, "pelvis", MappingType.Interpolated,
                new int[] { MP_LEFT_HIP, MP_RIGHT_HIP },
                new float[] { 0.5f, 0.5f },
                "Midpoint between hips"),

            new JointMapping(SMPLX_LEFT_HIP, "left_hip", MappingType.Direct,
                new int[] { MP_LEFT_HIP },
                null, "Direct mapping"),

            new JointMapping(SMPLX_RIGHT_HIP, "right_hip", MappingType.Direct,
                new int[] { MP_RIGHT_HIP },
                null, "Direct mapping"),

            new JointMapping(SMPLX_SPINE1, "spine1", MappingType.Interpolated,
                new int[] { MP_LEFT_HIP, MP_RIGHT_HIP, MP_LEFT_SHOULDER, MP_RIGHT_SHOULDER },
                new float[] { 0.35f, 0.35f, 0.15f, 0.15f },
                "Lower spine, closer to hips"),

            new JointMapping(SMPLX_LEFT_KNEE, "left_knee", MappingType.Direct,
                new int[] { MP_LEFT_KNEE },
                null, "Direct mapping"),

            new JointMapping(SMPLX_RIGHT_KNEE, "right_knee", MappingType.Direct,
                new int[] { MP_RIGHT_KNEE },
                null, "Direct mapping"),

            new JointMapping(SMPLX_SPINE2, "spine2", MappingType.Interpolated,
                new int[] { MP_LEFT_HIP, MP_RIGHT_HIP, MP_LEFT_SHOULDER, MP_RIGHT_SHOULDER },
                new float[] { 0.2f, 0.2f, 0.3f, 0.3f },
                "Mid spine"),

            new JointMapping(SMPLX_LEFT_ANKLE, "left_ankle", MappingType.Direct,
                new int[] { MP_LEFT_ANKLE },
                null, "Direct mapping"),

            new JointMapping(SMPLX_RIGHT_ANKLE, "right_ankle", MappingType.Direct,
                new int[] { MP_RIGHT_ANKLE },
                null, "Direct mapping"),

            new JointMapping(SMPLX_SPINE3, "spine3", MappingType.Interpolated,
                new int[] { MP_LEFT_SHOULDER, MP_RIGHT_SHOULDER },
                new float[] { 0.5f, 0.5f },
                "Upper spine at shoulder level"),

            new JointMapping(SMPLX_LEFT_FOOT, "left_foot", MappingType.Direct,
                new int[] { MP_LEFT_FOOT_INDEX },
                null, "Use foot index landmark"),

            new JointMapping(SMPLX_RIGHT_FOOT, "right_foot", MappingType.Direct,
                new int[] { MP_RIGHT_FOOT_INDEX },
                null, "Use foot index landmark"),

            new JointMapping(SMPLX_NECK, "neck", MappingType.Interpolated,
                new int[] { MP_LEFT_SHOULDER, MP_RIGHT_SHOULDER, MP_NOSE },
                new float[] { 0.35f, 0.35f, 0.3f },
                "Between shoulders and head"),

            new JointMapping(SMPLX_LEFT_COLLAR, "left_collar", MappingType.Interpolated,
                new int[] { MP_LEFT_SHOULDER, MP_RIGHT_SHOULDER, MP_LEFT_SHOULDER },
                new float[] { 0.75f, 0.0f, 0.25f },
                "Clavicle, close to spine3"),

            new JointMapping(SMPLX_RIGHT_COLLAR, "right_collar", MappingType.Interpolated,
                new int[] { MP_LEFT_SHOULDER, MP_RIGHT_SHOULDER, MP_RIGHT_SHOULDER },
                new float[] { 0.0f, 0.75f, 0.25f },
                "Clavicle, close to spine3"),

            new JointMapping(SMPLX_HEAD, "head", MappingType.Direct,
                new int[] { MP_NOSE },
                null, "Use nose as head position"),

            new JointMapping(SMPLX_LEFT_SHOULDER, "left_shoulder", MappingType.Direct,
                new int[] { MP_LEFT_SHOULDER },
                null, "Direct mapping"),

            new JointMapping(SMPLX_RIGHT_SHOULDER, "right_shoulder", MappingType.Direct,
                new int[] { MP_RIGHT_SHOULDER },
                null, "Direct mapping"),

            new JointMapping(SMPLX_LEFT_ELBOW, "left_elbow", MappingType.Direct,
                new int[] { MP_LEFT_ELBOW },
                null, "Direct mapping"),

            new JointMapping(SMPLX_RIGHT_ELBOW, "right_elbow", MappingType.Direct,
                new int[] { MP_RIGHT_ELBOW },
                null, "Direct mapping"),

            new JointMapping(SMPLX_LEFT_WRIST, "left_wrist", MappingType.Direct,
                new int[] { MP_LEFT_WRIST },
                null, "Direct mapping"),

            new JointMapping(SMPLX_RIGHT_WRIST, "right_wrist", MappingType.Direct,
                new int[] { MP_RIGHT_WRIST },
                null, "Direct mapping"),

            // Face Joints
            new JointMapping(SMPLX_JAW, "jaw", MappingType.FaceMesh,
                new int[] { MP_MOUTH_LEFT, MP_MOUTH_RIGHT },
                new float[] { 0.5f, 0.5f },
                "Requires Face Mesh for accurate tracking"),

            new JointMapping(SMPLX_LEFT_EYE, "left_eye_smplx", MappingType.Direct,
                new int[] { MP_LEFT_EYE },
                null, "Use eye center"),

            new JointMapping(SMPLX_RIGHT_EYE, "right_eye_smplx", MappingType.Direct,
                new int[] { MP_RIGHT_EYE },
                null, "Use eye center"),

            // Hand Joints - Left
            new JointMapping(SMPLX_LEFT_INDEX1, "left_index1", MappingType.HandTracker,
                new int[] { MP_LEFT_INDEX },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_LEFT_INDEX2, "left_index2", MappingType.HandTracker,
                new int[] { MP_LEFT_INDEX },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_LEFT_INDEX3, "left_index3", MappingType.HandTracker,
                new int[] { MP_LEFT_INDEX },
                null, "Requires Hand Landmarker"),

            new JointMapping(SMPLX_LEFT_MIDDLE1, "left_middle1", MappingType.HandTracker,
                new int[] { MP_LEFT_WRIST },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_LEFT_MIDDLE2, "left_middle2", MappingType.HandTracker,
                new int[] { MP_LEFT_WRIST },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_LEFT_MIDDLE3, "left_middle3", MappingType.HandTracker,
                new int[] { MP_LEFT_WRIST },
                null, "Requires Hand Landmarker"),

            new JointMapping(SMPLX_LEFT_PINKY1, "left_pinky1", MappingType.Estimated,
                new int[] { MP_LEFT_PINKY },
                null, "Estimated from pinky base"),
            new JointMapping(SMPLX_LEFT_PINKY2, "left_pinky2", MappingType.Estimated,
                new int[] { MP_LEFT_PINKY },
                null, "Estimated from pinky base"),
            new JointMapping(SMPLX_LEFT_PINKY3, "left_pinky3", MappingType.Estimated,
                new int[] { MP_LEFT_PINKY },
                null, "Estimated from pinky base"),

            new JointMapping(SMPLX_LEFT_RING1, "left_ring1", MappingType.HandTracker,
                new int[] { MP_LEFT_WRIST },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_LEFT_RING2, "left_ring2", MappingType.HandTracker,
                new int[] { MP_LEFT_WRIST },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_LEFT_RING3, "left_ring3", MappingType.HandTracker,
                new int[] { MP_LEFT_WRIST },
                null, "Requires Hand Landmarker"),

            new JointMapping(SMPLX_LEFT_THUMB1, "left_thumb1", MappingType.Estimated,
                new int[] { MP_LEFT_THUMB },
                null, "Estimated from thumb base"),
            new JointMapping(SMPLX_LEFT_THUMB2, "left_thumb2", MappingType.Estimated,
                new int[] { MP_LEFT_THUMB },
                null, "Estimated from thumb base"),
            new JointMapping(SMPLX_LEFT_THUMB3, "left_thumb3", MappingType.Estimated,
                new int[] { MP_LEFT_THUMB },
                null, "Estimated from thumb base"),

            // Hand Joints - Right (similar structure)
            new JointMapping(SMPLX_RIGHT_INDEX1, "right_index1", MappingType.HandTracker,
                new int[] { MP_RIGHT_INDEX },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_RIGHT_INDEX2, "right_index2", MappingType.HandTracker,
                new int[] { MP_RIGHT_INDEX },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_RIGHT_INDEX3, "right_index3", MappingType.HandTracker,
                new int[] { MP_RIGHT_INDEX },
                null, "Requires Hand Landmarker"),

            new JointMapping(SMPLX_RIGHT_MIDDLE1, "right_middle1", MappingType.HandTracker,
                new int[] { MP_RIGHT_WRIST },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_RIGHT_MIDDLE2, "right_middle2", MappingType.HandTracker,
                new int[] { MP_RIGHT_WRIST },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_RIGHT_MIDDLE3, "right_middle3", MappingType.HandTracker,
                new int[] { MP_RIGHT_WRIST },
                null, "Requires Hand Landmarker"),

            new JointMapping(SMPLX_RIGHT_PINKY1, "right_pinky1", MappingType.Estimated,
                new int[] { MP_RIGHT_PINKY },
                null, "Estimated from pinky base"),
            new JointMapping(SMPLX_RIGHT_PINKY2, "right_pinky2", MappingType.Estimated,
                new int[] { MP_RIGHT_PINKY },
                null, "Estimated from pinky base"),
            new JointMapping(SMPLX_RIGHT_PINKY3, "right_pinky3", MappingType.Estimated,
                new int[] { MP_RIGHT_PINKY },
                null, "Estimated from pinky base"),

            new JointMapping(SMPLX_RIGHT_RING1, "right_ring1", MappingType.HandTracker,
                new int[] { MP_RIGHT_WRIST },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_RIGHT_RING2, "right_ring2", MappingType.HandTracker,
                new int[] { MP_RIGHT_WRIST },
                null, "Requires Hand Landmarker"),
            new JointMapping(SMPLX_RIGHT_RING3, "right_ring3", MappingType.HandTracker,
                new int[] { MP_RIGHT_WRIST },
                null, "Requires Hand Landmarker"),

            new JointMapping(SMPLX_RIGHT_THUMB1, "right_thumb1", MappingType.Estimated,
                new int[] { MP_RIGHT_THUMB },
                null, "Estimated from thumb base"),
            new JointMapping(SMPLX_RIGHT_THUMB2, "right_thumb2", MappingType.Estimated,
                new int[] { MP_RIGHT_THUMB },
                null, "Estimated from thumb base"),
            new JointMapping(SMPLX_RIGHT_THUMB3, "right_thumb3", MappingType.Estimated,
                new int[] { MP_RIGHT_THUMB },
                null, "Estimated from thumb base"),
        };

        return mappingTable;
    }

    #endregion

    #region Parent-Child Hierarchy

    private static int[] parentIndices;

    /// <summary>
    /// Returns the parent joint index for each SMPLX joint.
    /// -1 indicates root (no parent)
    /// </summary>
    public static int[] GetParentIndices()
    {
        if (parentIndices != null) return parentIndices;

        parentIndices = new int[55];

        // Body joints
        parentIndices[0] = -1;  // pelvis (root)
        parentIndices[1] = 0;   // left_hip
        parentIndices[2] = 0;   // right_hip
        parentIndices[3] = 0;   // spine1
        parentIndices[4] = 1;   // left_knee
        parentIndices[5] = 2;   // right_knee
        parentIndices[6] = 3;   // spine2
        parentIndices[7] = 4;   // left_ankle
        parentIndices[8] = 5;   // right_ankle
        parentIndices[9] = 6;   // spine3
        parentIndices[10] = 7;  // left_foot
        parentIndices[11] = 8;  // right_foot
        parentIndices[12] = 9;  // neck
        parentIndices[13] = 9;  // left_collar
        parentIndices[14] = 9;  // right_collar
        parentIndices[15] = 12; // head
        parentIndices[16] = 13; // left_shoulder
        parentIndices[17] = 14; // right_shoulder
        parentIndices[18] = 16; // left_elbow
        parentIndices[19] = 17; // right_elbow
        parentIndices[20] = 18; // left_wrist
        parentIndices[21] = 19; // right_wrist

        // Face joints
        parentIndices[22] = 15; // jaw
        parentIndices[23] = 15; // left_eye
        parentIndices[24] = 15; // right_eye

        // Left hand
        parentIndices[25] = 20; parentIndices[26] = 25; parentIndices[27] = 26; // left_index
        parentIndices[28] = 20; parentIndices[29] = 28; parentIndices[30] = 29; // left_middle
        parentIndices[31] = 20; parentIndices[32] = 31; parentIndices[33] = 32; // left_pinky
        parentIndices[34] = 20; parentIndices[35] = 34; parentIndices[36] = 35; // left_ring
        parentIndices[37] = 20; parentIndices[38] = 37; parentIndices[39] = 38; // left_thumb

        // Right hand
        parentIndices[40] = 21; parentIndices[41] = 40; parentIndices[42] = 41; // right_index
        parentIndices[43] = 21; parentIndices[44] = 43; parentIndices[45] = 44; // right_middle
        parentIndices[46] = 21; parentIndices[47] = 46; parentIndices[48] = 47; // right_pinky
        parentIndices[49] = 21; parentIndices[50] = 49; parentIndices[51] = 50; // right_ring
        parentIndices[52] = 21; parentIndices[53] = 52; parentIndices[54] = 53; // right_thumb

        return parentIndices;
    }

    #endregion

    #region T-Pose Default Directions

    /// <summary>
    /// Returns the default bone direction in T-pose for each joint.
    /// Used for calculating rotations from positions.
    /// </summary>
    public static Vector3 GetTPoseBoneDirection(int smplxJointIndex)
    {
        switch (smplxJointIndex)
        {
            // Spine chain - upward
            case SMPLX_PELVIS:
            case SMPLX_SPINE1:
            case SMPLX_SPINE2:
            case SMPLX_SPINE3:
            case SMPLX_NECK:
            case SMPLX_HEAD:
                return Vector3.up;

            // Left arm chain - left (negative X)
            case SMPLX_LEFT_COLLAR:
            case SMPLX_LEFT_SHOULDER:
            case SMPLX_LEFT_ELBOW:
            case SMPLX_LEFT_WRIST:
                return Vector3.left;

            // Right arm chain - right (positive X)
            case SMPLX_RIGHT_COLLAR:
            case SMPLX_RIGHT_SHOULDER:
            case SMPLX_RIGHT_ELBOW:
            case SMPLX_RIGHT_WRIST:
                return Vector3.right;

            // Left leg chain - downward
            case SMPLX_LEFT_HIP:
            case SMPLX_LEFT_KNEE:
            case SMPLX_LEFT_ANKLE:
            case SMPLX_LEFT_FOOT:
                return Vector3.down;

            // Right leg chain - downward
            case SMPLX_RIGHT_HIP:
            case SMPLX_RIGHT_KNEE:
            case SMPLX_RIGHT_ANKLE:
            case SMPLX_RIGHT_FOOT:
                return Vector3.down;

            // Default
            default:
                return Vector3.forward;
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Get mapping information for a specific SMPLX joint
    /// </summary>
    public static JointMapping GetMappingForJoint(int smplxIndex)
    {
        var table = GetMappingTable();
        foreach (var mapping in table)
        {
            if (mapping.smplxIndex == smplxIndex)
                return mapping;
        }

        Debug.LogWarning($"No mapping found for SMPLX joint index {smplxIndex}");
        return default;
    }

    /// <summary>
    /// Check if a joint has a direct MediaPipe correspondence
    /// </summary>
    public static bool HasDirectMapping(int smplxIndex)
    {
        var mapping = GetMappingForJoint(smplxIndex);
        return mapping.mappingType == MappingType.Direct;
    }

    /// <summary>
    /// Get all children of a joint
    /// </summary>
    public static List<int> GetChildJoints(int parentIndex)
    {
        var children = new List<int>();
        var parents = GetParentIndices();

        for (int i = 0; i < parents.Length; i++)
        {
            if (parents[i] == parentIndex)
                children.Add(i);
        }

        return children;
    }

    #endregion
}
