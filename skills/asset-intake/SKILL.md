---
name: asset-intake
description: >
  Dua mesh tu bat ky nguon nao (GPT web sinh, worker dung trong Blender, artist giao) ve asset
  dat chuan project va ghi vao Assets/. KICH HOAT khi: "co model roi, dua vao Unity", "asset nay
  sai scale/pivot", "kiem tra model co dat budget khong", "export tu Blender sang Unity", hoac khi
  vua nhan mot file .obj/.glb/.fbx/.blend tu bat ky dau. KHONG dung de tao mesh moi.
---

# SKILL: asset-intake

Tool nay **khong sinh mesh**. No nhan mesh da co va dua ve chuan, validate, export, report.
Khau sinh mesh nam ngoai — xem bang chon nguon ben duoi.

Output la **mot `intake-report.json` + render**, khong phai mot cau "da xong".

---

## Buoc dau tien — doc context

`Docs/project-context.md` (lay `[GameRoot]`, prefix ten) ·
`standards/folder-structure.md §2, §5` (asset di vao dau, dat ten the nao) ·
`standards/performance-budget.md §1` (tri/draw call budget) ·
`Docs/asset-pipeline.json` (config do setup sinh ra: duong dan pipeline, Blender, defaults).

Khong hoi lai thu cac file do da tra loi.

---

## Chon nguon mesh — quyet dinh tien, hoi dev neu chua ro

| Loai asset | Nguon | Vi sao |
|---|---|---|
| Parametric / hard-surface (khay, hop, tru, khoi) | GPT web sinh `.obj` | Mo ta duoc bang so, khong can nhin |
| Props stylized co silhouette | Worker + blender-mcp | Can vong render–sua |
| Organic / character | Artist | LLM lam kem, dung ep |
| Bien the tu base da co | Worker | Base da chuan, chi can modifier |

**GPT web khong render duoc.** Sandbox cua no khong co viewport, nen model sinh ra khong ai
nhin thay truoc khi tai ve. No thang o luot dau, thua tu luot 3 tro di. Vuot qua 2 luot sua ma
van sai hinh → chuyen sang worker, dung co dam.

**Khong nhan `.fbx` do GPT web sinh.** Sandbox khong co FBX SDK → do la FBX ASCII viet tay,
Unity import hay lech axis va mat smoothing group. Xin lai `.obj` hoac `.glb`. Blender la khau
export FBX duy nhat. (D-005)

---

## Quy trinh

### 1. Viet `asset-brief.json`

Schema day du: `schema/asset-brief.schema.json`. Vi du: `examples/asset-brief.example.json`.
Chi tiet tung field va bay thuong gap: `refs/brief-authoring.md`.

Luat quan trong: **constraint nao khong validate duoc bang may thi khong dat trong
`constraints`** — dat trong `shape.description`, va no thuoc phan nguoi duyet bang mat.

`tri_max` lay tu `standards/performance-budget.md`, khong bia. Chua co con so cho loai asset nay
→ hoi dev bang trac nghiem, dung tu chon.

### 2. Chay pipeline

```
blender -b -P <pipeline>/pipeline/run.py -- \
    --brief <brief>.json \
    --mesh <mesh dau vao> \
    --project-root <Unity project root>
```

Them `--no-export` de chi validate + render, chua ghi vao `Assets/`.

Exit code: `0` = pass/warn · `1` = fail · `2` = brief sai hoac khong tim thay file.

### 3. Doc report, khong doc console

`runs/<run_id>_<brief_id>/intake-report.json` + hai file render.

| `status` | Nghia | Lam gi |
|---|---|---|
| `pass` | Khong con loi ky thuat | Dua render cho dev duyet tham my |
| `warn` | Da tu sua nhung lech so voi brief | Dua render cho dev, noi ro da sua gi |
| `fail` | Vi pham constraint, khong tu sua duoc | Theo `next_action` |

`next_action`:
- `fix_brief` — constraint dat sai (vi du `bounds_m` khong khop hinh that). Sua brief, chay lai.
- `remake_mesh` — mesh hong that (tri vuot xa, ngon, loose geometry). Quay lai khau sinh.

**`pass` khong co nghia la asset dep.** No chi co nghia la khong con loi ky thuat de ban.
Tham my luon do dev duyet qua render. Dung bao "da xong" khi chua ai nhin. (D-006)

### 4. Sau khi vao `Assets/`

Pipeline ghi them sidecar `<name>.fbx.import.json`. `AssetIntakePostprocessor.cs` trong project
doc file do va set import settings. Khong sua `.meta` bang tay.

Model moi vao chua phai prefab. Prefab contract nam o `standards/folder-structure.md §6`:
logic o root, visual o child `View/`. Placeholder cung la prefab that, khong `CreatePrimitive`.

---

## Khong lam gi

- Khong sinh mesh trong skill nay.
- Khong sua file goc cua artist — moi output la file moi.
- Khong ghi de file da co trong `Assets/` ma khong bao trong report.
- Khong tu them check moi vao code. Them check = sua `docs/data-model.md` truoc.
- Khong tu noi rong `constraints` khi mesh khong dat. Do la quyet dinh cua dev.

---

## Refs

- `refs/brief-authoring.md` — viet brief, tung field, bay thuong gap
- `refs/troubleshooting.md` — loi hay gap khi import/export va cach doc
