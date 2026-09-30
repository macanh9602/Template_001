# Reuse registry — [TÊN GAME]

> Hệ thống **đã có** mà agent phải dùng lại, và mẫu code "viết lại" cần bắt.
> Dữ liệu máy đọc: `Docs/reuse-registry.json`. File này giải thích cách dùng.

## Vì sao

Pilot WP004: worker tự viết 4 `AudioSource` pooled thay vì dùng `AudioController` có sẵn. Reviewer không bắt được,
phải sửa muộn, và implementation notes vẫn ghi sai. Registry biến "nhớ dùng lại" thành **kiểm tra bằng script**.

## Ai dùng

| Nơi | Làm gì |
|---|---|
| `tools/template-lint.ps1` | rule `REUSE_BYPASS` (WARN): quét cả `scan.include`, liệt kê dòng khớp `avoid` |
| `tools/run-task.ps1` | sau mỗi round implementer: quét **dòng mới thêm** trong diff → `r<n>.reuse.md` → đưa cho reviewer (chứng cứ, 0 token) |
| task `mustReuse` | liệt kê hệ thống task này bắt buộc dùng (implementer + reviewer đều thấy) |
| decision D-table / packet | trước khi chốt "tự viết X", tra registry |

## Thêm một hệ thống

```json
{
  "name": "AudioController",
  "path": "Assets/_Core/4_Scripts/System/Audio/AudioController.cs",
  "useFor": "mọi SFX/music",
  "avoid": [
    { "pattern": "new\\s+AudioSource|AddComponent<AudioSource>", "why": "SFX qua AudioController.PlaySound" }
  ],
  "allowIn": ["**/Audio/**"]
}
```

- `pattern`: regex .NET, so trên từng dòng code (bỏ comment `//`).
- `allowIn`: glob nơi được phép (chính hệ thống đó, UI panel...). Đặt ở mức system hoặc từng `avoid`.
- Một hit là **tín hiệu**, không phải lỗi chắc chắn: implementer có lý do thì ghi vào result, reviewer quyết.
- `scan.exclude` mặc định bỏ `Editor/` và `Tests/` (tool Editor được dùng `Instantiate`, `new Material`...).

## Mặc định của pack

Theo `AGENTS.md` §5 / §9: `VTLTools.ObjectPool` · PrefabProfile (không `CreatePrimitive` / `new Material` / `Shader.Find`) ·
UniTask (không coroutine mới) · tham chiếu scene qua inject/SerializeField (không `Find*`). Project thêm hệ thống của mình.
