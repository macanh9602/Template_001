---
name: art-gen
description: >
  Biến file .html GD vibe (2D canvas hoặc three.js) thành asset đồ họa placeholder chất lượng khá
  bằng frontier model (GPT/Claude image gen cho 2D, three.js export .glb cho 3D) trước khi senior art
  reskin. KÍCH HOẠT khi: "gen art từ html", "làm đồ họa cho prototype", "gen UI/sprite theo art style",
  "xuất model 3D từ html", "art manifest", GD vừa gửi html mới cần asset. KHÔNG dùng để sinh VFX,
  shader, simulation hay mesh procedural theo level.
---

# SKILL: art-gen

Pipeline **HTML (GD) → art manifest → gen → validate → Unity**. Mỗi khâu làm đúng việc nó giỏi:

| Khâu | Ai làm | Không giao cho |
|---|---|---|
| Đọc HTML, tách danh sách element | Frontier model (skill này) | GD viết JSON tay |
| Duyệt manifest + style | GD / Dev | Model tự chốt |
| Sinh hình 2D | Image model (GPT web…) theo batch ≤ 10 | — |
| Tên file, kích thước, trim, pack atlas | Script / Unity | **Image model** |
| Dựng mesh 3D tĩnh | three.js trong HTML → `.glb` | Unity tự gen lại mesh từ data |
| Validate + FBX 3D | `skills/asset-intake/` (Blender, D-005) | glTFast song song |

Mục tiêu cuối: **asset contract ổn định** (id, kích thước, pivot, 9-slice, material slot) để senior art
reskin bằng cách **thay file 1:1**, không sửa code/prefab.

---

## Bước đầu tiên — đọc context

`Docs/project-context.md` (prefix tên, `[GameRoot]`, Product input) ·
`standards/folder-structure.md §2, §5` · `standards/performance-budget.md` (texture, tri, draw call) ·
Authority Matrix của prototype (`workflow/prototype-to-contract.md §2`) — area **Visual** phải là
`REFERENCE`, không phải `AUTHORITATIVE`: HTML là intent, không phải final art.

**GD không phải đổi cách vibe HTML.** Mọi chuẩn hoá phục vụ dev/art do model làm ở Phase −1,
trên bản dẫn xuất. Thứ duy nhất xin GD: **ảnh style reference** + trả lời `manifest-review.md`.

---

## Phân loại element — `kind`

| `kind` | Ví dụ | Đi đâu |
|---|---|---|
| `ui_2d` | button, panel, icon, star, popup frame | Gen 2D → `refs/gen-2d.md` |
| `sprite_2d` | nhân vật tĩnh, item, decor 2D | Gen 2D |
| `modular_2d` | block polyomino, sàn/tường theo lưới | Gen **mảnh** (tileset) → Unity lắp |
| `prop_3d` | van, đèn, đá, đồ trang trí | three.js → `.glb` → asset-intake → `refs/export-3d.md` |
| `modular_3d` | ống dài/ngắn, cột cao/thấp | Export **mảnh** cap/body → Unity lắp |
| `procedural` | cốc có bán kính trên/dưới đổi theo level, mesh deform | **OUT** → `skills/technical-slice/` |
| `vfx` / `shader` / `sim` | hạt nước, kính trong suốt, confetti | **OUT** → `technical-slice/` / `game-feel-motion/` |
| `dev_ui` | tab chọn chế độ, nút Editor, "Xoá tiến trình" | Bỏ qua |

**Luật modular (đã chốt):** element có shape/size do level data quyết định → gen mảnh, Unity lắp
theo data. Chỉ hợp lệ khi biến thiên là **(a) số cell trên lưới** hoặc **(b) độ dài theo một trục**
với phần thân thẳng (stretch/repeat không méo). Biến thiên khác (taper, bán kính, cong) → `procedural`,
hỏi dev, không cố ép vào pipeline art.

Có yếu tố `rig_later` (sẽ gắn xương) → vẫn export `.glb` nhưng ghi rõ **chỉ là reference hình khối**;
topology ghép primitive không skin được. Bên rig có thể retopo/dựng lại.

---

## Quy trình

### Phase −1 — Normalize (model tự làm) · HARD GATE

Chi tiết: `refs/normalize.md`.

1. Copy HTML gốc vào `art/src/` (read-only). **Không sửa file của GD.**
2. Tạo `<name>.art.html`: append `?frame=phone`, `?export=art`, đánh `data-art`; tách `buildXxx()` cho 3D
   nếu cần. Trích palette/FEEL/level-driven vào `art-extract.json` bằng cách **đọc**, không refactor.
