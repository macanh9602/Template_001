# Harvest — rút kinh nghiệm về pack

> Không sửa file này khi làm game mới.
> Mục đích: pattern học được ở project này không chết trong project này.

---

## 1. Cuối mỗi story — bốn câu

Ghi vào final report, mục cuối:

```markdown
## Harvest

1. Pattern generic:  <cái gì không dính game cụ thể>  → đề xuất đẩy về: <knowledge/ | standards/ | skill nào>
2. Câu hỏi lặp lại:  <câu nào phải hỏi lần thứ 2+>    → đề xuất thêm vào: Docs/project-context.md
3. Bug loại lặp lại: <triệu chứng + nguyên nhân>       → đề xuất thêm dòng: standards/anti-patterns.md
4. Asset dùng lại:   <tween preset | metric | rubric>  → đề xuất thêm vào: knowledge/<nhóm>/
```

Không có gì để harvest thì ghi `— không có` cho từng mục, đừng bỏ trống. Bốn câu này rẻ và là thứ
duy nhất làm pack tốt lên theo thời gian.

Agent chỉ **đề xuất**. Dev quyết có đẩy về pack hay không.

---

## 2. Tiêu chí nhận vào pack

| Đẩy về | Chỉ khi |
|---|---|
| `standards/system-design.md` | pattern đã dùng ở **≥ 2 project** và không dính thể loại game |
| `standards/anti-patterns.md` | bug đã xảy ra thật và đã tốn thời gian thật — **1 lần là đủ** |
| `standards/performance-budget.md` | ngân sách/cách đo mới, đã áp dụng thật |
| `knowledge/motion/presets.json` | preset đã dùng và dev thấy "đã tay" |
| `knowledge/difficulty/` | metric/pattern đã dùng để ra quyết định thật, không chỉ hiển thị |
| `knowledge/editor-ux/` | rubric đã bắt được lỗi thật trong review |
| `skills/<x>/` | phương pháp lặp lại được, không phải một thủ thuật một lần |
| `templates/` | khung đã copy dùng ≥ 2 lần |

Không nhận vào pack: giá trị số cụ thể của một game · tên class của một game · quy ước chỉ đúng với
một thể loại. Những thứ đó ở lại `Docs/` của project.

---

## 3. Cách viết một mục harvest cho tốt

**Pattern** — viết ở mức *cơ chế*, không ở mức *cài đặt*:

- Xấu: *"CakeQueueRuntimeState nên có ring array"*
- Tốt: *"Collection có capacity cố định và truy cập theo id: dùng bucket cấp phát sẵn theo id, không dictionary động — O(1) và 0 alloc"*

**Anti-pattern** — luôn tách **triệu chứng** khỏi **nguyên nhân**:

- Triệu chứng là cái người ta sẽ google. Nguyên nhân là cái phải sửa.
- Ví dụ: *"Triệu chứng: object từ pool lệch vị trí khi parent có scale. Nguyên nhân: `SetParent(worldPositionStays: true)` không reset local scale."*

**Preset motion** — luôn kèm **ngữ cảnh dùng**:

- `id` · `ease` · `duration` · tham số · *"dùng cho loại chuyển động nào"* · *"nhìn thế nào thì biết đang đúng"*

**Metric difficulty** — luôn kèm **bẫy diễn giải**:

- đo bằng gì · nói lên gì · **cái nó KHÔNG nói** · khi nào con số này đánh lừa

---

## 3b. Cuối mỗi WP — số liệu, không trí nhớ

```powershell
.\tools\run-report.ps1 -Wp <wp>        # → handoff/_reports/run-report.html
```

Trang report gom tự động (không gọi AI): token/tiền/thời gian từng run, model nào tốn bao nhiêu, script verify tiết kiệm
bao nhiêu, và **tín hiệu harvest**. Mỗi ô tín hiệu > 0 là một câu hỏi harvest:

| Tín hiệu | Hỏi |
|---|---|
| Reviewer PASS sai (`REVIEW_OVERRIDE`) | luật reviewer / prompt thiếu gì → `workflow/run-task.md` bảng bài học |
| Run cần PATCH | lỗi lặp của implementer → `standards/anti-patterns.md` hoặc packet thiếu `Pre-decided` |
| Chạm loop cap · TARGET_RECONSIDER | task quá to / target mơ hồ → cắt task, sửa spec |
| BLOCKED_TOOLCHAIN · quota · budget | máy/host/profile → doctor, `config/run-profiles.json` |
| `REUSE_BYPASS` (lint) / reuse check trong run | hệ thống bị viết lại → thêm vào `Docs/reuse-registry.json` hoặc `mustReuse` |
| Task PASS bằng AI mà acceptance đo được bằng máy | chuyển sang `verify` script (0 token) |

---

## 4. Cuối project — harvest lớn

Chạy một lần cho toàn bộ project, trước khi đóng:

```
[ ] Đọc lại toàn bộ decision-log, gom các bài học meta (khuôn lỗi tư duy) → anti-patterns §A
[ ] Đọc lại các implementation-notes, gom Trade-off và Open risk chưa được giải quyết
[ ] Liệt kê mọi thứ đã phải đảo hướng ≥ 1 lần, ghi rõ tiền đề nào đã bị bỏ sót
[ ] Gom mọi tween/motion đã tune ưng ý → knowledge/motion/presets.json
[ ] Gom mọi metric/threshold đã dùng thật → knowledge/difficulty/
[ ] Rà project-context.md: fact nào đáng thành câu hỏi mặc định của template?
[ ] Rà glossary.md: từ nào đáng thành từ chuẩn của pack?
[ ] Cập nhật PACK-VERSION.md
```

---

## 5. Dấu hiệu pack đang lệch

- Có skill mà **chưa project nào dùng** → cân nhắc bỏ, hoặc nó viết sai trigger.
- Có mục trong `standards/` mà project nào cũng phải override → nó không phải hằng số, chuyển sang
  `Docs/` template.
- `anti-patterns.md` có mục **lặp lại nội dung** của mục khác → gom lại, khuôn lỗi chung mới là thứ
  đáng nhớ.
- Story vẫn dài và vẫn phải hỏi lại nhiều → template story hoặc skill tương ứng đang thiếu một mục.
