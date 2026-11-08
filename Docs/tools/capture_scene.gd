# Renders a "hero shot" of a level for the menu backgrounds. Usage (project root, needs a GPU window):
#   godot --path . --script Docs/tools/capture_scene.gd --write-movie out.png --fixed-fps 30 --resolution 1920x1080 \
#         -- res://Scenes/Levels/Level1_Rooftops.tscn x y z yaw pitch distance fov
# The player stands still (Idle animation) at x y z, the camera orbits to yaw/pitch/distance. Frame 60 is the keeper.
extends SceneTree

var lvl
var p
var cam
var args
var f = 0

func _initialize():
	args = OS.get_cmdline_user_args()
	lvl = load(args[0]).instantiate()
	root.add_child(lvl)
	p = lvl.get_node("Player")
	p.global_position = Vector3(float(args[1]), float(args[2]), float(args[3]))
	cam = lvl.get_node("CameraRig")
	cam.SetView(float(args[4]), float(args[5]), float(args[6]))
	cam.Cam.fov = float(args[7])
	p.Stats.FovMin = float(args[7])
	lvl.get_node("Hud").visible = false
	for n in ["Fish", "Enemies"]:
		if lvl.has_node(n): lvl.get_node(n).queue_free()

func _physics_process(_d):
	f += 1
	cam.Cam.fov = float(args[7])
	if f >= 62:
		quit()
	return false
