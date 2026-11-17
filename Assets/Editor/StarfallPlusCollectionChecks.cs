#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

internal static class StarfallPlusCollectionChecks
{
    private static void Require(bool condition, string message)
    { if (!condition) throw new InvalidOperationException(message); }

    [MenuItem("Starfall/Check Collection Progression")]
    public static void Run()
    {
        try
        {
            CheckNightClaims(); CheckLegacyMigration(); CheckSaveAndColors(); CheckEveryTrack(); CheckBotHelp();
            Debug.Log("STARFALL COLLECTION CHECKS PASSED: sequential claims, duplicate prevention, legacy migration, save roundtrip, all track stages, eight color indices and contextual Bot activation.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            if (Application.isBatchMode) EditorApplication.Exit(1); else throw;
        }
    }

    private static void CheckNightClaims()
    {
        var journal = new StarfallPilotJournal(); StarfallUnlocks.Migrate(journal);
        StarfallUnlockTrack night = StarfallUnlocks.Tracks[1];
        Require(!StarfallUnlocks.Claim(journal, night, 0) && journal.Tokens == 0, "An unfinished goal paid a reward.");
        Require(StarfallUnlocks.Claim(journal, night, 1) && journal.Tokens == 2, "First night reward was not paid exactly once.");
        Require(night.Goal(StarfallUnlocks.Stage(journal, night)) == "Survive 5 nights", "Claim did not advance to the related next goal.");
        Require(!StarfallUnlocks.Claim(journal, night, 1) && journal.Tokens == 2, "Repeating a completed milestone paid twice.");
        Require(!StarfallUnlocks.Claim(journal, night, 4), "Next goal paid before five nights.");
        Require(StarfallUnlocks.Claim(journal, night, 5) && journal.Tokens == 4, "Five-night reward did not advance correctly.");
        string saved = JsonUtility.ToJson(journal);
        var restored = JsonUtility.FromJson<StarfallPilotJournal>(saved);
        Require(!StarfallUnlocks.Claim(restored, night, 5) && restored.Tokens == 4, "Reloading allowed a duplicate claim.");
    }

    private static void CheckLegacyMigration()
    {
        var journal = new StarfallPilotJournal { Tokens = 17 };
        journal.Achievements.AddRange(new[] { "relay-0", "relay-2", "relay-1", "relay-9", "relay-5" });
        journal.Liveries.Add(3);
        StarfallUnlocks.Migrate(journal); StarfallUnlocks.Migrate(journal);
        Require(journal.Tokens == 17 && journal.Liveries.Contains(3), "Migration changed earned credits or colors.");
        Require(StarfallUnlocks.Stage(journal, StarfallUnlocks.Tracks[0]) == 2, "Build achievements were not merged.");
        Require(StarfallUnlocks.Stage(journal, StarfallUnlocks.Tracks[1]) == 2, "Night achievements were not merged.");
        Require(!StarfallUnlocks.Claim(journal, StarfallUnlocks.Tracks[1], 5), "A paid legacy reward could be collected again.");
        Require(journal.ClaimedStages.Count == 5, "Repeated migration duplicated milestone records.");
    }

    private static void CheckSaveAndColors()
    {
        var journal = new StarfallPilotJournal { Tokens = 9 };
        StarfallUnlocks.Migrate(journal); journal.Liveries.Add(7);
        var copy = JsonUtility.FromJson<StarfallPilotJournal>(JsonUtility.ToJson(journal));
        Require(copy.Liveries.Contains(7) && copy.Tokens == 9 && copy.UnlockRules == 1, "Expanded collection did not roundtrip through the save format.");
        for (int i = 0; i < StarfallCosmetics.Count; i++)
        {
            var profile = new StarfallLearningProfile { Accent = i }; profile.Validate();
            Require(profile.Accent == i, "A valid color was clamped out of the learning profile.");
        }
        Require(StarfallCosmetics.Names[0] == "Orange" && StarfallCosmetics.Names[3] == "Gold", "Existing color identities changed.");
    }

    private static void CheckEveryTrack()
    {
        foreach (StarfallUnlockTrack track in StarfallUnlocks.Tracks)
        {
            var journal = new StarfallPilotJournal(); StarfallUnlocks.Migrate(journal);
            int total = 0;
            Require(track.Targets.Length == track.Rewards.Length, "A goal lacks a reward.");
            for (int stage = 0; stage < track.Targets.Length; stage++)
            {
                Require(stage == 0 || track.Targets[stage] > track.Targets[stage - 1], "Track goals do not increase.");
                Require(StarfallUnlocks.Claim(journal, track, track.Targets[stage]), "A completed goal could not be claimed.");
                total += track.Rewards[stage];
                Require(journal.Tokens == total && StarfallUnlocks.Stage(journal, track) == stage + 1, "A claim skipped or repeated a goal.");
            }
            Require(!StarfallUnlocks.Claim(journal, track, int.MaxValue) && journal.Tokens == total, "A finished track kept paying.");
        }
    }

    private static void CheckBotHelp()
    {
        using (var probe = new StarfallPlusEconomyChecks.Probe())
        {
            probe.Set("menuOpen", true); probe.Enum("hubPage", "Achievements");
            probe.Set("floatingBot", new Vector2(1180, 230));
            Require(!(bool)probe.Call("TryOpenBotAt", new Vector2(900, 230)), "A click outside Bot opened help.");
            Require((bool)probe.Call("TryOpenBotAt", new Vector2(1180, 230)), "Bot's hit target did not open help.");
            Require((bool)probe.Get("guideOpen"), "Bot help did not pause the underlying interface.");
            Require(((string[])probe.Get("guidePages"))[0].Contains("progression tracks"), "Bot did not explain the active Unlockables screen.");
            Require(!(bool)probe.Call("TryOpenBotAt", new Vector2(1180, 230)), "A second pointer event reopened the dialogue.");
            probe.Call("FinishBot"); probe.Enum("hubPage", "Settings");
            Require((bool)probe.Call("TryOpenBotAt", new Vector2(1180, 230)), "Bot could not reopen on another screen.");
            Require(((string[])probe.Get("guidePages"))[0].Contains("calmer motion"), "Bot did not change its advice for Settings.");
        }
    }
}
#endif
