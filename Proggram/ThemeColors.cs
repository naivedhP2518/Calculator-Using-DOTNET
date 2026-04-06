using System.Drawing;

namespace ModernCalculator
{
    internal static class ThemeColors
    {
        // ── Backgrounds ────────────────────────────────────────────────────
        public static readonly Color AppBackground  = Color.FromArgb(228, 232, 240);
        public static readonly Color TitleBarBg     = Color.FromArgb(215, 221, 232);
        public static readonly Color DisplayBg      = Color.FromArgb(240, 244, 250);
        public static readonly Color HistoryBg      = Color.FromArgb(228, 232, 240);

        // ── Button surfaces ────────────────────────────────────────────────
        public static readonly Color ButtonBase     = Color.FromArgb(228, 232, 240);
        public static readonly Color ButtonFunction = Color.FromArgb(218, 224, 234);
        public static readonly Color ButtonHover    = Color.FromArgb(235, 239, 246);

        // ── Neumorphic shadows ─────────────────────────────────────────────
        public static readonly Color ShadowDark     = Color.FromArgb(163, 177, 198);
        public static readonly Color ShadowLight    = Color.FromArgb(255, 255, 255);

        // ── Accents ────────────────────────────────────────────────────────
        public static readonly Color AccentBlue        = Color.FromArgb(100, 149, 210);
        public static readonly Color AccentBlueHover   = Color.FromArgb(82, 132, 196);
        public static readonly Color AccentPurple      = Color.FromArgb(130, 100, 210);
        public static readonly Color AccentPurpleHover = Color.FromArgb(112, 84, 196);
        public static readonly Color AccentOrange      = Color.FromArgb(225, 140, 70);
        public static readonly Color AccentOrangeHover = Color.FromArgb(208, 124, 56);
        public static readonly Color ClearRed          = Color.FromArgb(220, 88, 100);
        public static readonly Color ClearRedHover     = Color.FromArgb(204, 72, 86);

        // ── Glow ───────────────────────────────────────────────────────────
        public static readonly Color GlowBlue   = Color.FromArgb(80, 100, 149, 210);
        public static readonly Color GlowPurple = Color.FromArgb(80, 130, 100, 210);

        // ── Text ───────────────────────────────────────────────────────────
        public static readonly Color TextPrimary   = Color.FromArgb(45, 60, 85);
        public static readonly Color TextSecondary = Color.FromArgb(110, 125, 150);
        public static readonly Color TextOperator  = Color.White;
        public static readonly Color TextGhost     = Color.FromArgb(170, 180, 200);

        // ── Borders ────────────────────────────────────────────────────────
        public static readonly Color BorderColor = Color.FromArgb(195, 205, 220);

        // ── Fonts ──────────────────────────────────────────────────────────
        public static readonly Font FontDisplay    = new Font("Segoe UI", 28, FontStyle.Regular);
        public static readonly Font FontExpression = new Font("Segoe UI", 11, FontStyle.Regular);
        public static readonly Font FontButton     = new Font("Segoe UI", 13, FontStyle.Regular);
        public static readonly Font FontScientific = new Font("Segoe UI", 10, FontStyle.Regular);
        public static readonly Font FontHistory    = new Font("Segoe UI", 10, FontStyle.Regular);
        public static readonly Font FontTitle      = new Font("Segoe UI", 10, FontStyle.Bold);

        // ── Radii ──────────────────────────────────────────────────────────
        public const int ButtonRadius  = 20;
        public const int DisplayRadius = 14;
        public const int WindowRadius  = 18;
    }
}
