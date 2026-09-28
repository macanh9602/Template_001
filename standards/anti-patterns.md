# Anti-patterns — sổ bug đã trả giá

> File này **lớn dần qua các project**. Mỗi mục là một lỗi đã xảy ra thật, đã tốn thời gian thật.
> Cuối mỗi story, `workflow/harvest.md` hỏi: "bug loại nào lặp lại?" → thêm một dòng vào đây.
>
> Cách đọc: mỗi mục có **triệu chứng** (cái thấy được) và **nguyên nhân** (cái phải sửa). Bug loại này
> nguy hiểm vì triệu chứng thường không trỏ về nguyên nhân.

---

## A. Khuôn lỗi tư duy — lặp lại nhiều nhất

### A1. Một thứ gánh hai mục đích
Một tham số vừa là **thước đo** vừa là **tham số điều khiển**; một component vừa là **trọng tài** vừa
là **dụng cụ đo**; một field vừa là **nhãn hiển thị** vừa là **khoá tra cứu**.

Hệ quả: cải thiện mặt này là âm thầm làm hỏng mặt kia. Đã xảy ra **ba lần trong một project**.

→ Thấy một thứ trả lời được hai câu hỏi khác nhau: **tách nó ra**, đặt hai tên.

### A2. Spec mô tả dữ liệu bằng khái niệm nghe hợp lý mà không kiểm nó có tồn tại trong model không
Ví dụ đã gặp: "rank của một màu" (rank là thuộc tính của element, không phải của màu);
"override per-field" (model không có chỗ chứa per-field). Người implement làm **đúng chữ**, bug vẫn ra.

→ Trước khi viết một câu spec về dữ liệu, kiểm: đại lượng này **thực sự** nằm ở đâu trong model?

### A3. Ràng buộc kỹ thuật ép data model xuống dạng "chuỗi + số cho người gõ tay"
Khi thấy mình đang thiết kế `presetId` dạng string GD phải gõ đúng từng ký tự, hoặc `overrideMask`
là số GD phải tự cộng bitmask — **hỏi ngược lại: ràng buộc kỹ thuật đó có bắt buộc không?**
Thường là không, và nó đến từ một tiền đề chưa ai đặt lên bàn (ví dụ "config phải nằm trong level JSON").

### A4. Giữ nguồn cũ "làm fallback cho chắc"
Hai nguồn cho cùng một sự thật thì sớm muộn cũng lệch, và lúc lệch thì công cụ **nói dối về chính
dữ liệu nó đang sửa**. Nguồn thứ hai **là lỗi**, không phải lưới an toàn.

### A5. Luật cấm hình dạng thay vì ngưỡng đo được
"Không được xếp 3 cái liền nhau" mô tả *triệu chứng*. Ngưỡng đo được mô tả *thứ người chơi thật sự
cảm nhận*, và điều khiển được bằng số mà không cần sửa code.

### A6. Đặt tên theo một tính chất mà model không bảo đảm
Gọi một thang là "yếu → mạnh" trong khi các bậc là những heuristic **khác nhau**, giữa chúng không
tồn tại quan hệ trội. Tên hứa một thứ mà cài đặt không giữ được.

### A7. Copy số/ví dụ từ decision cũ mà không kiểm giá trị hiện tại
Ví dụ trong decision cũ dùng bộ tham số cũ; giá trị đã đổi ⇒ ví dụ thành sai. Khi trích decision cũ,
kiểm lại con số theo giá trị **hiện tại**, hoặc ghi rõ "ví dụ này dùng giá trị của thời điểm X".

---

## B. Runtime / gameplay

