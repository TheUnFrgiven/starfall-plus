using System;

public static class StarfallCosmetics
{
    public static readonly string[] Names = { "Orange", "Green", "Purple", "Gold", "Blue", "Pink", "Red", "Teal" };
    public static readonly int[] Prices = { 0, 2, 4, 6, 3, 4, 5, 5 };
    public static readonly string[] Armor = { "ff744c", "58b990", "9872dc", "edb34d", "4b90e6", "ed7da8", "e66162", "4ac6ba" };
    public static readonly string[] Energy = { "68e4f0", "8de0a8", "c2a0ff", "ffca63", "9bd7ff", "ffc0df", "ffad91", "98f2dd" };
    public static int Count => Names.Length;
}

public sealed class StarfallUnlockTrack
{
    public readonly string Id;
    public readonly int Metric, Badge;
    public readonly int[] Targets, Rewards;
    public StarfallUnlockTrack(string id, int metric, int badge, int[] targets, int[] rewards)
    { Id = id; Metric = metric; Badge = badge; Targets = targets; Rewards = rewards; }
    public string Key(int stage) => Id + ":" + stage;
    public string Goal(int stage)
    {
        int n = Targets[Math.Min(stage, Targets.Length - 1)];
        string count = n.ToString("N0");
        switch (Metric)
        {
            case 0: return stage == 0 ? "Build your first Tower" : "Make " + count + " upgrades";
            case 1: return "Survive " + count + (n == 1 ? " night" : " nights");
            case 3: return "Use Blast " + count + " times";
            case 4: return "Protect all Health for " + count + " nights";
            case 5: return "Stop " + count + " meteors";
            case 6: return "Solve " + count + " puzzles with help";
            case 7: return "Solve " + count + " puzzles yourself";
            case 8: return "Practise on " + count + (n == 1 ? " day" : " days");
            case 10: return "Reach pilot level " + count;
            default: return "Reach power " + count;
        }
    }
}

public static class StarfallUnlocks
{
    public static readonly StarfallUnlockTrack[] Tracks = {
        new StarfallUnlockTrack("build", 0, 0, new[] { 1, 5, 15, 30, 60 }, new[] { 1, 1, 2, 3, 4 }),
        new StarfallUnlockTrack("night", 1, 1, new[] { 1, 5, 10, 20, 40 }, new[] { 2, 2, 3, 4, 5 }),
        new StarfallUnlockTrack("blast", 3, 3, new[] { 3, 10, 25, 50, 100 }, new[] { 1, 2, 2, 3, 4 }),
        new StarfallUnlockTrack("health", 4, 4, new[] { 3, 5, 10, 20 }, new[] { 2, 2, 3, 4 }),
        new StarfallUnlockTrack("meteors", 5, 5, new[] { 1000, 2500, 5000, 10000, 25000 }, new[] { 2, 2, 3, 4, 5 }),
        new StarfallUnlockTrack("help", 6, 6, new[] { 5, 15, 30, 60, 100 }, new[] { 1, 1, 2, 3, 4 }),
        new StarfallUnlockTrack("solve", 7, 7, new[] { 10, 25, 50, 100, 200 }, new[] { 2, 2, 3, 4, 5 }),
        new StarfallUnlockTrack("practice", 8, 8, new[] { 1, 3, 7, 14, 30 }, new[] { 1, 2, 2, 3, 4 }),
        new StarfallUnlockTrack("level", 10, 10, new[] { 18, 25, 40, 60, 100 }, new[] { 2, 2, 3, 4, 5 }),
        new StarfallUnlockTrack("power", 11, 11, new[] { 60, 100, 180, 300, 500 }, new[] { 3, 3, 4, 5, 6 })
    };

    public static int Stage(StarfallPilotJournal journal, StarfallUnlockTrack track)
    {
        for (int i = 0; i < track.Targets.Length; i++)
            if (!journal.ClaimedStages.Contains(track.Key(i))) return i;
        return track.Targets.Length;
    }

    public static bool Claim(StarfallPilotJournal journal, StarfallUnlockTrack track, int value)
    {
        int stage = Stage(journal, track);
        if (stage >= track.Targets.Length || value < track.Targets[stage]) return false;
        journal.ClaimedStages.Add(track.Key(stage));
        journal.Tokens += track.Rewards[stage];
        return true;
    }

    public static void Migrate(StarfallPilotJournal journal)
    {
        if (journal.Achievements == null) journal.Achievements = new System.Collections.Generic.List<string>();
        if (journal.ClaimedStages == null) journal.ClaimedStages = new System.Collections.Generic.List<string>();
        if (journal.NotifiedStages == null) journal.NotifiedStages = new System.Collections.Generic.List<string>();
        if (journal.UnlockRules >= 1) return;
        // Legacy rewards were already paid automatically. Mark exactly those stages,
        // keeping the existing balance and owned colors; never pay them a second time.
        int[] legacyTrack = { 0, 1, 0, 2, 3, 4, 5, 6, 7, 1, 8, 9 };
        int[] legacyStage = { 0, 0, 1, 0, 0, 0, 0, 0, 0, 1, 0, 0 };
        for (int i = 0; i < legacyTrack.Length; i++)
        {
            if (!journal.Achievements.Contains("relay-" + i)) continue;
            string key = Tracks[legacyTrack[i]].Key(legacyStage[i]);
            if (!journal.ClaimedStages.Contains(key)) journal.ClaimedStages.Add(key);
        }
        journal.UnlockRules = 1;
    }
}
