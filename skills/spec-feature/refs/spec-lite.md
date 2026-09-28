# Spec-lite — bản rút gọn cho prototype

> Dùng khi GDD **chưa lock**, đang thử nghiệm, hoặc feature nhỏ (< 1 ngày code).
> Mục tiêu: đủ để implement không phải hỏi lại, nhưng **không khoá kiến trúc**.

---

## Khi nào dùng spec-lite thay vì spec đầy đủ

| Dùng spec-lite | Dùng spec đầy đủ |
|---|---|
| feature đang thử, có thể vứt | feature chắc chắn ship |
| < 1 ngày implement | nhiều story phụ thuộc vào nó |
| chỉ mình dev đụng | GD/Art cần đọc để làm việc |
| không tạo contract mới cho story sau | tạo `Provides:` cho story sau |
| không có editor tooling | có tool cho GD |

**Nghi ngờ → spec đầy đủ.** Spec-lite mà phải nâng cấp giữa chừng thì đắt hơn viết đủ từ đầu.

---

## Khung

```markdown
# [Feature] — spec-lite

## Mục tiêu
1 câu. Người chơi làm được gì / thấy gì mới.

## Cách hoạt động
3–7 gạch đầu dòng. Trigger → xử lý → kết quả.

## Chạm vào đâu
| Layer | Việc |
|---|---|
| Domain | |
| Visual | |
| Data | |

## Số liệu tune được
| Giá trị | Nằm ở đâu | Default |
|---|---|---|
Mọi số phải có chỗ ở: prefab field · Profile SO · level data. **Không hardcode.**

## Edge case đã nghĩ tới
- ...

## Chưa quyết
- [ ] (câu hỏi + option đề xuất)

## Ranh giới — KHÔNG làm trong lần này
- ...
```

Phần **"KHÔNG làm trong lần này"** là bắt buộc. Spec-lite không có phần này thì scope sẽ trôi.

---

## Nâng cấp lên spec đầy đủ khi

Gặp **bất kỳ** dấu hiệu nào dưới đây thì dừng, viết spec đầy đủ:

- Phát sinh contract mà story khác sẽ dựa vào.
- Cần prefab structure mới hoặc profile mới.
- Cần GD nhập liệu (⇒ có editor setup ⇒ `skills/level-editor/`).
- Xuất hiện > 1 phương án kiến trúc hợp lý (⇒ `enrich-context` + trắc nghiệm).
- Ước lượng vượt 1 ngày.

Ghi lại lý do nâng cấp vào `implementation-notes.html` — đây là dữ liệu tốt cho `workflow/harvest.md`
để hiệu chỉnh ngưỡng sizing.
