using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public sealed class StarfallActivityDay
{
    public string Date;
    public int Solved, Independent, Supported, Mistakes, Builds, Salvage, Kills, Victories;
}

[Serializable]
public sealed class StarfallPilotJournal
{
    public int Version = 1, Tokens, HighestNight, CleanDefenses, Builds, Salvage, Pulses;
    public string TrackingSince = "";
    public List<string> Achievements = new List<string>();
    public int UnlockRules;
    public List<string> ClaimedStages = new List<string>(), NotifiedStages = new List<string>();
    public List<int> Liveries = new List<int> { 0 };
    public List<StarfallActivityDay> Days = new List<StarfallActivityDay>();
    public int[] Setpoints = new int[6];
    public float SoundVolume = .55f;
    public float MusicVolume = .32f;
    public int MusicSettingsVersion;
}

public sealed partial class RuneholdGame
{
    private StarfallPilotJournal pilotJournal = new StarfallPilotJournal();
    private bool journalLoaded;
    private int profileTab, collectionTab;
    private float achievementClock, pulseCharge = 35, pulseAge = 10, dashCooldown;
    private Vector2 pulseOrigin, lastMoveDirection = Vector2.down;
    private readonly List<CapacitorDrop> capacitors = new List<CapacitorDrop>();
    private readonly Queue<ExpeditionNotice> expeditionNotices = new Queue<ExpeditionNotice>();
    private ExpeditionNotice currentExpeditionNotice;
    private float expeditionNoticeTime;
    private readonly Dictionary<Texture2D, Texture2D> liveryTextures = new Dictionary<Texture2D, Texture2D>();
    private int cachedLivery = -1;
    private bool showFactRecords;
    private int factPage;
    private AudioSource relayAudio;
    private readonly List<AudioClip> relaySounds = new List<AudioClip>();
    private float nextShotSound;
    private sealed class CapacitorDrop { public Vector2 Position; public float Age; }
    private sealed class ExpeditionNotice { public string Title, Body; public Color Color; }
    private static readonly string[] LiveryNames = StarfallCosmetics.Names;
    private static readonly int[] LiveryPrices = StarfallCosmetics.Prices;
    private string JournalPath => Path.Combine(Application.persistentDataPath, "starfall-pilot-v1.json");
    private static string LocalDay => DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
    private int InstalledRanks => turretCount + shieldLevels + salvageLevels + turretMastery + shieldMastery + salvageMastery + rifleLevels + droneLevels + regenerationLevels;

    private void InitializeCombinedExpedition()
    {
        bool migrateLegacyColors = true;
        if (CanPersistProgress && File.Exists(JournalPath))
        {
            try { var saved = JsonUtility.FromJson<StarfallPilotJournal>(File.ReadAllText(JournalPath)); if (saved != null && saved.Version == 1) { pilotJournal = saved; migrateLegacyColors = false; } }
            catch (Exception e) { Debug.LogWarning("Pilot journal could not be read: " + e.GetType().Name); }
        }
        if (pilotJournal.Achievements == null) pilotJournal.Achievements = new List<string>();
        if (pilotJournal.Liveries == null) pilotJournal.Liveries = new List<int>();
        if (pilotJournal.Days == null) pilotJournal.Days = new List<StarfallActivityDay>();
        pilotJournal.Days.RemoveAll(d => d == null || !DateTime.TryParseExact(d.Date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out _));
        pilotJournal.Tokens = Mathf.Max(0, pilotJournal.Tokens);
        pilotJournal.HighestNight = Mathf.Max(pilotJournal.HighestNight, phase == Phase.Dawn ? night : night - 1);
        Array.Resize(ref pilotJournal.Setpoints, 6);
        if (string.IsNullOrEmpty(pilotJournal.TrackingSince)) pilotJournal.TrackingSince = LocalDay;
        if (!pilotJournal.Liveries.Contains(0)) pilotJournal.Liveries.Add(0);
        int[] legacyStars = { 0, 1, 3, 5 };
        for (int i = 1; i < 4; i++)
            if ((migrateLegacyColors && learningProfile.Stars >= legacyStars[i] || learningProfile.Accent == i) && !pilotJournal.Liveries.Contains(i)) pilotJournal.Liveries.Add(i);
        if (!pilotJournal.Liveries.Contains(learningProfile.Accent)) learningProfile.Accent = 0;
        StarfallUnlocks.Migrate(pilotJournal);
        journalLoaded = true;
        InitializeRelayAudio();
        InitializeRelayMusic();
        ApplyLivery(); RefreshAchievements(); SavePilotJournal();
    }

