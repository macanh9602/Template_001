# Feel checklist

> Chạy trước khi đóng một story feel/polish, và một lần nữa ở `playbooks/p7-ship.md`.
> Ghi PASS / FAIL / PENDING theo `workflow/verification.md`.

## A. Phản hồi tức thì

- [ ] Mỗi hành động bấm được có phản hồi ở **frame đầu tiên** (màu / scale punch / audio / haptic)
- [ ] Không có hành động nào "im lặng"
- [ ] Thao tác **không hợp lệ** cũng có phản hồi, và phản hồi đó đọc được là *"không được"*,
      không phải *"game lỗi"*
- [ ] Nút disabled nhìn ra được là disabled trước khi bấm

## B. Trạng thái đọc được bằng mắt

- [ ] Mỗi state gameplay phân biệt được **bằng mắt**, không chỉ trong code
- [ ] Element đang được chọn / đang chờ / đã dùng: khác nhau rõ
- [ ] Không dựa **chỉ** vào màu để phân biệt (một phần người chơi không phân biệt được màu)

## C. Chuyển động

- [ ] Mọi chuyển động ánh xạ về một `presetId` trong `knowledge/motion/presets.json`
- [ ] Không có hai preset gần giống nhau tồn tại song song
- [ ] Duration nằm trong ngân sách ở `vocabulary.md §8`
- [ ] `ease-out` là mặc định; `back`/`elastic` chỉ dành cho khoảnh khắc đáng nhấn
- [ ] Nhiều phần tử động cùng lúc → có `stagger`, không nhảy đồng loạt
- [ ] Chuyển động dài đi theo `arc`, không phải đường thẳng cứng

## D. Interrupt & vòng đời

- [ ] Mỗi tween khai báo interrupt policy (`restart`/`ignore`/`queue`/`blend`)
- [ ] Bấm liên tục thật nhanh → không kẹt, không chồng tween, không sai state
- [ ] Tween bị **kill** khi element về pool / level unload
- [ ] Reset scale/rotation/color lúc **lấy ra khỏi pool**
- [ ] Domain **không** `await` tween của Visual

## E. Nhịp & sự lặp lại

- [ ] Mọi sequence > 1 s **skip được bằng tap**
- [ ] Chơi 20 ván liên tiếp: không anim nào bắt đầu gây khó chịu
- [ ] Anim thắng/thua không chặn người chơi bấm "chơi lại"
- [ ] Idle hint chỉ bật sau khi người chơi đứng yên N giây, không bật thường trực

## F. Âm thanh & rung

- [ ] Mỗi `immediate cue` có audio đi kèm (hoặc quyết định rõ là không cần)
- [ ] Audio không chồng chập khi thao tác nhanh (có giới hạn số instance / cooldown)
- [ ] Haptic chỉ ở khoảnh khắc đáng — rung liên tục làm người chơi tắt haptic
- [ ] Tắt âm trong máy → game vẫn đọc được trạng thái bằng hình

## G. Số nằm đúng chỗ

- [ ] Mọi duration/easing/overshoot ở `MotionProfile` (visual) hoặc `TimingProfile` (gameplay)
- [ ] Component chỉ giữ `presetId`
- [ ] Không còn số chuyển động hardcode trong script
- [ ] Đổi một giá trị trong profile → thấy đổi trong game, không sửa code

## H. Chi phí

- [ ] `p95` frame time vẫn trong budget sau khi thêm juice
- [ ] Không alloc mỗi frame (tween tạo mới trong hot path là nguồn GC spike)
- [ ] VFX/text bay pooled — không `Instantiate` trong gameplay loop
- [ ] Camera shake từ nhiều nguồn cộng dồn có **trần**

## I. Kiểm chứng con người

- [ ] Người ngoài cầm máy chơi, **không** giải thích gì, và thấy sướng tay
- [ ] Chỗ họ do dự / bấm lại lần hai = chỗ phản hồi chưa rõ → ghi lại và sửa
