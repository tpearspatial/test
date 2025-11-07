# MediaPipe to SMPLX Joint Mapping Guide

## Executive Summary

This document provides a comprehensive guide for mapping MediaPipe pose landmarks to SMPLX model joints in Unity. The core challenge is that **MediaPipe provides 33 pose landmarks** while **SMPLX has 55 joints (indices 0-54)**, requiring careful mapping, interpolation, and transformation strategies.

---

## 1. MediaPipe Pose Landmark Structure

### 1.1 Overview
MediaPipe Pose Landmarker detects **33 body landmarks** representing key anatomical points. Each landmark provides:
- **Normalized coordinates**: 2D image coordinates normalized to [0, 1]
- **World coordinates**: 3D spatial coordinates in meters
- **Visibility**: Confidence score for landmark visibility

### 1.2 Complete Landmark List (Indices 0-32)

#### Head Region (Indices 0-10)
| Index | Name | Description |
|-------|------|-------------|
| 0 | NOSE | Center of nose |
| 1 | LEFT_EYE_INNER | Inner corner of left eye |
| 2 | LEFT_EYE | Center of left eye |
| 3 | LEFT_EYE_OUTER | Outer corner of left eye |
| 4 | RIGHT_EYE_INNER | Inner corner of right eye |
| 5 | RIGHT_EYE | Center of right eye |
| 6 | RIGHT_EYE_OUTER | Outer corner of right eye |
| 7 | LEFT_EAR | Left ear |
| 8 | RIGHT_EAR | Right ear |
| 9 | MOUTH_LEFT | Left corner of mouth |
| 10 | MOUTH_RIGHT | Right corner of mouth |

#### Upper Body (Indices 11-22)
| Index | Name | Description |
|-------|------|-------------|
| 11 | LEFT_SHOULDER | Left shoulder |
| 12 | RIGHT_SHOULDER | Right shoulder |
| 13 | LEFT_ELBOW | Left elbow |
| 14 | RIGHT_ELBOW | Right elbow |
| 15 | LEFT_WRIST | Left wrist |
| 16 | RIGHT_WRIST | Right wrist |
| 17 | LEFT_PINKY | Left pinky finger base |
| 18 | RIGHT_PINKY | Right pinky finger base |
| 19 | LEFT_INDEX | Left index finger base |
| 20 | RIGHT_INDEX | Right index finger base |
| 21 | LEFT_THUMB | Left thumb base |
| 22 | RIGHT_THUMB | Right thumb base |

#### Lower Body (Indices 23-32)
| Index | Name | Description |
|-------|------|-------------|
| 23 | LEFT_HIP | Left hip |
| 24 | RIGHT_HIP | Right hip |
| 25 | LEFT_KNEE | Left knee |
| 26 | RIGHT_KNEE | Right knee |
| 27 | LEFT_ANKLE | Left ankle |
| 28 | RIGHT_ANKLE | Right ankle |
| 29 | LEFT_HEEL | Left heel |
| 30 | RIGHT_HEEL | Right heel |
| 31 | LEFT_FOOT_INDEX | Left foot index (ball of foot) |
| 32 | RIGHT_FOOT_INDEX | Right foot index (ball of foot) |

### 1.3 Coordinate Systems

#### Normalized Landmarks
- **Range**: [0, 1] for both x and y coordinates
- **Origin**: Top-left corner of the image
- **Use case**: 2D overlay visualization

#### World Landmarks
- **Units**: Meters
- **Origin**: Hip center (midpoint between left and right hips)
- **Coordinate system**:
  - X: Left/Right (positive = right)
  - Y: Up/Down (positive = up)
  - Z: Forward/Backward (positive = towards camera)

---

## 2. SMPLX Model Joint Structure

### 2.1 Overview
SMPLX is an expressive body model with **55 joints (indices 0-54)** including:
- 22 body joints
- 3 face joints (jaw, eyes)
- 15 left hand joints (3 per finger × 5 fingers)
- 15 right hand joints (3 per finger × 5 fingers)

### 2.2 Complete Joint List (Indices 0-54)

