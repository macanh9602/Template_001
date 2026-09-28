# Bot & metrics — chi tiết triển khai

> Đọc khi thực sự dựng bot/simulator. `SKILL.md` đã nêu nguyên tắc; file này là phần kỹ thuật.

---

## 1. Bot chạy ở đâu

Bot chạy trên **Domain thuần C#**, không MonoBehaviour, không Visual, không coroutine.

Hệ quả kiến trúc (đã ghi ở `standards/system-design.md`):

- Domain không được đọc `Transform`, `Collider`, `Renderer`.
- Mọi trạng thái gameplay phải **snapshot/restore được** — bot cần rollback để thử nhánh.
- Mọi randomness đi qua **một** RNG có seed, không dùng `UnityEngine.Random` rải rác.

Không đạt được ba điều trên ⇒ bot sẽ chậm, không deterministic, và không chạy được trong test.
Đây là lý do kiến trúc Domain/Visual không phải sở thích mà là điều kiện cần.

## 2. Interface tối thiểu

```csharp
public interface ISimulatableState
{
    IReadOnlyList<Move> LegalMoves { get; }
    bool IsWin  { get; }
    bool IsLose { get; }
    void Apply(in Move move);
    ISimulatableState Snapshot();   // deep copy, không share reference
}

public interface IPolicy
{
    string Version { get; }          // đi kèm MỌI số đo
    Move Choose(ISimulatableState state, IRandom rng);
}
```

`Snapshot()` share reference là bug khó tìm nhất trong mảng này: bot chạy đúng vài trăm lần rồi
kết quả bắt đầu lệch. Có unit test riêng cho `Snapshot()` (mutate bản sao → bản gốc không đổi).

## 3. Human-model policy

```
Choose(state):
  1. lấy tập nước đi hợp lệ
  2. cắt còn K lựa chọn "đáng chú ý" (theo heuristic đơn giản, không phải search sâu)
  3. với xác suất p → chọn ngẫu nhiên trong K
     ngược lại   → chọn nước tốt nhất theo heuristic
```

- `K` mô hình **tầm chú ý**. K = ∞ ⇒ đang mô hình người chơi hoàn hảo, vô dụng để đo độ khó.
- `p` mô hình **sai sót**. p = 0 ⇒ level nào không có bẫy "một sai là chết" cũng thành dễ.
- Hiệu chuẩn `K`,`p` bằng cách khớp `passRate` mô phỏng với `passRate` thật trên tập level đã ship.
  Chưa có dữ liệu thật ⇒ chọn giá trị mặc định và **ghi rõ là chưa hiệu chuẩn**.

## 4. Chạy batch

```
foreach level:
  foreach seed in seedRange (N ≥ 200):
      chạy 1 ván, ghi: win?, moves, timeToWin, nearMiss?, lý do thua
```

Kết quả tổng hợp:

| Metric | Công thức | Nói lên | KHÔNG nói lên |
|---|---|---|---|
| `passRate` | thắng / N | độ khó cảm nhận | vì sao khó |
| `avgMoves` | trung bình nước đi ván thắng | độ dài | độ căng |
| `movesSlack` | `movesAllowed − minMoves` | mức bó hẹp | có bẫy hay không |
| `nearMissRate` | thua nhưng cách thắng ≤ ε | mức "ức chế" | độ khó tổng thể |
| `deadEndRate` | vào trạng thái không cứu được | mức trừng phạt sai lầm | |
| `branchingAvg` | trung bình nước hợp lệ mỗi lượt | tải nhận thức | |
| `failReasonHist` | phân bố lý do thua | **nguồn gốc độ khó** | |

`failReasonHist` là chỉ số hữu ích nhất khi cần *sửa* một level: nó nói level khó **vì cái gì**.

## 5. Golden test

```
tests/difficulty/golden/
  levels/          ← tập level cố định, không bao giờ sửa
  expected.json    ← passRate kỳ vọng ± dung sai, kèm botVersion
```

Chạy trong CI. Golden lệch ⇒ **build đỏ**, buộc phải giải thích:

- đổi có chủ đích (bot tốt hơn) ⇒ cập nhật `expected.json`, tăng `botVersion`, ghi `D-xxx`.
- không chủ đích ⇒ regression, phải sửa.

Dung sai phải tính theo N: `passRate` với N=200 có sai số ngẫu nhiên ±~3.5%. Đặt dung sai nhỏ hơn
sai số thống kê ⇒ CI đỏ liên tục vô nghĩa.

## 6. Hiệu năng batch

| Cách | Ảnh hưởng |
|---|---|
| chạy headless, không Visual | nhanh 100–1000× |
| object pool cho state snapshot | tránh GC nuốt hết thời gian |
| `struct` cho Move, tránh boxing | |
| song song theo seed (mỗi thread một RNG riêng) | tuyến tính theo core |
| **không** dùng `UnityEngine.Random` | không thread-safe, không seed cục bộ |

Batch mà chậm ⇒ không ai chạy ⇒ thước đo vô dụng. Mục tiêu thực dụng: **toàn bộ tập level chạy xong
trong lúc uống một ly cà phê**, nếu không thì tối ưu batch trước khi làm gì khác.

## 7. Báo cáo — khung chuẩn

```markdown
## Difficulty report — [tập level] — [ngày]

**Điều kiện đo:** botVersion `v3` · policy `human-K4-p0.12` · N=300/level · seed 1000–1299
**Hiệu chuẩn:** đã / chưa (nếu chưa → mọi kết luận là tương đối, không tuyệt đối)

| Level | passRate | band | avgMoves | slack | nearMiss | lý do thua top-1 |
|---|---|---|---|---|---|---|

### Ngoài kỳ vọng
| Level | Kỳ vọng | Đo được | Giả thuyết |
|---|---|---|---|

### Đề xuất
(mỗi đề xuất kèm: sửa gì · dự đoán passRate sau khi sửa · cách kiểm chứng)
```

Đưa cho GD ⇒ dùng `skills/gd-communication/`: hiển thị **band**, giấu số lẻ, ghi rõ chỉ số
**không** nói lên điều gì.
