# Conformance Pack

Mục đích: biến executable prototype/reference thành expected semantic behavior để Unity implementation
không phải diễn giải lại toàn source.

Mỗi case nên có:

```text
case-XXX-input.json
case-XXX-expected.json
```

Input:
- schema/oracle version;
- authored level;
- initial state nếu cần;
- command/action sequence.

Expected:
- semantic state;
- emitted events;
- result/win/lose;
- optional solver/evaluator metric.

Không đặt implementation detail (class name, GameObject hierarchy) vào expected result trừ khi chính nó là contract.
