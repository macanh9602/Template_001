# Bootstrap Patch workflow

> Frontier/planning layer owns reasoning. Script owns deterministic mechanical changes.
> Generated project-specific `.ps1` is **transient**; manifest/report are persistent audit records.

## Ownership

### Frontier/planning model

- inspect project/prototype;
- lock project/data/architecture contract;
- decide exact add/replace/delete/preserve set;
- generate `bootstrap-manifest.json`;
- generate project-specific temporary wrapper `.ps1` when useful;
- review diff + closure.

### Script

- validate root/state;
- backup;
- add/replace/delete exact files;
- write UTF-8 deterministically;
- verify expected hashes when supplied;
- run cheap mechanical checks;
- never make product/architecture decisions;
- never commit/push.

### Lower-reasoning worker

Starts **after** bootstrap contract is applied and verified.
Do not waste worker context on moving folders, recreating docs, boilerplate scaffolding or broad project organization.

---

## Generated script lifecycle

```text
generate
→ DryRun
→ review operations
→ Apply
→ inspect diff
→ compile/static verification
→ frontier review
→ commit project changes
→ DELETE generated project-specific .ps1
```

Keep:

- canonical Docs;
- `handoff/ROADMAP.md`;
- conformance fixtures;
- bootstrap manifest/report if they explain project state (`templates/project-bootstrap.ps1` writes a report by default under `handoff/bootstrap/`).

Delete after closure:

- generated `apply-*`, `setup-*`, `repair-*` wrappers;
- `.tmp/bootstrap/*`;
- redundant payload dumps.

Do **not** auto-delete the running script immediately after apply. If compile/review fails, it remains useful for audit/re-run.

---

## Safety contract

Project-specific updater must support when practical:

- `-DryRun`;
- dirty-worktree guard;
- `-Force` only as explicit override;
- backup outside repo or explicit backup root;
- UTF-8 byte-safe writes;
- expected-before hash for destructive replace/delete;
- idempotent/re-runnable behavior or explicit already-applied detection;
- `git diff --check`;
- no auto commit/push.

Use `templates/project-bootstrap.ps1` as the reusable executor or as the source for a self-contained generated wrapper.

---

## Persistent Bootstrap Manifest

Manifest records **intent**, not implementation mechanics.

Minimum:

```text
project/reason
operations:
  add
  replace
  delete
preserve
verification
```

A future agent should understand why files exist from Docs + manifest without needing the deleted generated `.ps1`.
