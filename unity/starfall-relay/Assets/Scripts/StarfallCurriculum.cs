using System;
using System.Collections.Generic;

[Serializable]
public sealed class StarfallFactRecord
{
    public string Key;
    public int Independent, Supported, Mistakes;
    public bool CheckEvidence;
    public string LastAnsweredDay = "", LastIndependentDay = "", ReviewOn = "";
    public int IndependentDays;
}

[Serializable]
public sealed class StarfallLearningProfile
{
    public int Version = 1;
    public int ActiveLesson, UnlockedLesson;
    public bool TutorialComplete;
    public int GuideVersion, GuideStep;
    public int[] Independent = new int[3], Supported = new int[3], Evidence = new int[3], Wins = new int[3];
    public List<StarfallFactRecord> Facts = new List<StarfallFactRecord>();
    public string CadetName = "Aria", PracticeDate = "", LastPracticeStarDate = "";
    public int LearningXp, Stars, DailySolved, PracticeDifficulty, Accent;
    public bool WelcomeComplete;
    public List<StarfallAttempt> Attempts = new List<StarfallAttempt>();
    public bool ReducedMotion, TypedAnswers = true;
    public bool[] LessonSeen = new bool[6];

    public void Validate()
    {
        Array.Resize(ref Independent, StarfallCurriculum.Names.Length);
        Array.Resize(ref Supported, StarfallCurriculum.Names.Length);
        Array.Resize(ref Evidence, StarfallCurriculum.Names.Length);
        Array.Resize(ref Wins, StarfallCurriculum.Names.Length);
        Array.Resize(ref LessonSeen, StarfallCurriculum.Names.Length);
        PracticeDifficulty = Math.Max(0, Math.Min(2, PracticeDifficulty));
        Accent = Math.Max(0, Math.Min(StarfallCosmetics.Count - 1, Accent));
        Stars = Math.Max(0, Stars); LearningXp = Math.Max(0, LearningXp); DailySolved = Math.Max(0, DailySolved);
        if (string.IsNullOrWhiteSpace(CadetName)) CadetName = "Aria";
        if (CadetName.Length > 20) CadetName = CadetName.Substring(0, 20);
        if (Attempts == null) Attempts = new List<StarfallAttempt>();
        if (Facts == null) Facts = new List<StarfallFactRecord>();
        Facts.RemoveAll(f => f == null || string.IsNullOrEmpty(f.Key));
        UnlockedLesson = Math.Max(0, Math.Min(StarfallCurriculum.Names.Length - 1, UnlockedLesson));
        ActiveLesson = Math.Max(0, Math.Min(UnlockedLesson, ActiveLesson));
        for (int i = 0; i < Independent.Length; i++)
        {
            Independent[i] = Math.Max(0, Independent[i]); Supported[i] = Math.Max(0, Supported[i]);
            Evidence[i] = Math.Max(0, Evidence[i]); Wins[i] = Math.Max(0, Wins[i]);
        }
    }

    public StarfallFactRecord Fact(string key)
    {
        StarfallFactRecord record = Facts.Find(f => f.Key == key);
        if (record != null) return record;
        record = new StarfallFactRecord { Key = key }; Facts.Add(record); return record;
    }

    public void Record(StarfallQuestion question, bool supported, bool countsForCheck)
    {
        Validate();
        StarfallFactRecord fact = Fact(question.Key);
        if (supported) { Supported[question.Lesson]++; fact.Supported++; }
        else
        {
            Independent[question.Lesson]++; fact.Independent++;
            if (countsForCheck && !fact.CheckEvidence) { fact.CheckEvidence = true; Evidence[question.Lesson]++; }
        }
        string today = DateTime.Now.ToString("yyyy-MM-dd");
        fact.LastAnsweredDay = today;
        if (!supported && fact.LastIndependentDay != today)
        { fact.LastIndependentDay = today; fact.IndependentDays++; }
        fact.ReviewOn = DateTime.Now.Date.AddDays(supported ? 1 : fact.IndependentDays >= 3 ? 7 : 3).ToString("yyyy-MM-dd");
        UpdateUnlocks();
    }

