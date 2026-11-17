using System;
using UnityEngine;

public sealed partial class StarfallPlusGame
{
    private enum HubPage { Home, Galaxy, Armory, Progress, Achievements, Settings, Lesson, Results, More, Difficulty, Account }
    private bool menuOpen, studySession, studyResult, reducedMotion;
    private HubPage hubPage;
    private Phase studyReturnPhase;
    private int selectedLesson, practiceSkill, studyIndex, studyIndependent, studySupported, studyXp;
    private string typedAnswer = "";
    private Font academyFont;
    private Vector2 lessonScroll;
    private GUIStyle hubTitle, hubCopy, hubLabel;
    private Color LearnerAccent => LiveryEnergy(learningProfile.Accent);

    private void InitializeAcademyMenu()
    {
        learningProfile.Validate();
        reducedMotion = learningProfile.ReducedMotion;
        academyFont = Resources.Load<Font>("Fonts/Nunito");
        academyBodyFont = Resources.Load<Font>("Fonts/NunitoRegular");
        if (academyBodyFont == null) academyBodyFont = academyFont;
        selectedLesson = learningProfile.ActiveLesson;
        menuOpen = true;
        hubPage = HubPage.Home;
        RefreshPracticeDate();
    }

    private void OpenAcademyMenu(HubPage page)
    {
        if (phase == Phase.Quiz)
        {
            if (studySession) EndStudySession(false);
            else { phase = Phase.Day; questionResolved = false; }
        }
        SaveExpedition();
        menuOpen = true; NavigateHub(page);
        selectedLesson = learningProfile.ActiveLesson;
        pauseOpen = buildPanelOpen = learningPanelOpen = false;
        pilotWalking = false;
    }

    private void ResumeExpedition()
    {
        menuOpen = false; pauseOpen = false;
        learningProfile.CadetName = learningProfile.CadetName.Trim(); learningProfile.Validate();
        SaveLearningProfile();
        SavePilotJournal();
    }

