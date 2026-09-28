using System.Collections.Generic;
using UnityEngine;

public static class EarClippingTriangulator
{
    public static List<int> Triangulate(List<Vector3> points)
    {
        List<int> triangles = new();

        int n = points.Count;
        if (n < 3)
            return triangles;

        List<int> indices = new();

        if (SignedArea(points) > 0f)
        {
            for (int i = 0; i < n; i++)
                indices.Add(i);
        }
        else
        {
            for (int i = n - 1; i >= 0; i--)
                indices.Add(i);
        }

        int guard = 0;

        while (indices.Count > 2)
        {
            guard++;

            if (guard > 10000)
            {
                Debug.LogWarning("Triangulation failed.");
                return triangles;
            }

            bool earFound = false;

            for (int i = 0; i < indices.Count; i++)
            {
                int prev = indices[(i - 1 + indices.Count) % indices.Count];
                int curr = indices[i];
                int next = indices[(i + 1) % indices.Count];

                Vector2 a = points[prev];
                Vector2 b = points[curr];
                Vector2 c = points[next];

                if (!IsConvex(a, b, c))
                    continue;

                bool containsPoint = false;

                for (int j = 0; j < indices.Count; j++)
                {
                    int test = indices[j];

                    if (test == prev ||
                        test == curr ||
                        test == next)
                        continue;

                    if (PointInTriangle(
                            points[test],
                            a,
                            b,
                            c))
                    {
                        containsPoint = true;
                        break;
                    }
                }

                if (containsPoint)
                    continue;

                triangles.Add(prev);
                triangles.Add(curr);
                triangles.Add(next);

                indices.RemoveAt(i);

                earFound = true;
                break;
            }

            if (!earFound)
            {
                Debug.LogWarning(
                    "No ear found. Polygon may be self-intersecting.");
                return triangles;
            }
        }

        return triangles;
    }

    static float SignedArea(List<Vector3> points)
    {
        float area = 0f;

        for (int i = 0; i < points.Count; i++)
        {
            Vector2 p0 = points[i];
            Vector2 p1 = points[(i + 1) % points.Count];

            area += p0.x * p1.y - p1.x * p0.y;
        }

        return area * 0.5f;
    }

    static bool IsConvex(
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        return Cross(b - a, c - b) > 0f;
    }

    static float Cross(
        Vector2 a,
        Vector2 b)
    {
        return a.x * b.y - a.y * b.x;
    }

    static bool PointInTriangle(
        Vector2 p,
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        float c1 = Cross(b - a, p - a);
        float c2 = Cross(c - b, p - b);
        float c3 = Cross(a - c, p - c);

        bool hasNeg =
            (c1 < 0) ||
            (c2 < 0) ||
            (c3 < 0);

        bool hasPos =
            (c1 > 0) ||
            (c2 > 0) ||
            (c3 > 0);

        return !(hasNeg && hasPos);
    }
}