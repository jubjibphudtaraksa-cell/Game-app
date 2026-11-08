extends Node3D
# โลกของแมตช์: แมพ, ผู้เล่น, HUD, ระเบิด

signal finished

const PlayerScript := preload("res://scripts/player.gd")
const GrenadeScript := preload("res://scripts/grenade.gd")
const MAP_NAMES := ["Arena", "Warehouse", "Courtyard"]

var players := {}
var me: Node = null
var live := false
var round_left := 0.0
var feed: Array = []
var msg_t := 0.0
var hud_root: Control
var l_hp: Label
var l_ammo: Label
var l_score: Label
var l_msg: Label
var l_feed: Label
var l_board: Label
var l_help: Label
var l_hit: Label
var hurt: ColorRect


func setup() -> void:
	_build_world(int(Net.room.map))
	_build_hud()
	Input.mouse_mode = Input.MOUSE_MODE_CAPTURED
	for id in Net.room.players:
		_spawn_player(id)
	Net.game_event.connect(_on_event)
	Net.room_changed.connect(_on_room_changed)

# ------------------------------------------------------------------ world
func _layout(m: int) -> Array:
	# [x, z, กว้าง, สูง, ลึก]  (สร้างสะท้อนหมุน 180° ให้สมมาตร)
	match m:
		0:
			return [[0,0,8,3,2],[-12,8,6,2.2,2],[10,12,2,3.5,8],[0,18,6,1.5,2],[-18,18,2,4,6],[8,5,3,1,3],[-4,12,2,1,2]]
		1:
			return [[-16,6,5,2.4,3],[-6,6,5,2.4,3],[6,10,5,2.4,3],[16,10,5,2.4,3],[-10,16,8,3,1],[10,20,8,3,1],[0,24,3,1,3],[-22,22,2,0.8,2],[-19,22,2,1.6,2]]
		_:
			return [[-12,0,16,4,1],[-8,10,2,6,2],[8,10,2,6,2],[0,14,6,2,2],[-20,14,2,6,2],[16,20,10,3,1],[-4,22,2,2,6]]

func _box(pos: Vector3, size: Vector3, col: Color) -> void:
	var b := StaticBody3D.new()
	b.position = pos
	var mi := MeshInstance3D.new()
	var bm := BoxMesh.new()
	bm.size = size
	mi.mesh = bm
	var mat := StandardMaterial3D.new()
	mat.albedo_color = col
	mi.material_override = mat
	var cs := CollisionShape3D.new()
	var sh := BoxShape3D.new()
	sh.size = size
	cs.shape = sh
	b.add_child(mi)
	b.add_child(cs)
	add_child(b)

func _build_world(m: int) -> void:
	var skies := [Color(0.45, 0.65, 0.9), Color(0.1, 0.1, 0.15), Color(0.9, 0.6, 0.4)]
	var we := WorldEnvironment.new()
	var e := Environment.new()
	e.background_mode = Environment.BG_COLOR
	e.background_color = skies[m]
	e.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	e.ambient_light_color = Color(0.75, 0.75, 0.8)
	e.ambient_light_energy = 0.4 if m == 1 else 0.7
	we.environment = e
	add_child(we)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-55, 35, 0)
	sun.shadow_enabled = true
	sun.light_energy = 0.6 if m == 1 else 1.0
	add_child(sun)
	_box(Vector3(0, -0.5, 0), Vector3(64, 1, 64), Color(0.28, 0.3, 0.32))
	var wc := Color(0.2, 0.22, 0.28)
	_box(Vector3(0, 4, 32.5), Vector3(66, 8, 1), wc)
	_box(Vector3(0, 4, -32.5), Vector3(66, 8, 1), wc)
	_box(Vector3(32.5, 4, 0), Vector3(1, 8, 66), wc)
	_box(Vector3(-32.5, 4, 0), Vector3(1, 8, 66), wc)
	for b in _layout(m):
		var size := Vector3(b[2], b[3], b[4])
		var col := Color.from_hsv(fposmod(absf(b[0] * 0.013 + b[1] * 0.021), 1.0), 0.3, 0.7)
		_box(Vector3(b[0], b[3] / 2.0, b[1]), size, col)
		if not (b[0] == 0 and b[1] == 0):
			_box(Vector3(-b[0], b[3] / 2.0, -b[1]), size, col)

func _spawn_xf(id: int) -> Array:
	var t: int = Net.room.players[id].team
	var mates: Array = []
	for k in Net.room.players:
		if Net.room.players[k].team == t:
			mates.append(k)
	mates.sort()
	var i := mates.find(id)
	var x := (i - (mates.size() - 1) / 2.0) * 3.0
	return [Vector3(x, 0.1, -26.0 if t == 0 else 26.0), PI if t == 0 else 0.0]

func _spawn_player(id: int) -> void:
	var info = Net.room.players[id]
	var p := PlayerScript.new()
	p.name = str(id)
	p.peer_id = id
	p.team = info.team
	p.pname = info.name
	p.is_local = id == Net.my_id()
	p.game = self
	add_child(p)
	players[id] = p
	if p.is_local:
		me = p
	var xf := _spawn_xf(id)
	p.respawn(xf[0], xf[1])

