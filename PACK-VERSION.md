# Pack version

## v5.0 (branch `claude/new-session-7wx9q1`, chưa release) — 2026-09-30

**Execution core: PO không còn là message broker** — review `claude_workflow_review_pack_v3` + pilot thật trên
`Ducan_SpeedRun_Demo` WP004 (2 máy, 5 run).

- `tools/doctor.ps1`: capability theo **máy và từng agent host** (`INSTALLED → AUTHENTICATED → UNITY_MCP_CONFIGURED →
  UNITY_MCP_SMOKE_PASS`, `REVIEWER_READONLY`), gọi Unity MCP headless thật. `-Repair` cài Claude/Codex CLI, đăng ký
  Unity MCP cho Claude từ entry của Codex. Output `.toolchain/capabilities.json` (gitignored).
- `tools/run-task.ps1`: một task end-to-end — implementer headless → `changedFiles` từ git → reviewer read-only
  (invocation riêng) → PATCH tối đa 2 vòng → `DONE_PENDING_FEEL` / `ESCALATE:LOOP_CAP` / `BLOCKED*`. Mọi lần cần người
  ghi `handoff/<wp>/interventions.jsonl`. `-Commit` commit code + evidence khi PASS.
- `templates/schemas/{task,result,review}.schema.json`; `task.json` là header máy đọc của `worker-packet.md`.
  `baselineRef` bắt buộc cho mọi claim "failure có sẵn".
- `tools/template-lint.ps1` + dọn Template: path `_Core/Scripts/Diagnostics` cũ, 2 `.cs` 0 byte, 15 folder chỉ còn
  `.meta` (thêm `.gitkeep`), `Docs/asset-pipeline.json` → `.example.json` + gitignore (bỏ path `D:/…`, `models_dir` →
  `_Core/2_Models`), pin `com.coplaydev.unity-mcp#v10.0.0`.
- `handoff/ROADMAP.md`: nhiều task được `EXECUTABLE`; chạy đồng thời chỉ khi dependency DONE, writeSet rời nhau,
  tối đa một task cần Unity.
- Chưa có (V2): DAG scheduler, worktree/lock, harvest `visual-lab`/`visual-review`/importer/capture runner từ Demo,
  reuse registry, board generated. Xem `workflow/run-task.md §7` cho bài học pilot.

---

## v4.6 — 2026-09-28

**Art placeholder từ GD HTML — `skills/art-gen/`**

- Thêm `skills/art-gen/` + refs (`art-manifest.md`, `gen-2d.md`, `export-3d.md`): HTML GD → art manifest (model trích, GD duyệt, HARD GATE) → style lock → gen → intake deterministic.
- Phân loại element theo `kind`; element có shape/size theo level data → gen **mảnh** (tileset / cap-body), Unity lắp. Biến thiên không phải cell/độ dài một trục → `procedural` → technical-slice.
- 2D: image model gen từng element (batch ≤ 10, cùng group, style anchor); **không** để model ghép atlas/đặt tên cuối — script validate, SpriteAtlas do Unity pack.
- 3D: prop tĩnh dựng trong three.js → `.glb` (một mesh, palette texture chung, một material slot) → `asset-intake` (D-005). Unity không gen lại mesh từ data HTML.
- Phase −1 Normalize (`refs/normalize.md`): GD không phải đổi cách vibe; model tạo `<name>.art.html` + `art-extract.json` từ bản gốc read-only, regenerate khi hash đổi, parity gate bằng screenshot diff (`parity.py`). Chỉ xin GD ảnh style ref.
- Chưa có: script intake 2D, Postprocessor sprite sidecar, Unity assembler cho modular — là story riêng.

---

## v4.5.1 — 2026-09-28

**Bootstrap workflow repair**

- `templates/worker-packet.md` ghi explicit `IMPLEMENTED != DONE` để verifier và worker dùng cùng invariant.
- `CLAUDE.md` ghi rõ hard entry gate thắng rule generic `PENDING MANUAL`.
- `templates/project-bootstrap.ps1` persist `bootstrap-report-<timestamp>.md` mặc định, thay vì chỉ giữ report trong memory.

---

## v4.5 — 2026-09-28

**Prototype → contract → bootstrap automation → worker packet workflow**

- Thêm `workflow/prototype-to-contract.md` + `templates/prototype-authority-contract.md`: executable prototype/external tool được classify theo từng authority area, không copy nguyên file thành contract.
- Thêm `skills/authoring-pipeline/` + `playbooks/p4-authoring-pipeline.md`: GD authoring có thể là external tool, Unity Editor, generator hoặc manual data; Unity LE không còn là default assumption.
- Thêm `templates/conformance-pack/`: executable engine/solver/evaluator có thể trở thành semantic oracle cho Unity tests.
- Thêm `templates/bootstrap-manifest.json`, `templates/project-bootstrap.ps1`, `workflow/bootstrap-patch.md`: frontier model quyết định add/replace/delete/preserve; script làm mechanical work deterministic.
- Generated project-specific `.ps1` là transient; canonical Docs + manifest/report/conformance là persistent.
- Thêm `templates/worker-packet.md` cho lower-reasoning worker: locked semantics, hard entry gate, baseline, conformance và forbidden scope rõ.
- Chuẩn hoá execution status: `PLANNED / LOCKED / EXECUTABLE / IMPLEMENTING / IMPLEMENTED / VERIFYING / BLOCKED / DONE / SUPERSEDED`; `IMPLEMENTED != DONE`.
- Hard entry gate có precedence cao hơn rule generic `PENDING MANUAL`: dependency gate PENDING ⇒ BLOCKED, không implement downstream.
- P1/readiness/project-context bổ sung Product Input, Level Authoring Mode, authority/oracle và bootstrap-manifest flow.

