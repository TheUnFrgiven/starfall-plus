# Starfall+

A child-friendly pixel-art maths game: explore a spaceship as an astronaut, collect supplies, solve questions to confirm upgrades, and defend against growing swarms of elemental meteors.

**This Unity project is the active game and the base for future development.** The former Galactacians browser and Godot projects are historical references in `archive/galactacians/`. Their design register does not govern Starfall+. This direction was confirmed on 17 November 2026.

- **Engine:** Unity 6000.6.7f1; gameplay in C#.
- **Open:** this repository root in Unity Hub, then `Assets/Scenes/StarfallPlus.unity`.
- **Backend:** Supabase Auth/PostgreSQL and a TypeScript account gateway in [Backend](Backend/README.md).
- **Game page:** [Starfall+](https://theunfrgiven.github.io/starfall-plus/).
- **Parent dashboard:** [Starfall+ learning dashboard](https://starfall-plus-dashboard.vercel.app/).
- **Repository:** [TheUnFrgiven/starfall-plus](https://github.com/TheUnFrgiven/starfall-plus).
- **Current decisions:** [Docs/DECISIONS.md](Docs/DECISIONS.md).
- **Verified state and remaining work:** [Docs/PROJECT_STATUS.md](Docs/PROJECT_STATUS.md).
- **Checkpoint and recovery:** [Docs/CHECKPOINT.md](Docs/CHECKPOINT.md).
- **Android APK:** [0.1.1 playtest download](https://github.com/TheUnFrgiven/starfall-plus/releases/tag/android-test-0.1.1).
- **Android phone testing:** [Install, share and test the APK](Docs/ANDROID_TESTING.md).

## Included in the baseline

Manual astronaut movement and automatic shooting; daytime resource collection and persistent upgrades; night swarms; Towers, Shield, Collector, Blaster, Drones and Repair on one upgrade page; persistent Health with minimal dawn healing and a recharging Shield. The expanded ship uses shared rendering/collision coordinates and smaller building signs.

The floating Bot provides a staged tutorial and contextual help. The home menu offers Play, Learn maths, Collection and Settings. Collection contains progressive claimable Unlockables, colors, Statistics and Tools. Artwork uses the established pixel-art assets and crisp filtering; the Starfall+ wordmark includes a pixel plus.

Fourteen maths topics include Make ten, Make twenty, Make zero, doubles, near doubles, skip counting and missing numbers. Players choose Easy, Medium or Challenge. Worked examples animate the model, then ask a new similar question; a demonstration cannot purchase an upgrade. Learning evidence distinguishes independent answers, help, first-check accuracy, response time and spaced recall.

Parent accounts use a learner nickname. Email signup and confirmation have been verified with the owner. Private parent/teacher reporting, classes, assignments and server-graded comparisons are implemented; their complete live multi-account verification remains open. Google sign-in still needs provider configuration.

## Run and build

The normal build saves progress and can upload signed-in learning events. Preview is an isolated demonstration: it neither saves progress nor uploads gameplay statistics. Use the normal build for account/statistics verification.

```sh
# Substitute your Unity executable path if needed.
Unity -batchmode -nographics -quit -projectPath . -executeMethod StarfallPlusBuild.BuildMac -logFile build-mac.log
Unity -batchmode -nographics -quit -projectPath . -executeMethod StarfallPlusBuild.BuildPreview -logFile build-preview.log
Unity -batchmode -nographics -quit -buildTarget Android -projectPath . -executeMethod StarfallPlusBuild.BuildAndroid -logFile build-android.log
```

Outputs: `Build/StarfallPlus.app` and `Build/StarfallPlusPreview.app`. Display names are **Starfall+** and **Starfall+ Preview**. The normal desktop app identifier is `com.starfallplus.game`; Preview uses `com.starfallplus.preview`.

Android requires this editor's Android Build Support, SDK/NDK Tools and OpenJDK. Its normal saved-game output is `Build/Android/StarfallPlus-android-0.1.1.apk`, an installable universal ARMv7/ARM64 APK with package `com.starfallplus.game`. Send it through WhatsApp as a document/file. See the phone-testing guide for requirements and device checks; the desktop Preview is separate.

For a new checkout, copy `Assets/Resources/StarfallOnline.example.txt` to the ignored `StarfallOnline.json` and supply public client configuration. The owner's local checkout is configured. Never commit SMTP credentials, server keys, sessions, personal saves or student records.

## Verification

```sh
Unity -batchmode -nographics -quit -projectPath . -executeMethod StarfallPlusBaselineChecks.Run -logFile baseline-checks.log
Unity -batchmode -nographics -quit -projectPath . -executeMethod StarfallPlusLearningUpgradeChecks.Run -logFile learning-checks.log
Unity -batchmode -nographics -quit -projectPath . -executeMethod StarfallPlusAndroidChecks.Run -logFile android-checks.log
cd Backend
npm ci --ignore-scripts
npm test
npx deno@2.5.6 check --config deno.json supabase/functions/accounts/index.ts
```

Baseline checks cover identity, canonical scene and a non-destructive, repeatable legacy-save import. Learning checks generate 21,000 questions and exercise fixed targets, answer choices, upgrade authorization and expanded-map collision. PostgreSQL tests cover account isolation, controlled writes, private comparisons and challenge replay. Builds and automated checks do not establish phone usability, long-session balance or educational effectiveness.

## Compatibility and history

Known desktop save files from the older Unity app are copied on first normal launch if the new file does not exist. Originals are retained. Save filenames, database identifiers, existing account IDs and the working dashboard URL remain stable compatibility contracts; branding does not reset player records. Browser/Godot saves are separate formats and are not silently imported.

Historical prompts, artwork notes and previous status reports remain in [Docs/history](Docs/history/README.md). The [checkpoint record](Docs/CHECKPOINT.md) identifies the original source commits and the preserved local Godot work. Open issues are recorded honestly; this checkpoint is a development base, not a completed mobile release.
