using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace AgentPack.VisualDirection.Tests
{
    public sealed class VisualDirectionTests
    {
        private const string Direction = @"{
  ""schema"": ""visual-direction/v1"",
  ""version"": 3,
  ""sections"": {
    ""look"": { ""status"": ""APPROVED"", ""values"": {
      ""camTilt"": 22, ""camFov"": 28, ""floorSeam"": 0.17, ""shadows"": ""on"",
      ""label"": ""cozy"", ""floorStyle"": ""tile grid"", ""tint"": ""#FF0000"", ""blockH"": 0.5 } },
    ""motion"": { ""status"": ""CANDIDATE"", ""values"": { ""floorSeam"": 0.9 } }
  },
  ""profileMap"": {
    ""camTilt"": ""TestProfile.camera.tiltDeg"", ""camFov"": ""TestProfile.camera.fov"",
    ""floorSeam"": ""TestProfile.seamStrength"", ""shadows"": ""TestProfile.shadows"",
    ""label"": ""TestProfile.label"", ""floorStyle"": ""TestProfile.floorStyle"",
    ""tint"": ""TestProfile.tint"", ""blockH"": ""Mesh.block.height""
  }
}";

        private VisualDirectionTestProfile _profile;

        [SetUp]
        public void SetUp() => _profile = ScriptableObject.CreateInstance<VisualDirectionTestProfile>();

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(_profile);

        private UnityEngine.Object Find(string name) => name == "TestProfile" ? _profile : null;

        [Test]
        public void MiniJson_ParsesNestedValuesAndEscapes()
        {
            var root = (Dictionary<string, object>)MiniJson.Deserialize("﻿{\"a\":[1,-2.5e1,true,null],\"b\":{\"c\":\"x\\u0041\\n\"}}");
            var a = (List<object>)root["a"];
            Assert.AreEqual(1d, a[0]);
            Assert.AreEqual(-25d, a[1]);
            Assert.AreEqual(true, a[2]);
            Assert.IsNull(a[3]);
            Assert.AreEqual("xA\n", ((Dictionary<string, object>)root["b"])["c"]);
            Assert.Throws<FormatException>(() => MiniJson.Deserialize("{\"a\":1,}"));
        }

        [Test]
        public void DryRun_ReportsDriftWithoutWriting()
        {
            ImportReport report = VisualDirectionImporter.Import(Direction, false, Find);

            Assert.IsEmpty(report.Errors, string.Join("\n", report.Errors));
            Assert.AreEqual(3, report.Version);
            Assert.AreEqual(7, report.Changes.Count, string.Join("\n", report.Changes));
            Assert.AreEqual(10f, _profile.camera.tiltDeg);
            Assert.AreEqual(0.5f, _profile.seamStrength);
            Assert.IsFalse(_profile.shadows);
        }

        [Test]
        public void Import_WritesOnlyApprovedSections()
        {
            ImportReport report = VisualDirectionImporter.Import(Direction, true, Find);

            Assert.IsEmpty(report.Errors, string.Join("\n", report.Errors));
            Assert.AreEqual(22f, _profile.camera.tiltDeg);
            Assert.AreEqual(28, _profile.camera.fov);
            Assert.AreEqual(0.17f, _profile.seamStrength, 1e-5f); // motion (CANDIDATE) = 0.9 khong duoc ghi
            Assert.IsTrue(_profile.shadows);
            Assert.AreEqual("cozy", _profile.label);
            Assert.AreEqual(VisualDirectionTestProfile.FloorStyle.TileGrid, _profile.floorStyle);
            Assert.AreEqual(Color.red, _profile.tint);
            Assert.That(report.Skipped.Any(s => s.Contains("motion")), "section CANDIDATE phai bi bo qua");
            Assert.That(report.Skipped.Any(s => s.Contains("Mesh.block.height")), "Mesh.* khong phai runtime data");

            ImportReport again = VisualDirectionImporter.Import(Direction, false, Find);
            Assert.IsEmpty(again.Changes, "import xong thi dry run khong con drift");
            Assert.AreEqual(7, again.Unchanged);
        }

        [Test]
        public void Import_ReportsMissingFieldAssetAndWrongSchema()
        {
            string json = Direction.Replace("TestProfile.label", "TestProfile.nope").Replace("TestProfile.tint", "OtherProfile.tint");
            ImportReport report = VisualDirectionImporter.Import(json, false, Find);
            Assert.That(report.Errors.Any(e => e.Contains("nope")));
            Assert.That(report.Errors.Any(e => e.Contains("OtherProfile")));

            ImportReport wrong = VisualDirectionImporter.Import(Direction.Replace("visual-direction/v1", "blockhome.visual-target/v1"), false, Find);
            Assert.IsNotEmpty(wrong.Errors);
        }

        private sealed class SineDemo : IMotionParityDemo
        {
            private readonly float _duration;
            public SineDemo(float duration) => _duration = duration; // co tham so => DiscoverDemos bo qua class test nay
            public string Id => "drag";
            public string[] Channels => new[] { "lift", null };
            public float Prepare(MotionDemoTarget target) => _duration;
            public void Sample(float t, float[] values) => values[0] = Mathf.Sin(Mathf.PI * t / _duration);
        }

        private static VisualDirectionFile MotionDirection(string status, float peak) => VisualDirectionFile.Parse("test.json", @"{
  ""schema"": ""visual-direction/v1"", ""version"": 2,
  ""sections"": { ""motion"": { ""status"": """ + status + @""", ""metrics"": { ""drag"": { ""total"": 1,
    ""channels"": { ""lift"": { ""peak"": " + peak.ToString(System.Globalization.CultureInfo.InvariantCulture) + @", ""tPeak"": 0.5, ""end"": 0 },
                    ""flash"": { ""peak"": 1, ""tPeak"": 0.1, ""end"": 0 } } } } } }
}");

        [Test]
        public void MotionParity_PassesWithinToleranceAndMarksUnmeasuredChannels()
        {
            MotionParity.Report report = MotionParity.Run(MotionDirection("APPROVED", 1f), new[] { new SineDemo(1f) }, null, "t");

            Assert.IsTrue(report.Pass, report.ToString());
            Assert.AreEqual(4, report.Measured);
            Assert.AreEqual(1, report.Skipped, "flash khong do => N/A");
            StringAssert.StartsWith("[MotionParity] PASS 4/4", report.ToString());
        }

        [Test]
        public void MotionParity_FailsOutsideToleranceOrUnapprovedTarget()
        {
            MotionParity.Report wrongPeak = MotionParity.Run(MotionDirection("APPROVED", 2f), new[] { new SineDemo(1f) }, null, "t");
            Assert.IsFalse(wrongPeak.Pass);
            Assert.AreEqual(1, wrongPeak.Failed);

            MotionParity.Report tooLong = MotionParity.Run(MotionDirection("APPROVED", 1f), new[] { new SineDemo(1.3f) }, null, "t");
            Assert.IsFalse(tooLong.Pass, "total lech 30% > 10%");

            MotionParity.Report candidate = MotionParity.Run(MotionDirection("CANDIDATE", 1f), new[] { new SineDemo(1f) }, null, "t");
            Assert.AreEqual(0, candidate.Failed);
            Assert.IsFalse(candidate.Pass, "target chua duyet khong duoc PASS");
            StringAssert.StartsWith("[MotionParity] FAIL", candidate.ToString());
        }

        [Test]
        public void Capture_GreyscaleAndSquintAreDeterministic()
        {
            Color32[] grey = VisualCapture.Greyscale(new[] { new Color32(255, 0, 0, 255), new Color32(255, 255, 255, 128) });
            Assert.AreEqual(54, grey[0].r);
            Assert.AreEqual(grey[0].r, grey[0].b);
            Assert.AreEqual(255, grey[1].g);
            Assert.AreEqual(128, grey[1].a);

            const int w = 8, h = 6;
            Color32[] flat = Enumerable.Repeat(new Color32(40, 80, 120, 255), w * h).ToArray();
            Assert.That(VisualCapture.Squint(flat, w, h, new[] { 2, 3 }).All(p => p.r == 40 && p.g == 80 && p.b == 120), "anh phang blur van phang");

            Color32[] dot = Enumerable.Repeat(new Color32(0, 0, 0, 255), w * h).ToArray();
            dot[3 * w + 4] = new Color32(255, 255, 255, 255);
            Color32[] blurred = VisualCapture.Squint(dot, w, h, new[] { 1 });
            Assert.Less(blurred[3 * w + 4].r, 255, "diem sang bi lan ra");
            Assert.Greater(blurred[3 * w + 3].r, 0);
        }
    }
}
