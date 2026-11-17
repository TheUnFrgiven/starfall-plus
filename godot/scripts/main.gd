extends Control
## First playable curriculum mission. Provisional balance values are called out in README.

const BoardScript = preload("res://scripts/battlefield.gd")
const BucketScript = preload("res://scripts/reward_bucket.gd")
const QuestionScript = preload("res://scripts/question_factory.gd")
const SaveScript = preload("res://scripts/curriculum_save.gd")

const INK := Color("254d45")
const MUTED := Color("648078")
const PAPER := Color("fbfcf5")
const MINT := Color("e7f1df")
const GOLD := Color("f2c76e")
const MISSION_NAME := "First Contact"
const LEARNING_CHECK_TARGET: int = 2 # Provisional until evidence thresholds are designed.
const WAVE_POWER_CAPACITY: int = 4 # Provisional balance value; never shown as a number.
const TOWER_ROLES: Dictionary = {
	"Pebble": "Reliable single-target shots",
	"Prism": "Slow armor-piercing beam",
	"Bricky": "Blocks and protects",
	"Frost": "Slows a lane",
	"Poppy": "Splash damage for clusters",
}
const TOWER_COLORS: Dictionary = {
	"Pebble": Color("5596d2"),
	"Prism": Color("f0b84d"),
	"Bricky": Color("df786d"),
	"Frost": Color("76c8d1"),
	"Poppy": Color("a67acb"),
}
const TOWER_UNLOCK_WAVE: Dictionary = {"Pebble": 1, "Bricky": 1, "Prism": 1, "Frost": 2, "Poppy": 3}
const WAVE_ENEMY_COUNTS: Array[int] = [5, 7, 9]
const HELP_OFFER_DELAY: float = 8.0 # Provisional presentation timing.

var profile: Dictionary = {}
var unlocked_towers: Array[String] = []
var ui_root: VBoxContainer
var board: Control
var screen: String = "menu"
var tutorial_step: int = 0
var tutorial_in_progress: bool = false
var wave_number: int = 1
var earth_health: int = 5
var independent_checks: int = 0
var supported_answers: int = 0
var questions_answered: int = 0
var active_bucket: RefCounted
var towers: Dictionary = {}
var hand: Array[Dictionary] = []
var enemies: Array[Dictionary] = []
var effects: Array[Dictionary] = []
var checkpoint: Dictionary = {}
var selected_tower: String = ""
var selected_hand_index: int = -1
var moving_cell: String = ""
var offered_power: int = 1
var question: Dictionary = {}
var question_sequence: int = 0
var question_mode: String = "reward"
var answer_text: String = ""
var question_attempts: int = 0
var used_hint: bool = false
var counted_dot_count: int = 0
var help_offer_elapsed: float = 0.0
var question_feedback: String = ""
var current_wave_spawned: int = 0
var spawn_timer: float = 0.0
var battle_elapsed: float = 0.0
var help_available: bool = false
var autosave_note: String = "Progress saves on this device"
var toast: String = ""
var toast_timer: float = 0.0
var reduced_motion: bool = false

func _ready() -> void:
	randomize()
	profile = SaveScript.load_profile()
	unlocked_towers.assign(profile.get("unlocked_towers", SaveScript.STARTER_UNLOCKS))
	reduced_motion = bool(profile.get("reduced_motion", false))
	_build_screen()

func _process(delta: float) -> void:
	if screen == "question":
		help_offer_elapsed += delta
		if not help_available and help_offer_elapsed >= HELP_OFFER_DELAY:
			help_available = true
			_build_screen()
	elif screen == "battle":
		_step_battle(delta)
	if toast_timer > 0.0:
		toast_timer = maxf(0.0, toast_timer - delta)
	if board != null and is_instance_valid(board):
		board.set_board(towers, enemies, earth_health <= 2, effects, _selected_cell(), not hand.is_empty() and screen in ["prep", "tutorial_place"])

func _unhandled_key_input(event: InputEvent) -> void:
	if screen != "question" or not (event is InputEventKey):
		return
	var key_event := event as InputEventKey
	if not key_event.pressed or key_event.echo:
		return
	var code: int = key_event.keycode
	if code >= KEY_0 and code <= KEY_9:
		_add_answer_digit(str(code - KEY_0))
		accept_event()
	elif code == KEY_KP_0:
		_add_answer_digit("0")
		accept_event()
	elif code == KEY_KP_1:
		_add_answer_digit("1")
		accept_event()
	elif code == KEY_KP_2:
		_add_answer_digit("2")
		accept_event()
	elif code == KEY_KP_3:
		_add_answer_digit("3")
		accept_event()
	elif code == KEY_KP_4:
		_add_answer_digit("4")
		accept_event()
	elif code == KEY_KP_5:
		_add_answer_digit("5")
		accept_event()
	elif code == KEY_KP_6:
		_add_answer_digit("6")
		accept_event()
	elif code == KEY_KP_7:
		_add_answer_digit("7")
		accept_event()
	elif code == KEY_KP_8:
		_add_answer_digit("8")
		accept_event()
	elif code == KEY_KP_9:
		_add_answer_digit("9")
		accept_event()
	elif code == KEY_BACKSPACE:
		answer_text = answer_text.left(maxi(0, answer_text.length() - 1))
		_build_screen()
		accept_event()
	elif code == KEY_ENTER or code == KEY_KP_ENTER:
		_submit_answer()
		accept_event()

func _build_screen() -> void:
	for child: Node in get_children():
		remove_child(child)
		child.queue_free()
	board = null
	var backdrop := ColorRect.new()
	backdrop.color = Color("eaf3e2")
	backdrop.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	add_child(backdrop)
	ui_root = VBoxContainer.new()
	ui_root.set_anchors_and_offsets_preset(Control.PRESET_FULL_RECT)
	ui_root.add_theme_constant_override("separation", 0)
	add_child(ui_root)
	var margins := MarginContainer.new()
	margins.add_theme_constant_override("margin_left", 28)
	margins.add_theme_constant_override("margin_right", 28)
	margins.add_theme_constant_override("margin_top", 18)
	margins.add_theme_constant_override("margin_bottom", 18)
	margins.size_flags_vertical = Control.SIZE_EXPAND_FILL
	ui_root.add_child(margins)
	var layout := VBoxContainer.new()
	layout.add_theme_constant_override("separation", 14)
	margins.add_child(layout)
	var title := "Galactacians"
	var subtitle := "Little minds, big universe"
	if screen in ["prep", "choose_tower", "choose_tier", "question", "tutorial_place", "battle", "wave_clear", "retry", "victory", "recovery"]:
		title = MISSION_NAME
		subtitle = "Mission 1 · Wave %d of 3" % wave_number
	_add_header(layout, title, subtitle)
	match screen:
		"menu": _build_menu(layout)
		"tutorial": _build_tutorial(layout)
		"choose_tower": _build_choose_tower(layout)
		"choose_tier": _build_choose_tier(layout)
		"question": _build_question(layout)
		"tutorial_place": _build_tutorial_place(layout)
		"prep": _build_prep(layout)
		"battle": _build_battle(layout)
		"wave_clear": _build_wave_clear(layout)
		"retry": _build_retry(layout)
		"victory": _build_victory(layout)
		"recovery": _build_recovery(layout)
		"settings": _build_settings(layout)

