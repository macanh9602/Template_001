using System.Collections.Generic;
using UnityEngine;

namespace AMZG.Helpers.OuterWall
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter))]
    [RequireComponent(typeof(MeshRenderer))]
    public class OuterWallGenerator : MonoBehaviour
    {
        public enum SourceMode
        {
            ManualPoints,
            GridRect,
            GridMask,
            PathPoints
        }

        [Header("Source")]
        public SourceMode mode = SourceMode.ManualPoints;
        public List<Transform> manualPoints = new List<Transform>();
        public List<Vector2> pathPoints = new List<Vector2>();
        public List<Vector2Int> occupiedCells = new List<Vector2Int>();
        [Min(1)] public int gridWidth = 8;
        [Min(1)] public int gridHeight = 8;
        [Min(0.001f)] public float cellSize = 1f;

        [Header("Wall")]
        [Min(0.001f)] public float height = 1f;
        [Min(0f)] public float thickness = 0.2f;
        [Min(0f)] public float cornerRadius = 0f;
        [Range(1, 24)] public int cornerSegments = 4;

        [Header("Output")]
        public bool generateTop = true;
        public bool generateBottom;
        public bool generateCollider = true;
        [Min(0.001f)] public float uvScale = 1f;
        public bool rebuildInEditor = true;
        public Material material;

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        MeshCollider meshCollider;
        Mesh generatedMesh;

        readonly List<Vector2> lastOuterPolygon = new List<Vector2>();
        readonly List<Vector2> lastInnerPolygon = new List<Vector2>();

        public IReadOnlyList<Vector2> LastOuterPolygon { get { return lastOuterPolygon; } }
        public IReadOnlyList<Vector2> LastInnerPolygon { get { return lastInnerPolygon; } }

        void OnEnable()
        {
            Build();
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (rebuildInEditor)
                Build();
        }
#endif

        [ContextMenu("Build")]
        public void Build()
        {
            EnsureComponents();

            List<Vector2> outer = ResolveOuterPolygon();
            lastOuterPolygon.Clear();
            lastInnerPolygon.Clear();

            if (outer.Count < 3)
                return;

            if (OuterWallGeometry.HasSelfIntersection(outer))
            {
                Debug.LogWarning("OuterWall polygon has self intersections.", this);
                return;
            }

            outer = OuterWallGeometry.BuildRoundedPolygon(outer, cornerRadius, cornerSegments);
            if (OuterWallGeometry.SignedArea(outer) < 0f)
                outer.Reverse();

            List<Vector2> inner = null;
            if (thickness > 0f)
            {
                inner = OuterWallGeometry.OffsetPolygon(outer, thickness);
                if (inner.Count < 3 || OuterWallGeometry.HasSelfIntersection(inner) || Mathf.Abs(OuterWallGeometry.SignedArea(inner)) < 0.0001f)
                {
                    Debug.LogWarning("OuterWall inner polygon is invalid. Lower thickness or simplify the shape.", this);
                    inner = null;
                }
                else if (OuterWallGeometry.SignedArea(inner) < 0f)
                {
                    inner.Reverse();
                }
            }

            lastOuterPolygon.AddRange(outer);
            if (inner != null)
                lastInnerPolygon.AddRange(inner);

            BuildMesh(outer, inner);
            ApplyMaterial();
        }

        public void ClearPoints()
        {
            manualPoints.Clear();
            pathPoints.Clear();
            occupiedCells.Clear();
            Build();
        }

        public Transform AddManualPoint()
        {
            GameObject pointObject = new GameObject("OuterWallPoint_" + manualPoints.Count);
            pointObject.transform.SetParent(transform, false);

            if (manualPoints.Count > 0 && manualPoints[manualPoints.Count - 1] != null)
                pointObject.transform.localPosition = manualPoints[manualPoints.Count - 1].localPosition + Vector3.right;

            manualPoints.Add(pointObject.transform);
            Build();
            return pointObject.transform;
        }

        public List<Vector2> ResolveOuterPolygon()
        {
            switch (mode)
            {
                case SourceMode.ManualPoints:
                    return BuildManualPointPolygon();
                case SourceMode.GridRect:
                    return OuterWallGeometry.BuildGridRect(gridWidth, gridHeight, cellSize);
                case SourceMode.GridMask:
                    return OuterWallGeometry.BuildGridMaskOutline(occupiedCells, cellSize);
                case SourceMode.PathPoints:
                    return new List<Vector2>(pathPoints);
                default:
                    return new List<Vector2>();
            }
        }

        List<Vector2> BuildManualPointPolygon()
        {
            var polygon = new List<Vector2>();
            if (manualPoints == null)
                return polygon;

            foreach (Transform point in manualPoints)
            {
                if (point == null)
                    return new List<Vector2>();

                Vector3 local = transform.InverseTransformPoint(point.position);
                polygon.Add(new Vector2(local.x, local.z));
            }

            return polygon;
        }

        void BuildMesh(IReadOnlyList<Vector2> outer, IReadOnlyList<Vector2> inner)
        {
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var uvs = new List<Vector2>();

            if (inner == null || inner.Count != outer.Count)
                BuildSolidPrism(outer, vertices, triangles, uvs);
            else
                BuildWallRing(outer, inner, vertices, triangles, uvs);

            if (generatedMesh == null)
            {
                generatedMesh = new Mesh();
                generatedMesh.name = "OuterWall";
                generatedMesh.MarkDynamic();
            }
            else
            {
                generatedMesh.Clear();
            }

            generatedMesh.SetVertices(vertices);
            generatedMesh.SetTriangles(triangles, 0);
            generatedMesh.SetUVs(0, uvs);
            generatedMesh.RecalculateBounds();
            generatedMesh.RecalculateNormals();

            meshFilter.sharedMesh = generatedMesh;

            if (generateCollider)
            {
                meshCollider = GetComponent<MeshCollider>();
                if (meshCollider == null)
                    meshCollider = gameObject.AddComponent<MeshCollider>();

                meshCollider.sharedMesh = null;
                meshCollider.sharedMesh = generatedMesh;
            }
            else if (meshCollider != null)
            {
                meshCollider.sharedMesh = null;
            }
        }

        void BuildSolidPrism(IReadOnlyList<Vector2> polygon, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            int count = polygon.Count;
            List<int> topTriangles = OuterWallTriangulator.Triangulate(polygon);
            Bounds2D bounds = new Bounds2D(polygon);

            int topStart = vertices.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 p = polygon[i];
                vertices.Add(new Vector3(p.x, height, p.y));
                uvs.Add(bounds.PlanarUv(p, uvScale));
            }

            if (generateTop)
            {
                for (int i = 0; i < topTriangles.Count; i += 3)
                {
                    triangles.Add(topStart + topTriangles[i]);
                    triangles.Add(topStart + topTriangles[i + 2]);
                    triangles.Add(topStart + topTriangles[i + 1]);
                }
            }

            int bottomStart = vertices.Count;
            if (generateBottom)
            {
                for (int i = 0; i < count; i++)
                {
                    Vector2 p = polygon[i];
                    vertices.Add(new Vector3(p.x, 0f, p.y));
                    uvs.Add(bounds.PlanarUv(p, uvScale));
                }

                for (int i = 0; i < topTriangles.Count; i += 3)
                {
                    triangles.Add(bottomStart + topTriangles[i + 2]);
                    triangles.Add(bottomStart + topTriangles[i + 1]);
                    triangles.Add(bottomStart + topTriangles[i]);
                }
            }

            AddVerticalStrip(polygon, false, vertices, triangles, uvs);
        }

        void BuildWallRing(IReadOnlyList<Vector2> outer, IReadOnlyList<Vector2> inner, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            AddVerticalStrip(outer, false, vertices, triangles, uvs);
            AddVerticalStrip(inner, true, vertices, triangles, uvs);

            if (generateTop)
                AddHorizontalRing(outer, inner, height, false, vertices, triangles, uvs);

            if (generateBottom)
                AddHorizontalRing(outer, inner, 0f, true, vertices, triangles, uvs);
        }

        void AddVerticalStrip(IReadOnlyList<Vector2> polygon, bool inward, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            float accumulated = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                int next = (i + 1) % polygon.Count;
                Vector2 p0 = polygon[i];
                Vector2 p1 = polygon[next];
                float edgeLength = Vector2.Distance(p0, p1) / Mathf.Max(0.001f, uvScale);
                int start = vertices.Count;

                vertices.Add(new Vector3(p0.x, height, p0.y));
                vertices.Add(new Vector3(p1.x, height, p1.y));
                vertices.Add(new Vector3(p1.x, 0f, p1.y));
                vertices.Add(new Vector3(p0.x, 0f, p0.y));

                uvs.Add(new Vector2(accumulated, 1f));
                uvs.Add(new Vector2(accumulated + edgeLength, 1f));
                uvs.Add(new Vector2(accumulated + edgeLength, 0f));
                uvs.Add(new Vector2(accumulated, 0f));
                accumulated += edgeLength;

                if (inward)
                {
                    triangles.Add(start + 0);
                    triangles.Add(start + 1);
                    triangles.Add(start + 2);
                    triangles.Add(start + 0);
                    triangles.Add(start + 2);
                    triangles.Add(start + 3);
                }
                else
                {
                    triangles.Add(start + 0);
                    triangles.Add(start + 2);
                    triangles.Add(start + 1);
                    triangles.Add(start + 0);
                    triangles.Add(start + 3);
                    triangles.Add(start + 2);
                }
            }
        }

        void AddHorizontalRing(IReadOnlyList<Vector2> outer, IReadOnlyList<Vector2> inner, float y, bool bottom, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
        {
            int count = outer.Count;
            Bounds2D bounds = new Bounds2D(outer);

            for (int i = 0; i < count; i++)
            {
                int next = (i + 1) % count;
                Vector2 outerA = outer[i];
                Vector2 outerB = outer[next];
                Vector2 innerA = inner[i];
                Vector2 innerB = inner[next];
                int start = vertices.Count;

                vertices.Add(new Vector3(outerA.x, y, outerA.y));
                vertices.Add(new Vector3(outerB.x, y, outerB.y));
                vertices.Add(new Vector3(innerB.x, y, innerB.y));
                vertices.Add(new Vector3(innerA.x, y, innerA.y));

                uvs.Add(bounds.PlanarUv(outerA, uvScale));
                uvs.Add(bounds.PlanarUv(outerB, uvScale));
                uvs.Add(bounds.PlanarUv(innerB, uvScale));
                uvs.Add(bounds.PlanarUv(innerA, uvScale));

                if (bottom)
                {
                    triangles.Add(start + 0);
                    triangles.Add(start + 2);
                    triangles.Add(start + 1);
                    triangles.Add(start + 0);
                    triangles.Add(start + 3);
                    triangles.Add(start + 2);
                }
                else
                {
                    triangles.Add(start + 0);
                    triangles.Add(start + 1);
                    triangles.Add(start + 2);
                    triangles.Add(start + 0);
                    triangles.Add(start + 2);
                    triangles.Add(start + 3);
                }
            }
        }

        void EnsureComponents()
        {
            if (meshFilter == null)
                meshFilter = GetComponent<MeshFilter>();
            if (meshRenderer == null)
                meshRenderer = GetComponent<MeshRenderer>();
            if (meshCollider == null)
                meshCollider = GetComponent<MeshCollider>();
        }

        void ApplyMaterial()
        {
            if (material != null && meshRenderer != null)
                meshRenderer.sharedMaterial = material;
        }

        void OnDrawGizmosSelected()
        {
            List<Vector2> polygon = ResolveOuterPolygon();
            if (polygon.Count < 2)
                return;

            Gizmos.color = Color.yellow;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector3 a = transform.TransformPoint(new Vector3(polygon[i].x, 0f, polygon[i].y));
                Vector2 next = polygon[(i + 1) % polygon.Count];
                Vector3 b = transform.TransformPoint(new Vector3(next.x, 0f, next.y));
                Gizmos.DrawLine(a, b);
            }
        }

        struct Bounds2D
        {
            readonly Vector2 min;
            readonly Vector2 size;

            public Bounds2D(IReadOnlyList<Vector2> points)
            {
                min = points[0];
                Vector2 max = points[0];
                for (int i = 1; i < points.Count; i++)
                {
                    min = Vector2.Min(min, points[i]);
                    max = Vector2.Max(max, points[i]);
                }

                size = max - min;
            }

            public Vector2 PlanarUv(Vector2 point, float scale)
            {
                float safeScale = Mathf.Max(0.001f, scale);
                return new Vector2(
                    (point.x - min.x) / Mathf.Max(size.x, 0.001f) / safeScale,
                    (point.y - min.y) / Mathf.Max(size.y, 0.001f) / safeScale);
            }
        }
    }
}
