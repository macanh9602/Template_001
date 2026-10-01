# P1 — Bootstrap / Adopt project

**Vào khi:** repo trống, mới có GDD, hoặc project Unity hiện hữu đang adopt Template.
**Ra khi:** project identity + architecture contract đã chốt, canonical folder/code root rõ ràng, Docs đủ để story sau không phải đoán lại, và chỉ **story kế tiếp** được materialize khi foundation gate đã PASS.

Mục tiêu không phải "có gameplay". Mục tiêu là **không tạo rework foundation** vì agent tự invent folder, assembly, scene ownership hoặc architecture trước khi hiểu project thật.

---

## Bước -2 — Phase A orchestration (khi có executable prototype)

Có `.html` prototype / input executable ⇒ Bước -1 tới Bước 4A chạy thành **một** Phase A có Director + Critic + PO song song:
`workflow/phase-a-contract-bootstrap.md`. Gate authority, conformance và foundation **giữ nguyên**; chỉ khác là
Critic review contract trước lock và PO trả lời một batch câu hỏi thay vì nhiều phiên tuần tự.

---

## Bước -1 — Classify product input / authoring source

Nếu đầu vào có executable prototype, external Level Editor, reference implementation, simulator/solver hoặc runtime cũ,
chạy `workflow/prototype-to-contract.md` **trước** khi worker Unity diễn giải source.

Chốt trong `Docs/project-context.md`:

- Product input.
- Level authoring mode.
- Authority theo từng area.
- Canonical exported level/data source.
- Conformance oracle/fixtures nếu executable logic tồn tại.

Không build Unity Level Editor chỉ vì GD cần làm level. Route authoring bằng `skills/authoring-pipeline/`.

---

## Bước 0 — Phân loại project trước khi tạo/sửa bất kỳ code nào

Chạy `handoff/PROJECT-READINESS-PROMPT.md`.

Chọn một mode:

| Mode | Khi dùng | Luật |
|---|---|---|
| `GREENFIELD` | Repo thực sự mới, chưa có code/folder/runtime ownership đáng giữ | Dùng default của Template |
| `EXISTING_PROJECT_ADOPTION` | Project đã có `Assets/`, scene, code, prefab, module, package hoặc flow runtime | **Inspect → harvest → confirm contract → chỉ xử lý adoption gaps** |

### Luật cứng cho `EXISTING_PROJECT_ADOPTION`

- Không tạo script root song song chỉ vì `standards/` có một path mặc định khác project thật.
- Không tạo asmdef mới trước khi audit assembly/dependency hiện tại.
- Không rewrite scene ownership nếu project đã có flow hợp lý mà chưa có decision đổi.
- Không "dọn" code cũ bằng namespace/folder migration hàng loạt trong bootstrap.
- Không materialize cả pack 10–15 story khi architecture/foundation chưa pass.
- Asset/reference ngoài workspace **không phải blocker mặc định**. Chỉ yêu cầu nếu user nói nó là source of truth bắt buộc.

Nếu project hiện hữu đã chứng minh một contract tốt, **giữ nó** và ghi vào `Docs/project-context.md`; Template thích nghi với project, không ép project reset theo skeleton.

---

## Bước 1 — Inventory + chốt Adoption Contract

Trước khi hỏi gameplay sâu, khảo sát có mục đích:

- Unity version + render pipeline.
- Script roots đang tồn tại.
- Prefab / scene / Resources roots.
- Assembly reality: `Assembly-CSharp`, asmdef nào đang tồn tại, dependency nào kéo theo.
- Runtime owners đang có: bootstrap, level lifecycle, spawner/factory, gameplay manager, input, HUD.
- Module reusable và code project cũ/game-specific.
- Source of truth của level/data hiện tại.
- Scene trong Build Settings và scene ownership.
- Placeholder/template remnants có thể gây nhầm identity.

Ghi kết quả vào `Docs/project-context.md §3` và `§8–§9`.