    private StarfallActivityDay ActivityToday()
    {
        string key = LocalDay;
        StarfallActivityDay day = pilotJournal.Days.Find(d => d.Date == key);
        if (day == null) { day = new StarfallActivityDay { Date = key }; pilotJournal.Days.Add(day); }
        if (pilotJournal.Days.Count > 90) pilotJournal.Days.RemoveRange(0, pilotJournal.Days.Count - 90);
        return day;
    }

    private bool SavePilotJournal()
    {
        if (!journalLoaded || !CanPersistProgress) return false;
        try
        {
            Directory.CreateDirectory(Application.persistentDataPath);
            File.WriteAllText(JournalPath + ".tmp", JsonUtility.ToJson(pilotJournal, true));
            File.Copy(JournalPath + ".tmp", JournalPath, true); File.Delete(JournalPath + ".tmp");
            return true;
        }
        catch (Exception e) { Debug.LogWarning("Pilot journal could not be saved: " + e.GetType().Name); return false; }
    }

    private void UpdateExpeditionPresentation(float dt)
    {
        achievementClock -= dt;
        if (achievementClock <= 0) { RefreshAchievements(); achievementClock = .5f; }
        bool canShow = !pauseOpen && !buildPanelOpen && !learningPanelOpen && !raidBriefOpen && phase != Phase.Quiz && phase != Phase.Dawn && phase != Phase.UpgradeReveal && phase != Phase.Warning;
        if (canShow)
        {
            expeditionNoticeTime -= dt;
            if (expeditionNoticeTime <= 0 && expeditionNotices.Count > 0)
            { currentExpeditionNotice = expeditionNotices.Dequeue(); expeditionNoticeTime = 5; }
        }
    }

    private void Announce(string title, string body, Color color)
    {
        if (expeditionNotices.Count >= 8) expeditionNotices.Dequeue();
        expeditionNotices.Enqueue(new ExpeditionNotice { Title = title, Body = body, Color = color });
    }

    private void DrawExpeditionNotice()
    {
        if (currentExpeditionNotice == null || expeditionNoticeTime <= 0 || !FieldControlsAvailable || noticeTimer > 0) return;
        UiCard(new Rect(355, 110, 565, 82), Gold);
        UiText(currentExpeditionNotice.Title.Replace("ACHIEVEMENT · ", "New badge: "), new Rect(373, 117, 529, 30), 23, true, UiBlue);
        UiText(currentExpeditionNotice.Body.Replace("color credits", "color credits"), new Rect(373, 152, 529, 31), 18, false, UiMuted);
    }

    private int TotalLearning(bool independent)
    {
        int sum = 0; foreach (int value in independent ? learningProfile.Independent : learningProfile.Supported) sum += value;
        return sum;
    }

    private int AchievementValue(int i)
    {
        switch (i)
        {
            case 0: return turretCount;
            case 1: return pilotJournal.HighestNight;
            case 2: return InstalledRanks;
            case 3: return pilotJournal.Pulses;
            case 4: return pilotJournal.CleanDefenses;
            case 5: return lifetimeKills;
            case 6: return TotalLearning(false);
            case 7: return TotalLearning(true);
            case 8: return learningProfile.Stars;
            case 9: return pilotJournal.HighestNight;
            case 10: return astronautLevel;
            default: return AscensionPower;
        }
    }
    private int UnlockValue(StarfallUnlockTrack track, int stage)
        => track.Metric == 0 && stage > 0 ? InstalledRanks : AchievementValue(track.Metric);