# ------------------------------------------------------------------ HUD
func _lbl(preset: int, size: int) -> Label:
	var l := Label.new()
	l.theme = Net.theme_ui
	l.add_theme_font_size_override("font_size", size)
	l.add_theme_color_override("font_outline_color", Color.BLACK)
	l.add_theme_constant_override("outline_size", 6)
	l.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hud_root.add_child(l)
	l.set_anchors_and_offsets_preset(preset, Control.PRESET_MODE_MINSIZE, 24)
	var right: bool = preset in [Control.PRESET_TOP_RIGHT, Control.PRESET_BOTTOM_RIGHT]
	var bottom: bool = preset in [Control.PRESET_BOTTOM_LEFT, Control.PRESET_BOTTOM_RIGHT, Control.PRESET_CENTER_BOTTOM]
	var mid: bool = preset in [Control.PRESET_CENTER_TOP, Control.PRESET_CENTER, Control.PRESET_CENTER_BOTTOM]
	l.grow_horizontal = Control.GROW_DIRECTION_BOTH if mid else (Control.GROW_DIRECTION_BEGIN if right else Control.GROW_DIRECTION_END)
	l.grow_vertical = Control.GROW_DIRECTION_BEGIN if bottom else (Control.GROW_DIRECTION_BOTH if preset == Control.PRESET_CENTER else Control.GROW_DIRECTION_END)
	if mid:
		l.horizontal_alignment = HORIZONTAL_ALIGNMENT_CENTER
	if right:
		l.horizontal_alignment = HORIZONTAL_ALIGNMENT_RIGHT
	return l

func _build_hud() -> void:
	var cl := CanvasLayer.new()
	add_child(cl)
	hud_root = Control.new()
	hud_root.set_anchors_preset(Control.PRESET_FULL_RECT)
	hud_root.mouse_filter = Control.MOUSE_FILTER_IGNORE
	cl.add_child(hud_root)
	hurt = ColorRect.new()
	hurt.color = Color(1, 0, 0, 0)
	hurt.set_anchors_preset(Control.PRESET_FULL_RECT)
	hurt.mouse_filter = Control.MOUSE_FILTER_IGNORE
	hud_root.add_child(hurt)
	var cross := _lbl(Control.PRESET_CENTER, 28)
	cross.text = "+"
	l_hit = _lbl(Control.PRESET_CENTER, 40)
	l_hit.text = ""
	l_hit.add_theme_color_override("font_color", Color(1, 0.3, 0.3))
	l_hp = _lbl(Control.PRESET_BOTTOM_LEFT, 44)
	l_ammo = _lbl(Control.PRESET_BOTTOM_RIGHT, 30)
	l_score = _lbl(Control.PRESET_CENTER_TOP, 28)
	l_msg = _lbl(Control.PRESET_CENTER_TOP, 52)
	l_msg.position.y += 90
	l_feed = _lbl(Control.PRESET_TOP_RIGHT, 18)
	l_board = _lbl(Control.PRESET_CENTER, 22)
	l_help = _lbl(Control.PRESET_BOTTOM_LEFT, 14)
	l_help.position.y -= 70
	l_help.text = "WASD เดิน | Space กระโดด | Shift เดินช้า | คลิกซ้าย ยิง | 1-4 เปลี่ยนปืน | R รีโหลด | G ระเบิด | Tab คะแนน | Esc เมาส์ | L ออกจากแมตช์"

func hitmarker() -> void:
	l_hit.text = "✕"
	get_tree().create_timer(0.12).timeout.connect(func(): if is_instance_valid(l_hit): l_hit.text = "")

func show_msg(t: String, secs: float = 2.5) -> void:
	l_msg.text = t
	msg_t = secs

func _process(delta: float) -> void:
	if me == null:
		return
	l_hp.text = "HP %d" % me.hp
	var w = PlayerScript.WEAPONS[me.cur]
	l_ammo.text = "%s  %d / %d%s\nGrenade x%d" % [w["name"], me.ammo[me.cur], w["mag"], "  (reloading)" if me.reload_t > 0 else "", me.grenades]
	var sc: Array = Net.room.get("score", [0, 0])
	if live:
		round_left = maxf(0.0, round_left - delta)
	l_score.text = "BLUE %d  :  %d RED     R%d   %d" % [sc[0], sc[1], Net.room.get("round", 0), int(round_left)]
	msg_t -= delta
	if msg_t <= 0.0:
		l_msg.text = ""
	hurt.color.a = maxf(0.0, hurt.color.a - delta * 1.5)
	l_board.visible = Input.is_key_pressed(KEY_TAB)
	if l_board.visible:
		var arr: Array = []
		for id in Net.room.players:
			arr.append(Net.room.players[id])
		arr.sort_custom(func(a, b): return a.kills > b.kills)
		var t := "ชื่อ              K / D\n"
		for p in arr:
			t += "%s %s   %d / %d\n" % ["■" if p.team == 0 else "□", p.name, p.kills, p.deaths]
		l_board.text = t

