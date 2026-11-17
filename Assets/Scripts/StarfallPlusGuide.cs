using System;
using UnityEngine;

public sealed partial class StarfallPlusGame
{
    private bool guideOpen, guideIsTutorial;
    private string[] guidePages = Array.Empty<string>();
    private int guidePage, guideSalvageBaseline, guideBuildBaseline, guideStartNight;
    private Vector2 guideMoveOrigin;
    private Action guideFinished;

    private void InitializeGuide()
    {
        learningProfile.GuideStep = Mathf.Clamp(learningProfile.GuideStep, 0, 9);
        guideMoveOrigin = pilotPosition;
        guideSalvageBaseline = pilotJournal.Salvage;
        guideBuildBaseline = InstalledRanks;
        guideStartNight = night;
    }

    private void OpenBot(string[] pages, Action finished = null, bool tutorial = false)
    {
        guidePages = pages; guidePage = 0; guideFinished = finished;
        guideIsTutorial = tutorial; guideOpen = true;
        touchMove = Vector2.zero; touchBoost = touchNova = false; pilotWalking = false;
        collectionDrag = collectionDragged = false;
    }

    private void FinishBot()
    {
        Action callback = guideFinished;
        guideOpen = false; guideFinished = null; guideIsTutorial = false;
        callback?.Invoke();
    }

    private void GuideAdvance(int step)
    {
        learningProfile.GuideStep = step;
        if (step == 1) guideMoveOrigin = pilotPosition;
        if (step == 2) guideSalvageBaseline = pilotJournal.Salvage;
        if (step == 5) guideBuildBaseline = InstalledRanks;
        if (step == 7) guideStartNight = night;
        if (step >= 9) learningProfile.GuideVersion = 1;
        SaveLearningProfile();
    }

    private void TutorialBot(int next, params string[] pages) => OpenBot(pages, () => GuideAdvance(next), true);

    private bool UpdateGuide()
    {
        if (guideOpen)
        {
            if (Input.GetKeyDown(KeyCode.Return)) { if (guidePage + 1 < guidePages.Length) guidePage++; else FinishBot(); }
            // Closing a tutorial explanation keeps its task; skipping the whole tutorial is explicit.
            else if (Input.GetKeyDown(KeyCode.Escape)) FinishBot();
            return true;
        }
        if (learningProfile.GuideVersion >= 1 || studySession || menuOpen || pauseOpen) return false;
        switch (learningProfile.GuideStep)
        {
            case 0:
                TutorialBot(1, "Hi! I'm Bot, your floating guide. Tap me whenever you need help with your ship, tools or maths. You stay in control of the astronaut.",
                    "Drag the Move stick to walk. Your Blaster aims and shoots by itself. Try walking a little now."); break;
            case 1:
                if (Vector2.Distance(pilotPosition, guideMoveOrigin) >= 90)
                    TutorialBot(2, "Nice! Metal and Crystal are the two resources. Walk over a pile to collect it. The counters at the top show what you have.",
                        "The small signs stay with the objects. Metal is blue. Crystal is yellow. Collect either one to try it.");
                break;
            case 2:
                if (pilotJournal.Salvage > guideSalvageBaseline || nodes.TrueForAll(n => n.Collected))
                    TutorialBot(3, "Now open Build & upgrade. Every tool shows its job, level and cost. You decide what to spend your Metal and Crystal on.",
                        "Tower shoots meteors. Shield absorbs damage. Collector gives more resources. Blaster, Drones and Repair help in different ways.");
                break;
            case 3:
                if (buildPanelOpen) TutorialBot(4, "Compare the cost with your counters. Start with a Tower if you do not have one. You can add a Tower or make installed Towers stronger.",
                    "Choose Solve & upgrade when you can afford it. Resources are only spent after you finish its maths puzzle.");
                break;
            case 4:
                if (phase == Phase.Quiz) TutorialBot(5, "This is an energy puzzle. Read the sign, use the cells or groups, then enter your answer. There is no timer.",
                    "A hint or a worked example can help. Your ship waits safely while you learn. Finish this puzzle to power up the tool.");
                break;
            case 5:
                if (phase == Phase.UpgradeReveal || InstalledRanks > guideBuildBaseline)
                    TutorialBot(6, "Your tool is ready! Now look at Health and Shield. Shield takes damage first and recharges before every night.",
                        "Health damage stays between rounds. Dawn restores only 10 Health. Upgrading Shield gives more protection, but does not heal Health.");
                break;
            case 6:
                if (raidBriefOpen) TutorialBot(7, "Each night brings more meteors. This guide compares your tools with the next shower. Upgrading before it gets too hard is your choice.",
                    "Day has no time limit. Check your resources, improve a tool if you can, then choose Start the shower when you are ready.");
                break;
            case 7:
                if (phase == Phase.Night) TutorialBot(8, "Here they come! Move with the stick. Your Blaster, Towers and Drones shoot automatically. Boost helps you move quickly.",
                    "Blast clears nearby meteors when its energy reaches 100%. Energy pickups recharge Blast. Protect the ship until the shower ends.");
                break;
            case 8:
                if (phase == Phase.Dawn || night > guideStartNight)
                    TutorialBot(9, "You made it! Collect the supplies. Your tools and learning stay with you. Health gains only 10; Shield recharges before the next night.",
                        "The next upgrades cost more, and later lessons use new maths. Choose what to buy rather than spending everything at once.",
                        "You can practise in Learn maths, see your progress and earn colors. My tutorial is finished. Tap the floating Bot any time you need me.");
                break;
        }
        return guideOpen;
    }