    private void RefreshAchievements()
    {
        if (!journalLoaded) return;
        bool changed = false;
        foreach (StarfallUnlockTrack track in StarfallUnlocks.Tracks)
        {
            int stage = StarfallUnlocks.Stage(pilotJournal, track);
            if (stage >= track.Targets.Length || UnlockValue(track, stage) < track.Targets[stage] || pilotJournal.NotifiedStages.Contains(track.Key(stage))) continue;
            pilotJournal.NotifiedStages.Add(track.Key(stage)); changed = true;
            Announce("Reward ready", track.Goal(stage) + ". Claim it in Collection > Unlockables.", Gold);
        }
        if (changed) SavePilotJournal();
    }

    private void ClaimUnlock(int index)
    {
        StarfallUnlockTrack track = StarfallUnlocks.Tracks[index];
        int stage = StarfallUnlocks.Stage(pilotJournal, track);
        if (stage >= track.Targets.Length || !StarfallUnlocks.Claim(pilotJournal, track, UnlockValue(track, stage))) return;
        collectedTrack = index; collectedTrackUntil = Time.unscaledTime + 4;
        PlayRelaySound(2); RefreshAchievements(); SavePilotJournal();
    }

    private string NextMilestone()
    {
        foreach (StarfallUnlockTrack track in StarfallUnlocks.Tracks)
        {
            int stage = StarfallUnlocks.Stage(pilotJournal, track);
            if (stage < track.Targets.Length) return track.Goal(stage) + " · " + Mathf.Min(track.Targets[stage], UnlockValue(track, stage)) + "/" + track.Targets[stage];
        }
        return "All goals completed. Keep exploring!";
    }

    private void RecordEngineeringBuilt()
    {
        pilotJournal.Builds++; ActivityToday().Builds++;
        pilotJournal.Setpoints[(int)selectedUpgrade] = question.Answer;
        RefreshAchievements(); SavePilotJournal();
    }

    private void RecordPilotAnswer(bool independent)
    {
        StarfallActivityDay day = ActivityToday();
        day.Solved++; if (independent) day.Independent++; else day.Supported++;
        learningProfile.LearningXp += independent ? 10 : 4;
        RefreshPracticeDate(); learningProfile.DailySolved++;
        if (learningProfile.DailySolved >= 5 && string.CompareOrdinal(learningProfile.PracticeDate, learningProfile.LastPracticeStarDate) > 0)
        {
            learningProfile.Stars++; learningProfile.LastPracticeStarDate = learningProfile.PracticeDate;
            pilotJournal.Tokens++;
            Announce("DAILY SIGNAL COMPLETE", "Five questions solved · +1 practice star and +1 color credit.", Mint);
        }
        RefreshAchievements(); SavePilotJournal();
    }

    private void RecordExpeditionVictory()
    {
        if (night <= pilotJournal.HighestNight) return;
        pilotJournal.HighestNight = night; ActivityToday().Victories++;
        if (dawnCleanDefense) pilotJournal.CleanDefenses++;
        RefreshAchievements(); SavePilotJournal();
    }

    private Color LiveryEnergy(int index) => Hex(StarfallCosmetics.Energy[Mathf.Clamp(index, 0, StarfallCosmetics.Count - 1)]);
    private Color LiveryArmor(int index) => Hex(StarfallCosmetics.Armor[Mathf.Clamp(index, 0, StarfallCosmetics.Count - 1)]);

