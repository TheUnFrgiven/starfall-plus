using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public sealed class StarfallAttempt
{
    public string Id = Guid.NewGuid().ToString("N"), At = DateTime.UtcNow.ToString("o"), Fact, Kind = "answer";
    public int Topic, Difficulty, Value;
    public bool Correct, FirstTry, Hint, Model, Demonstrated, Independent;
    public float Seconds;
}

public sealed partial class RuneholdGame
{
    private float questionStarted, demonstrationNextStep;
    private bool modelUsed, demonstrationPending;
    private Vector2 learningStatsScroll;
    private bool listDragging, listMoved;
    private float listStartY, listStartScroll, listBlockedUntil;
    private int listDragFrame = -1;
    private bool ListScrollBlocked => listMoved || Time.unscaledTime < listBlockedUntil;

    private void HandleListDrag(ref Vector2 scroll, Rect area, float contentHeight)
    {
        if (listDragFrame == Time.frameCount) return;
        listDragFrame = Time.frameCount;
        bool touch = Input.touchCount > 0;
        Vector2 at = InterfacePoint(touch ? Input.GetTouch(0).position : (Vector2)Input.mousePosition);
        bool began = touch ? Input.GetTouch(0).phase == TouchPhase.Began : Input.GetMouseButtonDown(0);
        bool held = touch ? Input.GetTouch(0).phase != TouchPhase.Ended && Input.GetTouch(0).phase != TouchPhase.Canceled : Input.GetMouseButton(0);
        if (began) { listDragging = area.Contains(at); listMoved = false; listStartY = at.y; listStartScroll = scroll.y; }
        if (listDragging && held && Mathf.Abs(at.y - listStartY) > 8)
        { listMoved = true; listBlockedUntil = Time.unscaledTime + .2f; scroll.y = Mathf.Clamp(listStartScroll + listStartY - at.y, 0, Mathf.Max(0, contentHeight - area.height)); }
        if (!held) { if (listMoved) listBlockedUntil = Time.unscaledTime + .2f; listDragging = listMoved = false; }
    }

    private void DrawFriendlyScrollRail(Rect view, ref Vector2 scroll, float height)
    {
        float max = Mathf.Max(0, height - view.height); if (max <= 0) return;
        Rect rail = new Rect(view.xMax - 10, view.y, 10, view.height);
        float thumb = Mathf.Max(50, view.height * view.height / height);
        var track = new GUIStyle(GUIStyle.none) { fixedWidth = 10 };
        var handle = new GUIStyle(GUIStyle.none) { fixedWidth = 10, fixedHeight = thumb };
        scroll.y = GUI.VerticalSlider(rail, scroll.y, 0, max, track, handle);
        UiRound(rail, Hex("dce6ee"), 3);
        UiRound(new Rect(rail.x, rail.y + (rail.height - thumb) * scroll.y / max, rail.width, thumb), Hex("839eb6"), 3);
    }

    private void DrawDifficultyChoice()
    {
        HubHeading("Where should we start?", StarfallCurriculum.Names[selectedLesson] + " · You can change this any time.");
        string[] names = { "Easy", "Medium", "Challenge" };
        string[] descriptions = { "Small steps. Visual help is always here.", "A wider range. Build on what you know.", "Larger numbers. Take all the time you need." };
        if (selectedLesson == 2 || selectedLesson == 6 || selectedLesson == 7) descriptions = new[] { "Small missing parts, with visual clues.", "Practise all the number partners.", "Recall the partners. Ask for a visual clue any time." };
        for (int i = 0; i < 3; i++)
        {
            Rect card = new Rect(334 + i * 292, 252, 282, 360); UiCard(card, Cyan, learningProfile.PracticeDifficulty == i);
            UiText(names[i], new Rect(card.x + 18, card.y + 20, 246, 46), 29, true);
            UiText(descriptions[i], new Rect(card.x + 18, card.y + 87, 246, 90), 23, false, UiMuted);
            UiText(StarfallCurriculum.Example(selectedLesson, i), new Rect(card.x + 18, card.y + 194, 246, 72), 30, true, UiBlue, TextAnchor.MiddleCenter);
            if (MenuButton(learningProfile.PracticeDifficulty == i ? "Selected" : "Choose", new Rect(card.x + 18, card.y + 284, 246, 56), Cyan, true, learningProfile.PracticeDifficulty == i))
            { learningProfile.PracticeDifficulty = i; SaveLearningProfile(); }
        }
        if (MenuButton("Start 5 questions", new Rect(334, 658, 866, 76), Cyan, true, true)) StartStudySession(selectedLesson);
    }

