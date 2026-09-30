# Phân tích video/gif reference

> Dev gửi video gameplay và nói *"làm giống cái này"*. File này là quy trình biến video thành
> spec implement được — và quan trọng hơn: **tách cái đáng học ra khỏi cái không nên copy.**

---

## Luật số 1 — mô tả lại trước khi làm

**Không bao giờ** xem video rồi code luôn. Luôn viết bản mô tả, gửi dev confirm.

Lý do: video chứa hàng chục quyết định thiết kế chồng lên nhau. Dev thường chỉ muốn **một hoặc hai**
trong số đó. Không confirm ⇒ copy nhầm phần không cần, bỏ sót phần cần.

Câu hỏi mở đầu bắt buộc:

> *"Trong video này, thứ anh muốn là [X] hay [Y]?"* — kèm mô tả kỹ thuật hai thứ đó.

---

## Bước 1 — timeline theo mốc thời gian

Mô tả lại theo giây, ngôn ngữ kỹ thuật, không cảm thán:

```markdown
## Video ref — [tên/link] — đoạn 0:12–0:18

| t (s) | Thấy gì | Thuật ngữ |
|---|---|---|
| 0.00 | element nén xuống ~85% chiều cao, giữ ~0.1s | anticipation, squash |
| 0.10 | bật lên nhanh, vượt đích rõ rệt | ease-out back, overshoot ~18% |
| 0.28 | rung nhẹ 1 nhịp rồi đứng yên | settle, 1 oscillation |
| 0.30 | particle bụi ở chân, trễ hơn thân | follow-through |
| 0.32 | các element bên cạnh nảy nhẹ theo thứ tự trái→phải | stagger ~0.05s |
```

Không đọc được chính xác thời gian ⇒ ghi **khoảng** (`~0.1s`) và đánh dấu là ước lượng.
**Không bịa số chính xác từ video** — số chính xác chỉ có sau khi tune trên visualiser.

## Bước 2 — tách 4 lớp

Video trộn lẫn nhiều lớp. Tách ra để biết lớp nào thực sự tạo ra cảm giác:

| Lớp | Trong video có gì | Có cần không? |
|---|---|---|
| **Motion** | quỹ đạo, timing, easing | |
| **Deform** | squash/stretch, scale punch | |
| **Cue** | VFX, particle, flash, trail | |
| **Audio/haptic** | tiếng, rung | |

Rất thường: cảm giác "đã tay" trong video đến từ **Cue + Audio**, không phải Motion. Copy motion mà
bỏ cue ⇒ làm xong vẫn thấy thiếu, rồi đổ lỗi cho easing.

## Bước 3 — tách "cái đáng học" khỏi "cái không nên copy"

| Đáng học | Không nên copy nguyên |
|---|---|
| nguyên tắc (có anticipation, có overshoot) | số cụ thể — khác kích thước màn, khác scale game |
| thứ tự các pha | art style, tỉ lệ nhân vật |
| tương quan timing giữa các phần | thứ phụ thuộc engine/tool của họ |
| lớp cue nào đi kèm lớp motion nào | thứ chỉ hợp với loại input khác (chuột vs chạm) |

Ghi rõ mục **"Trong video có nhưng ta KHÔNG làm"** + lý do. Thiếu mục này thì scope sẽ trôi và
sau đó không ai nhớ vì sao bỏ.

## Bước 4 — đối chiếu ràng buộc của mình

Trước khi chốt, kiểm tra từng dòng spec với:

- `standards/performance-budget.md` — video của họ có thể chạy trên PC.
- `standards/system-design.md` — chuyển động này thuộc Visual; nó **không được** quyết gameplay.
- Nhịp lặp: hành động này người chơi làm bao nhiêu lần/phút? Video thường quay lần **đầu tiên**,
  không phải lần thứ 200. Anim đẹp ở lần 1 có thể là tra tấn ở lần 200.
- `knowledge/motion/presets.json` — đã có preset gần đúng chưa?

## Bước 5 — dựng visualiser mô phỏng lại, rồi hỏi

Dựng `.html` chạy phiên bản đọc được từ video **cạnh** phiên bản hiện tại của game (nếu có), có
slider và slow-motion 0.25×. Rồi hỏi trắc nghiệm:

- mức overshoot: 0% / 10% / 18% (theo video) / khác
- có anticipation không: có / không / chỉ ở hành động lớn
- stagger: không / 0.03s / 0.05s (theo video)

Mỗi câu **một recommend** kèm lý do dựa trên nhịp lặp và performance budget.

---

## Khung output

```markdown
# Motion ref analysis — [element] — nguồn: [video, đoạn t1–t2]

## Ta muốn học gì từ video
1. ...

## Timeline đọc được
(bảng ở Bước 1 — đánh dấu rõ số nào là ước lượng)

## Bốn lớp
(bảng ở Bước 2)

## Trong video có nhưng ta KHÔNG làm
| Cái gì | Vì sao bỏ |
|---|---|

## Spec đề xuất
(bảng 5 pha theo SKILL.md + preset id + nơi giữ số)

## Câu hỏi cần chốt
(trắc nghiệm, kèm visualiser)
```

---

## Bẫy

| Bẫy | Hậu quả |
|---|---|
| copy số từ video làm số cuối | scale/màn hình/nhịp khác → sai cảm giác |
| bỏ qua lớp audio/VFX | làm đúng motion vẫn thấy "chưa đã" |
| xem video rồi code luôn | copy nhầm phần dev không muốn |
| quên video quay lần chơi đầu | anim quá dài cho vòng lặp thật |
| mô tả bằng tính từ ("mượt", "đã") | dev và agent hiểu hai kiểu |
| lấy ref từ game khác thể loại/input | cảm giác không chuyển sang được |
