# P4 — Level editor

**Vào khi:** core loop chạy, cần nhiều level và/hoặc GD tham gia.
**Ra khi:** GD tự làm được một level hoàn chỉnh, chạy được trong game, **không hỏi dev**.

Skill chính: `skills/level-editor/` (+ `refs/checklist.md`, `refs/anti-patterns.md` trong thư mục đó).
Tham chiếu: `knowledge/editor-ux/`.

---

## Bước 1 — Chốt level data trước khi chốt UI

Sai lầm phổ biến nhất là bắt đầu từ giao diện.

- [ ] Level data schema chốt trong `Docs/data-model.md`
- [ ] **Version field ngay từ file đầu tiên**
- [ ] Cái gì là source of truth, cái gì generated lúc load — ghi rõ
- [ ] Round-trip test: data → save → load → data (so từng field)

Runtime đang đọc level data theo cách khác với cái editor sẽ ghi ⇒ hợp nhất **bây giờ**, không phải sau.

## Bước 2 — Chốt GD workflow trước khi chốt field

Hỏi GD (trắc nghiệm): *"làm một level anh sẽ thao tác theo thứ tự nào?"*

Thứ tự thao tác của GD **≠** thứ tự field trong data. Tool dựng theo thứ tự data sẽ khó dùng dù đủ field.

Ghi lại 5–10 bước GD thực sự làm. Đây là xương sống của bố cục cửa sổ.

## Bước 3 — Dựng theo đúng thứ tự

```
1. Data model + save/load + version + migration
2. Hiển thị read-only (mở file → thấy level)
3. Sửa một loại phần tử + undo
4. Validation
5. Các loại phần tử còn lại
6. Tiện lợi: copy/paste, multi-select, shortcut, template
7. Preview / mô phỏng trong editor
```

Mỗi bước là một story riêng. Đừng gộp.

## Bước 4 — Ba tầng state ngay từ story đầu

`Document` / `ViewState` / `DerivedState` — tách bạch từ đầu, không refactor sau.

- [ ] ViewState không serialize vào level file
- [ ] Selection bằng **stable id**
- [ ] `ApplyEdit` ≠ `ReloadDocument` (bảng đầy đủ: `knowledge/editor-ux/update-model.md`)
- [ ] Undo chỉ ghi Document

## Bước 5 — Validation là feature, không phải phần thêm

- [ ] Phân mức `blocking` / `warning` / `info`
- [ ] Message theo ngôn ngữ GD: sai gì · ở đâu · sửa sao
- [ ] Xuất hiện đủ **5 chỗ** (`knowledge/editor-ux/validation-surfacing.md`)
- [ ] Blocking chặn save
- [ ] Load file cũ vi phạm rule mới ⇒ warning, **không tự sửa im lặng**

## Bước 6 — Ngôn ngữ & tên gọi

- [ ] Label/button **English ASCII**, có test gate quét literal
- [ ] Tooltip + text GD đọc: **tiếng Việt**, ở file riêng ngoài thư mục bị quét
- [ ] Tên field theo `Docs/glossary.md`; tên mới ⇒ thêm vào glossary trong cùng story

## Bước 7 — Tự làm một level bằng chính tool đó

Bắt buộc, trước khi bàn giao. Từ file trống tới file chạy được trong game.
Chỗ nào mình thấy khó chịu thì GD sẽ thấy không dùng được.

## Bước 8 — Bàn giao

- [ ] Chạy hết `skills/level-editor/refs/checklist.md`
- [ ] Hướng dẫn ngắn cho GD (ảnh + 5–10 bước) — `skills/gd-communication/`
- [ ] Ngồi xem GD dùng thử **một lần**; ghi lại mọi chỗ họ khựng
- [ ] Ghi rõ "tool chưa làm được gì" để GD không mất công tìm

---

## Xong khi

- [ ] GD làm được level hoàn chỉnh không cần hỏi
- [ ] Level đó chạy trong game không cần sửa tay
- [ ] Checklist pass hết (mục chưa chạy ghi PENDING, không ghi PASS)
- [ ] Mở được file version cũ
- [ ] Level lớn nhất dự kiến: editor không giật

## Bẫy

Bảng đầy đủ: `skills/level-editor/refs/anti-patterns.md`. Ba cái hay gặp nhất:

1. **`ReloadDocument` cho mọi edit** → tool nhảy, mất selection.
2. **Không có version từ đầu** → tới lúc cần migration thì đã có hàng trăm level.
3. **Dev chưa từng tự làm một level bằng tool** → bàn giao xong mới phát hiện thiếu thao tác cơ bản.
