// VerifiedSMPLXPoseController.cs
// Adapted from RealTimeSMPLX.cs (lines 1-289)
// Implements VERIFIED Unity Humanoid rig control with inverse rotations

using UnityEngine;

namespace VerifiedSMPLXMapping
{
    /// <summary>
    /// Applies pose to SMPLX model using Unity Humanoid rig
    /// VERIFIED pattern from RealTimeSMPLX.cs
    /// Uses Animator.GetBoneTransform() and inverse rotation calculations
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class VerifiedSMPLXPoseController : MonoBehaviour
    {
        [Header("Components")]
        [Tooltip("Reference to the verified mapper component")]
        public VerifiedMediaPipeMapper mapper;

        [Header("Pose Settings")]
        [Tooltip("Forward direction for rotation calculations (default: Vector3.forward)")]
        public Vector3 forward = Vector3.forward;

        [Tooltip("Initialize inverse rotations on start")]
        public bool initializeOnStart = true;

        private Animator anim;
        private VerifiedMediaPipeMapper.JointPoint[] jointPoints;
        private bool isInitialized = false;

        // Bone mapping from PositionIndex to Unity Humanoid bones
        // VERIFIED from RealTimeSMPLX.cs lines 125-164
        private struct BoneMapping
        {
            public VerifiedMediaPipeMapper.PositionIndex positionIndex;
            public HumanBodyBones humanBone;
            public VerifiedMediaPipeMapper.PositionIndex childPositionIndex;

            public BoneMapping(VerifiedMediaPipeMapper.PositionIndex pos, HumanBodyBones bone,
                             VerifiedMediaPipeMapper.PositionIndex child)
            {
                positionIndex = pos;
                humanBone = bone;
                childPositionIndex = child;
            }
        }

        private BoneMapping[] boneMappings;

        private void Awake()
        {
            anim = GetComponent<Animator>();

            if (anim == null)
            {
                Debug.LogError("VerifiedSMPLXPoseController requires an Animator component!");
                enabled = false;
                return;
            }

            if (!anim.isHuman)
            {
                Debug.LogError("Animator must be configured as Humanoid rig!");
                enabled = false;
                return;
            }

            InitializeBoneMappings();
        }

        private void Start()
        {
            if (mapper == null)
            {
                Debug.LogError("VerifiedMediaPipeMapper reference is required!");
                enabled = false;
                return;
            }

            if (initializeOnStart)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initialize bone mappings
        /// VERIFIED from RealTimeSMPLX.cs lines 125-164
        /// Maps 36 position indices to ~22 Unity Humanoid bones
        /// </summary>
        private void InitializeBoneMappings()
        {
            // Define bone mappings with parent->child relationships
            // Only body bones are mapped (hands stay in default pose)
            boneMappings = new BoneMapping[]
            {
                // Head and neck
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.head, HumanBodyBones.Head,
                              VerifiedMediaPipeMapper.PositionIndex.neck),
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.neck, HumanBodyBones.Neck,
                              VerifiedMediaPipeMapper.PositionIndex.spine),

                // Spine
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.spine, HumanBodyBones.Spine,
                              VerifiedMediaPipeMapper.PositionIndex.hip),
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.hip, HumanBodyBones.Hips,
                              VerifiedMediaPipeMapper.PositionIndex.hip), // Hips have no parent in this system

