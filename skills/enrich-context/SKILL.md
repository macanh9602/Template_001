---
name: enrich-context
description: >
  Phanh trước khi kết luận — ép quy trình hỏi đáp làm giàu context TRƯỚC khi đưa ra bất kỳ giải pháp
  nào. KÍCH HOẠT khi: vấn đề mơ hồ, nhiều nguyên nhân khả dĩ, yêu cầu chưa rõ, có trade-off chưa
  chốt, hoặc bất cứ lúc nào sắp assume rồi kết luận. Cũng dùng như modifier bật kèm skill khác.
  Dùng cho Unity mobile game development.
---

# SKILL: enrich-context

Đây là **phanh** — chặn thói quen nhảy thẳng vào câu trả lời.

Dùng độc lập, hoặc bật kèm bất kỳ skill nào khi thấy mình sắp assume.

---

## Bước đầu tiên — đọc context

Đọc `Docs/project-context.md` (và `Docs/glossary.md` nếu gặp từ không chắc nghĩa) để biết cái gì đã
có sẵn: stack · pattern · event · rule · fact đã chốt.
**Không hỏi lại những gì file đã trả lời.**

Chưa có `project-context.md` → vẫn chạy được, nhưng nhắc: *"Chưa có project-context.md — chạy
`playbooks/p1-bootstrap.md` để khỏi phải hỏi lại mỗi lần."*

---

## Nguyên tắc lõi

- **Không kết luận / không đề xuất giải pháp** cho tới khi đủ context **và** dev đã confirm phần
  tóm tắt hiểu biết.
- Hỏi theo **batch nhỏ 2–4 câu**, dạng trắc nghiệm.
- Mỗi câu hỏi phải có **mục đích chẩn đoán rõ** — nó phải phân biệt được giữa các hypothesis.
  Câu nào không đổi kết luận thì bỏ.
- Luôn tách bạch: **BIẾT** (fact) vs **ĐOÁN** (assumption) vs **CẦN HỎI** (gap).
- **Bắt buộc trắc nghiệm cho quyết định**, không free-text. Mỗi câu 2–4 lựa chọn + "Other" + đúng một
  option **recommend** kèm lý do. Free-text chỉ để xin dữ liệu thô (log · repro · screenshot · số đo).
- **Không tự chọn trade-off hay hướng kiến trúc** — kể cả khi phát hiện giữa lúc đang code. Hễ có
  > 1 phương án hợp lý → đưa thành câu trắc nghiệm.
- Dev đã nêu một yêu cầu/ràng buộc ⇒ đó là **hard constraint**. Không âm thầm hi sinh nó để đổi lấy
  sự đơn giản. Buộc phải đánh đổi ⇒ hỏi lại bằng trắc nghiệm.
- Câu hỏi liên quan **không gian · chuyển động · timing · nhiều biến tương tác** → dựng `.html`
  visualiser **trước** khi hỏi (`workflow/ask-and-visualise.md`).

---

## Flow

### Bước 0 — phân loại vấn đề

Nói ngắn cho dev:

- Vấn đề thuộc nhóm nào: gameplay logic · rendering/pipeline · performance · data · editor tooling ·
  build config · khác.
- Các **nhóm nguyên nhân khả dĩ** (hypothesis families) — liệt kê 2–4 hướng, chưa chốt hướng nào.

### Bước 1 — liệt kê ĐÃ BIẾT vs CẦN HỎI

```
## Đã biết
- [fact 1 — từ dev / project-context / code đã đọc]

## Đang giả định (cần verify)
- [assumption 1]

## Cần hỏi để thu hẹp
- [gap 1] → vì nó phân biệt hypothesis [A] với [B]
```

### Bước 2 — hỏi theo batch

- Mỗi batch 2–4 câu, trắc nghiệm.
- Ưu tiên câu **phân biệt được nhiều hypothesis nhất**.
- Gộp các câu cùng "một lần mở Unity / một lần kiểm tra" vào chung batch.
- Sau mỗi batch, cập nhật lại danh sách hypothesis còn lại.

### Bước 3 — tóm tắt & confirm

Khi đã thu hẹp còn 1–2 nguyên nhân:

```
## Tóm tắt hiểu biết
- Vấn đề: ...
- Context chốt: ...
- Nguyên nhân nghi ngờ (đã xếp hạng): 1) ...  2) ...
- Hướng đã loại: ... vì ...
```

Hỏi: *"Mình hiểu đúng chưa? Confirm để đưa kết luận + hướng xử lý."*

### Bước 4 — chỉ khi dev confirm → kết luận

- Đưa kết luận + giải pháp, có cân nhắc mobile performance.
- Còn > 1 nguyên nhân ngang nhau → đề xuất **cách test rẻ nhất để phân biệt**, không đoán bừa.
- Nối tiếp sang skill phù hợp: `debug-audit` · `spec-feature` · `technical-slice` · `level-editor` ·
  `difficulty-design` · `game-feel-motion`.

---

## Khi đã fix mà vẫn lỗi — instrument, đừng sửa mù

Bài học xương máu: lý thuyết về runtime state mà không có data thật → fix sai nhiều vòng.

- Một fix đã áp dụng mà vấn đề **vẫn còn** ⇒ **DỪNG đoán.** Chuyển sang thu thập **dữ liệu thật**
  trước khi thử fix tiếp.
- Trước khi sửa lần hai:
  1. Thêm instrumentation/audit log bám **đúng điểm ra quyết định** — dump đủ state để xác nhận/loại
     từng hypothesis.
  2. Kiểm tra log có thực sự ghi ra thứ cần: coi chừng filter che mất section; vùng dump có bao trùm
     đúng dữ liệu đang nghi không.
  3. Nhờ dev repro + gửi log, **đọc data thật** rồi mới kết luận.
- Khi đưa fix dựa trên data: nêu rõ **data nào trong log chứng minh nguyên nhân**, để dev kiểm chứng.

Chi tiết: `skills/debug-audit/`.

---

## Khi nào dừng hỏi

- Đã thu hẹp còn 1–2 nguyên nhân verify được → dừng, chuyển sang test/kết luận.
- Hỏi thêm không đổi hướng xử lý → dừng.
- Qua 2–3 batch vẫn mơ hồ → nói thẳng *"cần thêm dữ liệu loại X (log / screenshot / repro / số đo)"*
  thay vì hỏi lan man.

---

## Output

Skill này **không tạo file**. Output là:

1. Phần tóm tắt context đã confirm.
2. Kết luận + hướng xử lý (sau khi dev confirm).

Quyết định project-level phát sinh ⇒ ghi `D-xxx` theo `workflow/decisions.md`.
