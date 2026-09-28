# P3 — Core loop

**Vào khi:** bootstrap xong, rủi ro kỹ thuật đã rõ.
**Ra khi:** chơi được **một vòng hoàn chỉnh** từ vào level → thao tác → thắng/thua → chơi lại,
trên thiết bị thật.

Đây là mốc quan trọng nhất của dự án. Trước mốc này mọi thứ là phỏng đoán; sau mốc này mới nói được
game có vui không.

---

## Bước 1 — Chốt vòng lặp bằng một câu

> *"Người chơi [thao tác] để [mục tiêu], thắng khi [điều kiện], thua khi [điều kiện]."*

Không viết được một câu ⇒ chưa đủ rõ để code. Quay lại `enrich-context`.

Ghi câu này vào `Docs/project-context.md`. Mọi story trong giai đoạn này đối chiếu với nó.

## Bước 2 — Input trước, feedback ngay

Thứ tự: **input đúng → phản hồi tức thì → logic → visual đầy đủ.**

- [ ] Input đi qua **command buffer**: thu thập → xử lý theo thứ tự xác định. Không xử lý ngay trong
      callback (nhiều ngón chạm cùng frame là nguồn bug kinh điển).
- [ ] Chọn đối tượng: `RaycastNonAlloc` **không sắp xếp theo khoảng cách** — chọn thắng bằng **luật
      rõ ràng** (ưu tiên gì, hoà thì sao), ghi luật đó vào story.
- [ ] `immediate cue` xuất hiện ở frame đầu (đổi màu / scale punch / audio) — tách khỏi anim chính
      (`skills/game-feel-motion/`).

## Bước 3 — Domain giữ nhịp

`standards/system-design.md §6`.

- [ ] Domain có scheduler plain C#: `Tick(dt)` · `IsIdle` · `CancelAll`
- [ ] Domain **không** `await` tween của Visual
- [ ] Mọi delay đọc từ `TimingProfile`
- [ ] Property test: **đặt tất cả delay = 0 ⇒ toàn bộ chuỗi chạy trong một pha đồng bộ.**
      Test này bắt được gần hết lỗi phụ thuộc thời gian thật.
- [ ] Thao tác chồng lấn dùng **reservation** (giữ chỗ trước, xác nhận sau), không dựa vào thứ tự
      may rủi

## Bước 4 — Win/Lose là **state**, không phải hệ quả của một cú tap

Đánh giá điều kiện thắng/thua **sau mỗi lần state thay đổi**, không phải trong xử lý input.

Vì sao: gắn vào tap ⇒ trạng thái thắng đến từ nguồn khác (timer, chain reaction, hiệu ứng trễ) sẽ bị
bỏ sót, và analytics ghi nhận sai nguyên nhân thắng/thua.

- [ ] `EvaluateEndCondition()` gọi sau mọi mutation
- [ ] Trạng thái kết thúc **idempotent** — gọi hai lần không bắn event hai lần
- [ ] Chờ hết mọi hoạt động đang chạy (`IsIdle`) trước khi hiện màn kết thúc

## Bước 5 — HUD + luồng màn hình

- [ ] HUD đọc từ Domain qua bridge/event; HUD **không** quyết gameplay
- [ ] Vào level → chơi → thắng/thua → chơi lại / level tiếp: đi được **vòng tròn**
- [ ] Text qua prefab TMP có script quản lý, pooled
- [ ] Nút bấm có trạng thái disabled rõ ràng khi không dùng được

## Bước 6 — Load / Unload sạch

`standards/system-design.md §5`. Đây là chỗ nợ tích tụ âm thầm nhất.

- [ ] Unbind input **trước** khi cancel token
- [ ] Huỷ mọi token, kill mọi tween, unsubscribe mọi event
- [ ] Trả hết object về pool; `IPendingCleanup` chạy đủ
- [ ] `RuntimeState` theo level bị bỏ, **không** để lại state toàn cục
- [ ] Kiểm chứng: load → unload → load lại **10 lần**, số instance và bộ nhớ không tăng dần

## Bước 7 — Đo trên thiết bị thật

Theo `standards/performance-budget.md`. Đo ở **cấu hình nặng nhất hiện có**, không phải level 1.

---

## Xong khi

- [ ] Vòng lặp đi trọn vẹn trên thiết bị thật
- [ ] Delay = 0 test pass
- [ ] Load/unload 10 lần sạch
- [ ] Không hardcode số gameplay — mọi thứ trong `TimingProfile` / prefab / level data
- [ ] Đạt performance budget ở cấu hình nặng nhất hiện có
- [ ] Người khác cầm máy chơi được mà không cần hướng dẫn

## Bẫy

| Bẫy | Hậu quả |
|---|---|
| Domain đợi anim xong | gameplay khựng khi tween bị cắt; không test headless được |
| xử lý input ngay trong callback | multi-touch gây state sai |
| kiểm tra thắng/thua trong xử lý tap | bỏ sót thắng do nguồn khác; analytics sai |
| chưa làm unload đã làm feature tiếp | rò rỉ tích tụ, tới cuối dự án rất khó gỡ |
| Visual giữ state gameplay | hai nguồn sự thật |
| polish trước khi loop khép kín | polish thứ có thể bị vứt |
| chỉ test level 1 | gãy ở level nặng, phát hiện muộn |
