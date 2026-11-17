#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

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
