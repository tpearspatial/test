# MediaPipe Unity Plugin - Data Flow and Architecture Diagrams

## 1. Complete Data Processing Pipeline

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         IMAGE ACQUISITION LAYER                             │
├─────────────────────────────────────────────────────────────────────────────┤
│  ImageSource (Camera/File/Stream)                                           │
│    ↓                                                                         │
│  Get Current Texture (RGBA32)                                               │
│    ↓                                                                         │
│  TextureFramePool (10 buffered frames)                                      │
└─────────────────────────────────────────────────────────────────────────────┘
                                   ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                      IMAGE READING LAYER (PoseLandmarkerRunner)             │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ ImageReadMode Selection:                                             │ │
│  │                                                                        │ │
│  │  GPU Mode (Android + OpenGL ES3)                                     │ │
│  │    └─ textureFrame.ReadTextureOnGPU()                                │ │
│  │       └─ textureFrame.BuildGPUImage()                                │ │
│  │       └─ WaitForEndOfFrame                                           │ │
│  │                                                                        │ │
│  │  CPU Mode                                                             │ │
│  │    └─ textureFrame.ReadTextureOnCPU()                                │ │
│  │       └─ textureFrame.BuildCPUImage()                                │ │
│  │       └─ textureFrame.Release()                                      │ │
│  │                                                                        │ │
│  │  CPUAsync Mode (Default)                                             │ │
│  │    └─ textureFrame.ReadTextureAsync()                                │ │
│  │       └─ AsyncGPUReadbackRequest.done (wait)                         │ │
│  │       └─ textureFrame.BuildCPUImage()                                │ │
│  │       └─ textureFrame.Release()                                      │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│    Output: Image (MediaPipe native format)                                 │
└─────────────────────────────────────────────────────────────────────────────┘
                                   ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                      POSE DETECTION LAYER (PoseLandmarker)                  │
├─────────────────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────────────────────────┐ │
│  │ Running Mode Selection:                                              │ │
│  │                                                                        │ │
│  │  IMAGE Mode:                                                         │ │
│  │    └─ TryDetect(image, options)                                      │ │
│  │       └─ ProcessImageData() (synchronous)                            │ │
│  │       └─ Return: PoseLandmarkerResult immediately                    │ │
│  │                                                                        │ │
│  │  VIDEO Mode:                                                         │ │
│  │    └─ TryDetectForVideo(image, timestamp, options)                   │ │
│  │       └─ ProcessVideoData() (timestamped, sequential)                │ │
│  │       └─ Return: PoseLandmarkerResult immediately                    │ │
│  │                                                                        │ │
│  │  LIVE_STREAM Mode:                                                   │ │
│  │    └─ DetectAsync(image, timestamp, options)                         │ │
│  │       └─ SendLiveStreamData() (non-blocking, may drop frames)        │ │
│  │       └─ Return: void (async via ResultCallback)                     │ │
│  │       └─ Result arrives in OnPoseLandmarkDetectionOutput()          │ │
│  └────────────────────────────────────────────────────────────────────────┘ │
│                                                                              │
│  Internal Graph Streams:                                                    │
│    ├─ IMAGE_IN → IMAGE_OUT                                                 │
│    ├─ NORM_RECT_IN                                                          │
│    ├─ NORM_LANDMARKS → normalized 2D landmarks                             │
│    ├─ WORLD_LANDMARKS → 3D landmarks                                       │
│    └─ SEGMENTATION_MASK → optional                                         │
│                                                                              │
│  Output: PoseLandmarkerResult {                                             │
│    poseLandmarks: List<NormalizedLandmarks>,                               │
│    poseWorldLandmarks: List<Landmarks>,                                    │
│    segmentationMasks: List<Image>?                                         │
│  }                                                                           │
└─────────────────────────────────────────────────────────────────────────────┘
                                   ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                   RESULT ANNOTATION LAYER (Controllers)                     │
├─────────────────────────────────────────────────────────────────────────────┤
│  PoseLandmarkerResultAnnotationController                                   │
│    ├─ Thread-safe lock (_currentTargetLock)                                │
│    ├─ Clone result data (CloneTo)                                          │
│    ├─ ReadMask (if segmentation enabled)                                   │
│    └─ DrawNow() or DrawLater()                                             │
│       ├─ SyncNow() → annotation.Draw()                                     │
│       └─ isStale flag → LateUpdate sync                                    │
└─────────────────────────────────────────────────────────────────────────────┘
                                   ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│              VISUALIZATION RENDERING (Annotation Components)                │