| # | Triệu chứng | Nguyên nhân |
|---|---|---|
| B1 | Object từ pool bị lệch vị trí / sai kích thước khi parent có scale ≠ 1 | `SetParent(parent, worldPositionStays: true)` **không reset local scale** → child nhận inverse parent scale. Factory phải khôi phục `localScale` từ prefab sau mỗi spawn/reparent |
| B2 | Instance tái dùng mang trạng thái lượt trước (màu, reveal, scale dở) | Pool lifecycle chỉ reset ở **một đầu**. Release phải kill/cancel + neutralize state nguy hiểm; Acquire/Bind phải rebind đầy đủ từ source hiện tại. Chỉ dựa vào state còn sót của lượt trước là lỗi |
| B3 | Một property của shader bị mất giá trị sau khi đổi property khác | `MaterialPropertyBlock.Clear()` rồi chỉ set một property → xoá luôn phần còn lại. Gom vào **một** hàm `WriteBlock()` ghi đủ mọi property |
| B4 | Tap trúng object sai khi hai object chồng nhau | `RaycastNonAlloc` **không sắp theo khoảng cách**, và buffer 1 phần tử trả hit bất kỳ. Buffer đủ lớn + chọn winner bằng **tiêu chí của luật**, không tin thứ tự PhysX |
| B5 | Tap trúng vùng rỗng của object hình lõm | Collider dùng `mesh.bounds` (box) thay vì hình thật |
| B6 | Nhiều object cùng layer bị z-fighting, thứ tự nhìn ≠ thứ tự luật | Depth chỉ encode layer, không encode thứ tự trong layer. Encode phần rank ở transform lúc spawn, chia sub-step theo số object trong layer để không tràn sang layer kế |
| B7 | Nhiều lệnh cùng chiếm một tài nguyên khi người chơi thao tác nhanh | State chỉ đổi sau khi animation xong ⇒ lệnh sau đọc state cũ. Cần **reservation ngay tại thời điểm nhận lệnh** |
| B8 | Game treo: không thao tác được, cũng không báo thua | Thứ duy nhất gỡ được trạng thái lại cần đúng hành động mà trạng thái đó đang chặn. Hàm kiểm phải **tự bơm** (drain/settle trước rồi mới xét), và kiểm kết cục phải chạy sau mỗi mutation |
| B9 | Hai hàm hỏi cùng một câu, trả lời khác nhau | Ví dụ: một hàm quét cả collection, một hàm chỉ lấy phần tử đầu. Mọi hàm trả lời cùng một câu hỏi phải **lọc cùng một điều kiện** |
| B10 | Báo thắng trong khi vẫn còn thứ đang bay / order chưa xong | Điều kiện thắng nhìn sai đại lượng (hết object trên board ≠ hoàn thành mục tiêu). Và khi chạy song song, chỉ kiểm khi `inFlight == 0` |
| B11 | Analytics: level càng khó fail rate càng thấp | Kết cục chỉ được đánh giá khi người chơi tương tác ⇒ người kẹt thoát app không bắn event. Kết cục là **trạng thái**, kiểm sau mỗi mutation |
| B12 | Level override rỗng làm runtime bỏ qua profile global | `JsonUtility` tự dựng lại nested object khi field vắng. Serializer phải **omit** field null; deserializer phải kiểm field có thực sự tồn tại trong chuỗi JSON |
| B13 | Hiệu ứng/visual không chạy dù code đúng | Material đang trỏ shader **không có** property đó. `Initialize` phải log warning một lần khi `sharedMaterial.HasProperty(...)` false, thay vì im lặng |
| B14 | Blocking/overlap tính sai sau khi đổi cách sample geometry | Precondition ngầm "mật độ sample quyết định độ phân giải" không được ghi ở đâu. Tách mật độ của bake khỏi mật độ của mesh, **và** ghi precondition vào doc-comment |
| B15 | Presentation trông như sai logic | Thực ra domain đúng, chỉ là chuỗi visual còn xếp hàng. Đo bằng gameplay capture 30fps, ghi timestamp từng pha trước khi kết luận |
| B16 | Motion mới đang chạy nhưng motion cũ cancel xong lại mở khoá/reset state | Async completion/finally không kiểm **operation owner/generation**. Mỗi motion có id; chỉ active id được clear readiness/final-write |
| B17 | Slot logic đã trống nhưng visual mới đáp vào chồng lên content cũ đang shift | Gộp `logical reservation` với `physical presentation readiness`. Tách hai contract; block presentation synchronously trước async và mở chỉ khi active motion kết thúc |
| B18 | Piece/roll đổi owner rồi mang outline/visibility/scale của pha trước | Ownership boundary không normalize state. Destination owner phải `Adopt/Bind` và ghi đầy đủ presentation state nó sở hữu |
| B19 | Sau partial consume/drain, callback animation cũ làm renderer hiện lại số lượng cũ | Async task giữ **snapshot count/index** rồi final-write sau khi state đã đổi. Re-read current state hoặc validate generation/sequence trước khi render |
| B20 | Planner chọn đúng entity A nhưng consume thực tế rơi sang entity B 'tương đương' | Mutator có fallback mơ hồ (oldest/first match). Khi planner đã chọn stable id/sequence thì consume phải **exact identity**; mismatch ⇒ no-op/fail + audit, không silent fallback |

---

## C. Editor / tool

