# Level editor — checklist trước khi giao GD

> Tự tay chạy hết. Mỗi mục ghi PASS / FAIL / N/A theo `workflow/verification.md`.
> Mục nào chưa chạy thì ghi **PENDING**, không được ghi PASS.

## A. Vòng đời file

- [ ] New → document trống hợp lệ
- [ ] Save / Save As → đúng file, format, version; không ghi đè nhầm
- [ ] Open → round-trip đúng từng field
- [ ] File version cũ → migrate/báo rõ; không sửa im lặng
- [ ] File hỏng/schema lạ → báo lỗi đọc được, level đang mở không hỏng
- [ ] Dirty close/domain reload → có guard hoặc autosave/restore
- [ ] Save có blocking error → bị chặn + nói rõ lý do

## B. Input / focus / undo

- [ ] Gõ **12 ký tự liên tục** vào field → đủ 12, không mất focus, **1 undo entry**
- [ ] Field số/text cần derived rebuild dùng `isDelayed`; không rebuild mỗi keystroke
- [ ] Sửa field → selection, mode, inspector scroll **không nhảy**
- [ ] Field trống / xoá hết → không tự commit `0`/`NaN`
- [ ] Mixed-value multi-edit: blur không nhập gì → no-op
- [ ] Slider/drag 1 gesture dài → **1 undo entry**; Ctrl+Z về đúng start
- [ ] Undo/Redo không đổi zoom/pan/pane width ngoài ý muốn
- [ ] Undo sau structural edit khôi phục đúng stable selection/order

## C. Selection semantics

- [ ] Selection lưu bằng stable id, không index
- [ ] Add/remove/sort không làm chọn nhầm entity khác
- [ ] Xoá entity đang chọn → chọn nearest sensible target hoặc empty state rõ
- [ ] `visible`, `editable`, `selected` tách nhau
- [ ] Hide/chuyển layer khỏi editable → item bị loại khỏi selection hoặc notify rõ
- [ ] Ghost/reference visible nhưng không hit-test/edit được
- [ ] Multi-select bulk edit áp đúng tập, một undo

## D. Canvas / viewport

- [ ] Zoom quanh con trỏ; Pan không tranh chấp drag
- [ ] Fit / Frame đúng; shortcut chỉ active đúng mode
- [ ] Handle/hit radius screen-space-safe ở zoom min/max
- [ ] Một cặp `WorldToCanvas`/`CanvasToWorld` authoritative
- [ ] Marquee trong ScrollView dùng content coordinates
- [ ] Kéo ngoài vùng hợp lệ → block/warn, không tạo data rác
- [ ] Trong drag nặng: không bake/validate mỗi pointer move

## E. Workspace / responsive / splitter

- [ ] Pane persistent dùng split view; **kéo resize được**
- [ ] Mỗi pane có min width hợp lý, không ép canvas về 0
- [ ] Collapse secondary pane → canvas fill phần trống, layout không vỡ
- [ ] Double-click/reset splitter (nếu support) về default hợp lý
- [ ] Close/reopen/domain reload → pane width giữ đúng
- [ ] Resize window xuống khoảng **640px** → primary workflow vẫn dùng được, không control quan trọng bị cắt
- [ ] Context bar wrap/scroll/collapse theo policy; primary tool là thứ cuối cùng biến mất
- [ ] Persistent info không dùng overlay che mode bar/global actions/canvas

## F. Scroll / geometry-dependent UI

- [ ] Panel dài scroll **trong pane của nó**, không đẩy pane lớn ra
- [ ] Grid rộng có horizontal scroll thật; không shrink cell tới mức mất đọc
- [ ] Header/rank/orientation anchor được pin nếu scroll làm mất nghĩa
- [ ] Resize pane bằng splitter → chart tick/label/overlay tự cập nhật
- [ ] Không có hardcoded axis position kiểu `Left + 74f`; dùng content rect + `GeometryChangedEvent`
- [ ] Nhãn biên (`100%`, giá trị max...) không bị clip ở pane min-width

## G. Toolbar / chrome

- [ ] Global actions (Undo/Redo/Play Test/Save) **không đổi vị trí** giữa mode
- [ ] Tool không thuộc mode hiện tại không clutter toolbar
- [ ] Action quen thuộc unavailable → disable + tooltip, không làm layout nhảy
- [ ] Toolbar đông được tách `tool / options / context`, không 20+ control một hàng
- [ ] Đổi tool chỉ rebuild phần options cần thiết
- [ ] Sau chrome refactor, **mở từng mode bằng mắt**: không mode nào xếp dọc/full-width ngoài ý muốn
- [ ] Tool active nhận ra bằng liếc mắt; tooltip có tên + shortcut

## H. Callback / domain reload

- [ ] Callback register một lần; refresh list không register thêm
- [ ] External Editor events unsubscribe đúng lifecycle
- [ ] Reopen/domain reload không làm subscriber count tăng
- [ ] Persist: document path · mode/tool · stable selection · zoom/pan · search/filter · pane width
- [ ] Dirty doc không mất khi dev recompile script giữa lúc GD đang làm

## I. Validation

- [ ] Lỗi hiện ở canvas · inspector · panel · Save · load
- [ ] Click issue → switch mode nếu cần + select + frame target
- [ ] Message: sai gì · ở đâu · sửa thế nào
- [ ] Fix xong issue biến mất đúng lúc, không cần reload tool
- [ ] Disabled Save/Play Test có tooltip nêu blocker đầu tiên + số lỗi còn lại

## J. Data ↔ runtime / Play Test

- [ ] Level từ editor chạy trực tiếp trong game, không convert tay
- [ ] Editor/runtime dùng chung serializer/validator/resolver khi trả lời cùng một câu hỏi
- [ ] **Play Test document chưa Save** chạy đúng document hiện tại qua override/temp path
- [ ] Level tối thiểu và level lớn nhất dự kiến đều hoạt động
- [ ] Sau Play Test quay lại Editor không mất document/context

## K. Hiệu năng editor

- [ ] Level lớn nhất dự kiến → authoring còn responsive
- [ ] Không full-panel/full-canvas rebuild từ một edit nhỏ nếu không cần
- [ ] Không AssetDatabase scan / FindObjects / material-texture allocation trong repaint/hot editor path
- [ ] Splitter resize không rebuild data model nặng
- [ ] Model/index dùng dictionary/cache thay linear scan lặp trong nested loop

## L. Bàn giao GD

- [ ] Tự làm một level hoàn chỉnh từ blank → Save → Play Test
- [ ] Có hướng dẫn ngắn 5–10 bước + ảnh
- [ ] Quan sát ít nhất một lần GD dùng thật; điểm họ khựng lại được ghi thành issue
- [ ] Ghi rõ tool **chưa làm được gì** để tránh user đi tìm chức năng không tồn tại
