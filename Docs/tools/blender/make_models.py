"""
PawStorm placeholder-art generator (stylized low-poly primitives, rigged + animated).

Run (project root):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --python Docs/tools/blender/make_models.py
Optional env vars:
  PAWSTORM_OUT      output dir for .glb  (default: <project>/Art/Models)
  PAWSTORM_PREVIEW  dir for preview PNGs (rest views + one pose render per clip)
  PAWSTORM_ONLY     comma list of model names to build

Conventions: Blender Z up, model FRONT faces +Y (exports to glTF -Z = Godot forward), origin at the feet.
Rigging: every part is rigidly weighted (weight 1) to ONE bone, then all parts are joined into one skinned mesh.
Bones point up (+Z): pose rotation X = pitch (about global X), Y = yaw (about global Z), Z = roll;
pose location (x, y, z) = (side, up, -forward) in bone space.
Clips are pushed to NLA tracks and exported as separate glTF animations; names ending in "-loop" loop in Godot.
"""
import math
import os

import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.environ.get("PAWSTORM_OUT", os.path.join(ROOT, "Art", "Models"))
PREVIEW = os.environ.get("PAWSTORM_PREVIEW")
ONLY = [s for s in os.environ.get("PAWSTORM_ONLY", "").split(",") if s]
FPS = 24

# ---- palette (sRGB, converted to linear for glTF) ----
ORANGE = (1.0, 0.55, 0.15)
CREAM = (1.0, 0.9, 0.7)
PINK = (1.0, 0.5, 0.6)
BLUE = (0.2, 0.5, 1.0)
BLACK = (0.06, 0.06, 0.09)
WHITE = (1.0, 1.0, 1.0)
BROWN = (0.55, 0.35, 0.2)
DKBROWN = (0.35, 0.22, 0.15)
TAN = (0.85, 0.7, 0.5)
RED = (1.0, 0.15, 0.15)
GREY = (0.6, 0.62, 0.7)
YELLOW = (1.0, 0.85, 0.2)

S = math.sin
PI = math.pi


def lin(c):
    return tuple(ch ** 2.2 for ch in c)


_mat_cache = {}


def mat(name, rgb, emissive=0.0, rough=0.85):
    key = (name, rgb, emissive)
    if key in _mat_cache and _mat_cache[key].name in bpy.data.materials:
        return _mat_cache[key]
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    nt = m.node_tree
    nt.nodes.clear()
    out = nt.nodes.new("ShaderNodeOutputMaterial")
    bsdf = nt.nodes.new("ShaderNodeBsdfPrincipled")
    c = lin(rgb) + (1.0,)
    bsdf.inputs["Base Color"].default_value = c
    bsdf.inputs["Roughness"].default_value = rough
    if emissive > 0:
        bsdf.inputs["Emission Color"].default_value = c
        bsdf.inputs["Emission Strength"].default_value = emissive
    nt.links.new(bsdf.outputs["BSDF"], out.inputs["Surface"])
    m.diffuse_color = c
    _mat_cache[key] = m
    return m


def reset():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    _mat_cache.clear()


def add(kind, loc, m, scale=(1, 1, 1), rot=(0, 0, 0), smooth=True, bone=None, **kw):
    r = kw.get("r", 1.0)
    d = kw.get("d", 1.0)
    if kind == "sphere":
        bpy.ops.mesh.primitive_uv_sphere_add(segments=kw.get("seg", 16), ring_count=kw.get("rings", 10), radius=r, location=loc)
    elif kind == "ico":
        bpy.ops.mesh.primitive_ico_sphere_add(subdivisions=kw.get("sub", 1), radius=r, location=loc)
    elif kind == "cyl":
        bpy.ops.mesh.primitive_cylinder_add(vertices=kw.get("seg", 12), radius=r, depth=d, location=loc)
    elif kind == "cone":
        bpy.ops.mesh.primitive_cone_add(vertices=kw.get("seg", 8), radius1=r, radius2=kw.get("r2", 0.0), depth=d, location=loc)
    elif kind == "cube":
        bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc)
    elif kind == "torus":
        bpy.ops.mesh.primitive_torus_add(major_radius=r, minor_radius=kw.get("minor", 0.05),
                                         major_segments=kw.get("seg", 24), minor_segments=kw.get("mseg", 8), location=loc)
    else:
        raise ValueError(kind)
    o = bpy.context.active_object
    o.scale = scale
    o.rotation_euler = rot
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    o.data.materials.append(m)
    if smooth:
        bpy.ops.object.shade_smooth()
    if bone:
        vg = o.vertex_groups.new(name=bone)
        vg.add(list(range(len(o.data.vertices))), 1.0, "REPLACE")
    return o


