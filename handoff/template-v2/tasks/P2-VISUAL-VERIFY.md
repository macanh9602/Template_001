# P2-VISUAL-VERIFY — verify Phase 2 visual-direction Editor code in real Unity

Status: EXECUTABLE (script verify first; agent only if a step fails)
Header: `P2-VISUAL-VERIFY.json`

## 1. Observable result

The Phase 2 Editor code (`Assets/_Core/4_Scripts/Editor/VisualDirection/`: MiniJson, VisualDirectionFile,
VisualDirectionImporter, MotionParity, VisualCapture, Tests) was written without a Unity compile
(checked only with mcs against stubs + 7 tests on an emulated SerializedObject). This task proves it compiles
and its EditMode tests pass in Unity, with evidence files.

## 2. Locked semantics

- Schema `visual-direction/v1` (`templates/schemas/visual-direction.schema.json`) does not change.
- Importer imports only sections with status `APPROVED`; Dry Run never writes.
- Log contract: the FIRST line of every log is the verdict; PASS = `Debug.Log`, error/drift/FAIL = `Debug.LogWarning`
  (missing CURRENT = `Debug.LogError`). Script verify depends on it.
- Fixes are compile/test fixes only, inside the write set.

A semantic change is needed ⇒ stop with `RESULT: BLOCKED`.

## 3. Steps (Unity MCP; Unity Editor open on Template_001)

1. Refresh assets, wait for compilation, read the Console. Write `handoff/template-v2/captures/P2-console.txt`
   (error count + text of every error). Compile errors in the write set ⇒ fix minimally, repeat.
2. Run all EditMode tests. Write `handoff/template-v2/captures/P2-tests.md` (total/passed/failed + every failure).
3. Run menu `Tools/Visual Direction/Dry Run (log drift)`. There is no `handoff/visual/CURRENT` in the template,
   so exactly one `[VisualDirection]` error is expected. Copy it to `handoff/template-v2/captures/P2-dryrun.txt`.

## 4. Output

`RESULT: DONE` or `RESULT: BLOCKED` + one-paragraph summary + the three evidence files.
