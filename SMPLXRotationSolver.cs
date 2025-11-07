using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Solves joint rotations from joint positions for SMPLX model.
/// Converts position data to rotation data suitable for skeletal animation.
/// </summary>
public class SMPLXRotationSolver
{
    #region Configuration

    [System.Serializable]
    public class SolverSettings
    {
        [Tooltip("Smoothing factor for rotation interpolation (0 = no smoothing, 1 = maximum smoothing)")]
        [Range(0f, 1f)]
        public float rotationSmoothingFactor = 0.3f;

        [Tooltip("Apply anatomical joint constraints")]
        public bool applyJointLimits = true;

        [Tooltip("Use IK for limbs (provides better end-effector positioning)")]
        public bool useIKForLimbs = false;

        [Tooltip("Minimum angle change (degrees) to trigger rotation update")]
        public float minAngleThreshold = 0.5f;
    }

    #endregion

    #region Fields

    private SolverSettings settings;
    private Quaternion[] currentRotations;
    private Quaternion[] previousRotations;
    private Vector3[] tPoseDirections;
    private int[] parentIndices;

    #endregion

    #region Initialization

    public SMPLXRotationSolver(SolverSettings settings = null)
    {
        this.settings = settings ?? new SolverSettings();
        this.currentRotations = new Quaternion[55];
        this.previousRotations = new Quaternion[55];
        this.tPoseDirections = new Vector3[55];
        this.parentIndices = MediaPipeToSMPLXMapper.GetParentIndices();

        // Initialize rotations to identity
        for (int i = 0; i < 55; i++)
        {
            currentRotations[i] = Quaternion.identity;
            previousRotations[i] = Quaternion.identity;
            tPoseDirections[i] = MediaPipeToSMPLXMapper.GetTPoseBoneDirection(i);
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Solve rotations for all joints from their world positions
    /// </summary>
    /// <param name="jointPositions">World positions of all 55 SMPLX joints</param>
    /// <param name="rootTransform">Root transform of the SMPLX character</param>
    /// <returns>Local rotations for each joint</returns>
    public Quaternion[] SolveRotations(Vector3[] jointPositions, Transform rootTransform = null)
    {
        if (jointPositions == null || jointPositions.Length != 55)
        {
            Debug.LogError("Invalid joint positions. Expected 55 positions.");
            return currentRotations;
        }

        // Solve rotations in hierarchical order (parent before child)
        for (int i = 0; i < 55; i++)
        {
            Quaternion rotation = CalculateJointRotation(i, jointPositions);

            // Apply smoothing
            rotation = SmoothRotation(i, rotation);

            // Apply joint limits if enabled
            if (settings.applyJointLimits)
            {
                rotation = ApplyJointLimits(i, rotation);
            }

            currentRotations[i] = rotation;
        }

        // Update previous rotations for next frame
        System.Array.Copy(currentRotations, previousRotations, 55);

        return currentRotations;
    }

    /// <summary>
    /// Calculate rotation for a single joint
    /// </summary>
    private Quaternion CalculateJointRotation(int jointIndex, Vector3[] jointPositions)
    {
        // Get children of this joint
        var children = MediaPipeToSMPLXMapper.GetChildJoints(jointIndex);

        if (children.Count == 0)
        {
            // Leaf joint - maintain previous rotation or use parent's
            return previousRotations[jointIndex];
        }

        // Calculate rotation based on primary child
        int primaryChildIndex = GetPrimaryChild(jointIndex, children);

        if (primaryChildIndex < 0)
            return Quaternion.identity;

        // Calculate direction to child
        Vector3 currentDirection = (jointPositions[primaryChildIndex] - jointPositions[jointIndex]).normalized;

        // Get T-pose direction
        Vector3 tPoseDirection = tPoseDirections[jointIndex];

        // Calculate rotation from T-pose to current direction
        Quaternion worldRotation = Quaternion.FromToRotation(tPoseDirection, currentDirection);

        // Convert to local rotation
        Quaternion localRotation = ConvertToLocalRotation(jointIndex, worldRotation, jointPositions);

        // Handle special cases for specific joints
        localRotation = ApplyJointSpecificRotation(jointIndex, localRotation, jointPositions);

        return localRotation;
    }

    /// <summary>
    /// Get the primary child for rotation calculation
    /// </summary>
    private int GetPrimaryChild(int parentIndex, List<int> children)
    {
        if (children.Count == 0)
            return -1;

        // For joints with multiple children, select the primary one
        // (usually the one continuing the chain)

        switch (parentIndex)
        {
            case MediaPipeToSMPLXMapper.SMPLX_PELVIS:
                // Use spine1
                return MediaPipeToSMPLXMapper.SMPLX_SPINE1;

            case MediaPipeToSMPLXMapper.SMPLX_SPINE1:
                return MediaPipeToSMPLXMapper.SMPLX_SPINE2;

            case MediaPipeToSMPLXMapper.SMPLX_SPINE2:
                return MediaPipeToSMPLXMapper.SMPLX_SPINE3;

            case MediaPipeToSMPLXMapper.SMPLX_SPINE3:
                return MediaPipeToSMPLXMapper.SMPLX_NECK;

            default:
                // Return first child
                return children.Count > 0 ? children[0] : -1;
        }
    }

    /// <summary>
    /// Convert world rotation to local rotation relative to parent
    /// </summary>
    private Quaternion ConvertToLocalRotation(int jointIndex, Quaternion worldRotation, Vector3[] jointPositions)
    {
        int parentIndex = parentIndices[jointIndex];

        if (parentIndex < 0)
        {
            // Root joint - world rotation is local rotation
            return worldRotation;
        }

        // Get parent's world rotation
        Quaternion parentWorldRotation = GetWorldRotation(parentIndex, jointPositions);

        // Convert to local space
        Quaternion localRotation = Quaternion.Inverse(parentWorldRotation) * worldRotation;

        return localRotation;
    }

    /// <summary>
    /// Calculate accumulated world rotation for a joint
    /// </summary>
    private Quaternion GetWorldRotation(int jointIndex, Vector3[] jointPositions)
    {
        if (jointIndex < 0)
            return Quaternion.identity;

        Quaternion rotation = currentRotations[jointIndex];
        int parentIndex = parentIndices[jointIndex];

        if (parentIndex >= 0)
        {
            rotation = GetWorldRotation(parentIndex, jointPositions) * rotation;
        }

        return rotation;
    }

    /// <summary>
    /// Apply joint-specific rotation adjustments
    /// </summary>
    private Quaternion ApplyJointSpecificRotation(int jointIndex, Quaternion rotation, Vector3[] jointPositions)
    {
        switch (jointIndex)
        {
            // Elbow joints - calculate twist
            case MediaPipeToSMPLXMapper.SMPLX_LEFT_ELBOW:
            case MediaPipeToSMPLXMapper.SMPLX_RIGHT_ELBOW:
                return CalculateElbowRotation(jointIndex, rotation, jointPositions);

            // Knee joints - limit to single axis
            case MediaPipeToSMPLXMapper.SMPLX_LEFT_KNEE:
            case MediaPipeToSMPLXMapper.SMPLX_RIGHT_KNEE:
                return CalculateKneeRotation(jointIndex, rotation, jointPositions);

            // Spine joints - distribute rotation
            case MediaPipeToSMPLXMapper.SMPLX_SPINE1:
            case MediaPipeToSMPLXMapper.SMPLX_SPINE2:
            case MediaPipeToSMPLXMapper.SMPLX_SPINE3:
                return CalculateSpineRotation(jointIndex, rotation, jointPositions);

            default:
                return rotation;
        }
    }

    /// <summary>
    /// Calculate elbow rotation with proper twist
    /// </summary>
    private Quaternion CalculateElbowRotation(int elbowIndex, Quaternion rotation, Vector3[] jointPositions)
    {
        bool isLeft = elbowIndex == MediaPipeToSMPLXMapper.SMPLX_LEFT_ELBOW;

        int shoulderIndex = isLeft ? MediaPipeToSMPLXMapper.SMPLX_LEFT_SHOULDER : MediaPipeToSMPLXMapper.SMPLX_RIGHT_SHOULDER;
        int wristIndex = isLeft ? MediaPipeToSMPLXMapper.SMPLX_LEFT_WRIST : MediaPipeToSMPLXMapper.SMPLX_RIGHT_WRIST;

        Vector3 shoulderPos = jointPositions[shoulderIndex];
        Vector3 elbowPos = jointPositions[elbowIndex];
        Vector3 wristPos = jointPositions[wristIndex];

        // Calculate upper arm direction
        Vector3 upperArmDir = (elbowPos - shoulderPos).normalized;

        // Calculate lower arm direction
        Vector3 lowerArmDir = (wristPos - elbowPos).normalized;

        // Calculate bend angle
        float bendAngle = Vector3.Angle(upperArmDir, lowerArmDir);

        // Elbow can only bend in one direction (hinge joint)
        Vector3 bendAxis = Vector3.Cross(upperArmDir, lowerArmDir).normalized;

        return Quaternion.AngleAxis(bendAngle, bendAxis);
    }

    /// <summary>
    /// Calculate knee rotation (hinge joint)
    /// </summary>
    private Quaternion CalculateKneeRotation(int kneeIndex, Quaternion rotation, Vector3[] jointPositions)
    {
        bool isLeft = kneeIndex == MediaPipeToSMPLXMapper.SMPLX_LEFT_KNEE;

        int hipIndex = isLeft ? MediaPipeToSMPLXMapper.SMPLX_LEFT_HIP : MediaPipeToSMPLXMapper.SMPLX_RIGHT_HIP;
        int ankleIndex = isLeft ? MediaPipeToSMPLXMapper.SMPLX_LEFT_ANKLE : MediaPipeToSMPLXMapper.SMPLX_RIGHT_ANKLE;

        Vector3 hipPos = jointPositions[hipIndex];
        Vector3 kneePos = jointPositions[kneeIndex];
        Vector3 anklePos = jointPositions[ankleIndex];

        // Calculate thigh direction
        Vector3 thighDir = (kneePos - hipPos).normalized;

        // Calculate shin direction
        Vector3 shinDir = (anklePos - kneePos).normalized;

        // Calculate bend angle (knee only bends forward)
        float bendAngle = Vector3.Angle(thighDir, shinDir);

        // Limit to anatomically correct range (0-150 degrees)
        bendAngle = Mathf.Clamp(bendAngle, 0f, 150f);

        // Knee bends around lateral axis
        Vector3 bendAxis = Vector3.Cross(thighDir, shinDir).normalized;

        return Quaternion.AngleAxis(bendAngle, bendAxis);
    }

    /// <summary>
    /// Calculate spine rotation with distributed bending
    /// </summary>
    private Quaternion CalculateSpineRotation(int spineIndex, Quaternion rotation, Vector3[] jointPositions)
    {
        // Get pelvis and shoulder positions
        Vector3 pelvisPos = jointPositions[MediaPipeToSMPLXMapper.SMPLX_PELVIS];
        Vector3 spine3Pos = jointPositions[MediaPipeToSMPLXMapper.SMPLX_SPINE3];

        // Calculate overall spine direction
        Vector3 spineDir = (spine3Pos - pelvisPos).normalized;

        // Calculate rotation from T-pose (upward) to current direction
        Quaternion totalSpineRotation = Quaternion.FromToRotation(Vector3.up, spineDir);

        // Distribute rotation across spine joints (33% each for 3 joints)
        float distribution = 0.33f;

        // Apply partial rotation based on which spine joint this is
        float t = distribution;
        if (spineIndex == MediaPipeToSMPLXMapper.SMPLX_SPINE2)
            t = distribution * 2f;
        else if (spineIndex == MediaPipeToSMPLXMapper.SMPLX_SPINE3)
            t = distribution * 3f;

        return Quaternion.Slerp(Quaternion.identity, totalSpineRotation, t);
    }

    /// <summary>
    /// Smooth rotation to reduce jitter
    /// </summary>
    private Quaternion SmoothRotation(int jointIndex, Quaternion targetRotation)
    {
        Quaternion previousRotation = previousRotations[jointIndex];

        // Check if angle change is significant
        float angleDiff = Quaternion.Angle(previousRotation, targetRotation);

        if (angleDiff < settings.minAngleThreshold)
        {
            // Change too small, keep previous rotation
            return previousRotation;
        }

        // Spherical interpolation for smooth transition
        return Quaternion.Slerp(previousRotation, targetRotation, 1f - settings.rotationSmoothingFactor);
    }

    /// <summary>
    /// Apply anatomical joint limits
    /// </summary>
    private Quaternion ApplyJointLimits(int jointIndex, Quaternion rotation)
    {
        // Define joint limits (simplified - could be expanded)
        Vector3 euler = rotation.eulerAngles;

        switch (jointIndex)
        {
            case MediaPipeToSMPLXMapper.SMPLX_LEFT_ELBOW:
            case MediaPipeToSMPLXMapper.SMPLX_RIGHT_ELBOW:
                // Elbow: 0-150 degrees bend
                euler.x = Mathf.Clamp(euler.x, 0f, 150f);
                euler.y = 0f; // No lateral movement
                euler.z = Mathf.Clamp(euler.z, -45f, 45f); // Limited twist
                break;

            case MediaPipeToSMPLXMapper.SMPLX_LEFT_KNEE:
            case MediaPipeToSMPLXMapper.SMPLX_RIGHT_KNEE:
                // Knee: 0-150 degrees bend
                euler.x = Mathf.Clamp(euler.x, 0f, 150f);
                euler.y = 0f;
                euler.z = 0f;
                break;

            case MediaPipeToSMPLXMapper.SMPLX_HEAD:
                // Head: limited rotation ranges
                euler.x = Mathf.Clamp(euler.x, -60f, 60f); // Pitch
                euler.y = Mathf.Clamp(euler.y, -80f, 80f); // Yaw
                euler.z = Mathf.Clamp(euler.z, -45f, 45f); // Roll
                break;

            // Add more joint limits as needed
        }

        return Quaternion.Euler(euler);
    }

    #endregion

    #region IK Methods

    /// <summary>
    /// Two-bone IK solver for limbs (e.g., shoulder-elbow-wrist)
    /// </summary>
    public void SolveTwoBoneIK(
        int joint1Index,
        int joint2Index,
        int joint3Index,
        Vector3 targetPosition,
        Vector3[] jointPositions)
    {
        Vector3 pos1 = jointPositions[joint1Index];
        Vector3 pos2 = jointPositions[joint2Index];
        Vector3 pos3 = jointPositions[joint3Index];

        float length1 = Vector3.Distance(pos1, pos2);
        float length2 = Vector3.Distance(pos2, pos3);
        float targetDistance = Vector3.Distance(pos1, targetPosition);

        // Clamp target distance to reachable range
        float maxReach = length1 + length2;
        float minReach = Mathf.Abs(length1 - length2);
        targetDistance = Mathf.Clamp(targetDistance, minReach, maxReach);

        // Calculate angles using law of cosines
        float angle1 = Mathf.Acos(
            (length1 * length1 + targetDistance * targetDistance - length2 * length2) /
            (2f * length1 * targetDistance)
        );

        float angle2 = Mathf.Acos(
            (length1 * length1 + length2 * length2 - targetDistance * targetDistance) /
            (2f * length1 * length2)
        );

        // Calculate directions
        Vector3 dirToTarget = (targetPosition - pos1).normalized;

        // Apply rotations (simplified - full IK would require more computation)
        Vector3 joint2Direction = Quaternion.AngleAxis(angle1 * Mathf.Rad2Deg, Vector3.forward) * dirToTarget;
        jointPositions[joint2Index] = pos1 + joint2Direction * length1;
        jointPositions[joint3Index] = jointPositions[joint2Index] +
                                       (targetPosition - jointPositions[joint2Index]).normalized * length2;
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Get rotation for a specific joint
    /// </summary>
    public Quaternion GetJointRotation(int jointIndex)
    {
        if (jointIndex < 0 || jointIndex >= 55)
            return Quaternion.identity;

        return currentRotations[jointIndex];
    }

    /// <summary>
    /// Get all joint rotations
    /// </summary>
    public Quaternion[] GetAllRotations()
    {
        return currentRotations;
    }

    /// <summary>
    /// Reset all rotations to identity
    /// </summary>
    public void Reset()
    {
        for (int i = 0; i < 55; i++)
        {
            currentRotations[i] = Quaternion.identity;
            previousRotations[i] = Quaternion.identity;
        }
    }

    #endregion
}
