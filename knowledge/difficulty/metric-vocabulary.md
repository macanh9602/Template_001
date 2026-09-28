# Difficulty — từ vựng đo lường

> Mỗi metric ghi rõ **cái nó KHÔNG nói lên**. Đây là cột quan trọng nhất — chỗ GD (và cả dev)
> đọc quá đà và ra quyết định sai.
>
> Copy bảng này vào `Docs/glossary.md §3` của từng game, giữ lại cột "KHÔNG nói lên".

---

## 1. Trục độ khó

Chọn **2–3 trục** cho mỗi game, ghi rõ trục nào **không** dùng.

| Trục | Câu hỏi | Chỉ số điển hình |
|---|---|---|
| `constraint` | người chơi bị bó hẹp bao nhiêu | `movesSlack`, slot trống, thời gian dư |
| `search` | phải nghĩ nhiều không | `branchingAvg`, độ sâu nhìn trước cần thiết |
| `execution` | tay có phải chính xác không | cửa sổ thời gian, dung sai vị trí |
| `information` | có bị che thông tin không | tỉ lệ chưa lộ, độ ngẫu nhiên |
| `recovery` | sai một bước có cứu được không | `deadEndRate`, khoảng cách tới dead end |

## 2. Metric đo bằng bot

| Metric | Đo bằng | Nói lên | **KHÔNG nói lên** |
|---|---|---|---|
| `passRate` | thắng / N lần chạy | độ khó cảm nhận | **vì sao** khó; level có vui không |
| `avgMoves` | trung bình nước đi ván thắng | độ dài | độ căng thẳng |
| `minMoves` | solver tối ưu | trần lý thuyết | người thật đi được bao nhiêu |
| `movesSlack` | `movesAllowed − minMoves` | mức bó hẹp | có bẫy hay không |
| `nearMissRate` | thua nhưng cách thắng ≤ ε | mức ức chế / muốn chơi lại | độ khó tổng thể |
| `deadEndRate` | vào trạng thái không cứu được | mức trừng phạt sai lầm | tần suất người thật rơi vào |
| `branchingAvg` | trung bình nước hợp lệ mỗi lượt | tải nhận thức | chất lượng lựa chọn |
| `failReasonHist` | phân bố lý do thua | **nguồn gốc độ khó** | mức độ khó |
| `timeToWin` | thời gian mô phỏng | độ dài ván | tốc độ người thật |

## 3. Metric tổng hợp

| Metric | Nghĩa | **KHÔNG nói lên** |
|---|---|---|
| `difficultyScore` | tổ hợp có trọng số để **xếp hạng** | tỉ lệ — "khó gấp 1.7 lần" là vô nghĩa |
| `band` | Easy / Normal / Hard / Spike | ranh giới band là **quyết định**, không phải sự thật khách quan |

## 4. Metric từ người chơi thật

| Metric | Nói lên | **KHÔNG nói lên** |
|---|---|---|
| `actualPassRate` | độ khó thật | vì sao — cần kèm lý do thua |
| `attemptsToPass` | mức kiên nhẫn | độ khó thuần (còn phụ thuộc động lực) |
| `churnAtLevel` | chỗ người chơi bỏ | nguyên nhân bỏ (có thể do chán, không do khó) |
| `boosterUseRate` | mức cần trợ giúp | độ khó (có thể do UI đẩy booster) |

`churnAtLevel` cao ở một level **không** đồng nghĩa level đó khó. Kiểm tra `actualPassRate` và
`attemptsToPass` trước khi kết luận.

## 5. Điều kiện đo — bắt buộc kèm mọi con số

| Trường | Vì sao |
|---|---|
| `botVersion` | so số đo giữa hai đời bot là **so sai** |
| `policy` | `solver` và `human-model` cho ra hai thế giới khác nhau |
| `K`, `p` | tham số human model — đổi là đổi kết quả |
| `N` | quyết định sai số thống kê |
| `seedRange` | để tái tạo |
| `normalizeSet` | normalize theo tập nào |
| `calibrated` | đã hiệu chuẩn với dữ liệu thật chưa |

Thiếu một trường ⇒ **không đưa số cho GD**.

## 6. Từ dùng đúng

| Nói thế này | Không nói thế này | Vì sao |
|---|---|---|
| "policy `human-K4-p0.12` không qua được" | "level không giải được" | policy thua ≠ không có lời giải |
| "xếp hạng khó hơn level 12" | "khó gấp 1.7 lần level 12" | score không phải thang tỉ lệ |
| "passRate 62% ± 3.5% (N=200)" | "passRate 62.3%" | số lẻ tạo cảm giác chính xác giả |
| "chưa hiệu chuẩn, chỉ so tương đối" | (im lặng) | GD sẽ tưởng số là tuyệt đối |

## 7. Hai trục tiến trình — không được lẫn

| Tên | Trục X | Nói về |
|---|---|---|
| **Pacing curve** | `turnProgress` — tiến trình trong **một lượt chơi** | nhịp trong một level |
| **Phase curve** | `placementProgress` — tiến trình khi **sinh level** | bố cục vật thể trong level |

**Không bao giờ vẽ chồng lên nhau.** Trình bày cùng trang ⇒ hai biểu đồ tách biệt, mỗi cái ghi rõ
trục X là gì. Đây là lỗi đã gây hiểu sai thật.
