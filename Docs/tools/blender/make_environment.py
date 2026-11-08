"""
PawStorm environment kit: ground tiles, ramp tiles, edge barriers and set-dressing props (static, low-poly).

Run (project root):
  "C:\\Program Files\\Blender Foundation\\Blender 5.2\\blender.exe" --background --python Docs/tools/blender/make_environment.py
Env vars: PAWSTORM_PREVIEW (preview PNG dir), PAWSTORM_ONLY (comma list of model names).
Output: Art/Models/Env/*.glb

Conventions (same as make_models.py): Blender +Y = model front. Extra rules for the kit:
  * tiles are 4 x 4 m footprint, 1 m thick, TOP SURFACE at z = 0 (origin on the top face centre)
  * barriers are 4 m long along Y, origin at the ground centre
  * props have their origin on the ground
"""
import math
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import make_models as mm  # noqa: E402

mm.OUT = os.path.join(mm.ROOT, "Art", "Models", "Env")
ONLY = [s for s in os.environ.get("PAWSTORM_ONLY", "").split(",") if s]
PI = math.pi
add, mat = mm.add, mm.mat


def box(loc, size, m):
    return add("cube", loc, m, scale=size, smooth=False)


def cyl(loc, r, d, m, rot=(0, 0, 0), seg=12, **kw):
    return add("cyl", loc, m, rot=rot, r=r, d=d, seg=seg, **kw)


def build(name, fn):
    mm.reset()
    fn()
    mm.finish(name)


# ---------------------------------------------------------------- tiles
def planks_tile(base, cols, rows=5):
    def fn():
        box((0, 0, -0.5), (4, 4, 1), mat("base", base))
        step = 4.0 / rows
        for i in range(rows):
            y = -2 + step * (i + 0.5)
            box((0, y, 0.0), (3.9, step - 0.12, 0.07), mat("p%d" % (i % len(cols)), cols[i % len(cols)]))
    return fn


def roof_tile():
    box((0, 0, -0.5), (4, 4, 1), mat("base", (0.62, 0.36, 0.3)))
    for i, y in enumerate((-1.5, -0.5, 0.5, 1.5)):
        box((0, y, 0.0), (3.9, 0.9, 0.07), mat("s%d" % (i % 2), ((0.74, 0.46, 0.37), (0.68, 0.4, 0.33))[i % 2]))


def grass_tile():
    box((0, 0, -0.625), (4, 4, 0.75), mat("dirt", (0.45, 0.3, 0.2)))
    box((0, 0, -0.125), (4, 4, 0.25), mat("grass", (0.32, 0.7, 0.28)))
    for x, y in ((-1.1, -1.1), (1.1, -0.9), (-0.8, 1.2), (1.2, 1.2)):
        add("cone", (x, y, 0.07), mat("tuft", (0.42, 0.82, 0.35)), r=0.1, d=0.14, seg=5, smooth=False)


def factory_tile():
    box((0, 0, -0.5), (4, 4, 1), mat("base", (0.42, 0.44, 0.5)))
    box((0, 0, 0.0), (3.7, 3.7, 0.07), mat("plate", (0.52, 0.54, 0.6)))
    for x in (-1.7, 1.7):
        for y in (-1.7, 1.7):
            cyl((x, y, 0.06), 0.09, 0.08, mat("rivet", (0.85, 0.65, 0.2)), seg=8)


def cloud_tile():
    box((0, 0, -0.5), (4, 4, 1), mat("base", (0.82, 0.85, 0.97)))
    w = mat("puff", (0.96, 0.96, 1.0))
    for x, y, r in ((-1.0, -1.0, 0.9), (1.0, -0.8, 0.8), (0.0, 0.2, 1.0), (-1.1, 1.1, 0.8), (1.1, 1.2, 0.85)):
        add("ico", (x, y, -0.03), w, scale=(1, 1, 0.05), r=r, sub=2)


def stripe_tile(a, b, emissive=0.0):
    def fn():
        box((0, 0, -0.5), (4, 4, 1), mat("base", a))
        for y in (-1.3, 0.0, 1.3):
            box((0, y, 0.0), (3.9, 0.35, 0.07), mat("stripe", b, emissive=emissive))
    return fn


