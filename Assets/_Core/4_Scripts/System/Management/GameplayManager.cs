// using System;
// using System.Collections;
// using System.Collections.Generic;
// using CH013.Commons;
// using CH013.Data;
// using CH013.StickerSystem;
// using SFX.Visual;
// using UnityEngine;

// namespace CH013.Gameplay
// {
//     /// <summary>
//     /// Orchestrates one level: tap resolution, card stack refills, queue auto-fill,
//     /// piece drops and win/lose. Logic commits happen instantly (LevelRuntimeState);
//     /// this class only sequences the visuals around those commits.
//     /// </summary>
//     public sealed class GameplayManager : MonoBehaviour, IPendingCleanup
//     {
//         private const float CardCompleteSoundMinDelay = 0.08f;

//         [SerializeField, Min(0.05f)] private float cardFlyDuration = 0.2f;
//         [SerializeField, Min(0.05f)] private float autoFillMoveDuration = 0.4f;
//         [SerializeField, Min(0.05f)] private float pieceDropDuration = 0.6f;
//         [SerializeField, Min(0.05f)] private float mergeJumpDuration = 0.12f;
//         [SerializeField, Min(0.05f)] private float mergeFlyDuration = 0.18f;
//         [SerializeField, Min(0f)] private float mergeJumpHeight = 0.35f;

//         public event Action<int, CategoryType> CardCompleted;
//         public event Action<int> QueueChanged;
//         public event Action<CategoryType> MergeCompleted;
//         public event Action<PieceView> PieceCleared;
//         public event Action LevelWon;
//         public event Action LevelLost;

//         private readonly LevelRuntimeState state = new LevelRuntimeState();
//         private readonly List<StickerView> queueStickers = new List<StickerView>();
//         private readonly List<int> mergeIndices = new List<int>(3);
//         private readonly List<StickerView> mergeStickers = new List<StickerView>(3);
//         private readonly List<Vector3> mergeStartPositions = new List<Vector3>(3);

//         private ModelBoard board;
//         private CardView[] cardViews;
//         private CardStackView[] stackViews;
//         private QueueView queueView;
//         private StickerFlightAnimator animator;
//         private GameplayRuntimeProfile runtimeProfile;
//         private float queueLandSizeWorld = StickerLandTraySettings.Default.queueLandSizeWorld;
//         private float cardLandSizeWorld = StickerLandTraySettings.Default.cardLandSizeWorld;
//         private bool progressCardMode;
//         private bool creativeChainMode;
//         private bool mergeQueueMode;
//         private bool gameOver;
//         private bool inputLocked;
//         private int cardResolveDepth;

//         public bool IsGameOver => gameOver;
//         public LevelRuntimeState State => state;

//         public void Initialize(LevelJson level, ModelBoard modelBoard, CardView[] cards, CardStackView[] stacks, QueueView queue, StickerFlightAnimator flightAnimator, GameplayRuntimeProfile profile = null)
//         {
//             board = modelBoard;
//             cardViews = cards;
//             stackViews = stacks;
//             queueView = queue;
//             animator = flightAnimator;
//             runtimeProfile = profile;
//             CacheLandSizes();
//             mergeQueueMode = level != null && level.IsCreativeMergeQueueMode;
//             progressCardMode = level != null && level.IsCreativeMultiSlotsCardMode;
//             creativeChainMode = level != null && level.IsCreativeMultiPileMode;
//             gameOver = false;
//             inputLocked = false;
//             cardResolveDepth = 0;
//             queueStickers.Clear();
//             mergeIndices.Clear();
//             mergeStickers.Clear();
//             mergeStartPositions.Clear();

//             state.Initialize(level);
//             UpdateQueueWarning();
//             for (int i = 0; stackViews != null && i < stackViews.Length; i++)
//             {
//                 if (stackViews[i] != null)
//                 {
//                     stackViews[i].SetCount(state.StackRemainingAt(i));
//                 }
//             }

//             for (int i = 0; cardViews != null && i < cardViews.Length; i++)
//             {
//                 SpawnCardIntoSlot(i);
//             }
//         }

//         public void CleanupForLevelUnload()
//         {
//             StopAllCoroutines();
//             inputLocked = true;
//             cardResolveDepth = 0;
//             if (queueView != null)
//             {
//                 queueView.SetWarningActive(false);
//             }

//             queueStickers.Clear();
//         }

//         /// <summary>Called by the input controller with a screen-space tap.</summary>
//         public void HandleTap(Ray ray)
//         {
//             if (gameOver || inputLocked || board == null)
//             {
//                 return;
//             }

