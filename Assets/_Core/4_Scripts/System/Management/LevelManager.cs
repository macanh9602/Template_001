// using System;
// using System.IO;
// using System.Threading;
// using CH013.Commons;
// using CH013.Creation;
// using CH013.Data;
// using Cysharp.Threading.Tasks;
// using UnityEngine;
// using VTLTools;
// using Sirenix.OdinInspector;
// using TMPro;
// using UnityEngine.UI;

// namespace CH013.Gameplay
// {
//     /// <summary>Owns the complete lifecycle of the currently playable level.</summary>
//     public sealed class LevelManager : Singleton<LevelManager>
//     {
//         public const string PlaytestPrefsKey = "";
//         private const int ThemeCycleCount = 2;
//         private connst int LevelMax = 10;

//         [Header("Profile")]
//         [SerializeField] private GameplayRuntimeProfile runtimeProfile;
//         [SerializeField] private StickerCatalog catalog;

//         [Header("Scene References")]
//         [SerializeField] private GameplayInputController gameplayInput;

//         [SerializeField] private TextMeshProUGUI levelText;

//         public event Action OnWin;
//         public event Action OnLose;
//         public event Action OnLevelSpawned;
//         public event Action OnLevelWillUnload;

//         public bool IsWin { get; private set; }
//         public bool IsLose { get; private set; }
//         public bool IsSpawning { get; private set; }
//         public int CurrentLevel => StaticVariables.CurrentLevel;
//         public GameTheme ActiveTheme => ThemeForLevel(CurrentLevel);

//         // factory

//         private CancellationTokenSource spawnCts;
//         private GameObject levelRoot;
//         private GameplayManager gameplayManager;
//         private Camera gameplayCamera;

//         protected override void Awake()
//         {
//             base.Awake();
//         }

//         private async void Start()
//         {
//             if (Instance == this)
//             {
//                 UpdateLevelText();
//                 await SpawnLevelAsync();
//             }
//         }

// #if ODIN_INSPECTOR
//         [Button(ButtonSizes.Medium), PropertyOrder(-10)]
// #endif
//         public void NextLevel()
//         {
//             int count = CountAvailableLevels();
//             if (count <= 0) return;
//             if (CurrentLevel == LevelMax)
//             {
//                 StaticVariables.CurrentLevel = 0;
//             }
//             else
//             {
//                 StaticVariables.CurrentLevel = (CurrentLevel + 1) % count;
//             }
//             UpdateLevelText();
//             RequestSpawn();
//         }

// #if ODIN_INSPECTOR
//         [Button(ButtonSizes.Medium), PropertyOrder(-10)]
// #endif
//         public void PrevLevel()
//         {
//             StaticVariables.CurrentLevel = Mathf.Max(0, CurrentLevel - 1);
//             UpdateLevelText();
//             RequestSpawn();
//         }

// #if ODIN_INSPECTOR
//         [Button(ButtonSizes.Medium), PropertyOrder(-10)]
// #endif
//         public void ReloadLevel()
//         {
//             UpdateLevelText();
//             RequestSpawn();
//         }

//         public async UniTask SpawnLevelAsync()
//         {
//             // CancellationTokenSource previous = spawnCts;
//             // previous?.Cancel();

//             // var localCts = new CancellationTokenSource();
//             // spawnCts = localCts;
//             // using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(localCts.Token, destroyCancellationToken))
//             // {
//             //     CancellationToken token = linkedCts.Token;
//             //     IsSpawning = true;
//             //     try
//             //     {
//             //         if (!TryPrepareLevel(out LevelJson level, out GameObject modelPrefab)) return;
//             //         if (catalog == null)
//             //         {
//             //             Debug.LogError("[LevelManager] StickerCatalog is not assigned.");
//             //             return;
//             //         }

//             //         token.ThrowIfCancellationRequested();
//             //         OnLevelWillUnload?.Invoke();
//             //         DespawnCurrentLevel();

