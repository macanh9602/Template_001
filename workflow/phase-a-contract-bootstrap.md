# Phase A — Contract & Bootstrap (Director + Critic + PO)

> Orchestrator cho bước đầu của game mới hoặc project adopt Template, khi đã có input (GDD, **executable
> prototype `.html`**, reference runtime). Không thay thế tài liệu nào; chỉ xếp thứ tự và thêm **gate Critic trước lock**.
>
> | Việc | Tài liệu sở hữu |
> |---|---|
> | Audit readiness | `handoff/PROJECT-READINESS-PROMPT.md` (chạy như subtask bên trong Phase A) |
> | Authority Matrix, observable rules, conformance | `workflow/prototype-to-contract.md` |
> | Thứ tự bootstrap/adopt | `playbooks/p1-bootstrap.md` |
> | Sửa repo cơ học | `workflow/bootstrap-patch.md` + `templates/project-bootstrap.ps1` |
> | Implement Unity sau lock | `workflow/run-task.md` (`tools/run-task.ps1`) |
>
> Không sửa `AGENTS.md` / `standards/` để đổi nhịp workflow. Prompt: `handoff/PHASE-A-DIRECTOR-PROMPT.md`,
> `handoff/PHASE-A-CRITIC-PROMPT.md`. Hướng dẫn từng bước cho người: §8.

---

## 1. Vai trò

| Vai | Làm | Không làm |
|---|---|---|
| **Director** (phiên agent planning) | audit, Authority Matrix, câu hỏi PO + visualiser, full-file Docs, conformance, bootstrap manifest, packet đầu tiên | sửa `Assets/`, commit/push, tự lock thay PO, tự sửa prototype Core |
| **Critic** (phiên agent riêng, độc lập) | baseline độc lập **trước** khi đọc draft; review mọi artifact trước lock | sửa artifact của Director, viết code, chốt decision |
| **PO** | trả lời câu hỏi, lock, chạy DryRun/Apply, chạy runner, chơi thử | chép tay output của agent vào nhiều nơi |
| **Implementer + Code Reviewer** | chỉ sau gate, qua `tools/run-task.ps1` | đọc `.html` để đoán luật |

Critic (trước lock, review **contract**) ≠ Code Reviewer của runner (sau implement, review **diff + evidence**).

---

## 2. Trạng thái — dùng enum chuẩn `AGENTS.md §7.5`

Phase A không thêm enum mới. Mỗi artifact mang `status` §7.5 + `reason` khi cần:

| Giai đoạn | `status` | `reason` |
|---|---|---|
| Director vừa xuất draft | `PLANNED` | `AWAITING_CRITIC` và/hoặc `AWAITING_PO` |
| Còn BLOCKER/MAJOR, hoặc PO chưa xem visualiser | `BLOCKED` | finding ID / câu hỏi cụ thể |
| Review sạch + PO đã chọn (chưa ghi lock) | `PLANNED` | `LOCKABLE` |
| PO lock | `LOCKED` | — |
| Manifest reviewed, PO đang DryRun/Apply | `IMPLEMENTING` | `BOOTSTRAP_APPLY` |
| Đã Apply, chưa compile/console | `IMPLEMENTED` | `APPLIED_UNVERIFIED` |
| Compile/console/conformance đang chạy | `VERIFYING` | — |
| Gate readiness PASS có evidence | `DONE` | → materialize packet kế tiếp |

`IMPLEMENTED != DONE`. Chưa chạy ⇒ `NOT RUN`, không ghi PASS.

---

## 3. A — Audit + draft (Director, một lượt)

1. `PROJECT READINESS` đúng format `PROJECT-READINESS-PROMPT.md`.
2. Authority Matrix theo area (`templates/prototype-authority-contract.md`), parity YES/NO.
3. OBSERVABLE RULES / DATA / AUTHORING / AMBIGUITIES, trích `file:dòng` cho mỗi claim semantic. Tách:
   stated intent · executable behavior · **incidental ordering** (thứ tự duyệt mảng, threshold, timer tình cờ) ·
   contradiction · proposed Unity architecture.
4. Câu hỏi PO: chỉ gameplay semantics / data-authoring / architecture / performance khó đảo / scope.
   Tối đa 4 câu mỗi batch, theo `workflow/ask-and-visualise.md`.
5. Câu spatial/motion/timing ⇒ **visualiser `.html` thật** (phân biệt hành vi oracle thật vs đề xuất) trước khi xin lock.
6. Đề xuất Conformance Pack: pin hash/version của Core JS, schema input/expected, case positive/negative,
   tách characterization (hành vi hiện có) vs normative (đã lock).

Output (đều `PLANNED`): `reference/prototype-authority.md`, `handoff/_drafts/A-readiness.md`,
`handoff/_drafts/A-questions.md`, `handoff/visual/A-*.html` nếu có câu spatial.

## 4. B — Critic và PO song song

