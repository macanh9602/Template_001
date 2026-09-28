// using System.Collections.Generic;
// using CH013.StickerSystem;
// using Haptic.Visual;
// using UnityEngine;
// using UnityEngine.EventSystems;
// using UnityEngine.UI;

// #if ENABLE_INPUT_SYSTEM
// using UnityEngine.InputSystem;
// using UnityEngine.InputSystem.Controls;
// #endif

// namespace CH013.Gameplay
// {
//     [DisallowMultipleComponent]
//     public sealed class GameplayInputController : MonoBehaviour
//     {
//         [Header("Scene References")]
//         [SerializeField] private Slider zoomSlider;

//         [Header("Direct Manipulation")]
//         [SerializeField, Min(1f)] private float moveThresholdPixels = 24f;
//         [SerializeField, Min(0.02f)] private float tapTimeThreshold = 0.25f;
//         [SerializeField, Min(0f)] private float orbitSpeed = 0.18f;
//         [SerializeField, Min(0f)] private float dragInertiaDamping = 5.5f;
//         [SerializeField, Min(0f)] private float dragInertiaStopSpeed = 8f;

//         private Camera inputCamera;
//         private ModelBoard board;
//         private Transform orbitRoot;
//         private GameplayManager manager;
//         private LevelManager levelManager;
//         private Material xrayMaterial;
//         private InteractionSettings settings;
//         private PieceView holdCandidate;
//         private PieceView xrayPiece;
//         private Vector2 pointerDownPosition;
//         private Vector2 lastPointerPosition;
//         private Vector3 fitScale = Vector3.one;
//         private float pointerDownTime;
//         private float lastInputTime;
//         private float previousPinchDistance;
//         private float currentZoom = 1f;
//         private float targetZoom = 1f;
//         private float zoomVelocity;
//         private Vector2 dragInertiaVelocity;
//         private bool pointerDown;
//         private bool pointerBlockedByUi;
//         private bool dragging;
//         private bool holdConsumed;
//         private bool pinchActive;
//         private bool dragInertiaActive;
//         private bool sliderBound;
//         private bool missingSliderWarningLogged;
//         private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>(8);
//         private PointerEventData uiPointerData;

//         public void Initialize(
//             Camera cam,
//             ModelBoard modelBoard,
//             GameplayManager gameplayManager,
//             LevelManager owner,
//             InteractionSettings interactionSettings,
//             Material transparentXrayMaterial)
//         {
//             UnbindLevel();
//             inputCamera = cam;
//             board = modelBoard;
//             orbitRoot = modelBoard != null ? modelBoard.transform : null;
//             manager = gameplayManager;
//             levelManager = owner;
//             settings = interactionSettings.Sanitized();
//             xrayMaterial = transparentXrayMaterial;
//             fitScale = orbitRoot != null ? orbitRoot.localScale : Vector3.one;
//             currentZoom = 1f;
//             targetZoom = 1f;
//             zoomVelocity = 0f;
//             CancelDragInertia();
//             lastInputTime = Time.unscaledTime;
//             if (orbitRoot != null)
//             {
//                 orbitRoot.localScale = fitScale;
//             }

//             BindSlider();
//             SyncSlider();
//         }

//         public void UnbindLevel()
//         {
//             CancelPointer(true);
//             inputCamera = null;
//             board = null;
//             orbitRoot = null;
//             manager = null;
//             levelManager = null;
//             xrayMaterial = null;
//             pinchActive = false;
//             previousPinchDistance = 0f;
//             CancelDragInertia();
//         }

//         private void OnDestroy()
//         {
//             UnbindLevel();
//             if (sliderBound && zoomSlider != null)
//             {
//                 zoomSlider.onValueChanged.RemoveListener(HandleSliderChanged);
//             }
//         }

//         private void Update()
//         {
//             if (inputCamera == null || manager == null || orbitRoot == null)
//             {
//                 return;
//             }

//             UpdateZoom();

//             if (TryReadPinch(out Vector2 first, out Vector2 second))
//             {
//                 CancelDragInertia();
//                 HandlePinch(first, second);
//                 return;
//             }