                // Right arm
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.right_shoulder, HumanBodyBones.RightUpperArm,
                              VerifiedMediaPipeMapper.PositionIndex.right_elbow),
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.right_elbow, HumanBodyBones.RightLowerArm,
                              VerifiedMediaPipeMapper.PositionIndex.right_wrist),
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.right_wrist, HumanBodyBones.RightHand,
                              VerifiedMediaPipeMapper.PositionIndex.right_index),

                // Left arm
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.left_shoulder, HumanBodyBones.LeftUpperArm,
                              VerifiedMediaPipeMapper.PositionIndex.left_elbow),
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.left_elbow, HumanBodyBones.LeftLowerArm,
                              VerifiedMediaPipeMapper.PositionIndex.left_wrist),
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.left_wrist, HumanBodyBones.LeftHand,
                              VerifiedMediaPipeMapper.PositionIndex.left_index),

                // Right leg
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.right_hip, HumanBodyBones.RightUpperLeg,
                              VerifiedMediaPipeMapper.PositionIndex.right_knee),
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.right_knee, HumanBodyBones.RightLowerLeg,
                              VerifiedMediaPipeMapper.PositionIndex.right_ankle),
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.right_ankle, HumanBodyBones.RightFoot,
                              VerifiedMediaPipeMapper.PositionIndex.right_foot_index),

                // Left leg
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.left_hip, HumanBodyBones.LeftUpperLeg,
                              VerifiedMediaPipeMapper.PositionIndex.left_knee),
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.left_knee, HumanBodyBones.LeftLowerLeg,
                              VerifiedMediaPipeMapper.PositionIndex.left_ankle),
                new BoneMapping(VerifiedMediaPipeMapper.PositionIndex.left_ankle, HumanBodyBones.LeftFoot,
                              VerifiedMediaPipeMapper.PositionIndex.left_foot_index),
            };
        }

        /// <summary>
        /// Initialize the pose system
        /// VERIFIED from RealTimeSMPLX.cs lines 120-235
        /// Gets bone transforms from Humanoid rig and calculates inverse rotations in T-pose
        /// </summary>
        public void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("Already initialized");
                return;
            }

            jointPoints = mapper.GetAllJointPoints();

            if (jointPoints == null || jointPoints.Length != (int)VerifiedMediaPipeMapper.PositionIndex.Count)
            {
                Debug.LogError("Invalid joint points from mapper!");
                return;
            }

            // Get bone transforms from Animator
            // VERIFIED from RealTimeSMPLX.cs lines 125-164
            foreach (var mapping in boneMappings)
            {
                Transform boneTransform = anim.GetBoneTransform(mapping.humanBone);

                if (boneTransform == null)
                {
                    Debug.LogWarning($"Bone {mapping.humanBone} not found in Humanoid rig");
                    continue;
                }

                var jointPoint = jointPoints[(int)mapping.positionIndex];
                jointPoint.Transform = boneTransform;
                jointPoint.DefaultPoseRotation = boneTransform.rotation;

                // Set child relationship
                if (mapping.childPositionIndex != mapping.positionIndex)
                {
                    jointPoint.Child = jointPoints[(int)mapping.childPositionIndex];
                }
            }

            // Initialize positions from T-pose
            // VERIFIED from RealTimeSMPLX.cs lines 170-188
            foreach (var mapping in boneMappings)
            {
                var jointPoint = jointPoints[(int)mapping.positionIndex];

                if (jointPoint.Transform != null)
                {
                    jointPoint.Pos3D = jointPoint.Transform.position;
                    jointPoint.Now3D = jointPoint.Transform.position;
                }
            }

            // Calculate inverse rotations in T-pose
            // VERIFIED from RealTimeSMPLX.cs lines 196-209
            CalculateInverseRotations();

            isInitialized = true;
            Debug.Log("VerifiedSMPLXPoseController initialized successfully");
        }

        /// <summary>
        /// Calculate inverse rotations for all bones
        /// VERIFIED from RealTimeSMPLX.cs lines 196-209
        /// Must be called in T-pose to get correct inverse transforms
        /// </summary>
        private void CalculateInverseRotations()
        {
            foreach (var mapping in boneMappings)
            {
                var jointPoint = jointPoints[(int)mapping.positionIndex];

                if (jointPoint.Child != null && jointPoint.Transform != null && jointPoint.Child.Transform != null)
                {
                    // Calculate inverse using GetInverse formula from line 284-287
                    jointPoint.Inverse = GetInverse(jointPoint, jointPoint.Child, forward);
                    jointPoint.InverseRotation = jointPoint.Inverse * jointPoint.DefaultPoseRotation;
                }
            }
        }

        /// <summary>
        /// Calculate inverse rotation for a parent-child bone pair
        /// VERIFIED from RealTimeSMPLX.cs lines 284-287
        /// </summary>
        private Quaternion GetInverse(VerifiedMediaPipeMapper.JointPoint parent,
                                      VerifiedMediaPipeMapper.JointPoint child,
                                      Vector3 forwardDir)
        {
            // Direction from child to parent in T-pose
            Vector3 direction = parent.Transform.position - child.Transform.position;

            return Quaternion.Inverse(Quaternion.LookRotation(direction, forwardDir));
        }

        /// <summary>
        /// Update pose from tracked positions
        /// VERIFIED from RealTimeSMPLX.cs lines 237-271
        /// Should be called every frame after mapper processes new landmarks
        /// </summary>
        public void UpdatePose()
        {
            if (!isInitialized)
            {
                Debug.LogWarning("Not initialized! Call Initialize() first");
                return;
            }

            // Apply rotations to all bones
            // VERIFIED from RealTimeSMPLX.cs lines 249-255
            foreach (var mapping in boneMappings)
            {
                var jointPoint = jointPoints[(int)mapping.positionIndex];

                if (jointPoint.Child != null && jointPoint.Transform != null)
                {
                    // Calculate direction from current positions
                    Vector3 direction = jointPoint.Pos3D - jointPoint.Child.Pos3D;

                    // Apply rotation using LookRotation * InverseRotation pattern
                    // VERIFIED from line 252-254
                    jointPoint.Transform.rotation =
                        Quaternion.LookRotation(direction, forward) * jointPoint.InverseRotation;
                }
            }
        }

        private void LateUpdate()
        {
            if (isInitialized)
            {
                UpdatePose();
            }
        }

        /// <summary>
        /// Reset to T-pose
        /// </summary>
        public void ResetToTPose()
        {
            if (!isInitialized) return;

            foreach (var mapping in boneMappings)
            {
                var jointPoint = jointPoints[(int)mapping.positionIndex];

                if (jointPoint.Transform != null)
                {
                    jointPoint.Transform.rotation = jointPoint.DefaultPoseRotation;
                }
            }
        }

        /// <summary>
        /// Recalculate inverse rotations (call if you manually adjust T-pose)
        /// </summary>
        public void RecalculateInverses()
        {
            if (!isInitialized) return;
            CalculateInverseRotations();
        }

        private void OnValidate()
        {
            if (mapper == null)
            {
                mapper = GetComponent<VerifiedMediaPipeMapper>();
            }
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!isInitialized || jointPoints == null) return;

            // Draw skeleton connections
            Gizmos.color = Color.green;

            foreach (var mapping in boneMappings)
            {
                var jointPoint = jointPoints[(int)mapping.positionIndex];

                if (jointPoint.Child != null)
                {
                    Gizmos.DrawLine(jointPoint.Pos3D, jointPoint.Child.Pos3D);
                    Gizmos.DrawSphere(jointPoint.Pos3D, 0.01f);
                }
            }
        }
#endif
    }
}
