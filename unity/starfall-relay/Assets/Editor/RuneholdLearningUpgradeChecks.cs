#if UNITY_EDITOR
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;

internal static class RuneholdLearningUpgradeChecks
{
    private static void Require(bool yes, string why) { if (!yes) throw new Exception(why); }
    public static void Run()
    {
        try
        {
            for (int topic = 0; topic < 14; topic++) for (int level = 0; level < 3; level++) for (int seed = 0; seed < 500; seed++)
            {
                var q = StarfallCurriculum.CreateSkill(topic, level, seed);
                int result = q.Operation == '+' ? q.First + q.Second : q.Operation == '-' ? q.First - q.Second : q.Operation == '*' ? q.First * q.Second : q.First / q.Second;
                Require(result == q.Result && q.Answer >= 0, "Invalid arithmetic.");
                Require(q.Answer == (q.MissingNumber ? q.Second : q.Result), "Wrong missing part.");
                Require(q.Choices.Distinct().Count() == 4 && q.Choices.Count(x => x == q.Answer) == 1, "Invalid choices.");
                if (topic == 2 || topic == 6) Require(q.MissingNumber && q.Result == (topic == 2 ? 10 : 20), "Make target changed.");
                if (topic == 7) Require(q.Operation == '-' && q.First == q.Answer && q.Result == 0, "Make zero is not zero.");
                if (topic == 8) Require(q.First == q.Second, "Double is not a double.");
                if (topic == 9) Require(q.Second == q.First + 1, "Near double is incorrect.");
            }
            var profile = JsonUtility.FromJson<StarfallLearningProfile>("{\"Version\":1,\"Independent\":[4,3,2,1,0,0],\"Accent\":7}"); profile.Validate();
            Require(profile.Independent.Length == 14 && profile.Independent[0] == 4 && profile.Accent == 7, "Old learning progress changed.");
            using (var p = new RuneholdEconomyChecks.Probe())
            {
                p.Set("online", new StarfallOnlineConfig { url = "https://example.test", publishableKey = "public-test-key", emailReady = true });
                p.Set("parentAccepted", true); p.Set("accountUsername", "parent_1"); p.Set("accountEmail", "parent@example.test"); p.Set("accountPassword", "short");
                Require(((string)p.Call("AccountFormHint")).Contains("5/12"), "Short password has no actionable explanation.");
                p.Set("accountPassword", "test-only-long-password");
                Require((string)p.Call("AccountFormHint") == "", "Valid signup form is blocked.");
                p.Set("accountUsername", "parent with spaces");
                Require(((string)p.Call("AccountFormHint")).StartsWith("Username:"), "Invalid username is not explained.");
                p.Set("accountUsername", "parent_1"); p.Set("parentAccepted", false);
                Require(((string)p.Call("AccountFormHint")).StartsWith("Tick"), "Parent attestation requirement is not explained.");
                var invalid = new Vector2(float.NaN, float.NaN);
                var spawn = (Vector2)p.Call("NearestDeckPosition", invalid);
                Require((bool)p.Call("CanStandOnDeck", spawn), "Expanded map fallback is off the floor.");
                p.Set("pilotPosition", spawn); p.Call("MoveOnDeck", new Vector2(10000, 0));
                Require((bool)p.Call("CanStandOnDeck", (Vector2)p.Get("pilotPosition")), "Expanded map movement crossed an edge.");
                var deck = (Rect)typeof(RuneholdGame).GetProperty("DeckRect", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
                Require(deck.width == 4480 && deck.height == 2800, "The map is not twice as wide and tall.");
                p.Set("alloy", 20); p.Set("crystal", 20); p.Quiz("IonTurret");
                p.Call("ExplainQuestion"); p.Call("FinishLearningResolution");
                Require(p.Number("turretCount") == 0 && p.Number("alloy") == 20, "A demonstration granted an upgrade.");
                p.Set("visualSteps", 0); p.Call("TrySimilarQuestion");
                Require(!(bool)p.Get("questionExplained") && p.Phase == "Quiz" && p.Number("alloy") == 20, "Similar practice did not replace demonstration safely.");
                p.Call("SubmitLearningValue", ((StarfallQuestion)p.Get("learningQuestion")).Answer);
                Require(p.Number("turretCount") == 1, "Solving the replacement did not authorize the upgrade.");
            }
            Debug.Log("STARFALL LEARNING UPGRADE CHECKS PASSED: 21,000 generated questions, target bonds, choices, save expansion and demonstrate-then-retry purchase guard.");
            if (Application.isBatchMode) EditorApplication.Exit(0);
        }
        catch (Exception e) { Debug.LogException(e); if (Application.isBatchMode) EditorApplication.Exit(1); else throw; }
    }
}
#endif
