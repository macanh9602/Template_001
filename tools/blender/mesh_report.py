# Bao cao mesh cho verify script (0 token): blender -b --factory-startup -P tools/blender/mesh_report.py -- --mesh <file>
# Doc .fbx/.glb/.gltf/.obj/.blend -> in mot dong AGENTPACK_REPORT {json}: objects, tris, verts, materials,
# bounds (min/max/size, don vi file), pivotBottom (min z ~ 0: pivot o day, luat asset-intake).
# So lieu thuc te; nguong (maxTris, maxMaterials...) nam o task.verify, khong o day.
import json
import os
import sys

import bpy
from mathutils import Vector


def arg(name):
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    for i, a in enumerate(argv):
        if a == name and i + 1 < len(argv):
            return argv[i + 1]
    return None


def load(path):
    ext = os.path.splitext(path)[1].lower()
    if ext == ".blend":
        bpy.ops.wm.open_mainfile(filepath=path)
        return
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete()
    if ext == ".fbx":
        bpy.ops.import_scene.fbx(filepath=path)
    elif ext in (".glb", ".gltf"):
        bpy.ops.import_scene.gltf(filepath=path)
    elif ext == ".obj":
        # Blender 4.x: wm.obj_import; ban cu: import_scene.obj
        if hasattr(bpy.ops.wm, "obj_import"):
            bpy.ops.wm.obj_import(filepath=path)
        else:
            bpy.ops.import_scene.obj(filepath=path)
    else:
        raise ValueError("khong ho tro " + ext)


def main():
    path = arg("--mesh")
    report = {"ok": False, "mesh": path}
    try:
        if not path or not os.path.isfile(path):
            raise FileNotFoundError("khong thay file mesh: " + str(path))
        load(path)
        meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
        tris = verts = 0
        mats = set()
        lo = Vector((1e30, 1e30, 1e30))
        hi = Vector((-1e30, -1e30, -1e30))
        for ob in meshes:
            me = ob.data
            me.calc_loop_triangles()
            tris += len(me.loop_triangles)
            verts += len(me.vertices)
            for slot in ob.material_slots:
                if slot.material:
                    mats.add(slot.material.name)
            for v in me.vertices:
                w = ob.matrix_world @ v.co
                lo = Vector((min(lo.x, w.x), min(lo.y, w.y), min(lo.z, w.z)))
                hi = Vector((max(hi.x, w.x), max(hi.y, w.y), max(hi.z, w.z)))
        size = hi - lo if meshes else Vector((0, 0, 0))
        report.update({
            "ok": bool(meshes),
            "blender": bpy.app.version_string,
            "objects": len(meshes),
            "tris": tris,
            "verts": verts,
            "materials": len(mats),
            "bounds": {"min": [round(c, 4) for c in lo], "max": [round(c, 4) for c in hi], "size": [round(c, 4) for c in size]} if meshes else None,
            "pivotBottom": bool(meshes) and abs(lo.z) < 1e-3,
        })
    except Exception as exc:  # noqa: BLE001
        report["error"] = str(exc)
    print("AGENTPACK_REPORT " + json.dumps(report))


main()