    private void ApplyLivery()
    {
        if (cachedLivery == learningProfile.Accent) return;
        foreach (Texture2D old in liveryTextures.Values) Destroy(old);
        liveryTextures.Clear(); cachedLivery = learningProfile.Accent;
        if (cachedLivery == 0) return;
        foreach (Texture2D source in new[] { pilotTexture, carbineTexture, turretHeadTexture, turretMountTexture, shieldTexture, droneArt, botArt })
        {
            if (source == null || !source.isReadable || liveryTextures.ContainsKey(source)) continue;
            Color32[] colors = source.GetPixels32();
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = LiveryPixel(colors[i], source == pilotTexture, cachedLivery);
            }
            var variant = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = source.name + " livery" };
            variant.SetPixels32(colors); variant.Apply(false, true); liveryTextures.Add(source, variant);
        }
    }
    private Texture2D LiveryTexture(Texture2D original) => original != null && liveryTextures.TryGetValue(original, out Texture2D variant) ? variant : original;

    private void DrawCollection() => DrawPolishedCollection();

    private void EquipLivery(int index)
    {
        if (index < 0 || index >= StarfallCosmetics.Count) return;
        if (!pilotJournal.Liveries.Contains(index))
        {
            if (pilotJournal.Tokens < LiveryPrices[index]) return;
            pilotJournal.Tokens -= LiveryPrices[index]; pilotJournal.Liveries.Add(index);
        }
        learningProfile.Accent = index; ApplyLivery(); SaveLearningProfile(); SavePilotJournal();
        PlayRelaySound(2);
        Announce("LIVERY EQUIPPED", LiveryNames[index] + " · your astronaut and installed equipment now match.", LiveryEnergy(index));
    }

    private void DrawPilotProfile()
    {
        HubHeading("Statistics", "Your adventure and learning.");
        string[] tabs = { "Learning", "Adventure", "My class" };
        for (int i = 0; i < 3; i++) if (MenuTab(tabs[i], new Rect(334 + i * 292, 240, 282, 58), profileTab == i)) profileTab = i;
        if (profileTab == 0) { DrawLearningDashboard(); return; }
        if (profileTab == 2) { DrawCloudComparison(); return; }
        UiCard(new Rect(334, 320, 282, 390), Cyan); DrawMenuPilot(new Vector2(475, 473), 0, 2.1f);
        UiText(learningProfile.CadetName, new Rect(354, 594, 242, 45), 28, true, UiInk, TextAnchor.MiddleCenter);
        UiText("Pilot level " + astronautLevel, new Rect(354, 646, 242, 40), 23, false, UiBlue, TextAnchor.MiddleCenter);
        string[] labels = { "Nights protected", "Meteors stopped", "Tools upgraded", "Blast bursts", "Maths points", "Maths solved" };
        int[] values = { pilotJournal.HighestNight, lifetimeKills, InstalledRanks, pilotJournal.Pulses, learningProfile.LearningXp, TotalLearning(true) + TotalLearning(false) };
        for (int i = 0; i < 6; i++)
        {
            Rect card = new Rect(634 + i % 2 * 292, 320 + i / 2 * 134, 274, 122); UiCard(card, i % 2 == 0 ? Cyan : Gold);
            UiText(values[i].ToString(), new Rect(card.x + 20, card.y + 9, 234, 55), 38, true, UiBlue);
            UiText(labels[i], new Rect(card.x + 20, card.y + 73, 234, 36), 22);
        }
        UiText("Next goal: " + NextMilestone(), new Rect(334, 726, 866, 52), 22, false, UiMuted);
    }

    private void DrawActivityProfile()
    {
        UiText("This week", new Rect(334, 315, 866, 40), 27, true);
        for (int i = 0; i < 7; i++)
        {
            DateTime date = DateTime.Now.Date.AddDays(i - 6); string key = date.ToString("yyyy-MM-dd");
            StarfallActivityDay day = pilotJournal.Days.Find(d => d.Date == key);
            Rect row = new Rect(334, 375 + i * 51, 866, 44); UiRound(row, i % 2 == 0 ? Hex("e4edfa") : UiPaper, 8);
            UiText(date.ToString("MMM d", System.Globalization.CultureInfo.InvariantCulture), new Rect(352, row.y, 132, row.height), 21, true);
            UiText(day == null ? "A new day to discover" : day.Solved + " puzzles · " + day.Builds + " upgrades · " + day.Kills + " meteors · " + day.Victories + " nights", new Rect(504, row.y, 676, row.height), 20, false, UiMuted);
        }
        UiText("Little steps add up. Keep exploring at your own pace.", new Rect(334, 741, 866, 35), 21, false, UiMuted);
    }

    private string ThreatName(int wave) => wave % 3 == 0 ? "Big meteors" : wave % 3 == 2 ? "Fast meteors" : "Meteors";

    private string ThreatDescription(int wave) => wave % 3 == 0 ? "Tough meteors travel with a big meteor. Towers help stop the group."
        : wave % 3 == 2 ? "Fast meteors arrive in groups. Blaster and Drones help cover the edges."
        : "Small meteors arrive in big groups. Towers and Blaster help clear them.";

    private void DrawExpeditionDirector() => DrawGuideTaskHint();

    private void UpdatePilotAbilities(Vector2 direction, float dt)
    {
        dashCooldown = Mathf.Max(0, dashCooldown - dt);
        if (direction.sqrMagnitude > 0) lastMoveDirection = direction;
        bool boost = touchBoost || Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
        touchBoost = false;
        if (boost && dashCooldown <= 0)
        {
            Vector2 before = pilotPosition;
            MoveOnDeck((direction.sqrMagnitude > 0 ? direction : lastMoveDirection) * 135);
            for (int i = 0; i < 6; i++) SpawnBurst(Vector2.Lerp(before, pilotPosition, i / 5f), LearnerAccent, 2);
            if (phase == Phase.Day)
                foreach (SalvageNode node in nodes)
                    if (!node.Collected && DistanceToSegmentSq(node.Position, before, pilotPosition) < 50 * 50) CollectSalvage(node);
            dashCooldown = 2.5f;
        }
    }

    private void UpdateFieldCombat(float dt)
    {
        pulseCharge = Mathf.Min(100, pulseCharge + dt * 1.5f);
        pulseAge += dt;
        bool nova = touchNova || Input.GetKeyDown(KeyCode.Space); touchNova = false;
        if (nova && pulseCharge >= 100)
        {
            pulseOrigin = pilotPosition; pulseAge = 0; pulseCharge = 0; pilotJournal.Pulses++;
            PlayRelaySound(3);
            int damage = 6 + RifleDamage * 2 + TurretDamage;
            foreach (Meteor enemy in meteors)
                if (enemy.Alive && (enemy.Position - pulseOrigin).sqrMagnitude <= 420 * 420) DamageMeteor(enemy, damage, LearnerAccent);
            SpawnBurst(pilotPosition, LearnerAccent, 35); Announce("BLAST!", "Collect energy to charge another Blast.", LearnerAccent);
            RefreshAchievements(); SavePilotJournal();
        }
        for (int i = capacitors.Count - 1; i >= 0; i--)
        {
            CapacitorDrop drop = capacitors[i]; drop.Age += dt;
            float distance = Vector2.Distance(drop.Position, pilotPosition);
            if (distance < 120) drop.Position = Vector2.MoveTowards(drop.Position, pilotPosition, dt * 340);
            if (distance < 28)
            { pulseCharge = Mathf.Min(100, pulseCharge + 12); SpawnBurst(drop.Position, LearnerAccent, 3); capacitors.RemoveAt(i); }
            else if (drop.Age > 45) capacitors.RemoveAt(i);
        }
    }

    private void DropFieldCapacitor(Meteor enemy)
    {
        pulseCharge = Mathf.Min(100, pulseCharge + .32f);
        if (lifetimeKills % 12 != 0 || capacitors.Count >= 24) return;
        Vector2 position = NearestDeckPosition(enemy.Position);
        capacitors.Add(new CapacitorDrop { Position = position });
    }

    private void DrawFieldCombat()
    {
        foreach (CapacitorDrop drop in capacitors)
        {
            DrawPixelEllipse(drop.Position + new Vector2(0, 8), 9, 3, Ink);
            DrawEnergyCell(drop.Position, 20, LearnerAccent);
        }
        if (pulseAge < .6f)
        {
            float radius = reducedMotion ? 420 : Mathf.Lerp(20, 420, pulseAge / .6f);
            for (int i = 0; i < 72; i++)
            {
                float angle = i * Mathf.PI * 2 / 72;
                Vector2 at = pulseOrigin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                Fill(at.x, at.y, 6, 4, new Color(LearnerAccent.r, LearnerAccent.g, LearnerAccent.b, 1 - pulseAge / .6f));
            }
        }
        if (cachedLivery > 0)
            for (int i = 0; i < 8; i++)
            {
                Vector2 at = corePosition + new Vector2(Mathf.Cos(i * Mathf.PI / 4) * 128, Mathf.Sin(i * Mathf.PI / 4) * 110);
                Fill(at.x - 4, at.y - 2, 8, 4, LearnerAccent);
            }
    }


    private string CalibrationPurpose => question.Operation == '-' ? "ISOLATE THE REMAINING CELLS" : question.Operation == '*' ? "WIRE EQUAL MODULE BANKS" :
        question.Operation == '/' ? "BALANCE THE EMITTERS" : "COMBINE THE POWER BANKS";
    private string CalibrationContext => question.Operation == '-' ? "The " + UpgradeName(selectedUpgrade) + " routes " + question.First + " cells. Disconnect " + question.Second + ". Set the remaining load." :
        question.Operation == '*' ? "The " + UpgradeName(selectedUpgrade) + " has " + question.First + " module banks with " + question.Second + " cells each. Set the total load." :
        question.Operation == '/' ? "Share " + question.First + " control cells across " + question.Second + " equal emitters. Set cells per emitter." :
        "Combine banks of " + question.First + " and " + question.Second + " control cells to calibrate the " + UpgradeName(selectedUpgrade) + ".";

    private void DrawNumberPad() => DrawFriendlyNumberPad();

    private void DrawBuildRangePreview()
    {
        if (phase != Phase.Day || pauseOpen || buildPanelOpen || learningPanelOpen) return;
        float distance = Vector2.Distance(pilotPosition, NextUpgradePosition(Upgrade.IonTurret));
        if (distance >= 125 || turretCount >= 4) return;
        Vector2 at = NextUpgradePosition(Upgrade.IonTurret);
        for (int i = 0; i < 80; i++)
        {
            float angle = i * Mathf.PI * 2 / 80;
            Vector2 mark = at + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * 780;
            Vector2 screen = WorldToScreen(mark);
            if (screen.y < 90 || screen.y > 710 || screen.x < 0 || screen.x > 1280) continue;
            Fill(mark.x, mark.y, 5, 3, new Color(Cyan.r, Cyan.g, Cyan.b, .5f));
        }
        DrawBeam(at, corePosition, 2, new Color(Cyan.r, Cyan.g, Cyan.b, .3f));
        TextCentered("CANNON · 780 RANGE · SPLASH", at.x - 140, at.y - 80, 280, 24, new GUIStyle(smallStyle) { normal = { textColor = Cyan } });
    }

    private void InitializeRelayAudio()
    {
        relayAudio = gameObject.AddComponent<AudioSource>();
        gameObject.AddComponent<AudioListener>();
        relayAudio.playOnAwake = false; relayAudio.spatialBlend = 0;
        pilotJournal.SoundVolume = Mathf.Clamp01(pilotJournal.SoundVolume);
        relayAudio.volume = pilotJournal.SoundVolume;
        const int sampleRate = 22050;
        var random = new System.Random(31415);
        for (int kind = 0; kind < 5; kind++)
        {
            float duration = kind == 4 ? .065f : kind == 1 ? .065f : kind == 0 ? .2f : .35f;
            float[] samples = new float[Mathf.CeilToInt(sampleRate * duration)];
            float angle = 0;
            for (int i = 0; i < samples.Length; i++)
            {
                float time = i / (float)sampleRate, t = time / duration;
                float frequency = kind == 4 ? Mathf.Lerp(740, 520, t) : kind == 1 ? Mathf.Lerp(1100, 230, t) : kind == 3 ? Mathf.Lerp(150, 38, t) :
                    (kind == 2 ? 440 : 880) * (t < .33f ? 1 : t < .66f ? 1.25f : 1.5f);
                angle += frequency * Mathf.PI * 2 / sampleRate;
                float tone = Mathf.Sin(angle);
                if (kind == 3) tone = tone * .65f + ((float)random.NextDouble() * 2 - 1) * .35f;
                float envelope = Mathf.Min(1, time * 250) * Mathf.Pow(1 - t, kind == 1 ? 2 : 1.4f);
                samples[i] = tone * envelope * .24f;
            }
            AudioClip clip = AudioClip.Create("Relay effect " + kind, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0); relaySounds.Add(clip);
        }
    }

    private void PlayRelaySound(int kind, float volume = 1)
    {
        if (relayAudio == null || kind < 0 || kind >= relaySounds.Count) return;
        relayAudio.PlayOneShot(relaySounds[kind], volume);
    }

    private void PlayShotSound(float volume)
    {
        if (Time.unscaledTime < nextShotSound) return;
        nextShotSound = Time.unscaledTime + .09f;
        PlayRelaySound(1, volume);
    }
}
