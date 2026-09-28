# Project Context — [TÊN GAME]

Version: 0.2
Stage: Prototype / Discovery
Primary platform: Mobile

> Agent đọc file này **trước mọi task**. Mọi thứ riêng của game nằm ở đây, không nằm trong
> `standards/` hay `AGENTS.md`.
>
> Đây là file chống-hỏi-lại và chống-template-assumption: project thật thắng skeleton generic.

---

## 1. Product context

| | |
|---|---|
| Internal name / code | `[in0xx-tên]` |
| Genre | |
| Gameplay reference | |
| Mục tiêu giai đoạn này | |
| GDD đã lock chưa | chưa / một phần / rồi |
| Production-ready chưa | |
| Ai chốt gameplay | |
| Ai dùng level editor | |

## 2. Source of truth

| Thứ | File |
|---|---|
| Gameplay / design | `reference/[...]` |
| Gameplay / visual reference | `reference/[...].mp4` |
| Project guardrail | `AGENTS.md` + file này |
| Generic system guardrail | `standards/system-design.md` |
| Applied runtime architecture | `Docs/runtime-architecture.md` |
| Data contract | `Docs/data-model.md` |
| Vocabulary | `Docs/glossary.md` |
| Active scope | `handoff/ROADMAP.md` + story executable hiện tại |
| Decision | `Docs/decision-log.md` + implementation notes |

## 3. PROJECT FACTS TO CONFIRM

> Điền trước file production đầu tiên. Agent được inspect repo để **đề xuất** giá trị; các mục 🔒 là
> project-wide contract và cần dev confirm nếu chưa có evidence rõ.

| Fact | Giá trị | |
|---|---|---|
| Project mode | `GREENFIELD` / `EXISTING_PROJECT_ADOPTION` | 🔒 |
| Namespace root `[GameRoot]` | | 🔒 |
| Class name prefix | none / ... | |
| Unity version | | |
| Render pipeline | URP / ... | |
| **Canonical production script root** | `<inspect repo first>` | 🔒 |
| Prefab root | `<inspect repo first>` | |
| Scene root | `<inspect repo first>` | |
| Build scene / bootstrap owner | | 🔒 |
| Assembly reality | `Assembly-CSharp` / existing asmdefs | |
| Assembly strategy | preserve / add asmdef in-place / bridge / migrate | 🔒 |
| Existing code policy | reuse generic by audit; replace/adapt game-specific | 🔒 |
| Level serialization | JSON / SO / other | 🔒 |
| Level data path | | |
| Runtime/physics authority | | 🔒 |
| Physics dimension | 2D / 3D / hybrid | |
| Determinism requirement | | |
| Async library | UniTask / ... | |
| Tween library | DOTween / ... | |
| Pool | `VTLTools.ObjectPool` / ... | |
| Text | TextMeshPro qua prefab/script quản lý | |
| Inspector | Odin / default | |
| Test framework | Unity Test Framework | |
| Máy target low-end | | |
| Frame budget | 60 fps mid / 30 fps low | |
| Unity MCP có bật không | | |
| External reference asset bắt buộc? | none / path + lý do | |

### Canonical-root invariant

- Chỉ có **một** production script root cho code game mới.
- Nếu project hiện hữu có root khác default Template, preserve root đó trừ khi có migration decision riêng.
- Không tạo tree song song kiểu `_Core/Scripts` + `_Core/4_Scripts` cùng responsibility.

## 4. Development philosophy

- Build để khám phá design, không giả vờ GDD đã lock.
- Existing project: inspect → harvest → confirm contract → xử lý gap; không reset theo skeleton.
- Manual authoring trước → đo/observe → evaluator → generator khi phù hợp.
- Cắt technical risk thành slice nhỏ, runnable, verify độc lập.
- Không over-engineer. Không abstraction "phòng xa".
- Số liệu ra khỏi code: prefab field / Profile SO / level data.
- Architecture/foundation gate PASS trước khi materialize nhiều story downstream.

## 5. Roadmap hiện tại

Xem `handoff/ROADMAP.md`.

- Roadmap được phép mô tả capability/phases phía trước.
- Chỉ story executable kế tiếp được materialize đầy đủ khi dependency gate đã PASS.

## 6. Decisions đã chốt

> Tóm tắt một dòng mỗi decision, kèm số `D-xxx`. Chi tiết ở `decision-log.md`.

### Architecture / adoption
-

### Gameplay
-

### Data & authoring
-

### Visual & feel
-

### Performance
-

## 7. Câu hỏi đã trả lời — không hỏi lại

| Câu hỏi | Trả lời | Ngày |
|---|---|---|
| | | |

## 8. Existing/reusable module inventory

| Module / folder | Classification | Dùng lại gì | Không dùng lại gì | Rủi ro |
|---|---|---|---|---|
| | REUSE DIRECT / ADAPT / REFERENCE / IGNORE | | | |

**Không classify cả folder là reusable chỉ vì có vài interface generic.** Audit ở module/file cần thiết.

## 9. Legacy / external reference

| Reference | Vai trò | Có phải source of truth? | Có bắt buộc để bootstrap? |
|---|---|---|---|
| | architecture/style/gameplay reference | yes/no | yes/no |

Rules:

- reference dùng để học pattern không đồng nghĩa project phải copy folder/namespace/owner;
- asset `.zip` ngoài workspace không phải blocker mặc định;
- nếu reference là bắt buộc, ghi rõ phần nào của nó authoritative.

## 10. Adoption gaps

> Chỉ dùng khi `EXISTING_PROJECT_ADOPTION`.

| Gap | Evidence | Action | Gate |
|---|---|---|---|
| | | | |

Bootstrap không làm cleanup/migration ngoài bảng này.
