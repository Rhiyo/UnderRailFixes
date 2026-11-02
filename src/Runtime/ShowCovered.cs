// Show Covered (GameplayFixes + ShowCovered): whatever holding Tab would outline gets the outline all the time,
// but only where something in front of the creature covers it. Same colours and outline as Tab (the game's own
// outline layer and shader); with Tab held, hovering, or Character Outlines "Always in combat" the game's full outline
// shows as before, and with Character Outlines "Never" there's none (as with Tab).
//
// How the frame is drawn (a7m): world layers are drawn back to front through a7m.a(cp9, a2f, bool, bkx), so whatever
// is drawn after a creature is in front of it; layers with their own shader (the outline, u = "Outline") are kept
// aside and drawn after the world in a7m.b(cp9), which is why outlines show through walls.
// - eg0 creates the outline layer when Tab (or the reverse-highlight key) is held: the reverse-highlight check is
//   patched (Reverse) so it's created anyway, and the layer is marked (Add).
// - Drawn records the world layers in drawing order.
// - BeforePass takes the marked layers out of the game's shader pass; AfterPass draws each of them through a mask:
//   1. the layers drawn after the creature that overlap it, in white, into a mask target (their real shapes);
//   2. the outline, with the game's shader and colour, into a second target (ShowCoveredStyle=outline); or the
//      creature's own sprites there, recoloured to the outline's colour (fill);
//   3. the second target multiplied by the mask's coverage, then put on screen with ShowCoveredTransparency.
// With fill, creatures that already have the game's outline (Tab held, hovering, Always in combat) are filled too, on
// top of it (AfterPass runs after the game's shader pass); with outline, the game's outline already shows them.
//   Both targets are screen-sized, so the game's own batch (bc2, with the interface mapping) draws into them exactly
//   where it draws on screen. Only creatures with something in front of them get any of this.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class ShowCovered
{
    static bool On => FixSettings.ShowCovered;

    // --- eg0: the outline layer -------------------------------------------------------------------------------------

    [ThreadStatic] static bool forced;
    static readonly ConditionalWeakTable<a2f, object> Marked = new ConditionalWeakTable<a2f, object>();
    static readonly object Mark = new object();

    // eg0: the reverse-highlight key check, reached when the outline isn't otherwise shown and Tab isn't held.
    public static bool Reverse(bool down)
    {
        if (down || !On) return down;
        forced = true;
        diagReverse++;
        return true;
    }

    // eg0 builds creature outlines only while the world view takes input (mv.w), which is off during zone transitions.
    // With Show Covered they are built then too, and always marked (only the covered part, never a full outline).
    [ThreadStatic] static bool transition;
    public static bool Interactive(bool takesInput)
    {
        transition = !takesInput && On;
        return takesInput || On;
    }

    // eg0: bym.a(outline layer) (the creature's layers), right after; the layer is marked if only Reverse made it, or
    // during a transition.
    public static void Add(bym layers, a2f outline)
    {
        if (transition) { transition = false; forced = true; }
        if (forced) { forced = false; Marked.Remove(outline); Marked.Add(outline, Mark); diagAdd++; }
        layers.a(outline);
    }

    // --- a7m: the world pass and the shader pass --------------------------------------------------------------------

    // Creatures: whose layers have an outline this frame (every creature Tab would outline). They don't count as cover.
    sealed class View { public readonly List<a2f> Drawn = new List<a2f>(), Masked = new List<a2f>(); public readonly HashSet<bym> Creatures = new HashSet<bym>(); }
    static readonly ConditionalWeakTable<a7m, View> Views = new ConditionalWeakTable<a7m, View>();

    // Start of a7m.a(cp9, a2f, bool shaderPass, bkx): every layer the world pass draws, in order.
    public static void Drawn(a7m view, cp9 context, a2f layer, bool shaderPass)
    {
        if (shaderPass || !On || layer == null) return;
        var state = Views.GetOrCreateValue(view);
        state.Drawn.Add(layer);
        if (FixSettings.AnyKeyhole) Keyhole.Layer(view, context, layer, state.Drawn.Count - 1);
    }

    // a7m's world pass, after its loop, right before it sets the screen as the target again.
    public static void WorldDone(a7m view, cp9 context)
    {
        try { Keyhole.Done(view, context); }
        catch (Exception ex) { KeyholeDiag("ERROR " + ex); if (!failed) { failed = true; Log.Write("show covered (keyhole): " + ex); } }
    }

    // TEMPORARY keyhole diagnostics, once a second.
    static DateTime keyholeDiagNext;
    internal static bool KeyholeDiagDue { get { if (DateTime.Now < keyholeDiagNext) return false; keyholeDiagNext = DateTime.Now.AddSeconds(1); return true; } }
    internal static void KeyholeDiag(string line)
    {
        try { System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UnderrailFix-diag.log"), DateTime.Now.ToString("HH:mm:ss") + " keyhole: " + line + "\n"); } catch { }
    }

    internal static List<a2f> DrawnLayers(a7m view) { View v; return Views.TryGetValue(view, out v) ? v.Drawn : null; }
    internal static bool IsCreature(a7m view, bym owner) { View v; return owner != null && Views.TryGetValue(view, out v) && v.Creatures.Contains(owner); }

    static readonly FieldInfo ShaderLayers = typeof(a7m).GetField("x", BindingFlags.Instance | BindingFlags.NonPublic);

    // Start of a7m.b(cp9): the marked outlines leave the game's shader pass.
    public static void BeforePass(a7m view)
    {
        var state = Views.GetOrCreateValue(view);
        state.Masked.Clear();
        if (!On) return;
        var all = ShaderLayers.GetValue(view) as Dictionary<string, List<a2f>>;
        List<a2f> outlines;
        if (all == null || !all.TryGetValue("Outline", out outlines)) return;
        object m;
        state.Creatures.Clear();
        foreach (var o in outlines) if (o.n != null) state.Creatures.Add(o.n);
        for (int i = outlines.Count - 1; i >= 0; i--)
        {
            var effect = EffectFor(outlines[i].n);
            bool drawn = effect != FixSettings.CoveredEffect.Off && effect != FixSettings.CoveredEffect.Keyhole;
            if (Marked.TryGetValue(outlines[i], out m))
            {
                // Ours: never drawn whole; drawn on the covered part with this creature's effect, or not at all.
                var layer = outlines[i];
                outlines.RemoveAt(i);
                diagMarkedSeen++;
                if (drawn) state.Masked.Insert(0, layer);
            }
            else
            {
                // The game's own outline (Tab, hovering, Always in combat) stays in its pass; with a fill, the covered
                // part is still filled, drawn after that pass so it's above the outline.
                diagUnmarkedSeen++;
                if (drawn && effect != FixSettings.CoveredEffect.Outline) state.Masked.Insert(0, outlines[i]);
            }
        }
    }

    // End of a7m.b(cp9).
    public static void AfterPass(a7m view, cp9 context)
    {
        View state;
        if (!Views.TryGetValue(view, out state)) return;
        try { if (state.Masked.Count > 0) Draw(view, context, state); }
        catch (Exception ex) { Diag("ERROR " + ex); if (!failed) { failed = true; Log.Write("show covered: " + ex); } }
        finally { Diag("pass: drawn " + state.Drawn.Count + " masked " + state.Masked.Count); DiagFlush(true); state.Drawn.Clear(); state.Masked.Clear(); Effects.Clear(); }
    }

    static bool failed;

    // The effect chosen for a creature: the player's; hostile (the game's hostile and unfriendly attitude towards the
    // player, as eg0 colours outlines: red, orange); or other (everyone else, passive ones too). Once per frame each.
    static readonly Dictionary<bym, FixSettings.CoveredEffect> Effects = new Dictionary<bym, FixSettings.CoveredEffect>();
    internal static FixSettings.CoveredEffect EffectFor(bym owner)
    {
        if (owner == null) return FixSettings.CoveredEffect.Off;
        FixSettings.CoveredEffect e;
        if (Effects.TryGetValue(owner, out e)) return e;
        var character = owner.b as c5x;     // walls and props are c5x too: characters are the ones with stats (amc)
        e = character == null || character.GetAspect<amc>() == null ? FixSettings.CoveredEffect.Off : character.IsPlayer ? FixSettings.CoveredPlayer
            : Hostile(character) ? FixSettings.CoveredHostile : FixSettings.CoveredOther;
        Effects[owner] = e;
        return e;
    }

    static bool Hostile(c5x character)
    {
        try
        {
            var stats = character.GetAspect<amc>();
            var who = stats == null ? null : stats.aa();
            var game = bpo.GetService<cnk>();
            var party = game == null ? null : game.aqu();
            if (who == null || party == null || who.Passive) return false;
            var attitude = who.a(party.ac(), asp.a);
            return attitude == global::h.a || attitude == global::h.b;
        }
        catch { return false; }
    }

    // TEMPORARY diagnostics (UnderrailFix-diag.log), once a second.
    static DateTime diagNext;
    static readonly System.Text.StringBuilder diagFrame = new System.Text.StringBuilder();
    static int diagReverse, diagAdd, diagMarkedSeen, diagUnmarkedSeen;
    static void Diag(string line) { diagFrame.AppendLine(line); }
    static void DiagFlush(bool any)
    {
        if (DateTime.Now < diagNext) { diagFrame.Clear(); return; }
        diagNext = DateTime.Now.AddSeconds(1);
        try
        {
            System.IO.File.AppendAllText(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "UnderrailFix-diag.log"),
                DateTime.Now.ToString("HH:mm:ss") + " reverse-forced " + diagReverse + " marked-adds " + diagAdd + " outlines in pass: marked " + diagMarkedSeen + " unmarked " + diagUnmarkedSeen + "\n" + diagFrame);
        }
        catch { }
        diagFrame.Clear(); diagReverse = diagAdd = diagMarkedSeen = diagUnmarkedSeen = 0;
    }

    // --- Drawing --------------------------------------------------------------------------------------------------

    static RenderTarget2D mask, outline;
    static SpriteBatch batch;
    static Texture2D white;
    // Recolours what is already in the target to one colour, keeping its alpha (premultiplied: colour x alpha).
    static readonly BlendState Recolour = new BlendState
    {
        ColorSourceBlend = Blend.DestinationAlpha, ColorDestinationBlend = Blend.Zero,
        AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.One,
    };
    static readonly BlendState Multiply = new BlendState
    {
        ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.SourceAlpha,
        AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.SourceAlpha,
    };

    // Game members typed with XNA types are reached by reflection (see AGENTS.md, "XNA type identity").
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    // a2f.m is where a layer is drawn on screen, a2f.c its part of the texture; a2f.f3 passes them to chf.avw in that order.
    static readonly FieldInfo ZoomField = typeof(dnn).GetField("y");     // the world view's zoom (shares its name with a method)
    static readonly FieldInfo Device = typeof(cp9).GetField("c"), Placed = typeof(a2f).GetField("m"), Part = typeof(a2f).GetField("c"),
        Tint = typeof(a2f).GetField("e"), Rotation = typeof(a2f).GetField("i"), Flip = typeof(a2f).GetField("k"),
        Resources = typeof(cp9).GetField("a"), Clip = typeof(a7m).GetField("aq", Any),
        ScaleX = typeof(b0b).GetField("ac"), ScaleY = typeof(b0b).GetField("ad"), LayerId = typeof(a2f).GetField("a");     // fields share names with methods
    static readonly MethodInfo Resume = typeof(cp9).GetMethod("a", new[] { typeof(RasterizerState) }), End = typeof(cp9).GetMethod("a", Type.EmptyTypes),
        BeginBatchMethod = typeof(ob).GetMethod("w3"), DrawLayer = typeof(chf).GetMethod("avw"),
        ShaderEffect = typeof(cmk).GetMethod("d3"), ShaderParams = typeof(cmk).GetMethod("d4");

    static void Draw(a7m view, cp9 context, View state)
    {
        var g = (GraphicsDevice)Device.GetValue(context);
        int w = g.PresentationParameters.BackBufferWidth, h = g.PresentationParameters.BackBufferHeight;
        var work = new List<KeyValuePair<a2f, List<a2f>>>();
        foreach (var o in state.Masked)
        {
            var covering = Covering(o, state.Drawn, state.Creatures);
            if (covering.Count > 0) work.Add(new KeyValuePair<a2f, List<a2f>>(o, covering));
        }
        if (work.Count == 0) return;

        if (mask == null || mask.Width != w || mask.Height != h || mask.IsDisposed)
        {
            mask?.Dispose(); outline?.Dispose();
            mask = new RenderTarget2D(g, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
            outline = new RenderTarget2D(g, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.DiscardContents);
        }
        if (batch == null || batch.GraphicsDevice != g) { batch = new SpriteBatch(g); white = new Texture2D(g, 1, 1); white.SetData(new[] { Color.White }); }
        float opacity = 1f - FixSettings.ShowCoveredTransparency / 100f;

        var targets = g.GetRenderTargets();
        var viewport = g.Viewport;
        var scissor = g.ScissorRectangle;
        bool clipped = scissor.X != 0 || scissor.Y != 0;     // as a7m.b decides
        var clipState = clipped ? (RasterizerState)Clip.GetValue(view) : null;

        End.Invoke(context, null);     // end whichever batch is active, as a7m.b does before its own drawing
        ob game = context.b();
        cmk shader = b6l.a("Outline");
        var effect = shader == null ? null : (Effect)ShaderEffect.Invoke(shader, new object[] { Resources.GetValue(context), view });
        foreach (var item in work)
        {
            var chosen = EffectFor(item.Key.n);
            bool lines = chosen == FixSettings.CoveredEffect.Lines, dither = lines || chosen == FixSettings.CoveredEffect.Dither;
            bool fill = dither || chosen == FixSettings.CoveredEffect.Fill;
            // 1. Coverage: what is drawn after the creature, over it.
            g.SetRenderTarget(mask);
            g.Clear(Color.Transparent);
            BeginBatchMethod.Invoke(game, new object[] { SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null });
            foreach (var layer in item.Value) Silhouette(context, layer);
            game.an();

            // 2. The outline, as the game's shader pass draws it; or the creature itself in the outline's colour.
            g.SetRenderTarget(outline);
            g.Clear(Color.Transparent);
            if (fill)
            {
                BeginBatchMethod.Invoke(game, new object[] { SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, null });
                foreach (var layer in state.Drawn)
                    if (layer.n == item.Key.n && layer.d != null && layer.b && !NotBody(LayerId.GetValue(layer) as string)) Silhouette(context, layer);
                game.an();
                object colour;
                var c = item.Key.v != null && item.Key.v.TryGetValue("OutlineColor", out colour) && colour is Color ? (Color)colour : Color.White;
                batch.Begin(SpriteSortMode.Immediate, Recolour, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
                batch.Draw(white, new Rectangle(0, 0, w, h), c);
                batch.End();
            }
            else
            {
                BeginBatchMethod.Invoke(game, new object[] { SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone, effect });
                ShaderParams.Invoke(shader, new object[] { effect, item.Key, view });
                item.Key.f3(context, view);
                game.an();
            }

            // 3. Outline x coverage (x the dither pattern), then onto the screen.
            batch.Begin(SpriteSortMode.Immediate, Multiply, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            batch.Draw(mask, Vector2.Zero, Color.White);
            batch.End();
            if (dither)
            {
                // One pattern pixel = one world pixel at the current zoom (the game's zoom x the interface scale), so the
                // pattern keeps its size against the characters when zooming.
                int cell = Math.Max(1, (int)Math.Round((float)ZoomField.GetValue(view) * UiScaleFix.Scale));
                // Line fill drifts down slowly (whole screen pixels, so the lines stay sharp); the dots stay still.
                int period = (lines ? LineRows : 2) * cell, shift = lines ? (int)(Clock.Elapsed.TotalSeconds * LineSpeed * cell) % period : 0;
                var area = new Rectangle(0, shift - period, w, h + period);
                batch.Begin(SpriteSortMode.Immediate, Multiply, SamplerState.PointWrap, DepthStencilState.None, RasterizerState.CullNone);
                batch.Draw(Pattern(g, lines), area, new Rectangle(0, 0, (area.Width + cell - 1) / cell, (area.Height + cell - 1) / cell), Color.White);
                batch.End();
            }
            // Where the keyhole cut this frame, the covered effects are erased (soft, like the hole).
            if (Keyhole.Holes.Count > 0)
            {
                batch.Begin(SpriteSortMode.Immediate, Multiply, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
                foreach (var hole in Keyhole.Holes) batch.Draw(Keyhole.Erase(g), hole, Color.White);
                batch.End();
            }
            if (targets.Length == 0) g.SetRenderTarget(null); else g.SetRenderTargets(targets);
            g.Viewport = viewport;
            g.ScissorRectangle = scissor;
            batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, clipState ?? RasterizerState.CullNone);
            batch.Draw(outline, Vector2.Zero, Color.White * opacity);
            batch.End();
        }
        g.ScissorRectangle = scissor;
        Resume.Invoke(context, new object[] { clipState });     // as a7m.b ends: the normal batch again
    }

    // Line fill: one line in three (1 on, 2 off), drifting down at LineSpeed world pixels a second.
    const double LineSpeed = 3;
    const int LineRows = 3;
    static readonly System.Diagnostics.Stopwatch Clock = System.Diagnostics.Stopwatch.StartNew();

    static Texture2D LinePattern(GraphicsDevice g)
    {
        if (rows != null && !rows.IsDisposed) return rows;
        rows = new Texture2D(g, 1, LineRows);
        rows.SetData(new[] { Color.White, Color.Transparent, Color.Transparent });
        return rows;
    }

    static double Ease(double x) { x = Math.Max(0, Math.Min(1, x)); return x * x * (3 - 2 * x); }


    // Dithered fill: spaced-out dots, or with lines every other row; drawn tiled (PointWrap) with one
    // pattern pixel per `cell` screen pixels. ShowCoveredTransparency then applies to the pixels drawn.
    static Texture2D checker, rows;
    static Texture2D Pattern(GraphicsDevice g, bool lines)
    {
        if (lines) return LinePattern(g);
        if (checker == null || checker.IsDisposed)
        {
            // Spaced-out dots: two per 4x4 tile, at (0,0) and (2,2), each with a free pixel on every side.
            var dots = new Color[16];
            for (int i = 0; i < 16; i++) dots[i] = Color.Transparent;
            dots[0] = Color.White; dots[2 * 4 + 2] = Color.White;
            checker = new Texture2D(g, 4, 4);
            checker.SetData(dots);
        }
        return checker;
    }

    // A creature's layers that aren't its body: the game's marker, shield and shadow (as eg0 leaves them out of highlights).
    internal static void BeginBatch(ob batch, SpriteSortMode sort, RasterizerState rasterizer) { BeginBatchMethod.Invoke(batch, new object[] { sort, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, rasterizer, null }); }
    internal static bool NotBody(string id) { return id == "marker" || id == "shield" || id == "SHD"; }

    // The world layers drawn after the creature's last layer that overlap its outline (not its own layers).
    static List<a2f> Covering(a2f outlineLayer, List<a2f> drawn, HashSet<bym> creatures)
    {
        var result = new List<a2f>();
        var owner = outlineLayer.n;
        if (owner == null) return result;
        int last = -1;
        int own = 0;
        for (int i = 0; i < drawn.Count; i++) if (drawn[i].n == owner) { last = i; own++; }
        if (last < 0) { Diag("  outline " + outlineLayer.GetHashCode() + ": creature's layers not found among " + drawn.Count); return result; }
        var area = (Rectangle)Placed.GetValue(outlineLayer);
        area.Inflate(8, 8);
        for (int i = last + 1; i < drawn.Count; i++)
        {
            var layer = drawn[i];
            if (layer.n == owner || layer.d == null || !layer.b || (layer.n != null && creatures.Contains(layer.n))) continue;
            if (((Rectangle)Placed.GetValue(layer)).Intersects(area)) result.Add(layer);
        }
        Diag("  outline " + outlineLayer.GetHashCode() + " at " + area + ": own layers " + own + ", last at " + last + "/" + drawn.Count + ", covering " + result.Count);
        return result;
    }

    // A layer's shape in white (its own transparency kept), through the game's own draw path, as a2f.f3 draws it.
    internal static void Silhouette(cp9 context, a2f layer)
    {
        var tint = (Color?)Tint.GetValue(layer);
        var entity = layer.n == null ? null : layer.n.b;
        var scale = entity == null ? Vector2.One : new Vector2(Math.Max(0f, (float)ScaleX.GetValue(entity)), Math.Max(0f, (float)ScaleY.GetValue(entity)));
        DrawLayer.Invoke(layer.d, new object[] {
            context, Placed.GetValue(layer), Part.GetValue(layer), Color.White * ((tint?.A ?? 255) / 255f), Rotation.GetValue(layer),
            Vector2.Zero, Flip.GetValue(layer), 0f, (byte)(layer.n == null || !layer.n.f ? 1 : 2), scale });
    }
}

// Keyhole (a Show Covered effect for the player, hostile or other characters), after Neverwinter Nights: EE's
// "keyholing": a soft round hole, around each covered target, in the scenery standing in front of it.
// - Split: after the first target is drawn, the world pass continues into a front target of its own (the game's world
//   batch, restarted there with a7m.iu as by8 does, so lighting and shaders are unchanged).
// - Snapshot: when a later target (one drawn into the front target) is done, the front target around it is copied
//   aside: the target and everything behind it since the split, exactly as drawn (no character is drawn twice; their
//   composite sprites, by8, would rebuild their caches).
// - At the end of the pass (WorldDone), for each target that something standing in front of covers, in drawing order:
//   * behind: its snapshot (nothing for the first target: the screen has it), with the floor and ground drawn after
//     it in its circle on top (redrawn the world pass's way; plain tiles, cheap);
//   * mask: the scenery standing in front of it (not floor or ground, not creatures, not shadows or markers) inside
//     the circle, weighted by the hole's soft shape and KeyholeTransparency;
//   * the front target loses what the mask covers, and behind goes onto the screen through the mask.
//   Then the front target goes back onto the screen. Where two circles overlap, the later target's snapshot is used
//   in the overlap.
// KeyholeSize scales the circle (100 % = 0.85 x the character's height as radius). The circles cut are kept in Holes so
// ShowCovered.Draw erases the other effects there. Every clear and copy is limited to the circle.
static class Keyhole
{
    sealed class Target { public bym Owner; public Rectangle Body; public bool HasBody, Snapped; public int Last; }
    sealed class State
    {
        public bool Split; public int SplitAt = -1;
        public readonly List<Target> Targets = new List<Target>();
        public Target Open;     // the target whose layers are being drawn
        public RenderTargetBinding[] Screen; public Viewport Viewport;
    }
    static readonly System.Runtime.CompilerServices.ConditionalWeakTable<a7m, State> States = new System.Runtime.CompilerServices.ConditionalWeakTable<a7m, State>();
    static RenderTarget2D front, snap, behind, mask;
    static SpriteBatch batch;
    static Texture2D hole, erase, white;
    static int holeStrength = -1;
    static readonly RasterizerState Clipped = new RasterizerState { ScissorTestEnable = true, CullMode = CullMode.None };
    static readonly BlendState Multiply = new BlendState
    {
        ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.SourceAlpha,
        AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.SourceAlpha,
    };
    static readonly BlendState Cut = new BlendState
    {
        ColorSourceBlend = Blend.Zero, ColorDestinationBlend = Blend.InverseSourceAlpha,
        AlphaSourceBlend = Blend.Zero, AlphaDestinationBlend = Blend.InverseSourceAlpha,
    };
    static readonly FieldInfo Device = typeof(cp9).GetField("c"), Placed = typeof(a2f).GetField("m"), LayerId = typeof(a2f).GetField("a"),
        IsFloor = typeof(b0b).GetField("f"), IsGround = typeof(b0b).GetField("g");
    static readonly MethodInfo End = typeof(cp9).GetMethod("a", Type.EmptyTypes),
        Begin = typeof(a7m).GetMethod("iu", new[] { typeof(cp9), typeof(SpriteSortMode), typeof(BlendState), typeof(SamplerState), typeof(DepthStencilState), typeof(RasterizerState) });

    // The circles cut this frame (screen pixels), for ShowCovered.Draw to erase the covered effects there.
    internal static readonly List<Rectangle> Holes = new List<Rectangle>();

    static bool IsTarget(bym owner) { return owner != null && ShowCovered.EffectFor(owner) == FixSettings.CoveredEffect.Keyhole; }

    static Rectangle Circle(Target t, out Rectangle round)
    {
        var p = UiScaleFix.ToPixels(t.Body);
        int radius = Math.Max(16, (int)(p.Height * 0.85f * FixSettings.KeyholeSize / 100f));
        round = new Rectangle(p.Center.X - radius, p.Center.Y - radius, radius * 2, radius * 2);
        return Rectangle.Intersect(round, new Rectangle(0, 0, front.Width, front.Height));
    }

    // Every world layer, in drawing order (ShowCovered.Drawn).
    public static void Layer(a7m view, cp9 context, a2f layer, int index)
    {
        var s = States.GetOrCreateValue(view);
        var owner = layer.n;
        if (s.Open != null && s.Open.Owner == owner) { Grow(s.Open, layer, index); return; }
        if (s.Open != null) Closed(view, context, s);
        if (IsTarget(owner))
        {
            var t = s.Targets.Find(x => x.Owner == owner);
            if (t == null) { t = new Target { Owner = owner }; s.Targets.Add(t); }
            s.Open = t;
            Grow(t, layer, index);
            return;
        }
        if (s.Targets.Count == 0 || s.Split) return;
        // The first layer after the first target: everything from here on goes into the front target.
        var g = (GraphicsDevice)Device.GetValue(context);
        front = Sized(g, front);
        End.Invoke(context, null);
        s.Screen = g.GetRenderTargets();
        s.Viewport = g.Viewport;
        var scissor = g.ScissorRectangle;
        g.SetRenderTarget(front);
        g.Clear(Color.Transparent);
        g.ScissorRectangle = scissor;
        WorldBatch(view, context);
        s.Split = true;
        s.SplitAt = index;
    }

    static void Grow(Target t, a2f layer, int index)
    {
        t.Last = index;
        if (ShowCovered.NotBody(LayerId.GetValue(layer) as string)) return;     // the body only: not the shadow or markers
        var r = (Rectangle)Placed.GetValue(layer);
        t.Body = t.HasBody ? Rectangle.Union(t.Body, r) : r;
        t.HasBody = true;
    }

    // A target's layers are done: if it was drawn into the front target, copy the front target around it aside.
    static void Closed(a7m view, cp9 context, State s)
    {
        var t = s.Open;
        s.Open = null;
        if (!s.Split || !t.HasBody) return;
        Rectangle round, circle = Circle(t, out round);
        if (circle.Width <= 0 || circle.Height <= 0) return;
        var g = (GraphicsDevice)Device.GetValue(context);
        if (batch == null || batch.GraphicsDevice != g) Tools(g);
        snap = Sized(g, snap);
        var scissor = g.ScissorRectangle;
        End.Invoke(context, null);
        g.SetRenderTarget(snap);
        batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        batch.Draw(front, circle, circle, Color.White);
        batch.End();
        g.SetRenderTarget(front);
        g.ScissorRectangle = scissor;
        WorldBatch(view, context);
        t.Snapped = true;
    }

    static void Tools(GraphicsDevice g)
    {
        batch = new SpriteBatch(g);
        white = new Texture2D(g, 1, 1);
        white.SetData(new[] { Color.White });
    }

    static RenderTarget2D Sized(GraphicsDevice g, RenderTarget2D t)
    {
        int w = g.PresentationParameters.BackBufferWidth, h = g.PresentationParameters.BackBufferHeight;
        if (t != null && !t.IsDisposed && t.Width == w && t.Height == h) return t;
        t?.Dispose();
        // PreserveContents: these are switched away from and back to, and FNA clears DiscardContents targets then.
        return new RenderTarget2D(g, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
    }

    static void WorldBatch(a7m view, cp9 context)
    {
        Begin.Invoke(view, new object[] { context, SpriteSortMode.Deferred, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.Default, Clipped });
    }

    // Floor and ground (b0b.IsFloor / IsGround) are drawn in the same order as everything else, so many floor tiles
    // around a character come after it. They are never cover and are never cut.
    static bool Flat(a2f layer) { var e = layer.n == null ? null : layer.n.b; return e != null && ((bool)IsFloor.GetValue(e) || (bool)IsGround.GetValue(e)); }

    // Standing in front: not flat, not a creature, not a shadow or marker.
    static bool Standing(a7m view, a2f layer)
    {
        return !Flat(layer) && !ShowCovered.IsCreature(view, layer.n) && !ShowCovered.NotBody(LayerId.GetValue(layer) as string);
    }

    // a7m's world pass, after its loop (the world batch has been ended), right before it sets the screen again.
    public static void Done(a7m view, cp9 context)
    {
        State s;
        Holes.Clear();
        if (!States.TryGetValue(view, out s)) return;
        try
        {
            s.Open = null;
            if (!s.Split) return;
            var g = (GraphicsDevice)Device.GetValue(context);
            if (batch == null || batch.GraphicsDevice != g) Tools(g);
            var drawn = ShowCovered.DrawnLayers(view);
            if (drawn == null) return;
            // Each layer after the split: where it is (layout and pixels) and what it is, worked out once.
            int count = drawn.Count - s.SplitAt;
            if (count <= 0) return;
            var placed = new Rectangle[count];
            var pixels = new Rectangle[count];
            var kind = new byte[count];     // 0 skip, 1 flat, 2 standing, 3 other
            for (int i = 0; i < count; i++)
            {
                var layer = drawn[s.SplitAt + i];
                if (layer.d == null || !layer.b) continue;
                placed[i] = (Rectangle)Placed.GetValue(layer);
                pixels[i] = UiScaleFix.ToPixels(placed[i]);
                kind[i] = Flat(layer) ? (byte)1 : Standing(view, layer) ? (byte)2 : (byte)3;
            }
            s.Targets.Sort((a, b) => a.Last.CompareTo(b.Last));
            foreach (var t in s.Targets)
            {
                if (!t.HasBody) continue;
                Rectangle round, circle = Circle(t, out round);
                if (circle.Width <= 0 || circle.Height <= 0) continue;
                var cover = new List<a2f>();
                var floor = new List<a2f>();
                bool covered = false;
                for (int i = Math.Max(0, t.Last + 1 - s.SplitAt); i < count; i++)
                {
                    if (kind[i] == 0 || !pixels[i].Intersects(circle)) continue;
                    var layer = drawn[s.SplitAt + i];
                    if (kind[i] == 1) floor.Add(layer);
                    else if (kind[i] == 2 && layer.n != t.Owner) { cover.Add(layer); if (placed[i].Intersects(t.Body)) covered = true; }
                }
                if (!covered) continue;
                Cut1(view, context, g, s, t, floor, cover, round, circle);
                Holes.Add(round);
            }
            Restore(g, s);
            batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            batch.Draw(front, Vector2.Zero, Color.White);
            batch.End();
        }
        finally { s.Split = false; s.SplitAt = -1; s.Targets.Clear(); s.Open = null; s.Screen = null; }
    }

    static void Clear(Rectangle area)
    {
        batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        batch.Draw(white, area, Color.Transparent);
        batch.End();
    }

    static void Cut1(a7m view, cp9 context, GraphicsDevice g, State s, Target t, List<a2f> floor, List<a2f> cover, Rectangle round, Rectangle circle)
    {
        behind = Sized(g, behind);
        mask = Sized(g, mask);

        // Behind: the target and what is behind it (its snapshot), and the floor in front of it, redrawn on top.
        g.SetRenderTarget(behind);
        Clear(circle);
        if (t.Snapped)
        {
            batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
            batch.Draw(snap, circle, circle, Color.White);
            batch.End();
        }
        if (floor.Count > 0)
        {
            g.ScissorRectangle = circle;
            WorldBatch(view, context);
            foreach (var layer in floor) layer.f3(context, view);
            End.Invoke(context, null);
        }

        // Mask: what stands in front, inside the circle, weighted by the hole (strongest at the centre).
        g.SetRenderTarget(mask);
        Clear(circle);
        g.ScissorRectangle = circle;
        ob game = context.b();
        ShowCovered.BeginBatch(game, SpriteSortMode.Deferred, Clipped);
        foreach (var layer in cover) ShowCovered.Silhouette(context, layer);
        game.an();
        batch.Begin(SpriteSortMode.Immediate, Multiply, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
        batch.Draw(Hole(g), round, Color.White);
        batch.End();

        // Behind x mask, onto the screen; the front target loses what the mask covers.
        g.SetRenderTarget(behind);
        batch.Begin(SpriteSortMode.Immediate, Multiply, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        batch.Draw(mask, circle, circle, Color.White);
        batch.End();
        Restore(g, s);
        batch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        batch.Draw(behind, circle, circle, Color.White);
        batch.End();
        g.SetRenderTarget(front);
        batch.Begin(SpriteSortMode.Immediate, Cut, SamplerState.PointClamp, DepthStencilState.None, RasterizerState.CullNone);
        batch.Draw(mask, circle, circle, Color.White);
        batch.End();
    }

    static void Restore(GraphicsDevice g, State s)
    {
        if (s.Screen == null || s.Screen.Length == 0) g.SetRenderTarget(null); else g.SetRenderTargets(s.Screen);
        g.Viewport = s.Viewport;
    }

    // Alpha = how much of a covered effect stays: 0 at the centre, rising smoothly to 1 at the edge (the hole's shape).
    internal static Texture2D Erase(GraphicsDevice g)
    {
        if (erase != null && !erase.IsDisposed) return erase;
        erase = Round(g, t => t);
        return erase;
    }

    // Alpha = how much of what stands in front is removed: KeyholeTransparency at the centre, falling smoothly to 0 at
    // the edge.
    static Texture2D Hole(GraphicsDevice g)
    {
        int strength = FixSettings.KeyholeTransparency;
        if (hole != null && !hole.IsDisposed && holeStrength == strength) return hole;
        hole?.Dispose();
        float see = strength / 100f;
        hole = Round(g, t => see * (1 - t));
        holeStrength = strength;
        return hole;
    }

    // A round texture: value(t), t = 0 inside 55 % of the radius rising smoothly to 1 at the edge.
    static Texture2D Round(GraphicsDevice g, Func<float, float> value)
    {
        const int size = 128;
        var data = new Color[size * size];
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x + 0.5f) / size * 2 - 1, dy = (y + 0.5f) / size * 2 - 1;
                float t = Math.Max(0f, Math.Min(1f, ((float)Math.Sqrt(dx * dx + dy * dy) - 0.55f) / 0.45f));
                float v = value(t * t * (3 - 2 * t));
                data[y * size + x] = new Color(v, v, v, v);
            }
        var texture = new Texture2D(g, size, size);
        texture.SetData(data);
        return texture;
    }
}
