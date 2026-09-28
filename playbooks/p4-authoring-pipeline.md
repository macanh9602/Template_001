# P4 — Authoring pipeline

**Vào khi:** game cần scale content/level và phải chốt GD author ở đâu.
**Ra khi:** canonical authoring source + data boundary + validation/import path rõ; chỉ build Unity LE nếu thật sự cần.

Skill chính: `skills/authoring-pipeline/`.

## Bước 1 — Chọn mode

Ghi `Level authoring mode` trong `Docs/project-context.md`:

- `EXTERNAL_TOOL`
- `UNITY_EDITOR`
- `GENERATED`
- `MANUAL_JSON`
- `NONE_YET`

Nếu `NONE_YET`, resolve trước story production authoring.

## Bước 2 — Chốt canonical data

- [ ] Schema/version.
- [ ] Source of truth.
- [ ] Generated/baked/runtime state tách rõ.
- [ ] Migration/compatibility.
- [ ] Runtime loader không phụ thuộc UI authoring.

## Bước 3 — Route

### EXTERNAL_TOOL

```text
GD tool
→ export canonical data
→ Unity validation/import/load
→ conformance tests
→ runtime
```

Không build Unity LE duplicate nếu GD tool đã đủ workflow.

### UNITY_EDITOR

Route `playbooks/p4-level-editor.md`.

### GENERATED

Chốt generator version, constraints, evaluator/golden fixtures và canonical output.

### MANUAL_JSON

Chỉ giữ nếu project intentionally nhỏ/prototype. Production scale cần explicit decision.

## Bước 4 — Prove one complete level

Một level phải đi trọn:

```text
author
→ export/save
→ validate
→ Unity load
→ play
→ reload
```

External tool có executable engine/solver ⇒ thêm conformance fixture trước khi scale.

## Xong khi

- [ ] GD biết tool/source nào authoritative.
- [ ] Unity không duplicate authoring owner.
- [ ] Canonical data/version rõ.
- [ ] Validation failure có message actionable.
- [ ] Một complete level đi từ authoring source tới runtime.
- [ ] Nếu cần Unity LE, mới tiếp tục `p4-level-editor`.
