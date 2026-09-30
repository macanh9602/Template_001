# Blender headless smoke (tools/doctor.ps1 -> BLENDER_EXPORT_SMOKE). 0 token, deterministic.
# blender -b --factory-startup -P tools/blender/smoke_export.py -- --out <dir>
# Tao cube -> export FBX (duong vao Unity) + GLB -> import lai tung file, dem tris.
# In mot dong: AGENTPACK_SMOKE {"ok": true, ...}. Operator doi ten giua cac version -> kiem hasattr.
import json
import os
import sys

import bpy


def args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    out = None
    for i, a in enumerate(argv):
        if a == "--out" and i + 1 < len(argv):
            out = argv[i + 1]
    return out or os.path.join(bpy.app.tempdir or ".", "agentpack_smoke")


def clear():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()


def tris():
    total = 0
    for ob in bpy.context.scene.objects:
        if ob.type == "MESH":
            me = ob.data
            me.calc_loop_triangles()
            total += len(me.loop_triangles)
    return total


def main():
    out = args()
    os.makedirs(out, exist_ok=True)
    result = {"ok": False, "blender": bpy.app.version_string, "exports": {}}
    clear()
    bpy.ops.mesh.primitive_cube_add(size=1.0)
    bpy.context.active_object.name = "agentpack_smoke_cube"
    exporters = {
        "fbx": ("export_scene", "fbx", {"use_selection": False, "bake_space_transform": False}),
        "glb": ("export_scene", "gltf", {"export_format": "GLB"}),
    }
    importers = {"fbx": ("import_scene", "fbx"), "glb": ("import_scene", "gltf")}
    for ext, (mod, op, kw) in exporters.items():
        path = os.path.join(out, "agentpack_smoke." + ext)
        entry = {"path": path, "exported": False, "bytes": 0, "reimportTris": 0}
        try:
            fn = getattr(getattr(bpy.ops, mod), op)
            fn(filepath=path, **kw)
            entry["exported"] = os.path.isfile(path)
            entry["bytes"] = os.path.getsize(path) if entry["exported"] else 0
            imod, iop = importers[ext]
            clear()
            getattr(getattr(bpy.ops, imod), iop)(filepath=path)
            entry["reimportTris"] = tris()
            clear()
            bpy.ops.mesh.primitive_cube_add(size=1.0)
        except Exception as exc:  # noqa: BLE001 - bao loi ra report, khong crash smoke
            entry["error"] = str(exc)
        result["exports"][ext] = entry
    result["ok"] = all(e["exported"] and e["bytes"] > 0 and e["reimportTris"] == 12 for e in result["exports"].values())
    print("AGENTPACK_SMOKE " + json.dumps(result))


main()
