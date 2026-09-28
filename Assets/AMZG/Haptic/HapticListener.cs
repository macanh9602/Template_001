
// using UnityEngine;

// namespace Haptic.Visual
// {
//     public static class HapticListener
//     {
//         private static bool isRegistered;

//         [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
//         private static void Register()
//         {
//             if (isRegistered)
//             {
//                 return;
//             }
//             ServiceLocator.Global.EventBus
//                 .SubscribeWithDisposable<RequestPlayHoleSelectionHapticEvent>(OnRequestPlayHoleSelectionHaptic);
//             isRegistered = true;
//         }

//         private static void OnRequestPlayHoleSelectionHaptic(RequestPlayHoleSelectionHapticEvent evt)
//         {
//             HapticController.TriggerHaptic(ToHapticType(evt.Type));
//         }

//         private static HapticType ToHapticType(HoleHapticType type)
//         {
//             return type switch
//             {
//                 HoleHapticType.Warning => HapticType.Warning,
//                 HoleHapticType.Failure => HapticType.Failure,
//                 HoleHapticType.Success => HapticType.Success,
//                 HoleHapticType.Light => HapticType.Light,
//                 HoleHapticType.Medium => HapticType.Medium,
//                 HoleHapticType.Heavy => HapticType.Heavy,
//                 HoleHapticType.Default => HapticType.Default,
//                 HoleHapticType.Vibrate => HapticType.Vibrate,
//                 _ => HapticType.Selection
//             };
//         }
//     }
// }
