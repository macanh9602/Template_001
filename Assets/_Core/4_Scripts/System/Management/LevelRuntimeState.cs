// using System.Collections.Generic;
// using CH013.Data;

// namespace CH013.Gameplay
// {
//     public enum TapDestination
//     {
//         Card,
//         Queue
//     }

//     public struct TapResolution
//     {
//         public TapDestination destination;
//         public int slotIndex;
//         public int positionIndex;
//         public bool cardCompleted;
//         public bool queueFull;
//     }

//     public struct AutoFillMove
//     {
//         public object token;
//         public int queueIndex;
//         public int slotIndex;
//         public int positionIndex;
//         public bool cardCompleted;
//     }

//     public class CardSlotState
//     {
//         public bool hasCard;
//         public CategoryType category;
//         public int capacity;
//         public int committed;

//         public bool HasSpace => hasCard && committed < capacity;
//     }

//     /// <summary>Pure gameplay state. Each active card slot draws from its owning pile.</summary>
//     public class LevelRuntimeState
//     {
//         private struct QueuedSticker
//         {
//             public CategoryType category;
//             public object token;
//         }

//         private readonly List<List<CardEntryJson>> pendingStacks = new List<List<CardEntryJson>>();
//         private readonly List<QueuedSticker> queue = new List<QueuedSticker>();
//         private int[] slotPileIndices = new int[0];

//         public CardSlotState[] Slots { get; private set; }
//         public int QueueSize { get; private set; }
//         public int QueueCount => queue.Count;
//         public int StackCount => pendingStacks.Count;
//         public int StackRemaining => StackRemainingAt(0);
//         public bool IsMergeQueueMode { get; private set; }

//         public bool AllCardsCleared
//         {
//             get
//             {
//                 for (int p = 0; p < pendingStacks.Count; p++)
//                 {
//                     if (pendingStacks[p].Count > 0)
//                     {
//                         return false;
//                     }
//                 }

//                 for (int i = 0; i < Slots.Length; i++)
//                 {
//                     if (Slots[i].hasCard)
//                     {
//                         return false;
//                     }
//                 }

//                 return true;
//             }
//         }

//         public void Initialize(LevelJson level)
//         {
//             pendingStacks.Clear();
//             queue.Clear();
//             IsMergeQueueMode = level != null && level.IsCreativeMergeQueueMode;
//             level.NormalizeCardDataForRuntime();
//             QueueSize = UnityEngine.Mathf.Max(1, level.queueSize);
//             if (IsMergeQueueMode)
//             {
//                 Slots = new CardSlotState[0];
//                 slotPileIndices = new int[0];
//                 return;
//             }

//             for (int p = 0; p < level.cardStacks.Count; p++)
//             {
//                 CardStackJson source = level.cardStacks[p];
//                 var pile = new List<CardEntryJson>();
//                 if (source != null && source.cards != null)
//                 {
//                     for (int c = 0; c < source.cards.Count; c++)
//                     {
//                         CardEntryJson entry = source.cards[c];
//                         entry.stickerSlots = level.UsesCreativeCardView ? UnityEngine.Mathf.Clamp(entry.stickerSlots, 1, 7) : 3;
//                         pile.Add(entry);
//                     }
//                 }

//                 pendingStacks.Add(pile);
//             }

//             Slots = new CardSlotState[level.ActiveCardSlotCount];
//             slotPileIndices = new int[Slots.Length];
//             int slotIndex = 0;
//             for (int pileIndex = 0; pileIndex < level.cardStacks.Count; pileIndex++)
//             {
//                 int activeSlotCount = level.cardStacks[pileIndex].activeSlotCount;
//                 for (int i = 0; i < activeSlotCount; i++)
//                 {
//                     slotPileIndices[slotIndex++] = pileIndex;
//                 }
//             }

//             for (int i = 0; i < Slots.Length; i++)
//             {
//                 Slots[i] = new CardSlotState();
//             }
//         }

//         public int StackRemainingAt(int pileIndex)
//         {
//             return pileIndex >= 0 && pileIndex < pendingStacks.Count ? pendingStacks[pileIndex].Count : 0;
//         }

//         public int GetPileIndexForSlot(int slotIndex)
//         {
//             return slotIndex >= 0 && slotIndex < slotPileIndices.Length ? slotPileIndices[slotIndex] : -1;
//         }

//         public bool TrySpawnCard(int slotIndex, out CategoryType category, out int capacity, out int pileIndex)
//         {
//             category = default;
//             capacity = 0;
//             pileIndex = GetPileIndexForSlot(slotIndex);
//             if (pileIndex < 0 || pileIndex >= pendingStacks.Count || pendingStacks[pileIndex].Count == 0)
//             {
//                 return false;
//             }