//             if (!board.TryPickSticker(ray, out PieceView piece, out StickerView sticker))
//             {
//                 return;
//             }

//             var def = sticker.Def;
//             if (def == null)
//             {
//                 return;
//             }

//             TapResolution resolution = state.ResolveTap(def.CategoryType, sticker);
//             if (resolution.destination == TapDestination.Queue && resolution.queueFull)
//             {
//                 Lose();
//                 return;
//             }

//             sticker.IsCollected = true;

//             Transform target;
//             float targetLandSizeWorld;
//             if (resolution.destination == TapDestination.Card)
//             {
//                 target = cardViews[resolution.slotIndex].GetSlotAnchor(resolution.positionIndex);
//                 targetLandSizeWorld = cardLandSizeWorld;
//             }
//             else
//             {
//                 target = queueView.GetSlotAnchor(resolution.positionIndex);
//                 targetLandSizeWorld = queueLandSizeWorld;
//                 queueStickers.Add(sticker);
//                 NotifyQueueChanged();
//             }

//             TapResolution captured = resolution;
//             StickerView capturedSticker = sticker;
//             bool dropPieceAfterPeel = piece.RemainingStickers == 0;
//             if (mergeQueueMode)
//             {
//                 inputLocked = true;
//             }

//             if (captured.cardCompleted)
//             {
//                 inputLocked = true;
//             }

//             animator.AnimateCollect(
//                 sticker,
//                 target,
//                 targetLandSizeWorld,
//                 () =>
//                 {
//                     if (mergeQueueMode)
//                     {
//                         StartCoroutine(ResolveMergeQueueAfterLanding());
//                         return;
//                     }

//                     if (captured.destination != TapDestination.Card)
//                     {
//                         return;
//                     }

//                     cardViews[captured.slotIndex].HandleStickerLanded(capturedSticker, captured.positionIndex + 1);
//                     if (captured.cardCompleted)
//                     {
//                         StartCoroutine(CompleteCardRoutine(captured.slotIndex));
//                     }
//                 },
//                 () =>
//                 {
//                     if (!dropPieceAfterPeel || piece == null || piece.IsDropped)
//                     {
//                         return;
//                     }

//                     piece.Drop(pieceDropDuration);
//                     PieceCleared?.Invoke(piece);
//                 },
//                 () =>
//                 {
//                     if (captured.destination == TapDestination.Card)
//                     {
//                         CardView targetCard = cardViews[captured.slotIndex];
//                         targetCard.PlayStickerSnapFeedback(capturedSticker);
//                         targetCard.PlayStickerImpactFeedback();
//                     }
//                 });
//         }

//         private IEnumerator ResolveMergeQueueAfterLanding()
//         {
//             while (!gameOver && state.TryFindMergeTriple(mergeIndices, out CategoryType category))
//             {
//                 yield return MergeQueueTripleRoutine(category);
//             }

//             if (!gameOver && state.QueueCount >= state.QueueSize && !state.HasMergeTriple())
//             {
//                 Lose();
//             }

//             CheckWin();
//             inputLocked = gameOver;
//         }

//         private IEnumerator MergeQueueTripleRoutine(CategoryType category)
//         {
//             mergeStickers.Clear();
//             for (int i = 0; i < mergeIndices.Count; i++)
//             {
//                 int index = mergeIndices[i];
//                 if (index >= 0 && index < queueStickers.Count)
//                 {
//                     mergeStickers.Add(queueStickers[index]);
//                 }
//             }

//             yield return JumpMergeStickers();

//             Transform landAnchor = queueView != null ? queueView.GetNameBoxLandAnchor() : null;
//             for (int i = 0; i < mergeStickers.Count; i++)
//             {
//                 StickerView sticker = mergeStickers[i];
//                 if (sticker != null && landAnchor != null)
//                 {
//                     animator.AnimateMove(sticker.transform, landAnchor, mergeFlyDuration, null);
//                 }
//             }

//             yield return new WaitForSeconds(mergeFlyDuration);
//             SpawnMergeCompleteEffect(landAnchor);

//             for (int i = 0; i < mergeStickers.Count; i++)
//             {
//                 if (mergeStickers[i] != null)
//                 {
//                     mergeStickers[i].gameObject.SetActive(false);
//                 }
//             }

//             RemoveQueueStickersAt(mergeIndices);
//             state.RemoveQueueIndices(mergeIndices);
//             CompactQueueVisuals();
//             NotifyQueueChanged();

