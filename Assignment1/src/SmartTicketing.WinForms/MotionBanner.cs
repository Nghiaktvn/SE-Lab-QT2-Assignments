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
        if (Width > 4 && Height > 4)
        {
            var frame = new Rectangle(2, 2, Width - 5, Height - 5);
            using var edge = new LinearGradientBrush(frame, Color.White, Color.White, frames > 1 ? 360f * index / frames : 0f);
            edge.InterpolationColors = new ColorBlend { Colors = [Color.FromArgb(220, 244, 126, 137), Color.FromArgb(220, 245, 178, 106), Color.FromArgb(220, 232, 218, 126), Color.FromArgb(220, 124, 198, 149), Color.FromArgb(220, 120, 177, 223), Color.FromArgb(220, 157, 145, 218), Color.FromArgb(220, 231, 144, 192)], Positions = [0f, .16f, .33f, .5f, .67f, .83f, 1f] };
            using var pen = new Pen(edge, 3f);
            e.Graphics.DrawRectangle(pen, frame);
        }
    }
    protected override void Dispose(bool disposing) { if (disposing) { timer.Dispose(); art?.Dispose(); } base.Dispose(disposing); }
}
