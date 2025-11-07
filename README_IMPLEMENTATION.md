# MediaPipe to SMPLX Unity Implementation

This repository contains a complete implementation for mapping MediaPipe pose landmarks to SMPLX model joints in Unity.

## 📁 Files Overview

### Documentation
- **MEDIAPIPE_TO_SMPLX_MAPPING.md** - Comprehensive guide explaining the mapping strategy, joint structures, and technical details

### Core Scripts
1. **MediaPipeToSMPLXMapper.cs** - Mapping configuration and constants
2. **LandmarkPreprocessor.cs** - Landmark smoothing and filtering
3. **SMPLXJointCalculator.cs** - Position calculation from landmarks
4. **SMPLXRotationSolver.cs** - Rotation solving from positions
5. **MediaPipeSMPLXController.cs** - Main controller component

### Example Scripts
- **MediaPipeIntegrationExample.cs** - Complete working example with camera integration

## 🚀 Quick Start

### Prerequisites

1. **Unity 2022.3 or later**
2. **MediaPipe Unity Plugin** - Install from: https://github.com/homuler/MediaPipeUnityPlugin
3. **SMPLX Unity Model** - Import your SMPLX character model

### Installation Steps

#### Step 1: Install MediaPipe Unity Plugin

```bash
# Option 1: Unity Package Manager
# Add this to your Packages/manifest.json:
{
  "dependencies": {
    "com.github.homuler.mediapipe": "https://github.com/homuler/MediaPipeUnityPlugin.git?path=/Packages/com.github.homuler.mediapipe"
  }
}

# Option 2: Download and import
# Download from GitHub and import as Unity package
```

#### Step 2: Import Scripts

1. Copy all `.cs` files to your Unity project's `Assets/Scripts/` folder
2. Ensure MediaPipe Unity Plugin is properly installed

#### Step 3: Download MediaPipe Model

Download the pose landmarker model:
```bash
# Download from MediaPipe
# Place in Assets/StreamingAssets/
wget https://storage.googleapis.com/mediapipe-models/pose_landmarker/pose_landmarker_heavy/float16/1/pose_landmarker_heavy.task
```

#### Step 4: Setup SMPLX Character

1. Import your SMPLX character into Unity
2. Ensure it has a proper bone hierarchy (55 joints)
3. Set up the character in T-pose for initial calibration

#### Step 5: Create Scene Setup

1. **Create Empty GameObject** named "PoseTracking"
2. **Add Component**: `MediaPipeSMPLXController`
3. **Add Component**: `MediaPipeIntegrationExample`
4. **Assign References**:
   - SMPLX Root → Your character's root transform
   - SMPLX Joints → Use "Auto-Assign SMPLX Joints" button or assign manually

## 🎮 Usage

### Basic Usage

```csharp
using UnityEngine;

public class SimplePoseTracking : MonoBehaviour
{
    public MediaPipeSMPLXController controller;

    void Start()
    {
        // Controller auto-initializes
        // Camera feed starts automatically if using integration example
    }

    void Update()
    {
        // Check tracking status
        if (controller.IsTrackingActive())
        {
            Debug.Log("Pose tracking active!");
        }
    }
}
```

### Advanced Usage with Custom Pipeline

```csharp
using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;

public class CustomPoseTracking : MonoBehaviour
{
    private MediaPipeSMPLXController smplxController;
    private PoseLandmarker poseLandmarker;

    void Start()
    {
        smplxController = GetComponent<MediaPipeSMPLXController>();

        // Initialize MediaPipe
        var options = new PoseLandmarkerOptions
        {
            runningMode = RunningMode.LIVE_STREAM,
            numPoses = 1,
            minPoseDetectionConfidence = 0.5f
        };

        poseLandmarker = PoseLandmarker.CreateFromOptions(options);
    }

    void ProcessFrame(Texture2D frame, long timestamp)
    {
        // Detect pose
        poseLandmarker.DetectAsync(frame, timestamp, OnPoseDetected);
    }

    void OnPoseDetected(PoseLandmarkerResult result, Image image, long timestamp)
    {
        // Update SMPLX character
        smplxController.ProcessPoseResult(result);
    }
}
```

## ⚙️ Configuration

### Preprocessor Settings

```csharp
// In Inspector or code
preprocessorSettings.minCutoff = 1.0f;        // Lower = more smoothing
preprocessorSettings.beta = 0.007f;           // Higher = less lag
preprocessorSettings.visibilityThreshold = 0.5f;
preprocessorSettings.scaleFactor = 1.0f;
preprocessorSettings.flipZ = true;            // Convert camera to world space
```

### Calculator Settings

```csharp
calculatorSettings.estimateHandJoints = true;   // Estimate when no hand data
calculatorSettings.defaultFingerCurl = 0.3f;    // Default finger curl amount
calculatorSettings.fingerLengthRatio = 0.15f;   // Finger length relative to hand
```

### Solver Settings

