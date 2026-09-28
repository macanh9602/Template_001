# System Design Blueprint — Mobile Game (generic)

> **KHÔNG sửa file này khi làm game mới.** Đây là tầng hệ thống chung: mọi game có cùng bộ layer,
> cùng luồng khởi tạo, cùng chỗ để chỉnh số. Cái thay đổi giữa các game là *nội dung* của Domain và
> Visual, không phải *hình dạng* của hệ thống.
>
> Instance cụ thể cho một game nằm ở `Docs/runtime-architecture.md`.
> Muốn lệch khỏi blueprint → `AGENTS.md §10` + một entry `D-xxx` trong `Docs/decision-log.md`.

---

## 0. Câu một dòng

```
Bootstrap boots → Profile configures → Spawner orchestrates → Factory creates
→ Domain decides → RuntimeState indexes → Visual renders → Bridge reports → HUD shows
```

Đọc ngược lại cũng đúng: muốn biết *"ai quyết định cái này"*, đi từ phải sang trái tới layer đầu
tiên được phép quyết.

---

## 1. Bảng layer — trách nhiệm và cấm kỵ

| Layer | Sống ở đâu | Được làm | Cấm |
|---|---|---|---|
| **Bootstrap** | `System/Bootstrapper` | khởi tạo scene, load profile, warm pool, dựng level context | chứa gameplay rule |
| **Profile / Config** | `ScriptableObject` + `Resources` qua `ResourceAsset<T>` | giữ số tune được: timing, layout, prefab ref, palette, motion preset | giữ **state** runtime |
| **Level Data** | JSON trong `Resources/Levels/` | mô tả một level cụ thể | chứa logic |
| **Save / Progress** | `MDATools/Save` (`StaticVariables`, PlayerPrefs-backed) | progress, unlock, setting người chơi | làm source of truth cho gameplay trong 1 ván |
| **Spawner / Level loader** | `System/Creation` | load + validate data → tạo RuntimeState → gọi Factory đúng thứ tự → cleanup level cũ | tự quyết rule |
| **Factory** | `<Feature>/Creation` | tạo/reuse entity, validate `ICreateParameters`, gọi `OnCreated` | chứa gameplay rule |
| **Domain** | `<Feature>/Domain` | state + rule gameplay, phát event | chạm Mesh/Renderer/Collider/Physics, tự đi tìm dependency |
| **RuntimeState** | `System/Management` | index theo stable ID, occupancy/graph/reservation, query O(1) | là global singleton |
| **Scheduler** | `System/Management` | giữ nhịp domain bằng `Tick(dt)` + delayed action | biết gì về animation |
| **Visual** | `<Feature>/Visual` | render + animate từ data, tween, VFX, SFX cue | quyết định gameplay, đọc rule |
| **Bridge** | `Bridge/` | dịch giữa gameplay ↔ HUD / meta / analytics | 2 chiều tuỳ tiện — chỉ một hướng rõ |
| **HUD / UI** | project UI stack qua presenter/bridge | hiện state, nhận input UI | chứa gameplay rule |
| **Authoring / Tool** | external tool / `Editor/` / generator | authoring, validate, preview, export canonical data | runtime phụ thuộc authoring UI/tool |

Luật rút gọn, thuộc lòng:

- **Domain không phụ thuộc Visual.** Visual không quyết gameplay.
- **RuntimeState per-level**, tạo mới mỗi lần load, truyền qua parameters — không static.
- **Authoring UI/tool không phải runtime dependency.** Unity Editor tool có thể phụ thuộc Runtime; external tool giao tiếp qua canonical versioned data.
- GD cần làm level **không đồng nghĩa** phải có Unity Level Editor. Chọn authoring owner ở `Docs/project-context.md`.
- **Xoá sạch tầng Visual thì Domain vẫn chạy hết ván và ra đúng kết cục.** Đây là phép thử duy nhất
  cần nhớ để biết ranh giới có bị vi phạm chưa.

---

## 2. Contracts — 4 interface, giữ nhỏ

```csharp
public interface ICreateParameters { }

public interface IRuntimeCreatable
{
    void OnCreated(ICreateParameters parameters);
}

public interface IFactory<T>
{
    UniTask<T> Create(ICreateParameters parameters, CancellationToken cancellationToken);
}

public interface IPendingCleanup
{
    bool IsPendingCleanup { get; }
    void CleanupForLevelUnload();
}
```

- Mỗi element type có **một sealed `XxxCreateParameters`** riêng: data + adapter + parent +
  runtime state + service/callback. Domain **không** tự đi tìm dependency bằng global search.
