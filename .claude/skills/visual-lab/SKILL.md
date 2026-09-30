---
name: visual-lab
description: >
  Chốt art direction và motion TRƯỚC khi implement visual trong Unity: dựng lab HTML trên level thật,
  so sánh 2–4 phương án cạnh nhau theo từng lớp, xuất **proposal** `visual-direction/v1` để PO promote
  thành direction. KÍCH HOẠT khi: bắt đầu visual/feel pass, "chưa biết nên làm art kiểu gì", đổi art
  direction, thêm element visual mới cần chốt look, hoặc visual-review ESCALATE vì target mơ hồ.
  KHÔNG dùng để tune một tween lẻ (→ game-feel-motion).
---

# SKILL: visual-lab

Vấn đề cốt lõi: **agent tự review visual mà không có target cố định thì sẽ trôi.** Mỗi vòng đều "đẹp hơn"
theo một hướng khác. Lab biến "đẹp" thành **một file có số**; `visual-review` chỉ đo khoảng cách tới file đó.

```
visual-lab (agent dựng, PO chọn) → proposal (CANDIDATE) → promote-direction.ps1 (PO) → direction vN (APPROVED theo section)
                                                                                     → Importer → Profile SO → visual-review loop
```

Flow đầy đủ, vai trò, lệnh: `workflow/visual-direction.md`. Schema: `refs/visual-direction-schema.md`.

---

## Bước đầu tiên — đọc context

`Docs/project-context.md` · `Docs/glossary.md` · `knowledge/motion/presets.json` ·
`skills/game-feel-motion/SKILL.md` (khung 5 pha, interrupt policy) · direction hiện hành
(`.\tools\promote-direction.ps1 -Status`).

---

## Luật

1. **Lab dựng trên level JSON thật**, không phải cảnh mẫu. Level **đông nhất** quyết định độ ồn chịu được,
   không phải level đẹp nhất. Ghi level đó vào `referenceLevel`.
2. **So sánh cạnh nhau, cùng lúc.** 2–4 pane, cùng camera, cùng level. Luôn có pane **Baseline** (giống build
   hiện tại). Có chế độ xem nhiều level để kiểm consistency.
3. **Chốt theo lớp, từ thô tới mịn.** Khoá xong lớp này mới sang lớp sau:

   | Lớp | Câu hỏi chốt | Section |
   |---|---|---|
   | L1 Composition | camera, background: board chiếm bao nhiêu màn | `look` |
   | L2 Shape | silhouette, khối liền hay rời | `look` |
   | L3 Color | ai được giữ hue bão hoà | `look` |
   | L4 Depth/light | depth còn đọc được khi tắt real-time shadow không | `look` |
   | M1–Mn Motion | số cho MotionProfile / timing profile + `metrics` | `motion` |
   | FX | cue, effect layer, budget | `fx` |

4. **Color language:** quyết định sớm "ai được giữ hue bão hoà" và ghi vào `intent.locked`. Ví dụ Block Home:
   hue bão hoà chỉ cho cặp Room–Human, block (furniture) mang value/material tonal. Phá rule phải có lý do trong `notes`.
5. **Mọi số trong lab map 1:1 sang profile** qua `profileMap` (`TenAsset.field.path`). Field không map được
   sang Profile SO hoặc `Mesh.*` (brief Blender) ⇒ **không được có trong lab** (chốt xong không implement được, hoặc bị hardcode).
6. **Lab không bao giờ ghi `APPROVED`.** Lab xuất `CANDIDATE` vào `handoff/visual/proposals/<id>.json`.
   APPROVED chỉ do `tools/promote-direction.ps1` ghi theo quyết định PO, **theo section**.
7. **Lab không phải final art.** Nó chốt *hướng + số*. HTML render ≠ URP render; parity đo ở visual-review
   bằng rubric + số, không pixel-diff.

---

## Flow

### Bước 1 — chuẩn bị phương án
- Pane Baseline + 2–3 direction **khác nhau rõ rệt** (không phải 3 biến thể màu của cùng một ý).
- Element/option mới chưa có trong lab → thêm vào schema của lab (kèm đường dẫn profile) thay vì dựng lab riêng.
- Điểm xuất phát cho lab: `templates/visualiser-base.html`. Lab cần có: pane, lock theo nhóm, Greyscale + Squint, export proposal.

### Bước 2 — hỏi trắc nghiệm theo lớp
Theo `workflow/ask-and-visualise.md`: 2–4 option, đúng một recommend, mỗi option ghi **Được / Mất / ảnh hưởng
mobile** (draw call, overdraw, số mesh variant cần làm). Bật **Greyscale + Squint** khi hỏi về color/value.

### Bước 3 — khoá lớp + ghi quyết định
PO tick 🔒 nhóm (`lockedGroups`), ghi `notes` (chọn gì, loại gì, vì sao). Preset không ghi đè nhóm đã khoá.

### Bước 4 — xuất proposal + promote
1. Export JSON từ lab → `handoff/visual/proposals/<id>.json` (`status: CANDIDATE`).
2. Motion: lab xuất `sections.motion.metrics` (total, peak, tPeak, min, end mỗi channel mỗi demo) — target
   máy-kiểm-được cho `MotionParity`.
3. PO chọn section đã chốt:
   `.\tools\promote-direction.ps1 -Proposal handoff\visual\proposals\<id>.json -Sections look -DryRun` rồi bỏ `-DryRun`.
4. Chụp reference PNG cho từng pose (tên pose + kích thước như `skills/visual-review/refs/capture-protocol.md`)
   → `handoff/visual/reference/`.
5. `meshBrief` → Blender generation brief (`asset-intake`).

**Gate:** chưa có direction với section `look` APPROVED ⇒ `visual-review` không được chạy loop (`BLOCKED: no target`).

---

## Bẫy hay gặp

| Bẫy | Hậu quả |
|---|---|
| chốt màu trước khi chốt shape | đổi shape xong phải chọn lại màu |
| chỉ xem level đẹp nhất | level đông vỡ readability lúc production |
| 3 direction chỉ khác nhau màu | không thật sự có lựa chọn |
| lab có tham số không map được sang profile | chốt xong không implement được, hoặc bị hardcode |
| lab export thẳng thành target | agent "tự duyệt"; mất lịch sử quyết định → luôn qua proposal + promote |
| sửa tay Profile SO field có trong `profileMap` | Import lần sau ghi đè; muốn đổi → sửa proposal + promote |
| lấy HTML render làm chuẩn pixel | URP khác lighting/AA; chuẩn là rubric + số |
