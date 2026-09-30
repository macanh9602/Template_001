# Visual rubric — khung

Rubric **cố định trong một checkpoint**: không thêm/bớt item giữa loop. Mỗi item PASS / FAIL kèm evidence.
⚙ = đo được bằng máy (script hoặc số trong profile); còn lại chấm bằng mắt trên capture.

Project copy bảng này vào `handoff/visual/rubric.md`, thay tên object theo glossary và điền ngưỡng.
Cột "Ví dụ Block Home" là ngưỡng đã dùng thật ở Ducan_SpeedRun_Demo (WP004), chỉ để tham khảo.

| # | Lớp | Item | Cách chấm | Ví dụ Block Home |
|---|---|---|---|---|
| R1 | 1 Composition | Board/play area trong safe area, không bị HUD che | ⚙ bounds trên màn | trái/phải ≥ 4% width; board ≥ 80% width |
| R2 | 1 Composition | Camera khớp direction | ⚙ field camera vs `look.values` | tilt/fov đúng |
| R3 | 2 Shape | Object nhiều ô đọc thành **một** vật | mắt, ảnh squint | không thấy ô rời ở squint |
| R4 | 2 Shape | Đích đến đọc được, hướng vào rõ | mắt | nhận ra cửa < 1 s ở mọi level |
| R5 | 2 Shape | Nhân vật/đơn vị chính đọc được < 1 s | ⚙ bề ngang trên màn + mắt | ≥ 0.36 ô; silhouette khác block |
| R6 | 3 Color | Object phụ không tranh hue với cặp chính | ⚙ HSL từ profile | sat ≥ 0.28 thì hue cách ≥ 28° |
| R7 | 3 Color | Value hierarchy: gameplay object > nền > background | ảnh greyscale | nền không sáng/đậm hơn object |
| R8 | 3 Color | Nền không cạnh tranh | ảnh squint | grid/seam không rõ hơn cạnh object |
| R9 | 3 Color | Object nổi trên vùng cùng màu | ⚙ contrast | ≥ 1.4 hoặc có outline |
| R10 | 4 Depth | Độ cao/lõm đọc được, có contact shadow | mắt | đọc được khi tắt real-time shadow |
| R11 | 4 Depth | Không z-fighting, shadow acne, răng cưa rõ | mắt, zoom 200% | không có |
| R12 | 5 Motion | Immediate cue ở frame đầu khi chạm | ⚙ frame 1 sau input có đổi | có |
| R13 | 5 Motion | Curve khớp direction | ⚙ `MotionParity` | total ±10%, peak ±15%, tPeak ±0.03 s |
| R14 | 5 Motion | Không nhảy loạn khi tap nhanh / reuse pool | test rapid input + reload | 0 transform sai sau 20 thao tác nhanh |
| R15 | 6 VFX/UI | VFX qua hạ tầng effect, không che gameplay | ⚙ grep `Instantiate`/`.Play()` + mắt | 0 vi phạm; không che > 0.15 s |
| R16 | 6 VFX/UI | HUD cùng ngôn ngữ visual với board | mắt | màu/bo góc/weight chữ thuộc direction |
| R17 | Toàn cục | Consistency nhiều level | capture idle mọi level canonical | mọi item PASS ở mọi level |
| R18 | Toàn cục | Performance | ⚙ Frame Debugger / stats | draw call, tri, overdraw trong budget |

## Blocker vs non-blocker

- Project tự khai trong bản copy. Mặc định: R11, R16 non-blocker; còn lại blocker.
- R17 chỉ chấm ở vòng cuối checkpoint (tốn capture); vòng giữa dùng `referenceLevel`.

## Chấm ảnh thế nào

- So **current vs reference cùng pose**, cạnh nhau, cùng kích thước. Không pixel-diff (URP ≠ HTML).
- Luôn dùng thêm `.grey.png` và `.squint.png` (capture tự sinh). R7, R8 chấm trên hai bản này.
- Mỗi FAIL chỉ vùng cụ thể ("góc trên phải, block 1×3 cạnh room xanh"), không chung chung.
