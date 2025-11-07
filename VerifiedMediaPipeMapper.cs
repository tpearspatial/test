// VerifiedMediaPipeMapper.cs
// Adapted from MedPipe2HumanoidAvatar.cs (lines 1-100)
// Implements the VERIFIED reordering algorithm and Kalman filtering from actual working code

using System.Collections.Generic;
using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;

namespace VerifiedSMPLXMapping
{
    /// <summary>
    /// Maps MediaPipe pose landmarks (33 points) to SMPLX-compatible positions (36 points)
    /// using the verified reordering algorithm from MedPipe2HumanoidAvatar.cs lines 49-53
    /// </summary>
    public class VerifiedMediaPipeMapper : MonoBehaviour
    {
        // VERIFIED: 36 position indices (33 MediaPipe + 3 computed)
        // Source: MedPipe2HumanoidAvatar.cs lines 7-10 and RealTimeSMPLX.cs lines 9-55
        public enum PositionIndex : int
        {
            nose = 0,
            left_eye_inner, left_eye, left_eye_outer,
            right_eye_inner, right_eye, right_eye_outer,
            left_ear, right_ear,
            mouth_left, mouth_right,
            left_shoulder, right_shoulder,
            left_elbow, right_elbow,
            left_wrist, right_wrist,
            left_pinky, right_pinky,
            left_index, right_index,
            left_thumb, right_thumb,
            left_hip, right_hip,
            left_knee, right_knee,
            left_ankle, right_ankle,
            left_heel, right_heel,
            left_foot_index, right_foot_index,

            // Computed joints (from lines 58-64 of MedPipe2HumanoidAvatar.cs)
            head = 33,      // Interpolate ears
            hip = 34,       // Interpolate hips
            spine = 35,     // Interpolate hips and shoulders
            Count = 36
        }

        [Header("Kalman Filter Parameters")]
        [Tooltip("Process noise (verified from MedPipe2HumanoidAvatar.cs line 18)")]
        public float KalmanParamQ = 0.001f;

        [Tooltip("Measurement noise (verified from MedPipe2HumanoidAvatar.cs line 19)")]
        public float KalmanParamR = 0.0015f;

        [Header("Coordinate Conversion")]
        [Tooltip("Scale factors for RealWorldToLocalPoint (verified: 50, 50, 50)")]
        public Vector3 scaleFactors = new Vector3(50, 50, 50);

        [Tooltip("Mirror the input landmarks")]
        public bool isMirrored = true;

        // Joint point structure with Kalman state
        // VERIFIED from RealTimeSMPLX.cs lines 67-86
        public class JointPoint
        {
            public Vector3 Pos3D = Vector3.zero;      // Final filtered position
            public Vector3 Now3D = Vector3.zero;      // Current measurement
            public Vector3 P = Vector3.zero;          // Kalman error covariance
            public Vector3 X = Vector3.zero;          // Kalman state estimate
            public Vector3 K = Vector3.zero;          // Kalman gain
            public Transform Transform = null;         // Unity bone transform
            public Quaternion Inverse;                 // T-pose inverse
            public Quaternion InverseRotation;         // Combined inverse
            public Quaternion DefaultPoseRotation = Quaternion.identity;
            public JointPoint Child = null;            // Child joint for direction

            public void Reset()
            {
                Pos3D = Vector3.zero;
                Now3D = Vector3.zero;
                P = Vector3.zero;
                X = Vector3.zero;
                K = Vector3.zero;
            }
        }

        // Array of 36 joint points
        private JointPoint[] jointPoints = new JointPoint[(int)PositionIndex.Count];

        private void Awake()
        {
            InitializeJointPoints();
        }

        private void InitializeJointPoints()
        {
            for (int i = 0; i < (int)PositionIndex.Count; i++)
            {
                jointPoints[i] = new JointPoint();
            }
        }

        /// <summary>
        /// CRITICAL REORDERING ALGORITHM
        /// Verified from MedPipe2HumanoidAvatar.cs lines 49-53
        /// Converts MediaPipe landmark indices to SMPLX-compatible indices
        /// </summary>
        /// <param name="mediaPipeIndex">MediaPipe landmark index (0-32)</param>
        /// <param name="position">3D position from MediaPipe</param>
        private void ApplyReorderedPosition(int mediaPipeIndex, Vector3 position)
        {
            int targetIndex;

            // VERIFIED REORDERING LOGIC from lines 49-53
            if (mediaPipeIndex == 0)
            {
                // Nose stays at index 0
                targetIndex = 0;
            }
            else if (mediaPipeIndex >= 1 && mediaPipeIndex <= 3)
            {
                // Left eye: 1→4, 2→5, 3→6
                targetIndex = mediaPipeIndex + 3;
            }
            else if (mediaPipeIndex >= 4 && mediaPipeIndex <= 6)
            {
                // Right eye: 4→1, 5→2, 6→3
                targetIndex = mediaPipeIndex - 3;
            }
            else if (mediaPipeIndex % 2 == 1)
            {
                // Odd indices shift +1
                targetIndex = mediaPipeIndex + 1;
            }
            else
            {
                // Even indices shift -1
                targetIndex = mediaPipeIndex - 1;
            }

            // Store the measurement
            jointPoints[targetIndex].Now3D = position;
        }

