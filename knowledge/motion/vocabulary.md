# Motion vocabulary — từ vựng chuyển động

> Mục đích: **dev và agent nói cùng một thứ.** Luôn dùng English technical term, giải thích ngắn
> tiếng Việt. Đừng mô tả bằng tính từ ("mượt", "đã", "cứng") khi đã có term.

---

## 1. Pha chuyển động

| Term | Nghĩa | Nhận ra bằng |
|---|---|---|
| `anticipation` | pha chuẩn bị ngược hướng trước khi động | nén xuống trước khi nhảy, kéo lùi trước khi phóng |
| `action` | pha chính, đi từ A tới B | |
| `overshoot` | vượt quá đích rồi quay lại | vượt 10–20% rồi lùi |
| `settle` | lắng về giá trị cuối | rung nhỏ dần rồi đứng yên |
| `follow-through` | phần phụ chuyển động trễ hơn phần chính | đuôi, áo, particle trễ 0.03–0.08s |
| `hold` | dừng có chủ đích giữa chuỗi | khoảng lặng tạo nhấn |

## 2. Biến dạng

| Term | Nghĩa |
|---|---|
| `squash & stretch` | nén/giãn theo hướng chuyển động, giữ thể tích cảm giác |
| `scale punch` | phóng to nhanh rồi về ngay — phản hồi tức thì rẻ nhất |
| `bend / arc` | đi theo cung thay vì đường thẳng — gần như luôn tự nhiên hơn |
| `wobble` | dao động tắt dần quanh trạng thái cuối |
| `recoil` | giật ngược sau va chạm |

## 3. Nhóm & thời gian

| Term | Nghĩa |
|---|---|
| `stagger` | nhiều phần tử chạy lệch nhau một khoảng đều (0.03–0.08s/phần tử) |
| `cascade` | stagger lan theo không gian (từ điểm va chạm ra ngoài) |
| `sequence` | chạy nối tiếp |
| `parallel` | chạy đồng thời |
| `delay` | trễ trước khi bắt đầu |
| `loop / ping-pong / yoyo` | lặp; lặp qua lại |

## 4. Easing — dùng đúng chỗ

| Easing | Cảm giác | Dùng cho |
|---|---|---|
| `linear` | máy móc, vô hồn | thanh tiến trình, chuyển động cơ khí |
| `ease-out` (quad/cubic) | bắt đầu nhanh, dừng mềm | **mặc định** cho hầu hết chuyển động UI/element |
| `ease-in` | khởi động chậm | vật rơi, tăng tốc, biến mất |
| `ease-in-out` | mềm hai đầu | di chuyển camera, chuyển cảnh |
| `ease-out-back` | vượt đích rồi về | nhấn mạnh, xuất hiện, "đã tay" |
| `ease-out-elastic` | nảy nhiều nhịp | vui nhộn — **dùng dè**, dễ ngán |
| `ease-out-bounce` | nảy như bóng | rơi xuống mặt đất |
| `ease-in-back` | lùi lại trước khi đi | anticipation rẻ, gộp trong một tween |

Quy tắc thực dụng: **`ease-out` là mặc định.** `ease-in` cho thứ rời đi. `back`/`elastic` chỉ dành
cho khoảnh khắc đáng nhấn — dùng khắp nơi thì không còn gì nổi bật.

## 5. Lớp phản hồi

| Term | Độ trễ | Ví dụ |
|---|---|---|
| `immediate cue` | 0 frame | đổi màu, scale punch nhỏ, click, haptic |
| `main motion` | 0.15–0.5s | di chuyển, xoay, bay tới đích |
| `aftermath` | sau khi xong | particle tan, số điểm bay lên |

Thiếu `immediate cue` ⇒ luôn bị mô tả là "chậm phản hồi", kể cả khi duration đã ngắn.

## 6. Interrupt policy

| Term | Nghĩa | Dùng khi |
|---|---|---|
| `restart` | kill cũ, chạy lại từ đầu | feedback ngắn lặp nhanh |
| `ignore` | đang chạy thì bỏ lệnh mới | anim không được cắt |
| `queue` | xếp hàng | chuỗi có thứ tự |
| `blend` | tiếp tục từ giá trị hiện tại | đổi đích giữa đường |

## 7. Bảng dịch cảm tính → technical term

| Dev nói | Thường là | Chỉnh gì |
|---|---|---|
| "cứng", "khô" | thiếu `anticipation` / `follow-through` | thêm pha chuẩn bị + đuôi |
| "chưa đã tay" | thiếu `overshoot` + `impact cue` | `ease-out-back`, `scale punch`, VFX/audio |
| "nặng nề", "lê thê" | duration dài hoặc ease sai | giảm duration, chuyển `ease-out` |
| "giật cục" | thiếu `settle`, hoặc tween bị cắt | thêm pha lắng, chọn interrupt policy đúng |
| "trôi tuột", "không trọng lượng" | thiếu `squash & stretch` / gia tốc | biến dạng theo vận tốc |
| "rối mắt" | quá nhiều thứ động cùng lúc | `stagger`, giảm biên độ phụ |
| "chậm phản hồi" | thiếu `immediate cue` | tách cue khỏi main motion |
| "nhàm" | mọi thứ dùng chung một preset, không có nhấn | dành `back`/`elastic` cho khoảnh khắc chính |

## 8. Ngân sách thời lượng (điểm khởi đầu)

| Loại | Duration |
|---|---|
| micro feedback (nút, highlight) | 0.08 – 0.15s |
| element di chuyển ngắn | 0.2 – 0.35s |
| element bay xa / theo curve | 0.35 – 0.6s |
| chuyển state UI/panel | 0.2 – 0.3s |
| celebration / win | 0.8 – 2.0s (có skip) |
| stagger giữa phần tử | 0.03 – 0.08s |

Trên mobile, hành động lặp hàng trăm lần ⇒ **thà ngắn hơn 20% còn hơn dài hơn 20%.**

## 9. Luật khi dùng từ vựng này

1. Đề xuất chuyển động ⇒ nói bằng term ở đây, kèm bảng 5 pha + số.
2. Số cuối cùng nằm ở `MotionProfile` / `TimingProfile`, component giữ `preset id`.
3. Trước khi tạo preset mới ⇒ tra `presets.json`, gộp nếu gần giống.
4. Chốt easing/duration ⇒ dựng `playground.html`, so sánh cạnh nhau, rồi hỏi trắc nghiệm.
