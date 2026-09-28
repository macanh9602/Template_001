# P2 — Technical slice

**Vào khi:** có một rủi ro kỹ thuật chưa biết làm nổi không, và nhiều story sau phụ thuộc vào nó.
**Ra khi:** có kết luận **đo được** + quyết định go/no-go + ràng buộc cho story sau.

Skill chính: `technical-slice`. Playbook này là trình tự chạy nó trong bối cảnh dự án.

---

## Bước 0 — Có đáng chạy slice không?

| Trả lời "có" ≥ 2 câu ⇒ chạy slice |
|---|
| Chưa ai trong team làm thứ này bao giờ? |
| Sai thì phải làm lại > 3 ngày? |
| Nghi ngờ không đạt performance budget trên thiết bị chuẩn? |
| Có > 1 cách làm và chưa biết cách nào trụ được? |
| Nhiều story sau xây trên nền này? |

Không đủ ⇒ bỏ qua playbook này, làm thẳng story.

## Bước 1 — Slice brief

Theo `skills/technical-slice/` Bước 0. Confirm với dev **trước khi mở Unity**.

Bắt buộc có: câu hỏi (một câu) · ngưỡng số + thiết bị · các phương án đem thử · budget thời gian ·
phần "KHÔNG làm".

Ngưỡng lấy từ `standards/performance-budget.md`. Chưa có ngưỡng ⇒ chốt ngưỡng trước, đó cũng là một
quyết định cần trắc nghiệm.

## Bước 2 — Harness đo trước

Thứ tự cố định: **thước trước, vật cần đo sau.**

- [ ] Scene test riêng, số lượng object cấu hình qua SO/field
- [ ] HUD số đo: ms/frame, draw call, GC alloc/frame, mem — qua prefab TMP có script quản lý
- [ ] Slider/nút đổi tham số tại chỗ (không build lại để đổi một số)
- [ ] Stress mode 2× / 5× / 10×
- [ ] Build lên **thiết bị chuẩn** ngay từ lần đo đầu

## Bước 3 — Chạy từng phương án

Cùng scene, cùng thiết bị, cùng số lượng, cùng build config.

| Phương án | ms/frame (avg) | ms/frame (p95) | draw call | GC alloc/frame | mem | ghi chú |
|---|---|---|---|---|---|---|

`p95` quan trọng hơn `avg` với trải nghiệm — giật đến từ đuôi phân bố, không đến từ trung bình.

## Bước 4 — Tìm điểm gãy

Không dừng ở "đạt ngưỡng". Tăng tải tới khi vượt ngưỡng để biết **trần thực tế**.
Trần này thành ràng buộc trong `Docs/project-context.md` (ví dụ: *"tối đa N element động cùng lúc"*).

## Bước 5 — Kết luận + quyết định

Viết phần **Kết luận slice** theo `skills/technical-slice/` Bước 3, gồm cả:

- **Nợ kỹ thuật để lại** — cái gì trong slice không được bê thẳng vào production
- **Ràng buộc cho story sau**

Ghi `D-xxx` vào `Docs/decision-log.md`. Cập nhật `Docs/project-context.md` (🔒 nếu là contract).

## Bước 6 — Chuyển sang production

Slice **không** tự thành feature. Tạo story riêng:

- [ ] Bỏ hardcode, đẩy số vào Profile/prefab/level data
- [ ] Đặt đúng layer theo `standards/system-design.md`
- [ ] Pool hoá thứ sinh nhiều
- [ ] Thêm test cho phần logic (chạy headless được)
- [ ] Xoá hoặc tách riêng scene/script slice, đánh dấu rõ

---

## Xong khi

- [ ] Câu hỏi ở brief đã trả lời bằng **số**, đo trên thiết bị thật
- [ ] Biết điểm gãy, không chỉ biết "đủ dùng"
- [ ] `D-xxx` đã ghi, project-context đã cập nhật
- [ ] Story production đã lên roadmap

Quá budget thời gian mà chưa kết luận ⇒ **đó là kết luận** ("cách này không rẻ"). Báo dev, hỏi trắc
nghiệm hướng tiếp. Không âm thầm kéo dài.

## Bẫy

Xem bảng bẫy trong `skills/technical-slice/`. Hai cái chết người nhất:
**đo trong Editor rồi kết luận**, và **slice phình thành feature**.
