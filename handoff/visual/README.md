# handoff/visual

Visual direction của project. Flow và luật: `workflow/visual-direction.md`.

| Đường dẫn | Ai ghi | Ghi chú |
|---|---|---|
| `proposals/<id>.json` | lab / agent | `visual-direction/v1`, section ≤ CANDIDATE |
| `direction.vNNN.json` | `tools/promote-direction.ps1` | bất biến; section đã promote = APPROVED |
| `CURRENT` | `tools/promote-direction.ps1` | một dòng: direction hiện hành (chưa có = chưa promote lần nào) |
| `promotions.jsonl` | `tools/promote-direction.ps1` | lịch sử promote (append) |
| `reference/<pose>.png` | lab / PO | ảnh reference cho rubric |
| `captures/` | Unity (`VisualCapture`, `MotionParity`) | PNG + `.grey` / `.squint`, motion CSV |
| `reviews/` | reviewer | `<cp>-<iter>.review.json` + `.md` |
| `rubric.md` | project | copy từ `skills/visual-review/refs/rubric.md`, điền ngưỡng |
