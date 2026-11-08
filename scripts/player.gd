extends CharacterBody3D

const WEAPONS := [
	{"name": "Pistol",  "dmg": 25,  "rate": 0.28, "mag": 12, "reload": 1.3, "spread": 0.010, "pellets": 1, "auto": false},
	{"name": "Rifle",   "dmg": 22,  "rate": 0.10, "mag": 30, "reload": 2.0, "spread": 0.020, "pellets": 1, "auto": true},
	{"name": "Shotgun", "dmg": 14,  "rate": 0.90, "mag": 6,  "reload": 2.4, "spread": 0.070, "pellets": 8, "auto": false},
	{"name": "Sniper",  "dmg": 110, "rate": 1.40, "mag": 5,  "reload": 2.8, "spread": 0.000, "pellets": 1, "auto": false},
]

var peer_id := 0
var pname := ""
var team := 0
var is_local := false
var game: Node

var hp := 100
var alive := true
var yaw := 0.0
var pitch := 0.0
var target_pos := Vector3.ZERO
var target_yaw := 0.0

var cam: Camera3D
var view: MeshInstance3D
var flash: OmniLight3D
var body_root: Node3D

var cur := 1
var ammo := [12, 30, 6, 5]
var cooldown := 0.0
var reload_t := 0.0
var grenades := 2
var sync_t := 0.0
var fire_held := false


func team_color() -> Color:
	return Color(0.25, 0.55, 1.0) if team == 0 else Color(1.0, 0.3, 0.3)

func _ready() -> void:
	collision_layer = 2
	collision_mask = 1 if is_local else 0
	var cs := CollisionShape3D.new()
	var cap := CapsuleShape3D.new()
	cap.radius = 0.4
	cap.height = 1.8
	cs.shape = cap
	cs.position.y = 0.9
	add_child(cs)
	if is_local:
		cam = Camera3D.new()
		cam.position.y = 1.65
		cam.fov = 80
		add_child(cam)
		cam.make_current()
		view = MeshInstance3D.new()
		var bm := BoxMesh.new()
		bm.size = Vector3(0.08, 0.1, 0.5)
		view.mesh = bm
		var vm := StandardMaterial3D.new()
		vm.albedo_color = Color(0.15, 0.15, 0.18)
		view.material_override = vm
		view.position = Vector3(0.22, -0.22, -0.5)
		cam.add_child(view)
		flash = OmniLight3D.new()
		flash.omni_range = 6.0
		flash.light_energy = 0.0
		flash.position = Vector3(0.22, -0.15, -1.1)
		cam.add_child(flash)
	else:
		body_root = Node3D.new()
		add_child(body_root)
		var mat := StandardMaterial3D.new()
		mat.albedo_color = team_color()
		var body := MeshInstance3D.new()
		var cm := CapsuleMesh.new()
		cm.radius = 0.4
		cm.height = 1.6
		body.mesh = cm
		body.material_override = mat
		body.position.y = 0.8
		body_root.add_child(body)
		var head := MeshInstance3D.new()
		var sm := SphereMesh.new()
		sm.radius = 0.22
		sm.height = 0.44
		head.mesh = sm
		head.position.y = 1.7
		var hm := StandardMaterial3D.new()
		hm.albedo_color = Color(0.9, 0.75, 0.6)
		head.material_override = hm
		body_root.add_child(head)
		var gun := MeshInstance3D.new()
		var gb := BoxMesh.new()
		gb.size = Vector3(0.08, 0.1, 0.6)
		gun.mesh = gb
		gun.position = Vector3(0.35, 1.2, -0.45)
		var gm := StandardMaterial3D.new()
		gm.albedo_color = Color(0.1, 0.1, 0.1)
		gun.material_override = gm
		body_root.add_child(gun)
		var lb := Label3D.new()
		lb.text = pname
		lb.font = Net.font
		lb.billboard = BaseMaterial3D.BILLBOARD_ENABLED
		lb.no_depth_test = true
		lb.pixel_size = 0.006
		lb.font_size = 48
		lb.modulate = team_color()
		lb.position.y = 2.3
		add_child(lb)

func respawn(pos: Vector3, y: float) -> void:
	global_position = pos
	velocity = Vector3.ZERO
	yaw = y
	pitch = 0.0
	target_pos = pos
	target_yaw = y
	rotation.y = y
	hp = 100
	alive = true
	visible = true
	collision_layer = 2
	if is_local:
		ammo = [12, 30, 6, 5]
		cur = 1
		grenades = 2
		reload_t = 0.0
		cooldown = 0.0
		cam.position.y = 1.65
		cam.rotation.z = 0.0
		view.visible = true

func on_death() -> void:
	alive = false
	if is_local:
		view.visible = false
	else:
		visible = false
		collision_layer = 0

func set_target(pos: Vector3, y: float) -> void:
	target_pos = pos
	target_yaw = y

func _unhandled_input(e: InputEvent) -> void:
	if not is_local:
		return
	if e is InputEventMouseMotion and Input.mouse_mode == Input.MOUSE_MODE_CAPTURED:
		yaw -= e.relative.x * 0.0025
		pitch = clampf(pitch - e.relative.y * 0.0025, -1.5, 1.5)
	elif e is InputEventKey and e.pressed and not e.echo:
		if not (alive and game.live):
			return
		match e.keycode:
			KEY_1: _switch(0)
			KEY_2: _switch(1)
			KEY_3: _switch(2)
			KEY_4: _switch(3)
			KEY_R: _reload()
			KEY_G: _throw()