def mirror_x(fn):
    """Call fn(sign) for the left (-1) and right (+1) side."""
    for s in (-1, 1):
        fn(s)


def side_name(s):
    return "L" if s < 0 else "R"


# ---------------- rigging / animation ----------------

def build_armature(name, bones):
    """bones: list of (bone_name, head_xyz, parent_name|None). Every bone points up, 0.15 long."""
    bpy.ops.object.armature_add(enter_editmode=True, location=(0, 0, 0))
    arm_obj = bpy.context.active_object
    arm_obj.name = name + "_Rig"
    arm = arm_obj.data
    arm.name = name + "_Rig"
    for b in list(arm.edit_bones):
        arm.edit_bones.remove(b)
    for bn, head, parent in bones:
        eb = arm.edit_bones.new(bn)
        eb.head = Vector(head)
        eb.tail = Vector(head) + Vector((0, 0, 0.15))
        if parent:
            eb.parent = arm.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    for pb in arm_obj.pose.bones:
        pb.rotation_mode = "XYZ"
    return arm_obj


def add_clip(arm_obj, bone_names, name, length, fn, loop=True, step=2, snap_frame=None):
    """Sample fn(t) (t in 0..1) -> {bone: {'r': (x,y,z), 'l': (x,y,z)}} and push the clip to an NLA track."""
    ad = arm_obj.animation_data_create()
    act = bpy.data.actions.new(name)
    ad.action = act
    frames = list(range(0, length + 1, step))
    if frames[-1] != length:
        frames.append(length)
    for f in frames:
        pose = fn(f / length)
        for bn in bone_names:
            pb = arm_obj.pose.bones[bn]
            d = pose.get(bn, {})
            pb.rotation_euler = d.get("r", (0, 0, 0))
            pb.location = d.get("l", (0, 0, 0))
            pb.keyframe_insert("rotation_euler", frame=f + 1)
            pb.keyframe_insert("location", frame=f + 1)
    if PREVIEW and snap_frame is not None:
        bpy.context.scene.frame_set(snap_frame + 1)
        render_view(f"{arm_obj.name}_{name}", "front34")
        bpy.context.scene.frame_set(1)
    track = ad.nla_tracks.new()
    track.name = name + ("-loop" if loop else "")
    track.strips.new(track.name, 1, act)
    ad.action = None
    for pb in arm_obj.pose.bones:
        pb.rotation_euler = (0, 0, 0)
        pb.location = (0, 0, 0)


_preview_state = {}


def setup_preview(obj):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.render.resolution_x = sc.render.resolution_y = 480
    sh = sc.display.shading
    sh.light = "STUDIO"
    sh.color_type = "MATERIAL"
    sh.show_object_outline = True
    sh.object_outline_color = (0.05, 0.02, 0.1)
    sc.world = bpy.data.worlds.new("w")
    sc.world.color = (0.55, 0.75, 0.95)
    bb = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    lo = Vector((min(v.x for v in bb), min(v.y for v in bb), min(v.z for v in bb)))
    hi = Vector((max(v.x for v in bb), max(v.y for v in bb), max(v.z for v in bb)))
    cam_data = bpy.data.cameras.new("cam")
    cam_data.lens = 50
    cam = bpy.data.objects.new("cam", cam_data)
    sc.collection.objects.link(cam)
    sc.camera = cam
    _preview_state.update(center=(lo + hi) / 2, size=max((hi - lo).length, 0.5), cam=cam)
    os.makedirs(PREVIEW, exist_ok=True)


def render_view(label, view):
    st = _preview_state
    offs = {"front34": Vector((0.8, 1.15, 0.45)), "back34": Vector((-0.9, -1.1, 0.5)), "side": Vector((1.0, 0.05, 0.25))}
    cam = st["cam"]
    cam.location = st["center"] + offs[view].normalized() * st["size"] * 1.35
    cam.rotation_euler = (st["center"] - cam.location).to_track_quat("-Z", "Y").to_euler()
    sc = bpy.context.scene
    sc.render.filepath = os.path.join(PREVIEW, f"{label}_{view}.png")
    bpy.ops.render.render(write_still=True)


def finish(name, bones=None, clips=None):
    """Join everything into one mesh; optionally rig + animate; export GLB (+ previews); clear the scene."""
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    bpy.ops.object.join()
    obj = bpy.context.active_object
    obj.name = name
    obj.data.name = name
    # Joining keeps the first part's origin; put the origin back at the world origin so the exported mesh
    # (which Godot code may use on its own, e.g. MultiMesh tiles) is not offset.
    bpy.context.scene.cursor.location = (0, 0, 0)
    bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
    export_objs = [obj]
    if PREVIEW:
        setup_preview(obj)
        render_view(name, "front34")
        render_view(name, "back34")
    if bones:
        arm_obj = build_armature(name, bones)
        mod = obj.modifiers.new("Armature", "ARMATURE")
        mod.object = arm_obj
        obj.parent = arm_obj
        export_objs = [obj, arm_obj]
        names = [b[0] for b in bones]
        for cname, length, fn, loop, snap in clips:
            add_clip(arm_obj, names, cname, length, fn, loop=loop, snap_frame=snap)
    bpy.ops.object.select_all(action="DESELECT")
    for o in export_objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = export_objs[-1]
    os.makedirs(OUT, exist_ok=True)
    path = os.path.join(OUT, name + ".glb")
    bpy.ops.export_scene.gltf(
        filepath=path, export_format="GLB", use_selection=True, export_apply=False, export_yup=True,
        export_animations=bool(bones), export_animation_mode="NLA_TRACKS", export_skins=bool(bones),
    )
    print("EXPORTED", path, "verts", len(obj.data.vertices), "clips", [c[0] for c in clips] if clips else [])


# convenient pose-space helpers (bone points up)
def up(dz): return (0, dz, 0)
def fwd(dy): return (0, 0, -dy)
def sideways(dx): return (dx, 0, 0)


# ================= PATI =================

def pati():
    o, c, pk, bl, bk, w = mat("orange", ORANGE), mat("cream", CREAM), mat("pink", PINK), mat("blue", BLUE), mat("black", BLACK), mat("white", WHITE)
    add("sphere", (0, -0.05, 0.62), o, scale=(0.36, 0.5, 0.42), seg=32, rings=20, bone="Body")
    add("sphere", (0, 0.2, 0.56), c, scale=(0.3, 0.36, 0.33), seg=32, rings=20, bone="Body")
    add("torus", (0, 0.14, 0.93), bl, scale=(1.0, 1.15, 0.8), r=0.23, minor=0.05, bone="Body")
    add("sphere", (0, 0.28, 1.15), o, scale=(1.05 * 0.34, 0.95 * 0.34, 0.9 * 0.34), bone="Head")
    add("sphere", (0, 0.54, 1.06), c, scale=(0.14, 0.11, 0.105), bone="Head")
    add("sphere", (0, 0.65, 1.1), pk, scale=(0.045, 0.04, 0.035), bone="Head")

    def side(s):
        n = side_name(s)
        add("sphere", (0.14 * s, 0.56, 1.23), w, scale=(0.085, 0.06, 0.095), bone="Head")
        add("sphere", (0.14 * s, 0.615, 1.23), bk, scale=(0.05, 0.03, 0.06), bone="Head")
        add("cone", (0.2 * s, 0.2, 1.47), o, rot=(-0.15, 0.25 * s, 0), r=0.12, d=0.28, seg=6, bone="Ear" + n)
        add("cone", (0.2 * s, 0.24, 1.46), pk, rot=(-0.15, 0.25 * s, 0), r=0.07, d=0.2, seg=6, bone="Ear" + n)
        add("cyl", (0.17 * s, 0.2, 0.22), o, r=0.08, d=0.44, bone="LegF" + n)
        add("sphere", (0.17 * s, 0.23, 0.07), c, scale=(0.1, 0.13, 0.07), bone="LegF" + n)
        add("cyl", (0.2 * s, -0.22, 0.22), o, r=0.09, d=0.44, bone="LegB" + n)
        add("sphere", (0.2 * s, -0.19, 0.07), c, scale=(0.11, 0.14, 0.07), bone="LegB" + n)
    mirror_x(side)

    p0, p1, p2 = Vector((0, -0.4, 0.5)), Vector((0, -1.0, 0.75)), Vector((0, -0.45, 1.3))

    def bez(t):
        return (1 - t) ** 2 * p0 + 2 * (1 - t) * t * p1 + t * t * p2
    n = 14
    for i in range(n + 1):
        t = i / n
        seg = "Tail1" if t < 0.34 else "Tail2" if t < 0.67 else "Tail3"
        add("sphere", tuple(bez(t)), c if i >= n - 2 else o, r=0.105 - 0.03 * t, seg=10, rings=8, bone=seg)

    bones = [
        ("Root", (0, 0, 0), None), ("Body", (0, -0.05, 0.62), "Root"), ("Head", (0, 0.12, 0.95), "Body"),
        ("EarL", (-0.2, 0.2, 1.38), "Head"), ("EarR", (0.2, 0.2, 1.38), "Head"),
        ("Tail1", tuple(bez(0)), "Body"), ("Tail2", tuple(bez(0.34)), "Tail1"), ("Tail3", tuple(bez(0.67)), "Tail2"),
        ("LegFL", (-0.17, 0.2, 0.44), "Body"), ("LegFR", (0.17, 0.2, 0.44), "Body"),
        ("LegBL", (-0.2, -0.22, 0.44), "Body"), ("LegBR", (0.2, -0.22, 0.44), "Body"),
    ]

    def idle(t):
        ph = 2 * PI * t
        return {"Body": {"l": up(0.012 * S(ph))}, "Head": {"r": (0.05 * S(ph + 1), 0.08 * S(ph * 2), 0)},
                "Tail1": {"r": (0, 0.15 * S(ph), 0)}, "Tail2": {"r": (0, 0.3 * S(ph - 0.4), 0)}, "Tail3": {"r": (0, 0.4 * S(ph - 0.8), 0)},
                "EarL": {"r": (0.08 * S(ph * 2), 0, 0)}, "EarR": {"r": (0.08 * S(ph * 2 + 1), 0, 0)}}

    def run(t):
        ph = 2 * PI * t
        return {"Body": {"l": up(0.035 * math.cos(2 * ph)), "r": (-0.12 + 0.06 * S(2 * ph), 0, 0)},
                "Head": {"r": (0.1 - 0.05 * S(2 * ph), 0, 0)},
                "LegFL": {"r": (0.95 * S(ph), 0, 0)}, "LegBR": {"r": (0.95 * S(ph + 0.4), 0, 0)},
                "LegFR": {"r": (0.95 * S(ph + PI), 0, 0)}, "LegBL": {"r": (0.95 * S(ph + PI + 0.4), 0, 0)},
                "Tail1": {"r": (0.5 + 0.1 * S(2 * ph), 0, 0)}, "Tail2": {"r": (0.2 * S(2 * ph - 0.5), 0.2 * S(ph), 0)}, "Tail3": {"r": (0.2 * S(2 * ph - 1), 0.3 * S(ph - 0.5), 0)},
                "EarL": {"r": (-0.3 + 0.12 * S(2 * ph), 0, 0)}, "EarR": {"r": (-0.3 + 0.12 * S(2 * ph + 0.5), 0, 0)}}

    def jump(t):
        k = min(1.0, t * 2.0)
        return {"Body": {"r": (0.18 * k, 0, 0)}, "Head": {"r": (-0.12 * k, 0, 0)},
                "LegFL": {"r": (0.9 * k, 0, 0)}, "LegFR": {"r": (0.9 * k, 0, 0)},
                "LegBL": {"r": (-1.0 * k, 0, 0)}, "LegBR": {"r": (-1.0 * k, 0, 0)},
                "Tail1": {"r": (0.7 * k, 0, 0)}, "Tail2": {"r": (0.3 * k, 0, 0)},
                "EarL": {"r": (-0.5 * k, 0, 0)}, "EarR": {"r": (-0.5 * k, 0, 0)}}

    def fall(t):
        ph = 2 * PI * t
        return {"Body": {"r": (-0.12, 0, 0)}, "Head": {"r": (0.1, 0, 0)},
                "LegFL": {"r": (0.55 + 0.18 * S(ph), 0, 0)}, "LegFR": {"r": (0.55 + 0.18 * S(ph + 1), 0, 0)},
                "LegBL": {"r": (-0.45 + 0.18 * S(ph + 2), 0, 0)}, "LegBR": {"r": (-0.45 + 0.18 * S(ph + 3), 0, 0)},
                "Tail1": {"r": (0.8, 0, 0)}, "Tail2": {"r": (0.3 * S(2 * ph), 0.3 * S(ph), 0)}, "Tail3": {"r": (0.4 * S(2 * ph - 1), 0, 0)},
                "EarL": {"r": (-0.6 + 0.3 * S(4 * ph), 0, 0)}, "EarR": {"r": (-0.6 + 0.3 * S(4 * ph + 1), 0, 0)}}

    def ball(t):
        ph = 2 * PI * t
        return {"Body": {"r": (0.1, 0, 0)}, "Head": {"r": (-0.5, 0, 0)},
                "LegFL": {"r": (1.5, 0, 0)}, "LegFR": {"r": (1.5, 0, 0)}, "LegBL": {"r": (-1.5, 0, 0)}, "LegBR": {"r": (-1.5, 0, 0)},
                "Tail1": {"r": (1.0, 0, 0)}, "Tail2": {"r": (0.4, 0, 0)},
                "EarL": {"r": (-0.6, 0, 0)}, "EarR": {"r": (-0.6, 0, 0)}}

    def charge(t):
        ph = 2 * PI * t
        return {"Body": {"l": (0.012 * S(6 * ph), -0.06, 0), "r": (-0.25, 0, 0)}, "Head": {"r": (-0.25, 0, 0)},
                "LegFL": {"r": (0.7, 0, 0)}, "LegFR": {"r": (0.7, 0, 0)}, "LegBL": {"r": (-0.7, 0, 0)}, "LegBR": {"r": (-0.7, 0, 0)},
                "Tail1": {"r": (0.6, 0, 0)}, "Tail2": {"r": (0, 0.6 * S(3 * ph), 0)}, "Tail3": {"r": (0, 0.8 * S(3 * ph - 1), 0)},
                "EarL": {"r": (-0.7, 0, 0)}, "EarR": {"r": (-0.7, 0, 0)}}

    def hurt(t):
        k = min(1.0, t * 3.0)
        return {"Body": {"r": (0.5 * k, 0, 0)}, "Head": {"r": (0.45 * k, 0, 0)},
                "LegFL": {"r": (1.0 * k, 0, 0)}, "LegFR": {"r": (0.7 * k, 0, 0)}, "LegBL": {"r": (-0.6 * k, 0, 0)}, "LegBR": {"r": (-0.9 * k, 0, 0)},
                "Tail1": {"r": (0.9 * k, 0, 0)}, "EarL": {"r": (-0.9 * k, 0, 0)}, "EarR": {"r": (-0.9 * k, 0, 0)}}

    def grind(t):
        ph = 2 * PI * t
        return {"Body": {"r": (-0.05, 0, 0.1 * S(ph))}, "Head": {"r": (-0.05, 0, 0)},
                "LegFL": {"r": (0.35, 0, 0)}, "LegFR": {"r": (0.35, 0, 0)}, "LegBL": {"r": (-0.35, 0, 0)}, "LegBR": {"r": (-0.35, 0, 0)},
                "Tail1": {"r": (0.2, 0.4 * S(ph), 0)}, "Tail2": {"r": (0, 0.7 * S(ph - 0.6), 0)}, "Tail3": {"r": (0, 0.9 * S(ph - 1.2), 0)},
                "EarL": {"r": (-0.4, 0, 0)}, "EarR": {"r": (-0.4, 0, 0)}}

    def climb(t):
        ph = 2 * PI * t
        return {"Body": {"r": (0.3, 0, 0)}, "Head": {"r": (0.25, 0, 0)},
                "LegFL": {"r": (2.2 + 0.5 * S(ph), 0, 0)}, "LegFR": {"r": (2.2 + 0.5 * S(ph + PI), 0, 0)},
                "LegBL": {"r": (-0.3 + 0.7 * S(ph + PI), 0, 0)}, "LegBR": {"r": (-0.3 + 0.7 * S(ph), 0, 0)},
                "Tail1": {"r": (-0.3, 0, 0)}, "Tail2": {"r": (0, 0.3 * S(ph), 0)}}

    clips = [("Idle", 48, idle, True, 12), ("Run", 12, run, True, 3), ("Jump", 8, jump, False, 8), ("Fall", 24, fall, True, 6),
             ("Ball", 12, ball, True, 0), ("Charge", 12, charge, True, 2), ("Hurt", 10, hurt, False, 10),
             ("Grind", 24, grind, True, 6), ("Climb", 16, climb, True, 4)]
    finish("Pati", bones, clips)


