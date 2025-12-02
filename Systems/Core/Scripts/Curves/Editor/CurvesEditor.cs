using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CustomEditor(typeof(Curves))]
public class CurvesEditor : Editor
{
    private Curves curves;
    // Which indices are selected in the inspector
    private List<int> selectedPoints = new List<int>();

    // For the "Set Y" batch operation
    private float setYValue = 0f;

    private void OnEnable()
    {
        curves = (Curves)target;
    }

    public override void OnInspectorGUI()
    {
        // Draw default fields (curveType, isLoop, catmullAlpha, segmentsPerCurve)
        DrawDefaultInspector();

        EditorGUILayout.Space();

        // --- Add Point to Front/Back (same line) ---
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Add Point (Front)"))
        {
            Undo.RecordObject(curves, "Add Point Front");
            Vector3 newPos = curves.transform.position;
            curves.InsertControlPoint(0, newPos);
            EditorUtility.SetDirty(curves);
        }
        if (GUILayout.Button("Add Point (Back)"))
        {
            Undo.RecordObject(curves, "Add Point Back");
            Vector3 newPos = curves.transform.position;
            curves.AddControlPoint(newPos);
            EditorUtility.SetDirty(curves);
        }
        EditorGUILayout.EndHorizontal();

        // --- Reverse Path ---
        if (GUILayout.Button("Reverse Path"))
        {
            Undo.RecordObject(curves, "Reverse Control Points");
            curves.ReversePoints();
            EditorUtility.SetDirty(curves);
        }

        // --- Subdivide Path ---
        if (GUILayout.Button("Subdivide Path"))
        {
            Undo.RecordObject(curves, "Subdivide Path");
            SubdividePath();
            EditorUtility.SetDirty(curves);
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Control Points", EditorStyles.boldLabel);

        if (curves.ControlPoints.Count == 0)
        {
            EditorGUILayout.HelpBox("No control points yet. Use the Add Point buttons above.", MessageType.Info);
        }
        else
        {
            // List each control point
            for (int i = 0; i < curves.ControlPoints.Count; i++)
            {
                Vector3 point = curves.GetControlPoint(i);
                bool isSelected = selectedPoints.Contains(i);

                EditorGUILayout.BeginHorizontal();

                // Toggle for selection
                bool toggle = EditorGUILayout.ToggleLeft($"Point {i}", isSelected, GUILayout.Width(80));
                if (toggle != isSelected)
                {
                    if (toggle) selectedPoints.Add(i);
                    else selectedPoints.Remove(i);
                }

                // Display coordinates (read-only label)
                GUILayout.Label($"({point.x:F2}, {point.y:F2}, {point.z:F2})", GUILayout.MinWidth(120));

                if (GUILayout.Button("Focus", GUILayout.Width(50)))
                {
                    // Move SceneView camera pivot to this point
                    SceneView.lastActiveSceneView.pivot = point;
                    SceneView.lastActiveSceneView.size = 10f;
                    SceneView.lastActiveSceneView.Repaint();
                }

                if (GUILayout.Button("Remove", GUILayout.Width(60)))
                {
                    Undo.RecordObject(curves, "Remove Control Point");
                    curves.RemoveControlPoint(i);
                    selectedPoints.Remove(i);
                    EditorUtility.SetDirty(curves);
                    break;
                }

                EditorGUILayout.EndHorizontal();
            }
        }

        EditorGUILayout.Space();
        // "Set Y" area
        EditorGUILayout.LabelField("Set Y for Selected Points", EditorStyles.boldLabel);
        setYValue = EditorGUILayout.FloatField("Y Value", setYValue);

        if (GUILayout.Button("Apply Y to Selected"))
        {
            ApplyYToSelected(setYValue);
        }

        // We skip the "Add Point" and "Clear All Points" from the old version 
        // because we replaced them with new advanced buttons. 
        // But you could keep them if desired.
    }

    private void OnSceneGUI()
    {
        if (curves.ControlPoints.Count < 2) return;

        // Draw the interpolated curve lines
        DrawCurveLines();

        // Draw handles for each point
        for (int i = 0; i < curves.ControlPoints.Count; i++)
        {
            Vector3 currentPos = curves.GetControlPoint(i);

            // Label with index
            Handles.Label(currentPos + Vector3.up * 0.25f, $"[{i}]", EditorStyles.boldLabel);

            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(currentPos, Quaternion.identity);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(curves, "Move Control Point");
                curves.SetControlPoint(i, newPos);
                EditorUtility.SetDirty(curves);
            }
        }
    }

    /// <summary>
    /// Draws the curve lines in the Scene View.
    /// </summary>
    private void DrawCurveLines()
    {
        Handles.color = Color.yellow;
        int segCount = curves.SegmentCount;
        if (segCount == 0) return;

        for (int i = 0; i < segCount; i++)
        {
            Vector3 prevPos = CurvesUtils.EvaluateCurve(curves, i, 0f);
            int steps = curves.segmentsPerCurve;
            for (int s = 1; s <= steps; s++)
            {
                float t = s / (float)steps;
                Vector3 pos = CurvesUtils.EvaluateCurve(curves, i, t);
                Handles.DrawLine(prevPos, pos);
                prevPos = pos;
            }
        }
    }

    /// <summary>
    /// Sets the Y coordinate of all selected points to the specified value.
    /// </summary>
    private void ApplyYToSelected(float yValue)
    {
        Undo.RecordObject(curves, "Set Y for Selected Points");
        foreach (int idx in selectedPoints)
        {
            Vector3 p = curves.GetControlPoint(idx);
            p.y = yValue;
            curves.SetControlPoint(idx, p);
        }
        EditorUtility.SetDirty(curves);
    }

    /// <summary>
    /// Subdivide the path by adding a midpoint for each segment.
    /// For a loop, we do it for every pair including the wrap-around.
    /// For a non-loop, we do it for each adjacent pair.
    /// </summary>
    private void SubdividePath()
    {
        var oldPoints = new List<Vector3>(curves.ControlPoints);
        var newPoints = new List<Vector3>();

        int count = oldPoints.Count;
        if (count < 2) return;

        if (!curves.isLoop)
        {
            // Non-loop: Just go from 0 to count-2 for the pairs,
            // then add the last point at the end.
            for (int i = 0; i < count - 1; i++)
            {
                Vector3 p0 = oldPoints[i];
                Vector3 p1 = oldPoints[i + 1];
                Vector3 mid = (p0 + p1) * 0.5f;

                newPoints.Add(p0);
                newPoints.Add(mid);
            }
            // Add the very last point
            newPoints.Add(oldPoints[count - 1]);
        }
        else
        {
            // Loop: every point has a "next" = (i+1)%count
            for (int i = 0; i < count; i++)
            {
                Vector3 p0 = oldPoints[i];
                Vector3 p1 = oldPoints[(i + 1) % count];
                Vector3 mid = (p0 + p1) * 0.5f;

                newPoints.Add(p0);
                newPoints.Add(mid);
            }
        }

        // Replace
        curves.Clear();
        foreach (var p in newPoints)
        {
            curves.AddControlPoint(p);
        }
    }
}
