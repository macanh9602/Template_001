# visual-direction/v1

JSON Schema: `templates/schemas/visual-direction.schema.json` · ví dụ: `templates/visual-direction.example.json`.

**Một schema cho cả proposal lẫn direction.** Khác nhau ở `status` từng section và `version`:

| File | Ai ghi | `version` | `status` được phép |
|---|---|---|---|
| `handoff/visual/proposals/<id>.json` | lab / agent | 0 | DRAFT · CANDIDATE |
| `handoff/visual/direction.vNNN.json` | `tools/promote-direction.ps1` (PO quyết) | N | section đã promote = APPROVED, còn lại giữ |
| `handoff/visual/CURRENT` | promote-direction | — | một dòng: đường dẫn direction hiện hành |

Direction vN **bất biến** (promote lần sau tạo vN+1, `supersedes` trỏ về vN). Lịch sử: `handoff/visual/promotions.jsonl`.

```jsonc
{
  "schema": "visual-direction/v1",
  "project": "block-home",            // id project; KHÔNG dùng làm prefix schema
  "version": 3,
  "proposal": "handoff/visual/proposals/cozy-b.json",
  "supersedes": "handoff/visual/direction.v002.json",
  "referenceLevel": "level_002",      // level đông nhất, dùng cho capture vòng giữa
  "intent": { "summary": "vài câu", "locked": ["hue bão hoà chỉ Room–Human"] },
  "notes": "chọn gì / loại gì / vì sao",
  "sections": {
    "look":   { "status": "APPROVED", "approvedBy": "PO", "approvedAt": "2026-09-28",
                "lockedGroups": ["camera", "floor"], "values": { "camTilt": 22, "floorSeam": 0.17, "floorColor": "#f6ecdc" } },
    "motion": { "status": "CANDIDATE", "values": { "pickDur": 0.08 },
                "metrics": { "drag": { "total": 1.32, "channels": { "block y / lift": { "peak": 1, "tPeak": 0.083, "min": 0, "tMin": 0, "end": 0 } } } } },
    "fx":     { "status": "DRAFT", "values": {}, "cues": {}, "effects": {}, "budget": {} }
  },
  "profileMap": { "camTilt": "VisualProfile.camera.tiltDeg", "floorSeam": "VisualProfile.floor.seamStrength", "blockH": "Mesh.block.height" },
  "tolerance": { "total": 0.10, "peak": 0.15, "tPeak": 0.03, "end": 0.02 },
  "meshBrief": { "block": { "height": 0.5, "cornerRadius": 0.14 } }
}
```

## profileMap

| Dạng | Ý nghĩa | Ai xử lý |
|---|---|---|
| `TenAsset.field.path` | ScriptableObject tên `TenAsset` (duy nhất trong project), `field.path` = SerializedProperty path | `VisualDirectionImporter` |
| `Mesh.*` | không phải runtime data ⇒ brief Blender | `asset-intake` |

Kiểu được importer hỗ trợ: float · int · bool (`true/false/on/off`) · string · enum (tên, không phân biệt hoa/thường/khoảng trắng) · Color (`#RRGGBB[AA]`).
Kiểu khác ⇒ importer báo lỗi, không đoán.

## metrics (section motion)

Mỗi demo: `total` (giây) + `channels.<tên>`: `peak`, `tPeak`, `min`, `tMin`, `end`. Tên channel là key lab xuất;
class `IMotionParityDemo` phía Unity phải dùng **đúng tên đó** (so theo tên, không theo thứ tự).
Tolerance thiếu ⇒ total ±10%, peak ±15%, tPeak ±0.03 s, end 0.02 + 15%.

## Migrate từ schema cũ (`<game>.visual-target/v1`)

`.\tools\promote-direction.ps1 -Migrate handoff\wp-00X\visual-target.json` ⇒ proposal: `look` → `sections.look.values`,
`lockedGroups` → `sections.look.lockedGroups`, `motion` + `motionMetrics` → `sections.motion.values/metrics`,
`fx` → `sections.fx`, field lạ → `legacy`. Mọi section = CANDIDATE; PO promote lại section đã chốt.