# ================= PUP (enemy) =================

def pup():
    br, dk, tn, bk, rd = mat("brown", BROWN), mat("dkbrown", DKBROWN), mat("tan", TAN), mat("black", BLACK), mat("redglow", RED, emissive=1.5)
    mt = mat("metal", GREY, rough=0.4)
    add("sphere", (0, 0.0, 0.65), br, scale=(0.42, 0.55, 0.4), bone="Body")
    add("cube", (0, -0.38, 0.85), mt, scale=(0.32, 0.18, 0.3), smooth=False, bone="Body")
    add("sphere", (0, 0.45, 1.05), br, r=0.3, bone="Head")
    add("sphere", (0, 0.72, 0.98), tn, scale=(0.15, 0.18, 0.12), bone="Head")
    add("sphere", (0, 0.9, 1.02), bk, r=0.06, seg=10, rings=8, bone="Head")
    add("cyl", (0.1, -0.38, 1.15), mt, r=0.025, d=0.4, seg=6, bone="Antenna")
    add("sphere", (0.1, -0.38, 1.36), rd, r=0.06, seg=8, rings=6, bone="Antenna")

    def side(s):
        n = side_name(s)
        add("sphere", (0.13 * s, 0.68, 1.16), rd, scale=(0.07, 0.05, 0.07), bone="Head")
        add("sphere", (0.3 * s, 0.4, 1.0), dk, scale=(0.07, 0.14, 0.26), rot=(0, 0.25 * s, 0), bone="Ear" + n)
        add("cyl", (0.2 * s, 0.25, 0.22), br, r=0.09, d=0.44, bone="LegF" + n)
        add("sphere", (0.2 * s, 0.28, 0.06), tn, scale=(0.1, 0.13, 0.06), bone="LegF" + n)
        add("cyl", (0.2 * s, -0.25, 0.22), br, r=0.09, d=0.44, bone="LegB" + n)
        add("sphere", (0.2 * s, -0.22, 0.06), tn, scale=(0.1, 0.13, 0.06), bone="LegB" + n)
    mirror_x(side)
    add("cone", (0, -0.55, 0.85), dk, rot=(math.radians(-120), 0, 0), r=0.09, d=0.4, seg=6, bone="Tail")

    bones = [("Root", (0, 0, 0), None), ("Body", (0, 0, 0.65), "Root"), ("Head", (0, 0.3, 0.95), "Body"),
             ("EarL", (-0.3, 0.4, 1.1), "Head"), ("EarR", (0.3, 0.4, 1.1), "Head"),
             ("LegFL", (-0.2, 0.25, 0.44), "Body"), ("LegFR", (0.2, 0.25, 0.44), "Body"),
             ("LegBL", (-0.2, -0.25, 0.44), "Body"), ("LegBR", (0.2, -0.25, 0.44), "Body"),
             ("Tail", (0, -0.5, 0.8), "Body"), ("Antenna", (0.1, -0.38, 0.98), "Body")]

    def walk(t):
        ph = 2 * PI * t
        return {"Body": {"l": up(0.03 * math.cos(2 * ph)), "r": (0.05 * S(2 * ph), 0, 0.04 * S(ph))},
                "Head": {"r": (0.12 * S(2 * ph + 1), 0.12 * S(ph), 0)},
                "LegFL": {"r": (0.7 * S(ph), 0, 0)}, "LegBR": {"r": (0.7 * S(ph), 0, 0)},
                "LegFR": {"r": (0.7 * S(ph + PI), 0, 0)}, "LegBL": {"r": (0.7 * S(ph + PI), 0, 0)},
                "Tail": {"r": (0.2, 0.7 * S(2 * ph), 0)}, "Antenna": {"r": (0.2 * S(2 * ph - 1), 0, 0.2 * S(ph))},
                "EarL": {"r": (0.3 * S(2 * ph + 0.5), 0, 0.2 * S(ph))}, "EarR": {"r": (0.3 * S(2 * ph), 0, -0.2 * S(ph))}}
    finish("Pup", bones, [("Walk", 16, walk, True, 4)])


