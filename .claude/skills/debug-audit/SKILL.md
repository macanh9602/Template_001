---
name: debug-audit
description: >
  Runtime bug investigation for Unity bằng targeted file-based instrumentation. KÍCH HOẠT ngay khi
  user báo bug intermittent, sai state/order/ownership, visual-runtime desync, pooling/async race,
  reload bug, rapid-input bug, hoặc nguyên nhân chưa được chứng minh rõ bằng static inspection.
  Agent tự inspect causal path, tự hook audit vào code, compile, rồi chỉ nhờ dev manual repro. Sau
  khi dev nói "đã repro", agent tự đọc file audit .md và tìm first incorrect transition trước khi fix.
---

# SKILL: debug-audit v2 — agent-owned instrumentation

## Contract lõi

1. **Agent instruments; dev không thiết kế log.**
2. **Một repro = một file `.md`**, overwrite/clear ở đầu level-load/repro boundary.
3. **File only; không spam Console.** Không `Debug.Log` chỉ để báo audit path.
4. **Log transition/decision, không log mỗi frame.**
5. Dev chỉ manual test và nói **`đã repro`**.
6. **Không fix khi chưa có evidence.** Evidence chưa đủ → agent tự thêm hook hẹp hơn rồi nhờ repro lại.

Bug hiển nhiên bằng static code inspection (null guard thiếu, off-by-one rõ, condition đảo) có thể fix
thẳng. Bug state/timing/ordering mà chưa chứng minh được thì instrument **ngay từ vòng đầu**, không đợi
fix sai một lần.

---

## 0. Trước khi hook

Đọc đúng causal slice, không scan toàn project:

`input/trigger → domain decision → reservation/ownership → mutation → presentation/handoff → cleanup`

Tra `standards/anti-patterns.md §B` và skill `presentation-lifecycle/` nếu có pool/async/tween.

Viết nội bộ 2–5 hypothesis; mỗi hypothesis phải có **một dấu hiệu phân biệt được trong audit**. Không
cần hỏi dev duyệt hypothesis.

---

## 1. Instrumentation ownership

Agent tự làm đủ vòng sau:

```
Bug report
  ↓
inspect targeted code
  ↓
identify causal boundaries + hypotheses
  ↓
add targeted audit hooks
  ↓
compile/source-check
  ↓
"Audit ready — repro case lỗi một lần rồi nhắn 'đã repro'."
```

Không dừng ở việc đưa snippet để dev tự chèn. Không bắt dev copy/paste console log.

Nếu project đã có reusable `AgentDebugAudit`, dùng nó. Nếu chưa có, tạo một helper generic nhỏ ở
`Assets/_Core/4_Scripts/Diagnostics/AgentDebugAudit.cs`; helper sống lại qua bug sau, hook cụ thể thì có
thể tháo sau khi fix.

---

## 2. File contract

Trong Unity Editor, audit nằm ở project root để coding agent đọc trực tiếp:

```text
AgentAudit/<channel>.md
```

Development Build có thể fallback sang `Application.persistentDataPath/AgentAudit/`.

Mỗi investigation có **một channel** ngắn, ví dụ:

```text
queue-drain-priority
strip-reload-visual
tray-delivery-order
```

`Begin(...)` phải overwrite file cũ ở đầu repro boundary. Với level-based game, hook mặc định là
**ngay khi LevelManager nhận request load**, trước cleanup level cũ, để cùng file capture được:

`old cleanup → pool release → new spawn/bind → stale async completion → gameplay`.

Không append nhiều lần chơi vào cùng file rồi bắt AI đoán phiên nào là phiên lỗi.

---

## 3. Toggle / build guard / budget

- Có toggle/flag rõ: `Enabled` hoặc per-channel enable.
- Chỉ hoạt động trong `UNITY_EDITOR || DEVELOPMENT_BUILD`.
- Default budget: khoảng **500 event/repro**; agent được đổi khi case cần.
- Khi chạm budget phải append `AUDIT_TRUNCATED`, **không silently stop**.
- Sau khi fix: disable channel hoặc remove temporary hook. Giữ core helper reusable.

