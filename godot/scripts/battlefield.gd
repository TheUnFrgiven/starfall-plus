extends Control
## Responsive three-lane board shared by preparation and automatic combat.

signal cell_selected(lane: int, column: int)

const LANES: int = 3
const COLUMNS: int = 5
const EARTH_COLOR := Color("78c9de")
const TOWER_COLORS: Dictionary = {
	"Pebble": Color("5596d2"),
	"Prism": Color("f0b84d"),
	"Bricky": Color("df786d"),
	"Frost": Color("76c8d1"),
	"Poppy": Color("a67acb"),
}
const TOWER_SHAPES: Dictionary = {
	"Pebble": 0, "Prism": 1, "Bricky": 2, "Frost": 3, "Poppy": 4,
}

var towers: Dictionary = {}
var enemies: Array[Dictionary] = []
var ghost_cell: String = ""
var earth_worried: bool = false
var effects: Array[Dictionary] = []
var reduced_motion: bool = false
var animation_time: float = 0.0
var placement_mode: bool = false

func _process(delta: float) -> void:
	if not reduced_motion:
		animation_time += delta
		queue_redraw()

func set_board(next_towers: Dictionary, next_enemies: Array[Dictionary], worried: bool,
		next_effects: Array[Dictionary] = [], selected_cell: String = "", allow_placement: bool = false) -> void:
	towers = next_towers
	enemies = next_enemies
	earth_worried = worried
	effects = next_effects
	ghost_cell = selected_cell
	placement_mode = allow_placement
	queue_redraw()

func _draw() -> void:
	var board := Rect2(Vector2(12, 10), size - Vector2(24, 20))
	draw_rect(board, Color("f5f8ed"), true)
	var label_width := 54.0
	var left := board.position.x + label_width
	var right := board.end.x - 24.0
	var top := board.position.y + 13.0
	var bottom := board.end.y - 12.0
	var lane_height := (bottom - top) / LANES
	var cell_width := (right - left) / COLUMNS
	for lane: int in range(LANES):
		var lane_y := top + lane * lane_height
		var lane_color := Color("e5f0db") if lane % 2 == 0 else Color("edf3e5")
		draw_rect(Rect2(left, lane_y + 4.0, right - left, lane_height - 8.0), lane_color, true)
		draw_string(ThemeDB.fallback_font, Vector2(board.position.x + 12.0, lane_y + lane_height * 0.58),
			"LANE %d" % (lane + 1), HORIZONTAL_ALIGNMENT_LEFT, -1, 13, Color("55766c"))
		for column: int in range(COLUMNS):
			var cell := _cell_id(lane, column)
			var center := Vector2(left + (column + 0.5) * cell_width, lane_y + lane_height * 0.56)
			if column > 0:
				draw_arc(center, minf(22.0, lane_height * 0.23), 0.0, TAU, 24, Color("bfd1bb"), 2.0, true)
			if towers.has(cell):
				_draw_tower(center, towers[cell])
			elif ghost_cell == cell:
				draw_arc(center, 25.0, 0.0, TAU, 24, Color("edaa43"), 3.0, true)
			elif placement_mode and column > 0:
				draw_arc(center, 25.0, 0.0, TAU, 24, Color("edaa43"), 2.0, true)
			else:
				draw_circle(center, 5.0, Color("b9cdb6"))
	var earth_center := Vector2(left - 22.0, (top + bottom) * 0.5)
	if not reduced_motion:
		earth_center.y += sin(animation_time * 1.5) * 3.0
	var earth_radius := minf(31.0, lane_height * 0.29)
	draw_circle(earth_center, earth_radius + 5.0, Color("b8dbb0"))
	draw_circle(earth_center, earth_radius, EARTH_COLOR)
	draw_circle(earth_center + Vector2(-earth_radius * 0.4, -earth_radius * 0.3), earth_radius * 0.33, Color("92c96b"))
	draw_circle(earth_center + Vector2(earth_radius * 0.43, earth_radius * 0.37), earth_radius * 0.38, Color("92c96b"))
	var face := Color("254d45")
	for eye_x: float in [-earth_radius * 0.28, earth_radius * 0.28]:
		draw_circle(earth_center + Vector2(eye_x, 1.0), 2.7, face)
	if earth_worried:
		draw_arc(earth_center + Vector2(0.0, earth_radius * 0.4), earth_radius * 0.24, PI + 0.2, TAU - 0.2, 16, face, 2.4, true)
	else:
		draw_arc(earth_center + Vector2(0.0, 0.0), earth_radius * 0.25, 0.25, PI - 0.25, 16, face, 2.4, true)
	for enemy: Dictionary in enemies:
		var lane: int = int(enemy.get("lane", 0))
		var center := Vector2(left + float(enemy.get("x", 0.9)) * (right - left), top + (lane + 0.5) * lane_height)
		_draw_alien(center, enemy)
	for effect: Dictionary in effects:
		var from_point: Vector2 = effect.get("from", Vector2.ZERO)
		var to_point: Vector2 = effect.get("to", Vector2.ZERO)
		var from_screen := Vector2(left + from_point.x * (right - left), top + (from_point.y + 0.5) * lane_height)
		var to_screen := Vector2(left + to_point.x * (right - left), top + (to_point.y + 0.5) * lane_height)
		draw_line(from_screen, to_screen, Color("fff0a8"), 4.0, true)
		_draw_spark(to_screen, Color("ffd36a"))

