# RUN STORY PROMPT — chạy một story

> Dán khối dưới đây khi giao agent implement một story đã viết xong.
> Story chưa viết ⇒ viết trước bằng `templates/story.md`.

---

```
Chạy handoff/story-XXX-<slug>/story.md.

ĐỌC TRƯỚC KHI CODE:
1. AGENTS.md
2. Docs/project-context.md          — fact đã chốt, 🔒 = không tự đổi
3. standards/system-design.md       — layer nào chịu trách nhiệm gì
4. standards/code-style.md          — bắt buộc, trước MỌI bước viết code
5. story.md của story này
6. Skill mà story chỉ định (skills/README.md để tra)
7. standards/anti-patterns.md       — nếu story chạm vùng đã từng có bug

Không scan toàn project. Bắt đầu từ file story chỉ định.

TRƯỚC KHI VIẾT DÒNG CODE ĐẦU TIÊN:
- Tóm tắt nội bộ/ngắn trong implementation notes: story làm gì, chạm layer nào, tạo contract gì.
- Story rõ và không có blocker ⇒ **implement ngay, không chờ confirm, không re-plan lại story**.
- Story còn Open Question dạng blocker hoặc mâu thuẫn với Docs/standards ⇒ escalate bằng trắc nghiệm; đừng tự hoà giải.

TRONG LÚC CODE:
- Vừa code vừa cập nhật handoff/story-XXX/implementation-notes.html — ghi NGAY khi phát sinh.
- Decision **cục bộ, reversible, không đổi contract/gameplay/data/scope** ⇒ agent tự quyết và ghi ngắn vào notes.
- Chỉ DỪNG hỏi khi decision chạm guardrail/escalation trong `AGENTS.md §6` hoặc có hard constraint của dev bị xung đột.
- Số liệu tune được ⇒ prefab field / Profile SO / level data. KHÔNG hardcode.
- KHÔNG CreatePrimitive, KHÔNG new Material, KHÔNG Shader.Find trong runtime path.
- ObjectPool (VTLTools) cho thứ sinh nhiều; không Instantiate/Destroy trong gameplay loop.
- Text qua prefab TMP có script quản lý (Init/Show/Hide/feedback), pooled.
- Domain không phụ thuộc Visual. Visual không quyết gameplay. Không FindObjectOfType
  hay scene search trong gameplay loop.
- Từ mới hoặc đổi nghĩa ⇒ cập nhật Docs/glossary.md trong cùng story này.
- Quyết định project-level ⇒ D-xxx trong Docs/decision-log.md
  (khung: templates/decision-record.md).

TRƯỚC KHI BÁO XONG:
- Điền bảng Verification trong story theo workflow/verification.md.
  Trạng thái hợp lệ: PASS / FAIL / PENDING / PENDING RERUN / PARTIAL /
  KNOWN BASELINE FAILURES / N/A.
- KHÔNG ghi PASS cho thứ chưa chạy thật. Chưa chạy ⇒ PENDING + nêu cần gì để chạy.
- Có UI ⇒ tự bấm hết luồng một lượt (UI walkthrough), ghi lại đã bấm gì.
- Kiểm: load → unload → load lại 10 lần, không rò object/event.
- Đo performance nếu story có tiêu chí performance — trên thiết bị chuẩn, không phải Editor.
- Chạy phần Harvest: có gì nên đẩy về standards/anti-patterns.md, knowledge/, hoặc workflow/?

BÁO CÁO CUỐI:
- Làm được gì (quan sát được từ bên ngoài).
- Bảng Verification.
- Cái gì lệch so với story và vì sao.
- Nợ để lại.
- Câu hỏi còn mở.
Ngắn gọn, tiếng Việt, technical term giữ English.
```

---

## Biến thể

**Story quá lớn, muốn chia mốc** ⇒ thêm:

```
Story này size L. Tự chia thành các mốc, mỗi mốc CHẠY ĐƯỢC và KIỂM CHỨNG ĐƯỢC, rồi implement tuần tự. Chỉ dừng hỏi nếu cách chia làm đổi deliverable/scope/contract của story.
```

**Muốn agent tự do hơn (Unity MCP đã kết nối)** ⇒ thêm:

```
Story này viết ở mức mục tiêu, không mô tả cách làm. Bạn tự quyết cách implement
trong khuôn khổ standards/. Nhưng: quyết định nào tạo contract cho story sau,
hoặc có >1 phương án hợp lý, thì vẫn phải hỏi trắc nghiệm trước.
```

**Sửa bug thay vì làm feature** ⇒ dùng `skills/debug-audit/` và thêm:

```
Đây là bug đã fix một lần mà vẫn còn. KHÔNG đoán nguyên nhân và fix tiếp.
Chạy skills/debug-audit/: liệt kê hypothesis, đặt instrumentation tại điểm ra quyết định,
nhờ tôi repro và gửi log, đọc data thật rồi mới kết luận.
Kết luận phải trích đúng dòng log chứng minh nguyên nhân.
```
