#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.Rendering;

internal static class StarfallPlusBuild
{
    public static void BuildMac()
    {
        Build(false);
    }

    public static void BuildPreview()
    {
        Build(true);
    }

    internal static void ConfigureAndroidPlayer()
    {
        PlayerSettings.productName = "Starfall+";
        PlayerSettings.companyName = "StarfallPlus";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.starfallplus.game");
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Minimal);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARMv7 | AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = (AndroidSdkVersions)26;
        PlayerSettings.Android.targetSdkVersion = (AndroidSdkVersions)36;
        PlayerSettings.Android.bundleVersionCode = 2;
        PlayerSettings.Android.useCustomKeystore = false;
        PlayerSettings.Android.forceInternetPermission = true;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
        PlayerSettings.SetGraphicsAPIs(BuildTarget.Android, new[] { GraphicsDeviceType.OpenGLES3 });
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        EditorUserBuildSettings.androidBuildSubtarget = MobileTextureSubtarget.ETC2;
        Texture2D icon = Resources.Load<Texture2D>("Art/BotGuide");
        if (icon != null)
        {
            int[] sizes = PlayerSettings.GetIconSizes(NamedBuildTarget.Android, IconKind.Any);
            var icons = new Texture2D[sizes.Length];
            for (int i = 0; i < icons.Length; i++) icons[i] = icon;
            PlayerSettings.SetIcons(NamedBuildTarget.Android, icons, IconKind.Any);
#if UNITY_ANDROID
            const string backgroundPath = "Assets/Editor/StarfallPlusAndroidIconBackground.asset";
            var background = AssetDatabase.LoadAssetAtPath<Texture2D>(backgroundPath);
            if (background == null)
            {
                background = new Texture2D(16, 16, TextureFormat.RGBA32, false) { name = "Starfall+ icon background", filterMode = FilterMode.Point };
                var pixels = new Color[16 * 16];
                for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(228, 239, 248, 255);
                background.SetPixels(pixels); background.Apply();
                AssetDatabase.CreateAsset(background, backgroundPath);
            }
            var adaptiveKind = UnityEditor.Android.AndroidPlatformIconKind.Adaptive;
            var adaptiveIcons = PlayerSettings.GetPlatformIcons(NamedBuildTarget.Android, adaptiveKind);
            foreach (var adaptive in adaptiveIcons) adaptive.SetTextures(new[] { background, icon });
            PlayerSettings.SetPlatformIcons(NamedBuildTarget.Android, adaptiveKind, adaptiveIcons);
#endif
        }
    }

    public static void BuildAndroid()
    {
        try
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Android, BuildTarget.Android))
                throw new InvalidOperationException("Install Android Build Support, SDK/NDK Tools and OpenJDK for this Unity version.");
            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.Android)
                throw new InvalidOperationException("Start the batch build with -buildTarget Android.");
            PixelArtTextureSettings.RefreshArtwork();
            ConfigureAndroidPlayer();
            Directory.CreateDirectory("Build/Android");
            AssetDatabase.SaveAssets();
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/StarfallPlus.unity" },
                locationPathName = "Build/Android/StarfallPlus-android-0.1.1.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            });
            Debug.Log("STARFALL+ ANDROID BUILD: " + report.summary.result + " (" + report.summary.totalSize + " bytes)");
            EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
    }

    private static void Build(bool preview)
    {
        PixelArtTextureSettings.RefreshArtwork();
        string output = preview ? "Build/StarfallPlusPreview.app" : "Build/StarfallPlus.app";
        Directory.CreateDirectory("Build");
        string originalIdentifier = PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone);
        FullScreenMode originalScreenMode = PlayerSettings.fullScreenMode;
        bool originalBackground = PlayerSettings.runInBackground;
        if (preview) { PlayerSettings.fullScreenMode = FullScreenMode.Windowed; PlayerSettings.runInBackground = true; }
        PlayerSettings.productName = preview ? "Starfall+ Preview" : "Starfall+";
        if (preview) PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, "com.starfallplus.preview");
        PlayerSettings.companyName = "StarfallPlus";
        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/StarfallPlus.unity" },
            locationPathName = output,
            target = BuildTarget.StandaloneOSX,
            options = BuildOptions.None
        };
        BuildReport report = BuildPipeline.BuildPlayer(options);
        PlayerSettings.productName = "Starfall+";
        PlayerSettings.fullScreenMode = originalScreenMode;
        PlayerSettings.runInBackground = originalBackground;
        PlayerSettings.SetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone, originalIdentifier);
        Debug.Log("StarfallPlus build result: " + report.summary.result + " (" + report.summary.totalSize + " bytes)");
        EditorApplication.Exit(report.summary.result == BuildResult.Succeeded ? 0 : 1);
    }
}
#endif
