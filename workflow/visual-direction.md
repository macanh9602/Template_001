# Visual direction — từ lab tới Profile SO

> Mục tiêu: GD/PO chốt visual trên HTML **một lần**, số tự chảy vào Unity, agent tự hội tụ visual về số đó.
> Không ai chép tay số từ HTML sang Inspector; không agent nào tự "duyệt" hướng visual.

```
 lab HTML (agent dựng, trên level thật)
   │  export
   ▼
 handoff/visual/proposals/<id>.json        status: CANDIDATE     ← agent được ghi
   │  PO chọn section đã chốt
   ▼  tools/promote-direction.ps1 -Sections look
 handoff/visual/direction.v00N.json        look: APPROVED        ← chỉ script ghi, bất biến
 handoff/visual/CURRENT                    → direction.v00N.json
   │  Unity: Tools/Visual Direction/Dry Run → Import approved sections
   ▼
 Profile ScriptableObject (derived)        field trong profileMap = không chỉnh tay
   │
   ▼  visual-review loop: capture → rubric → patch có số → capture lại
 captures/ · reviews/ · motion CSV · MotionParity PASS
```

## Ai làm gì

| Vai | Được | Không được |
|---|---|---|
| **PO / GD** | chọn phương án trong lab, chạy `promote-direction.ps1` theo section | — |
| **Agent director** (Cowork/chat, có browser) | dựng lab, xuất proposal, hỏi trắc nghiệm, gọi promote **khi PO đã chọn** | ghi `APPROVED` trực tiếp, sửa `Assets/` |
| **Agent implementer** (runner, có Unity MCP) | Import, code theo direction, capture, parity, patch trong `writeSet` | sửa `handoff/visual/` (direction/proposal), đổi gameplay rule |
| **Reviewer** (phiên riêng, chỉ đọc) | chấm rubric trên capture + số, ra `review.json` | xem reasoning của implementer, sửa file |

## Lệnh

| Việc | Lệnh |
|---|---|
| Xem direction hiện hành | `.\tools\promote-direction.ps1 -Status` |
| Xem trước promote | `.\tools\promote-direction.ps1 -Proposal handoff\visual\proposals\<id>.json -Sections look -DryRun` |
| Promote | `... -Sections look,motion -By "<tên PO>"` |
| Chuyển file cũ `<game>.visual-target/v1` | `.\tools\promote-direction.ps1 -Migrate handoff\wp-00X\visual-target.json` |
| Direction đổi (vN → vN+1): đổi gì, Import gì, task nào STALE | `.\tools\direction-delta.ps1` (mặc định CURRENT vs bản nó supersedes) |
| Drift giữa direction và Profile SO | Unity menu `Tools/Visual Direction/Dry Run (log drift)` |
| Nhập số đã duyệt | `Tools/Visual Direction/Import approved sections` |
| Motion parity | `Tools/Visual Direction/Motion Parity (CSV + so target)` |
| Chụp level tham chiếu | `Tools/Visual Direction/Capture reference level (idle)` |

Log Unity: **dòng đầu là kết luận** (`read_console` chỉ trả dòng đầu); PASS = `Debug.Log`, lỗi/drift/FAIL = `Debug.LogWarning`.
Nhờ vậy script verify của runner kiểm được bằng log level, 0 token (`workflow/run-task.md` §3a, `skills/visual-review/SKILL.md` › Script verify).

## Luật

1. **Một schema** `visual-direction/v1` cho proposal và direction; khác nhau ở `status` theo section + `version`.
2. **Promote theo section.** Look thường khoá trước; Motion/FX mở tới checkpoint sau. Section chưa APPROVED: importer bỏ qua, parity đo được nhưng không PASS.
3. **Direction vN bất biến.** Đổi hướng = proposal mới + promote ⇒ vN+1. Lịch sử ở `promotions.jsonl`; quyết định cấp project (khoá art direction) ghi thêm `Docs/decision-log.md`.
4. **Profile SO là derived.** Field có trong `profileMap` chỉ đổi qua Import. Dry Run có drift ⇒ Import (hoặc ESCALATE nếu drift là chủ ý của ai đó).
5. **Task có thể ghi `targetRef: "handoff/visual/CURRENT#motion"`**; runner đổi thành file cụ thể lúc chạy (vd `direction.v003.json#motion`) và ghi vào result/review. Promote bản mới ⇒ `direction-delta.ps1` so run DONE gần nhất với CURRENT: phần task quan tâm có đổi ⇒ **STALE**, in lệnh `run-batch` để chạy lại (target delta, V2).
6. **Implementer không sửa target.** Fix cần đổi số của section APPROVED ⇒ `TARGET_RECONSIDER` (reviewer) → proposal mới → PO.

## Project cần làm một lần

- Profile SO có tên duy nhất trong project (vd `VisualProfile`, `MotionProfile`) — importer tìm theo tên asset.
- `IVisualCaptureScenario` (load level + pose) và mỗi demo motion một `IMotionParityDemo` — `skills/visual-review/refs/capture-protocol.md`.
- Copy `skills/visual-review/refs/rubric.md` → `handoff/visual/rubric.md`, điền ngưỡng của game.

## Nguồn harvest

Ducan_SpeedRun_Demo WP004: `VisualTargetImporter` (viết cứng từng field) → `VisualDirectionImporter` (theo `profileMap`, SerializedObject);
`MotionParity` (4 demo cứng, so channel theo thứ tự) → `IMotionParityDemo` (so theo tên, tolerance trong direction);
`VisualCaptureRunner` (gắn scene BlockHome) → `VisualCapture` + `IVisualCaptureScenario`;
`blockhome.visual-target/v1` + `visual-proposal/v1` (2 tên cho 1 khái niệm) → `visual-direction/v1`.
