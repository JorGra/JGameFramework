using System;
using System.Collections.Generic;
using UnityEngine;

public class CurvesFollower : MonoBehaviour
{
    [Header("Curves Reference")]
    public Curves curves;

    [Tooltip("Movement speed in units/second.")]
    public float speed = 2f;

    [Tooltip("If true, object will face forward along the path.")]
    public bool faceForward = true;

    [Tooltip("Sideways offset from the path direction, so units don't overlap.")]
    public float lateralOffset = 0f;

    /// <summary>
    /// Fires once if/when we reach the end of the path (i.e. distance == totalLength), but only if not looping.
    /// </summary>
    public event Action OnPathEndReached;

    // Arc-length table for uniform speed
    private List<CurvesUtils.ArcLengthSample> arcLengthTable;

    // Total distance traveled so far
    public float DistanceTravelled { get; set; } = 0f;

    // How long the entire path is (from the last sample's cumulativeLength)
    private float totalCurveLength = 0f;

    // Internal flag to prevent firing OnPathEndReached multiple times
    private bool hasFiredEndEvent = false;

    // Whether we are paused (movement stopped)
    private bool isPaused = false;

    private void Start()
    {
        if (curves == null)
        {
            Debug.LogError("No Curves assigned to CurvesFollower!", this);
            enabled = false;
            return;
        }

        // Build the arc-length table
        arcLengthTable = CurvesUtils.BuildArcLengthTable(curves, 20);

        if (arcLengthTable.Count > 0)
        {
            totalCurveLength = arcLengthTable[arcLengthTable.Count - 1].cumulativeLength;
            // Initialize position
            transform.position = arcLengthTable[0].position;
        }
    }

    private void Update()
    {
        if (arcLengthTable == null || arcLengthTable.Count == 0)
            return;

        if (isPaused)
            return; // Do nothing if paused

        // Move along the path
        DistanceTravelled += speed * Time.deltaTime;

        // If looping, wrap around
        if (curves.isLoop)
        {
            DistanceTravelled %= totalCurveLength;
        }
        else
        {
            // Otherwise, clamp to end
            if (DistanceTravelled >= totalCurveLength)
            {
                DistanceTravelled = totalCurveLength;

                // Fire end-event once
                if (!hasFiredEndEvent)
                {
                    hasFiredEndEvent = true;
                    OnPathEndReached?.Invoke();
                }
            }
        }

        // Compute current position on path
        Vector3 pathPos = CurvesUtils.GetPositionAtDistance(DistanceTravelled, arcLengthTable);

        // Determine forward direction for rotation if needed
        Vector3 forwardDir = transform.forward;
        if (faceForward && DistanceTravelled < totalCurveLength)
        {
            float lookAhead = 0.1f;
            float futureDist = DistanceTravelled + lookAhead;

            // If we might go beyond total length in a non-loop scenario, clamp
            // Or wrap if loop
            if (curves.isLoop)
                futureDist %= totalCurveLength;
            else
                futureDist = Mathf.Min(futureDist, totalCurveLength);

            Vector3 futurePos = CurvesUtils.GetPositionAtDistance(futureDist, arcLengthTable);
            Vector3 dir = (futurePos - pathPos).normalized;
            if (dir.sqrMagnitude > 0.0001f)
            {
                forwardDir = dir;
                transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
            }
        }

        // Apply lateral offset if needed
        if (Mathf.Abs(lateralOffset) > 0.001f)
        {
            Vector3 rightDir = Vector3.Cross(forwardDir, Vector3.up).normalized;
            pathPos += rightDir * lateralOffset;
        }

        // Set final position
        transform.position = pathPos;
    }

    /// <summary>
    /// Pauses the movement along the path (retains current distance).
    /// </summary>
    public void Pause()
    {
        isPaused = true;
    }

    /// <summary>
    /// Resumes movement along the path from the current distance.
    /// </summary>
    public void Resume()
    {
        isPaused = false;
    }
}
