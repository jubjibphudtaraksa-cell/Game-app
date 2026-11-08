extends RigidBody3D

var game: Node
var thrower := 0
var fuse := 2.4
var done := false

func _ready() -> void:
	collision_layer = 0
	collision_mask = 1
	var cs := CollisionShape3D.new()
	var s := SphereShape3D.new()
	s.radius = 0.15
	cs.shape = s
	add_child(cs)
	var m := MeshInstance3D.new()
	var sm := SphereMesh.new()
	sm.radius = 0.15
	sm.height = 0.3
	m.mesh = sm
	var mat := StandardMaterial3D.new()
	mat.albedo_color = Color(0.2, 0.5, 0.2)
	m.material_override = mat
	add_child(m)
	var pm := PhysicsMaterial.new()
	pm.bounce = 0.35
	pm.friction = 0.8
	physics_material_override = pm
	linear_damp = 0.2
	mass = 0.5

func _physics_process(d: float) -> void:
	fuse -= d
	if fuse <= 0.0 and not done:
		done = true
		game.explode(global_position, thrower)
		queue_free()
