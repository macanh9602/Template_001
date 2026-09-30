# Viet `asset-brief.json`

> Brief la **hop dong giua nao va tool**, khong phai giua nao va nguoi.
> Nao sinh JSON, khong sinh van xuoi.

## Nguyen tac chon field

Cau hoi duy nhat: **may kiem duoc khong?**

| Kiem duoc bang may | Khong kiem duoc |
|---|---|
| so tam giac, kich thuoc, so material slot, vi tri pivot, ngon, UV channel, ten | "trong giong khay banh", "bo goc mem mai", "ti le can doi" |
| → `constraints` | → `shape.description` |

Nhet mot thu khong kiem duoc vao `constraints` se lam pipeline fail voi ly do vo nghia.
Nhet mot thu kiem duoc vao `shape` se lam no lot luoi.

---

## Tung field

### `id`
snake_case, `^[a-z][a-z0-9_]*$`. Cung la ten object trong Blender va ten file export.
Theo `standards/folder-structure.md §5` — prefix cua game lay tu `Docs/project-context.md`.

### `source`
`gpt-web` · `worker` · `artist`. Chi de thong ke, khong doi logic pipeline.
Nhung no la thu duy nhat cho biet sau nay asset nao hay fail nhat den tu dau.

### `constraints.tri_max`
Lay tu `standards/performance-budget.md`. **Khong bia.** Chua co con so cho loai asset nay
→ hoi dev bang trac nghiem.

Nho: budget cua mot asset khong phai budget cua man hinh. 30 prop × 500 tri = 15k tri,
cong voi board va UI. Neu khong chac, hoi.

### `constraints.bounds_m`
Met, world space, **kich thuoc mong muon** — khong phai kich thuoc file dau vao.
Pipeline se scale dong deu de khop. Scale dong deu nghia la neu ti le hinh sai so voi brief
thi khong truc nao khop hoan toan → check `bounds_m` fail, va do la dung: hinh sai ti le that.

### `constraints.bounds_tolerance`
Ti le, `0.10` = cho lech 10% moi truc. Dat chat qua thi asset stylized se fail lien tuc;
dat long qua thi asset vao game sai kich thuoc ma khong ai biet. `0.10` la diem bat dau hop ly.

### `constraints.pivot`
- `bottom_center` — prop dat tren mat phang. Mac dinh cho hau het prop.
- `center` — object xoay quanh tam, hoac bay.
- `custom` — phai co `pivot_custom` la toa do 3 so.

Pivot sai la loi ton thoi gian nhat ve sau: moi prefab dung asset do deu phai bu offset bang tay,
va con so bu do se bi hardcode trong code. Dung de lot.

### `constraints.material_slots_max`
Mobile casual: thuong la `1`. Moi slot them la mot draw call them.
Xem `standards/performance-budget.md §1` — draw call gameplay <= 60.

### `constraints.uv_channels`
`["UV0"]` la toi thieu. Can lightmap thi `["UV0","UV1"]`.
Mesh khong co UV se pass moi check khac roi chet luc gan material.

### `constraints.ngon_allowed`
`false` cho asset that. `true` chi cho `category: blockout`.
Ngon lam Unity triangulate theo cach khong doan truoc duoc → shading loang lo.

### `shape.description`
Viet cho **nguoi doc de doi chieu voi render**, khong phai cho may.
Mot hai cau, mo ta silhouette va do chi tiet. Dung ke so lieu o day — so lieu thuoc `constraints`.

### `optimize.decimate_to_budget`
`true` = vuot `tri_max` thi tu decimate thay vi fail.

Tradeoff that: decimate cuu duoc asset vuot budget it, nhung vuot nhieu thi no pha silhouette
va ban chi biet khi nhin render. Vuot > 2 lan budget → de fail, lam lai mesh, dung decimate.

### `export.target_dir`
Relative toi Unity project root. Theo `standards/folder-structure.md §2`:
noi dung cua game nam trong `Assets/_Core/`, khong phai `Assets/`.

---

## Bay thuong gap

**Dat `bounds_m` theo file dau vao thay vi theo game.**
GPT web hay sinh mesh co kich thuoc tuy y. Brief phai ghi kich thuoc **trong game**, bang met.
Mot khay banh la `0.4 × 0.1 × 0.3`, khong phai `40 × 10 × 30`.

**Quen `scale_unit`.**
Blender mac dinh 1 unit = 1 m, Unity cung vay. Nhung file `.obj` tu sandbox thuong khong co
don vi → pipeline dua ve dung `bounds_m`, va do la ly do `bounds_m` bat buoc.

**Dat `tri_max` bang dung so tri cua mesh hien co.**
The la brief dang mo ta cai da co, khong phai cai can co. Budget den tu performance, khong tu mesh.

**Viet mot brief cho nhieu asset.**
Mot brief = mot asset = mot file export. Bien the thi `category: variant`, brief rieng, `id` rieng.
