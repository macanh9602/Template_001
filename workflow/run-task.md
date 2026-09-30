# Run task — doctor · runner · task/result/review

> Cách chạy một task end-to-end mà **Product Owner không phải chuyển prompt, screenshot, log hay compile hộ**.
> V1: một task mỗi lần, một Unity lane. Chưa có DAG scheduler.

```text
tools/doctor.ps1          máy + từng agent host có làm được việc không  → .toolchain/capabilities.json
tools/run-task.ps1        task.json → implementer headless → git diff → reviewer read-only → PATCH/PASS
tools/template-lint.ps1   cấu trúc repo không tái phạm lỗi đã trả giá
templates/schemas/        task/v1 · result/v1 · review/v1
```

---

## 1. Setup máy (một lần mỗi máy)

1. Mở Unity project. Trong panel **MCP for Unity**: *Start Server*, rồi *Client: Codex → Configure*.
2. Chạy `tools/doctor.ps1 -Repair`:
   - tự cài Claude Code CLI / Codex CLI nếu thiếu;
   - tự đăng ký Unity MCP cho Claude Code bằng đúng entry panel đã ghi cho Codex;
   - chạy **lệnh thật** qua từng host (đọc Unity Console headless) và kiểm reviewer không ghi được file.
3. Bước tay còn lại doctor sẽ in đúng một dòng hướng dẫn cho mỗi bước:
   - đăng nhập `claude auth login` và `codex login` (OAuth trình duyệt);
   - model trong `~/.codex/config.toml` không dùng được với tài khoản → sửa dòng `model =`.

Mức kiểm theo từng host: `INSTALLED → AUTHENTICATED → UNITY_MCP_CONFIGURED → UNITY_MCP_SMOKE_PASS`,
thêm `REVIEWER_READONLY` cho Claude. Task khai mức cần trong `requires`; runner chỉ giao cho host PASS đủ.

> Capability là của **host được giao**, không chỉ của máy. Máy có Unity nhưng host implementer không có
> Unity MCP thì không được giao task Unity — nếu không PO lại thành người compile/capture hộ.

## 2. Viết task

Task = **packet prose** (`handoff/<wp>/tasks/<id>.md`, theo `templates/worker-packet.md`) +
**header máy đọc** (`handoff/<wp>/tasks/<id>.json`, schema `templates/schemas/task.schema.json`):

| Field | Ý nghĩa |
|---|---|
| `implementers` | thứ tự ưu tiên host (`codex`, `claude`) |
| `requires` | mức doctor host phải PASS, vd `UNITY_MCP_SMOKE_PASS` |
| `writeSet` | glob được phép sửa; ghi ngoài ⇒ `BLOCKED` |
| `acceptance` | `auto` / `review` / `manual` — reviewer chấm `auto`+`review`, bỏ `manual` |
| `targetRef` | target đã APPROVED, nếu có — worker/reviewer không được sửa |
| `baselineRef` | commit/tag **trước** phần việc; bắt buộc nếu muốn gọi failure là "có sẵn" |
| `mustReuse` | hệ thống có sẵn phải dùng lại |

Packet phải nói rõ thao tác **cấm** (vd menu ghi đè baseline) và dòng exit `RESULT:` / `EVIDENCE:` / `SUMMARY:`.

## 3. Chạy

```powershell
powershell -ExecutionPolicy Bypass -File .\tools\run-task.ps1 -Task handoff\<wp>\tasks\<id>.json -Commit
git push
```

- Capabilities thiếu/quá hạn ⇒ runner tự chạy doctor.
- Implementer và reviewer là **hai invocation riêng**; reviewer chỉ có Read/Grep/Glob và chỉ nhận packet, result,
  diff, evidence — không nhận reasoning của implementer.
- `changedFiles` do runner tính từ git (file dirty từ trước bị loại ra), không tin worker tự khai.
- `-Commit`: task PASS ⇒ commit code + evidence. Không PASS ⇒ runner in lệnh commit **chỉ evidence** và liệt kê
  code chưa review còn trên working tree. Runner không bao giờ push.

