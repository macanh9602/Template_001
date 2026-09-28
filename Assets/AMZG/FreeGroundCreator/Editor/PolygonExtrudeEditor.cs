using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PolygonExtrudeGenerator))]
public class PolygonExtrudeEditor : Editor
{
    PolygonExtrudeGenerator generator;

    void OnEnable()
    {
        generator =
            (PolygonExtrudeGenerator)target;
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);

        if (GUILayout.Button("Build"))
        {
            generator.Build();

            EditorUtility.SetDirty(generator);
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Add Point"))
        {
            CreatePoint();
        }
    }

    void CreatePoint()
    {
        GameObject go =
            new GameObject(
                $"Point_{generator.points.Count}");

        Undo.RegisterCreatedObjectUndo(
            go,
            "Create Polygon Point");

        go.transform.SetParent(
            generator.transform,
            false);

        if (generator.points.Count > 0)
        {
            Transform last =
                generator.points[
                    generator.points.Count - 1];

            go.transform.localPosition =
                last.localPosition +
                Vector3.right;
        }

        generator.points.Add(
            go.transform);

        generator.Build();

        EditorUtility.SetDirty(generator);
    }

    void OnSceneGUI()
    {
        if (generator.points == null)
            return;

        Undo.RecordObject(
            generator,
            "Move Polygon Point");

        Handles.color = Color.cyan;

        bool changed = false;

        for (int i = 0; i < generator.points.Count; i++)
        {
            Transform point =
                generator.points[i];

            if (point == null)
                continue;

            EditorGUI.BeginChangeCheck();

            Vector3 pos =
                Handles.PositionHandle(
                    point.position,
                    Quaternion.identity);

            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(
                    point,
                    "Move Polygon Point");

                point.position = pos;

                changed = true;
            }

            Handles.Label(
                pos + Vector3.up * 0.2f,
                i.ToString());
        }

        if (changed)
        {
            generator.Build();

            EditorUtility.SetDirty(
                generator);
        }

        DrawPolygonPreview();
    }

    void DrawPolygonPreview()
    {
        int count =
            generator.points.Count;

        if (count < 2)
            return;

        Handles.color =
            new Color(
                1,
                1,
                0,
                0.7f);

        for (int i = 0; i < count; i++)
        {
            Transform a =
                generator.points[i];

            Transform b =
                generator.points[
                    (i + 1) % count];

            if (a == null || b == null)
                continue;

            Handles.DrawLine(
                a.position,
                b.position);
        }
    }
}