    public void UpdateUnlocks()
    {
        for (int i = 0; i < StarfallCurriculum.Names.Length - 1; i++)
            if (i <= UnlockedLesson && Wins[i] > 0 && Evidence[i] >= 2) UnlockedLesson = Math.Max(UnlockedLesson, i + 1);
    }
}

public sealed class StarfallQuestion
{
    public int Lesson, First, Second, Answer;
    public bool MissingNumber;
    public int Result;
    public char Operation;
    public string Key, Prompt, Hint, Explanation;
    public int[] Choices;
}

public static class StarfallCurriculum
{
    public static bool ReviewDue(StarfallFactRecord fact)
        => !string.IsNullOrEmpty(fact.ReviewOn) && string.CompareOrdinal(fact.ReviewOn, DateTime.Now.ToString("yyyy-MM-dd")) <= 0;
    public static int ReviewWeight(StarfallFactRecord fact)
        => Math.Max(1, 5 + Math.Min(8, Math.Max(0, fact.Mistakes + fact.Supported - fact.Independent)) * 3 - Math.Min(4, fact.Independent) + (ReviewDue(fact) ? 8 : 0));
    // Existing lesson IDs stay stable so old saves keep their original topic totals.
    public static readonly string[] Names = { "Addition", "Subtraction", "Make ten", "Multiplication", "Division", "Mixed maths", "Make twenty", "Make zero", "Doubles", "Near doubles", "Count by twos", "Count by fives", "Count by tens", "Missing number" };
    public static readonly string[] Topics = { "Join amounts", "Take away", "Number bonds to 10", "Equal groups", "Equal shares", "Read the sign", "Number bonds to 20", "Take it all away", "Two equal amounts", "One more than a double", "Steps of 2", "Steps of 5", "Steps of 10", "Find the missing part" };
    public static readonly string[] MissionNames = Names;
    public static readonly string[] Teachings = {
        "Addition joins two amounts. Start at the first number and count on.",
        "Subtraction takes an amount away. Count what remains.",
        "Find the missing part that makes exactly ten.",
        "Multiplication counts equal groups.",
        "Division shares a total into equal groups.",
        "Look at the sign. Decide whether to join, take away, group or share.",
        "Find the missing part that makes exactly twenty.",
        "Taking a whole amount away from itself leaves zero.",
        "A double adds the same number twice.",
        "Use a double, then add one more.",
        "Each number is two more than the one before.",
        "Each number is five more than the one before.",
        "Each number is ten more than the one before.",
        "Find the part that is missing from the total." };
    public static string Example(int lesson, int level = 0)
    {
        var q = CreateSkill(lesson, level, 271 + lesson);
        return q.Prompt.Replace("?", q.Answer.ToString());
    }
    public static StarfallQuestion Create(int lesson, int factIndex, int salt)
        => CreateSkill(lesson, 0, (salt ^ factIndex * 7919) & int.MaxValue);

    public static StarfallQuestion Next(StarfallLearningProfile profile, int salt, string previousKey)
    {
        profile.Validate();
        return NextPractice(profile, profile.ActiveLesson, profile.PracticeDifficulty, salt, previousKey);
    }

    public static StarfallQuestion NextPractice(StarfallLearningProfile profile, int lesson, int level, int salt, string previousKey)
    {
        var random = new Random(salt & int.MaxValue);
        var candidates = new List<StarfallQuestion>(); var weights = new List<int>();
        var seen = new HashSet<string>(); int total = 0;
        for (int i = 0; i < 32; i++)
        {
            StarfallQuestion candidate = CreateSkill(lesson, level, random.Next());
            if (candidate.Key == previousKey || !seen.Add(candidate.Key)) continue;
            StarfallFactRecord fact = profile.Facts.Find(f => f.Key == candidate.Key);
            int weight = fact == null ? 6 : ReviewWeight(fact);
            candidates.Add(candidate); weights.Add(weight); total += weight;
        }
        if (candidates.Count == 0) return CreateSkill(lesson, level, salt);
        int pick = random.Next(Math.Max(1, total));
        for (int i = 0; i < candidates.Count; i++) { if (pick < weights[i]) return candidates[i]; pick -= weights[i]; }
        return candidates[0];
    }

