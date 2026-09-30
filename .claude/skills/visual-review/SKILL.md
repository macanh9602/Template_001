---
name: visual-review
description: >
  Vòng tự động "implement → capture → chấm theo rubric → patch có số → capture lại" để visual trong Unity
  hội tụ về direction đã duyệt (`handoff/visual/CURRENT`), không cần người đứng giữa từng vòng.
  KÍCH HOẠT khi: đang làm visual/feel pass, "visual chưa ổn / chưa giống target", cần evidence ảnh trước/sau,
  hoặc chuẩn bị báo IMPLEMENTED cho story visual. Motion curve → dùng kèm game-feel-motion.
---

# SKILL: visual-review

Model mạnh giúp chọn hướng; **loop có đo đạc** mới làm visual hội tụ. Skill này định nghĩa loop đó.

Điều kiện chạy (HARD GATE):
- Có direction (`.\tools\promote-direction.ps1 -Status`) với section `look` **APPROVED** (xem `skills/visual-lab/`).
  Chưa có ⇒ `BLOCKED: no target`.
- Host có Unity MCP (doctor `UNITY_MCP_SMOKE_PASS`) vào được Play Mode + chụp ảnh. Không có ⇒ `BLOCKED: no capture`,
  không được thay bằng "tự đánh giá từ code".
- Project đã implement `IVisualCaptureScenario` (capture) và `IMotionParityDemo` (motion) — xem `refs/capture-protocol.md`.

---

## Bước đầu tiên — đọc context

Direction hiện hành (`handoff/visual/CURRENT`) · `handoff/visual/reference/*.png` · `refs/rubric.md` ·
`refs/capture-protocol.md` · `refs/review-schema.md` · `skills/game-feel-motion/SKILL.md` (motion) ·
`skills/presentation-lifecycle/SKILL.md` (pool/tween lifecycle).

---

## Loop

```
direction (APPROVED) + reference PNG
  │
  ├─► 0. Tools/Visual Direction/Dry Run → drift? → Import approved sections (số vào Profile SO trước)
  ├─► 1. chọn MỘT lớp đang FAIL cao nhất theo thứ tự ưu tiên
  ├─► 2. implement 1 bounded change (chỉ lớp đó)
  ├─► 3. compile + console sạch
  ├─► 4. capture theo capture-protocol (cố định level, camera, pose, kích thước)
  ├─► 5. review: current vs reference theo rubric → review.json
  ├─► 6. PASS lớp đó? → sang lớp kế · FAIL → NEXT PATCH (có số) → quay lại 2
  └─► dừng: PASS toàn bộ + regression xanh  |  ESCALATE
```

**Thứ tự lớp** (không nhảy cóc): 1 Composition → 2 Shape/silhouette → 3 Color/contrast →
4 Depth/lighting → 5 Motion/feel → 6 VFX/UI polish. Mỗi vòng chỉ sửa **một lớp**.

**Giới hạn:** tối đa **4 vòng / lớp / checkpoint** trong skill; runner (`tools/run-task.ps1`) chặn thêm ở
tầng task (≤ N vòng PATCH / task / direction version, `ESCALATE:LOOP_CAP`).

**ESCALATE** (dừng, báo người, kèm ảnh):
- quá 4 vòng mà lớp đó vẫn FAIL;
- 2 vòng liên tiếp số FAIL không giảm, hoặc patch đảo ngược patch trước (dao động);
- fix cần đổi thứ **đã APPROVED** trong direction (vd. palette đã khoá) ⇒ `TARGET_RECONSIDER`: đề xuất proposal mới, PO promote;
- cần quyết định subjective lớn không có trong direction;
- fix cần mesh mới mà pipeline asset không sẵn sàng.

Không báo `IMPLEMENTED` sau lần code đầu. Chỉ báo khi loop **PASS** hoặc **ESCALATE** có evidence.

---

## Review phải ra actionable delta

Sai:
> "visual chưa đẹp, cần tinh chỉnh thêm"

