class_name CurriculumSave
extends RefCounted
## Small versioned local profile. Invalid or unknown files are ignored safely.

const PATH: String = "user://galactacians_curriculum_save.json"
const VERSION: int = 1
const STARTER_UNLOCKS: Array[String] = ["Pebble", "Bricky", "Prism"]

static func defaults() -> Dictionary:
	return {
		"version": VERSION,
		"tutorial_complete": false,
		"mission_1_complete": false,
		"unlocked_towers": STARTER_UNLOCKS.duplicate(),
		"best_independent_checks": 0,
		"total_independent_answers": 0,
		"total_supported_answers": 0,
		"reduced_motion": false,
		"narration": false,
	}

static func load_profile() -> Dictionary:
	var profile := defaults()
	if not FileAccess.file_exists(PATH):
		return profile
	var file := FileAccess.open(PATH, FileAccess.READ)
	if file == null:
		return profile
	var parsed: Variant = JSON.parse_string(file.get_as_text())
	if not parsed is Dictionary or int(parsed.get("version", -1)) != VERSION:
		return profile
	for key: String in profile.keys():
		if parsed.has(key) and typeof(parsed[key]) == typeof(profile[key]):
			profile[key] = parsed[key]
	var valid_unlocks: Array[String] = []
	for tower: Variant in profile["unlocked_towers"]:
		if tower is String and ["Pebble", "Prism", "Bricky", "Frost", "Poppy"].has(tower) and not valid_unlocks.has(tower):
			valid_unlocks.append(tower)
	if valid_unlocks.is_empty():
		valid_unlocks = STARTER_UNLOCKS.duplicate()
	profile["unlocked_towers"] = valid_unlocks
	return profile

static func save_profile(profile: Dictionary) -> bool:
	var safe_profile := defaults()
	for key: String in safe_profile.keys():
		if profile.has(key) and typeof(profile[key]) == typeof(safe_profile[key]):
			safe_profile[key] = profile[key]
	var file := FileAccess.open(PATH, FileAccess.WRITE)
	if file == null:
		return false
	file.store_string(JSON.stringify(safe_profile, "\t"))
	return file.get_error() == OK
