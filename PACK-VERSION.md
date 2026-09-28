# Pack version

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