#### Body Joints (Indices 0-21)
| Index | Name | Parent | Description |
|-------|------|--------|-------------|
| 0 | pelvis | - | Root joint (center of rotation) |
| 1 | left_hip | 0 | Left hip joint |
| 2 | right_hip | 0 | Right hip joint |
| 3 | spine1 | 0 | Lower spine |
| 4 | left_knee | 1 | Left knee joint |
| 5 | right_knee | 2 | Right knee joint |
| 6 | spine2 | 3 | Middle spine |
| 7 | left_ankle | 4 | Left ankle joint |
| 8 | right_ankle | 5 | Right ankle joint |
| 9 | spine3 | 6 | Upper spine |
| 10 | left_foot | 7 | Left foot |
| 11 | right_foot | 8 | Right foot |
| 12 | neck | 9 | Neck joint |
| 13 | left_collar | 9 | Left clavicle |
| 14 | right_collar | 9 | Right clavicle |
| 15 | head | 12 | Head joint |
| 16 | left_shoulder | 13 | Left shoulder |
| 17 | right_shoulder | 14 | Right shoulder |
| 18 | left_elbow | 16 | Left elbow |
| 19 | right_elbow | 17 | Right elbow |
| 20 | left_wrist | 18 | Left wrist |
| 21 | right_wrist | 19 | Right wrist |

#### Face Joints (Indices 22-24)
| Index | Name | Parent | Description |
|-------|------|--------|-------------|
| 22 | jaw | 15 | Jaw joint |
| 23 | left_eye_smplx | 15 | Left eyeball |
| 24 | right_eye_smplx | 15 | Right eyeball |

#### Left Hand Joints (Indices 25-39)
| Index | Name | Parent | Description |
|-------|------|--------|-------------|
| 25 | left_index1 | 20 | Left index proximal |
| 26 | left_index2 | 25 | Left index intermediate |
| 27 | left_index3 | 26 | Left index distal |
| 28 | left_middle1 | 20 | Left middle proximal |
| 29 | left_middle2 | 28 | Left middle intermediate |
| 30 | left_middle3 | 29 | Left middle distal |
| 31 | left_pinky1 | 20 | Left pinky proximal |
| 32 | left_pinky2 | 31 | Left pinky intermediate |
| 33 | left_pinky3 | 32 | Left pinky distal |
| 34 | left_ring1 | 20 | Left ring proximal |
| 35 | left_ring2 | 34 | Left ring intermediate |
| 36 | left_ring3 | 35 | Left ring distal |
| 37 | left_thumb1 | 20 | Left thumb proximal |
| 38 | left_thumb2 | 37 | Left thumb intermediate |
| 39 | left_thumb3 | 38 | Left thumb distal |

#### Right Hand Joints (Indices 40-54)
| Index | Name | Parent | Description |
|-------|------|--------|-------------|
| 40 | right_index1 | 21 | Right index proximal |
| 41 | right_index2 | 40 | Right index intermediate |
| 42 | right_index3 | 41 | Right index distal |
| 43 | right_middle1 | 21 | Right middle proximal |
| 44 | right_middle2 | 43 | Right middle intermediate |
| 45 | right_middle3 | 44 | Right middle distal |
| 46 | right_pinky1 | 21 | Right pinky proximal |
| 47 | right_pinky2 | 46 | Right pinky intermediate |
| 48 | right_pinky3 | 47 | Right pinky distal |
| 49 | right_ring1 | 21 | Right ring proximal |
| 50 | right_ring2 | 49 | Right ring intermediate |
| 51 | right_ring3 | 50 | Right ring distal |
| 52 | right_thumb1 | 21 | Right thumb proximal |
| 53 | right_thumb2 | 52 | Right thumb intermediate |
| 54 | right_thumb3 | 53 | Right thumb distal |

### 2.3 Kinematic Hierarchy
SMPLX uses a tree-based kinematic hierarchy where:
- **Root**: Pelvis (index 0)
- **Parent-child relationships** define the kinematic chain
- **Pose parameters** represent rotations at each joint relative to parent
- **Forward kinematics** propagates transformations through the tree

---

## 3. Mapping Strategy: MediaPipe to SMPLX

### 3.1 Core Challenge

**The fundamental problem**: MediaPipe provides 33 landmarks vs SMPLX's 55 joints.

