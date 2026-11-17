using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

[Serializable] public sealed class StarfallOnlineConfig { public string url = "", publishableKey = "", redirect = "starfallplus://auth", dashboard = ""; public bool emailReady; }
[Serializable] public sealed class StarfallProviders { public bool google; }
[Serializable] public sealed class StarfallAuthSettings { public StarfallProviders external; }
[Serializable] public sealed class StarfallAuthUser { public string email; }
[Serializable] public sealed class StarfallSession { public string access_token, refresh_token; public int expires_in; public StarfallAuthUser user; }
[Serializable] public sealed class StarfallAccountReply { public StarfallSession session; public string message, error; }
[Serializable] public sealed class StarfallLearner { public string id, nickname; public bool comparisons; }
[Serializable] public sealed class StarfallAccountState { public string username; public StarfallLearner[] learners; }
[Serializable] public sealed class StarfallBoardRow { public string nickname; public int points, questions; }
[Serializable] public sealed class StarfallBoard { public StarfallBoardRow[] rows; public string message; }
[Serializable] public sealed class StarfallGameSnapshot { public int Night, Level, Kills, Upgrades, Health, Shield, Metal, Crystals; }
[Serializable] public sealed class StarfallChallenge { public string id, prompt; }
[Serializable] public sealed class StarfallGrade { public bool correct; public int answer, points; }

public sealed partial class StarfallPlusGame
{
    private StarfallOnlineConfig online = new StarfallOnlineConfig();
    private string accountUsername = "", accountEmail = "", accountPassword = "", accountCurrentPassword = "", accountNote = "", accountToken = "", accountLearner = "", oauthVerifier = "", classCode = "", className = "", classRoom = "", onlineAnswer = "";
    private bool accountCreate = true, accountBusy, parentAccepted, nicknamePrompt, googleAvailable;
    private float accountExpires;
    private StarfallBoard classBoard;
    private StarfallChallenge classChallenge;
    private int classTopic, classDifficulty;
    private bool OnlineConfigured => online != null && Uri.TryCreate(online.url, UriKind.Absolute, out Uri u) && u.Scheme == "https" && !string.IsNullOrWhiteSpace(online.publishableKey);
    private bool SignedIn => !string.IsNullOrEmpty(accountToken) && Time.unscaledTime < accountExpires;
    private readonly Queue<StarfallAttempt> cloudAttempts = new Queue<StarfallAttempt>();
    private bool sendingAttempt, sendingSnapshot;
    private StarfallGameSnapshot pendingSnapshot;

    private void InitializeAccount()
    {
        TextAsset config = Resources.Load<TextAsset>("StarfallOnline");
        if (config != null) { try { online = JsonUtility.FromJson<StarfallOnlineConfig>(config.text); } catch { accountNote = "Online setup is incomplete."; } }
        if (OnlineConfigured) StartCoroutine(OnlineRequest("/auth/v1/settings", null, text => { var settings = JsonUtility.FromJson<StarfallAuthSettings>(text); googleAvailable = settings.external != null && settings.external.google; }, "GET", false));
        Application.deepLinkActivated += ReceiveAccountLink;
        if (!learningProfile.WelcomeComplete) { menuOpen = true; hubPage = HubPage.Account; }
    }

    private void UpdateCloudAccount()
    {
        if (accountToken.Length > 0 && !SignedIn) { SaveLearningProfile(); accountToken = ""; accountLearner = ""; cloudAttempts.Clear(); pendingSnapshot = null; learningProfile = new StarfallLearningProfile(); LoadLearningProfile(); learningProfile.Validate(); ApplyLivery(); accountNote = "Please sign in again to continue online."; }
        if (SignedIn && pendingSnapshot != null && !sendingSnapshot) StartCoroutine(SendGameSnapshot());
        if (SignedIn && accountLearner.Length > 0 && cloudAttempts.Count > 0 && !sendingAttempt) StartCoroutine(SendAttempt());
    }