# ---------------------------------------------------------------- barriers (4 m along Y, ~2 m tall)
def rail_roof():
    brick, metal = mat("brick", (0.55, 0.33, 0.28)), mat("metal", (0.65, 0.68, 0.75), rough=0.4)
    box((0, 0, 0.25), (0.5, 4, 0.5), brick)
    for y in (-1.8, -0.6, 0.6, 1.8):
        cyl((0, y, 1.25), 0.06, 1.5, metal, seg=8)
    for z in (1.2, 1.95):
        cyl((0, 0, z), 0.06, 4, metal, rot=(PI / 2, 0, 0), seg=8)


def rail_dock():
    wood, rope, cap = mat("wood", (0.5, 0.33, 0.2)), mat("rope", (0.82, 0.72, 0.5)), mat("cap", (0.35, 0.22, 0.14))
    for y in (-1.8, 0, 1.8):
        cyl((0, y, 0.95), 0.11, 1.9, wood, seg=8)
        add("sphere", (0, y, 1.95), cap, r=0.14, seg=8, rings=6)
    for z in (1.0, 1.65):
        cyl((0, 0, z), 0.045, 4, rope, rot=(PI / 2, 0, 0), seg=6)


def fence_forest():
    log, bush, flower = mat("log", (0.5, 0.33, 0.2)), mat("bush", (0.3, 0.65, 0.3)), mat("flower", (1.0, 0.55, 0.7))
    for y in (-1.8, 0, 1.8):
        cyl((0, y, 0.95), 0.12, 1.9, log, seg=8)
    for z in (1.1, 1.7):
        cyl((0, 0, z), 0.1, 4, log, rot=(PI / 2, 0, 0), seg=8)
    for y in (-1.0, 1.0):
        add("ico", (0.1, y, 0.4), bush, r=0.55, sub=2)
        add("sphere", (0.4, y + 0.2, 0.75), flower, r=0.08, seg=6, rings=4)


def rail_factory():
    yel, blk = mat("yellow", (0.95, 0.8, 0.1)), mat("black", (0.1, 0.1, 0.12))
    box((0, 0, 0.075), (0.45, 4, 0.15), blk)
    for y in (-1.8, 0, 1.8):
        box((0, y, 1.0), (0.14, 0.14, 1.85), yel)
    for z in (1.0, 1.7):
        for i in range(8):
            box((0, -1.75 + 0.5 * i, z), (0.1, 0.5, 0.18), yel if i % 2 == 0 else blk)


def rail_sky():
    post, orb, rib = mat("post", (0.82, 0.86, 0.96)), mat("orb", (0.4, 0.95, 1.0), emissive=2.5), mat("rib", (1.0, 0.7, 0.95), emissive=0.8)
    for y in (-1.8, 0, 1.8):
        cyl((0, y, 0.95), 0.05, 1.9, post, seg=8)
        add("sphere", (0, y, 2.0), orb, r=0.13, seg=8, rings=6)
    for z in (1.0, 1.6):
        box((0, 0, z), (0.04, 4, 0.14), rib)


# ---------------------------------------------------------------- props
def chimney():
    box((0, 0, 1.5), (0.9, 0.9, 3), mat("brick", (0.6, 0.32, 0.26)))
    box((0, 0, 3.1), (1.2, 1.2, 0.25), mat("cap", (0.3, 0.25, 0.25)))


def ac_unit():
    box((0, 0, 0.55), (2.0, 1.4, 1.1), mat("body", (0.85, 0.87, 0.9)))
    cyl((0, 0, 1.12), 0.5, 0.08, mat("fan", (0.2, 0.22, 0.26)), seg=16)
    for y in (-0.4, -0.2, 0, 0.2, 0.4):
        box((1.01, y, 0.55), (0.05, 0.08, 0.7), mat("vent", (0.4, 0.42, 0.48)))