    private void RecordAttempt(int value, bool correct, string kind = "answer")
    {
        var record = new StarfallAttempt { Fact = question.FactKey, Topic = question.Lesson, Difficulty = learningProfile.PracticeDifficulty,
            Value = value, Correct = correct, FirstTry = questionAttempts == 0, Hint = questionHintVisible, Model = modelUsed,
            Demonstrated = demonstrationPending, Independent = correct && questionAttempts == 0 && !questionSupported,
            Seconds = Mathf.Round(Mathf.Clamp(Time.unscaledTime - questionStarted, 0, 3600) * 10) / 10, Kind = kind };
        learningProfile.Attempts.Add(record);
        if (learningProfile.Attempts.Count > 5000) learningProfile.Attempts.RemoveAt(0);
        QueueCloudAttempt(record);
    }

    private void UpdateWorkedExample()
    {
        if (!questionExplained || guideOpen || visualSteps >= VisualStepTarget || Time.unscaledTime < demonstrationNextStep) return;
        visualSteps = Mathf.Min(VisualStepTarget, visualSteps + Mathf.Max(1, Mathf.CeilToInt(VisualStepTarget / 12f)));
        demonstrationNextStep = Time.unscaledTime + .45f;
    }

    private void TrySimilarQuestion()
    {
        if (!questionExplained || visualSteps < VisualStepTarget) return;
        int lesson = question.Lesson; string old = question.FactKey;
        // Demonstration is useful evidence of support, never a completed answer or a purchase.
        RecordAttempt(0, false, "demonstration");
        var fact = learningProfile.Fact(old); fact.Supported++; fact.LastAnsweredDay = LocalDay; fact.ReviewOn = DateTime.Now.AddDays(1).ToString("yyyy-MM-dd");
        learningQuestion = StarfallCurriculum.NextPractice(learningProfile, lesson, learningProfile.PracticeDifficulty, UnityEngine.Random.Range(0, int.MaxValue), old);
        previousFactKey = learningQuestion.Key; AssignLearningQuestion(learningQuestion);
        bool practice = learningPractice; ResetLearningQuestion(practice);
        answerFeedback = "Your turn! Try this similar question. Help is here if you need it."; SaveLearningProfile();
    }

    private void DrawLearningDashboard()
    {
        var attempts = learningProfile.Attempts.Where(a => a.Kind == "answer").ToList();
        var first = attempts.Where(a => a.FirstTry).ToList();
        var timed = first.Where(a => a.Correct && a.Seconds > 0 && a.Seconds <= 180).Select(a => a.Seconds).OrderBy(x => x).ToList();
        int due = learningProfile.Facts.Count(f => StarfallCurriculum.ReviewDue(f));
        string[] values = { first.Count == 0 ? "—" : Mathf.RoundToInt(100f * first.Count(a => a.Correct) / first.Count) + "%", first.Count == 0 ? "—" : Mathf.RoundToInt(100f * first.Count(a => a.Hint || a.Model) / first.Count) + "%", timed.Count == 0 ? "—" : timed[timed.Count / 2].ToString("0.0") + "s", due.ToString() };
        string[] labels = { "Correct on first check", "Used a hint or model", "Typical correct time", "Facts due to revisit" };
        for (int i = 0; i < 4; i++)
        {
            Rect card = new Rect(334 + i * 219, 320, 209, 110); UiCard(card, Cyan);
            UiText(values[i], new Rect(card.x + 14, card.y + 6, 181, 48), 34, true, UiBlue);
            UiText(labels[i], new Rect(card.x + 14, card.y + 57, 181, 46), 18, false, UiMuted);
        }
        UiText("Recorded answers on this device · " + MenuCount(first.Count, "question") + " · Times over 3 minutes excluded", new Rect(334, 437, 866, 36), 18, false, UiMuted);
        Rect view = new Rect(334, 482, 866, 263);
        learningStatsScroll = GUI.BeginScrollView(view, learningStatsScroll, new Rect(0, 0, 842, StarfallCurriculum.Names.Length * 96), false, false, GUIStyle.none, GUIStyle.none);
        for (int i = 0; i < StarfallCurriculum.Names.Length; i++)
        {
            var topic = first.Where(a => a.Topic == i).ToList(); int correct = topic.Count(a => a.Correct);
            Rect row = new Rect(0, i * 96, 842, 84); UiCard(row, Cyan);
            UiText(StarfallCurriculum.Names[i], new Rect(16, row.y + 8, 240, 31), 22, true);
            UiText(topic.Count == 0 ? "No new checks yet" : correct + "/" + topic.Count + " first checks correct", new Rect(280, row.y + 8, 540, 31), 20, true, UiBlue);
            int strong = learningProfile.Facts.Count(f => f.Key.StartsWith(i + ":") && f.IndependentDays >= 3);
            UiText(learningProfile.Independent[i] + " independent · " + learningProfile.Supported[i] + " supported · " + strong + " facts recalled on 3+ days", new Rect(16, row.y + 46, 810, 30), 18, false, UiMuted);
        }
        GUI.EndScrollView(); DrawFriendlyScrollRail(view, ref learningStatsScroll, StarfallCurriculum.Names.Length * 96); HandleListDrag(ref learningStatsScroll, new Rect(view.x - 127, view.y, view.width, view.height), StarfallCurriculum.Names.Length * 96);
    }
}
