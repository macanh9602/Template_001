# D-XXX — [Tiêu đề nói được KẾT LUẬN, không phải chủ đề]

> ✗ *"Bàn về cách lưu level data"* — đây là chủ đề.
> ✓ *"Level data dùng JSON một file mỗi level, có version field"* — đây là kết luận.
>
> Copy vào **đầu** `Docs/decision-log.md` (mới nhất lên trên).
> Ba mục **Chốt** / **Loại phương án nào, vì sao** / **Đánh đổi đã chấp nhận** không được bỏ.

---

**Status:** Proposed / Accepted / Superseded by D-YYY — YYYY-MM-DD
**Story:** `handoff/story-XXX/`
**Supersedes:** D-YYY *(nếu có)*

## Vấn đề

Cái gì buộc phải quyết. Nêu ràng buộc thật, không nêu sở thích.

## Chốt

Quyết định, viết đủ cụ thể để người khác làm theo mà không hỏi lại.

## Loại phương án nào, vì sao

| Phương án | Vì sao loại |
|---|---|
| | |

Mục này quan trọng ngang mục **Chốt**. Thiếu nó thì 3 tháng sau sẽ có người đề xuất lại đúng phương
án đã loại, và không ai nhớ vì sao đã loại.

## Đánh đổi đã chấp nhận

Cái gì **xấu đi** vì quyết định này. Ghi rõ, đừng giấu.

Không có đánh đổi nào ⇒ hoặc chưa nghĩ đủ, hoặc đây không phải quyết định đáng ghi.

## Hệ quả

- Code / data / workflow phải đổi gì
- Story nào bị ảnh hưởng
- Ràng buộc mới cho story sau (cập nhật `Docs/project-context.md`, đánh 🔒 nếu là contract toàn project)

## Xem lại khi

Điều kiện cụ thể khiến quyết định này nên được mở lại. Không phải "khi cần", mà là dạng:
*"khi số element cùng lúc vượt N"*, *"khi cần hỗ trợ nền tảng Z"*.

---

<!--
## Còn mở

Chưa chốt được thì vẫn ghi entry, Status = Proposed, và ghi rõ:
- Đang chờ gì (dữ liệu nào, ai quyết)
- Điều kiện để chốt
- Cái gì đang bị chặn

Entry Proposed treo lơ lửng tới lúc ship là dấu hiệu quy trình có vấn đề — `playbooks/p7-ship.md` Bước 6.
-->

<!--
## Supersede

Decision cũ **không xoá**. Giữ nguyên, thêm ở đầu:

> ⚠️ **HISTORICAL — superseded by D-YYY.** Giữ lại để hiểu bối cảnh. Không làm theo.

Decision mới ghi `Supersedes: D-XXX` và nêu **cái gì đã đổi khiến quyết định cũ không còn đúng**.

**Đảo chiều lần thứ hai** (D-A → D-B → quay lại gần D-A) ⇒ bắt buộc thêm mục:

### §0 Bài học
Vì sao vòng lặp này xảy ra, và dấu hiệu nào lẽ ra phải nhận ra sớm hơn.

Không có mục này thì vòng lặp sẽ lặp lần thứ ba.
-->

<!--
## Đính chính

Phát hiện một decision cũ ghi **sai sự thật** (không phải đổi ý — mà là ghi sai):

> **Đính chính YYYY-MM-DD:** dòng "..." là sai. Thực tế là "...". Nguyên nhân ghi sai: ...

Đính chính ghi **thêm vào**, không sửa đè lên chữ cũ — để thấy được cả sai lầm lẫn cách phát hiện.
-->
