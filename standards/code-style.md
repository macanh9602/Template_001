# Code Style — Mobile Runtime Element Architecture

> Đọc file này **trước mọi bước viết code Unity**. Không sửa khi làm game mới.
> Namespace root là per-project — lấy từ `Docs/project-context.md`, chưa có thì hỏi dev một câu.

## SRP một dòng

**Spawner orchestrates → Factory creates → Domain decides → RuntimeState indexes → Visual renders.**

---

## 1. Contracts (4 interface, giữ nhỏ)

```csharp
public interface ICreateParameters { }
public interface IRuntimeCreatable  { void OnCreated(ICreateParameters parameters); }
public interface IFactory<T>        { UniTask<T> Create(ICreateParameters p, CancellationToken ct); }
public interface IPendingCleanup    { bool IsPendingCleanup { get; } void CleanupForLevelUnload(); }
```

- Mỗi element type có **một sealed `XxxCreateParameters`** riêng: data + adapter + parent +
  runtime state + service/callback. Domain **không** tự tìm dependency bằng global search.
- Element cần spawn element khác → nhận `Func<TData, UniTask<TEntity>>` hoặc service interface qua
  parameters. Không hardcode factory lookup.
- Factory sealed, không gameplay rule: validate parameter type (`throw ArgumentException` nếu sai)
  → lấy prefab/pool → đặt name → ensure domain component → gọi `OnCreated`.
- `Instantiate` chỉ ở level-load / editor. Gameplay loop dùng pool.

---

## 2. Ba lớp component

- **Domain** — gameplay rule/state: occupancy, move plan, reserve target, xử lý event.
  Không dựng mesh/material, không đọc Renderer/Collider/Physics.
- **Visual** — CHỈ render/animate từ data: `Render(data, adapter)` · `SetCounter(n)` ·
  `PlayMoveAsync(ct)` · `StopAllTweens()`. Không query pathfinding/occupancy/gameplay decision.
- **Decorator** — state phụ (hidden/locked/frozen) gắn cùng entity: đổi interaction + presentation,
  **không** chiếm occupancy riêng. Rule của nó nằm trong runtime state, không nằm trong decorator.

---

## 2.5. Component ownership — Caller quyết WHAT, component quyết HOW

- Caller yêu cầu **ý định** qua public API nhỏ: `Show`, `Hide`, `Bind`, `PlayShift`, `SetState`...
- Component tự quản lý **HOW** thuộc ownership của nó: tween, child renderer, local cache, cancellation, cleanup, visual state trung gian.
- Caller **không micromanage** child object/tween/internal flag của component. Nếu caller phải set 4–5 field nội bộ theo đúng thứ tự thì API đang sai ownership.
- Self-contained component **không có nghĩa** được tự quyết gameplay/domain semantics. Domain vẫn quyết WHAT; Visual/Component chỉ thực hiện HOW.
- Khi ownership của một visual chuyển từ component A → B, B phải `Bind/Adopt` và **normalize toàn bộ state mà B sở hữu**; không tin state presentation do A để lại.

---

## 3. RuntimeState

Tạo mới theo level, truyền qua `CreateParameters` — **không singleton global**.
Chứa: index theo stable ID, occupancy map, obstacle map, reservation service, graph/adjacency,
pathfinder, hint service.

Subscribe event → **bắt buộc** unsubscribe trong `CleanupForLevelUnload` **và** `OnDestroy`.

Ngoại lệ hợp lệ duy nhất cho static: **config chỉ-đọc** (ColorProfile, EffectsProfile) truy cập qua
`ResourceAsset<T>` + static accessor. Cấm để bất kỳ state nào thay đổi trong lúc chơi vào đó.

---

## 4. Spawn lifecycle

Thứ tự bắt buộc ở `standards/system-design.md §5`. Tóm tắt:

```
cancel token cũ → cleanup level cũ → load + validate data → resolve config (profile ← override)
→ runtime state → root transform + layout → base board → movable element (+decorator ngay sau)
→ seed static obstacle TRƯỚC → producer/dynamic → await async → build graph → refresh hint
→ OnLevelSpawned
```

---

## 5. Số liệu — luật cứng

**Không hardcode số liệu.** Mọi giá trị tune được nằm ở một trong ba chỗ:

| Phạm vi | Chỗ đúng |
|---|---|
| riêng một instance trong scene | field trên prefab / component |
| dùng chung cả game | Profile ScriptableObject (`ResourceAsset<T>`) |
| riêng một level | level JSON (+ resolver nếu override profile) |

