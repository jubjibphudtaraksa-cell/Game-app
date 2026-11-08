extends Node
# Autoload "Net": ทำหน้าที่ทั้งฝั่ง Server (บัญชี/ห้อง/แมตช์/รอบ) และฝั่ง Client (RPC wrapper)
# รัน server:  godot --headless --path <โปรเจกต์> -- --server

const PORT := 24565
const MODES := {"1v1": 2, "2v2": 4, "4v4": 8}
const WIN_ROUNDS := 5
const ROUND_SECONDS := 100.0

signal connected
signal connect_failed
signal connection_lost
signal login_result(ok: bool, msg: String)
signal rooms_received(list: Array)
signal room_changed(room: Dictionary)
signal match_started(room: Dictionary)
signal game_event(ev: String, d: Dictionary)
signal error_msg(msg: String)

var is_server := false
var online := false
var logged_in := false
var my_name := ""
var room: Dictionary = {}
var font: SystemFont
var theme_ui: Theme

# ---- server state ----
var accounts := {}
var sessions := {}   # peer -> username
var rooms := {}      # code -> room dict
var peer_room := {}  # peer -> code


func _ready() -> void:
	font = SystemFont.new()
	font.font_names = PackedStringArray(["Thonburi", "Sukhumvit Set", "Leelawadee UI", "Noto Sans Thai", "Tahoma", "Arial"])
	font.allow_system_fallback = true
	theme_ui = Theme.new()
	theme_ui.default_font = font
	theme_ui.default_font_size = 18
	if "--server" in OS.get_cmdline_user_args() or DisplayServer.get_name() == "headless":
		_start_server()
	else:
		multiplayer.connected_to_server.connect(func(): online = true; connected.emit())
		multiplayer.connection_failed.connect(func(): connect_failed.emit())
		multiplayer.server_disconnected.connect(_on_server_lost)


# =====================================================================
# CLIENT API
# =====================================================================
func connect_to(addr: String) -> int:
	var peer := ENetMultiplayerPeer.new()
	var err := peer.create_client(addr, PORT)
	if err != OK:
		return err
	multiplayer.multiplayer_peer = peer
	return OK

func _on_server_lost() -> void:
	online = false
	logged_in = false
	room = {}
	multiplayer.multiplayer_peer = null
	connection_lost.emit()

func my_id() -> int:
	return multiplayer.get_unique_id()

func login(u: String, p: String, reg: bool) -> void: c_login.rpc_id(1, u, p, reg)
func list_rooms() -> void: c_list_rooms.rpc_id(1)
func create_room(n: String, mode: String, maxp: int, map_idx: int) -> void: c_create_room.rpc_id(1, n, mode, maxp, map_idx)
func join_room(code: String) -> void: c_join_room.rpc_id(1, code.strip_edges().to_upper())
func quick_match(mode: String) -> void: c_quick.rpc_id(1, mode)
func leave_room() -> void: c_leave.rpc_id(1)
func start_match() -> void: c_start.rpc_id(1)
func send_sync(pos: Vector3, yaw: float, pitch: float) -> void: c_sync.rpc_id(1, pos, yaw, pitch)
func send_fire(widx: int, from: Vector3, ends: Array) -> void: c_fire.rpc_id(1, widx, from, ends)
func send_hit(target: int, dmg: int, weapon: String) -> void: c_hit.rpc_id(1, target, dmg, weapon)
func send_grenade(pos: Vector3, vel: Vector3) -> void: c_grenade.rpc_id(1, pos, vel)


# ---- server -> client ----
@rpc("authority", "call_remote", "reliable")
func s_login_result(ok: bool, msg: String) -> void:
	if ok:
		logged_in = true
		my_name = msg
	login_result.emit(ok, msg)

@rpc("authority", "call_remote", "reliable")
func s_rooms(list: Array) -> void:
	rooms_received.emit(list)

@rpc("authority", "call_remote", "reliable")
func s_error(msg: String) -> void:
	error_msg.emit(msg)

@rpc("authority", "call_remote", "reliable")
func s_room_state(r: Dictionary) -> void:
	room = r
	room_changed.emit(r)

@rpc("authority", "call_remote", "reliable")
func s_start(r: Dictionary) -> void:
	room = r
	match_started.emit(r)

@rpc("authority", "call_remote", "reliable")
func s_round(r: Dictionary) -> void:
	room = r
	game_event.emit("round", {})

@rpc("authority", "call_remote", "reliable")
func s_live(secs: float) -> void:
	game_event.emit("live", {"secs": secs})