func _switch(i: int) -> void:
	if i != cur:
		cur = i
		reload_t = 0.0
		cooldown = 0.25
		view.scale = Vector3(1, 1, [0.5, 1.0, 1.1, 1.5][i] / 1.0)

func _reload() -> void:
	if reload_t <= 0.0 and ammo[cur] < WEAPONS[cur]["mag"]:
		reload_t = WEAPONS[cur]["reload"]

func _throw() -> void:
	if grenades <= 0:
		return
	grenades -= 1
	var cb := cam.global_transform.basis
	Net.send_grenade(cam.global_position - cb.z * 0.8, -cb.z * 15.0 + Vector3.UP * 3.0 + velocity * 0.5)

func _physics_process(delta: float) -> void:
	if not is_local:
		global_position = global_position.lerp(target_pos, clampf(delta * 15.0, 0.0, 1.0))
		rotation.y = lerp_angle(rotation.y, target_yaw, clampf(delta * 15.0, 0.0, 1.0))
		return

	var captured := Input.mouse_mode == Input.MOUSE_MODE_CAPTURED
	var can: bool = alive and game.live and captured

	var inp := Vector2.ZERO
	if can:
		inp.x = int(Input.is_key_pressed(KEY_D)) - int(Input.is_key_pressed(KEY_A))
		inp.y = int(Input.is_key_pressed(KEY_S)) - int(Input.is_key_pressed(KEY_W))
	var dir := (Basis(Vector3.UP, yaw) * Vector3(inp.x, 0, inp.y)).normalized()
	var spd := 3.5 if Input.is_key_pressed(KEY_SHIFT) else 7.0
	velocity.x = move_toward(velocity.x, dir.x * spd, 70.0 * delta)
	velocity.z = move_toward(velocity.z, dir.z * spd, 70.0 * delta)
	if is_on_floor():
		if can and Input.is_key_pressed(KEY_SPACE):
			velocity.y = 7.5
	else:
		velocity.y -= 22.0 * delta
	move_and_slide()

	rotation.y = yaw
	cam.rotation.x = pitch
	if alive:
		var zoom := can and cur == 3 and Input.is_mouse_button_pressed(MOUSE_BUTTON_RIGHT)
		cam.fov = lerpf(cam.fov, 25.0 if zoom else 80.0, delta * 12.0)
	else:
		cam.position.y = lerpf(cam.position.y, 0.3, delta * 3.0)
		cam.rotation.z = lerpf(cam.rotation.z, 0.6, delta * 3.0)

	cooldown = maxf(0.0, cooldown - delta)
	flash.light_energy = move_toward(flash.light_energy, 0.0, 40.0 * delta)
	view.position.z = lerpf(view.position.z, -0.5, delta * 15.0)
	if reload_t > 0.0:
		reload_t -= delta
		if reload_t <= 0.0:
			ammo[cur] = WEAPONS[cur]["mag"]

	var held := Input.is_mouse_button_pressed(MOUSE_BUTTON_LEFT)
	if can and reload_t <= 0.0 and cooldown <= 0.0 and held:
		if ammo[cur] <= 0:
			_reload()
		elif WEAPONS[cur]["auto"] or not fire_held:
			_shoot()
	fire_held = held

	sync_t += delta
	if sync_t >= 0.05:
		sync_t = 0.0
		Net.send_sync(global_position, yaw, pitch)

func _shoot() -> void:
	var w = WEAPONS[cur]
	ammo[cur] -= 1
	cooldown = w["rate"]
	var space := get_world_3d().direct_space_state
	var origin := cam.global_position
	var cb := cam.global_transform.basis
	var dmg_map := {}
	var ends: Array = []
	var s: float = w["spread"]
	for i in int(w["pellets"]):
		var d := (-cb.z + cb.x * randf_range(-s, s) + cb.y * randf_range(-s, s)).normalized()
		var q := PhysicsRayQueryParameters3D.create(origin, origin + d * 150.0)
		q.exclude = [get_rid()]
		var res := space.intersect_ray(q)
		var end := origin + d * 150.0
		if not res.is_empty():
			end = res["position"]
			var c = res["collider"]
			if c is CharacterBody3D and c != self and c.team != team and c.alive:
				var dm: float = w["dmg"]
				if end.y - c.global_position.y > 1.45:
					dm *= 2.0  # headshot
				dmg_map[c.peer_id] = dmg_map.get(c.peer_id, 0.0) + dm
		ends.append(end)
	for id in dmg_map:
		Net.send_hit(id, int(dmg_map[id]), w["name"])
		game.hitmarker()
	var muzzle := origin - cb.z * 0.9 + cb.x * 0.22 - cb.y * 0.2
	for e in ends:
		game.spawn_tracer(muzzle, e, team_color())
	Net.send_fire(cur, muzzle, ends)
	flash.light_energy = 3.0
	view.position.z = -0.4
	pitch = minf(pitch + 0.012 * (3.0 if cur == 3 else 1.0), 1.5)
