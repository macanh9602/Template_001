# P1 — Bootstrap project

**Vào khi:** repo trống hoặc mới có GDD.
**Ra khi:** có `Docs/` điền đủ, folder structure dựng xong, một scene chạy được với Bootstrap →
Profile → một element hiển thị từ data.

Mục tiêu không phải "có gameplay". Mục tiêu là **mọi story sau không phải hỏi lại những câu đã hỏi
ở đây**.

---

## Bước 1 — Đọc GDD, hỏi cho đủ (enrich-context)

Skill: `enrich-context`.

Hỏi theo batch trắc nghiệm. Nhóm câu hỏi tối thiểu:

| Nhóm | Cần chốt |
|---|---|
| Thể loại & vòng lặp | người chơi làm gì trong 30 giây đầu; thắng/thua là gì |
| Quy mô | bao nhiêu level; level do người làm hay sinh tự động |
| Element | các loại đối tượng chính; số lượng tối đa cùng lúc trên màn |
| Input | tap / drag / swipe / multi-touch; có gì làm cùng lúc không |
| Thiết bị chuẩn | máy nào là mốc để đo performance |
| Orientation & tỉ lệ | portrait/landscape; dải aspect ratio phải chịu |
| Meta | có progression / shop / booster không (chỉ cần biết **có**, chưa cần chi tiết) |
| Ràng buộc | deadline, team size, thứ bắt buộc dùng lại từ project cũ |

**Không** hỏi những gì GDD đã trả lời. Trả lời rồi ⇒ ghi vào `Docs/project-context.md §7`
("câu hỏi đã trả lời — không hỏi lại").

## Bước 2 — Điền `Docs/`

Thứ tự điền, mỗi file confirm với dev trước khi sang file sau:

1. `Docs/project-context.md` — stack, thiết bị chuẩn, ràng buộc, fact đã chốt (đánh dấu 🔒 cho
   contract toàn project).
2. `Docs/glossary.md` — tên các element và khái niệm chính. Làm **sớm**; đổi tên sau khi đã có 50
   file code là đắt.
3. `Docs/data-model.md` — cái gì là source of truth, cái gì generated, level data trông thế nào.
4. `Docs/runtime-architecture.md` — instance hoá `standards/system-design.md` cho game này: layer
   nào có gì, profile nào giữ số gì.
5. `Docs/decision-log.md` — mở file, ghi `D-001` cho quyết định đầu tiên đã chốt ở Bước 1.

## Bước 3 — Dựng folder + assembly

Theo `standards/folder-structure.md`.

- [ ] Cây thư mục dựng đủ (kể cả thư mục còn trống)
- [ ] Quyết định asmdef: chia assembly hay để `Assembly-CSharp` — **chốt ngay bây giờ**, ghi `D-xxx`.
      Đổi sau tốn hơn nhiều lần.
- [ ] Namespace mirror theo thư mục
- [ ] `.gitignore`, `.editorconfig`
- [ ] Test assembly dựng sẵn (kể cả chưa có test)

## Bước 4 — Dựng bộ Profile

Theo `standards/system-design.md §4`. Tạo **đủ 6 profile** kể cả khi còn rỗng — có sẵn chỗ thì
không ai hardcode:

| Profile | Giữ gì |
|---|---|
| `PrefabProfile` | prefab + shared material, tra theo id |
| `TimingProfile` | mọi delay/duration của **gameplay** |
| `MotionProfile` | duration/easing/overshoot của **visual** |
| `LayoutProfile` | khoảng cách, kích thước, safe area |
| `AudioProfile` | cue |
| `ColorProfile` | bảng màu theo state |

Truy cập qua `ResourceAsset<T>` (ngoại lệ static hợp lệ duy nhất — `system-design.md §1`).

## Bước 5 — Vertical slice tối thiểu

Một scene chạy được:

```
Bootstrap → đọc Profile → Spawner → Factory tạo 1 element từ data → Domain giữ state
          → Visual hiển thị → HUD hiện 1 giá trị từ Domain
```

Chưa cần gameplay. Cần chứng minh **đường dây layer thông suốt** và mọi số đến từ data/profile.

Checklist:

- [ ] Element hiện lên từ **level data**, không phải đặt tay trong scene
- [ ] Đổi một số trong Profile → thấy thay đổi trong game, không sửa code
- [ ] Không `CreatePrimitive`, không `new Material`, không `Shader.Find`
- [ ] Có ObjectPool (`VTLTools`) cho loại element sẽ sinh nhiều
- [ ] Text đi qua prefab TMP có script quản lý (`Init`/`Show`/`Hide`)
- [ ] Load → unload → load lại level: không rò event, không rò object (kiểm bằng số lượng instance)

## Bước 6 — Dựng `handoff/`

- [ ] `handoff/ROADMAP.md` — danh sách story dự kiến, sizing S/M/L
- [ ] Story 001 viết xong theo `templates/story.md`
- [ ] `handoff/START-PROMPT.md` / `handoff/RUN-STORY-PROMPT.md` đã trỏ đúng tên game

---

## Xong khi

- [ ] `Docs/` 5 file điền xong, dev đã confirm
- [ ] Vertical slice chạy trên **thiết bị thật**, không chỉ Editor
- [ ] Load/unload sạch
- [ ] Roadmap + story 001 sẵn sàng
- [ ] Không còn open question dạng blocker

## Bẫy

| Bẫy | Hậu quả |
|---|---|
| bỏ qua glossary "để sau" | đổi tên khi đã có nhiều code, hoặc sống chung với tên sai |
| chưa chốt asmdef | phát hiện editor code không tách được khi đã muộn |
| slice có gameplay nhưng không đi qua đủ layer | phát hiện đường dây gãy ở story thứ 5 |
| hardcode "tạm" trong slice | "tạm" sống tới lúc ship |
| chỉ test trong Editor | số performance sai, thứ tự init khác |
| tạo profile khi cần | mỗi story đẻ một chỗ chứa số mới |
