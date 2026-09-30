# Authority + conformance reference

## Authority

Không copy cả prototype/tool thành một contract duy nhất.

Tách ít nhất:

- gameplay rules;
- data schema;
- authoring workflow;
- solver/evaluator;
- visual;
- feel/timing;
- meta/progression.

## External authoring rules

- Export là one-way trừ khi project **thật sự cần** round-trip.
- Canonical exported data phải có schema/version.
- Unity validation không được silently mutate user-authored data.
- Derived/baked cache không trở thành source of truth.
- Tool-specific view state không đi vào runtime level data.

## Conformance

Ưu tiên semantic fixture:

```text
input data
+ commands
→ expected semantic state/events/result
```

Nếu oracle thay đổi, bump oracle version và regenerate fixture có chủ đích.

Conformance không chứng minh visual parity trừ khi fixture/capture được thiết kế riêng cho visual.