3. Parity gate: `art/tools/parity.py` so bản gốc với `.art.html`. FAIL → `BLOCKED`, không sang Phase 0.
4. GD gửi bản mới (hash khác) → normalize lại từ bản gốc mới, không vá bản cũ.

### Phase 0 — Extract manifest (model làm, GD duyệt) · HARD GATE

1. Đọc `.art.html` + `art-extract.json`: DOM (`data-art`), palette, hàm vẽ canvas, `buildXxx()`, level data.
2. Viết `art/art-manifest.json` theo `refs/art-manifest.md`.
3. Viết `art/manifest-review.md` cho GD: bảng id · kind · mô tả · size · ghi chú; mục **OUT** và
   **thiếu thông tin**. Hỏi GD theo `workflow/ask-and-visualise.md` (trắc nghiệm, một recommend).
4. Manifest `status: LOCKED` mới được sang Phase 1. Chưa lock = `BLOCKED`, không gen.

Không bịa kích thước/budget: `size_px` suy từ layout HTML ở khung 1080×1920; `tri_max` lấy từ
`performance-budget.md`, chưa có → hỏi dev.

### Phase 1 — Style lock (2D và 3D dùng chung palette)

1. Chụp HTML ở `?frame=phone` (viewport 360×640 CSS, deviceScaleFactor 3 = 1080×1920; Playwright
   headless) → `art/ref/layout-<screen>.png`. Mỗi màn/state cần art (menu, play, win) một ảnh.
2. Image model gen 1–2 mockup full màn **vẽ đè lên layout đó** theo ảnh style ref GD đưa.
3. GD chọn → `art/style/style-anchor.png` + `art/style/style-block.txt` (đoạn prompt cố định,
   dán nguyên văn vào mọi batch). Palette chốt ghi vào `manifest.style.palette`.
4. Chưa có ảnh style ref → hỏi GD, không tự chọn art style.

### Phase 2 — Gen

- 2D: `refs/gen-2d.md` — batch ≤ 10, cùng `group`, mỗi element một ảnh, nền trong suốt, zip theo batch.
- 3D: `refs/export-3d.md` — nút/URL export trong HTML, mỗi `prop_3d` / part một `.glb`.

### Phase 3 — Intake (deterministic, không phải model)

- 2D: script validate coverage theo manifest → trim/resize/pad → `Assets/_Core/0_Texture2D/<Group>/`
  + sidecar `.import.json` (pivot, border 9-slice, PPU). SpriteAtlas do Unity pack
  (`unity:manage-sprite-atlas`, prebuild) — **không** để image model ghép atlas.
- 3D: viết `asset-brief.json` cho từng `.glb` → chạy `skills/asset-intake/`.

### Phase 4 — Review

`contact-sheet.png` (script ghép toàn bộ sprite + render 3D) + screenshot Unity màn 1080×1920
đặt cạnh mockup đã chốt. **Validate pass ≠ đẹp** — GD/Dev duyệt bằng mắt. Không báo "xong" khi chưa ai nhìn.

---

## Output layout

```text
art/
  src/                     <name>.html (gốc GD, read-only) · <name>.art.html · art-extract.json
  tools/parity.py
  art-manifest.json        source of truth: id, kind, size, pivot, slice, material
  manifest-review.md       cho GD
  ref/                     screenshot khung 1080×1920 từ HTML
  style/                   style-anchor.png, style-block.txt, ảnh ref GD đưa
  gen/2d/batch-01.zip…     output thô từ image model (giữ nguyên, không sửa)
  gen/3d/<id>.glb          export từ HTML
  review/contact-sheet.png
```

`art/` nằm ngoài `Assets/` — chỉ file đã qua intake mới vào `Assets/`.

---

## Không làm gì

- Không bảo image model ghép atlas, giữ grid/pixel chính xác, hay đặt tên file cuối cùng mà không validate.
- Không cho Unity tự gen lại mesh từ data của HTML cho `prop_3d` (hai implementation sẽ lệch).
- Không convert material/shader three.js sang URP — chỉ truyền `material_id`, dev map sang material có sẵn.
- Không gen `procedural` / `vfx` / `shader` / `sim` / `dev_ui`.
- Không sửa file thô trong `art/gen/` — output mới là file mới.
- Không tự chọn art style, tự nới budget, hay tự đổi kind của element GD đã duyệt.

---

## Refs

- `refs/normalize.md` — Phase −1: bản dẫn xuất, snippet frame/freeze, parity gate
- `refs/art-manifest.md` — schema manifest, quy ước id, ví dụ từ 2 prototype mẫu
- `refs/gen-2d.md` — style lock, prompt batch, post-process, Unity intake 2D, tileset modular
- `refs/export-3d.md` — contract export three.js → `.glb`, modular part, handoff asset-intake