Kèm theo:

- **Không** `GameObject.CreatePrimitive(...)` — placeholder cũng là prefab thật trỏ từ `PrefabProfile`.
- **Không** `new Material(...)`, **không** `Shader.Find(...)` trong runtime path — material lấy từ
  `PrefabProfile`, khác biệt per-instance đi qua `MaterialPropertyBlock`.
- **Không** hằng số thời gian trong domain — đọc từ `TimingProfile` / `MotionProfile`.
- Component giữ **id của preset**, không giữ số. `[SerializeField] float duration` rải khắp Visual
  là anti-pattern.
- Đọc profile **mỗi lần bắt đầu tween/schedule**, không cache lúc bind → sửa số trong Inspector
  lúc đang Play có hiệu lực ngay ở chuỗi kế tiếp.

---

## 6. Pooling — `VTLTools.ObjectPool`

- Mọi thứ spawn/despawn lặp trong gameplay đều pool: element, roll/piece, effect, counter, floating
  text, decorator.
- Prewarm theo **số hiển thị đồng thời**, không theo tổng số của level.
- `CleanupForLevelUnload()` → reset state → `Recycle`, **không** `Destroy`.
- Pool sống qua level, chỉ clear khi đổi scene.

### Ba bẫy pool đã trả giá thật

1. `SetParent(parent)` với `worldPositionStays = true` **không reset local scale** → child nhận
   inverse parent scale, mesh lệch pivot khi parent có scale ≠ 1.
   → **Factory phải dùng `SetParent(parent, false)` và khôi phục `localScale` từ prefab sau mỗi spawn/reparent.**
2. Pooled lifecycle có **hai ranh giới bắt buộc**, không chọn một trong hai:
   - **Release:** cancel task/tween → invalidate operation owner/generation → clear refs/readiness → normalize state an toàn → recycle.
   - **Acquire/Bind:** rebind **đầy đủ** mọi presentation state từ data hiện tại (visibility, color, outline, scale, count...). Không dựa vào giả định release trước đã sạch.
3. Component nào có tween/async: completion/cancellation cũ chỉ được ghi final state nếu nó vẫn là **active owner**. `finally` mù quáng reset flag là race kinh điển khi motion B đã thay motion A.

---

## 7. Text — TextMeshPro qua prefab có script quản lý

**Bắt buộc.** Không `TextMesh` legacy. Không dựng text bằng code.

Một prefab TMP dùng chung cho world text (và một cho HUD text nếu khác), trỏ từ `PrefabProfile`,
pooled. Trên prefab có **script quản lý riêng** với API:

```csharp
public sealed class WorldTextView : MonoBehaviour, IPendingCleanup
{
    // Init nhận param — không đọc gì từ global
    public void Init(string text, Color color, Vector3 worldAnchor);

    public void Show();                     // hoặc UniTask ShowAsync(CancellationToken)
    public void Hide();
    public UniTask PlayFeedbackAsync(CancellationToken ct);   // pop / float up / fade

    public void CleanupForLevelUnload();    // kill tween, reset scale, recycle
}
```

- Font, size, alignment, material **nằm trên prefab** — artist chỉnh không cần dev.
- Tham số animation (`spawnOffset`, `startDelay`, `moveDuration`, `moveDistanceY`, `moveCurve`,
  `fadeCurve`, `lifeTime`) là field trên prefab hoặc đọc từ `MotionProfile`.
- Gọi `Init` lại khi instance đang chạy ⇒ **reset về đầu**, không spawn instance thứ hai
  (chống chồng 10 text khi spam tap).
- Text hướng camera: quay một lần lúc `Init` nếu camera cố định — không billboard mỗi `LateUpdate`.

---

## 8. Mobile performance (conflict với convenience → performance thắng)

- Không `FindObjectOfType` / `GameObject.Find` / scene search trong gameplay loop.
- Không `Instantiate` / `Destroy` trong gameplay.
- Không allocation lặp mỗi frame — reuse `List<T>`, buffer, `MaterialPropertyBlock`, cached shader
  property id (`static readonly int`).
- Không LINQ trong hot path.
- Visual: đổi vị trí thì update transform, không destroy/recreate.
- `MaterialPropertyBlock` phải ghi **đủ mọi property cùng lúc** trong một hàm `WriteBlock()` duy nhất.
  Gọi `propertyBlock.Clear()` rồi chỉ set màu sẽ **xoá mất** những property khác vừa ghi.