    public static StarfallQuestion CreateSkill(int lesson, int level, int seed)
    {
        lesson = Math.Max(0, Math.Min(Names.Length - 1, lesson)); level = Math.Max(0, Math.Min(2, level));
        var r = new Random(seed & int.MaxValue);
        int limit = level == 0 ? 10 : level == 1 ? 20 : 100;
        int a = 0, b = 0, answer = 0, result = 0; char op = '+'; bool missing = false;
        int family = lesson == 5 ? r.Next(4) : lesson == 1 ? 1 : lesson == 3 ? 2 : lesson == 4 ? 3 : 0;
        if (lesson == 2 || lesson == 6 || lesson == 7 || lesson == 13)
        {
            missing = true;
            result = lesson == 2 ? 10 : lesson == 6 ? 20 : lesson == 7 ? 0 : r.Next(2, limit + 1);
            if (lesson == 7) { a = r.Next(1, (level == 0 ? 5 : level == 1 ? 10 : 20) + 1); b = a; op = '-'; }
            else { a = r.Next(level == 0 ? Math.Max(0, result - 5) : 0, result + 1); b = result - a; }
            answer = b;
        }
        else if (lesson == 8 || lesson == 9)
        {
            a = r.Next(level == 2 ? 6 : 1, (level == 0 ? 5 : level == 1 ? 10 : 20) + 1);
            b = a + (lesson == 9 ? 1 : 0); answer = result = a + b;
        }
        else if (lesson >= 10 && lesson <= 12)
        {
            b = lesson == 10 ? 2 : lesson == 11 ? 5 : 10;
            a = r.Next(1, (level == 0 ? 5 : level == 1 ? 10 : 20) + 1) * b;
            answer = result = a + b;
        }
        else if (family == 0) { a = r.Next(0, limit); b = r.Next(0, limit - a + 1); answer = result = a + b; }
        else if (family == 1) { a = r.Next(1, limit + 1); b = r.Next(0, a + 1); answer = result = a - b; op = '-'; }
        else if (family == 2) { a = r.Next(1, (level == 0 ? 5 : level == 1 ? 10 : 12) + 1); b = r.Next(1, (level == 0 ? 3 : level == 1 ? 10 : 12) + 1); answer = result = a * b; op = '*'; }
        else { b = r.Next(1, (level == 0 ? 5 : level == 1 ? 10 : 12) + 1); answer = r.Next(1, (level == 0 ? 3 : level == 1 ? 10 : 12) + 1); a = b * answer; result = answer; op = '/'; }
        string sign = op == '*' ? "×" : op == '/' ? "÷" : op == '-' ? "−" : "+";
        string prompt = missing ? a + " " + sign + " ? = " + result : a + " " + sign + " " + b + " = ?";
        if (lesson >= 10 && lesson <= 12) prompt = (a - b) + ", " + a + ", ?";
        string hint = op == '+' ? "Start at " + a + ". Count on to " + result + "." : op == '-' ? "Start with " + a + ". Take away cells and count what remains." : op == '*' ? "Make " + a + " equal groups of " + b + "." : "Share " + a + " equally into " + b + " groups.";
        string explanation = a + " " + sign + " " + b + " = " + result;
        int[] choices = new int[4]; int slot = r.Next(4), maximum = Math.Max(10, answer + 5);
        var used = new HashSet<int> { answer };
        for (int i = 0; i < 4; i++)
        {
            if (i == slot) { choices[i] = answer; continue; }
            int candidate = Math.Max(0, answer + r.Next(-5, 6));
            while (!used.Add(candidate)) candidate = (candidate + 1) % (maximum + 1);
            choices[i] = candidate;
        }
        return new StarfallQuestion { Lesson = lesson, First = a, Second = b, Answer = answer, Result = result, MissingNumber = missing, Operation = op,
            Key = lesson + ":" + a + op + b + (missing ? "=?" + result : ""), Prompt = prompt, Hint = hint, Explanation = explanation, Choices = choices };
    }
}