**Key mapping challenges**:
1. **Missing joints**: MediaPipe lacks spine subdivisions, collar bones, and detailed finger joints
2. **Different representations**: MediaPipe outputs positions; SMPLX expects rotations
3. **Coordinate system differences**: Need transformation between MediaPipe's camera space and Unity's world space
4. **Finger detail**: MediaPipe only provides basic hand landmarks (pinky, index, thumb tips), not all finger joints
5. **Face detail**: MediaPipe's face landmarks don't directly map to SMPLX jaw/eye joints

### 3.2 Direct Mapping (One-to-One Correspondences)

These MediaPipe landmarks have direct SMPLX joint correspondences:

| MediaPipe Index | MediaPipe Name | SMPLX Index | SMPLX Name | Mapping Quality |
|----------------|----------------|-------------|------------|-----------------|
| 11 | LEFT_SHOULDER | 16 | left_shoulder | ⭐⭐⭐ Excellent |
| 12 | RIGHT_SHOULDER | 17 | right_shoulder | ⭐⭐⭐ Excellent |
| 13 | LEFT_ELBOW | 18 | left_elbow | ⭐⭐⭐ Excellent |
| 14 | RIGHT_ELBOW | 19 | right_elbow | ⭐⭐⭐ Excellent |
| 15 | LEFT_WRIST | 20 | left_wrist | ⭐⭐⭐ Excellent |
| 16 | RIGHT_WRIST | 21 | right_wrist | ⭐⭐⭐ Excellent |
| 23 | LEFT_HIP | 1 | left_hip | ⭐⭐⭐ Excellent |
| 24 | RIGHT_HIP | 2 | right_hip | ⭐⭐⭐ Excellent |
| 25 | LEFT_KNEE | 4 | left_knee | ⭐⭐⭐ Excellent |
| 26 | RIGHT_KNEE | 5 | right_knee | ⭐⭐⭐ Excellent |
| 27 | LEFT_ANKLE | 7 | left_ankle | ⭐⭐⭐ Excellent |
| 28 | RIGHT_ANKLE | 8 | right_ankle | ⭐⭐⭐ Excellent |
| 0 | NOSE | 15 | head | ⭐⭐ Good (approximation) |

### 3.3 Interpolated/Estimated Joints

These SMPLX joints must be estimated from MediaPipe landmarks:

#### Spine Joints (spine1, spine2, spine3)
```
spine1 (index 3) = interpolate between pelvis and mid_shoulder
spine2 (index 6) = interpolate at 2/3 from pelvis to mid_shoulder
spine3 (index 9) = mid_shoulder position
```

Where:
- `pelvis = midpoint(left_hip, right_hip)`
- `mid_shoulder = midpoint(left_shoulder, right_shoulder)`

#### Pelvis (Root Joint)
```
pelvis (index 0) = midpoint(MediaPipe_left_hip, MediaPipe_right_hip)
```

#### Neck Joint
```
neck (index 12) = midpoint(mid_shoulder, nose)
```

#### Collar Bones
```
left_collar (index 13) = interpolate between spine3 and left_shoulder (25% from spine3)
right_collar (index 14) = interpolate between spine3 and right_shoulder (25% from spine3)
```

#### Foot Joints
```
left_foot (index 10) = use MediaPipe LEFT_FOOT_INDEX (index 31)
right_foot (index 11) = use MediaPipe RIGHT_FOOT_INDEX (index 32)
```

#### Eye Joints
```
left_eye_smplx (index 23) = use MediaPipe LEFT_EYE (index 2)
right_eye_smplx (index 24) = use MediaPipe RIGHT_EYE (index 5)
```

### 3.4 Hand Joints (Indices 25-54)

**Challenge**: MediaPipe only provides 3 landmarks per hand:
- Pinky base (indices 17, 18)
- Index base (indices 19, 20)
- Thumb base (indices 21, 22)

**Solutions**:
1. **Use MediaPipe Hand Landmarker** (recommended): Run separate hand tracking for 21 hand landmarks per hand
2. **Estimate finger poses**: Use default/rest poses or simple curl estimation
3. **Hybrid approach**: Detect hand presence from pose landmarks, then trigger detailed hand tracking

#### Hand Mapping (If using MediaPipe Hand Landmarker)