Output: `handoff/<wp>/runs/<id>/<timestamp>/` — prompt, `*.out.txt` (không dùng `.log`: `.gitignore` của Unity bỏ
qua), `rN.diff`, `rN.result.json`, `rN.review.json`, `status.json`, `unity-editor-log-tail.txt`.

## 3a. Xác minh bằng script (0 token)

Task chỉ gồm thao tác máy móc (compile, đếm lỗi console, chạy test, chạy menu, kiểm file) thì thêm khối `verify`:
runner gọi **thẳng Unity MCP qua HTTP** (`tools/UnityMcp.psm1`, URL lấy từ doctor), không gọi AI nào.

```json
"verify": {
  "onFail": "implementer",
  "steps": [
    { "do": "console-clear" },
    { "do": "refresh", "compile": true },
    { "do": "console", "types": ["error"], "maxCount": 0, "out": "handoff/<wp>/captures/console.txt" },
    { "do": "tests", "mode": "EditMode", "out": "handoff/<wp>/captures/tests.md" },
    { "do": "console-clear" },
    { "do": "menu", "path": "Game/Tool/Menu Item" },
    { "do": "console", "types": ["log"], "filter": "CSV:", "minCount": 1, "out": "handoff/<wp>/captures/parity.txt" },
    { "do": "console", "types": ["warning", "error"], "filter": "CSV:", "maxCount": 0, "out": "handoff/<wp>/captures/parity-fail.txt" },
    { "do": "files", "exist": ["handoff/<wp>/captures/a.csv"] }
  ]
}
```

- Kết luận PASS/FAIL nên dựa vào **log level** (tool log `Debug.Log` khi pass, `LogWarning` khi fail) và `minCount`/`maxCount`, không dựa vào text: `read_console` chỉ trả **dòng đầu** của log nhiều dòng. `expect` (regex) vẫn dùng được cho log một dòng; không khớp thì thử lại trên JSON thô.
- Tất cả bước PASS ⇒ `DONE_PENDING_FEEL` ngay, **không implementer, không reviewer** (acceptance là số đo được).
- Có bước FAIL ⇒ `onFail: implementer` (mặc định): giao implementer với báo cáo `r0.verify.md`; `stop` ⇒ `BLOCKED`.
- `-VerifyOnly`: chỉ chạy script, FAIL ⇒ `BLOCKED`, **không bao giờ gọi AI**; không hỏi y/N, không đòi host AI qua smoke (repo mới chỉ cần doctor thấy Unity MCP). `-Commit` vẫn commit evidence của run BLOCKED.
- Unity không trả lời khi đang compile/chạy test ⇒ mỗi lệnh tự thử lại (tới 10–15 phút); test job poll tới khi xong.
- Tool dùng (MCP for Unity v10): `refresh_unity`, `read_console`, `run_tests`, `get_test_job`, `execute_menu_item`.
- Pilot WP004-B-VERIFY: cùng việc này bằng agent tốn ~110k token Codex + ~$0.20 Claude.

## 3b. Duyệt trước khi giao + xem tiến độ

**Profile** (`config/run-profiles.json`, sửa được theo project):

| Profile | Dùng khi |
|---|---|
| `balanced` (mặc định) | thường ngày |
| `economy` | muốn tiết kiệm token/usage: effort thấp hơn, reviewer `sonnet`, 1 vòng sửa |
| `fast` | cần nhanh: Codex `service_tier=priority`, effort `medium` |
| `quality` | việc khó: effort `xhigh`, reviewer `opus`, 3 vòng sửa |

**Kiểm soát chi phí (bài học pilot: ~760k token Codex, model Sol dùng mà không ai được báo trước):**

- `confirmBeforeDispatch: true` là mặc định: runner luôn in kế hoạch và hỏi `y/N` (bỏ qua bằng `-Yes`).
- Model **không chỉ định** ⇒ cảnh báo trong terminal và trên trang kế hoạch, kèm model đo được ở lần trước trên máy này.
- **Model khả dụng khác nhau theo tài khoản** (máy công ty có Luna, máy nhà chỉ có Sol): ghi model theo máy vào
  `config/run-profiles.local.json` (gitignored, ghi đè từng khoá lên `run-profiles.json`), ví dụ
  `{"profiles":{"economy":{"implementer":{"codex":{"model":"<model rẻ của tài khoản này>"}}}}}`.
