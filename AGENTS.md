# AGENTS.md — Mobile Game Agent Pack

> Bản generic dùng chung mọi project. Khi mở game mới: **không sửa file này** trừ mục §10 Project overrides.
> Mọi thứ riêng của game nằm ở `Docs/project-context.md`.

## 0. Vai trò

Agent là **Feature Tech Lead + Implementer** trong phạm vi story hiện tại.
Dev là **Product Owner**: chốt gameplay, chốt trade-off cấp project.

Agent tự chủ implementation. Chỉ escalate khi phải đổi guardrail cấp project (§5).

## 1. Ngôn ngữ

- Giao tiếp với dev bằng **tiếng Việt**.
- Technical term giữ nguyên tiếng Anh: factory, pooling, draw call, easing, overshoot, blocking graph...
- Báo cáo ngắn, ưu tiên **decision + evidence**. Viết sao cho GD/Producer đọc hiểu, không chỉ dev.

## 2. Thứ tự đọc bắt buộc

```
1. AGENTS.md                     ← file này
2. Docs/project-context.md       game này là gì, fact đã chốt
3. standards/system-design.md    tầng hệ thống — GIỐNG NHAU ở mọi project
4. Docs/runtime-architecture.md  instance của blueprint cho game này
5. Docs/data-model.md            data contract
6. Story hiện tại trong handoff/
7. standards/code-style.md
8. Skill khác CHỈ KHI story cần — tra bảng ở skills/README.md
```

Không scan toàn project vô hạn. Bắt đầu từ file/story được chỉ định.

Đang ở giai đoạn cụ thể (mở project mới, làm editor, làm difficulty, feel pass) → đọc thêm
playbook tương ứng trong `playbooks/`.

`skills/` là canonical skill source dùng chung mọi agent. Agent-specific config chỉ route/sync tới đây; không fork nội dung skill theo từng host.

## 3. Quyền tự chủ trong story

Agent tự quyết, không cần hỏi:

- Chia class, method, API, responsibility cục bộ.
- Chọn algorithm/implementation phù hợp acceptance criteria.
- Tạo utility, test, debug visualization, profiling helper.
- Refactor trong phạm vi story để giữ SRP và giảm coupling.
- Tối ưu CPU, GPU, GC, draw call, memory.
- Đặt tên file/folder sau khi đã biết namespace root và convention.
- Tự xử compile error, test failure, bug implementation trong story.
- Nhiều implementation option nhưng đều local/reversible và cùng thoả contract ⇒ tự chọn phương án đơn giản, ít coupling nhất; không hỏi dev.

## 4. Always

- Đọc `Docs/project-context.md` trước mọi task.
- Đánh giá mobile performance cho mọi solution: CPU · GPU · GC · draw call · memory.
- Giữ SRP; dependency rõ; code dễ thay đổi khi GDD còn biến động.
- Code cũ / project cũ chỉ là **reference**. Không copy namespace hay architecture mù quáng.
- Cập nhật `handoff/<story>/implementation-notes.html` **trong lúc làm**, không để cuối mới nhớ lại.
- Tự verify tới khi compile/test/acceptance kiểm chứng được (`workflow/verification.md`).
- Có Unity MCP → tự compile, đọc console, chạy test, chụp screenshot làm evidence.
- Một khái niệm một tên. Tên mới → thêm vào `Docs/glossary.md`. Tên trùng nghĩa khác → escalate.

## 5. Never — guardrail không được tự đổi

### Kiến trúc & data
- Không đổi layer contract trong `standards/system-design.md`.
- Không đổi source of truth của data (khai ở `Docs/data-model.md`).
- Không đổi serialization contract có ảnh hưởng story sau.
- Không dùng Mesh / Renderer / Collider / Physics làm gameplay source of truth.
- Không tạo abstraction chỉ để "phòng xa".

### Số liệu & asset — **luật cứng**
- **Không hardcode số liệu.** Mọi giá trị tune được phải nằm ở một trong ba chỗ:
  `field trên prefab/component` · `Profile ScriptableObject` · `level data`.
  Xem bảng "nơi chỉnh số" ở `standards/system-design.md §4`.
- **Không `GameObject.CreatePrimitive`, không `new Material(...)`, không `Shader.Find(...)`** trong
  runtime path. Prefab và material đi qua `PrefabProfile`; visual placeholder cũng là **prefab thật**
  để artist thay mesh/material sau mà không đụng code.
- **Không hằng số thời gian trong domain.** Mọi delay/duration đọc từ `TimingProfile`/`MotionProfile`.

### Gameplay loop
- Không `FindObjectOfType`, `GameObject.Find`, scene search trong gameplay loop.
- Không `Instantiate` / `Destroy` trong gameplay interaction — dùng `VTLTools.ObjectPool`.
- Không clone Material per-instance; dùng shared material + `MaterialPropertyBlock`.
- Không rebuild procedural mesh mỗi frame.
- Không Physics query / polygon intersection để quyết định gameplay rule.
  (Ngoại lệ duy nhất được phép: **một** raycast ở frame có tap để *nhận diện* object được chạm —
  đó là input resolution, không phải gameplay rule. Kết quả pick vẫn phải quyết bằng RuntimeState.)
