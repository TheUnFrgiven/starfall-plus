#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
internal static class StarfallPlusProjectSetup
{
    private const string StarterScenePath = "Assets/Scenes/StarfallPlus.unity";

    static StarfallPlusProjectSetup()
    {
        EditorApplication.delayCall += EnsureStarterScene;
    }

    private static void EnsureStarterScene()
    {
        if (File.Exists(StarterScenePath)) return;
        Directory.CreateDirectory("Assets/Scenes");
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var root = new GameObject("Starfall+ Game");
        root.AddComponent<StarfallPlusGame>();
        EditorSceneManager.SaveScene(scene, StarterScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(StarterScenePath, true) };
    }
}
#endif