- `budget.maxTokensPerRun`: đã vượt thì **không mở vòng sửa kế tiếp** (`BLOCKED_BUDGET`). `reviewerMaxUsd` /
  `implementerMaxUsd` ⇒ `--max-budget-usd` cho Claude. Codex không có cờ giới hạn trong một lần gọi.
- **Vòng sửa tiếp tục phiên implementer cũ** (`codex exec resume <id>` / `claude -p --resume <id>`): không đọc lại
  packet, AGENTS, code từ đầu. Pilot: vòng 2 bằng phiên mới tốn 132k token chỉ để sửa 1 dòng. Reviewer **luôn phiên mới**
  (độc lập). Không mở lại được phiên ⇒ tự chạy lại vòng đó bằng phiên mới. Tắt: `-NoResume` hoặc `"resumeOnPatch": false`
  trong profile. Dashboard đánh dấu bước tiếp phiên bằng `↻`.
- Runner tự chạy doctor với `-SkipSmoke` (không gọi model); doctor giữ kết quả smoke PASS ≤ 7 ngày. Chỉ chạy
  `doctor.ps1` đầy đủ khi đổi máy/đổi cấu hình host.

`null` trong profile = để host tự chọn (theo `~/.codex/config.toml` / setting Claude).
Ghi đè từng lần: `-Profile`, `-Implementer`, `-ImplementerModel`, `-ImplementerEffort`, `-CodexServiceTier`,
`-ReviewerModel`, `-ReviewerEffort`, `-MaxPatchRounds`. Claude CLI chưa có cờ tăng tốc; "nhanh" hiện chỉ áp cho Codex.

**Xem kế hoạch, không chạy gì:**

```powershell
.\tools\run-task.ps1 -Task handoff\<wp>\tasks\<id>.json -Plan
```

Mở `handoff/<wp>/runs/<id>/_plan/plan.html`: luồng giao việc (agent · model · effort · tier · số vòng sửa),
nút chọn profile, ô chỉnh từng thông số, biểu đồ ước tính token/chi phí/thời gian theo lịch sử, và **lệnh đã
ghép sẵn** để copy. `-Confirm` hiện kế hoạch rồi hỏi `y/N` ngay trong terminal; đặt `"confirmBeforeDispatch": true`
trong config để luôn hỏi (bỏ qua một lần bằng `-Yes`).

**Tiến độ:** `handoff/<wp>/runs/dashboard.html` — runner ghi lại sau mỗi bước, trang tự làm mới 5 giây khi có
task đang chạy: số task đang chạy/đạt, số lần người can thiệp, tổng token và chi phí; mỗi task là một dòng thời gian
`V1 implementer → V1 reviewer (PATCH) → V2 …`, bước đang chạy nhấp nháy. Trên Windows runner tự mở trang
(tắt bằng `-NoOpen`).

Usage lấy từ output thật: Claude `--output-format json` (token, `total_cost_usd`, thời gian, model); Codex header
`model:` / `reasoning effort:` và dòng `tokens used`. Token Claude không tính cache read (rẻ, ghi riêng
`cacheReadTokens`); Codex không báo chi phí USD.

## 4. Kết quả

| Status | Nghĩa | Ai làm tiếp |
|---|---|---|
| `DONE_PENDING_FEEL` | reviewer PASS mọi item `auto`/`review` | PO: item `manual` (feel, thiết bị) |
| `PATCH` (nội bộ) | runner tự giao lại implementer kèm findings, tối đa `-MaxPatchRounds` (2) | runner |
| `ESCALATE:LOOP_CAP` | vẫn PATCH sau 2 vòng | PO |
| `BLOCKED_ON_TARGET` | reviewer `TARGET_RECONSIDER` **có** `implHypothesesRuledOut` | PO + director |
| `BLOCKED` | implementer/reviewer chặn, ghi ngoài writeSet, JSON review hỏng, hoặc TARGET_RECONSIDER thiếu evidence | đọc `status.json` |
| `BLOCKED_TOOLCHAIN` | không host nào đủ `requires` | doctor in bước sửa |
| `BLOCKED_QUOTA` | host hết quota/rate limit (vd ChatGPT "usage limit … try again at …"); reviewer không được gọi | chờ tới giờ ghi trong lý do rồi chạy lại, hoặc `-Implementer claude` |