├─────────────────────────────────────────────────────────────────────────────┤
│  MultiPoseLandmarkListWithMaskAnnotation (ListAnnotation)                  │
│    └─ For each pose in results:                                            │
│       └─ PoseLandmarkListWithMaskAnnotation                               │
│          ├─ PoseLandmarkListAnnotation                                     │
│          │  ├─ PointListAnnotation (33 landmarks)                         │
│          │  │  ├─ Left landmarks: indices [1,2,3,7,9,11,13,15,17,19...] │
│          │  │  ├─ Right landmarks: indices [4,5,6,8,10,12,14,16,18,20..]│
│          │  │  └─ Colors: green, radius: 15.0                             │
│          │  └─ ConnectionListAnnotation (32 connections)                  │
│          │     ├─ Eye, mouth, arms, hands, torso, legs                    │
│          │     ├─ Color: white, width: 1.0                                │
│          │     └─ Drawn via (int, int) pairs                              │
│          └─ MaskOverlayAnnotation (if enabled)                            │
│             ├─ RawImage screen overlay                                     │
│             ├─ Color: blue, threshold: 0.9                                │
│             └─ Per-landmark segmentation masks                            │
└─────────────────────────────────────────────────────────────────────────────┘
                                   ↓
┌─────────────────────────────────────────────────────────────────────────────┐
│                      UNITY RENDERING (Canvas/Screen)                        │
├─────────────────────────────────────────────────────────────────────────────┤
│  Canvas                                                                      │
│    ├─ Screen (RawImage)                                                     │
│    ├─ AnnotationLayer                                                       │
│    │  └─ RootAnnotation (MultiPoseLandmarkListWithMaskAnnotation)         │
│    │     └─ Child instances of PoseLandmarkListWithMaskAnnotation         │
│    └─ ModalCanvas                                                          │
│       └─ Config Window (PoseLandmarkDetectionConfigWindow)                │
│                                                                              │
│  Output to Screen/Camera                                                    │
└─────────────────────────────────────────────────────────────────────────────┘
```

---

## 2. Class Hierarchy Diagram

```
HierarchicalAnnotation
├── ListAnnotation<T>
│   └── MultiPoseLandmarkListWithMaskAnnotation
│       └── [Contains List<PoseLandmarkListWithMaskAnnotation>]
│
├── PoseLandmarkListAnnotation
│   ├── PointListAnnotation (for landmarks)
│   └── ConnectionListAnnotation (for skeleton)
│
├── PoseLandmarkListWithMaskAnnotation
│   ├── PoseLandmarkListAnnotation
│   └── MaskOverlayAnnotation
│
└── PoseWorldLandmarkListAnnotation

AnnotationController<T>
├── PoseLandmarkerResultAnnotationController
│   └── Generic<MultiPoseLandmarkListWithMaskAnnotation>
│
├── PoseLandmarkListAnnotationController
│   └── Generic<PoseLandmarkListAnnotation>
│
└── PoseWorldLandmarkListAnnotationController
    └── Generic<PoseLandmarkListAnnotation>

VisionTaskApiRunner<TTask>
└── PoseLandmarkerRunner
    └── Generic<PoseLandmarker>

struct PoseLandmarkerResult
├── List<NormalizedLandmarks> poseLandmarks
├── List<Landmarks> poseWorldLandmarks
└── List<Image> segmentationMasks?

struct NormalizedLandmarks
└── List<NormalizedLandmark> landmarks

struct Landmarks
└── List<Landmark> landmarks

struct NormalizedLandmark
├── float x [0, 1]
├── float y [0, 1]
├── float z
├── float? visibility
├── float? presence
└── string name

struct Landmark
├── float x (meters)
├── float y (meters)
├── float z (meters, depth)
├── float? visibility
├── float? presence
└── string name
```

---

## 3. Thread Safety and Synchronization

```
Thread 1: Detection Thread              Thread 2: Render Thread (Main)
─────────────────────────────────────────────────────────────────────────

