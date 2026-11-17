using UnityEngine;

public sealed partial class StarfallPlusGame
{
    private int homeChoice, moreChoice;

    private void NavigateHub(HubPage page)
    {
        hubPage = page; menuNavPending = false;
        if (page == HubPage.Home) homeChoice = 0;
        menuClearFocus = true;
    }

    private HubPage MenuParent => hubPage == HubPage.Progress || hubPage == HubPage.Achievements || hubPage == HubPage.Armory ? HubPage.More
        : hubPage == HubPage.Difficulty ? HubPage.Lesson : hubPage == HubPage.Account ? HubPage.Settings : hubPage == HubPage.Lesson || hubPage == HubPage.Results ? HubPage.Galaxy : HubPage.Home;

    private void DrawRelayMenuShell()
    {
        Fill(0, 0, 1280, 800, Hex("f2f6fa"));
        if (hubPage == HubPage.Home || hubPage == HubPage.Account && !learningProfile.WelcomeComplete) return;
        if (MenuLink("<  Back", new Rect(207, 35, 180, 58))) NavigateHub(MenuParent);
    }

    private void ChooseHomeAction()
    {
        menuNavPending = false;
        if (homeChoice == 0) ResumeExpedition();
        else NavigateHub(homeChoice == 1 ? HubPage.Galaxy : homeChoice == 2 ? HubPage.More : HubPage.Settings);
    }

    private void DrawRelayHome()
    {
        DrawStarfallWordmark(new Vector2(530, 131));
        UiText("A maths space adventure", new Rect(350, 176, 580, 44), 24, false, UiMuted, TextAnchor.MiddleCenter);
        DrawHomeArtwork();
        DrawMenuPilot(new Vector2(446, 413), -8, 2.6f);
        UiText(learningProfile.CadetName, new Rect(316, 562, 260, 40), 26, true, UiInk, TextAnchor.MiddleCenter);
        UiText("Level " + astronautLevel, new Rect(316, 606, 260, 30), 20, false, UiMuted, TextAnchor.MiddleCenter);
        if (MenuButton("Play", new Rect(650, 295, 330, 82), Cyan, true, true)) ResumeExpedition();
        if (MenuButton("Learn maths", new Rect(650, 404, 330, 64), Cyan, true, menuNavPending && homeChoice == 1)) NavigateHub(HubPage.Galaxy);
        if (MenuButton("Collection", new Rect(650, 484, 330, 64), Cyan, true, menuNavPending && homeChoice == 2)) NavigateHub(HubPage.More);
        if (MenuButton("Settings", new Rect(650, 564, 330, 64), Cyan, true, menuNavPending && homeChoice == 3)) NavigateHub(HubPage.Settings);
        if (menuNavPending && homeChoice == 0) Fill(634, 327, 5, 18, UiBlue);
    }

    private void DrawMoreMenu()
    {
        HubHeading("Collection", "");
        string[] labels = { "Unlockables", "Statistics", "Tools" };
        HubPage[] pages = { HubPage.Achievements, HubPage.Progress, HubPage.Armory };
        for (int i = 0; i < labels.Length; i++)
        {
            Rect row = new Rect(454, 274 + i * 118, 626, 82);
            if (MenuButton(labels[i], row, Cyan, true, menuNavPending && moreChoice == i)) NavigateHub(pages[i]);
        }
    }

    private void DrawHomeArtwork()
    {
        // Keep the illustration confined to one side; the actions retain clear space.
        DrawPixelEllipse(new Vector2(437, 434), 147, 124, Hex("e1edf6"));
        DrawPixelEllipse(new Vector2(434, 423), 138, 115, Hex("e9f2f9"));
        DrawElementSprite(1, new Vector2(330, 291), 48, -14);
        DrawElementSprite(2, new Vector2(558, 341), 43, 12);
        DrawElementSprite(3, new Vector2(335, 532), 33, -8);
        PixelSpark(new Vector2(307, 381), 9, Hex("95bbd8"));
        PixelSpark(new Vector2(538, 493), 7, Hex("b4cddd"));
        PixelSpark(new Vector2(521, 285), 7, Hex("b4cddd"));
    }
}
