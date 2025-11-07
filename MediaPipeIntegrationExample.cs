using UnityEngine;
using Mediapipe.Tasks.Vision.PoseLandmarker;
using UnityEngine.UI;

/// <summary>
/// Example script showing how to integrate MediaPipe with SMPLX using the provided components.
/// This demonstrates a complete working setup for live camera tracking.
/// </summary>
public class MediaPipeIntegrationExample : MonoBehaviour
{
    #region Inspector Configuration

    [Header("References")]
    [Tooltip("Reference to the MediaPipe SMPLX Controller")]
    public MediaPipeSMPLXController smplxController;

    [Tooltip("Reference to the camera or video source")]
    public WebCamTexture webcamTexture;

    [Tooltip("UI RawImage to display camera feed")]
    public RawImage cameraDisplay;

    [Header("MediaPipe Settings")]
    [Tooltip("Path to the MediaPipe pose landmarker model")]
    public string modelPath = "pose_landmarker.task";

    [Tooltip("Running mode for pose detection")]
    public RunningMode runningMode = RunningMode.LIVE_STREAM;

    [Tooltip("Target frame rate for pose detection")]
    [Range(15, 60)]
    public int targetFrameRate = 30;

    [Header("Camera Settings")]
    [Tooltip("Name of the webcam device (leave empty for default)")]
    public string webcamDeviceName = "";

    [Tooltip("Camera resolution width")]
    public int cameraWidth = 1280;

    [Tooltip("Camera resolution height")]
    public int cameraHeight = 720;

    #endregion

    #region Private Fields

