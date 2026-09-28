# Skills — bảng tra

> `skills/` là **canonical source of truth dùng chung mọi agent**. Không maintain một bản Codex và một bản Claude riêng.
> `AGENTS.md` / `CLAUDE.md` chỉ làm adapter + routing; nếu một host cần native skill directory thì sync/symlink/generate từ đây, không sửa generated copy.
> Agent đọc file này để biết **load skill nào**, không load hết. Mỗi skill là một thư mục: `SKILL.md` với frontmatter `name` + `description`; `description` là trigger metadata cho router/host hỗ trợ skill discovery. `refs/` giữ phần dài, chỉ load khi task chạm đúng phần đó.

## Luôn áp dụng (không phải skill — là hằng số)

| File | Khi nào |
|---|---|
| `AGENTS.md` | mọi task |
| `workflow/loop.md` | mọi task — nhịp làm việc, story sizing |
| `standards/code-style.md` | trước **mọi** bước viết code Unity |
| `workflow/ask-and-visualise.md` | khi sắp hỏi dev bất cứ điều gì |

## Load theo tình huống

| Trigger (dev nói / task là) | Skill |
|---|---|
| "vấn đề này do đâu" · nhiều nguyên nhân khả dĩ · yêu cầu mơ hồ · agent sắp assume | `enrich-context/` |
| "làm feature X" · cần spec đầy đủ gameplay + visual + editor | `spec-feature/` |
| prototype · GDD chưa lock · cần spec nhanh để thử | `spec-feature/refs/spec-lite.md` |
| procedural mesh · runtime generation · save architecture · custom rendering · editor foundation · rủi ro performance lớn | `technical-slice/` |
| external Level Editor · HTML authoring · export level JSON · generator · cần quyết định author level ở đâu | `authoring-pipeline/` |
| level editor · Unity authoring window · UI Toolkit · editor UX/responsive/splitter/input | `level-editor/` |
| "animation này chưa đã" · chuyển động · tween · juice · có video ref | `game-feel-motion/` |
| độ khó · level generation · DDA · difficulty curve · booster trigger · "level dễ quá / khó quá" | `difficulty-design/` |
| giải thích cơ chế cho GD · viết tooltip · chốt open question với GD · GD đọc số liệu sai | `gd-communication/` |
| có model rồi · "đưa asset vào Unity" · sai scale/pivot · kiểm tra tri budget · export Blender → Unity · asset artist vừa giao | `asset-intake/` |
| bug lặp lại · fix rồi vẫn lỗi · cần data thật | `debug-audit/` |
| pooled visual · tween/async chồng nhau · queue/slot shift · handoff · stale callback · overlap khi tap nhanh | `presentation-lifecycle/` |

## Đang ở một giai đoạn lớn → đọc playbook

| Giai đoạn | Playbook |
|---|---|
| repo trống, mới có GDD | `playbooks/p1-bootstrap.md` |
| chưa biết rủi ro kỹ thuật có làm được không | `playbooks/p2-technical-slice.md` |
| cần chơi được vòng cơ bản | `playbooks/p3-core-loop.md` |
| cần chốt GD author ở đâu / external tool / generator | `playbooks/p4-authoring-pipeline.md` |
| đã chốt Unity Editor là authoring owner | `playbooks/p4-level-editor.md` |
| cần kiểm soát độ khó | `playbooks/p5-difficulty.md` |
| chạy đúng rồi nhưng chưa đã tay | `playbooks/p6-feel-pass.md` |
| chuẩn bị build thật | `playbooks/p7-ship.md` |

## Kết hợp hay dùng

```
Feature mới có GDD rõ     → spec-feature → code-style → verification
Feature mơ hồ             → enrich-context → visualiser → spec-lite
Rủi ro kỹ thuật           → technical-slice → code-style
"Chuyển động chưa đã"     → game-feel-motion (+ visualiser) → code-style
Authoring pipeline         → authoring-pipeline → external tool / generator / level-editor branch
Tool Unity cho GD          → authoring-pipeline → level-editor (+ enrich-context) → gd-communication
Cân bằng độ khó           → difficulty-design (+ visualiser) → gd-communication
Bug dai                   → enrich-context → debug-audit
Visual race / pooled handoff → presentation-lifecycle (+ debug-audit nếu chưa có evidence)
GD hiểu sai số liệu       → gd-communication
```

## Luật chung cho mọi skill

1. Đọc `Docs/project-context.md` trước; **không hỏi lại thứ file đó đã trả lời**.
2. Hỏi bằng **trắc nghiệm 2–4 option + "Other" + đúng một option recommend**.
   Free-text chỉ để xin dữ liệu thô (log, số đo, video, screenshot).
3. Không tự chọn trade-off thay dev.
4. Có yếu tố không gian / chuyển động / timing / nhiều biến → **dựng visualiser trước khi hỏi**.
5. Không hardcode số liệu; không `CreatePrimitive`; không `new Material` / `Shader.Find`.
6. Rút được pattern generic → đề xuất đẩy về `knowledge/` hoặc skill tương ứng ở cuối task
   (`workflow/harvest.md`).
