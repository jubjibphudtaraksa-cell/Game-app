extends Node
# หน้าล็อกอิน / ล็อบบี้ 3D / ห้อง

const GameScript := preload("res://scripts/game.gd")
const MAP_NAMES := ["Arena", "Warehouse", "Courtyard"]

var ui: CanvasLayer
var l3: Node3D
var cam: Camera3D
var avatar_root: Node3D
var screens := {}
var game: Node = null
var pending := {}
var t := 0.0
var refresh_t := 0.0
var cur_screen := "login"

var e_server: LineEdit
var e_user: LineEdit
var e_pass: LineEdit
var l_login_status: Label
var l_hello: Label
var e_rname: LineEdit
var o_mode: OptionButton
var s_max: SpinBox
var o_map: OptionButton
var e_code: LineEdit
var rooms_list: ItemList
var room_codes: Array = []
var l_menu_status: Label
var l_room_info: Label
var l_room_players: Label
var b_start: Button


func _ready() -> void:
	if Net.is_server:
		return
	randomize()
	_build_lobby3d()
	ui = CanvasLayer.new()
	add_child(ui)
	_build_login()
	_build_menu()
	_build_room()
	Net.connected.connect(_on_connected)
	Net.connect_failed.connect(func(): l_login_status.text = "เชื่อมต่อเซิร์ฟเวอร์ไม่ได้")
	Net.connection_lost.connect(_on_lost)
	Net.login_result.connect(_on_login)
	Net.rooms_received.connect(_on_rooms)
	Net.room_changed.connect(_on_room_changed)
	Net.match_started.connect(_on_match_started)
	Net.error_msg.connect(func(m): l_menu_status.text = m)
	_show("login")
	_refresh_avatars()


# ------------------------------------------------------------------ UI helpers
func _screen(key: String) -> Control:
	var c := Control.new()
	c.theme = Net.theme_ui
	c.set_anchors_preset(Control.PRESET_FULL_RECT)
	c.mouse_filter = Control.MOUSE_FILTER_IGNORE
	ui.add_child(c)
	screens[key] = c
	return c

func _box(parent: Control, preset: int, w: int) -> VBoxContainer:
	var p := PanelContainer.new()
	p.custom_minimum_size = Vector2(w, 0)
	parent.add_child(p)
	var m := MarginContainer.new()
	for s in ["left", "right", "top", "bottom"]:
		m.add_theme_constant_override("margin_" + s, 16)
	p.add_child(m)
	var v := VBoxContainer.new()
	v.add_theme_constant_override("separation", 8)
	m.add_child(v)
	p.set_anchors_and_offsets_preset.call_deferred(preset, Control.PRESET_MODE_MINSIZE, 24)
	return v

func _label(parent: Control, text: String, size: int = 18) -> Label:
	var l := Label.new()
	l.text = text
	l.add_theme_font_size_override("font_size", size)
	l.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	parent.add_child(l)
	return l

func _button(parent: Control, text: String, cb: Callable) -> Button:
	var b := Button.new()
	b.text = text
	b.pressed.connect(cb)
	parent.add_child(b)
	return b

func _edit(parent: Control, ph: String, secret: bool = false) -> LineEdit:
	var e := LineEdit.new()
	e.placeholder_text = ph
	e.secret = secret
	parent.add_child(e)
	return e

func _show(key: String) -> void:
	cur_screen = key
	for k in screens:
		screens[k].visible = (k == key)


# ------------------------------------------------------------------ screens
func _build_login() -> void:
	var s := _screen("login")
	var b := _box(s, Control.PRESET_CENTER, 400)
	_label(b, "ARENA STRIKE", 40)
	_label(b, "เกมยิงทีม 1v1 / 2v2 / 4v4", 16)
	e_server = _edit(b, "IP เซิร์ฟเวอร์")
	e_server.text = "127.0.0.1"
	e_user = _edit(b, "ชื่อผู้ใช้ (3-16 ตัว)")
	e_pass = _edit(b, "รหัสผ่าน", true)
	_button(b, "เข้าสู่ระบบ", func(): _do_login(false))
	_button(b, "สมัครสมาชิก (สร้างบัญชีใหม่)", func(): _do_login(true))
	_button(b, "เข้าสู่ระบบด้วย Gmail", func(): l_login_status.text = "Gmail ต้องตั้งค่า Google OAuth ก่อน (ดู README)")
	l_login_status = _label(b, "", 16)

