# Verification — verify thế nào và báo cáo trung thực

> Không sửa file này khi làm game mới.
> Nguyên tắc: **`PASS` chỉ được ghi khi đã thật sự chạy.** Không chạy được là câu trả lời hợp lệ;
> claim sai thì không.

---

## 1. Bảng trạng thái — dùng đúng những chữ này

| Status | Nghĩa | Bắt buộc kèm |
|---|---|---|
| `PASS` | đã chạy, đã xanh | evidence: tên test + số lượng, hoặc log/capture |
| `FAIL` | đã chạy, đỏ | lỗi cụ thể + đang xử lý thế nào |
| `PENDING` | chưa chạy được | **lý do cụ thể** (ví dụ: Unity MCP không có session, project đang bị instance khác giữ, chưa có device) |
| `PENDING RERUN` | đã chạy trước đó nhưng code đã đổi sau đó | ghi rõ số liệu cũ đã stale |
| `PARTIAL` | một phần xanh, một phần chưa | liệt kê rõ phần nào là phần nào |
| `KNOWN BASELINE FAILURES` | có test đỏ **có sẵn từ trước story** | số lượng + tên nhóm + xác nhận không phải do story này |
| `N/A` | không áp dụng | **lý do** — không để trống |

Cấm dùng: "đã tối ưu", "hoạt động tốt", "ổn", "đã kiểm tra" mà không có evidence.

---

## 2. Bảng Verification trong implementation notes

Mỗi story có đúng một bảng dạng này:

```
| Check                | Status  | Evidence                                              |
|----------------------|---------|-------------------------------------------------------|
| Compile              | PASS    | Unity <ver> domain reload xong; console 0 error       |
| EditMode tests       | PASS    | 71/71 qua Unity Test Runner job <id>                  |
| PlayMode tests       | PENDING | Unity MCP chưa có Editor session trong phiên này      |
| Console sạch         | PASS    | MainScene Play smoke 6s: 0 error/warning              |
| Acceptance           | PARTIAL | mục 1–7 xong; mục 8 chờ device                        |
| Performance          | PASS    | 1,000 pick sau warm-up = 0 B alloc (test X)           |
| Manual UI walkthrough| PASS    | đi hết luồng: tạo → sửa → save → load                 |
```

---

## 3. Thang verify — làm từ rẻ tới đắt

```
1. Static compile          dotnet build từng csproj — chạy được kể cả khi Unity bận
2. Unity compile           domain reload, đọc Editor.log
3. EditMode test           rule / baker / validator / generator — không cần scene
4. PlayMode test           lifecycle, cleanup, cancel token
5. Console smoke           vào Play, để chạy vài giây, xác nhận 0 error/warning
6. Screenshot / capture    visual, HUD, tool UI
7. Manual walkthrough      đi hết luồng bằng tay (BẮT BUỘC với story có UI)
8. Device profiling        frame time, GC, draw call, memory trên máy target
```

Bậc 1–2 luôn làm được. Bậc 3–6 cần Unity MCP hoặc dev chạy giúp. Bậc 8 luôn là gate ngoài, ghi rõ
là việc của dev/QA.

---

## 4. Unity MCP — có thì tự verify

Có kết nối Unity MCP thì agent **phải** tự làm, không hỏi dev:

- refresh/compile và đọc console
- chạy Test Runner (EditMode + PlayMode), lấy số test pass/fail
- vào Play mode, chạy smoke, đọc console
- chụp screenshot làm evidence cho visual/tool

Không kết nối được thì ghi `PENDING` + lý do, và **liệt kê chính xác** những bước dev cần chạy giúp:

```
Manual verification steps:
1. Mở Unity, Window → General → Test Runner → EditMode → Run All
2. Play MainScene 10s, xác nhận console 0 error
3. Mở <Tool> window, thao tác: ... → xác nhận ...
```

---

## 5. Bốn tình huống hay gặp và cách ghi

**Project đang bị Unity instance khác giữ** → không chạy batchmode được.
→ `PENDING` + *"project đang mở ở Unity instance khác; không kill process của dev"*.
Vẫn làm được: static compile từng csproj, và đọc `Editor.log` để xác nhận build success.

**Test đỏ có sẵn từ trước story** → `KNOWN BASELINE FAILURES`, ghi số lượng + tên nhóm + câu xác nhận
*"không test nào của story này đỏ, không warning mới"*. Không được im lặng bỏ qua, cũng không được
sửa test cũ để cho xanh.

**Số đo cũ đã stale sau khi sửa tiếp** → `PENDING RERUN`, ghi rõ *"số liệu 42/42 bên dưới có trước khi
di chuyển file"*.

**Không đo được perf vì thiếu device** → khối Performance vẫn phải có đủ 5 dòng, dòng
`Measurement` ghi *"chưa đo trên device; cần capture Profiler trên `<máy target>`"*. Không suy luận
thay số đo.

---

## 6. Story có UI — walkthrough là bắt buộc

Lỗi chí mạng của tool (không có nút để tạo dữ liệu, phase mặc định chồng lấn nhau, field không bao
giờ render) **chỉ lộ ra khi thao tác thật**. Đọc code không thấy, unit test không bắt.

Acceptance của mọi story có UI phải có một dòng:

```
- [ ] Đi hết luồng bằng tay một lần: <bước 1> → <bước 2> → ... → <kết quả kỳ vọng>
```

---

## 7. Dụng cụ đo phải có version + golden test

Bất kỳ thứ gì sinh ra **con số để đánh giá** (simulator, scorer, evaluator, metric calculator) là
**dụng cụ đo**:

- có `Version` và `Name` đọc được;
- mọi report đóng dấu version;
- có **characterization test** trên một fixture cố định.

Sửa dụng cụ đo ⇒ test đỏ ⇒ buộc bump version **trong cùng commit**. Không có cơ chế này thì số liệu
của mọi level đổi mà không ai được báo.

---

## 8. Execution lifecycle — IMPLEMENTED ≠ DONE

Dùng execution state ở roadmap/story:

| State | Nghĩa |
|---|---|
| `PLANNED` | capability mới ở roadmap, chưa mở |
| `LOCKED` | semantics/dependency đã biết nhưng gate trước chưa mở |
| `EXECUTABLE` | đủ input + gate, worker được bắt đầu |
| `IMPLEMENTING` | đang code |
| `IMPLEMENTED` | code/scope xong; closure evidence có thể còn pending |
| `VERIFYING` | đang chạy closure evidence |
| `BLOCKED` | dependency/decision/evidence gate chặn |
| `DONE` | mọi required closure gate PASS |
| `SUPERSEDED` | historical, không execute |

Hard dependency/entry gate khác manual acceptance thông thường:
nếu packet ghi `ENTRY/HARD GATE`, `PENDING` nghĩa là **BLOCKED downstream**.

---

## 9. Trước khi báo DONE

```
[ ] Compile pass, console sạch (không error/warning mới do story gây ra)
[ ] Mọi acceptance có status đúng theo bảng §1, không dùng chữ mơ hồ
[ ] Mọi acceptance PASS đều có evidence cụ thể
[ ] Mọi PENDING đều có lý do và bước thủ công tương ứng
[ ] Khối Performance đủ 5 dòng, dòng Measurement không trống
[ ] Story có UI: đã đi hết luồng bằng tay
[ ] implementation-notes.html cập nhật
[ ] Project-level decision đã vào decision-log
[ ] Harvest đã trả lời 4 câu (workflow/harvest.md)
```
