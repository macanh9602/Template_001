# Editor UX — Responsive Workspace Contract

> Harvest từ các lỗi Level Editor thực tế: input mất focus, fixed columns, overlay che chrome, chart
> label hardcode pixel, hidden selection, toolbar quá tải và state mất sau domain reload.

## 1. Workspace là resizable desktop tool, không phải layout cố định

Ưu tiên `TwoPaneSplitView`/split-view built-in cho các cột thật. Không tự viết floating panel + drag +
resize nếu thông tin cần nhìn thường xuyên.

Mỗi pane khai:

- min width để vẫn usable;
- preferred/default width;
- collapse policy;
- scroll ownership;
- width persist trong `SessionState` nếu Unity không giữ đủ context;
- double-click splitter reset về default nếu project cần.

Persistent information (Inspector, Difficulty, hierarchy) → **real pane**. Floating overlay chỉ dùng cho
transient HUD/tooltip; overlay tuyệt đối không được che mode bar, global actions hoặc canvas interaction.

## 2. Responsive priority

Khi window hẹp, không đơn giản `display:none` ngẫu nhiên. Chốt thứ tự ưu tiên:

1. canvas / primary authoring surface;
2. current tool controls;
3. global Save/Undo/Play Test;
4. context cần cho thao tác hiện tại;
5. secondary inspector/analysis.

Secondary pane có thể collapse; context bar có thể wrap thành 2 hàng; panel dài phải scroll. Tool chính
là thứ cuối cùng được phép biến mất.

Baseline manual checks nên gồm ít nhất: width nhỏ (~640px), laptop phổ biến (~1280px) và rộng. Đây là
verification baseline, không phải pixel contract cố định cho mọi project.

## 3. Input/focus/undo

- `TextField`/`IntegerField`/`FloatField`: mặc định `isDelayed = true` khi edit làm rebuild/validate.
- Không rebuild inspector/container đang chứa field trong `valueChanged` của chính field đó.
- Slider: preview nhẹ khi kéo; heavy derived rebuild/undo commit ở gesture end.
- **1 user intent = 1 undo entry**. Drag 200 frames vẫn là 1 undo.
- Mixed-value multi-edit: blur mà user không nhập gì ⇒ no-op; không commit `0`/empty.
- Sửa field không được làm mất focus, selection, mode, scroll, zoom/pan.

## 4. Callback lifecycle

- Register callbacks một lần trong `CreateGUI`/construction path.
- Refresh function chỉ update `itemsSource`, class, text, enabled state rồi `Rebuild` phần cần thiết.
- Không register callback bên trong `RefreshXxx()`.
- External/editor event (`playModeStateChanged`, `undoRedoPerformed`, `projectChanged`...) phải unsubscribe.
- Domain reload/reopen không được làm subscriber tăng dần.

## 5. Geometry-aware UI

Mọi vị trí phụ thuộc bề rộng/chiều cao thực phải tính từ layout thật:

- `resolvedStyle` / `contentRect` / `resolvedContentRect`;
- rebuild label/overlay trong `GeometryChangedEvent`;
- không hardcode kiểu `Left + 74f`, `x=150` cho tick/axis/marker.

Acceptance: resize pane bằng splitter thì tick, label, chart overlay, hit area vẫn đúng mà không cần
reopen window.

## 6. Scroll ownership

- Pane dài → scroll dọc riêng, không ép cả workspace cao/rộng theo content.
- Grid rộng → horizontal scroll thật; không shrink ô tới mức mất readability.
- Header/rank/row label là orientation anchor ⇒ pin/freeze nếu mất nó làm user không đọc được bảng.
- Marquee trong `ScrollView` dùng **content coordinates**, không viewport coordinates.
- Tránh nested scroll cùng một trục nếu không có lý do rõ.

## 7. Visible ≠ Editable ≠ Selected

Ba state phải tách:

- **Visible**: có được vẽ không;
- **Editable/Active**: có hit-test/move/delete được không;
- **Selected**: target hiện tại của action.

Ẩn hoặc chuyển object sang group/layer không editable ⇒ loại khỏi selection ngay hoặc notify rõ. Không
để user nhấn Delete và xoá một object đang không nhìn thấy.

Ghost/reference object có thể visible nhưng không editable/hit-test.

## 8. Toolbar/chrome

- Global actions đứng cố định giữa mode: Undo/Redo/Play Test/Save.
- Tool-specific control chỉ hiện trong mode sở hữu nó.
- Toolbar nhiều hơn một hàng logic ⇒ chia `tool column` / `options bar` / `context bar`, không đổ 20+
  control vào một hàng phẳng.
- Action có vị trí quen thuộc nên **disable thay vì biến mất** khi tạm unavailable; tooltip nói vì sao.
- Control hoàn toàn không liên quan mode hiện tại thì hide/remove để giảm clutter.
- Đổi tool chỉ rebuild options của tool; không rebuild toàn toolbar/context/canvas.

## 9. Context survives development

Persist ViewState cần thiết qua domain reload/reopen:

- document path;
- mode/tool;
- stable selection id;
- zoom/pan;
- relevant scroll position;
- splitter/pane widths;
- search/filter;
- active analysis tab/mode.

Dirty document cần autosave/restore hoặc explicit guard trước domain reload nếu project workflow thường
recompile trong khi GD đang mở tool.

## 10. Manual acceptance tối thiểu

- nhập chuỗi 12 ký tự liên tục: không mất focus, 1 undo;
- sửa field khi đang selected: selection/mode/scroll giữ nguyên;
- drag 1 gesture: 1 undo;
- resize mọi splitter đến min/max, collapse/reopen, layout không che nhau;
- resize window ~640px: primary workflow vẫn dùng được, không control quan trọng bị cắt mất;
- resize chart/panel liên tục: label/tick đúng theo geometry;
- domain reload: document/mode/selection/zoom/pane width quay lại đúng;
- hide/ghost layer: hidden/non-editable item không còn trong selection;
- scroll bảng rộng/dài: anchor header/rank vẫn đọc được;
- mở từng mode sau chrome refactor: toolbar không xếp sai hướng, không overlay che global action.