---

## v4.4 — 2026-09-28

**Bootstrap/adoption anti-rework pass — harvest từ SE-001 foundation**

- Thêm `handoff/PROJECT-READINESS-PROMPT.md`: audit-only gate trước active story đầu tiên.
- `playbooks/p1-bootstrap.md` tách rõ `GREENFIELD` và `EXISTING_PROJECT_ADOPTION`.
- Canonical root default đồng bộ với asset thật của Template: `Assets/_Core/4_Scripts`.
- Cấm tự tạo framework root song song (`_Core/Scripts` + `_Core/4_Scripts`). Folder/namespace/asmdef migration phải là decision/story riêng.
- Assembly strategy đổi từ skeleton-first sang dependency-first: project đang compile trong `Assembly-CSharp` không bị ép tách asmdef sớm.
- `Docs/project-context.md` bổ sung project mode, canonical roots, assembly reality/strategy, existing-code policy, external-reference policy và adoption gaps.
- Roadmap dùng capability/gate và **lazy story materialization**: chỉ story executable kế tiếp được viết đầy đủ sau khi gate trước PASS.
- External `.zip`/reference asset không còn là blocker mặc định; chỉ bắt buộc khi user định nghĩa nó là authoritative source.
- `START-PROMPT.md` route bootstrap/adoption qua readiness gate trước khi implement.

### Rework pattern được chặn

```text
standard path ≠ project thật
→ agent tạo tree mới
→ foundation implement
→ project owner sửa canonical root/architecture
→ port code ngược + rewrite docs + supersede story pack
```

v4.4 biến bước đầu thành:

```text
inspect project thật
→ lock canonical contract
→ fill adoption gaps
→ foundation gate
→ materialize story kế tiếp
```

---

## v4.3 — 2026-09-15

**Claude / cross-agent execution adapter**
- Review `CLAUDE.md` giữa `in006-cake-roll` và Template: giữ model host-adapter, nhưng harvest story-first/autonomy/targeted inspection/Unity session behavior từ in006.
- `CLAUDE.md` mới không duplicate architecture; `@AGENTS.md` vẫn là constitution, `skills/` vẫn canonical source dùng chung agent.
- Thêm routing project skill → Unity official/native skill (`ui`/`ui-uitk`/`ui-ugui`/`unity-cli`/Editor Search khi khả dụng), tránh fork workflow Unity vào skill riêng.
- Chuẩn hoá verification: agent tự compile/console/targeted test khi hữu ích; manual gameplay/feel/GD acceptance do user thực hiện và ghi `PENDING MANUAL` nếu chưa chạy.
- Debug workflow được đưa cả vào Claude adapter: runtime bug chưa proven → tự instrument `AgentAudit/*.md`; user chỉ repro và báo `đã repro`; agent tự đọc file và tìm first divergence.
- Đồng bộ `AGENTS.md §7` để debug-audit trigger **trước speculative fix**, không còn chờ "fix lần 2".

---

## v4.2 — 2026-09-15

**Debug workflow**
- `debug-audit` v2: agent tự instrument → dev chỉ manual repro → agent tự đọc `AgentAudit/*.md`; file-only, per-repro overwrite, transition logging, record budget, no console spam.
- Thêm reusable `Assets/_Core/Scripts/Diagnostics/AgentDebugAudit.cs` và ignore `/AgentAudit/`.

**Level Editor UX harvest từ in006 Story 010/013/014/015/020/029**
- Thêm `knowledge/editor-ux/responsive-workspace.md`.
- Siết input/focus/undo, callback lifecycle, stable selection, domain-reload state, split panes, collapse/responsive low-width, scroll ownership, geometry-aware chart/layout và toolbar information architecture.
- Checklist Level Editor có acceptance cho ~640px window, splitter/pane persistence, `GeometryChangedEvent`, horizontal scroll + pinned anchor, hidden/ghost/editable selection semantics, Play Test parity.
- Anti-patterns C16–C22 harvest từ bug thật thay vì guideline giả định.

---

## v4.1 — 2026-09-15

Harvest từ `in006-cake-roll` tới story 042, tập trung vào scale nhiều project thay vì copy game-specific logic.

