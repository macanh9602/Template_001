# PROJECT READINESS PROMPT — trước active story đầu tiên

> Dùng ở đầu project mới hoặc khi adopt một Unity project/template hiện hữu.
> Đây là **audit-only pass**: chưa implement gameplay, chưa migrate folder/namespace, chưa tạo framework mới.

```text
Mục tiêu: xác định project đã sẵn sàng cho active story chưa, và chặn rework foundation do assumption sai.

ĐỌC:
1. AGENTS.md
2. playbooks/p1-bootstrap.md
3. Docs/project-context.md
4. standards/folder-structure.md
5. standards/system-design.md
6. Docs/runtime-architecture.md
7. Docs/data-model.md
8. handoff/ROADMAP.md

SAU ĐÓ INSPECT CÓ MỤC ĐÍCH, KHÔNG SCAN VÔ HẠN:
- Unity version / render pipeline / package baseline.
- Các script root đang tồn tại trong Assets/_Core và Assets/.
- Scene root + Build Settings + scene đang làm bootstrap/gameplay.
- Assembly-CSharp và asmdef đang tồn tại; dependency shared module/package.
- Prefab/Resources/level-data roots.
- Runtime owner đang có: bootstrap, level lifecycle, composition/spawn, gameplay, input, HUD.
- Placeholder identity trong Docs/handoff/code.
- Code/template cũ: generic reusable vs project/game-specific.
- Có root song song hoặc owner trùng responsibility không.
- Có story downstream nào đã materialize dù architecture/foundation chưa PASS không.
- Product input là GDD-only, executable prototype, reference game/video hay existing runtime.
- Level được author trong Unity, external tool, generator hay manual data.
- Nếu có prototype/external tool: area nào authoritative/oracle/reference; có contradiction chưa resolve không.
- Có conformance fixture/oracle cho semantic behavior critical không.

KHÔNG ĐƯỢC TRONG PASS NÀY:
- Không tạo root `_Core/Scripts` nếu project đang dùng `_Core/4_Scripts`, hoặc ngược lại.
- Không move/rename code hàng loạt.
- Không thêm asmdef để "đẹp architecture" trước khi audit dependency.
- Không resurrect/comment-in gameplay code project cũ.
- Không yêu cầu asset/reference ngoài workspace nếu nó không được user định nghĩa là blocker.
- Không viết cả story pack tương lai.

OUTPUT BẮT BUỘC:

## PROJECT READINESS

- Mode: GREENFIELD | EXISTING_PROJECT_ADOPTION
- Product input: GDD_ONLY | EXECUTABLE_PROTOTYPE | REFERENCE_GAME | VIDEO_MOCKUP | EXISTING_RUNTIME
- Level authoring mode: UNITY_EDITOR | EXTERNAL_TOOL | GENERATED | MANUAL_JSON | NONE_YET
- Prototype authority contract: NONE | <path>
- Conformance oracle: NONE | <name/version/path>
- Identity: PASS | FAIL
- Canonical script root: <path> | UNRESOLVED
- Canonical prefab root: <path> | UNRESOLVED
- Scene/bootstrap owner: <value> | UNRESOLVED
- Assembly reality: <summary>
- Assembly strategy: <preserve / in-place asmdef / bridge / migration-needed> | UNRESOLVED
- Data source of truth: <value> | UNRESOLVED
- Runtime authority: <value> | UNRESOLVED
- Existing code policy: <summary>
- Parallel-root risk: NONE | <details>
- Duplicate-owner risk: NONE | <details>
- Placeholder/identity risk: NONE | <details>
- External reference blocker: NONE | <exact blocker>
- Story materialization risk: NONE | <details>
- Compile/console baseline: PASS | KNOWN FAILURE | NOT RUN + reason

## ADOPTION GAPS

Bảng:
Gap | Evidence | Proposed action | Cần dev decision? | Chặn story nào

Chỉ đưa gap có evidence thật. Không invent task để "hoàn thiện template".

## DECISIONS NEEDED

Chỉ hỏi project-wide contract chưa resolve. Mỗi câu 2–4 option + Other, có recommend.
Nếu không có blocker thì ghi NONE.

## READY FOR ACTIVE STORY

READY FOR ACTIVE STORY: YES | NO

YES chỉ khi:
- canonical roots đã resolve;
- không có hai production script/framework root cùng responsibility;
- assembly strategy không tự mâu thuẫn dependency hiện tại;
- scene ownership rõ;
- data source-of-truth rõ;
- authoring source/canonical exported data rõ nếu project cần level/content authoring;
- executable prototype/oracle không còn contradiction có thể đổi semantic implementation;
- runtime architecture docs không mâu thuẫn repo thật;
- active story kế tiếp không phụ thuộc contract chưa chốt.

Nếu NO: dừng sau report/decision questions. Không tự implement phần phụ thuộc blocker.
```