| # | Triệu chứng | Nguyên nhân |
|---|---|---|
| C1 | Gõ được đúng 1 ký tự vào field rồi mất focus | `RegisterValueChangedCallback` bắn mỗi ký tự → handler rebuild inspector → field đang gõ bị destroy. Field phải `isDelayed = true`; **cấm** rebuild trong value-changed handler |
| C2 | Sửa một field làm mất selection / nhảy tab / nhảy scroll | Handler gọi hàm "reload document" (trong đó có `selection = -1`). Tách `ApplyEdit` ≠ `ReloadDocument` |
| C3 | Undo một phát không về đúng chỗ | Push undo mỗi keystroke / mỗi frame drag. 1 undo entry = **1 ý định của người dùng**, push tại ranh giới gesture |
| C4 | Selection trỏ nhầm object sau khi add/remove | Selection lưu bằng index. Phải lưu bằng **stable id**, remap sau structural mutation |
| C5 | Click một item mà hàm load chạy 4 lần | Đăng ký callback **bên trong** hàm refresh → handler nhân bản. Đăng ký một lần trong `CreateGUI()` |
| C6 | Mất level/mode/zoom sau mỗi lần Unity recompile | Không persist view state. `SessionState` cho phiên, `EditorPrefs` cho preference lâu dài |
| C7 | Editor lag khi kéo | Bake/validate nặng chạy đồng bộ mỗi thay đổi nhỏ. Debounce ~100–200 ms, và **không** chạy giữa gesture |
| C8 | Nút xám mà không biết vì sao | `SetEnabled(false)` trần. Nút disable **phải** có tooltip nêu lý do |
| C9 | Bảng đối chiếu giấu mất tài nguyên thừa | Chỉ loop theo một chiều (theo demand). Phải là **union** của cả hai chiều |
| C10 | Tính năng "chưa từng dùng được" nhưng code đọc vẫn hợp lý | Không có đường UI nào để tạo dữ liệu (thiếu nút Add). Acceptance của story có UI **bắt buộc** gồm một lần đi hết luồng bằng tay |
| C11 | Cùng một khái niệm ba màu khác nhau ở ba màn hình | Ba nơi tự định nghĩa bảng màu riêng. Một nguồn màu dùng chung |
| C12 | Overlay nổi che mất thứ khác | Overlay "luôn nổi" nghĩa là luôn che một thứ gì đó. Dùng cột thật + splitter, để layout engine lo |
| C13 | Refactor chrome làm vỡ mode khác | Đổi ngữ nghĩa của thứ dùng chung (direction của container, góc màn hình) mà không rà lại mọi nơi đang dựa vào nó. Chrome không có test tự động ⇒ **phải mở từng mode kiểm bằng mắt** |
| C14 | Class USS khai báo mà không có tác dụng | Code set inline style thay vì gán class → theme không đổi được |
| C15 | Editor và runtime hiển thị khác nhau cho cùng một dữ liệu | Hai bản sao logic. Trích thành **một** helper dùng chung; test parity là thứ bảo chứng "editor = in-game" |
| C16 | Biểu đồ/label đúng lúc mở nhưng lệch khi kéo splitter | Vị trí hardcode pixel hoặc chỉ build một lần. Tính từ resolved content rect và rebuild theo `GeometryChangedEvent` |
| C17 | Laptop/window hẹp làm control bị cắt, canvas mất chỗ | Pane width cố định, không có collapse/responsive priority. Dùng split view + min width + secondary collapse/wrap/scroll |
| C18 | Panel nổi che mode bar/canvas/global action | Persistent information được làm absolute overlay. Chuyển thành real pane có splitter |
| C19 | Object ẩn/ghost vẫn bị select/move/delete | Visibility và editability bị trộn. Tách `Visible / Editable / Selected`; filter hit-test + selection ngay khi state đổi |
| C20 | Sau Save/reload callback chạy nhiều lần | Callback được register trong refresh/rebuild. Register một lần; refresh chỉ cập nhật data; unsubscribe external events |
| C21 | Toolbar thành một hàng 20+ control hoặc mode khác vẫn hiện tool không liên quan | Tool/action/context không phân tầng. Tách tool/options/context; global action cố định; controls scoped theo mode |
| C22 | Bảng rộng mất cột neo hoặc marquee sai sau scroll | Không thiết kế scroll ownership/content coordinates. Horizontal scroll thật + pin orientation anchor + pointer→content transform |


---

## D. Quy trình

| # | Triệu chứng | Nguyên nhân |
|---|---|---|
| D1 | Fix ba vòng vẫn không đúng | Theory-craft về runtime state thay vì instrument. Fix thứ hai **phải** dựa trên data thật |
| D2 | Story chạy chậm, nhiều lần hỏi lại | Story liệt kê từng class phải tạo ⇒ dev đang thay agent nghĩ, hoặc story quá lớn |
| D3 | Đảo hướng thiết kế nhiều lần cùng một vấn đề | Thêm một lớp gián tiếp mà **không ai yêu cầu**, mua một tính chất chưa ai cần. Hỏi: tính chất này có phải yêu cầu thật không? |
| D4 | Số liệu đổi mà không ai biết | Thứ đóng vai dụng cụ đo không có version + golden test |
| D5 | Doc cũ bị dùng như contract hiện hành | Doc bị supersede không đóng dấu. Gắn banner `HISTORICAL — contract hiện hành ở X` ngay đầu file |
| D6 | Test xanh nhưng ý định của test đã bị vi phạm | Lách test bằng cách đặt code ra ngoài vùng quét mà không sửa comment của test. Đổi ý định của một gate = **deviation công khai**, sửa cả comment tại chỗ |
| D7 | Claim "đã pass" rồi phát hiện chưa chạy | Không phân biệt `PASS` / `PENDING`. Xem `workflow/verification.md` |
