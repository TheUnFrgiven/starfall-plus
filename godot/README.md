# Galactacians native rebuild

Open `project.godot` in Godot 4.7.2 and press F5 to run the native project. The
first curriculum mission is implemented in typed GDScript alongside the preserved
browser prototypes.

## First Contact

- A guided opening teaches the three-lane board, the question-to-tower loop, and
  tower placement.
- Choose a defender, then choose a warm-up, steady, or stretch question. The
  first chapter practices addition within 10 in waves 1–2 and subtraction within
  10 in wave 3.
- Answers use a large on-screen number pad or the physical keyboard, followed by
  **Check**. A hint can be requested; after a wrong answer, the same question gets
  one retry. A full worked explanation awards a Pebble and records supported
  learning separately from independent answers. The ten-frame counters can be
  tapped in order to count through the groups.
- Place defenders in one of three lanes during preparation. Surviving towers stay
  between waves and heal fully. Earth damage carries through the mission. Press
  **Ready** to begin automatic combat; formations lock until the wave ends.
- A lost wave can be retried from its checkpoint. Previously earned towers and
  answered questions are restored without duplicate rewards.
- Mission victory is preserved even if the independent learning check needs more
  evidence. Fresh practice questions can complete that check without awarding
  extra towers or replaying the battle.
- Reduced motion, discovered tower types, tutorial completion, and mission
  completion are saved locally in a versioned profile under `user://`.

## Provisional implementation choices

The design register intentionally leaves tuning open. This playable slice uses
three waves, a four-unit hidden allowance per wave, reward strengths of
one/two/three units, a two-answer independent-check target, and an eight-second
delay before offering a full explanation. It introduces Frost in wave 2 and Poppy
in wave 3, with addition in the first two waves and subtraction in the last.
These are implementation choices for the first slice, not finalized design
decisions. Tower combat statistics and enemy pacing also need playtesting. The
first version uses tap-to-count visual counters and text explanations; voice
narration and richer interactive teaching animations are not implemented yet.

## Project structure

- `scripts/main.gd` — menu, preparation, question flow, mission progression,
  combat, checkpoint retry and saves.
- `scripts/battlefield.gd` — responsive lane board, towers, aliens and hit cues.
- `scripts/question_factory.gd` — bounded first-chapter addition/subtraction.
- `scripts/reward_bucket.gd` — hidden reward allowance and duplicate prevention.
- `scripts/curriculum_save.gd` — versioned local progress profile.
- `tests/test_reward_bucket.gd` — reward accounting checks.

The authoritative accepted choices are in `../docs/migration-decisions.md`.
Landscape layout supports mouse and touch input. No mobile export or installer has
been produced. Godot was not available in the environment used for this edit, so
the project still needs an engine compile/run and visual playthrough.
