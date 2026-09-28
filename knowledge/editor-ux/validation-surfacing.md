# Editor — Validation surfacing

> Validation viết đúng mà GD không thấy thì bằng không. File này nói **hiện ở đâu** và **viết thế nào**.

---

## 1. Ba mức

| Mức | Nghĩa | Hành vi | Màu quy ước |
|---|---|---|---|
| `blocking` | level **không chạy được** | **chặn save** | đỏ |
| `warning` | chạy được nhưng nghi ngờ | cho save, hiện cảnh báo | vàng |
| `info` | gợi ý / thống kê | hiện nhẹ | xám/xanh |

Chỉ có hai mức (`lỗi`/`không lỗi`) ⇒ hoặc chặn GD vì thứ không đáng chặn, hoặc để lọt thứ đáng chặn.

**Không dùng riêng màu để truyền đạt mức** — thêm icon hoặc chữ.

## 2. Năm chỗ phải hiện

Thiếu bất kỳ chỗ nào ⇒ sẽ có tình huống GD không thấy lỗi.

| # | Chỗ | Hiện gì | Vì sao cần |
|---|---|---|---|
| 1 | **trên canvas, tại phần tử** | badge/viền trên đúng phần tử sai | GD làm việc ở canvas, mắt ở đó |
| 2 | **trong inspector, tại field** | dòng đỏ/vàng ngay dưới field | biết **field nào** sai, không chỉ "phần tử này sai" |
| 3 | **panel tổng hợp** | danh sách, **click → focus** phần tử | thấy toàn cảnh, sửa lần lượt |
| 4 | **lúc bấm Save** | chặn nếu có blocking, liệt kê | chốt chặn cuối |
| 5 | **lúc Load** | data cũ vi phạm rule mới | rule đổi sau khi level đã làm |

Chỗ số 3 phải **click-to-focus**. Danh sách lỗi không nhảy tới được phần tử thì GD phải tự đi tìm,
và sẽ bỏ qua.

## 3. Viết message

Công thức: **cái gì sai · ở đâu · sửa thế nào.**

| Xấu | Tốt |
|---|---|
| `NullReferenceException in LevelValidator` | `Ô số 3 chưa gán loại nguyên liệu — chọn một loại trong Inspector.` |
| `Invalid config` | `Thời gian giới hạn (12s) ngắn hơn thời gian tối thiểu để hoàn thành (18s) — tăng lên ít nhất 20s.` |
| `count > MAX_ITEMS` | `Có 14 vật thể, tối đa là 12 — xoá bớt 2.` |
| `Missing reference at index 5` | `Chướng ngại thứ 5 chưa có prefab — chọn trong danh sách Obstacle.` |

Luật:

- Không tên class, không tên field code, không exception text.
- Dùng tên trong `Docs/glossary.md` — tên GD nhìn thấy trong tool.
- Có **số cụ thể** khi rule là về số.
- Câu cuối là **hành động**, không phải mô tả.
- Tiếng Việt (text GD đọc), đặt ở file riêng ngoài thư mục bị test gate quét
  (`standards/code-style.md §10`).

## 4. Khi nào chạy validation

| Thời điểm | Chạy gì |
|---|---|
| sau mỗi `ApplyEdit` | validation **của phần bị ảnh hưởng** (phải rẻ) |
| sau `ReloadDocument` / Undo | toàn bộ |
| lúc Save | toàn bộ + chặn nếu blocking |
| nút "Validate" thủ công | toàn bộ + rule nặng (mô phỏng, solver) |

Rule nặng (chạy bot, solver) **không** chạy sau mỗi edit — đưa vào nút thủ công hoặc chạy nền có huỷ.

Sửa xong ⇒ lỗi biến mất **ngay**, không cần bấm lại. Không được thì cache validation đang không
invalidate đúng (`update-model.md`).

## 5. Load file cũ vi phạm rule mới

Tình huống chắc chắn xảy ra: rule thêm sau khi GD đã làm 80 level.

- [ ] **Không tự sửa im lặng.** GD mất công làm lại mà không biết vì sao.
- [ ] Hiện danh sách vi phạm ngay khi mở, phân biệt rõ *"lỗi do rule mới"* với *"lỗi bạn vừa tạo"*.
- [ ] Có nút "sửa tự động" **tuỳ chọn** cho rule sửa được máy móc — và nó phải nói **sẽ sửa gì**
      trước khi sửa.
- [ ] Rule mới quá nghiêm với data cũ ⇒ cân nhắc đưa về `warning` một thời gian.

## 6. Panel tổng hợp — bố cục

```
┌ Validation ────────────────────────── [ ] chỉ hiện lỗi chặn ─┐
│ ● 2 blocking   ▲ 5 warning   ○ 3 info                        │
├──────────────────────────────────────────────────────────────┤
│ ● Ô số 3 chưa gán loại nguyên liệu            → [Đi tới]     │
│ ● Thời gian giới hạn ngắn hơn tối thiểu       → [Đi tới]     │
│ ▲ 3 vật thể cùng loại nằm liền nhau           → [Đi tới]     │
└──────────────────────────────────────────────────────────────┘
```

- Nhóm theo mức, blocking lên đầu.
- Bộ lọc "chỉ hiện lỗi chặn" cho lúc chuẩn bị save.
- Số đếm ở status bar để GD liếc thấy mà không cần mở panel.

## 7. Kiểm chứng

- [ ] Tạo cố ý mỗi loại lỗi → hiện đủ ở cả 5 chỗ
- [ ] Click lỗi trong panel → focus **đúng** phần tử (và scroll tới nếu ngoài màn hình)
- [ ] Sửa lỗi → biến mất ngay
- [ ] Có blocking → không save được, thông báo nói rõ lỗi nào
- [ ] Mở file cũ vi phạm rule mới → báo rõ, không tự sửa
- [ ] Level lớn nhất → validation sau mỗi edit không gây giật
- [ ] Nhờ một người **không** làm tool đọc message → hiểu và sửa được