    private IEnumerator OnlineRequest(string path, string json, Action<string> success, string method = "POST", bool authenticated = true)
    {
        if (!OnlineConfigured) { accountNote = "Online accounts are not connected yet. Local play is available."; yield break; }
        using (var req = new UnityWebRequest(online.url.TrimEnd('/') + path, method))
        {
            req.downloadHandler = new DownloadHandlerBuffer(); req.timeout = 20;
            if (json != null) { req.uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(json)); req.SetRequestHeader("Content-Type", "application/json"); }
            req.SetRequestHeader("apikey", online.publishableKey);
            if (authenticated && SignedIn) req.SetRequestHeader("Authorization", "Bearer " + accountToken);
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success)
            {
                accountNote = req.responseCode == 401 ? "Please sign in again or check your details." : "That could not be completed. Check your connection and details.";
                if (req.responseCode >= 400 && req.downloadHandler.text.StartsWith("{"))
                { try { var error = JsonUtility.FromJson<StarfallAccountReply>(req.downloadHandler.text); string note = error.error ?? error.message; if (!string.IsNullOrEmpty(note) && note.Length < 300) accountNote = note; } catch { } }
                yield break;
            }
            success?.Invoke(req.downloadHandler.text);
        }
    }
    private static string JsonValue(string value) => JsonUtility.ToJson(new StringValue { value = value }).Substring(9).TrimEnd('}');
    [Serializable] private sealed class StringValue { public string value; }
    private static string Body(params string[] pairs)
    { var b = new StringBuilder("{"); for (int i = 0; i < pairs.Length; i += 2) { if (i > 0) b.Append(','); b.Append(JsonValue(pairs[i])).Append(':').Append(JsonValue(pairs[i + 1])); } return b.Append('}').ToString(); }

    private void AcceptSession(StarfallSession session)
    {
        accountPassword = accountCurrentPassword = "";
        if (session == null || string.IsNullOrEmpty(session.access_token)) { accountNote = "Check the parent's email to confirm, then sign in."; return; }
        cloudAttempts.Clear(); pendingSnapshot = null; if (session.user != null) accountEmail = session.user.email;
        accountToken = session.access_token; accountExpires = Time.unscaledTime + Mathf.Max(0, session.expires_in - 60);
        StartCoroutine(OnlineRequest("/rest/v1/rpc/account_state", "{}", text =>
        {
            var state = JsonUtility.FromJson<StarfallAccountState>(text); accountUsername = state.username;
            if (state.learners != null && state.learners.Length > 0) { SaveLearningProfile(); accountLearner = state.learners[0].id;
                learningProfile = new StarfallLearningProfile(); LoadLearningProfile(); learningProfile.Validate();
                learningProfile.CadetName = state.learners[0].nickname; ApplyLivery(); }
            nicknamePrompt = true; accountNote = "Signed in. Choose a nickname for this learner.";
        }));
    }
    private IEnumerator Authenticate()
    {
        accountBusy = true; accountNote = "Connecting…";
        string json = Body("action", accountCreate ? "signup" : "login", "username", accountUsername.Trim().ToLowerInvariant(), "email", accountEmail.Trim(), "password", accountPassword, "parentAccepted", parentAccepted ? "yes" : "no");
        yield return OnlineRequest("/functions/v1/accounts", json, text => { var reply = JsonUtility.FromJson<StarfallAccountReply>(text); accountNote = reply.message ?? reply.error ?? "Check the parent's email."; if (reply.session != null) AcceptSession(reply.session); }, authenticated:false);
        accountPassword = ""; accountBusy = false;
    }
    private void BeginGoogleSignIn()
    {
        if (!OnlineConfigured || !parentAccepted || !googleAvailable) return;
        byte[] bytes = new byte[32]; using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(bytes);
        oauthVerifier = Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        string challenge; using (var sha = SHA256.Create()) challenge = Convert.ToBase64String(sha.ComputeHash(Encoding.ASCII.GetBytes(oauthVerifier))).TrimEnd('=').Replace('+', '-').Replace('/', '_');
        Application.OpenURL(online.url.TrimEnd('/') + "/auth/v1/authorize?provider=google&redirect_to=" + Uri.EscapeDataString(online.redirect) + "&code_challenge=" + challenge + "&code_challenge_method=s256&scope=openid%20email%20profile");
        accountNote = "Finish the parent's Google sign-in in your browser.";
    }
    private void ReceiveAccountLink(string link)
    {
        if (!Uri.TryCreate(link, UriKind.Absolute, out Uri actual) || !Uri.TryCreate(online.redirect, UriKind.Absolute, out Uri expected) || actual.Scheme != expected.Scheme || actual.Host != expected.Host || actual.AbsolutePath != expected.AbsolutePath || oauthVerifier.Length == 0) return;
        foreach (string part in actual.Query.TrimStart('?').Split('&'))
        {
            string[] pieces = part.Split(new[] { '=' }, 2); if (pieces.Length != 2 || pieces[0] != "code") continue;
            string verifier = oauthVerifier; oauthVerifier = "";
            StartCoroutine(OnlineRequest("/auth/v1/token?grant_type=pkce", Body("auth_code", Uri.UnescapeDataString(pieces[1]), "code_verifier", verifier), text => AcceptSession(JsonUtility.FromJson<StarfallSession>(text)), authenticated:false));
        }
    }
    private void SaveNickname()
    {
        learningProfile.CadetName = learningProfile.CadetName.Trim(); learningProfile.Validate();
        if (SignedIn && accountLearner.Length > 0) StartCoroutine(OnlineRequest("/rest/v1/rpc/rename_learner", Body("learner", accountLearner, "nickname", learningProfile.CadetName), _ => accountNote = "Nickname saved online."));
        learningProfile.WelcomeComplete = true; SaveLearningProfile(); nicknamePrompt = false; NavigateHub(HubPage.Home);
    }
    private void QueueCloudAttempt(StarfallAttempt record)
    { if (CanPersistProgress && SignedIn && accountLearner.Length > 0 && cloudAttempts.Count < 100) cloudAttempts.Enqueue(record); }
    private IEnumerator SendAttempt()
    {
        sendingAttempt = true; StarfallAttempt a = cloudAttempts.Peek(); bool delivered = false;
        string learner = accountLearner;
        string json = "{\"id\":" + JsonValue(a.Id) + ",\"learner\":" + JsonValue(learner) + ",\"event\":" + JsonUtility.ToJson(a) + "}";
        yield return OnlineRequest("/rest/v1/rpc/record_device_attempt", json, _ => delivered = true);
        // Failed events stay queued during this session; identical IDs are idempotent on the server.
        if (delivered && cloudAttempts.Count > 0 && ReferenceEquals(cloudAttempts.Peek(), a)) cloudAttempts.Dequeue();
        if (!delivered) yield return new WaitForSecondsRealtime(20);
        sendingAttempt = false;
    }
    private void QueueGameSnapshot()
    {
        if (!CanPersistProgress || !SignedIn || accountLearner.Length == 0) return;
        pendingSnapshot = new StarfallGameSnapshot { Night = night, Level = astronautLevel, Kills = lifetimeKills, Upgrades = InstalledRanks, Health = coreHp, Shield = shieldHp, Metal = alloy, Crystals = crystal };
    }
    private IEnumerator SendGameSnapshot()
    {
        sendingSnapshot = true; var value = pendingSnapshot; pendingSnapshot = null;
        yield return OnlineRequest("/rest/v1/rpc/save_game_snapshot", "{\"learner\":" + JsonValue(accountLearner) + ",\"snapshot\":" + JsonUtility.ToJson(value) + "}", null);
        sendingSnapshot = false;
    }
    private string AccountField(string label, string value, Rect rect, bool password = false)
    {
        UiText(label, new Rect(rect.x, rect.y, rect.width, 29), 20, true);
        Rect input = new Rect(rect.x, rect.y + 36, rect.width, 48); UiRound(input, Hex("e5eef8"));
        GUIStyle style = UiStyle(23, UiInk); style.padding = new RectOffset(12, 12, 6, 6); style.wordWrap = false;
        return password ? GUI.PasswordField(input, value, '●', 128, style) : GUI.TextField(input, value, 100, style);
    }
    private string AccountFormHint()
    {
        if (!OnlineConfigured) return "Online accounts are not connected. You can play on this device.";
        if (accountBusy) return "Connecting…";
        if (!parentAccepted) return "Tick the parent, guardian or teacher box to continue.";
        if (accountCreate)
        {
            if (!online.emailReady) return "Account email setup is pending. You can play on this device.";
            if (!System.Text.RegularExpressions.Regex.IsMatch(accountUsername.Trim().ToLowerInvariant(), "^[a-z0-9_]{3,24}$")) return "Username: use 3–24 letters, numbers or underscores, with no spaces.";
            if (!System.Text.RegularExpressions.Regex.IsMatch(accountEmail.Trim(), @"^[^\s@]+@[^\s@]+\.[^\s@]+$")) return "Enter a valid parent email address.";
            if (accountPassword.Length < 12) return "Password: " + accountPassword.Length + "/12 characters. Add " + (12 - accountPassword.Length) + " more to continue.";
        }
        else if (string.IsNullOrWhiteSpace(accountUsername) && string.IsNullOrWhiteSpace(accountEmail)) return "Enter your username or email address.";
        if (accountPassword.Length == 0) return "Enter your password to sign in.";
        return "";
    }
    private void DrawAccountScreen()
    {
        if (nicknamePrompt)
        {
            HubHeading("What should we call you?", "Choose a nickname. Keep your real full name private.");
            DrawMenuPilot(new Vector2(762, 345), 0, 1.8f);
            learningProfile.CadetName = AccountField("Nickname", learningProfile.CadetName, new Rect(484, 458, 566, 90));
            if (MenuButton("Let's play", new Rect(484, 602, 566, 72), Cyan, !string.IsNullOrWhiteSpace(learningProfile.CadetName), true)) SaveNickname(); return;
        }
        HubHeading(SignedIn ? "Account" : "Parent setup", SignedIn ? "Manage this learner's account." : "A parent sets up the account. Your child plays with a nickname.");
        if (SignedIn)
        {
            UiText("Signed in as " + accountUsername, new Rect(334, 239, 866, 45), 26, true, UiBlue);
            accountUsername = AccountField("Username", accountUsername, new Rect(334, 307, 414, 88));
            accountCurrentPassword = AccountField("Current password", accountCurrentPassword, new Rect(786, 307, 414, 88), true);
            accountPassword = AccountField("New password (12+ characters)", accountPassword, new Rect(334, 416, 414, 88), true);
            accountEmail = AccountField("Parent email", accountEmail, new Rect(786, 416, 414, 88));
            if (MenuButton("Save account changes", new Rect(334, 532, 414, 64), Cyan, !accountBusy, true)) StartCoroutine(ChangeAccount());
            if (MenuButton("Sign out", new Rect(786, 532, 414, 64), Cyan)) StartCoroutine(SignOutAccount());
            if (MenuButton("Change nickname", new Rect(334, 620, 866, 58), Cyan)) nicknamePrompt = true;
        }
        else
        {
            if (MenuTab("Create account", new Rect(334, 235, 414, 54), accountCreate)) accountCreate = true;
            if (MenuTab("Sign in", new Rect(786, 235, 414, 54), !accountCreate)) accountCreate = false;
            bool enabled = GUI.enabled; GUI.enabled &= OnlineConfigured && !accountBusy;
            accountUsername = AccountField("Username", accountUsername, new Rect(334, 314, 414, 88));
            accountEmail = AccountField(accountCreate ? "Parent email" : "Email (or use username)", accountEmail, new Rect(786, 314, 414, 88));
            accountPassword = AccountField(accountCreate ? "Password · " + accountPassword.Length + "/12 characters minimum" : "Password", accountPassword, new Rect(334, 420, 866, 88), true);
            Rect parentRow = new Rect(334, 523, 866, 42);
            UiRound(new Rect(parentRow.x, parentRow.y + 7, 28, 28), UiBlue, 3);
            UiRound(new Rect(parentRow.x + 2, parentRow.y + 9, 24, 24), parentAccepted ? UiBlue : Color.white, 2);
            if (parentAccepted) UiText("✓", new Rect(parentRow.x, parentRow.y + 4, 28, 32), 23, true, Color.white, TextAnchor.MiddleCenter);
            UiText("I am the parent, guardian or teacher setting up this account.", new Rect(parentRow.x + 42, parentRow.y, parentRow.width - 42, parentRow.height), 21);
            if (GUI.Button(parentRow, GUIContent.none, GUIStyle.none)) parentAccepted = !parentAccepted;
            if (MenuButton(accountBusy ? "Connecting…" : accountCreate ? online.emailReady ? "Create account" : "Email setup pending" : "Sign in", new Rect(334, 578, 414, 62), Cyan, AccountFormHint().Length == 0, true)) StartCoroutine(Authenticate());
            if (MenuButton(googleAvailable ? "Continue with Google" : "Google setup pending", new Rect(786, 578, 414, 62), Gold, parentAccepted && googleAvailable)) BeginGoogleSignIn();
            GUI.enabled = enabled;
            if (MenuLink("Play on this device", new Rect(334, 653, 414, 54))) { nicknamePrompt = true; accountNote = ""; }
            if (MenuLink("Forgot password?", new Rect(786, 653, 414, 54)) && OnlineConfigured && parentAccepted) StartCoroutine(RecoverAccount());
        }
        UiText(accountNote.Length > 0 ? accountNote : SignedIn ? "" : AccountFormHint(), new Rect(334, 716, 866, 70), 20, false, UiMuted);
    }
    private IEnumerator ChangeAccount()
    {
        accountBusy = true;
        yield return OnlineRequest("/functions/v1/accounts", Body("action", "change", "username", accountUsername, "password", accountPassword, "currentPassword", accountCurrentPassword, "email", accountEmail), text => accountNote = JsonUtility.FromJson<StarfallAccountReply>(text).message);
        accountPassword = accountCurrentPassword = ""; accountBusy = false;
    }
    private IEnumerator RecoverAccount()
    { yield return OnlineRequest("/functions/v1/accounts", Body("action", "recover", "email", accountEmail, "username", accountUsername), _ => accountNote = "If the account exists, a recovery email has been sent.", authenticated:false); }
    private IEnumerator SignOutAccount()
    {
        yield return OnlineRequest("/auth/v1/logout", "{}", null);
        SaveLearningProfile(); accountToken = accountLearner = accountPassword = accountCurrentPassword = ""; cloudAttempts.Clear(); pendingSnapshot = null;
        learningProfile = new StarfallLearningProfile(); LoadLearningProfile(); learningProfile.Validate(); ApplyLivery(); classBoard = null; classChallenge = null; accountNote = "Signed out.";
    }
    private void DrawCloudComparison()
    {
        UiText("Private class comparisons", new Rect(334, 319, 866, 42), 27, true);
        if (!SignedIn) { UiText("A parent connects your account and approves your class. No public rankings or real names.", new Rect(334, 385, 866, 110), 25, false, UiMuted); if (MenuButton("Parent account", new Rect(334, 544, 866, 68), Cyan)) NavigateHub(HubPage.Account); return; }
        classCode = AccountField("Class code (approved by your parent)", classCode, new Rect(334, 380, 566, 88));
        if (MenuButton("Show class", new Rect(928, 416, 272, 50), Cyan))
            StartCoroutine(OnlineRequest("/rest/v1/rpc/class_board", "{\"room\":" + JsonValue(classCode.Trim()) + ",\"topic\":" + classTopic + ",\"difficulty\":" + classDifficulty + "}", text => classBoard = JsonUtility.FromJson<StarfallBoard>(text)));
        if (MenuButton(StarfallCurriculum.Names[classTopic] + " >", new Rect(334, 485, 414, 52), Cyan)) { classTopic = (classTopic + 1) % StarfallCurriculum.Names.Length; classBoard = null; }
        if (MenuButton(new[] { "Easy", "Medium", "Challenge" }[classDifficulty] + " >", new Rect(786, 485, 414, 52), Cyan)) { classDifficulty = (classDifficulty + 1) % 3; classBoard = null; }
        if (classBoard != null && classBoard.rows != null)
        {
            for (int i = 0; i < Mathf.Min(3, classBoard.rows.Length); i++)
                UiText(classBoard.rows[i].nickname + " · " + classBoard.rows[i].points + " class points", new Rect(334, 546 + i * 36, 866, 34), 21, true, UiBlue);
            if (classBoard.rows.Length == 0) UiText("No opted-in classmates yet.", new Rect(334, 550, 866, 50), 22, false, UiMuted);
        }
        if (MenuButton("Parent & teacher dashboard", new Rect(334, 682, 866, 58), Cyan, OnlineConfigured && !string.IsNullOrEmpty(online.dashboard), true)) Application.OpenURL(online.dashboard);
        UiText("Same topic and difficulty · This week · Private nicknames", new Rect(334, 751, 866, 30), 18, false, UiMuted);

    }
}
