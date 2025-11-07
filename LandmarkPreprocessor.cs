using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Preprocesses MediaPipe landmarks with smoothing, filtering, and coordinate transformation.
/// Implements One Euro Filter for optimal smoothing with low latency.
/// </summary>
public class LandmarkPreprocessor
{
    #region Configuration

    [System.Serializable]
    public class FilterSettings
    {
        [Tooltip("Minimum cutoff frequency (lower = more smoothing)")]
        public float minCutoff = 1.0f;

        [Tooltip("Speed coefficient (higher = less lag during fast motion)")]
        public float beta = 0.007f;

        [Tooltip("Derivative cutoff frequency")]
        public float dCutoff = 1.0f;

        [Tooltip("Minimum visibility threshold for landmark validity")]
        [Range(0f, 1f)]
        public float visibilityThreshold = 0.5f;

        [Tooltip("Scale factor for converting MediaPipe units to Unity units")]
        public float scaleFactor = 1.0f;

        [Tooltip("Flip X coordinate (mirror effect)")]
        public bool flipX = false;

        [Tooltip("Flip Y coordinate")]
        public bool flipY = false;

        [Tooltip("Flip Z coordinate (convert camera space to world space)")]
        public bool flipZ = true;
    }

    #endregion

    #region One Euro Filter Implementation

    private class OneEuroFilter
    {
        private float minCutoff;
        private float beta;
        private float dCutoff;

        private Vector3 previousFiltered;
        private Vector3 previousRaw;
        private float previousTime;
        private bool isInitialized;

        public OneEuroFilter(float minCutoff, float beta, float dCutoff)
        {
            this.minCutoff = minCutoff;
            this.beta = beta;
            this.dCutoff = dCutoff;
            this.isInitialized = false;
        }

        public Vector3 Filter(Vector3 raw, float currentTime)
        {
            if (!isInitialized)
            {
                previousRaw = raw;
                previousFiltered = raw;
                previousTime = currentTime;
                isInitialized = true;
                return raw;
            }

            float deltaTime = currentTime - previousTime;
            if (deltaTime <= 0)
                return previousFiltered;

            // Calculate velocity
            Vector3 velocity = (raw - previousRaw) / deltaTime;

            // Smooth velocity
            float alpha = Alpha(dCutoff, deltaTime);
            Vector3 smoothedVelocity = Vector3.Lerp(
                (previousFiltered - previousRaw) / deltaTime,
                velocity,
                alpha
            );

            // Calculate adaptive cutoff
            float cutoff = minCutoff + beta * smoothedVelocity.magnitude;

            // Smooth position
            alpha = Alpha(cutoff, deltaTime);
            Vector3 filtered = Vector3.Lerp(previousFiltered, raw, alpha);

            // Update state
            previousRaw = raw;
            previousFiltered = filtered;
            previousTime = currentTime;

            return filtered;
        }

        private float Alpha(float cutoff, float deltaTime)
        {
            float tau = 1.0f / (2.0f * Mathf.PI * cutoff);
            return 1.0f / (1.0f + tau / deltaTime);
        }

        public void Reset()
        {
            isInitialized = false;
        }
    }

    #endregion

    #region Fields

    private FilterSettings settings;
    private OneEuroFilter[] filters;
    private Vector3[] previousLandmarks;
    private float[] landmarkVisibility;
    private bool isInitialized;

    #endregion

    #region Initialization

