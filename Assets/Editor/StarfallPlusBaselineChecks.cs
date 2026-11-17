#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class StarfallPlusBaselineChecks
{
    public static void Run()
    {
        string temporary = Path.Combine(Path.GetTempPath(), "starfall-plus-save-check-" + Guid.NewGuid().ToString("N"));
        try
        {
            string old = Path.Combine(temporary, "old"), current = Path.Combine(temporary, "current");
            Directory.CreateDirectory(old); Directory.CreateDirectory(current);
            File.WriteAllText(Path.Combine(old, "starfall-pilot-v1.json"), "legacy cosmetics");
            File.WriteAllText(Path.Combine(old, "starfall-learning-v1-learner.json"), "legacy learning");
            File.WriteAllText(Path.Combine(old, "starfall-expedition-v1.json"), "old ship");
            File.WriteAllText(Path.Combine(old, "starfall-unrelated.json"), "must stay local");
            File.WriteAllText(Path.Combine(current, "starfall-expedition-v1.json"), "new ship");
            if (StarfallPlusSaveMigration.ImportFromDirectory(old, current) != 2) throw new Exception("Wrong import count.");
            if (File.ReadAllText(Path.Combine(current, "starfall-expedition-v1.json")) != "new ship") throw new Exception("Existing progress was replaced.");
            if (!File.Exists(Path.Combine(old, "starfall-pilot-v1.json"))) throw new Exception("An original save was removed.");
            if (File.Exists(Path.Combine(current, "starfall-unrelated.json"))) throw new Exception("An unrelated record was copied.");
            if (StarfallPlusSaveMigration.ImportFromDirectory(old, current) != 0) throw new Exception("Repeated migration changed progress.");
            if (PlayerSettings.productName != "Starfall+") throw new Exception("Wrong product name.");
            if (PlayerSettings.GetApplicationIdentifier(UnityEditor.Build.NamedBuildTarget.Standalone) != "com.starfallplus.game") throw new Exception("Wrong app identifier.");
            if (!File.Exists("Assets/Scenes/StarfallPlus.unity")) throw new Exception("Missing canonical scene.");
            Debug.Log("STARFALL+ BASELINE CHECKS PASSED: product identity, scene, non-destructive and idempotent save migration.");
            EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
    }
}
#endif