    private void OpenResourceHelp(Upgrade kind)
    {
        UpgradeCost(kind, out int metal, out int gems);
        OpenBot(new[] { UpgradeName(kind) + " costs " + metal + " Metal and " + gems + " Crystal. You have " + alloy + " Metal and " + crystal + " Crystal.",
            "Move to the piles and walk over them to collect resources. Collector increases what each pile gives. Night rewards bring more supplies when today's piles are gone." });
    }

    private void OpenContextHelp()
    {
        if (guideOpen) return;
        if (menuOpen && phase != Phase.Quiz)
        {
            if (hubPage == HubPage.Account) OpenBot(new[] { "A parent sets up the account using a username and password or Google. Then choose a nickname for your astronaut. Use a parent email for confirmation and password recovery.", "Your parent chooses who can see your progress and whether your nickname appears in private class comparisons. You can also play on this device without an account." });
            else if (hubPage == HubPage.Difficulty) OpenBot(new[] { "Choose Easy, Medium or Challenge. The examples show the kinds of numbers you will see. You can change your choice any time, and there is no timer." });
            else if (hubPage == HubPage.Galaxy) OpenBot(new[] { "Scroll to choose any of the fourteen maths topics. Make ten finds a missing part that makes exactly ten. Make zero takes a whole amount away from itself.", "Look at the short example, then choose a difficulty before you start." });
            else if (hubPage == HubPage.Achievements)
                OpenBot(collectionTab == 0 ? new[] { "These goals are progression tracks. Scroll to see them all. When a goal is ready, collect its credits. That same card then shows your next goal.", "For example: survive 1 night, collect the reward, then work toward 5 nights. Your progress and claimed rewards are saved." }
                    : new[] { "Scroll to see all eight colors. Earn color credits from goals and daily practice. Unlock a color once, then wear it whenever you like. Your astronaut, tools and lights will match." });
            else if (hubPage == HubPage.More) OpenBot(new[] { "Collection holds Unlockables, Statistics and Tools. Unlockables has your goals and colors. Statistics tracks your adventure and maths. Tools explains what each upgrade does." });
            else if (hubPage == HubPage.Armory) OpenBot(new[] { "Tower and Blaster shoot meteors. Shield protects Health. Collector gives more resources. Drones help attack, and Repair slowly heals during a night.", "Choose upgrades from Build & upgrade on your ship. This page explains their jobs." });
            else if (hubPage == HubPage.Progress) OpenBot(new[] { "Statistics shows your adventure, learning and activity. The tabs let you see what you have achieved and which maths to revisit." });
            else if (hubPage == HubPage.Settings) OpenBot(new[] { "Choose your name, sound, answer style and calmer motion here. Calmer motion also keeps me still so I am easier to tap." });
            else OpenBot(new[] { "Play returns to the ship. Learn maths has lessons and practice. Collection holds Unlockables, Statistics and Tools.", "I am Bot. I float nearby on every screen. Tap me when you need an explanation; I pause when you point at me." });
            return;
        }
        if (phase == Phase.Quiz)
        {
            OpenBot(new[] { "Read the maths sign first. " + StarfallCurriculum.Teachings[question.Lesson],
                "Use the answer pad or choices. For help with this exact puzzle, choose Show hint or Work together. There is no timer, and the ship is paused." }); return;
        }
        if (buildPanelOpen) { OpenResourceHelp(selectedUpgrade); return; }
        if (phase == Phase.UpgradeReveal) { OpenBot(new[] { UpgradeName(selectedUpgrade) + " has been upgraded. The new power is shown here. Choose Back to my spaceship to continue." }); return; }
        if (raidBriefOpen || phase == Phase.Warning)
        { OpenBot(new[] { "More meteors need stronger tools. Use the preparation guide to decide whether to upgrade before the next night. Shield refills when the night is launched; Health does not." }); return; }
        if (phase == Phase.Night)
        { OpenBot(new[] { "Move with the stick. Your weapons fire automatically. Tap Boost to move quickly and Blast when it reaches 100%.",
            "Shield absorbs hits first. Once it is empty, meteors damage Health. Repair can slowly heal Health if you bought it. Energy pickups recharge Blast, not Health." }); return; }
        if (phase == Phase.Dawn)
        { OpenBot(new[] { "Collect these supplies once to start the next day. Health gains only 10. Tools, resources and learning continue into the next round." }); return; }
        if (phase == Phase.Defeat)
        { OpenBot(new[] { "You can prepare and try this night again. Your tools and completed maths are kept. The retry starts with the Health you had before this shower, not a free full heal.",
            "A stronger Shield can absorb more hits next time. You can also improve Tower, Blaster or Drones to stop meteors sooner." }); return; }
        OpenBot(new[] { "You move the astronaut yourself with the stick. Walk over Metal and Crystal to collect them. Tap a building's sign or Build & upgrade to inspect tools.",
            "Tower shoots. Shield protects. Collector gathers more. Blaster and Drones attack. Repair heals Health during a night.",
            "Health carries over with only 10 restored at dawn. Shield recharges before each night. Spend resources on the protection and power you need next." });
    }