    public LandmarkPreprocessor(FilterSettings settings = null)
    {
        this.settings = settings ?? new FilterSettings();
        this.filters = new OneEuroFilter[33]; // MediaPipe has 33 landmarks
        this.previousLandmarks = new Vector3[33];
        this.landmarkVisibility = new float[33];

        for (int i = 0; i < 33; i++)
        {
            filters[i] = new OneEuroFilter(
                this.settings.minCutoff,
                this.settings.beta,
                this.settings.dCutoff
            );
        }

        isInitialized = false;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Process raw MediaPipe world landmarks with smoothing and transformation
    /// </summary>
    /// <param name="rawLandmarks">Raw 3D landmarks from MediaPipe (in meters)</param>
    /// <param name="visibility">Visibility scores for each landmark</param>
    /// <param name="timestamp">Current timestamp in seconds</param>
    /// <returns>Processed landmarks in Unity world space</returns>
    public Vector3[] ProcessLandmarks(Vector3[] rawLandmarks, float[] visibility, float timestamp)
    {
        if (rawLandmarks == null || rawLandmarks.Length != 33)
        {
            Debug.LogError("Invalid landmark count. Expected 33 landmarks.");
            return previousLandmarks ?? new Vector3[33];
        }

        Vector3[] processed = new Vector3[33];

        for (int i = 0; i < 33; i++)
        {
            // Check visibility
            bool isVisible = visibility != null && visibility[i] >= settings.visibilityThreshold;

            if (!isVisible)
            {
                // Use previous landmark if not visible
                processed[i] = previousLandmarks[i];
                continue;
            }

            // Transform coordinate system
            Vector3 transformed = TransformCoordinates(rawLandmarks[i]);

            // Apply One Euro Filter
            Vector3 filtered = filters[i].Filter(transformed, timestamp);

            processed[i] = filtered;
            landmarkVisibility[i] = visibility?[i] ?? 1.0f;
        }

        previousLandmarks = processed;
        isInitialized = true;

        return processed;
    }

    /// <summary>
    /// Transform from MediaPipe coordinate system to Unity coordinate system
    /// </summary>
    private Vector3 TransformCoordinates(Vector3 mpLandmark)
    {
        Vector3 transformed = new Vector3(
            settings.flipX ? -mpLandmark.x : mpLandmark.x,
            settings.flipY ? -mpLandmark.y : mpLandmark.y,
            settings.flipZ ? -mpLandmark.z : mpLandmark.z
        );

        // Scale to Unity units
        transformed *= settings.scaleFactor;

        return transformed;
    }

    /// <summary>
    /// Get visibility score for a landmark
    /// </summary>
    public float GetVisibility(int landmarkIndex)
    {
        if (landmarkIndex < 0 || landmarkIndex >= 33)
            return 0f;

        return landmarkVisibility[landmarkIndex];
    }

    /// <summary>
    /// Check if a landmark is valid (visible enough)
    /// </summary>
    public bool IsLandmarkValid(int landmarkIndex)
    {
        return GetVisibility(landmarkIndex) >= settings.visibilityThreshold;
    }

    /// <summary>
    /// Reset all filters (call when tracking is lost)
    /// </summary>
    public void Reset()
    {
        foreach (var filter in filters)
        {
            filter.Reset();
        }
        isInitialized = false;
    }

    /// <summary>
    /// Update filter settings at runtime
    /// </summary>
    public void UpdateSettings(FilterSettings newSettings)
    {
        this.settings = newSettings;

        // Recreate filters with new settings
        for (int i = 0; i < 33; i++)
        {
            filters[i] = new OneEuroFilter(
                settings.minCutoff,
                settings.beta,
                settings.dCutoff
            );
        }
    }

    #endregion

    #region Utility Methods

    /// <summary>
    /// Calculate average visibility across all landmarks
    /// </summary>
    public float GetAverageVisibility()
    {
        float sum = 0f;
        for (int i = 0; i < 33; i++)
        {
            sum += landmarkVisibility[i];
        }
        return sum / 33f;
    }

    /// <summary>
    /// Get the center point (pelvis) from hip landmarks
    /// </summary>
    public Vector3 GetCenterPoint(Vector3[] processedLandmarks)
    {
        if (processedLandmarks == null || processedLandmarks.Length != 33)
            return Vector3.zero;

        // MediaPipe hip indices: 23 (left), 24 (right)
        return (processedLandmarks[23] + processedLandmarks[24]) * 0.5f;
    }

    /// <summary>
    /// Normalize landmarks relative to hip center
    /// </summary>
    public Vector3[] NormalizeToCenter(Vector3[] processedLandmarks)
    {
        Vector3 center = GetCenterPoint(processedLandmarks);
        Vector3[] normalized = new Vector3[33];

        for (int i = 0; i < 33; i++)
        {
            normalized[i] = processedLandmarks[i] - center;
        }

        return normalized;
    }

    /// <summary>
    /// Enforce anatomical constraints on limb lengths
    /// </summary>
    public void EnforceLimbLengths(Vector3[] landmarks, Dictionary<string, float> limbLengths)
    {
        if (!isInitialized || landmarks == null)
            return;

        // Left arm
        EnforceBoneLength(landmarks, 11, 13, limbLengths.GetValueOrDefault("left_upper_arm", 0.3f));
        EnforceBoneLength(landmarks, 13, 15, limbLengths.GetValueOrDefault("left_lower_arm", 0.25f));

        // Right arm
        EnforceBoneLength(landmarks, 12, 14, limbLengths.GetValueOrDefault("right_upper_arm", 0.3f));
        EnforceBoneLength(landmarks, 14, 16, limbLengths.GetValueOrDefault("right_lower_arm", 0.25f));

        // Left leg
        EnforceBoneLength(landmarks, 23, 25, limbLengths.GetValueOrDefault("left_upper_leg", 0.4f));
        EnforceBoneLength(landmarks, 25, 27, limbLengths.GetValueOrDefault("left_lower_leg", 0.4f));

        // Right leg
        EnforceBoneLength(landmarks, 24, 26, limbLengths.GetValueOrDefault("right_upper_leg", 0.4f));
        EnforceBoneLength(landmarks, 26, 28, limbLengths.GetValueOrDefault("right_lower_leg", 0.4f));
    }

    private void EnforceBoneLength(Vector3[] landmarks, int parentIdx, int childIdx, float targetLength)
    {
        if (parentIdx < 0 || parentIdx >= 33 || childIdx < 0 || childIdx >= 33)
            return;

        Vector3 direction = (landmarks[childIdx] - landmarks[parentIdx]).normalized;
        landmarks[childIdx] = landmarks[parentIdx] + direction * targetLength;
    }

    #endregion
}
