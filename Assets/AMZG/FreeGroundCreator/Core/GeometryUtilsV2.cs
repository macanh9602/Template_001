using System.Collections.Generic;
using UnityEngine;

public static class GeometryUtilsV2
{
    public static bool IsClockwise(List<Vector2> polygon)
    {
        float area = 0f;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];

            area += (b.x - a.x) * (b.y + a.y);
        }

        return area > 0f;
    }

    public static float PolygonArea(List<Vector2> polygon)
    {
        float area = 0f;

        for (int i = 0; i < polygon.Count; i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[(i + 1) % polygon.Count];

            area += a.x * b.y - b.x * a.y;
        }

        return area * 0.5f;
    }

    public static bool IsConvexCorner(
        Vector2 prev,
        Vector2 current,
        Vector2 next,
        bool polygonClockwise)
    {
        Vector2 a = (current - prev).normalized;
        Vector2 b = (next - current).normalized;

        float cross =
            a.x * b.y -
            a.y * b.x;

        return polygonClockwise
            ? cross < 0f
            : cross > 0f;
    }

    public static List<Vector3> BuildRoundedPolygon(
    List<Vector3> source,
    float radius,
    int segments)
    {
        List<Vector3> result = new();

        if (radius <= 0f)
        {
            result.AddRange(source);
            return result;
        }

        int count = source.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 prev =
                source[(i - 1 + count) % count];

            Vector2 current =
                source[i];

            Vector2 next =
                source[(i + 1) % count];

            var arc =
                FilletCorner(
                    prev,
                    current,
                    next,
                    radius,
                    segments);

            if (arc.Count == 0)
                continue;

            // tránh duplicate point
            if (result.Count > 0)
                arc.RemoveAt(0);

            result.AddRange(arc);
        }

        return result;
    }

    public static List<Vector3> FilletCorner(Vector3 p0, Vector3 pc, Vector3 p1, float radius, int segments)
    {
        var points = new List<Vector3>();

        Vector3 v0 = (p0 - pc).normalized;
        Vector3 v1 = (p1 - pc).normalized;

        float angle = Mathf.Acos(Mathf.Clamp(Vector2.Dot(v0, v1), -1f, 1f));
        if (angle < 1e-4f)
        {
            points.Add(pc);
            return points;
        }

        float tanHalf = Mathf.Tan(angle / 2f);
        float dist = radius / tanHalf;

        float len0 = (p0 - pc).magnitude;
        float len1 = (p1 - pc).magnitude;
        dist = Mathf.Min(dist, Mathf.Min(len0, len1) * 0.999f);

        Vector3 p0a = pc + v0 * dist;
        Vector3 p1a = pc + v1 * dist;

        Vector3 n0 = new(-v0.y, v0.x);
        Vector3 n1 = new(-v1.y, v1.x);

        if (!LineLine(p0a, n0, p1a, n1, out Vector2 center))
        {
            points.Add(pc);
            return points;
        }

        float startAng = Mathf.Atan2(p0a.y - center.y, p0a.x - center.x);
        float endAng = Mathf.Atan2(p1a.y - center.y, p1a.x - center.x);

        float cross = v0.x * v1.y - v0.y * v1.x;
        bool convex = cross < 0;
        if (convex)
        {
            if (endAng < startAng) endAng += Mathf.PI * 2f;
        }
        else
        {
            if (endAng > startAng) endAng -= Mathf.PI * 2f;
        }

        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float ang = Mathf.Lerp(startAng, endAng, t);
            Vector2 p = center + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
            points.Add(p);
        }

        return points;
    }

    static bool LineLine(Vector2 p, Vector2 d, Vector2 q, Vector2 e, out Vector2 x)
    {
        float den = d.x * e.y - d.y * e.x;
        if (Mathf.Abs(den) < 1e-6f) { x = default; return false; }
        float t = ((q.x - p.x) * e.y - (q.y - p.y) * e.x) / den;
        x = p + d * t;
        return true;
    }

    public static bool HasSelfIntersection(
        List<Vector3> polygon)
    {
        int count = polygon.Count;

        for (int i = 0; i < count; i++)
        {
            Vector2 a1 = polygon[i];
            Vector2 a2 = polygon[(i + 1) % count];

            for (int j = i + 1; j < count; j++)
            {
                if (Mathf.Abs(i - j) <= 1)
                    continue;

                if (i == 0 &&
                    j == count - 1)
                    continue;

                Vector2 b1 = polygon[j];
                Vector2 b2 = polygon[(j + 1) % count];

                if (SegmentsIntersect(
                        a1,
                        a2,
                        b1,
                        b2))
                {
                    return true;
                }
            }
        }

        return false;
    }

    static bool SegmentsIntersect(
        Vector2 p1,
        Vector2 p2,
        Vector2 q1,
        Vector2 q2)
    {
        float o1 = Orientation(p1, p2, q1);
        float o2 = Orientation(p1, p2, q2);

        float o3 = Orientation(q1, q2, p1);
        float o4 = Orientation(q1, q2, p2);

        return o1 * o2 < 0f &&
               o3 * o4 < 0f;
    }

    static float Orientation(
        Vector2 a,
        Vector2 b,
        Vector2 c)
    {
        return
            (b.x - a.x) * (c.y - a.y)
          - (b.y - a.y) * (c.x - a.x);
    }
}