def water_tower():
    wood, dk = mat("wood", (0.5, 0.35, 0.25)), mat("roof", (0.35, 0.25, 0.2))
    for x in (-0.8, 0.8):
        for y in (-0.8, 0.8):
            cyl((x, y, 1.25), 0.1, 2.5, dk, seg=8)
    cyl((0, 0, 3.6), 1.3, 2.2, wood, seg=16)
    for z in (3.0, 4.2):
        add("torus", (0, 0, z), dk, r=1.3, minor=0.05, seg=20)
    add("cone", (0, 0, 5.15), dk, r=1.5, d=0.9, seg=16)


def building():
    body = mat("body", (0.55, 0.5, 0.68))
    box((0, 0, 20), (10, 10, 40), body)
    box((0, 0, 40.3), (10.6, 10.6, 0.6), mat("cap", (0.35, 0.32, 0.45)))
    lit, dark = mat("lit", (1.0, 0.85, 0.4), emissive=1.2), mat("dark", (0.22, 0.28, 0.45))
    for face in range(4):
        for col in range(4):
            for row in range(12):
                u, z = -3.6 + col * 2.4, 3 + row * 3.1
                m = lit if (col * 7 + row * 3 + face) % 5 == 0 else dark
                if face == 0: box((u, 5.0, z), (1.4, 0.12, 1.6), m)
                elif face == 1: box((u, -5.0, z), (1.4, 0.12, 1.6), m)
                elif face == 2: box((5.0, u, z), (0.12, 1.4, 1.6), m)
                else: box((-5.0, u, z), (0.12, 1.4, 1.6), m)


def barrel():
    wood, band = mat("wood", (0.55, 0.36, 0.2)), mat("band", (0.25, 0.25, 0.3))
    cyl((0, 0, 0.5), 0.42, 1.0, wood, seg=14)
    for z in (0.2, 0.8):
        add("torus", (0, 0, z), band, r=0.43, minor=0.03, seg=16)


def crane():
    yel, red, dk = mat("yel", (0.95, 0.75, 0.1)), mat("red", (0.85, 0.2, 0.15)), mat("dk", (0.2, 0.2, 0.25))
    box((0, 0, 0.5), (3, 3, 1), dk)
    box((0, 0, 9), (0.8, 0.8, 16), yel)
    box((0, 3.5, 17), (0.6, 12, 0.6), red)
    box((0, -4, 16.5), (2, 2, 1.6), dk)
    cyl((0, 8, 14), 0.04, 5.6, dk, seg=6)
    add("sphere", (0, 8, 11.1), dk, r=0.25, seg=8, rings=6)


def lighthouse():
    wh, rd, glass = mat("wh", (0.95, 0.95, 0.95)), mat("rd", (0.85, 0.2, 0.2)), mat("glass", (1.0, 0.9, 0.5), emissive=2.0)
    for i, (z, r1, r2) in enumerate(((2.5, 2.4, 2.0), (7.5, 2.0, 1.7), (12.5, 1.7, 1.45))):
        add("cone", (0, 0, z), rd if i % 2 == 0 else wh, r=r1, r2=r2, d=5.0, seg=16)
    cyl((0, 0, 15.6), 1.3, 1.4, glass, seg=14)
    add("cone", (0, 0, 17.0), rd, r=1.7, d=1.4, seg=14)


def boat():
    hull, wh, rd = mat("hull", (0.6, 0.2, 0.2)), mat("wh", (0.95, 0.95, 0.95)), mat("rd", (0.85, 0.2, 0.2))
    add("sphere", (0, 0, 0.25), hull, scale=(1.6, 4.5, 0.9), seg=16, rings=10)
    box((0, 0, 0.95), (2.2, 6.2, 0.12), mat("deck", (0.7, 0.55, 0.35)))
    box((0, -0.5, 1.6), (1.6, 1.8, 1.2), wh)
    cyl((0, -0.5, 2.5), 0.3, 1.0, rd, seg=10)


def rock():
    g = mat("rock", (0.55, 0.57, 0.62))
    add("ico", (0, 0, 0.6), g, scale=(1.3, 1.0, 0.8), r=1.0, sub=1, smooth=False)
    add("ico", (1.1, 0.5, 0.35), mat("rock2", (0.5, 0.52, 0.58)), scale=(1, 1, 0.8), r=0.6, sub=1, smooth=False)


