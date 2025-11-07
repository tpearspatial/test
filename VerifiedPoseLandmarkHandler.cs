// VerifiedPoseLandmarkHandler.cs
// Adapted from PoseLandmarkerResultAnnotationController.cs (lines 1-62)
// Implements VERIFIED thread-safe result handling pattern from MediaPipe Unity Plugin

using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;

namespace VerifiedSMPLXMapping
{
    /// <summary>
    /// Thread-safe handler for MediaPipe PoseLandmarkerResult
    /// VERIFIED pattern from PoseLandmarkerResultAnnotationController.cs
    /// Provides DrawNow/DrawLater methods and safe result cloning
    /// </summary>
    public class VerifiedPoseLandmarkHandler : MonoBehaviour
    {
        [Header("Component References")]
        [Tooltip("Reference to the verified mapper")]
        public VerifiedMediaPipeMapper mapper;

        [Tooltip("Reference to the verified pose controller")]
        public VerifiedSMPLXPoseController poseController;

        [Header("Threading")]
        [Tooltip("Use thread-safe DrawLater pattern (recommended for real-time tracking)")]
        public bool useThreadSafeLater = true;

        // Thread-safe lock object
        // VERIFIED from PoseLandmarkerResultAnnotationController.cs line 17
        private readonly object _currentTargetLock = new object();

        // Current result being processed
        // VERIFIED from PoseLandmarkerResultAnnotationController.cs line 18
        private PoseLandmarkerResult _currentTarget;

        // Flag indicating data needs to be synced
        private bool isStale = false;

        private void Start()
        {
            ValidateReferences();
        }

        private void ValidateReferences()
        {
            if (mapper == null)
            {
                Debug.LogError("VerifiedMediaPipeMapper reference is required!");
                enabled = false;
                return;
            }

            if (poseController == null)
            {
                Debug.LogError("VerifiedSMPLXPoseController reference is required!");
                enabled = false;
                return;
            }

            Debug.Log("VerifiedPoseLandmarkHandler initialized successfully");
        }

        /// <summary>
        /// Process result immediately (synchronous)
        /// VERIFIED from PoseLandmarkerResultAnnotationController.cs lines 22-32
        /// Use this when you need immediate processing
        /// </summary>
        public void DrawNow(PoseLandmarkerResult target)
        {
            if (target == null)
            {
                Debug.LogWarning("Received null PoseLandmarkerResult");
                return;
            }

            // Safe cloning (VERIFIED from line 24)
            target.CloneTo(ref _currentTarget);

            // Handle segmentation masks if present (VERIFIED from lines 25-30)
            if (_currentTarget.segmentationMasks != null)
            {
                // We don't use segmentation masks for SMPLX pose control
                // But we clear them to match the verified pattern
                _currentTarget.segmentationMasks.Clear();
            }

            // Process immediately
            SyncNow();
        }

        /// <summary>
        /// Queue result for processing (asynchronous, thread-safe)
        /// VERIFIED from PoseLandmarkerResultAnnotationController.cs line 34
        /// Use this for real-time tracking to avoid blocking
        /// </summary>
        public void DrawLater(PoseLandmarkerResult target)
        {
            if (target == null)
            {
                Debug.LogWarning("Received null PoseLandmarkerResult");
                return;
            }

            UpdateCurrentTarget(target);
        }

        /// <summary>
        /// Thread-safe update of current target
        /// VERIFIED from PoseLandmarkerResultAnnotationController.cs lines 38-51
        /// </summary>
        private void UpdateCurrentTarget(PoseLandmarkerResult newTarget)
        {
            lock (_currentTargetLock)
            {
                // Safe cloning (VERIFIED from line 42)
                newTarget.CloneTo(ref _currentTarget);

                // Handle segmentation masks if present (VERIFIED from lines 43-48)
                if (_currentTarget.segmentationMasks != null)
                {
                    // Clear masks (we don't use them for pose control)
                    _currentTarget.segmentationMasks.Clear();
                }

                // Mark as needing sync (VERIFIED from line 49)
                isStale = true;
            }
        }

        /// <summary>
        /// Synchronize landmarks to mapper and pose controller
        /// VERIFIED from PoseLandmarkerResultAnnotationController.cs lines 53-60
        /// </summary>
        private void SyncNow()
        {
            lock (_currentTargetLock)
            {
                isStale = false;

                if (_currentTarget == null || _currentTarget.poseLandmarks == null)
                {
                    return;
                }

                // Process landmarks through mapper
                // This applies the verified reordering and Kalman filtering
                mapper.ProcessLandmarks(_currentTarget);

                // Pose controller will update in its LateUpdate
                // (No explicit call needed - it automatically reads from mapper's jointPoints)
            }
        }

        private void Update()
        {
            // If using DrawLater pattern, sync on main thread when stale
            if (useThreadSafeLater && isStale)
            {
                SyncNow();
            }
        }

        /// <summary>
        /// Public API: Process a new result
        /// Automatically uses DrawNow or DrawLater based on settings
        /// </summary>
        public void ProcessResult(PoseLandmarkerResult result)
        {
            if (useThreadSafeLater)
            {
                DrawLater(result);
            }
            else
            {
                DrawNow(result);
            }
        }

        /// <summary>
        /// Reset the handler
        /// </summary>
        public void Reset()
        {
            lock (_currentTargetLock)
            {
                _currentTarget = null;
                isStale = false;
            }

            mapper?.ResetFilters();
            poseController?.ResetToTPose();
        }

        private void OnValidate()
        {
            if (mapper == null)
            {
                mapper = GetComponent<VerifiedMediaPipeMapper>();
            }

            if (poseController == null)
            {
                poseController = GetComponent<VerifiedSMPLXPoseController>();
            }
        }

        private void OnDisable()
        {
            Reset();
        }
    }
}