func _input(e: InputEvent) -> void:
	if e is InputEventKey and e.pressed and not e.echo:
		if e.keycode == KEY_ESCAPE:
			Input.mouse_mode = Input.MOUSE_MODE_VISIBLE if Input.mouse_mode == Input.MOUSE_MODE_CAPTURED else Input.MOUSE_MODE_CAPTURED
		elif e.keycode == KEY_L:
			Net.leave_room()
	elif e is InputEventMouseButton and e.pressed and Input.mouse_mode == Input.MOUSE_MODE_VISIBLE:
		Input.mouse_mode = Input.MOUSE_MODE_CAPTURED

# ------------------------------------------------------------------ events
func _on_room_changed(r: Dictionary) -> void:
	if r.is_empty():
		return
	for id in players.keys():
		if not r.players.has(id):
			players[id].queue_free()
			players.erase(id)

func _name_of(id: int) -> String:
	return Net.room.players[id].name if Net.room.players.has(id) else "?"

func _on_event(ev: String, d: Dictionary) -> void:
	match ev:
		"sync":
			if players.has(d.id):
				players[d.id].set_target(d.pos, d.yaw)
		"fire":
			if players.has(d.id):
				for e in d.ends:
					spawn_tracer(d.from, e, players[d.id].team_color())
		"grenade":
			var g := GrenadeScript.new()
			g.game = self
			g.thrower = d.id
			g.position = d.pos
			g.linear_velocity = d.vel
			add_child(g)
		"round":
			live = false
			for id in players:
				var xf := _spawn_xf(id)
				players[id].respawn(xf[0], xf[1])
			show_msg("รอบที่ %d" % Net.room.round, 3.0)
		"live":
			live = true
			round_left = d.secs
			show_msg("เริ่ม!", 1.0)
		"hit":
			var p = players.get(d.id)
			if p:
				p.hp = d.hp
				if d.hp <= 0:
					p.on_death()
			if d.id == Net.my_id():
				hurt.color.a = 0.45
			if d.hp <= 0:
				feed.append("%s [%s] %s" % [_name_of(d.attacker), d.weapon, _name_of(d.id)])
				if feed.size() > 5:
					feed.pop_front()
				l_feed.text = "\n".join(feed)
				if d.id == Net.my_id():
					show_msg("คุณตายแล้ว", 3.0)
		"round_end":
			live = false
			var t := "เสมอ" if d.winner < 0 else ("ทีมฟ้าชนะรอบนี้" if d.winner == 0 else "ทีมแดงชนะรอบนี้")
			show_msg(t, 3.0)
		"match_end":
			live = false
			show_msg("ทีมฟ้าชนะแมตช์!" if d.winner == 0 else "ทีมแดงชนะแมตช์!", 5.0)
			get_tree().create_timer(5.0).timeout.connect(_finish)

func _finish() -> void:
	finished.emit()

# ------------------------------------------------------------------ effects
func spawn_tracer(from: Vector3, to: Vector3, col: Color) -> void:
	var len := from.distance_to(to)
	if len < 0.1:
		return
	var mi := MeshInstance3D.new()
	var bm := BoxMesh.new()
	bm.size = Vector3(0.03, 0.03, len)
	mi.mesh = bm
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = col.lightened(0.5)
	mi.material_override = mat
	add_child(mi)
	var dir := (to - from).normalized()
	var up := Vector3.RIGHT if absf(dir.y) > 0.99 else Vector3.UP
	mi.look_at_from_position((from + to) / 2.0, to, up)
	get_tree().create_timer(0.07).timeout.connect(mi.queue_free)

func explode(pos: Vector3, thrower: int) -> void:
	var fx := MeshInstance3D.new()
	var sm := SphereMesh.new()
	sm.radius = 0.5
	sm.height = 1.0
	fx.mesh = sm
	var mat := StandardMaterial3D.new()
	mat.shading_mode = BaseMaterial3D.SHADING_MODE_UNSHADED
	mat.albedo_color = Color(1.0, 0.55, 0.1)
	fx.material_override = mat
	fx.position = pos
	add_child(fx)
	var l := OmniLight3D.new()
	l.light_energy = 6.0
	l.omni_range = 14.0
	l.light_color = Color(1, 0.6, 0.2)
	fx.add_child(l)
	var tw := create_tween()
	tw.tween_property(fx, "scale", Vector3.ONE * 12.0, 0.25)
	tw.tween_callback(fx.queue_free)
	if thrower != Net.my_id() or not live:
		return
	var space := get_world_3d().direct_space_state
	for id in players:
		var p = players[id]
		if not p.alive:
			continue
		var chest: Vector3 = p.global_position + Vector3(0, 1.0, 0)
		var dist := pos.distance_to(chest)
		if dist > 7.0:
			continue
		var q := PhysicsRayQueryParameters3D.create(pos + Vector3(0, 0.3, 0), chest)
		q.collision_mask = 1
		if not space.intersect_ray(q).is_empty():
			continue
		Net.send_hit(id, int(130.0 * (1.0 - dist / 7.0)) + 10, "Grenade")
