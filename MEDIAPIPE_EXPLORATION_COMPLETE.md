# MediaPipe Unity Plugin - Pose Landmarker Comprehensive Exploration

## Overview
The MediaPipe Unity Plugin provides a complete pose landmarking system with real-time detection and visualization. This document covers all components, data structures, and processing pipelines.

---

## 1. CORE POSE LANDMARKER CLASSES

### 1.1 PoseLandmarker.cs
**Path:** `/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Vision/PoseLandmarker/PoseLandmarker.cs`

Main task API class for pose detection with three running modes:
- **IMAGE Mode**: Single image detection
- **VIDEO Mode**: Frame-by-frame video processing
- **LIVE_STREAM Mode**: Continuous stream with async callbacks

**Key Methods:**
```csharp
public static PoseLandmarker CreateFromModelPath(string modelPath, GpuResources gpuResources = null)
public static PoseLandmarker CreateFromOptions(PoseLandmarkerOptions options, GpuResources gpuResources = null)

// Image mode
public PoseLandmarkerResult Detect(Image image, Core.ImageProcessingOptions? imageProcessingOptions = null)
public bool TryDetect(Image image, Core.ImageProcessingOptions? imageProcessingOptions, ref PoseLandmarkerResult result)

// Video mode
public PoseLandmarkerResult DetectForVideo(Image image, long timestampMillisec, Core.ImageProcessingOptions? imageProcessingOptions = null)
public bool TryDetectForVideo(Image image, long timestampMillisec, Core.ImageProcessingOptions? imageProcessingOptions, ref PoseLandmarkerResult result)

// Live stream mode
public void DetectAsync(Image image, long timestampMillisec, Core.ImageProcessingOptions? imageProcessingOptions = null)
```

**Internal Streams:**
- IMAGE_IN / IMAGE_OUT: Input/output image streams
- NORM_RECT_IN: Normalized ROI rectangles
- NORM_LANDMARKS: Normalized pose landmarks (2D)
- WORLD_LANDMARKS: World-space 3D pose landmarks
- SEGMENTATION_MASK: Optional segmentation masks

### 1.2 PoseLandmarkerResult.cs
**Path:** `/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Vision/PoseLandmarker/PoseLandmarkerResult.cs`

**Data Structure:**
```csharp
public readonly struct PoseLandmarkerResult
{
    // Detected pose landmarks in normalized image coordinates
    public readonly List<NormalizedLandmarks> poseLandmarks;
    
    // Detected pose landmarks in world coordinates
    public readonly List<Landmarks> poseWorldLandmarks;
    
    // Optional segmentation masks for pose
    public readonly List<Image> segmentationMasks;
}
```

**Key Methods:**
- `Alloc(int capacity, bool outputSegmentationMasks)` - Pre-allocate result buffers
- `CloneTo(ref PoseLandmarkerResult destination)` - Copy results safely
- `ToString()` - Format result as JSON string

### 1.3 PoseLandmarkerOptions.cs
**Path:** `/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Vision/PoseLandmarker/PoseLandmarkerOptions.cs`

**Configuration Options:**
```csharp
public Tasks.Core.BaseOptions baseOptions { get; }
public Core.RunningMode runningMode { get; }
public int numPoses { get; }  // Default: 1
public float minPoseDetectionConfidence { get; }  // Default: 0.5f
public float minPosePresenceConfidence { get; }   // Default: 0.5f
public float minTrackingConfidence { get; }       // Default: 0.5f
public bool outputSegmentationMasks { get; }      // Default: false
public ResultCallback resultCallback { get; }     // For LIVE_STREAM mode
```

**Result Callback Signature:**
```csharp
public delegate void ResultCallback(
    PoseLandmarkerResult poseLandmarksResult,
    Image image,
    long timestampMillisec
);
```

---

## 2. DATA STRUCTURES - LANDMARKS

### 2.1 Landmark Structure
**Path:** `/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Components/Containers/Landmark.cs`

