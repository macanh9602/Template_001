# P6 — Feel pass

**Vào khi:** logic đứng, vòng lặp khép kín, nhưng "chưa đã tay".
**Ra khi:** mỗi hành động chính có phản hồi tức thì + chuyển động nhất quán + số nằm trong
`MotionProfile`, và người ngoài cầm máy chơi thấy **sướng tay**.

Skill chính: `skills/game-feel-motion/` (+ `refs/video-ref-analysis.md` trong thư mục đó).
Tham chiếu: `knowledge/motion/` · `knowledge/feel/checklist.md`.

**Đừng chạy playbook này trước khi logic đứng.** Polish thứ sắp bị sửa là lãng phí.

---

## Bước 1 — Liệt kê hành động chính

Bảng mọi hành động người chơi làm nhiều nhất:

| Hành động | Số lần / phút | Hiện có phản hồi gì | Thiếu gì |
|---|---|---|---|

Sắp xếp theo **tần suất giảm dần**. Hành động làm 200 lần/ván quan trọng hơn hành động làm 1 lần.

## Bước 2 — Sửa "phản hồi tức thì" trước tiên

Đây là thứ rẻ nhất và hiệu quả nhất.

- [ ] Mỗi hành động có `immediate cue` ở **frame đầu tiên** (đổi màu / scale punch nhỏ / audio /
      haptic), tách khỏi `main motion`
- [ ] Không có hành động nào "im lặng" — bấm mà không thấy gì là lỗi cảm giác nặng nhất

Làm xong bước này rồi mới đánh giá lại. Rất thường, phần lớn cảm giác "chậm/cứng" biến mất ở đây.

## Bước 3 — Chuẩn hoá từ vựng chuyển động

- [ ] Đọc `knowledge/motion/vocabulary.md`
- [ ] Đối chiếu `knowledge/motion/presets.json` — chuyển động hiện có ánh xạ về preset nào?
- [ ] Chuyển động gần giống nhau nhưng số khác nhau ⇒ **gộp về một preset**. Đây là bước tạo ra
      "ngôn ngữ chuyển động" nhất quán cho game.

## Bước 4 — Từng hành động: spec 5 pha

Với mỗi hành động ở Bước 1 (theo thứ tự tần suất):

| Pha | Duration | Easing | Từ → Đến | Preset id |
|---|---|---|---|---|
| anticipation | | | | |
| action | | | | |
| overshoot | | | | |
| settle | | | | |
| follow-through | | | | |

Kèm: **Trigger** · **Interrupt policy** (`restart`/`ignore`/`queue`/`blend`) · **Cue** (VFX/audio/haptic).

Có video ref ⇒ chạy `skills/game-feel-motion/refs/video-ref-analysis.md` trước.

## Bước 5 — Visualiser + chốt số

Bắt buộc dựng `.html` khi chọn easing/duration hoặc so sánh phương án.

- [ ] Các phương án chạy **cạnh nhau, cùng lúc**
- [ ] Slider duration / overshoot / stagger, hiện số
- [ ] Replay + slow-motion 0.25×
- [ ] Nhãn bằng technical term, không "Option A/B/C"

Rồi hỏi trắc nghiệm 2–4 phương án + đúng một recommend.

## Bước 6 — Đẩy số vào profile

- [ ] Mọi duration/easing/overshoot nằm trong `MotionProfile` (visual) hoặc `TimingProfile` (nhịp
      gameplay)
- [ ] Component chỉ giữ **preset id**
- [ ] Không còn số chuyển động nào hardcode trong script

## Bước 7 — Interrupt & vòng đời

Chỗ sinh bug nhiều nhất trong feel pass:

- [ ] Mỗi tween khai báo interrupt policy
- [ ] Tween bị **kill** khi element về pool / level unload
- [ ] Pool lifecycle: **Release** kill/cancel + neutralize; **Acquire/Bind** rebind đầy đủ scale/rotation/color/visibility từ data hiện tại
- [ ] Domain **không** `await` tween

## Bước 8 — Nhịp cho vòng lặp lặp lại nhiều

- [ ] Mọi sequence > 1s **skip được bằng tap**
- [ ] Anim trong vòng lặp chính: thà ngắn hơn 20% còn hơn dài hơn 20%
- [ ] Nhiều phần tử động cùng lúc ⇒ dùng `stagger`, giảm biên độ phụ, tránh rối mắt
- [ ] Chơi thử **20 ván liên tiếp** — cái gì bắt đầu gây khó chịu thì cắt

## Bước 9 — Kiểm tra chi phí

`standards/performance-budget.md`:

- [ ] Tween/particle thêm vào không đẩy `p95` frame time vượt ngưỡng
- [ ] Không alloc mỗi frame (tween tạo mới trong hot path là nguồn GC spike)
- [ ] VFX pooled, không `Instantiate` trong gameplay loop

## Bước 10 — Harvest

- [ ] Preset dùng lại được ⇒ thêm vào `knowledge/motion/presets.json` (id · phases · khi nào dùng)
- [ ] Từ vựng mới ⇒ `knowledge/motion/vocabulary.md`
- [ ] Chạy `knowledge/feel/checklist.md`

---

## Xong khi

- [ ] Mọi hành động chính có `immediate cue`
- [ ] Chuyển động ánh xạ về tập preset thống nhất, không mỗi chỗ một kiểu
- [ ] Số nằm trong profile, không trong code
- [ ] Interrupt policy khai báo đủ; không còn bug tween sau pool/unload
- [ ] Chơi 20 ván liên tiếp không thấy anim nào phiền
- [ ] `p95` frame time vẫn trong budget
- [ ] Người ngoài cầm máy chơi và nói "đã tay" — không cần giải thích gì

## Bẫy

| Bẫy | Hậu quả |
|---|---|
| polish trước khi logic đứng | làm lại |
| thêm juice khắp nơi cùng lúc | rối mắt, không biết cái nào có tác dụng |
| mỗi element một preset riêng | game mất nhất quán |
| chỉ chỉnh easing, không chỉnh duration | vẫn "chưa đã" |
| anim dài trong vòng lặp chính | phiền ở lần thứ 50 |
| tween không kill khi pool | object nhảy loạn sau respawn |
| đánh giá feel chỉ qua 2–3 ván | không thấy chỗ gây mệt |
| chốt bằng lời thay vì visualiser | hai bên hình dung hai kiểu |
