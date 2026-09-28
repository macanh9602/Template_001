# Hỏi & Trực quan hoá

> Không sửa file này khi làm game mới.
> Đây là **một chỗ duy nhất** định nghĩa: khi nào hỏi, hỏi dạng gì, và khi nào bắt buộc dựng `.html`
> trước khi hỏi.

---

## 1. Luật hỏi

### Luôn dùng trắc nghiệm cho quyết định

Mọi decision đều hỏi bằng **option-select**, không bao giờ free-text.
Free-text chỉ dùng để xin **dữ liệu thô**: log · repro steps · screenshot · video · số đo.

```
Q: <câu hỏi một dòng, cụ thể>

  A) <tên phương án>                    ← RECOMMEND
     Được:  ...
     Mất:   ...
     Ảnh hưởng: CPU / GPU / GC / draw call / memory / maintainability
  B) <tên phương án>
     Được: ...  Mất: ...
  C) <tên phương án>
     Được: ...  Mất: ...
  D) Other — dev tự nhập
```

Ràng buộc:

- 2–4 option, **luôn có "Other"**, **luôn có đúng một** option được recommend kèm lý do một câu.
- Batch tối đa 2–4 câu. Ưu tiên câu **phân biệt được nhiều hypothesis nhất** trước.
- Gộp các câu cùng "một lần mở Unity / một lần kiểm tra" vào chung batch để dev đỡ qua lại.
- Câu hỏi không làm đổi kết luận → **bỏ**, đừng hỏi cho đủ.
- Không hỏi lại thứ đã có trong `Docs/project-context.md`, `glossary.md`, hoặc đã nói trong hội thoại.

### Khi nào phải hỏi

| Tình huống | Hỏi hay tự quyết |
|---|---|
| chia class, đặt tên, chọn algorithm cục bộ | **tự quyết** |
| refactor trong phạm vi story | **tự quyết** |
| tối ưu CPU/GC/draw call | **tự quyết** |
| project architecture · layer contract | **hỏi** |
| gameplay semantics (điều kiện thắng/thua, luật tương tác) | **hỏi** |
| serialization / data compatibility | **hỏi** |
| performance budget theo hướng khó đảo | **hỏi** |
| story scope / deliverable | **hỏi** |
| có ≥ 2 phương án cục bộ/reversible, cùng thoả contract | **tự quyết** + ghi rationale ngắn |
| có ≥ 2 phương án làm đổi contract/gameplay/data/scope hoặc khó đảo | **hỏi** |
| dev đã nêu một ràng buộc mà giải pháp đơn giản sẽ hi sinh nó | **hỏi** — ràng buộc là hard constraint |

> Mục tiêu khi scale là **ít round-trip**: agent phải tự quyết mọi micro-decision. Chỉ giữ quyền chốt của dev cho quyết định thay đổi contract, gameplay semantics, data compatibility, scope hoặc trade-off khó đảo.

### Khi nào dừng hỏi

- Đã thu hẹp còn 1–2 phương án verify được → dừng, chuyển sang test/kết luận.
- Hỏi thêm không đổi hướng xử lý → dừng.
- Qua 2–3 batch vẫn mơ hồ → nói thẳng *"cần thêm dữ liệu loại X"* thay vì hỏi lan man.

---

## 2. Luật trực quan hoá

### Bắt buộc dựng `.html` TRƯỚC khi hỏi, khi decision liên quan

- **không gian** — layout, vị trí, pivot, bounds, thứ tự chồng lớp
- **chuyển động** — đường bay, easing, timing, stagger, sequence nhiều pha
- **curve / phân bố** — độ khó, pacing, tần suất, so sánh nhiều bộ tham số
- **nhiều biến tương tác** — đổi A thì B và C đổi thế nào
- **so sánh phương án** — hai cách xếp, hai cách bố cục, hai bộ số

Lý do: bắt dev đọc 200 chữ rồi tự tưởng tượng là cách chậm nhất và dễ hiểu sai nhất. Một trang HTML
mất vài phút dựng và tiết kiệm một vòng làm lại.

### Không cần visualiser khi

- Câu hỏi thuần kiến trúc/data (chọn interface, chọn nơi đặt file, chọn schema).
- Câu hỏi nhị phân rõ ràng, không có yếu tố không gian/thời gian.
- Dev đã đưa video/ảnh ref và câu hỏi chỉ là *"đúng cái này chưa"*.

### Ba loại artifact

| Loại | Template | Dùng khi |
|---|---|---|
| **Visualiser** | `templates/visualiser-base.html` | mô phỏng cơ chế, cho kéo tham số, xem kết quả đổi theo |
| **Option picker** | `templates/option-picker.html` | trình bày 2–4 phương án cạnh nhau, mỗi cái có preview + được/mất |
| **Open questions** | `templates/open-questions.html` | nhiều câu cần chốt cùng lúc, có owner (GD/Dev), có phương án đề xuất |

Ngoài ra khi đối tượng đọc là **GD chứ không phải dev** → `skills/gd-communication/`.

### Yêu cầu kỹ thuật của mọi `.html`

- **Một file duy nhất**, inline hết CSS/JS. Không phụ thuộc mạng.
- Dark theme, đọc được trên màn hình dev.
- Tham số **kéo được** (slider/input) nếu mục đích là để dev cảm nhận, không chỉ để xem.
- Số hiển thị kèm **đơn vị** và **giá trị tuyệt đối**, không chỉ tỉ lệ đã chuẩn hoá.
- Có một dòng ghi rõ **cái gì là dữ liệu thật, cái gì là giả lập**.
- Đặt trong `handoff/<story>/` hoặc `Docs/` tuỳ phạm vi. Đặt tên nói được nội dung.

### Bẫy khi vẽ số liệu

- Chuẩn hoá theo đỉnh của chính nó ⇒ **không so sánh được giữa hai level/hai bộ tham số** bằng chiều
  cao. Phải nói rõ điều này ngay trên trang.
- Hai biểu đồ khác trục thì **không được** chồng lên nhau, kể cả khi cùng chạy từ 0 đến 100%.
- Mẫu khuyết thì vẽ đứt đoạn, **không nội suy** — nội suy là bịa dữ liệu không có thật.
- Điểm tổng hợp (một chữ "thấp/vừa/cao") phải hiện được **từng số hạng và giá trị thật** khi hover.
  Điểm tổng hợp là nguồn *chính xác giả* kinh điển.

---

## 3. Sau khi dev chọn

1. Ghi lựa chọn + lý do vào `implementation-notes.html` (mục 🔵 Decision).
2. Project-level → thêm `D-xxx` vào `Docs/decision-log.md` theo `workflow/decisions.md`.
3. Ghi **cái đã loại và vì sao loại** — đây là phần cứu người đọc sau khỏi đề xuất lại đúng phương án
   đã bị loại.
4. Tiếp tục đúng từ decision đó. Không làm trước phần phụ thuộc nó.

---

## 4. Mẫu tóm tắt khi context còn rủi ro

Chỉ dùng bước này khi còn **giả định có thể đổi hướng xử lý**. Nếu facts đã đủ thì kết luận/implement ngay, không xin confirm hình thức.

```
## Đã biết (fact)
- ...

## Đang giả định (có thể đổi hướng)
- ...

## Cần dữ liệu/decision để phân biệt
- ... → vì nó phân biệt [A] với [B]
```

Nếu phần "Đang giả định" rỗng ⇒ không hỏi confirm.
