using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ModernCalculator
{
    /// <summary>
    /// Neumorphic rounded button — soft raised/pressed shadow effect.
    /// </summary>
    internal class RoundButton : Button
    {
        public enum ButtonStyle { Normal, Operator, Equals, Clear, Scientific, Function }

        private ButtonStyle _style = ButtonStyle.Normal;
        public ButtonStyle Style
        {
            get => _style;
            set { _style = value; ApplyStyle(); Invalidate(); }
        }

        private int _cornerRadius = ThemeColors.ButtonRadius;
        public int CornerRadius
        {
            get => _cornerRadius;
            set { _cornerRadius = value; Invalidate(); }
        }

        // ── Animation ──────────────────────────────────────────────────────
        private float _hoverAlpha = 0f;
        private float _pressAlpha = 0f;
        private bool  _isHovered  = false;
        private bool  _isPressed  = false;
        private readonly System.Windows.Forms.Timer _animTimer;

        // ── Style colours ──────────────────────────────────────────────────
        private Color _fillColor;

        public RoundButton()
        {
            SetStyle(ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.AllPaintingInWmPaint  |
                     ControlStyles.UserPaint             |
                     ControlStyles.ResizeRedraw, true);

            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            FlatAppearance.MouseOverBackColor = Color.Transparent;
            FlatAppearance.MouseDownBackColor = Color.Transparent;
            Cursor    = Cursors.Hand;
            Font      = ThemeColors.FontButton;
            ForeColor = ThemeColors.TextPrimary;
            BackColor = ThemeColors.AppBackground;

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 };
            _animTimer.Tick += OnAnimTick;

            ApplyStyle();
        }

        private void ApplyStyle()
        {
            (_fillColor, ForeColor) = _style switch
            {
                ButtonStyle.Operator   => (ThemeColors.AccentBlue,    ThemeColors.TextOperator),
                ButtonStyle.Equals     => (ThemeColors.AccentPurple,   ThemeColors.TextOperator),
                ButtonStyle.Clear      => (ThemeColors.ClearRed,       ThemeColors.TextOperator),
                ButtonStyle.Scientific => (ThemeColors.AccentOrange,   ThemeColors.TextOperator),
                ButtonStyle.Function   => (ThemeColors.ButtonFunction, ThemeColors.TextSecondary),
                _                      => (ThemeColors.ButtonBase,     ThemeColors.TextPrimary),
            };
        }

        // ── Animation tick ─────────────────────────────────────────────────
        private void OnAnimTick(object? sender, EventArgs e)
        {
            float step = 0.12f;
            float ph = _hoverAlpha, pp = _pressAlpha;

            _hoverAlpha = _isHovered ? Math.Min(1f, _hoverAlpha + step) : Math.Max(0f, _hoverAlpha - step);
            _pressAlpha = _isPressed ? Math.Min(1f, _pressAlpha + step*2) : Math.Max(0f, _pressAlpha - step*2);

            if (Math.Abs(_hoverAlpha - ph) > 0.001f || Math.Abs(_pressAlpha - pp) > 0.001f)
                Invalidate();
            else if (!_isHovered && !_isPressed && _hoverAlpha <= 0 && _pressAlpha <= 0)
                _animTimer.Stop();
        }

        protected override void OnMouseEnter(EventArgs e) { _isHovered = true;  _animTimer.Start(); base.OnMouseEnter(e); }
        protected override void OnMouseLeave(EventArgs e) { _isHovered = false; _animTimer.Start(); base.OnMouseLeave(e); }
        protected override void OnMouseDown(MouseEventArgs e) { _isPressed = true;  _animTimer.Start(); base.OnMouseDown(e); }
        protected override void OnMouseUp(MouseEventArgs e)   { _isPressed = false; _animTimer.Start(); base.OnMouseUp(e); }

        // ── Paint ──────────────────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.PixelOffsetMode   = PixelOffsetMode.HighQuality;

            // Clear with background so corners blend with form
            g.Clear(ThemeColors.AppBackground);

            const int sh = 4;   // shadow margin
            var pill = new Rectangle(sh, sh, Width - sh * 2, Height - sh * 2);
            int r = _cornerRadius;
            bool pressed = _isPressed || _pressAlpha > 0.5f;

            // ── Neumorphic shadows ─────────────────────────────────────────
            if (!pressed)
            {
                // Dark shadow — bottom-right offset
                DrawShadow(g, pill, r, +4, ThemeColors.ShadowDark, 22);
                DrawShadow(g, pill, r, +3, ThemeColors.ShadowDark, 42);
                DrawShadow(g, pill, r, +2, ThemeColors.ShadowDark, 65);
                DrawShadow(g, pill, r, +1, ThemeColors.ShadowDark, 90);

                // White highlight — top-left offset (clipped at control edge)
                DrawShadow(g, pill, r, -4, ThemeColors.ShadowLight, 60);
                DrawShadow(g, pill, r, -3, ThemeColors.ShadowLight, 100);
                DrawShadow(g, pill, r, -2, ThemeColors.ShadowLight, 150);
                DrawShadow(g, pill, r, -1, ThemeColors.ShadowLight, 210);
            }
            else
            {
                // Pressed: invert (dark top-left, light bottom-right)
                DrawShadow(g, pill, r, -2, ThemeColors.ShadowDark,  80);
                DrawShadow(g, pill, r, -1, ThemeColors.ShadowDark, 110);
                DrawShadow(g, pill, r, +2, ThemeColors.ShadowLight, 120);
                DrawShadow(g, pill, r, +1, ThemeColors.ShadowLight, 160);
            }

            // ── Fill pill ─────────────────────────────────────────────────
            Color fill = _fillColor;
            if (pressed)
                fill = Darken(fill, 18);
            else if (_hoverAlpha > 0)
                fill = Lerp(fill, Lighten(fill, 12), _hoverAlpha * 0.6f);

            using (var path = RoundedRect(pill, r))
            using (var brush = new SolidBrush(fill))
                g.FillPath(brush, path);

            // ── Top-half gradient overlay (gives 3-D depth) ────────────────
            using (var clipPath = RoundedRect(pill, r))
            {
                g.SetClip(clipPath);
                var grad = new Rectangle(pill.X, pill.Y, pill.Width, pill.Height);
                using var gb = new LinearGradientBrush(
                    new PointF(grad.Left, grad.Top),
                    new PointF(grad.Left, grad.Bottom),
                    Color.FromArgb(pressed ? 40 : 65, pressed ? ThemeColors.ShadowDark : Color.White),
                    Color.Transparent);
                g.FillRectangle(gb, grad);
                g.ResetClip();
            }

            // ── Pill border ───────────────────────────────────────────────
            using (var path = RoundedRect(pill, r))
            using (var pen = new Pen(Color.FromArgb(pressed ? 55 : 30, ThemeColors.ShadowDark), 1f))
                g.DrawPath(pen, path);

            // ── Text ──────────────────────────────────────────────────────
            var sf = new StringFormat
            {
                Alignment     = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                FormatFlags   = StringFormatFlags.NoWrap,
                Trimming      = StringTrimming.EllipsisCharacter
            };
            using var tb = new SolidBrush(ForeColor);
            g.DrawString(Text, Font, tb, new RectangleF(0, 0, Width, Height), sf);
        }

        // ── Helpers ────────────────────────────────────────────────────────
        private static void DrawShadow(Graphics g, Rectangle pill, int r, int offset, Color color, int alpha)
        {
            var rect = new Rectangle(pill.X + offset, pill.Y + offset, pill.Width, pill.Height);
            using var path = RoundedRect(rect, r);
            using var brush = new SolidBrush(Color.FromArgb(Math.Clamp(alpha, 0, 255), color));
            g.FillPath(brush, path);
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            // Clamp to prevent negative arc size
            d = Math.Min(d, Math.Min(Math.Abs(r.Width), Math.Abs(r.Height)));
            if (d < 2) d = 2;
            var path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        private static Color Lerp(Color a, Color b, float t)
        {
            t = Math.Clamp(t, 0, 1);
            return Color.FromArgb(
                (int)(a.A + (b.A - a.A) * t),
                (int)(a.R + (b.R - a.R) * t),
                (int)(a.G + (b.G - a.G) * t),
                (int)(a.B + (b.B - a.B) * t));
        }

        private static Color Darken(Color c, int v) =>
            Color.FromArgb(c.A, Math.Max(0, c.R-v), Math.Max(0, c.G-v), Math.Max(0, c.B-v));

        private static Color Lighten(Color c, int v) =>
            Color.FromArgb(c.A, Math.Min(255, c.R+v), Math.Min(255, c.G+v), Math.Min(255, c.B+v));

        protected override void Dispose(bool disposing)
        {
            if (disposing) _animTimer.Dispose();
            base.Dispose(disposing);
        }
    }
}