Mọi lần cần người ⇒ một dòng trong `handoff/<wp>/interventions.jsonl`. Đó là KPI:
**số lần PO phải can thiệp từ Goal tới playable/final review**.

## 5. Luật reviewer (tóm tắt)

- PASS chỉ khi mọi item `auto` + `review` có evidence.
- "Failure có sẵn" chỉ được chấp nhận với evidence tại `baselineRef`. Base commit của runner **đã chứa**
  phần việc đang kiểm — fail ở đó không chứng minh gì.
- Test mâu thuẫn với luật đã khoá trong packet = test lỗi thời ⇒ PATCH cập nhật test theo luật.
- Không bao giờ đề xuất đổi target đã APPROVED như một PATCH.

## 6. Parallel

Nhiều task được ở `EXECUTABLE` cùng lúc (`handoff/ROADMAP.md`). Chạy đồng thời chỉ khi dependency DONE,
`writeSet` rời nhau và tối đa một task cần Unity. V1 runner chạy từng task; song song là việc của V2.

## 7. Bài học đã trả giá (pilot WP004, 2026-09-29/30)

| Triệu chứng | Nguyên nhân | Đã chặn bằng |
|---|---|---|
| PO compile/capture hộ | Claude CLI không có Unity MCP | capability theo host |
| Implementer bỏ cuộc khi chạy test | Unity bận main thread, MCP không trả ping | prompt: chờ + poll lại tới 20 phút |
| Log không lên git | Unity `.gitignore` bỏ `*.log` | đuôi `*.out.txt` |
| PASS sai "failure có sẵn" | baseline = HEAD đã chứa phần việc | `baselineRef` |
| Fix đã PASS mất khi đổi máy | chỉ commit evidence, không commit code | `-Commit` + lệnh commit in cuối run |
| Runner chặn chỉ vì qua đêm | capabilities quá hạn | runner tự chạy doctor |
| Doctor dừng ở máy mới | installer đặt `ErrorActionPreference=Stop`; stderr lẫn JSON | installer process riêng, tách JSON |
| Script hỏng trên PS 5.1 | `.ps1` UTF-8 không BOM có tiếng Việt | `tools/*.ps1` chỉ ASCII (lint `PS1_NON_ASCII`) |
| Runner tưởng implementer đã xong khi nó hết quota giữa chừng | không có tin nhắn cuối → runner đọc log thô, trong đó có dòng `RESULT:` của prompt mẫu | chỉ tin tin nhắn cuối (`-o`); chặn dòng mẫu; trạng thái `BLOCKED_QUOTA` |
| Codex mất shell (`Access is denied`) | sandbox Windows không spawn được `pwsh` bản Microsoft Store | doctor WARN `SANDBOX_SHELL`: cài PowerShell 7 bản MSI hoặc `-CodexSandbox danger-full-access` |
| Script verify lần đầu trên Unity thật: `run_tests` "không trả job_id", console "0 dòng" | FastMCP bọc kết quả trong `{"result": {...}}`, còn server giả trả thẳng | module bóc `result`; đọc console mà sai định dạng thì báo lỗi, không coi là 0 dòng (nếu coi là 0 dòng thì `maxCount 0` sẽ PASS giả) |
| Script verify: parity "không khớp `(fail 0)`" dù parity 38/38 PASS | `read_console` chỉ trả dòng đầu (`[MotionParity]`) của log nhiều dòng | kiểm bằng log level: `log` có ≥1 dòng (`minCount`), `warning/error` có 0 dòng |