- Element cần spawn element khác → nhận `Func<TData, UniTask<TEntity>>` hoặc service interface qua
  parameters, không hardcode factory lookup.
- Factory sealed, không rule: validate parameter type (`throw ArgumentException` nếu sai) → lấy
  prefab/pool → đặt name → ensure domain component → gọi `OnCreated`.
- `Instantiate` chỉ ở level-load hoặc editor. Gameplay loop dùng pool.

---

## 3. Ba loại component trên một entity

| Loại | Ví dụ tên | Chức năng |
|---|---|---|
| **Domain** | `XxxEntity`, `XxxPiece` | rule + state: occupancy, move plan, reserve target, xử lý event |
| **Visual** | `XxxView`, `XxxVisual` | `Render(data, adapter)` · `SetCounter(n)` · `PlayMoveAsync(ct)` · `StopAllTweens()` |
| **Decorator** | `XxxLockDecorator`, `XxxFrozenDecorator` | state phụ (hidden/locked/frozen): đổi interaction + presentation, **không** chiếm occupancy riêng |

Decorator là **pooled child prefab** do Factory gắn lúc level load theo data, subscribe event từ
runtime state, cleanup theo `IPendingCleanup`. Không subclass Domain/View cho từng element phụ —
tổ hợp element sẽ nổ số lớp.