@rpc("authority", "call_remote", "reliable")
func s_hit(target: int, hp: int, attacker: int, weapon: String) -> void:
	if room.has("players") and room.players.has(target):
		room.players[target].hp = hp
		room.players[target].alive = hp > 0
		if hp <= 0:
			room.players[target].deaths += 1
			if attacker != target and room.players.has(attacker):
				room.players[attacker].kills += 1
	game_event.emit("hit", {"id": target, "hp": hp, "attacker": attacker, "weapon": weapon})

@rpc("authority", "call_remote", "reliable")
func s_round_end(winner: int, score: Array) -> void:
	room.score = score
	game_event.emit("round_end", {"winner": winner})

@rpc("authority", "call_remote", "reliable")
func s_match_end(winner: int, r: Dictionary) -> void:
	room = r
	game_event.emit("match_end", {"winner": winner})

@rpc("authority", "call_remote", "unreliable_ordered")
func s_sync(id: int, pos: Vector3, yaw: float, pitch: float) -> void:
	game_event.emit("sync", {"id": id, "pos": pos, "yaw": yaw, "pitch": pitch})

@rpc("authority", "call_remote", "reliable")
func s_fire(id: int, widx: int, from: Vector3, ends: Array) -> void:
	game_event.emit("fire", {"id": id, "widx": widx, "from": from, "ends": ends})

@rpc("authority", "call_remote", "reliable")
func s_grenade(id: int, pos: Vector3, vel: Vector3) -> void:
	game_event.emit("grenade", {"id": id, "pos": pos, "vel": vel})


# =====================================================================
# SERVER
# =====================================================================
func _start_server() -> void:
	is_server = true
	_load_accounts()
	var peer := ENetMultiplayerPeer.new()
	var err := peer.create_server(PORT, 128)
	if err != OK:
		push_error("เปิด server ไม่ได้ (พอร์ต %d ถูกใช้อยู่?)" % PORT)
		return
	multiplayer.multiplayer_peer = peer
	multiplayer.peer_disconnected.connect(func(id: int): _leave(id, false); sessions.erase(id))
	print("[Arena Strike] Server พร้อมแล้ว UDP port ", PORT)

func _load_accounts() -> void:
	if FileAccess.file_exists("user://accounts.json"):
		var f := FileAccess.open("user://accounts.json", FileAccess.READ)
		var d = JSON.parse_string(f.get_as_text())
		if d is Dictionary:
			accounts = d

func _save_accounts() -> void:
	var f := FileAccess.open("user://accounts.json", FileAccess.WRITE)
	f.store_string(JSON.stringify(accounts))

func _process(_d: float) -> void:
	if not is_server:
		return
	var now := Time.get_ticks_msec()
	for code in rooms.keys():
		var r = rooms[code]
		if r.state == "playing" and r.live and now > r.deadline:
			_end_round(code, -1)

func _room_of(id: int) -> Dictionary:
	return rooms.get(peer_room.get(id, ""), {})

func _new_code() -> String:
	var chars := "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"
	var c := ""
	for i in 5:
		c += chars[randi() % chars.length()]
	return _new_code() if rooms.has(c) else c

func _broadcast_room(r: Dictionary) -> void:
	for p in r.players:
		s_room_state.rpc_id(p, r)


@rpc("any_peer", "call_remote", "reliable")
func c_login(uname: String, pw: String, register: bool) -> void:
	if not is_server: return
	var id := multiplayer.get_remote_sender_id()
	uname = uname.strip_edges()
	if uname.length() < 3 or uname.length() > 16 or pw.length() < 4:
		s_login_result.rpc_id(id, false, "ชื่อ 3-16 ตัวอักษร และรหัสผ่านอย่างน้อย 4 ตัว")
		return
	if sessions.values().has(uname):
		s_login_result.rpc_id(id, false, "บัญชีนี้ออนไลน์อยู่แล้ว")
		return
	if register:
		if accounts.has(uname):
			s_login_result.rpc_id(id, false, "ชื่อนี้ถูกใช้แล้ว")
			return
		var salt := str(randi()) + str(Time.get_ticks_usec())
		accounts[uname] = {"salt": salt, "hash": (salt + pw).sha256_text()}
		_save_accounts()
	else:
		if not accounts.has(uname) or accounts[uname]["hash"] != (accounts[uname]["salt"] + pw).sha256_text():
			s_login_result.rpc_id(id, false, "ชื่อหรือรหัสผ่านไม่ถูกต้อง")
			return
	sessions[id] = uname
	s_login_result.rpc_id(id, true, uname)

