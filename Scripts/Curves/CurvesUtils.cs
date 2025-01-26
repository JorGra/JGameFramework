using System.Collections.Generic;
using UnityEngine;

public static class CurvesUtils
{
    /// <summary>
    /// Single entry point for evaluating the curve at segmentIndex with local parameter t in [0..1].
    /// Depending on curves.curveType, it calls the appropriate function (Bezier, B-Spline, or Catmull-Rom Alpha).
    /// </summary>
    public static Vector3 EvaluateCurve(Curves curves, int segmentIndex, float t)
    {
        switch (curves.curveType)
        {
            case Curves.CurveType.Bezier:
                return EvaluateBezierSegment(curves, segmentIndex, t);

            case Curves.CurveType.BSpline:
                return EvaluateBSplineSegment(curves, segmentIndex, t);

            case Curves.CurveType.CatmullRomAlpha:
            default:
                return EvaluateCatmullRomAlpha(curves, segmentIndex, t, curves.catmullAlpha);
        }
    }

    // --------------------------------------------------------------------------
    //  1) Catmull–Rom (Alpha-based / "centripetal") Implementation
    // --------------------------------------------------------------------------

    /// <summary>
    /// Evaluate alpha-based Catmull–Rom, sometimes called "Centripetal" (alpha=0.5) or "Chordal" (alpha=1),
    /// or "Uniform" (alpha=0).
    /// 
    /// This uses the distance-based parameterization to reduce overshoot.
    /// </summary>
    private static Vector3 EvaluateCatmullRomAlpha(Curves curves, int segmentIndex, float u, float alpha)
    {
        // We need 4 points: p0, p1, p2, p3
        Vector3 p0 = curves.GetControlPoint(segmentIndex - 1);
        Vector3 p1 = curves.GetControlPoint(segmentIndex);
        Vector3 p2 = curves.GetControlPoint(segmentIndex + 1);
        Vector3 p3 = curves.GetControlPoint(segmentIndex + 2);

        // Distances between points, raised to the alpha power
        float d01 = Mathf.Pow(Vector3.Distance(p0, p1), alpha);
        float d12 = Mathf.Pow(Vector3.Distance(p1, p2), alpha);
        float d23 = Mathf.Pow(Vector3.Distance(p2, p3), alpha);

        float t0 = 0f;
        float t1 = t0 + d01;
        float t2 = t1 + d12;
        float t3 = t2 + d23;

        // s = the "real" parameter in [t1..t2], as we go u in [0..1]
        float s = Mathf.Lerp(t1, t2, u);

        // Interpolate in p0..p1..p2..p3 steps
        Vector3 A1 = Interpolate(p0, p1, t0, t1, s);
        Vector3 A2 = Interpolate(p1, p2, t1, t2, s);
        Vector3 A3 = Interpolate(p2, p3, t2, t3, s);

        Vector3 B1 = Interpolate(A1, A2, t0, t2, s);
        Vector3 B2 = Interpolate(A2, A3, t1, t3, s);

        Vector3 C = Interpolate(B1, B2, t1, t2, s);
        return C;
    }

    /// <summary>
    /// Helper for the alpha-based Catmull–Rom to do one linear interpolation step.
    /// </summary>
    private static Vector3 Interpolate(Vector3 p0, Vector3 p1, float t0, float t1, float t)
    {
        if (Mathf.Approximately(t0, t1)) return p0; // avoid division by zero
        return (t1 - t) / (t1 - t0) * p0 + (t - t0) / (t1 - t0) * p1;
    }

    // --------------------------------------------------------------------------
    //  2) B-Spline (Cubic, Uniform) Implementation
    // --------------------------------------------------------------------------
    private static Vector3 EvaluateBSplineSegment(Curves curves, int segmentIndex, float t)
    {
        // We need p(i-1), p(i), p(i+1), p(i+2).
        Vector3 p0 = curves.GetControlPoint(segmentIndex - 1);
        Vector3 p1 = curves.GetControlPoint(segmentIndex);
        Vector3 p2 = curves.GetControlPoint(segmentIndex + 1);
        Vector3 p3 = curves.GetControlPoint(segmentIndex + 2);

        // Standard uniform cubic B-spline basis matrix:
        // 1/6 * [ -1  3 -3  1
        //         3 -6  3  0
        //        -3  0  3  0
        //         1  4  1  0 ]
        float t2 = t * t;
        float t3 = t2 * t;

        float b0 = (-1f * t3 + 3f * t2 - 3f * t + 1f) / 6f;
        float b1 = (3f * t3 - 6f * t2 + 0f * t + 4f) / 6f;
        float b2 = (-3f * t3 + 3f * t2 + 3f * t + 1f) / 6f;
        float b3 = (1f * t3 + 0f * t2 + 0f * t + 0f) / 6f;

        return p0 * b0 + p1 * b1 + p2 * b2 + p3 * b3;
    }