    private bool UpdateAcademyNavigation()
    {
        if (menuOpen && hubPage == HubPage.Account)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (nicknamePrompt) nicknamePrompt = false;
                else if (learningProfile.WelcomeComplete) NavigateHub(HubPage.Settings);
                else nicknamePrompt = true;
                return true;
            }
            if (Input.GetKeyDown(KeyCode.Return) && !guideOpen && !accountBusy)
            {
                if (nicknamePrompt) SaveNickname();
                else if (!OnlineConfigured) nicknamePrompt = true;
                else if (parentAccepted && !SignedIn) StartCoroutine(Authenticate());
            }
            return true;
        }
        // Text-entry keys belong to IMGUI while its field owns keyboard focus.
        if (menuOpen && (hubPage == HubPage.Account || hubPage == HubPage.Settings && menuNameEditing)) return true;
        if (Input.GetKeyDown(KeyCode.M) && !raidBriefOpen)
        {
            if (studySession) EndStudySession(false);
            else { if (menuOpen && phase != Phase.Quiz) ResumeExpedition(); else OpenAcademyMenu(HubPage.Home); }
            return true;
        }
        if (phase == Phase.Quiz && studySession && Input.GetKeyDown(KeyCode.Escape))
        { EndStudySession(false); return true; }
        if (!menuOpen || phase == Phase.Quiz) return false;
        if (Input.GetKeyDown(KeyCode.Escape)) { if (hubPage == HubPage.Home) ResumeExpedition(); else NavigateHub(MenuParent); return true; }
        if (hubPage == HubPage.Home || hubPage == HubPage.More)
        {
            int direction = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S) ? 1 : Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W) ? -1 : 0;
            if (direction != 0)
            {
                if (hubPage == HubPage.Home) homeChoice = (homeChoice + direction + 4) % 4;
                else moreChoice = (moreChoice + direction + 3) % 3;
                menuNavPending = true;
            }
            if (Input.GetKeyDown(KeyCode.Return))
            {
                if (hubPage == HubPage.Home) ChooseHomeAction();
                else NavigateHub(moreChoice == 0 ? HubPage.Achievements : moreChoice == 1 ? HubPage.Progress : HubPage.Armory);
            }
            return true;
        }
        if (hubPage == HubPage.Settings)
        {
            if ((Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand)) && Input.GetKeyDown(KeyCode.S))
            { SaveMenuPreferences(); return true; }
            if (Input.GetKeyDown(KeyCode.R)) { reducedMotion = !reducedMotion; learningProfile.ReducedMotion = reducedMotion; SaveLearningProfile(); }
            if (Input.GetKeyDown(KeyCode.Q)) { learningProfile.TypedAnswers = !learningProfile.TypedAnswers; SaveLearningProfile(); }
            if (Input.GetKeyDown(KeyCode.F)) Screen.fullScreen = !Screen.fullScreen;
            if (Input.GetKeyDown(KeyCode.Alpha1)) pilotJournal.MusicVolume = ToggleMenuVolume(pilotJournal.MusicVolume, ref menuMusicRestore);
            if (Input.GetKeyDown(KeyCode.Alpha2)) pilotJournal.SoundVolume = ToggleMenuVolume(pilotJournal.SoundVolume, ref menuEffectsRestore);
        }
        if (hubPage == HubPage.Galaxy)
        {
            if (Input.GetKeyDown(KeyCode.RightArrow)) selectedLesson = (selectedLesson + 1) % StarfallCurriculum.Names.Length;
            if (Input.GetKeyDown(KeyCode.LeftArrow)) selectedLesson = (selectedLesson + StarfallCurriculum.Names.Length - 1) % StarfallCurriculum.Names.Length;
            if (Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.UpArrow)) selectedLesson = (selectedLesson + 3) % 6;
            if (Input.GetKeyDown(KeyCode.Return)) { NavigateHub(HubPage.Lesson); return true; }
        }
        if (hubPage == HubPage.Lesson && Input.GetKeyDown(KeyCode.Return)) { NavigateHub(HubPage.Difficulty); return true; }
        if (hubPage == HubPage.Difficulty && Input.GetKeyDown(KeyCode.Return)) { StartStudySession(selectedLesson); return true; }
        if (hubPage == HubPage.Progress && Input.GetKeyDown(KeyCode.Tab)) profileTab = (profileTab + 1) % 3;
        if (hubPage == HubPage.Achievements)
        {
            UpdateCollectionNavigation();
        }
        if (hubPage == HubPage.Results && Input.GetKeyDown(KeyCode.Return)) { ResumeExpedition(); return true; }
        return true;
    }

    private void DrawAcademyMenu()
    {
        if (menuClearFocus) { GUIUtility.keyboardControl = 0; GUI.FocusControl(""); menuClearFocus = false; }
        hubTitle = MenuType(30, Cream, true);
        hubCopy = MenuType(20, Hex("becddd"));
        hubLabel = MenuType(22, Cream, true);
        DrawRelayMenuShell();
        Matrix4x4 menuMatrix = GUI.matrix;
        if (hubPage != HubPage.Home) GUI.matrix *= Matrix4x4.Translate(new Vector3(-127, 0, 0));
        switch (hubPage)
        {
            case HubPage.Home: DrawHubHome(); break;
            case HubPage.More: DrawMoreMenu(); break;
            case HubPage.Galaxy: DrawGalaxyRoute(); break;
            case HubPage.Lesson: DrawLessonBrief(); break;
            case HubPage.Armory: DrawDefenderGuide(); break;
            case HubPage.Progress: DrawLearningProgress(); break;
            case HubPage.Achievements: DrawAchievements(); break;
            case HubPage.Settings: DrawLearningSettings(); break;
            case HubPage.Results: DrawStudyResults(); break;
            case HubPage.Difficulty: DrawDifficultyChoice(); break;
            case HubPage.Account: DrawAccountScreen(); break;
        }
        GUI.matrix = menuMatrix;
        menuNameEditing = hubPage == HubPage.Settings && GUI.GetNameOfFocusedControl() == "CadetName";
    }

    private void HubHeading(string title, string description)
    {
        UiText(title, new Rect(334, 135, 866, 45), 32, true);
        UiText(description, new Rect(334, 184, 866, 42), 21, false, UiMuted);
    }

    private void DrawHubHome()
    {
        DrawRelayHome();
    }

    private void DrawGalaxyRoute()
    {
        HubHeading("Learn maths", "Choose a lesson.");
        DrawRouteNodes(240, false);
    }

    private void DrawRouteNodes(float top, bool compact)
    {
        Rect view = new Rect(334, top, 866, 510);
        lessonScroll = GUI.BeginScrollView(view, lessonScroll, new Rect(0, 0, 842, Mathf.Ceil(StarfallCurriculum.Names.Length / 3f) * 148), false, false, GUIStyle.none, GUIStyle.none);
        for (int i = 0; i < StarfallCurriculum.Names.Length; i++)
        {
            Rect card = new Rect(i % 3 * 282, i / 3 * 148, 270, 134);
            UiCard(card, Cyan, i == selectedLesson);
            DrawElementSprite(i % 4, new Vector2(card.x + 40, card.y + 38), 42);
            UiText(StarfallCurriculum.Names[i], new Rect(card.x + 75, card.y + 12, 183, 58), 23, true);
            UiText(StarfallCurriculum.Example(i), new Rect(card.x + 16, card.y + 79, 238, 40), 23, false, UiBlue, TextAnchor.MiddleCenter);
            if (GUI.Button(card, GUIContent.none, GUIStyle.none) && !ListScrollBlocked)
            { selectedLesson = i; PlayRelaySound(4, .5f); NavigateHub(HubPage.Lesson); }
        }
        GUI.EndScrollView(); DrawFriendlyScrollRail(view, ref lessonScroll, Mathf.Ceil(StarfallCurriculum.Names.Length / 3f) * 148);
        HandleListDrag(ref lessonScroll, new Rect(view.x - 127, view.y, view.width, view.height), Mathf.Ceil(StarfallCurriculum.Names.Length / 3f) * 148);
    }

    private void DrawLessonBrief()
    {
        int lesson = selectedLesson;
        HubHeading(StarfallCurriculum.Names[lesson], "Read the idea. Look at the example. Then have a go!");
        UiCard(new Rect(334, 240, 866, 182), Cyan);
        UiText("Think about it this way", new Rect(358, 252, 818, 38), 24, true, UiBlue);
        UiText(StarfallCurriculum.Teachings[lesson], new Rect(358, 298, 818, 105), 26);
        StarfallQuestion example = StarfallCurriculum.CreateSkill(lesson, 0, 271 + lesson);
        UiCard(new Rect(334, 440, 866, 160), Gold);
        UiText("An example", new Rect(358, 451, 818, 36), 24, true);
        UiText(StarfallCurriculum.Example(lesson), new Rect(358, 493, 818, 88), 48, true, UiBlue, TextAnchor.MiddleCenter);
        if (MenuButton("Choose difficulty", new Rect(334, 630, 415, 74), Cyan, true, true)) NavigateHub(HubPage.Difficulty);
        bool unlocked = lesson <= learningProfile.UnlockedLesson;
        if (MenuButton(unlocked ? "Use in my adventure" : "Space lesson locked", new Rect(767, 630, 433, 74), Gold, unlocked))
        { learningProfile.ActiveLesson = lesson; learningProfile.LessonSeen[lesson] = true; SaveLearningProfile(); ResumeExpedition(); }
        UiText(unlocked ? "Your astronaut and all your tools are kept." : "Practice is open. Protect your ship and solve upgrade puzzles to unlock this space lesson.", new Rect(334, 721, 866, 55), 20, false, UiMuted);
    }

    private void DrawDefenderGuide()
    {
        HubHeading("Tools", "What each tool does.");
        for (int i = 0; i < 6; i++)
        {
            Upgrade kind = (Upgrade)i; Rect card = new Rect(334 + i % 3 * 292, 240 + i / 3 * 228, 282, 210);
            UiCard(card, UpgradeColor(kind)); DrawMenuEquipmentArt(kind, new Vector2(card.x + 49, card.y + 48), 60);
            UiText(FriendlyUpgrade(kind), new Rect(card.x + 94, card.y + 14, 172, 68), 24, true);
            UiText(FriendlyBenefit(kind), new Rect(card.x + 18, card.y + 99, 246, 96), 20, false, UiMuted);
        }
    }

    private void DrawLearningProgress()
    {
        DrawPilotProfile();
    }

    private void DrawAchievements()
    {
        DrawCollection();
    }

    private void DrawLearningSettings() => DrawPolishedSettings();

    private void DrawMenuPilot(Vector2 position, float angle, float zoom = 1.25f)
    {
        Vector2 previousPosition = pilotPosition; float previousAngle = pilotAimAngle;
        int previousFacing = pilotFacing; bool previousWalk = pilotWalking; float previousKick = pilotWeaponKick, flash = pilotMuzzleFlash;
        Matrix4x4 matrix = GUI.matrix;
        GUI.matrix *= Matrix4x4.TRS(new Vector3(position.x, position.y, 0), Quaternion.identity, new Vector3(zoom, zoom, 1));
        pilotPosition = Vector2.zero; pilotAimAngle = angle; pilotWalking = false; pilotWeaponKick = pilotMuzzleFlash = 0;
        Vector2 direction = AimDirection(angle);
        pilotFacing = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? direction.x < 0 ? 2 : 3 : direction.y < 0 ? 1 : 0;
        DrawPilot();
        GUI.matrix = matrix; pilotPosition = previousPosition; pilotAimAngle = previousAngle; pilotFacing = previousFacing;
        pilotWalking = previousWalk; pilotWeaponKick = previousKick; pilotMuzzleFlash = flash;
    }

    private void RefreshPracticeDate()
    {
        string today = DateTime.Now.ToString("yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        if (learningProfile.PracticeDate != today) { learningProfile.PracticeDate = today; learningProfile.DailySolved = 0; }
    }

    private void StartStudySession(int lesson)
    {
        studyReturnPhase = phase;
        studySession = true; studyIndex = studyIndependent = studySupported = studyXp = 0;
        selectedLesson = lesson; learningProfile.LessonSeen[lesson] = true;
        NewStudyQuestion(); SaveLearningProfile();
    }

    private void NewStudyQuestion()
    {
        learningQuestion = StarfallCurriculum.NextPractice(learningProfile, selectedLesson, learningProfile.PracticeDifficulty, UnityEngine.Random.Range(0, int.MaxValue), previousFactKey);
        previousFactKey = learningQuestion.Key;
        AssignLearningQuestion(learningQuestion);
        ResetLearningQuestion(true); studyResult = false; typedAnswer = "";
        phase = Phase.Quiz; answerFeedback = "Take your time. Help and visual models are available.";
    }

    private void CompleteStudyQuestion(bool independent)
    {
        int xp = independent ? 10 : 4;
        studyXp += xp;
        if (independent) studyIndependent++; else studySupported++;
        studyIndex++; studyResult = true; visualSteps = VisualStepTarget; SaveLearningProfile();
    }

    private void ContinueStudy()
    {
        if (studyIndex < 5) NewStudyQuestion(); else EndStudySession(true);
    }

    private void EndStudySession(bool completed)
    {
        if (!studySession) return;
        phase = studyReturnPhase; studySession = false; studyResult = false;
        menuOpen = true; NavigateHub(completed ? HubPage.Results : HubPage.Galaxy);
        questionResolved = false; SaveLearningProfile(); SaveExpedition();
    }

    private void DrawStudyResults()
    {
        HubHeading("Nice work, " + learningProfile.CadetName + "!", "Five puzzles complete. Every step helps your maths grow.");
        UiCard(new Rect(334, 240, 866, 372), Gold);
        DrawClearIcon(5, new Vector2(1023, 283), 10, Hex("c99129"));
        UiText("+" + studyXp + " maths points", new Rect(364, 269, 570, 66), 38, true, UiBlue);
        UiText(studyIndependent + " worked out yourself", new Rect(364, 362, 570, 45), 27, true);
        UiText(studySupported + " learned together", new Rect(364, 420, 570, 45), 27, true);
        UiText("Today: " + Mathf.Min(5, learningProfile.DailySolved) + " of 5 puzzles · " + MenuCount(learningProfile.Stars, "learning star"), new Rect(364, 506, 806, 65), 24, false, UiMuted);
        if (MenuButton("Try more puzzles", new Rect(334, 650, 415, 76), Cyan)) NavigateHub(HubPage.Difficulty);
        if (MenuButton("Back to space", new Rect(767, 650, 433, 76), Gold, true, true)) ResumeExpedition();
        UiText("Learning with help counts too. Keep going at your own pace.", new Rect(334, 740, 866, 38), 20, false, UiMuted);
    }

    private int VisualStepTarget => question.Operation == '*' ? question.First : question.Operation == '/' ? question.Answer : question.Second;

    private void DismissQuestionIntro()
    {
        questionIntro = false; learningProfile.LessonSeen[question.Lesson] = true; SaveLearningProfile();
    }

    private void DrawQuestionIntroduction()
    {
        TeachingBackdrop("Let's learn it first", "A little practice can give your spaceship a lot of power.");
        UiCard(new Rect(32, 128, 790, 516), Cyan);
        UiText(StarfallCurriculum.Topics[question.Lesson], new Rect(64, 154, 726, 60), 32, true, UiBlue);
        UiText(StarfallCurriculum.Teachings[question.Lesson], new Rect(64, 234, 726, 142), 27);
        StarfallQuestion example = StarfallCurriculum.CreateSkill(question.Lesson, 0, 271 + question.Lesson);
        UiRound(new Rect(64, 402, 726, 206), Hex("eaf4fc"));
        UiText("Try thinking about this", new Rect(86, 417, 680, 41), 25, true);
        UiText(StarfallCurriculum.Example(question.Lesson), new Rect(86, 475, 680, 112), 48, true, UiBlue, TextAnchor.MiddleCenter);
        DrawMenuPilot(new Vector2(1030, 369), 0, 2.5f);
        UiText("I'll wait while you think!", new Rect(867, 515, 337, 64), 26, true, UiBlue, TextAnchor.MiddleCenter);
        if (MenuButton("I'm ready to try", new Rect(32, 684, 790, 80), Cyan, true, true)) DismissQuestionIntro();
        UiText("No timer. We can work together.", new Rect(866, 684, 338, 80), 22, false, UiMuted, TextAnchor.MiddleCenter);
    }

    private void DrawEqualGroupModel() => DrawFriendlyGroups(new Rect(56, 242, 612, 290));

    private void DrawStudyAnswerResult()
    {
        TeachingBackdrop("Good thinking!", "Question " + studyIndex + " of 5 complete. Every step is practice.");
        UiText(question.Text.Replace("?", question.Answer.ToString()), new Rect(32, 112, 660, 96), 64, true, UiBlue, TextAnchor.MiddleCenter);
        UiCard(new Rect(32, 224, 660, 388), Cyan);
        DrawFriendlyModel(new Rect(56, 242, 612, 290), false);
        UiText(question.Explanation, new Rect(56, 541, 612, 56), 22, true);
        UiCard(new Rect(720, 112, 528, 540), Gold);
        DrawClearIcon(5, new Vector2(919, 153), 9, Hex("d59a2d"));
        UiText("+" + (questionSupported ? 4 : 10) + " maths points", new Rect(748, 288, 472, 66), 35, true, UiBlue, TextAnchor.MiddleCenter);
        UiText(questionSupported ? "We worked it out together.\nThat is learning too!" : "You worked it out yourself!", new Rect(758, 381, 452, 100), 27, false, UiInk, TextAnchor.MiddleCenter);
        UiText("Your spaceship is waiting safely.", new Rect(752, 546, 464, 53), 22, false, UiMuted, TextAnchor.MiddleCenter);
        if (MenuButton(studyIndex < 5 ? "Next question" : "See my progress", new Rect(720, 686, 528, 80), Cyan, true, true)) ContinueStudy();
    }
}