@rpc("any_peer", "call_remote", "reliable")
func c_list_rooms() -> void:
	if not is_server: return
	var id := multiplayer.get_remote_sender_id()
	if not sessions.has(id): return
	var list: Array = []
	for code in rooms:
		var r = rooms[code]
		if r.state == "lobby":
			list.append({"code": code, "name": r.name, "mode": r.mode, "max": r.max, "count": r.players.size(), "map": r.map})
	s_rooms.rpc_id(id, list)

@rpc("any_peer", "call_remote", "reliable")
func c_create_room(rname: String, mode: String, maxp: int, map_idx: int) -> void:
	if not is_server: return
	var id := multiplayer.get_remote_sender_id()
	if not sessions.has(id) or not MODES.has(mode): return
	_make_room(id, rname, mode, maxp, map_idx, false)

func _make_room(id: int, rname: String, mode: String, maxp: int, map_idx: int, auto: bool) -> void:
	_leave(id, true)
	var code := _new_code()
	var cap: int = MODES[mode]
	rooms[code] = {
		"code": code, "name": rname.substr(0, 20), "mode": mode, "max": clampi(maxp, 2, cap),
		"map": clampi(map_idx, 0, 2), "host": id, "state": "lobby", "auto": auto,
		"players": {}, "score": [0, 0], "round": 0, "live": false, "deadline": 0,
	}
	_join(id, code)

@rpc("any_peer", "call_remote", "reliable")
func c_join_room(code: String) -> void:
	if not is_server: return
	var id := multiplayer.get_remote_sender_id()
	if sessions.has(id):
		_join(id, code)

@rpc("any_peer", "call_remote", "reliable")
func c_quick(mode: String) -> void:
	if not is_server: return
	var id := multiplayer.get_remote_sender_id()
	if not sessions.has(id) or not MODES.has(mode): return
	for code in rooms:
		var r = rooms[code]
		if r.auto and r.mode == mode and r.state == "lobby" and r.players.size() < r.max:
			if _join(id, code):
				return
	_make_room(id, "Quick " + mode, mode, MODES[mode], randi() % 3, true)

@rpc("any_peer", "call_remote", "reliable")
func c_leave() -> void:
	if not is_server: return
	var id := multiplayer.get_remote_sender_id()
	_leave(id, true)

func _join(id: int, code: String) -> bool:
	var r = rooms.get(code)
	if r == null:
		s_error.rpc_id(id, "ไม่พบห้องรหัส " + code)
		return false
	if peer_room.get(id, "") == code:
		return true
	if r.state != "lobby":
		s_error.rpc_id(id, "ห้องนี้กำลังแข่งอยู่")
		return false
	if r.players.size() >= r.max:
		s_error.rpc_id(id, "ห้องเต็ม")
		return false
	var cnt := [0, 0]
	for p in r.players.values():
		cnt[p.team] += 1
	var cap := ceili(r.max / 2.0)
	var team := 0 if cnt[0] <= cnt[1] else 1
	if cnt[team] >= cap:
		team = 1 - team
	if cnt[team] >= cap:
		s_error.rpc_id(id, "ห้องเต็ม")
		return false
	_leave(id, true)
	r.players[id] = {"name": sessions[id], "team": team, "kills": 0, "deaths": 0, "hp": 100, "alive": true}
	peer_room[id] = code
	_broadcast_room(r)
	if r.auto and r.players.size() >= r.max:
		_start_match(code)
	return true

func _leave(id: int, notify: bool) -> void:
	var code: String = peer_room.get(id, "")
	if code == "":
		return
	peer_room.erase(id)
	var r = rooms.get(code)
	if r == null:
		return
	r.players.erase(id)
	if notify:
		s_room_state.rpc_id(id, {})
	if r.players.is_empty():
		rooms.erase(code)
		return
	if r.host == id:
		r.host = r.players.keys()[0]
	if r.state == "playing":
		var tot := [0, 0]
		for p in r.players.values():
			tot[p.team] += 1
		if tot[0] == 0 or tot[1] == 0:
			_end_match(code, 1 if tot[0] == 0 else 0)
			return
		_check_round(code)
	_broadcast_room(r)

@rpc("any_peer", "call_remote", "reliable")
func c_start() -> void:
	if not is_server: return
	var id := multiplayer.get_remote_sender_id()
	var r := _room_of(id)
	if r.is_empty() or r.host != id or r.state != "lobby": return
	var cnt := [0, 0]
	for p in r.players.values():
		cnt[p.team] += 1
	if cnt[0] == 0 or cnt[1] == 0:
		s_error.rpc_id(id, "ต้องมีผู้เล่นทั้งสองทีมก่อนเริ่ม")
		return
	_start_match(r.code)