**3D World Landmarks (meters):**
```csharp
public readonly struct Landmark : IEquatable<Landmark>
{
    public readonly float x;              // X coordinate in meters
    public readonly float y;              // Y coordinate in meters
    public readonly float z;              // Z coordinate (depth, meters)
    public readonly float? visibility;    // Visibility score [0, 1]
    public readonly float? presence;      // Presence score [0, 1]
    public readonly string name;          // Landmark name
}
```

**Normalized 2D Landmarks:**
```csharp
public readonly struct NormalizedLandmark : IEquatable<NormalizedLandmark>
{
    public readonly float x;              // X [0, 1] normalized
    public readonly float y;              // Y [0, 1] normalized
    public readonly float z;              // Z coordinate
    public readonly float? visibility;    // Visibility score [0, 1]
    public readonly float? presence;      // Presence score [0, 1]
    public readonly string name;          // Landmark name
}
```

### 2.2 Landmark Collections
```csharp
public readonly struct Landmarks
{
    public readonly List<Landmark> landmarks;
}

public readonly struct NormalizedLandmarks
{
    public readonly List<NormalizedLandmark> landmarks;
}
```

### 2.3 Pose Landmark Indices (33 total)
The MediaPipe pose model detects 33 landmarks:
- **0**: Nose
- **1-10**: Face landmarks
- **11-22**: Arm and hand landmarks
- **23-28**: Torso and hip
- **29-32**: Leg landmarks

---

## 3. POSE LANDMARK ANNOTATION SYSTEM

### 3.1 PoseLandmarkerResultAnnotationController
**Path:** `/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseLandmarkerResultAnnotationController.cs`

Main controller for rendering pose results:
```csharp
public class PoseLandmarkerResultAnnotationController 
    : AnnotationController<MultiPoseLandmarkListWithMaskAnnotation>
{
    [SerializeField] private bool _visualizeZ = false;
    
    private readonly object _currentTargetLock = new object();
    private PoseLandmarkerResult _currentTarget;
    
    public void InitScreen(int maskWidth, int maskHeight);
    public void DrawNow(PoseLandmarkerResult target);
    public void DrawLater(PoseLandmarkerResult target);
    
    protected override void SyncNow();
}
```

**Features:**
- Thread-safe result handling with locks
- Support for segmentation mask visualization
- Immediate and deferred rendering modes
- Z-depth visualization option

### 3.2 Annotation Visualization Hierarchy

```
MultiPoseLandmarkListWithMaskAnnotation (ListAnnotation)
├── PoseLandmarkListWithMaskAnnotation (per pose)
│   ├── PoseLandmarkListAnnotation
│   │   ├── PointListAnnotation (landmarks)
│   │   └── ConnectionListAnnotation (skeleton)
│   └── MaskOverlayAnnotation (segmentation)
└── ... (multiple poses)
```

### 3.3 PoseLandmarkListAnnotation
**Path:** `/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseLandmarkListAnnotation.cs`

Renders individual pose with landmarks and connections:

```csharp
public sealed class PoseLandmarkListAnnotation : HierarchicalAnnotation
{
    private PointListAnnotation _landmarkListAnnotation;
    private ConnectionListAnnotation _connectionListAnnotation;
    
    // 33 pose landmarks
    private const int _LandmarkCount = 33;
    
    // Left body landmarks (green by default)
    private static readonly int[] _LeftLandmarks = { 1, 2, 3, 7, 9, 11, 13, 15, 17, 19, 21, 23, 25, 27, 29, 31 };
    
    // Right body landmarks (green by default)
    private static readonly int[] _RightLandmarks = { 4, 5, 6, 8, 10, 12, 14, 16, 18, 20, 22, 24, 26, 28, 30, 32 };
    
    // Skeleton connections (32 total)
    private static readonly List<(int, int)> _Connections = new List<(int, int)> {
        // Left Eye: (0,1), (1,2), (2,3), (3,7)
        // Right Eye: (0,4), (4,5), (5,6), (6,8)
        // Lips: (9,10)
        // Left Arm: (11,13), (13,15)
        // Left Hand: (15,17), (15,19), (15,21), (17,19)
        // Right Arm: (12,14), (14,16)
        // Right Hand: (16,18), (16,20), (16,22), (18,20)
        // Torso: (11,12), (12,24), (24,23), (23,11)
        // Left Leg: (23,25), (25,27), (27,29), (27,31), (29,31)
        // Right Leg: (24,26), (26,28), (28,30), (28,32), (30,32)
    };
    
    public void Draw(IReadOnlyList<mptcc.NormalizedLandmark> target, bool visualizeZ = false);
    public void SetLeftLandmarkColor(Color color);
    public void SetRightLandmarkColor(Color color);
    public void SetLandmarkRadius(float radius);
    public void SetConnectionColor(Color color);
    public void SetConnectionWidth(float width);
}
```

