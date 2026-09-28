using AMZG.Helpers.OuterWall;
using UnityEditor;
using UnityEngine;

namespace AMZG.Helpers.OuterWall.Editor
{
    [CustomEditor(typeof(OuterWallGenerator))]
    public class OuterWallGeneratorEditor : UnityEditor.Editor
    {
        OuterWallGenerator generator;

        void OnEnable()
        {
            generator = (OuterWallGenerator)target;
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            GUILayout.Space(10f);

            using (new GUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Build"))
                {
                    Undo.RecordObject(generator, "Build Outer Wall");
                    generator.Build();
                    EditorUtility.SetDirty(generator);
                }

                if (GUILayout.Button("Clear Points"))
                {
                    Undo.RecordObject(generator, "Clear Outer Wall Points");
                    generator.ClearPoints();
                    EditorUtility.SetDirty(generator);
                }
            }

            using (new EditorGUI.DisabledScope(generator.mode != OuterWallGenerator.SourceMode.ManualPoints))
            {
                if (GUILayout.Button("Add Point"))
                {
                    Undo.IncrementCurrentGroup();
                    Transform point = generator.AddManualPoint();
                    Undo.RegisterCreatedObjectUndo(point.gameObject, "Add Outer Wall Point");
                    EditorUtility.SetDirty(generator);
                }
            }
        }

        void OnSceneGUI()
        {
            if (generator == null)
                return;

            DrawGridPreview();

            if (generator.mode != OuterWallGenerator.SourceMode.ManualPoints || generator.manualPoints == null)
                return;

            Handles.color = Color.cyan;
            bool changed = false;

            for (int i = 0; i < generator.manualPoints.Count; i++)
            {
                Transform point = generator.manualPoints[i];
                if (point == null)
                    continue;

                EditorGUI.BeginChangeCheck();
                Vector3 position = Handles.PositionHandle(point.position, Quaternion.identity);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(point, "Move Outer Wall Point");
                    point.position = position;
                    changed = true;
                }

                Handles.Label(position + Vector3.up * 0.25f, i.ToString());
            }

            if (changed)
            {
                generator.Build();
                EditorUtility.SetDirty(generator);
            }
        }

        void DrawGridPreview()
        {
            if (generator.mode != OuterWallGenerator.SourceMode.GridRect && generator.mode != OuterWallGenerator.SourceMode.GridMask)
                return;

            Handles.color = new Color(0f, 0.8f, 1f, 0.35f);
            float size = Mathf.Max(0.001f, generator.cellSize);

            if (generator.mode == OuterWallGenerator.SourceMode.GridRect)
            {
                float width = generator.gridWidth * size;
                float height = generator.gridHeight * size;
                Vector3 origin = generator.transform.position - new Vector3(width, 0f, height) * 0.5f;

                for (int x = 0; x <= generator.gridWidth; x++)
                {
                    Vector3 a = origin + new Vector3(x * size, 0f, 0f);
                    Vector3 b = origin + new Vector3(x * size, 0f, height);
                    Handles.DrawLine(a, b);
                }

                for (int y = 0; y <= generator.gridHeight; y++)
                {
                    Vector3 a = origin + new Vector3(0f, 0f, y * size);
                    Vector3 b = origin + new Vector3(width, 0f, y * size);
                    Handles.DrawLine(a, b);
                }
            }
            else
            {
                foreach (Vector2Int cell in generator.occupiedCells)
                {
                    Vector3 local = new Vector3(cell.x * size + size * 0.5f, 0f, cell.y * size + size * 0.5f);
                    Vector3 center = generator.transform.TransformPoint(local);
                    Handles.DrawWireCube(center, new Vector3(size, 0.02f, size));
                }
            }
        }
    }
}