**Structural fix**
- Đổi kho reusable `library/` → **`knowledge/`** để không xung đột với Unity `Library/` trên Windows và không bị `.c2cignore` nuốt khỏi MCP.
- Sửa toàn bộ reference trong skills/playbooks/workflow/templates.

**Shared skills**
- `skills/` là canonical source of truth cho Codex/Claude/agent khác; agent-specific adapter chỉ route/sync, không fork skill body.
- Frontmatter `description` là trigger metadata; không assume mọi host tự discover một root path tuỳ ý.

**Agent workflow**
- Story rõ ⇒ agent implement ngay, **không chờ confirm / không re-plan**. Local reversible decision tự chốt; chỉ escalate project contract, gameplay semantics, data compatibility, hard-to-reverse performance, scope.
- `ask-and-visualise` chỉ yêu cầu confirm khi assumption thật sự có thể đổi hướng.

**Presentation lifecycle**
- Thêm skill `presentation-lifecycle`: identity · ownership · generation · readiness · handoff.
- Chuẩn hoá `logical reservation != physical presentation readiness`; cấm magic-delay sync.
- Async completion chỉ final-write nếu vẫn là active owner; final handoff re-resolve/validate stable id/sequence.
- Pool lifecycle hai đầu: Release cleanup + Acquire full rebind.
- Harvest anti-pattern B16–B20 từ queue/outline/stale-task/exact-sequence bugs của in006.

**Reusable runtime fixes**
- `ObjectPool`: `SetParent(parent, false)`, restore prefab localScale, safe null/recycle khi pool đã destroy, clear static instance trong `OnDestroy`.
- `Effect/EffectsProfile`: particle burst density thành prefab tunable, bỏ hardcode `10f`, thêm prewarm/null-safe lookup, fix box-shape Z scale.
- `MiniGameCamera`: bỏ runtime debug log khỏi aspect check.

**Template contract cleanup**
- Không hard-require legacy `MDATools/UIBase` khi asset đó không nằm trong template; UI framework là project choice nhưng gameplay vẫn chỉ nói qua presenter/bridge.

---

## v4.0 — 2026-08-19

Viết lại từ đầu, chưng cất từ `in006-cake-roll` (30 story · 62 decision entry · 22 implementation
notes · 5 GD-facing doc) + `unity-skills/ship-level-design` + `unity-skills/game-design-advisor` +
`Order Generation Documentation` (ch003-my-dream-cafe).

**Cấu trúc**
- Tách `standards/` (hằng số) và `workflow/` (cách làm việc) khỏi `Docs/` (điền mỗi game).
  Trước đây trộn chung nên không phân biệt được file nào được sửa.
- Skill chuyển sang chuẩn **`SKILL.md` + YAML frontmatter + `refs/`** — auto-trigger theo
  `description`, đồng bộ với `MEGA/Skill/unity-skills`.
- Thêm tầng **`playbooks/`**: thứ tự việc theo giai đoạn của game + gate giữa các giai đoạn.

**Mới**
- `standards/performance-budget.md` — ngân sách **kèm cách đo**.
- `standards/anti-patterns.md` — sổ bug đã trả giá, tích luỹ qua project.
- `workflow/ask-and-visualise.md` · `decisions.md` · `verification.md` · `harvest.md`.
- `Docs/glossary.md` — chống "một khái niệm hai tên" và "một tên hai nghĩa".
- Skill `game-feel-motion` · `difficulty-design` · `gd-communication`.
- `knowledge/motion` (vocabulary + presets + playground.html) · `knowledge/feel` ·
  `knowledge/difficulty` · `knowledge/editor-ux`.
- `templates/`: `open-questions.html` · `gd-brief.html` · `option-picker.html` · `visualiser-base.html`.

**Luật code siết thêm**
- Không hardcode số liệu · không `CreatePrimitive` · không `new Material` / `Shader.Find` trong
  runtime path. Số đi qua prefab field / Profile SO / level data.
- Pool bắt buộc `VTLTools.ObjectPool`.
- Text dùng TextMeshPro qua **prefab riêng có script quản lý** (`Init(params)` / `Show` / `Hide` /
  anim feedback), pooled — không `TextMesh` legacy, không dựng text bằng code.

**Bỏ khỏi pack (đặc thù project cũ)**
- Con số cụ thể của Cake Roll (queue capacity, pieces/roll, tray slot, số line) — chỉ còn làm **ví dụ**
  trong skill difficulty, không phải giá trị mặc định.
- Ràng buộc hình học polyline/fillet/blocking-theo-đường-tâm — chuyển thành **bộ câu hỏi**
  trong `technical-slice`.
- Từ "Codex" → "agent" ở toàn bộ pack.
- Lối viết story 50–80KB liệt kê từng class — thay bằng template theo lối story 026/028/030.

---

## v3.0 — 2026-08-19 (bản trước, đã được thay thế)
Skeleton đầu tiên tách `system-design.md` thành blueprint cố định. Phần lớn skill/template/library
mới chỉ khai trong README, chưa có nội dung.

## v2 — 2026-08-01
Codex-first pack, project-scoped cho Cake Roll.
