# Roadmap — [TÊN GAME]

> Roadmap là **capability map + gate**, không phải lý do để materialize toàn bộ story tương lai.
> Cập nhật khi đóng gate/story hoặc khi project-level architecture decision đổi.

---

## Giai đoạn hiện tại

**Project mode:** GREENFIELD / EXISTING_PROJECT_ADOPTION
**Playbook đang chạy:** `playbooks/pX-....md`
**Gate hiện tại:** [readiness / foundation / technical slice / ...]
**Executable worker packet/story duy nhất:** [path hoặc NONE]
**Execution state:** PLANNED / LOCKED / EXECUTABLE / IMPLEMENTING / IMPLEMENTED / VERIFYING / BLOCKED / DONE / SUPERSEDED
**Mốc gần nhất:** [observable capability]

---

## Materialization rule

Canonical execution status:

```text
PLANNED → LOCKED → EXECUTABLE → IMPLEMENTING → IMPLEMENTED → VERIFYING → DONE
                                  ↘ BLOCKED
SUPERSEDED = historical, never executable
```

`IMPLEMENTED` nghĩa là code/scope đã tồn tại. `DONE` chỉ khi required closure evidence/gate PASS.
Tại một thời điểm chỉ có **một executable worker packet/story** trừ khi roadmap ghi rõ các packet độc lập không chạm ownership/file.

- Phase/capability phía trước có thể ghi ngắn ở roadmap.
- **Chỉ story gần nhất sau gate hiện tại được materialize thành execution spec đầy đủ.**
- Story N+1 không materialize khi Story N còn là architecture/foundation gate chưa PASS.
- Khi architecture đổi, sửa capability map thay vì xóa/rewrite cả pack story đã viết sẵn.

---

## Capability / phase map

| Phase | Capability quan sát được | Gate để mở | Trạng thái |
|---|---|---|---|
| A | Project readiness + canonical contracts | — | TODO |
| B | Foundation runtime ownership chạy sạch | A PASS | LOCKED |
| C | Technical vertical slice rủi ro cao nhất | B PASS | LOCKED |
| D | Playable core loop | C PASS | LOCKED |
| E | Level authoring/editor | D PASS hoặc project-specific gate | LOCKED |
| F | Difficulty / feel / ship | dependency tương ứng | LOCKED |

Không bắt project dùng đúng chữ A–F; đổi tên theo game, nhưng giữ nguyên logic gate.

---

## Active / historical story

| # | Tên | Size | Trạng thái | Dependency | Source |
|---|---|---|---|---|---|
| 001 | [story kế tiếp] | S/M/L | EXECUTABLE / TODO | [gate] | `handoff/story-001-.../story.md` |

Trạng thái theo `workflow/verification.md` — DONE nghĩa là đã có evidence thật.

Story bị CUT/SUPERSEDED phải ghi lý do; historical evidence có thể archive thay vì để executable lẫn lộn.

---

## Rủi ro đang theo dõi

| Rủi ro | Evidence | Ứng phó | Trạng thái |
|---|---|---|---|

## Câu hỏi project-level còn mở

| Câu hỏi | Owner | Chặn gate/story nào | Hạn |
|---|---|---|---|

## Nợ kỹ thuật

| Nợ | Từ story | Vì sao chấp nhận | Phải trả khi |
|---|---|---|---|