func _build_menu() -> void:
	var s := _screen("menu")
	var left := _box(s, Control.PRESET_CENTER_LEFT, 360)
	l_hello = _label(left, "", 22)
	_label(left, "จับคู่เร็ว", 20)
	for mode in Net.MODES:
		_button(left, "Quick Match  " + mode, func(): Net.quick_match(mode))
	_label(left, "สร้างห้อง", 20)
	e_rname = _edit(left, "ชื่อห้อง")
	o_mode = OptionButton.new()
	for mode in Net.MODES:
		o_mode.add_item(mode)
	left.add_child(o_mode)
	var hb := HBoxContainer.new()
	left.add_child(hb)
	_label(hb, "จำนวนผู้เล่นสูงสุด")
	s_max = SpinBox.new()
	s_max.min_value = 2
	s_max.max_value = 2
	s_max.value = 2
	hb.add_child(s_max)
	o_mode.item_selected.connect(func(i: int):
		var cap: int = Net.MODES[o_mode.get_item_text(i)]
		s_max.max_value = cap
		s_max.value = cap)
	o_map = OptionButton.new()
	for m in MAP_NAMES:
		o_map.add_item(m)
	left.add_child(o_map)
	_button(left, "สร้างห้อง", func():
		Net.create_room(e_rname.text if e_rname.text != "" else Net.my_name + "'s room", o_mode.get_item_text(o_mode.selected), int(s_max.value), o_map.selected))
	l_menu_status = _label(left, "", 16)

	var right := _box(s, Control.PRESET_CENTER_RIGHT, 420)
	_label(right, "เข้าห้องด้วยรหัส", 20)
	var hb2 := HBoxContainer.new()
	right.add_child(hb2)
	e_code = LineEdit.new()
	e_code.placeholder_text = "รหัสห้อง เช่น AB3K9"
	e_code.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hb2.add_child(e_code)
	_button(hb2, "เข้าร่วม", func(): Net.join_room(e_code.text))
	_label(right, "ห้องที่เปิดอยู่ (ดับเบิลคลิกเพื่อเข้า)", 18)
	rooms_list = ItemList.new()
	rooms_list.custom_minimum_size = Vector2(0, 260)
	rooms_list.item_activated.connect(func(i: int): Net.join_room(room_codes[i]))
	right.add_child(rooms_list)
	_button(right, "รีเฟรชรายการห้อง", func(): Net.list_rooms())
	_button(right, "ออกจากระบบ", _logout)

func _logout() -> void:
	Net.multiplayer.multiplayer_peer = null
	Net.online = false
	Net.logged_in = false
	Net.room = {}
	_show("login")
	_refresh_avatars()

func _build_room() -> void:
	var s := _screen("room")
	var b := _box(s, Control.PRESET_CENTER_LEFT, 380)
	l_room_info = _label(b, "", 20)
	l_room_players = _label(b, "", 18)
	b_start = _button(b, "เริ่มเกม", func(): Net.start_match())
	_button(b, "ออกจากห้อง", func(): Net.leave_room())


# ------------------------------------------------------------------ 3D lobby
func _mat(c: Color, emit: bool = false) -> StandardMaterial3D:
	var m := StandardMaterial3D.new()
	m.albedo_color = c
	if emit:
		m.emission_enabled = true
		m.emission = c
		m.emission_energy_multiplier = 2.0
	return m

