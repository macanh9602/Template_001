# Prototype → Contract workflow

> Dùng khi đầu vào của project/feature không chỉ là GDD mà có executable prototype, external Level Editor,
> reference implementation, simulator/solver, video/mockup hoặc runtime cũ.
>
> Mục tiêu: **không bắt Unity worker tự diễn giải prototype**. Frontier/planning layer phải biến input
> thành contract + conformance evidence trước khi lower-reasoning worker implement.

---

## 1. Classify input, không gọi mọi thứ là "reference"

Ghi vào `Docs/project-context.md`:

- `Product input`: `GDD_ONLY / EXECUTABLE_PROTOTYPE / REFERENCE_GAME / VIDEO_MOCKUP / EXISTING_RUNTIME`.
- `Level authoring mode`: `UNITY_EDITOR / EXTERNAL_TOOL / GENERATED / MANUAL_JSON / NONE_YET`.
- External authoring source/path nếu có.
- Prototype authority theo **từng area**, không theo cả file.

Một executable prototype có thể authoritative cho gameplay/data nhưng chỉ reference cho visual/feel.

---

## 2. Authority Matrix

Copy `templates/prototype-authority-contract.md` vào project/reference hoặc ghi tương đương trong Docs.

Mỗi area chỉ được một vai trò chính:

- `AUTHORITATIVE` — Unity phải giữ semantics.
- `ORACLE` — dùng để so expected result/conformance; không nhất thiết ship runtime.
- `REFERENCE` — học intent/style/pattern; không copy contract mù quáng.
- `OUT_OF_SCOPE` — không dùng để quyết implementation.

Các area tối thiểu:

- gameplay semantics;
- level schema/data;
- solver/evaluator;
- authoring workflow;
- input;
- progression;
- visual;
- feel/timing;
- audio;
- performance behavior.

---

## 3. Extract contract

Frontier/planning layer tách:

```text
OBSERVABLE RULES
→ player command / action
→ state mutation
→ automatic resolution
→ win / lose / completion

DATA
→ source of truth
→ generated/baked
→ runtime-only

AUTHORING
→ ai tạo level
→ tool nào ghi data
→ validation ở đâu
→ version/migration

AMBIGUITIES
→ contradiction trong prototype
→ behavior chỉ tình cờ do implementation
→ product decision còn thiếu
```

Không hỏi lại thứ prototype/data đã chứng minh được.

---

## 4. Resolve ambiguity trước worker

Chỉ hỏi Dev decision có thể đổi:

- gameplay semantics;
- serialization/data compatibility;
- runtime/physics authority;
- hard-to-reverse performance direction;
- deliverable/scope.

Hỏi bằng `workflow/ask-and-visualise.md`.

Nếu prototype tự mâu thuẫn, **không tự chọn một nhánh**. Ghi contradiction + hỏi Dev.

---

## 5. Canonical data boundary

Nếu GD author bằng tool ngoài Unity:

```text
External authoring tool
    ↓
canonical exported data
    ↓
schema/version validation
    ↓
Unity loader
    ↓
runtime
```

Runtime không được phụ thuộc cách level được author.

Không build Unity Level Editor chỉ vì Template có `skills/level-editor/`.
Chỉ build Unity LE khi `Level authoring mode = UNITY_EDITOR`.

---

## 6. Conformance Pack

Khi prototype/reference có logic executable, ưu tiên tạo:

```text
reference/conformance/
├── README.md
├── case-001-input.json
├── case-001-expected.json
├── case-002-input.json
└── case-002-expected.json
```

Một case nên mô tả:

```text
Given:
  authored level
  initial state
  command/action sequence

Expect:
  resulting semantic state
  emitted semantic events
  win/lose/result
  optional oracle metric/solver result
```

Không dùng screenshot làm oracle cho gameplay rule nếu có thể assert semantic state.

Conformance fixture phải versioned nếu tool/oracle tạo expected result có thể đổi.

---

## 7. Exit

Chỉ sang Bootstrap khi:

- [ ] Authority Matrix đã lock.
- [ ] Canonical level/data source rõ.
- [ ] External authoring ownership rõ.
- [ ] Prototype contradiction đã resolve hoặc được ghi BLOCKED.
- [ ] Conformance Pack có ít nhất các case critical nếu executable oracle tồn tại.
- [ ] Unity worker không cần đọc toàn prototype để đoán semantics.
