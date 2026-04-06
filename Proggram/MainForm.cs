using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace ModernCalculator
{
    /// <summary>
    /// Main calculator window — borderless, draggable, with dark glassmorphism theme.
    /// Supports: Standard & Scientific modes, History panel, Keyboard input, Memory.
    /// </summary>
    public partial class MainForm : Form
    {
        // ── Engine ─────────────────────────────────────────────────────────
        private readonly CalculatorEngine _engine = new();

        // ── Layout constants ───────────────────────────────────────────────
        private const int TitleH      = 44;
        private const int DisplayH    = 110;
        private const int BtnW        = 68;
        private const int BtnH        = 58;
        private const int Gap         = 8;
        private const int PadX        = 12;
        private const int PadY        = 12;
        private const int ColsStd     = 4;
        private const int ColsSci     = 5;
        private const int RowsStd     = 6;
        private const int HistoryW    = 230;

        private bool _sciMode         = false;
        private bool _historyVisible  = false;

        // ── Drag ───────────────────────────────────────────────────────────
        private Point _dragStart;
        private bool  _dragging;

        // ── Controls ───────────────────────────────────────────────────────
        private Label  _lblExpression = null!;
        private Label  _lblDisplay    = null!;
        private Label  _lblMemory     = null!;
        private Panel  _displayPanel  = null!;
        private Panel  _titleBar      = null!;
        private Panel  _buttonPanel   = null!;
        private Panel  _historyPanel  = null!;
        private ListBox _historyList  = null!;
        private Label  _lblTitle      = null!;
        private RoundButton _btnSciToggle = null!;
        private RoundButton _btnHistToggle = null!;

        private readonly List<RoundButton> _buttons = new();

        // ── Constructor ────────────────────────────────────────────────────
        public MainForm()
        {
            InitializeComponent();
            BuildUI();
            UpdateDisplay();
        }

        // ── UI Construction ────────────────────────────────────────────────
        private void BuildUI()
        {
            SuspendLayout();

            // Window
            FormBorderStyle = FormBorderStyle.None;
            BackColor       = ThemeColors.AppBackground;
            ForeColor       = ThemeColors.TextPrimary;
            StartPosition   = FormStartPosition.CenterScreen;
            DoubleBuffered  = true;

            SetWindowSize();

            // ── Title bar ─────────────────────────────────────────────────
            _titleBar = new Panel
            {
                BackColor = ThemeColors.TitleBarBg,
                Dock      = DockStyle.Top,
                Height    = TitleH,
            };
            _titleBar.MouseDown += TitleBar_MouseDown;
            _titleBar.MouseMove += TitleBar_MouseMove;
            _titleBar.MouseUp   += TitleBar_MouseUp;
            _titleBar.Paint     += TitleBar_Paint;
            Controls.Add(_titleBar);

            // Title label
            _lblTitle = new Label
            {
                Text      = "✦ CALCULATOR",
                Font      = ThemeColors.FontTitle,
                ForeColor = ThemeColors.TextSecondary,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Left      = 14, Top = 0,
                Width     = 160, Height = TitleH,
                BackColor = Color.Transparent
            };
            _titleBar.Controls.Add(_lblTitle);

            // Close button
            var btnClose = MakeTitleBtn("✕", Color.FromArgb(220, 50, 50));
            btnClose.Click += (s, e) => Application.Exit();
            _titleBar.Controls.Add(btnClose);

            // Minimise button
            var btnMin = MakeTitleBtn("─", Color.FromArgb(60, 180, 60));
            btnMin.Left = btnClose.Left - 34;
            btnMin.Click += (s, e) => WindowState = FormWindowState.Minimized;
            _titleBar.Controls.Add(btnMin);

            // Mode toggles
            _btnSciToggle = new RoundButton
            {
                Text     = "SCI",
                Style    = RoundButton.ButtonStyle.Function,
                Font     = new Font("Segoe UI", 9, FontStyle.Bold),
                Size     = new Size(46, 26),
                Left     = btnMin.Left - 56, Top = (TitleH - 26) / 2,
            };
            _btnSciToggle.Click += (s, e) => ToggleScientific();
            _titleBar.Controls.Add(_btnSciToggle);

            _btnHistToggle = new RoundButton
            {
                Text  = "HIST",
                Style = RoundButton.ButtonStyle.Function,
                Font  = new Font("Segoe UI", 9, FontStyle.Bold),
                Size  = new Size(46, 26),
                Left  = _btnSciToggle.Left - 54, Top = (TitleH - 26) / 2,
            };
            _btnHistToggle.Click += (s, e) => ToggleHistory();
            _titleBar.Controls.Add(_btnHistToggle);

            // ── Display panel ─────────────────────────────────────────────
            _displayPanel = new Panel
            {
                BackColor = ThemeColors.DisplayBg,
                Left      = PadX,
                Top       = TitleH + PadY,
                Height    = DisplayH,
            };
            _displayPanel.Paint += DisplayPanel_Paint;
            Controls.Add(_displayPanel);

            _lblExpression = new Label
            {
                Font      = ThemeColors.FontExpression,
                ForeColor = ThemeColors.TextSecondary,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize  = false,
                Dock      = DockStyle.None,
                Left      = 8, Top = 8,
                Height    = 22,
                BackColor = Color.Transparent,
            };
            _displayPanel.Controls.Add(_lblExpression);

            _lblDisplay = new Label
            {
                Font      = ThemeColors.FontDisplay,
                ForeColor = ThemeColors.TextPrimary,
                TextAlign = ContentAlignment.MiddleRight,
                AutoSize  = false,
                Dock      = DockStyle.None,
                Left      = 8, Top = 30,
                Height    = 56,
                BackColor = Color.Transparent,
            };
            _displayPanel.Controls.Add(_lblDisplay);

            _lblMemory = new Label
            {
                Font      = new Font("Segoe UI", 9, FontStyle.Italic),
                ForeColor = ThemeColors.AccentBlue,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize  = false,
                Left      = 10, Top = DisplayH - 22,
                Height    = 18,
                BackColor = Color.Transparent,
                Visible   = false,
            };
            _displayPanel.Controls.Add(_lblMemory);

            // ── Button panel ──────────────────────────────────────────────
            _buttonPanel = new Panel
            {
                BackColor = Color.Transparent,
                Left      = PadX,
                Top       = TitleH + PadY + DisplayH + PadY,
            };
            Controls.Add(_buttonPanel);

            BuildButtons();
            PositionButtons();

            // ── History panel ─────────────────────────────────────────────
            _historyPanel = new Panel
            {
                BackColor = ThemeColors.HistoryBg,
                Width     = HistoryW,
                Visible   = false,
            };
            _historyPanel.Paint += HistoryPanel_Paint;
            Controls.Add(_historyPanel);

            // History header: title + close button
            var histHeader = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 42,
                BackColor = Color.Transparent,
            };

            var histTitle = new Label
            {
                Text      = "📋  HISTORY",
                Font      = ThemeColors.FontTitle,
                ForeColor = ThemeColors.TextSecondary,
                AutoSize  = false,
                TextAlign = ContentAlignment.MiddleLeft,
                Left = 10, Top = 0,
                Width = 160, Height = 42,
                BackColor = Color.Transparent,
            };
            histHeader.Controls.Add(histTitle);

            // ✕ Close (Fechar) button inside history panel
            var btnCloseHist = new Button
            {
                Text      = "✕",
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(28, 28),
                BackColor = Color.Transparent,
                ForeColor = ThemeColors.TextSecondary,
                Font      = new Font("Segoe UI", 10),
                Cursor    = Cursors.Hand,
                Left      = HistoryW - 36,
                Top       = (42 - 28) / 2,
            };
            btnCloseHist.FlatAppearance.BorderSize = 0;
            btnCloseHist.FlatAppearance.MouseOverBackColor = Color.FromArgb(220, 88, 100);
            btnCloseHist.FlatAppearance.MouseDownBackColor = Color.FromArgb(180, 60, 75);
            btnCloseHist.Click += (s, ev) => ToggleHistory();
            histHeader.Controls.Add(btnCloseHist);

            _historyPanel.Controls.Add(histHeader);

            // Separator line below header
            var histSep = new Panel
            {
                Dock      = DockStyle.Top,
                Height    = 1,
                BackColor = ThemeColors.BorderColor,
            };
            _historyPanel.Controls.Add(histSep);

            _historyList = new ListBox
            {
                BackColor       = ThemeColors.HistoryBg,
                ForeColor       = ThemeColors.TextPrimary,
                Font            = ThemeColors.FontHistory,
                BorderStyle     = BorderStyle.None,
                DrawMode        = DrawMode.OwnerDrawFixed,
                ItemHeight      = 44,
                SelectionMode   = SelectionMode.None,
                Dock            = DockStyle.Fill,
            };
            _historyList.DrawItem += HistoryList_DrawItem;
            _historyPanel.Controls.Add(_historyList);

            var btnClearHist = new RoundButton
            {
                Text   = "Clear History",
                Style  = RoundButton.ButtonStyle.Clear,
                Font   = new Font("Segoe UI", 9),
                Height = 32,
                Dock   = DockStyle.Bottom,
            };
            btnClearHist.Click += (s, e) =>
            {
                _engine.ClearHistory();
                RefreshHistory();
            };
            _historyPanel.Controls.Add(btnClearHist);

            // Keyboard
            KeyPreview = true;
            KeyDown   += MainForm_KeyDown;

            ResumeLayout(false);
            PerformLayout();
            SizeControls();
        }

        // ── Button grid ───────────────────────────────────────────────────
        private void BuildButtons()
        {
            _buttons.Clear();
            _buttonPanel.Controls.Clear();

            if (_sciMode)
                BuildScientificButtons();
            else
                BuildStandardButtons();
        }

        private void BuildStandardButtons()
        {
            var defs = new (string text, string value, RoundButton.ButtonStyle style)[]
            {
                // Row 0 — Memory
                ("MC", "mc", RoundButton.ButtonStyle.Function),
                ("MR", "mr", RoundButton.ButtonStyle.Function),
                ("M+", "m+", RoundButton.ButtonStyle.Function),
                ("MS", "ms", RoundButton.ButtonStyle.Function),
                // Row 1
                ("%",  "%",  RoundButton.ButtonStyle.Function),
                ("CE", "ce", RoundButton.ButtonStyle.Function),
                ("C",  "c",  RoundButton.ButtonStyle.Clear),
                ("⌫",  "bs", RoundButton.ButtonStyle.Operator),
                // Row 2
                ("1/x","inv",RoundButton.ButtonStyle.Function),
                ("x²", "sq", RoundButton.ButtonStyle.Function),
                ("√",  "sqrt",RoundButton.ButtonStyle.Function),
                ("÷",  "/",  RoundButton.ButtonStyle.Operator),
                // Row 3
                ("7",  "7",  RoundButton.ButtonStyle.Normal),
                ("8",  "8",  RoundButton.ButtonStyle.Normal),
                ("9",  "9",  RoundButton.ButtonStyle.Normal),
                ("×",  "*",  RoundButton.ButtonStyle.Operator),
                // Row 4
                ("4",  "4",  RoundButton.ButtonStyle.Normal),
                ("5",  "5",  RoundButton.ButtonStyle.Normal),
                ("6",  "6",  RoundButton.ButtonStyle.Normal),
                ("−",  "-",  RoundButton.ButtonStyle.Operator),
                // Row 5
                ("1",  "1",  RoundButton.ButtonStyle.Normal),
                ("2",  "2",  RoundButton.ButtonStyle.Normal),
                ("3",  "3",  RoundButton.ButtonStyle.Normal),
                ("+",  "+",  RoundButton.ButtonStyle.Operator),
                // Row 6
                ("+/−","neg",RoundButton.ButtonStyle.Function),
                ("0",  "0",  RoundButton.ButtonStyle.Normal),
                (".",  ".",  RoundButton.ButtonStyle.Normal),
                ("=",  "=",  RoundButton.ButtonStyle.Equals),
            };

            foreach (var (text, value, style) in defs)
                AddButton(text, value, style);
        }

        private void BuildScientificButtons()
        {
            var defs = new (string text, string value, RoundButton.ButtonStyle style)[]
            {
                // Row 0
                ("sin","sin",RoundButton.ButtonStyle.Scientific),
                ("cos","cos",RoundButton.ButtonStyle.Scientific),
                ("tan","tan",RoundButton.ButtonStyle.Scientific),
                ("MC", "mc", RoundButton.ButtonStyle.Function),
                ("MR", "mr", RoundButton.ButtonStyle.Function),
                // Row 1
                ("log","log",RoundButton.ButtonStyle.Scientific),
                ("ln", "ln", RoundButton.ButtonStyle.Scientific),
                ("eˣ","exp",RoundButton.ButtonStyle.Scientific),
                ("M+", "m+", RoundButton.ButtonStyle.Function),
                ("MS", "ms", RoundButton.ButtonStyle.Function),
                // Row 2
                ("π",  "pi", RoundButton.ButtonStyle.Scientific),
                ("e",  "e",  RoundButton.ButtonStyle.Scientific),
                ("|x|","abs",RoundButton.ButtonStyle.Scientific),
                ("CE", "ce", RoundButton.ButtonStyle.Function),
                ("C",  "c",  RoundButton.ButtonStyle.Clear),
                // Row 3
                ("x²", "sq", RoundButton.ButtonStyle.Scientific),
                ("x³","cube",RoundButton.ButtonStyle.Scientific),
                ("xⁿ","pow",RoundButton.ButtonStyle.Scientific),
                ("√",  "sqrt",RoundButton.ButtonStyle.Function),
                ("⌫",  "bs", RoundButton.ButtonStyle.Operator),
                // Row 4
                ("1/x","inv",RoundButton.ButtonStyle.Function),
                ("n!", "fact",RoundButton.ButtonStyle.Scientific),
                ("%",  "%",  RoundButton.ButtonStyle.Function),
                ("÷",  "/",  RoundButton.ButtonStyle.Operator),
                ("+/−","neg",RoundButton.ButtonStyle.Function),
                // Row 5
                ("7",  "7",  RoundButton.ButtonStyle.Normal),
                ("8",  "8",  RoundButton.ButtonStyle.Normal),
                ("9",  "9",  RoundButton.ButtonStyle.Normal),
                ("×",  "*",  RoundButton.ButtonStyle.Operator),
                ("−",  "-",  RoundButton.ButtonStyle.Operator),
                // Row 6
                ("4",  "4",  RoundButton.ButtonStyle.Normal),
                ("5",  "5",  RoundButton.ButtonStyle.Normal),
                ("6",  "6",  RoundButton.ButtonStyle.Normal),
                ("+",  "+",  RoundButton.ButtonStyle.Operator),
                ("1",  "1",  RoundButton.ButtonStyle.Normal),
                // Row 7
                ("2",  "2",  RoundButton.ButtonStyle.Normal),
                ("3",  "3",  RoundButton.ButtonStyle.Normal),
                ("0",  "0",  RoundButton.ButtonStyle.Normal),
                (".",  ".",  RoundButton.ButtonStyle.Normal),
                ("=",  "=",  RoundButton.ButtonStyle.Equals),
            };

            foreach (var (text, value, style) in defs)
                AddButton(text, value, style);
        }

        private void AddButton(string text, string value, RoundButton.ButtonStyle style)
        {
            var btn = new RoundButton
            {
                Text   = text,
                Tag    = value,
                Style  = style,
                Font   = style == RoundButton.ButtonStyle.Scientific
                         ? ThemeColors.FontScientific
                         : ThemeColors.FontButton,
            };
            btn.Click += Btn_Click;
            _buttonPanel.Controls.Add(btn);
            _buttons.Add(btn);
        }

        // ── Sizing & layout ───────────────────────────────────────────────
        private void SetWindowSize()
        {
            int cols = _sciMode ? ColsSci : ColsStd;
            int rows = _sciMode ? 8 : RowsStd + 1;
            int innerW = cols * BtnW + (cols - 1) * Gap;
            int innerH = TitleH + PadY + DisplayH + PadY
                       + rows * BtnH + (rows - 1) * Gap + PadY;
            ClientSize = new Size(innerW + PadX * 2, innerH);
        }

        private void PositionButtons()
        {
            int cols = _sciMode ? ColsSci : ColsStd;
            for (int i = 0; i < _buttons.Count; i++)
            {
                int col = i % cols;
                int row = i / cols;
                _buttons[i].SetBounds(
                    col * (BtnW + Gap),
                    row * (BtnH + Gap),
                    BtnW, BtnH);
            }
            int rows = _sciMode ? 8 : RowsStd + 1;
            int innerW = cols * BtnW + (cols - 1) * Gap;
            int innerH = rows * BtnH + (rows - 1) * Gap;
            _buttonPanel.Size = new Size(innerW, innerH);
        }

        private void SizeControls()
        {
            if (_displayPanel == null) return;  // not yet initialized

            int innerW = ClientSize.Width - PadX * 2;

            _displayPanel.Width  = innerW;
            _displayPanel.Left   = PadX;

            _lblExpression.Width = innerW - 16;
            _lblDisplay.Width    = innerW - 16;
            _lblMemory.Width     = innerW / 2;

            _buttonPanel.Left    = PadX;

            // Title bar buttons
            int bx = Width - 14;
            foreach (Control c in _titleBar.Controls)
            {
                if (c is Button b && b.Tag is string tag)
                {
                    if (tag == "close") b.Left = bx - b.Width;
                    bx -= (b.Width + 8);
                }
            }

            // Reposition sci/hist toggles
            int rightEdge = ClientSize.Width - PadX;
            foreach (Control c in _titleBar.Controls)
            {
                if (c is RoundButton rb)
                {
                    if (rb == _btnSciToggle || rb == _btnHistToggle)
                        continue; // positioned elsewhere
                }
            }

            // History panel
            if (_historyVisible)
            {
                _historyPanel.Left   = ClientSize.Width;
                _historyPanel.Top    = TitleH;
                _historyPanel.Width  = HistoryW;
                _historyPanel.Height = ClientSize.Height - TitleH;
                _historyPanel.Visible = true;
            }
        }

        // ── Mode toggles ──────────────────────────────────────────────────
        private void ToggleScientific()
        {
            _sciMode = !_sciMode;
            _btnSciToggle.Style = _sciMode
                ? RoundButton.ButtonStyle.Operator
                : RoundButton.ButtonStyle.Function;

            BuildButtons();
            SetWindowSize();
            PositionButtons();
            SizeControls();
        }

        private void ToggleHistory()
        {
            _historyVisible = !_historyVisible;
            _btnHistToggle.Style = _historyVisible
                ? RoundButton.ButtonStyle.Operator
                : RoundButton.ButtonStyle.Function;

            if (_historyVisible)
            {
                RefreshHistory();
                _historyPanel.BringToFront();

                int extraW = HistoryW + Gap;
                int newW = ClientSize.Width + extraW;
                ClientSize = new Size(newW, ClientSize.Height);
                _historyPanel.Left   = ClientSize.Width - HistoryW;
                _historyPanel.Top    = TitleH;
                _historyPanel.Height = ClientSize.Height - TitleH;
                _historyPanel.Visible = true;
            }
            else
            {
                int newW = ClientSize.Width - (HistoryW + Gap);
                _historyPanel.Visible = false;
                ClientSize = new Size(newW, ClientSize.Height);
            }
        }

        // ── Button click handler ──────────────────────────────────────────
        private void Btn_Click(object? sender, EventArgs e)
        {
            if (sender is RoundButton btn && btn.Tag is string val)
                ProcessInput(val);
        }

        private void ProcessInput(string val)
        {
            switch (val)
            {
                case "0": case "1": case "2": case "3": case "4":
                case "5": case "6": case "7": case "8": case "9":
                case ".":
                    _engine.InputDigit(val); break;

                case "+": case "-": case "*": case "/": case "^":
                    _engine.InputOperator(val); break;

                case "=":   _engine.Equals(); break;
                case "c":   _engine.Clear(); break;
                case "ce":  _engine.ClearEntry(); break;
                case "bs":  _engine.Backspace(); break;
                case "neg": _engine.ToggleSign(); break;
                case "%":   _engine.Percentage(); break;

                // Scientific
                case "sin": case "cos": case "tan":
                case "log": case "ln":  case "sqrt":
                case "sq":  case "cube":case "inv":
                case "fact":case "pi":  case "e":
                case "abs":
                    _engine.ScientificOperation(val); break;

                case "pow": _engine.InputOperator("^"); break;
                case "exp": _engine.ScientificOperation("e"); break;

                // Memory
                case "mc":  _engine.MemoryClear(); break;
                case "mr":  _engine.MemoryRecall(); break;
                case "m+":  _engine.MemoryAdd(); break;
                case "ms":  _engine.MemoryStore(); break;
            }

            UpdateDisplay();
            if (_historyVisible) RefreshHistory();
        }

        // ── Display update ────────────────────────────────────────────────
        private void UpdateDisplay()
        {
            string display = _engine.CurrentDisplay;

            // Auto-shrink font so long numbers always fit
            float size = display.Length switch
            {
                <= 6  => 28,
                <= 10 => 22,
                <= 14 => 17,
                <= 18 => 13,
                _     => 11,
            };
            _lblDisplay.Font = new Font("Segoe UI", size, FontStyle.Regular);
            _lblDisplay.Text = display;
            _lblExpression.Text = _engine.ExpressionDisplay;

            _lblMemory.Visible = _engine.HasMemory;
            if (_engine.HasMemory)
                _lblMemory.Text = $"M: {_engine.MemoryValue}";
        }

        private void RefreshHistory()
        {
            _historyList.BeginUpdate();
            _historyList.Items.Clear();
            foreach (var item in _engine.History)
                _historyList.Items.Add(item);
            _historyList.EndUpdate();
        }

        // ── Keyboard input ────────────────────────────────────────────────
        private void MainForm_KeyDown(object? sender, KeyEventArgs e)
        {
            string? val = e.KeyCode switch
            {
                Keys.D0 or Keys.NumPad0 => "0",
                Keys.D1 or Keys.NumPad1 => "1",
                Keys.D2 or Keys.NumPad2 => "2",
                Keys.D3 or Keys.NumPad3 => "3",
                Keys.D4 or Keys.NumPad4 => "4",
                Keys.D5 or Keys.NumPad5 => "5",
                Keys.D6 or Keys.NumPad6 => "6",
                Keys.D7 or Keys.NumPad7 => "7",
                Keys.D8 or Keys.NumPad8 => "8",
                Keys.D9 or Keys.NumPad9 => "9",
                Keys.OemPeriod or Keys.Decimal => ".",
                Keys.Add  or Keys.Oemplus when e.Shift => "+",
                Keys.Add => "+",
                Keys.Subtract or Keys.OemMinus => "-",
                Keys.Multiply => "*",
                Keys.Divide or Keys.OemQuestion => "/",
                Keys.Enter or Keys.Return => "=",
                Keys.Escape => "c",
                Keys.Back   => "bs",
                Keys.Delete => "ce",
                Keys.F1     => "sin",
                Keys.F2     => "cos",
                Keys.F3     => "tan",
                Keys.F4     => "sqrt",
                _           => null,
            };

            if (val != null)
            {
                e.Handled = true;
                ProcessInput(val);
            }
        }

        // ── Custom painting ───────────────────────────────────────────────
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Subtle border around window
            using var pen = new Pen(Color.FromArgb(80, ThemeColors.ShadowDark), 1.2f);
            var rect = new Rectangle(0, 0, Width - 1, Height - 1);
            int r = ThemeColors.WindowRadius;
            int d = r * 2;
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            g.DrawPath(pen, path);
            path.Dispose();
        }

        private void DisplayPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            int r = ThemeColors.DisplayRadius;
            int d = r * 2;
            var rect = new Rectangle(0, 0, _displayPanel.Width - 1, _displayPanel.Height - 1);

            var path = new System.Drawing.Drawing2D.GraphicsPath();
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();

            // Inset shadow top-left (dark), bottom-right (light) → recessed display
            using var bg = new SolidBrush(ThemeColors.DisplayBg);
            g.FillPath(bg, path);

            using var shadDark  = new Pen(Color.FromArgb(55, ThemeColors.ShadowDark),  1.2f);
            using var shadLight = new Pen(Color.FromArgb(180, ThemeColors.ShadowLight), 1.2f);
            g.DrawPath(shadDark,  path);
            path.Dispose();
        }

        private void TitleBar_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            // Bottom separator line
            using var pen = new Pen(ThemeColors.BorderColor, 1f);
            g.DrawLine(pen, 0, TitleH - 1, _titleBar.Width, TitleH - 1);
        }

        private void HistoryPanel_Paint(object? sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            using var pen = new Pen(ThemeColors.BorderColor, 1f);
            g.DrawLine(pen, 0, 0, 0, _historyPanel.Height);
        }

        private void HistoryList_DrawItem(object? sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            var g = e.Graphics;
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

            // Alternate row background
            var bg = e.Index % 2 == 0
                ? ThemeColors.HistoryBg
                : Color.FromArgb(215, 221, 232);
            e.Graphics.FillRectangle(new SolidBrush(bg), e.Bounds);

            string item = _historyList.Items[e.Index].ToString() ?? "";
            int eqIdx = item.LastIndexOf('=');
            if (eqIdx >= 0)
            {
                string expr   = item[..eqIdx];
                string result = item[(eqIdx + 1)..].Trim();

                using var exprBrush   = new SolidBrush(ThemeColors.TextSecondary);
                using var resultBrush = new SolidBrush(ThemeColors.TextPrimary);

                g.DrawString(expr.Trim(), new Font("Segoe UI", 9), exprBrush,
                    new RectangleF(e.Bounds.X + 8, e.Bounds.Y + 4, e.Bounds.Width - 16, 18));
                g.DrawString(result, new Font("Segoe UI", 13, FontStyle.Bold), resultBrush,
                    new RectangleF(e.Bounds.X + 8, e.Bounds.Y + 22, e.Bounds.Width - 16, 20));
            }
            else
            {
                using var brush = new SolidBrush(ThemeColors.TextPrimary);
                g.DrawString(item, ThemeColors.FontHistory, brush, e.Bounds.X + 8, e.Bounds.Y + 12);
            }

            // Separator
            using var sepPen = new Pen(Color.FromArgb(60, ThemeColors.ShadowDark));
            g.DrawLine(sepPen, e.Bounds.Left, e.Bounds.Bottom - 1, e.Bounds.Right, e.Bounds.Bottom - 1);
        }

        // ── Title-bar drag ────────────────────────────────────────────────
        private void TitleBar_MouseDown(object? sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                _dragging = true;
                _dragStart = new Point(e.X + _titleBar.Left, e.Y + _titleBar.Top);
            }
        }

        private void TitleBar_MouseMove(object? sender, MouseEventArgs e)
        {
            if (!_dragging) return;
            var delta = new Point(e.X + _titleBar.Left - _dragStart.X,
                                  e.Y + _titleBar.Top  - _dragStart.Y);
            Location = new Point(Location.X + delta.X, Location.Y + delta.Y);
        }

        private void TitleBar_MouseUp(object? sender, MouseEventArgs e)
            => _dragging = false;

        // ── Helper: create title bar icon button ──────────────────────────
        private static Button MakeTitleBtn(string text, Color hoverColor)
        {
            var btn = new Button
            {
                Text      = text,
                FlatStyle = FlatStyle.Flat,
                Size      = new Size(28, 28),
                BackColor = Color.Transparent,
                ForeColor = ThemeColors.TextSecondary,
                Font      = new Font("Segoe UI", 10),
                Cursor    = Cursors.Hand,
                Tag       = text == "✕" ? "close" : "min",
            };
            btn.Top = (44 - 28) / 2;
            btn.FlatAppearance.BorderSize = 0;
            btn.FlatAppearance.MouseOverBackColor  = hoverColor;
            btn.FlatAppearance.MouseDownBackColor  = ControlPaint.Dark(hoverColor, 0.2f);
            return btn;
        }

        // ── Anchor close/min on resize ────────────────────────────────────
        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);

            // Guard: controls may not exist yet during InitializeComponent
            if (_titleBar == null || _btnSciToggle == null || _btnHistToggle == null)
                return;

            int right = ClientSize.Width - 8;
            foreach (Control c in _titleBar.Controls)
            {
                if (c is Button b)
                {
                    b.Left = right - b.Width;
                    right -= (b.Width + 6);
                }
            }

            // Reposition mode toggles
            int toggleRight = right - 4;
            _btnSciToggle.Left  = toggleRight - _btnSciToggle.Width;
            toggleRight        -= (_btnSciToggle.Width + 6);
            _btnHistToggle.Left = toggleRight - _btnHistToggle.Width;

            SizeControls();
        }
    }
}