```csharp
solverSettings.rotationSmoothingFactor = 0.3f;  // Rotation smoothing (0-1)
solverSettings.applyJointLimits = true;         // Enforce anatomical limits
solverSettings.useIKForLimbs = false;           // Use IK solving (experimental)
solverSettings.minAngleThreshold = 0.5f;        // Min angle change in degrees
```

## 🎯 Joint Mapping Reference

### Direct Mappings (Excellent Quality)
| MediaPipe Landmark | SMPLX Joint | Quality |
|-------------------|-------------|---------|
| LEFT_SHOULDER (11) | left_shoulder (16) | ⭐⭐⭐ |
| RIGHT_SHOULDER (12) | right_shoulder (17) | ⭐⭐⭐ |
| LEFT_ELBOW (13) | left_elbow (18) | ⭐⭐⭐ |
| RIGHT_ELBOW (14) | right_elbow (19) | ⭐⭐⭐ |
| LEFT_WRIST (15) | left_wrist (20) | ⭐⭐⭐ |
| RIGHT_WRIST (16) | right_wrist (21) | ⭐⭐⭐ |
| LEFT_HIP (23) | left_hip (1) | ⭐⭐⭐ |
| RIGHT_HIP (24) | right_hip (2) | ⭐⭐⭐ |
| LEFT_KNEE (25) | left_knee (4) | ⭐⭐⭐ |
| RIGHT_KNEE (26) | right_knee (5) | ⭐⭐⭐ |
| LEFT_ANKLE (27) | left_ankle (7) | ⭐⭐⭐ |
| RIGHT_ANKLE (28) | right_ankle (8) | ⭐⭐⭐ |

### Interpolated Joints
| SMPLX Joint | Calculation | Quality |
|------------|-------------|---------|
| pelvis (0) | Midpoint of hips | ⭐⭐⭐ |
| spine1 (3) | Interpolated from hips/shoulders | ⭐⭐ |
| spine2 (6) | Interpolated from hips/shoulders | ⭐⭐ |
| spine3 (9) | Midpoint of shoulders | ⭐⭐⭐ |
| neck (12) | Between shoulders and nose | ⭐⭐ |

### Hand Joints (25-54)
- **Basic mode**: Estimated from wrist and finger base landmarks
- **Advanced mode**: Use MediaPipe Hand Landmarker (21 landmarks per hand)

## 🔧 Troubleshooting

### Common Issues

#### 1. Character Not Moving
**Problem**: SMPLX character doesn't respond to tracking

**Solutions**:
- Verify SMPLX joints are correctly assigned
- Check that `enableTracking` is true
- Ensure camera is providing frames
- Check console for errors

#### 2. Jittery Motion
**Problem**: Character movements are shaky

**Solutions**:
- Increase `minCutoff` in preprocessor settings (more smoothing)
- Increase `rotationSmoothingFactor` in solver settings
- Ensure good lighting for camera
- Reduce camera noise

#### 3. Unnatural Poses
**Problem**: Character assumes anatomically impossible poses

**Solutions**:
- Enable `applyJointLimits` in solver settings
- Check that joint hierarchy is correct
- Verify T-pose directions are set properly
- Consider using IK for limbs

#### 4. Performance Issues
**Problem**: Low frame rate

**Solutions**:
- Reduce `targetFrameRate` (e.g., 15-30 FPS)
- Lower camera resolution
- Use lighter MediaPipe model
- Disable debug visualization

#### 5. Wrong Scale or Orientation
**Problem**: Character is wrong size or facing wrong direction

**Solutions**:
- Adjust `scaleFactor` in preprocessor settings
- Modify flip settings (flipX, flipY, flipZ)
- Check camera orientation
- Verify SMPLX model import settings

## 📊 Performance Optimization

### Recommended Settings by Platform

#### Desktop (High-end)
```
Target Frame Rate: 30 FPS
Camera Resolution: 1280x720
Model: pose_landmarker_heavy.task
Enable Hand Tracking: Yes
```

#### Desktop (Mid-range)
```
Target Frame Rate: 30 FPS
Camera Resolution: 640x480
Model: pose_landmarker_lite.task
Enable Hand Tracking: No
```

#### Mobile (High-end)
```
Target Frame Rate: 15 FPS
Camera Resolution: 640x480
Model: pose_landmarker_lite.task
Enable Hand Tracking: Conditional
```

#### Mobile (Low-end)
```
Target Frame Rate: 15 FPS
Camera Resolution: 320x240
Model: pose_landmarker_lite.task
Enable Hand Tracking: No
```

## 🎨 Extending the System

### Adding Hand Tracking

