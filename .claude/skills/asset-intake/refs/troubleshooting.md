# Loi hay gap

## Brief

**`BRIEF INVALID: brief.<field>: ...`** (exit 2)
Validator bao dung cho. Sua brief, khong sua validator.
Hay gap nhat: `id` viet PascalCase; thieu field bat buoc; them field khong co trong schema
(brief la `additionalProperties: false` — co y, de nao khong tu bia field moi).

## Import

**`file khong chua mesh object nao`**
File chi co empty/curve/light. Curve chua convert sang mesh. Xin lai file da convert.

**`format khong ho tro`**
Nhan `.obj` `.fbx` `.glb` `.gltf` `.blend`. Nhan `.stl` tu dau → xin lai, STL khong co UV va
khong co material slot nen se fail hai check ngay.

**Nhieu object trong file** → pipeline tu join, ghi `join_N_objects` vao `fixes_applied`.
Neu asset that su can nhieu part rieng thi day la dau hieu brief sai: tach thanh nhieu brief.

## Check fail

**`tri_count` vuot nhieu lan budget**
`decimate_to_budget` khong phai cach chua. Vuot > 2 lan → lam lai mesh. Decimate manh pha
silhouette, va no pha am tham — check se pass trong khi hinh da hong.

**`bounds_m` fail du da scale**
Pipeline scale **dong deu**. Fail sau khi scale nghia la **ti le hinh sai**, khong phai kich thuoc sai.
Vi du brief muon `0.4 × 0.1 × 0.3` ma mesh la khoi lap phuong → khong co he so nao khop ca ba truc.
Sua brief neu ti le brief sai; lam lai mesh neu mesh sai.

**`ngon` > 0**
Mesh tu sandbox hay co ngon vi sinh bang code. Worker mo trong Blender, triangulate hoac
sua topology. Hoac dat `category: blockout` + `ngon_allowed: true` neu day chi la placeholder.

**`uv_channels` fail**
Mesh khong co UV. Tu sinh UV bang smart project se cho ket qua xau cho asset that.
Blockout thi chap nhan duoc; asset that thi xin lai tu nguon co UV.

**`pivot_position` fail**
Hiem, vi pipeline tu dat pivot. Fail o day thuong la mesh co transform la ma apply khong het —
kiem `fixes_applied` xem `apply_rotation_scale` co chay khong.

**`scale_applied` fail**
Tuong tu. Neu thay thi la bug pipeline, khong phai loi mesh — bao dev.

## Export

**File vao `Assets/` nhung Unity import sai rotation**
Kiem `bake_space_transform` — phai la `False`. Bat len la nguyen nhan pho bien nhat cua
"model nam nghieng trong Unity".

**Import settings khong duoc ap**
`AssetIntakePostprocessor.cs` chua co trong project, hoac sidecar `.import.json` khong nam
canh file. Chay lai `bin/setup.py`.

**Ghi de file da co**
Pipeline khong hoi truoc khi ghi de. Neu lo ghi de asset cua artist → lay lai tu git.
Day la ly do `Assets/` phai nam trong version control truoc khi chay pipeline lan dau.

## Render

**Report khong co render**
La bug, khong phai thieu sot nho (D-006). Kiem `errors` trong report — thuong la pipeline
chet truoc buoc render.

**Render toi den hoac trang xoa**
Engine la `BLENDER_WORKBENCH`, khong dung material cua scene. Render trang xoa thuong la
mesh qua nho hoac qua lon so voi camera framing — kiem `bounds_m` trong report truoc.
