# Capture protocol

Mục tiêu: hai capture ở hai vòng khác nhau **chỉ khác nhau do patch**, không do camera, thời điểm, random hay kích thước cửa sổ.

## Cố định

| Thứ | Giá trị |
|---|---|
| Resolution | 1080×1920 mặc định (`VisualCapture.CaptureSet(..., width, height)`), render qua RenderTexture, không chụp Game view |
| Level | `referenceLevel` của direction (vòng giữa); mọi level canonical (vòng cuối checkpoint) |
| Camera | camera gameplay thật, không camera debug |
| Time | pose dựng bằng fixture tất định, không chụp theo "chờ 0.5 s" |
| Random | seed cố định cho mọi thứ có random (particle: `useAutoRandomSeed = false` khi capture) |
| Post-processing | như build thật |
| HUD | bật (canvas overlay được runner chuyển tạm sang ScreenSpaceCamera) |

## Pose

Project khai danh sách pose trong `handoff/visual/rubric.md`. Gợi ý:

| Pose id | Trạng thái | Dùng cho |
|---|---|---|
| `idle` | level vừa load xong, không input | R1–R11, R16, R17 |
| `dragging` | object đang cầm, giữa đường | R3, R10, R12 |
| `invalid` | frame phản hồi sai lớn nhất | R12, R15 |
| `resolve-mid` | đơn vị đầu wave giữa chuyển động | R5, R9, R14 |
| `win` | đỉnh hiệu ứng thắng | R15, R16 |

Object dùng cho pose chọn **cùng quy tắc như lab** (vd. drag = object đi được xa nhất), để so được với reference.

## Phần Template đã có (Editor, `AgentPack.VisualDirection`)

| Thứ | Việc |
|---|---|
| `VisualCapture.CaptureSet(levels, poses, outDir, prefix)` | tự vào Play Mode (sống qua domain reload), gọi scenario, chụp, thoát Play Mode |
| `VisualCapture.CaptureCamera(camera, path, w, h)` | PNG + `.grey.png` (luminance Rec.709) + `.squint.png` (box blur 3 lần) |
| menu `Tools/Visual Direction/Capture reference level (idle)` | chụp `referenceLevel` pose idle |
| log | dòng đầu `[VisualCapture] DONE n anh` (Log) hoặc `[VisualCapture] FAIL ...` (LogWarning) |

## Phần project phải viết (một lần, đầu visual pass)

```csharp
// Editor assembly. Đúng MỘT class, constructor không tham số.
public sealed class MyGameCaptureScenario : AgentPack.VisualDirection.IVisualCaptureScenario
{
    public async UniTask WaitReadyAsync(CancellationToken ct) { /* chờ scene gameplay sẵn sàng theo state, không delay cố định */ }
    public async UniTask<Camera> PreparePoseAsync(string levelId, string poseId, CancellationToken ct)
    {
        /* load level qua con đường gameplay thật → đưa presentation tới pose (tween Goto(t), fixture) → trả camera gameplay */
    }
}

// Mỗi demo motion của lab một class; tên channel = key trong sections.motion.metrics.
public sealed class DragParityDemo : AgentPack.VisualDirection.IMotionParityDemo { ... }
```

- Chỉ chạm Presentation. Không đổi domain state ngoài con đường gameplay thật.
- Code capture/parity nằm trong Editor assembly, runtime không phụ thuộc vào nó.
- Sampler motion phải gọi **chính hàm curve runtime dùng** (không viết lại công thức), nếu không parity vô nghĩa.

## Đặt tên file

`handoff/visual/captures/<cp>-<iter 2 số>-<level>-<pose>.png`, vd `A-03-level_002-idle.png`
(prefix = `<cp>-<iter>`). Bản `.grey.png` / `.squint.png` sinh kèm.
