// Save screenshots (tb.Draw, when a save is made): the game renders the screen into a picture of at most 2048 pixels
// a side, shrinking by 2048 / the longer side. It does that by setting the full-screen viewport on the smaller
// target, which doesn't scale anything: only the top-left 2048 pixels of the screen end up in the picture.
// With ResolutionFix and SaveScreenshots the screen is rendered at full size (Limit) and the picture is what a
// 1920x1080 screen would show (Shrink): the game is made for 1080p, the load menu fits the picture in a box (a 32:9
// screen shrunk whole would be a thin strip), and the view at a given zoom is the same as at 1080p (ZoomFix).
// Screens up to 2048 pixels never went through this and still don't.
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

public static class SaveScreenshot
{
    const int Max = 2048;
    static bool On => FixSettings.ResolutionFix && FixSettings.SaveScreenshots;

    // The two "> 2048" checks in tb.Draw: with the fix the target stays screen-sized and the viewport alone.
    public static int Limit(int limit) { return On ? int.MaxValue : limit; }

    // tb.Draw stores the rendered target as the screenshot (tb.x); called on it, while it is still the render target.
    public static Texture2D Shrink(Texture2D shot)
    {
        if (shot == null || !On || (shot.Width <= Max && shot.Height <= Max)) return shot;
        try
        {
            // The largest 16:9 area at the centre, at 1080p's size (never enlarged).
            int rw = shot.Width, rh = shot.Height;
            if (rw * 9 >= rh * 16) rw = rh * 16 / 9; else rh = rw * 9 / 16;
            var source = new Rectangle((shot.Width - rw) / 2, (shot.Height - rh) / 2, rw, rh);
            float k = Math.Min(1f, 1080f / rh);
            int w = (int)Math.Round(rw * k), h = (int)Math.Round(rh * k);
            var device = shot.GraphicsDevice;
            var small = new RenderTarget2D(device, w, h, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);
            device.SetRenderTarget(small);
            device.Clear(Color.Black);
            using (var batch = new SpriteBatch(device))
            {
                batch.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, DepthStencilState.None, RasterizerState.CullNone);
                batch.Draw(shot, new Rectangle(0, 0, w, h), source, Color.White);
                batch.End();
            }
            shot.Dispose();
            return small;
        }
        catch (Exception ex) { Log.Write("screenshot: " + ex.Message); return shot; }
    }
}
