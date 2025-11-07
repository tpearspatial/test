# MediaPipe Unity Plugin - Pose Landmarker Complete Exploration Index

This comprehensive exploration contains the complete source code analysis and documentation of the MediaPipe Unity Plugin's Pose Landmarker system.

## Documents Created

### 1. MEDIAPIPE_EXPLORATION_COMPLETE.md (20KB)
**Comprehensive technical documentation covering:**
- Core PoseLandmarker classes (PoseLandmarker, PoseLandmarkerResult, PoseLandmarkerOptions)
- Complete data structures (Landmark, NormalizedLandmark, Landmarks, NormalizedLandmarks)
- Pose Landmark Annotation System with complete controller hierarchy
- Sample scene implementation details
- Complete processing pipeline
- Visualization configuration
- Scene structure and prefabs
- Key enumerations
- Important implementation notes

**Use this for:** Understanding the complete architecture and all components

---

### 2. KEY_SOURCE_FILES.md (8KB)
**Reference guide with file paths and purposes:**
- All 17 core source files with absolute paths
- File purposes and responsibilities
- Key methods and properties for each file
- Line counts and statistics
- Quick reference sections for:
  - How results are processed
  - Drawing pipeline
  - Thread safety
  - Memory management
  - Confidence thresholds

**Use this for:** Quickly locating specific files and understanding what they do

---

### 3. DATA_FLOW_AND_ARCHITECTURE.md (15KB)
**Visual diagrams and flow charts:**
- Complete data processing pipeline (box diagrams)
- Class hierarchy diagram
- Thread safety and synchronization patterns
- Landmark skeleton connection map (33 landmarks, 32 connections)
- Configuration and execution flow
- Memory and resource management
- Drawing synchronization pattern
- Configuration models enumeration
- Landmark visibility and presence scores
- Segmentation mask processing

**Use this for:** Understanding system architecture and data flows visually

---

## Core Source Files (with full content available)

### Detection Core
1. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Vision/PoseLandmarker/PoseLandmarker.cs` (297 lines)
   - Main pose detection API
   - 3 running modes: IMAGE, VIDEO, LIVE_STREAM
   - Key methods: Detect(), DetectForVideo(), DetectAsync()

2. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Vision/PoseLandmarker/PoseLandmarkerResult.cs` (84 lines)
   - Result data structure
   - Contains: poseLandmarks, poseWorldLandmarks, segmentationMasks
   - Methods: Alloc(), CloneTo()

3. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Vision/PoseLandmarker/PoseLandmarkerOptions.cs` (123 lines)
   - Configuration options
   - Confidence thresholds
   - Running mode selection
   - ResultCallback definition

### Data Structures
4. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Tasks/Components/Containers/Landmark.cs` (303 lines)
   - Landmark struct (3D, meters)
   - NormalizedLandmark struct (2D, [0,1])
   - Landmarks and NormalizedLandmarks collections

### Annotation Controllers
5. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseLandmarkerResultAnnotationController.cs` (62 lines)
   - Main result controller
   - Thread-safe result handling
   - DrawNow() and DrawLater() methods

6. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseLandmarkListAnnotationController.cs` (46 lines)
   - 2D landmark visualization controller
   - Simple landmark list handling

7. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseWorldLandmarkListAnnotationController.cs` (54 lines)
   - 3D world-space visualization
   - Hip height offset and scaling

### Annotation Visualization
8. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/MultiPoseLandmarkListWithMaskAnnotation.cs` (187 lines)
   - Multi-pose rendering with segmentation
   - Landmark coloring and sizing
   - Mask overlay management

9. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseLandmarkListAnnotation.cs` (307 lines)
   - Per-pose visualization
   - 33 landmarks with left/right coloring
   - 32 skeleton connections
   - Body parts masking

10. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/PoseLandmarkListWithMaskAnnotation.cs` (71 lines)
    - Combines landmarks with mask overlay
    - Delegates to child components

11. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/AnnotationController.cs` (113 lines)
    - Abstract base for all controllers
    - Thread-safe rendering system
    - Stale flag mechanism

12. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/HierarchicalAnnotation.cs` (97 lines)
    - Base for GameObject-based annotations
    - Hierarchy management
    - Transformation support

13. `/home/user/media-pipe-unity/Packages/com.github.homuler.mediapipe/Runtime/Scripts/Unity/Annotation/ListAnnotation.cs` (123 lines)
    - Base for list-based annotations
    - Child management
    - Fill and CallActionForAll methods

### Sample Scene Implementation
14. `/home/user/media-pipe-unity/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/PoseLandmarkerRunner.cs` (180 lines)
    - Main scene runner
    - Image acquisition and processing
    - Texture pooling with async GPU readback
    - All running modes support
    - Segmentation mask handling

15. `/home/user/media-pipe-unity/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/PoseLandmarkDetectionConfig.cs` (75 lines)
    - Configuration data holder
    - Model selection (Lite/Full/Heavy)
    - Running mode and image read mode selection

16. `/home/user/media-pipe-unity/Assets/MediaPipeUnity/Samples/Scenes/Pose Landmark Detection/PoseLandmarkDetectionConfigWindow.cs` (171 lines)
    - UI configuration window
    - Dropdowns, input fields, toggles
    - Dynamic option management

