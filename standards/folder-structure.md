# Folder & Namespace Convention (generic)

> Không sửa file này khi làm game mới. Chỉ thay `[GameRoot]` bằng namespace root thật
> (lấy từ `Docs/project-context.md`).

## 1. Hai tư duy — chọn đúng ngay từ đầu

| Tư duy | Khi nào dùng | Namespace |
|---|---|---|
| **Layer-first** — module tái sử dụng | copy sang project khác **không cần sửa dòng nào** (ObjectPool, Fluid, VFXPool, AudioSystem) | `MDATools.[Module].[Layer]` |
| **Feature-first / Screaming** — feature của game | gắn với game cụ thể (Board, Booster, Shop, Tutorial) | `[GameRoot].[Feature].[Context].[Layer]` |

Test một câu: *"Copy folder này sang game khác, có phải sửa dòng nào không?"*
Không sửa → `MDATools`. Phải sửa → `[GameRoot]`.

## 2. Cây chuẩn

```text
Assets/
  Plugins/                  DOTween · Odin · Dreamteck · third-party   (KHÔNG sửa)
  AMZG/                     module studio dùng chung: Haptic · SFX · FixResolution · HandCursorUI
  MDATools/                 tool + module tái sử dụng: ObjectPool · Save · UIBase · Helpers · ResourceAsset
  _Core/                    TẤT CẢ nội dung của game này
    Animations/ Fonts/ Materials/ Mesh/ Models/ Shaders/ Texture2D/ VFX/ SFX/
    Prefabs/
      <Feature>/            prefab gameplay theo feature
      UI/                   panel, popup
      Common/               floating text, marker, effect dùng chung
    Data/
      Profiles/             PrefabProfile · TimingProfile · LayoutProfile · MotionProfile · AudioProfile · ColorProfile
      <Domain>Configs/      SO config authoring theo domain (difficulty, palette...) — editor-only
    Resources/
      Data/                 SO cần load runtime qua ResourceAsset<T>
      Levels/               level JSON
    Scenes/
    Scripts/
      Commons/              interface + type dùng chung             → [GameRoot].Commons
      Data/
        Level/              DTO level + entity data
        Serialization/      serializer + migration
        Templates/          (nếu có) template authoring
      System/
        Bootstrapper/       khởi động · profile · pool warmup
        Creation/           spawner / level loader
        Management/         RuntimeState · GameFlow · Scheduler · CommandBuffer · InputController
        Instrumentation/    (nếu có) đo đạc, export
        Simulation/         (nếu có) trình chơi thử / evaluator
      Elements/
        <Feature>/
          Domain/           rule + state
          Visual/           render + tween
          Creation/         factory + create parameters
          Event/            (tuỳ chọn) event type riêng feature
      Baking/               generator: Geometry / Graph / Validation   (nếu có procedural)
      Presentation/         world-space text · marker · camera fx
      Bridge/               gameplay ↔ HUD / meta / analytics
      Editor/
        <Tool>/
          C#/               partial class của EditorWindow + views
          UXML/
          USS/
        Localization/       chuỗi GD-facing (ngoài vùng quét ASCII gate)
      Tests/
        Editor/  PlayMode/
```

## 3. Luật namespace

- **Namespace mirror CHÍNH XÁC folder path.**
  `_Core/Scripts/Elements/Board/Domain` → `[GameRoot].Board.Domain`.
- Không namespace trần: `namespace Common` đứng một mình = sai.
- Tránh dùng `System` làm segment (đụng namespace `System` của .NET) → dùng `Systems`.
- `[GameRoot]` lấy từ `project-context.md`. **Chưa có → hỏi dev một câu trước khi tạo file đầu tiên**,
  rồi ghi lại để không hỏi nữa.
- Module tái sử dụng mặc định root `MDATools`.
- Nhìn `Scripts/Elements/` top-level phải biết ngay game có feature gì. Chỉ thấy `Managers/`,
  `Utils/`, `Misc/` → sai tư duy.

## 4. Assembly definition

