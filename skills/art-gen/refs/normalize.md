# Phase −1 — Normalize HTML của GD (model tự làm, GD không phải đổi cách vibe)

> GD cứ vibe như bình thường. Trước khi extract manifest, model tạo **bản dẫn xuất** phục vụ dev/art.
> Thứ duy nhất xin GD: **ảnh style reference** (2–5 ảnh). Không có → hỏi, không tự chọn style.

## 3 nguyên tắc

1. **Không sửa file gốc của GD.** Output luôn là file mới:
   ```text
   art/src/<name>.html            bản gốc GD (copy nguyên, read-only)
   art/src/<name>.art.html        bản dẫn xuất do model tạo
   art/src/art-extract.json       palette + FEEL + danh sách builder trích ra
   ```
2. **Regenerate, không vá.** GD gửi bản mới → hash khác `source_html_hash` trong manifest →
   normalize lại **từ bản gốc mới**, không vá tay lên `.art.html` cũ. Manifest cũ chuyển review lại.
3. **Parity gate.** `.art.html` phải render giống bản gốc (mục 3). Fail → normalize sai, dừng,
   không extract manifest.

## 1. Checklist normalize

Ưu tiên **thêm lớp** (append script/style) hơn **refactor**. Chỉ refactor khi không thể trích bằng cách khác.

| # | Việc | Cách làm | Rủi ro |
|---|---|---|---|
| 1 | Chế độ khung điện thoại | Append snippet `?frame=phone` (mục 2) | Không |
| 2 | Chế độ đóng băng để chụp | Append snippet `?freeze=1` (mục 3) | Không |
| 3 | Palette | **Đọc** CSS var + hex trong code → `art-extract.json.palette` (key đặt tên theo vai trò: `wood`, `accent`…). Không thay hex trong code | Không |
| 4 | FEEL | **Đọc** keyframes, `cubic-bezier`, duration, hằng số tween/lerp (`k=.35`, `SPEED=9`…) → `art-extract.json.feel`, ghi kèm vị trí trong file gốc. Dùng cho `game-feel-motion`, không đổi code | Không |
| 5 | Ship vs dev UI | **Đoán** và đánh `data-art="ship"/"dev"` trong `.art.html`; mọi phán đoán đưa vào `manifest-review.md` để GD xác nhận | Thấp |
| 6 | 3D builder | Tách phần dựng hình thành `buildXxx()` dựng ở gốc toạ độ, `material.name` = key palette; code game gọi lại builder đó. **Chỉ bước này là refactor** | Cao — parity gate bắt buộc |
| 7 | Nút export | Append `exportArt()` khi `?export=art` (`export-3d.md §3`) | Không |
| 8 | Ghi chú biến thiên theo level | Đọc level data → element nào đổi size/shape theo level → ghi vào `art-extract.json.level_driven` | Không |

Không làm: đổi gameplay, đổi layout, "sửa đẹp" màu/font, xoá dev UI, nâng version thư viện.

## 2. Snippet `?frame=phone`

Điện thoại 1080×1920 hiển thị web ở 360×640 CSS px (mật độ ×3). Chụp viewport 360×640,
`deviceScaleFactor: 3` → ảnh 1080×1920.

