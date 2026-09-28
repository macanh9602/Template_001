---
name: gd-communication
description: >
  Giải thích cơ chế, số liệu và trade-off cho GD/PM/GĐ — người không đọc code. KÍCH HOẠT khi: cần
  giải thích một cơ chế cho GD, viết tooltip, chốt open question với GD, GD đọc số liệu sai, hoặc
  chuẩn bị brief/báo cáo cho người ngoài team code. Nguyên tắc: nói cái họ quyết được, và nói rõ
  mỗi chỉ số KHÔNG nói lên điều gì.
---

# SKILL: gd-communication

Sai lầm mặc định của agent: giải thích **cách hệ thống hoạt động**. GD cần biết **họ quyết được gì
và hệ quả là gì**.

---

## Bốn luật

1. **Bắt đầu bằng quyết định, không bằng cơ chế.**
   ✗ *"Hệ thống dùng weighted score normalize theo tập level..."*
   ✓ *"Anh cần chốt: level 15 nên nằm band Normal hay Hard? Chọn Hard thì ~35% người chơi sẽ fail
   lần đầu."*

2. **Mỗi chỉ số đưa ra phải kèm cái nó KHÔNG nói lên.** Đây là chỗ GD đọc quá đà và ra quyết định sai.
   Ví dụ: *"passRate 62% — nói lên độ khó cảm nhận, KHÔNG nói lên level có vui hay không, và KHÔNG
   nói lên vì sao khó (xem cột lý do thua)."*

3. **Dùng đúng tên trong `glossary.md`.** Một khái niệm một tên, trong code, trong tooltip, và trong
   lời nói. Đặt tên mới giữa cuộc họp là cách chắc chắn tạo hiểu nhầm về sau.

4. **Có yếu tố không gian / chuyển động / phân bố số → dựng visualiser, đừng viết dài.**
   (`workflow/ask-and-visualise.md`)

---

## Khung giải thích một cơ chế

```markdown
# [Tên cơ chế] — cho GD

## Người chơi thấy gì
(1–3 câu, từ góc nhìn người chơi, không có từ kỹ thuật)

## Anh chỉnh được gì
| Chỉnh cái gì | Ở đâu | Miền hợp lệ | Tăng lên thì sao | Giảm xuống thì sao |
|---|---|---|---|---|

## Anh KHÔNG chỉnh được (và vì sao)
| Cái gì | Vì sao cố định | Muốn đổi thì cần gì |
|---|---|---|

## Cơ chế hoạt động (chỉ phần cần để chỉnh đúng)
(sơ đồ hoặc 3–5 bước; bỏ qua chi tiết implement)

## Bẫy hay gặp khi chỉnh
| Nếu anh làm ... | Sẽ xảy ra ... |
|---|---|
```

Bảng **"anh KHÔNG chỉnh được"** thường hữu ích hơn bảng chỉnh được — nó ngăn GD dành thời gian cho
thứ không đổi được, và biến "sao không làm được" thành một yêu cầu rõ ràng.

---

## Trình bày số liệu — luật chống đọc sai

| Luật | Vì sao |
|---|---|
| Hiển thị **band**, không hiển thị số lẻ | `0.634` mời gọi so sánh giả; `Hard` thì không |
| Ghi rõ **điều kiện đo** kèm mọi bảng số | botVersion, N, policy — thiếu thì số vô nghĩa |
| Ghi rõ **đã hiệu chuẩn hay chưa** | chưa hiệu chuẩn ⇒ chỉ so tương đối với nhau |
| Không vẽ hai trục X khác bản chất lên cùng biểu đồ | pacing curve vs phase curve — đã gây hiểu sai thật |
| Không nội suy điểm khuyết | đường liền mạch trên dữ liệu thưa = bịa |
| Không dùng thang tỉ lệ cho điểm xếp hạng | "khó gấp 1.7 lần" là vô nghĩa với score tổ hợp |
| Normalize theo tập nào thì ghi tập đó | so số normalize khác tập là sai |

Bảng số cho GD luôn có dòng đầu:

> *Đo bằng: [công cụ + version] · [N lần] · [điều kiện]. Chỉ so được với các số khác trong **cùng
> bảng này**.*

---

## Chốt open question với GD

Không viết đoạn văn dài rồi hỏi *"anh thấy sao?"*. Dùng khung trắc nghiệm:

```markdown
## Cần anh chốt: [câu hỏi một dòng]

**Vì sao cần chốt bây giờ:** (cái gì bị chặn)

| | Phương án | Người chơi cảm thấy | Đánh đổi | Chi phí làm |
|---|---|---|---|---|
| A | | | | |
| B | | | | |
| C | | | | |

**Đề xuất: [A]** — vì ...

**Chốt xong sẽ khoá lại:** (đổi sau này tốn gì)
```

Cột **"chốt xong sẽ khoá lại"** giúp GD biết quyết định nào rẻ để đổi và quyết định nào đắt — đó là
thông tin họ không có mà dev thì có.

Có yếu tố cảm giác/không gian ⇒ đính kèm `.html` visualiser trước bảng.

---

## Viết tooltip / text trong tool

| Nguyên tắc | Ví dụ xấu | Ví dụ tốt |
|---|---|---|
| nói **hệ quả**, không nói kiểu dữ liệu | "float, 0–1" | "Càng cao thì chướng ngại xuất hiện càng sớm (0–1)" |
| nêu đơn vị | "Delay" | "Delay (giây)" |
| nêu khoảng dùng thật | "0–100" | "Thường dùng 20–40. Trên 60 sẽ rất khó." |
| cảnh báo tương tác | — | "Chỉ có tác dụng khi bật Auto Spawn." |
| dùng tên trong glossary | "priority" (2 nghĩa) | tên đã chốt trong glossary |

Ngôn ngữ: label/button **English ASCII**; tooltip và text GD đọc để **tiếng Việt**, đặt ở file riêng
ngoài thư mục bị test gate quét (`standards/code-style.md §10`).

---

## Báo cáo tiến độ cho GĐ/PM

Ngắn, có quyết định, có bằng chứng. Không kể quá trình.

```markdown
## [Story/Giai đoạn] — [ngày]

**Trạng thái:** DONE / IN PROGRESS / BLOCKED
**Làm được gì mà tuần trước chưa làm được:** (nói bằng thứ người ngoài thấy được)

**Cần quyết định:**
| Câu hỏi | Ai quyết | Chặn cái gì | Hạn |
|---|---|---|---|

**Rủi ro:**
| Rủi ro | Bằng chứng | Nếu xảy ra |
|---|---|---|

**Tiếp theo:** (2–3 gạch đầu dòng)
```

Trạng thái phải trung thực theo `workflow/verification.md`. **Không ghi DONE cho thứ chưa chạy thật.**
Một lần ghi DONE sai làm mọi báo cáo sau đó mất giá trị.

---

## Khi GD đã hiểu sai rồi

1. Đừng bắt đầu bằng *"không phải vậy"*. Bắt đầu bằng **họ đã kết luận gì** và **kết luận đó đúng
   trong điều kiện nào**.
2. Chỉ ra **chính xác chỗ số liệu không nói điều đó**.
3. Đưa số liệu **trả lời đúng câu hỏi họ thực sự quan tâm**.
4. Sửa gốc: nếu hiểu sai đến từ **tên gọi** ⇒ đổi tên + ghi vào `glossary.md §5`; nếu đến từ **cách
   hiển thị** ⇒ sửa hiển thị. Giải thích lại bằng miệng mà không sửa gốc thì tuần sau lặp lại.

---

## Đầu ra

- Brief/tài liệu cơ chế (khung ở trên) → `handoff/<story>/gd-brief.html` hoặc `.md`.
- Bảng trắc nghiệm chốt quyết định (+ visualiser nếu cần).
- Tooltip/text đã viết, đặt đúng chỗ.
- Từ mới hoặc từ đổi nghĩa → cập nhật `Docs/glossary.md` **trong cùng story**.