```csharp
// 1. Initialize MediaPipe Hand Landmarker
var handOptions = new HandLandmarkerOptions
{
    runningMode = RunningMode.LIVE_STREAM,
    numHands = 2,
    minHandDetectionConfidence = 0.5f
};
var handLandmarker = HandLandmarker.CreateFromOptions(handOptions);

// 2. Process hand landmarks
void OnHandsDetected(HandLandmarkerResult result)
{
    Vector3[] leftHand = ExtractHandLandmarks(result, 0);
    Vector3[] rightHand = ExtractHandLandmarks(result, 1);

    // 3. Pass to joint calculator
    smplxJointPositions = jointCalculator.CalculateJointPositions(
        mediapipeLandmarks,
        leftHand,
        rightHand
    );
}
```

### Adding Face Tracking

```csharp
// Similar approach using MediaPipe Face Mesh
// Extract jaw and eye movements
// Map to SMPLX face joints (22-24)
```

### Recording and Playback

```csharp
// Record poses
List<Vector3[]> recordedPoses = new List<Vector3[]>();

void RecordFrame()
{
    Vector3[] currentLandmarks = controller.GetMediaPipeLandmarks();
    recordedPoses.Add(currentLandmarks);
}

// Playback
void PlaybackFrame(int frameIndex)
{
    Vector3[] landmarks = recordedPoses[frameIndex];
    controller.UpdateFromLandmarks(landmarks, visibility);
}
```

## 📚 API Reference

### MediaPipeSMPLXController

#### Public Methods
```csharp
void ProcessPoseResult(PoseLandmarkerResult result)
void UpdateFromLandmarks(Vector3[] rawLandmarks, float[] visibility)
void ResetTracking()
void UpdateSettings()
Vector3[] GetMediaPipeLandmarks()
Vector3[] GetSMPLXJointPositions()
Quaternion[] GetSMPLXJointRotations()
bool IsTrackingActive()
```

### LandmarkPreprocessor

#### Public Methods
```csharp
Vector3[] ProcessLandmarks(Vector3[] raw, float[] visibility, float timestamp)
float GetVisibility(int landmarkIndex)
bool IsLandmarkValid(int landmarkIndex)
void Reset()
void UpdateSettings(FilterSettings settings)
```

### SMPLXJointCalculator

#### Public Methods
```csharp
Vector3[] CalculateJointPositions(Vector3[] mpLandmarks, Vector3[] handLeft, Vector3[] handRight)
Vector3 GetJointPosition(int smplxIndex)
Vector3 GetBoneDirection(int parentIndex, int childIndex)
float GetBoneLength(int parentIndex, int childIndex)
```

### SMPLXRotationSolver

#### Public Methods
```csharp
Quaternion[] SolveRotations(Vector3[] jointPositions, Transform rootTransform)
Quaternion GetJointRotation(int jointIndex)
void Reset()
void SolveTwoBoneIK(int joint1, int joint2, int joint3, Vector3 target, Vector3[] positions)
```

## 🧪 Testing

### Test Poses Checklist

- [ ] T-Pose (arms horizontal)
- [ ] A-Pose (arms at 45 degrees)
- [ ] Arms up (reaching overhead)
- [ ] Squat (deep knee bend)
- [ ] Walking motion
- [ ] Sitting position
- [ ] Side lean
- [ ] Torso twist

### Validation Checklist

- [ ] No inverted joints
- [ ] Constant limb lengths
- [ ] Smooth motion
- [ ] Feet grounded
- [ ] Natural spine curvature
- [ ] Proper shoulder movement
- [ ] Head tracking accuracy

## 📖 Additional Resources

- [MediaPipe Pose Documentation](https://ai.google.dev/edge/mediapipe/solutions/vision/pose_landmarker)
- [SMPLX Official Site](https://smpl-x.is.tue.mpg.de/)
- [MediaPipe Unity Plugin](https://github.com/homuler/MediaPipeUnityPlugin)
- [Unity Animation Rigging](https://docs.unity3d.com/Packages/com.unity.animation.rigging@latest)

## 🤝 Contributing

To improve this implementation:
1. Test with various poses and report issues
2. Optimize performance
3. Add support for more MediaPipe models
4. Improve hand/face tracking integration

## 📄 License

This implementation is provided as-is for educational and research purposes. Please respect the licenses of:
- MediaPipe (Apache 2.0)
- SMPLX (research/commercial licenses vary)
- MediaPipe Unity Plugin (MIT)

## 🐛 Known Limitations

1. **Hand joints**: Limited accuracy without Hand Landmarker
2. **Face joints**: Requires Face Mesh for detailed tracking
3. **Depth ambiguity**: Z-axis can be unreliable in some poses
4. **Occlusion**: Tracking fails when body parts are hidden
5. **Multiple people**: Currently supports single person tracking

## 🚀 Future Improvements

- [ ] Multi-person tracking support
- [ ] Full hand tracking integration
- [ ] Face expression mapping
- [ ] Motion retargeting to different skeleton types
- [ ] Real-time performance profiling
- [ ] Editor tools for calibration
- [ ] Animation blending with procedural motion

---

For detailed technical information, see **MEDIAPIPE_TO_SMPLX_MAPPING.md**
