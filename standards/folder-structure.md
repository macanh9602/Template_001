# Folder & Namespace Convention (generic)

> File này là guardrail generic. **Không ép project hiện hữu migrate chỉ để giống ví dụ.**
> Project-specific canonical roots phải được ghi trong `Docs/project-context.md`.

## 0. Canonical-root rule — một project chỉ có một production script root

### Default của Template này

Template hiện chứa code reusable/project skeleton dưới:

```text
Assets/_Core/4_Scripts
```

Vì vậy **greenfield project copy từ Template mặc định dùng `Assets/_Core/4_Scripts`**.

### Existing project adoption

Nếu project đã có script root khác và đang hoạt động tốt:

1. inspect root thật;
2. ghi root đó vào `Docs/project-context.md`;
3. giữ nguyên trong bootstrap;
4. chỉ migrate bằng một decision + migration story riêng nếu có lợi ích rõ.

**Cấm tạo root song song** chỉ để code mới "sạch":

```text
Assets/_Core/4_Scripts     ← project hiện hữu
Assets/_Core/Scripts       ← agent tự tạo thêm
```

Hai tree cùng chứa `Management/`, `Elements/`, `Creation/` là architecture bug, không phải separation.

---

## 1. Hai tư duy — chọn theo ownership

| Tư duy | Khi nào dùng | Namespace |
|---|---|---|
| **Layer-first** — module tái sử dụng | copy sang project khác không cần sửa gameplay semantics | `MDATools.[Module].[Layer]` |
| **Feature-first / Screaming** — feature của game | gắn với game cụ thể | `[GameRoot].[Feature].[Context].[Layer]` |

Test: *"Copy module này sang game khác, có phải sửa domain/gameplay semantics không?"*
Không → reusable module. Có → project feature.

---

## 2. Cây khuyến nghị bên trong canonical root

Giả sử canonical root đã chốt là `<GameScriptsRoot>`:

```text
Assets/
  Plugins/                  third-party — không sửa nếu không cần
  AMZG/                     module studio dùng chung
  MDATools/                 reusable tools/modules
  _Core/
    0_Texture2D/
    1_Materials/
    2_Models/
    3_Prefabs/
    4_Scripts/              ← default của Template; project-context có thể override
      Commons/
      Data/
        Level/
        Serialization/
        Profiles/
      Creation/
      Elements/
        <Feature>/
          Domain/
          Visual/
          Creation/
          Event/            optional
      Simulation/           nếu game có custom simulation
      System/
        Bootstrapper/
        Creation/
        Management/
        Instrumentation/
      Presentation/
      Bridge/
      Editor/
        <Tool>/
          C#/
          UXML/
          USS/
      Tests/
        Editor/
        PlayMode/
    5_Shaders/
    Scenes/
    Resources/
      Levels/
      Profiles/
```

**Không tạo folder rỗng chỉ để "đủ cây".** Tạo khi có responsibility thật.

---

## 3. Namespace rule

- Namespace root `[GameRoot]` lấy từ `Docs/project-context.md` và phải lock trước file production đầu tiên.
- Canonical filesystem root (`4_Scripts`, `Scripts`, ...) **không tự động trở thành namespace segment**.
- Organizational container như `Elements`, `System` có thể được bỏ khỏi namespace nếu project contract đã chốt như vậy; quan trọng là **một rule nhất quán**, không phải mirror literal mọi folder.
- Không dùng namespace trần kiểu `namespace Common`.
- Tránh segment `System` ở namespace nếu gây đụng `System` của .NET; có thể dùng `Systems` hoặc namespace theo feature.

Ví dụ với canonical root `Assets/_Core/4_Scripts`:

```text
Assets/_Core/4_Scripts/Elements/Board/Domain/BoardState.cs
→ namespace [GameRoot].Board.Domain
```

Project đã có namespace convention hợp lý ⇒ preserve, ghi trong project-context, không rename hàng loạt ở bootstrap.

---

## 4. Assembly definition — dependency-first, không skeleton-first

### Default an toàn

Nếu project/template đang compile trong `Assembly-CSharp`, **không tách asmdef chỉ vì standard có thể tách**.

Chỉ thêm asmdef khi có reason thật:

- Editor/runtime isolation;
- package/module boundary;
- compile-time dependency cần kiểm soát;
- test assembly cần reference rõ;
- build-time/platform split.

### Existing project adoption

Audit trước:

- module reusable nào đang ở `Assembly-CSharp`;
- DOTween/Odin/package assembly nào đang reference được;
- Editor code đang nằm đâu;
- code mới cần dependency gì.

Nếu thêm asmdef làm Runtime không còn gọi được shared module ở `Assembly-CSharp`, không được tự tiếp tục tạo thêm asmdef dây chuyền. Chọn một trong các hướng và ghi decision:

| Hướng | Khi hợp | Trade-off |
|---|---|---|
| Giữ project assembly hiện tại | prototype/small game, dependency ổn | ít isolation hơn, rework thấp |
| Thêm asmdef **tại canonical tree** | boundary có giá trị thật | cần audit dependency kỹ |
| Interface + bridge | muốn tách runtime khỏi legacy/shared code | thêm interface/binding nhưng blast radius nhỏ |
| Migration module reusable | module thực sự nên độc lập lâu dài | scope riêng, không làm lẫn bootstrap |

**Không tạo một script root mới để né assembly problem.**

---

## 5. Naming

| Thứ | Quy ước | Ví dụ |
|---|---|---|
| Domain component | `<Thing>` / `<Prefix><Thing>` theo project | `CakeStrip`, `SandSource` |
| Visual component | `<Thing>View` / `Visual` | `CupVisual` |
| Factory | `<Thing>Factory` | `SourceFactory` |
| Create parameters | `<Thing>CreateParameters` | `SourceCreateParameters` |
| Data DTO | `<Thing>Data` | `CupData` |
| Profile SO | `<Kind>Profile` | `SandSimulationProfile` |
| Runtime state | `<Domain>RuntimeState` | `LevelRuntimeState` |
| Level JSON | `<game>_level_<nnn>.json` | `sand_level_001.json` |
| Editor window | `<Tool>EditorWindow` | `LevelEditorWindow` |
| Editor partial | `<Window>.<Responsibility>.cs` | `LevelEditorWindow.Inspector.cs` |

Class prefix là project decision, không bắt buộc generic standard.

---

## 6. Prefab contract — logic ở root, visual ở child

```text
<ElementRoot>                  ← behavior/composition
├── Domain / Visual controller
├── Collider / authored gameplay proxy nếu contract cho phép
├── Decorator anchor (optional)
└── View/                      ← replaceable presentation
    ├── MeshFilter / SpriteRenderer / TMP
    └── Renderer
```

- Không hardcode visual offset/scale trong domain code.
- Runtime không `CreatePrimitive`, `new Material`, `Shader.Find` cho production path.
- Placeholder cũng nên là prefab thật nếu nó sẽ được art replace.
- Gameplay source-of-truth không được suy ra từ Renderer/Mesh nếu data contract đã có authored geometry.

---

## 7. Migration rule

Folder/namespace/asmdef migration là **một story riêng** nếu nó:

- move nhiều file;
- đổi namespace public;
- sửa scene/prefab serialized references;
- ảnh hưởng assembly/package dependency;
- làm thay đổi owner/lifecycle.

Bootstrap chỉ được **chốt canonical contract**, không âm thầm thực hiện migration lớn.