Instrumentation không được làm thay đổi gameplay timing đáng kể. Ưu tiên 5–15 hook chiến lược hơn
100 `Write()` rải khắp class.

---

## 4. Log **decision + reason**, không dump state vô nghĩa

Log yếu:

```text
slot=2; pieceCount=3
```

Log tốt:

```text
QueueDrain.SourceSelected
cake=Blue; sequence=17; slot=3; pieces=3
candidates=[seq15/slot2/p4, seq17/slot3/p3, seq20/slot4/p3]
decision=sequence17
reason=min pieces; tie-break higher slot index
```

Mỗi event nên có khi relevant:

- stable/domain id
- Unity instance id nếu pool/reuse quan trọng
- sequence/order id
- generation / operation id / ownership id cho async
- slot/index **chỉ như vị trí**, không thay stable id
- state before → state after
- decision + reason

Planner/mutator flow phải log **planned identity và actual mutated identity**. Async/pool flow phải log
**started generation/owner và current generation/owner** ở completion/finally.

---

## 5. Chỉ log transition / boundary

### Nên hook

- command accepted/rejected + reason
- candidate set → selected candidate
- reserve/release
- mutation request → mutation result
- ownership handoff
- pool acquire/release/rebind
- async start/replaced/cancelled/completed
- state transition `Ready ↔ Blocked`
- load begin / cleanup / spawn ready

### Cấm mặc định

```csharp
void Update() => Audit(...);
```

Không frame-by-frame position/progress dump. Nếu cần biết threshold crossing, log **một lần khi state
đổi** (`HoldBegin`, `HoldEnd`), không log `progress=.01/.02/...`.

---

## 6. Sau khi dev nói `đã repro`

Agent tự tìm channel đang active và đọc **toàn bộ file audit**. Không hỏi dev gửi lại log nếu file
nằm trong workspace/project mà agent truy cập được.

Phân tích theo thứ tự:

1. dựng timeline từ đầu repro;
2. tìm **first divergence** — event đầu tiên khác expected contract;
3. lùi một boundary để tìm writer/decision gây sai;
4. đối chiếu code ở đúng boundary;
5. phân loại evidence:
   - `PROVEN` — log chứng minh root cause;
   - `STRONG` — chỉ còn một hypothesis hợp lý nhưng thiếu một transition;
   - `INSUFFICIENT` — chưa phân biệt được hypothesis.

`INSUFFICIENT` ⇒ tự hook thêm đúng 1–3 boundary còn thiếu rồi nói ngắn: *"Đã thêm audit hẹp hơn ở
reservation→consume. Repro lại một lần."* Không đưa speculative fix.

---

## 7. Khi evidence đủ

Trả lời ngắn theo:

**Root cause → Evidence → Fix → Regression risk → Manual verify**.

Trích vài event đủ chứng minh, không dump cả file. Nếu fix nằm trong autonomy của story thì implement
luôn; chỉ escalate nếu chạm gameplay semantics / project contract / serialization / scope.

Sau fix, giữ audit bật cho **một lần verify lại** nếu bug intermittent. Khi verify ổn:

- disable/remove temporary hooks;
- giữ reusable helper;
- harvest `triệu chứng → nguyên nhân → cách tránh` vào `standards/anti-patterns.md` nếu generic.

---

## Quick patterns

| Triệu chứng | Hook trước |
|---|---|
| object sai sau respawn/reload | release → acquire → bind generation → async completion |
| nhanh tay mới lỗi | command accepted → reservation → mutation identities |
| visual đúng lúc đầu rồi bật ngược | visual write + async owner/generation completion |
| planner chọn đúng nhưng consume sai | candidate → selected stableId → actual consumed stableId |
| slot logic rảnh nhưng visual overlap | logical assignment + physical readiness + handoff |
| lỗi biến mất khi thêm nhiều log | giảm hook, chỉ transition; nghi timing/race |

## Đầu ra vòng 1

Không cần báo hypothesis dài. Chỉ báo:

- đã hook vùng nào;
- audit channel/file;
- flag đang bật;
- dev cần repro thao tác gì.

Sau đó chờ đúng một việc từ dev: **manual repro**.
