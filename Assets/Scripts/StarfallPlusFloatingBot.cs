using UnityEngine;

public sealed partial class StarfallPlusGame
{
    private Vector2 floatingBot = new Vector2(1180, 230);
    private int botZone = -1, botStop;
    private float botRest, botConsumeUntil;
    private static readonly Vector2[][] BotRoutes = {
        new[] { new Vector2(1180, 230), new Vector2(1180, 540), new Vector2(1180, 70), new Vector2(940, 70), new Vector2(550, 70), new Vector2(940, 70), new Vector2(1180, 70) },
        new[] { new Vector2(1130, 51), new Vector2(1212, 51), new Vector2(1084, 51) },
        new[] { new Vector2(1225, 230), new Vector2(1225, 555), new Vector2(1225, 700) },
        new[] { new Vector2(1190, 234), new Vector2(1125, 354), new Vector2(1190, 466), new Vector2(1190, 320) },
        new[] { new Vector2(1190, 200), new Vector2(1190, 470), new Vector2(1190, 620), new Vector2(1190, 340) }
    };
    private Rect FloatingBotHit => new Rect(floatingBot.x - 44, floatingBot.y - 45, 88, 100);

    private bool UpdateFloatingBot(float dt)
    {
        if (guideOpen) return false;
        int zone = menuOpen && phase != Phase.Quiz ? 0 : phase == Phase.Quiz ? 1 : buildPanelOpen ? 2 : FieldControlsAvailable ? 3 : 4;
        Vector2[] route = BotRoutes[zone];
        if (botZone != zone) { botZone = zone; botStop = 1; floatingBot = route[0]; botRest = 1; }
        Vector2 mouse = InterfacePoint(Input.mousePosition);
        bool hovering = FloatingBotHit.Contains(mouse);
        bool tapped = Input.touchCount == 0 && Input.GetMouseButtonDown(0) && hovering;
        foreach (Touch touch in Input.touches)
            if (touch.phase == TouchPhase.Began && FloatingBotHit.Contains(InterfacePoint(touch.position))) tapped = true;
        if (tapped)
        {
            return TryOpenBotAt(floatingBot);
        }
        if (hovering) botRest = .25f;
        botRest = Mathf.Max(0, botRest - dt);
        if (reducedMotion) { floatingBot = route[0]; return false; }
        if (botRest <= 0)
        {
            floatingBot = Vector2.MoveTowards(floatingBot, route[botStop], dt * 34);
            if (Vector2.Distance(floatingBot, route[botStop]) < 1) { botStop = (botStop + 1) % route.Length; botRest = .15f; }
        }
        return false;
    }

    private bool TryOpenBotAt(Vector2 point)
    {
        if (guideOpen || !FloatingBotHit.Contains(point)) return false;
        botConsumeUntil = Time.unscaledTime + .3f;
        OpenContextHelp();
        return true;
    }

    private void ConsumeBotPointer()
    {
        if (Time.unscaledTime < botConsumeUntil && Event.current.isMouse) Event.current.Use();
    }

    private void DrawFloatingBot()
    {
        if (guideOpen || botArt == null) return;
        Rect hit = FloatingBotHit;
        bool hover = hit.Contains(Event.current.mousePosition);
        DrawPixelEllipse(floatingBot + new Vector2(0, 42), 23, 5, new Color(.08f, .21f, .36f, .12f));
        DrawBotPortrait(new Rect(floatingBot.x - 42, floatingBot.y - 44, 84, 87));
        if (hover)
        {
            Rect bubble = new Rect(Mathf.Clamp(floatingBot.x - 155, 12, 1100), Mathf.Clamp(floatingBot.y - 69, 8, 728), 148, 36);
            PixelSurface(bubble, UiPaper, 4);
            UiText("Tap Bot for help", bubble, 17, true, UiBlue, TextAnchor.MiddleCenter);
        }
        if (GUI.Button(hit, GUIContent.none, GUIStyle.none)) { botConsumeUntil = Time.unscaledTime + .3f; OpenContextHelp(); }
    }
}