MediaPipe Hand provides 21 landmarks per hand:
```
0: WRIST
1-4: THUMB (CMC, MCP, IP, TIP)
5-8: INDEX (MCP, PIP, DIP, TIP)
9-12: MIDDLE (MCP, PIP, DIP, TIP)
13-16: RING (MCP, PIP, DIP, TIP)
17-20: PINKY (MCP, PIP, DIP, TIP)
```

Mapping to SMPLX (example for left hand):
```
MediaPipe Hand 1-3 → SMPLX 37-39 (left_thumb1-3)
MediaPipe Hand 5-7 → SMPLX 25-27 (left_index1-3)
MediaPipe Hand 9-11 → SMPLX 28-30 (left_middle1-3)
MediaPipe Hand 13-15 → SMPLX 34-36 (left_ring1-3)
MediaPipe Hand 17-19 → SMPLX 31-33 (left_pinky1-3)
```

### 3.5 Jaw Joint (Index 22)

**Challenge**: MediaPipe Pose doesn't provide jaw tracking.

**Solutions**:
1. **Use MediaPipe Face Mesh**: Provides 468+ facial landmarks
2. **Estimate from mouth landmarks**: Use distance between mouth corners (9, 10)
3. **Keep neutral**: Leave jaw at rest position

---

## 4. Position to Rotation Conversion

### 4.1 The Core Problem

**MediaPipe provides positions** (x, y, z coordinates) but **SMPLX expects rotations** (quaternions or axis-angle).

### 4.2 Rotation Calculation Approach

For each joint, calculate rotation from position of child landmarks:

```
1. Get parent and child landmark positions
2. Calculate direction vector: child - parent
3. Compute rotation to align bone with direction vector
4. Convert to local space (relative to parent bone)
5. Apply to SMPLX joint
```

### 4.3 Mathematical Formulation

#### Step 1: Calculate Direction Vector
```csharp
Vector3 direction = childPosition - parentPosition;
direction = direction.normalized;
```

#### Step 2: Calculate Rotation
```csharp
// Get default bone direction in T-pose
Vector3 defaultDirection = GetTPoseDirection(jointIndex);

// Calculate rotation from default to current direction
Quaternion rotation = Quaternion.FromToRotation(defaultDirection, direction);
```

#### Step 3: Convert to Local Space
```csharp
// Get parent's world rotation
Quaternion parentWorldRotation = GetParentWorldRotation(jointIndex);

// Convert to local rotation
Quaternion localRotation = Quaternion.Inverse(parentWorldRotation) * rotation;
```

#### Step 4: Apply Constraints
```csharp
// Clamp rotation to anatomically valid ranges
localRotation = ApplyJointLimits(jointIndex, localRotation);

// Apply smoothing to reduce jitter
localRotation = Quaternion.Slerp(previousRotation, localRotation, smoothingFactor);
```

### 4.4 Specific Joint Rotation Calculations

#### Upper Arm (Shoulder to Elbow)
```csharp
Vector3 shoulderPos = landmarks[11]; // LEFT_SHOULDER
Vector3 elbowPos = landmarks[13];    // LEFT_ELBOW
Vector3 wristPos = landmarks[15];    // LEFT_WRIST

// Upper arm direction
Vector3 upperArmDir = (elbowPos - shoulderPos).normalized;

// Calculate shoulder rotation
Quaternion shoulderRot = Quaternion.FromToRotation(Vector3.down, upperArmDir);
```

#### Lower Arm (Elbow to Wrist)
```csharp
// Lower arm direction
Vector3 lowerArmDir = (wristPos - elbowPos).normalized;

// Calculate elbow rotation (twist around arm axis)
Vector3 armAxis = upperArmDir;
Vector3 perpendicular = Vector3.Cross(armAxis, lowerArmDir);
float angle = Vector3.Angle(armAxis, lowerArmDir);
Quaternion elbowRot = Quaternion.AngleAxis(angle, perpendicular);
```

#### Spine Chain
```csharp
Vector3 pelvisPos = (landmarks[23] + landmarks[24]) * 0.5f;
Vector3 shoulderMid = (landmarks[11] + landmarks[12]) * 0.5f;

Vector3 spineDir = (shoulderMid - pelvisPos).normalized;

// Distribute rotation across spine joints
float totalAngle = Vector3.Angle(Vector3.up, spineDir);
float anglePerJoint = totalAngle / 3.0f; // 3 spine joints

// Apply to spine1, spine2, spine3
```

