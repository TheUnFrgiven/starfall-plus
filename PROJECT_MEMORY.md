# Starfall+ project record

Updated 18 November 2026. The owner's adopted Unity base now also has an Android playtest build.

## Identity and source of truth

- Product: **Starfall+**. GitHub slug: **starfall-plus** (`+` is not permitted in repository names).
- Canonical local project: `/Users/theunfrgiven/Documents/Galactacians` (folder name is a stable workspace path, not the product name).
- Unity 6000.6.7f1 / C#, at the repository root. Scene: `Assets/Scenes/StarfallPlus.unity`.
- This replaces the earlier browser and Godot direction as the active implementation. Read `Docs/DECISIONS.md`; old migration records are archived history.
- The original standalone Unity checkout at `/Users/theunfrgiven/Documents/ChatGPT/copy this shit` is a preserved predecessor, not the active base.

## State

The pixel-art astronaut/elemental-meteor game, original music, collection/cosmetics, progressive unlocks, contextual Bot, expanded ship, 14-topic maths flow and account/statistics backend are included. Gmail sender credentials were replaced directly by the owner; real signup, delivered confirmation and the authenticated parent dashboard now work. Google OAuth setup, live multi-account statistics validation, durable offline sync, full cross-device saves, iOS export and long-session balance remain unfinished.

Android 0.1.1 is a normal saved-game APK for ARMv7/ARM64, Android 8.0+ and OpenGL ES 3.1. The official toolchain is installed; the build, APK signature/manifest, 16 KB ARM64 alignment, Android interruption checks and 21,000 learning checks passed. Physical-phone gameplay, input, accounts and performance remain unverified. Share the APK through WhatsApp as a document/file; see `Docs/ANDROID_TESTING.md`. Private signing files and native build caches stay ignored.

Do not infer completion from older build reports or fixtures. Preview does not persist or upload gameplay events. Use the normal saved build for the live statistics flow. The clearer gateway email-error response is committed but its hosted deployment remains pending after the browser editor rejected the attempted update; the working deployed gateway remains in place.

## Next design review

Six upgrades currently remain. Review `Docs/upgrade-design-review.md` before changing Collector, shared Tower improvement, Drones or Repair. Questions currently authorize purchases; maths based on the actual bill/income/protection is a proposed redesign, not an adopted mechanic.

## Records

See `Docs/PROJECT_STATUS.md` for current boundaries, `Docs/CHECKPOINT.md` for recovery points and `Backend/README.md` for data provenance and deployment. Historical names in compatibility paths, archived source, original art prompts and past commits are intentional. Do not rename live account IDs or rewrite history to change branding.
