using System;
using UnityEngine;

namespace AgentPack.VisualDirection.Tests
{
    /// <summary>Profile gia cho EditMode test cua importer (chi tao bang ScriptableObject.CreateInstance, khong co asset).</summary>
    public sealed class VisualDirectionTestProfile : ScriptableObject
    {
        public enum FloorStyle { Slab, TileGrid }

        [Serializable]
        public sealed class CameraBlock
        {
            public float tiltDeg = 10f;
            public int fov = 30;
        }

        public CameraBlock camera = new CameraBlock();
        public float seamStrength = 0.5f;
        public bool shadows;
        public string label = "baseline";
        public FloorStyle floorStyle = FloorStyle.Slab;
        public Color tint = Color.white;
    }
}
