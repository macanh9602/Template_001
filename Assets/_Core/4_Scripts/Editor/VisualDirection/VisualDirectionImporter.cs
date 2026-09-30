using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AgentPack.VisualDirection
{
    /// <summary>
    /// visual-direction/v1 → Profile ScriptableObject, theo <c>profileMap</c> (khoa lab → "TenAsset.field.path").
    /// Chi nhap section co status APPROVED; section DRAFT/CANDIDATE bi bo qua (lab la proposal, khong phai target).
    /// Dry Run khong ghi gi, chi liet ke chenh lech = drift giua target da duyet va asset trong project.
    /// Asset la derived: sua tay field co trong profileMap se bi Import lan sau ghi de.
    /// Harvest tu Ducan_SpeedRun_Demo VisualTargetImporter (ban do viet cung tung field).
    /// </summary>
    public static class VisualDirectionImporter
    {
        public const string Schema = VisualDirectionFile.Schema;
        public const string PointerPath = "handoff/visual/CURRENT";
        public const string Approved = VisualDirectionFile.Approved;

        [MenuItem("Tools/Visual Direction/Dry Run (log drift)")]
        private static void DryRunMenu() => Run(false);

        [MenuItem("Tools/Visual Direction/Import approved sections")]
        private static void ImportMenu() => Run(true);

        /// <summary>Entry cho menu, Unity MCP va runner: doc file ma handoff/visual/CURRENT tro toi.</summary>
        public static ImportReport Run(bool apply)
        {
            string path = ResolveCurrentPath();
            if (path == null)
            {
                var missing = new ImportReport(apply);
                missing.Errors.Add($"khong tim thay {PointerPath} hoac file no tro toi");
                Debug.LogError(missing.ToString());
                return missing;
            }
            ImportReport report = Import(File.ReadAllText(path), apply, FindProfileAsset);
            report.Source = path;
            // Loi, hoac Dry Run con drift => LogWarning, de script verify (read_console types=warning) bat duoc.
            if (report.Errors.Count > 0 || (!apply && report.Changes.Count > 0)) Debug.LogWarning(report.ToString());
            else Debug.Log(report.ToString());
            return report;
        }

        public static string ResolveCurrentPath()
        {
            string root = Directory.GetCurrentDirectory();
            string pointer = Path.Combine(root, PointerPath);
            if (!File.Exists(pointer)) return null;
            string rel = File.ReadAllText(pointer).Trim().TrimStart('\uFEFF');
            if (string.IsNullOrEmpty(rel)) return null;
            string full = Path.IsPathRooted(rel) ? rel : Path.Combine(root, rel);
            return File.Exists(full) ? full : null;
        }

        /// <summary>Tim ScriptableObject theo ten file (khong phan biet thu muc). Nhieu ket qua trung ten => null.</summary>
        public static UnityEngine.Object FindProfileAsset(string assetName)
        {
            UnityEngine.Object found = null;
            foreach (string guid in AssetDatabase.FindAssets($"{assetName} t:ScriptableObject"))
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.Equals(Path.GetFileNameWithoutExtension(assetPath), assetName, StringComparison.Ordinal)) continue;
                if (found != null)
                {
                    Debug.LogWarning($"[VisualDirection] nhieu asset ten '{assetName}'; dat ten duy nhat de import dung.");
                    return null;
                }
                found = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            }
            return found;
        }

        public static ImportReport Import(string json, bool apply, Func<string, UnityEngine.Object> findAsset)
        {
            var report = new ImportReport(apply);
            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> root))
            {
                report.Errors.Add("goc JSON khong phai object");
                return report;
            }
            string schema = root.TryGetValue("schema", out object s) ? s as string : null;
            if (schema != Schema)
            {
                report.Errors.Add($"schema '{schema}' khac '{Schema}'");
                return report;
            }
            report.Version = root.TryGetValue("version", out object v) && v is double dv ? (int)dv : 0;
            var profileMap = root.TryGetValue("profileMap", out object pm) ? pm as Dictionary<string, object> : null;
            var sections = root.TryGetValue("sections", out object sec) ? sec as Dictionary<string, object> : null;
            if (profileMap == null || sections == null)
            {
                report.Errors.Add("thieu profileMap hoac sections");
                return report;
            }

            var serialized = new Dictionary<string, SerializedObject>(StringComparer.Ordinal);
            var missingAssets = new HashSet<string>(StringComparer.Ordinal);

            foreach (KeyValuePair<string, object> sectionPair in sections)
            {
                if (!(sectionPair.Value is Dictionary<string, object> section)) continue;
                string status = section.TryGetValue("status", out object st) ? st as string : null;
                if (status != Approved)
                {
                    report.Skipped.Add($"section '{sectionPair.Key}' = {status ?? "?"} (chua duyet, khong import)");
                    continue;
                }
                if (!(section.TryGetValue("values", out object vals) && vals is Dictionary<string, object> values)) continue;

                foreach (KeyValuePair<string, object> kv in values)
                {
                    if (!profileMap.TryGetValue(kv.Key, out object mapObj) || !(mapObj is string mapping) || string.IsNullOrEmpty(mapping))
                    {
                        report.Skipped.Add($"{sectionPair.Key}.{kv.Key}: khong co trong profileMap");
                        continue;
                    }
                    int dot = mapping.IndexOf('.');
                    if (dot <= 0 || dot == mapping.Length - 1)
                    {
                        report.Errors.Add($"{kv.Key}: profileMap '{mapping}' phai co dang TenAsset.field.path");
                        continue;
                    }
                    string assetName = mapping.Substring(0, dot);
                    string fieldPath = mapping.Substring(dot + 1);
                    if (assetName == "Mesh")
                    {
                        report.Skipped.Add($"{kv.Key} -> {mapping}: mesh brief, khong phai runtime data");
                        continue;
                    }
                    if (!serialized.TryGetValue(assetName, out SerializedObject so))
                    {
                        if (missingAssets.Contains(assetName)) continue;
                        UnityEngine.Object asset = findAsset(assetName);
                        if (asset == null)
                        {
                            missingAssets.Add(assetName);
                            report.Errors.Add($"khong tim thay asset '{assetName}' (tao Profile SO ten nay truoc)");
                            continue;
                        }
                        so = new SerializedObject(asset);
                        serialized[assetName] = so;
                    }
                    SerializedProperty prop = so.FindProperty(fieldPath);
                    if (prop == null)
                    {
                        report.Errors.Add($"{mapping}: khong co field '{fieldPath}' tren {assetName}");
                        continue;
                    }
                    if (!TryConvert(prop, kv.Value, out string oldText, out string newText, out bool changed, out Action write, out string error))
                    {
                        report.Errors.Add($"{mapping}: {error}");
                        continue;
                    }
                    if (!changed) { report.Unchanged++; continue; }
                    report.Changes.Add($"{mapping}: {oldText} -> {newText}");
                    if (apply) write();
                }
            }

            if (apply && report.Changes.Count > 0)
            {
                foreach (SerializedObject so in serialized.Values)
                {
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(so.targetObject);
                }
                AssetDatabase.SaveAssets();
            }
            return report;
        }

        /// <summary>So sanh + chuan bi ghi mot gia tri JSON vao SerializedProperty. Khong ghi cho toi khi goi write().</summary>
        public static bool TryConvert(SerializedProperty prop, object value, out string oldText, out string newText,
            out bool changed, out Action write, out string error)
        {
            oldText = newText = null;
            changed = false;
            write = null;
            error = null;
            switch (prop.propertyType)
            {
                case SerializedPropertyType.Float:
                {
                    if (!TryNumber(value, out double d)) { error = $"can so, nhan '{value}'"; return false; }
                    float f = (float)d;
                    oldText = prop.floatValue.ToString("0.####", CultureInfo.InvariantCulture);
                    newText = f.ToString("0.####", CultureInfo.InvariantCulture);
                    changed = Math.Abs(prop.floatValue - f) > 1e-5f;
                    write = () => prop.floatValue = f;
                    return true;
                }
                case SerializedPropertyType.Integer:
                {
                    if (!TryNumber(value, out double d)) { error = $"can so nguyen, nhan '{value}'"; return false; }
                    int n = (int)Math.Round(d);
                    oldText = prop.intValue.ToString(CultureInfo.InvariantCulture);
                    newText = n.ToString(CultureInfo.InvariantCulture);
                    changed = prop.intValue != n;
                    write = () => prop.intValue = n;
                    return true;
                }
                case SerializedPropertyType.Boolean:
                {
                    if (!TryBool(value, out bool b)) { error = $"can bool (true/false/on/off), nhan '{value}'"; return false; }
                    oldText = prop.boolValue ? "true" : "false";
                    newText = b ? "true" : "false";
                    changed = prop.boolValue != b;
                    write = () => prop.boolValue = b;
                    return true;
                }
                case SerializedPropertyType.String:
                {
                    string text = value == null ? string.Empty : Convert.ToString(value, CultureInfo.InvariantCulture);
                    oldText = prop.stringValue;
                    newText = text;
                    changed = !string.Equals(prop.stringValue, text, StringComparison.Ordinal);
                    write = () => prop.stringValue = text;
                    return true;
                }
                case SerializedPropertyType.Enum:
                {
                    int index = FindEnumIndex(prop.enumNames, value);
                    if (index < 0) { error = $"'{value}' khong thuoc enum [{string.Join(", ", prop.enumNames)}]"; return false; }
                    oldText = prop.enumValueIndex >= 0 && prop.enumValueIndex < prop.enumNames.Length ? prop.enumNames[prop.enumValueIndex] : prop.enumValueIndex.ToString(CultureInfo.InvariantCulture);
                    newText = prop.enumNames[index];
                    changed = prop.enumValueIndex != index;
                    write = () => prop.enumValueIndex = index;
                    return true;
                }
                case SerializedPropertyType.Color:
                {
                    if (!(value is string hex) || !ColorUtility.TryParseHtmlString(hex, out Color c)) { error = $"can mau '#RRGGBB', nhan '{value}'"; return false; }
                    oldText = "#" + ColorUtility.ToHtmlStringRGBA(prop.colorValue);
                    newText = "#" + ColorUtility.ToHtmlStringRGBA(c);
                    changed = !string.Equals(oldText, newText, StringComparison.Ordinal);
                    write = () => prop.colorValue = c;
                    return true;
                }
                default:
                    error = $"kieu field {prop.propertyType} chua ho tro";
                    return false;
            }
        }

        private static bool TryNumber(object value, out double d)
        {
            if (value is double x) { d = x; return true; }
            if (value is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out d)) return true;
            d = 0;
            return false;
        }

        private static bool TryBool(object value, out bool b)
        {
            if (value is bool x) { b = x; return true; }
            if (value is string s)
            {
                switch (s.Trim().ToLowerInvariant())
                {
                    case "true": case "on": case "yes": b = true; return true;
                    case "false": case "off": case "no": b = false; return true;
                }
            }
            if (value is double d) { b = Math.Abs(d) > double.Epsilon; return true; }
            b = false;
            return false;
        }

        private static int FindEnumIndex(string[] names, object value)
        {
            if (value is double d)
            {
                int i = (int)Math.Round(d);
                return i >= 0 && i < names.Length ? i : -1;
            }
            if (!(value is string s)) return -1;
            string wanted = Normalize(s);
            for (int i = 0; i < names.Length; i++)
            {
                if (Normalize(names[i]) == wanted) return i;
            }
            return -1;
        }

        private static string Normalize(string s)
        {
            var sb = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (c == ' ' || c == '_' || c == '-') continue;
                sb.Append(char.ToLowerInvariant(c));
            }
            return sb.ToString();
        }
    }

    public sealed class ImportReport
    {
        public ImportReport(bool applied) { Applied = applied; }

        public bool Applied { get; }
        public string Source { get; set; }
        public int Version { get; set; }
        public int Unchanged { get; set; }
        public List<string> Changes { get; } = new List<string>();
        public List<string> Skipped { get; } = new List<string>();
        public List<string> Errors { get; } = new List<string>();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.Append("[VisualDirection] ").Append(Applied ? "IMPORT" : "DRY RUN")
              .Append(" v").Append(Version).Append(" - ")
              .Append(Changes.Count).Append(Applied ? " thay doi" : " chenh lech (drift)")
              .Append(", ").Append(Unchanged).Append(" khop, ")
              .Append(Errors.Count).Append(" loi");
            if (!string.IsNullOrEmpty(Source)) sb.Append("\nnguon: ").Append(Source);
            foreach (string c in Changes) sb.Append("\n  ~ ").Append(c);
            foreach (string e in Errors) sb.Append("\n  ! ").Append(e);
            foreach (string k in Skipped) sb.Append("\n  - ").Append(k);
            return sb.ToString();
        }
    }
}