# ================= DR. KENNEL (boss) =================

def dr_kennel():
    wh, br, dk, tn, bk = mat("coat", (0.95, 0.95, 1.0)), mat("brown", BROWN), mat("dkbrown", DKBROWN), mat("tan", TAN), mat("black", BLACK)
    gr, yl, lens = mat("hair", (0.8, 0.8, 0.85)), mat("yellow", YELLOW), mat("lens", (0.1, 0.2, 0.5), emissive=0.6)
    pod, glow, fire = mat("pod", GREY, rough=0.4), mat("cyan", (0.2, 0.9, 1.0), emissive=2.0), mat("fire", (1.0, 0.5, 0.1), emissive=2.5)
    add("sphere", (0, 0, 0.45), pod, scale=(1.3, 1.3, 0.45), bone="Root")
    add("torus", (0, 0, 0.3), glow, r=1.2, minor=0.06, seg=32, bone="Root")
    for x in (-0.55, 0.55):
        add("cyl", (x, 0, 0.1), fire, r=0.18, d=0.2, seg=10, bone="Root")
    add("cone", (0, 0, 1.4), wh, r=1.0, r2=0.55, d=1.7, seg=16, bone="Body")
    add("torus", (0, 0, 1.0), dk, r=0.88, minor=0.05, seg=24, bone="Body")

    def side(s):
        n = side_name(s)
        add("cyl", (0.95 * s, 0, 1.7), wh, rot=(0, 0.9 * s, 0), r=0.18, d=0.9, bone="Arm" + n)
        add("sphere", (1.3 * s, 0, 1.98), br, r=0.22, seg=12, rings=8, bone="Arm" + n)
        add("sphere", (0.28 * s, 0.1, 2.9), yl, scale=(0.25, 0.2, 0.25), bone="Head")
        add("sphere", (0.28 * s, 0.26, 2.9), lens, scale=(0.17, 0.08, 0.17), bone="Head")
        add("sphere", (0.58 * s, 0.05, 2.6), dk, scale=(0.14, 0.28, 0.5), rot=(0, -0.2 * s, 0), bone="Head")
        add("cone", (0.3 * s, 0.0, 3.2), gr, rot=(0, 0.4 * s, 0), r=0.12, d=0.55, seg=6, bone="Head")
    mirror_x(side)
    add("sphere", (0, 0.1, 2.55), br, scale=(0.6, 0.63, 0.57), bone="Head")
    add("sphere", (0, 0.62, 2.42), tn, scale=(0.27, 0.39, 0.24), bone="Head")
    add("sphere", (0, 0.98, 2.5), bk, r=0.12, seg=10, rings=8, bone="Head")
    add("torus", (0, 0.1, 2.85), bk, scale=(1.0, 1.05, 1.0), r=0.6, minor=0.04, seg=28, bone="Head")
    add("cone", (0, 0.0, 3.25), gr, r=0.12, d=0.6, seg=6, bone="Head")

    bones = [("Root", (0, 0, 0), None), ("Body", (0, 0, 1.0), "Root"), ("Head", (0, 0.1, 2.2), "Body"),
             ("ArmL", (-0.8, 0, 1.85), "Body"), ("ArmR", (0.8, 0, 1.85), "Body")]

    def hover(t):
        ph = 2 * PI * t
        return {"Root": {"l": up(0.07 * S(ph))}, "Body": {"r": (0.04 * S(ph + 1), 0.05 * S(ph), 0)},
                "Head": {"r": (0.06 * S(2 * ph), 0.1 * S(ph), 0)},
                "ArmL": {"r": (0, 0, 0.35 * S(2 * ph))}, "ArmR": {"r": (0, 0, 0.35 * S(2 * ph + PI))}}

    def charge(t):
        ph = 2 * PI * t
        return {"Root": {"l": (0.02 * S(8 * ph), 0, 0)}, "Body": {"r": (-0.3, 0, 0)}, "Head": {"r": (-0.2, 0, 0)},
                "ArmL": {"r": (0, 0, -0.9 + 0.1 * S(6 * ph))}, "ArmR": {"r": (0, 0, 0.9 - 0.1 * S(6 * ph))}}

    def stunned(t):
        ph = 2 * PI * t
        return {"Root": {"l": up(0.02 * S(ph))}, "Body": {"r": (0.15, 0, 0)}, "Head": {"r": (-0.55 + 0.05 * S(ph), 0.2 * S(ph), 0)},
                "ArmL": {"r": (0, 0, 0.7)}, "ArmR": {"r": (0, 0, -0.7)}}
    finish("DrKennel", bones, [("Hover", 48, hover, True, 12), ("Charge", 12, charge, True, 3), ("Stunned", 24, stunned, True, 6)])


