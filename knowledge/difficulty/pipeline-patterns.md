# Difficulty — pipeline patterns

> Khung Producer / Mutator / Validator để sinh level có kiểm soát.
> Chỉ dùng khi đã có thước đo (`INSTRUMENT` + `EVALUATE` xong).

---

## 1. Ba tầng, ba trách nhiệm không chồng lấn

```
Producer  → sinh ứng viên thô, từ seed
Mutator   → biến đổi ứng viên để đẩy score về mục tiêu
Validator → loại ứng viên vi phạm rule cứng
Evaluator → chấm điểm (dùng bot, đã có ở giai đoạn INSTRUMENT)
```

| Tầng | ĐƯỢC làm | KHÔNG được làm |
|---|---|---|
| Producer | sinh mới từ seed | biết về ngưỡng score |
| Mutator | sửa ứng viên | tạo ứng viên từ đầu |
| Validator | **loại** | **sửa** |
| Evaluator | chấm điểm | thay đổi ứng viên |

**Validator sửa dữ liệu** là lỗi hay gặp nhất và khó tìm nhất: level cuối khác bản sinh mà không ai
biết khác chỗ nào, seed không tái tạo được kết quả.

## 2. Vòng lặp chuẩn

```
generate(target, seed):
  rng = new Rng(seed)
  best = null
  for attempt in 1..maxAttempts:
      cand = Producer.Create(rng)
      if !Validator.Check(cand): record(cand, reason); continue
      score = Evaluator.Score(cand)
      for step in 1..maxMutations:
          if inTarget(score): return cand with {seed, attempt, score}
          cand2 = Mutator.Nudge(cand, towards=target, rng)
          if !Validator.Check(cand2): record(cand2, reason); continue
          cand = cand2; score = Evaluator.Score(cand)
      best = keepIfCloser(best, cand, target)     // near-miss
  return fail(reason = lý do fail cuối cùng, nearMiss = best)
```

Bắt buộc:

- **Seed ghi vào level data.** Không tái tạo được level thì không debug được báo cáo lỗi.
- **`maxAttempts` và `maxMutations` có giới hạn**, không `while(true)`.
- **Hết retry ⇒ trả về lý do fail cuối cùng + near-miss**, không trả `null` im lặng.
- **Giữ near-miss** kèm lý do trượt — dữ liệu quý nhất để biết ngưỡng có đang quá chặt không.

## 3. Producer

| Kiểu | Cách làm | Hợp với |
|---|---|---|
| `random + filter` | sinh ngẫu nhiên, loại cái xấu | không gian nhỏ, rule đơn giản |
| `constructive` | dựng theo luật, luôn hợp lệ | rule cứng nhiều, random hay trượt |
| `reverse-play` | bắt đầu từ trạng thái thắng, đi ngược | đảm bảo **có lời giải** theo xây dựng |
| `template + fill` | khung do GD làm, máy điền chi tiết | giữ được ý đồ thiết kế |

`reverse-play` là cách rẻ nhất để bảo đảm solvability mà không cần solver — nhưng nó chỉ bảo đảm
**tồn tại** một lời giải, không nói gì về độ khó.

## 4. Mutator — nudge, đừng nhảy

- Mỗi bước mutate thay đổi **một** thứ, biên độ nhỏ. Nhảy lớn ⇒ score dao động, không hội tụ.
- Mutator phải biết **hướng**: mutate nào làm khó lên, mutate nào làm dễ đi. Bảng này là kiến thức
  thiết kế, ghi lại rõ ràng:

| Mutate | Hướng | Trục ảnh hưởng |
|---|---|---|
| giảm `movesAllowed` | khó ↑ | constraint |
| thêm chướng ngại | khó ↑ | constraint, search |
| thêm loại phần tử | khó ↑ | search |
| nới cửa sổ thời gian | khó ↓ | execution |
| thêm đường lùi | khó ↓ | recovery |

- Mutate không đổi score sau vài bước ⇒ dừng sớm, đổi ứng viên. Đừng phí vòng lặp.

## 5. Validator — rule cứng

| Loại rule | Ví dụ |
|---|---|
| cấu trúc | không phần tử chồng nhau, không nằm ngoài vùng chơi |
| khả thi | có ít nhất một lời giải (theo solver hoặc theo xây dựng) |
| nội dung | cơ chế chưa được dạy thì không xuất hiện |
| thẩm mỹ | không quá N phần tử cùng loại liền nhau |

Mỗi rule trả về **lý do đọc được**, không phải `false`. Thống kê lý do loại cho biết rule nào đang
quá chặt.

## 6. Phase window — theo tiến trình, không theo index

Rule dạng *"loại A chỉ xuất hiện ở nửa sau"* viết theo `placementProgress ∈ [0,1]`:

```
allowedAt(type) = placementProgress ∈ [w.start, w.end]
```

Dùng index tuyệt đối ⇒ level dài 20 phần tử và level dài 60 phần tử ra bố cục khác hẳn nhau, và
không ai hiểu vì sao.

Bảng window là **dữ liệu**, nằm trong SO/JSON để GD chỉnh — không phải hằng số trong code.

## 7. Target theo band, không theo số lẻ

```
inTarget(score) = band(score) == targetBand
```

Nhắm vào một số cụ thể (`score == 0.63`) ⇒ retry vô ích. Nhắm vào band ⇒ hội tụ nhanh và khớp với
cách GD thực sự nghĩ.

## 8. Kiểm chứng batch sinh level

Sinh xong một tập ⇒ **luôn** chạy các kiểm tra này trước khi đưa vào build:

- [ ] Phân bố band khớp ý đồ (vẽ histogram, không nhìn số trung bình)
- [ ] Không có level trùng lặp (hash cấu trúc)
- [ ] Mọi level tái tạo được từ seed (chạy lại 10 level ngẫu nhiên, so byte)
- [ ] Đường cong độ khó theo thứ tự level: tăng dần, **không đơn điệu**, có nhịp nghỉ
- [ ] Level đầu tiên của mỗi cơ chế mới nằm ở band Easy
- [ ] Tỉ lệ loại của validator hợp lý — loại > 90% nghĩa là Producer sai hướng, không phải rule tốt

## 9. Bẫy

| Bẫy | Hậu quả |
|---|---|
| Validator vừa loại vừa sửa | level cuối khác bản sinh, seed vô dụng |
| không lưu seed | không tái tạo được level người chơi báo lỗi |
| `while(true)` retry | treo, hoặc chạy vài giờ |
| vứt near-miss | mất thông tin về ngưỡng quá chặt |
| mutate biên độ lớn | score dao động, không hội tụ |
| phase window theo index tuyệt đối | bố cục khác nhau giữa level dài/ngắn |
| nhắm score lẻ thay vì band | retry vô ích |
| chỉ nhìn score trung bình của tập | phân bố lệch mà không biết |
| sinh trước khi có thước đo | không biết mình vừa sinh ra cái gì |
