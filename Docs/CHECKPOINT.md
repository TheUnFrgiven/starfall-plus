# Starfall+ base checkpoint — 17 November 2026

The owner designated the current Unity game as the new Galactacians successor, named **Starfall+**, and authorized updating the GitHub repository and project records.

## Recovery points

- New baseline tag: `starfall-plus-base-2026-11-17` (the validated renamed root Unity project).
- Pre-rebrand working Unity/account publication: `f6e229324d4b01a558849339dcaf2fef111cda7a`, tagged `starfall-pre-plus-2026-11-17`.
- Local browser/Godot work that was uncommitted when this task began: `c3a2bd8`, branch `codex/checkpoint-before-starfall-plus`, tag `galactacians-local-checkpoint-2026-11-17`. This checkpoint preserves those exact files without replacing the newer remote Godot history.
- The previous remote main is retained through its history and the `galactacians-remote-checkpoint-2026-11-17` tag.

No published history was rewritten. The active project moved from `unity/starfall-relay/` to the repository root; the former root browser/Godot code moved to `archive/galactacians/`. Unity script/scene `.meta` GUIDs were preserved while renaming source files and classes. Original names remain in historical archives and explicit compatibility migration paths.

## Identity

Display name: **Starfall+**. Repository: `https://github.com/TheUnFrgiven/starfall-plus`. C# entry point: `StarfallPlusGame`; build entry point: `StarfallPlusBuild`. Scene: `Assets/Scenes/StarfallPlus.unity`. Normal build: `Build/StarfallPlus.app`; isolated Preview: `Build/StarfallPlusPreview.app`.

The existing database/project IDs, student/account IDs, save formats and production dashboard address remain stable so branding does not reset or disconnect accounts. Existing server records and private saves are not published as source artifacts.

## Restore

Clone the repository and check out the desired checkpoint tag into a separate folder. Open the root containing `Assets`, `Packages` and `ProjectSettings` in Unity 6000.6.7f1. For the old nested checkpoint, open `unity/starfall-relay` instead. Supply public client configuration from your own environment using the checked-in template; provider secrets stay in Supabase.

GitHub release notes list the validation results and any supplied Mac build. Source control does not include local player data, generated caches, credentials or dependencies.

## Completed baseline validation

The renamed identity/save migration checks and all 21,000 learning checks passed. Backend PostgreSQL tests and Deno checks passed. Both Starfall+ Mac builds succeeded. The hosted dashboard uses the new brand, both domains remain supported, and the new native callback is allow-listed. The source baseline was begun on 17 November and published on 18 November local time. The dated checkpoint tag retains its original 17 November identifier.

## Android playtest — 18 November 2026

The `android-test-0.1.1` tag adds the normal saved-game Android APK builder, landscape/touch interruption handling, the account callback registration and adaptive Bot launcher artwork. The original `starfall-plus-base-2026-11-17` recovery point is unchanged. The Android release carries the APK and its checksum; private signing keys, local configuration and generated build caches remain excluded from source.

The Android build and artifact inspection passed, along with Android configuration/interruption fixtures and the 21,000 learning checks. Real-phone gameplay, accounts, layout and performance remain unverified. See `ANDROID_TESTING.md` and `PROJECT_STATUS.md` for the exact limits.
