# Decision log — cách viết, supersede, đính chính

> Không sửa file này khi làm game mới.
> Decision log là thứ trả lời câu hỏi đắt nhất của mọi project: **"vì sao nó lại như thế này?"**
> Nếu không có nó, mỗi người mới sẽ đề xuất lại đúng phương án đã bị loại.

---

## 1. Cái gì vào decision-log, cái gì không

| Vào `Docs/decision-log.md` | Vào `implementation-notes.html` của story |
|---|---|
| đổi layer contract / architecture | chia class, đặt tên, chọn algorithm cục bộ |
| đổi gameplay semantics | chọn easing cụ thể trong khoảng đã chốt |
| đổi data contract / schema / serialization | cách tổ chức test |
| chốt một trade-off có ảnh hưởng story sau | trade-off chỉ ảnh hưởng trong story |
| đảo một decision cũ | deviation nhỏ so với story |
| chốt giá trị mà nhiều nơi sẽ đọc | số nội bộ của một component |

Không chắc → hỏi: *"story sau có cần biết cái này không?"* Có → decision log.

---

## 2. Khung một entry

```markdown
## D-0xx — [Tiêu đề nói được KẾT LUẬN, không phải chủ đề]

Status: Accepted — YYYY-MM-DD
Story: `handoff/story-xxx.md`
Supersedes: D-0yy §z   (nếu có)

### Vấn đề
Cái gì đang sai / thiếu. Có bằng chứng thì trích đúng chỗ.

### Chốt
Câu kết luận, đủ để người đọc làm theo mà không cần đọc phần dưới.

### Loại phương án nào, vì sao
- **Loại** X — vì ...
- **Loại** Y — vì ...

### Đánh đổi đã chấp nhận
Cái gì mất đi. Ghi thẳng, đừng giấu.

### Hệ quả
Cái gì phải đổi theo: file · test · doc · story nào bị ảnh hưởng.

### Xem lại khi
Điều kiện cụ thể khiến decision này nên được mở lại.
```

Ba mục **không được bỏ**: `Chốt` · `Loại phương án nào, vì sao` · `Đánh đổi đã chấp nhận`.
Thiếu chúng thì entry chỉ là ghi chú, không phải decision.

Tiêu đề phải nói được kết luận:

- Xấu: `D-041 — Về điều kiện thua`
- Tốt: `D-041 — Thua là một TRẠNG THÁI, không phải hệ quả của một cú tap`

---

## 3. Supersede — đảo một decision cũ

Khi decision mới đảo decision cũ:

1. Entry mới ghi `Supersedes: D-0yy §z` ở header.
2. Entry **cũ** ghi thêm một dòng ngay dưới `Status`:
   `Status: Superseded by D-0xx — YYYY-MM-DD`.
3. **Không xoá** entry cũ. Đường đi tới quyết định là thông tin.
4. Nếu đây là lần đảo **thứ hai trở lên** của cùng một vấn đề → entry mới **bắt buộc** có mục
   `§0 Vì sao đảo tiếp`, ghi rõ bài học. Và story liên quan phải ghi *"đọc D-0xx §0 trước khi viết
   dòng code nào"*.

> Đảo hai lần cùng một vấn đề là tín hiệu: có một tiền đề chưa ai đặt lên bàn.
> Tìm tiền đề đó trước khi chốt lần ba.

---

## 4. Đính chính — sửa một entry đã Accepted

Khi phát hiện một câu trong decision cũ **sai** (không phải đổi hướng, mà là viết sai):

- Sửa **tại chỗ**, trong chính entry đó, bằng một khối đính chính có ngày:

```markdown
> **Đính chính YYYY-MM-DD.** Bản gốc viết "...". Câu đó **sai** vì ...
> Hệ quả: implementation làm đúng theo chữ và vì vậy mang lỗi — **lỗi ở spec, không ở người implement**.
> Chốt: ...
```

- Không tạo entry mới cho một lỗi chính tả ngữ nghĩa — người đọc sẽ đọc entry cũ và làm sai lại.
- Nếu spec sai đã sinh ra bug thật: ghi rõ *"lỗi ở spec, không ở người implement"*. Câu này quan trọng
  hơn nó có vẻ — nó giữ cho việc báo lỗi spec không bị hiểu là đổ lỗi.

---

## 5. Đóng dấu doc lỗi thời

Doc thảo luận / spec cũ bị supersede **phải** có banner ngay dòng đầu:

```markdown
> **HISTORICAL — KHÔNG PHẢI CONTRACT HIỆN HÀNH.** File này giữ reasoning lịch sử, gồm cả
> field/metric đã bị loại. Contract hiện hành ở `Docs/data-model.md`, `Docs/runtime-architecture.md`
> và `Docs/decision-log.md` (D-xxx). Không copy giá trị từ đây vào code.
```

Không có banner thì sớm muộn sẽ có người implement theo doc chết.

---

## 6. Ghi cả thứ cố ý chưa làm

Mỗi decision nên có mục *"Còn mở, cố ý chưa chốt"* khi phù hợp. Ghi rõ:

- cái gì chưa quyết,
- **điều kiện để mở lại** nó.

Ví dụ: *"Difficulty curve 0–1: chưa làm. Mở lại khi GD thực sự cần author pacing liên tục **và** đã có
≥ 1 metric được playtest xác nhận là dự đoán được game feel."*

Không có điều kiện mở lại thì "để sau" sẽ thành "quên".

---

## 7. Đặt số và thứ tự

- Số tăng dần, **không tái sử dụng** số của entry đã xoá.
- Entry mới nằm **trên cùng** file (đọc gần đây trước).
- Trong một story sinh nhiều decision → đánh số liên tiếp, và story ghi rõ *"đọc D-0xx, D-0yy trước
  Phase A"*.

---

## 8. Bảy bài học meta đáng nhắc lại

Khi viết decision, kiểm xem có đang mắc một trong bảy khuôn lỗi này không
(chi tiết ở `standards/anti-patterns.md §A`):

1. Một thứ gánh hai mục đích.
2. Spec mô tả dữ liệu bằng khái niệm không tồn tại trong model.
3. Ràng buộc kỹ thuật ép data model xuống "chuỗi + số cho người gõ tay".
4. Giữ nguồn cũ "làm fallback cho chắc".
5. Luật cấm hình dạng thay vì ngưỡng đo được.
6. Đặt tên theo tính chất mà model không bảo đảm.
7. Copy số/ví dụ từ decision cũ mà không kiểm giá trị hiện tại.
