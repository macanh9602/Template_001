using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace AgentPack.VisualDirection
{
    /// <summary>
    /// Phan RIENG cua game: dua scene toi mot pose tat dinh roi tra camera gameplay de chup.
    /// Project implement dung MOT class (constructor khong tham so, Editor assembly); runner tim qua TypeCache.
    /// Chi cham Presentation / con duong gameplay that; khong sua domain state tat ngang.
    /// </summary>
    public interface IVisualCaptureScenario
    {
        /// <summary>Goi mot lan sau khi vao Play Mode: cho scene gameplay san sang (khong dung delay co dinh).</summary>
        UniTask WaitReadyAsync(CancellationToken ct);

        /// <summary>Load level + dua presentation toi pose (idle, dragging, ...). Tra camera gameplay that.</summary>
        UniTask<Camera> PreparePoseAsync(string levelId, string poseId, CancellationToken ct);
    }

    /// <summary>
    /// Capture tat dinh cho visual-review: RenderTexture co dinh kich thuoc (khong chup Game view),
    /// kem ban .grey.png (luminance) va .squint.png (blur) de cham value hierarchy.
    /// Harvest tu Ducan_SpeedRun_Demo VisualCaptureRunner; phan load level / cho scene chuyen sang IVisualCaptureScenario.
    /// Log dong DAU la ket luan: "[VisualCapture] DONE n anh" (Log) hoac "[VisualCapture] FAIL ..." (LogWarning).
    /// </summary>
    [InitializeOnLoad]
    public static class VisualCapture
    {
        public const int DefaultWidth = 1080;
        public const int DefaultHeight = 1920;
        public const string DefaultCaptureDir = "handoff/visual/captures";
        private static readonly int[] SquintRadii = { 2, 2, 3 };
        private const string PendingJobKey = "AgentPack.VisualCapture.PendingJob";
        private const string StartedPlayModeKey = "AgentPack.VisualCapture.StartedPlayMode";
        private static bool _running;

        [Serializable]
        private sealed class CaptureJob
        {
            public string[] levels;
            public string[] poses;
            public string outDir;
            public string prefix;
            public int width;
            public int height;
        }

        private struct CanvasState
        {
            public Canvas Canvas;
            public RenderMode Mode;
            public Camera Camera;
            public float Distance;
        }

        static VisualCapture()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        [MenuItem("Tools/Visual Direction/Capture reference level (idle)")]
        private static void CaptureReferenceMenu()
        {
            VisualDirectionFile direction = VisualDirectionFile.LoadCurrent();
            string level = direction != null && direction.Root.TryGetValue("referenceLevel", out object l) ? l as string : null;
            if (string.IsNullOrEmpty(level))
            {
                Debug.LogWarning("[VisualCapture] FAIL: direction hien hanh khong co referenceLevel");
                return;
            }
            CaptureSet(new[] { level }, new[] { "idle" }, DefaultCaptureDir, "cur");
        }

        /// <summary>Entry cho Unity MCP / runner. Tu vao Play Mode neu can, chup xong thi thoat Play Mode.</summary>
        public static void CaptureSet(string[] levels, string[] poses, string outDir, string prefix,
            int width = DefaultWidth, int height = DefaultHeight)
        {
            if (levels == null || levels.Length == 0) throw new ArgumentException("can it nhat mot level", nameof(levels));
            if (poses == null || poses.Length == 0) throw new ArgumentException("can it nhat mot pose", nameof(poses));
            if (string.IsNullOrWhiteSpace(outDir)) throw new ArgumentException("can thu muc output", nameof(outDir));
            var job = new CaptureJob { levels = levels, poses = poses, outDir = outDir, prefix = prefix ?? "", width = width, height = height };
            if (_running) throw new InvalidOperationException("dang co capture job chay");
            if (EditorApplication.isPlaying)
            {
                RunJob(job, false).Forget();
                return;
            }
            SessionState.SetString(PendingJobKey, JsonUtility.ToJson(job));
            SessionState.SetBool(StartedPlayModeKey, true);
            EditorApplication.EnterPlaymode();
        }

        public static string FileName(string prefix, string levelId, string poseId) =>
            (string.IsNullOrEmpty(prefix) ? "" : prefix + "-") + levelId + "-" + poseId + ".png";

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                string serialized = SessionState.GetString(PendingJobKey, string.Empty);
                if (string.IsNullOrEmpty(serialized)) return;
                SessionState.SetString(PendingJobKey, string.Empty);
                EditorApplication.delayCall += () =>
                {
                    CaptureJob job = JsonUtility.FromJson<CaptureJob>(serialized);
                    if (job != null) RunJob(job, SessionState.GetBool(StartedPlayModeKey, false)).Forget();
                };
            }
            else if (state == PlayModeStateChange.EnteredEditMode)
            {
                SessionState.SetBool(StartedPlayModeKey, false);
            }
        }

        private static async UniTaskVoid RunJob(CaptureJob job, bool exitPlayModeWhenDone)
        {
            if (_running) return;
            _running = true;
            bool previousRunInBackground = Application.runInBackground;
            var written = new List<string>();
            try
            {
                Application.runInBackground = true;
                IVisualCaptureScenario scenario = FindScenario();
                await scenario.WaitReadyAsync(CancellationToken.None);
                string dir = Path.IsPathRooted(job.outDir) ? job.outDir : Path.Combine(Directory.GetCurrentDirectory(), job.outDir);
                foreach (string level in job.levels)
                {
                    foreach (string pose in job.poses)
                    {
                        Camera camera = await scenario.PreparePoseAsync(level, pose, CancellationToken.None);
                        if (camera == null) throw new InvalidOperationException($"scenario tra camera null cho {level}/{pose}");
                        await UniTask.NextFrame();
                        Canvas.ForceUpdateCanvases();
                        string path = Path.Combine(dir, FileName(job.prefix, level, pose));
                        CaptureCamera(camera, path, job.width, job.height);
                        written.Add(path);
                    }
                }
                Debug.Log($"[VisualCapture] DONE {written.Count} anh ({job.width}x{job.height}, kem .grey/.squint)\n  " + string.Join("\n  ", written));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[VisualCapture] FAIL sau {written.Count} anh: {e.Message}\n{e}");
            }
            finally
            {
                Application.runInBackground = previousRunInBackground;
                _running = false;
                if (exitPlayModeWhenDone && EditorApplication.isPlaying) EditorApplication.ExitPlaymode();
            }
        }

        private static IVisualCaptureScenario FindScenario()
        {
            var found = new List<Type>();
            foreach (Type type in TypeCache.GetTypesDerivedFrom<IVisualCaptureScenario>())
                if (!type.IsAbstract && !type.IsInterface && !type.ContainsGenericParameters && type.GetConstructor(Type.EmptyTypes) != null)
                    found.Add(type);
            if (found.Count != 1)
                throw new InvalidOperationException($"can dung 1 class IVisualCaptureScenario, dang co {found.Count}");
            return (IVisualCaptureScenario)Activator.CreateInstance(found[0]);
        }

        /// <summary>Render camera vao RenderTexture co dinh; ghi PNG + .grey.png + .squint.png. Canvas overlay tam chuyen sang ScreenSpaceCamera de HUD vao anh.</summary>
        public static void CaptureCamera(Camera camera, string outputPath, int width, int height)
        {
            string absolute = Path.GetFullPath(outputPath);
            string directory = Path.GetDirectoryName(absolute);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            RenderTexture previousTarget = camera.targetTexture;
            RenderTexture previousActive = RenderTexture.active;
            var canvases = new List<CanvasState>();
            RenderTexture rt = null;
            Texture2D source = null;
            try
            {
                foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (!canvas.isRootCanvas || canvas.renderMode != RenderMode.ScreenSpaceOverlay) continue;
                    canvases.Add(new CanvasState { Canvas = canvas, Mode = canvas.renderMode, Camera = canvas.worldCamera, Distance = canvas.planeDistance });
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = camera;
                    canvas.planeDistance = Mathf.Clamp(camera.nearClipPlane + 1f, camera.nearClipPlane + 0.01f, camera.farClipPlane - 0.01f);
                }
                Canvas.ForceUpdateCanvases();

                rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32) { antiAliasing = 1, useMipMap = false, autoGenerateMips = false };
                rt.Create();
                camera.targetTexture = rt;
                camera.Render();
                RenderTexture.active = rt;
                source = new Texture2D(width, height, TextureFormat.RGB24, false, false);
                source.ReadPixels(new Rect(0, 0, width, height), 0, 0, false);
                source.Apply(false, false);
                File.WriteAllBytes(absolute, source.EncodeToPNG());

                Color32[] pixels = source.GetPixels32();
                WritePng(VariantPath(absolute, ".grey.png"), Greyscale(pixels), width, height);
                WritePng(VariantPath(absolute, ".squint.png"), Squint(pixels, width, height, SquintRadii), width, height);
            }
            finally
            {
                camera.targetTexture = previousTarget;
                RenderTexture.active = previousActive;
                for (int i = canvases.Count - 1; i >= 0; i--)
                {
                    CanvasState state = canvases[i];
                    if (state.Canvas == null) continue;
                    state.Canvas.renderMode = state.Mode;
                    state.Canvas.worldCamera = state.Camera;
                    state.Canvas.planeDistance = state.Distance;
                }
                if (source != null) UnityEngine.Object.DestroyImmediate(source);
                if (rt != null)
                {
                    rt.Release();
                    UnityEngine.Object.DestroyImmediate(rt);
                }
            }
        }

        public static string VariantPath(string pngPath, string suffix) =>
            Path.Combine(Path.GetDirectoryName(pngPath) ?? "", Path.GetFileNameWithoutExtension(pngPath) + suffix);

        /// <summary>Luminance Rec.709, giu alpha.</summary>
        public static Color32[] Greyscale(Color32[] pixels)
        {
            var result = new Color32[pixels.Length];
            for (int i = 0; i < pixels.Length; i++)
            {
                Color32 p = pixels[i];
                byte l = (byte)Mathf.Clamp(Mathf.RoundToInt(p.r * 0.2126f + p.g * 0.7152f + p.b * 0.0722f), 0, 255);
                result[i] = new Color32(l, l, l, p.a);
            }
            return result;
        }

        /// <summary>"Nheo mat": box blur tach truc lap theo radii (3 lan ~ gaussian). Bien kep (clamp).</summary>
        public static Color32[] Squint(Color32[] pixels, int width, int height, int[] radii)
        {
            if (pixels.Length != width * height) throw new ArgumentException("pixels.Length != width*height");
            var work = (Color32[])pixels.Clone();
            var tmp = new Color32[work.Length];
            foreach (int radius in radii)
            {
                BoxBlur(work, tmp, width, height, radius, true);
                BoxBlur(tmp, work, width, height, radius, false);
            }
            return work;
        }

        private static void BoxBlur(Color32[] input, Color32[] output, int width, int height, int radius, bool horizontal)
        {
            int window = radius * 2 + 1;
            int lines = horizontal ? height : width;
            int length = horizontal ? width : height;
            for (int line = 0; line < lines; line++)
            {
                int r = 0, g = 0, b = 0;
                for (int k = -radius; k <= radius; k++)
                {
                    Color32 p = input[PixelIndex(line, k, length, width, horizontal)];
                    r += p.r; g += p.g; b += p.b;
                }
                for (int k = 0; k < length; k++)
                {
                    output[PixelIndex(line, k, length, width, horizontal)] =
                        new Color32((byte)((r + window / 2) / window), (byte)((g + window / 2) / window), (byte)((b + window / 2) / window), 255);
                    Color32 leaving = input[PixelIndex(line, k - radius, length, width, horizontal)];
                    Color32 entering = input[PixelIndex(line, k + radius + 1, length, width, horizontal)];
                    r += entering.r - leaving.r;
                    g += entering.g - leaving.g;
                    b += entering.b - leaving.b;
                }
            }
        }

        /// <summary>Vi tri k tren dong/cot line, kep vao [0, length-1] (bien lap lai pixel mep).</summary>
        private static int PixelIndex(int line, int k, int length, int width, bool horizontal)
        {
            int c = Mathf.Clamp(k, 0, length - 1);
            return horizontal ? line * width + c : c * width + line;
        }

        private static void WritePng(string path, Color32[] pixels, int width, int height)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGB24, false, false);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(texture);
            }
        }
    }
}