---

## 5. Coordinate System Transformations

### 5.1 Coordinate System Differences

#### MediaPipe World Coordinates
- Origin: Hip center
- X: Right (+) / Left (-)
- Y: Up (+) / Down (-)
- Z: Forward (+) / Back (-)
- Units: Meters

#### Unity World Space
- Origin: Scene origin
- X: Right (+) / Left (-)
- Y: Up (+) / Down (-)
- Z: Forward (+) / Back (-)
- Units: Unity units (typically meters)

#### SMPLX Model Space
- Origin: Pelvis
- Coordinate frame: Depends on model import orientation
- Requires T-pose calibration

### 5.2 Transformation Pipeline

```csharp
// 1. MediaPipe world landmarks (meters, origin at hips)
Vector3 mpLandmark = poseLandmarks[index];

// 2. Transform to Unity world space
Vector3 unityPos = new Vector3(
    -mpLandmark.x,  // Flip X if needed (depends on camera)
    mpLandmark.y,
    -mpLandmark.z   // Flip Z to convert from camera to world space
);

// 3. Scale to Unity scene
unityPos *= scaleFactor; // Adjust to match your character size

// 4. Offset to character position
unityPos += characterRootPosition;

// 5. Transform to SMPLX model local space
Vector3 smplxLocalPos = smplxTransform.InverseTransformPoint(unityPos);
```

### 5.3 Handling Camera Orientation

If camera is not facing forward, apply camera transform:

```csharp
Quaternion cameraRotation = Camera.main.transform.rotation;
Vector3 worldPos = cameraRotation * mpLandmark;
```

---

## 6. Implementation Architecture

### 6.1 System Components

```
┌─────────────────────────┐
│  MediaPipe Unity Plugin │
│  (Pose Landmarker)      │
└───────────┬─────────────┘
            │ 33 Landmarks
            ▼
┌─────────────────────────┐
│  Landmark Preprocessor  │
│  - Smoothing            │
│  - Coordinate transform │
└───────────┬─────────────┘
            │ Processed Landmarks
            ▼
┌─────────────────────────┐
│  Joint Mapper           │
│  - Direct mapping       │
│  - Interpolation        │
└───────────┬─────────────┘
            │ 55 Joint Positions
            ▼
┌─────────────────────────┐
│  Rotation Solver        │
│  - Position→Rotation    │
│  - IK solving           │
└───────────┬─────────────┘
            │ Joint Rotations
            ▼
┌─────────────────────────┐
│  SMPLX Controller       │
│  - Apply rotations      │
│  - Blend with animation │
└─────────────────────────┘
```

### 6.2 Data Flow

```csharp
// 1. Capture MediaPipe landmarks
PoseLandmarkerResult result = poseLandmarker.Detect(inputImage);
List<NormalizedLandmark> landmarks = result.poseLandmarks;
List<Landmark> worldLandmarks = result.poseWorldLandmarks;

// 2. Preprocess landmarks
Vector3[] processedLandmarks = PreprocessLandmarks(worldLandmarks);

// 3. Map to SMPLX joint positions
Vector3[] smplxJointPositions = MapToSMPLXPositions(processedLandmarks);

// 4. Solve for rotations
Quaternion[] jointRotations = SolveJointRotations(smplxJointPositions);

// 5. Apply to SMPLX model
ApplyRotationsToSMPLX(jointRotations);
```

---

## 7. Key Implementation Challenges & Solutions

### 7.1 Jitter and Noise

**Problem**: Raw MediaPipe landmarks are noisy, causing jittery motion.

**Solutions**:
1. **One Euro Filter**: Adaptive filter that reduces lag while maintaining smoothness
2. **Moving Average**: Simple but introduces lag
3. **Kalman Filter**: Predictive filtering for smoother motion
4. **Double Exponential Smoothing**: Good balance of smoothness and responsiveness

```csharp
// One Euro Filter (recommended)
public class OneEuroFilter
{
    private float minCutoff = 1.0f;
    private float beta = 0.007f;

    public Vector3 Filter(Vector3 raw, float deltaTime)
    {
        // Implementation of One Euro Filter
        // See: http://cristal.univ-lille.fr/~casiez/1euro/
    }
}
```

### 7.2 Depth Ambiguity

