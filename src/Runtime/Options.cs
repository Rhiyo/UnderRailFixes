// A "Fixes" tab in the game's Options, with a group (heading, rows) for each fix that is switched on. It is built at the end
// of t1.h (where t1 builds its tabs), with t1's own row builder and dropdown factory through reflection, so it looks
// like the game's tabs. Accept (end of t1.d) writes the values to UnderrailFix.ini, and they apply at once: zoom
// limits are read every frame; scale, insets and the default zoom need the interface laid out again, which is done
// the way the game does it when Interface Size changes (bl9.b + ps.anc), after the game has applied its own options.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

// A dropdown entry: what it shows and what it writes to the ini.
public sealed class FixChoice
{
    public readonly string Text, Value;
    public FixChoice(string text, string value) { Text = text; Value = value; }
    public override string ToString() { return Text; }
    public override bool Equals(object o) { return o is FixChoice c && c.Value == Value; }
    public override int GetHashCode() { return Value.GetHashCode(); }
}

enum StyleOnly { A }

public static class UnderrailOptions
{
    const string TabName = "Fixes";
    sealed class Rows { public aw9 Screenshots, Scale, InsetX, InsetY, Background, ZoomDefault, ZoomMin, ZoomMax, MaxFps; }
    static readonly ConditionalWeakTable<t1, Rows> Added = new ConditionalWeakTable<t1, Rows>();
    static bool relayout;
    static Dictionary<string, string> confirm;     // interface settings to put back unless the player keeps them
    const int ConfirmSeconds = 15;

    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    static readonly MethodInfo Combo = typeof(t1).GetMethod("CreateComboBoxFromEnum", Private).MakeGenericMethod(typeof(StyleOnly));
    static readonly MethodInfo Row = typeof(t1).GetMethods(Private).Single(m => m.Name == "a" && m.GetParameters().Select(p => p.ParameterType).SequenceEqual(
        new[] { typeof(string), typeof(euz), typeof(euz), typeof(bool), typeof(bool), typeof(int), typeof(bool) }));
    static readonly FieldInfo Page = typeof(t1).GetField("s", Private), Tabs = typeof(t1).GetField("u", Private);
    // t1's group heading (a full-width text line) and separator (a line plus spacing), as its own tabs use them.
    static readonly MethodInfo Heading = typeof(t1).GetMethod("a", Private, null, new[] { typeof(string) }, null),
        Separator = typeof(t1).GetMethod("a", Private, null, new[] { typeof(bool) }, null);
    static readonly MethodInfo EndTab = typeof(t1).GetMethod("f", Private, null, Type.EmptyTypes, null), StartTab = typeof(t1).GetMethod("g", Private, null, Type.EmptyTypes, null);

    static string Pct(double v) { return v.ToString("0.##", CultureInfo.InvariantCulture) + "%"; }
    static FixChoice Percent(double v) { return v == 0 ? new FixChoice("0%", "0") : new FixChoice(Pct(v), Pct(v)); }

    // The ini's current values as choices (null if not valid).
    static FixChoice CurrentScale()
    {
        int? p = FixSettings.UiScale;
        return p.HasValue ? new FixChoice(p.Value + "%", p.Value.ToString()) : new FixChoice("Auto", "auto");
    }

    static FixChoice CurrentInset(string text)
    {
        text = (text ?? "").Trim().ToLowerInvariant();
        double v; int px;
        var inv = CultureInfo.InvariantCulture;
        if (text.Length == 0 || text == "0" || text == "0px" || text == "0%") return new FixChoice("0%", "0");
        if (text.EndsWith("%") && double.TryParse(text.TrimEnd('%').Trim(), NumberStyles.Float, inv, out v)) return Percent(v);
        if (text.EndsWith("px") && int.TryParse(text.Substring(0, text.Length - 2).Trim(), out px)) return new FixChoice(px + " px", px + "px");
        if ((text.Contains(".") || text.Contains(",")) && double.TryParse(text.Replace(',', '.'), NumberStyles.Float, inv, out v)) return Percent(v * 100);
        if (int.TryParse(text, out px)) return new FixChoice(px + " px", px + "px");
        return null;
    }

    static FixChoice CurrentScreenshots() { return new FixChoice("", FixSettings.SaveScreenshots ? "1" : "0"); }