func _add_header(parent: VBoxContainer, title: String, subtitle: String) -> void:
	var bar := HBoxContainer.new()
	bar.add_theme_constant_override("separation", 14)
	parent.add_child(bar)
	var brand := _button("✦  GALACTACIANS", Callable(self, "_go_menu"), 56)
	brand.custom_minimum_size.x = 230
	bar.add_child(brand)
	var titles := VBoxContainer.new()
	titles.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	titles.add_child(_label(title, 26, INK, HORIZONTAL_ALIGNMENT_CENTER))
	titles.add_child(_label(subtitle, 14, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	bar.add_child(titles)
	var settings := _button("Settings", Callable(self, "_show_settings"), 56)
	settings.custom_minimum_size.x = 120
	bar.add_child(settings)
	_add_rule(parent)

func _build_menu(parent: VBoxContainer) -> void:
	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	parent.add_child(spacer)
	var center := VBoxContainer.new()
	center.alignment = BoxContainer.ALIGNMENT_CENTER
	center.add_theme_constant_override("separation", 18)
	center.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	parent.add_child(center)
	center.add_child(_label("A little math. A big brave team.", 34, INK, HORIZONTAL_ALIGNMENT_CENTER))
	center.add_child(_label("Help Earth by learning, choosing, and placing your defenders.", 19, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	var earth := _make_mascot(132)
	center.add_child(earth)
	var adventure_text := "Play First Contact"
	if bool(profile.get("mission_1_complete", false)):
		adventure_text = "Play First Contact again"
	center.add_child(_button(adventure_text, Callable(self, "_start_adventure"), 72, 320, true))
	if bool(profile.get("tutorial_complete", false)):
		center.add_child(_button("Replay the guided tutorial", Callable(self, "_start_tutorial"), 54, 320))
	var status := "Your learning progress is saved on this device."
	if bool(profile.get("mission_1_complete", false)):
		status = "First Contact complete · %s tower types discovered" % str(unlocked_towers.size())
	center.add_child(_label(status, 15, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	var learning_summary := "Learning answers · %d independent · %d with support" % [
		int(profile.get("total_independent_answers", 0)), int(profile.get("total_supported_answers", 0)),
	]
	center.add_child(_label(learning_summary, 14, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	var bottom_space := Control.new()
	bottom_space.size_flags_vertical = Control.SIZE_EXPAND_FILL
	parent.add_child(bottom_space)

func _build_tutorial(parent: VBoxContainer) -> void:
	var card := _card(parent)
	card.size_flags_vertical = Control.SIZE_EXPAND_FILL
	card.add_child(_make_mascot(120))
	card.add_child(_label("Welcome, Earth Defender!", 34, INK, HORIZONTAL_ALIGNMENT_CENTER))
	card.add_child(_label("Three clear lanes lead to Earth. Your towers will hold them back while you choose what to teach your team.", 21, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	card.add_child(_label("First, choose a tower and solve a small question. Then place your new defender and press Ready when you feel prepared.", 20, INK, HORIZONTAL_ALIGNMENT_CENTER))
	var button := _button("Let's earn our first defender", Callable(self, "_start_tutorial_question"), 68, 390, true)
	card.add_child(button)
	card.add_child(_label("You can take all the time you need. Help is always available.", 16, MUTED, HORIZONTAL_ALIGNMENT_CENTER))

func _build_choose_tower(parent: VBoxContainer) -> void:
	parent.add_child(_label("Choose a defender", 28, INK, HORIZONTAL_ALIGNMENT_CENTER))
	parent.add_child(_label("A question will earn the tower you choose. Each role helps in a different way.", 17, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	var unlocked := _offered_tower_pool()
	var cards := HBoxContainer.new()
	cards.alignment = BoxContainer.ALIGNMENT_CENTER
	cards.add_theme_constant_override("separation", 14)
	cards.size_flags_vertical = Control.SIZE_EXPAND_FILL
	parent.add_child(cards)
	for tower_name: String in unlocked:
		var card := _tower_offer_card(tower_name)
		card.custom_minimum_size = Vector2(200, 240)
		card.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		card.pressed.connect(_select_tower.bind(tower_name))
		cards.add_child(card)
	if unlocked.is_empty():
		parent.add_child(_label("This wave's team is ready. Arrange your defenders before the next wave.", 20, INK, HORIZONTAL_ALIGNMENT_CENTER))
	parent.add_child(_button("Back to preparation", Callable(self, "_back_to_prep"), 56, 260))

func _build_choose_tier(parent: VBoxContainer) -> void:
	parent.add_child(_label("Pick your question", 28, INK, HORIZONTAL_ALIGNMENT_CENTER))
	parent.add_child(_label("The challenge changes this tower's strength. All routes are designed to build a balanced team.", 17, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	var card := _tower_offer_card(selected_tower)
	card.custom_minimum_size = Vector2(380, 110)
	card.disabled = true
	parent.add_child(card)
	var options := HBoxContainer.new()
	options.alignment = BoxContainer.ALIGNMENT_CENTER
	options.add_theme_constant_override("separation", 16)
	options.size_flags_vertical = Control.SIZE_EXPAND_FILL
	parent.add_child(options)
	var tiers: Array[Dictionary] = [
		{"power": 1, "title": "Warm-up", "detail": "A small step"},
		{"power": 2, "title": "Steady", "detail": "A little more to think about"},
		{"power": 3, "title": "Stretch", "detail": "A bigger brain-teaser"},
	]
	var count := 0
	for tier: Dictionary in tiers:
		var power: int = int(tier.power)
		if active_bucket != null and not active_bucket.can_offer(power):
			continue
		var button := _button("%s\n%s" % [tier.title, tier.detail], Callable(self, "_start_reward_question").bind(power), 90, 230, power == 2)
		button.size_flags_horizontal = Control.SIZE_EXPAND_FILL
		options.add_child(button)
		count += 1
	if count == 0:
		parent.add_child(_label("Your reinforcements are ready for this wave.", 20, INK, HORIZONTAL_ALIGNMENT_CENTER))
	parent.add_child(_button("Choose another tower", Callable(self, "_show_tower_offers"), 54, 270))

func _build_question(parent: VBoxContainer) -> void:
	var reward_label := "Earn %s" % selected_tower if question_mode == "reward" else "Fresh learning check"
	parent.add_child(_label(reward_label, 26, INK, HORIZONTAL_ALIGNMENT_CENTER))
	if question_mode == "reward":
		parent.add_child(_label("First Contact · %s · %s" % [_topic_name(), _tier_name(int(question.get("tier", 1)))], 16, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	else:
		parent.add_child(_label("A new question just for learning · no extra tower reward", 16, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	var work := HBoxContainer.new()
	work.alignment = BoxContainer.ALIGNMENT_CENTER
	work.add_theme_constant_override("separation", 22)
	work.size_flags_vertical = Control.SIZE_EXPAND_FILL
	parent.add_child(work)
	var visual := _math_visual()
	visual.custom_minimum_size = Vector2(270, 250)
	visual.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	work.add_child(visual)
	var right := VBoxContainer.new()
	right.alignment = BoxContainer.ALIGNMENT_CENTER
	right.add_theme_constant_override("separation", 12)
	right.custom_minimum_size.x = 440
	right.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	work.add_child(right)
	right.add_child(_label("Solve this together", 19, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	right.add_child(_label(str(question.get("prompt", "")), 42, INK, HORIZONTAL_ALIGNMENT_CENTER))
	var display := _label(answer_text if not answer_text.is_empty() else "Your answer", 30, INK, HORIZONTAL_ALIGNMENT_CENTER)
	display.custom_minimum_size.y = 52
	display.add_theme_color_override("font_color", INK if not answer_text.is_empty() else Color("9bac9f"))
	right.add_child(display)
	var keypad := GridContainer.new()
	keypad.columns = 5
	keypad.add_theme_constant_override("h_separation", 7)
	keypad.add_theme_constant_override("v_separation", 7)
	for digit: int in range(1, 10):
		keypad.add_child(_button(str(digit), Callable(self, "_add_answer_digit").bind(str(digit)), 52, 58))
	keypad.add_child(_button("Clear", Callable(self, "_clear_answer"), 52, 82))
	keypad.add_child(_button("0", Callable(self, "_add_answer_digit").bind("0"), 52, 58))
	keypad.add_child(_button("⌫", Callable(self, "_backspace_answer"), 52, 58))
	if not question_feedback.begins_with("Here is how"):
		right.add_child(keypad)
		right.add_child(_button("Check", Callable(self, "_submit_answer"), 58, 250, true))
	if not question_feedback.is_empty():
		right.add_child(_label(question_feedback, 16, Color("a04e43"), HORIZONTAL_ALIGNMENT_CENTER))
	var help_row := HBoxContainer.new()
	help_row.alignment = BoxContainer.ALIGNMENT_CENTER
	help_row.add_theme_constant_override("separation", 10)
	var explanation_open := question_feedback.begins_with("Here is how")
	if not explanation_open and not used_hint:
		help_row.add_child(_button("Show me a hint", Callable(self, "_show_hint"), 48, 174))
	if not explanation_open and help_available:
		help_row.add_child(_button("Teach me step by step", Callable(self, "_reveal_explanation"), 48, 220))
	if not explanation_open:
		right.add_child(help_row)
	if explanation_open:
		var continue_text := "Continue with Pebble" if question_mode == "reward" else "Continue learning"
		right.add_child(_button(continue_text, Callable(self, "_finish_with_fallback"), 54, 250, true))
	var input_note := "Review the steps, then continue when ready." if explanation_open else "Take your time · keyboard works too · press Enter to check"
	right.add_child(_label(input_note, 13, MUTED, HORIZONTAL_ALIGNMENT_CENTER))

func _build_tutorial_place(parent: VBoxContainer) -> void:
	parent.add_child(_label("Place your first defender", 28, INK, HORIZONTAL_ALIGNMENT_CENTER))
	parent.add_child(_label("Tap one of the glowing spots. This tower will protect its lane.", 18, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	_add_board(parent, 320)
	parent.add_child(_build_hand_row())
	if not toast.is_empty() and toast_timer > 0.0:
		parent.add_child(_label(toast, 15, Color("9d6330"), HORIZONTAL_ALIGNMENT_CENTER))
	var continue_button := _button("Continue to preparation", Callable(self, "_finish_tutorial"), 58, 320, true)
	continue_button.disabled = towers.is_empty()
	parent.add_child(continue_button)

func _build_prep(parent: VBoxContainer) -> void:
	var status := HBoxContainer.new()
	status.add_theme_constant_override("separation", 14)
	parent.add_child(status)
	status.add_child(_label("Wave %d · %s" % [wave_number, _topic_name()], 20, INK))
	var fill := Control.new()
	fill.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	status.add_child(fill)
	status.add_child(_label("Independent check: %d / %d" % [mini(independent_checks, LEARNING_CHECK_TARGET), LEARNING_CHECK_TARGET], 17, MUTED))
	parent.add_child(_label("Earn defenders, place them where they help, then press Ready when your plan is set.", 16, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	parent.add_child(_label(_wave_preview(), 15, Color("55766c"), HORIZONTAL_ALIGNMENT_CENTER))
	_add_board(parent, 285)
	parent.add_child(_build_hand_row())
	var actions := HBoxContainer.new()
	actions.add_theme_constant_override("separation", 12)
	parent.add_child(actions)
	var earn := "Earn a tower"
	if active_bucket != null and active_bucket.remaining() <= 0:
		earn = "Reinforcements ready"
	var earn_button := _button(earn, Callable(self, "_show_tower_offers"), 58, 220, true)
	earn_button.disabled = active_bucket == null or active_bucket.remaining() <= 0
	actions.add_child(earn_button)
	var move_help := _label("Select a tower, then an empty spot to rearrange.", 15, MUTED)
	move_help.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	actions.add_child(move_help)
	var ready_button := _button("Ready for wave %d  →" % wave_number, Callable(self, "_ready_wave"), 58, 280, true)
	ready_button.disabled = not hand.is_empty() or towers.is_empty()
	actions.add_child(ready_button)
	if not toast.is_empty() and toast_timer > 0.0:
		parent.add_child(_label(toast, 15, Color("9d6330"), HORIZONTAL_ALIGNMENT_CENTER))

func _build_battle(parent: VBoxContainer) -> void:
	var hud := HBoxContainer.new()
	hud.add_theme_constant_override("separation", 12)
	parent.add_child(hud)
	hud.add_child(_label("EARTH  %d / 5" % earth_health, 20, INK))
	var flexible := Control.new()
	flexible.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	hud.add_child(flexible)
	hud.add_child(_label("WAVE %d · %d / %d aliens" % [wave_number, mini(current_wave_spawned, WAVE_ENEMY_COUNTS[wave_number - 1]), WAVE_ENEMY_COUNTS[wave_number - 1]], 18, MUTED))
	_add_board(parent, 370)
	parent.add_child(_label("Your formation is holding. The battle runs by itself.", 18, INK, HORIZONTAL_ALIGNMENT_CENTER))
	var note := _label("Watch the lanes and cheer your team on!", 15, MUTED, HORIZONTAL_ALIGNMENT_CENTER)
	parent.add_child(note)
	var spacer := Control.new()
	spacer.size_flags_vertical = Control.SIZE_EXPAND_FILL
	parent.add_child(spacer)

func _build_wave_clear(parent: VBoxContainer) -> void:
	var card := _card(parent)
	card.size_flags_vertical = Control.SIZE_EXPAND_FILL
	card.add_child(_make_mascot(105))
	card.add_child(_label("Wave %d cleared!" % wave_number, 32, INK, HORIZONTAL_ALIGNMENT_CENTER))
	var next_text := "Your towers heal and stay in their lanes. Earth keeps its battle scars."
	if wave_number < 3:
		next_text += " You can move surviving towers and earn new defenders."
	else:
		next_text = "The last wave is safe. Let's see what your answers showed you learned."
	card.add_child(_label(next_text, 19, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	if wave_number < 3:
		var unlocked_message := ""
		if wave_number == 1:
			unlocked_message = "Frost has joined your discovered defenders."
		elif wave_number == 2:
			unlocked_message = "Poppy has joined your discovered defenders."
		if not unlocked_message.is_empty():
			card.add_child(_label(unlocked_message, 17, INK, HORIZONTAL_ALIGNMENT_CENTER))
		card.add_child(_button("Prepare wave %d" % (wave_number + 1), Callable(self, "_next_wave"), 66, 300, true))
	else:
		card.add_child(_button("See mission result", Callable(self, "_finish_mission"), 66, 300, true))

func _build_retry(parent: VBoxContainer) -> void:
	var card := _card(parent)
	card.size_flags_vertical = Control.SIZE_EXPAND_FILL
	card.add_child(_make_mascot(110))
	card.add_child(_label("Earth needs another try", 31, INK, HORIZONTAL_ALIGNMENT_CENTER))
	card.add_child(_label("This wave will restart from its beginning. Your earned defenders and completed questions stay saved, and you can change their positions before Ready.", 18, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	card.add_child(_button("Retry wave %d" % wave_number, Callable(self, "_retry_wave"), 66, 300, true))

func _build_victory(parent: VBoxContainer) -> void:
	var card := _card(parent)
	card.size_flags_vertical = Control.SIZE_EXPAND_FILL
	card.add_child(_make_mascot(118))
	card.add_child(_label("Earth is safe!", 34, INK, HORIZONTAL_ALIGNMENT_CENTER))
	card.add_child(_label("You cleared all three waves. %d answers were independent; %d used learning support." % [independent_checks, supported_answers], 19, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	if independent_checks >= LEARNING_CHECK_TARGET:
		card.add_child(_label("First Contact complete · your progress is saved.", 18, INK, HORIZONTAL_ALIGNMENT_CENTER))
		card.add_child(_button("Back to Adventure", Callable(self, "_go_menu"), 64, 300, true))
	else:
		card.add_child(_label("A mission win is still a win. A few fresh questions will finish the lesson check and unlock the next chapter.", 17, INK, HORIZONTAL_ALIGNMENT_CENTER))
		card.add_child(_button("Continue with a fresh learning check", Callable(self, "_start_recovery"), 64, 390, true))

func _build_recovery(parent: VBoxContainer) -> void:
	var card := _card(parent)
	card.size_flags_vertical = Control.SIZE_EXPAND_FILL
	card.add_child(_make_mascot(90))
	card.add_child(_label("A fresh question, just for learning", 28, INK, HORIZONTAL_ALIGNMENT_CENTER))
	card.add_child(_label("This practice does not affect your mission win or give extra towers. Ask for a hint if you need one; only a fresh independent answer fills the learning check.", 17, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	card.add_child(_label("Independent check: %d / %d" % [mini(independent_checks, LEARNING_CHECK_TARGET), LEARNING_CHECK_TARGET], 21, INK, HORIZONTAL_ALIGNMENT_CENTER))
	card.add_child(_button("Try a fresh question", Callable(self, "_start_recovery_question"), 62, 300, true))
	card.add_child(_button("Back to Adventure", Callable(self, "_go_menu"), 52, 260))

func _add_board(parent: VBoxContainer, min_height: float) -> void:
	var frame := PanelContainer.new()
	frame.size_flags_vertical = Control.SIZE_EXPAND_FILL
	frame.custom_minimum_size.y = min_height
	frame.add_theme_stylebox_override("panel", _panel_style(Color("fbfcf5"), 18, Color("d4e3cf")))
	parent.add_child(frame)
	board = BoardScript.new()
	board.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	board.size_flags_vertical = Control.SIZE_EXPAND_FILL
	board.cell_selected.connect(_on_cell_selected)
	board.reduced_motion = reduced_motion
	frame.add_child(board)
	board.set_board(towers, enemies, earth_health <= 2, effects, _selected_cell(), not hand.is_empty() and screen in ["prep", "tutorial_place"])

func _build_hand_row() -> HBoxContainer:
	var row := HBoxContainer.new()
	row.add_theme_constant_override("separation", 9)
	row.alignment = BoxContainer.ALIGNMENT_CENTER
	if hand.is_empty():
		row.add_child(_label("No new towers waiting to be placed", 15, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
		return row
	for index: int in range(hand.size()):
		var unit: Dictionary = hand[index]
		var tower_name: String = str(unit.get("type", "Pebble"))
		var strength: int = int(unit.get("power", 1))
		var text := "%s  %s" % [tower_name, "✦".repeat(strength)]
		var button := _button(text, Callable(self, "_select_hand_tower").bind(index), 48, 148, selected_hand_index == index)
		button.add_theme_color_override("font_color", INK)
		button.tooltip_text = str(TOWER_ROLES.get(tower_name, "Defender"))
		row.add_child(button)
	return row

func _build_settings(parent: VBoxContainer) -> void:
	parent.add_child(_label("Make the game comfortable", 27, INK, HORIZONTAL_ALIGNMENT_CENTER))
	parent.add_child(_label("Your choices are saved on this device.", 16, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	var settings := VBoxContainer.new()
	settings.alignment = BoxContainer.ALIGNMENT_CENTER
	settings.add_theme_constant_override("separation", 16)
	settings.size_flags_vertical = Control.SIZE_EXPAND_FILL
	parent.add_child(settings)
	var motion := CheckButton.new()
	motion.text = "Reduced motion"
	motion.button_pressed = reduced_motion
	motion.custom_minimum_size = Vector2(320, 62)
	motion.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	motion.toggled.connect(_set_reduced_motion)
	settings.add_child(motion)
	settings.add_child(_label("Voice narration and sound settings will be added as the native mission grows.", 16, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	settings.add_child(_button("Back", Callable(self, "_go_menu"), 56, 240))

func _show_settings() -> void:
	screen = "settings"
	_build_screen()

func _build_math_visual() -> Control:
	var panel := PanelContainer.new()
	panel.add_theme_stylebox_override("panel", _panel_style(Color("f4f8ec"), 18, Color("dbe8d7")))
	var column := VBoxContainer.new()
	column.alignment = BoxContainer.ALIGNMENT_CENTER
	column.add_theme_constant_override("separation", 12)
	panel.add_child(column)
	column.add_child(_label("COUNT THE DOTS", 14, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	var frame := GridContainer.new()
	frame.columns = 5
	frame.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	frame.add_theme_constant_override("h_separation", 8)
	frame.add_theme_constant_override("v_separation", 8)
	var first: int = int(question.get("first", 0))
	var second: int = int(question.get("second", 0))
	var operation: String = str(question.get("operation", "+"))
	var visible_count := first + second
	if operation == "-":
		visible_count = first
	elif int(question.get("tier", 1)) == 3:
		visible_count = first + int(question.get("answer", 0))
	var dots_total: int = mini(visible_count, 10)
	for index: int in range(dots_total):
		var dot_color := Color("78c9de") if index < first else Color("f0b84d")
		var dot := _button("●", Callable(self, "_mark_dot").bind(index), 36, 36)
		dot.toggle_mode = true
		dot.button_pressed = index < counted_dot_count
		dot.add_theme_font_size_override("font_size", 20)
		dot.add_theme_color_override("font_color", Color("ffffff"))
		var normal_dot := _panel_style(dot_color, 18, dot_color.darkened(0.12))
		normal_dot.content_margin_left = 0
		normal_dot.content_margin_right = 0
		normal_dot.content_margin_top = 0
		normal_dot.content_margin_bottom = 0
		var hover_dot := _panel_style(dot_color.lightened(0.1), 18, dot_color.darkened(0.12))
		hover_dot.content_margin_left = 0
		hover_dot.content_margin_right = 0
		hover_dot.content_margin_top = 0
		hover_dot.content_margin_bottom = 0
		var pressed_dot := _panel_style(dot_color.darkened(0.18), 18, dot_color.darkened(0.25))
		pressed_dot.content_margin_left = 0
		pressed_dot.content_margin_right = 0
		pressed_dot.content_margin_top = 0
		pressed_dot.content_margin_bottom = 0
		dot.add_theme_stylebox_override("normal", normal_dot)
		dot.add_theme_stylebox_override("hover", hover_dot)
		dot.add_theme_stylebox_override("pressed", pressed_dot)
		frame.add_child(dot)
	column.add_child(frame)
	var caption := "Count on to fill the ten-frame" if operation == "+" and int(question.get("tier", 1)) == 3 else "Blue group, then gold group" if operation == "+" else "Start with the whole group"
	column.add_child(_label(caption, 15, INK, HORIZONTAL_ALIGNMENT_CENTER))
	column.add_child(_label("Tap the dots to count · %d of %d" % [counted_dot_count, dots_total], 14, MUTED, HORIZONTAL_ALIGNMENT_CENTER))
	if operation == "-":
		var removed := HBoxContainer.new()
		removed.alignment = BoxContainer.ALIGNMENT_CENTER
		for index: int in range(mini(second, 10)):
			var mark := _label("×", 20, Color("dd786b"), HORIZONTAL_ALIGNMENT_CENTER)
			mark.custom_minimum_size.x = 24
			removed.add_child(mark)
		column.add_child(removed)
	return panel

func _math_visual() -> Control:
	return _build_math_visual()

func _card(parent: Node) -> VBoxContainer:
	var panel := PanelContainer.new()
	panel.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	panel.add_theme_stylebox_override("panel", _panel_style(PAPER, 24, Color("d7e5d1")))
	parent.add_child(panel)
	var body := VBoxContainer.new()
	body.alignment = BoxContainer.ALIGNMENT_CENTER
	body.add_theme_constant_override("separation", 16)
	body.add_theme_constant_override("margin_left", 24)
	body.add_theme_constant_override("margin_right", 24)
	panel.add_child(body)
	return body

func _tower_offer_card(tower_name: String) -> Button:
	var button := _button("", Callable(), 62)
	button.text = "%s\n%s\n\n%s" % [tower_name, "★  ★  ★" if tower_name == "Poppy" else "◆   ◆   ◆", str(TOWER_ROLES.get(tower_name, "Defender"))]
	button.add_theme_font_size_override("font_size", 18)
	button.add_theme_color_override("font_color", INK)
	var style := _panel_style(Color("fbfcf5"), 18, TOWER_COLORS.get(tower_name, Color("d4e3cf")))
	style.set_border_width_all(2)
	button.add_theme_stylebox_override("normal", style)
	button.add_theme_stylebox_override("hover", _panel_style(Color("f3f8ec"), 18, TOWER_COLORS.get(tower_name, Color("d4e3cf"))))
	button.add_theme_stylebox_override("pressed", _panel_style(Color("e5f0db"), 18, TOWER_COLORS.get(tower_name, Color("d4e3cf"))))
	return button

func _make_mascot(diameter: float) -> Control:
	var mascot := Control.new()
	mascot.custom_minimum_size = Vector2(diameter, diameter)
	mascot.size_flags_horizontal = Control.SIZE_SHRINK_CENTER
	mascot.draw.connect(func() -> void:
		var center := mascot.size * 0.5
		var radius := minf(mascot.size.x, mascot.size.y) * 0.42
		mascot.draw_circle(center + Vector2(0, 4), radius, Color("b8dbb0"))
		mascot.draw_circle(center, radius - 4, Color("78c9de"))
		mascot.draw_circle(center + Vector2(-radius * 0.35, -radius * 0.45), radius * 0.3, Color("92c96b"))
		mascot.draw_circle(center + Vector2(radius * 0.35, radius * 0.42), radius * 0.32, Color("92c96b"))
		for eye_x: float in [-radius * 0.28, radius * 0.28]:
			mascot.draw_circle(center + Vector2(eye_x, 0), radius * 0.075, INK)
		mascot.draw_arc(center + Vector2(0, radius * 0.12), radius * 0.2, 0.2, PI - 0.2, 16, INK, 2.5, true)
	)
	mascot.queue_redraw()
	return mascot

func _button(text: String, action: Callable, height: float = 56.0, width: float = 0.0, primary: bool = false) -> Button:
	var button := Button.new()
	button.text = text
	button.custom_minimum_size = Vector2(width, height)
	button.size_flags_horizontal = Control.SIZE_SHRINK_CENTER if width > 0.0 else Control.SIZE_SHRINK_BEGIN
	button.focus_mode = Control.FOCUS_ALL
	button.add_theme_font_size_override("font_size", 18 if height >= 56.0 else 16)
	button.add_theme_color_override("font_color", INK)
	button.add_theme_color_override("font_hover_color", INK)
	button.add_theme_stylebox_override("normal", _panel_style(Color("f9fbf5") if not primary else Color("f5d77d"), 16, Color("cbdcc6")))
	button.add_theme_stylebox_override("hover", _panel_style(Color("eef5e8") if not primary else Color("f8df99"), 16, Color("9fbda0")))
	button.add_theme_stylebox_override("pressed", _panel_style(Color("e0edda"), 16, Color("88a98e")))
	button.add_theme_stylebox_override("disabled", _panel_style(Color("edf0e9"), 16, Color("d7ddd3")))
	if action.is_valid():
		button.pressed.connect(action)
	return button

func _label(text: String, font_size: int = 18, color: Color = INK, alignment: HorizontalAlignment = HORIZONTAL_ALIGNMENT_LEFT) -> Label:
	var label := Label.new()
	label.text = text
	label.horizontal_alignment = alignment
	label.vertical_alignment = VERTICAL_ALIGNMENT_CENTER
	label.autowrap_mode = TextServer.AUTOWRAP_WORD_SMART
	label.add_theme_font_size_override("font_size", font_size)
	label.add_theme_color_override("font_color", color)
	return label

func _panel_style(color: Color, radius: int, border: Color) -> StyleBoxFlat:
	var style := StyleBoxFlat.new()
	style.bg_color = color
	style.border_color = border
	style.set_border_width_all(1)
	style.set_corner_radius_all(radius)
	style.content_margin_left = 8
	style.content_margin_right = 8
	style.content_margin_top = 6
	style.content_margin_bottom = 6
	return style

func _add_rule(parent: VBoxContainer) -> void:
	var rule := ColorRect.new()
	rule.color = Color("d2e0cd")
	rule.custom_minimum_size.y = 1
	parent.add_child(rule)

func _offered_tower_pool() -> Array[String]:
	var pool: Array[String] = []
	for tower_name: String in unlocked_towers:
		if int(TOWER_UNLOCK_WAVE.get(tower_name, 1)) <= wave_number:
			pool.append(tower_name)
	if pool.size() > 3:
		var offset: int = (question_sequence + wave_number) % pool.size()
		var rotated: Array[String] = []
		for i: int in range(pool.size()):
			rotated.append(pool[(i + offset) % pool.size()])
		pool = rotated.slice(0, 3)
	return pool

func _topic_name() -> String:
	return "Subtraction within 10" if wave_number >= 3 else "Addition within 10"

func _wave_preview() -> String:
	match wave_number:
		1:
			return "Wave preview · quick scouts in changing lanes"
		2:
			return "Wave preview · armored aliens and quick swarms"
		_:
			return "Wave preview · fast scouts, armored guards, and clustered swarms"

func _tier_name(tier: int) -> String:
	return ["", "Warm-up", "Steady", "Stretch"][clampi(tier, 1, 3)]

func _select_tower(tower_name: String) -> void:
	selected_tower = tower_name
	screen = "choose_tier"
	_build_screen()

func _show_tower_offers() -> void:
	if active_bucket == null or active_bucket.remaining() <= 0:
		toast = "This wave's reinforcements are ready."
		toast_timer = 2.5
		_build_screen()
		return
	screen = "choose_tower"
	_build_screen()

func _back_to_prep() -> void:
	screen = "prep"
	_build_screen()

func _start_reward_question(power: int) -> void:
	if active_bucket == null or not active_bucket.can_offer(power):
		return
	offered_power = power
	question_sequence += 1
	question_mode = "reward"
	question = QuestionScript.make_question("mission1-wave%d-q%d" % [wave_number, question_sequence], power, wave_number)
	counted_dot_count = 0
	answer_text = ""
	question_attempts = 0
	used_hint = false
	help_available = false
	help_offer_elapsed = 0.0
	question_feedback = ""
	screen = "question"
	_build_screen()

func _start_tutorial_question() -> void:
	wave_number = 1
	if active_bucket == null:
		active_bucket = BucketScript.new(WAVE_POWER_CAPACITY)
	selected_tower = "Pebble"
	_start_reward_question(1)
	question_feedback = "Let's count on together. Take your time, and ask for a hint whenever you like."
	_build_screen()

func _start_adventure() -> void:
	tutorial_in_progress = false
	wave_number = 1
	earth_health = 5
	independent_checks = 0
	supported_answers = 0
	questions_answered = 0
	towers.clear()
	hand.clear()
	enemies.clear()
	effects.clear()
	active_bucket = BucketScript.new(WAVE_POWER_CAPACITY)
	selected_hand_index = -1
	moving_cell = ""
	toast = ""
	if not bool(profile.get("tutorial_complete", false)):
		_start_tutorial()
	else:
		screen = "prep"
		_build_screen()

func _start_tutorial() -> void:
	tutorial_step = 0
	tutorial_in_progress = true
	wave_number = 1
	earth_health = 5
	independent_checks = 0
	supported_answers = 0
	questions_answered = 0
	towers.clear()
	hand.clear()
	enemies.clear()
	effects.clear()
	selected_hand_index = -1
	moving_cell = ""
	active_bucket = BucketScript.new(WAVE_POWER_CAPACITY)
	screen = "tutorial"
	_build_screen()

func _finish_tutorial() -> void:
	if towers.is_empty():
		toast = "First, place your Pebble in a glowing lane spot."
		toast_timer = 3.0
		_build_screen()
		return
	profile["tutorial_complete"] = true
	tutorial_in_progress = false
	_save_profile()
	screen = "prep"
	_build_screen()

func _select_hand_tower(index: int) -> void:
	if index < 0 or index >= hand.size():
		return
	selected_hand_index = index
	moving_cell = ""
	toast = "Choose an empty spot in a lane."
	toast_timer = 2.0
	_build_screen()

func _on_cell_selected(lane: int, column: int) -> void:
	if screen not in ["prep", "tutorial_place"]:
		return
	var cell := "%d:%d" % [lane, column]
	if selected_hand_index >= 0:
		if towers.has(cell):
			toast = "That spot is occupied. Choose an empty lane spot."
			toast_timer = 2.0
			return
		var placed: Dictionary = hand[selected_hand_index]
		towers[cell] = placed.duplicate(true)
		hand.remove_at(selected_hand_index)
		selected_hand_index = -1
		toast = "%s is protecting lane %d." % [placed.type, lane + 1]
		toast_timer = 2.2
		_build_screen()
		return
	if not moving_cell.is_empty():
		if towers.has(cell):
			moving_cell = cell
			toast = "Select an empty spot to move this defender."
			toast_timer = 2.2
		else:
			towers[cell] = towers[moving_cell]
			towers.erase(moving_cell)
			moving_cell = ""
			toast = "Defender moved to lane %d." % (lane + 1)
			toast_timer = 2.2
		_build_screen()
		return
	if towers.has(cell):
		moving_cell = cell
		toast = "Choose an empty spot to move this defender."
		toast_timer = 2.2
		_build_screen()

func _ready_wave() -> void:
	if not hand.is_empty():
		toast = "Place each new defender before pressing Ready."
		toast_timer = 2.5
		_build_screen()
		return
	if towers.is_empty():
		toast = "Earn and place at least one defender first."
		toast_timer = 2.5
		_build_screen()
		return
	checkpoint = {"towers": towers.duplicate(true), "earth_health": earth_health, "bucket_spent": active_bucket.spent if active_bucket != null else 0}
	enemies.clear()
	effects.clear()
	current_wave_spawned = 0
	spawn_timer = 0.65
	battle_elapsed = 0.0
	screen = "battle"
	_build_screen()

func _step_battle(delta: float) -> void:
	effects.clear()
	battle_elapsed += delta
	spawn_timer -= delta
	var total: int = WAVE_ENEMY_COUNTS[wave_number - 1]
	if current_wave_spawned < total and spawn_timer <= 0.0:
		_spawn_enemy(current_wave_spawned)
		current_wave_spawned += 1
		spawn_timer = 1.55 if wave_number == 1 else 1.35
	_update_enemies(delta)
	_update_towers(delta)
	if earth_health <= 0:
		earth_health = 0
		screen = "retry"
		_build_screen()
		return
	if current_wave_spawned >= total and enemies.is_empty():
		_clear_wave()
		return

func _spawn_enemy(index: int) -> void:
	var kind := "scout"
	if wave_number >= 2 and index % 3 == 1:
		kind = "armored"
	elif wave_number >= 2 and index % 3 == 2:
		kind = "swarm"
	elif wave_number == 3 and index % 2 == 1:
		kind = "swarm"
	var lane := (index + wave_number - 1) % 3
	var hp := 1
	var speed := 0.055
	if kind == "armored":
		hp = 3
		speed = 0.032
	elif kind == "swarm":
		hp = 1
		speed = 0.078
	enemies.append({"kind": kind, "lane": lane, "x": 1.0, "hp": hp, "max_hp": hp, "speed": speed, "slow": 0.0, "attack_clock": 0.0})

func _update_enemies(delta: float) -> void:
	var survivors: Array[Dictionary] = []
	for enemy: Dictionary in enemies:
		if int(enemy.get("hp", 0)) <= 0:
			continue
		enemy["slow"] = maxf(0.0, float(enemy.get("slow", 0.0)) - delta)
		var speed: float = float(enemy.get("speed", 0.05))
		if float(enemy.get("slow", 0.0)) > 0.0:
			speed *= 0.48
		var blocked := false
		for cell: String in towers.keys():
			if not towers.has(cell):
				continue
			var parts := cell.split(":")
			if int(parts[0]) != int(enemy.lane) or str(towers[cell].get("type", "")) != "Bricky":
				continue
			var tower_x := (float(int(parts[1])) + 0.5) / 5.0
			if float(enemy.x) < tower_x and float(enemy.x) > tower_x - 0.12:
				blocked = true
				enemy["attack_clock"] = float(enemy.get("attack_clock", 0.0)) + delta
				if float(enemy.attack_clock) >= 1.4:
				enemy["attack_clock"] = 0.0
				towers[cell]["hp"] = int(towers[cell].get("hp", 3)) - 1
				if int(towers[cell].hp) <= 0:
					towers.erase(cell)
				else:
					enemy["hp"] = int(enemy.hp) - 1
				break
		if not blocked:
			enemy["x"] = float(enemy.x) - speed * delta
		if int(enemy.get("hp", 0)) <= 0:
			continue
		if float(enemy.x) <= 0.055:
			earth_health -= 1
			continue
		survivors.append(enemy)
	enemies = survivors

func _update_towers(delta: float) -> void:
	var defeated: Array[Dictionary] = []
	for cell: String in towers:
		var unit: Dictionary = towers[cell]
		unit["cooldown"] = maxf(0.0, float(unit.get("cooldown", 0.0)) - delta)
		if float(unit.cooldown) > 0.0:
			continue
		var parts := cell.split(":")
		var lane: int = int(parts[0])
		var tower_x := (float(int(parts[1])) + 0.5) / 5.0
		var tower_name: String = str(unit.type)
		if tower_name == "Bricky":
			unit["cooldown"] = 0.35
			continue
		var range := 0.36
		var cooldown := 0.65
		if tower_name == "Prism":
			range = 0.78
			cooldown = 1.2
		elif tower_name == "Frost":
			range = 0.44
			cooldown = 0.9
		elif tower_name == "Poppy":
			range = 0.42
			cooldown = 1.0
		var target_index := -1
		var nearest_x := 2.0
		for index: int in range(enemies.size()):
			var enemy: Dictionary = enemies[index]
			var distance: float = float(enemy.x) - tower_x
			if int(enemy.lane) == lane and distance >= -0.02 and distance <= range and float(enemy.x) < nearest_x:
				nearest_x = float(enemy.x)
				target_index = index
		if target_index < 0:
			continue
			var target: Dictionary = enemies[target_index]
			if int(target.get("hp", 0)) <= 0:
				continue
		var strength: int = int(unit.get("power", 1))
		var damage := 1
		if tower_name == "Prism" and str(target.kind) == "armored":
			damage += 1
		damage += strength - 1
		if tower_name == "Frost":
			target["slow"] = 1.4
		if tower_name == "Poppy":
			for splash: Dictionary in enemies:
				if splash == target:
					continue
				if absf(float(splash.x) - float(target.x)) <= 0.13 and absf(float(splash.lane) - float(target.lane)) <= 1.0:
					splash["hp"] = int(splash.hp) - 1
					if int(splash.hp) <= 0:
						defeated.append(splash)
		target["hp"] = int(target.hp) - damage
		unit["cooldown"] = cooldown / (1.0 + float(strength - 1) * 0.12)
		var origin := Vector2(tower_x, lane)
		var destination := Vector2(float(target.x), int(target.lane))
		effects.append({"from": origin, "to": destination})
		if int(target.hp) <= 0:
			defeated.append(target)
	for dead: Dictionary in defeated:
		enemies.erase(dead)

func _clear_wave() -> void:
	for cell: String in towers:
		towers[cell]["hp"] = 3
	enemies.clear()
	effects.clear()
	if wave_number < 3:
		_unlock_wave_towers(wave_number + 1)
	screen = "wave_clear"
	_build_screen()

func _next_wave() -> void:
	wave_number += 1
	active_bucket = BucketScript.new(WAVE_POWER_CAPACITY)
	selected_hand_index = -1
	moving_cell = ""
	screen = "prep"
	_save_profile()
	_build_screen()

func _retry_wave() -> void:
	if checkpoint.is_empty():
		screen = "prep"
	else:
		towers = checkpoint.towers.duplicate(true)
		earth_health = int(checkpoint.earth_health)
		enemies.clear()
		effects.clear()
		current_wave_spawned = 0
		spawn_timer = 0.5
		selected_hand_index = -1
		moving_cell = ""
		screen = "prep"
	_build_screen()

func _finish_mission() -> void:
	if independent_checks >= LEARNING_CHECK_TARGET:
		profile["mission_1_complete"] = true
		profile["best_independent_checks"] = maxi(int(profile.get("best_independent_checks", 0)), independent_checks)
		_save_profile()
		screen = "victory"
	else:
		screen = "victory"
	_build_screen()

func _start_recovery() -> void:
	screen = "recovery"
	_build_screen()

func _start_recovery_question() -> void:
	question_sequence += 1
	question_mode = "recovery"
	question = QuestionScript.make_question("mission1-recovery-%d" % question_sequence, 2, wave_number)
	counted_dot_count = 0
	answer_text = ""
	question_attempts = 0
	used_hint = false
	help_available = false
	help_offer_elapsed = 0.0
	question_feedback = ""
	screen = "question"
	_build_screen()

func _answer_is_correct() -> bool:
	return not answer_text.is_empty() and int(answer_text) == int(question.get("answer", -999))

func _submit_answer() -> void:
	if screen != "question":
		return
	if question_feedback.begins_with("Here is how"):
		return
	if not _answer_is_correct():
		question_attempts += 1
		answer_text = ""
		used_hint = true
		if question_attempts == 1:
			question_feedback = "Let's work it out: %s Try the same question once more." % str(question.hint)
		else:
			question_feedback = "Here is how we can solve it: %s" % str(question.explanation)
			answer_text = str(question.get("answer", ""))
		_build_screen()
		return
	var independent := question_attempts == 0 and not used_hint
	_record_answer(independent)
	if question_mode == "reward":
		var reward: Dictionary = active_bucket.award(str(question.id), offered_power, question_attempts > 0, false, used_hint)
		if reward.is_empty():
			toast = "That reward offer has changed. Choose another question."
			toast_timer = 2.4
			screen = "prep"
		else:
			var awarded_power: int = int(reward.power)
			var awarded_tower := "Pebble" if bool(reward.fallback) else selected_tower
			hand.append({"type": awarded_tower, "power": awarded_power, "hp": 3, "cooldown": 0.0})
			var assist_note := ""
			if not bool(reward.independent):
				assist_note = " (learning support used)"
			toast = "%s joined your team%s." % [awarded_tower, assist_note]
			toast_timer = 3.0
			if tutorial_in_progress:
				screen = "tutorial_place"
			else:
				screen = "prep"
	else:
		if independent_checks >= LEARNING_CHECK_TARGET:
			profile["mission_1_complete"] = true
			_save_profile()
			screen = "victory"
		else:
			screen = "recovery"
	_save_profile()
	_build_screen()

func _show_hint() -> void:
	used_hint = true
	question_feedback = str(question.get("hint", "Look closely at the numbers."))
	_build_screen()

func _reveal_explanation() -> void:
	used_hint = true
	question_feedback = "Here is how we can solve it: %s" % str(question.get("explanation", ""))
	answer_text = str(question.get("answer", ""))
	_build_screen()

func _finish_with_fallback() -> void:
	if question_mode == "reward":
		_record_answer(false)
		var fallback: Dictionary = active_bucket.award(str(question.id), offered_power, true, true, true)
		if not fallback.is_empty():
				hand.append({"type": "Pebble", "power": int(fallback.power), "hp": 3, "cooldown": 0.0})
			toast = "Pebble joined your team. Supported learning is recorded separately."
		else:
			toast = "Your team is ready for this wave."
		toast_timer = 3.0
		screen = "tutorial_place" if tutorial_in_progress else "prep"
	else:
		_record_answer(false, false)
		screen = "recovery"
	_save_profile()
	_build_screen()

func _add_answer_digit(digit: String) -> void:
	if screen != "question" or answer_text.length() >= 2:
		return
	answer_text += digit
	_build_screen()

func _clear_answer() -> void:
	answer_text = ""
	_build_screen()

func _backspace_answer() -> void:
	answer_text = answer_text.left(maxi(0, answer_text.length() - 1))
	_build_screen()

func _mark_dot(index: int) -> void:
	if index < counted_dot_count:
		counted_dot_count = index
	elif index == counted_dot_count:
		counted_dot_count += 1
	_build_screen()

func _unlock_wave_towers(next_wave: int) -> void:
	var newly_unlocked: Array[String] = []
	for tower_name: String in TOWER_UNLOCK_WAVE:
		if int(TOWER_UNLOCK_WAVE[tower_name]) == next_wave and not unlocked_towers.has(tower_name):
			unlocked_towers.append(tower_name)
			newly_unlocked.append(tower_name)
	profile["unlocked_towers"] = unlocked_towers.duplicate()
	if not newly_unlocked.is_empty():
		toast = "%s discovered!" % ", ".join(newly_unlocked)
	_save_profile()

func _selected_cell() -> String:
	return moving_cell

func _record_answer(independent: bool, counts_for_check: bool = true) -> void:
	questions_answered += 1
	if independent:
		profile["total_independent_answers"] = int(profile.get("total_independent_answers", 0)) + 1
		if counts_for_check:
			independent_checks += 1
			profile["best_independent_checks"] = maxi(int(profile.get("best_independent_checks", 0)), independent_checks)
	else:
		supported_answers += 1
		profile["total_supported_answers"] = int(profile.get("total_supported_answers", 0)) + 1

func _go_menu() -> void:
	screen = "menu"
	_build_screen()

func _set_reduced_motion(enabled: bool) -> void:
	reduced_motion = enabled
	profile["reduced_motion"] = enabled
	_save_profile()

func _save_profile() -> void:
	profile["version"] = SaveScript.VERSION
	profile["unlocked_towers"] = unlocked_towers.duplicate()
	var saved: bool = SaveScript.save_profile(profile)
	autosave_note = "Saved on this device" if saved else "Could not save progress on this device"
