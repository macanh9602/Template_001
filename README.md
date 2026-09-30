# Mobile Game Template — Code + Agent Pack

Template dùng chung cho mobile game Unity, gồm hai nửa:

| Nửa | Nội dung | Vai trò |
|---|---|---|
| `Assets/` | reusable modules/plugins + `_Core` project skeleton | runtime/editor baseline |
| Pack ở root | `AGENTS.md`, `standards/`, `workflow/`, `Docs/`, `skills/`, `playbooks/`, `templates/`, `knowledge/`, `handoff/` | cách agent làm việc |

Mục tiêu: **giảm rework khi mở project mới hoặc adopt project hiện hữu**, bằng cách chốt project contract trước khi agent tạo architecture/folder/story downstream.

---

## Hai mode bootstrap

### 1. `GREENFIELD`

Repo thật sự mới, chưa có runtime ownership/code structure đáng giữ.

- Default production script root của Template: `Assets/_Core/4_Scripts`.
- Dùng standard làm baseline.
- Chỉ tạo folder/class có responsibility thật.

### 2. `EXISTING_PROJECT_ADOPTION`

Project đã có code/scene/prefab/module/package/flow.

- **Inspect → harvest → confirm contract → xử lý adoption gaps.**
- Project thật thắng skeleton generic.
- Không tạo root song song chỉ vì path default khác.
- Không tách asmdef trước khi audit dependency.
- Không migrate folder/namespace trong bootstrap nếu chưa có decision riêng.

Bắt đầu bằng `handoff/PROJECT-READINESS-PROMPT.md`.

---

## Standard project-start flow

Nếu có executable prototype / external GD tool / solver:

```text
GD input
→ workflow/prototype-to-contract.md
→ Authority Contract
→ canonical data + Conformance Pack
→ Project Readiness
→ Bootstrap Manifest
→ generated/transient .ps1
→ apply + compile/review
→ Big Phase capability map
→ exactly one EXECUTABLE Worker Packet
→ lower-reasoning agent
```

Frontier model dùng reasoning cho contract/decision/phase; `.ps1` gánh mechanical add/replace/delete/scaffold;
lower worker tập trung implementation packet hiện tại.

Generated project-specific `.ps1` được delete sau closure/commit. `templates/project-bootstrap.ps1` là reusable engine nên giữ.

## Bootstrap flow

```text
0. Copy Template hoặc mở project đã adopt Template
1. Chạy handoff/PROJECT-READINESS-PROMPT.md
2. Resolve:
   - project mode
   - canonical script/prefab/scene roots
   - namespace
   - assembly reality/strategy
   - scene ownership
   - data source of truth
   - runtime authority
3. Ghi contract vào Docs/project-context.md + decision-log
4. Chạy playbooks/p1-bootstrap.md
5. Chỉ implement foundation gap cần thiết
6. Roadmap mô tả capability/phases
7. Chỉ materialize story executable kế tiếp sau gate PASS
```

---

## Cấu trúc pack

```text
AGENTS.md
CLAUDE.md
PACK-VERSION.md

standards/
  system-design.md
  folder-structure.md
  code-style.md
  performance-budget.md
  anti-patterns.md

workflow/
  loop.md
  ask-and-visualise.md
  decisions.md
  verification.md
  harvest.md
  run-task.md          doctor + runner + task/result/review
  visual-direction.md  lab → proposal → PO promote → Import → visual-review

tools/
  doctor.ps1           capability máy + từng agent host (Unity MCP headless thật)
  run-task.ps1         task.json → implementer → git diff → reviewer read-only → PATCH/PASS
  template-lint.ps1    chặn root song song, meta mồ côi, path máy, BOM, ref gãy, package trôi
  run-dashboard.html   trang duyệt kế hoạch + tiến độ agent (runner tự điền dữ liệu)
  run-batch.ps1        nhiều task song song có giới hạn: dependsOn, writeSet rời nhau, ≤ 1 luồng Unity
  run-report.ps1       tổng hợp mọi run: token, tiền, thời gian, trạng thái + tín hiệu harvest → handoff/_reports/run-report.html
  promote-direction.ps1  PO promote visual direction theo section (look / motion / fx)
  direction-delta.ps1  direction vN → vN+1: đổi gì, Profile nào Import lại, task nào STALE
  new-game.ps1         mở game mới từ Template: tên, code, namespace, bundle id, dọn phần riêng của Template
  sync-skills.ps1      sinh .claude/skills/ từ skills/ (canonical); -Check báo lệch
  ReuseRegistry.psm1   quét code viết lại hệ thống có sẵn (Docs/reuse-registry.json) cho lint + runner
  blender/             smoke_export.py (doctor -Blender) + mesh_report.py (verify step 'blender', 0 token)
  UnityMcp.psm1        gọi Unity MCP từ script (verify 0 token)

config/
  run-profiles.json    profile balanced / economy / fast / quality: model, effort, tier, số vòng sửa

Docs/
  project-context.md
  runtime-architecture.md
  data-model.md
  decision-log.md
  glossary.md

skills/
playbooks/
templates/
knowledge/

handoff/
  PROJECT-READINESS-PROMPT.md
  ROADMAP.md
  START-PROMPT.md
  RUN-STORY-PROMPT.md
  story-xxx/
```