def bush():
    g = mat("bush", (0.3, 0.66, 0.3))
    for x, y, z, r in ((0, 0, 0.7, 0.8), (0.7, 0.3, 0.5, 0.6), (-0.6, -0.2, 0.5, 0.6)):
        add("ico", (x, y, z), g, r=r, sub=2)


def mushroom():
    add("cyl", (0, 0, 0.3), mat("stem", (0.95, 0.9, 0.8)), r=0.15, d=0.6, seg=10)
    add("sphere", (0, 0, 0.7), mat("cap", (0.9, 0.2, 0.2)), scale=(0.6, 0.6, 0.38), seg=14, rings=8)
    for x, y in ((0.25, 0.2), (-0.25, 0.1), (0.05, -0.3)):
        add("sphere", (x, y, 0.88), mat("spot", (1, 1, 1)), r=0.07, seg=6, rings=4)


def pipe():
    cu, fl = mat("cu", (0.65, 0.4, 0.3)), mat("fl", (0.35, 0.35, 0.4))
    cyl((0, 0, 1.2), 0.45, 8, cu, rot=(0, PI / 2, 0), seg=14)
    for x in (-3.8, -1.3, 1.3, 3.8):
        cyl((x, 0, 1.2), 0.6, 0.25, fl, rot=(0, PI / 2, 0), seg=14)
    for x in (-2.5, 2.5):
        box((x, 0, 0.5), (0.3, 0.3, 1.0), fl)


def smokestack():
    rd, wh, dk = mat("rd", (0.8, 0.2, 0.2)), mat("wh", (0.92, 0.92, 0.92)), mat("dk", (0.2, 0.2, 0.22))
    for i in range(4):
        add("cone", (0, 0, 2.5 + i * 5), rd if i % 2 == 0 else wh, r=1.5 - i * 0.1, r2=1.4 - i * 0.1, d=5, seg=14)
    add("torus", (0, 0, 20), dk, r=1.15, minor=0.12, seg=20)


def gear():
    g, hub = mat("gear", (0.5, 0.52, 0.58)), mat("hub", (0.95, 0.6, 0.15))
    cyl((0, 0, 2.5), 2.0, 0.6, g, rot=(PI / 2, 0, 0), seg=20)
    cyl((0, 0, 2.5), 0.5, 0.9, hub, rot=(PI / 2, 0, 0), seg=10)
    for i in range(12):
        a = math.tau * i / 12
        box((math.cos(a) * 2.2, 0, 2.5 + math.sin(a) * 2.2), (0.55, 0.6, 0.7), g).rotation_euler = (0, -a, 0)


def tank():
    m, band = mat("tank", (0.6, 0.65, 0.72)), mat("band", (0.3, 0.32, 0.38))
    cyl((0, 0, 2.2), 1.8, 4, m, seg=18)
    add("sphere", (0, 0, 4.2), m, scale=(1.8, 1.8, 0.8), seg=16, rings=8)
    for z in (1.0, 3.2):
        add("torus", (0, 0, z), band, r=1.81, minor=0.06, seg=22)


def cloud():
    w = mat("cloud", (0.97, 0.97, 1.0))
    for x, y, z, r in ((0, 0, 0, 2.0), (2, 0.5, -0.3, 1.6), (-2, 0, -0.2, 1.7), (0.8, -1, 0.5, 1.4), (-1, 1, 0.6, 1.3), (3.5, 0, -0.6, 1.1)):
        add("sphere", (x, y, z), w, r=r, seg=12, rings=8)


def float_rock():
    add("cone", (0, 0, -4.5), mat("rock", (0.5, 0.45, 0.44)), rot=(PI, 0, 0), r=6, d=7, seg=9, smooth=False)
    add("cone", (1.5, 1, -7), mat("rock2", (0.45, 0.4, 0.4)), rot=(PI, 0, 0), r=2.5, d=4, seg=7, smooth=False)

