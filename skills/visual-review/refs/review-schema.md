# review.json — một file mỗi vòng

`handoff/visual/reviews/<cp>-<iter>.review.json` (+ `.md` tóm tắt 5–10 dòng cho người đọc). UTF-8 **không BOM**.

```jsonc
{
  "schema": "visual-review/v1",
  "checkpoint": "A",                    // project tự đặt (vd A = visual language, B = interaction feel, C = feedback polish)
  "iteration": 2,
  "layer": 3,                           // lớp đang sửa vòng này (1..6)
  "targetRef": "handoff/visual/direction.v003.json",   // direction đã dùng để chấm (không phải CURRENT)
  "reviewedCommit": "a1b2c3d",
  "reviewer": "claude",                 // host chấm; không phải phiên đã implement
  "captures": ["captures/A-02-level_002-idle.png", "captures/A-02-level_002-idle.squint.png"],
  "rubric": [
    { "id": "R6", "status": "PASS", "evidence": "block sat 0.18 < 0.28" },
    { "id": "R8", "status": "FAIL", "evidence": "A-02 idle.squint: grid line rõ hơn cạnh block vùng giữa board",
      "note": "floor seam cạnh tranh block" }
  ],
  "nextPatch": [
    { "field": "VisualProfile.floor.seamStrength", "from": 0.55, "to": 0.20, "why": "R8", "targetValue": 0.20 }
  ],
  "scoreDelta": { "passed": 11, "failed": 2, "previousFailed": 4 },
  "verdict": "PATCH",                   // PASS | PATCH | TARGET_RECONSIDER | BLOCKED  (cùng enum với runner: templates/schemas/review.schema.json)
  "decision": "CONTINUE",               // CONTINUE | LAYER_PASS | CHECKPOINT_PASS | ESCALATE
  "escalation": null                    // khi ESCALATE: { "reason": "...", "kind": "TARGET_RECONSIDER|LOOP|SUBJECTIVE|ASSET", "question": "trắc nghiệm 2–4 option + recommend" }
}
```

## Luật

- `nextPatch[].field` phải là field có trong `profileMap` của direction, hoặc `Mesh.*` (⇒ asset pipeline).
- Patch chỉ chạm lớp `layer` của vòng này.
- `to` khác `targetValue` phải có `why` (vd. URP lighting làm màu tối hơn HTML ⇒ bù). Bù lệch > 25% so với target ⇒ ESCALATE thay vì tự bù.
- Fix cần đổi field thuộc section **APPROVED** ⇒ `verdict: TARGET_RECONSIDER` + `decision: ESCALATE` (đề xuất proposal mới), không tự đổi số.
- `targetRef` ≠ `handoff/visual/CURRENT` lúc đọc review ⇒ review **STALE**, phải chấm lại trên direction mới.
- `failed` không giảm 2 vòng liền ⇒ `ESCALATE`.
- ESCALATE luôn kèm câu hỏi trắc nghiệm theo `workflow/ask-and-visualise.md`, không hỏi mở.