Đúng:
```
FAIL [L3 color] floor seam cạnh tranh với block
  evidence: cur-03-level_002-idle.squint.png vs reference/idle.png — grid line rõ hơn cạnh block vùng giữa board
NEXT PATCH:
  VisualProfile.floor.seamStrength 0.55 → 0.20 (target 0.20)
```

Mỗi item FAIL: lớp · mô tả một câu · evidence (file ảnh + vùng) · **patch ghi field profile và số cũ → số mới**.
Patch không chỉ ra được field ⇒ vấn đề target, ESCALATE.

Số lệch direction do **implementation** (code chưa đọc profile, importer chưa chạy) ⇒ sửa implementation, không sửa số.
Số trong Profile SO khác direction ⇒ chạy Import, **không** chỉnh tay SO (field trong `profileMap` là derived).

Luôn review cả bản **greyscale + squint** của capture: hierarchy phải còn đọc được khi mất màu.

---

## Motion (lớp 5)

Ảnh tĩnh không chấm được easing:
1. **Parity bằng số:** `Tools/Visual Direction/Motion Parity (CSV + so target)` — sample từng `IMotionParityDemo`
   bằng chính hàm curve runtime dùng → CSV `handoff/visual/captures/motion-<demo>-<tag>.csv` + so với
   `sections.motion.metrics` theo tolerance. Dòng đầu log là kết luận (`[MotionParity] PASS n/n ...`).
   Motion section chưa APPROVED ⇒ đo được nhưng **không PASS**.
2. **Pose capture:** pose giữa chừng (dragging, invalid, mid-hop) để kiểm readability khi động.
3. **Tactile feel** là việc của người. Đánh `PENDING MANUAL`, không giả vờ.

Luật motion giữ nguyên từ `game-feel-motion`: số trong profile, interrupt policy, kill tween khi release pool,
domain không đợi tween.

---

## Particle / VFX

Mọi particle đi qua hạ tầng effect/pool của project (khai ở `Docs/runtime-architecture.md`):
cấm `Instantiate(particlePrefab)` trong gameplay, cấm gameplay component tự giữ / tự `Play()` / tự tune `ParticleSystem`;
effect mới = id + prefab + entry trong profile effect; prewarm lúc load level; tuneable qua profile/prefab, không hardcode.
Rubric: VFX không che gameplay object ở frame quan trọng; overdraw trong budget.

---

## Artifact trên disk

```
handoff/visual/
  CURRENT · direction.vNNN.json · proposals/ · promotions.jsonl
  reference/          <pose>.png                           (từ lab / PO)
  captures/           <cp>-<iter>-<level>-<pose>.png (+ .grey/.squint), motion-<demo>-<tag>.csv
  reviews/            <cp>-<iter>.review.json  + .md tóm tắt
```
Giữ toàn bộ history — "đẹp hơn" phải chỉ ra được đẹp hơn ở ảnh nào.

---

## Script verify (0 token)

Phần đo được bằng máy nên để runner chạy trước agent (`workflow/run-task.md` §3a):

```json
{ "do": "menu", "path": "Tools/Visual Direction/Dry Run (log drift)" },
{ "do": "console", "types": ["warning", "error"], "filter": "[VisualDirection]", "maxCount": 0, "out": "handoff/<wp>/captures/direction-drift.txt" },
{ "do": "console-clear" },
{ "do": "menu", "path": "Tools/Visual Direction/Motion Parity (CSV + so target)" },
{ "do": "console", "types": ["log"], "filter": "[MotionParity] PASS", "minCount": 1, "out": "handoff/<wp>/captures/motion-parity.txt" }
```

---

## Report cuối checkpoint

- Trạng thái: PASS / ESCALATE
- Bảng rubric cuối (item · PASS/FAIL · evidence)
- Số vòng mỗi lớp · patch đã áp (field: cũ → mới)
- Regression: EditMode test xanh
- Performance: draw call / tri / overdraw so với budget project
- `PENDING MANUAL`: tactile feel, thiết bị thật
