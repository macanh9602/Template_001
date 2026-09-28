# Prototype Authority Contract — [PROJECT]

Source: `[file/path/url]`
Reviewed: `[date]`
Owner: Product Owner / Dev

> Không có khái niệm "file này authoritative toàn bộ".
> Authority phải tách theo area để Unity không copy nhầm UI/architecture/implementation detail của prototype.

| Area | Source | Authority | Unity parity required? | Notes |
|---|---|---|---|---|
| Gameplay semantics | | AUTHORITATIVE / ORACLE / REFERENCE / OUT_OF_SCOPE | YES/NO | |
| Level schema/data | | | | |
| Solver/evaluator | | | | |
| Authoring workflow | | | | |
| Input | | | | |
| Progression | | | | |
| Visual | | | | |
| Feel/timing | | | | |
| Audio | | | | |
| Performance behavior | | | | |

## Known contradictions / ambiguity

| ID | Observation A | Observation B | Decision needed | Status |
|---|---|---|---|---|
| | | | | OPEN / RESOLVED |

## Canonical boundary

```text
[Authoring source]
    ↓
[Canonical data]
    ↓
[Validation/version]
    ↓
Unity runtime
```

## Conformance oracle

- Available: YES / NO
- Location: `reference/conformance/`
- Oracle version/name:
- What it proves:
- What it does NOT prove:
