---
name: spec-feature
description: >
  Lên spec đầy đủ cho một feature Unity mobile — gameplay logic, visual, editor setup — đủ để giao
  agent implement mà không phải giải thích thêm. KÍCH HOẠT khi: "làm feature X", "spec cho ...",
  cần mô tả một tính năng trước khi code, hoặc biến yêu cầu GDD thành thứ implement được.
  Prototype/GDD chưa lock thì dùng bản rút gọn ở refs/spec-lite.md.
---

# SKILL: spec-feature

Output là **một spec**, không phải code. Spec mô tả **cái gì**, không mô tả **làm thế nào**.

---

## Bước đầu tiên — đọc context

`Docs/project-context.md` · `Docs/glossary.md` · `standards/system-design.md §1` (layer nào chịu
trách nhiệm phần nào) · `Docs/data-model.md`.

Chỉ hỏi những gì **không** có trong các file đó:

- Feature này thuộc layer nào, chạm layer nào khác?
- Có event / data / prefab / profile nào liên quan đã tồn tại?
- Có ảnh reference, mockup, video gameplay không?

Có video/ảnh ref → mô tả lại bằng English technical term theo mốc thời gian trước khi spec
(xem `skills/game-feel-motion/`).

Thiếu thứ nào không suy ra được → hỏi trắc nghiệm (`workflow/ask-and-visualise.md`).

---

## Cấu trúc spec — ba phần, confirm từng phần

### Phần 0 — Vì sao có feature này

Bảng ba cột. Đây là phần quyết định spec có chạy trơn không.

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| cái gì đang thiếu/sai | trích **đúng chỗ** trong code, log, hoặc GDD | người chơi hoặc GD chịu hậu quả gì |

---

### Phần 1 — Gameplay logic

```markdown
## [Feature] — Gameplay Logic

### Mô tả ngắn
1–2 câu, từ góc nhìn người chơi.

### Luồng chính (happy path)
1. Trigger / điều kiện bắt đầu
2. Bước xử lý logic
3. State thay đổi
4. Output: event / callback / data update

### Edge cases
| Ca | Xử lý |
|---|---|

### Data model
- Input:
- Output:
- State cần lưu: (và **thuộc loại nào** — source of truth / generated / runtime state)

### Events
| Tên | Trigger khi | Listener dự kiến | Unsubscribe ở đâu |
|---|---|---|---|

### Layer assignment
| Việc | Layer chịu trách nhiệm |
|---|---|
Đối chiếu `standards/system-design.md §1`. Nếu một việc không rơi gọn vào layer nào → escalate.

### Dependency
- Depends on:
- Provides: (cái mà story sau sẽ dựa vào — đổi là escalate)

### Chưa rõ ở phần này
- [ ] (câu hỏi + phương án đề xuất)
```

**Confirm rồi mới sang Phần 2.**

---

### Phần 2 — Visual

```markdown
## [Feature] — Visual

### Mô tả visual tổng thể
Trông thế nào, cảm giác muốn đạt.

### Prefab structure
Theo contract `standards/folder-structure.md §6`: logic ở root, visual ở child `View/`.

<ElementRoot>
├── Domain / Visual controller
├── Collider (nếu cần picking)
└── View/  ← child duy nhất chứa phần nhìn thấy được

Prefab giữ **số slot tối đa**; runtime bật/tắt theo data.

### Animation / Tween
| Tên | Trigger | Duration | Easing | Preset id | Ghi chú |
|---|---|---|---|---|---|

Duration/easing **đọc từ TimingProfile/MotionProfile**, component chỉ giữ preset id.
Chuyển động phức tạp → dùng vocabulary ở `knowledge/motion/vocabulary.md`.

### Material / Shader
- Shared material từ PrefabProfile; khác biệt per-instance qua MaterialPropertyBlock.
- Property nào ghi qua MPB — liệt kê hết, ghi **cùng một hàm** (xem code-style §8).

### VFX / Audio
| Cue | Trigger | Pooled? |
|---|---|---|

### Text (nếu có)
Qua prefab TMP có script quản lý (`Init` / `Show` / `Hide` / feedback), pooled.

### Trạng thái đọc được
| State | Người chơi nhận ra bằng gì |
|---|---|
Mỗi state phải phân biệt được **bằng mắt**, không chỉ trong code.

### Responsive / mobile
- Safe area, aspect ratio, số phần tử hiển thị đồng thời theo màn hình.

### Polish checklist
- [ ] ...

### Chưa rõ ở phần này
- [ ]
```

**Confirm rồi mới sang Phần 3.**

---

### Phần 3 — Editor setup

Chỉ viết nếu feature cần tooling cho GD/level designer. Không cần thì ghi *"Không cần Editor Setup"*
và bỏ qua.

Feature là **level editor** hoặc tool lớn → dùng `skills/level-editor/` thay cho phần này.

```markdown
## [Feature] — Editor Setup

### Mục đích
GD cần làm được gì với tool này?

### Loại tool
- [ ] EditorWindow (UI Toolkit)  - [ ] Custom Inspector  - [ ] SO config  - [ ] MenuItem  - [ ] Gizmo

### GD workflow
1. ... (thứ tự thao tác thật của GD, không phải thứ tự field trong data)

### Data flow
Document ↔ level JSON ↔ runtime. Ai đọc, ai ghi, convert ở đâu.

### Validation
| Rule | Severity | Xử lý |
|---|---|---|
Blocking chặn save, warning cho save. Message viết bằng ngôn ngữ GD.

### GD workflow target
> GD làm được [mục tiêu cụ thể] trong [X phút] mà không cần hỏi dev.
```

---

## Bước cuối — assumptions & ready check

```markdown
## Assumptions
- (ghi rõ, để sau này biết chỗ nào là giả định)

## Open questions
- [ ] Q1 — ... → Owner: Dev / GD / Art

## Ready-to-implement checklist
- [ ] Gameplay logic đã confirm
- [ ] Visual reference đủ
- [ ] Editor workflow GD đã duyệt (nếu có)
- [ ] Dependency đã có hoặc có kế hoạch tạo
- [ ] Không còn open question blocker
- [ ] handoff/<story>/implementation-notes.html đã tạo
```

---

## Song song khi implement

Agent **vừa code vừa cập nhật** `implementation-notes.html` — ghi ngay khi phát sinh, không để cuối
mới nhớ lại. Khung: `templates/implementation-notes.html`.

> ⚠️ Quyết định / đánh đổi phát sinh giữa lúc code mà spec chưa định ⇒ **DỪNG, hỏi trắc nghiệm**
> (`enrich-context`) trước khi làm. Sau khi dev chọn → ghi vào notes. Đây là chốt chặn quan trọng
> nhất: tránh agent âm thầm chọn hướng sai rồi sửa đi sửa lại nhiều vòng.

---

## Lưu ý

- Không viết code trong spec.
- Không liệt kê tên class agent phải tạo — chỉ ghi `Required contracts`.
- Dùng đúng tên trong `glossary.md`; không tự đặt tên mới cho khái niệm đã có.
- Dev cung cấp ảnh/video ref → mô tả visual **từ ref đó**, không tự sáng tác thêm chi tiết.
