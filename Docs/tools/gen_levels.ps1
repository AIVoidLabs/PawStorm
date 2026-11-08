# Generates Scenes/Levels/Level2..6 (placeholder greybox levels). Run from the project root:
#   powershell -File Docs/tools/gen_levels.ps1
$ErrorActionPreference = 'Stop'
$ci = [Globalization.CultureInfo]::InvariantCulture
$utf8 = New-Object Text.UTF8Encoding($false)
function F($x) { ([double]$x).ToString('0.####', $ci) }
function V($a, $b, $c) { "Vector3($(F $a), $(F $b), $(F $c))" }
function T($x, $y, $z) { "Transform3D(1, 0, 0, 0, 1, 0, 0, 0, 1, $(F $x), $(F $y), $(F $z))" }
function XF($ang, $x, $y, $z) { $c = [math]::Cos($ang); $s = [math]::Sin($ang); "Transform3D(1, 0, 0, 0, $(F $c), $(F (-$s)), 0, $(F $s), $(F $c), $(F $x), $(F $y), $(F $z))" }

function New-Level($def) {
	$script:n = New-Object Text.StringBuilder
	$script:sub = New-Object Text.StringBuilder
	$script:uid = 0
	function Add($s) { [void]$script:n.AppendLine($s) }
	function AddSub($s) { [void]$script:sub.AppendLine($s) }

	# ---- node helpers (top surface at $top) ----
	function Plat($name, [double]$x, [double]$top, [double]$z, [double]$w, [double]$l, $mat = 'ground', [double]$thick = 1) {
		Add "`n[node name=`"$name`" type=`"CSGBox3D`" parent=`"Geometry`"]`ntransform = $(T $x ($top - $thick / 2) $z)`nuse_collision = true`nsize = $(V $w $thick $l)`nmaterial = SubResource(`"$mat`")"
	}
	function Slope($name, [double]$x, [double]$y0, [double]$z0, [double]$len, [double]$w, [double]$deg, $mat = 'accent') {
		$a = $deg * [math]::PI / 180; $s = [math]::Sin($a); $c = [math]::Cos($a); $t = 1
		$cy = $y0 + ($len / 2) * $s - ($t / 2) * $c; $cz = $z0 - ($len / 2) * $c - ($t / 2) * $s
		Add "`n[node name=`"$name`" type=`"CSGBox3D`" parent=`"Geometry`"]`ntransform = $(XF $a $x $cy $cz)`nuse_collision = true`nsize = $(V $w $t $len)`nmaterial = SubResource(`"$mat`")"
	}
	function Trunk($name, [double]$x, [double]$z, [double]$h, [double]$r = 1.2, $mat = 'accent') {
		Add "`n[node name=`"$name`" type=`"CSGCylinder3D`" parent=`"Geometry`" groups=[`"climbable`"]]`ntransform = $(T $x ($h / 2) $z)`nuse_collision = true`nradius = $(F $r)`nheight = $(F $h)`nmaterial = SubResource(`"$mat`")"
	}
	function Wall($name, [double]$x, [double]$z, [double]$h, [double]$w, [double]$l, $mat = 'accent') {
		Add "`n[node name=`"$name`" type=`"CSGBox3D`" parent=`"Geometry`" groups=[`"climbable`"]]`ntransform = $(T $x ($h / 2) $z)`nuse_collision = true`nsize = $(V $w $h $l)`nmaterial = SubResource(`"$mat`")"
	}
	function Mover($name, [double]$x, [double]$y, [double]$z, [double]$ax, [double]$ay, [double]$az, [double]$freq) {
		Add "`n[node name=`"$name`" type=`"AnimatableBody3D`" parent=`".`"]`ntransform = $(T $x $y $z)`nscript = ExtResource(`"6`")`nAmplitude = $(V $ax $ay $az)`nFrequency = $(F $freq)`nSpinSpeed = 0.0`n`n[node name=`"Shape`" type=`"CollisionShape3D`" parent=`"$name`"]`nshape = SubResource(`"plat_shape`")`n`n[node name=`"Mesh`" type=`"MeshInstance3D`" parent=`"$name`"]`nmesh = SubResource(`"plat_mesh`")"
	}
	function Rail($name, $pts) {
		$s = ($pts | ForEach-Object { "$(F $_[0]), $(F $_[1]), $(F $_[2])" }) -join ', '
		Add "`n[node name=`"$name`" type=`"Path3D`" parent=`"Rails`"]`nscript = ExtResource(`"7`")`nPoints = PackedVector3Array($s)"
	}
	function Wind($name, [double]$x, [double]$y, [double]$z, [double]$sx, [double]$sy, [double]$sz, [double]$ax, [double]$ay, [double]$az) {
		$script:uid++; $id = "wind_$($script:uid)"
		AddSub "[sub_resource type=`"BoxShape3D`" id=`"$id`"]`nsize = $(V $sx $sy $sz)`n"
		Add "`n[node name=`"$name`" type=`"Area3D`" parent=`".`"]`ntransform = $(T $x $y $z)`nscript = ExtResource(`"5`")`nAcceleration = $(V $ax $ay $az)`n`n[node name=`"Shape`" type=`"CollisionShape3D`" parent=`"$name`"]`nshape = SubResource(`"$id`")"
	}
	function Loop($name, [double]$x, [double]$z) {
		Add "`n[node name=`"$name`" type=`"Area3D`" parent=`".`"]`ntransform = $(T $x 0 $z)`nscript = ExtResource(`"9`")`nRadius = 8.0`nLateralOffset = -4.0`n`n[node name=`"Trigger`" type=`"CollisionShape3D`" parent=`"$name`"]`ntransform = $(T 0 1.5 0)`nshape = SubResource(`"loop_trigger`")"
	}
	$script:fishN = 0
	function FishAt([double]$x, [double]$y, [double]$z) { $script:fishN++; Add "`n[node name=`"F$($script:fishN)`" parent=`"Fish`" instance=ExtResource(`"2`")]`ntransform = $(T $x $y $z)" }
	function FishLine([double]$x, [double]$y, [double]$z0, [double]$z1, [int]$cnt) { for ($i = 0; $i -lt $cnt; $i++) { FishAt $x $y ($z0 + ($z1 - $z0) * $i / [math]::Max(1, $cnt - 1)) } }
	function FishArc([double]$x, [double]$y, [double]$z0, [double]$z1, [int]$cnt, [double]$hgt = 0) { for ($i = 0; $i -lt $cnt; $i++) { $t = $i / [math]::Max(1, $cnt - 1); FishAt $x ($y + $hgt * 4 * $t * (1 - $t)) ($z0 + ($z1 - $z0) * $t) } }
	$script:enN = 0
	function Enemy([double]$x, [double]$y, [double]$z, [double]$ox, [double]$oy, [double]$oz) { $script:enN++; Add "`n[node name=`"E$($script:enN)`" parent=`"Enemies`" instance=ExtResource(`"10`")]`ntransform = $(T $x $y $z)`nPatrolOffset = $(V $ox $oy $oz)" }
	$script:tgN = 0
	function Target([double]$x, [double]$y, [double]$z) { $script:tgN++; Add "`n[node name=`"T$($script:tgN)`" type=`"Node3D`" parent=`"PounceTargets`"]`ntransform = $(T $x $y $z)`nscript = ExtResource(`"8`")`n`n[node name=`"Mesh`" type=`"MeshInstance3D`" parent=`"PounceTargets/T$($script:tgN)`"]`nmesh = SubResource(`"target_mesh`")" }
	$script:dcN = 0
	function Decor([string]$kind, [double]$x, [double]$y, [double]$z, [double]$sc = 1, [double]$rotY = 0) { $script:dcN++; $id = if ($kind -eq 'tree') { 20 } else { 21 }; $a = $rotY * [math]::PI / 180; $c = [math]::Cos($a) * $sc; $s = [math]::Sin($a) * $sc; Add "`n[node name=`"D$($script:dcN)`" parent=`"Decor`" instance=ExtResource(`"$id`")]`ntransform = Transform3D($(F $c), 0, $(F (-$s)), 0, $(F $sc), 0, $(F $s), 0, $(F $c), $(F $x), $(F $y), $(F $z))`nscript = ExtResource(`"23`")" }
	$script:cpN = 0
	function Check([double]$x, [double]$y, [double]$z) { $script:cpN++; Add "`n[node name=`"CP$($script:cpN)`" parent=`".`" instance=ExtResource(`"11`")]`ntransform = $(T $x $y $z)" }

	# ---- body ----
	Add "`n[node name=`"Geometry`" type=`"Node3D`" parent=`".`"]"
	& $def.Geometry
	Add "`n[node name=`"Rails`" type=`"Node3D`" parent=`".`"]"
	if ($def.Rails) { & $def.Rails }
	Add "`n[node name=`"PounceTargets`" type=`"Node3D`" parent=`".`"]"
	if ($def.Targets) { & $def.Targets }
	Add "`n[node name=`"Fish`" type=`"Node3D`" parent=`".`"]"
	if ($def.Fish) { & $def.Fish }
	Add "`n[node name=`"Enemies`" type=`"Node3D`" parent=`".`"]"
	if ($def.Enemies) { & $def.Enemies }
	Add "`n[node name=`"Decor`" type=`"Node3D`" parent=`".`"]"
	if ($def.Decor) { & $def.Decor }
	if ($def.Extra) { & $def.Extra }

	$c = $def.Colors
	$head = @"
[gd_scene load_steps=30 format=3]

[ext_resource type="Material" path="res://Art/Materials/Outline.tres" id="outline"]
[ext_resource type="PackedScene" path="res://Scenes/Player/Player.tscn" id="1"]
[ext_resource type="PackedScene" path="res://Scenes/Props/Fish.tscn" id="2"]
[ext_resource type="PackedScene" path="res://Scenes/UI/Hud.tscn" id="3"]
[ext_resource type="Script" path="res://Scripts/Camera/FollowCamera.cs" id="4"]
[ext_resource type="Script" path="res://Scripts/Core/WindZone.cs" id="5"]
[ext_resource type="Script" path="res://Scripts/Core/FloatingProp.cs" id="6"]
[ext_resource type="Script" path="res://Scripts/Core/GrindRail.cs" id="7"]
[ext_resource type="Script" path="res://Scripts/Enemies/PounceTarget.cs" id="8"]
[ext_resource type="Script" path="res://Scripts/Core/LoopTrack.cs" id="9"]
[ext_resource type="PackedScene" path="res://Scenes/Props/Enemy.tscn" id="10"]
[ext_resource type="PackedScene" path="res://Scenes/Props/Checkpoint.tscn" id="11"]
[ext_resource type="PackedScene" path="res://Scenes/Props/StormCore.tscn" id="12"]
[ext_resource type="Script" path="res://Scripts/Core/LevelController.cs" id="13"]
[ext_resource type="Script" path="res://Scripts/Enemies/DrKennel.cs" id="14"]
[ext_resource type="PackedScene" path="res://Art/Models/Tree.glb" id="20"]
[ext_resource type="PackedScene" path="res://Art/Models/Crate.glb" id="21"]
[ext_resource type="PackedScene" path="res://Art/Models/DrKennel.glb" id="22"]
[ext_resource type="Script" path="res://Scripts/Core/ToonModel.cs" id="23"]

[sub_resource type="StandardMaterial3D" id="ground"]
diffuse_mode = 3
specular_mode = 2
albedo_color = $($c.Ground)

[sub_resource type="StandardMaterial3D" id="accent"]
diffuse_mode = 3
specular_mode = 2
albedo_color = $($c.Accent)

[sub_resource type="StandardMaterial3D" id="plat_mat"]
diffuse_mode = 3
specular_mode = 2
albedo_color = $($c.Platform)

[sub_resource type="StandardMaterial3D" id="target_mat"]
diffuse_mode = 3
specular_mode = 2
next_pass = ExtResource("outline")
albedo_color = Color(1, 0.2, 0.2, 1)
emission_enabled = true
emission = Color(0.8, 0.1, 0.1, 1)

[sub_resource type="SphereMesh" id="target_mesh"]
material = SubResource("target_mat")
radius = 0.5
height = 1.0

[sub_resource type="BoxShape3D" id="plat_shape"]
size = Vector3(6, 0.6, 6)

[sub_resource type="BoxMesh" id="plat_mesh"]
material = SubResource("plat_mat")
size = Vector3(6, 0.6, 6)

[sub_resource type="BoxShape3D" id="loop_trigger"]
size = Vector3(4, 3, 2)

[sub_resource type="SphereShape3D" id="boss_shape"]
radius = 2.2

[sub_resource type="StandardMaterial3D" id="boss_coat"]
diffuse_mode = 3
specular_mode = 2
next_pass = ExtResource("outline")
albedo_color = Color(0.92, 0.92, 0.97, 1)

[sub_resource type="StandardMaterial3D" id="boss_fur"]
diffuse_mode = 3
specular_mode = 2
next_pass = ExtResource("outline")
albedo_color = Color(0.55, 0.35, 0.2, 1)

[sub_resource type="StandardMaterial3D" id="boss_glass"]
diffuse_mode = 3
specular_mode = 2
albedo_color = Color(1, 0.85, 0.2, 1)

[sub_resource type="CapsuleMesh" id="boss_body"]
material = SubResource("boss_coat")
radius = 1.1
height = 2.6

[sub_resource type="SphereMesh" id="boss_head"]
material = SubResource("boss_fur")
radius = 0.8
height = 1.6

[sub_resource type="SphereMesh" id="boss_goggle"]
material = SubResource("boss_glass")
radius = 0.3
height = 0.6
[sub_resource type="ProceduralSkyMaterial" id="skymat"]
sky_top_color = $($c.SkyTop)
sky_horizon_color = $($c.SkyHorizon)
ground_horizon_color = $($c.SkyHorizon)

[sub_resource type="Sky" id="sky"]
sky_material = SubResource("skymat")

[sub_resource type="Environment" id="env"]
background_mode = 2
sky = SubResource("sky")
ambient_light_source = 3
ambient_light_color = Color(0.8, 0.85, 1, 1)

"@
	$body = @"

[node name="$($def.NodeName)" type="Node3D"]

[node name="Env" type="WorldEnvironment" parent="."]
environment = SubResource("env")

[node name="Sun" type="DirectionalLight3D" parent="."]
transform = Transform3D(0.8, -0.35, 0.5, 0, 0.82, 0.57, -0.6, -0.46, 0.66, 0, 20, 0)
shadow_enabled = true

[node name="Player" parent="." instance=ExtResource("1")]
transform = $(T $def.Start[0] $def.Start[1] $def.Start[2])

[node name="CameraRig" type="Node3D" parent="." node_paths=PackedStringArray("Target", "Arm", "Cam")]
script = ExtResource("4")
Target = NodePath("../Player")
Arm = NodePath("Arm")
Cam = NodePath("Arm/Camera3D")

[node name="Arm" type="SpringArm3D" parent="CameraRig"]
spring_length = 6.0
margin = 0.3

[node name="Camera3D" type="Camera3D" parent="CameraRig/Arm"]
current = true

[node name="Hud" parent="." instance=ExtResource("3")]

[node name="LevelController" type="Node" parent="."]
script = ExtResource("13")
LevelTitle = "$($def.Title)"
"@
	$tail = ""
	if ($def.CoreAt) { $tail = "`n[node name=`"StormCore`" parent=`".`" instance=ExtResource(`"12`")]`ntransform = $(T $def.CoreAt[0] $def.CoreAt[1] $def.CoreAt[2])`n" }
	$text = $head + $script:sub.ToString() + $body + $script:n.ToString() + $tail
	[IO.File]::WriteAllText((Join-Path $PWD "Scenes/Levels/$($def.File)"), $text, $utf8)
	Write-Host "wrote $($def.File) (fish $($script:fishN), enemies $($script:enN))"
}

# ================= 2. HARBOR =================
New-Level @{
	File = 'Level2_Harbor.tscn'; NodeName = 'Harbor'; Title = 'Liman'; Start = @(0, 1, 5); CoreAt = @(0, 1.8, -232)
	Colors = @{ Ground = 'Color(0.6, 0.42, 0.25, 1)'; Accent = 'Color(0.25, 0.5, 0.65, 1)'; Platform = 'Color(0.8, 0.8, 0.85, 1)'; SkyTop = 'Color(0.95, 0.55, 0.35, 1)'; SkyHorizon = 'Color(1, 0.85, 0.6, 1)' }
	Geometry = {
		Plat 'Dock1' 0 0 -25 24 70
		Plat 'Dock2' 0 0 -112 24 76
		Plat 'Dock3' 0 0 -203 24 74
		Slope 'RampA' 0 0 -38 14 8 14
		Plat 'Crate1' -6 3 -12 3 3 'accent' 3
		Plat 'Crate2' 5 3 -92 3 3 'accent' 3
		Plat 'Crate3' 5 6 -92 3 3 'accent' 3
		Plat 'Crate4' -4 3 -130 3 3 'accent' 3
		Wall 'Container' -9 -100 8 6 8
	}
	Decor = { $i = 0; for ($z = -8; $z -gt -235; $z -= 17) { $i++; if (($z -lt -58 -and $z -gt -76) -or ($z -lt -148 -and $z -gt -168)) { continue }; Decor 'crate' 11 0 $z 1 ($i * 37); if ($i % 3 -eq 0) { Decor 'crate' 11 1.2 $z 0.9 ($i * 53) }; Decor 'crate' -11 0 ($z - 6) 1.1 ($i * 71) } }
	Rails = { Rail 'RailGap' @(@(0, 2.0, -146), @(0, 1.6, -158), @(0, 1.0, -170)) }
	Targets = { Target 0 5 -80; Target 3 5 -100; Target 0 4 -158 }
	Extra = {
		Mover 'Lift1' 0 -0.3 -67 6 0 0 0.2
		Loop 'Loop' 8 -8
		Check 0 0 -80; Check 0 0 -170
	}
	Fish = { FishLine 0 1.2 0 -22 8; FishArc 0 1.2 -32 -50 6 4; FishLine -9 9.2 -100 -100 1; FishLine 0 1.2 -90 -140 10; FishArc 0 3 -150 -168 6 2; FishLine 0 1.2 -175 -225 10 }
	Enemies = { Enemy -5 0 -30 8 0 0; Enemy 4 0 -95 0 0 10; Enemy 0 0 -120 8 0 0; Enemy 0 0 -185 6 0 0; Enemy 0 0 -215 0 0 8 }
}

# ================= 3. FOREST =================
New-Level @{
	File = 'Level3_Forest.tscn'; NodeName = 'Forest'; Title = 'Orman'; Start = @(0, 1, 5); CoreAt = @(0, 1.8, -242)
	Colors = @{ Ground = 'Color(0.3, 0.65, 0.28, 1)'; Accent = 'Color(0.5, 0.33, 0.2, 1)'; Platform = 'Color(0.4, 0.75, 0.35, 1)'; SkyTop = 'Color(0.3, 0.65, 0.95, 1)'; SkyHorizon = 'Color(0.8, 0.95, 0.85, 1)' }
	Geometry = {
		Plat 'GroundA' 0 0 -45 30 110
		Plat 'GroundB' 0 0 -200 30 120
		Slope 'Hill1Up' 0 0 -15 24 14 12
		Slope 'Hill1Down' 0 4.99 -38.5 24 14 -12
		Slope 'Hill2Up' 0 0 -60 20 14 15
		Slope 'Hill2Down' 0 5.18 -79.3 20 14 -15
		Trunk 'Tree1' -11 -30 14 2.5
		Trunk 'Tree2' 11 -165 14 2.5
		Plat 'LogStep1' 0 1 -170 6 3 'accent' 1
		Plat 'LogStep2' 0 2 -176 6 3 'accent' 1
	}
	Decor = { $i = 0; for ($z = 4; $z -gt -245; $z -= 13) { $i++; $sc = 1.3 + (($i * 7) % 5) * 0.15; Decor 'tree' 19 0 $z $sc ($i * 40); Decor 'tree' -19 0 ($z - 6) ($sc + 0.2) ($i * 25) } }
	Rails = { Rail 'RailRavine' @(@(0, 3.5, -102), @(0, 3.2, -120), @(0, 2.4, -138)) }
	Targets = { Target 0 4 -105; Target 0 5 -118; Target -2 4 -130 }
	Extra = {
		Loop 'Loop' -8 -92
		Check 0 0 -96; Check 0 0 -150
	}
	Fish = { FishLine 0 1.2 0 -12 5; FishArc 0 5.5 -20 -44 8 1; FishLine -11 15.2 -30 -30 1; FishArc 0 5.5 -60 -80 8 1; FishArc 0 3 -100 -140 10 4; FishLine 11 15.2 -165 -165 1; FishLine 0 1.2 -150 -235 14 }
	Enemies = { Enemy 4 0 -55 0 0 8; Enemy -5 0 -90 0 0 -8; Enemy 0 0 -155 8 0 0; Enemy 0 0 -190 8 0 0; Enemy 4 0 -210 0 0 8; Enemy -4 0 -225 0 0 -6 }
}

# ================= 4. FACTORY =================
New-Level @{
	File = 'Level4_Factory.tscn'; NodeName = 'Factory'; Title = 'Fabrika'; Start = @(0, 1, 5); CoreAt = @(0, 1.8, -252)
	Colors = @{ Ground = 'Color(0.45, 0.45, 0.5, 1)'; Accent = 'Color(0.95, 0.6, 0.15, 1)'; Platform = 'Color(0.85, 0.85, 0.2, 1)'; SkyTop = 'Color(0.4, 0.4, 0.5, 1)'; SkyHorizon = 'Color(0.75, 0.7, 0.65, 1)' }
	Geometry = {
		Plat 'Floor1' 0 0 -25 30 70
		Plat 'Floor2' 0 0 -115 30 70
		Plat 'Floor3' 0 0 -215 30 90
		Plat 'Crate1' 6 3 -20 3 3 'accent' 3
		Plat 'Crate2' -6 3 -100 3 3 'accent' 3
		Wall 'Shaft' 11 -125 12 6 8
		Slope 'Ramp1' 0 0 -50 12 8 12
	}
	Decor = { $i = 0; for ($z = -5; $z -gt -250; $z -= 20) { $i++; if (($z -lt -55 -and $z -gt -85) -or ($z -lt -145 -and $z -gt -178)) { continue }; Decor 'crate' 13.5 0 $z 1.2 ($i * 37); Decor 'crate' -13.5 0 ($z - 8) 1.1 ($i * 61) } }
	Rails = { Rail 'RailPit1' @(@(0, 2.2, -56), @(0, 2.0, -70), @(0, 1.2, -86)); Rail 'RailPit2' @(@(0, 2.2, -146), @(0, 2.0, -160), @(0, 1.2, -176)) }
	Targets = { Target 0 5 -72; Target 0 5 -158 }
	Extra = {
		Wind 'ConveyorBoost' 0 1 -25 28 4 30 0 0 -14
		Wind 'ConveyorBack' 0 1 -115 28 4 30 0 0 11
		Mover 'Lift1' 0 -0.3 -70 7 0 0 0.25
		Mover 'Lift2' 0 -0.3 -160 0 0 5 0.25
		Loop 'Loop' -9 -35
		Check 0 0 -95; Check 0 0 -185
	}
	Fish = { FishLine 0 1.2 0 -45 10; FishArc 0 1.2 -52 -90 10 5; FishLine 11 13.2 -125 -125 1; FishLine 0 1.2 -95 -140 8; FishArc 0 1.2 -148 -178 8 4; FishLine 0 1.2 -185 -245 12 }
	Enemies = { Enemy -5 0 -18 8 0 0; Enemy 5 0 -95 -8 0 0; Enemy 0 0 -110 0 0 6; Enemy 4 0 -190 0 0 8; Enemy -4 0 -200 8 0 0; Enemy 0 0 -225 8 0 0; Enemy 11 12 -125 0 0 2 }
}

# ================= 5. SKY ISLAND =================
New-Level @{
	File = 'Level5_SkyIsland.tscn'; NodeName = 'SkyIsland'; Title = 'Gökyüzü Adası'; Start = @(0, 1, 5); CoreAt = @(0, 1.8, -252)
	Colors = @{ Ground = 'Color(0.85, 0.9, 0.95, 1)'; Accent = 'Color(0.95, 0.75, 0.95, 1)'; Platform = 'Color(1, 0.95, 0.7, 1)'; SkyTop = 'Color(0.45, 0.35, 0.85, 1)'; SkyHorizon = 'Color(1, 0.85, 0.95, 1)' }
	Geometry = {
		Plat 'Island1' 0 0 -10 20 40
		Plat 'Island2' 0 0 -90 16 40
		Plat 'Island3' 0 0 -165 20 30
		Wall 'Cliff' 6 -165 12 6 8
		Plat 'Island5' 0 0 -240 24 40
	}
	Rails = { Rail 'RailSky' @(@(0, 2.5, -108), @(0, 2.0, -130), @(0, 1.0, -152)) }
	Targets = { Target 0 4 -42; Target 0 5 -54; Target 0 4 -66 }
	Extra = {
		Wind 'GustSide' 0 5 -50 40 20 40 8 0 0
		Wind 'Updraft' 0 10 -130 12 40 30 0 22 0
		Mover 'Step1' 0 -0.3 -190 0 2 0 0.3
		Mover 'Step2' 0 -0.3 -204 0 2 0 0.3
		Mover 'Step3' 0 -0.3 -218 0 2 0 0.3
		Check 0 0 -90; Check 0 0 -165
	}
	Fish = { FishLine 0 1.2 0 -25 8; FishArc 0 4 -34 -68 10 3; FishLine 0 1.2 -75 -105 8; FishArc 0 3 -115 -150 10 3; FishLine 6 13.2 -165 -165 1; FishLine 0 1.2 -230 -255 8 }
	Enemies = { Enemy -4 0 -20 8 0 0; Enemy 0 0 -85 6 0 0; Enemy 4 0 -165 -8 0 0; Enemy 0 0 -245 8 0 0 }
}

# ================= 6. BOSS =================
New-Level @{
	File = 'Level6_Boss.tscn'; NodeName = 'BossArena'; Title = 'Dr. Kennel'; Start = @(0, 1, 22); CoreAt = $null
	Colors = @{ Ground = 'Color(0.5, 0.5, 0.58, 1)'; Accent = 'Color(0.8, 0.2, 0.3, 1)'; Platform = 'Color(0.8, 0.8, 0.85, 1)'; SkyTop = 'Color(0.2, 0.15, 0.35, 1)'; SkyHorizon = 'Color(0.6, 0.4, 0.6, 1)' }
	Geometry = { Plat 'Arena' 0 0 0 60 60; Plat 'Pillar1' -22 4 -22 3 3 'accent' 8; Plat 'Pillar2' 22 4 -22 3 3 'accent' 8 }
	Fish = { for ($i = 0; $i -lt 12; $i++) { $a = 6.2832 * $i / 12; FishAt ([math]::Cos($a) * 18) 1.2 ([math]::Sin($a) * 18) } }
	Extra = {
		Add @"

[node name="Boss" type="Area3D" parent="."]
transform = $(T 0 3 -8)
script = ExtResource("14")

[node name="Collision" type="CollisionShape3D" parent="Boss"]
transform = $(T 0 1.2 0)
shape = SubResource("boss_shape")

[node name="Model" parent="Boss" instance=ExtResource("22")]
script = ExtResource("23")
"@
	}
	BossResources = $true
}
