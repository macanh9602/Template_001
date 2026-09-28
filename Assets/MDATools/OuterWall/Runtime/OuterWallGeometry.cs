using System.Collections.Generic;
using UnityEngine;

namespace AMZG.Helpers.OuterWall
{
    public static class OuterWallGeometry
    {
        public static float SignedArea(IReadOnlyList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 3)
                return 0f;

            float area = 0f;
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 a = polygon[i];
                Vector2 b = polygon[(i + 1) % polygon.Count];
                area += a.x * b.y - b.x * a.y;
            }

            return area * 0.5f;
        }

        public static bool IsClockwise(IReadOnlyList<Vector2> polygon)
        {
            return SignedArea(polygon) < 0f;
        }

        public static bool HasSelfIntersection(IReadOnlyList<Vector2> polygon)
        {
            if (polygon == null || polygon.Count < 4)
                return false;

            int count = polygon.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 a1 = polygon[i];
                Vector2 a2 = polygon[(i + 1) % count];

                for (int j = i + 1; j < count; j++)
                {
                    if (Mathf.Abs(i - j) <= 1)
                        continue;

                    if (i == 0 && j == count - 1)
                        continue;

                    Vector2 b1 = polygon[j];
                    Vector2 b2 = polygon[(j + 1) % count];
                    if (SegmentsIntersect(a1, a2, b1, b2))
                        return true;
                }
            }

            return false;
        }

        public static List<Vector2> BuildRoundedPolygon(IReadOnlyList<Vector2> source, float radius, int segments)
        {
            var result = new List<Vector2>();
            if (source == null)
                return result;

            if (radius <= 0f || segments <= 1)
            {
                result.AddRange(source);
                return result;
            }

            int count = source.Count;
            for (int i = 0; i < count; i++)
            {
                Vector2 previous = source[(i - 1 + count) % count];
                Vector2 current = source[i];
                Vector2 next = source[(i + 1) % count];

                List<Vector2> arc = FilletCorner(previous, current, next, radius, segments);
                if (arc.Count == 0)
                    continue;

                if (result.Count > 0)
                    arc.RemoveAt(0);

                result.AddRange(arc);
            }

            return result;
        }

        public static List<Vector2> OffsetPolygon(IReadOnlyList<Vector2> polygon, float distance)
        {
            var result = new List<Vector2>();
            if (polygon == null || polygon.Count < 3 || Mathf.Approximately(distance, 0f))
            {
                if (polygon != null)
                    result.AddRange(polygon);
                return result;
            }

            int count = polygon.Count;
            bool clockwise = IsClockwise(polygon);

            for (int i = 0; i < count; i++)
            {
                Vector2 previous = polygon[(i - 1 + count) % count];
                Vector2 current = polygon[i];
                Vector2 next = polygon[(i + 1) % count];

                Vector2 previousDirection = (current - previous).normalized;
                Vector2 nextDirection = (next - current).normalized;
                Vector2 previousNormal = InwardNormal(previousDirection, clockwise);
                Vector2 nextNormal = InwardNormal(nextDirection, clockwise);

                Vector2 pA = previous + previousNormal * distance;
                Vector2 pC = current + nextNormal * distance;

                if (LineLine(pA, previousDirection, pC, nextDirection, out Vector2 intersection))
                    result.Add(intersection);
                else
                    result.Add(current + (previousNormal + nextNormal).normalized * distance);
            }

            return result;
        }

        public static List<Vector2> BuildGridRect(float widthCells, float heightCells, float cellSize)
        {
            float width = Mathf.Max(1f, widthCells) * Mathf.Max(0.001f, cellSize);
            float height = Mathf.Max(1f, heightCells) * Mathf.Max(0.001f, cellSize);
            float halfWidth = width * 0.5f;
            float halfHeight = height * 0.5f;

            return new List<Vector2>
            {
                new Vector2(-halfWidth, -halfHeight),
                new Vector2(halfWidth, -halfHeight),
                new Vector2(halfWidth, halfHeight),
                new Vector2(-halfWidth, halfHeight)
            };
        }

        public static List<Vector2> BuildGridMaskOutline(IReadOnlyList<Vector2Int> occupiedCells, float cellSize)
        {
            var result = new List<Vector2>();
            if (occupiedCells == null || occupiedCells.Count == 0)
                return result;

            float size = Mathf.Max(0.001f, cellSize);
            var occupied = new HashSet<Vector2Int>(occupiedCells);
            var edges = new List<Edge>();

            foreach (Vector2Int cell in occupied)
            {
                Vector2 min = new Vector2(cell.x * size, cell.y * size);
                Vector2 max = min + Vector2.one * size;

                if (!occupied.Contains(cell + Vector2Int.down))
                    edges.Add(new Edge(new Vector2(min.x, min.y), new Vector2(max.x, min.y)));
                if (!occupied.Contains(cell + Vector2Int.right))
                    edges.Add(new Edge(new Vector2(max.x, min.y), new Vector2(max.x, max.y)));
                if (!occupied.Contains(cell + Vector2Int.up))
                    edges.Add(new Edge(new Vector2(max.x, max.y), new Vector2(min.x, max.y)));
                if (!occupied.Contains(cell + Vector2Int.left))
                    edges.Add(new Edge(new Vector2(min.x, max.y), new Vector2(min.x, min.y)));
            }

            if (edges.Count == 0)
                return result;

            var remaining = new List<Edge>(edges);
            var loops = new List<List<Vector2>>();
            while (remaining.Count > 0)
            {
                Edge first = remaining[0];
                remaining.RemoveAt(0);

                var loop = new List<Vector2> { first.Start, first.End };
                Vector2 cursor = first.End;
                int guard = 0;

                while (remaining.Count > 0 && guard < 10000)
                {
                    guard++;
                    int nextIndex = remaining.FindIndex(edge => Approximately(edge.Start, cursor));
                    if (nextIndex < 0)
                        break;

                    Edge next = remaining[nextIndex];
                    remaining.RemoveAt(nextIndex);
                    cursor = next.End;

                    if (Approximately(cursor, loop[0]))
                        break;

                    loop.Add(cursor);
                }

                if (loop.Count >= 3)
                    loops.Add(RemoveCollinear(loop));
            }

            float largestArea = 0f;
            foreach (List<Vector2> loop in loops)
            {
                float area = Mathf.Abs(SignedArea(loop));
                if (area > largestArea)
                {
                    largestArea = area;
                    result = loop;
                }
            }

            if (result.Count > 0)
                CenterPolygon(result);

            return result;
        }

        static List<Vector2> FilletCorner(Vector2 p0, Vector2 current, Vector2 p1, float radius, int segments)
        {
            var points = new List<Vector2>();
            Vector2 v0 = (p0 - current).normalized;
            Vector2 v1 = (p1 - current).normalized;

            float angle = Mathf.Acos(Mathf.Clamp(Vector2.Dot(v0, v1), -1f, 1f));
            if (angle < 0.0001f)
            {
                points.Add(current);
                return points;
            }

            float tanHalf = Mathf.Tan(angle * 0.5f);
            float distance = radius / Mathf.Max(tanHalf, 0.0001f);
            distance = Mathf.Min(distance, Mathf.Min(Vector2.Distance(p0, current), Vector2.Distance(p1, current)) * 0.999f);

            Vector2 p0a = current + v0 * distance;
            Vector2 p1a = current + v1 * distance;
            Vector2 n0 = new Vector2(-v0.y, v0.x);
            Vector2 n1 = new Vector2(-v1.y, v1.x);

            if (!LineLine(p0a, n0, p1a, n1, out Vector2 center))
            {
                points.Add(current);
                return points;
            }

            float startAngle = Mathf.Atan2(p0a.y - center.y, p0a.x - center.x);
            float endAngle = Mathf.Atan2(p1a.y - center.y, p1a.x - center.x);
            float cross = v0.x * v1.y - v0.y * v1.x;
            bool convex = cross < 0f;

            if (convex)
            {
                if (endAngle < startAngle)
                    endAngle += Mathf.PI * 2f;
            }
            else
            {
                if (endAngle > startAngle)
                    endAngle -= Mathf.PI * 2f;
            }

            for (int i = 0; i <= segments; i++)
            {
                float t = i / (float)segments;
                float angleAtT = Mathf.Lerp(startAngle, endAngle, t);
                points.Add(center + new Vector2(Mathf.Cos(angleAtT), Mathf.Sin(angleAtT)) * radius);
            }

            return points;
        }

        static Vector2 InwardNormal(Vector2 direction, bool clockwise)
        {
            return clockwise ? new Vector2(direction.y, -direction.x) : new Vector2(-direction.y, direction.x);
        }

        static bool LineLine(Vector2 p, Vector2 d, Vector2 q, Vector2 e, out Vector2 x)
        {
            float denominator = d.x * e.y - d.y * e.x;
            if (Mathf.Abs(denominator) < 0.000001f)
            {
                x = default;
                return false;
            }

            float t = ((q.x - p.x) * e.y - (q.y - p.y) * e.x) / denominator;
            x = p + d * t;
            return true;
        }

        static bool SegmentsIntersect(Vector2 p1, Vector2 p2, Vector2 q1, Vector2 q2)
        {
            float o1 = Orientation(p1, p2, q1);
            float o2 = Orientation(p1, p2, q2);
            float o3 = Orientation(q1, q2, p1);
            float o4 = Orientation(q1, q2, p2);
            return o1 * o2 < 0f && o3 * o4 < 0f;
        }

        static float Orientation(Vector2 a, Vector2 b, Vector2 c)
        {
            return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        }

        static bool Approximately(Vector2 a, Vector2 b)
        {
            return (a - b).sqrMagnitude < 0.000001f;
        }

        static List<Vector2> RemoveCollinear(List<Vector2> source)
        {
            var result = new List<Vector2>();
            for (int i = 0; i < source.Count; i++)
            {
                Vector2 previous = source[(i - 1 + source.Count) % source.Count];
                Vector2 current = source[i];
                Vector2 next = source[(i + 1) % source.Count];
                Vector2 a = (current - previous).normalized;
                Vector2 b = (next - current).normalized;

                if (Mathf.Abs(a.x * b.y - a.y * b.x) > 0.0001f)
                    result.Add(current);
            }

            return result;
        }

        static void CenterPolygon(List<Vector2> polygon)
        {
            if (polygon.Count == 0)
                return;

            Vector2 min = polygon[0];
            Vector2 max = polygon[0];
            foreach (Vector2 point in polygon)
            {
                min = Vector2.Min(min, point);
                max = Vector2.Max(max, point);
            }

            Vector2 center = (min + max) * 0.5f;
            for (int i = 0; i < polygon.Count; i++)
                polygon[i] -= center;
        }

        readonly struct Edge
        {
            public readonly Vector2 Start;
            public readonly Vector2 End;

            public Edge(Vector2 start, Vector2 end)
            {
                Start = start;
                End = end;
            }
        }
    }
}