# ---------------------------------------------------------------- levels 7-9 kit: crystal caves, temple, storm fortress
def crystal_tile():
    box((0, 0, -0.5), (4, 4, 1), mat("base", (0.28, 0.21, 0.38)))
    for i, y in enumerate((-1.5, -0.5, 0.5, 1.5)):
        box((0, y, 0.0), (3.9, 0.9, 0.07), mat("s%d" % (i % 2), ((0.4, 0.3, 0.52), (0.34, 0.25, 0.46))[i % 2]))
    glow = mat("vein", (0.3, 0.95, 1.0), emissive=1.8)
    for y in (-1.0, 1.0):
        box((0, y, 0.01), (3.95, 0.07, 0.085), glow)


def temple_tile():
    box((0, 0, -0.5), (4, 4, 1), mat("base", (0.68, 0.56, 0.38)))
    for i, y in enumerate((-1.5, -0.5, 0.5, 1.5)):
        box((0, y, 0.0), (3.9, 0.9, 0.07), mat("s%d" % (i % 2), ((0.78, 0.66, 0.46), (0.72, 0.6, 0.42))[i % 2]))
    box((0, 0, 0.01), (0.12, 3.9, 0.085), mat("groove", (0.5, 0.4, 0.26)))


def fortress_tile():
    box((0, 0, -0.5), (4, 4, 1), mat("base", (0.14, 0.15, 0.22)))
    box((0, 0, 0.0), (3.7, 3.7, 0.07), mat("plate", (0.2, 0.22, 0.32)))
    bolt = mat("bolt", (0.35, 0.65, 1.0), emissive=2.0)
    box((0, 0, 0.02), (3.9, 0.1, 0.09), bolt)
    box((0, 0, 0.02), (0.1, 3.9, 0.09), bolt)


def rail_crystal():
    rock, glow, pink = mat("rock", (0.22, 0.16, 0.3)), mat("glow", (0.3, 0.95, 1.0), emissive=2.0), mat("pink", (1.0, 0.4, 0.9), emissive=2.0)
    box((0, 0, 0.25), (0.5, 4, 0.5), rock)
    for i, y in enumerate((-1.8, -0.6, 0.6, 1.8)):
        add("cone", (0, y, 1.3), pink if i % 2 else glow, r=0.16, d=1.6, seg=5, smooth=False)
    for z in (1.0, 1.7):
        cyl((0, 0, z), 0.04, 4, glow, rot=(PI / 2, 0, 0), seg=6)


def rail_temple():
    stone, cap = mat("stone", (0.78, 0.7, 0.55)), mat("cap", (0.65, 0.55, 0.4))
    box((0, 0, 0.3), (0.55, 4, 0.6), stone)
    for y in (-1.6, -0.8, 0.0, 0.8, 1.6):
        cyl((0, y, 1.1), 0.12, 1.0, stone, seg=8)
    box((0, 0, 1.7), (0.5, 4, 0.22), cap)


def rail_fortress():
    dk, rd, bar = mat("dk", (0.12, 0.13, 0.2)), mat("redtip", (1.0, 0.2, 0.25), emissive=2.2), mat("bar", (0.25, 0.27, 0.36))
    box((0, 0, 0.15), (0.5, 4, 0.3), dk)
    for y in (-1.8, -0.6, 0.6, 1.8):
        box((0, y, 1.0), (0.12, 0.12, 1.5), bar)
        add("cone", (0, y, 1.95), rd, r=0.12, d=0.5, seg=4, smooth=False)
    for z in (0.9, 1.5):
        box((0, 0, z), (0.08, 4, 0.1), bar)


def crystal_cluster():
    cols = [mat("c1", (0.3, 0.95, 1.0), emissive=1.8), mat("c2", (1.0, 0.4, 0.9), emissive=1.8), mat("c3", (0.6, 0.5, 1.0), emissive=1.8)]
    for i, (x, y, h, r) in enumerate(((0, 0, 2.0, 0.35), (0.5, 0.2, 1.2, 0.25), (-0.5, 0.1, 1.5, 0.28), (0.1, -0.5, 0.9, 0.22), (-0.3, 0.5, 1.0, 0.24))):
        add("cone", (x, y, h / 2), cols[i % 3], rot=(0.1 * (i % 3 - 1), 0.12 * (i - 2), 0), r=r, d=h, seg=6, smooth=False)


