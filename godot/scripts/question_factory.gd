class_name QuestionFactory
extends RefCounted
## Authored arithmetic generation for the introductory addition/subtraction mission.

static func make_question(question_id: String, tier: int, wave: int) -> Dictionary:
	var operation := "-" if wave >= 3 else "+"
	var first: int = 0
	var second: int = 0
	var answer: int = 0
	var prompt: String = ""
	var hint: String = ""
	var explanation: String = ""
	if operation == "+":
		if tier <= 1:
			first = randi_range(1, 3)
			second = randi_range(1, 5 - first)
			answer = first + second
			prompt = "%d + %d = ?" % [first, second]
			hint = "Start at %d and count on %d more." % [first, second]
			explanation = "%d plus %d makes %d." % [first, second, answer]
		elif tier == 2:
			first = randi_range(3, 8)
			second = randi_range(1, mini(10 - first, 5))
			answer = first + second
			prompt = "%d + %d = ?" % [first, second]
			hint = "Count on %d from %d, one number at a time." % [second, first]
			explanation = "Keep %d in your head and count on %d steps. You land on %d." % [first, second, answer]
		else:
			answer = randi_range(2, 9)
			first = randi_range(1, answer - 1)
			second = answer
			prompt = "%d + ? = %d" % [first, second]
			hint = "Count up from %d until you reach %d." % [first, second]
			explanation = "Starting at %d, count up to %d. You count %d steps, so the missing number is %d." % [first, second, answer - first, answer - first]
			answer -= first
	else:
		if tier <= 1:
			second = randi_range(1, 3)
			first = randi_range(second, mini(5, second + 3))
		elif tier == 2:
			second = randi_range(2, 6)
			first = randi_range(second, mini(10, second + 4))
		else:
			first = randi_range(6, 10)
			second = randi_range(1, first - 1)
		answer = first - second
		prompt = "%d − %d = ?" % [first, second]
		hint = "Start at %d and count back %d." % [first, second]
		explanation = "Take %d away from %d. Counting back lands on %d." % [second, first, answer]
	return {
		"id": question_id,
		"tier": tier,
		"wave": wave,
		"operation": operation,
		"prompt": prompt,
		"answer": answer,
		"hint": hint,
		"explanation": explanation,
		"first": first,
		"second": second,
	}
