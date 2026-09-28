# START PROMPT — mở phiên mới

> Dán nguyên khối dưới đây vào đầu một phiên agent mới. Thay `[TÊN GAME]` và phần bối cảnh.
> Dùng khi **bắt đầu dự án** hoặc **mở phiên sau một quãng nghỉ**.
> Chạy một story cụ thể ⇒ dùng `RUN-STORY-PROMPT.md`.

---

```
Bạn đang làm Unity mobile game [TÊN GAME].

ĐỌC THEO THỨ TỰ, KHÔNG BỎ QUA:
1. AGENTS.md                       — guardrail, luật cứng
2. workflow/loop.md                — nhịp làm việc, story sizing
3. Docs/project-context.md         — stack, thiết bị chuẩn, fact đã chốt (🔒 = contract, không tự đổi)
4. standards/system-design.md      — kiến trúc bắt buộc
5. Docs/runtime-architecture.md    — kiến trúc cụ thể của game này
6. Docs/data-model.md              — cái gì là source of truth
7. Docs/glossary.md                — một khái niệm một tên
8. handoff/ROADMAP.md              — đang ở đâu, làm gì tiếp

Đọc thêm skill/playbook chỉ khi task chạm đúng phần đó — tra ở skills/README.md
và playbooks/README.md. Không load hết.

LUẬT LÀM VIỆC:
- Có Open Question **blocker cấp gameplay/project contract/data/scope** chưa chốt ⇒ tạo CỬA SỔ TRẮC NGHIỆM có recommend. Decision local/reversible thì agent tự chốt.
  Không tự quyết thay tôi. Free-text chỉ để xin dữ liệu thô (log, số đo, video, screenshot).
- Quyết định liên quan không gian / chuyển động / timing / nhiều biến / phân bố số
  ⇒ dựng .html visualiser TRƯỚC khi hỏi (templates/visualiser-base.html).
- KHÔNG hardcode số liệu. Mọi giá trị tune được nằm ở: prefab field · Profile SO · level data.
- KHÔNG GameObject.CreatePrimitive, KHÔNG new Material(...), KHÔNG Shader.Find(...) trong runtime path.
- Dùng ObjectPool (namespace VTLTools) cho thứ sinh nhiều. Không Instantiate/Destroy trong gameplay loop.
- Text dùng TMP qua prefab riêng có script quản lý: Init(params) · Show/Hide · anim feedback.
- Fix rồi vẫn lỗi ⇒ instrument và đọc data thật, KHÔNG đoán tiếp (skills/debug-audit/).
- Không ghi PASS/DONE cho thứ chưa chạy thật (workflow/verification.md).

TRẢ LỜI:
- Tiếng Việt. Technical term giữ nguyên English.
- Ngắn, rõ, ưu tiên decision + bằng chứng. GĐ đọc cũng hiểu.
- Vấn đề trình bày bằng bảng 3 cột: Vấn đề · Bằng chứng · Ảnh hưởng.

BỐI CẢNH HIỆN TẠI:
[dán vào đây: đang ở giai đoạn nào, vừa xong gì, đang vướng gì]

VIỆC ĐẦU TIÊN:
[một trong các dạng dưới đây]
- "Đọc context rồi tóm tắt lại cho tôi xem bạn hiểu đúng chưa, chưa làm gì cả."
- "Chạy playbooks/pX-....md, bắt đầu từ Bước 1."
- "Viết story cho [việc], theo templates/story.md."
```

---

## Ghi chú khi dùng

**Dự án mới hoàn toàn** ⇒ việc đầu tiên là:

```
Chạy playbooks/p1-bootstrap.md. Bắt đầu từ Bước 1: hỏi tôi theo batch trắc nghiệm
để điền Docs/. GDD ở [đường dẫn]. Đừng hỏi lại thứ GDD đã trả lời.
```

**Mở lại sau quãng nghỉ** ⇒ việc đầu tiên nên là tóm tắt hiểu biết trước khi làm. Agent tóm tắt sai
thì phát hiện ngay, rẻ hơn nhiều so với phát hiện sau khi đã code.

**Agent bắt đầu assume** ⇒ nói: *"bật `skills/enrich-context/`"*. Đó là cái phanh.
