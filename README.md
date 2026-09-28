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

---

## Feedback loop

Cuối project, chạy `workflow/harvest.md` và đặc biệt hỏi:

- Template assumption nào đã khác project thật?
- Folder/root/asmdef decision nào bị sửa lại?
- Story nào bị supersede vì architecture chốt quá muộn?
- Có dependency/reference nào từng bị coi là blocker nhưng thực ra không cần?

Nếu lặp lại ở project thứ hai, update `standards/`/`playbooks/` thay vì workaround riêng từng repo.
