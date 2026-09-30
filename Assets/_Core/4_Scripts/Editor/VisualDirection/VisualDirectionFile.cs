using System.Collections.Generic;
using System.Globalization;
using System.IO;

namespace AgentPack.VisualDirection
{
    /// <summary>
    /// Doc visual-direction/v1 dang cay MiniJson. Dung chung cho importer, motion parity va capture.
    /// File that duoc chon qua con tro <c>handoff/visual/CURRENT</c> (promote-direction.ps1 ghi).
    /// </summary>
    public sealed class VisualDirectionFile
    {
        public const string Schema = "visual-direction/v1";
        public const string Approved = "APPROVED";

        private VisualDirectionFile(string path, Dictionary<string, object> root)
        {
            Path = path;
            Root = root;
        }

        public string Path { get; }
        public Dictionary<string, object> Root { get; }
        public int Version => Root.TryGetValue("version", out object v) && v is double d ? (int)d : 0;

        /// <summary>Doc file ma CURRENT tro toi; null neu chua co direction nao duoc promote.</summary>
        public static VisualDirectionFile LoadCurrent()
        {
            string path = VisualDirectionImporter.ResolveCurrentPath();
            return path == null ? null : Parse(path, File.ReadAllText(path));
        }

        /// <summary>Parse + kiem schema. Sai schema => InvalidDataException (khong doan).</summary>
        public static VisualDirectionFile Parse(string path, string json)
        {
            if (!(MiniJson.Deserialize(json) is Dictionary<string, object> root))
                throw new InvalidDataException($"{path}: goc JSON khong phai object");
            string schema = root.TryGetValue("schema", out object s) ? s as string : null;
            if (schema != Schema) throw new InvalidDataException($"{path}: schema '{schema}' khac '{Schema}'");
            return new VisualDirectionFile(path, root);
        }

        public Dictionary<string, object> Section(string name) =>
            Root.TryGetValue("sections", out object sec) && sec is Dictionary<string, object> sections &&
            sections.TryGetValue(name, out object v) ? v as Dictionary<string, object> : null;

        public string SectionStatus(string name)
        {
            Dictionary<string, object> section = Section(name);
            return section != null && section.TryGetValue("status", out object st) ? st as string : null;
        }

        /// <summary>Tolerance parity: lay tu file, thieu thi dung mac dinh cua MotionParity.</summary>
        public float Tolerance(string key, float fallback)
        {
            if (Root.TryGetValue("tolerance", out object t) && t is Dictionary<string, object> tol &&
                tol.TryGetValue(key, out object v) && v is double d)
                return (float)d;
            return fallback;
        }

        public static float Number(Dictionary<string, object> obj, string key, float fallback = 0f)
        {
            if (obj == null || !obj.TryGetValue(key, out object v)) return fallback;
            if (v is double d) return (float)d;
            if (v is string s && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float f)) return f;
            return fallback;
        }
    }
}