# ================= FISH =================

def fish():
    bl, bel, dfin, w, bk = mat("fishblue", (0.25, 0.7, 1.0)), mat("belly", (0.9, 0.95, 1.0)), mat("finblue", (0.1, 0.4, 0.9)), mat("white", WHITE), mat("black", BLACK)
    add("sphere", (0, 0, 0), bl, scale=(0.14, 0.32, 0.17), seg=14, rings=10, bone="Root")
    add("sphere", (0, 0.01, -0.05), bel, scale=(0.12, 0.29, 0.1), seg=12, rings=8, bone="Root")
    add("cone", (0, -0.42, 0), dfin, scale=(0.25, 1, 1), rot=(-PI / 2, 0, 0), r=0.17, d=0.26, seg=6, bone="Tail")
    add("cone", (0, -0.02, 0.18), dfin, scale=(0.2, 1, 1), r=0.09, d=0.2, seg=6, bone="Root")
    for s in (-1, 1):
        add("sphere", (0.1 * s, 0.17, 0.05), w, r=0.045, seg=8, rings=6, bone="Root")
        add("sphere", (0.13 * s, 0.17, 0.05), bk, r=0.025, seg=6, rings=4, bone="Root")
    bones = [("Root", (0, 0, 0), None), ("Tail", (0, -0.3, 0), "Root")]

    def swim(t):
        ph = 2 * PI * t
        return {"Root": {"r": (0, 0.1 * S(ph + 1), 0), "l": up(0.01 * S(2 * ph))}, "Tail": {"r": (0, 0.6 * S(ph), 0)}}
    finish("Fish", bones, [("Swim", 16, swim, True, 4)])


