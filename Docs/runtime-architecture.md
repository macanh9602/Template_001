# Runtime Architecture — [TÊN GAME]

> Đây là **instance** của `standards/system-design.md` cho game này.
> Không lặp lại nội dung blueprint. Chỉ map layer → class thật + ghi cái gì đặc thù.
> Giống hệt blueprint ở mục nào thì ghi "theo blueprint §x".

---

## 1. Layer map

| Layer (blueprint §1) | Class / asset thật trong game này |
|---|---|
| Bootstrap | |
| Profile | |
| Level Data | |
| Save / Progress | |
| Spawner | |
| Factory | |
| Domain | |
| RuntimeState | |
| Scheduler | |
| Visual | |
| Bridge | |
| HUD | |
| Editor | |

## 2. Placement contract

> Toạ độ · plane · depth · pivot của từng root. Viết rõ — đây là nguồn bug lặp lại nhiều nhất.

- Mặt phẳng gameplay:
- Depth / trục chồng lớp:
- Root nào có pivot ở đâu:
- Ai được phép ghi Camera:
- Scale: cái nào là absolute, cái nào nhân theo parent:

## 3. Spawn / unload lifecycle

> Giống blueprint §5 thì ghi "theo blueprint §5". Chỉ ghi phần khác.

-

## 4. Query contract (gameplay hot path)

| Query | Độ phức tạp | Nguồn dữ liệu |
|---|---|---|
| | | |

## 5. Nhịp & scheduler

- Ai gọi `Tick(dt)`:
- Delay nào đọc từ profile nào:
- Tài nguyên nào có trạng thái `Reserved`, và mỗi luật tính nó thế nào:

| Luật | Reserved tính là |
|---|---|
| | |

## 6. Điều kiện kết cục

- Win khi:
- Lose khi:
- Đánh giá tại thời điểm nào (sau mutation nào):
- Thứ tự kiểm:
- Banner chờ điều kiện gì:

## 7. Baking / generated data

```text
[SourceData]
    ↓ generated
[Mesh / Footprint / ...]
    ↓ baked
[Graph / Cache]
    ↓ runtime
[RuntimeState]
```

- Cache có serialize không, hay bake lại lúc load:
- Test parity giữa editor và runtime:

## 8. Performance guardrail riêng của game

> Chỉ ghi cái **khác** `standards/performance-budget.md`.

-

## 9. Supersede log

> Khi một decision sau ghi đè mô tả ở trên, ghi một dòng ở đây kèm `D-xxx`. Không xoá mô tả cũ khỏi
> decision log.

| Ngày | Mục bị đổi | Decision |
|---|---|---|
| | | |