//             //         GameplayRuntimeProfile profile = ResolveProfile();
//             //         ApplyLevelTheme(profile);
//             //         GameplayLayoutSettings layout = profile != null ? profile.Layout : GameplayLayoutSettings.Default;
//             //         layout.cardsPosition = layout.GetCardsPosition(level.Mode);
//             //         if (level.UsesCreativeCardView)
//             //         {
//             //             layout.cardSpacing = layout.creativeColumnSpacing;
//             //             layout.stackSpacing = layout.creativeColumnSpacing;
//             //             if (level.IsCreativeMultiPileMode)
//             //             {
//             //                 layout.stackPosition = layout.cardsPosition;
//             //             }
//             //         }
//             //         Camera camera = ResolveCamera();
//             //         EnsureMiniGameCamera(camera);
//             //         EnsureDirectionalLight();
//             //         Material modelMaterial = profile != null ? profile.ModelMaterial : null;
//             //         ApplyThemeBackground(profile);

//             //         levelRoot = new GameObject("LevelRoot");
//             //         levelRoot.transform.SetParent(transform, false);

//             //         ModelBoard board = await modelBoardFactory.Create(new ModelBoardCreateParameters(modelPrefab, modelMaterial, level, catalog, profile.StickerMaterial, profile, levelRoot.transform, camera, layout), token);
//             //         await UniTask.Yield(token);

//             //         int slotCount = level.ActiveCardSlotCount;
//             //         var cards = new CardView[slotCount];
//             //         var cardsRoot = new GameObject("Cards").transform;
//             //         cardsRoot.SetParent(levelRoot.transform, false);
//             //         for (int i = 0; i < cards.Length; i++)
//             //         {
//             //             cards[i] = await cardViewFactory.Create(new CardViewCreateParameters(profile != null ? profile.CardViewPrefab : null, profile, cardsRoot, camera, layout, i, cards.Length), token);
//             //         }

//             //         int pileCount = level.CardStackCount;
//             //         var stacks = new CardStackView[pileCount];
//             //         var stacksRoot = new GameObject("CardStacks").transform;
//             //         stacksRoot.SetParent(levelRoot.transform, false);
//             //         for (int i = 0; i < stacks.Length; i++)
//             //         {
//             //             stacks[i] = await cardStackFactory.Create(new CardStackCreateParameters(
//             //                 profile != null ? profile.CardStackViewPrefab : null,
//             //                 profile,
//             //                 stacksRoot,
//             //                 camera,
//             //                 layout,
//             //                 i,
//             //                 stacks.Length,
//             //                 level.cardStacks[i].cards.Count,
//             //                 level.IsCreativeMultiPileMode), token);
//             //         }

//             //         QueueView queue = await queueFactory.Create(new QueueCreateParameters(profile != null ? profile.QueueViewPrefab : null, profile, levelRoot.transform, camera, layout, level.queueSize), token);

//             //         var animator = levelRoot.AddComponent<StickerFlightAnimator>();
//             //         animator.Initialize(camera);
//             //         animator.Configure(profile);
//             //         gameplayManager = levelRoot.AddComponent<GameplayManager>();
//             //         gameplayManager.Initialize(level, board, cards, stacks, queue, animator, profile);
//             //         gameplayManager.LevelWon += HandleLevelWon;
//             //         gameplayManager.LevelLost += HandleLevelLost;

//             //         if (gameplayInput != null)
//             //         {
//             //             gameplayInput.Initialize(camera, board, gameplayManager, this, profile.Interaction, profile.XrayMaterial);
//             //         }
//             //         else
//             //         {
//             //             Debug.LogError("[LevelManager] GameplayInputController scene reference is not assigned.", this);
//             //         }

