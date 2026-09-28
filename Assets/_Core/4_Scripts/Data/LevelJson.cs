// using System;
// using System.Collections.Generic;
// using UnityEngine;

// namespace CH013.Data
// {
//     public enum LevelMode
//     {
//         Default,
//         CreativeMergeQueue,
//         CreativeMultiPile,
//         CreativeMultiSlotsCard
//     }

//     /// <summary>
//     /// Level DTO — source of truth in Assets/_Core/Resources/Levels/*.json.
//     /// Pure data, no asset references: stickers are referenced by catalog id,
//     /// the model by its Resources path. One-way convert to runtime at load time.
//     /// </summary>
//     [Serializable]
//     public class LevelJson
//     {
//         public const int StandardActiveSlotCount = 2;
//         public const int StandardQueueSize = 5;
//         public const int MergeQueueSize = 6;

//         public string levelId = "level_01";
//         public string modelPath = string.Empty;
//         public Vector3 modelRotationEuler = Vector3.zero;
//         public string levelMode = nameof(LevelMode.Default);
//         /// <summary>Legacy flag kept for old JSON/editor migration. Prefer Mode helpers.</summary>
//         public bool isCreative;
//         /// <summary>Legacy flag kept for old JSON/editor migration. Prefer Mode helpers.</summary>
//         public bool isMergeQueue;
//         /// <summary>Each pile owns the active card slots that draw from it.</summary>
//         public List<CardStackJson> cardStacks = new List<CardStackJson>();
//         /// <summary>Legacy global slot count. Read for migration only; never written.</summary>
//         public int cardSlotCount;
//         public int queueSize = StandardQueueSize;
//         /// <summary>Legacy flat stack. Read/migrated for existing JSON; do not author against it.</summary>
//         public List<CardEntryJson> cardStack = new List<CardEntryJson>();
//         public List<PieceJson> pieces = new List<PieceJson>();

//         public static LevelJson FromJson(string json)
//         {
//             LevelJson result = JsonUtility.FromJson<LevelJson>(json);
//             result?.MigrateCardData();
//             return result;
//         }

//         public LevelMode Mode => ResolveMode();

//         public bool IsDefaultMode => Mode == LevelMode.Default;
//         public bool IsCreativeLevel => Mode != LevelMode.Default;
//         public bool IsCreativeMergeQueueMode => Mode == LevelMode.CreativeMergeQueue;
//         public bool IsCreativeMultiPileMode => Mode == LevelMode.CreativeMultiPile;
//         public bool IsCreativeMultiSlotsCardMode => Mode == LevelMode.CreativeMultiSlotsCard;
//         public bool UsesCreativeCardView => IsCreativeMultiPileMode || IsCreativeMultiSlotsCardMode;

//         public void SetMode(LevelMode mode)
//         {
//             levelMode = mode.ToString();
//             SyncLegacyFlags(mode);
//         }

//         public string ToJson()
//         {
//             LevelMode mode = Mode;
//             SyncLegacyFlags(mode);
//             var save = new LevelJsonSaveData
//             {
//                 levelId = levelId,
//                 modelPath = modelPath,
//                 modelRotationEuler = modelRotationEuler,
//                 levelMode = mode.ToString(),
//                 isCreative = isCreative,
//                 isMergeQueue = isMergeQueue,
//                 cardStacks = cardStacks,
//                 queueSize = queueSize,
//                 pieces = pieces
//             };
//             return JsonUtility.ToJson(save, true);
//         }

//         public int TotalPlacements()
//         {
//             int count = 0;
//             for (int i = 0; i < pieces.Count; i++)
//             {
//                 count += pieces[i].placements.Count;
//             }

//             return count;
//         }

//         public int CardStackCount => IsCreativeMergeQueueMode || cardStacks == null ? 0 : cardStacks.Count;

//         public int ActiveCardSlotCount
//         {
//             get
//             {
//                 int count = 0;
//                 if (IsCreativeMergeQueueMode)
//                 {
//                     return count;
//                 }

//                 if (cardStacks == null)
//                 {
//                     return count;
//                 }

//                 for (int i = 0; i < cardStacks.Count; i++)
//                 {
//                     count += cardStacks[i] != null ? Mathf.Max(0, cardStacks[i].activeSlotCount) : 0;
//                 }

