using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
namespace LabUi;

internal sealed class MotionBanner : Control
{
    private Image? art; private readonly System.Windows.Forms.Timer timer = new() { Interval = 125 };
    private int frames, index; private bool playing; public bool Playing => playing;
    public MotionBanner() { DoubleBuffered = true; SetStyle(ControlStyles.ResizeRedraw, true); timer.Tick += (_, _) => { if (art is null || frames < 2) return; index = (index + 1) % frames; art.SelectActiveFrame(FrameDimension.Time, index); Invalidate(); }; }
    public void LoadArt(string gifPath, string posterPath, bool animate = true)
    {
        timer.Stop(); art?.Dispose(); art = null; index = 0;
        var path = animate && File.Exists(gifPath) ? gifPath : posterPath;
        if (!File.Exists(path)) return;
        art = Image.FromFile(path); frames = ImageAnimator.CanAnimate(art) ? art.GetFrameCount(FrameDimension.Time) : 1;
        playing = animate && frames > 1; if (playing) timer.Start(); Invalidate();
    }
    public void Toggle() { playing = !playing && frames > 1; if (playing) timer.Start(); else timer.Stop(); Invalidate(); }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e); e.Graphics.SmoothingMode = SmoothingMode.AntiAlias; e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
        using var brush = new LinearGradientBrush(ClientRectangle, Color.FromArgb(254, 241, 222), Color.FromArgb(237, 228, 250), 0f); e.Graphics.FillRectangle(brush, ClientRectangle);
        if (art is not null) { var scale = Math.Max((float)Width / art.Width, (float)Height / art.Height); var w = art.Width * scale; var h = art.Height * scale; e.Graphics.DrawImage(art, (Width - w) / 2, (Height - h) / 2, w, h); }
    }
    protected override void Dispose(bool disposing) { if (disposing) { timer.Dispose(); art?.Dispose(); } base.Dispose(disposing); }
}