```html
<script>
if (new URLSearchParams(location.search).get('frame') === 'phone') {
  const s = document.createElement('style');
  s.textContent = `html{background:#222}
    body{width:360px!important;height:640px!important;margin:24px auto!important;
         overflow:hidden!important;position:relative;transform:translateZ(0)}`;
  document.head.appendChild(s);
  dispatchEvent(new Event('resize'));
}
</script>
```

`transform` trên body biến body thành containing block cho `position:fixed` → các `.screen{position:fixed}`
nằm gọn trong khung. Đã verify trên Block Home và Pour Path (ra đúng 1080×1920).
Frame mode chỉ để xem trên desktop / chụp layout — **không** dùng khi chạy parity (xem §3).

## 3. Parity gate

Nguồn không-tất-định trong prototype: `Math.random` (confetti), animation theo thời gian (blink, bob),
`requestAnimationFrame`. Chụp ở trạng thái đóng băng, **inject trước khi page chạy** vào cả hai bản:

```python
# art/tools/parity.py — python -m pip install playwright pillow
import sys, os
from playwright.sync_api import sync_playwright
from PIL import Image, ImageChops

FREEZE = """
(() => { let s = 1; Math.random = () => (s = (s * 16807) % 2147483647) / 2147483647;
  const T = 1000; performance.now = () => T; Date.now = () => T;
  const raf = window.requestAnimationFrame; let n = 0;
  window.requestAnimationFrame = cb => n++ < 30 ? raf(() => cb(T)) : 0; })();
"""

def shot(page, path, out, query):
    # KHÔNG dùng ?frame=phone ở đây: viewport đã là 360×640, và transform của frame mode
    # đổi cách khử răng cưa chữ → lệch giả. Frame mode chỉ để xem trên desktop / chụp layout.
    page.goto(f"file://{os.path.abspath(path)}?_{query}")
    page.wait_for_timeout(2500)
    # đóng băng CSS animation/transition về frame 0 (JS time đã freeze bằng init script)
    page.evaluate("document.getAnimations().forEach(a => { a.pause(); a.currentTime = 0; })")
    page.locator("body").screenshot(path=out, animations="disabled")

orig, art, states = sys.argv[1], sys.argv[2], sys.argv[3:] or [""]
with sync_playwright() as pw:
    b = pw.chromium.launch()
    p = b.new_page(viewport={"width": 360, "height": 640}, device_scale_factor=3)
    p.add_init_script(FREEZE)
    worst = 0
    for i, q in enumerate(states):          # q ví dụ "&state=play" nếu prototype hỗ trợ
        shot(p, orig, f"parity_{i}_orig.png", q); shot(p, art, f"parity_{i}_art.png", q)
        a = Image.open(f"parity_{i}_orig.png").convert("RGB"); c = Image.open(f"parity_{i}_art.png").convert("RGB")
        d = ImageChops.difference(a, c)                      # lệch theo kênh lớn nhất, không theo độ sáng
        mx = ImageChops.lighter(ImageChops.lighter(*d.split()[:2]), d.split()[2])
        diff = mx.point(lambda v: 255 if v > 16 else 0)
        diff.save(f"parity_{i}_diff.png")
        ratio = diff.histogram()[255] / (a.width * a.height)
        worst = max(worst, ratio); print(f"state {i}: {ratio:.4%} pixel lệch")
    b.close()
sys.exit(0 if worst <= 0.002 else 1)       # > 0.2% → FAIL
```

- Chụp đủ các màn có art (menu, play, win). Prototype không có cách vào thẳng một màn → append
  hook `?state=` trong `.art.html` **và** chụp bản gốc bằng cách click tương ứng; ghi cách vào màn
  trong `art-extract.json.states`.
- 3D (WebGL) có thể lệch vài pixel khử răng cưa → ngưỡng 0.2% là đủ; lệch hơn là builder refactor sai.
- Đã verify trên Block Home: bản gốc vs bản có snippet → 0.00%; đổi màu chữ phụ 20 đơn vị R → 1.33% (bắt được);
  đổi màu nền → 61% (bắt được). Pour Path chưa verify phần 3D (môi trường test không tải được three.js CDN).
- Kết quả + ảnh diff lưu `art/review/parity/`. Parity FAIL = `BLOCKED`, không sang Phase 0.

## 4. Đưa GD cái gì

Chỉ `manifest-review.md` (Phase 0): danh sách element, các phán đoán ship/dev, element OUT,
câu hỏi còn mở — và yêu cầu ảnh style ref nếu chưa có. GD không cần mở `.art.html`.