//             if (pinchActive)
//             {
//                 pinchActive = false;
//                 previousPinchDistance = 0f;
//             }

//             if (TryReadPointer(out Vector2 position, out bool pressed, out bool released, out bool held))
//             {
//                 if (HandlePointer(position, pressed, released, held))
//                 {
//                     RegisterInput();
//                 }
//                 else
//                 {
//                     UpdateIdleRotation();
//                 }

//                 return;
//             }

//             CancelXRay(false);
//             if (UpdateDragInertia())
//             {
//                 RegisterInput();
//                 return;
//             }

//             UpdateIdleRotation();
//         }

//         private bool HandlePointer(Vector2 position, bool pressed, bool released, bool held)
//         {
//             if (pressed)
//             {
//                 return BeginPointer(position);
//             }

//             if (!pointerDown)
//             {
//                 return false;
//             }

//             if (pointerBlockedByUi)
//             {
//                 if (released)
//                 {
//                     CancelPointer(false);
//                 }
//                 return false;
//             }

//             bool consumedManualCameraInput = xrayPiece != null;
//             Vector2 totalDelta = position - pointerDownPosition;
//             if (held && totalDelta.sqrMagnitude > moveThresholdPixels * moveThresholdPixels)
//             {
//                 dragging = true;
//                 holdCandidate = null;
//             }

//             if (dragging && held)
//             {
//                 Vector2 delta = position - lastPointerPosition;
//                 RotateOrbit(delta);
//                 float deltaTime = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
//                 dragInertiaVelocity = delta / deltaTime;
//                 consumedManualCameraInput = true;
//             }

//             if (released)
//             {
//                 bool wasDragging = dragging;
//                 bool movedWithinTapThreshold = totalDelta.sqrMagnitude <= moveThresholdPixels * moveThresholdPixels;
//                 bool validTap = !dragging && !holdConsumed && movedWithinTapThreshold && Time.unscaledTime - pointerDownTime <= tapTimeThreshold;

//                 CancelXRay(false);
//                 pointerDown = false;
//                 dragging = false;
//                 holdCandidate = null;
//                 if (validTap)
//                 {
//                     manager.HandleTap(inputCamera.ScreenPointToRay(position));
//                 }
//                 else if (wasDragging)
//                 {
//                     StartDragInertia();
//                 }

//                 consumedManualCameraInput |= wasDragging || holdConsumed;
//             }

//             lastPointerPosition = position;
//             return consumedManualCameraInput;
//         }

//         private bool BeginPointer(Vector2 position)
//         {
//             CancelXRay(false);
//             CancelDragInertia();
//             pointerDown = true;
//             dragging = false;
//             holdConsumed = false;
//             pointerBlockedByUi = IsPointerOverUi(position);
//             pointerDownPosition = position;
//             lastPointerPosition = position;
//             pointerDownTime = Time.unscaledTime;
//             holdCandidate = null;
//             bool xrayStarted = false;
//             if (!pointerBlockedByUi && board != null)
//             {
//                 Ray ray = inputCamera.ScreenPointToRay(position);
//                 if (!board.TryPickSticker(ray, out _, out _) &&
//                     board.TryPickPiece(ray, out holdCandidate) &&
//                     holdCandidate != null &&
//                     !holdCandidate.IsDropped)
//                 {
//                     holdConsumed = true;
//                     xrayStarted = holdCandidate.BeginXRay(xrayMaterial, settings.xrayAlpha, settings.xrayFadeDuration);
//                     if (xrayStarted)
//                     {
//                         xrayPiece = holdCandidate;
//                         HapticController.TriggerHaptic(HapticType.Light);
//                     }
//                 }
//             }

//             return xrayStarted;
//         }

//         private void HandlePinch(Vector2 first, Vector2 second)
//         {
//             RegisterInput();
//             float distance = Vector2.Distance(first, second);
//             if (!pinchActive)
//             {
//                 pinchActive = true;
//                 previousPinchDistance = distance;
//                 CancelPointer(false, true);
//                 return;
//             }

//             if (previousPinchDistance > 0.01f && distance > 0.01f)
//             {
//                 targetZoom = Mathf.Clamp(targetZoom * (distance / previousPinchDistance), settings.zoomMinScale, settings.zoomMaxScale);
//                 SyncSlider();
//             }

