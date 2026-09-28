using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using VTLTools;
namespace Haptic.Visual
{
    public enum HapticType
    {
        Warning,
        Failure,
        Success,
        Light,
        Medium,
        Heavy,
        Default,
        Vibrate,
        Selection
    }

    public static class HapticController
    {
        public static void TriggerHaptic(HapticType type)
        {
            //Debug.Log($"<color=green>[DA]</color> TriggerHaptic: {type}");
            if (!StaticVariables.IsVibrationOn)
            {
                return;
            }
            switch (type)
            {
                case HapticType.Warning:
                    Taptic.Warning();
                    break;
                case HapticType.Failure:
                    Taptic.Failure();
                    break;
                case HapticType.Success:
                    Taptic.Success();
                    break;
                case HapticType.Light:
                    Taptic.Light();
                    break;
                case HapticType.Medium:
                    Taptic.Medium();
                    break;
                case HapticType.Heavy:
                    Taptic.Heavy();
                    break;
                case HapticType.Default:
                    Taptic.Default();
                    break;
                case HapticType.Vibrate:
                    Taptic.Vibrate();
                    break;
                case HapticType.Selection:
                    Taptic.Selection();
                    break;
                default:
                    break;
            }
        }

        public static void PlayTrainStartBurst(int totalTiles)
        {
            if (!StaticVariables.IsVibrationOn || !HapticBurstSettings.IsAvailable)
            {
                return;
            }

            int count = HapticBurstSettings.ResolvePulseCount(totalTiles);
            PlayHapticWithCurve(
                HapticBurstSettings.BurstHapticType,
                count,
                HapticBurstSettings.TotalDuration,
                HapticBurstSettings.IntervalCurve,
                HapticBurstSettings.FirstDelay);
        }

        public static void PlayHapticWithCurve(
            HapticType type,
            int count,
            float totalDuration = 0.4f,
            AnimationCurve intervalCurve = null,
            float firstDelay = 0f)
        {
            if (count <= 0 || !StaticVariables.IsVibrationOn)
            {
                return;
            }

            if (intervalCurve == null || intervalCurve.length == 0)
            {
                intervalCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);
            }

            if (firstDelay > 0f)
            {
                DOVirtual.DelayedCall(firstDelay, () => PlayNextPulse(type, 0, count, totalDuration, intervalCurve));
            }
            else
            {
                PlayNextPulse(type, 0, count, totalDuration, intervalCurve);
            }
        }

        private static void PlayNextPulse(HapticType type, int currentCount, int total, float duration, AnimationCurve curve)
        {
            if (currentCount >= total)
            {
                return;
            }

            // TriggerHaptic tự kiểm tra IsVibrationOn (cổng gate), nên đổi setting giữa chừng vẫn an toàn.
            TriggerHaptic(type);

            float progress = (float)currentCount / total;
            float baseInterval = total > 0 ? duration / total : duration;
            float dynamicInterval = baseInterval * curve.Evaluate(progress);

            if (dynamicInterval <= 0f)
            {
                PlayNextPulse(type, currentCount + 1, total, duration, curve);
                return;
            }

            DOVirtual.DelayedCall(dynamicInterval, () => PlayNextPulse(type, currentCount + 1, total, duration, curve));
        }
    }
}
