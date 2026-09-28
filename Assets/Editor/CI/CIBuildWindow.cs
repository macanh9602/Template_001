// ============================================================
//  CIBuildWindow.cs  -  cua so dieu khien trong Unity Editor
//  Menu:  CI Build > Bang dieu khien
//  File nay do tool sinh ra, dung sua tay.
// ============================================================
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;

namespace VTL.CI
{
    [Serializable] class CiLink   { public string toolDir; }

    [Serializable] class CiConfig
    {
        public string projectName;
        public string projectPath;
        public string ciRoot;
        public string worktreePath;
        public string buildsPath;
    }

    [Serializable] class CiResultView
    {
        public string id;
        public bool   success;
        public string branch;
        public string shaShort;
        public string subject;
        public string format;
        public string config;
        public string outputPath;
        public string errorsPath;
        public string failReason;
        public double durationSec;
    }

    public class CIBuildWindow : EditorWindow
    {
        const string LinkFile = "UserSettings/CIBuildLink.json";

        string       _toolDir;
        CiConfig     _cfg;
        DropdownField _formatField, _configField;
        Label        _headLabel, _stateLabel;
        HelpBox      _dirtyBox, _errorBox;
        ScrollView   _history;

        // ---------- menu ----------
        [MenuItem("CI Build/Bang dieu khien", false, 0)]
        public static void Open()
        {
            var w = GetWindow<CIBuildWindow>("CI Build");
            w.minSize = new Vector2(380, 420);
            w.Show();
        }

        [MenuItem("CI Build/Build nhanh - APK dev %&b", false, 20)]
        public static void QuickDevApk() { EnqueueFromMenu("apk", "dev"); }

        [MenuItem("CI Build/Build - AAB release", false, 21)]
        public static void QuickReleaseAab() { EnqueueFromMenu("aab", "release"); }

        [MenuItem("CI Build/Mo thu muc file build", false, 40)]
        public static void OpenBuildsFolder()
        {
            var cfg = LoadConfig(LoadToolDir());
            if (cfg != null && Directory.Exists(cfg.buildsPath)) EditorUtility.RevealInFinder(cfg.buildsPath);
            else Debug.LogWarning("[CI] Chua co thu muc build. Chay install.bat truoc.");
        }

        // ---------- UI ----------
        void CreateGUI()
        {
            _toolDir = LoadToolDir();
            _cfg     = LoadConfig(_toolDir);

            var root = rootVisualElement;
            root.style.paddingLeft = 10; root.style.paddingRight = 10;
            root.style.paddingTop  = 10; root.style.paddingBottom = 10;

            if (_cfg == null)
            {
                root.Add(new HelpBox(
                    "Chua cai dat.\nMo thu muc tool va double-click install.bat, roi mo lai cua so nay.",
                    HelpBoxMessageType.Warning));
                return;
            }

            var title = new Label(_cfg.projectName) { style = { unityFontStyleAndWeight = FontStyle.Bold, fontSize = 14 } };
            root.Add(title);

            _headLabel = new Label { style = { marginBottom = 6, color = new StyleColor(new Color(.6f,.6f,.6f)) } };
            root.Add(_headLabel);

            _dirtyBox = new HelpBox("", HelpBoxMessageType.Info) { style = { display = DisplayStyle.None } };
            root.Add(_dirtyBox);

            _formatField = new DropdownField("Dinh dang", new List<string> { "APK", "AAB" }, 0);
            _configField = new DropdownField("Ban",       new List<string> { "dev", "release" }, 0);
            root.Add(_formatField);
            root.Add(_configField);

            var buildBtn = new Button(OnBuildClicked) { text = "Build (chay nen)" };
            buildBtn.style.height = 32;
            buildBtn.style.marginTop = 8;
            root.Add(buildBtn);

            _stateLabel = new Label { style = { marginTop = 8, marginBottom = 4 } };
            root.Add(_stateLabel);

            _errorBox = new HelpBox("", HelpBoxMessageType.Error) { style = { display = DisplayStyle.None } };
            root.Add(_errorBox);

            var row = new VisualElement { style = { flexDirection = FlexDirection.Row, marginTop = 4 } };
            row.Add(new Button(() => EditorUtility.RevealInFinder(_cfg.buildsPath)) { text = "Mo thu muc build", style = { flexGrow = 1 } });
            row.Add(new Button(Refresh) { text = "Lam moi", style = { flexGrow = 1 } });
            root.Add(row);

            root.Add(new Label("Ket qua gan day") { style = { unityFontStyleAndWeight = FontStyle.Bold, marginTop = 10 } });
            _history = new ScrollView { style = { flexGrow = 1 } };
            root.Add(_history);

            Refresh();
            root.schedule.Execute(Refresh).Every(3000);
        }

