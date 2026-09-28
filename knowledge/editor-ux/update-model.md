# Editor — Update Model

> Bảng này là hợp đồng cập nhật của mọi tool authoring. Trộn các đường cập nhật với nhau là nguyên
> nhân gốc của phần lớn bug editor.

---

## Ba tầng state

```
Document      — dữ liệu thật, sẽ serialize. Nguồn sự thật duy nhất.
ViewState     — selection, zoom, pan, tab, filter. KHÔNG serialize vào level file.
DerivedState  — preview, validation result, thống kê. Tính ra từ Document, read-only trên UI.
```

## Bảng 4 trigger × 6 cột

| Trigger | Document | ViewState | DerivedState | Undo | Repaint | Ghi chú |
|---|---|---|---|---|---|---|
| **`ApplyEdit(change)`**<br>user sửa một thứ | mutate phần liên quan | **giữ nguyên** | rebuild **phần bị ảnh hưởng** | push 1 entry | vùng liên quan | đường đi phổ biến nhất — phải rẻ |
| **`ReloadDocument(doc)`**<br>mở file / đổi level | thay **toàn bộ** | reset có kiểm soát; selection cố khôi phục theo **id** | rebuild toàn bộ | **không** push | toàn bộ | không dùng cho edit thường |
| **`ChangeView(v)`**<br>zoom / pan / chọn / filter | **không đụng** | mutate | không rebuild (trừ derived phụ thuộc selection) | **không** push | vùng nhìn | dirty flag **không** đổi |
| **`Undo/Redo`** | khôi phục snapshot Document | **giữ nguyên** view; selection khôi phục theo id | rebuild toàn bộ | pop / push | toàn bộ | undo không được làm nhảy zoom |

## Luật rút ra

1. **`ApplyEdit` ≠ `ReloadDocument`.** Dùng reload cho mọi edit ⇒ tool nhảy, mất selection, mất zoom,
   chậm. Dùng ApplyEdit khi cần reload ⇒ derived cũ còn sót, hiển thị sai.
2. **`ChangeView` không làm dirty.** Zoom rồi bị hỏi "lưu không?" là dấu hiệu ViewState đang lẫn vào
   Document.
3. **Selection bằng stable id**, không phải index — thêm/xoá phần tử không được làm chọn nhầm.
4. **Undo chỉ ghi Document.**
5. **DerivedState không bao giờ là nguồn sự thật.** UI cho sửa derived ⇒ sửa xong mất khi rebuild.

## Khai báo phụ thuộc derived

Mỗi loại edit khai báo nó làm bẩn derived nào. Không có bảng này thì hoặc rebuild thừa (chậm), hoặc
rebuild thiếu (hiển thị sai).

| Loại edit | Derived bị bẩn |
|---|---|
| đổi vị trí phần tử | preview layout · validation vị trí · thống kê phân bố |
| đổi thuộc tính phần tử | validation của phần tử đó · thống kê liên quan |
| thêm / xoá phần tử | **toàn bộ** derived |
| đổi setting level | toàn bộ derived |

Nghi ngờ ⇒ rebuild toàn bộ (đúng nhưng chậm) rồi tối ưu sau bằng đo, không đoán.

## Batching

Thao tác sinh nhiều edit liên tiếp (kéo thả, nhập liệu, thao tác hàng loạt):

```
BeginBatch("Move elements")
   ... nhiều ApplyEdit, KHÔNG rebuild derived, KHÔNG push undo riêng lẻ
EndBatch()   → rebuild derived một lần, push MỘT undo entry
```

Không batch ⇒ kéo một phần tử tạo 60 undo entry và 60 lần rebuild.

## Field UX gắn với model này

| Vấn đề | Cách làm |
|---|---|
| mỗi ký tự gõ trigger `ApplyEdit` | `isDelayed = true` cho field số/text |
| kéo slider gây rebuild nặng | preview lúc kéo (ViewState), `ApplyEdit` lúc thả |
| thao tác hàng loạt | `BeginBatch`/`EndBatch` |

## Kiểm chứng

- [ ] Gõ vào field: zoom/pan/selection không đổi
- [ ] Kéo một phần tử: **một** undo entry
- [ ] Undo: view không nhảy
- [ ] Xoá phần tử khác: selection hiện tại không đổi
- [ ] Zoom/pan: dirty flag không bật
- [ ] Sửa xong: validation cập nhật **ngay**, không cần bấm nút
- [ ] Mở file khác rồi undo: không trộn hai document