Image → PoseLandmarker.Detect()
          │
          ├─ [ASYNC CALLBACK]
          └─ OnPoseLandmarkDetectionOutput()
             │
             ├─ Lock(_currentTargetLock) ────────┐
             │                                   │
             ├─ PoseLandmarkerResult.CloneTo()   │
             │ (copy data safely)                │
             │                                   │
             ├─ PoseLandmarkerResult            │ Render Thread
             │ .segmentationMasks.Clear()        │ waiting on lock
             │                                   │
             └─ UpdateCurrentTarget()            │ Lock acquired
                ├─ CloneTo()                     │
                ├─ ReadMask()                    │
                └─ isStale = true ───────────────┼──> LateUpdate()
                                                 │
                └─ Unlock ───────────────────────┤
                                                 │
                                          if (isStale) {
                                            SyncNow()
                                            annotation.Draw()
                                          }
```

---

## 4. Landmark Skeleton Connection Map

33 Landmarks → 32 Connections

```
                          0 (Nose)
                         / \
                        /   \
              1(LEye) -2-3-7  4(REye)-5-6-8
                      |        |
                      |        |
                   9-10(Mouth)
                      
              11(LShoulder)-12(RShoulder)
              /              \
             13-15-17        14-16-18
           (LElbow-LWrist)  (RElbow-RWrist)
               /|\              /|\
              / | \            / | \
            19  21 (hand)     20 22 (hand)

            23(LHip)-24(RHip)
              |         |
             25        26
              |         |
             27        28
            / \        / \
           29  31     30  32
```

**Connection Pairs (32 total):**
```
Eyes: (0,1), (1,2), (2,3), (3,7), (0,4), (4,5), (5,6), (6,8)
Mouth: (9,10)
Arms: (11,13), (13,15), (12,14), (14,16)
Hands: (15,17), (15,19), (15,21), (17,19), (16,18), (16,20), (16,22), (18,20)
Torso: (11,12), (12,24), (24,23), (23,11)
Legs: (23,25), (25,27), (27,29), (27,31), (29,31), (24,26), (26,28), (28,30), (28,32), (30,32)
```

---

## 5. Configuration and Execution Flow

```
PoseLandmarkDetectionConfigWindow
├─ User Input (UI Dropdowns/InputFields)
│  ├─ Delegate: CPU or GPU
│  ├─ ImageReadMode: GPU, CPU, or CPUAsync
│  ├─ Model: Lite, Full, or Heavy
│  ├─ RunningMode: IMAGE, VIDEO, or LIVE_STREAM
│  ├─ NumPoses: Integer (1-n)
│  ├─ MinPoseDetectionConfidence: Float [0, 1]
│  ├─ MinPosePresenceConfidence: Float [0, 1]
│  ├─ MinTrackingConfidence: Float [0, 1]
│  └─ OutputSegmentationMasks: Boolean
│
└─ Stored in PoseLandmarkDetectionConfig
   └─ PoseLandmarkerRunner.config
      └─ Run() coroutine
         ├─ config.GetPoseLandmarkerOptions()
         │  └─ PoseLandmarker.CreateFromOptions()
         │     ├─ new PoseLandmarkerOptions(
         │     │   baseOptions,
         │     │   runningMode,
         │     │   numPoses,
         │     │   minPoseDetectionConfidence,
         │     │   minPosePresenceConfidence,
         │     │   minTrackingConfidence,
         │     │   outputSegmentationMasks,
         │     │   resultCallback (LIVE_STREAM only)
         │     │ )
         │     └─ Build CalculatorGraph
         │
         └─ MainLoop: while(true)
            ├─ Acquire Image
            ├─ Branch on RunningMode:
            │  ├─ IMAGE: TryDetect() → DrawNow()
            │  ├─ VIDEO: TryDetectForVideo() → DrawNow()
            │  └─ LIVE_STREAM: DetectAsync() → Callback → DrawLater()
            └─ DisposeAllMasks()
```

---

## 6. Memory and Resource Management

```
Initialization
├─ TextureFramePool (10 frames)
│  └─ Pre-allocated GPU/CPU texture buffers
│
└─ PoseLandmarkerResult.Alloc(numPoses, outputSegmentationMasks)
   ├─ Pre-allocate List<NormalizedLandmarks>
   ├─ Pre-allocate List<Landmarks>
   └─ Pre-allocate List<Image> (if masks enabled)

Per-Frame
├─ Get TextureFrame from pool
├─ Build Image
├─ Detect (reuses result buffer via CloneTo)
├─ ReadMask (if enabled)
└─ DisposeAllMasks (explicit cleanup)
   └─ foreach mask in segmentationMasks
      └─ mask.Dispose()

