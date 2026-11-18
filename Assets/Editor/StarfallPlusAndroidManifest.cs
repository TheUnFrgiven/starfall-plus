#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

internal static class StarfallPlusAndroidManifest
{
    internal static void InsetAdaptiveIcon(string path)
    {
        XNamespace android = "http://schemas.android.com/apk/res/android";
        var document = XDocument.Load(path);
        var foreground = document.Root?.Element("foreground");
        var drawable = foreground?.Attribute(android + "drawable");
        if (drawable == null) return;
        // Keep the complete Bot inside launcher masks without changing its pixel-art source.
        var inset = new XElement("inset", new XAttribute(android + "drawable", drawable.Value));
        foreach (string edge in new[] { "Left", "Top", "Right", "Bottom" })
            inset.SetAttributeValue(android + "inset" + edge, "18%");
        drawable.Remove(); foreground.Add(inset); document.Save(path);
    }

    internal static void RegisterCallback(string path)
    {
        XNamespace android = "http://schemas.android.com/apk/res/android";
        XDocument document = XDocument.Load(path);
        var activities = document.Root?.Element("application")?.Elements("activity");
        XElement activity = activities?.FirstOrDefault(a => a.Elements("intent-filter").Elements("action")
            .Any(x => (string)x.Attribute(android + "name") == "android.intent.action.MAIN"));
        if (activity == null) throw new InvalidOperationException("The generated Android manifest has no launcher activity.");
        bool exists = activity.Elements("intent-filter").Elements("data").Any(d =>
            (string)d.Attribute(android + "scheme") == "starfallplus" && (string)d.Attribute(android + "host") == "auth");
        if (!exists)
        {
            activity.Add(new XElement("intent-filter",
                new XElement("action", new XAttribute(android + "name", "android.intent.action.VIEW")),
                new XElement("category", new XAttribute(android + "name", "android.intent.category.DEFAULT")),
                new XElement("category", new XAttribute(android + "name", "android.intent.category.BROWSABLE")),
                new XElement("data", new XAttribute(android + "scheme", "starfallplus"), new XAttribute(android + "host", "auth"))));
        }
        activity.SetAttributeValue(android + "launchMode", "singleTask");
        document.Save(path);
    }
}

#if UNITY_ANDROID
internal sealed class StarfallPlusAndroidPostprocess : UnityEditor.Android.IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 50;
    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifest = Path.Combine(path, "src/main/AndroidManifest.xml");
        if (!File.Exists(manifest)) manifest = Path.Combine(path, "unityLibrary/src/main/AndroidManifest.xml");
        StarfallPlusAndroidManifest.RegisterCallback(manifest);
        string icon = Path.GetFullPath(Path.Combine(path, "../launcher/src/main/res/mipmap-anydpi-v26/app_icon.xml"));
        if (File.Exists(icon)) StarfallPlusAndroidManifest.InsetAdaptiveIcon(icon);
    }
}
#endif
#endif