func _draw_tower(center: Vector2, tower: Dictionary) -> void:
	var name: String = str(tower.get("type", "Pebble"))
	var power: int = int(tower.get("power", 1))
	var color: Color = TOWER_COLORS.get(name, Color("5596d2"))
	var radius := 15.0 + float(power - 1) * 2.5
	draw_circle(center + Vector2(0, 3), radius + 4, Color(0.2, 0.3, 0.25, 0.14))
	var points := _shape_points(center, radius, int(TOWER_SHAPES.get(name, 0)))
	draw_colored_polygon(points, color)
	var outline := points.duplicate()
	outline.append(points[0])
	draw_polyline(outline, Color("ffffff"), 2.0, true)
	if power > 1:
		draw_circle(center + Vector2(0, -radius - 8), 3.0, Color("ffe48a"))
	if int(tower.get("hp", 1)) < 2:
		draw_line(center + Vector2(-6, -5), center + Vector2(2, 3), Color("713e48"), 2.0, true)
		draw_line(center + Vector2(2, 3), center + Vector2(-3, 9), Color("713e48"), 2.0, true)

func _draw_alien(center: Vector2, enemy: Dictionary) -> void:
	var kind: String = str(enemy.get("kind", "scout"))
	var color := Color("e88970")
	if kind == "armored":
		color = Color("9578bd")
	elif kind == "swarm":
		color = Color("e8bd4e")
	var radius := 13.0 if kind != "swarm" else 10.0
	draw_circle(center + Vector2(0, 2), radius + 3, Color(0.2, 0.2, 0.2, 0.12))
	draw_circle(center, radius, color)
	for eye_x: float in [-4.5, 4.5]:
		draw_circle(center + Vector2(eye_x, -1.0), 2.4, Color("ffffff"))
		draw_circle(center + Vector2(eye_x + 0.5, -1.0), 1.15, Color("34434a"))
	if kind == "armored":
		draw_arc(center, radius + 4, PI, TAU, 20, Color("6d5b8a"), 3.0, true)
	var hp: int = int(enemy.get("hp", 1))
	var max_hp: int = int(enemy.get("max_hp", 1))
	if max_hp > 1:
		draw_rect(Rect2(center + Vector2(-14, radius + 4), Vector2(28, 4)), Color("c8d2c5"), true)
		draw_rect(Rect2(center + Vector2(-14, radius + 4), Vector2(28.0 * float(hp) / float(max_hp), 4)), Color("dd665c"), true)

func _draw_spark(center: Vector2, color: Color) -> void:
	for direction: Vector2 in [Vector2.LEFT, Vector2.RIGHT, Vector2.UP, Vector2.DOWN]:
		draw_line(center + direction * 4.0, center + direction * 12.0, color, 2.0, true)

func _shape_points(center: Vector2, radius: float, shape: int) -> PackedVector2Array:
	var shape_sides: Array[int] = [16, 3, 4, 4, 5]
	var count: int = shape_sides[clampi(shape, 0, 4)]
	var points := PackedVector2Array()
	for index: int in range(count):
		var angle := -PI * 0.5 + TAU * float(index) / float(count)
		if shape == 3:
			angle += PI * 0.25
		points.append(center + Vector2(cos(angle), sin(angle)) * radius)
	return points

func _cell_id(lane: int, column: int) -> String:
	return "%d:%d" % [lane, column]

func _gui_input(event: InputEvent) -> void:
	var point: Vector2 = Vector2.ZERO
	if event is InputEventMouseButton and event.pressed and event.button_index == MOUSE_BUTTON_LEFT:
		point = event.position
	elif event is InputEventScreenTouch and event.pressed:
		point = event.position
	else:
		return
	var board := Rect2(Vector2(12, 10), size - Vector2(24, 20))
	var label_width := 54.0
	var left := board.position.x + label_width
	var right := board.end.x - 24.0
	var top := board.position.y + 13.0
	var bottom := board.end.y - 12.0
	if point.x < left or point.x >= right or point.y < top or point.y >= bottom:
		return
	var lane_height := (bottom - top) / LANES
	var cell_width := (right - left) / COLUMNS
	var lane := clampi(int((point.y - top) / lane_height), 0, LANES - 1)
	var column := clampi(int((point.x - left) / cell_width), 0, COLUMNS - 1)
	if column == 0:
		return
	cell_selected.emit(lane, column)
	accept_event()
