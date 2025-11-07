// VerifiedIntegrationExample.cs
// Complete integration example showing how to use all verified components together
// This demonstrates the CORRECT way to map MediaPipe landmarks to SMPLX model

using UnityEngine;
using Mediapipe.Unity;
using Mediapipe.Tasks.Vision.PoseLandmarker;

namespace VerifiedSMPLXMapping
{
    /// <summary>
    /// Complete integration example for MediaPipe to SMPLX mapping
    /// Shows how to wire up all verified components correctly
    /// </summary>
    public class VerifiedIntegrationExample : MonoBehaviour
    {
        [Header("SMPLX Model")]
        [Tooltip("GameObject with SMPLX model (must have Humanoid Animator)")]
        public GameObject smplxModel;

        [Header("MediaPipe Components")]
        [Tooltip("MediaPipe PoseLandmarker runner (from MediaPipe Unity Plugin)")]
        public Mediapipe.Unity.Sample.PoseLandmarker.PoseLandmarkerRunner poseLandmarkerRunner;

        [Header("Verified Components (Auto-created)")]
        private VerifiedMediaPipeMapper mapper;
        private VerifiedSMPLXPoseController poseController;
        private VerifiedPoseLandmarkHandler landmarkHandler;

        [Header("Settings")]
        [Tooltip("Initialize pose controller on start")]
        public bool initializeOnStart = true;

        [Tooltip("Kalman filter Q parameter (process noise)")]
        public float kalmanQ = 0.001f;

        [Tooltip("Kalman filter R parameter (measurement noise)")]
        public float kalmanR = 0.0015f;

        private void Start()
        {
            SetupComponents();

            if (initializeOnStart)
            {
                InitializePoseSystem();
            }
        }

        /// <summary>
        /// Setup all verified components
        /// </summary>
        private void SetupComponents()
        {
            if (smplxModel == null)
            {
                Debug.LogError("SMPLX model reference is required!");
                enabled = false;
                return;
            }

            // Verify SMPLX model has Humanoid Animator
            Animator animator = smplxModel.GetComponent<Animator>();
            if (animator == null || !animator.isHuman)
            {
                Debug.LogError("SMPLX model must have a Humanoid Animator component!");
                enabled = false;
                return;
            }

            // Add VerifiedMediaPipeMapper to SMPLX model
            mapper = smplxModel.GetComponent<VerifiedMediaPipeMapper>();
            if (mapper == null)
            {
                mapper = smplxModel.AddComponent<VerifiedMediaPipeMapper>();
                Debug.Log("Added VerifiedMediaPipeMapper to SMPLX model");
            }

            // Configure Kalman filter parameters
            mapper.KalmanParamQ = kalmanQ;
            mapper.KalmanParamR = kalmanR;

            // Add VerifiedSMPLXPoseController to SMPLX model
            poseController = smplxModel.GetComponent<VerifiedSMPLXPoseController>();
            if (poseController == null)
            {
                poseController = smplxModel.AddComponent<VerifiedSMPLXPoseController>();
                Debug.Log("Added VerifiedSMPLXPoseController to SMPLX model");
            }

            // Link mapper to pose controller
            poseController.mapper = mapper;

            // Add VerifiedPoseLandmarkHandler to this GameObject
            landmarkHandler = GetComponent<VerifiedPoseLandmarkHandler>();
            if (landmarkHandler == null)
            {
                landmarkHandler = gameObject.AddComponent<VerifiedPoseLandmarkHandler>();
                Debug.Log("Added VerifiedPoseLandmarkHandler");
            }

            // Link components to handler
            landmarkHandler.mapper = mapper;
            landmarkHandler.poseController = poseController;

            // Subscribe to MediaPipe events if runner is available
            if (poseLandmarkerRunner != null)
            {
                SubscribeToMediaPipeEvents();
            }
            else
            {
                Debug.LogWarning("PoseLandmarkerRunner not assigned. You'll need to manually call ProcessLandmarks()");
            }

            Debug.Log("All verified components setup successfully");
        }

        /// <summary>
        /// Subscribe to MediaPipe PoseLandmarker events
        /// </summary>
        private void SubscribeToMediaPipeEvents()
        {
            // Note: The actual event subscription depends on the MediaPipe Unity Plugin version
            // You may need to adapt this based on your plugin's API
            //
            // Typical pattern (adapt as needed):
            // poseLandmarkerRunner.OnPoseLandmarkerResultReceived += OnPoseLandmarkerResult;

            Debug.Log("Subscribe to MediaPipe events in your actual implementation");
        }

        /// <summary>
        /// Initialize the pose system (must be called in T-pose)
        /// IMPORTANT: SMPLX model should be in T-pose when this is called!
        /// </summary>
        public void InitializePoseSystem()
        {
            if (poseController == null)
            {
                Debug.LogError("Pose controller not setup!");
                return;
            }

            Debug.Log("Initializing pose system (ensure SMPLX model is in T-pose)...");
            poseController.Initialize();
        }

        /// <summary>
        /// Callback for MediaPipe PoseLandmarker results
        /// Call this from your MediaPipe integration
        /// </summary>
        public void OnPoseLandmarkerResult(PoseLandmarkerResult result)
        {
            if (landmarkHandler != null)
            {
                landmarkHandler.ProcessResult(result);
            }
        }

        /// <summary>
        /// Manual processing method for testing
        /// </summary>
        public void ProcessLandmarks(PoseLandmarkerResult result)
        {
            if (result == null)
            {
                Debug.LogWarning("Received null result");
                return;
            }

            OnPoseLandmarkerResult(result);
        }

        /// <summary>
        /// Reset everything
        /// </summary>
        public void Reset()
        {
            landmarkHandler?.Reset();
            Debug.Log("System reset");
        }

        /// <summary>
        /// Example: Get current joint position
        /// </summary>
        public Vector3 GetJointPosition(VerifiedMediaPipeMapper.PositionIndex index)
        {
            if (mapper == null)
            {
                Debug.LogWarning("Mapper not initialized");
                return Vector3.zero;
            }

            return mapper.GetJointPosition(index);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor helper: Initialize in editor
        /// </summary>
        [ContextMenu("Initialize Pose System")]
        private void EditorInitialize()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode first!");
                return;
            }

            InitializePoseSystem();
        }

        /// <summary>
        /// Editor helper: Reset system
        /// </summary>
        [ContextMenu("Reset System")]
        private void EditorReset()
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Enter Play mode first!");
                return;
            }

            Reset();
        }

        /// <summary>
        /// Editor helper: Print joint positions
        /// </summary>
        [ContextMenu("Debug: Print Joint Positions")]
        private void DebugPrintJointPositions()
        {
            if (!Application.isPlaying || mapper == null)
            {
                Debug.LogWarning("System not running!");
                return;
            }

            Debug.Log("=== Current Joint Positions ===");
            for (int i = 0; i < (int)VerifiedMediaPipeMapper.PositionIndex.Count; i++)
            {
                var index = (VerifiedMediaPipeMapper.PositionIndex)i;
                var pos = mapper.GetJointPosition(index);
                Debug.Log($"{index}: {pos}");
            }
        }
#endif
    }
}