### Chỉ escalate những contract kéo theo toàn project

| Contract | Ví dụ |
|---|---|
| Canonical script root | giữ `Assets/_Core/4_Scripts` hay migrate có chủ đích |
| Namespace root | `SE001`, `CakeRoll`, ... |
| Assembly strategy | giữ `Assembly-CSharp` hay tách asmdef tại chỗ |
| Scene ownership | một `GameScene` hay bootstrap scene riêng |
| Data source of truth | JSON / SO / external authored data |
| Runtime authority | custom simulation / Rigidbody / hybrid |

Decision local/reversible thì agent tự quyết. Contract project-wide phải ghi `D-xxx`.

---

## Bước 2 — Đọc GDD, hỏi phần project chưa trả lời

Skill: `enrich-context`.

Hỏi theo batch trắc nghiệm, nhưng **không hỏi lại thứ inventory/GDD đã trả lời**.

| Nhóm | Cần chốt |
|---|---|
| Thể loại & vòng lặp | người chơi làm gì trong 30 giây đầu; thắng/thua là gì |
| Quy mô | bao nhiêu level; level do người làm hay sinh tự động |
| Element | các loại đối tượng chính; số lượng tối đa cùng lúc trên màn |
| Input | tap / drag / swipe / multi-touch; có gì làm cùng lúc không |
| Thiết bị chuẩn | máy nào là mốc để đo performance |
| Orientation & tỉ lệ | portrait/landscape; dải aspect ratio phải chịu |
| Meta | có progression / shop / booster không |
| Ràng buộc | deadline, team size, thứ bắt buộc dùng lại |

Trả lời rồi ⇒ ghi vào `Docs/project-context.md §7` để không hỏi lại.

---

## Bước 3 — Điền Docs theo thứ tự source-of-truth

1. `Docs/project-context.md`
   - project mode;
   - canonical roots;
   - namespace;
   - assembly reality/strategy;
   - scene ownership;
   - existing-code policy;
   - target device + performance contract.
2. `Docs/glossary.md` — tên element/khái niệm chính.
3. `Docs/data-model.md` — authoring source-of-truth / generated / runtime state.
4. `Docs/runtime-architecture.md` — map owner/layer vào class/module thật của project.
5. `Docs/decision-log.md` — ghi project-level decisions đã chốt.

### Gate

Nếu `project-context`, `runtime-architecture` và code/folder thật **mâu thuẫn nhau**, chưa được tạo foundation code mới. Sửa contract trước.

### Conformance gate

Nếu một executable prototype/oracle được đánh dấu `AUTHORITATIVE` hoặc `ORACLE`, tạo fixture critical theo
`templates/conformance-pack/` trước khi lower-reasoning worker implement semantic core.
Worker không được phải đọc toàn prototype để tự đoán expected behavior.

---

## Bước 4 — Folder + assembly: preserve-first

Theo `standards/folder-structure.md`.

### Greenfield

- Dùng canonical root mặc định của Template: `Assets/_Core/4_Scripts`.
- Tạo **chỉ folder có responsibility thật**; không dựng cây rỗng khổng lồ.
- Không pre-create asmdef nếu chưa có dependency reason.

### Existing project adoption

- Giữ script root hiện hữu nếu coherent và dev không yêu cầu migrate.
- Nếu muốn migrate root/folder/asmdef: viết decision riêng + migration story riêng.
- Không tạo root thứ hai để "code mới sạch hơn".
- Namespace và folder organization áp dụng **bên trong canonical root đã chốt**.

Checklist:

- [ ] Chỉ có **một canonical production script root**.
- [ ] Không có framework tree song song cùng trách nhiệm.
- [ ] Assembly strategy phản ánh dependency thật, không phải skeleton mong muốn.
- [ ] Namespace root đã lock trước file production đầu tiên.
- [ ] Existing reusable module được audit theo file/module, không copy gameplay semantics mù quáng.