```
[GameRoot].Runtime        Scripts/  (trừ Editor, Tests)
[GameRoot].Editor         Scripts/Editor/           → reference Runtime
[GameRoot].Tests.Editor   Scripts/Tests/Editor/     → reference Runtime (+ Editor nếu test tool)
[GameRoot].Tests.PlayMode Scripts/Tests/PlayMode/
```

- Editor asmdef **không bao giờ** được reference ngược vào Runtime bằng `#if UNITY_EDITOR` rải rác
  trong Domain.
- Rule pure-logic đáng tách riêng (`[GameRoot].Rules`) khi muốn test nhanh không kéo `UnityEngine`.

### Bẫy asmdef phải xử lý ở story đầu tiên

Code trong `Assembly-CSharp` (folder không có asmdef) compile **sau** mọi asmdef, nên
`[GameRoot].Runtime` **không thể** reference nó. Nếu `HUDSystem`, `ObjectPool`, `StaticVariables`,
`AudioController` đang nằm ở `Assembly-CSharp` thì có hai đường:

| Hướng | Được | Mất |
|---|---|---|
| **(a)** Thêm asmdef cho các module đó (và cho dependency của chúng, ví dụ DOTween) | dependency sạch, reference thẳng | phải dò hết dependency ngoài; blast radius khó chặn trên |
| **(b)** Đảo dependency bằng interface + bridge — `Commons/` khai `IPool`, `IHudPresenter`, `IInputGate`; `Bridge/` (nằm trong `Assembly-CSharp`) implement bằng class thật; dev gán qua `[SerializeField]` | không đụng module cũ, đúng chiều dependency, test mock được dễ | thêm vài interface mỏng; Runtime không dùng được thư viện chỉ có ở `Assembly-CSharp` |

**Phải chốt trước dòng code đầu tiên.** Hỏi dev bằng trắc nghiệm, đừng tự chọn — nó quyết định cả
cách viết tween trong runtime.

## 5. Đặt tên

| Thứ | Quy ước | Ví dụ |
|---|---|---|
| Domain component | `<Prefix><Thing>` | `CakeStrip`, `IconTile` |
| Visual component | `<Prefix><Thing>View` / `Visual` | `CakeStripView` |
| Decorator | `<Prefix><Thing><State>Decorator` | `CakeStripFrozenDecorator` |
| Factory | `<Prefix><Thing>Factory` | `CakeStripFactory` |
| Create parameters | `<Prefix><Thing>CreateParameters` | `CakeStripCreateParameters` |
| Data DTO | `<Prefix><Thing>Data` | `CakeStripCurveData` |
| Profile SO | `<Prefix><Kind>Profile` | `CakeTimingProfile` |
| Runtime state | `<Prefix><Domain>RuntimeState` | `CakeQueueRuntimeState` |
| Level JSON | `<game>_level_<nnn>.json` | `cake_level_001.json` |
| Editor window | `<Prefix><Tool>EditorWindow` | `CakeLevelEditorWindow` |
| Editor partial | `<Window>.<Responsibility>.cs` | `CakeLevelEditorWindow.Inspector.cs` |

`<Prefix>` là một từ ngắn của game, thống nhất toàn project, khai trong `project-context.md`.

## 6. Prefab contract — logic ở root, visual ở child

```
<ElementRoot>                  ← logic, KHÔNG chứa Renderer
├── Domain / Visual controller
├── Collider (nếu cần picking)
├── Decorator anchor (nếu có)
└── View/                      ← child duy nhất chứa phần nhìn thấy được
    ├── MeshFilter / SpriteRenderer / TMP
    └── MeshRenderer  (shared material từ PrefabProfile)
```

- Script **không bao giờ** `GetComponent<Renderer>()` trên chính nó — luôn cache qua
  `[SerializeField] Transform view` / `[SerializeField] Renderer viewRenderer` gán sẵn trong prefab.
  Artist đổi mesh trong child không được làm null reference.
- Không hardcode scale/offset trong code; mọi canh chỉnh nằm ở transform của `View/`.
- Placeholder cũng là prefab thật, không `CreatePrimitive`.
- Prefab giữ **số slot/child tối đa**; runtime bật/tắt và reposition theo data. Đổi số trong data
  không phải mở prefab.
