using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AgentPack.VisualDirection
{
    /// <summary>
    /// Mot demo motion cua lab (drag, invalid, win...) dung lai bang CHINH ham curve ma runtime dung.
    /// Project implement interface nay (class co constructor khong tham so, trong Editor assembly);
    /// MotionParity tu tim qua TypeCache. Harvest tu Ducan_SpeedRun_Demo MotionParity (ban viet cung 4 demo).
    /// </summary>
    public interface IMotionParityDemo
    {
        /// <summary>= key trong <c>sections.motion.metrics</c> cua direction.</summary>
        string Id { get; }

        /// <summary>= key trong <c>metrics.&lt;id&gt;.channels</c>. Phan tu null = kenh khong do duoc (bao N/A).</summary>
        string[] Channels { get; }

        /// <summary>Suy tham so kich ban tu target neu lab khong export plan (vd. so o keo). Tra ve thoi luong can sample.</summary>
        float Prepare(MotionDemoTarget target);

        /// <summary>Ghi gia tri moi kenh tai thoi diem t (giay) vao values (cung thu tu Channels).</summary>
        void Sample(float t, float[] values);
    }

    public sealed class MotionChannelTarget
    {
        public string Name;
        public float Peak, TPeak, Min, TMin, End;
    }

    public sealed class MotionDemoTarget
    {
        public string Id;
        public float Total;
        public readonly List<MotionChannelTarget> Channels = new List<MotionChannelTarget>();

        public MotionChannelTarget Find(string name) => Channels.FirstOrDefault(c => c.Name == name);
    }

    /// <summary>Mac dinh = luat visual-review (total ±10%, peak ±15%, tPeak ±0.03s). Direction co the ghi de qua "tolerance".</summary>
    public sealed class MotionTolerance
    {
        public float Total = 0.10f, Peak = 0.15f, TPeak = 0.03f, End = 0.02f;

        public static MotionTolerance From(VisualDirectionFile direction)
        {
            var t = new MotionTolerance();
            if (direction == null) return t;
            t.Total = direction.Tolerance("total", t.Total);
            t.Peak = direction.Tolerance("peak", t.Peak);
            t.TPeak = direction.Tolerance("tPeak", t.TPeak);
            t.End = direction.Tolerance("end", t.End);
            return t;
        }
    }

    /// <summary>
    /// Sample moi demo → CSV + so voi <c>sections.motion.metrics</c> cua direction hien hanh.
    /// Dong DAU cua log la ket luan (read_console chi tra dong dau): "[MotionParity] PASS 38/38 ...".
    /// PASS => Debug.Log; FAIL/thieu target => Debug.LogWarning, de script verify kiem bang log level.
    /// </summary>
    public static class MotionParity
    {
        public const int DefaultSamples = 240;
        public const string DefaultCaptureDir = "handoff/visual/captures";

        public sealed class ChannelResult
        {
            public string Name;
            public float Peak = float.NegativeInfinity, TPeak, Min = float.PositiveInfinity, TMin, End;
        }

        public sealed class DemoResult
        {
            public string Id;
            public float Total;
            public ChannelResult[] Channels;
            public readonly List<float[]> Rows = new List<float[]>();
        }

        public sealed class Check
        {
            public string Demo, Channel, Metric;
            public float Target, Actual;
            public bool Pass, Skipped;
        }

        public sealed class Report
        {
            public int Version;
            public string MotionStatus;
            public readonly List<Check> Checks = new List<Check>();
            public readonly List<string> Errors = new List<string>();
            public readonly List<string> Notes = new List<string>();

            public int Failed => Checks.Count(c => !c.Pass && !c.Skipped);
            public int Skipped => Checks.Count(c => c.Skipped);
            public int Measured => Checks.Count(c => !c.Skipped);
            public bool Pass => Errors.Count == 0 && Failed == 0 && Measured > 0;

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append("[MotionParity] ").Append(Pass ? "PASS " : "FAIL ")
                  .Append(Measured - Failed).Append('/').Append(Measured)
                  .Append(" (fail ").Append(Failed).Append(", n/a ").Append(Skipped).Append(", loi ").Append(Errors.Count).Append(')')
                  .Append(" direction v").Append(Version).Append(" motion=").Append(MotionStatus ?? "?");
                foreach (string e in Errors) sb.Append("\n  ! ").Append(e);
                foreach (Check c in Checks)
                {
                    sb.Append('\n').Append(c.Skipped ? "N/A " : c.Pass ? "PASS" : "FAIL").Append(' ')
                      .Append(c.Demo).Append(" · ").Append(c.Channel).Append(" · ").Append(c.Metric);
                    if (!c.Skipped)
                        sb.Append(": target=").Append(c.Target.ToString("0.###", CultureInfo.InvariantCulture))
                          .Append(" unity=").Append(c.Actual.ToString("0.###", CultureInfo.InvariantCulture));
                }
                foreach (string n in Notes) sb.Append("\n  - ").Append(n);
                return sb.ToString();
            }
        }

        [MenuItem("Tools/Visual Direction/Motion Parity (CSV + so target)")]
        private static void RunMenu() => RunAndLog(DefaultCaptureDir, "cur");

        /// <summary>Entry cho menu / Unity MCP: doc CURRENT, tim demo qua TypeCache, ghi CSV, log ket qua.</summary>
        public static Report RunAndLog(string captureDir, string tag)
        {
            Report report;
            try
            {
                VisualDirectionFile direction = VisualDirectionFile.LoadCurrent();
                string dir = Path.IsPathRooted(captureDir) ? captureDir : Path.Combine(Directory.GetCurrentDirectory(), captureDir);
                report = Run(direction, DiscoverDemos(), dir, tag);
                report.Notes.Add($"CSV: {captureDir}/motion-<demo>-{tag}.csv");
            }
            catch (Exception e)
            {
                report = new Report();
                report.Errors.Add(e.Message);
            }
            if (report.Pass) Debug.Log(report.ToString());
            else Debug.LogWarning(report.ToString());
            return report;
        }

        public static Report Run(VisualDirectionFile direction, IEnumerable<IMotionParityDemo> demos, string captureDirAbs, string tag,
            int samples = DefaultSamples)
        {
            var report = new Report();
            if (direction == null)
            {
                report.Errors.Add($"chua co direction: {VisualDirectionImporter.PointerPath} chua tro toi file nao (promote-direction.ps1)");
                return report;
            }
            report.Version = direction.Version;
            report.MotionStatus = direction.SectionStatus("motion");
            // Target chua duyet van do duoc (de lap), nhung khong duoc tinh la PASS.
            if (report.MotionStatus != VisualDirectionFile.Approved)
                report.Errors.Add($"section motion = {report.MotionStatus ?? "?"}: target chua duoc PO duyet");

            Dictionary<string, MotionDemoTarget> targets = ReadTargets(direction.Section("motion"));
            if (targets.Count == 0) report.Errors.Add("section motion khong co metrics");
            MotionTolerance tol = MotionTolerance.From(direction);
            var byId = new Dictionary<string, IMotionParityDemo>(StringComparer.Ordinal);
            foreach (IMotionParityDemo d in demos)
            {
                if (byId.ContainsKey(d.Id)) report.Errors.Add($"2 class cung demo id '{d.Id}'");
                else byId[d.Id] = d;
            }

            foreach (MotionDemoTarget target in targets.Values)
            {
                if (!byId.TryGetValue(target.Id, out IMotionParityDemo demo))
                {
                    report.Checks.Add(new Check { Demo = target.Id, Channel = "-", Metric = "all", Skipped = true });
                    report.Notes.Add($"demo '{target.Id}' chua co class IMotionParityDemo");
                    continue;
                }
                DemoResult result = Sample(demo, demo.Prepare(target), samples);
                report.Checks.AddRange(Compare(result, target, tol));
                foreach (ChannelResult c in result.Channels)
                    if (c.Name != null && target.Find(c.Name) == null)
                        report.Notes.Add($"{target.Id}: kenh '{c.Name}' khong co trong target (ten phai trung key lab)");
                if (!string.IsNullOrEmpty(captureDirAbs))
                    WriteCsv(Path.Combine(captureDirAbs, $"motion-{target.Id}-{tag}.csv"), result);
            }
            foreach (string id in byId.Keys)
                if (!targets.ContainsKey(id)) report.Notes.Add($"demo '{id}' khong co trong target, bo qua");
            return report;
        }

        public static Dictionary<string, MotionDemoTarget> ReadTargets(Dictionary<string, object> motionSection)
        {
            var result = new Dictionary<string, MotionDemoTarget>(StringComparer.Ordinal);
            if (motionSection == null || !motionSection.TryGetValue("metrics", out object m) || !(m is Dictionary<string, object> metrics))
                return result;
            foreach (KeyValuePair<string, object> pair in metrics)
            {
                if (!(pair.Value is Dictionary<string, object> demo)) continue;
                var target = new MotionDemoTarget { Id = pair.Key, Total = VisualDirectionFile.Number(demo, "total") };
                if (demo.TryGetValue("channels", out object ch) && ch is Dictionary<string, object> channels)
                {
                    foreach (KeyValuePair<string, object> c in channels)
                    {
                        if (!(c.Value is Dictionary<string, object> v)) continue;
                        target.Channels.Add(new MotionChannelTarget
                        {
                            Name = c.Key,
                            Peak = VisualDirectionFile.Number(v, "peak"),
                            TPeak = VisualDirectionFile.Number(v, "tPeak"),
                            Min = VisualDirectionFile.Number(v, "min"),
                            TMin = VisualDirectionFile.Number(v, "tMin"),
                            End = VisualDirectionFile.Number(v, "end")
                        });
                    }
                }
                result[pair.Key] = target;
            }
            return result;
        }

        public static DemoResult Sample(IMotionParityDemo demo, float duration, int samples)
        {
            string[] names = demo.Channels ?? new string[0];
            var result = new DemoResult { Id = demo.Id, Total = duration, Channels = new ChannelResult[names.Length] };
            for (int i = 0; i < names.Length; i++) result.Channels[i] = new ChannelResult { Name = names[i] };
            var values = new float[names.Length];
            int steps = Math.Max(1, samples);
            for (int s = 0; s <= steps; s++)
            {
                float t = duration * s / steps;
                Array.Clear(values, 0, values.Length);
                demo.Sample(t, values);
                var row = new float[names.Length + 1];
                row[0] = t;
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i] == null) continue;
                    ChannelResult c = result.Channels[i];
                    float v = values[i];
                    if (v > c.Peak) { c.Peak = v; c.TPeak = t; }
                    if (v < c.Min) { c.Min = v; c.TMin = t; }
                    c.End = v;
                    row[i + 1] = v;
                }
                result.Rows.Add(row);
            }
            return result;
        }

        public static List<Check> Compare(DemoResult result, MotionDemoTarget target, MotionTolerance tol)
        {
            var checks = new List<Check>
            {
                Make(target.Id, "-", "total", target.Total, result.Total, Math.Abs(result.Total - target.Total) <= tol.Total * Math.Abs(target.Total))
            };
            foreach (MotionChannelTarget t in target.Channels)
            {
                ChannelResult c = result.Channels.FirstOrDefault(x => x.Name == t.Name);
                if (c == null)
                {
                    checks.Add(new Check { Demo = target.Id, Channel = t.Name, Metric = "all", Skipped = true });
                    continue;
                }
                checks.Add(Make(target.Id, t.Name, "peak", t.Peak, c.Peak, Math.Abs(c.Peak - t.Peak) <= tol.Peak * Math.Max(0.05f, Math.Abs(t.Peak))));
                checks.Add(Make(target.Id, t.Name, "tPeak", t.TPeak, c.TPeak, Math.Abs(c.TPeak - t.TPeak) <= tol.TPeak));
                checks.Add(Make(target.Id, t.Name, "end", t.End, c.End, Math.Abs(c.End - t.End) <= tol.End + tol.Peak * Math.Abs(t.End)));
            }
            return checks;
        }

        /// <summary>Moi class IMotionParityDemo co constructor khong tham so (tru abstract/generic).</summary>
        public static List<IMotionParityDemo> DiscoverDemos()
        {
            var list = new List<IMotionParityDemo>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IMotionParityDemo>())
            {
                if (type.IsAbstract || type.IsInterface || type.ContainsGenericParameters || type.GetConstructor(Type.EmptyTypes) == null) continue;
                list.Add((IMotionParityDemo)Activator.CreateInstance(type));
            }
            return list;
        }

        private static Check Make(string demo, string channel, string metric, float target, float actual, bool pass) =>
            new Check { Demo = demo, Channel = channel, Metric = metric, Target = target, Actual = actual, Pass = pass };

        private static void WriteCsv(string path, DemoResult result)
        {
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            var sb = new StringBuilder("t");
            foreach (ChannelResult c in result.Channels) sb.Append(',').Append(Csv(c.Name ?? "n/a"));
            sb.Append('\n');
            foreach (float[] row in result.Rows)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(row[i].ToString("0.####", CultureInfo.InvariantCulture));
                }
                sb.Append('\n');
            }
            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(false));
        }

        private static string Csv(string s) => s.IndexOfAny(new[] { ',', '"', '\n' }) >= 0 ? "\"" + s.Replace("\"", "\"\"") + "\"" : s;
    }
}
