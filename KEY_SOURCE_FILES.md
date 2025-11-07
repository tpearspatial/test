# MediaPipe Unity Plugin - Key Source Files Reference

## Core PoseLandmarker Classes

### PoseLandmarker.cs
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Vision/PoseLandmarker/PoseLandmarker.cs`
- **Purpose:** Main pose detection API with 3 running modes (IMAGE, VIDEO, LIVE_STREAM)
- **Key Methods:** Detect(), DetectForVideo(), DetectAsync(), TryDetect(), TryDetectForVideo()
- **Size:** ~297 lines

### PoseLandmarkerResult.cs
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Vision/PoseLandmarker/PoseLandmarkerResult.cs`
- **Purpose:** Result data structure containing normalized landmarks, world landmarks, and segmentation masks
- **Key Properties:** poseLandmarks, poseWorldLandmarks, segmentationMasks
- **Size:** ~84 lines

### PoseLandmarkerOptions.cs
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Vision/PoseLandmarker/PoseLandmarkerOptions.cs`
- **Purpose:** Configuration options for pose landmarker
- **Key Properties:** baseOptions, runningMode, numPoses, confidence thresholds, segmentation masks toggle
- **Size:** ~123 lines

---

## Landmark Data Structures

### Landmark.cs (Containers)
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Components/Containers/Landmark.cs`
- **Purpose:** Define Landmark, NormalizedLandmark, Landmarks, and NormalizedLandmarks structures
- **Key Structs:**
  - `Landmark` - 3D world coordinates (meters)
  - `NormalizedLandmark` - 2D normalized coordinates [0,1]
  - `Landmarks` - List of Landmarks
  - `NormalizedLandmarks` - List of NormalizedLandmarks
- **Size:** ~303 lines

---

## Annotation Controllers

### PoseLandmarkerResultAnnotationController.cs
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseLandmarkerResultAnnotationController.cs`
- **Purpose:** Main result controller that bridges detection results to visualization
- **Key Methods:** DrawNow(), DrawLater(), InitScreen()
- **Features:** Thread-safe result handling, segmentation mask support, Z-depth visualization
- **Size:** ~62 lines

### PoseLandmarkListAnnotationController.cs
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseLandmarkListAnnotationController.cs`
- **Purpose:** Simple 2D landmark list visualization controller
- **Key Methods:** DrawNow(), DrawLater()
- **Size:** ~46 lines

### PoseWorldLandmarkListAnnotationController.cs
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseWorldLandmarkListAnnotationController.cs`
- **Purpose:** 3D world landmark visualization with scaling
- **Key Properties:** hipHeightMeter, scale (X100 for visualization)
- **Size:** ~54 lines

---

## Annotation Visualization Components

### MultiPoseLandmarkListWithMaskAnnotation.cs
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/MultiPoseLandmarkListWithMaskAnnotation.cs`
- **Purpose:** Multi-pose rendering with segmentation mask support
- **Key Properties:** landmarkRadius (15.0), connectionColor, connectionWidth
- **Key Methods:** Draw(), ReadMask(), InitMask(), SetMaskThreshold()
- **Size:** ~187 lines

### PoseLandmarkListAnnotation.cs
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseLandmarkListAnnotation.cs`
- **Purpose:** Per-pose visualization with landmarks and skeleton connections
- **Key Features:**
  - 33 landmarks
  - 32 skeleton connections
  - Left/right color differentiation
  - BodyParts masking enum
- **Size:** ~307 lines

### PoseLandmarkListWithMaskAnnotation.cs
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseLandmarkListWithMaskAnnotation.cs`
- **Purpose:** Combines pose landmarks with mask overlay
- **Key Components:** PoseLandmarkListAnnotation + MaskOverlayAnnotation
- **Size:** ~71 lines

### AnnotationController.cs (Base)
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/AnnotationController.cs`
- **Purpose:** Abstract base class for all annotation controllers
- **Key Features:** Thread-safe rendering, isStale flag system, transformation support
- **Size:** ~113 lines

### HierarchicalAnnotation.cs (Base)
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/HierarchicalAnnotation.cs`
- **Purpose:** Base class for hierarchical GameObject-based annotations
- **Key Methods:** ActivateFor(), SetActive(), InstantiateChild()
- **Size:** ~97 lines

