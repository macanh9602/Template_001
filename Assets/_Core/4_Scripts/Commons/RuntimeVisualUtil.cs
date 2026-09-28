using UnityEngine;

namespace CH013.Commons
{
    /// <summary>Placeholder-art helpers: unlit quads, 3D text, deterministic category colors.</summary>
    public static class RuntimeVisualUtil
    {
        private static Material unlitBase;

        public static Material CreateUnlitColor(Color color)
        {
            if (unlitBase == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
                if (shader == null)
                {
                    shader = Shader.Find("Unlit/Color");
                }

                unlitBase = new Material(shader);
            }

            var material = new Material(unlitBase);
            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.color = color;
            }

            return material;
        }

        public static GameObject CreateQuad(string name, Transform parent, Vector3 localPos, Vector2 size, Color color)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.SetParent(parent, false);
            quad.transform.localPosition = localPos;
            quad.transform.localRotation = Quaternion.identity;
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            quad.GetComponent<MeshRenderer>().sharedMaterial = CreateUnlitColor(color);
            return quad;
        }

        public static TextMesh CreateText(string name, Transform parent, Vector3 localPos, string text, float characterSize, Color color, TextAnchor anchor = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            var textMesh = go.AddComponent<TextMesh>();
            textMesh.text = text;
            textMesh.characterSize = characterSize;
            textMesh.fontSize = 48;
            textMesh.anchor = anchor;
            textMesh.alignment = TextAlignment.Center;
            textMesh.color = color;
            return textMesh;
        }

    }
}
