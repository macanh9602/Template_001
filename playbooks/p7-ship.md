# P7 — Ship

**Vào khi:** chuẩn bị build thật / lên store / gửi bản cho publisher.
**Ra khi:** build chạy ổn trên dải thiết bị mục tiêu, không rò rỉ, số liệu bật, và có bản ghi lại
trạng thái thật của dự án.

---

## Bước 1 — Build thật, sớm và nhiều lần

Bug chỉ xuất hiện trong build là loại tốn thời gian nhất. Đừng để tới cuối mới build lần đầu.

- [ ] Build release (không phải development) chạy được
- [ ] Chạy trên **thiết bị yếu nhất** trong dải mục tiêu, không chỉ máy dev
- [ ] Chạy trên ít nhất 3 aspect ratio khác nhau, có cả máy có notch
- [ ] Code strip / IL2CPP không làm mất thứ dùng qua reflection (serialization, SO, DI)
- [ ] `#if UNITY_EDITOR` không che mất logic cần trong build

## Bước 2 — Vòng đời & rò rỉ

- [ ] Chơi **30 level liên tiếp** không tắt app: memory không tăng dần, FPS không tụt dần
- [ ] Load → unload → load lại nhiều lần: số instance ổn định
- [ ] Sự kiện, token, tween đều được huỷ khi rời level
- [ ] Vào/ra background (khoá màn, chuyển app, cuộc gọi) → quay lại không hỏng state
- [ ] Mất mạng giữa chừng (nếu có tính năng online) → xử lý được, không treo

## Bước 3 — Performance cuối

`standards/performance-budget.md`, đo trên thiết bị yếu nhất:

| Chỉ số | Ngưỡng | Đo được |
|---|---|---|
| frame time avg | | |
| frame time p95 | | |
| GC alloc / frame ở gameplay | | |
| draw call ở cảnh nặng nhất | | |
| memory sau 30 level | | |
| thời gian mở app tới màn hình đầu | | |
| thời gian load một level | | |

`p95` và **memory sau 30 level** là hai số hay bị bỏ qua và hay gây review 1 sao nhất.

## Bước 4 — Nội dung & dữ liệu

- [ ] Mọi level trong build **mở được và chơi được** (chạy batch validate, không mở tay từng cái)
- [ ] Level data version khớp với code; migration cho file cũ có test
- [ ] Không còn placeholder art/text lọt vào build
- [ ] Text hiển thị: label/button English ASCII (test gate quét literal pass); text tiếng Việt ở
      đúng file riêng
- [ ] Không còn số hardcode "tạm" — soát lại theo `AGENTS.md §5`

## Bước 5 — Số liệu & log

- [ ] Analytics event bắn đúng chỗ, đúng tên, có test một lượt end-to-end
- [ ] Sự kiện thắng/thua ghi nhận từ **state**, không từ tap (`p3-core-loop` Bước 4) — nếu không,
      số liệu sẽ lệch có hệ thống
- [ ] Crash reporting bật
- [ ] Log debug/audit tắt hoặc sau flag — không log mỗi frame trong release
- [ ] Level data có seed (nếu sinh tự động) để tái tạo được level người chơi báo lỗi

## Bước 6 — Chốt tài liệu

- [ ] `Docs/decision-log.md` không còn entry `Proposed` treo lơ lửng
- [ ] `Docs/project-context.md` phản ánh **trạng thái hiện tại**, không phải dự định
- [ ] `Docs/glossary.md` khớp với tên trong code và trong tool
- [ ] `standards/anti-patterns.md` đã nhận các bug gặp trong giai đoạn ship
- [ ] `handoff/ROADMAP.md` đánh dấu đúng cái nào DONE, cái nào cắt (kèm lý do cắt)

## Bước 7 — Báo cáo trung thực

Theo `workflow/verification.md`. Bảng trạng thái với PASS / FAIL / PENDING / PARTIAL /
KNOWN BASELINE FAILURES / N/A.

**Không ghi PASS cho thứ chưa chạy.** Danh sách "biết là còn lỗi" công khai có giá trị hơn danh sách
toàn xanh mà không đúng — nó cho phép người quyết định biết mình đang đánh đổi cái gì.

```markdown
## Ship readiness — [ngày] — build [số]

| Hạng mục | Trạng thái | Bằng chứng |
|---|---|---|

### Biết là còn lỗi
| Vấn đề | Ảnh hưởng | Vì sao chấp nhận ship |
|---|---|---|

### Rủi ro sau khi ship
| Rủi ro | Dấu hiệu sẽ thấy | Ứng phó |
|---|---|---|
```

## Bước 8 — Harvest cả dự án

`workflow/harvest.md`. Đây là lúc pack được cập nhật:

- [ ] Bug lặp lại nhiều lần ⇒ `standards/anti-patterns.md`
- [ ] Pattern generic ⇒ `knowledge/`
- [ ] Bài học quy trình ⇒ `workflow/`
- [ ] Preset chuyển động ⇒ `knowledge/motion/presets.json`
- [ ] Câu hỏi phải hỏi lại nhiều lần ⇒ thêm vào playbook bootstrap
- [ ] Chỗ nào story quá chặt / quá lỏng ⇒ chỉnh `workflow/loop.md` sizing

---

## Xong khi

- [ ] Build release chạy trên thiết bị yếu nhất, đủ dải aspect ratio
- [ ] 30 level liên tiếp không rò rỉ
- [ ] Mọi ngưỡng performance đo được và ghi lại
- [ ] Analytics kiểm chứng end-to-end
- [ ] Báo cáo ship readiness trung thực, có mục "biết là còn lỗi"
- [ ] Harvest đã đẩy về pack — dự án sau bắt đầu tốt hơn dự án này
