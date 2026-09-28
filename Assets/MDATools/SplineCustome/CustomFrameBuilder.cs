using System.Collections.Generic;
using Dreamteck.Splines;
using NgoUyenNguyen.Line;
using Sirenix.OdinInspector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace BoardSpline.Runtime
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SplineComputer))]
    [RequireComponent(typeof(SplineMesh))]
    public sealed class CustomFrameBuilder : MonoBehaviour
    {
        private const string ChannelName = "Conveyor Frame";
        private const float Epsilon = 0.001f;
        private static readonly Vector3 DefaultMapTestMeshRotation = new Vector3(-90f, 90f, 90f);
        private static readonly Vector3 DefaultMapTestMeshRotationFlipped = new Vector3(-90f, -90f, 90f);

        [TitleGroup("Path")]
        [SerializeField] private Vector3[] centerPaths = new Vector3[0];

        [TitleGroup("Path")]
        [SerializeField] private bool editCenterPath;

        [TitleGroup("Path")]
        [SerializeField, Min(0f)] private float cornerRadius = 0.25f;

        [TitleGroup("Path")]
        [SerializeField, Min(1)] private int cornerSegments = 6;

        [TitleGroup("Path")]
        [SerializeField] private bool closed;

        [TitleGroup("Path")]
        [SerializeField] private Vector3 splineNormal = Vector3.up;

        [TitleGroup("Mesh")]
        [SerializeField] private Mesh uShapeCrossSection;

        [TitleGroup("Mesh")]
        [SerializeField] private Mesh exitSectionMesh;

        [TitleGroup("Mesh")]
        [SerializeField, Min(0f)] private float exitSectionLength = 2f;

        // Kept for callers that configure the builder directly. Conveyor exits now carry their own flip.
        private bool customMeshPresetFlipped;

        [TitleGroup("Custom Mesh")]
        [SerializeField] private bool customMeshUseMapTestPreset = true;

        [TitleGroup("Custom Mesh")]
        [SerializeField] private Vector3 customMeshRotation = DefaultMapTestMeshRotation;

        [TitleGroup("Custom Mesh")]
        [SerializeField] private Vector3 customMeshOffset = Vector3.zero;

        [TitleGroup("Custom Mesh")]
        [SerializeField] private Vector3 customMeshScale = Vector3.one;

        [TitleGroup("Mesh Override Test")]
        [SerializeField, Range(0f, 1f)] private float overrideAnchorPercent = 0.5f;

        [TitleGroup("Mesh Override Test")]
        [SerializeField, Min(0f)] private float overrideLength = 2f;

        [TitleGroup("Mesh Override Test")]
        [SerializeField] private Mesh overrideMesh;

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [SerializeField] private bool showRoundedPathGizmos = true;

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [ShowIf(nameof(showRoundedPathGizmos))]
        [SerializeField] private Color pathColor = new Color(0.15f, 0.85f, 1f, 0.9f);

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [SerializeField] private bool showCheckpointGizmo = false;

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [ShowIf(nameof(showCheckpointGizmo))]
        [SerializeField, Range(0f, 1f)] private float checkpointPercent = 0.5f;

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [ShowIf(nameof(showCheckpointGizmo))]
        [SerializeField] private Color checkpointColor = new Color(1f, 0.25f, 0.1f, 1f);

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [ShowIf(nameof(showCheckpointGizmo))]
        [SerializeField, Min(0.01f)] private float checkpointRadius = 0.12f;

        [FoldoutGroup("Debug Gizmos", Expanded = false)]
        [ShowIf(nameof(showRoundedPathGizmos))]
        [SerializeField] private bool showDebugLabels;

        private readonly List<float> exitPercents = new List<float>();
        private readonly List<bool> exitFlipped = new List<bool>();
        private readonly List<float> exitLengths = new List<float>();

        public Vector3[] CenterPaths
        {
            get => centerPaths;
            set => centerPaths = value ?? new Vector3[0];
        }

        public float CornerRadius
        {
            get => cornerRadius;
            set => cornerRadius = Mathf.Max(0f, value);
        }

        public int CornerSegments
        {
            get => cornerSegments;
            set => cornerSegments = Mathf.Max(1, value);
        }

        public bool Closed
        {
            get => closed;
            set => closed = value;
        }

        public Mesh UShapeCrossSection
        {
            get => uShapeCrossSection;
            set => uShapeCrossSection = value;
        }

        public Mesh ExitSectionMesh
        {
            get => exitSectionMesh;
            set => exitSectionMesh = value;
        }

        public float ExitSectionLength
        {
            get => exitSectionLength;
            set => exitSectionLength = Mathf.Max(0f, value);
        }

        public Vector3 SplineNormal
        {
            get => GetNormal();
            set => splineNormal = value == Vector3.zero ? Vector3.up : value.normalized;
        }

        public bool CustomMeshUseMapTestPreset
        {
            get => customMeshUseMapTestPreset;
            set => customMeshUseMapTestPreset = value;
        }

        /// <summary>
        /// When <see cref="CustomMeshUseMapTestPreset"/> is true, selects between the two preset
        /// rotations: false → (-90°,+90°,90°), true → (-90°,-90°,90°).
        /// </summary>
        public bool CustomMeshPresetFlipped
        {
            get => customMeshPresetFlipped;
            set => customMeshPresetFlipped = value;
        }

        public Vector3 CustomMeshRotation
        {
            get => GetCustomMeshRotation();
            set => customMeshRotation = value;
        }

        public Vector3 CustomMeshOffset
        {
            get => customMeshOffset;
            set => customMeshOffset = value;
        }

        public Vector3 CustomMeshScale
        {
            get => customMeshScale;
            set => customMeshScale = value == Vector3.zero ? Vector3.one : value;
        }

        public float OverrideAnchorPercent
        {
            get => overrideAnchorPercent;
            set => overrideAnchorPercent = Mathf.Clamp01(value);
        }

        public float OverrideLength
        {
            get => overrideLength;
            set => overrideLength = Mathf.Max(0f, value);
        }

        public Mesh OverrideMesh
        {
            get => overrideMesh;
            set => overrideMesh = value;
        }

        private void Reset()
        {
            splineNormal = Vector3.up;
            customMeshUseMapTestPreset = true;
            customMeshRotation = DefaultMapTestMeshRotation;
            customMeshOffset = Vector3.zero;
            customMeshScale = Vector3.one;
            exitSectionLength = 2f;
            overrideAnchorPercent = 0.5f;
            overrideLength = 2f;
        }

        private void OnValidate()
        {
            cornerRadius = Mathf.Max(0f, cornerRadius);
            cornerSegments = Mathf.Max(1, cornerSegments);
            if (splineNormal == Vector3.zero) splineNormal = Vector3.up;
            if (customMeshScale == Vector3.zero) customMeshScale = Vector3.one;
            exitSectionLength = Mathf.Max(0f, exitSectionLength);
            checkpointPercent = Mathf.Clamp01(checkpointPercent);
            checkpointRadius = Mathf.Max(0.01f, checkpointRadius);
            overrideAnchorPercent = Mathf.Clamp01(overrideAnchorPercent);
            overrideLength = Mathf.Max(0f, overrideLength);
        }

        public void SetPath(Vector3[] path, bool isClosed)
        {
            centerPaths = path ?? new Vector3[0];
            closed = isClosed;
        }

        public void SetExitPercents(IEnumerable<float> percents)
        {
            exitPercents.Clear();
            exitFlipped.Clear();
            exitLengths.Clear();
            if (percents == null)
                return;

            foreach (var percent in percents)
            {
                exitPercents.Add(Mathf.Clamp01(percent));
                exitFlipped.Add(false);
                exitLengths.Add(-1f);
            }
        }

        public void SetExits(IEnumerable<(float percent, bool flipped)> exits)
        {
            exitPercents.Clear();
            exitFlipped.Clear();
            exitLengths.Clear();
            if (exits == null)
                return;

            foreach (var exit in exits)
            {
                exitPercents.Add(Mathf.Clamp01(exit.percent));
                exitFlipped.Add(exit.flipped);
                exitLengths.Add(-1f);
            }
        }

        public void SetExits(IEnumerable<(float percent, bool flipped, float length)> exits)
        {
            exitPercents.Clear();
            exitFlipped.Clear();
            exitLengths.Clear();
            if (exits == null)
                return;

            foreach (var exit in exits)
            {
                exitPercents.Add(Mathf.Clamp01(exit.percent));
                exitFlipped.Add(exit.flipped);
                exitLengths.Add(Mathf.Max(0f, exit.length));
            }
        }

        public Vector3[] GetRoundedPath()
        {
            return CreateRoundedPath(centerPaths, cornerRadius, cornerSegments, closed);
        }

        public Vector3[] GetRoundedPathWorld(float yOffset = 0f, bool includeClosedSegment = true)
        {
            var localPath = GetRoundedPath();
            if (localPath == null || localPath.Length == 0)
                return new Vector3[0];

            int extraCount = includeClosedSegment && closed && localPath.Length > 1 ? 1 : 0;
            var worldPath = new Vector3[localPath.Length + extraCount];
            Vector3 offset = transform.TransformDirection(GetNormal()) * yOffset;
            for (var i = 0; i < localPath.Length; i++)
                worldPath[i] = transform.TransformPoint(localPath[i]) + offset;

            if (extraCount > 0)
                worldPath[worldPath.Length - 1] = worldPath[0];

            return worldPath;
        }

        public static float CalculateLength(IReadOnlyList<Vector3> path, bool closed)
        {
            var count = path?.Count ?? 0;
            if (count < 2) return 0f;

            var length = 0f;
            var segmentCount = closed ? count : count - 1;
            for (var i = 0; i < segmentCount; i++)
                length += Vector3.Distance(path[i], path[(i + 1) % count]);

            return length;
        }

        public static Vector3 SamplePathAtDistance(IReadOnlyList<Vector3> path, float distance, bool closed)
        {
            var count = path?.Count ?? 0;
            if (count == 0) return Vector3.zero;
            if (count == 1) return path[0];

            var totalLength = CalculateLength(path, closed);
            if (totalLength <= Epsilon) return path[0];

            distance = closed
                ? Mathf.Repeat(distance, totalLength)
                : Mathf.Clamp(distance, 0f, totalLength);

            var walked = 0f;
            var segmentCount = closed ? count : count - 1;
            for (var i = 0; i < segmentCount; i++)
            {
                var start = path[i];
                var end = path[(i + 1) % count];
                var segmentLength = Vector3.Distance(start, end);
                if (segmentLength <= Epsilon) continue;

                if (walked + segmentLength >= distance)
                {
                    var t = Mathf.Clamp01((distance - walked) / segmentLength);
                    return Vector3.Lerp(start, end, t);
                }

                walked += segmentLength;
            }

            return closed ? path[0] : path[count - 1];
        }

        public static Vector3 SamplePathAtPercent(IReadOnlyList<Vector3> path, float percent, bool closed)
        {
            var count = path?.Count ?? 0;
            if (count == 0) return Vector3.zero;
            if (count == 1) return path[0];

            percent = Mathf.Clamp01(percent);
            if (closed && percent >= 1f) return path[0];

            var totalLength = CalculateLength(path, closed);
            return SamplePathAtDistance(path, totalLength * percent, closed);
        }

        [Button("Build Conveyor Frame")]
        public bool Build()
        {
            return Build(GetComponent<SplineComputer>(), GetComponent<SplineMesh>());
        }

        public bool Build(SplineComputer splineComputer, SplineMesh splineMesh)
        {
            if (splineComputer == null || splineMesh == null) return false;

            var roundedPath = GetRoundedPath();
            if (!HasEnoughPoints(roundedPath, closed)) return false;

            var crossSection = GetCrossSectionMesh();
            if (crossSection == null) return false;

            IReadOnlyList<float> buildExitPercents = exitPercents;
            IReadOnlyList<float> buildExitLengths = exitLengths;
            if (TryShiftClosedPathSeamAwayFromExits(
                    roundedPath,
                    closed,
                    exitSectionMesh,
                    exitPercents,
                    exitFlipped,
                    exitLengths,
                    exitSectionLength,
                    out var shiftedPath,
                    out var shiftedExitPercents))
            {
                roundedPath = shiftedPath;
                buildExitPercents = shiftedExitPercents;
            }

            var normals = CreatePointNormals(roundedPath, closed, GetNormal());
            var splinePoints = new SplinePoint[roundedPath.Length];
            for (var i = 0; i < roundedPath.Length; i++)
            {
                splinePoints[i] = new SplinePoint
                {
                    position = roundedPath[i],
                    normal = normals[i],
                    size = 1f,
                    color = Color.white
                };
            }

            splineComputer.type = Spline.Type.Linear;
            splineComputer.space = SplineComputer.Space.Local;
            splineComputer.SetPoints(splinePoints, SplineComputer.Space.Local);
            if (closed) splineComputer.Close();
            else splineComputer.Break();

            splineMesh.spline = splineComputer;
            ConfigureSplineMesh(
                splineMesh,
                crossSection,
                exitSectionMesh,
                buildExitPercents,
                exitFlipped,
                buildExitLengths,
                exitSectionLength,
                roundedPath,
                closed,
                GetNormal(),
                GetSectionCount(roundedPath.Length, closed),
                GetCustomMeshRotation(),
                DefaultMapTestMeshRotation,
                DefaultMapTestMeshRotationFlipped,
                customMeshOffset,
                customMeshScale
            );
            splineMesh.RebuildImmediate();
            return true;
        }

        [TitleGroup("Mesh Override Test")]
        [Button("Test Replace Segment Mesh")]
        public bool TestReplaceSegment()
        {
            return TestReplaceSegment(GetComponent<SplineComputer>(), GetComponent<SplineMesh>());
        }

        public bool TestReplaceSegment(SplineComputer splineComputer, SplineMesh splineMesh)
        {
            if (!Build(splineComputer, splineMesh)) return false;

            var baseMesh = GetCrossSectionMesh();
            if (overrideMesh == null)
            {
                Debug.LogWarning("Override mesh is missing. Keeping the normal conveyor frame build.", this);
                return true;
            }

            var roundedPath = GetRoundedPath();
            var totalLength = CalculateLength(roundedPath, closed);
            if (totalLength <= Epsilon) return false;

            var anchorDistance = totalLength * Mathf.Clamp01(overrideAnchorPercent);
            var halfLength = Mathf.Max(0f, overrideLength) * 0.5f;
            var startDistance = anchorDistance - halfLength;
            var endDistance = anchorDistance + halfLength;
            var wasClamped = startDistance < 0f || endDistance > totalLength;

            startDistance = Mathf.Clamp(startDistance, 0f, totalLength);
            endDistance = Mathf.Clamp(endDistance, 0f, totalLength);

            if (closed && wasClamped)
                Debug.LogWarning(
                    "Override range crosses the closed spline boundary. Test Replace Segment clamps it in this phase.",
                    this
                );

            if (endDistance - startDistance <= Epsilon)
            {
                Debug.LogWarning("Override length is too small for Test Replace Segment. Keeping the normal conveyor frame build.", this);
                return true;
            }

            ConfigureOverrideSplineMesh(
                splineMesh,
                baseMesh,
                overrideMesh,
                roundedPath,
                closed,
                startDistance,
                endDistance,
                totalLength,
                GetNormal(),
                GetSectionCount(roundedPath.Length, closed),
                GetCustomMeshRotation(),
                customMeshOffset,
                customMeshScale
            );
            splineMesh.RebuildImmediate();
            return true;
        }

        public static Vector3[] CreateRoundedPath(
            IReadOnlyList<Vector3> path,
            float radius,
            int segments,
            bool isClosed
        )
        {
            var simplified = SimplifyPath(path, isClosed);
            if (!HasEnoughPoints(simplified, isClosed)) return simplified;

            return RoundedCornerLine.AddRoundedCorners(
                simplified,
                Mathf.Max(0f, radius),
                Mathf.Max(1, segments),
                isClosed
            );
        }

        public static Vector3[] SimplifyPath(IReadOnlyList<Vector3> path, bool isClosed)
        {
            if (path == null || path.Count == 0) return new Vector3[0];

            var deduped = new List<Vector3>(path.Count);
            for (var i = 0; i < path.Count; i++)
            {
                var point = path[i];
                if (deduped.Count == 0 || Vector3.Distance(deduped[deduped.Count - 1], point) > Epsilon)
                    deduped.Add(point);
            }

            if (isClosed && deduped.Count > 1 && Vector3.Distance(deduped[0], deduped[deduped.Count - 1]) <= Epsilon)
                deduped.RemoveAt(deduped.Count - 1);

            if (deduped.Count < 3) return deduped.ToArray();

            var result = new List<Vector3>(deduped.Count);
            for (var i = 0; i < deduped.Count; i++)
            {
                if (!isClosed && (i == 0 || i == deduped.Count - 1))
                {
                    result.Add(deduped[i]);
                    continue;
                }

                var previous = deduped[(i + deduped.Count - 1) % deduped.Count];
                var current = deduped[i];
                var next = deduped[(i + 1) % deduped.Count];
                var incoming = current - previous;
                var outgoing = next - current;

                if (incoming.sqrMagnitude <= Epsilon * Epsilon || outgoing.sqrMagnitude <= Epsilon * Epsilon)
                    continue;

                if (IsCollinearSameDirection(incoming, outgoing))
                    continue;

                result.Add(current);
            }

            return HasEnoughPoints(result, isClosed) ? result.ToArray() : deduped.ToArray();
        }

        public static Vector3[] CreatePointNormals(
            IReadOnlyList<Vector3> path,
            bool isClosed,
            Vector3 preferredNormal
        )
        {
            var count = path?.Count ?? 0;
            if (count == 0) return new Vector3[0];

            preferredNormal = preferredNormal == Vector3.zero ? Vector3.up : preferredNormal.normalized;
            var normals = new Vector3[count];

            var tangents = new Vector3[count];
            for (var i = 0; i < count; i++)
                tangents[i] = GetPointTangent(path, i, isClosed);

            for (var i = 0; i < count; i++)
                normals[i] = GetPerpendicularNormal(preferredNormal, tangents[i]);

            return normals;
        }

        private static bool TryShiftClosedPathSeamAwayFromExits(
            Vector3[] roundedPath,
            bool isClosed,
            Mesh replacementMesh,
            IReadOnlyList<float> replacementPercents,
            IReadOnlyList<bool> replacementFlipped,
            IReadOnlyList<float> replacementLengths,
            float defaultReplacementLength,
            out Vector3[] shiftedPath,
            out List<float> shiftedReplacementPercents
        )
        {
            shiftedPath = roundedPath;
            shiftedReplacementPercents = null;

            if (!isClosed ||
                replacementMesh == null ||
                replacementPercents == null ||
                replacementPercents.Count == 0 ||
                roundedPath == null ||
                roundedPath.Length < 3)
            {
                return false;
            }

            var totalLength = CalculateLength(roundedPath, true);
            var replacementIntervals = CreateReplacementIntervals(
                replacementPercents,
                replacementFlipped,
                replacementLengths,
                defaultReplacementLength,
                totalLength,
                true
            );

            if (replacementIntervals.Count == 0 ||
                !ClosedLoopSeamTouchesReplacement(replacementIntervals, totalLength) ||
                !TryFindClosedLoopGap(replacementIntervals, totalLength, out var seamDistance))
            {
                return false;
            }

            var candidatePath = ShiftClosedPathStart(roundedPath, seamDistance);
            if (!HasEnoughPoints(candidatePath, true))
                return false;

            shiftedPath = candidatePath;
            shiftedReplacementPercents = ShiftReplacementPercents(replacementPercents, seamDistance, totalLength);
            return true;
        }

        private static bool ClosedLoopSeamTouchesReplacement(
            IReadOnlyList<DistanceInterval> intervals,
            float totalLength
        )
        {
            if (intervals == null || totalLength <= Epsilon)
                return false;

            for (var i = 0; i < intervals.Count; i++)
            {
                var start = Mathf.Clamp(intervals[i].StartDistance, 0f, totalLength);
                var end = Mathf.Clamp(intervals[i].EndDistance, 0f, totalLength);
                if (start <= Epsilon || end >= totalLength - Epsilon)
                    return true;
            }

            return false;
        }

        private static bool TryFindClosedLoopGap(
            IReadOnlyList<DistanceInterval> intervals,
            float totalLength,
            out float seamDistance
        )
        {
            seamDistance = 0f;
            if (intervals == null || intervals.Count == 0 || totalLength <= Epsilon)
                return false;

            var largestGap = 0f;
            for (var i = 0; i < intervals.Count; i++)
            {
                var currentEnd = Mathf.Clamp(intervals[i].EndDistance, 0f, totalLength);
                var nextStart = i == intervals.Count - 1
                    ? Mathf.Clamp(intervals[0].StartDistance, 0f, totalLength) + totalLength
                    : Mathf.Clamp(intervals[i + 1].StartDistance, 0f, totalLength);
                var gap = nextStart - currentEnd;
                if (gap <= largestGap + Epsilon)
                    continue;

                largestGap = gap;
                seamDistance = Mathf.Repeat(currentEnd + gap * 0.5f, totalLength);
            }

            return largestGap > Epsilon;
        }

        private static Vector3[] ShiftClosedPathStart(IReadOnlyList<Vector3> path, float startDistance)
        {
            var count = path?.Count ?? 0;
            if (count < 3)
                return path == null ? new Vector3[0] : new List<Vector3>(path).ToArray();

            var totalLength = CalculateLength(path, true);
            if (totalLength <= Epsilon)
                return new List<Vector3>(path).ToArray();

            startDistance = Mathf.Repeat(startDistance, totalLength);
            var walked = 0f;
            var segmentIndex = 0;
            var segmentT = 0f;

            for (var i = 0; i < count; i++)
            {
                var start = path[i];
                var end = path[(i + 1) % count];
                var segmentLength = Vector3.Distance(start, end);
                if (segmentLength <= Epsilon)
                    continue;

                if (walked + segmentLength >= startDistance)
                {
                    segmentIndex = i;
                    segmentT = Mathf.Clamp01((startDistance - walked) / segmentLength);
                    break;
                }

                walked += segmentLength;
            }

            var seam = Vector3.Lerp(path[segmentIndex], path[(segmentIndex + 1) % count], segmentT);
            var shifted = new List<Vector3>(count + 1) { seam };
            var nextIndex = (segmentIndex + 1) % count;

            for (var i = 0; i < count; i++)
            {
                var point = path[(nextIndex + i) % count];
                if (Vector3.Distance(point, seam) <= Epsilon)
                    continue;
                if (shifted.Count > 0 && Vector3.Distance(shifted[shifted.Count - 1], point) <= Epsilon)
                    continue;

                shifted.Add(point);
            }

            return shifted.ToArray();
        }

        private static List<float> ShiftReplacementPercents(
            IReadOnlyList<float> replacementPercents,
            float seamDistance,
            float totalLength
        )
        {
            var shifted = new List<float>(replacementPercents?.Count ?? 0);
            if (replacementPercents == null || totalLength <= Epsilon)
                return shifted;

            for (var i = 0; i < replacementPercents.Count; i++)
            {
                var distance = Mathf.Clamp01(replacementPercents[i]) * totalLength;
                shifted.Add(Mathf.Repeat(distance - seamDistance, totalLength) / totalLength);
            }

            return shifted;
        }

        private static void ConfigureSplineMesh(
            SplineMesh splineMesh,
            Mesh crossSection,
            Mesh replacementMesh,
            IReadOnlyList<float> replacementPercents,
            IReadOnlyList<bool> replacementFlipped,
            IReadOnlyList<float> replacementLengths,
            float defaultReplacementLength,
            IReadOnlyList<Vector3> roundedPath,
            bool isClosed,
            Vector3 normal,
            int sectionCount,
            Vector3 customRotation,
            Vector3 replacementRotation,
            Vector3 replacementFlippedRotation,
            Vector3 customOffset,
            Vector3 customScale
        )
        {
            var totalLength = CalculateLength(roundedPath, isClosed);
            var replacementIntervals = CreateReplacementIntervals(
                replacementPercents,
                replacementFlipped,
                replacementLengths,
                defaultReplacementLength,
                totalLength,
                isClosed
            );

            if (replacementMesh != null && replacementIntervals.Count > 0)
            {
                ConfigureSegmentedSplineMesh(
                    splineMesh,
                    crossSection,
                    replacementMesh,
                    roundedPath,
                    isClosed,
                    replacementIntervals,
                    totalLength,
                    normal,
                    sectionCount,
                    customRotation,
                    replacementRotation,
                    replacementFlippedRotation,
                    customOffset,
                    customScale,
                    ChannelName,
                    "Conveyor Exit"
                );
                return;
            }

            for (var i = splineMesh.GetChannelCount() - 1; i >= 0; i--)
                splineMesh.RemoveChannel(i);

            AddExtrudeChannel(
                splineMesh,
                crossSection,
                ChannelName,
                0f,
                1f,
                sectionCount,
                normal,
                customRotation,
                customOffset,
                customScale
            );
        }

        private static void ConfigureOverrideSplineMesh(
            SplineMesh splineMesh,
            Mesh baseMesh,
            Mesh replacementMesh,
            IReadOnlyList<Vector3> roundedPath,
            bool isClosed,
            float startDistance,
            float endDistance,
            float totalLength,
            Vector3 normal,
            int sectionCount,
            Vector3 customRotation,
            Vector3 customOffset,
            Vector3 customScale
        )
        {
            for (var i = splineMesh.GetChannelCount() - 1; i >= 0; i--)
                splineMesh.RemoveChannel(i);

            var startPercent = DistanceToLinearSplinePercent(roundedPath, startDistance, isClosed);
            var endPercent = DistanceToLinearSplinePercent(roundedPath, endDistance, isClosed);

            if (startPercent > Epsilon)
            {
                AddExtrudeChannel(
                    splineMesh,
                    baseMesh,
                    "Channel_Base_Before",
                    0f,
                    startPercent,
                    GetRangeSectionCount(sectionCount, startDistance / totalLength),
                    normal,
                    customRotation,
                    customOffset,
                    customScale
                );
            }

            AddExtrudeChannel(
                splineMesh,
                replacementMesh,
                "Channel_Override",
                startPercent,
                endPercent,
                GetRangeSectionCount(sectionCount, (endDistance - startDistance) / totalLength),
                normal,
                customRotation,
                customOffset,
                customScale
            );

            if (endPercent < 1f - Epsilon)
            {
                AddExtrudeChannel(
                    splineMesh,
                    baseMesh,
                    "Channel_Base_After",
                    endPercent,
                    1f,
                    GetRangeSectionCount(sectionCount, (totalLength - endDistance) / totalLength),
                    normal,
                    customRotation,
                    customOffset,
                    customScale
                );
            }
        }

        private static void ConfigureSegmentedSplineMesh(
            SplineMesh splineMesh,
            Mesh baseMesh,
            Mesh replacementMesh,
            IReadOnlyList<Vector3> roundedPath,
            bool isClosed,
            IReadOnlyList<DistanceInterval> replacementIntervals,
            float totalLength,
            Vector3 normal,
            int sectionCount,
            Vector3 customRotation,
            Vector3 replacementRotation,
            Vector3 replacementFlippedRotation,
            Vector3 customOffset,
            Vector3 customScale,
            string baseChannelName,
            string replacementChannelName
        )
        {
            for (var i = splineMesh.GetChannelCount() - 1; i >= 0; i--)
                splineMesh.RemoveChannel(i);

            if (totalLength <= Epsilon || replacementIntervals == null || replacementIntervals.Count == 0)
            {
                AddExtrudeChannel(
                    splineMesh,
                    baseMesh,
                    baseChannelName,
                    0f,
                    1f,
                    sectionCount,
                    normal,
                    customRotation,
                    customOffset,
                    customScale
                );
                return;
            }

            var cursor = 0f;
            for (var i = 0; i < replacementIntervals.Count; i++)
            {
                var interval = replacementIntervals[i];
                if (interval.StartDistance > cursor + Epsilon)
                {
                    AddDistanceChannel(
                        splineMesh,
                        baseMesh,
                        baseChannelName,
                        roundedPath,
                        isClosed,
                        cursor,
                        interval.StartDistance,
                        totalLength,
                        sectionCount,
                        normal,
                        customRotation,
                        customOffset,
                        customScale
                    );
                }

                if (interval.EndDistance > interval.StartDistance + Epsilon)
                {
                    Vector3 intervalRotation = interval.Flipped
                        ? replacementFlippedRotation
                        : replacementRotation;
                    AddDistanceChannel(
                        splineMesh,
                        replacementMesh,
                        replacementChannelName,
                        roundedPath,
                        isClosed,
                        interval.StartDistance,
                        interval.EndDistance,
                        totalLength,
                        sectionCount,
                        normal,
                        intervalRotation,
                        customOffset,
                        customScale
                    );
                }

                cursor = Mathf.Max(cursor, interval.EndDistance);
            }

            if (cursor < totalLength - Epsilon)
            {
                AddDistanceChannel(
                    splineMesh,
                    baseMesh,
                    baseChannelName,
                    roundedPath,
                    isClosed,
                    cursor,
                    totalLength,
                    totalLength,
                    sectionCount,
                    normal,
                    customRotation,
                    customOffset,
                    customScale
                );
            }
        }

        private static void AddDistanceChannel(
            SplineMesh splineMesh,
            Mesh mesh,
            string channelName,
            IReadOnlyList<Vector3> roundedPath,
            bool isClosed,
            float startDistance,
            float endDistance,
            float totalLength,
            int sectionCount,
            Vector3 normal,
            Vector3 customRotation,
            Vector3 customOffset,
            Vector3 customScale
        )
        {
            var startPercent = DistanceToLinearSplinePercent(roundedPath, startDistance, isClosed);
            var endPercent = DistanceToLinearSplinePercent(roundedPath, endDistance, isClosed);
            if (endPercent - startPercent <= Epsilon)
                return;

            AddExtrudeChannel(
                splineMesh,
                mesh,
                channelName,
                startPercent,
                endPercent,
                GetRangeSectionCount(sectionCount, (endDistance - startDistance) / totalLength),
                normal,
                customRotation,
                customOffset,
                customScale
            );
        }

        private static List<DistanceInterval> CreateReplacementIntervals(
            IReadOnlyList<float> replacementPercents,
            IReadOnlyList<bool> replacementFlipped,
            IReadOnlyList<float> replacementLengths,
            float defaultReplacementLength,
            float totalLength,
            bool isClosed
        )
        {
            var intervals = new List<DistanceInterval>();
            if (replacementPercents == null || replacementPercents.Count == 0 || totalLength <= Epsilon)
                return intervals;

            for (var i = 0; i < replacementPercents.Count; i++)
            {
                var replacementLength = replacementLengths != null && i < replacementLengths.Count
                    ? replacementLengths[i]
                    : -1f;
                if (replacementLength < 0f)
                    replacementLength = defaultReplacementLength;
                if (replacementLength <= Epsilon)
                    continue;

                var halfLength = replacementLength * 0.5f;
                bool flipped = replacementFlipped != null &&
                               i < replacementFlipped.Count &&
                               replacementFlipped[i];
                var centerDistance = Mathf.Clamp01(replacementPercents[i]) * totalLength;
                var startDistance = centerDistance - halfLength;
                var endDistance = centerDistance + halfLength;

                if (isClosed)
                    AddClosedInterval(intervals, startDistance, endDistance, totalLength, flipped);
                else
                    AddOpenInterval(intervals, startDistance, endDistance, totalLength, flipped);
            }

            return MergeIntervals(intervals, totalLength);
        }

        private static void AddOpenInterval(
            List<DistanceInterval> intervals,
            float startDistance,
            float endDistance,
            float totalLength,
            bool flipped
        )
        {
            startDistance = Mathf.Clamp(startDistance, 0f, totalLength);
            endDistance = Mathf.Clamp(endDistance, 0f, totalLength);
            if (endDistance > startDistance + Epsilon)
                intervals.Add(new DistanceInterval(startDistance, endDistance, flipped));
        }

        private static void AddClosedInterval(
            List<DistanceInterval> intervals,
            float startDistance,
            float endDistance,
            float totalLength,
            bool flipped
        )
        {
            var length = endDistance - startDistance;
            if (length >= totalLength - Epsilon)
            {
                intervals.Add(new DistanceInterval(0f, totalLength, flipped));
                return;
            }

            startDistance = Mathf.Repeat(startDistance, totalLength);
            endDistance = Mathf.Repeat(endDistance, totalLength);

            if (startDistance <= endDistance)
            {
                intervals.Add(new DistanceInterval(startDistance, endDistance, flipped));
                return;
            }

            intervals.Add(new DistanceInterval(startDistance, totalLength, flipped));
            intervals.Add(new DistanceInterval(0f, endDistance, flipped));
        }

        private static List<DistanceInterval> MergeIntervals(List<DistanceInterval> intervals, float totalLength)
        {
            var result = new List<DistanceInterval>();
            if (intervals == null || intervals.Count == 0)
                return result;

            intervals.Sort((a, b) =>
            {
                int startComparison = a.StartDistance.CompareTo(b.StartDistance);
                return startComparison != 0
                    ? startComparison
                    : a.EndDistance.CompareTo(b.EndDistance);
            });

            var boundaries = new List<float>(intervals.Count * 2);
            for (var i = 0; i < intervals.Count; i++)
            {
                boundaries.Add(Mathf.Clamp(intervals[i].StartDistance, 0f, totalLength));
                boundaries.Add(Mathf.Clamp(intervals[i].EndDistance, 0f, totalLength));
            }

            boundaries.Sort();
            for (var boundaryIndex = 0; boundaryIndex < boundaries.Count - 1; boundaryIndex++)
            {
                float start = boundaries[boundaryIndex];
                float end = boundaries[boundaryIndex + 1];
                if (end <= start + Epsilon)
                    continue;

                float midpoint = (start + end) * 0.5f;
                int winningInterval = -1;
                for (var intervalIndex = 0; intervalIndex < intervals.Count; intervalIndex++)
                {
                    var candidate = intervals[intervalIndex];
                    if (midpoint >= candidate.StartDistance - Epsilon &&
                        midpoint <= candidate.EndDistance + Epsilon)
                    {
                        // Later-starting exits own an overlap, so differently rotated meshes never overlap.
                        winningInterval = intervalIndex;
                    }
                }

                if (winningInterval < 0)
                    continue;

                var winner = intervals[winningInterval];
                if (result.Count > 0)
                {
                    var previous = result[result.Count - 1];
                    if (previous.Flipped == winner.Flipped &&
                        start <= previous.EndDistance + Epsilon)
                    {
                        result[result.Count - 1] = new DistanceInterval(
                            previous.StartDistance,
                            end,
                            previous.Flipped
                        );
                        continue;
                    }
                }

                result.Add(new DistanceInterval(start, end, winner.Flipped));
            }

            return result;
        }

        private static SplineMesh.Channel AddExtrudeChannel(
            SplineMesh splineMesh,
            Mesh crossSection,
            string channelName,
            float clipFrom,
            float clipTo,
            int sectionCount,
            Vector3 normal,
            Vector3 customRotation,
            Vector3 customOffset,
            Vector3 customScale
        )
        {
            var channel = splineMesh.AddChannel(crossSection, channelName);
            channel.type = SplineMesh.Channel.Type.Extrude;
            channel.count = Mathf.Max(1, sectionCount);
            channel.autoCount = false;
            channel.overrideNormal = false;
            channel.customNormal = normal;
            channel.clipFrom = Mathf.Clamp01(clipFrom);
            channel.clipTo = Mathf.Clamp01(clipTo);

            var definition = channel.GetMesh(0);
            definition.rotation = customRotation;
            definition.offset = customOffset;
            definition.scale = customScale == Vector3.zero ? Vector3.one : customScale;
            return channel;
        }

        private static int GetRangeSectionCount(int fullSectionCount, float lengthPercent)
        {
            return Mathf.Max(1, Mathf.CeilToInt(Mathf.Max(1, fullSectionCount) * Mathf.Clamp01(lengthPercent)));
        }

        private static float DistanceToLinearSplinePercent(IReadOnlyList<Vector3> path, float distance, bool isClosed)
        {
            var count = path?.Count ?? 0;
            if (count < 2) return 0f;

            var totalLength = CalculateLength(path, isClosed);
            if (totalLength <= Epsilon) return 0f;

            if (distance <= Epsilon) return 0f;
            if (distance >= totalLength - Epsilon) return 1f;

            distance = isClosed
                ? Mathf.Repeat(distance, totalLength)
                : Mathf.Clamp(distance, 0f, totalLength);

            var walked = 0f;
            var segmentCount = isClosed ? count : count - 1;
            for (var i = 0; i < segmentCount; i++)
            {
                var start = path[i];
                var end = path[(i + 1) % count];
                var segmentLength = Vector3.Distance(start, end);
                if (segmentLength <= Epsilon) continue;

                if (walked + segmentLength >= distance)
                {
                    var segmentT = Mathf.Clamp01((distance - walked) / segmentLength);
                    return (i + segmentT) / segmentCount;
                }

                walked += segmentLength;
            }

            return 1f;
        }

        private readonly struct DistanceInterval
        {
            public readonly float StartDistance;
            public readonly float EndDistance;
            public readonly bool Flipped;

            public DistanceInterval(float startDistance, float endDistance)
                : this(startDistance, endDistance, false)
            {
            }

            public DistanceInterval(float startDistance, float endDistance, bool flipped)
            {
                StartDistance = startDistance;
                EndDistance = endDistance;
                Flipped = flipped;
            }
        }

        private Mesh GetCrossSectionMesh()
        {
            return uShapeCrossSection;
        }

        private static Vector3 GetPointTangent(IReadOnlyList<Vector3> path, int index, bool isClosed)
        {
            var count = path.Count;
            Vector3 tangent;

            if (isClosed)
                tangent = path[(index + 1) % count] - path[(index + count - 1) % count];
            else if (index == 0)
                tangent = path[1] - path[0];
            else if (index == count - 1)
                tangent = path[index] - path[index - 1];
            else
                tangent = path[index + 1] - path[index - 1];

            if (tangent.sqrMagnitude > Epsilon * Epsilon)
                return tangent.normalized;

            return Vector3.forward;
        }

        private static Vector3 GetPerpendicularNormal(Vector3 preferredNormal, Vector3 tangent)
        {
            var normal = Vector3.ProjectOnPlane(preferredNormal, tangent);
            if (normal.sqrMagnitude > Epsilon * Epsilon)
                return normal.normalized;

            var fallback = Mathf.Abs(Vector3.Dot(tangent, Vector3.up)) < 0.9f ? Vector3.up : Vector3.forward;
            normal = Vector3.ProjectOnPlane(fallback, tangent);
            return normal.sqrMagnitude > Epsilon * Epsilon ? normal.normalized : Vector3.right;
        }

        private static bool HasEnoughPoints(IReadOnlyCollection<Vector3> points, bool isClosed)
        {
            var count = points?.Count ?? 0;
            return isClosed ? count >= 3 : count >= 2;
        }

        private static int GetSectionCount(int roundedPathLength, bool isClosed)
        {
            return isClosed ? Mathf.Max(1, roundedPathLength) : Mathf.Max(1, roundedPathLength - 1);
        }

        private static bool IsCollinearSameDirection(Vector3 incoming, Vector3 outgoing)
        {
            var magnitudeProduct = incoming.magnitude * outgoing.magnitude;
            return magnitudeProduct > Epsilon
                   && Vector3.Cross(incoming, outgoing).magnitude <= Epsilon * magnitudeProduct
                   && Vector3.Dot(incoming, outgoing) > 0f;
        }

        private Vector3 GetNormal()
        {
            return splineNormal == Vector3.zero ? Vector3.up : splineNormal.normalized;
        }

        private Vector3 GetCustomMeshRotation()
        {
            if (customMeshUseMapTestPreset)
                return customMeshPresetFlipped ? DefaultMapTestMeshRotationFlipped : DefaultMapTestMeshRotation;

            return customMeshRotation;
        }

        private void OnDrawGizmos()
        {
            if (!showRoundedPathGizmos && !showCheckpointGizmo)
                return;

            var worldPath = GetRoundedPathWorld();
            if (worldPath.Length == 0)
                return;
            var length = CalculateLength(worldPath, closed);
            if (showRoundedPathGizmos)
            {
                Gizmos.color = pathColor;
                DrawPath(worldPath, closed);
            }

        }

        private static void DrawPath(IReadOnlyList<Vector3> path, bool isClosed)
        {
            var count = path?.Count ?? 0;
            if (count < 2) return;

            for (var i = 0; i < count - 1; i++)
                Gizmos.DrawLine(path[i], path[i + 1]);

            if (isClosed)
                Gizmos.DrawLine(path[count - 1], path[0]);
        }

        private Renderer meshRenderer;
        private MaterialPropertyBlock propBlock;
        private bool isFlashing = false;
        private float flashTimer = 0f;
        [TitleGroup("Frame Flashing")]
        [SerializeField] private float flashSpeed = 5f;

        private static readonly int ColorPropId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropId = Shader.PropertyToID("_BaseColor");
        private int activePropId = -1;

        private void Start()
        {
            meshRenderer = GetComponent<Renderer>();
            propBlock = new MaterialPropertyBlock();
            if (meshRenderer != null && meshRenderer.sharedMaterial != null)
            {
                Material mat = meshRenderer.sharedMaterial;
                if (mat.HasProperty(ColorPropId))
                    activePropId = ColorPropId;
                else if (mat.HasProperty(BaseColorPropId))
                    activePropId = BaseColorPropId;
            }
        }

        public void SetFlashing(bool flashing)
        {
            isFlashing = flashing;
            if (!flashing && meshRenderer != null && activePropId != -1)
            {
                meshRenderer.SetPropertyBlock(null);
            }
        }

        private void Update()
        {
            if (isFlashing && meshRenderer != null && activePropId != -1)
            {
                flashTimer += Time.deltaTime * flashSpeed;
                float lerp = (Mathf.Sin(flashTimer) + 1f) * 0.5f; // 0 to 1
                Color flashColor = Color.Lerp(Color.red, Color.white, lerp);

                meshRenderer.GetPropertyBlock(propBlock);
                propBlock.SetColor(activePropId, flashColor);
                meshRenderer.SetPropertyBlock(propBlock);
            }
        }
    }
}