def crystal_big():
    cols = [mat("c1", (0.3, 0.95, 1.0), emissive=1.6), mat("c2", (0.7, 0.45, 1.0), emissive=1.6), mat("c3", (1.0, 0.4, 0.9), emissive=1.6)]
    for i, (x, y, h, r) in enumerate(((0, 0, 7.0, 1.0), (1.4, 0.4, 4.0, 0.7), (-1.5, 0.2, 5.0, 0.8), (0.4, -1.4, 3.0, 0.6), (-0.8, 1.3, 3.6, 0.7), (1.8, -1.0, 2.2, 0.5))):
        add("cone", (x, y, h / 2), cols[i % 3], rot=(0.12 * (i % 3 - 1), 0.1 * (i - 2), 0), r=r, d=h, seg=6, smooth=False)


def stalagmite():
    g = mat("rock", (0.3, 0.24, 0.38))
    add("cone", (0, 0, 1.8), g, r=0.9, d=3.6, seg=7, smooth=False)
    add("cone", (1.0, 0.4, 0.9), mat("rock2", (0.26, 0.2, 0.34)), r=0.5, d=1.8, seg=6, smooth=False)


def glow_shroom():
    add("cyl", (0, 0, 0.35), mat("stem", (0.8, 0.85, 0.95)), r=0.12, d=0.7, seg=8)
    add("sphere", (0, 0, 0.8), mat("capg", (0.3, 1.0, 0.9), emissive=2.0), scale=(0.55, 0.55, 0.32), seg=12, rings=6)


def temple_pillar():
    st, dk = mat("stone", (0.8, 0.72, 0.58)), mat("dk", (0.62, 0.54, 0.4))
    box((0, 0, 0.3), (2.2, 2.2, 0.6), dk)
    cyl((0, 0, 3.6), 0.85, 6.0, st, seg=14)
    for z in (1.0, 6.4):
        add("torus", (0, 0, z), dk, r=0.88, minor=0.07, seg=16)
    box((0, 0, 7.0), (2.0, 2.0, 0.5), dk)


def temple_arch():
    st, gold = mat("stone", (0.8, 0.72, 0.58)), mat("gold", (1.0, 0.8, 0.3), emissive=0.5)
    for x in (-2.6, 2.6):
        cyl((x, 0, 2.5), 0.6, 5.0, st, seg=12)
        box((x, 0, 0.25), (1.6, 1.6, 0.5), st)
    box((0, 0, 5.4), (6.8, 1.0, 0.9), st)
    box((0, 0, 6.05), (1.2, 1.1, 0.5), gold)


def brazier():
    dk, fire, glow = mat("dk", (0.3, 0.28, 0.3)), mat("fire", (1.0, 0.55, 0.1), emissive=3.0), mat("fire2", (1.0, 0.85, 0.3), emissive=3.0)
    add("cone", (0, 0, 0.6), dk, r=0.4, r2=0.12, d=1.2, seg=8)
    cyl((0, 0, 1.3), 0.5, 0.3, dk, seg=12)
    add("cone", (0, 0, 1.9), fire, r=0.35, d=0.9, seg=6, smooth=False)
    add("cone", (0.1, 0, 1.75), glow, r=0.18, d=0.6, seg=5, smooth=False)


def cat_statue():
    st, dk = mat("stone", (0.62, 0.62, 0.66)), mat("plinth", (0.5, 0.5, 0.55))
    box((0, 0, 0.4), (1.8, 1.8, 0.8), dk)
    add("sphere", (0, 0, 1.7), st, scale=(0.6, 0.5, 0.95), seg=16, rings=10)
    add("sphere", (0, 0.1, 2.9), st, r=0.5, seg=16, rings=10)
    for s in (-1, 1):
        add("cone", (0.3 * s, 0.05, 3.5), st, r=0.18, d=0.45, seg=5, smooth=False)
    add("sphere", (-0.2, -0.55, 1.0), st, scale=(0.18, 0.5, 0.18), seg=10, rings=6)


