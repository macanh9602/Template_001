# Mobile Performance Budget — và cách đo

> Không sửa file này khi làm game mới. Ngưỡng riêng của game (nếu khác) ghi vào
> `Docs/runtime-architecture.md §6`.
>
> Nguyên tắc: **một con số không kèm cách đo là một con số vô nghĩa.** Mỗi dòng dưới đây có cột
> "đo bằng gì" — acceptance criteria phải trích đúng cách đo đó, không được ghi "đã tối ưu".

---

## 1. Ngân sách mặc định

| Chỉ số | Ngưỡng | Đo bằng gì |
|---|---|---|
| Frame time | 16.6 ms (60fps) trên máy target; tối thiểu 33.3 ms (30fps) trên máy thấp | Unity Profiler trên **device thật**, không phải Editor |
| GC alloc trong gameplay loop | **0 B/frame** | Profiler → GC Alloc column; hoặc EditMode test chạy N lần sau warm-up và assert `GC.GetTotalMemory` không tăng |
| Draw call gameplay | ≤ 60 | Frame Debugger / Stats overlay trên device |
| Level load | ≤ 300 ms | `Stopwatch` quanh `LoadLevelAsync`, log ra console |
| Bake / generate (nếu có) | ≤ 50 ms cho level lớn nhất hiện có | EditMode performance test, ghi số thật vào notes |
| Query gameplay (legal move, pick, occupancy) | O(1) từ counter, hoặc O(out-degree) từ adjacency | đọc code + test số phần tử duyệt |
| Interaction frame (có tap) | 1 raycast · 0 allocation | Profiler frame có tap |
| Frame không tương tác | 0 raycast · 0 `Update` per-element | Profiler + grep `void Update()` |
| Texture | ASTC, ≤ 2048 | Import settings |
| Mesh memory | ghi số thật cho level mẫu | Memory Profiler |
| Editor responsiveness (tool) | thao tác thường < 100 ms; > 50 ms thì hiện trạng thái "computing…" | `Stopwatch` trong editor, ghi vào notes |

Xung đột giữa convenience và performance → **performance thắng**, trừ khi dev chốt khác.

---

## 2. Cách viết acceptance criteria về performance

Sai:
```
- [ ] Tối ưu allocation trong pick path
- [ ] Draw call hợp lý
```

Đúng:
```
- [ ] 1,000 lượt pick sau warm-up: 0 B managed allocation (EditMode test tên X)
- [ ] Level 30 element: bake < 50 ms — ghi số đo thật vào implementation notes
- [ ] Frame không tap: 0 raycast, 0 GC — Profiler capture trên <tên device>
```

Nếu chưa đo được thì ghi `PENDING` kèm lý do, **không** ghi `PASS`. Xem `workflow/verification.md`.

---

## 3. Đo cái gì ở giai đoạn nào

| Giai đoạn | Đo | Không cần đo |
|---|---|---|
| Technical slice | chi phí của **rủi ro chính** (bake time, mesh size, cook time, draw call của cơ chế mới) | frame time tổng thể |
| Core loop | GC trong interaction path · graph update complexity · cleanup không leak | GPU |
| Level editor | editor responsiveness · bake debounce · repaint không allocation | mọi thứ runtime |
| Difficulty / generation | thời gian generate × retry · số simulation move · thời gian một lần Fill | GPU, draw call |
| Feel pass | draw call thêm do effect · overdraw · pool peak · GC của tween | bake time |
| Ship | **tất cả**, trên device thật | — |

---

## 4. Những chỗ tốn mà hay bị bỏ sót

- **Collider cook.** Gán `sharedMesh` mới mỗi lần spawn từ pool ⇒ PhysX cook lại. Đo ms/instance và
  tổng cho level lớn nhất. Vượt ngưỡng → giảm `cookingOptions` (bỏ mesh cleaning / weld) rồi đo lại,
  hoặc dùng collision mesh tối giản (chỉ mặt cần raycast, không normal/UV).
- **Full-screen effect** (outline, blur, bloom). Là một pass toàn màn hình — chi phí không tỉ lệ số
  object nhưng vẫn đắt trên máy thấp. Phải có phương án hạ cấp sẵn (chỉ áp cho object đang tương tác).
- **Transparent overdraw.** Ưu tiên opaque + clip thay vì alpha blend/pulse khi visual cho phép.
- **`MaterialPropertyBlock` phá SRP Batcher.** Chấp nhận được nhưng số property per renderer tăng thì
  phải đo trên máy thật, không suy luận.
- **Editor-only asset lọt vào `Resources/`.** Bị build vào app, ăn build size mà không dùng tới.
  Config authoring để **ngoài** `Resources/`.
- **`Resources.Load` đầu tiên** rơi vào frame đang chơi. Chạm nó lúc **load level**, không phải giữa
  gameplay.
- **Serialize/IO trong interaction frame.** Buffer rồi flush cuối run hoặc trên thread-pool.
- **String formatting trong hot path.** Dùng bảng chuỗi tra sẵn cho số nhỏ.
- **Retry × simulation.** `attempts × (generate + simulate)` nổ rất nhanh. Instrument
  `attempt count` · `generate ms` · `simulate ms` · `total ms` **trước khi** ai đó tăng `maxRetry`.

---

## 5. Bắt buộc ghi vào implementation notes

Mỗi story chạm runtime phải có khối **Performance** với đúng 5 dòng:

```
CPU:              <cái gì chạy, bao lâu, đo ở đâu>
GPU / draw call:  <tăng/giảm bao nhiêu, vì sao>
GC:               <có allocation trong loop không, đo thế nào>
Memory:           <mesh/texture/pool tăng bao nhiêu>
Measurement:      <tên test / capture / device — hoặc "chưa đo, lý do X">
```

Dòng cuối không được để trống. "Chưa đo" là câu trả lời hợp lệ; "đã tối ưu" thì không.
