using System.Collections;
using UnityEngine;

/// <summary>
/// Animates position, rotation, and/or scale of a target Transform over 'duration' seconds,
/// using separate animation curves for each component. 
/// Optionally resets each component to its original value at the end, 
/// but only after *all* overlapping coroutines finish (when concurrency is zero).
/// 
/// If preserveFirstStartIfInProgress is true:
///  - The first triggered effect calculates the baseline,
///  - Subsequent triggers reuse that same baseline and do not recalc the transform at the start.
/// Once the final effect finishes (activeCoroutines == 0), we reset if requested.
/// </summary>
public class TransformManipulationEffect : EffectBase
{
    [Header("Target / Duration")]
    [Tooltip("If null, uses this GameObject's transform.")]
    public Transform target;
    [Tooltip("Total duration (seconds) for the transform animation.")]
    public float duration = 1f;

    [Header("Position Settings")]
    public bool affectPosition = false;
    public bool resetPosition = true;
    [Tooltip("How the local position changes from its original. Multiplied by positionCurve over time.")]
    public Vector3 positionChange = Vector3.zero;
    [Tooltip("Animation curve, sampled from time=0..duration. Output: 0..1 scale.")]
    public AnimationCurve positionCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Rotation Settings (local Euler)")]
    public bool affectRotation = false;
    public bool resetRotation = true;
    [Tooltip("Change in local Euler angles (in degrees) from the original.")]
    public Vector3 rotationChange = Vector3.zero;
    [Tooltip("Animation curve for rotation, 0..1 scale of rotationChange.")]
    public AnimationCurve rotationCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Scale Settings (local)")]
    public bool affectScale = false;
    public bool resetScale = true;
    [Tooltip("Change in local scale from the original, used additively in this example.")]
    public Vector3 scaleChange = Vector3.one;
    [Tooltip("Animation curve for scale, 0..1 scale of scaleChange.")]
    public AnimationCurve scaleCurve = AnimationCurve.Linear(0, 0, 1, 1);

    [Header("Multiple Trigger Behavior")]
    [Tooltip("If true, once an effect is playing, subsequent triggers won't recalc the original baseline.")]
    public bool preserveFirstStartIfInProgress = false;

    // We store a baseline only once if preserveFirstStartIfInProgress == true
    // or each time if it's false
    private Vector3 baselinePos;
    private Vector3 baselineRot;
    private Vector3 baselineScale;

    // Has at least one effect stored the baseline?
    private bool baselineSet = false;

    // Count how many instances of this effect are currently running
    private int activeCoroutines = 0;

    protected override IEnumerator PlayEffectLogic()
    {
        Transform t = target != null ? target : transform;

        // Decide if we need to store the baseline now:
        // 1) If no effect is running or preserve is false, we recalc the baseline.
        // 2) If preserve is true AND baselineSet == false, we do it the first time only.
        bool needToStoreBaseline = true;

        if (preserveFirstStartIfInProgress)
        {
            // If we have *already* stored the baseline once, don't store again.
            if (baselineSet)
                needToStoreBaseline = false;
        }
        // If we've never stored or preserve is off, then we do store
        if (needToStoreBaseline)
        {
            baselinePos = t.localPosition;
            baselineRot = t.localEulerAngles;
            baselineScale = t.localScale;
            baselineSet = true;
        }

        // Increment concurrency count
        activeCoroutines++;

        // Take local copies of the baseline for *this* coroutine 
        // (in case subsequent triggers modify baseline if preserve = false).
        Vector3 myBasePos = baselinePos;
        Vector3 myBaseRot = baselineRot;
        Vector3 myBaseScale = baselineScale;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float normalized = Mathf.Clamp01(elapsed / duration);

            // Position
            if (affectPosition)
            {
                float posCurveValue = positionCurve.Evaluate(normalized);
                Vector3 offset = positionChange * posCurveValue;
                t.localPosition = myBasePos + offset;
            }

            // Rotation
            if (affectRotation)
            {
                float rotCurveValue = rotationCurve.Evaluate(normalized);
                Vector3 rotOffset = rotationChange * rotCurveValue;
                t.localEulerAngles = myBaseRot + rotOffset;
            }

            // Scale
            if (affectScale)
            {
                float scaleCurveValue = scaleCurve.Evaluate(normalized);
                // Just adding for an example. 
                Vector3 scaleOffset = scaleChange * scaleCurveValue;
                t.localScale = myBaseScale + scaleOffset;
            }

            yield return null;
        }

        // This coroutine is done, so decrement concurrency
        activeCoroutines--;

        // If activeCoroutines hits 0, that means no more overlapping effects are running.
        // Now we reset, if requested
        if (activeCoroutines <= 0)
        {
            if (affectPosition && resetPosition)
            {
                t.localPosition = baselinePos;
            }
            if (affectRotation && resetRotation)
            {
                t.localEulerAngles = baselineRot;
            }
            if (affectScale && resetScale)
            {
                t.localScale = baselineScale;
            }

            // After everything is done, we can choose to clear baselineSet 
            // so next time we do a new baseline. 
            // If you prefer to keep it for subsequent triggers, comment out:
            baselineSet = false;
        }
    }
}