def spire():
    dk, blue, rd = mat("dk", (0.13, 0.14, 0.22)), mat("blue", (0.35, 0.65, 1.0), emissive=2.5), mat("rd", (1.0, 0.2, 0.25), emissive=2.0)
    for i, (z, r1, r2, d) in enumerate(((3, 2.2, 1.6, 6), (9, 1.6, 1.0, 6), (15, 1.0, 0.5, 6), (21, 0.5, 0.2, 6))):
        add("cone", (0, 0, z), dk, r=r1, r2=r2, d=d, seg=8, smooth=False)
    add("sphere", (0, 0, 24.5), blue, r=0.6, seg=12, rings=8)
    add("torus", (0, 0, 12), rd, r=1.25, minor=0.07, seg=20)


def banner():
    pole, cloth, mark = mat("pole", (0.2, 0.2, 0.26)), mat("cloth", (0.7, 0.1, 0.15)), mat("mark", (0.95, 0.95, 0.95))
    cyl((0, 0, 3.5), 0.1, 7.0, pole, seg=8)
    box((0.9, 0, 5.4), (1.6, 0.06, 2.4), cloth)
    add("sphere", (0.9, 0.05, 5.6), mark, scale=(0.4, 0.05, 0.4), seg=10, rings=6)
    add("cone", (0.9, 0.05, 5.1), mark, rot=(PI, 0, 0), r=0.3, d=0.6, seg=4, smooth=False)

MODELS = {
    "TileRoof": roof_tile,
    "TileDock": planks_tile((0.45, 0.3, 0.18), [(0.62, 0.44, 0.26), (0.55, 0.38, 0.22)]),
    "TileGrass": grass_tile, "TileFactory": factory_tile, "TileCloud": cloud_tile,
    "RampRoof": stripe_tile((0.85, 0.55, 0.25), (0.95, 0.95, 0.95)),
    "RampDock": stripe_tile((0.25, 0.5, 0.65), (0.95, 0.95, 0.95)),
    "RampForest": stripe_tile((0.5, 0.33, 0.2), (0.72, 0.52, 0.32)),
    "RampFactory": stripe_tile((0.95, 0.6, 0.15), (0.1, 0.1, 0.12)),
    "RampSky": stripe_tile((0.95, 0.75, 0.95), (0.6, 0.95, 1.0), emissive=1.0),
    "RailRoof": rail_roof, "RailDock": rail_dock, "RailForest": fence_forest, "RailFactory": rail_factory, "RailSky": rail_sky,
    "Chimney": chimney, "ACUnit": ac_unit, "WaterTower": water_tower, "Building": building,
    "Barrel": barrel, "Crane": crane, "Lighthouse": lighthouse, "Boat": boat,
    "Rock": rock, "Bush": bush, "Mushroom": mushroom,
    "Pipe": pipe, "SmokeStack": smokestack, "Gear": gear, "Tank": tank,
    "Cloud": cloud, "FloatRock": float_rock,
    "TileCrystal": crystal_tile, "TileTemple": temple_tile, "TileFortress": fortress_tile,
    "RampCrystal": stripe_tile((0.35, 0.2, 0.5), (0.3, 0.95, 1.0), emissive=1.5),
    "RampTemple": stripe_tile((0.55, 0.4, 0.25), (0.95, 0.8, 0.3)),
    "RampFortress": stripe_tile((0.2, 0.1, 0.16), (1.0, 0.2, 0.25), emissive=1.5),
    "RailCrystal": rail_crystal, "RailTemple": rail_temple, "RailFortress": rail_fortress,
    "Crystal": crystal_cluster, "CrystalBig": crystal_big, "Stalagmite": stalagmite, "GlowShroom": glow_shroom,
    "TemplePillar": temple_pillar, "Arch": temple_arch, "Brazier": brazier, "CatStatue": cat_statue,
    "Spire": spire, "Banner": banner,
}

if __name__ == "__main__":
    for name, fn in MODELS.items():
        if ONLY and name not in ONLY:
            continue
        build(name, fn)
    print("DONE")