---

## Bước 4A — Bootstrap Manifest + mechanical patch

Sau khi project/data/authority contract đã lock, frontier/planning layer lập `bootstrap-manifest.json`:

- add / replace / delete exact file;
- preserve user-authored/dirty data;
- scaffold/fixture/doc nào được generate;
- expected-before hash cho destructive operation khi hữu ích;
- verification sau apply.

Dùng `templates/project-bootstrap.ps1` hoặc generated project-specific `.ps1`.
Generated wrapper là **transient**: giữ tới khi diff + compile/review PASS, commit xong thì delete.
Giữ manifest/report nếu chúng giải thích project state.

Script chỉ làm mechanical work; không chứa architecture/product reasoning.

---

## Bước 5 — Foundation gap, không dựng framework phòng xa

Chỉ implement capability còn thiếu để chứng minh ownership:

```text
Bootstrap / Scene owner
    → Level lifecycle owner
    → composition/spawn owner
    → per-level context/state
    → một entity từ authored data
    → visual/HUD đọc state
```

Không bắt buộc tên class cụ thể. Không bắt project hiện hữu rewrite flow chỉ để khớp ví dụ.

Checklist:

- [ ] Một owner duy nhất cho level lifecycle.
- [ ] Một owner rõ cho composition/spawn/unload.
- [ ] Runtime state per-level, không global singleton gameplay state.
- [ ] Data/profile/prefab là nguồn tune; không hardcode.
- [ ] Load → reload → unload không leak root/context/event.
- [ ] Production startup không phụ thuộc smoke/debug auto-create ẩn.

---

## Bước 6 — Roadmap theo capability, materialize story lười

`handoff/ROADMAP.md` được phép mô tả **phase/capability** phía trước, nhưng:

- Chỉ story gần nhất sau gate hiện tại được viết thành execution spec đầy đủ.
- Story N+1 chưa materialize nếu Story N là architecture/foundation gate chưa PASS.
- Không tạo sẵn 10–15 story chi tiết dựa trên architecture còn chưa verify.
- Khi một project-level architecture decision đổi, update roadmap capability map; không phải xóa hàng nghìn dòng story đã viết sẵn.

Cuối bootstrap:

- [ ] `handoff/PROJECT-READINESS-PROMPT.md` trả `READY FOR ACTIVE STORY: YES`.
- [ ] `handoff/ROADMAP.md` có phase/capability + dependency gate.
- [ ] Chỉ story executable kế tiếp được materialize.
- [ ] `START-PROMPT.md` / `RUN-STORY-PROMPT.md` trỏ đúng project.

---

## Xong khi

- [ ] Canonical production roots khớp giữa repo thật và Docs.
- [ ] Không có script/framework tree song song ngoài migration có chủ đích.
- [ ] Assembly strategy đã chốt từ dependency thật.
- [ ] Scene ownership rõ.
- [ ] Data source-of-truth rõ.
- [ ] Runtime ownership đủ để feature story sau không tự invent lifecycle.
- [ ] Compile/console baseline biết rõ trạng thái.
- [ ] Roadmap chỉ materialize story kế tiếp.
- [ ] Không còn open question dạng blocker.

## Bẫy đã trả giá

| Bẫy | Hậu quả |
|---|---|
| standard path khác project thật nhưng agent vẫn tạo root mới | hai framework song song, phải port ngược |
| tách asmdef trước khi audit Assembly-CSharp/shared modules | dependency blast radius, rework hàng loạt |
| architecture source-of-truth xuất hiện sau foundation implementation | sửa ownership + scene + docs lần hai |
| materialize 10–15 story trước architecture gate | xóa/rewrite hàng nghìn dòng handoff |
| coi asset/reference ngoài workspace là blocker mặc định | mất thời gian xin/copy dữ liệu không cần thiết |
| "dọn legacy" bằng search/replace namespace hàng loạt | resurrect gameplay semantics project cũ |
