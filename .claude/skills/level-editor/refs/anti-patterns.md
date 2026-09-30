# Level editor — anti-patterns

> Triệu chứng → nguyên nhân gốc → cách sửa. Harvest từ các story editor thực tế.

## Input / update / state

| Triệu chứng | Nguyên nhân | Sửa |
|---|---|---|
| gõ được 1 ký tự rồi mất focus | field callback rebuild inspector/ancestor mỗi keystroke | `isDelayed`, update targeted label/state; không rebuild ancestor |
| một từ tạo 10 undo | push undo trong `valueChanged` | commit boundary = field commit / gesture start-end |
| sửa field làm mất selection/scroll | dùng `ReloadDocument`/`BindDocument` cho local edit | `ApplyEdit`, preserve ViewState |
| mixed field blur thành 0 | empty/mixed state bị parse như value thật | no-op nếu user chưa commit explicit value |
| chọn nhầm sau add/remove/sort | selection lưu index | stable id + remap |
| callback chạy 2/4/8 lần sau Save | register callback trong `RefreshXxx` | register once in `CreateGUI`, refresh chỉ update data |
| sửa code xong window mất level/mode/zoom | không persist ViewState | `SessionState` + dirty autosave/restore |
| undo làm view nhảy | ViewState nằm trong undo/document snapshot | undo chỉ Document |

## Selection / visibility

| Triệu chứng | Nguyên nhân | Sửa |
|---|---|---|
| item ẩn vẫn bị marquee/select/delete | visibility được coi như editability | tách `Visible / Editable / Selected`; hidden/non-editable phải lọc khỏi hit-test/selection |
| cần thấy layer làm mốc nhưng lại select trúng | visible reference không có ghost/read-only state | Active layer editable, layer khác ghost |
| đổi layer xong selection biến mất không hiểu vì sao | item rời editable scope mà không feedback | remove selection + notification/hint |
| rời mode rồi quay lại thấy selection ẩn sống dậy | transient mode-selection được persist sai | clear selection khi rời mode nếu semantics không còn |

## Canvas / geometry

| Triệu chứng | Nguyên nhân | Sửa |
|---|---|---|
| zoom nhỏ không bấm trúng handle | hit radius theo world scale | screen-space radius |
| kéo object thì canvas pan | gesture collision | input contract riêng cho pan/drag |
| zoom/pan lệch dần | nhiều phép convert toạ độ | một cặp authoritative transform |
| chart label đúng lúc mở nhưng sai sau resize | hardcode pixel + build label một lần | `GeometryChangedEvent` + resolved content rect |
| `100%` bị clip ở pane nhỏ | anchor tính tâm/offset mà không account text bounds | layout theo measured geometry + edge padding |
| marquee sai khi scroll | dùng viewport coordinate cho content | convert pointer → content coordinates |

## Workspace / responsive

| Triệu chứng | Nguyên nhân | Sửa |
|---|---|---|
| không kéo được cột, laptop nhỏ gần như không dùng được | width cố định trong USS | `TwoPaneSplitView`, min width, collapse policy |
| panel nổi che mode bar/canvas | persistent tool dùng absolute overlay | real pane + splitter |
| resize window làm mất control | không có responsive priority/wrap/scroll | primary canvas/tool giữ lại; secondary pane collapse/wrap/scroll trước |
| bảng dài làm cột nở | content không nằm trong ScrollView riêng | per-pane scroll |
| bảng rộng bị cắt hoặc ô teo | thiếu horizontal scroll | fixed readable cell + horizontal ScrollView; pin anchor column |
| splitter resize làm tool giật | geometry change trigger full data rebuild | resize chỉ layout/repaint, không rebuild model |

## Toolbar / chrome

| Triệu chứng | Nguyên nhân | Sửa |
|---|---|---|
| toolbar 20+ control, không biết cái nào quan trọng | tool/action/context trộn một hàng | tách tool column · options bar · context bar |
| sang Tray vẫn thấy tool curve | toolbar không scoped theo mode | hide irrelevant controls |
| nút nhảy vị trí khi selection đổi | action biến mất thay vì disabled | action quen thuộc giữ chỗ + disabled + tooltip |
| đổi CSS direction làm mode khác xếp dọc full-width | shared chrome semantic đổi nhưng chỉ test mode đang sửa | manual walkthrough **mọi mode** sau chrome refactor |
| overlay/status che control góc canvas | absolute UI dùng chung góc mà không audit | real layout slot hoặc reserved HUD area |

## Validation / information architecture

| Triệu chứng | Nguyên nhân | Sửa |
|---|---|---|
| GD không thấy lỗi mode khác | issue chỉ nằm trong panel hiện tại | badge theo mode + global validation summary |
| nút Save xám không biết vì sao | `SetEnabled(false)` trần | tooltip blocker + remaining count |
| xem Difficulty thì mất Inspector cần đối chiếu | hai thông tin đồng thời bị nhét vào mutually-exclusive tab | nếu workflow cần nhìn cùng lúc → hai pane thật |
| metadata lấn át thông tin hành động | mọi metric/detail ở cùng hierarchy | progressive disclosure, foldout details, primary signals ở top |
| control chỉ có nghĩa khi có context nhưng vẫn hiện | dead control | conditional visibility hoặc disabled+reason tùy tính ổn định layout |

## Save / runtime parity

| Triệu chứng | Nguyên nhân | Sửa |
|---|---|---|
| level editor nhìn đúng nhưng runtime khác | editor copy logic/resolver riêng | shared serializer/validator/resolver; parity test |
| Play Test chỉ chạy bản Save cũ | runtime chỉ load Resources asset | temp/override document hiện tại → LevelManager load một lần |
| mở file cũ hỏng sau schema change | không version/migration | version từ đầu + migration test |
| crash lúc save làm mất file | overwrite trực tiếp | temp → atomic replace |

## Performance

| Triệu chứng | Nguyên nhân | Sửa |
|---|---|---|
| panel lag theo số entity | linear `Find()` trong nested traversal | build dictionary/index một lần |
| click selection rebuild hàng trăm element không cần | full `Bind`/`Clear` | update class/text targeted; đặt performance threshold nếu full rebuild tạm chấp nhận |
| drag giật | bake/validate/AssetDatabase trong pointer move | repaint lightweight khi drag, debounce heavy derived work ở gesture end |
