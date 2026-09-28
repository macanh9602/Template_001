using UnityEngine;

namespace Haptic.Visual
{
    [CreateAssetMenu(fileName = "HapticBurstSettings", menuName = "ScriptableObjects/HapticBurstSettings")]
    public class HapticBurstSettings : ScriptableObject
    {
        private const string RESOURCE_FOLDER_PATH = "Data/HapticBurstSettings";

        private static ResourceAsset<HapticBurstSettings> asset = new(RESOURCE_FOLDER_PATH);

        [SerializeField] private HapticType hapticType = HapticType.Light;

        [SerializeField, Min(0)] private int minPulses = 1;
        [SerializeField, Min(1)] private int maxPulses = 8;

        [SerializeField, Min(0f)] private float totalDuration = 0.4f;

        [SerializeField, Min(0f)] private float firstDelay = 0f;

        [SerializeField]
        private AnimationCurve intervalCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        public static bool IsAvailable => asset.Value != null;
        public static HapticType BurstHapticType => asset.Value.hapticType;
        public static float TotalDuration => asset.Value.totalDuration;
        public static float FirstDelay => asset.Value.firstDelay;
        public static AnimationCurve IntervalCurve => asset.Value.intervalCurve;

        /// <summary>n = clamp(totalTiles, min, max).</summary>
        public static int ResolvePulseCount(int totalTiles)
        {
            var data = asset.Value;
            int min = Mathf.Max(0, data.minPulses);
            int max = Mathf.Max(min, data.maxPulses);
            return Mathf.Clamp(totalTiles, min, max);
        }
    }
}
