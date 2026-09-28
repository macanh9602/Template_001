using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class PolygonExtrudeGenerator : MonoBehaviour
{
    [Header("Shape")]
    public List<Transform> points = new();

    [Min(0)]
    public float height = 1f;

    [Header("Rounded Corners")]
    public float cornerRadius = 0f;

    [Range(1, 16)]
    public int cornerSegments = 4;

    [Header("Options")]
    public bool generateBottom = true;
    public bool generateCollider = true;

    MeshFilter meshFilter;
    Mesh generatedMesh;

    void OnEnable()
    {
        Build();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        Build();
    }
#endif

    [ContextMenu("Build")]
    public void Build()
    {
        if (points == null || points.Count < 3)
            return;

        meshFilter ??= GetComponent<MeshFilter>();

        List<Vector3> polygon = new();

        foreach (var t in points)
        {
            if (t == null)
                return;

            Vector3 local =
                transform.InverseTransformPoint(
                    t.position);

            polygon.Add(
                new Vector2(
                    local.x,
                    local.z));
        }

        if (GeometryUtilsV2.HasSelfIntersection(polygon))
        {
            Debug.LogWarning(
                "Polygon has self intersections.",
                this);

            return;
        }

        List<Vector3> rounded =
            GeometryUtilsV2.BuildRoundedPolygon(
                polygon,
                cornerRadius,
                cornerSegments);

        BuildMesh(rounded);
    }

    void BuildMesh(List<Vector3> polygon)
    {
        int count = polygon.Count;

        if (count < 3)
            return;

        List<int> topTris =
            EarClippingTriangulator
                .Triangulate(polygon);

        List<Vector3> vertices = new();
        List<int> triangles = new();
        List<Vector2> uvs = new();

        //--------------------------------------------------
        // bounds for UV
        //--------------------------------------------------

        Vector2 min = polygon[0];
        Vector2 max = polygon[0];

        foreach (var p in polygon)
        {
            min = Vector2.Min(min, p);
            max = Vector2.Max(max, p);
        }

        Vector2 size = max - min;

        //--------------------------------------------------
        // top
        //--------------------------------------------------

        int topStart = vertices.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 p = polygon[i];

            vertices.Add(
                new Vector3(
                    p.x,
                    height,
                    p.y));

            uvs.Add(
                new Vector2(
                    (p.x - min.x) / Mathf.Max(size.x, 0.001f),
                    (p.y - min.y) / Mathf.Max(size.y, 0.001f)));
        }

        for (int i = 0; i < topTris.Count; i += 3)
        {
            triangles.Add(topStart + topTris[i]);
            triangles.Add(topStart + topTris[i + 2]);
            triangles.Add(topStart + topTris[i + 1]);
        }

        //--------------------------------------------------
        // bottom
        //--------------------------------------------------

        int bottomStart = vertices.Count;

        if (generateBottom)
        {
            for (int i = 0; i < count; i++)
            {
                Vector2 p = polygon[i];

                vertices.Add(
                    new Vector3(
                        p.x,
                        0f,
                        p.y));

                uvs.Add(
                    new Vector2(
                        (p.x - min.x) / Mathf.Max(size.x, 0.001f),
                        (p.y - min.y) / Mathf.Max(size.y, 0.001f)));
            }

            for (int i = 0; i < topTris.Count; i += 3)
            {
                triangles.Add(bottomStart + topTris[i + 2]);
                triangles.Add(bottomStart + topTris[i + 1]);
                triangles.Add(bottomStart + topTris[i]);
            }
        }

        //--------------------------------------------------
        // sides
        //--------------------------------------------------

        float accumulatedU = 0f;

        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;

            Vector2 p0 = polygon[i];
            Vector2 p1 = polygon[next];

            float edgeLength =
                Vector2.Distance(p0, p1);

            int vStart = vertices.Count;

            vertices.Add(
                new Vector3(
                    p0.x,
                    height,
                    p0.y));

            vertices.Add(
                new Vector3(
                    p1.x,
                    height,
                    p1.y));

            vertices.Add(
                new Vector3(
                    p1.x,
                    0,
                    p1.y));

            vertices.Add(
                new Vector3(
                    p0.x,
                    0,
                    p0.y));

            uvs.Add(new Vector2(accumulatedU, 1));
            uvs.Add(new Vector2(accumulatedU + edgeLength, 1));
            uvs.Add(new Vector2(accumulatedU + edgeLength, 0));
            uvs.Add(new Vector2(accumulatedU, 0));

            accumulatedU += edgeLength;

            triangles.Add(vStart + 0);
            triangles.Add(vStart + 2);
            triangles.Add(vStart + 1);

            triangles.Add(vStart + 0);
            triangles.Add(vStart + 3);
            triangles.Add(vStart + 2);
        }

        //--------------------------------------------------
        // mesh
        //--------------------------------------------------

        if (generatedMesh == null)
        {
            generatedMesh = new Mesh();
            generatedMesh.name = "PolygonExtrude";
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
            MeshCollider mc =
                GetComponent<MeshCollider>();

            if (mc == null)
                mc = gameObject.AddComponent<MeshCollider>();

            mc.sharedMesh = null;
            mc.sharedMesh = generatedMesh;
        }
    }

    void OnDrawGizmosSelected()
    {
        if (points == null)
            return;

        Gizmos.color = Color.yellow;

        for (int i = 0; i < points.Count; i++)
        {
            if (points[i] == null)
                continue;

            Transform a = points[i];
            Transform b =
                points[(i + 1) % points.Count];

            if (b == null)
                continue;

            Gizmos.DrawLine(
                a.position,
                b.position);
        }
    }
}