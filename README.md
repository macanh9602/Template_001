# Mobile Game Template — Code + Agent Pack

Template dùng chung cho mọi project mobile game mới. Gồm **hai nửa**:

| Nửa | Nội dung | Vai trò |
|---|---|---|
| `Assets/` | MDATools, AMZG, plugins (DOTween, Odin, Dreamteck), `_Core` skeleton | code tái sử dụng |
| Pack ở root | `AGENTS.md`, `standards/`, `workflow/`, `Docs/`, `skills/`, `playbooks/`, `templates/`, `knowledge/`, `handoff/` | cách làm việc với agent |

Mục tiêu: **mô tả rõ requirement → agent + skill + template triển khai được toàn diện**, và
**tầng hệ thống của mọi game giống nhau** để không phải học lại kiến trúc mỗi lần mở game mới.

---

## Hai nhóm thư mục — phân biệt trước khi làm gì khác

```
KHÔNG SỬA khi làm game mới          ĐIỀN mỗi game
────────────────────────────        ──────────────────────
standards/   tầng hệ thống          Docs/       game này là gì
workflow/    cách làm việc          handoff/    roadmap + story
skills/      phương pháp
playbooks/   thứ tự giai đoạn
templates/   khung để copy
knowledge/    kho dùng lại
```

Muốn lệch khỏi `standards/` → ghi vào `AGENTS.md §10 Project overrides` **và** một entry `D-xxx`
trong `Docs/decision-log.md`. Đừng sửa file trong `standards/`.

---

## Bootstrap một project mới

```text
1. Copy toàn bộ nội dung Template/ vào root repo Unity mới.
2. Mở agent tại root repo, gửi handoff/START-PROMPT.md.
   → agent chạy playbooks/p1-bootstrap.md
3. Agent khảo sát repo, đề xuất PROJECT FACTS, hỏi trắc nghiệm cái nào cần dev chốt.
4. Đặt GDD / video ref vào reference/.
5. Agent tóm tắt core loop + liệt kê chỗ GDD mơ hồ + đề xuất ROADMAP → dừng chờ confirm.
6. Chốt roadmap → viết story đầu tiên (templates/story.md) → chạy handoff/RUN-STORY-PROMPT.md.
```

---

## Cấu trúc pack

```text
AGENTS.md                     guardrail · Always / Never / Escalation / DoD
CLAUDE.md                     pointer ngắn
PACK-VERSION.md               changelog của pack

standards/                    ── HẰNG SỐ ──
  system-design.md            tầng hệ thống blueprint: layer · contract · nơi chỉnh số · lifecycle
  folder-structure.md         folder · namespace · asmdef · naming
  code-style.md               cách viết code Unity
  performance-budget.md       ngân sách mobile + CÁCH ĐO từng chỉ số
  anti-patterns.md            sổ bug đã trả giá — lớn dần qua các project

workflow/                     ── HẰNG SỐ ──
  loop.md                     vòng 7 bước · story sizing S/M/L
  ask-and-visualise.md        luật hỏi trắc nghiệm + khi nào bắt buộc dựng .html
  decisions.md                cách viết D-xxx · supersede · đính chính
  verification.md             Unity MCP · trạng thái verify trung thực · evidence
  harvest.md                  cuối story/project rút gì về pack

Docs/                         ── ĐIỀN ──
  project-context.md          game này là gì + PROJECT FACTS TO CONFIRM
  runtime-architecture.md     instance của standards/system-design cho game này
  data-model.md               source of truth / generated / runtime state
  decision-log.md             D-xxx, append-only
  glossary.md                 một khái niệm một tên — dùng chung GD ↔ dev ↔ code

skills/                       ── load theo trigger, không load hết ──
  README.md                   BẢNG TRA
  enrich-context/SKILL.md     phanh — hỏi cho đủ trước khi kết luận
  spec-feature/               SKILL.md + refs/spec-lite.md
  technical-slice/SKILL.md    lát cắt kỹ thuật có kết luận đo được
  level-editor/               SKILL.md + refs/{checklist,anti-patterns}.md
  game-feel-motion/           SKILL.md + refs/video-ref-analysis.md
  difficulty-design/          SKILL.md + refs/bot-and-metrics.md
  gd-communication/SKILL.md   nói chuyện với người không đọc code
  debug-audit/SKILL.md        instrument → đọc data thật → mới kết luận

playbooks/                    ── chuỗi việc theo giai đoạn ──
  README.md
  p1-bootstrap · p2-technical-slice · p3-core-loop · p4-level-editor
  p5-difficulty · p6-feel-pass · p7-ship

templates/                    ── copy rồi điền ──
  story.md · implementation-notes.html · decision-record.md
  open-questions.html · gd-brief.html · option-picker.html · visualiser-base.html

knowledge/                      ── kho dùng lại, lớn dần qua project ──
  motion/     vocabulary.md · presets.json · playground.html
  feel/       checklist.md
  difficulty/ metric-vocabulary.md · pipeline-patterns.md
  editor-ux/  update-model.md · validation-surfacing.md

handoff/                      ── ĐIỀN ──
  ROADMAP.md · START-PROMPT.md · RUN-STORY-PROMPT.md
  story-xxx/implementation-notes.html

reference/                    GDD · video ref · doc ngoài (tạo khi cần)
```

---

## Bảy nguyên tắc của pack

1. **Tầng hệ thống là hằng số.** `standards/system-design.md` giống nhau ở mọi game. Chỉ nội dung
   Domain và Visual thay đổi, không phải *hình dạng* của hệ thống.
2. **Decision đi trước code.** Trade-off chưa chốt → hỏi trắc nghiệm có recommend, không tự quyết.
   Chốt xong ghi lại **cả cái đã loại** và **đánh đổi đã chấp nhận**.
3. **Thấy trước khi đọc.** Không gian · chuyển động · timing · nhiều biến → dựng `.html` rồi mới bàn.
4. **Story vừa đủ, không cần chặt.** Story mô tả *goal + boundary + acceptance*, không mô tả *cách code*.
   Có Unity MCP thì agent tự verify; story không cần liệt kê từng bước.
5. **Số liệu không nằm trong code.** Prefab field / Profile SO / level data. Đây là điều kiện để
   GD và art tự chỉnh mà không cần dev.
6. **Verify trung thực.** `PASS` chỉ ghi khi đã chạy. Chưa chạy được thì ghi `PENDING` + lý do.
7. **Kho dùng lại lớn dần.** Mỗi story rút ra pattern generic → đẩy về `knowledge/` hoặc skill
   hoặc `standards/anti-patterns.md`. Đừng để chết trong project.

---

## Feedback loop giữa các project

Cuối mỗi story và cuối mỗi project, chạy `workflow/harvest.md`:

| Thấy gì | Đẩy về đâu |
|---|---|
| Pattern lặp lại ≥ 2 project | `standards/system-design.md` hoặc `knowledge/` |
| Câu hỏi phải hỏi lại ≥ 2 lần | thành mục trong `Docs/project-context.md` (template) |
| Bug loại lặp lại | một dòng trong `standards/anti-patterns.md` |
| Tween/motion đẹp | preset trong `knowledge/motion/presets.json` |
| Metric difficulty dùng được | `knowledge/difficulty/metric-vocabulary.md` |