func _build_lobby3d() -> void:
	l3 = Node3D.new()
	add_child(l3)
	var we := WorldEnvironment.new()
	var e := Environment.new()
	e.background_mode = Environment.BG_COLOR
	e.background_color = Color(0.03, 0.04, 0.09)
	e.ambient_light_source = Environment.AMBIENT_SOURCE_COLOR
	e.ambient_light_color = Color(0.35, 0.4, 0.6)
	e.ambient_light_energy = 0.8
	e.glow_enabled = true
	we.environment = e
	l3.add_child(we)
	var sun := DirectionalLight3D.new()
	sun.rotation_degrees = Vector3(-45, 30, 0)
	sun.light_energy = 0.8
	l3.add_child(sun)
	var neon := OmniLight3D.new()
	neon.light_color = Color(0.2, 0.8, 1.0)
	neon.omni_range = 14.0
	neon.position = Vector3(0, 3, 2)
	l3.add_child(neon)
	var plat := MeshInstance3D.new()
	var cm := CylinderMesh.new()
	cm.top_radius = 6.0
	cm.bottom_radius = 6.0
	cm.height = 0.4
	plat.mesh = cm
	plat.material_override = _mat(Color(0.1, 0.12, 0.18))
	plat.position.y = -0.2
	l3.add_child(plat)
	var ring := MeshInstance3D.new()
	var tm := TorusMesh.new()
	tm.inner_radius = 5.8
	tm.outer_radius = 6.0
	ring.mesh = tm
	ring.material_override = _mat(Color(0.1, 0.8, 1.0), true)
	l3.add_child(ring)
	for i in 16:
		var a := TAU * i / 16.0
		var p := MeshInstance3D.new()
		var bm := BoxMesh.new()
		var h := 2.0 + (i % 4) * 1.5
		bm.size = Vector3(0.8, h, 0.8)
		p.mesh = bm
		p.material_override = _mat(Color.from_hsv(0.55 + (i % 3) * 0.1, 0.8, 1.0), true)
		p.position = Vector3(cos(a) * 14.0, h / 2.0 - 0.4, sin(a) * 14.0 - 4.0)
		l3.add_child(p)
	cam = Camera3D.new()
	l3.add_child(cam)
	cam.position = Vector3(0, 2.0, 7.5)
	cam.look_at(Vector3(0, 1.1, 0))
	cam.make_current()
	avatar_root = Node3D.new()
	l3.add_child(avatar_root)

func _make_avatar(pname: String, team: int) -> Node3D:
	var n := Node3D.new()
	var col := Color(0.25, 0.55, 1.0) if team == 0 else Color(1.0, 0.3, 0.3)
	var body := MeshInstance3D.new()
	var cm := CapsuleMesh.new()
	cm.radius = 0.4
	cm.height = 1.6
	body.mesh = cm
	body.material_override = _mat(col)
	body.position.y = 0.8
	n.add_child(body)
	var head := MeshInstance3D.new()
	var sm := SphereMesh.new()
	sm.radius = 0.22
	sm.height = 0.44
	head.mesh = sm
	head.material_override = _mat(Color(0.9, 0.75, 0.6))
	head.position.y = 1.7
	n.add_child(head)
	var gun := MeshInstance3D.new()
	var bm := BoxMesh.new()
	bm.size = Vector3(0.1, 0.12, 0.7)
	gun.mesh = bm
	gun.material_override = _mat(Color(0.1, 0.1, 0.1))
	gun.position = Vector3(0.35, 1.1, 0.4)
	n.add_child(gun)
	var lb := Label3D.new()
	lb.text = pname
	lb.font = Net.font
	lb.billboard = BaseMaterial3D.BILLBOARD_ENABLED
	lb.no_depth_test = true
	lb.pixel_size = 0.006
	lb.font_size = 56
	lb.modulate = col.lightened(0.3)
	lb.position.y = 2.3
	n.add_child(lb)
	return n