# ================= static props =================

def storm_core():
    core, spark = mat("core", YELLOW, emissive=3.0), mat("spark", (1.0, 1.0, 0.7), emissive=3.0)
    add("ico", (0, 0, 0), core, scale=(0.6, 0.6, 1.0), sub=1, smooth=False)
    for i in range(6):
        a = math.tau * i / 6
        add("ico", (math.cos(a) * 0.95, math.sin(a) * 0.95, 0), spark, r=0.13, sub=1, smooth=False)
    finish("StormCore")


def checkpoint_pole():
    mt, gold, base = mat("metal", GREY, rough=0.4), mat("gold", YELLOW, emissive=0.5), mat("base", (0.4, 0.4, 0.5))
    add("cyl", (0, 0, 0.06), base, r=0.32, d=0.12, seg=16)
    add("cyl", (0, 0, 1.3), mt, r=0.06, d=2.5, seg=8)
    add("sphere", (0, 0, 2.58), gold, r=0.1, seg=10, rings=8)
    finish("CheckpointPole")


def checkpoint_flag():
    flag, paw = mat("flag", (0.95, 0.25, 0.4)), mat("paw", WHITE)
    add("cube", (0.5, 0, 2.2), flag, scale=(0.9, 0.04, 0.55), smooth=False)
    add("sphere", (0.5, 0.03, 2.15), paw, scale=(0.1, 0.02, 0.085))
    for x, z in ((-0.1, 2.28), (0.0, 2.33), (0.1, 2.33), (0.2, 2.28)):
        add("sphere", (0.5 + x * 0.9, 0.03, z - 0.02), paw, scale=(0.035, 0.02, 0.04), seg=8, rings=6)
    finish("CheckpointFlag")


def crate():
    wood, beam = mat("wood", (0.65, 0.45, 0.25)), mat("beam", (0.4, 0.26, 0.14))
    add("cube", (0, 0, 0.6), wood, scale=(1.2, 1.2, 1.2), smooth=False)
    h = 1.2
    for sx in (-1, 1):
        for sy in (-1, 1):
            add("cube", (sx * 0.6, sy * 0.6, 0.6), beam, scale=(0.12, 0.12, h + 0.04), smooth=False)
    for sz in (0.03, 1.17):
        for s in (-1, 1):
            add("cube", (s * 0.6, 0, sz), beam, scale=(0.12, h, 0.12), smooth=False)
            add("cube", (0, s * 0.6, sz), beam, scale=(h, 0.12, 0.12), smooth=False)
    finish("Crate")


def tree():
    trunk = mat("trunk", (0.5, 0.33, 0.2))
    g1, g2, g3 = mat("leaf1", (0.3, 0.7, 0.3)), mat("leaf2", (0.35, 0.78, 0.3)), mat("leaf3", (0.42, 0.85, 0.36))
    add("cyl", (0, 0, 1.1), trunk, r=0.35, d=2.2, seg=8)
    add("ico", (0, 0, 2.7), g1, r=1.3, sub=2)
    add("ico", (0.2, 0.1, 3.6), g2, r=1.0, sub=2)
    add("ico", (-0.1, 0.0, 4.3), g3, r=0.75, sub=2)
    finish("Tree")


MODELS = {
    "Pati": pati, "Pup": pup, "DrKennel": dr_kennel, "Fish": fish, "StormCore": storm_core,
    "CheckpointPole": checkpoint_pole, "CheckpointFlag": checkpoint_flag, "Crate": crate, "Tree": tree,
}

if __name__ == "__main__":
    for name, fn in MODELS.items():
        if ONLY and name not in ONLY:
            continue
        reset()
        fn()
    print("DONE")
