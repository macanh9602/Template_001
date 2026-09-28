# Roadmap — [TÊN GAME]

> Bản đồ story. Cập nhật **mỗi khi đóng một story**, không phải mỗi tháng.
> Sizing theo `workflow/loop.md`: **S** (nửa ngày) · **M** (1–2 ngày) · **L** (chia mốc).

---

## Giai đoạn hiện tại

**Playbook đang chạy:** `playbooks/pX-....md`
**Mốc gần nhất:** [cái gì sẽ chạy được sau 1–2 story tới]

---

## Story

| # | Tên | Size | Trạng thái | Phụ thuộc | Decision liên quan |
|---|---|---|---|---|---|
| 001 | | S/M/L | TODO / DOING / DONE / CUT | | |

Trạng thái theo `workflow/verification.md` — **DONE nghĩa là đã kiểm chứng thật**, không phải
"code xong".

**Story bị CUT phải ghi lý do cắt.** Cắt mà không ghi lý do thì 2 tháng sau sẽ có người làm lại.

---

## Mốc

| Mốc | Nghĩa là gì (quan sát được từ bên ngoài) | Story cần xong | Trạng thái |
|---|---|---|---|
| Vertical slice | đường dây layer thông suốt, element hiện từ data | 001–003 | |
| Playable loop | vào level → chơi → thắng/thua → chơi lại | | |
| Editor ready | GD tự làm được level | | |
| Difficulty measurable | trả lời được "level này khó bao nhiêu" bằng số | | |
| Feel pass | người ngoài cầm máy thấy đã tay | | |
| Ship candidate | build release chạy trên thiết bị yếu nhất | | |

---

## Rủi ro đang theo dõi

| Rủi ro | Dấu hiệu sẽ thấy | Ứng phó | Trạng thái |
|---|---|---|---|

---

## Câu hỏi còn mở ở mức dự án

| Câu hỏi | Owner | Chặn cái gì | Hạn |
|---|---|---|---|

Câu hỏi trong phạm vi một story ⇒ `handoff/story-XXX/open-questions.html`, không để ở đây.

---

## Nợ kỹ thuật

| Nợ | Từ story | Vì sao chấp nhận | Phải trả khi |
|---|---|---|---|

Nợ từ technical slice ghi ở đây ngay khi slice kết thúc, đừng đợi tới lúc ship.