func _refresh_avatars() -> void:
	for c in avatar_root.get_children():
		c.queue_free()
	var list: Array = []
	var solo := Net.room.is_empty()
	if solo:
		list.append({"name": Net.my_name if Net.my_name != "" else "Player", "team": 0})
	else:
		for id in Net.room.players:
			list.append(Net.room.players[id])
	var cnt := [0, 0]
	for p in list:
		var tm: int = p.team
		var i: int = cnt[tm]
		cnt[tm] += 1
		var a := _make_avatar(p.name, tm)
		if solo:
			a.position = Vector3.ZERO
		else:
			a.position = Vector3((-3.2 if tm == 0 else 3.2) + (i % 2) * (1.5 if tm == 1 else -1.5), 0, -floorf(i / 2.0) * 1.6)
			a.rotation.y = 0.5 if tm == 0 else -0.5
		avatar_root.add_child(a)

func _process(d: float) -> void:
	if Net.is_server or game != null:
		return
	t += d
	cam.position = Vector3(sin(t * 0.25) * 2.0, 2.0, 7.5)
	cam.look_at(Vector3(0, 1.1, 0))
	var i := 0
	for a in avatar_root.get_children():
		a.position.y = sin(t * 2.0 + i) * 0.04
		i += 1
	if cur_screen == "menu":
		refresh_t += d
		if refresh_t > 3.0:
			refresh_t = 0.0
			Net.list_rooms()


# ------------------------------------------------------------------ net callbacks
func _do_login(reg: bool) -> void:
	pending = {"u": e_user.text, "p": e_pass.text, "r": reg}
	if Net.online:
		Net.login(pending.u, pending.p, pending.r)
		return
	l_login_status.text = "กำลังเชื่อมต่อ..."
	if Net.connect_to(e_server.text.strip_edges()) != OK:
		l_login_status.text = "ที่อยู่เซิร์ฟเวอร์ไม่ถูกต้อง"

func _on_connected() -> void:
	Net.login(pending.u, pending.p, pending.r)

func _on_login(ok: bool, msg: String) -> void:
	if not ok:
		l_login_status.text = msg
		return
	l_hello.text = "สวัสดี, " + msg
	l_menu_status.text = ""
	_show("menu")
	_refresh_avatars()
	Net.list_rooms()

func _on_lost() -> void:
	if game != null:
		_leave_game()
	_show("login")
	l_login_status.text = "การเชื่อมต่อหลุด"
	_refresh_avatars()

func _on_rooms(list: Array) -> void:
	rooms_list.clear()
	room_codes = []
	for r in list:
		rooms_list.add_item("%s  [%s]  %s  %d/%d  %s" % [r.code, r.mode, r.name, r.count, r.max, MAP_NAMES[r.map]])
		room_codes.append(r.code)

func _on_room_changed(r: Dictionary) -> void:
	if game != null:
		if r.is_empty():
			_leave_game()
		return
	_apply_room_ui()

func _apply_room_ui() -> void:
	var r := Net.room
	if r.is_empty():
		_show("menu")
		_refresh_avatars()
		Net.list_rooms()
		return
	_show("room")
	l_room_info.text = "รหัสห้อง: %s\n%s  |  โหมด %s  |  แมพ %s\nผู้เล่น %d/%d" % [r.code, r.name, r.mode, MAP_NAMES[r.map], r.players.size(), r.max]
	var blue := "ทีมฟ้า\n"
	var red := "ทีมแดง\n"
	for id in r.players:
		var line: String = "  • " + r.players[id].name + (" ★" if id == r.host else "") + "\n"
		if r.players[id].team == 0:
			blue += line
		else:
			red += line
	l_room_players.text = blue + "\n" + red
	b_start.visible = r.host == Net.my_id()
	_refresh_avatars()

func _on_match_started(_r: Dictionary) -> void:
	for s in screens.values():
		s.visible = false
	remove_child(l3)
	game = GameScript.new()
	add_child(game)
	game.setup()
	game.finished.connect(_leave_game)

func _leave_game() -> void:
	if game == null:
		return
	game.queue_free()
	game = null
	add_child(l3)
	cam.make_current()
	Input.mouse_mode = Input.MOUSE_MODE_VISIBLE
	if Net.logged_in:
		_apply_room_ui()