//             if (queueView != null)
//             {
//                 queueView.PlayMergeName(category, ResolveCategoryColor(category));
//             }

//             MergeCompleted?.Invoke(category);
//             yield return new WaitForSeconds(autoFillMoveDuration * 0.6f);
//         }

//         private IEnumerator JumpMergeStickers()
//         {
//             float duration = Mathf.Max(0.01f, mergeJumpDuration);
//             float height = Mathf.Max(0f, mergeJumpHeight);
//             Vector3 up = queueView != null ? queueView.transform.up : Vector3.up;
//             mergeStartPositions.Clear();
//             for (int i = 0; i < mergeStickers.Count; i++)
//             {
//                 mergeStartPositions.Add(mergeStickers[i] != null ? mergeStickers[i].transform.position : Vector3.zero);
//             }

//             float elapsed = 0f;
//             while (elapsed < duration)
//             {
//                 elapsed += Time.deltaTime;
//                 float t = Mathf.Clamp01(elapsed / duration);
//                 float arc = Mathf.Sin(t * Mathf.PI) * height;
//                 for (int i = 0; i < mergeStickers.Count; i++)
//                 {
//                     if (mergeStickers[i] != null)
//                     {
//                         mergeStickers[i].transform.position = mergeStartPositions[i] + up * arc;
//                     }
//                 }

//                 yield return null;
//             }
//         }

//         private void RemoveQueueStickersAt(List<int> indices)
//         {
//             for (int i = indices.Count - 1; i >= 0; i--)
//             {
//                 int index = indices[i];
//                 if (index >= 0 && index < queueStickers.Count)
//                 {
//                     queueStickers.RemoveAt(index);
//                 }
//             }
//         }

//         private Color ResolveCategoryColor(CategoryType category)
//         {
//             return runtimeProfile != null ? runtimeProfile.GetCategoryColor(category) : RuntimeVisualUtil.CategoryColor(category);
//         }

//         private static void PlayGameplaySound(AudioClip clip, float minDelay)
//         {
//             if (clip == null)
//             {
//                 return;
//             }

//             AudioController.PlaySound(clip, minDelay: minDelay);
//         }

//         private void SpawnMergeCompleteEffect(Transform landAnchor)
//         {
//             if (landAnchor == null)
//             {
//                 return;
//             }

//             EffectsProfile.SpawnEffect(EffectsProfile.EffectId.Starts_Sparks, landAnchor.position);
//         }

//         private IEnumerator CompleteCardRoutine(int slotIndex)
//         {
//             if (gameOver)
//             {
//                 yield break;
//             }

//             cardResolveDepth++;
//             PlayGameplaySound(AudioController.Sounds?.completeClip, CardCompleteSoundMinDelay);
//             CardCompleted?.Invoke(slotIndex, state.Slots[slotIndex].category);
//             yield return cardViews[slotIndex].FlyOutAndClear(cardFlyDuration);
//             state.ClearSlot(slotIndex);

//             bool slotReady = false;
//             SpawnCardIntoSlot(slotIndex, () => slotReady = true);
//             while (!slotReady)
//             {
//                 yield return null;
//             }

//             yield return new WaitForSeconds(cardFlyDuration * 0.5f);

//             List<AutoFillMove> moves = state.AutoFill();
//             var chainedCompletions = new List<int>();
//             for (int i = 0; i < moves.Count; i++)
//             {
//                 AutoFillMove move = moves[i];
//                 var sticker = move.token as StickerView;
//                 if (sticker != null)
//                 {
//                     queueStickers.Remove(sticker);
//                     CardView targetCard = cardViews[move.slotIndex];
//                     int committed = move.positionIndex + 1;
//                     Transform anchor = targetCard.GetSlotAnchor(move.positionIndex);
//                     animator.AnimateMove(
//                         sticker.transform,
//                         anchor,
//                         autoFillMoveDuration,
//                         () =>
//                         {
//                             targetCard.PlayStickerSnapFeedback(sticker);
//                             targetCard.PlayStickerImpactFeedback();
//                             targetCard.HandleStickerLanded(sticker, committed);
//                         },
//                         cardLandSizeWorld);
//                 }

//                 if (move.cardCompleted)
//                 {
//                     chainedCompletions.Add(move.slotIndex);
//                 }
//             }

//             if (moves.Count > 0)
//             {
//                 CompactQueueVisuals();
//                 NotifyQueueChanged();
//                 yield return new WaitForSeconds(autoFillMoveDuration);
//             }

