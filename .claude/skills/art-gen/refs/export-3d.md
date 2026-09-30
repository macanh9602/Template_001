# Export 3D — three.js trong HTML → `.glb` → asset-intake

> Geometry chỉ sống ở **một nơi**: hàm `buildXxx()` trong HTML. Unity **không** gen lại mesh từ data
> cho `prop_3d`. `.glb` đi vào `skills/asset-intake/` (Blender validate → FBX, D-005).

## 1. Contract export (áp cho mọi `.glb`)

| Mục | Quy ước |
|---|---|
| Đơn vị | 1 unit three.js = 1 m |
| Up / forward | +Y up, mặt trước nhìn về +Z (glTF chuẩn). asset-intake lo đổi hệ trục sang Unity |
| Pivot | tâm đáy (`bottom_center`), trừ `modular_3d` part: pivot tại khớp nối (`custom`) |
| Transform | apply hết vào geometry; node root identity |
| Mesh | merge toàn bộ → **một mesh, một material slot**, tên node = `id` |
| Màu | **palette texture chung**: mỗi màu trong `ART.palette` là một ô; UV của từng phần trỏ vào ô màu của nó. Một material `<prefix>_mat_palette` cho mọi prop → batch tốt, reskin = đổi palette |
| Loại bỏ | light, camera, helper, shadow disc, InstancedMesh (hạt), material transparent/physical |
| Budget | `tri_max` từ manifest; export page tự đếm tri và cảnh báo trước khi tải |

Vì sao palette mà không nhiều material: asset-intake mặc định `material_slots_max: 1` cho mobile casual
(mỗi slot thêm = một draw call). Vertex color thì URP Lit không hiển thị nếu không viết shader riêng.

Glass/`MeshPhysicalMaterial`/transparent **không** export — đó là `shader` → OUT.

## 2. `modular_3d`

- Mỗi part một `.glb`: `pp_part_pipe_cap.glb`, `pp_part_pipe_body.glb`…
- Part `stretch` phải là thân **thẳng, không bevel theo trục stretch** (cylinder, box thẳng), chiều dài
  chuẩn 1 m theo `axis`. Cap/đầu nối giữ kích thước cố định.
- Manifest ghi `assembly` (`stretch_part`, `axis`, `driven_by`). Unity assembler là **story dev riêng**,
  đọc level data → đặt cap + scale body. Không nằm trong skill này.

## 3. Nút export trong HTML (snippet — model thêm vào, GD không cần viết)

```js
// Chỉ bật khi ?export=art — không ảnh hưởng gameplay.
import {GLTFExporter} from 'three/addons/exporters/GLTFExporter.js';
import {mergeGeometries} from 'three/addons/utils/BufferGeometryUtils.js';

// ART.palette: { key: '#hex', ... } — thứ tự key = thứ tự ô trong palette texture (N×1).
const PAL_KEYS = Object.keys(ART.palette);

function paletteTexture() {            // xuất 1 lần: <prefix>_tex_palette.png
  const c = document.createElement('canvas'); c.width = PAL_KEYS.length * 8; c.height = 8;
  const g = c.getContext('2d');
  PAL_KEYS.forEach((k, i) => { g.fillStyle = ART.palette[k]; g.fillRect(i * 8, 0, 8, 8); });
  return c;
}

// items: [{ id, build: () => THREE.Object3D }] — build dựng element ở gốc toạ độ, mỗi mesh có
// material.name = key trong ART.palette. Không đụng scene game.
async function exportArt(items, prefix) {
  const exporter = new GLTFExporter();
  const palTex = new THREE.CanvasTexture(paletteTexture());
  palTex.magFilter = palTex.minFilter = THREE.NearestFilter;
  const palMat = new THREE.MeshStandardMaterial({name: `${prefix}_mat_palette`, map: palTex});
  for (const {id, build} of items) {
    const root = build(); root.updateMatrixWorld(true);
    const geos = [];
    root.traverse(o => {
      if (!o.isMesh || o.isInstancedMesh) return;
      const idx = PAL_KEYS.indexOf(o.material.name);
      if (idx < 0) { console.warn(`[art-export] ${id}: material '${o.material.name}' không có trong ART.palette`); return; }
      let g = o.geometry.clone().applyMatrix4(o.matrixWorld);
      g = g.index ? g.toNonIndexed() : g;
      for (const k of Object.keys(g.attributes)) if (!['position', 'normal'].includes(k)) g.deleteAttribute(k);
      const n = g.attributes.position.count, u = (idx + 0.5) / PAL_KEYS.length;
      g.setAttribute('uv', new THREE.Float32BufferAttribute(new Float32Array(n * 2).map((_, i) => i % 2 ? 0.5 : u), 2));
      geos.push(g);
    });
    const mesh = new THREE.Mesh(mergeGeometries(geos), palMat); mesh.name = id;
    const tris = mesh.geometry.attributes.position.count / 3;
    const glb = await exporter.parseAsync(mesh, {binary: true});
    const a = document.createElement('a');
    a.href = URL.createObjectURL(new Blob([glb], {type: 'model/gltf-binary'}));
    a.download = `${id}.glb`; a.click();
    console.log(`[art-export] ${id}: ${tris} tris`);
  }
}
```

Mesh non-indexed sau merge → asset-intake (Blender) merge-by-distance và tính lại normal.
Normal phẳng/mượt là quyết định style, ghi trong `desc`. Snippet là điểm xuất phát, verify bằng một
asset thật trước khi áp hàng loạt.

## 4. Handoff sang asset-intake

Với mỗi `.glb`, sinh `asset-brief.json` từ manifest (field theo `asset-intake/refs/brief-authoring.md`):

- `id` ← manifest `id`
- `source` ← `worker` (schema hiện chỉ có `gpt-web · worker · artist`; muốn thống kê riêng thì thêm
  `html-threejs` vào schema trước — sửa schema là quyết định riêng)
- `constraints.tri_max` ← `tri_max` · `constraints.bounds_m` ← `bounds_m`
- `constraints.pivot` ← `bottom_center` (prop) / `custom` + `pivot_custom` (part)
- `constraints.material_slots_max` ← `1`
- `shape.description` ← manifest `desc`

Rồi chạy theo `skills/asset-intake/SKILL.md §2`. Report `fail` + `remake_mesh` → sửa `buildXxx()`
trong HTML và export lại, **không sửa mesh trong Blender** (nguồn phải ở một nơi).

Palette texture (`<prefix>_tex_palette.png`) vào `Assets/_Core/0_Texture2D/`, material
`<prefix>_mat_palette` (URP Lit/toon của project) tạo một lần, mọi prop dùng chung.

## 5. `rig_later: true`

Export như bình thường nhưng ghi vào report: *"reference hình khối/tỉ lệ, topology ghép primitive —
bên rig retopo hoặc dựng lại"*. Thống nhất với bên rig trước khi gửi.
