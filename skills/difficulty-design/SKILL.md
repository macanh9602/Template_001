---
name: difficulty-design
description: >
  Thiết kế và kiểm soát độ khó — từ đo lường độ khó thật, tới sinh level, tới đường cong khó theo
  tiến trình. KÍCH HOẠT khi: "level dễ quá / khó quá", cần level generation, cần difficulty curve,
  DDA, booster trigger, cân bằng, hoặc cần trả lời "level này khó bao nhiêu". Nguyên tắc lõi:
  Instrument → Evaluate → Generate. Không đoán độ khó.
---

# SKILL: difficulty-design

Luật lõi, theo đúng thứ tự, **không được đảo**:

```
1. INSTRUMENT  — đo được độ khó của một level đã có
2. EVALUATE    — chấm điểm & phân loại level, đối chiếu với dữ liệu thật
3. GENERATE    — sinh/mutate level nhắm vào điểm số mục tiêu
```

Nhảy thẳng sang GENERATE khi chưa có thước đo là lỗi đắt nhất trong mảng này: sinh ra hàng nghìn
level mà **không biết chúng khó bao nhiêu**, rồi tune bằng cảm giác.

---

## Bước đầu tiên — đọc context

`knowledge/difficulty/metric-vocabulary.md` (từ vựng đo lường) ·
`knowledge/difficulty/pipeline-patterns.md` (khung Producer/Mutator/Validator) ·
`Docs/data-model.md` (level data hiện tại) · `Docs/glossary.md`.

Skill liên quan: `gd-communication` (GD sẽ đọc số này — dễ đọc quá đà), `game-design-advisor` và
`ship-level-design` nếu project có.

---

## Giai đoạn 1 — INSTRUMENT

### 1.1 Chốt "khó" nghĩa là gì trong game này

Không có định nghĩa chung. Phải chọn, và ghi vào `Docs/glossary.md §3`.

| Trục | Câu hỏi | Ví dụ chỉ số |
|---|---|---|
| **Constraint** | người chơi bị bó hẹp bao nhiêu | số nước đi dư, slot trống, thời gian dư |
| **Search** | phải nghĩ nhiều không | số nhánh hợp lệ mỗi lượt, độ sâu nhìn trước cần thiết |
| **Execution** | tay có phải chính xác không | cửa sổ thời gian, độ chính xác vị trí |
| **Information** | có bị che thông tin không | phần chưa lộ, độ ngẫu nhiên |
| **Recovery** | sai một bước có cứu được không | số state chết, khoảng cách tới dead end |

Chọn **2–3 trục chính**, không phải tất cả. Ghi rõ trục nào **không** dùng, để sau này không ai
lôi ra tranh cãi.

### 1.2 Dựng bot/simulator — coi nó là **dụng cụ đo**

Đây là điểm quan trọng nhất và hay bị hiểu sai. Bot **không phải AI để chơi hộ**; nó là **thước**.
Mà thước thì phải có version, phải hiệu chuẩn, phải có golden test.

Luật:

1. **Bot có version.** `botVersion` ghi kèm **mọi** số đo. So số đo giữa hai version bot là so sai.
2. **Golden test.** Một tập level cố định + kết quả kỳ vọng. Đổi bot mà golden đổi ⇒ phải giải thích
   được vì sao, hoặc là regression.
3. **Deterministic theo seed.** Cùng seed + cùng level + cùng botVersion ⇒ cùng kết quả. Không
   deterministic thì mọi so sánh vô nghĩa.
4. **Bot chạy trên Domain, không cần Visual.** Đây là lý do `standards/system-design.md` cấm Domain
   phụ thuộc Visual — bot chạy headless nhanh gấp hàng nghìn lần.
5. **Tách hai loại bot:**

| Loại | Trả lời | Dùng để |
|---|---|---|
| `solver` (chơi tối ưu / gần tối ưu) | level có giải được không, tối thiểu bao nhiêu bước | validate, đo trần |
| `human-model` (chơi như người) | người thường có qua được không, tỉ lệ bao nhiêu | đo độ khó cảm nhận |

### 1.3 Human model — hai tham số tối thiểu

Bot tối ưu **không** đo được độ khó cảm nhận: level nào solver cũng qua. Cần mô hình người:

| Tham số | Nghĩa | Ảnh hưởng |
|---|---|---|
| `attention K` | nhìn trước được bao nhiêu bước / xét được bao nhiêu lựa chọn | K nhỏ → level đòi hỏi nhìn xa thành khó |
| `mistake p` | xác suất chọn nước không tối ưu | p > 0 → level không có đường lùi thành rất khó |

Chạy N lần (N ≥ 200) với seed khác nhau → `passRate`, `avgMoves`, `nearMissRate`.
`passRate` của human-model chính là chỉ số độ khó dùng được nhất.

### 1.4 Luật vàng: `policy-passable ≠ solvable`

Bot qua được **không** có nghĩa level giải được, và ngược lại:

- Bot dùng một **policy** (một chiến lược). Policy thua ⇒ chỉ chứng minh *policy này* thua.
- Muốn khẳng định "không giải được" ⇒ cần **solver đầy đủ** (exhaustive/proof), không phải policy.

Luôn báo cáo bằng đúng từ: **"policy vX không qua được"**, không phải **"level không giải được"**.
Đây là chỗ GD hiểu sai và loại nhầm level tốt.

### 1.5 Báo cáo số đo — luôn kèm điều kiện

Mọi con số độ khó phải đi kèm: `botVersion` · `policy` · `K`,`p` · `N runs` · `seed range`.
Thiếu một trong số đó ⇒ số vô nghĩa, không được đưa cho GD.

---

## Giai đoạn 2 — EVALUATE

### 2.1 Điểm khó = tổ hợp có trọng số, và **trọng số phải giải thích được**

```
difficultyScore = Σ wᵢ · normalize(metricᵢ)
```

- `normalize` phải nêu rõ dựa trên **tập level nào** — normalize theo tập A rồi so với số normalize
  theo tập B là lỗi kinh điển.
- Trọng số `wᵢ` là **quyết định thiết kế**, phải ghi `D-xxx` + lý do, không phải kết quả fit số.
- Score chỉ dùng để **xếp hạng và phân band**, không dùng để nói "level này khó gấp 1.7 lần".
  Thang này không phải thang tỉ lệ.

### 2.2 Phân band, đừng dùng số lẻ

GD làm việc bằng band (`Easy / Normal / Hard / Spike`), không bằng `0.63`. Chốt ngưỡng band một lần,
ghi vào decision-log, và **hiển thị band ở mọi nơi GD nhìn thấy**.

### 2.3 Hiệu chuẩn với dữ liệu thật

Có analytics ⇒ so `predicted passRate` với `actual passRate`:

| Kết quả | Nghĩa | Làm gì |
|---|---|---|
| tương quan tốt | thước dùng được | dùng để sinh level |
| lệch hệ thống (luôn dự đoán dễ hơn) | thiếu một trục khó | tìm trục còn thiếu, đừng chỉ chỉnh trọng số |
| lệch ngẫu nhiên | metric nhiễu / N quá nhỏ | tăng N, xem lại metric |

Chưa có dữ liệu thật ⇒ ghi rõ trong báo cáo: *"chưa hiệu chuẩn"*. Không im lặng để GD tưởng đã chuẩn.

---

## Giai đoạn 3 — GENERATE

### 3.1 Ba tầng — Producer / Mutator / Validator

Chi tiết ở `knowledge/difficulty/pipeline-patterns.md`.

```
Producer  → sinh ứng viên (có seed)
Mutator   → biến đổi ứng viên để đẩy score về mục tiêu
Validator → loại ứng viên vi phạm rule cứng
```

Luật:

- **Validator không được sửa**, chỉ loại. Sửa trong validator ⇒ không ai biết level cuối khác gì bản gốc.
- **Producer phải có seed**, và seed ghi vào level data. Không tái tạo được level đã sinh là hỏng.
- **Retry có giới hạn**, và khi hết retry thì **báo lý do fail cuối cùng**, không trả về im lặng.
- **Giữ near-miss.** Ứng viên trượt sát ngưỡng là dữ liệu quý — lưu lại kèm lý do trượt để GD xem.
  Vứt thẳng ⇒ mất thông tin về việc ngưỡng đang quá chặt.

### 3.2 Phase windows — theo tiến trình, không theo index tuyệt đối