func _start_match(code: String) -> void:
	var r = rooms[code]
	r.state = "playing"
	r.score = [0, 0]
	r.round = 0
	r.live = false
	for p in r.players.values():
		p.kills = 0
		p.deaths = 0
	for id in r.players:
		s_start.rpc_id(id, r)
	await get_tree().create_timer(1.5).timeout
	if rooms.has(code) and r.state == "playing":
		_new_round(code)

func _new_round(code: String) -> void:
	var r = rooms[code]
	r.round += 1
	r.live = false
	for p in r.players.values():
		p.hp = 100
		p.alive = true
	for id in r.players:
		s_round.rpc_id(id, r)
	var rn: int = r.round
	await get_tree().create_timer(3.0).timeout
	if rooms.has(code) and r.state == "playing" and r.round == rn:
		r.live = true
		r.deadline = Time.get_ticks_msec() + int(ROUND_SECONDS * 1000.0)
		for id in r.players:
			s_live.rpc_id(id, ROUND_SECONDS)

@rpc("any_peer", "call_remote", "reliable")
func c_hit(target: int, dmg: int, weapon: String) -> void:
	if not is_server: return
	var id := multiplayer.get_remote_sender_id()
	var r := _room_of(id)
	if r.is_empty() or r.state != "playing" or not r.live: return
	var shooter = r.players[id]
	var t = r.players.get(target)
	if t == null or not shooter.alive or not t.alive: return
	if t.team == shooter.team and target != id: return  # ไม่มี friendly fire
	t.hp = maxi(0, t.hp - clampi(dmg, 0, 200))
	if t.hp <= 0:
		t.alive = false
		t.deaths += 1
		if target != id:
			shooter.kills += 1
	for p in r.players:
		s_hit.rpc_id(p, target, t.hp, id, weapon)
	_check_round(r.code)

func _check_round(code: String) -> void:
	var r = rooms.get(code)
	if r == null or not r.live:
		return
	var alive := [0, 0]
	for p in r.players.values():
		if p.alive:
			alive[p.team] += 1
	if alive[0] > 0 and alive[1] > 0:
		return
	_end_round(code, 0 if alive[0] > 0 else (1 if alive[1] > 0 else -1))

func _end_round(code: String, winner: int) -> void:
	var r = rooms[code]
	r.live = false
	if winner >= 0:
		r.score[winner] += 1
	for p in r.players:
		s_round_end.rpc_id(p, winner, r.score)
	var rn: int = r.round
	await get_tree().create_timer(3.5).timeout
	if not rooms.has(code) or r.state != "playing" or r.round != rn:
		return
	if r.score[0] >= WIN_ROUNDS or r.score[1] >= WIN_ROUNDS:
		_end_match(code, 0 if r.score[0] > r.score[1] else 1)
	else:
		_new_round(code)

func _end_match(code: String, winner: int) -> void:
	var r = rooms[code]
	r.state = "lobby"
	r.live = false
	for p in r.players:
		s_match_end.rpc_id(p, winner, r)
	for p in r.players.values():
		p.hp = 100
		p.alive = true

@rpc("any_peer", "call_remote", "unreliable_ordered")
func c_sync(pos: Vector3, yaw: float, pitch: float) -> void:
	if not is_server: return
	var id := multiplayer.get_remote_sender_id()
	var r := _room_of(id)
	if r.is_empty() or r.state != "playing": return
	for p in r.players:
		if p != id:
			s_sync.rpc_id(p, id, pos, yaw, pitch)

@rpc("any_peer", "call_remote", "reliable")
func c_fire(widx: int, from: Vector3, ends: Array) -> void:
	if not is_server: return
	var id := multiplayer.get_remote_sender_id()
	var r := _room_of(id)
	if r.is_empty() or r.state != "playing": return
	for p in r.players:
		if p != id:
			s_fire.rpc_id(p, id, widx, from, ends)

@rpc("any_peer", "call_remote", "reliable")
func c_grenade(pos: Vector3, vel: Vector3) -> void:
	if not is_server: return
	var id := multiplayer.get_remote_sender_id()
	var r := _room_of(id)
	if r.is_empty() or r.state != "playing" or not r.live: return
	if not r.players[id].alive: return
	for p in r.players:
		s_grenade.rpc_id(p, id, pos, vel)
