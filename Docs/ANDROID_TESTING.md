# Android phone testing

Starfall+ uses the same Unity scene, pixel art, learning flow and six upgrades on Android. The normal phone build saves progress on that device and can upload signed-in learning events. It is not the isolated desktop Preview.

## Install

Send `StarfallPlus-android-0.1.1.apk` through WhatsApp as a **document/file**, or share the Android test release download link. On the phone, download the file and open it from WhatsApp or Downloads. Android may ask you to allow that browser or Files app to install unknown apps. Allow it for this installation; no Play Store subscription or developer account is needed. Launch **Starfall+** and hold the phone in landscape.

Requirements: Android 8.0 or newer, an ARMv7 or ARM64 processor and OpenGL ES 3.1 or newer. This universal APK includes both ARM architectures; it is not an x86 emulator build. Sign in with an existing parent account or use the offline learning/game options. Google sign-in still requires provider setup and is not part of this test.

This is a locally signed development distribution, not a Play Store release. Keep the signing key private and retain it locally for compatible test updates. A full app restart returns to the saved expedition setup/checkpoint rather than restoring every enemy mid-fight. Uninstalling clears device-local progress. Account statistics do not currently restore the ship or cosmetics to another device.

## Controls and checks on a real phone

- Move directly with the left stick. Shooting is automatic. Try moving while tapping Boost and, at night, Blast.
- Open the Bot, menus and upgrades. Check the smallest labels, scrolling, finger targets and both landscape orientations, including phones with camera cutouts.
- Solve a maths question, use a worked example, and confirm that the next similar question must be answered independently before an upgrade can be purchased.
- Check account text entry with the Android keyboard, sign-in and an actual learning-event upload to the private parent dashboard.
- During a night, switch to another app or lock the phone. Returning should show a pause; movement must stop until resumed. Repeat during menus and questions.
- Relaunch and check ship/learning progress. Health must not reset between nights; Shield recharges separately. Observe frame rate and temperature as swarms grow.

Report the phone model, Android version, the screen/action involved and a screenshot or recording for any issue. Desktop checks and APK inspection do not establish these device results.

## Rebuild

Install Android Build Support, SDK/NDK Tools and OpenJDK for Unity **6000.6.7f1**. Use the root project and supply the ignored public client configuration from its template. Do not commit private keys, credentials, saves or learner data.

```sh
Unity -batchmode -nographics -quit -projectPath . \
  -executeMethod StarfallPlusAndroidChecks.Run -logFile android-checks.log
Unity -batchmode -nographics -quit -buildTarget Android -projectPath . \
  -executeMethod StarfallPlusBuild.BuildAndroid -logFile android-build.log
```

Output: `Build/Android/StarfallPlus-android-0.1.1.apk`. Package: `com.starfallplus.game`; version 0.1.1, code 2; minimum API 26 and target API 36. The builder selects IL2CPP, ARMv7 + ARM64, landscape and the OpenGL ES 3 renderer (the generated manifest requires ES 3.1). The manifest postprocessor registers `starfallplus://auth` on the generated launcher activity without replacing Unity's GameActivity manifest. Callback registration alone does not verify Google OAuth.

## Verified 18 November 2026

The APK build succeeded (49,581,248 bytes). APK v2 signing, package/version/SDK values, both ARM architectures, landscape and callback registration, non-debuggable status, ZIP alignment and all six ARM64 libraries' 16 KB LOAD alignment passed inspection. Android configuration/interruption fixtures and 21,000 learning checks passed. `SHA256SUMS` accompanies the download.

No emulator or physical-phone gameplay session was performed. The real-phone checks above remain open. Bot launcher artwork is included in legacy and adaptive icons. Exported layers were visually inspected; a padded foreground keeps the Bot inside launcher masks.

The adaptive foreground uses Android's [InsetDrawable](https://developer.android.com/reference/android/graphics/drawable/InsetDrawable) so padding changes the icon layout without modifying the original Bot artwork.
