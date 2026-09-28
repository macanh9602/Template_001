# Data Model — [TÊN GAME]

## 1. Ba loại data — bắt buộc phân biệt

| Loại | Ví dụ trong game này | Được save? |
|---|---|---|
| **Source of truth** (authoring) | | ✅ cái duy nhất được save |
| **Generated / baked** | | regenerate được |
| **Runtime state** | | ❌ sinh khi load, chết khi unload |

Không coi Mesh · Collider · debug geometry · scene hierarchy là source of truth.

## 2. Schema

### `[Prefix]LevelJson`

| Field | Type | Ý nghĩa | Bắt buộc |
|---|---|---|---|
| `schemaVersion` | int | | ✅ |
| | | | |

### `[Prefix][Entity]Data`

| Field | Type | Ý nghĩa |
|---|---|---|
| stable id | string | không dùng index mảng làm định danh |
| | | |

## 3. Global vs level override

| Giá trị | Global (Profile) | Level override | Resolver |
|---|---|---|---|
| | | | |

Luật: mọi override đi qua **một** hàm resolve duy nhất. Cấm đọc `profile.X` ở chỗ này và `level.X` ở
chỗ khác.

Serializer phải **omit field khi override null**, và deserializer phải kiểm field có thực sự tồn tại
trong chuỗi JSON — `JsonUtility` tự dựng lại nested object rỗng, và override rỗng sẽ shadow profile
global mà không ai biết.

## 4. Giá trị nào ở đâu

> Đối chiếu `standards/system-design.md §4`. Liệt kê ở đây những giá trị mà người ta hay tìm nhầm chỗ.

| Giá trị | Ở đâu | Ai chỉnh |
|---|---|---|
| | prefab field / Profile / level JSON | dev / GD / art |

## 5. Save / load policy

Bắt buộc save:
-

Open decision:
- [ ] Serialize generated cache hay bake lại khi load?
  Cân nhắc: editor responsiveness · runtime load time · file size/versioning · workflow generator về sau.
  Nếu bake lại: **bắt buộc** có test parity editor ↔ runtime và số đo bake time.

## 6. Migration

| From → To | Thay đổi | Cách migrate | Test |
|---|---|---|---|
| | | | round-trip |

Luật: field mới có default an toàn ⇒ **không cần** bump `schemaVersion`. Bump chỉ tạo nhiễu khi không
có gì để migrate. Field **đổi nghĩa** thì bắt buộc bump.

## 7. Invariants (validator phải check)

- [ ] ID unique, không dangling reference
- [ ] Graph deterministic, không duplicate edge, không cycle trái contract
- [ ] Ràng buộc chia hết / cân bằng cung-cầu (nếu có)
- [ ]

## 8. Editor view state — KHÔNG vào level data

> Liệt kê ở đây để không ai nhét nhầm.

| View state | Lưu ở đâu |
|---|---|
| selection, mode/tab, layer đang active | `EditorViewState` (`SessionState`) |
| zoom, pan, panel width | `SessionState` |
| preference của máy (độ mờ, snap on/off) | `EditorPrefs` |