### ListAnnotation.cs (Base)
- **Full Path:** `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/ListAnnotation.cs`
- **Purpose:** Base class for list-based annotations (handles child instantiation)
- **Key Methods:** Fill(), CallActionForAll()
- **Size:** ~123 lines

---

## Sample Scene Implementation

### PoseLandmarkerRunner.cs
- **Full Path:** `/home/user/media-pipe-unity/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/PoseLandmarkerRunner.cs`
- **Purpose:** Main scene runner - orchestrates full pose detection pipeline
- **Key Features:**
  - Image source handling (Camera/File/Stream)
  - Texture pooling with async GPU readback
  - Three running modes support
  - Segmentation mask disposal
- **Key Methods:** Run() coroutine, OnPoseLandmarkDetectionOutput()
- **Size:** ~180 lines

### PoseLandmarkDetectionConfig.cs
- **Full Path:** `/home/user/media-pipe-unity/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/PoseLandmarkDetectionConfig.cs`
- **Purpose:** Configuration data holder
- **Key Properties:** Delegate, ImageReadMode, Model (Lite/Full/Heavy), RunningMode, confidence thresholds
- **Size:** ~75 lines

### PoseLandmarkDetectionConfigWindow.cs
- **Full Path:** `/home/user/media-pipe-unity/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/PoseLandmarkDetectionConfigWindow.cs`
- **Purpose:** UI configuration window
- **Key Components:** Dropdowns, InputFields, Toggles for all configuration options
- **Size:** ~171 lines

---

## Base Infrastructure

### VisionTaskApiRunner.cs
- **Full Path:** `/home/user/media-pipe-unity/Assets/MediaPipeUnity/Samples/Common/Scripts/VisionTaskApiRunner.cs`
- **Purpose:** Generic base class for vision task runners
- **Key Methods:** Play(), Pause(), Resume(), Stop(), Run()
- **Size:** ~61 lines

---

## Scene Assets

### Pose Landmark Detection.unity
- **Full Path:** `/home/user/media-pipe-unity/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/Pose Landmark Detection.unity`
- **Purpose:** Main sample scene
- **Components:** Canvas with Screen, Solution (PoseLandmarkerRunner), Annotation layers

### Pose Landmark Detection Config Window.prefab
- **Full Path:** `/home/user/media-pie-unity/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/Pose Landmark Detection Config Window.prefab`
- **Purpose:** Configuration UI prefab

---

## Summary Statistics

| Category | Files | Total Lines |
|----------|-------|-------------|
| Core Landmarker | 3 | ~504 |
| Data Structures | 1 | ~303 |
| Annotation Controllers | 3 | ~162 |
| Annotation Visualization | 5 | ~785 |
| Sample Scene | 3 | ~426 |
| Base Infrastructure | 2 | ~174 |
| **TOTAL** | **17** | **~2,354** |

---

## Quick Reference: Finding Specific Functionality

### How Pose Results are Processed:
1. **Capture** - PoseLandmarkerRunner.cs (line ~130-158)
2. **Detect** - PoseLandmarker.cs (line ~117-144)
3. **Result Container** - PoseLandmarkerResult.cs (line ~15-37)
4. **Annotation** - PoseLandmarkerResultAnnotationController.cs (line ~22-60)
5. **Rendering** - MultiPoseLandmarkListWithMaskAnnotation.cs (line ~105-114)

### Drawing Pipeline:
1. Result passed to controller via DrawNow() or DrawLater()
2. Multi-pose annotation processes each pose
3. Per-pose annotation draws landmarks + connections
4. MaskOverlayAnnotation renders segmentation (if enabled)
5. Canvas renders to screen

### Thread Safety:
- Lock object in PoseLandmarkerResultAnnotationController (line 17-18)
- Used in DrawNow() and UpdateCurrentTarget() methods
- SyncNow() calls happen on main thread only

### Memory Management:
- PoseLandmarkerResult.Alloc() pre-allocates buffers
- Segmentation masks explicitly disposed in DisposeAllMasks()
- TextureFramePool manages GPU/CPU buffers

### Confidence Thresholds:
All defined in PoseLandmarkerOptions.cs:
- minPoseDetectionConfidence (default 0.5)
- minPosePresenceConfidence (default 0.5)
- minTrackingConfidence (default 0.5)

### Landmark Indices:
Defined in PoseLandmarkListAnnotation.cs:
- _LeftLandmarks[] indices
- _RightLandmarks[] indices
- _Connections[] pairs