**Problem**: MediaPipe's Z-depth can be unreliable, especially for poses facing sideways.

**Solutions**:
1. **Depth clamping**: Limit unrealistic Z-values
2. **Confidence weighting**: Use visibility scores to weight depth
3. **Biomechanical constraints**: Enforce limb length consistency

```csharp
// Enforce limb length consistency
float expectedLength = GetBoneLength(jointIndex);
Vector3 direction = (child - parent).normalized;
Vector3 correctedChild = parent + direction * expectedLength;
```

### 7.3 Gimbal Lock

**Problem**: Euler angle rotations can suffer from gimbal lock.

**Solutions**:
1. **Use Quaternions**: Always work in quaternion space
2. **Axis-angle representation**: SMPLX native format
3. **Swing-twist decomposition**: Separate rotation components

### 7.4 Unnatural Poses

**Problem**: Direct position mapping can create anatomically impossible poses.

**Solutions**:
1. **Joint constraints**: Enforce rotation limits
2. **IK post-processing**: Use inverse kinematics for limbs
3. **Physics-based correction**: Simulate skeletal constraints

```csharp
// Example joint limits for elbow (hinge joint)
float elbowBend = Quaternion.Angle(upperArmRot, lowerArmRot);
elbowBend = Mathf.Clamp(elbowBend, 0f, 150f); // 0-150 degrees
```

### 7.5 Performance Optimization

**Key considerations**:
1. **Run MediaPipe on separate thread**: Don't block main Unity thread
2. **Async result handling**: Process landmarks asynchronously
3. **LOD for SMPLX**: Reduce joint count for distant characters
4. **Batch processing**: Update multiple characters in batches

---

## 8. Integration with MediaPipe Unity Plugin

### 8.1 Plugin Setup

```csharp
using Mediapipe.Tasks.Vision.PoseLandmarker;

public class PoseTracker : MonoBehaviour
{
    private PoseLandmarker poseLandmarker;
    private PoseLandmarkerOptions options;

    void Start()
    {
        // Configure options
        options = new PoseLandmarkerOptions
        {
            runningMode = RunningMode.LIVE_STREAM,
            numPoses = 1,
            minPoseDetectionConfidence = 0.5f,
            minPosePresenceConfidence = 0.5f,
            minTrackingConfidence = 0.5f,
            outputSegmentationMasks = false
        };

        // Create landmarker
        poseLandmarker = PoseLandmarker.CreateFromOptions(options);
    }
}
```

### 8.2 Processing Live Camera Feed

```csharp
public void ProcessCameraFrame(Texture2D frame, long timestampMs)
{
    // Detect pose asynchronously
    poseLandmarker.DetectAsync(frame, timestampMs, OnPoseDetected);
}

private void OnPoseDetected(PoseLandmarkerResult result)
{
    if (result.poseLandmarks.Count > 0)
    {
        // Get first detected pose
        var landmarks = result.poseWorldLandmarks[0];

        // Update SMPLX model
        UpdateSMPLXFromLandmarks(landmarks);
    }
}
```

---

## 9. Complete Mapping Table

### 9.1 Full Correspondence Matrix

