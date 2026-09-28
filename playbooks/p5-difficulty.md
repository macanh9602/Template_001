# P5 — Difficulty

**Vào khi:** có level (làm tay hoặc sinh) và cần kiểm soát độ khó.
**Ra khi:** đo được độ khó của một level, phân band được, và (nếu cần) sinh được level nhắm điểm số
mục tiêu — tất cả bằng **số**, không bằng cảm giác.

Skill chính: `skills/difficulty-design/` (+ `refs/bot-and-metrics.md` trong thư mục đó).
Tham chiếu: `knowledge/difficulty/`.

**Thứ tự không được đảo: INSTRUMENT → EVALUATE → GENERATE.**

---

## Giai đoạn A — INSTRUMENT

### A1. Chốt "khó" nghĩa là gì

Chọn **2–3 trục** trong: constraint · search · execution · information · recovery.
Ghi vào `Docs/glossary.md §3`, kèm cột **"KHÔNG nói lên"** cho mỗi metric.
Ghi rõ trục nào **không** dùng.

### A2. Kiểm tra Domain có mô phỏng được không

Điều kiện cần (nếu thiếu ⇒ sửa trước, đây là story riêng):

- [ ] Domain thuần C#, chạy headless, không phụ thuộc Visual/MonoBehaviour
- [ ] State `Snapshot()` deep copy (có unit test: mutate bản sao → bản gốc không đổi)
- [ ] Mọi randomness qua **một** RNG có seed; không `UnityEngine.Random` rải rác
- [ ] Liệt kê được `LegalMoves`, `Apply(move)`, `IsWin`, `IsLose`

### A3. Dựng bot như **dụng cụ đo**

- [ ] `botVersion` ghi kèm **mọi** số đo
- [ ] Deterministic theo seed
- [ ] Tách `solver` (đo trần, validate) và `human-model` (đo độ khó cảm nhận, tham số `K`, `p`)
- [ ] Golden test: tập level cố định + `expected.json` + dung sai **tính theo N**
- [ ] Batch chạy đủ nhanh để người ta thực sự chạy nó (song song theo seed, pool snapshot)

### A4. Chạy batch, ra bảng

N ≥ 200/level. Metrics: `passRate` · `avgMoves` · `movesSlack` · `nearMissRate` · `deadEndRate` ·
`branchingAvg` · `failReasonHist`.

`failReasonHist` là chỉ số hữu ích nhất khi cần **sửa** một level.

## Giai đoạn B — EVALUATE

### B1. Điểm khó

- [ ] Công thức + trọng số, mỗi trọng số có **lý do thiết kế**, ghi `D-xxx`
- [ ] `normalize` ghi rõ dựa trên tập level nào
- [ ] Score chỉ dùng **xếp hạng + phân band** — không phải thang tỉ lệ

### B2. Band

Chốt ngưỡng `Easy / Normal / Hard / Spike` một lần, ghi decision-log.
**Hiển thị band ở mọi nơi GD nhìn thấy**, giấu số lẻ.

### B3. Hiệu chuẩn

Có analytics ⇒ so `predicted passRate` vs `actual passRate`.

| Kết quả | Làm gì |
|---|---|
| tương quan tốt | dùng được để sinh level |
| lệch **hệ thống** | thiếu một trục khó — tìm trục, đừng chỉ chỉnh trọng số |
| lệch **ngẫu nhiên** | tăng N, xem lại metric |

Chưa hiệu chuẩn ⇒ ghi rõ trong mọi báo cáo.

## Giai đoạn C — GENERATE

Chỉ vào giai đoạn này khi A và B xong.

### C1. Pipeline ba tầng

`Producer` → `Mutator` → `Validator` (`knowledge/difficulty/pipeline-patterns.md`).

- [ ] Producer có **seed**, seed ghi vào level data
- [ ] Validator **chỉ loại, không sửa**
- [ ] Retry có giới hạn, hết retry ⇒ **báo lý do fail cuối cùng**
- [ ] **Giữ near-miss** kèm lý do trượt

### C2. Rule theo tiến trình

Phase window viết theo `placementProgress ∈ [0,1]`, **không** theo index tuyệt đối.

### C3. Hai đường cong — không vẽ chồng

| | trục X | nói về |
|---|---|---|
| Pacing curve | `turnProgress` | nhịp trong một lượt chơi |
| Phase curve | `placementProgress` | bố cục khi sinh level |

Trình bày cùng trang ⇒ hai biểu đồ tách biệt, mỗi cái ghi rõ trục X.

### C4. Difficulty curve theo tiến trình người chơi

Tăng dần nhưng **không đơn điệu** · nhịp nghỉ mỗi 3–5 level · spike có chủ đích ở mốc ·
level đầu dạy **một** cơ chế và không được fail · cơ chế mới ra mắt ở level dễ.

## Giai đoạn D — Trình bày cho GD

`skills/gd-communication/`.

- [ ] Bảng số kèm dòng điều kiện đo (botVersion · policy · K,p · N · seed range)
- [ ] Hiển thị band, không số lẻ
- [ ] Visualiser: phân bố score, curve, predicted vs actual
- [ ] Mỗi chỉ số kèm **"KHÔNG nói lên điều gì"**

---

## Xong khi

- [ ] Trả lời được "level X khó bao nhiêu" bằng số, kèm điều kiện đo
- [ ] Golden test chạy trong CI
- [ ] Trọng số + ngưỡng band đã ghi `D-xxx`
- [ ] (Nếu có generate) sinh được level đạt band mục tiêu, tái tạo được từ seed
- [ ] GD đọc báo cáo và ra quyết định được, không hiểu sai

## Bẫy

Bảng đầy đủ trong `skills/difficulty-design/`. Ba cái đắt nhất:

1. **Generate trước khi có thước đo** → hàng nghìn level không biết khó bao nhiêu.
2. **Nói "không giải được" khi thực ra chỉ là policy thua** → loại nhầm level tốt.
3. **Vẽ pacing curve chồng phase curve** → GD kết luận sai hoàn toàn.
