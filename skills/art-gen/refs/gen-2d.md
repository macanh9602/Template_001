# Gen 2D — style lock → batch → intake

## 1. Style lock

Đầu vào: `art/ref/layout-<screen>.png` (HTML ở `?frame=phone`, 1080×1920) + ảnh style ref của GD.

Prompt mockup (GPT web / image model):

```text
Đây là layout màn hình game mobile 1080×1920 (ảnh 1) và art style mục tiêu (ảnh 2..n).
Vẽ lại màn hình này theo đúng art style đó. GIỮ NGUYÊN vị trí, kích thước, số lượng element
của layout. Không thêm element mới. Không vẽ chữ trừ tiêu đề game.
Output: 1 ảnh 1080×1920.
```

GD chọn 1 mockup → `style-anchor.png`. Sau đó viết `style-block.txt` (≤ 80 từ, cố định, dán vào
mọi batch): chất liệu, viền, bóng, ánh sáng, độ bão hoà, palette hex từ `manifest.style.palette`.

## 2. Batch gen (≤ 10 ảnh / batch)

- Một batch = một `group` (ui / icon / board…). Không trộn — style drift ít hơn.
- Luôn đính kèm `style-anchor.png` + dán `style-block.txt` nguyên văn.
- Mỗi element **một ảnh**, không sprite sheet, không atlas.

Prompt batch:

```text
[style-block.txt]

Dựa vào ảnh style anchor, gen từng element dưới đây, MỖI ELEMENT MỘT ẢNH RIÊNG:
- Nền trong suốt (PNG alpha). Nếu không hỗ trợ: nền phẳng #FF00FF, không gradient.
- Element nằm giữa, chừa lề ~8% mỗi cạnh, không cắt mép.
- Không đổ bóng ra nền, không chữ (trừ khi ghi "có chữ"), không viền trắng quanh.
- Góc nhìn thẳng (UI) / top-down (board) như anchor.

1. bh_ui_btn_primary_normal — nút chính bo góc, tỉ lệ 5:2, màu accent #f2b632
2. bh_ui_btn_primary_pressed — như 1, tối hơn 10%, đáy mỏng hơn (đã nhấn)
...
10. …

Cuối cùng: đóng gói toàn bộ ảnh vào batch-03.zip, tên file = id + ".png".
```

Tên trong zip **chỉ là gợi ý** — script vẫn đối chiếu manifest và báo thiếu/thừa. Batch lỗi style →
gen lại **cả batch** với anchor, không sửa từng ảnh bằng prompt nối tiếp (drift tích luỹ).

## 3. Modular 2D (tileset)

Element theo lưới (block polyomino, sàn, tường) → gen mảnh, Unity lắp theo level data.

| `tileset` | Số mảnh | Khi nào |
|---|---|---|
| `nine_slice` | 1 ảnh + border | hình chữ nhật co giãn (panel, sàn phẳng) |
| `mask16` | 16 (mask 4 cạnh N/E/S/W) + 1 inner-corner | polyomino bo góc như Block Home |
| `blob47` | 47 | cần góc trong/ngoài chi tiết — tốn gen, chỉ khi `mask16` không đủ |

Cách gen `mask16` ổn định hơn: gen **1 ảnh tile mẫu lớn** (block 3×3 đầy đủ) theo style → script cắt
thành các mảnh theo mask, thay vì bảo model vẽ 16 ảnh rời (16 ảnh rời gần như chắc chắn lệch viền).
Mask bit: N=1, E=2, S=4, W=8 → id `_m00`…`_m15`.

## 4. Intake 2D (script — tool chưa có, là story riêng)

Input: `art/gen/2d/*.zip` + manifest. Deterministic, không gọi model.

1. Giải nén → map file theo `id`; báo `missing` / `unexpected`.
2. Chroma `#FF00FF` → alpha nếu cần; khử halo viền (defringe 1–2px).
3. Trim alpha → scale về `size_px` (giữ tỉ lệ, fit) → pad về đúng `size_px`.
4. Validate: có alpha, kích thước, không pixel đặc chạm mép (bị cắt), border 9-slice < nửa cạnh.
5. Ghi `Assets/_Core/0_Texture2D/<Group>/<id>.png` + `<id>.png.import.json`
   (`pivot`, `nine_slice`, `ppu`, `atlas_group`) → Postprocessor Unity set import settings
   (giống pattern `AssetIntakePostprocessor`), không sửa `.meta` tay.
6. Atlas: mỗi `atlas_group` một SpriteAtlas V2, pack prebuild (`unity:manage-sprite-atlas`).
   Texture ASTC, ≤ 2048 theo `performance-budget.md`.
7. Xuất `art/review/contact-sheet.png` (ghép toàn bộ sprite có nhãn id) cho GD duyệt.

"File lớn gộp chung" mà ý tưởng ban đầu muốn = SpriteAtlas (runtime) + contact-sheet (review).
Không ai phải cắt atlas bằng tay, reskin chỉ thay PNG lẻ.
