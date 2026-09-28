---
name: authoring-pipeline
description: Route and design level/content authoring when GD may use an external tool, Unity Level Editor, generator, or manual data. Use before deciding to build a Unity Editor tool.
---

# Authoring Pipeline

## Goal

Chọn **authoring source of truth** trước khi xây tool.

Không assume "GD cần làm level" ⇒ "phải build Unity Level Editor".

## Modes

- `EXTERNAL_TOOL` — HTML/web/custom app/third-party tool export canonical data.
- `UNITY_EDITOR` — GD author trực tiếp bằng Unity UI Toolkit/EditorWindow.
- `GENERATED` — generator tạo level/data, GD chỉnh constraints/preset.
- `MANUAL_JSON` — chỉ phù hợp prototype nhỏ/dev-only.
- `NONE_YET` — chưa chốt, phải resolve trước production pipeline.

## Core rule

```text
Authoring source
    ↓
canonical versioned data
    ↓
validation/migration
    ↓
runtime loader
```

Runtime không phụ thuộc authoring UI.

## External tool

Nếu external tool đã author được complete level:

- giữ tool làm source nếu workflow GD tốt;
- define schema/version/export contract;
- build Unity import/load/validate/conformance;
- không duplicate một Unity LE chỉ để "đúng template";
- nếu executable engine/solver tồn tại, tạo conformance fixtures.

## Unity Editor

Chỉ route sang `skills/level-editor/` + `playbooks/p4-level-editor.md` khi `Level authoring mode = UNITY_EDITOR`.

## Generator

Tách:
- authoring constraints/presets;
- deterministic/versioned generator;
- output canonical data;
- evaluator/oracle version + golden fixtures.

## Questions to resolve

- Ai author?
- Tool nào là source?
- Canonical file format/path/version?
- Runtime có cần importer hay chỉ loader?
- Validation nằm ở tool, Unity, hay cả hai?
- Có round-trip không, hay one-way export?
- Có executable oracle/solver để conformance không?

Chi tiết authority/conformance: `refs/authority-and-conformance.md`.