//                 return count;
//             }
//         }

//         public int TotalCardCount()
//         {
//             int count = 0;
//             if (IsCreativeMergeQueueMode)
//             {
//                 return count;
//             }

//             if (cardStacks == null)
//             {
//                 return count;
//             }

//             for (int i = 0; i < cardStacks.Count; i++)
//             {
//                 count += cardStacks[i] != null && cardStacks[i].cards != null ? cardStacks[i].cards.Count : 0;
//             }

//             return count;
//         }

//         /// <summary>Migrates the former flat list into pile zero without destroying legacy data.</summary>
//         public void MigrateCardData()
//         {
//             if (cardStacks == null)
//             {
//                 cardStacks = new List<CardStackJson>();
//             }

//             LevelMode mode = Mode;
//             SyncLegacyFlags(mode);

//             if (mode == LevelMode.CreativeMergeQueue)
//             {
//                 return;
//             }

//             if (cardStacks.Count == 0 && cardStack != null && cardStack.Count > 0)
//             {
//                 int migratedSlotCount = mode != LevelMode.Default ? Mathf.Max(1, cardSlotCount) : StandardActiveSlotCount;
//                 cardStacks.Add(new CardStackJson
//                 {
//                     activeSlotCount = migratedSlotCount,
//                     cards = new List<CardEntryJson>(cardStack)
//                 });
//             }

//             for (int i = 0; i < cardStacks.Count; i++)
//             {
//                 if (cardStacks[i] == null)
//                 {
//                     cardStacks[i] = new CardStackJson();
//                 }

//                 cardStacks[i].EnsureCards();
//                 if (cardStacks[i].activeSlotCount <= 0)
//                 {
//                     cardStacks[i].activeSlotCount = mode != LevelMode.Default ? 1 : StandardActiveSlotCount;
//                 }
//             }
//         }

//         /// <summary>Runtime-only rule enforcement. Editor validation reports violations before save.</summary>
//         public void NormalizeCardDataForRuntime()
//         {
//             MigrateCardData();
//             LevelMode mode = Mode;
//             SyncLegacyFlags(mode);

//             if (mode == LevelMode.CreativeMergeQueue)
//             {
//                 if (cardStacks == null)
//                 {
//                     cardStacks = new List<CardStackJson>();
//                 }

//                 cardStacks.Clear();
//                 queueSize = MergeQueueSize;
//                 return;
//             }

//             if (mode == LevelMode.Default)
//             {
//                 var merged = new List<CardEntryJson>();
//                 for (int i = 0; i < cardStacks.Count; i++)
//                 {
//                     if (cardStacks[i].cards != null)
//                     {
//                         merged.AddRange(cardStacks[i].cards);
//                     }
//                 }

//                 cardStacks.Clear();
//                 cardStacks.Add(new CardStackJson
//                 {
//                     activeSlotCount = StandardActiveSlotCount,
//                     cards = merged
//                 });
//                 for (int i = 0; i < merged.Count; i++)
//                 {
//                     merged[i].stickerSlots = 3;
//                 }

//                 queueSize = StandardQueueSize;
//                 return;
//             }

//             if (cardStacks.Count == 0)
//             {
//                 cardStacks.Add(new CardStackJson());
//             }

//             if (cardStacks.Count > 3)
//             {
//                 cardStacks.RemoveRange(3, cardStacks.Count - 3);
//             }

//             if (mode == LevelMode.CreativeMultiSlotsCard && cardStacks.Count > 1)
//             {
//                 var merged = new List<CardEntryJson>();
//                 int activeSlots = 0;
//                 for (int i = 0; i < cardStacks.Count; i++)
//                 {
//                     activeSlots += Mathf.Max(0, cardStacks[i].activeSlotCount);
//                     if (cardStacks[i].cards != null)
//                     {
//                         merged.AddRange(cardStacks[i].cards);
//                     }
//                 }

//                 cardStacks.Clear();
//                 cardStacks.Add(new CardStackJson
//                 {
//                     activeSlotCount = Mathf.Max(1, activeSlots),
//                     cards = merged
//                 });
//             }