//             //         token.ThrowIfCancellationRequested();
//             //         OnLevelSpawned?.Invoke();
//             //     }
//             //     catch (OperationCanceledException)
//             //     {
//             //         // A newer request owns the lifecycle now.
//             //     }
//             //     catch (Exception exception)
//             //     {
//             //         Debug.LogException(exception);
//             //         DespawnCurrentLevel();
//             //     }
//             //     finally
//             //     {
//             //         if (spawnCts == localCts)
//             //         {
//             //             spawnCts = null;
//             //             IsSpawning = false;
//             //         }
//             //         localCts.Dispose();
//             //     }
//             // }
//         }

//         protected override void OnDestroy()
//         {
//             spawnCts?.Cancel();
//             DespawnCurrentLevel();
//             base.OnDestroy();
//         }

//         private async void RequestSpawn()
//         {
//             await SpawnLevelAsync();
//         }

//         private bool TryPrepareLevel(out LevelJson level, out GameObject modelPrefab)
//         {
//             level = LoadLevel();
//             modelPrefab = null;
//             level?.NormalizeCardDataForRuntime();
//             if (level == null || !ValidateLevel(level)) return false;
//             modelPrefab = Resources.Load<GameObject>(level.modelPath);
//             if (modelPrefab != null) return true;
//             Debug.LogError($"[LevelManager] Could not load Resources model '{level.modelPath}'.");
//             return false;
//         }

//         private LevelJson LoadLevel()
//         {
// #if UNITY_EDITOR
//             string overridePath = UnityEditor.EditorPrefs.GetString(PlaytestPrefsKey, string.Empty);
//             if (!string.IsNullOrEmpty(overridePath) && File.Exists(overridePath))
//             {
//                 Debug.Log($"[LevelManager] Play Test override: {overridePath}");
//                 return LevelJson.FromJson(File.ReadAllText(overridePath));
//             }
// #endif
//             TextAsset levelAsset = Resources.Load<TextAsset>($"Levels/level_{CurrentLevel + 1:00}");
//             if (levelAsset == null)
//             {
//                 int count = CountAvailableLevels();
//                 if (count > 0 && CurrentLevel >= count)
//                 {
//                     StaticVariables.CurrentLevel = 0;
//                     UpdateLevelText();
//                     levelAsset = Resources.Load<TextAsset>("Levels/level_01");
//                 }
//             }
//             if (levelAsset == null)
//             {
//                 Debug.LogError($"[LevelManager] Missing Resources/Levels/level_{CurrentLevel + 1:00}.json.");
//                 return null;
//             }
//             return LevelJson.FromJson(levelAsset.text);
//         }

//         private static bool ValidateLevel(LevelJson level)
//         {
//             if (level.IsCreativeMergeQueueMode)
//             {
//                 if (level.queueSize != LevelJson.MergeQueueSize || level.pieces == null || level.pieces.Count == 0 || string.IsNullOrEmpty(level.modelPath))
//                 {
//                     Debug.LogError("[LevelManager] Invalid merge-queue level JSON: modelPath, pieces and exactly 6 queue slots are required.");
//                     return false;
//                 }

//                 return true;
//             }

//             if (level.CardStackCount <= 0 || level.ActiveCardSlotCount <= 0 || level.TotalCardCount() <= 0 || level.queueSize <= 0 || level.pieces == null || level.pieces.Count == 0 || string.IsNullOrEmpty(level.modelPath))
//             {
//                 Debug.LogError("[LevelManager] Invalid level JSON: queue, active card slots, card piles, pieces and modelPath are required.");
//                 return false;
//             }
//             return true;
//         }

//         private int CountAvailableLevels()
//         {
//             int count = 0;
//             while (Resources.Load<TextAsset>($"Levels/level_{count + 1:00}") != null) count++;

//             return count;
//         }

//         private void UpdateLevelText()
//         {
//             if (levelText == null) return;
//             levelText.SetText("Level {0}", CurrentLevel + 1);
//         }

//         private GameplayRuntimeProfile ResolveProfile()
//         {
//             return runtimeProfile != null ? runtimeProfile : GameplayRuntimeProfile.DefaultProfile;
//         }

//         private void ApplyLevelTheme(GameplayRuntimeProfile profile)
//         {
//             if (profile == null)
//             {
//                 return;
//             }

//             profile.SetRuntimeTheme(ThemeForLevel(CurrentLevel));
//         }

//         private static GameTheme ThemeForLevel(int levelIndex)
//         {
//             int themeIndex = Mathf.Clamp(levelIndex, 0, int.MaxValue) % ThemeCycleCount;
//             return (GameTheme)themeIndex;
//         }

//         private void ApplyThemeBackground(GameplayRuntimeProfile profile)
//         {
//             if (profile == null || profile.Theme == null || profile.Theme.Background == null)
//             {
//                 return;
//             }

//             Image image = ResolveBackgroundImage();
//             if (image != null)
//             {
//                 image.sprite = profile.Theme.Background;
//             }
//         }

//         private Image ResolveBackgroundImage()
//         {
//             if (backgroundImage != null)
//             {
//                 return backgroundImage;
//             }

//             Image[] images = FindObjectsByType<Image>(FindObjectsInactive.Include, FindObjectsSortMode.None);
//             for (int i = 0; i < images.Length; i++)
//             {
//                 if (images[i] != null && images[i].name == "BG")
//                 {
//                     backgroundImage = images[i];
//                     return backgroundImage;
//                 }
//             }

//             return null;
//         }

//         private Camera ResolveCamera()
//         {
//             if (gameplayCamera != null) return gameplayCamera;
//             gameplayCamera = Camera.main;
//             if (gameplayCamera != null) return gameplayCamera;
//             var cameraObject = new GameObject("Main Camera");
//             gameplayCamera = cameraObject.AddComponent<Camera>();
//             cameraObject.tag = "MainCamera";
//             cameraObject.transform.position = new Vector3(0f, 0.6f, -9f);
//             return gameplayCamera;
//         }

//         private static void EnsureMiniGameCamera(Camera camera)
//         {
//             if (camera == null) return;
//             var miniGameCamera = camera.GetComponent<global::AntiStress.MiniGame.MiniGameCamera.MiniGameCamera>();
//             if (miniGameCamera == null)
//             {
//                 miniGameCamera = camera.gameObject.AddComponent<global::AntiStress.MiniGame.MiniGameCamera.MiniGameCamera>();
//             }

//             miniGameCamera.Calculate();
//         }

//         private static void EnsureDirectionalLight()
//         {
//             if (UnityEngine.Object.FindFirstObjectByType<Light>() != null) return;
//             var lightObject = new GameObject("Directional Light");
//             Light light = lightObject.AddComponent<Light>();
//             light.type = LightType.Directional;
//             light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
//         }

//         private void HandleLevelWon()
//         {
//             if (IsWin || IsLose) return;
//             IsWin = true;
//             OnWin?.Invoke();
//             HUDSystem.Instance.Show<WinPanel>();
//         }

//         private void HandleLevelLost()
//         {
//             if (IsWin || IsLose) return;
//             IsLose = true;
//             OnLose?.Invoke();
//             HUDSystem.Instance.Show<LosePanel>();
//         }

//         private void DespawnCurrentLevel()
//         {
//             if (gameplayInput != null)
//             {
//                 gameplayInput.UnbindLevel();
//             }

//             if (gameplayManager != null)
//             {
//                 gameplayManager.LevelWon -= HandleLevelWon;
//                 gameplayManager.LevelLost -= HandleLevelLost;
//                 gameplayManager = null;
//             }
//             if (levelRoot == null) return;
//             MonoBehaviour[] behaviours = levelRoot.GetComponentsInChildren<MonoBehaviour>(true);
//             for (int i = 0; i < behaviours.Length; i++)
//             {
//                 if (behaviours[i] is IPendingCleanup cleanup) cleanup.CleanupForLevelUnload();
//             }
//             Destroy(levelRoot);
//             levelRoot = null;
//             IsWin = false;
//             IsLose = false;
//         }
//     }
// }
