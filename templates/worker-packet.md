# Worker Packet [ID] — [Observable capability]

Status: `EXECUTABLE`
Big Phase: `[phase]`
Worker profile: lower-reasoning / implementation-focused

> Packet này không chứa open product decision.
> Frontier/planning layer phải resolve semantics/architecture/data blocker trước khi packet thành EXECUTABLE.

## 1. Observable result

> Sau packet này, [ai/system] có thể [observable capability].

## 2. Locked semantics

- 
- 
- 

Không reinterpret các mục này. Semantic deviation ⇒ `BLOCKED`, không tự redesign.

## 3. Inputs / source of truth

| Input | Path | Vai trò |
|---|---|---|
| Project contract | `Docs/project-context.md` | authoritative |
| Runtime architecture | `Docs/runtime-architecture.md` | authoritative |
| Data model | `Docs/data-model.md` | authoritative |
| Conformance fixture | `reference/conformance/...` | oracle |
| Decision | `D-xxx` | authoritative |

## 4. Entry gate

- [ ] Dependency packet/gate = PASS/DONE.
- [ ] Compile/baseline state recorded.
- [ ] Không có unresolved product/data/architecture blocker.

**Hard gate rule:** entry gate `PENDING` = `BLOCKED`; không implement downstream.

## 5. Allowed scope

- 

## 6. Forbidden / preserve

- Không mở rộng Big Phase.
- Không đổi serialization/gameplay semantics ngoài locked contract.
- Không sửa unrelated dirty/user-authored data.
- Không tạo parallel owner/source-of-truth.
- 

## 7. Implementation freedom

Worker sở hữu **HOW** trong allowed scope:

- local class/method split;
- reversible utilities;
- targeted refactor;
- test helpers;
- performance implementation miễn giữ contract.

## 8. Conformance / acceptance

- [ ] Fixture/oracle case ... PASS.
- [ ] Targeted test ... PASS.
- [ ] Compile/console evidence.
- [ ] Lifecycle/performance gate phù hợp packet.
- [ ] `git diff --check`.
- [ ] `implementation-notes.html` cập nhật.

## 9. Baseline debt

| Check | Baseline before packet | Rule |
|---|---|---|
| | | Không biến pre-existing fail thành story fail; cũng không gọi full suite PASS |

## 10. Exit

Worker chỉ report một trong:

```text
IMPLEMENTED
```

Code/scope đã hoàn thành nhưng closure/manual/device/review còn pending.

```text
BLOCKED — <exact blocker + evidence>
```

`IMPLEMENTED != DONE`.

Không tự claim `DONE`. `DONE` là closure state sau required evidence/review.