### 3.4 PoseWorldLandmarkListAnnotationController
**Path:** `/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseWorldLandmarkListAnnotationController.cs`

3D world-space visualization:
```csharp
public class PoseWorldLandmarkListAnnotationController 
    : AnnotationController<PoseLandmarkListAnnotation>
{
    [SerializeField] private float _hipHeightMeter = 0.9f;
    [SerializeField] private Vector3 _scale = new Vector3(100, 100, 100);
    [SerializeField] private bool _visualizeZ = true;
    
    public void Draw(IReadOnlyList<Landmark> target);
}
```

### 3.5 MultiPoseLandmarkListWithMaskAnnotation
**Path:** `/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/MultiPoseLandmarkListWithMaskAnnotation.cs`

Handles multiple poses with segmentation masks:

```csharp
public sealed class MultiPoseLandmarkListWithMaskAnnotation 
    : ListAnnotation<PoseLandmarkListWithMaskAnnotation>
{
    [SerializeField] private Color _leftLandmarkColor = Color.green;
    [SerializeField] private Color _rightLandmarkColor = Color.green;
    [SerializeField] private float _landmarkRadius = 15.0f;
    [SerializeField] private Color _connectionColor = Color.white;
    [SerializeField, Range(0, 1)] private float _connectionWidth = 1.0f;
    [SerializeField] private RawImage _screen;
    [SerializeField] private Texture2D _maskTexture;
    [SerializeField] private Color _color = Color.blue;
    [SerializeField, Range(0, 1)] private float _maskThreshold = 0.9f;
    
    public void InitMask(int width, int height);
    public void ReadMask(IReadOnlyList<Image> segmentationMasks, bool isMirrored = false);
    public void Draw(IReadOnlyList<mptcc.NormalizedLandmarks> targets, bool visualizeZ = false);
}
```

### 3.6 Base Annotation Controller
**Path:** `/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/AnnotationController.cs`

Abstract base for all annotation controllers:
```csharp
public abstract class AnnotationController<T> : MonoBehaviour where T : HierarchicalAnnotation
{
    [SerializeField] protected T annotation;
    protected bool isStale = false;
    
    public bool isMirrored { get; set; }
    public RotationAngle rotationAngle { get; set; }
    public Vector2Int imageSize { get; set; }
    
    protected abstract void SyncNow();
    protected void UpdateCurrentTarget<TValue>(TValue newTarget, ref TValue currentTarget);
}
```

---

## 4. SAMPLE SCENE IMPLEMENTATION

### 4.1 PoseLandmarkerRunner.cs
**Path:** `/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/PoseLandmarkerRunner.cs`

Main scene runner orchestrating the pose detection pipeline:

