// Auto-installed by blender-with-chatgpt / bin/setup.py
// Doc sidecar <model>.import.json do pipeline sinh ra va ap import settings.
// Khong sua .meta bang tay: guid do Unity sinh, viet tay de sai.
#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AssetIntake
{
    public sealed class AssetIntakePostprocessor : AssetPostprocessor
    {
        [System.Serializable]
        private class UnityImportSettings
        {
            public float scaleFactor = 1f;
            public bool importMaterials;
            public bool readWriteEnabled;
            public bool optimizeMesh = true;
            public bool generateColliders;
        }

        [System.Serializable]
        private class Sidecar
        {
            public string brief_id;
            public UnityImportSettings unity_import;
        }

        private void OnPreprocessModel()
        {
            string sidecarPath = assetPath + ".import.json";
            if (!File.Exists(sidecarPath)) return;

            Sidecar sidecar;
            try
            {
                // JsonUtility khong doc snake_case -> normalise truoc
                string raw = File.ReadAllText(sidecarPath)
                    .Replace("\"scale_factor\"", "\"scaleFactor\"")
                    .Replace("\"import_materials\"", "\"importMaterials\"")
                    .Replace("\"read_write_enabled\"", "\"readWriteEnabled\"")
                    .Replace("\"optimize_mesh\"", "\"optimizeMesh\"")
                    .Replace("\"generate_colliders\"", "\"generateColliders\"");
                sidecar = JsonUtility.FromJson<Sidecar>(raw);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[asset-intake] khong doc duoc {sidecarPath}: {e.Message}");
                return;
            }

            if (sidecar?.unity_import == null) return;
            var s = sidecar.unity_import;

            var importer = (ModelImporter)assetImporter;
            importer.globalScale = s.scaleFactor;
            importer.materialImportMode = s.importMaterials
                ? ModelImporterMaterialImportMode.ImportStandard
                : ModelImporterMaterialImportMode.None;
            importer.isReadable = s.readWriteEnabled;
            importer.optimizeMeshPolygons = s.optimizeMesh;
            importer.optimizeMeshVertices = s.optimizeMesh;
            importer.addCollider = s.generateColliders;

            // mobile-first: bo thu khong dung de giam mesh memory
            importer.importCameras = false;
            importer.importLights = false;
            importer.importAnimation = false;
            importer.importBlendShapes = false;
            importer.importVisibility = false;

            Debug.Log($"[asset-intake] ap import settings cho {Path.GetFileName(assetPath)}" +
                      $" (brief {sidecar.brief_id})");
        }
    }
}
#endif
