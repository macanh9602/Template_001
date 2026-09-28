# `art-manifest.json` — hợp đồng giữa HTML, image model, script và Unity

> Model sinh manifest từ HTML, GD duyệt, script + Unity đọc. **Tên file cuối cùng = `id`**, luôn luôn.
> Senior art reskin = thay file cùng `id`, cùng kích thước/pivot/slice.

## Quy ước `id`

`<prefix>_<group>_<name>[_<variant>][_<state>]` — snake_case, `^[a-z][a-z0-9_]*$`.

- `prefix`: lấy từ `Docs/project-context.md` (ví dụ `bh` cho Block Home, `pp` cho Pour Path).
- `group`: `ui`, `hud`, `popup`, `icon`, `board`, `elem`, `prop`, `tile`, `part`.
- `state` (UI): `normal`, `pressed`, `disabled`, `on`, `off`.
- Mảnh modular: `_<part>` (`cap`, `body`, `base`) hoặc `_m<mask>` (tile mask, xem `gen-2d.md`).

Ví dụ: `bh_ui_btn_primary_normal`, `bh_tile_block_m05`, `pp_prop_valve`, `pp_part_pipe_body`.

## Schema (rút gọn)

```jsonc
{
  "schema": "art-manifest/1",
  "status": "DRAFT",              // DRAFT → LOCKED (GD duyệt) → SUPERSEDED
  "source_html": "art/src/Pour_Path_3D.html",       // bản gốc GD, read-only
  "source_html_hash": "sha1:…",   // HTML gốc đổi → normalize lại + manifest review lại
  "normalized_html": "art/src/Pour_Path_3D.art.html",
  "parity": { "status": "PASS", "worst_ratio": 0.0, "report": "art/review/parity/" },
  "ref_resolution": [1080, 1920],
  "style": {
    "anchor": "art/style/style-anchor.png",
    "prompt_block": "art/style/style-block.txt",
    "palette": { "ink": "#4A3558", "accent": "#FF5E87", "teal": "#35C4AE" }
  },
  "items": [
    {
      "id": "pp_ui_btn_valve_normal",
      "kind": "ui_2d",             // ui_2d | sprite_2d | modular_2d | prop_3d | modular_3d
      "group": "ui",
      "desc": "Nút chính 'Mở van', bo góc 16px, đáy đậm hơn 5px (pressable)",
      "html_ref": "#valve",        // selector / tên hàm để truy ngược
      "size_px": [360, 144],       // ở khung 1080×1920 = CSS px trong ?frame=phone × 3
      "pivot": [0.5, 0.5],
      "nine_slice": [48, 48, 48, 56],   // L T R B px; null nếu không
      "text_baked": false,         // text do TMP render, không vẽ vào ảnh
      "states": ["normal", "pressed"]
    },
    {
      "id": "pp_prop_valve",
      "kind": "prop_3d",
      "html_ref": "buildSources() › valve",
      "bounds_m": [0.5, 0.5, 0.12],
      "tri_max": 600,              // từ performance-budget.md, không bịa
      "material_ids": ["metal", "wheel_red"],   // key trong ART.palette → ô màu palette texture
      "rig_later": false
    },
    {
      "id": "pp_part_pipe",
      "kind": "modular_3d",
      "parts": ["cap", "body", "noz"],
      "assembly": { "stretch_part": "body", "axis": "y", "driven_by": "level.sources[].r" }
    },
    {
      "id": "bh_tile_block",
      "kind": "modular_2d",
      "tileset": "mask16",         // mask16 | nine_slice | blob47 — chọn ở review
      "cell_px": 120,
      "driven_by": "level.blocks[].cells"
    }
  ],
  "out": [
    { "what": "hạt nước (InstancedMesh beads)", "kind": "sim", "route": "technical-slice" },
    { "what": "kính cốc MeshPhysicalMaterial", "kind": "shader", "route": "technical-slice" },
    { "what": "cốc: rT/rB/Hh theo level", "kind": "procedural", "route": "technical-slice" },
    { "what": "tab Node lưới/Kéo đoạn, nút Editor", "kind": "dev_ui", "route": "ignore" }
  ],
  "missing": [
    "Chưa có ảnh style reference"
  ]
}
```

Field không kiểm được bằng máy (dáng, cảm giác) → chỉ nằm trong `desc`. Kiểm được (size, slice,
tri, bounds, material count) → field riêng, script validate.

## Ví dụ phân loại từ 2 file mẫu

**Block Home (2D canvas)**

| Element trong HTML | kind | Ghi chú |
|---|---|---|
| `drawBlock` khối gỗ polyomino | `modular_2d` (`mask16`) | shape theo `blocks[].cells` |
| `drawFloor` ô sàn | `modular_2d` (`nine_slice` hoặc tile đơn) | |
| `drawRooms` phòng màu + cửa cung tròn | `modular_2d` + `sprite_2d` cửa | màu tint theo `COLORS`, gen 1 bản trắng rồi tint |
| `drawHuman` người tròn + mắt | `sprite_2d` (thân) + `sprite_2d` (mắt, blink) | màu tint runtime |
| sao, nút, card win, toast | `ui_2d` | |
| confetti | `vfx` → OUT | |
| "Xoá tiến trình", level grid dev | `dev_ui` hoặc `ui_2d` | **hỏi GD** — màn chọn level có ship không? |

**Pour Path 3D (three.js)**

| Element | kind | Ghi chú |
|---|---|---|
| van, tay quay, đèn | `prop_3d` | tĩnh |
| đá `RoundedBoxGeometry` | `prop_3d` | |
| ống nguồn (pipe/collar/nozzle, dài theo level) | `modular_3d` | stretch body theo y |
| đoạn ống cố định (seg + cap + bolt) | `modular_3d` | body stretch giữa 2 cap |
| cốc (lathe, rT/rB/Hh theo level) | `procedural` → OUT | trừ khi GD chốt vài size cố định → `prop_3d` từng size |
| kính, hạt nước | `shader` / `sim` → OUT | |
| board slab + mặt board | `modular_3d` hoặc `procedural` | **hỏi dev** — BW/BH đổi theo level |
| tray piece icon, nút, sao, wincard | `ui_2d` | |
| tab điều khiển, Editor panel | `dev_ui` | |

Màu cùng element khác nhau chỉ ở màu (người/phòng/cốc) → gen **một bản grayscale/trắng** + tint
runtime, không gen N bản màu. Ít texture, reskin dễ.