//             for (int i = 0; i < chainedCompletions.Count; i++)
//             {
//                 yield return CompleteCardRoutine(chainedCompletions[i]);
//             }

//             CheckWin();
//             cardResolveDepth--;
//             if (cardResolveDepth <= 0)
//             {
//                 cardResolveDepth = 0;
//                 inputLocked = gameOver;
//             }
//         }

//         private void SpawnCardIntoSlot(int slotIndex, Action onReady = null)
//         {
//             if (state.TrySpawnCard(slotIndex, out CategoryType category, out int capacity, out int pileIndex))
//             {
//                 CardStackView stack = stackViews != null && pileIndex >= 0 && pileIndex < stackViews.Length ? stackViews[pileIndex] : null;
//                 if (stack == null)
//                 {
//                     onReady?.Invoke();
//                     return;
//                 }

//                 if (creativeChainMode)
//                 {
//                     stack.RefreshChainBacks(state.StackRemainingAt(pileIndex), autoFillMoveDuration, () =>
//                     {
//                         cardViews[slotIndex].Setup(category, capacity, progressCardMode);
//                         StartCoroutine(cardViews[slotIndex].FlipReveal(cardFlyDuration));
//                         onReady?.Invoke();
//                     });
//                 }
//                 else
//                 {
//                     cardViews[slotIndex].Setup(category, capacity, progressCardMode);
//                     stack.SetCount(state.StackRemainingAt(pileIndex));
//                     StartCoroutine(cardViews[slotIndex].FlyIn(stack.SpawnWorldPosition, cardFlyDuration));
//                     onReady?.Invoke();
//                 }
//             }
//             else
//             {
//                 int mappedPileIndex = state.GetPileIndexForSlot(slotIndex);
//                 CardStackView stack = stackViews != null && mappedPileIndex >= 0 && mappedPileIndex < stackViews.Length ? stackViews[mappedPileIndex] : null;
//                 if (stack == null)
//                 {
//                     onReady?.Invoke();
//                     return;
//                 }

//                 if (creativeChainMode)
//                 {
//                     stack.RefreshChainBacks(state.StackRemainingAt(mappedPileIndex), autoFillMoveDuration, onReady);
//                 }
//                 else
//                 {
//                     stack.SetCount(state.StackRemainingAt(mappedPileIndex));
//                     onReady?.Invoke();
//                 }
//             }
//         }

//         private void CompactQueueVisuals()
//         {
//             for (int i = 0; i < queueStickers.Count; i++)
//             {
//                 StickerView sticker = queueStickers[i];
//                 if (sticker != null)
//                 {
//                     animator.AnimateMove(sticker.transform, queueView.GetSlotAnchor(i), autoFillMoveDuration * 0.6f, null);
//                 }
//             }
//         }

//         private void NotifyQueueChanged()
//         {
//             UpdateQueueWarning();
//             QueueChanged?.Invoke(state.QueueCount);
//         }

//         private void CacheLandSizes()
//         {
//             StickerLandTraySettings landSettings = runtimeProfile != null
//                 ? runtimeProfile.StickerFlight.landTrayRestick.Sanitized()
//                 : StickerLandTraySettings.Default;
//             queueLandSizeWorld = landSettings.queueLandSizeWorld;
//             cardLandSizeWorld = landSettings.cardLandSizeWorld;
//         }

//         private void UpdateQueueWarning()
//         {
//             if (queueView == null)
//             {
//                 return;
//             }

//             int remainingSlots = state.QueueSize - state.QueueCount;
//             queueView.SetWarningActive(remainingSlots <= 1);
//         }

//         private void CheckWin()
//         {
//             if (gameOver)
//             {
//                 return;
//             }

//             if (mergeQueueMode)
//             {
//                 if (board != null && board.RemainingStickers == 0)
//                 {
//                     gameOver = true;
//                     Debug.Log("[GameplayManager] LEVEL WON");
//                     LevelWon?.Invoke();
//                 }

//                 return;
//             }

//             if (state.AllCardsCleared)
//             {
//                 gameOver = true;
//                 Debug.Log("[GameplayManager] LEVEL WON");
//                 LevelWon?.Invoke();
//             }
//         }

//         private void Lose()
//         {
//             if (gameOver)
//             {
//                 return;
//             }

//             if (queueView != null)
//             {
//                 queueView.SetWarningActive(false);
//             }

//             gameOver = true;
//             Debug.Log("[GameplayManager] LEVEL LOST — queue full");
//             LevelLost?.Invoke();
//         }
//     }
// }
