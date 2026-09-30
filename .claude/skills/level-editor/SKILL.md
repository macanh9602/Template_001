---
name: level-editor
description: >
  Dựng tool authoring cho GD/level designer bằng UI Toolkit — EditorWindow, canvas, inspector,
  validation, save/load. KÍCH HOẠT khi: "làm level editor", tool cho GD, authoring window, cần GD
  tự tạo/sửa level mà không nhờ dev. Nguyên tắc lõi: Document / ViewState / DerivedState tách bạch,
  và ApplyEdit ≠ ReloadDocument.
---

# SKILL: level-editor

Thước đo duy nhất của một level editor: **GD tự làm được một level hoàn chỉnh mà không hỏi dev.**
Không phải "tool có đủ field".

---

## Bước đầu tiên — đọc context

`Docs/data-model.md` (level data hiện tại) · `knowledge/editor-ux/update-model.md` ·
`knowledge/editor-ux/validation-surfacing.md` · `knowledge/editor-ux/responsive-workspace.md` ·
`refs/checklist.md` · `refs/anti-patterns.md` · `Docs/glossary.md` (tên GD dùng ≠ tên trong code).

---

## 1. Mô hình ba tầng — nền của mọi thứ

```
Document      — dữ liệu thật, sẽ được serialize. Nguồn sự thật duy nhất.
ViewState     — trạng thái nhìn: selection, zoom, pan, tab đang mở, filter.
DerivedState  — thứ tính ra từ Document: preview, validation result, thống kê.
```

Luật:

| Luật | Vì sao |
|---|---|
| ViewState **không bao giờ** được serialize vào level file | mở file trên máy khác ra layout khác nhau |
| DerivedState **không bao giờ** là nguồn sự thật | sửa derived → mất khi rebuild |
| Selection lưu bằng **stable id**, không phải index | thêm/xoá phần tử → chọn nhầm thứ khác |
| Undo chỉ ghi Document | undo mà nhảy zoom là trải nghiệm tệ |

Trộn ba tầng là nguyên nhân gốc của phần lớn bug editor. Nếu chỉ nhớ một điều từ skill này, nhớ điều này.

## 2. `ApplyEdit` ≠ `ReloadDocument`

Hai đường cập nhật **hoàn toàn khác nhau**, không được gọi thay nhau:

| | `ApplyEdit(change)` | `ReloadDocument(doc)` |
|---|---|---|
| khi nào | user sửa một thứ | mở file / undo / đổi level |
| Document | mutate phần liên quan | thay toàn bộ |
| ViewState | **giữ nguyên** | reset có kiểm soát (selection cố gắng khôi phục theo id) |
| DerivedState | rebuild phần bị ảnh hưởng | rebuild toàn bộ |
| Undo | push một entry | không push |

Dùng `ReloadDocument` cho mọi thay đổi ⇒ tool "nhảy" mỗi lần gõ, mất selection, mất zoom, và chậm.
Dùng `ApplyEdit` khi thực sự cần reload ⇒ derived state cũ còn sót, hiển thị sai.

Bảng đầy đủ 4 trigger × 6 cột: `knowledge/editor-ux/update-model.md`.

## 3. Canvas viewport contract

Editor có canvas ⇒ chốt trước bốn thao tác này, đừng để mỗi chỗ làm một kiểu:

| Thao tác | Định nghĩa |
|---|---|
| `Zoom` | quanh **con trỏ**, không quanh tâm view |
| `Pan` | middle-drag hoặc space-drag; không tranh chấp với drag phần tử |
| `Fit` | vừa toàn bộ nội dung + padding |
| `Frame` | vừa **selection** hiện tại |

Luật handle: **handle vẽ ở screen-space**, kích thước không đổi theo zoom. Handle scale theo zoom
⇒ zoom nhỏ thì không bấm trúng, zoom lớn thì handle che hết nội dung.

Chuyển đổi toạ độ phải đi qua **đúng một cặp hàm** `WorldToCanvas` / `CanvasToWorld`. Có hai chỗ tự
tính toạ độ ⇒ chắc chắn sẽ lệch.

## 4. Field UX

| Vấn đề | Cách làm |
|---|---|
| gõ số mà mỗi ký tự trigger rebuild | `isDelayed = true` cho mọi field số/text |
| kéo slider gây rebuild nặng | chỉ apply lúc thả, preview lúc kéo |
| field không nói được đơn vị | ghi đơn vị vào label (`Delay (s)`) |
| tên field theo code, GD không hiểu | dùng tên trong `glossary.md`, tooltip tiếng Việt |
| giá trị mặc định vô nghĩa | default phải là **cấu hình chạy được**, không phải 0 |

Text hiển thị: label/button **English ASCII** (có test gate quét literal), tooltip và text GD đọc
để tiếng Việt, đặt ở file riêng ngoài thư mục bị quét — xem `standards/code-style.md §10`.

## 5. Validation — quan trọng hơn field

| Mức | Nghĩa | Hành vi |
|---|---|---|
| `blocking` | level không chạy được | **chặn save**, hiện rõ |
| `warning` | chạy được nhưng nghi ngờ | cho save, hiện cảnh báo |
| `info` | gợi ý | hiện nhẹ |

Message viết cho GD: **cái gì sai · ở đâu · sửa thế nào**. Không dùng tên class, không dùng
exception message.

Validation phải xuất hiện ở **5 chỗ** (chi tiết: `knowledge/editor-ux/validation-surfacing.md`):

1. ngay tại phần tử trên canvas
2. tại field trong inspector
3. panel tổng hợp có click-to-focus
4. lúc bấm Save
5. lúc load file cũ (data cũ có thể vi phạm rule mới)

Chỉ hiện ở một chỗ ⇒ GD sẽ không thấy.

## 6. Save / Load

- **Version trong file.** Ngay từ file đầu tiên. Thêm version sau khi đã có 200 level là ác mộng.
- **Migration có test.** Mỗi lần đổi schema: hàm migrate + test load file version cũ.
- **Save phải atomic** — ghi file tạm rồi rename. Crash giữa lúc ghi không được làm mất level.
- **Không tự động sửa data lúc load.** Data cũ vi phạm rule mới ⇒ báo warning, để GD quyết.
  Tự sửa im lặng ⇒ GD mất công làm lại mà không biết vì sao.
- Dirty flag thật: so với bản đã lưu, không phải "có thao tác nào chưa". Hỏi "lưu không?" khi thực
  sự chưa lưu — hỏi thừa vài lần là GD tắt não bấm Discard.

## 7. Bố cục cửa sổ

```
┌──────────────────────────────────────────────┐
│ Toolbar: New Open Save | Validate | Zoom Fit │
├────────────┬─────────────────────┬───────────┤
│ Hierarchy  │      Canvas         │ Inspector │
│ (danh sách)│  (chỉnh trực tiếp)  │ (chi tiết)│
├────────────┴─────────────────────┴───────────┤
│ Status bar: đường dẫn · dirty · lỗi/cảnh báo │
└──────────────────────────────────────────────┘
```

Status bar hay bị bỏ qua nhưng là chỗ GD liếc nhìn nhiều nhất: **đang mở file nào, đã lưu chưa,
còn bao nhiêu lỗi.**

## 8. Editor UX robustness — bắt buộc ngay từ story đầu

Level Editor là **desktop production tool**, nên correctness chưa đủ. Agent phải thiết kế luôn cho nhập
liệu dài, domain reload, resize pane và laptop một màn hình. Contract đầy đủ ở
`knowledge/editor-ux/responsive-workspace.md`.

### Input / focus

- Field text/số dùng commit policy rõ; mặc định `isDelayed = true` nếu edit kéo theo derived rebuild.
- Không rebuild ancestor đang chứa field trong callback của chính field.
- 1 gesture = 1 undo; drag không push undo theo frame.
- Mixed multi-edit blur mà không nhập gì = no-op, không tự thành 0.

### Pane / responsive

- Cột persistent dùng `TwoPaneSplitView` hoặc split-view built-in; có min width + collapse policy.
- Canvas/primary tool là ưu tiên số 1 khi window hẹp. Secondary pane collapse/wrap/scroll trước.
- Không dùng floating overlay cho panel luôn cần đọc; overlay rất dễ che mode bar/canvas/global action.
- Resize window/pane phải được coi là một interaction chính thức, không phải edge case.

### Geometry / scroll

- UI phụ thuộc kích thước thật phải rebuild theo `GeometryChangedEvent`; cấm hardcode vị trí tick/
  label/marker theo pixel ban đầu.
- Panel dài scroll riêng. Bảng rộng scroll ngang thật; pin orientation anchor (rank/header) nếu cần.
- Marquee trong ScrollView tính theo content coordinates.

### State continuity

Sửa field / add-remove / undo-redo / switch mode / domain reload không được vô tình reset:
`stable selection · mode/tool · zoom/pan · scroll · pane width · search/filter`.

`visible`, `editable`, `selected` là ba state khác nhau. Hidden/ghost/non-editable item không được còn
trong selection để rồi bị move/delete âm thầm.

### Chrome

Global action giữ vị trí cố định. Tool irrelevant mode thì hide; action quen thuộc nhưng tạm unavailable
thì disable + tooltip. Toolbar đông phải chia theo `tool / options / context`, không nhồi một hàng.

Nếu Unity official plugin skills khả dụng: dùng `ui-uitk` cho UXML/USS/UI Toolkit implementation và
`unity-cli` để inspect/verify Editor thật; project skill này vẫn là contract UX/domain cấp cao.

---

## 9. Thứ tự làm — đừng đảo

```
1. Data model + save/load + version        ← chưa lưu được thì tool vô nghĩa
2. Hiển thị read-only (mở file, thấy level)
3. Sửa một loại phần tử, có undo
4. Validation
5. Các loại phần tử còn lại
6. Tiện lợi: copy/paste, multi-select, shortcut, template
7. Preview / mô phỏng trong editor
```

Làm canvas đẹp trước khi save/load chạy ⇒ luôn phải làm lại.

---

## 10. Đầu ra

1. Spec editor (Phần 3 của `spec-feature`, hoặc spec riêng nếu tool lớn).
2. Tool trong project + level file mẫu.
3. **Hướng dẫn cho GD** — ngắn, có ảnh, theo `skills/gd-communication/`.
4. Chạy `refs/checklist.md` trước khi giao GD.

Trước khi tuyên bố xong: **tự làm một level hoàn chỉnh bằng chính tool đó**, từ file trống tới file
chạy được trong game. Không làm bước này thì mọi lời "đã xong" đều là phỏng đoán —
`workflow/verification.md` yêu cầu UI walkthrough thật.
