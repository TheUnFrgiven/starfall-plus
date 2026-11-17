using UnityEngine;

public sealed partial class RuneholdGame
{
    private Vector2 collectionScroll;
    private int unlockFocus, colorFocus, collectedTrack = -1;
    private float collectedTrackUntil, suppressCollectionClickUntil;
    private bool collectionDrag, collectionDragged;
    private float collectionDragY, collectionDragScroll;
    private static readonly Rect CollectionView = new Rect(334, 320, 866, 430);
    private float CollectionContentHeight => collectionTab == 0 ? StarfallUnlocks.Tracks.Length * 156 : Mathf.Ceil(StarfallCosmetics.Count / 2f) * 208;

    private void SwitchCollectionTab(int tab)
    {
        if (collectionTab == tab) return;
        collectionTab = tab; collectionScroll = Vector2.zero; collectionDrag = collectionDragged = false;
    }

    private void UpdateCollectionNavigation()
    {
        if (Input.GetKeyDown(KeyCode.Tab)) SwitchCollectionTab(1 - collectionTab);
        int down = Input.GetKeyDown(KeyCode.DownArrow) ? 1 : Input.GetKeyDown(KeyCode.UpArrow) ? -1 : 0;
        if (collectionTab == 0)
        {
            if (down != 0)
            {
                unlockFocus = (unlockFocus + down + StarfallUnlocks.Tracks.Length) % StarfallUnlocks.Tracks.Length;
                collectionScroll.y = Mathf.Clamp(unlockFocus * 156 - 144, 0, CollectionContentHeight - CollectionView.height);
            }
            if (Input.GetKeyDown(KeyCode.Return)) ClaimUnlock(unlockFocus);
        }
        else
        {
            int move = down * 2 + (Input.GetKeyDown(KeyCode.RightArrow) ? 1 : Input.GetKeyDown(KeyCode.LeftArrow) ? -1 : 0);
            if (move != 0)
            {
                colorFocus = (colorFocus + move + StarfallCosmetics.Count) % StarfallCosmetics.Count;
                collectionScroll.y = Mathf.Clamp(colorFocus / 2 * 208 - 104, 0, CollectionContentHeight - CollectionView.height);
            }
            if (Input.GetKeyDown(KeyCode.Return)) EquipLivery(colorFocus);
            for (int i = 0; i < StarfallCosmetics.Count; i++)
                if (Input.GetKeyDown((KeyCode)((int)KeyCode.Alpha1 + i))) { colorFocus = i; EquipLivery(i); }
        }

        // A scroll gesture must never also collect a reward or buy a color.
        Rect screenView = new Rect(CollectionView.x - 127, CollectionView.y, CollectionView.width, CollectionView.height);
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            Vector2 at = InterfacePoint(touch.position);
            if (touch.phase == TouchPhase.Began) BeginCollectionDrag(at, screenView);
            else if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) MoveCollectionDrag(at);
            else EndCollectionDrag();
        }
        else
        {
            Vector2 at = InterfacePoint(Input.mousePosition);
            if (Input.GetMouseButtonDown(0)) BeginCollectionDrag(at, screenView);
            if (Input.GetMouseButton(0)) MoveCollectionDrag(at);
            if (Input.GetMouseButtonUp(0)) EndCollectionDrag();
        }
    }

    private void BeginCollectionDrag(Vector2 at, Rect area)
    {
        collectionDrag = area.Contains(at); collectionDragged = false;
        collectionDragY = at.y; collectionDragScroll = collectionScroll.y;
    }
    private void MoveCollectionDrag(Vector2 at)
    {
        if (!collectionDrag || Mathf.Abs(at.y - collectionDragY) < 8 && !collectionDragged) return;
        collectionDragged = true; suppressCollectionClickUntil = Time.unscaledTime + .2f;
        collectionScroll.y = Mathf.Clamp(collectionDragScroll + collectionDragY - at.y, 0, Mathf.Max(0, CollectionContentHeight - CollectionView.height));
    }
    private void EndCollectionDrag()
    {
        if (collectionDragged) suppressCollectionClickUntil = Time.unscaledTime + .2f;
        collectionDrag = collectionDragged = false;
    }

    private void DrawUnlockCollection()
    {
        HubHeading("Unlockables", "Complete goals. Collect credits. Choose colors.");
        if (MenuTab("Goals", new Rect(334, 240, 282, 58), collectionTab == 0)) SwitchCollectionTab(0);
        if (MenuTab("My colors", new Rect(634, 240, 282, 58), collectionTab == 1)) SwitchCollectionTab(1);
        UiText(MenuCount(pilotJournal.Tokens, "color credit"), new Rect(934, 240, 266, 58), 23, true, UiBlue, TextAnchor.MiddleRight);

        collectionScroll = GUI.BeginScrollView(CollectionView, collectionScroll, new Rect(0, 0, 842, CollectionContentHeight), false, false, GUIStyle.none, GUIStyle.none);
        if (collectionTab == 0)
            for (int i = 0; i < StarfallUnlocks.Tracks.Length; i++) DrawUnlockGoal(i);
        else
            for (int i = 0; i < StarfallCosmetics.Count; i++) DrawColorChoice(i);
        GUI.EndScrollView();
        DrawCollectionScrollRail();
    }

    private void DrawUnlockGoal(int index)
    {
        StarfallUnlockTrack track = StarfallUnlocks.Tracks[index];
        int stage = StarfallUnlocks.Stage(pilotJournal, track);
        bool done = stage >= track.Targets.Length;
        int shown = Mathf.Min(stage, track.Targets.Length - 1), target = track.Targets[shown];
        int value = done ? target : Mathf.Min(target, UnlockValue(track, stage));
        bool ready = !done && value >= target;
        Rect card = new Rect(0, index * 156, 842, 142); UiCard(card, ready ? Gold : Cyan);
        DrawAchievementMedal(track.Badge, new Vector2(43, card.y + 65), done || stage > 0);
        UiText(track.Goal(shown), new Rect(92, card.y + 12, 530, 50), 25, true);
        UiText("Goal " + (shown + 1) + "/" + track.Targets.Length, new Rect(628, card.y + 18, 194, 32), 19, false, UiMuted, TextAnchor.MiddleRight);
        UiRound(new Rect(94, card.y + 78, 472, 9), Hex("dce6ef"), 3);
        if (value > 0) UiRound(new Rect(94, card.y + 78, Mathf.Max(4, 472f * value / target), 9), ready || done ? Hex("d7a746") : UiBlue, 2);
        bool justCollected = collectedTrack == index && Time.unscaledTime < collectedTrackUntil;
        UiText(justCollected ? "Reward collected · next goal above" : done ? "All goals completed" : value.ToString("N0") + " / " + target.ToString("N0"), new Rect(92, card.y + 98, 490, 32), 19, false, UiMuted);
        string label = done ? "Completed" : ready ? "Collect " + MenuCount(track.Rewards[stage], "credit") : MenuCount(track.Rewards[stage], "credit");
        if (MenuButton(label, new Rect(620, card.y + 72, 202, 54), Gold, ready && Time.unscaledTime >= suppressCollectionClickUntil, ready)) ClaimUnlock(index);
    }

    private void DrawColorChoice(int index)
    {
        Rect card = new Rect(index % 2 * 432, index / 2 * 208, 410, 190);
        bool owned = pilotJournal.Liveries.Contains(index), equipped = learningProfile.Accent == index;
        bool affordable = pilotJournal.Tokens >= LiveryPrices[index];
        UiCard(card, LiveryEnergy(index), equipped);
        DrawLiveryPortrait(index, new Vector2(card.x + 61, card.y + 96), 125);
        UiText(LiveryNames[index], new Rect(card.x + 126, card.y + 17, 266, 36), 26, true);
        UiText("Astronaut, tools and lights", new Rect(card.x + 126, card.y + 63, 266, 49), 20, false, UiMuted);
        string label = equipped ? "Wearing these!" : owned ? "Wear these colors" : affordable ? "Unlock · " + MenuCount(LiveryPrices[index], "credit") : "Need " + MenuCount(LiveryPrices[index] - pilotJournal.Tokens, "credit");
        if (MenuButton(label, new Rect(card.x + 126, card.y + 126, 266, 50), LiveryEnergy(index), !equipped && (owned || affordable) && Time.unscaledTime >= suppressCollectionClickUntil, equipped)) EquipLivery(index);
    }

    private void DrawCollectionScrollRail()
    {
        float max = Mathf.Max(0, CollectionContentHeight - CollectionView.height);
        if (max <= 0) return;
        Rect rail = new Rect(CollectionView.xMax - 10, CollectionView.y, 10, CollectionView.height);
        float thumbHeight = Mathf.Max(50, rail.height * rail.height / CollectionContentHeight);
        var trackStyle = new GUIStyle(GUIStyle.none) { fixedWidth = 10 };
        var thumbStyle = new GUIStyle(GUIStyle.none) { fixedWidth = 10, fixedHeight = thumbHeight };
        collectionScroll.y = GUI.VerticalSlider(rail, collectionScroll.y, 0, max, trackStyle, thumbStyle);
        UiRound(rail, Hex("dce6ee"), 3);
        UiRound(new Rect(rail.x, rail.y + (rail.height - thumbHeight) * collectionScroll.y / max, rail.width, thumbHeight), Hex("839eb6"), 3);
    }
}