| SMPLX Index | SMPLX Joint | Mapping Type | Source | Notes |
|-------------|-------------|--------------|--------|-------|
| 0 | pelvis | Calculated | MP 23,24 | Midpoint of hips |
| 1 | left_hip | Direct | MP 23 | Excellent |
| 2 | right_hip | Direct | MP 24 | Excellent |
| 3 | spine1 | Interpolated | MP 23,24,11,12 | Lower spine |
| 4 | left_knee | Direct | MP 25 | Excellent |
| 5 | right_knee | Direct | MP 26 | Excellent |
| 6 | spine2 | Interpolated | MP 23,24,11,12 | Mid spine |
| 7 | left_ankle | Direct | MP 27 | Excellent |
| 8 | right_ankle | Direct | MP 28 | Excellent |
| 9 | spine3 | Calculated | MP 11,12 | Upper spine |
| 10 | left_foot | Direct | MP 31 | Use foot index |
| 11 | right_foot | Direct | MP 32 | Use foot index |
| 12 | neck | Interpolated | MP 0,11,12 | Between shoulders and nose |
| 13 | left_collar | Interpolated | MP 9,11 | 25% from spine3 to shoulder |
| 14 | right_collar | Interpolated | MP 9,12 | 25% from spine3 to shoulder |
| 15 | head | Approximated | MP 0 | Use nose position |
| 16 | left_shoulder | Direct | MP 11 | Excellent |
| 17 | right_shoulder | Direct | MP 12 | Excellent |
| 18 | left_elbow | Direct | MP 13 | Excellent |
| 19 | right_elbow | Direct | MP 14 | Excellent |
| 20 | left_wrist | Direct | MP 15 | Excellent |
| 21 | right_wrist | Direct | MP 16 | Excellent |
| 22 | jaw | Face Mesh | - | Requires Face Mesh |
| 23 | left_eye_smplx | Direct | MP 2 | Use eye center |
| 24 | right_eye_smplx | Direct | MP 5 | Use eye center |
| 25-27 | left_index1-3 | Hand Tracker | - | Requires Hand Landmarker |
| 28-30 | left_middle1-3 | Hand Tracker | - | Requires Hand Landmarker |
| 31-33 | left_pinky1-3 | Estimated | MP 17 | Only base available |
| 34-36 | left_ring1-3 | Hand Tracker | - | Requires Hand Landmarker |
| 37-39 | left_thumb1-3 | Estimated | MP 21 | Only base available |
| 40-42 | right_index1-3 | Hand Tracker | - | Requires Hand Landmarker |
| 43-45 | right_middle1-3 | Hand Tracker | - | Requires Hand Landmarker |
| 46-48 | right_pinky1-3 | Estimated | MP 18 | Only base available |
| 49-51 | right_ring1-3 | Hand Tracker | - | Requires Hand Landmarker |
| 52-54 | right_thumb1-3 | Estimated | MP 22 | Only base available |

*MP = MediaPipe Pose Landmark Index*

---

## 10. Recommendations for Production

### 10.1 Multi-Model Approach (Recommended)

For best results, use multiple MediaPipe models:

```
┌──────────────┐
│ Pose Tracker │ → 33 body landmarks → Body joints (0-21)
└──────────────┘

┌──────────────┐
│ Hand Tracker │ → 21 × 2 hand landmarks → Hand joints (25-54)
└──────────────┘

┌──────────────┐
│  Face Mesh   │ → 468 face landmarks → Face joints (22-24)
└──────────────┘
```

### 10.2 Optimization Strategy

1. **Always run Pose Tracker** (core body motion)
2. **Run Hand Tracker conditionally**:
   - When hands are near body center (waving, gesturing)
   - When hands are in focus
   - Skip when arms are down/resting
3. **Run Face Mesh optionally**:
   - For close-up shots
   - When facial expression is important
   - Can be lower framerate (15 FPS vs 30 FPS)

### 10.3 Quality vs Performance Trade-offs

| Quality Level | Models Used | Frame Rate | Use Case |
|--------------|-------------|------------|----------|
| Low | Pose only | 60 FPS | Distant view, low-end devices |
| Medium | Pose + Hand (simplified) | 30 FPS | Standard gameplay |
| High | Pose + Hand (full) | 30 FPS | Close-up, high-end devices |
| Ultra | Pose + Hand + Face | 15-30 FPS | Cinematics, streaming |

---

## 11. Testing and Validation

### 11.1 Test Poses

Test your implementation with these standard poses:
1. **T-Pose**: Arms extended horizontally
2. **A-Pose**: Arms at 45 degrees
3. **Squat**: Deep knee bend
4. **Reach Up**: Arms fully extended upward
5. **Side Lean**: Lateral spine bend
6. **Twist**: Spinal rotation
7. **Walk Cycle**: Dynamic motion

### 11.2 Validation Checklist

- [ ] No inverted joints (knees/elbows bending wrong way)
- [ ] Limb lengths remain constant
- [ ] Smooth motion without jitter
- [ ] Feet stay grounded (no skating)
- [ ] Hands align with wrists
- [ ] Spine bends naturally
- [ ] Head orientation matches body
- [ ] No gimbal lock artifacts

### 11.3 Debug Visualization