Shutdown
├─ _textureFramePool?.Dispose()
├─ taskApi?.Close()
└─ Annotation?.Destroy()
```

---

## 7. Drawing Synchronization Pattern

```
Frame N-1                          Frame N                           Frame N+1
─────────────────────────────────────────────────────────────────────────────

                              DrawNow(result)
                                    ↓
                           CloneTo (lock held)
                                    ↓
                           ReadMask (lock held)
                                    ↓
                           SyncNow() (immediate)
                                    ↓
                           annotation.Draw()
                                    ↓
                           isStale = false
                                    ↓
Canvas.Render()  ◄─────────────────────────────────────────────────────────

                                  OR

                              DrawLater(result)
                                    ↓
                          UpdateCurrentTarget()
                           CloneTo (lock held)
                                    ↓
                           ReadMask (lock held)
                                    ↓
                           isStale = true
                                    ↓
                         (locked, release)
                                    ↓
                          LateUpdate() {
                            if (isStale) {
                              SyncNow()
                              annotation.Draw()
                            }
                          }
                                    ↓
Canvas.Render()  ◄──────────────────────────────────────────────────
```

---

## 8. Configuration Models

```
ModelType Enumeration:
├─ BlazePoseLite (0)
│  └─ "pose_landmarker_lite.bytes"
│     └─ Fastest, least accurate
│
├─ BlazePoseFull (1) [DEFAULT]
│  └─ "pose_landmarker_full.bytes"
│     └─ Balanced performance/accuracy
│
└─ BlazePoseHeavy (2)
   └─ "pose_landmarker_heavy.bytes"
      └─ Slowest, most accurate

RunningMode Enumeration:
├─ IMAGE (0)
│  └─ Single frame, synchronous
│  └─ Best for: still images, screenshots
│
├─ VIDEO (1)
│  └─ Frame sequence with timestamps
│  └─ Best for: video files, recorded footage
│
└─ LIVE_STREAM (2) [DEFAULT FOR SAMPLE]
   └─ Continuous async with frame dropping
   └─ Best for: camera feed, real-time interaction

ImageReadMode Enumeration:
├─ GPU (0)
│  └─ Direct GPU copy
│  └─ Best for: Android with OpenGL ES3
│
├─ CPU (1)
│  └─ Sync GPU→CPU readback
│  └─ Best for: Editor, full synchronous
│
└─ CPUAsync (2) [DEFAULT]
   └─ Async GPU readback
   └─ Best for: balanced performance
```

---

## 9. Landmark Visibility and Presence

```
Each landmark has optional confidence scores:

visibility: [0.0, 1.0]
├─ 0.0 = completely hidden/occluded
├─ 0.5 = partially visible
└─ 1.0 = fully visible

presence: [0.0, 1.0]
├─ 0.0 = not present in scene
├─ 0.5 = partially in bounds
└─ 1.0 = fully within bounds

Default confidence thresholds:
├─ minPoseDetectionConfidence = 0.5
│  └─ Person must be detected with > 50% confidence
│
├─ minPosePresenceConfidence = 0.5
│  └─ Individual landmarks must be present with > 50% confidence
│
└─ minTrackingConfidence = 0.5
   └─ Frame-to-frame tracking must be > 50% confident
```

---

## 10. Segmentation Mask Processing

```
Optional segmentation per pose:

PoseLandmarkerResult.segmentationMasks
├─ List<Image> (one per pose)
│  └─ Each Image is a mask texture
│     ├─ Size: imageWidth × imageHeight
│     ├─ Format: Grayscale (0-255)
│     └─ Value: per-pixel person probability
│
PoseLandmarkerResultAnnotationController
├─ InitScreen(maskWidth, maskHeight)
│  └─ Setup MaskOverlayAnnotation dimensions
│
├─ ReadMask(List<Image> masks, isMirrored)
│  └─ Read GPU Image → Texture2D
│     ├─ Handle mirroring if needed
│     └─ Update display texture
│
└─ Draw()
   └─ MultiPoseLandmarkListWithMaskAnnotation.Draw()
      └─ For each pose:
         └─ MaskOverlayAnnotation.Draw()
            ├─ Apply threshold (0.9 default)
            ├─ Blend with color (blue default)
            └─ Display on RawImage (screen overlay)

Cleanup:
└─ DisposeAllMasks(result)
   └─ foreach mask in segmentationMasks
      └─ mask.Dispose() // Release GPU memory
```

