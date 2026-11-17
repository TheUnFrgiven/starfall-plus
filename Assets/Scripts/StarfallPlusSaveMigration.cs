using System;
using System.IO;
using UnityEngine;

// Branding changes Unity's save directory. Copy known game saves once; keep originals.
public static class StarfallPlusSaveMigration
{
    public static int ImportFromDirectory(string source, string destination)
    {
        if (!Directory.Exists(source) || Path.GetFullPath(source) == Path.GetFullPath(destination)) return 0;
        int imported = 0;
        foreach (string file in Directory.GetFiles(source, "starfall-*.json"))
        {
            string name = Path.GetFileName(file);
            if (name != "starfall-pilot-v1.json" && name != "starfall-expedition-v1.json" &&
                name != "starfall-learning-v1.json" && !name.StartsWith("starfall-learning-v1-", StringComparison.Ordinal)) continue;
            Directory.CreateDirectory(destination);
            string target = Path.Combine(destination, name);
            if (File.Exists(target)) continue;
            File.Copy(file, target, false);
            imported++;
        }
        return imported;
    }

    internal static void ImportLegacySaves()
    {
        if (!Application.isPlaying || Application.productName.EndsWith("Preview", StringComparison.Ordinal)) return;
        if (Application.platform != RuntimePlatform.OSXPlayer && Application.platform != RuntimePlatform.WindowsPlayer &&
            Application.platform != RuntimePlatform.LinuxPlayer) return;
        string destination = Application.persistentDataPath;
        string marker = Path.Combine(destination, ".starfall-plus-migration-v1");
        if (File.Exists(marker)) return;
        try
        {
            string parent = Directory.GetParent(destination)?.Parent?.FullName;
            if (parent == null) return;
            foreach (string company in new[] { "Moonvale Studio", "MoonvaleStudio", "Moonvale-Studio" })
                ImportFromDirectory(Path.Combine(parent, company, "Runehold"), destination);
            Directory.CreateDirectory(destination);
            File.WriteAllText(marker, "Known game saves copied without replacing Starfall+ progress. Legacy files retained.");
        }
        catch (IOException) { Debug.LogWarning("Starfall+ could not import an older save. The original remains unchanged."); }
        catch (UnauthorizedAccessException) { Debug.LogWarning("Starfall+ could not access an older save. The original remains unchanged."); }
    }
}
