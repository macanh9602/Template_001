# Playbooks — chạy giai đoạn nào

> **Skill** = cách làm một việc. **Playbook** = trình tự làm cả một giai đoạn.
> Playbook nói: làm gì trước, dùng skill nào, ra artifact gì, khi nào coi là xong.

## Chọn playbook

| Tình huống hiện tại | Playbook |
|---|---|
| repo trống / mới có GDD, chưa biết bắt đầu từ đâu | `p1-bootstrap.md` |
| có rủi ro kỹ thuật chưa biết làm nổi không | `p2-technical-slice.md` |
| cần chơi được vòng cơ bản từ đầu tới cuối | `p3-core-loop.md` |
| GD cần tự tạo/sửa level | `p4-level-editor.md` |
| cần kiểm soát độ khó, sinh level | `p5-difficulty.md` |
| chạy đúng rồi nhưng chưa đã tay | `p6-feel-pass.md` |
| chuẩn bị build thật / lên store | `p7-ship.md` |

## Thứ tự điển hình

```
p1 bootstrap
   ↓
p2 technical slice   ← chỉ khi có rủi ro lớn; bỏ qua nếu không
   ↓
p3 core loop         ← mốc "chơi được"
   ↓
p4 level editor      ← khi cần nhiều level / GD tham gia
   ↓
p5 difficulty        ← cần p4 trước, vì phải có level để đo
   ↓
p6 feel pass         ← sau khi logic đứng, trước khi ship
   ↓
p7 ship
```

p4 và p5 có thể chạy song song một phần, nhưng **không sinh level trước khi đo được độ khó**
(`skills/difficulty-design/`).

## Luật chung cho mọi playbook

1. Mỗi bước ra **artifact cụ thể**, không phải "đã làm xong".
2. Bước nào có quyết định ⇒ **trắc nghiệm + recommend**, có visualiser nếu liên quan không gian /
   chuyển động / timing / phân bố số.
3. Kết thúc mỗi playbook ⇒ chạy `workflow/harvest.md`, đẩy cái generic về `knowledge/` hoặc skill.
4. Trạng thái báo cáo theo `workflow/verification.md` — không ghi DONE cho thứ chưa chạy thật.
5. Số liệu luôn đi vào prefab field / Profile SO / level data — không hardcode (`AGENTS.md §5`).
