"""
Renders Pati's head from Art/Models/Pati.glb into the game icon: Art/Icon/icon.png (256) and Art/Icon/icon.ico (16..256).
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --python Docs/tools/blender/make_icon.py
"""
import os
import struct

import bpy
from mathutils import Vector

ROOT = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", "..", ".."))
OUT = os.path.join(ROOT, "Art", "Icon")
os.makedirs(OUT, exist_ok=True)

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=os.path.join(ROOT, "Art", "Models", "Pati.glb"))

sc = bpy.context.scene
sc.render.engine = "BLENDER_WORKBENCH"
sc.render.resolution_x = sc.render.resolution_y = 256
sc.render.film_transparent = True
sc.render.image_settings.file_format = "PNG"
sc.render.image_settings.color_mode = "RGBA"
sh = sc.display.shading
sh.light = "STUDIO"
sh.color_type = "MATERIAL"
sh.show_object_outline = True
sh.object_outline_color = (0.05, 0.02, 0.1)

cam_data = bpy.data.cameras.new("cam")
cam_data.type = "ORTHO"
cam_data.ortho_scale = 1.15
cam = bpy.data.objects.new("cam", cam_data)
sc.collection.objects.link(cam)
sc.camera = cam
target = Vector((0, 0.2, 1.12))  # head + collar
cam.location = target + Vector((0.55, 2.0, 0.35))
cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()

sc.render.filepath = os.path.join(OUT, "icon.png")
bpy.ops.render.render(write_still=True)

# --- build a multi-size .ico (PNG-compressed entries) ---
src = bpy.data.images.load(os.path.join(OUT, "icon.png"))
entries = []
for size in (256, 128, 64, 48, 32, 16):
    img = src.copy()
    img.scale(size, size)
    path = os.path.join(OUT, f"_tmp_{size}.png")
    img.filepath_raw = path
    img.file_format = "PNG"
    img.save()
    with open(path, "rb") as f:
        entries.append((size, f.read()))
    os.remove(path)

header = struct.pack("<HHH", 0, 1, len(entries))
offset = 6 + 16 * len(entries)
dirs, blobs = b"", b""
for size, data in entries:
    dirs += struct.pack("<BBBBHHII", 0 if size >= 256 else size, 0 if size >= 256 else size, 0, 0, 1, 32, len(data), offset)
    offset += len(data)
    blobs += data
with open(os.path.join(OUT, "icon.ico"), "wb") as f:
    f.write(header + dirs + blobs)
print("ICON DONE", os.listdir(OUT))