Rule dạng *"chướng ngại loại A chỉ xuất hiện ở nửa sau"* phải viết theo **tiến trình chuẩn hoá**
(`placementProgress ∈ [0,1]`), không theo số thứ tự tuyệt đối. Level dài ngắn khác nhau ⇒ index
tuyệt đối cho ra bố cục khác hẳn nhau.

### 3.3 Hai đường cong — **không bao giờ vẽ chồng lên nhau**

| Đường cong | Trục X | Nói về |
|---|---|---|
| **Pacing curve** | `turnProgress` — tiến trình *trong một lượt chơi* | nhịp trong một level |
| **Phase curve** | `placementProgress` — tiến trình *khi sinh level* | bố cục vật thể trong level |

Hai trục **khác nhau về bản chất**. Vẽ chung một biểu đồ ⇒ GD kết luận sai. Đây là lỗi đã xảy ra
thật. Nếu phải trình bày cùng trang ⇒ hai biểu đồ tách biệt, mỗi cái ghi rõ trục X là gì.

### 3.4 Difficulty curve theo tiến trình người chơi

| Yếu tố | Nguyên tắc |
|---|---|
| xu hướng chung | tăng dần, **không đơn điệu** |
| nhịp nghỉ | cứ 3–5 level khó thì một level dễ rõ rệt |
| spike | có chủ đích, đặt ở mốc (level tròn chục), không rơi ngẫu nhiên |
| level đầu | dạy cơ chế, không được fail — mỗi level dạy **một** thứ |
| cơ chế mới | ra mắt ở level dễ, khó lên sau 2–3 level |

### 3.5 DDA (nếu có) — luật an toàn

- DDA điều chỉnh **đầu vào của việc sinh level**, không sửa level đang chơi giữa chừng.
- Mọi điều chỉnh phải **log được**: người chơi này đang ở band nào, vì sao.
- Có **trần và sàn**. DDA không giới hạn ⇒ trôi về một trong hai cực.
- Có **chế độ tắt** để test và để so sánh A/B.
- Người chơi **không được cảm thấy** bị điều chỉnh — thay đổi nhỏ, chậm, không đảo chiều liên tục.

---

## Booster / trợ giúp — trigger

| Trigger | Chỉ số |
|---|---|
| fail liên tiếp | `consecutiveFails ≥ N` |
| gần thắng mà thua | `nearMissRate` cao ở level đó |
| đứng yên lâu | thời gian không thao tác |

Luật: booster xuất hiện **sau khi** thua, không phải trước — gợi ý trước khi thua làm người chơi
thấy bị coi thường và làm hỏng số liệu độ khó.

---

## Bẫy hay gặp

| Bẫy | Hậu quả |
|---|---|
| generate trước khi có thước đo | hàng nghìn level không biết khó bao nhiêu |
| bot không có version | so số đo giữa hai đời bot → kết luận sai |
| dùng solver để đo độ khó cảm nhận | mọi level đều "dễ" |
| nói "không giải được" khi chỉ là policy thua | loại nhầm level tốt |
| normalize theo tập khác nhau rồi so | xếp hạng sai |
| coi difficultyScore là thang tỉ lệ | "khó gấp 1.7 lần" — vô nghĩa |
| vẽ pacing curve chồng phase curve | GD đọc sai hoàn toàn |
| validator vừa loại vừa sửa | không ai biết level cuối khác gì bản sinh |
| vứt near-miss | mất dữ liệu về ngưỡng quá chặt |
| phase window theo index tuyệt đối | level dài ngắn khác nhau ra bố cục khác nhau |
| không lưu seed | không tái tạo được level bị báo lỗi |
| đưa số cho GD không kèm điều kiện đo | GD tin số sai |
| DDA không trần/sàn | trôi cực đoan |

---

## Đầu ra

1. **Định nghĩa độ khó** (2–3 trục) → `Docs/glossary.md §3`.
2. **Bot/simulator có version + golden test** trong project.
3. **Báo cáo đo** — bảng số kèm `botVersion / policy / K,p / N / seed range`.
4. **Trọng số + ngưỡng band** → `D-xxx` trong `Docs/decision-log.md`.
5. **Visualiser** phân bố score, curve, và so sánh predicted vs actual (`gd-communication` để trình
   bày cho GD).
6. Pattern generic rút ra → `knowledge/difficulty/`.