    private void DrawBotPortrait(Rect box)
    {
        float bob = reducedMotion ? 0 : Mathf.Round(Mathf.Sin(corePulse * 1.8f) * 2);
        DrawTrimmedSprite(botArt, botArtUv, box.center + new Vector2(0, bob), box.height, Color.white);
    }

    private void DrawGuideTaskHint()
    {
        if (!FieldControlsAvailable || learningProfile.GuideVersion >= 1) return;
        string[] tasks = { "Meet Bot", "Try moving", "Collect a resource", "Open Build & upgrade", "Choose an upgrade", "Finish the puzzle", "Prepare for night", "Start the shower", "Protect the ship", "Tutorial complete" };
        UiCard(new Rect(24, 112, 280, 65), Cyan);
        UiText(tasks[Mathf.Clamp(learningProfile.GuideStep, 0, 9)], new Rect(40, 121, 248, 47), 22, true);
    }

    private void DrawGuideDialogue()
    {
        if (!guideOpen || guidePages.Length == 0) return;
        OverlayShade(.52f);
        Rect panel = new Rect(64, 404, 1152, 340);
        PixelPanel(panel, Cyan, false, true);
        PixelPanel(new Rect(84, 428, 195, 290), Gold, false, true);
        DrawBotPortrait(new Rect(100, 466, 163, 206));
        UiText("Bot", new Rect(306, 425, 800, 43), 30, true, Gold);
        UiText(guidePages[guidePage], new Rect(306, 481, 874, 160), 26, false, Hex("f4f8ff"));
        if (MenuButton("Back", new Rect(306, 664, 148, 60), Cyan, guidePage > 0)) guidePage--;
        bool math = phase == Phase.Quiz && !questionExplained && !questionResolved && !questionIntro;
        if (math)
        {
            if (MenuButton("Show hint", new Rect(470, 664, 228, 60), Cyan)) { ShowTeachingHint(); FinishBot(); }
            if (MenuButton("Work together", new Rect(714, 664, 228, 60), Gold)) { ExplainQuestion(); FinishBot(); }
        }
        else
        {
            UiText((guidePage + 1) + " / " + guidePages.Length, new Rect(470, 664, 320, 60), 20, false, Color.white, TextAnchor.MiddleCenter);
            if (!guideIsTutorial && MenuButton("Close", new Rect(804, 664, 138, 60), Cyan)) FinishBot();
        }
        if (MenuButton(guidePage + 1 < guidePages.Length ? "Next" : "Continue", new Rect(958, 664, 222, 60), Cyan, true, true))
        { if (guidePage + 1 < guidePages.Length) guidePage++; else FinishBot(); }
        if (guideIsTutorial && MenuButton("End tutorial", new Rect(1010, 346, 206, 46), Cyan)) { guideFinished = null; guideOpen = false; guideIsTutorial = false; GuideAdvance(9); }
    }
}