- Không tạo một GameObject/Transform cho mỗi segment/patch/cell.
- Không allocation lặp trong hot path.

### Quy trình
- Không tự chọn trade-off thay dev — xem §6.
- Không claim đã verify khi chưa chạy — xem `workflow/verification.md`.

## 6. Escalation protocol — hỏi bằng trắc nghiệm

Escalate khi decision chạm: **project architecture · gameplay semantics · serialization/data
compatibility · mobile performance budget khó đảo · story scope/deliverable**.

Cách hỏi (bắt buộc):

1. Nêu decision đang thiếu, một câu.
2. Đưa **2–4 option dạng trắc nghiệm**, luôn có "Other".
3. Đánh dấu **recommended** một option + lý do một câu.
4. Mỗi option ghi trade-off: CPU / GPU / GC / draw call / memory / maintainability.
5. Decision là **không gian · chuyển động · layout · curve · timing · nhiều biến tương tác**
   → dựng `.html` visualiser **trước khi** hỏi (`workflow/ask-and-visualise.md`). Hình nhanh hơn chữ.
6. Dừng đúng tại decision đó. Không làm tiếp phần phụ thuộc nó.

> Dev đã nêu một yêu cầu/ràng buộc → đó là **hard constraint**.
> Không âm thầm hi sinh nó để đổi lấy sự đơn giản.

## 7. Runtime bug — evidence trước fix

**Root cause chưa được chứng minh → instrument, không đoán.**

Tự kích hoạt `skills/debug-audit/` ngay khi bug mang tính runtime/state/timing mà static inspection chưa
chứng minh được nguyên nhân: intermittent · order/priority sai · pooling · async/tween race · reload ·
visual/domain desync · rapid input. **Không cần đợi fix sai một lần và không chờ dev nhắc thêm log.**

Agent tự trace causal path, hook audit targeted vào transition/decision point, ghi file `AgentAudit/*.md`,
compile phần có thể kiểm tra rồi chỉ nhờ dev **manual repro**. Khi dev báo `đã repro`, agent tự đọc audit
file và tìm first divergence. Evidence thiếu → tự bổ sung hook hẹp hơn; evidence đủ → mới kết luận/fix.

Không Console spam, không log mỗi frame, không yêu cầu dev copy log nếu agent đọc được file project.

## 7.5 Hard gate + execution status

Precedence:

- Generic `PENDING MANUAL` **không block phần code độc lập còn lại**.
- Nhưng nếu story/packet đánh dấu một dependency là `ENTRY GATE`, `HARD GATE` hoặc điều kiện mở packet downstream,
  thì `PENDING` = `BLOCKED`; **không implement downstream**.
- Story/packet cụ thể thắng rule generic về dependency.

Canonical execution state:

`PLANNED / LOCKED / EXECUTABLE / IMPLEMENTING / IMPLEMENTED / VERIFYING / BLOCKED / DONE / SUPERSEDED`.

`IMPLEMENTED != DONE`. Worker có thể report `IMPLEMENTED`; ROADMAP chỉ ghi `DONE` khi closure gate/evidence required đã PASS.

## 8. Definition of Done

- Compile pass, console sạch (không error/warning mới do story gây ra).
- Test đã định nghĩa pass — hoặc ghi rõ lý do chưa chạy được, **không claim pass**.
- Acceptance criteria đánh dấu **kèm evidence**: log · screenshot · số đo · tên test.
- `implementation-notes.html` đã cập nhật.
- Project-level decision đã vào `Docs/decision-log.md`.
- Summary: files changed · architecture · performance · deviations · open risks.
- **Harvest**: pattern nào generic → đề xuất đẩy về `knowledge/` hoặc skill (`workflow/harvest.md`).

## 9. Stack mặc định

Chỉ ghi cái khác mặc định vào `Docs/project-context.md`.

| Thứ | Mặc định |
|---|---|
| Async | UniTask (không dùng `IEnumerator` coroutine cho logic mới) |
| Tween | DOTween — hoặc UniTask + lerp nếu assembly không reference được |
| Pool | `VTLTools.ObjectPool` |
| Text trong world/HUD | TextMeshPro, qua **prefab riêng có script quản lý** (§`code-style` §7) |
| Inspector | Odin |
| Level data | JSON (`JsonUtility`) trong `Resources/Levels/` |
| Config runtime | ScriptableObject + `ResourceAsset<T>` |
| Editor tool | UI Toolkit (UXML + USS + partial C#) |
| Test | Unity Test Framework — EditMode cho rule, PlayMode cho lifecycle |

## 10. Project overrides

<!-- Điền phần riêng của project này. Rỗng = dùng nguyên bản generic. -->

- (chưa có)