//             previousPinchDistance = distance;
//         }

//         private void UpdateZoom()
//         {
//             currentZoom = Mathf.SmoothDamp(
//                 currentZoom,
//                 targetZoom,
//                 ref zoomVelocity,
//                 settings.zoomSmoothing,
//                 Mathf.Infinity,
//                 Time.unscaledDeltaTime);
//             orbitRoot.localScale = fitScale * currentZoom;
//         }

//         private void RotateOrbit(Vector2 delta)
//         {
//             orbitRoot.Rotate(Vector3.up, -delta.x * orbitSpeed, Space.World);
//             orbitRoot.Rotate(inputCamera.transform.right, delta.y * orbitSpeed, Space.World);
//         }

//         private void StartDragInertia()
//         {
//             dragInertiaActive = dragInertiaVelocity.sqrMagnitude > dragInertiaStopSpeed * dragInertiaStopSpeed;
//         }

//         private bool UpdateDragInertia()
//         {
//             if (!dragInertiaActive)
//             {
//                 return false;
//             }

//             if (levelManager == null || levelManager.IsSpawning || manager.IsGameOver)
//             {
//                 CancelDragInertia();
//                 return false;
//             }

//             float deltaTime = Time.unscaledDeltaTime;
//             RotateOrbit(dragInertiaVelocity * deltaTime);
//             float dampT = Mathf.Clamp01(dragInertiaDamping * deltaTime);
//             dragInertiaVelocity = Vector2.Lerp(dragInertiaVelocity, Vector2.zero, dampT);
//             if (dragInertiaVelocity.sqrMagnitude <= dragInertiaStopSpeed * dragInertiaStopSpeed)
//             {
//                 CancelDragInertia();
//             }

//             return true;
//         }

//         private void CancelDragInertia()
//         {
//             dragInertiaActive = false;
//             dragInertiaVelocity = Vector2.zero;
//         }

//         private void UpdateIdleRotation()
//         {
//             if (levelManager == null || levelManager.IsSpawning || manager.IsGameOver)
//             {
//                 return;
//             }

//             float idleDuration = Time.unscaledTime - lastInputTime;
//             if (idleDuration < settings.idleRotateDelay)
//             {
//                 return;
//             }

//             float ease = Mathf.Clamp01((idleDuration - settings.idleRotateDelay) / settings.idleRotateEaseIn);
//             ease = ease * ease * (3f - 2f * ease);
//             orbitRoot.Rotate(Vector3.up, settings.idleRotateSpeed * ease * Time.unscaledDeltaTime, Space.World);
//         }

//         private void RegisterInput()
//         {
//             lastInputTime = Time.unscaledTime;
//         }

//         private void CancelPointer(bool immediateXray, bool keepXray = false)
//         {
//             if (!keepXray)
//             {
//                 CancelXRay(immediateXray);
//             }

//             pointerDown = false;
//             pointerBlockedByUi = false;
//             dragging = false;
//             holdConsumed = false;
//             holdCandidate = null;
//         }

//         private void CancelXRay(bool immediate)
//         {
//             if (xrayPiece != null)
//             {
//                 xrayPiece.EndXRay(settings.xrayFadeDuration, immediate);
//                 xrayPiece = null;
//             }
//         }

//         private void BindSlider()
//         {
//             if (sliderBound || zoomSlider == null)
//             {
//                 if (zoomSlider == null && !missingSliderWarningLogged)
//                 {
//                     missingSliderWarningLogged = true;
//                     Debug.LogWarning("[GameplayInput] Zoom Slider is not assigned. Pinch zoom remains available.", this);
//                 }
//                 return;
//             }

//             zoomSlider.minValue = 0f;
//             zoomSlider.maxValue = 1f;
//             zoomSlider.wholeNumbers = false;
//             zoomSlider.onValueChanged.AddListener(HandleSliderChanged);
//             sliderBound = true;
//         }

//         private void HandleSliderChanged(float normalizedValue)
//         {
//             CancelDragInertia();
//             targetZoom = Mathf.Lerp(settings.zoomMinScale, settings.zoomMaxScale, Mathf.Clamp01(normalizedValue));
//             RegisterInput();
//         }