Rule của element phụ nằm trong **runtime state (plain C#, test được không cần scene)**, không nằm
trong decorator. Decorator chỉ render và feedback từ event.

---

## 4. Nơi chỉnh số — quyết định một lần, dùng mãi

Đây là câu hỏi lặp lại nhiều nhất giữa các project. Chốt luôn:

| Loại giá trị | Chỗ đúng | Vì sao |
|---|---|---|
| Số riêng **một instance** trong scene (offset của một anchor cụ thể) | field trên **prefab / component** | designer sửa tại chỗ, không cần asset trung gian |
| Số dùng chung **cả game** (timing chuẩn, spacing chuẩn, palette, tween preset) | **Profile ScriptableObject** trong `Resources/Data/`, đọc qua `ResourceAsset<T>` | một chỗ sửa, không diff scene, không phải mở prefab |
| Số riêng **một level** | **Level JSON** | data-driven, level editor ghi được |
| Số **override level đè global** | field optional trong level JSON + resolver trong Profile | pattern `resolved = level.HasOverride ? level.Value : profile.Value` — luôn **một** hàm resolve duy nhất |
| Config **authoring** tái dùng giữa nhiều level (difficulty preset, palette preset) | ScriptableObject **ngoài `Resources/`** (editor assembly) | editor-only, không bị build vào app |
| Progress / unlock / setting người chơi | `StaticVariables` (Save) | persist qua session |
| Hằng số kỹ thuật không ai tune (epsilon, layer mask) | `const` / `static readonly` trong code | không tạo noise trong inspector |

### Profile bắt buộc có ở mọi project

Đặt trong `_Core/Data/Profiles/` (hoặc `Resources/Data/` nếu cần load runtime):

```
PrefabProfile     mọi prefab ref của gameplay + shared material + pool warmup count
TimingProfile     mọi duration/delay của gameplay & presentation
LayoutProfile     world position / spacing / scale của các root (Board, Queue, Tray, HUD anchor)
MotionProfile     tween preset id → curve/duration/ease   (xem knowledge/motion/)
AudioProfile      SFX/BGM cue id → clip
ColorProfile      color id → màu chuẩn + override vật liệu theo element
```

### Ba anti-pattern bị cấm ở mục này

1. Rải `[SerializeField] float duration` khắp Visual component. Duration thuộc
   `TimingProfile`/`MotionProfile`; component chỉ giữ **id của preset**.
2. `GameObject.CreatePrimitive(...)`, `new Material(...)`, `Shader.Find(...)` trong runtime path.
   Placeholder cũng phải là **prefab thật** trỏ từ `PrefabProfile`, để artist thay mesh/material
   trong prefab mà không đụng code.
3. **Hai nguồn cho cùng một sự thật.** Một màu, một timing, một prefab chỉ được có đúng một nguồn.
   Giữ nguồn cũ "làm fallback cho chắc" chính là lỗi, không phải lưới an toàn.

---

## 5. Spawn lifecycle — thứ tự bắt buộc

```
1.  Cancel token của level cũ
2.  Cleanup level cũ (IPendingCleanup.CleanupForLevelUnload trên mọi entity + unsubscribe event)
3.  Load + validate level data
4.  Resolve config: profile ← level override   (một hàm resolve duy nhất)
5.  Tạo RuntimeState mới (per-level)
6.  Tạo root transform (Board / Queue / Tray / ...) và áp world position từ LayoutProfile
7.  Spawn static / base board
8.  Spawn movable element  (decorator spawn NGAY SAU entity gốc của nó)
9.  Seed static obstacle TRƯỚC khi spawn producer / dynamic obstacle
10. Await mọi async initial
11. Build graph / bake cache (blocking graph, pathfinding, occupancy index)
12. Refresh hint / interaction state
13. Phát OnLevelSpawned → HUD bind, input mở
```

Sai thứ tự hay gặp:
- Build graph trước khi seed obstacle → graph thiếu cạnh.
- Spawn decorator trước entity gốc → decorator mất reference.
- Resolve layout **trước** cleanup → cleanup xoá mất settings vừa resolve.

### Unload — thứ tự cũng bắt buộc

```
1. Phát OnLevelWillUnload
2. Unbind input          ← trước bước 3, để không có tap lọt vào giữa teardown
3. Unsubscribe mọi event
4. Cancel token          (huỷ mọi animation đang chạy)
5. CleanupForLevelUnload trên mọi entity → reset state → Recycle về pool
6. Dispose runtime state
7. Destroy container rỗng (levelRoot), pool KHÔNG clear
```

Pool sống qua level để tránh GC spike khi Next/Reload; chỉ clear khi đổi scene.

---

## 6. Async, nhịp và lifetime

- Dùng **UniTask** cho mọi async gameplay/presentation. Không `IEnumerator` coroutine cho logic mới.
- Mọi method async nhận `CancellationToken`. Token gắn lifetime của **level**; object có thể chết
  giữa chừng → `CancellationTokenSource.CreateLinkedTokenSource`.
- Reserve target **trước** khi move; release trong `finally`.
- Tween phải kill khi cleanup (`StopAllTweens()` trong `CleanupForLevelUnload`).
- Subscribe event → **bắt buộc** unsubscribe ở cả `CleanupForLevelUnload` và `OnDestroy`.

### Domain giữ nhịp, Visual chỉ vẽ

Sai lầm phổ biến: `await visual.PlayAsync()` **rồi mới** mutate domain. Khi đó state chỉ đổi khi một
`MonoBehaviour` chạy xong `Time.deltaTime` của nó → không chồng nhịp được, và domain không test được
nếu không dựng scene.

Đúng: domain có một **scheduler** riêng.

```csharp
public sealed class ActionScheduler       // plain C#: không MonoBehaviour, không UniTask,
{                                          // không Time.deltaTime bên trong
    public void Schedule(float delay, DeferredAction action);
    public void Tick(float deltaTime);     // caller truyền dt vào
    public bool IsIdle { get; }
    public void CancelAll();
}
```

- Đúng **một** MonoBehaviour mỏng gọi `Tick(Time.deltaTime)` mỗi frame. Đó là toàn bộ chỗ domain
  chạm Unity.
- EditMode test: gọi `Tick(10f)` để nhảy tới cuối, hoặc `Tick(0.05f)` từng bước để assert trạng thái
  trung gian. Không cần scene, không cần chờ frame.
- **Đặt mọi delay = 0 thì toàn bộ chuỗi thu về một pha đồng bộ** — test cũ chạy nguyên không sửa.
  Đây là property test bắt buộc của scheduler.
- Delay và duration của tween **cùng đọc một bảng số** (`TimingProfile`). Đây là chỗ duy nhất domain
  và visual "gặp" nhau, và nó là **data chứ không phải reference**.
  Rủi ro kèm theo: ai đó hardcode duration trong code → hai bên drift **âm thầm**. Guard: grep test
  cấm hằng số thời gian trong domain.

### Presentation readiness & async ownership

`Logical destination/reservation` và `physical presentation readiness` là **hai sự thật khác nhau**.
Domain vẫn animation-agnostic; nhưng presentation handoff có thể cần biết vùng đích còn đang bị visual cũ chiếm hay không.

Contract generic:

1. **Reserve logic ngay** khi command được nhận — domain không chờ animation.
2. Visual owner set `IsArrivalBlocked/IsReady` **đồng bộ trước khi launch async**; không set sau `.Forget()`.
3. Mỗi motion có `operationId/generation`. Motion cũ bị cancel/replaced **không được** clear readiness hay restore state của motion mới. Chỉ active owner được final-write.
4. Không sync bằng magic delay (`0.1s`). Chờ bằng **explicit readiness state** hoặc cùng source timing data khi đúng semantics.
5. Destination/index/sequence có thể đổi trong lúc bay ⇒ **resolve lại trước handoff** hoặc validate stable id/sequence; không dùng cached slot index mù quáng.
6. Handoff ownership A → B ⇒ B normalize state nó sở hữu (outline, visibility, scale, renderer flags...) ngay tại boundary.
7. Cleanup/rebind pooled object ⇒ cancel + invalidate generation + readiness về neutral + full rebind.

Presentation readiness chỉ điều phối **presentation**, không được biến thành gameplay rule ẩn. Xoá Visual thì Domain vẫn phải ra kết quả y hệt.

### Reservation — cái làm cho chồng nhịp an toàn

Khi một hành động chiếm tài nguyên (slot, ô, target) qua nhiều nhịp, tài nguyên đó phải có trạng
thái **Reserved** và mọi luật phải đọc nó:

| Luật | Reserved tính thế nào |
|---|---|
| kiểm "đầy chưa" / admission | **có** tính là đã chiếm |
| kiểm điều kiện thua/kẹt | **có** tính là có nội dung (không kết luận vội khi thứ đang bay có thể cứu ván) |
| tiêu thụ thật (consume) | **không** tính — chưa có nội dung thật |

Không luật nào được đọc *"animation đang chạy hay chưa"*. Chúng chỉ đọc `Reserved` / `Filled`.
Reserved cũng phải **vẽ ra được** — nó là trạng thái thật, không phải mẹo che độ trễ.

---

## 7. Input

- Input đi qua **một** controller duy nhất → sinh **Command** → đẩy vào `CommandBuffer` → xử lý.
- Lý do: deterministic, replay được, test được, và chặn double-tap/race khi animation đang chạy.
- Không component nào tự đọc `Input`/`EventSystem` để đổi gameplay state.
- Frame không có tap → **0 raycast, 0 allocation**. Frame có tap → đúng 1 raycast, buffer
  `RaycastNonAlloc` cấp phát sẵn.
- `RaycastNonAlloc` **không sắp theo khoảng cách**. Chọn winner bằng **tiêu chí của luật**
  (stack order / layer / order key), không tin thứ tự PhysX trả về.
- Physics chỉ để *nhận diện* object được chạm. Hợp lệ hay không vẫn quyết bằng RuntimeState.

### Chồng nhịp

| # | Quy tắc | Tầng |
|---|---|---|
| R1 | Nhiều chuỗi action pending cùng lúc là **hợp lệ**. Không có khoá toàn cục | domain |
| R2 | Xung đột tài nguyên xử lý bằng **reservation**, không bằng luật riêng | domain |
| R3 | Action đến hạn cùng frame giải quyết theo `(dueTime, sequence)` — deterministic | domain |
| R4 | Trần số chuỗi đồng thời là **ngân sách pool của visual**, không phải luật. Vượt trần thì visual tái dùng instance, domain vẫn chạy đúng | visual |
| R5 | Win/Lose quyết trên domain ngay khi state thoả điều kiện. **Banner** chờ `scheduler.IsIdle` để người chơi không thấy kết quả trong lúc thứ khác còn đang bay | visual |

---

## 8. Kết cục (Win / Lose) là TRẠNG THÁI, không phải hệ quả của một cú tap

Đây là lỗi thiết kế lặp lại nhiều nhất và tốn nhất.

- Kiểm kết cục chạy **sau mỗi mutation** của state liên quan — kể cả deferred action của scheduler —
  không phải chỉ khi người chơi tương tác.
- Nếu chỉ kiểm trong `Evaluate(input)` thì trạng thái thua tồn tại mà game không biết cho tới lần tap
  tiếp theo. Người chơi kẹt → chán → thoát app → event `LevelFailed` **không bao giờ bắn** ⇒ lượt đó
  vào rổ *abandon* thay vì *fail*. Lệch **có hệ thống, đúng chiều ngược**: level càng khó thì fail
  rate ghi nhận càng thấp giả tạo.
- Thứ tự kiểm bắt buộc: **Win trước** → điều kiện kẹt → **còn action đang bay thì chưa kết luận** →
  mới Fail.
- Chạy song song nhiều chuỗi thì chỉ kiểm khi `inFlight == 0` — nếu không sẽ cắt mất những chuỗi
  chưa đáp.

---

## 9. Data & serialization

- **Level data = JSON** trong `Resources/Levels/`, tên có prefix loại: `<game>_level_XXX.json`.
- Serializer mặc định `JsonUtility`. Cần polymorphism / dictionary / nullable → Newtonsoft, và ghi
  vào `project-context.md`.
- Mọi entity có **stable ID** trong data. Runtime index theo ID — **không** theo index mảng,
  **không** theo tên GameObject. Selection trong editor cũng theo ID.
- Data có `schemaVersion`. Đổi schema → viết migration + test round-trip, không im lặng phá file cũ.
  Field mới có default an toàn thì **không cần** bump version — bump chỉ tạo nhiễu khi không có gì để
  migrate.
- Serializer phải **omit field null**, và khi đọc phải kiểm field có thực sự tồn tại trong JSON.
  `JsonUtility` tự dựng lại nested object rỗng → override rỗng sẽ **shadow** profile global mà không
  ai biết.
- Phân biệt rõ ba loại data, ghi vào `data-model.md`:
  - **Source of truth** — authoring data, cái duy nhất được save
  - **Generated / baked** — mesh, footprint, graph, cache; regenerate được
  - **Runtime state** — occupancy, reservation; sinh khi load, chết khi unload

Không bao giờ coi Mesh / Collider / Scene hierarchy là source of truth.

### Editor view state ≠ level data

Selection, mode/tab, zoom, pan, layer đang active, panel width **không** được ghi vào level JSON —
level sẽ dirty giả và sinh diff rác chỉ vì GD click một cái. Chúng thuộc `EditorViewState`
(`SessionState`) hoặc `EditorPrefs` (sở thích của máy, ví dụ độ mờ ghost).

---

## 10. HUD & UI

- Template **không hard-require một UI framework legacy**. Project có thể dùng `HUDSystem/Panel`, custom Canvas stack, hoặc framework khác; ghi lựa chọn vào `Docs/project-context.md`.
- Gameplay **không** biết HUD implementation tồn tại. Giao tiếp qua `Bridge/`:
  - `IHudPresenter` khai ở gameplay side (`Commons/`).
  - `XxxHudBridge` trong `Bridge/` implement nó, gọi UI stack của project.
- HUD đọc state qua event từ Domain/RuntimeState, **không poll trong `Update`**.
- Safe area / aspect: dùng `FixResolutionCanvas` + `SafeArea` có sẵn trong `AMZG/FixResolution`.
- **Mọi text trong world/HUD đi qua một prefab TMP có script quản lý riêng** — xem
  `standards/code-style.md §7`. Không `TextMesh` legacy, không dựng text bằng code.

---

## 11. Test

| Loại | Nhắm vào | Chạy khi |
|---|---|---|
| EditMode unit | Domain rule, baker, validator, generator (pure logic) | luôn |
| EditMode integration | pipeline data → generated → validated | khi có generation |
| PlayMode | spawn lifecycle, cleanup không leak, cancel token | mỗi milestone |
| Characterization / golden | thứ đóng vai **dụng cụ đo** (simulator, policy, scorer) | khi có dụng cụ đo |
| Manual + screenshot | visual, game feel, HUD, tool UI | qua Unity MCP nếu có |

- Domain viết theo hướng **plain-logic-first** để test được không cần scene. Visual không cần unit test.
- Thứ nào là **dụng cụ đo** thì phải có `Version` + golden test: sửa nó ⇒ test đỏ ⇒ buộc bump version
  trong cùng commit. Nếu không, số liệu của mọi level đổi mà không ai được báo.
- Story có UI: acceptance **bắt buộc** gồm một lần đi hết luồng bằng tay. Lỗi chí mạng kiểu
  "không có nút để thêm config" chỉ lộ ra khi thao tác thật, không lộ khi đọc code hay chạy unit test.

---

## 12. Checklist áp blueprint cho game mới

- [ ] `project-context.md` đã điền namespace root, prefix, Unity version, RP, scripts root, serialization.
- [ ] `runtime-architecture.md` đã map từng layer §1 sang tên class thật của game.
- [ ] `data-model.md` đã tách rõ source of truth / generated / runtime state.
- [ ] `glossary.md` có mọi khái niệm gameplay chính, mỗi cái đúng một tên.
- [ ] Các Profile SO ở §4 đã tồn tại (kể cả còn rỗng).
- [ ] Spawn lifecycle §5 có **một** entry point duy nhất.
- [ ] Có một `RuntimeState` per-level, không static.
- [ ] Có `CommandBuffer` cho input.
- [ ] Có scheduler nếu presentation kéo dài nhiều nhịp; property test "delay = 0" pass.
- [ ] Có `IHudPresenter` + Bridge; gameplay không tham chiếu UI framework cụ thể trực tiếp.
- [ ] Grep sạch: `CreatePrimitive` · `new Material(` · `Shader.Find` · `FindObjectOfType` trong runtime.
- [ ] Có ít nhất một EditMode test chạy được.