    static FixChoice CurrentBackground() { return new FixChoice("", FixSettings.BackgroundFit ? "1" : "0"); }

    static FixChoice CurrentFps() { int f = FixSettings.MaxFps; return f > 0 ? new FixChoice(f + " FPS", f.ToString()) : new FixChoice("Unlimited", "0"); }

    static FixChoice Zoom(int p) { return new FixChoice(p + "%", p.ToString()); }

    static aw9 Add(t1 options, string label, IEnumerable<FixChoice> standard, FixChoice current, FixChoice fallback)
    {
        var items = standard.ToList();
        if (current != null && !items.Contains(current)) items.Add(current);     // a valid ini value the list doesn't have
        var combo = (aw9)Combo.Invoke(options, new object[] { StyleOnly.A, null, null });     // styled like the game's dropdowns
        combo.a(items, null);
        combo.b(items.FirstOrDefault(i => i.Equals(current)) ?? fallback);
        Row.Invoke(options, new object[] { label, combo, null, false, true, 0, false });
        return combo;
    }

    // End of t1.h: our tab, after the game's own, with a group per fix that is switched on (none: no tab).
    public static void AddTab(t1 options)
    {
        if (!FixSettings.ResolutionFix && !FixSettings.UiScaleFix && !FixSettings.ZoomFix && !FixSettings.FrameRateFix) return;
        try
        {
            var page = new cvh();
            page.Text = TabName;
            ((dpg)Tabs.GetValue(options)).n().Add(page);
            Page.SetValue(options, page);
            StartTab.Invoke(options, null);

            var rows = new Rows();
            bool first = true;
            Action<string> group = title =>
            {
                if (!first) Separator.Invoke(options, new object[] { false });
                Heading.Invoke(options, new object[] { title });
                first = false;
            };
            if (FixSettings.ResolutionFix)
            {
                group("Resolution");
                var shots = new[] { new FixChoice("1080p view", "1"), new FixChoice("Game default", "0") };
                rows.Screenshots = Add(options, "Save screenshots", shots, CurrentScreenshots(), shots[0]);
            }
            if (FixSettings.UiScaleFix)
            {
                group("Interface");
                rows.Scale = Add(options, "Interface scale", new[] { new FixChoice("Auto", "auto") }.Concat(new[] { 100, 125, 150, 175, 200, 250, 300 }.Select(p => new FixChoice(p + "%", p.ToString()))),
                    CurrentScale(), new FixChoice("Auto", "auto"));
                var insets = Enumerable.Range(0, 11).Select(i => Percent(i * 5)).ToList();
                rows.InsetX = Add(options, "Interface inset, sides", insets, CurrentInset(FixSettings.UiInsetX), insets[0]);
                rows.InsetY = Add(options, "Interface inset, top and bottom", insets, CurrentInset(FixSettings.UiInsetY), insets[0]);
                var backgrounds = new[] { new FixChoice("Fill the screen", "0"), new FixChoice("Show whole image", "1") };
                rows.Background = Add(options, "Menu backgrounds", backgrounds, CurrentBackground(), backgrounds[0]);
            }
            if (FixSettings.ZoomFix)
            {
                group("Zoom");
                var zooms = new[] { 50, 75, 100, 125, 150, 175, 200, 250, 300, 400 }.Select(Zoom).ToList();
                rows.ZoomDefault = Add(options, "Default zoom", zooms, Zoom(FixSettings.ZoomDefault), Zoom(FixSettings.DefaultZoom));
                rows.ZoomMin = Add(options, "Zoom out limit", zooms, Zoom(FixSettings.ZoomMin), Zoom(FixSettings.DefaultZoomMin));
                rows.ZoomMax = Add(options, "Zoom in limit", zooms, Zoom(FixSettings.ZoomMax), Zoom(FixSettings.DefaultZoomMax));
            }
            if (FixSettings.FrameRateFix)
            {
                group("Frame rate");
                var caps = new[] { new FixChoice("Unlimited", "0") }.Concat(new[] { 30, 60, 120, 144, 165, 240 }.Select(f => new FixChoice(f + " FPS", f.ToString()))).ToList();
                rows.MaxFps = Add(options, "Frame rate cap", caps, CurrentFps(), caps[0]);
            }
            EndTab.Invoke(options, null);
            StartTab.Invoke(options, null);
            Added.Remove(options);
            Added.Add(options, rows);
        }
        catch (Exception ex) { Log.Write("options tab failed: " + ex); }
    }