        void OnBuildClicked()
        {
            var git = GetGit();
            if (git == null) { ShowError("Khong doc duoc git tai " + _cfg.projectPath); return; }

            if (git.Dirty)
            {
                bool go = EditorUtility.DisplayDialog("Co thay doi chua commit",
                    "Build lay dung commit HEAD.\n\nNhung thay doi chua commit SE KHONG co trong ban build nay.\n\nVan build?",
                    "Build tu HEAD", "Huy");
                if (!go) return;
            }

            try
            {
                var job = Enqueue(_toolDir, _cfg, git,
                                  _formatField.value.ToLower(),
                                  _configField.value,
                                  "unity-editor");
                StartRunner(_toolDir);
                ShowError("");
                _stateLabel.text = "Da xep hang " + job + " - dang build nen...";
                Refresh();
            }
            catch (Exception e) { ShowError(e.Message); }
        }

        void ShowError(string msg)
        {
            if (_errorBox == null) return;
            _errorBox.text = msg;
            _errorBox.style.display = string.IsNullOrEmpty(msg) ? DisplayStyle.None : DisplayStyle.Flex;
        }

        void Refresh()
        {
            if (_cfg == null || _headLabel == null) return;

            var git = GetGit();
            if (git != null)
            {
                _headLabel.text = git.Branch + " @ " + git.ShaShort + "  -  " + git.Subject;
                if (git.Dirty)
                {
                    _dirtyBox.text = "Dang co file chua commit. Ban build se KHONG gom nhung thay doi do.";
                    _dirtyBox.style.display = DisplayStyle.Flex;
                }
                else _dirtyBox.style.display = DisplayStyle.None;
            }

            int queued = 0;
            var qdir = Path.Combine(_cfg.ciRoot, "queue");
            if (Directory.Exists(qdir)) queued = Directory.GetFiles(qdir, "*.json").Length;

            var pdir = Path.Combine(_cfg.ciRoot, "processing");
            bool running = Directory.Exists(pdir) && Directory.GetFiles(pdir, "*.json").Length > 0;

            _stateLabel.text = running
                ? "DANG BUILD" + (queued > 0 ? "  (+" + queued + " cho)" : "")
                : (queued > 0 ? queued + " job dang cho" : "Ranh");

            RefreshHistory();
        }

        void RefreshHistory()
        {
            _history.Clear();
            var rdir = Path.Combine(_cfg.ciRoot, "results");
            if (!Directory.Exists(rdir)) return;

            var files = Directory.GetFiles(rdir, "*.json")
                                 .OrderByDescending(f => f)
                                 .Take(12);

            foreach (var f in files)
            {
                CiResultView r;
                try { r = JsonUtility.FromJson<CiResultView>(File.ReadAllText(f)); }
                catch { continue; }
                if (r == null) continue;

                var row = new VisualElement
                {
                    style = { flexDirection = FlexDirection.Row, marginBottom = 2, alignItems = Align.Center }
                };

                var dot = new Label(r.success ? "OK" : "X")
                {
                    style = { width = 22, unityFontStyleAndWeight = FontStyle.Bold,
                              color = new StyleColor(r.success ? new Color(.3f,.8f,.4f) : new Color(.9f,.35f,.35f)) }
                };
                row.Add(dot);

                var text = r.branch + "@" + r.shaShort + "  " + (r.format ?? "").ToUpper() + "/" + r.config +
                           "  " + Mathf.RoundToInt((float)r.durationSec) + "s";
                row.Add(new Label(text) { style = { flexGrow = 1 } });

                if (r.success && !string.IsNullOrEmpty(r.outputPath))
                    row.Add(new Button(() => EditorUtility.RevealInFinder(r.outputPath)) { text = "..." });
                else if (!string.IsNullOrEmpty(r.errorsPath) && File.Exists(r.errorsPath))
                    row.Add(new Button(() => Process.Start(new ProcessStartInfo(r.errorsPath) { UseShellExecute = true })) { text = "loi" });

                _history.Add(row);
            }
        }

