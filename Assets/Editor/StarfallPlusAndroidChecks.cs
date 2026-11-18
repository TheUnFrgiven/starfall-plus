#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

internal static class StarfallPlusAndroidChecks
{
    private static void Require(bool value, string reason) { if (!value) throw new Exception(reason); }

    public static void Run()
    {
        string manifest = Path.Combine(Path.GetTempPath(), "starfall-android-check-" + Guid.NewGuid().ToString("N") + ".xml");
        try
        {
            StarfallPlusBuild.ConfigureAndroidPlayer();
            Require(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android) == "com.starfallplus.game", "Wrong package identity.");
            Require(PlayerSettings.GetScriptingBackend(NamedBuildTarget.Android) == ScriptingImplementation.IL2CPP, "ARM64 needs IL2CPP.");
            Require(PlayerSettings.Android.targetArchitectures == (AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64), "Missing phone architecture.");
            Require(!EditorUserBuildSettings.buildAppBundle && !EditorUserBuildSettings.exportAsGoogleAndroidProject, "The output must be a directly installable APK.");
            Require(!PlayerSettings.allowedAutorotateToPortrait && !PlayerSettings.allowedAutorotateToPortraitUpsideDown && PlayerSettings.allowedAutorotateToLandscapeLeft && PlayerSettings.allowedAutorotateToLandscapeRight, "Landscape orientation is not configured.");
            Require(PlayerSettings.GetGraphicsAPIs(BuildTarget.Android).SequenceEqual(new[] { GraphicsDeviceType.OpenGLES3 }), "Missing GLES phone renderer.");
            File.WriteAllText(manifest, "<manifest xmlns:android=\"http://schemas.android.com/apk/res/android\"><application><activity android:name=\"com.unity3d.player.UnityPlayerGameActivity\"><intent-filter><action android:name=\"android.intent.action.MAIN\"/><category android:name=\"android.intent.category.LAUNCHER\"/></intent-filter></activity><activity android:name=\"OtherActivity\"/></application></manifest>");
            StarfallPlusAndroidManifest.RegisterCallback(manifest);
            StarfallPlusAndroidManifest.RegisterCallback(manifest);
            var doc = XDocument.Load(manifest); XNamespace android = "http://schemas.android.com/apk/res/android";
            var data = doc.Descendants("data").ToList();
            Require(data.Count == 1 && (string)data[0].Attribute(android + "scheme") == "starfallplus" && (string)data[0].Attribute(android + "host") == "auth", "Native callback was lost or duplicated.");
            Require(data[0].Parent.Parent.Attribute(android + "launchMode")?.Value == "singleTask", "Callback must reuse the running game.");
            using (var p = new StarfallPlusEconomyChecks.Probe())
            {
                p.Enum("phase", "Night"); p.Set("menuOpen", false); p.Set("pauseOpen", false);
                p.Set("touchMove", Vector2.one); p.Set("moveFinger", 4); p.Set("touchBoost", true);
                p.Call("PauseForMobileInterruption");
                Require((bool)p.Get("pauseOpen") && p.Phase == "Night", "Returning from an interruption must not resume combat automatically.");
                Require((Vector2)p.Get("touchMove") == Vector2.zero && p.Number("moveFinger") == -1 && !(bool)p.Get("touchBoost"), "An interrupted touch stayed active.");
                p.Set("menuOpen", true); p.Set("pauseOpen", false); p.Call("PauseForMobileInterruption");
                Require(!(bool)p.Get("pauseOpen"), "Account/browser transitions must not create a hidden pause overlay.");
            }
            AssetDatabase.SaveAssets();
            Debug.Log("STARFALL+ ANDROID CHECKS PASSED: APK settings, landscape, callback registration and interrupted input/combat.");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        finally { if (File.Exists(manifest)) File.Delete(manifest); }
    }
}
#endif
