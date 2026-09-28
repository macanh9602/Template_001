# Project Context — [TÊN GAME]

Version: 0.1
Stage: Prototype / Discovery
Primary platform: Mobile

> Agent đọc file này **trước mọi task**. Mọi thứ riêng của game nằm ở đây, không nằm trong
> `standards/` hay `AGENTS.md`.
>
> Đây là file chống-hỏi-lại: câu nào agent phải hỏi tới lần thứ hai thì câu trả lời thuộc về file này.

---

## 1. Product context

| | |
|---|---|
| Internal name / code | `[in0xx-tên]` |
| Genre | |
| Gameplay reference (game có thật) | |
| Mục tiêu giai đoạn này | |
| GDD đã lock chưa | chưa / một phần / rồi |
| Production-ready chưa | |
| Ai là người chốt gameplay | |
| Ai dùng level editor | |

## 2. Source of truth

| Thứ | File |
|---|---|
| Gameplay / design | `reference/[...]` |
| Gameplay / visual reference | `reference/[...].mp4` |
| Project guardrail | `AGENTS.md` + file này |
| Tầng hệ thống | `standards/system-design.md` (generic, **không sửa**) |
| Kiến trúc game này | `Docs/runtime-architecture.md` |
| Data contract | `Docs/data-model.md` |
| Từ vựng | `Docs/glossary.md` |
| Story scope | `handoff/story-xxx.md` |
| Decision | `Docs/decision-log.md` + implementation notes |

## 3. PROJECT FACTS TO CONFIRM

> **Điền trước file code đầu tiên.** Agent được khảo sát repo để **đề xuất** giá trị, nhưng phải chờ
> dev confirm những mục kéo theo project-wide contract (đánh dấu 🔒).

| Fact | Giá trị | |
|---|---|---|
| Namespace root `[GameRoot]` | | 🔒 |
| Class name prefix | | 🔒 |
| Unity version | | |
| Render pipeline | URP | |
| Root scripts folder | `Assets/_Core/Scripts` | |
| Assembly convention | feature-scoped Runtime / Editor / Tests | 🔒 |
| **Module cũ nằm ở `Assembly-CSharp`?** → chọn hướng (a) thêm asmdef hay (b) interface + bridge | | 🔒 |
| Level serialization | JSON (`JsonUtility`) tại `Assets/_Core/Resources/Levels/` | 🔒 |
| Async library | UniTask | |
| Tween library | DOTween (hoặc UniTask + lerp nếu assembly không reference được) | |
| Pool | `VTLTools.ObjectPool` | |
| Text | TextMeshPro qua prefab có script quản lý | |
| Inspector | Odin | |
| Test framework | Unity Test Framework | |
| Máy target (low-end) | | |
| Frame budget | 60fps mid / 30fps low | |
| Unity MCP có bật không | | |
| Code cũ được phép tham khảo | `Assets/Legacy/...` | |

## 4. Development philosophy

- Build để khám phá design, không giả vờ GDD đã lock.
- Manual authoring trước → đo/observe → evaluator → generator. Không đảo thứ tự.
- Cắt technical risk thành slice nhỏ, runnable, verify độc lập.
- Không over-engineer. Không abstraction "phòng xa".
- Số liệu ra khỏi code: prefab field / Profile SO / level data.

## 5. Roadmap hiện tại

Xem `handoff/ROADMAP.md`. Không nhảy story khi foundation trước chưa đạt acceptance.

## 6. Decisions đã chốt

> Tóm tắt **một dòng** mỗi decision, kèm số `D-xxx`. Chi tiết ở `decision-log.md`.

### Gameplay
-

### Data & authoring
-

### Visual & feel
-

### Performance
-

## 7. Câu hỏi đã trả lời — không hỏi lại

> Mỗi lần agent phải hỏi một câu tới **lần thứ hai**, câu trả lời được ghi xuống đây.

| Câu hỏi | Trả lời | Ngày |
|---|---|---|
| | | |

## 8. Module tái sử dụng đã kiểm kê

| Module | Ở đâu | Dùng được cho | Rủi ro khi tái sử dụng |
|---|---|---|---|
| | | | |

## 9. Legacy reference

- Path:
- Rules: đọc để tham khảo architecture/style · **không copy namespace** · không sửa trừ khi story yêu cầu.