---

## Canonical-root principle

Template hiện có code ở:

```text
Assets/_Core/4_Scripts
```

Vì vậy greenfield copy từ Template mặc định giữ root này.

Nếu project hiện hữu dùng root khác thì **không tự tạo root thứ hai**. Ghi root thật vào `Docs/project-context.md`. Migration, nếu cần, là story riêng.

Ví dụ lỗi cần tránh:

```text
Assets/_Core/4_Scripts   ← code/flow đang chạy
Assets/_Core/Scripts     ← agent tạo thêm vì standard cũ
```

Điều này tạo hai nơi ownership song song và thường dẫn tới port ngược + sửa docs + sửa scene.

---

## Roadmap: capability trước, story sau

Roadmap được phép biết xa, nhưng execution spec chỉ materialize gần:

```text
Readiness PASS
  ↓
Foundation story
  ↓ PASS
Technical slice story
  ↓ PASS
Core loop story
  ↓ ...
```

Không viết sẵn 10–15 story chi tiết khi architecture/foundation chưa verify.

---

## Bảy nguyên tắc

1. **Project reality before template assumption.** Existing project phải inspect trước khi tạo structure mới.
2. **Một canonical production root.** Không framework tree song song.
3. **Decision đi trước hard-to-reverse migration.** Folder/asmdef/scene/data ownership là project contract.
4. **Story materialization lười.** Chỉ story executable kế tiếp sau gate.
5. **Số liệu không nằm trong code.** Prefab field / Profile SO / level data.
6. **Verify trung thực.** PASS chỉ khi đã chạy/evidence thật.
7. **Harvest ngược về Template.** Bug/rework lặp lại phải sửa pack, không trả lại ở project sau.
8. **Prototype authority theo area.** Gameplay/data/oracle/reference tách riêng; không copy nguyên prototype thành Unity contract.
9. **Authoring owner là project decision.** External tool/Unity Editor/generator đều first-class; runtime chỉ đọc canonical data.
10. **Reasoning ở frontier, mechanics ở script, execution ở worker.** Generated updater transient; manifest/conformance/Docs persistent.

---

## Chạy task không cần PO làm trung gian

Game mới: `tools/new-game.ps1 -Name ... -Code ... -Namespace ... -BundleId ...`. Máy mới: `tools/doctor.ps1 -Repair`. Task: `tools/run-task.ps1 -Task handoff/<wp>/tasks/<id>.json -Commit`. Chi phí + harvest: `tools/run-report.ps1`.
Trước khi publish/merge pack: `tools/template-lint.ps1`. Chi tiết: `workflow/run-task.md`.

---

## Feedback loop

Cuối project, chạy `workflow/harvest.md` và đặc biệt hỏi:

- Template assumption nào đã khác project thật?
- Folder/root/asmdef decision nào bị sửa lại?
- Story nào bị supersede vì architecture chốt quá muộn?
- Có dependency/reference nào từng bị coi là blocker nhưng thực ra không cần?

Nếu lặp lại ở project thứ hai, update `standards/`/`playbooks/` thay vì workaround riêng từng repo.