- Async animation nhận `CancellationToken` gắn lifetime object/level. Reserve target trước move,
  release trong `finally`.
- Mỗi async visual operation có **operation id / generation / owner token**. Callback/finally cũ chỉ được mutate state nếu token vẫn current.
- Không capture `count/index/destination` rồi dùng lại ở cuối tween nếu state có thể đổi giữa chừng; re-read state hiện tại hoặc validate stable sequence/id trước final write/handoff.
- Query gameplay phải O(1) từ counter/index, hoặc O(out-degree) từ adjacency — không quét toàn bộ
  collection mỗi lần tương tác.

Ngân sách cụ thể và **cách đo**: `standards/performance-budget.md`.

---

## 9. Editor code

- 3 folder: `Editor/<Tool>/C#`, `/UXML`, `/USS`. Mỗi partial class một trách nhiệm.
- Tách 3 loại state: **Document / ViewState / DerivedState**. Sửa Document **không được** ghi đè
  ViewState. Chi tiết + bảng Update Model: `knowledge/editor-ux/update-model.md`.
- `ApplyEdit(...)` ≠ `ReloadDocument()`. `ReloadDocument` chỉ gọi khi document swap.
- Selection lưu bằng **stable id**, không phải index.
- Text/number field `isDelayed = true`. **Cấm** rebuild inspector bên trong value-changed handler.
- Đăng ký callback **một lần** trong `CreateGUI()`. Hàm refresh chỉ đổi `itemsSource` + `Rebuild()`.
- Chuỗi hiển thị: label/button **English ASCII** (có test gate quét literal); tooltip và text
  GD-facing tiếng Việt gom trong `Editor/Localization/` (ngoài vùng quét). Xem `§10`.

---

## 10. Chuỗi hiển thị & localization

Default của pack (đổi thì ghi vào `AGENTS.md §10`):

| Loại | Ngôn ngữ | Ở đâu |
|---|---|---|
| Label, button, tên section trong editor | English ASCII | inline, bị test gate quét |
| Tooltip | tiếng Việt | `Editor/Localization/<Prefix>EditorText.cs` |
| Text GD-facing diễn giải kết quả | tiếng Việt | cùng file trên |
| Technical term | giữ English | mọi nơi |

- Test gate quét mọi string literal trong thư mục editor, fail nếu có ký tự ngoài ASCII (trừ
  allowlist ký hiệu trạng thái). File localization nằm **ngoài** thư mục quét — đây là chủ đích,
  phải ghi rõ trong comment của chính file test, không phải mẹo lách.
- Đổi phạm vi localization = **deviation công khai**, ghi decision entry. Không âm thầm.
- Tooltip nói **GD nên làm gì** với con số, không định nghĩa lại tên field.
  - Xấu: `"Peak queue: số roll tối đa trong hàng đợi"`
  - Tốt: `"Hàng đợi chạm trần 2/5. Chạm trần sớm là người chơi bị dồn ép giữa màn. Muốn nhẹ hơn: giảm Spread ở phase cuối."`
- Tên hiển thị ≠ khoá tra cứu. Nếu tên đang được dùng làm key (`Find("Typical")`), **không** đổi
  `Name`; thêm hàm `DisplayName(...)` riêng.

---

## 11. Self-review trước khi báo xong

```
[ ] Domain không reference Visual; xoá Visual thì Domain vẫn chạy hết ván
[ ] RuntimeState per-level, không static (trừ config chỉ-đọc)
[ ] Không FindObjectOfType / Find / scene search trong gameplay loop
[ ] Không Instantiate/Destroy trong gameplay; mọi thứ lặp đều pooled
[ ] Không CreatePrimitive / new Material / Shader.Find trong runtime path
[ ] Không hằng số thời gian trong domain; số đọc từ Profile
[ ] Không hardcode số liệu tune được; đúng chỗ theo bảng §5
[ ] MaterialPropertyBlock ghi đủ property trong một hàm
[ ] Async nhận CancellationToken; tween kill khi cleanup
[ ] Event có unsubscribe ở CleanupForLevelUnload + OnDestroy
[ ] Namespace mirror folder
[ ] Text đi qua prefab TMP có script quản lý
[ ] Query hot path O(1) hoặc O(out-degree)
```