```csharp
// Visualize landmark mappings
void OnDrawGizmos()
{
    // Draw MediaPipe landmarks
    Gizmos.color = Color.green;
    foreach (var landmark in mediapipeLandmarks)
    {
        Gizmos.DrawSphere(landmark, 0.02f);
    }

    // Draw SMPLX joints
    Gizmos.color = Color.blue;
    foreach (var joint in smplxJoints)
    {
        Gizmos.DrawSphere(joint.position, 0.03f);
    }

    // Draw mapping connections
    Gizmos.color = Color.yellow;
    foreach (var mapping in directMappings)
    {
        Gizmos.DrawLine(mediapipeLandmarks[mapping.mpIndex],
                       smplxJoints[mapping.smplxIndex].position);
    }
}
```

---

## 12. Common Pitfalls and Solutions

### 12.1 Pitfall: Direct Position Assignment

❌ **Wrong**:
```csharp
smplxJoint.position = mediapipeLandmark.position;
```

✅ **Correct**:
```csharp
Quaternion rotation = CalculateRotationFromPositions(parent, child);
smplxJoint.localRotation = rotation;
```

### 12.2 Pitfall: Ignoring Parent Transforms

❌ **Wrong**:
```csharp
joint.rotation = worldRotation;
```

✅ **Correct**:
```csharp
joint.localRotation = Quaternion.Inverse(parent.rotation) * worldRotation;
```

### 12.3 Pitfall: Not Handling Missing Data

❌ **Wrong**:
```csharp
Vector3 pos = worldLandmarks[index].position; // May be invalid
```

✅ **Correct**:
```csharp
if (worldLandmarks[index].visibility > 0.5f)
{
    Vector3 pos = worldLandmarks[index].position;
    // Use position
}
else
{
    // Use previous frame or default
}
```

---

## 13. Advanced Techniques

### 13.1 Inverse Kinematics (IK)

For limbs, use IK to ensure better end-effector positioning:

```csharp
// Two-bone IK for arm (shoulder-elbow-wrist)
void SolveArmIK(Transform shoulder, Transform elbow, Transform wrist,
                Vector3 targetWristPos)
{
    Vector3 shoulderPos = shoulder.position;
    float upperLength = Vector3.Distance(shoulder.position, elbow.position);
    float lowerLength = Vector3.Distance(elbow.position, wrist.position);

    // Apply two-bone IK algorithm
    // (Implementation details omitted for brevity)
}
```

### 13.2 Full-Body IK

Use Unity's Animation Rigging package for full-body IK:
- Constraints on hands and feet
- Look-at constraints for head
- Multi-parent constraints for spine

### 13.3 Motion Capture Refinement

Post-process with physics:
1. **Ragdoll simulation**: For natural collision response
2. **Joint stabilization**: Physics-based joint stabilizers
3. **Ground contact**: Ray-cast for foot placement

---

## 14. References and Resources

### 14.1 Official Documentation
- [MediaPipe Pose Landmarker](https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker)
- [MediaPipe Unity Plugin](https://github.com/homuler/MediaPipeUnityPlugin)
- [SMPL-X GitHub](https://github.com/vchoutas/smplx)
- [SMPL-X Website](https://smpl-x.is.tue.mpg.de/)

### 14.2 Related Projects
- [Realtime_SMPLX_Unity](https://github.com/sangho0n/Realtime_SMPLX_Unity)
- [MediaPipe-UnitySolver](https://github.com/BrandonBartram98/MediaPipe-UnitySolver)
- [Unity-SMPLX](https://gitlab.tuebingen.mpg.de/jtesch/smplx-unity)

### 14.3 Academic Papers
- **SMPL-X**: "Expressive Body Capture: 3D Hands, Face, and Body from a Single Image" (CVPR 2019)
- **MediaPipe**: "BlazePose: On-device Real-time Body Pose tracking" (2020)

---

## 15. Conclusion

Mapping MediaPipe pose landmarks to SMPLX joints requires:
1. **Understanding both systems** thoroughly
2. **Handling missing joints** through interpolation
3. **Converting positions to rotations** correctly
4. **Managing coordinate systems** properly
5. **Applying smoothing and constraints** for natural motion
6. **Using complementary models** (Hand, Face) for complete capture

The key to success is **iterative refinement**: start with direct mappings, add interpolation, tune parameters, and validate with diverse test cases.

For production use, consider **the multi-model approach** with pose, hand, and face tracking for complete expressive capture of human motion.