    // End of t1.d (Accept saves the options): write what changed.
    public static void Save(t1 options)
    {
        Rows rows;
        if (!Added.TryGetValue(options, out rows)) return;
        try
        {
            // Interface scale and insets can leave the menus hard to use, so they are confirmed (see Applied).
            var before = new[] { "UiScale", "UiInsetX", "UiInsetY" }.ToDictionary(k => k, k => FixSettings.Raw(k) ?? "");
            bool ui = Write(rows.Scale, "UiScale", CurrentScale());
            ui |= Write(rows.InsetX, "UiInsetX", CurrentInset(FixSettings.UiInsetX));
            ui |= Write(rows.InsetY, "UiInsetY", CurrentInset(FixSettings.UiInsetY));
            if (ui) { confirm = before; relayout = true; }
            Write(rows.Screenshots, "SaveScreenshots", CurrentScreenshots());     // used at the next save
            relayout |= Write(rows.Background, "BackgroundFit", CurrentBackground());     // only rebuilds the backgrounds; no confirmation needed
            relayout |= Write(rows.ZoomDefault, "ZoomDefault", Zoom(FixSettings.ZoomDefault));
            Write(rows.ZoomMin, "ZoomMin", Zoom(FixSettings.ZoomMin));
            Write(rows.ZoomMax, "ZoomMax", Zoom(FixSettings.ZoomMax));
            if (Write(rows.MaxFps, "MaxFps", CurrentFps())) FrameRate.Reapply();     // applies at once
        }
        catch (Exception ex) { Log.Write("saving UnderrailFix options failed: " + ex); }
    }

    static bool Write(aw9 combo, string key, FixChoice current)
    {
        var chosen = combo == null ? null : combo.l() as FixChoice;
        if (chosen == null || chosen.Equals(current)) return false;
        FixSettings.SetValue(key, chosen.Value);
        Log.Write("options: " + key + "=" + chosen.Value);
        return true;
    }

    // End of d6f.a(ls) (the game applying its options after Accept): lay the interface out again if needed.
    public static void Applied()
    {
        if (!relayout) return;
        relayout = false;
        try
        {
            UnderrailDisplayModes.Reapply();
            bl9.b();
            ps screen = bpo.GetService<ps>();
            if (screen != null) screen.anc();
            UnderrailDisplayModes.ApplyFixedZoom();
            Log.Write("options: interface laid out again");
            if (confirm != null) { var previous = confirm; confirm = null; Ask(previous); }
        }
        catch (Exception ex) { Log.Write("applying UnderrailFix options failed: " + ex); }
    }

    // The game's own "keep the new video settings?" countdown dialog (bnr.a); running out of time picks Revert.
    // Its signature has XNA Color? parameters typed with the game's XNA reference, so it is called through reflection.
    static readonly MethodInfo Dialog = typeof(bnr).GetMethods(BindingFlags.Public | BindingFlags.Static)
        .Single(m => m.Name == "a" && m.GetParameters().Length == 12 && m.GetParameters()[0].ParameterType == typeof(esj));

    static void Ask(Dictionary<string, string> previous)
    {
        euz owner = null;
        bcm screens = bpo.GetService<bcm>();
        if (screens != null && screens.aau() != null) owner = screens.aau().Controls.OfType<cef>().FirstOrDefault();     // as the game does: over the Escape menu
        Dialog.Invoke(null, new object[] {
            new esj(500, 250), new Confirm(previous), "Interface settings",
            "Do you want to keep the new interface settings? They will be put back in {0} second(s).",
            null, "R", null, null, ConfirmSeconds * 1000, new[] { new cpk("K", "Keep"), new cpk("R", "Revert") }, owner, owner });
    }

    sealed class Confirm : eyp
    {
        readonly Dictionary<string, string> previous;
        public Confirm(Dictionary<string, string> previous) { this.previous = previous; }

        public void g0(string button)
        {
            if (button == "K") { Log.Write("options: interface settings kept"); return; }
            foreach (var kv in previous) FixSettings.SetValue(kv.Key, kv.Value);
            Log.Write("options: interface settings reverted");
            relayout = true;
            Applied();
        }
    }
}