- Critic viết `handoff/_reviews/_critic-baseline.md` **trước** khi mở draft, rồi review từng artifact theo 4 trục:
  Fidelity · Guardrail · Executable · Question.
- PO trả lời câu hỏi ngay khi thấy recommendation; câu visual chỉ chốt sau khi đã xem visualiser.
- Director trả lời **từng** finding: `ACCEPT` + sửa / `REJECT` + evidence / `NEED-PO`; xuất lại **full file**.
- Finding làm đổi căn cứ một lựa chọn PO ⇒ chỉ hỏi lại đúng câu đó.
- Critic vòng sau **chỉ** xem finding còn mở + dòng đã đổi (không re-audit).
- Không lock khi còn BLOCKER/MAJOR chạm semantic/data/architecture/scope.

## 5. C — Consolidate + lock

Director xuất full file: `Docs/project-context.md`, `Docs/data-model.md`, `Docs/runtime-architecture.md`,
`Docs/decision-log.md`, `Docs/glossary.md` (nếu có từ mới), `handoff/ROADMAP.md` (capability map + **chỉ**
story executable kế tiếp), `reference/prototype-authority.md`, `reference/conformance/`.

- Tách authoring source-of-truth / generated / runtime; serialization version + migration.
- Event order, validation, expected outcome đủ để worker **không cần mở `.html`**.
- PO chọn khác Core ⇒ ghi deviation rõ (oracle version vs locked rule), không sửa golden âm thầm.
- Critic review bản consolidated; PASS conformance chỉ khi generator/test đã chạy thật.
- PO ghi lock vào `Docs/decision-log.md` ⇒ `LOCKED`.

## 6. D — Bootstrap cơ học

1. Director lập `handoff/bootstrap/bootstrap-manifest.json` (`manifestVersion: 1`, `operations[]` add/replace/delete,
   `contentBase64`, `expectedBeforeSha256` cho replace/delete). Không base64 an toàn được ⇒ xuất payload + `BLOCKED`, không giả vờ runnable.
2. Critic review manifest: đúng scope, hash, không đụng code semantic trong `Assets/`, không tạo root/asmdef song song, không rename hàng loạt.
3. PO chạy:

   ```powershell
   .\templates\project-bootstrap.ps1 -ProjectRoot . -ManifestPath .\handoff\bootstrap\bootstrap-manifest.json -DryRun
   .\templates\project-bootstrap.ps1 -ProjectRoot . -ManifestPath .\handoff\bootstrap\bootstrap-manifest.json
   ```

   DryRun báo file bẩn (`TOUCHES-DIRTY`) và thao tác sẽ fail (`WOULD-FAIL`) nhưng không chặn. Apply đòi working tree sạch:
   commit prototype `.html` trước (nó là oracle, phải được pin). **Không** dùng `-Force` để né guard.
4. Sau Apply: `git diff --check`, Unity compile/console đúng project ⇒ `VERIFYING` → `DONE`.

C# runtime/gameplay, owner lifecycle mới, tích hợp scene có semantics ⇒ **foundation worker packet** sau lock, không giấu trong `.ps1`.

## 7. E — Gate sang Phase B

`READY FOR ACTIVE STORY: YES` (gate của `PROJECT-READINESS-PROMPT.md`) + bootstrap `DONE` ⇒ Director materialize
**một** packet (`templates/worker-packet.md` + `.json`), Critic review, PO chạy `tools/run-task.ps1`.
Runner tự gọi implementer + code reviewer; không prompt tay.

---

## 8. Hướng dẫn cho người (PO/GD) — kickoff khi đã có `.html`

| # | Ai | Làm |
|---|---|---|
| 1 | PO | Tạo repo từ Template (`tools/new-game.ps1`), `tools/doctor.ps1` không còn FAIL |
| 2 | PO | Chép prototype vào `reference/prototype/<Ten>.html`, **commit** (pin oracle) |
| 3 | PO | Mở phiên **Director**, dán `handoff/PHASE-A-DIRECTOR-PROMPT.md` (điền tên game + path html) |
| 4 | PO | Mở phiên **Critic** riêng, dán `handoff/PHASE-A-CRITIC-PROMPT.md` — nó làm baseline trong lúc Director audit |
| 5 | PO | Director xong draft ⇒ trả lời `A-questions.md` (xem visualiser trước câu visual); nhắn Critic `review draft` |
| 6 | PO | Nhắn Director `đã có review` ⇒ Director xử lý finding. Lặp 5–6 tới khi không còn BLOCKER/MAJOR |
| 7 | PO | Lock ⇒ Director xuất Docs + manifest ⇒ Critic review manifest |
| 8 | PO | DryRun → xem → Apply → mở Unity xem console |
| 9 | PO | Director xuất packet đầu tiên ⇒ `tools/run-task.ps1 -Task ... -Plan`, rồi `-Confirm` |

Chi phí: Critic chỉ đọc artifact đang review + file nó trích; vòng 2+ chỉ đọc phần đổi. Một batch câu hỏi PO, không hỏi lẻ.