        // ---------- dung chung ----------
        class GitInfo { public string Sha, ShaShort, Branch, Subject; public bool Dirty; public int Count; }

        GitInfo GetGit() { return ReadGit(_cfg.projectPath); }

        static GitInfo ReadGit(string repo)
        {
            try
            {
                var sha = Git(repo, "rev-parse HEAD");
                if (string.IsNullOrEmpty(sha)) return null;
                return new GitInfo
                {
                    Sha      = sha,
                    ShaShort = sha.Substring(0, 7),
                    Branch   = Git(repo, "rev-parse --abbrev-ref HEAD"),
                    Subject  = Git(repo, "log -1 --pretty=%s"),
                    Dirty    = !string.IsNullOrEmpty(Git(repo, "status --porcelain")),
                    Count    = int.TryParse(Git(repo, "rev-list --count HEAD"), out var c) ? c : 0
                };
            }
            catch { return null; }
        }

        static string Git(string repo, string args)
        {
            var psi = new ProcessStartInfo("git", "-C \"" + repo + "\" " + args)
            {
                UseShellExecute        = false,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
                CreateNoWindow         = true
            };
            using (var p = Process.Start(psi))
            {
                var o = p.StandardOutput.ReadToEnd();
                p.WaitForExit(10000);
                return o.Trim();
            }
        }

        static string LoadToolDir()
        {
            try
            {
                if (!File.Exists(LinkFile)) return null;
                var link = JsonUtility.FromJson<CiLink>(File.ReadAllText(LinkFile));
                return link != null ? link.toolDir : null;
            }
            catch { return null; }
        }

        static CiConfig LoadConfig(string toolDir)
        {
            try
            {
                if (string.IsNullOrEmpty(toolDir)) return null;
                var f = Path.Combine(toolDir, "config.json");
                if (!File.Exists(f)) return null;
                return JsonUtility.FromJson<CiConfig>(File.ReadAllText(f));
            }
            catch { return null; }
        }

        static void EnqueueFromMenu(string format, string config)
        {
            var toolDir = LoadToolDir();
            var cfg     = LoadConfig(toolDir);
            if (cfg == null) { Debug.LogError("[CI] Chua cai dat - chay install.bat truoc."); return; }

            var git = ReadGit(cfg.projectPath);
            if (git == null) { Debug.LogError("[CI] Khong doc duoc git."); return; }

            if (git.Dirty && !EditorUtility.DisplayDialog("Co thay doi chua commit",
                    "Build lay dung commit HEAD, nhung thay doi chua commit se khong co trong ban build.\n\nVan build?",
                    "Build tu HEAD", "Huy")) return;

            var id = Enqueue(toolDir, cfg, git, format, config, "unity-menu");
            StartRunner(toolDir);
            Debug.Log("[CI] Da xep hang " + id + " - dang build nen, Editor van dung duoc binh thuong.");
        }

        static string Enqueue(string toolDir, CiConfig cfg, GitInfo git, string format, string config, string by)
        {
            var id = DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 4);
            var job = new CiJob
            {
                id          = id,
                sha         = git.Sha,
                shaShort    = git.ShaShort,
                branch      = git.Branch,
                subject     = git.Subject,
                format      = format,
                config      = config,
                by          = by,
                versionCode = git.Count,
                versionName = "",
                outputPath  = "",
                resultPath  = "",
                createdAt   = DateTime.Now.ToString("o")
            };

            var qdir = Path.Combine(cfg.ciRoot, "queue");
            Directory.CreateDirectory(qdir);
            var tmp = Path.Combine(qdir, id + ".json.tmp");
            var fin = Path.Combine(qdir, id + ".json");
            File.WriteAllText(tmp, JsonUtility.ToJson(job, true));
            if (File.Exists(fin)) File.Delete(fin);
            File.Move(tmp, fin);      // rename = atomic, runner khong doc phai file dang ghi do
            return id;
        }

        static void StartRunner(string toolDir)
        {
            var runner = Path.Combine(toolDir, "runner.ps1");
            if (!File.Exists(runner)) { Debug.LogError("[CI] Khong tim thay runner.ps1"); return; }
            var psi = new ProcessStartInfo("powershell.exe",
                "-NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File \"" + runner + "\"")
            {
                UseShellExecute = false,
                CreateNoWindow  = true
            };
            Process.Start(psi);
        }
    }
}
