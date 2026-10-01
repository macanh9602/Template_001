# START PROMPT — mở phiên mới

> Dán nguyên khối dưới đây vào đầu một phiên agent mới. Thay `[TÊN GAME]` và phần bối cảnh.
> Dùng khi bắt đầu project hoặc mở lại sau một quãng nghỉ.
> Chạy story cụ thể ⇒ dùng `RUN-STORY-PROMPT.md`.

---

```text
Bạn đang làm Unity mobile game [TÊN GAME].

ĐỌC THEO THỨ TỰ, KHÔNG BỎ QUA:
1. AGENTS.md
2. Docs/project-context.md
3. standards/system-design.md
4. Docs/runtime-architecture.md
5. Docs/data-model.md
6. Docs/glossary.md
7. handoff/ROADMAP.md
8. playbook tương ứng với phase hiện tại

Nếu đây là bootstrap/adoption hoặc project-context còn placeholder:
→ chạy handoff/PROJECT-READINESS-PROMPT.md TRƯỚC active story.

LUẬT BOOTSTRAP/ADOPTION:
- Project hiện hữu thắng skeleton generic: inspect root/scene/assembly thật trước khi tạo code mới.
- Không tạo production script root song song.
- Không thêm asmdef trước khi audit dependency thật.
- Không migrate folder/namespace hàng loạt trong bootstrap nếu chưa có decision riêng.
- Không coi asset/reference ngoài workspace là blocker nếu user chưa nói nó authoritative.
- Roadmap có thể có phase/capability dài hạn, nhưng chỉ materialize story executable kế tiếp sau gate.

LUẬT LÀM VIỆC:
- Open Question blocker cấp gameplay/project contract/data/scope ⇒ hỏi trắc nghiệm có recommend.
- Decision local/reversible ⇒ agent tự chốt và ghi notes.
- Quyết định không gian/chuyển động/timing/nhiều biến ⇒ dựng .html visualiser trước khi hỏi.
- Không hardcode tunable: prefab field · Profile SO · level data.
- Không GameObject.CreatePrimitive / new Material / Shader.Find trong runtime production path.
- Dùng pooling cho thứ sinh nhiều; không Instantiate/Destroy trong gameplay loop.
- Text dùng TMP qua prefab/script quản lý.
- Runtime bug chưa proven ⇒ instrument/read evidence trước speculative fix.
- Không ghi PASS/DONE cho thứ chưa chạy thật.

TRẢ LỜI:
- Tiếng Việt; technical term giữ English.
- Ngắn, rõ, ưu tiên decision + evidence.
- Vấn đề trình bày bằng: Vấn đề · Bằng chứng · Ảnh hưởng.

BỐI CẢNH HIỆN TẠI:
[dán vào đây]

VIỆC ĐẦU TIÊN:
[một trong các dạng]
- "Chạy PROJECT-READINESS-PROMPT, chưa implement."
- "Đọc context rồi tóm tắt lại, chưa làm gì cả."
- "Chạy playbooks/pX-....md từ gate hiện tại."
- "Chạy story executable hiện tại."
```

---

## Gợi ý

**Có executable prototype (`.html`) hoặc project chưa có identity** ⇒ không dùng START-PROMPT; chạy Phase A:
`handoff/PHASE-A-DIRECTOR-PROMPT.md` + `handoff/PHASE-A-CRITIC-PROMPT.md` song song
(`workflow/phase-a-contract-bootstrap.md`). PROJECT-READINESS chạy như subtask bên trong Director, PO không phải mở thêm phiên.

**Project mới hoàn toàn**

```text
Chạy handoff/PROJECT-READINESS-PROMPT.md để xác nhận GREENFIELD, sau đó chạy
playbooks/p1-bootstrap.md. Đừng hỏi lại thứ GDD đã trả lời.
```

**Adopt project/template có sẵn**

```text
Chạy handoff/PROJECT-READINESS-PROMPT.md. Ưu tiên EXISTING_PROJECT_ADOPTION:
inspect canonical root/scene/assembly thật, không tạo root mới và không migrate trước khi tôi chốt.
```

**Mở lại sau quãng nghỉ** ⇒ tóm tắt six-file context trước khi implement.
