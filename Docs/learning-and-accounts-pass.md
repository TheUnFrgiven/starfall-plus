# Starfall+ learning, accounts and expanded ship

17 November 2026.

## Implemented learning flow

Learn maths contains 14 topics. The original six topic IDs are preserved for saved progress; eight are appended: Make twenty, Make zero, Doubles, Near doubles, Count by twos, Count by fives, Count by tens and Missing number.

Make ten now asks a missing part whose completed sum equals exactly ten, such as `7 + ? = 10`. Make twenty targets exactly twenty; Make zero asks how much to remove to leave zero. Doubles and near doubles train related facts. Counting topics use short fixed-step sequences. All topics stay open for practice.

A topic opens a short teaching idea and a numerical example. Choose Easy, Medium or Challenge before starting. General arithmetic uses wider ranges at higher difficulty. Fixed-target topics keep their promised target and vary the missing-part range and visual scaffolding. Challenge does not pretend that Make ten should suddenly equal another total. Quick practice is removed.

The visual worked-example action starts the model at its initial state and animates its steps. Afterwards it asks a fresh similar question. It does not count the demonstration as a completed answer, grant XP or install an upgrade. Asking for help remains available on the replacement problem. Correctly answering that replacement authorizes the upgrade. Mistakes, cancellations and demonstration do not spend resources.

Missing-number models do not print the missing answer before help is requested. The learner can count known cells and empty spaces. Topic examples contain equations rather than explanatory paragraphs. A timer is not imposed.

Existing adventure-topic unlock gates and resource pricing remain. The older proposal to tie questions to real purchase budgets has not been silently adopted; see the upgrade review for decisions still needed.

## Gameplay and UI

The deck transform increased from 1.75 to 3.5: twice the width and height, four times the area. The original artwork and point filtering are retained. Rendering, floor collision, building/resource positions and the map navigator share the transform. Old saved positions are migrated once using the expedition layout version; a valid floor fallback and swept movement prevent off-floor traversal. Meteor spawn radii move outside the enlarged ship. Travel and approach distances therefore change and need long-session balance review.

Map-sign boxes changed from 118×31 to 82×22, with rounded text sizes. Their invisible interaction area remains larger than the visible sign. Bot no longer pauses for every held input or unrelated touch: it moves on its safe routes and pauses only when approached/hovered, when dialogue is open, or when calmer motion is enabled. All six upgrades share a two-column page with costs and before/after benefits. Add Tower and Improve Towers are explicit choices while tower slots remain.

The nickname prompt uses the astronaut illustration; the floating help Bot remains the single guide. Topic and statistics scrolling use the same quiet blue-gray rail as Unlockables.

## Verification report

Story: a parent sets up an account, chooses a learner nickname, and the learner practises a selected topic/difficulty; observed learning events are stored privately and linked adults see the resulting statistics. Competitive scores use separately server-issued questions.

| Boundary | Evidence and limits |
| --- | --- |
| Maths generation | 21,000 generated questions checked for arithmetic validity, unique choices, exact fixed targets, doubles/near doubles and old-save array expansion. |
| Upgrade authorization | A worked example cannot install hardware. A replacement question can authorize one purchase after its correct answer. |
| Expanded map | Checks cover exact 4480×2800 world bounds, a valid fallback position and a large swept movement remaining on the floor. |
| Native UI | Desktop review covered parent setup, nickname entry, 14-topic selection, numerical teaching example, difficulty choice, exact-ten question, worked example, fresh similar question, learning Statistics and all six upgrades on one screen. |
| Recorded result | `7 + ? = 10` was demonstrated as `7 + 3 = 10`, then replaced by `9 + ? = 10`. Answering 1 produced one independent Make ten check in Statistics. |
| Browser UI | The dashboard rendered live over HTTPS. Fixture-backed browser review rendered topic/difficulty, 8/10 first checks, 20% help, an 8.2-second median, a matched week-to-week trend and a private class board. Those figures were test fixtures, not real students. |
| API to data | PostgreSQL tests apply both migrations and cover row isolation, teacher access/revocation, controlled writes, rate limits, event deduplication, private leaderboard access and single-use challenges. The live gateway reaches its rate-limit database helper and permits the dashboard origin. |
| Hosted protection | All 11 game tables have RLS enabled. Anonymous learner and statistics reads return 401. Email lookup is denied to authenticated player roles. Security Advisor: 0 errors, 0 warnings; its two informational notices describe the intentionally policy-free server-only private tables. |
| Email | The owner entered and saved the replacement Gmail app password directly in Supabase. Real signup, delivered confirmation and the authenticated parent dashboard were then verified. |
| Google | Provider is currently disabled. A Google OAuth client and native callback registration are still required. |

Native mouse automation remained unreliable. Keyboard-assisted desktop paths and automated hit tests do not establish mobile touch/multitouch behavior or long-term combat balance. The native callback is not yet validated on exported target platforms. Real signup and the authenticated parent dashboard were verified. The full teacher/multi-account workflow remains unverified.

Detailed account architecture, data provenance and deployment notes: [Backend/README.md](../Backend/README.md). All upgrades and narrowing questions: [upgrade-design-review.md](upgrade-design-review.md).