        /// <summary>
        /// Process MediaPipe PoseLandmarkerResult
        /// VERIFIED from MedPipe2HumanoidAvatar.cs lines 24-76
        /// </summary>
        public void ProcessLandmarks(PoseLandmarkerResult result)
        {
            if (result == null || result.poseLandmarks == null || result.poseLandmarks.Count == 0)
            {
                Debug.LogWarning("No pose landmarks detected");
                return;
            }

            var landmarks = result.poseLandmarks[0]; // First detected person

            if (landmarks.Count < 33)
            {
                Debug.LogWarning($"Insufficient landmarks: {landmarks.Count}/33");
                return;
            }

            // Process all 33 MediaPipe landmarks with coordinate conversion
            for (int i = 0; i < 33; i++)
            {
                var landmark = landmarks[i];

                // VERIFIED coordinate conversion from lines 42-45
                Vector3 pos = Mediapipe.Unity.CoordinateSystem.RealWorldCoordinate.RealWorldToLocalPoint(
                    landmark.x,
                    landmark.y,
                    landmark.z,
                    scaleFactors.x,
                    scaleFactors.y,
                    scaleFactors.z,
                    Mediapipe.Unity.RotationAngle.Rotation0,
                    isMirrored: isMirrored
                );

                // Apply the critical reordering algorithm
                ApplyReorderedPosition(i, pos);
            }

            // Compute 3 additional joints (VERIFIED from lines 58-64)
            ComputeAdditionalJoints();

            // Apply Kalman filtering to all joints
            ApplyKalmanFiltering();
        }

        /// <summary>
        /// Compute the 3 additional joints
        /// VERIFIED from MedPipe2HumanoidAvatar.cs lines 58-64
        /// </summary>
        private void ComputeAdditionalJoints()
        {
            // Head: average of ears
            jointPoints[(int)PositionIndex.head].Now3D =
                (jointPoints[(int)PositionIndex.left_ear].Now3D +
                 jointPoints[(int)PositionIndex.right_ear].Now3D) / 2.0f;

            // Hip: average of hips
            jointPoints[(int)PositionIndex.hip].Now3D =
                (jointPoints[(int)PositionIndex.left_hip].Now3D +
                 jointPoints[(int)PositionIndex.right_hip].Now3D) / 2.0f;

            // Spine: weighted average (25% pseudo-neck, 75% hip)
            Vector3 pseudoNeck =
                (jointPoints[(int)PositionIndex.left_shoulder].Now3D +
                 jointPoints[(int)PositionIndex.right_shoulder].Now3D) / 2.0f;

            jointPoints[(int)PositionIndex.spine].Now3D =
                pseudoNeck * 0.25f + jointPoints[(int)PositionIndex.hip].Now3D * 0.75f;
        }

        /// <summary>
        /// Apply Kalman filtering to all joint points
        /// VERIFIED from MedPipe2HumanoidAvatar.cs lines 82-99
        /// </summary>
        private void ApplyKalmanFiltering()
        {
            for (int i = 0; i < (int)PositionIndex.Count; i++)
            {
                KalmanUpdate(jointPoints[i]);
            }
        }

        /// <summary>
        /// Kalman filter update
        /// VERIFIED from MedPipe2HumanoidAvatar.cs lines 82-99
        /// </summary>
        private void KalmanUpdate(JointPoint measurement)
        {
            // Measurement update
            MeasurementUpdate(measurement);

            // State update for each axis
            measurement.Pos3D.x = measurement.X.x + (measurement.Now3D.x - measurement.X.x) * measurement.K.x;
            measurement.Pos3D.y = measurement.X.y + (measurement.Now3D.y - measurement.X.y) * measurement.K.y;
            measurement.Pos3D.z = measurement.X.z + (measurement.Now3D.z - measurement.X.z) * measurement.K.z;

            // Update state estimate
            measurement.X = measurement.Pos3D;
        }

        /// <summary>
        /// Kalman measurement update
        /// VERIFIED from MedPipe2HumanoidAvatar.cs lines 88-99
        /// </summary>
        private void MeasurementUpdate(JointPoint measurement)
        {
            // Update Kalman gain for each axis
            measurement.K.x = (measurement.P.x + KalmanParamQ) / (measurement.P.x + KalmanParamQ + KalmanParamR);
            measurement.K.y = (measurement.P.y + KalmanParamQ) / (measurement.P.y + KalmanParamQ + KalmanParamR);
            measurement.K.z = (measurement.P.z + KalmanParamQ) / (measurement.P.z + KalmanParamQ + KalmanParamR);

            // Update error covariance for each axis
            measurement.P.x = KalmanParamR * (measurement.P.x + KalmanParamQ) / (KalmanParamR + measurement.P.x + KalmanParamQ);
            measurement.P.y = KalmanParamR * (measurement.P.y + KalmanParamQ) / (KalmanParamR + measurement.P.y + KalmanParamQ);
            measurement.P.z = KalmanParamR * (measurement.P.z + KalmanParamQ) / (KalmanParamR + measurement.P.z + KalmanParamQ);
        }

        /// <summary>
        /// Get the filtered position for a specific joint
        /// </summary>
        public Vector3 GetJointPosition(PositionIndex index)
        {
            return jointPoints[(int)index].Pos3D;
        }

        /// <summary>
        /// Get the joint point data structure for a specific joint
        /// </summary>
        public JointPoint GetJointPoint(PositionIndex index)
        {
            return jointPoints[(int)index];
        }

        /// <summary>
        /// Get all joint points
        /// </summary>
        public JointPoint[] GetAllJointPoints()
        {
            return jointPoints;
        }

        /// <summary>
        /// Reset all Kalman filter states
        /// </summary>
        public void ResetFilters()
        {
            foreach (var joint in jointPoints)
            {
                joint.Reset();
            }
        }
    }
}