    private PoseLandmarker poseLandmarker;
    private PoseLandmarkerOptions options;
    private Texture2D frameTexture;
    private float lastDetectionTime;
    private float detectionInterval;
    private long frameTimestamp;
    private bool isInitialized = false;

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        InitializeMediaPipe();
        InitializeCamera();
    }

    void Update()
    {
        if (!isInitialized || webcamTexture == null || !webcamTexture.isPlaying)
            return;

        // Check if it's time for next detection
        float timeSinceLastDetection = Time.time - lastDetectionTime;
        if (timeSinceLastDetection < detectionInterval)
            return;

        // Process current camera frame
        ProcessCameraFrame();

        lastDetectionTime = Time.time;
    }

    void OnDestroy()
    {
        // Cleanup
        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }

        if (poseLandmarker != null)
        {
            poseLandmarker.Dispose();
        }
    }

    #endregion

    #region Initialization

    private void InitializeMediaPipe()
    {
        try
        {
            // Configure MediaPipe options
            options = new PoseLandmarkerOptions
            {
                baseOptions = new BaseOptions
                {
                    modelAssetPath = modelPath
                },
                runningMode = runningMode,
                numPoses = 1,
                minPoseDetectionConfidence = 0.5f,
                minPosePresenceConfidence = 0.5f,
                minTrackingConfidence = 0.5f,
                outputSegmentationMasks = false
            };

            // Create pose landmarker
            poseLandmarker = PoseLandmarker.CreateFromOptions(options);

            // Calculate detection interval
            detectionInterval = 1f / targetFrameRate;

            Debug.Log("MediaPipe initialized successfully");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize MediaPipe: {e.Message}");
            return;
        }

        isInitialized = true;
    }

    private void InitializeCamera()
    {
        try
        {
            // Get available webcam devices
            WebCamDevice[] devices = WebCamTexture.devices;

            if (devices.Length == 0)
            {
                Debug.LogError("No webcam devices found!");
                return;
            }

            // Select webcam
            string deviceName = string.IsNullOrEmpty(webcamDeviceName) ?
                               devices[0].name : webcamDeviceName;

            // Create and start webcam texture
            webcamTexture = new WebCamTexture(deviceName, cameraWidth, cameraHeight, targetFrameRate);
            webcamTexture.Play();

            // Create frame texture for processing
            frameTexture = new Texture2D(cameraWidth, cameraHeight, TextureFormat.RGB24, false);

            // Display camera feed
            if (cameraDisplay != null)
            {
                cameraDisplay.texture = webcamTexture;
            }

            Debug.Log($"Camera initialized: {deviceName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize camera: {e.Message}");
        }
    }

    #endregion

    #region Frame Processing

    private void ProcessCameraFrame()
    {
        if (webcamTexture == null || !webcamTexture.didUpdateThisFrame)
            return;

        // Convert WebCamTexture to Texture2D
        frameTexture.SetPixels32(webcamTexture.GetPixels32());
        frameTexture.Apply();

        // Increment timestamp (in milliseconds)
        frameTimestamp += (long)(detectionInterval * 1000);

        // Detect pose based on running mode
        if (runningMode == RunningMode.LIVE_STREAM)
        {
            DetectAsyncWithCallback(frameTexture, frameTimestamp);
        }
        else if (runningMode == RunningMode.VIDEO)
        {
            DetectVideo(frameTexture, frameTimestamp);
        }
        else // IMAGE mode
        {
            DetectImage(frameTexture);
        }
    }

    #endregion

    #region MediaPipe Detection Methods

    private void DetectAsyncWithCallback(Texture2D frame, long timestamp)
    {
        try
        {
            poseLandmarker.DetectAsync(frame, timestamp, OnPoseDetected);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Pose detection failed: {e.Message}");
        }
    }

    private void DetectVideo(Texture2D frame, long timestamp)
    {
        try
        {
            PoseLandmarkerResult result = poseLandmarker.DetectForVideo(frame, timestamp);
            ProcessPoseResult(result);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Pose detection failed: {e.Message}");
        }
    }

    private void DetectImage(Texture2D frame)
    {
        try
        {
            PoseLandmarkerResult result = poseLandmarker.Detect(frame);
            ProcessPoseResult(result);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Pose detection failed: {e.Message}");
        }
    }

    /// <summary>
    /// Callback for async pose detection
    /// </summary>
    private void OnPoseDetected(PoseLandmarkerResult result, Image image, long timestamp)
    {
        ProcessPoseResult(result);
    }

    /// <summary>
    /// Process the pose detection result
    /// </summary>
    private void ProcessPoseResult(PoseLandmarkerResult result)
    {
        if (result == null || smplxController == null)
            return;

        // Update SMPLX controller with the result
        smplxController.ProcessPoseResult(result);
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Start pose tracking
    /// </summary>
    public void StartTracking()
    {
        if (smplxController != null)
        {
            smplxController.enableTracking = true;
        }

        if (webcamTexture != null && !webcamTexture.isPlaying)
        {
            webcamTexture.Play();
        }

        Debug.Log("Tracking started");
    }

    /// <summary>
    /// Stop pose tracking
    /// </summary>
    public void StopTracking()
    {
        if (smplxController != null)
        {
            smplxController.enableTracking = false;
        }

        if (webcamTexture != null && webcamTexture.isPlaying)
        {
            webcamTexture.Stop();
        }

        Debug.Log("Tracking stopped");
    }

    /// <summary>
    /// Reset tracking state
    /// </summary>
    public void ResetTracking()
    {
        if (smplxController != null)
        {
            smplxController.ResetTracking();
        }

        frameTimestamp = 0;
        lastDetectionTime = 0;

        Debug.Log("Tracking reset");
    }

    /// <summary>
    /// Toggle debug visualization
    /// </summary>
    public void ToggleDebugVisualization()
    {
        if (smplxController != null)
        {
            smplxController.showDebugVisualization = !smplxController.showDebugVisualization;
        }
    }

    /// <summary>
    /// Get tracking status information
    /// </summary>
    public string GetTrackingStatus()
    {
        if (smplxController == null)
            return "Controller not assigned";

        if (!smplxController.IsTrackingActive())
            return "Tracking inactive";

        return $"Tracking active - FPS: {(1f / detectionInterval):F1}";
    }

    #endregion

    #region Advanced Features (Optional)

    /// <summary>
    /// Example: Add hand tracking (requires MediaPipe Hand Landmarker)
    /// </summary>
    public void EnableHandTracking()
    {
        // TODO: Initialize MediaPipe Hand Landmarker
        // TODO: Process hand landmarks and pass to jointCalculator
        Debug.LogWarning("Hand tracking not yet implemented. See documentation for details.");
    }

    /// <summary>
    /// Example: Record and playback pose data
    /// </summary>
    private class PoseRecording
    {
        public float timestamp;
        public Vector3[] landmarks;
        public float[] visibility;
    }

    private System.Collections.Generic.List<PoseRecording> recordedPoses =
        new System.Collections.Generic.List<PoseRecording>();
    private bool isRecording = false;

    public void StartRecording()
    {
        recordedPoses.Clear();
        isRecording = true;
        Debug.Log("Recording started");
    }

    public void StopRecording()
    {
        isRecording = false;
        Debug.Log($"Recording stopped. Captured {recordedPoses.Count} frames");
    }

    private void RecordCurrentPose()
    {
        if (!isRecording || smplxController == null)
            return;

        var landmarks = smplxController.GetMediaPipeLandmarks();
        if (landmarks == null || landmarks.Length == 0)
            return;

        // Create recording entry
        var recording = new PoseRecording
        {
            timestamp = Time.time,
            landmarks = new Vector3[landmarks.Length],
            visibility = new float[33]
        };

        System.Array.Copy(landmarks, recording.landmarks, landmarks.Length);

        recordedPoses.Add(recording);
    }

    #endregion

    #region UI Callbacks

    // Add these methods to UI buttons for interactive control

    public void OnStartButtonClicked()
    {
        StartTracking();
    }

    public void OnStopButtonClicked()
    {
        StopTracking();
    }

    public void OnResetButtonClicked()
    {
        ResetTracking();
    }

    public void OnDebugToggleClicked()
    {
        ToggleDebugVisualization();
    }

    #endregion
}
