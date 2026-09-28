using System.Collections.Generic;
using UnityEngine;

namespace AMZG.Helpers.OuterWall
{
    public static class OuterWallTriangulator
    {
        public static List<int> Triangulate(IReadOnlyList<Vector2> points)
        {
            var triangles = new List<int>();
            int count = points == null ? 0 : points.Count;
            if (count < 3)
                return triangles;

            var indices = new List<int>(count);
            if (SignedArea(points) > 0f)
            {
                for (int i = 0; i < count; i++)
                    indices.Add(i);
            }
            else
            {
                for (int i = count - 1; i >= 0; i--)
                    indices.Add(i);
            }

            int guard = 0;
            while (indices.Count > 2)
            {
                guard++;
                if (guard > 10000)
                {
                    Debug.LogWarning("OuterWall triangulation failed. Polygon may be invalid.");
                    return triangles;
                }

                bool earFound = false;
                for (int i = 0; i < indices.Count; i++)
                {
                    int previous = indices[(i - 1 + indices.Count) % indices.Count];
                    int current = indices[i];
                    int next = indices[(i + 1) % indices.Count];

                    Vector2 a = points[previous];
                    Vector2 b = points[current];
                    Vector2 c = points[next];

                    if (!IsConvex(a, b, c))
                        continue;

                    bool containsPoint = false;
                    for (int j = 0; j < indices.Count; j++)
                    {
                        int test = indices[j];
                        if (test == previous || test == current || test == next)
                            continue;

                        if (PointInTriangle(points[test], a, b, c))
                        {
                            containsPoint = true;
                            break;
                        }
                    }

                    if (containsPoint)
                        continue;

                    triangles.Add(previous);
                    triangles.Add(current);
                    triangles.Add(next);
                    indices.RemoveAt(i);
                    earFound = true;
                    break;
                }

                if (!earFound)
                {
                    Debug.LogWarning("No outer-wall ear found. Polygon may be self-intersecting or too thin.");
                    return triangles;
                }
            }

            return triangles;
        }

        static float SignedArea(IReadOnlyList<Vector2> points)
        {
            float area = 0f;
            for (int i = 0; i < points.Count; i++)
            {
                Vector2 a = points[i];
                Vector2 b = points[(i + 1) % points.Count];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        static bool IsConvex(Vector2 a, Vector2 b, Vector2 c)
        {
            return Cross(b - a, c - b) > 0f;
        }

        static float Cross(Vector2 a, Vector2 b)
        {
            return a.x * b.y - a.y * b.x;
        }

        static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
        {
            float c1 = Cross(b - a, p - a);
            float c2 = Cross(c - b, p - b);
            float c3 = Cross(a - c, p - c);

            bool hasNegative = c1 < 0f || c2 < 0f || c3 < 0f;
            bool hasPositive = c1 > 0f || c2 > 0f || c3 > 0f;
            return !(hasNegative && hasPositive);
        }
    }
}
