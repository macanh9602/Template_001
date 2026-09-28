---
name: technical-slice
description: >
  Dựng lát cắt kỹ thuật mỏng nhất để chứng minh một rủi ro lớn là làm được (hoặc không) TRƯỚC khi
  xây feature đầy đủ. KÍCH HOẠT khi: procedural mesh, runtime generation, custom rendering, save/load
  architecture, editor foundation, hoặc bất cứ thứ gì "chưa biết có chạy nổi trên mobile không".
  Output là kết luận đo được + quyết định go/no-go, không phải feature hoàn chỉnh.
---

# SKILL: technical-slice

Một technical slice trả lời **đúng một câu hỏi**: *"Cách làm X có đáp ứng được ràng buộc không?"*

Không phải prototype gameplay. Không phải feature. Là **thí nghiệm có kết luận đo được**.

---

## Bước đầu tiên — đọc context

`Docs/project-context.md` · `standards/system-design.md` (slice sẽ nằm ở layer nào) ·
`standards/performance-budget.md` (ngưỡng phải đạt là bao nhiêu).

---

## Khi nào cần slice

| Dấu hiệu | Ví dụ |
|---|---|
| chưa ai trong team làm bao giờ | procedural mesh deform theo curve |
| chi phí sửa sai rất cao | format save/load, cấu trúc level data |
| nghi ngờ performance mobile | vài trăm object động, custom shader, mesh rebuild |
| là nền cho nhiều story sau | editor foundation, runtime state indexing |
| có >1 cách làm, chưa biết cách nào trụ được | mesh vs sprite vs particle |

**Không cần slice** khi: đã có tiền lệ trong project cũ chạy ổn, hoặc chi phí thử = chi phí làm thật.

---

## Luật của một slice

1. **Một câu hỏi, một slice.** Hai câu hỏi ⇒ hai slice, chạy nối tiếp.
2. **Có ngưỡng số trước khi code.** "Nhanh" không phải ngưỡng. `≤ 2ms/frame trên [thiết bị chuẩn]`
   mới là ngưỡng. Lấy từ `standards/performance-budget.md`.
3. **Đo trên thiết bị thật**, không phải Editor. Editor profiler chỉ để tìm hot spot, không để kết luận.
4. **Slice được phép xấu** — placeholder art, hardcode input, không UI. Nhưng **vẫn không được
   hardcode số liệu tune được** và **vẫn không `CreatePrimitive` / `new Material`**: nếu slice đi vào
   production thì đúng chỗ đó là nợ. Prefab placeholder vẫn là **prefab thật**.
5. **Slice có ngày hết hạn.** Quá budget thời gian mà chưa kết luận ⇒ đó cũng là một kết luận
   ("cách này không rẻ") ⇒ báo dev, hỏi trắc nghiệm hướng tiếp.
6. **Slice không tự thành feature.** Chuyển sang production ⇒ story riêng, có refactor pass.

---

## Flow

### Bước 0 — viết Slice Brief (trước khi mở Unity)

```markdown
# Slice: [tên]

## Câu hỏi cần trả lời
(một câu, có thể trả lời được bằng có/không hoặc bằng một con số)

## Vì sao hỏi bây giờ
Story nào bị chặn nếu không biết câu trả lời.

## Ngưỡng chấp nhận
| Chỉ số | Ngưỡng | Đo bằng gì | Thiết bị |
|---|---|---|---|

## Phương án đem thử
| # | Cách làm | Kỳ vọng | Rủi ro |
|---|---|---|---|

## Budget
Thời gian tối đa: ... Quá thì dừng và báo.

## KHÔNG làm trong slice này
- ...
```

Confirm brief với dev **trước khi code**. Đây là chỗ rẻ nhất để phát hiện mình đang trả lời sai câu hỏi.

### Bước 1 — dựng harness đo trước, feature sau

Thứ tự này quan trọng: **có thước đo trước, rồi mới có thứ để đo.**

- Scene test riêng, số lượng object cấu hình được (qua SO/field, không hardcode).
- Hiển thị số đo ngay trên màn hình (ms/frame, draw call, alloc/frame, mem) — dùng prefab TMP có
  script quản lý theo `standards/code-style.md §7`.
- Nút/slider để đổi tham số tại chỗ — mỗi lần build lại để đổi một số là một lần lãng phí.
- Stress mode: nhân số lượng lên 2×/5×/10× để tìm điểm gãy, không chỉ đo ở mức "đủ dùng".

### Bước 2 — chạy phương án, ghi số

Mỗi phương án ghi **cùng một bảng**, cùng thiết bị, cùng số lượng object:

| Phương án | ms/frame | draw call | GC alloc/frame | mem | ghi chú |
|---|---|---|---|---|---|

Không so số giữa hai lần đo khác điều kiện. Khác thiết bị / khác build config / khác số lượng ⇒
**không phải cùng một phép đo**.

### Bước 3 — kết luận

```markdown
## Kết luận slice

**Trả lời:** (đúng câu hỏi ở brief)

**Bằng chứng:** (bảng số + thiết bị + điều kiện đo)

**Điểm gãy:** vượt [ngưỡng] khi [điều kiện] — đây là trần thực tế.

**Chốt:** dùng phương án [X].

**Loại phương án nào, vì sao:** ...

**Đánh đổi đã chấp nhận:** ...

**Nợ kỹ thuật để lại:** (cái gì trong slice KHÔNG được bê thẳng vào production)

**Ràng buộc cho story sau:** (số lượng tối đa, cách tổ chức data, thứ không được làm)
```

Kết luận project-level ⇒ ghi `D-xxx` vào `Docs/decision-log.md` theo `workflow/decisions.md`.
Ràng buộc rút ra ⇒ cập nhật `Docs/project-context.md` (đánh dấu 🔒 nếu là contract toàn project).

---

## Bẫy hay gặp

| Bẫy | Hậu quả | Cách tránh |
|---|---|---|
| đo trong Editor rồi kết luận | số sai 2–10×, quyết định sai | build lên device |
| slice phình thành feature | mất tuần, vẫn không có kết luận | budget thời gian + phần "KHÔNG làm" |
| đo ở mức "đủ dùng" | không biết trần, gặp gãy ở level 40 | stress 2×/5×/10× |
| so hai phương án khác điều kiện | chọn nhầm | cùng scene, cùng device, cùng count |
| chỉ nhìn ms/frame | GC spike gây giật không thấy trong average | đo alloc/frame + frame time percentile |
| bê nguyên code slice vào production | hardcode + shortcut lan ra | story refactor riêng, liệt kê nợ ở kết luận |
| slice trả lời câu hỏi khác câu hỏi cần | tưởng an toàn, gãy sau | confirm brief trước khi code |

---

## Output

1. `handoff/<slice>/brief.md` — trước khi code.
2. Scene + script harness trong project (đánh dấu rõ là slice).
3. Bảng số đo.
4. Phần **Kết luận slice** (dán vào story hoặc decision-log).

Slice nào **không** kết luận được thì vẫn phải viết mục Kết luận với trạng thái
`PENDING` + lý do + cần thêm gì — theo `workflow/verification.md`. Không im lặng bỏ dở.