//         private void SyncSlider()
//         {
//             if (zoomSlider == null)
//             {
//                 return;
//             }

//             float normalized = Mathf.InverseLerp(settings.zoomMinScale, settings.zoomMaxScale, targetZoom);
//             zoomSlider.SetValueWithoutNotify(normalized);
//         }

//         private bool IsPointerOverUi(Vector2 position)
//         {
//             if (EventSystem.current == null)
//             {
//                 return false;
//             }

//             if (uiPointerData == null)
//             {
//                 uiPointerData = new PointerEventData(EventSystem.current);
//             }

//             uiPointerData.Reset();
//             uiPointerData.position = position;
//             uiRaycastResults.Clear();
//             EventSystem.current.RaycastAll(uiPointerData, uiRaycastResults);

//             bool overUi = false;
//             for (int i = 0; i < uiRaycastResults.Count; i++)
//             {
//                 if (uiRaycastResults[i].module is GraphicRaycaster)
//                 {
//                     overUi = true;
//                     break;
//                 }
//             }

//             if (overUi)
//             {
//                 DebugLogUiBlockers();
//             }

//             return overUi;
//         }

//         // TODO [TapDebug]: temporary diagnostics — remove with the other TapDebug logs.
//         private void DebugLogUiBlockers()
//         {
//             for (int i = 0; i < uiRaycastResults.Count; i++)
//             {
//                 if (uiRaycastResults[i].module is GraphicRaycaster)
//                 {
//                     // Debug.Log($"[TapDebug] UI blocker[{i}]: '{uiRaycastResults[i].gameObject.name}' (parent: {(uiRaycastResults[i].gameObject.transform.parent != null ? uiRaycastResults[i].gameObject.transform.parent.name : "none")})", uiRaycastResults[i].gameObject);
//                 }
//             }
//         }

//         private bool TryReadPinch(out Vector2 first, out Vector2 second)
//         {
// #if ENABLE_INPUT_SYSTEM
//             if (Touchscreen.current != null)
//             {
//                 int found = 0;
//                 first = default;
//                 second = default;
//                 foreach (TouchControl touch in Touchscreen.current.touches)
//                 {
//                     if (!touch.press.isPressed)
//                     {
//                         continue;
//                     }

//                     if (found == 0)
//                     {
//                         first = touch.position.ReadValue();
//                     }
//                     else
//                     {
//                         second = touch.position.ReadValue();
//                         return true;
//                     }

//                     found++;
//                 }

//                 return false;
//             }
// #endif
//             if (Input.touchCount >= 2)
//             {
//                 first = Input.GetTouch(0).position;
//                 second = Input.GetTouch(1).position;
//                 return true;
//             }

//             first = default;
//             second = default;
//             return false;
//         }

//         private bool TryReadPointer(out Vector2 position, out bool pressed, out bool released, out bool held)
//         {
// #if ENABLE_INPUT_SYSTEM
//             if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
//             {
//                 position = Touchscreen.current.primaryTouch.position.ReadValue();
//                 pressed = Touchscreen.current.primaryTouch.press.wasPressedThisFrame;
//                 released = Touchscreen.current.primaryTouch.press.wasReleasedThisFrame;
//                 held = true;
//                 return true;
//             }

//             if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.wasReleasedThisFrame)
//             {
//                 position = Touchscreen.current.primaryTouch.position.ReadValue();
//                 pressed = false;
//                 released = true;
//                 held = false;
//                 return true;
//             }

//             if (Mouse.current != null)
//             {
//                 position = Mouse.current.position.ReadValue();
//                 pressed = Mouse.current.leftButton.wasPressedThisFrame;
//                 released = Mouse.current.leftButton.wasReleasedThisFrame;
//                 held = Mouse.current.leftButton.isPressed;
//                 return pressed || released || held;
//             }
// #endif
//             position = Input.mousePosition;
//             pressed = Input.GetMouseButtonDown(0);
//             released = Input.GetMouseButtonUp(0);
//             held = Input.GetMouseButton(0);
//             return pressed || released || held;
//         }
//     }
// }
