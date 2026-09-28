---
name: presentation-lifecycle
description: >
  Giữ visual đúng khi object pooled, tween/UniTask chồng nhau, queue/slot compaction, transfer ownership,
  handoff giữa nhiều presentation phase hoặc bug kiểu visual cũ ghi đè state mới. KÍCH HOẠT khi:
  object bay/shift/land đồng thời, slot logic trống nhưng visual còn chiếm chỗ, callback cũ bật lại renderer,
  outline/visibility sai sau detach/reparent, hoặc bug chỉ xuất hiện khi tap nhanh.
---

# SKILL: presentation-lifecycle

Mục tiêu: tách rõ **identity · ownership · generation · readiness · handoff** để presentation có thể chạy
song song mà không tạo race và không kéo animation semantics vào Domain.

## 1. Năm câu phải trả lời trước khi sửa

| Contract | Câu hỏi |
|---|---|
| Identity | Object/sequence nào đang được nói tới? Có stable id không? |
| Ownership | Component nào hiện được quyền mutate visual state này? |
| Generation | Async operation hiện tại là lần thứ mấy? Callback cũ nhận ra mình stale bằng gì? |
| Readiness | Destination logic đã tồn tại có đồng nghĩa physical landing area đã trống không? |
| Handoff | Khi owner đổi A → B, state nào B phải normalize/rebind ngay? |

Không trả lời được một hàng ⇒ instrument trước (`skills/debug-audit/`).

## 2. Contract chuẩn

### Command/domain
- Reserve gameplay resource **ngay khi nhận command**.
- Domain không await tween và không đọc `IsAnimating` để quyết gameplay.
- Stable id/sequence đã được planner chọn ⇒ mutator consume đúng identity đó; không silent fallback sang
  "first/oldest matching".

### Presentation owner
- Trước khi launch async (`.Forget`, tween callback...), set trạng thái blocking/readiness **đồng bộ**.
- Mỗi operation nhận `operationId` hoặc `generation` tăng dần.
- Completion / cancellation / `finally` chỉ được final-write khi id vẫn là active owner.

Pseudo-pattern:

```csharp
var op = ++_motionGeneration;
_isArrivalBlocked = true;
try
{
    await PlayMotionAsync(ct);
}
finally
{
    if (_motionGeneration == op)
    {
        _isArrivalBlocked = false;
        NormalizeOwnedState();
    }
}
```

### Handoff
- Resolve destination/current owner **lại ngay trước handoff** nếu nó có thể đổi trong lúc bay.
- Handoff bằng stable id/sequence, không bằng cached index nếu collection có thể compact/reorder.
- Destination owner gọi `Adopt/Bind` và ghi đầy đủ state mình sở hữu: renderer enable, outline, color,
  transform baseline, count, local flags...

### Pool
- **Release:** cancel task/tween → invalidate generation → clear refs/readiness → normalize safe state → recycle.
- **Acquire/Bind:** luôn rebind đầy đủ từ source hiện tại; không dựa vào state của lượt trước.

## 3. Logical reservation ≠ physical readiness

Ví dụ generic: slot đã được domain assign cho item mới, nhưng content cũ của slot vẫn đang shift ra ngoài.

Không được fix bằng `await 0.15s` hoặc "đợi duration hiện tại". Timing sẽ đổi sau.

Dùng hai state riêng:
- `Reserved/Filled` — Domain contract.
- `IsReadyForArrival` / `IsArrivalBlocked` — Presentation contract.

Incoming visual có thể bay phần lớn quãng đường, nhưng chỉ final-land/handoff khi presentation destination
ready. Contract readiness không được thay đổi gameplay ordering.

## 4. Checklist race nhanh

- [ ] Flag block được set trước async launch, không phải frame sau.
- [ ] Old operation không thể clear flag của new operation.
- [ ] Zero-duration path trả readiness về đúng ngay trong cùng frame.
- [ ] Cleanup/unload luôn neutralize readiness + invalidate generation.
- [ ] Final callback không dùng captured count/index nếu state có thể mutate.
- [ ] Destination resolve lại trước handoff.
- [ ] Ownership boundary normalize visual state.
- [ ] Exact stable id/sequence từ planner đến mutator.
- [ ] Có audit event cho blocked → ready → handoff và mismatch.

## 5. Output

1. Ownership map: `phase → owner → state được phép mutate`.
2. Lifecycle table: `Acquire/Bind → Active → Transfer → Release`.
3. Async ownership rule: generation/token và nơi increment/invalidate.
4. Readiness contract nếu có destination dùng chung.
5. Audit points + manual repro case rapid input/replaced motion/unload giữa animation.

**Không broad-refactor Domain để chữa một race presentation.** Fix đúng ownership boundary nhỏ nhất trước.
