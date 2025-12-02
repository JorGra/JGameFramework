using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class Curves : MonoBehaviour
{
    public enum CurveType
    {
        Bezier,
        BSpline,
        CatmullRomAlpha // alpha-based ("centripetal") Catmull–Rom
    }

    [Header("Curve Settings")]
    [Tooltip("Which type of curve to use for interpolation.")]
    public CurveType curveType = CurveType.CatmullRomAlpha;

    [Tooltip("If true, the curve is closed (loop).")]
    public bool isLoop = false;

    [Header("Catmull–Rom Alpha (0=Uniform, 0.5=Centripetal, 1=Chordal)")]
    [Range(0f, 1f)]
    public float catmullAlpha = 0.5f;

    [Header("Drawing Settings")]
    [Tooltip("Number of subdivisions per segment for Scene view drawing.")]
    [Range(2, 50)]
    public int segmentsPerCurve = 10;

    [SerializeField]
    private List<Vector3> controlPoints = new List<Vector3>();

    /// <summary> Read-only access to the control points array. </summary>
    public IReadOnlyList<Vector3> ControlPoints => controlPoints;

    /// <summary>
    /// Number of segments in the curve:
    /// If N control points, then (N-1) if not looping, else N.
    /// </summary>
    public int SegmentCount
    {
        get
        {
            if (controlPoints.Count < 2) return 0;
            return isLoop ? controlPoints.Count : controlPoints.Count - 1;
        }
    }

    /// <summary> Gets the control point at 'index', wrapping around if looping. </summary>
    public Vector3 GetControlPoint(int index)
    {
        if (controlPoints.Count == 0)
            return Vector3.zero;

        if (!isLoop)
        {
            index = Mathf.Clamp(index, 0, controlPoints.Count - 1);
        }
        else
        {
            index = (index + controlPoints.Count) % controlPoints.Count;
        }
        return controlPoints[index];
    }

    /// <summary> Sets (replaces) the control point at the given index. </summary>
    public void SetControlPoint(int index, Vector3 newValue)
    {
        if (index < 0 || index >= controlPoints.Count)
            return;
        controlPoints[index] = newValue;
    }

    /// <summary> Adds a new control point at the end (back). </summary>
    public void AddControlPoint(Vector3 point)
    {
        controlPoints.Add(point);
    }

    /// <summary> Inserts a new control point at a specific index. </summary>
    public void InsertControlPoint(int index, Vector3 point)
    {
        if (index < 0 || index > controlPoints.Count)
            return;
        controlPoints.Insert(index, point);
    }

    /// <summary> Removes a control point at a specific index. </summary>
    public void RemoveControlPoint(int index)
    {
        if (index < 0 || index >= controlPoints.Count)
            return;
        controlPoints.RemoveAt(index);
    }

    /// <summary> Clears all control points. </summary>
    public void Clear()
    {
        controlPoints.Clear();
    }

    /// <summary>
    /// Reverse the order of the existing control points.
    /// </summary>
    public void ReversePoints()
    {
        controlPoints.Reverse();
    }
}