//             CardEntryJson entry = pendingStacks[pileIndex][0];
//             pendingStacks[pileIndex].RemoveAt(0);
//             if (!entry.TryGetCategory(out category))
//             {
//                 category = CategoryType.Food;
//             }

//             capacity = UnityEngine.Mathf.Clamp(entry.stickerSlots, 1, 7);
//             CardSlotState slot = Slots[slotIndex];
//             slot.hasCard = true;
//             slot.category = category;
//             slot.capacity = capacity;
//             slot.committed = 0;
//             return true;
//         }

//         public void ClearSlot(int slotIndex)
//         {
//             Slots[slotIndex].hasCard = false;
//             Slots[slotIndex].committed = 0;
//         }

//         public TapResolution ResolveTap(CategoryType category, object token)
//         {
//             var result = new TapResolution();
//             if (IsMergeQueueMode)
//             {
//                 result.destination = TapDestination.Queue;
//                 if (queue.Count >= QueueSize)
//                 {
//                     result.positionIndex = queue.Count;
//                     result.queueFull = true;
//                     return result;
//                 }

//                 queue.Add(new QueuedSticker { category = category, token = token });
//                 result.positionIndex = queue.Count - 1;
//                 return result;
//             }

//             for (int i = 0; i < Slots.Length; i++)
//             {
//                 CardSlotState slot = Slots[i];
//                 if (slot.HasSpace && slot.category == category)
//                 {
//                     result.destination = TapDestination.Card;
//                     result.slotIndex = i;
//                     result.positionIndex = slot.committed;
//                     slot.committed++;
//                     result.cardCompleted = slot.committed >= slot.capacity;
//                     return result;
//                 }
//             }

//             result.destination = TapDestination.Queue;
//             if (queue.Count >= QueueSize)
//             {
//                 result.positionIndex = queue.Count;
//                 result.queueFull = true;
//                 return result;
//             }

//             queue.Add(new QueuedSticker { category = category, token = token });
//             result.positionIndex = queue.Count - 1;
//             return result;
//         }

//         public List<AutoFillMove> AutoFill()
//         {
//             var moves = new List<AutoFillMove>();
//             int index = 0;
//             while (index < queue.Count)
//             {
//                 QueuedSticker queued = queue[index];
//                 bool moved = false;
//                 for (int s = 0; s < Slots.Length; s++)
//                 {
//                     CardSlotState slot = Slots[s];
//                     if (slot.HasSpace && slot.category == queued.category)
//                     {
//                         var move = new AutoFillMove
//                         {
//                             token = queued.token,
//                             queueIndex = index,
//                             slotIndex = s,
//                             positionIndex = slot.committed
//                         };
//                         slot.committed++;
//                         move.cardCompleted = slot.committed >= slot.capacity;
//                         moves.Add(move);
//                         queue.RemoveAt(index);
//                         moved = true;
//                         break;
//                     }
//                 }

//                 if (!moved)
//                 {
//                     index++;
//                 }
//             }

//             return moves;
//         }

//         public object GetQueueToken(int index)
//         {
//             return queue[index].token;
//         }

//         public CategoryType GetQueueCategory(int index)
//         {
//             return queue[index].category;
//         }

//         public bool TryFindMergeTriple(List<int> indices, out CategoryType category)
//         {
//             category = default;
//             indices.Clear();
//             for (int i = 0; i < queue.Count; i++)
//             {
//                 CategoryType candidate = queue[i].category;
//                 indices.Clear();
//                 for (int j = i; j < queue.Count; j++)
//                 {
//                     if (queue[j].category != candidate)
//                     {
//                         continue;
//                     }

//                     indices.Add(j);
//                     if (indices.Count == 3)
//                     {
//                         category = candidate;
//                         return true;
//                     }
//                 }
//             }

//             indices.Clear();
//             return false;
//         }

//         public bool HasMergeTriple()
//         {
//             for (int i = 0; i < queue.Count; i++)
//             {
//                 CategoryType candidate = queue[i].category;
//                 int count = 0;
//                 for (int j = i; j < queue.Count; j++)
//                 {
//                     if (queue[j].category == candidate)
//                     {
//                         count++;
//                         if (count >= 3)
//                         {
//                             return true;
//                         }
//                     }
//                 }
//             }

//             return false;
//         }

//         public void RemoveQueueIndices(List<int> indices)
//         {
//             for (int i = indices.Count - 1; i >= 0; i--)
//             {
//                 int index = indices[i];
//                 if (index >= 0 && index < queue.Count)
//                 {
//                     queue.RemoveAt(index);
//                 }
//             }
//         }
//     }
// }