//             for (int i = 0; i < cardStacks.Count; i++)
//             {
//                 cardStacks[i].EnsureCards();
//                 cardStacks[i].activeSlotCount = mode == LevelMode.CreativeMultiPile
//                     ? 1
//                     : Mathf.Max(1, cardStacks[i].activeSlotCount);
//                 for (int c = 0; c < cardStacks[i].cards.Count; c++)
//                 {
//                     cardStacks[i].cards[c].stickerSlots = mode == LevelMode.CreativeMultiPile
//                         ? 3
//                         : Mathf.Clamp(cardStacks[i].cards[c].stickerSlots, 1, 7);
//                 }
//             }

//             queueSize = StandardQueueSize;
//         }

//         public static bool TryParseMode(string value, out LevelMode mode)
//         {
//             return Enum.TryParse(value, false, out mode) && Enum.IsDefined(typeof(LevelMode), mode);
//         }

//         private LevelMode ResolveMode()
//         {
//             if (isMergeQueue)
//             {
//                 return LevelMode.CreativeMergeQueue;
//             }

//             if (TryParseMode(levelMode, out LevelMode parsed) && parsed != LevelMode.Default)
//             {
//                 return parsed;
//             }

//             if (!isCreative)
//             {
//                 return LevelMode.Default;
//             }

//             return InferCreativeModeFromData();
//         }

//         private LevelMode InferCreativeModeFromData()
//         {
//             int pileCount = cardStacks != null ? cardStacks.Count : 0;
//             if (pileCount > 1)
//             {
//                 return LevelMode.CreativeMultiPile;
//             }

//             if (cardStacks != null && cardStacks.Count == 1)
//             {
//                 CardStackJson pile = cardStacks[0];
//                 if (pile != null)
//                 {
//                     if (pile.activeSlotCount != StandardActiveSlotCount)
//                     {
//                         return LevelMode.CreativeMultiSlotsCard;
//                     }

//                     pile.EnsureCards();
//                     for (int i = 0; i < pile.cards.Count; i++)
//                     {
//                         if (pile.cards[i].stickerSlots != 3)
//                         {
//                             return LevelMode.CreativeMultiSlotsCard;
//                         }
//                     }
//                 }
//             }

//             return LevelMode.CreativeMultiSlotsCard;
//         }

//         private void SyncLegacyFlags(LevelMode mode)
//         {
//             isMergeQueue = mode == LevelMode.CreativeMergeQueue;
//             isCreative = mode == LevelMode.CreativeMultiPile || mode == LevelMode.CreativeMultiSlotsCard;
//         }
//     }

//     [Serializable]
//     public class CardStackJson
//     {
//         [Min(1)] public int activeSlotCount;
//         public List<CardEntryJson> cards = new List<CardEntryJson>();

//         public void EnsureCards()
//         {
//             if (cards == null)
//             {
//                 cards = new List<CardEntryJson>();
//             }
//         }
//     }

//     [Serializable]
//     internal class LevelJsonSaveData
//     {
//         public string levelId;
//         public string modelPath;
//         public Vector3 modelRotationEuler;
//         public string levelMode;
//         public bool isCreative;
//         public bool isMergeQueue;
//         public List<CardStackJson> cardStacks;
//         public int queueSize;
//         public List<PieceJson> pieces;
//     }

//     [Serializable]
//     public class CardEntryJson
//     {
//         public string category = "Food";
//         public int stickerSlots = 3;

//         public bool TryGetCategory(out CategoryType type)
//         {
//             return Enum.TryParse(category, false, out type);
//         }
//     }

//     [Serializable]
//     public class PieceJson
//     {
//         public string meshName = string.Empty;
//         /// <summary>Model layer this piece belongs to (outer shell = 0, inner shells = 1+).</summary>
//         public int layer;
//         public List<PlacementJson> placements = new List<PlacementJson>();
//     }

//     [Serializable]
//     public class PlacementJson
//     {
//         public string stickerId = string.Empty;
//         public Vector2 uv = new Vector2(0.5f, 0.5f);
//         public float rotationDeg;
//         public float sizeWorld = 1f;
//         public bool flipX;
//         public bool flipY;
//         public int layer;
//     }
// }