    // --------------------------------------------------------------------------
    //  3) Bezier (Cubic) Implementation (pseudo-handles approach)
    // --------------------------------------------------------------------------
    private static Vector3 EvaluateBezierSegment(Curves curves, int segmentIndex, float t)
    {
        // For simplicity, we'll re-use the same approach as "Catmull–Rom tangents"
        // to generate p1,p2. In a real workflow, you might have separate handle points.
        // We just get 4 points for a cubic segment and pass them into the standard
        // cubic Bezier formula.
        Vector3 p0 = curves.GetControlPoint(segmentIndex - 1);
        Vector3 p1 = curves.GetControlPoint(segmentIndex);
        Vector3 p2 = curves.GetControlPoint(segmentIndex + 1);
        Vector3 p3 = curves.GetControlPoint(segmentIndex + 2);

        // We'll do a "catmull-like" approach to define the middle Bezier handles:
        // offset from p1 to p2 is the main direction, offset from p0 to p3 is outside direction, etc.
        // This is not a "pure" Bezier with user-defined handles, but good enough for demonstration.
        float tension = 0.5f; // you can tweak or expose if you like
        Vector3 d1 = (p2 - p0) * tension;
        Vector3 d2 = (p3 - p1) * tension;

        // Our "4" actual Bezier points
        Vector3 b0 = p1;
        Vector3 b1 = p1 + d1;
        Vector3 b2 = p2 - d2;
        Vector3 b3 = p2;

        return CubicBezier(b0, b1, b2, b3, t);
    }

    /// <summary> Standard cubic Bezier interpolation given four control points. </summary>
    private static Vector3 CubicBezier(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float u = 1f - t;
        float tt = t * t;
        float uu = u * u;
        float uuu = uu * u;
        float ttt = tt * t;
        return uuu * p0 + 3f * uu * t * p1 + 3f * u * tt * p2 + ttt * p3;
    }

    // --------------------------------------------------------------------------
    //  ARC-LENGTH TABLE for Uniform Speed
    // --------------------------------------------------------------------------

    /// <summary>
    /// Builds a table of ArcLengthSample so we can do distance -> position lookups for uniform speed.
    /// Subdivides each segment in 'samplesPerSegment' steps.
    /// 
    /// For each step, we store (segmentIndex, localT, cumulativeLength, worldPosition).
    /// </summary>
    public static List<ArcLengthSample> BuildArcLengthTable(Curves curves, int samplesPerSegment = 20)
    {
        var table = new List<ArcLengthSample>();
        float totalLength = 0f;
        int segCount = curves.SegmentCount;
        if (segCount == 0) return table;

        // For convenience, let's start by adding the first sample
        Vector3 startPos = EvaluateCurve(curves, 0, 0f);
        table.Add(new ArcLengthSample(0, 0f, 0f, startPos));

        for (int i = 0; i < segCount; i++)
        {
            Vector3 prevPos = EvaluateCurve(curves, i, 0f);
            for (int s = 1; s <= samplesPerSegment; s++)
            {
                float t = s / (float)samplesPerSegment;
                Vector3 pos = EvaluateCurve(curves, i, t);
                float dist = Vector3.Distance(prevPos, pos);
                totalLength += dist;

                table.Add(new ArcLengthSample(i, t, totalLength, pos));
                prevPos = pos;
            }
        }

        return table;
    }

    /// <summary>
    /// Given a distance along the entire curve, returns the associated position via the arcLengthTable.
    /// This uses a simple linear search; for large tables, you can do a binary search.
    /// </summary>
    public static Vector3 GetPositionAtDistance(float distance, List<ArcLengthSample> arcLengthTable)
    {
        if (arcLengthTable == null || arcLengthTable.Count == 0)
            return Vector3.zero;

        float maxDist = arcLengthTable[arcLengthTable.Count - 1].cumulativeLength;
        distance = Mathf.Clamp(distance, 0f, maxDist);

        for (int i = 0; i < arcLengthTable.Count - 1; i++)
        {
            float currDist = arcLengthTable[i].cumulativeLength;
            float nextDist = arcLengthTable[i + 1].cumulativeLength;
            if (currDist <= distance && nextDist >= distance)
            {
                // Lerp between sample i and i+1
                ArcLengthSample A = arcLengthTable[i];
                ArcLengthSample B = arcLengthTable[i + 1];
                float segLen = nextDist - currDist;
                if (Mathf.Approximately(segLen, 0f))
                    return A.position; // no movement

                float lerpT = (distance - currDist) / segLen;
                return Vector3.Lerp(A.position, B.position, lerpT);
            }
        }

        // If we somehow pass the loop, just return the last sample
        return arcLengthTable[arcLengthTable.Count - 1].position;
    }
}

/// <summary>
/// Holds data for each subdivision sample in the arc-length table.
/// </summary>
public struct ArcLengthSample
{
    public int segmentIndex;
    public float t;
    public float cumulativeLength;
    public Vector3 position;

    public ArcLengthSample(int segIndex, float localT, float length, Vector3 pos)
    {
        segmentIndex = segIndex;
        t = localT;
        cumulativeLength = length;
        position = pos;
    }
}
