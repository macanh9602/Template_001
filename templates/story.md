# Story [XXX] — [Tên nói được KẾT QUẢ, không phải chủ đề]

> Size: **S / M / L** — theo `workflow/loop.md`.
> Copy file này vào `handoff/story-XXX-<slug>/story.md`.
> Xoá mọi phần không dùng. Story để lại phần rỗng là story chưa viết xong.

---

## 0. Vì sao có story này

| Vấn đề | Bằng chứng | Ảnh hưởng |
|---|---|---|
| | trích **đúng chỗ**: file:line, log, GDD, hoặc số đo | người chơi / GD chịu gì |

Không điền được cột **Bằng chứng** ⇒ chưa đủ dữ liệu để làm. Quay lại `enrich-context`.

## 1. Kết quả mong đợi

Một câu, quan sát được từ bên ngoài:

> Sau story này, [ai] có thể [làm gì] mà trước đó không làm được.

## 2. Ranh giới

**Làm:**
-

**KHÔNG làm trong story này:**
-

Phần "KHÔNG làm" là bắt buộc. Thiếu nó thì scope sẽ trôi và không ai biết lúc nào là xong.

## 3. Context cần đọc

- `Docs/project-context.md`  ·  `standards/system-design.md`  ·  `standards/code-style.md`
- Skill: `skills/<...>/SKILL.md`
- Story liên quan: `handoff/story-XXX/`
- Decision liên quan: `D-XXX`

## 4. Đầu vào đã có

| Cái gì | Ở đâu | Trạng thái |
|---|---|---|

## 5. Việc cần làm

> Sizing (`workflow/loop.md`): **S** — 3–6 gạch đầu dòng, để agent tự quyết cách làm.
> **M** — có bảng layer. **L** — chia mốc, mỗi mốc chạy được và kiểm chứng được.

### 5.1 Layer assignment

| Việc | Layer | Ghi chú |
|---|---|---|
| | Domain / Visual / Data / Editor / HUD | |

Việc nào không rơi gọn vào một layer ⇒ **escalate**, đừng tự đặt bừa.

### 5.2 Số liệu tune được

| Giá trị | Nằm ở đâu | Default | Ai chỉnh |
|---|---|---|---|

Mọi số phải có chỗ ở: **prefab field · Profile SO · level data**. Không hardcode (`AGENTS.md §5`).

### 5.3 Contract mới (nếu có)

| Contract | Ai dùng | Đổi sau này tốn gì |
|---|---|---|

## 6. Acceptance criteria

Viết dạng **kiểm chứng được**, không phải "hoạt động tốt".

- [ ]
- [ ] **Performance:** [chỉ số] ≤ [ngưỡng] trên [thiết bị chuẩn], đo ở [cấu hình nặng nhất]
- [ ] Load → unload → load lại 10 lần: không rò object, không rò event
- [ ] Không hardcode số; không `CreatePrimitive`; không `new Material` / `Shader.Find`

## 7. Cần hỏi trước khi làm

- [ ] Q1 — ... → phương án đề xuất: ... (hỏi bằng **trắc nghiệm**, có visualiser nếu liên quan
      không gian / chuyển động / timing / phân bố số)

Story còn open question dạng **blocker** ⇒ chưa được bắt đầu code.

## 8. Rủi ro

| Rủi ro | Dấu hiệu sẽ thấy | Ứng phó |
|---|---|---|

---

## 9. Khi implement

- Vừa code vừa cập nhật `implementation-notes.html` — ghi ngay, đừng để cuối mới nhớ lại.
- Gặp quyết định/trade-off spec chưa định ⇒ **DỪNG, hỏi trắc nghiệm**, rồi ghi vào notes.
- Quyết định project-level ⇒ `D-xxx` trong `Docs/decision-log.md`.
- Từ mới / đổi nghĩa ⇒ cập nhật `Docs/glossary.md` **trong cùng story**.

## 10. Verification

Điền theo `workflow/verification.md`. Trạng thái: PASS / FAIL / PENDING / PENDING RERUN /
PARTIAL / KNOWN BASELINE FAILURES / N/A.

| Hạng mục | Trạng thái | Bằng chứng (log / số đo / ảnh / bước đã chạy) |
|---|---|---|

**Không ghi PASS cho thứ chưa chạy thật.**

## 11. Harvest

Theo `workflow/harvest.md`:

- Bug/anti-pattern mới ⇒ `standards/anti-patterns.md`
- Pattern generic ⇒ `knowledge/`
- Bài học quy trình ⇒ `workflow/`
- Story quá chặt / quá lỏng ở đâu ⇒ ghi lại, dùng để chỉnh sizing