```csharp
public class PoseLandmarkerRunner : VisionTaskApiRunner<PoseLandmarker>
{
    [SerializeField] private PoseLandmarkerResultAnnotationController _poseLandmarkerResultAnnotationController;
    private Experimental.TextureFramePool _textureFramePool;
    public readonly PoseLandmarkDetectionConfig config = new PoseLandmarkDetectionConfig();
    
    protected override IEnumerator Run()
    {
        // 1. Initialize PoseLandmarker with options
        var options = config.GetPoseLandmarkerOptions(
            config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM 
                ? OnPoseLandmarkDetectionOutput 
                : null
        );
        taskApi = PoseLandmarker.CreateFromOptions(options, GpuManager.GpuResources);
        
        // 2. Setup image source
        var imageSource = ImageSourceProvider.ImageSource;
        yield return imageSource.Play();
        
        // 3. Initialize texture pool and annotation controller
        _textureFramePool = new Experimental.TextureFramePool(
            imageSource.textureWidth, 
            imageSource.textureHeight, 
            TextureFormat.RGBA32, 
            10
        );
        SetupAnnotationController(_poseLandmarkerResultAnnotationController, imageSource);
        _poseLandmarkerResultAnnotationController.InitScreen(
            imageSource.textureWidth, 
            imageSource.textureHeight
        );
        
        // 4. Main processing loop
        var result = PoseLandmarkerResult.Alloc(options.numPoses, options.outputSegmentationMasks);
        
        while (true)
        {
            // Get texture frame
            if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
                continue;
            
            // Build image based on read mode (GPU/CPU/CPUAsync)
            Image image = BuildImage(textureFrame, imageSource);
            
            // Process based on running mode
            switch (taskApi.runningMode)
            {
                case RunningMode.IMAGE:
                    if (taskApi.TryDetect(image, imageProcessingOptions, ref result))
                        _poseLandmarkerResultAnnotationController.DrawNow(result);
                    break;
                    
                case RunningMode.VIDEO:
                    if (taskApi.TryDetectForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions, ref result))
                        _poseLandmarkerResultAnnotationController.DrawNow(result);
                    break;
                    
                case RunningMode.LIVE_STREAM:
                    taskApi.DetectAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
                    break;
            }
            
            DisposeAllMasks(result);
        }
    }
    
    private void OnPoseLandmarkDetectionOutput(
        PoseLandmarkerResult result, 
        Image image, 
        long timestamp)
    {
        _poseLandmarkerResultAnnotationController.DrawLater(result);
        DisposeAllMasks(result);
    }
    
    private void DisposeAllMasks(PoseLandmarkerResult result)
    {
        if (result.segmentationMasks != null)
        {
            foreach (var mask in result.segmentationMasks)
                mask.Dispose();
        }
    }
}
```

**Image Read Modes:**
- **GPU**: Direct GPU texture copy (Android only)
- **CPU**: Synchronous CPU texture readback
- **CPUAsync**: Asynchronous GPU readback with AsyncGPUReadbackRequest

### 4.2 PoseLandmarkDetectionConfig.cs
**Path:** `/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/PoseLandmarkDetectionConfig.cs`

Configuration management:
```csharp
public class PoseLandmarkDetectionConfig
{
    public Tasks.Core.BaseOptions.Delegate Delegate { get; set; }  // CPU/GPU
    public ImageReadMode ImageReadMode { get; set; }               // GPU/CPU/CPUAsync
    public ModelType Model { get; set; }                           // Lite/Full/Heavy
    public Tasks.Vision.Core.RunningMode RunningMode { get; set; } // IMAGE/VIDEO/LIVE_STREAM
    
    public int NumPoses { get; set; } = 1;
    public float MinPoseDetectionConfidence { get; set; } = 0.5f;
    public float MinPosePresenceConfidence { get; set; } = 0.5f;
    public float MinTrackingConfidence { get; set; } = 0.5f;
    public bool OutputSegmentationMasks { get; set; } = false;
    
    public PoseLandmarkerOptions GetPoseLandmarkerOptions(
        PoseLandmarkerOptions.ResultCallback resultCallback = null);
}
```

**Available Models:**
- `pose_landmarker_lite.bytes` - Light model
- `pose_landmarker_full.bytes` - Full model
- `pose_landmarker_heavy.bytes` - Heavy model

### 4.3 PoseLandmarkDetectionConfigWindow.cs
**Path:** `/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/PoseLandmarkDetectionConfigWindow.cs`

UI for configuration:
- Delegate selector (CPU/GPU)
- Image read mode selector
- Model type selector
- Running mode selector
- Numeric inputs for confidence thresholds
- Toggle for segmentation masks

---

## 5. PROCESSING PIPELINE FLOW

### Complete Detection Flow:

```
Input Image
    ↓
ImageSource (Camera/File/Stream)
    ↓
TextureFramePool (Buffered frames)
    ↓
Image Building (GPU/CPU based on mode)
    ↓
PoseLandmarker Task
    ├─ Run MediaPipe graph
    ├─ Extract normalized landmarks
    ├─ Extract world landmarks
    └─ Extract segmentation masks (optional)
    ↓
PoseLandmarkerResult {
    poseLandmarks: List<NormalizedLandmarks>,
    poseWorldLandmarks: List<Landmarks>,
    segmentationMasks: List<Image>?
}
    ↓
PoseLandmarkerResultAnnotationController
    ├─ DrawNow() - immediate render
    └─ DrawLater() - deferred render
    ↓
MultiPoseLandmarkListWithMaskAnnotation
    ├─ For each pose:
    │   ├─ PoseLandmarkListAnnotation
    │   │   ├─ Draw 33 landmarks (PointListAnnotation)
    │   │   └─ Draw 32 connections (ConnectionListAnnotation)
    │   └─ MaskOverlayAnnotation (if enabled)
    ↓
Unity Canvas/Screen Rendering
```

### Thread Safety:
- Results locked during updates
- Clone operation copies data safely
- Segmentation masks cleared after visualization

---

## 6. VISUALIZATION CONFIGURATION

### Color Scheme:
- **Left landmarks**: Green (configurable)
- **Right landmarks**: Green (configurable)
- **Connections**: White (configurable)
- **Landmark radius**: 15.0 units (configurable)
- **Connection width**: 1.0 (configurable)

### 3D World Visualization:
- Hip height offset: 0.9m
- Scale factors: (100, 100, 100) for world coordinates
- Z-depth visualization: Enabled for 3D view

### Segmentation Mask:
- Threshold: 0.9 (configurable)
- Color: Blue (configurable)
- RawImage display on screen

---

## 7. SCENE STRUCTURE

### Scene: "Pose Landmark Detection.unity"
**Location:** `/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/`

**Hierarchy:**
```
Canvas
├── Screen (RawImage for video/mask)
├── Solution (PoseLandmarkerRunner)
│   └── AnnotationLayer
│       └── RootAnnotation (MultiPoseLandmarkListWithMaskAnnotation)
│           └── PoseLandmarkListWithMaskAnnotation (per pose, child prefabs)
└── ModalCanvas
    └── Config Modal (PoseLandmarkDetectionConfigWindow)
```

### Prefabs:
- **Pose Landmark Detection Config Window.prefab** - Configuration UI
- **PoseLandmarkListWithMaskAnnotation** - Per-pose rendering (instantiated)

---

## 8. KEY ENUMERATIONS

### BodyParts Flags (for visualization masking):
```csharp
[Flags]
public enum BodyParts : short
{
    None = 0,
    Face = 1,
    LeftArm = 4,
    LeftHand = 8,
    RightArm = 16,
    RightHand = 32,
    LowerBody = 64,
    All = 127,
}
```

### Running Modes:
```csharp
public enum RunningMode
{
    IMAGE = 0,      // Single frame
    VIDEO = 1,      // Sequential frames with timestamps
    LIVE_STREAM = 2 // Async with callback
}
```

### Image Read Modes:
```csharp
public enum ImageReadMode
{
    GPU = 0,        // GPU texture copy
    CPU = 1,        // Synchronous CPU readback
    CPUAsync = 2    // Asynchronous GPU readback
}
```

---

## 9. IMPORTANT NOTES

### Memory Management:
- Pre-allocated result buffers via `PoseLandmarkerResult.Alloc()`
- Segmentation masks must be explicitly disposed
- TextureFramePool manages texture memory

### Performance Considerations:
- GPU mode only on Android with OpenGL ES3
- Async GPU readback reduces frame drops
- Live stream mode auto-drops frames for latency
- Normalized landmarks (2D) are primary output
- World landmarks provide 3D position

### Confidence Thresholds:
- **minPoseDetectionConfidence**: Initial detection threshold
- **minPosePresenceConfidence**: Landmark detection threshold
- **minTrackingConfidence**: Frame-to-frame tracking threshold

### Segmentation:
- Per-landmark segmentation masks
- Optional output (improves performance when disabled)
- Threshold-based visualization
- Disposed after rendering to save memory