### Base Infrastructure
17. `/home/user/media-pipe-unity/Assets/MediaPipeUnity/Samples/Common/Scripts/VisionTaskApiRunner.cs` (61 lines)
    - Generic base for vision task runners
    - Play, Pause, Resume, Stop control
    - Coroutine-based execution

---

## Key Concepts Explained

### Running Modes
- **IMAGE**: Single frame, synchronous detection
- **VIDEO**: Frame sequence with timestamps
- **LIVE_STREAM**: Continuous async with optional frame dropping

### Image Read Modes
- **GPU**: Direct GPU copy (Android + OpenGL ES3 only)
- **CPU**: Synchronous GPU→CPU readback
- **CPUAsync**: Asynchronous readback (default, best performance)

### Data Flow
1. Image source provides frames
2. TextureFramePool buffers them (10 frames)
3. PoseLandmarkerRunner reads frame (GPU/CPU/CPUAsync)
4. PoseLandmarker detects (returns PoseLandmarkerResult)
5. PoseLandmarkerResultAnnotationController processes result
6. MultiPoseLandmarkListWithMaskAnnotation visualizes
7. Unity Canvas renders to screen

### Thread Safety
- Lock object protects result updates
- CloneTo() copies data safely between threads
- isStale flag triggers main-thread updates
- SyncNow() always called from main thread

### Memory Management
- Results pre-allocated via Alloc()
- Segmentation masks explicitly disposed
- TextureFramePool manages GPU/CPU textures
- No automatic cleanup - manual Dispose() required

### Visualization
- 33 landmarks (pose joints)
- 32 skeleton connections (limbs)
- Left/right body coloring
- Optional segmentation mask overlay
- Z-depth visualization support

---

## How to Use These Documents

### For Understanding the System:
1. Start with **DATA_FLOW_AND_ARCHITECTURE.md** - Get visual overview
2. Read **MEDIAPIPE_EXPLORATION_COMPLETE.md** - Deep technical understanding
3. Reference **KEY_SOURCE_FILES.md** - For specific file locations

### For Implementation:
1. Check **KEY_SOURCE_FILES.md** for file paths
2. Look up specific components in **MEDIAPIPE_EXPLORATION_COMPLETE.md**
3. Review threading pattern in **DATA_FLOW_AND_ARCHITECTURE.md**

### For Debugging:
1. Check thread safety section in **DATA_FLOW_AND_ARCHITECTURE.md**
2. Review memory management notes
3. Look at drawing synchronization pattern

### For Performance Optimization:
1. Review image read modes in **DATA_FLOW_AND_ARCHITECTURE.md**
2. Check memory management section
3. Look at running mode trade-offs

---

## File Statistics

| Category | Files | Lines | Purpose |
|----------|-------|-------|---------|
| Detection Core | 3 | 504 | Main PoseLandmarker task |
| Data Structures | 1 | 303 | Landmark definitions |
| Controllers | 3 | 162 | Result handling |
| Visualization | 5 | 785 | Drawing and rendering |
| Sample Scene | 3 | 426 | Complete example |
| Infrastructure | 2 | 174 | Base classes |
| **TOTAL** | **17** | **~2,354** | Complete system |

---

## Important Notes

### Confidence Thresholds
All default to 0.5 (50%):
- `minPoseDetectionConfidence`: Person detection threshold
- `minPosePresenceConfidence`: Landmark detection threshold
- `minTrackingConfidence`: Frame-to-frame tracking threshold

### Landmark Indices (0-32)
- 0: Nose
- 1-10: Face
- 11-22: Arms and hands
- 23-28: Torso and hips
- 29-32: Legs

### Segmentation Masks
- Optional (improves performance if disabled)
- Per-pose masks
- Threshold-based visualization (default 0.9)
- Explicitly disposed after rendering

### Running on Different Platforms
- **Android**: Can use GPU mode with OpenGL ES3
- **Editor/Standalone**: CPU or CPUAsync mode
- **Performance**: CPUAsync provides best balance

---

## Repository Information

**Source:** https://github.com/homuler/MediaPipeUnityPlugin

**Clone Location:** `/home/user/media-pipe-unity`

**License:** MIT (homuler/MediaPipeUnityPlugin)

---

## Document Sizes

- MEDIAPIPE_EXPLORATION_COMPLETE.md: ~20KB
- KEY_SOURCE_FILES.md: ~8KB
- DATA_FLOW_AND_ARCHITECTURE.md: ~15KB
- INDEX.md (this file): ~7KB
- **Total Documentation: ~50KB**

---

## Quick Links to Key Sections

**In MEDIAPIPE_EXPLORATION_COMPLETE.md:**
- Sections 1-3: Core classes and data structures
- Sections 4-5: How the system works
- Sections 6-9: Configuration and notes

**In KEY_SOURCE_FILES.md:**
- File paths for all 17 core files
- Quick reference sections at bottom

**In DATA_FLOW_AND_ARCHITECTURE.md:**
- Complete pipeline diagram (Section 1)
- Thread safety pattern (Section 3)
- Skeleton connections (Section 4)
- Segmentation processing (Section 10)

