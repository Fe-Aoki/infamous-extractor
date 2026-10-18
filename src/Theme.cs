// GPL-3.0-or-later. Palette and fonts follow HayatoG/iss_pc Theme.qml.
using System;
using System.IO;
using System.Linq;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Preparador
{
    static class Skin
    {
        public static readonly Color Bg = ColorTranslator.FromHtml("#101216"), Rail = ColorTranslator.FromHtml("#0d0f12"), Card = ColorTranslator.FromHtml("#15181d"), Line = ColorTranslator.FromHtml("#262b33"), Text = ColorTranslator.FromHtml("#e9e6df"), Muted = ColorTranslator.FromHtml("#a3a9b3"), Orange = ColorTranslator.FromHtml("#ff6a2b"), Cyan = ColorTranslator.FromHtml("#19e3ff"), Yellow = ColorTranslator.FromHtml("#f5c400");
        static readonly PrivateFontCollection fonts = new PrivateFontCollection();
        static FontFamily body, display;
        [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
        static extern int AddFontResourceEx(string file, uint flags, IntPtr reserved);
        static Skin()
        {
            string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "fonts");
            if (Directory.Exists(dir))
                foreach (string p in Directory.GetFiles(dir, "*.ttf"))
                {
                    try
                    {
                        fonts.AddFontFile(p);
                        AddFontResourceEx(p, 0x10, IntPtr.Zero);
                    }
                    catch
                    {
                    }
                }

            body = fonts.Families.FirstOrDefault(f => f.Name.Contains("Barlow")) ?? FontFamily.GenericSansSerif;
            display = fonts.Families.FirstOrDefault(f => f.Name.Contains("Shoulders")) ?? body;
        }

        public static Font Body(float size, bool bold = false)
        {
            return new Font(body, size, bold && body.IsStyleAvailable(FontStyle.Bold) ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
        }

        public static Font Display(float size)
        {
            return new Font(display, size, FontStyle.Regular, GraphicsUnit.Pixel);
        }

        public static GraphicsPath Round(RectangleF r, float radius)
        {
            var p = new GraphicsPath();
            float d = radius * 2;
            p.AddArc(r.X, r.Y, d, d, 180, 90);
            p.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            p.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            p.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static Label Label(string text, float size, Color color, bool bold = false)
        {
            return new Label
            {
                Text = text,
                Font = Body(size, bold),
                ForeColor = color,
                BackColor = Color.Transparent,
                AutoSize = true,
                UseCompatibleTextRendering = true
            };
        }
    }

    sealed class Surface : Panel
    {
        public Surface()
        {
            DoubleBuffered = true;
            BackColor = Skin.Card;
            Padding = new Padding(20);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var p = Skin.Round(new RectangleF(.5f, .5f, Width - 1, Height - 1), 12))
            using (var pen = new Pen(Skin.Line))
                e.Graphics.DrawPath(pen, p);
        }
    }

    sealed class EmberButton : Button
    {
        public bool Compact { get; set; }
        public bool Primary;
        bool hover, pressed;
        public EmberButton()
        {
            SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
            UseVisualStyleBackColor = false;
            BackColor = Skin.Card;
            Cursor = Cursors.Hand;
            Font = new Font("Segoe UI", 14.5f, FontStyle.Bold, GraphicsUnit.Pixel);
            Margin = new Padding(8, 0, 0, 0);
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            hover = true;
            Invalidate();
            base.OnMouseEnter(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            hover = false;
            pressed = false;
            Invalidate();
            base.OnMouseLeave(e);
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            pressed = e.Button == MouseButtons.Left;
            Invalidate();
            base.OnMouseDown(e);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            pressed = false;
            Invalidate();
            base.OnMouseUp(e);
        }

        protected override void OnEnabledChanged(EventArgs e)
        {
            hover = false;
            pressed = false;
            Invalidate();
            base.OnEnabledChanged(e);
        }

        protected override void OnGotFocus(EventArgs e)
        {
            Invalidate();
            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            Invalidate();
            base.OnLostFocus(e);
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? Skin.Bg);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Parent?.BackColor ?? Skin.Bg);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            Color bg = Primary ? (Enabled ? (pressed ? Color.FromArgb(226, 86, 30) : hover ? Color.FromArgb(255, 129, 72) : Skin.Orange) : Color.FromArgb(66, 43, 34)) : (Enabled && pressed ? Color.FromArgb(40, 44, 53) : hover && Enabled ? Color.FromArgb(34, 38, 46) : Color.FromArgb(26, 29, 35));
            Color border = Primary ? bg : (Enabled && (hover || Focused) ? Color.FromArgb(105, 112, 126) : Color.FromArgb(48, 54, 65));
            using (var p = Skin.Round(new RectangleF(1, 1, Width - 2, Height - 2), 8))
            using (var brush = new SolidBrush(bg))
            using (var pen = new Pen(border))
            {
                e.Graphics.FillPath(brush, p);
                e.Graphics.DrawPath(pen, p);
            }

            int inset = Compact ? 4 : 10;
            var bounds = new Rectangle(inset, pressed ? 2 : 0, Width - inset * 2, Height);
            using (var b = new SolidBrush(Enabled ? (Primary ? Color.FromArgb(22, 10, 4) : Color.FromArgb(240, 240, 242)) : Color.FromArgb(138, 143, 152)))
            using (var f = new Font("Segoe UI", Compact ? 12 : Primary ? 16 : 14, FontStyle.Bold, GraphicsUnit.Pixel))
            using (var fmt = new StringFormat
            {
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            }

            )
                e.Graphics.DrawString(Text, f, b, bounds, fmt);
            if (Focused && Enabled)
                using (var p = Skin.Round(new RectangleF(4, 4, Width - 8, Height - 8), 6))
                using (var pen = new Pen(Primary ? Color.FromArgb(130, 22, 10, 4) : Skin.Orange, 1))
                    e.Graphics.DrawPath(pen, p);
        }
    }

    sealed class EmberProgress : Control
    {
        int value;
        public int Value
        {
            get
            {
                return value;
            }

            set
            {
                this.value = Math.Max(0, Math.Min(100, value));
                Invalidate();
            }
        }

        public EmberProgress()
        {
            DoubleBuffered = true;
            Height = 4;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Skin.Line);
            if (value > 0)
                using (var b = new SolidBrush(Skin.Orange))
                    e.Graphics.FillRectangle(b, 0, 0, Width * value / 100f, Height);
        }
    }

    sealed class Hero : Panel
    {
        public Hero()
        {
            DoubleBuffered = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            e.Graphics.Clear(Skin.Bg);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using (var gradient = new LinearGradientBrush(new Rectangle(0, 0, Math.Max(1, Width), Math.Max(1, Height)), Color.FromArgb(42, 28, 29), Skin.Bg, 15f))
                e.Graphics.FillRectangle(gradient, ClientRectangle);
            using (var p = new Pen(Color.FromArgb(32, 25, 227, 255), 1))
                for (int x = Width / 2; x < Width + 100; x += 19)
                    e.Graphics.DrawLine(p, x, 0, x - 50, Height);
            using (var p = new Pen(Color.FromArgb(65, 255, 106, 43), 1.5f))
            {
                e.Graphics.DrawLines(p, new[] { new Point(Width - 170, 5), new Point(Width - 230, 45), new Point(Width - 180, 55), new Point(Width - 270, 110) });
            }

            using (var b = new SolidBrush(Color.FromArgb(150, 255, 106, 43)))
            {
                var rnd = new Random(16);
                for (int i = 0; i < 26; i++)
                {
                    int x = rnd.Next(Math.Max(1, Width - 300), Math.Max(2, Width));
                    int y = rnd.Next(Math.Max(1, Height));
                    e.Graphics.FillRectangle(b, x, y, i % 3 + 1, i % 3 + 1);
                }
            }
        }
    }

    sealed class Rail : Panel
    {
        Image icon;
        public Rail()
        {
            DoubleBuffered = true;
            BackColor = Skin.Rail;
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "infamous.png");
            if (File.Exists(path))
                icon = Image.FromFile(path);
        }

        void Draw(Graphics g, string text, float size, Color color, float x, float y, bool display = false)
        {
            using (var f = display ? Skin.Display(size) : Skin.Body(size, true))
            using (var b = new SolidBrush(color))
                g.DrawString(text, f, b, x, y);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            if (icon != null)
                g.DrawImage(icon, 28, 30, 76, 76);
            Draw(g, "inFAMOUS", 36, Skin.Text, 25, 122, true);
            Draw(g, "SECOND SON", 15, Skin.Muted, 28, 166);
            Draw(g, L.T("PREPARADOR"), 12, Skin.Orange, 28, 203);
            using (var p = new Pen(Skin.Line))
                g.DrawLine(p, 28, 240, Width - 28, 240);
            Draw(g, L.T("SUA INSTALAÇÃO"), 11, Skin.Muted, 28, 263);
            string[] names =
            {
                L.T("JOGO BASE"),
                L.T("ATUALIZAÇÃO"),
                L.T("PASTA FINAL")
            }, hints =
            {
                L.T("Todas as partes juntas"),
                "Update 1.07",
                L.T("Base + update unidos")
            };
            for (int i = 0; i < 3; i++)
            {
                int y = 307 + i * 80;
                using (var p = new Pen(i == 0 ? Skin.Orange : Skin.Line, 1.5f))
                    g.DrawEllipse(p, 28, y, 27, 27);
                Draw(g, (i + 1).ToString(), 13, i == 0 ? Skin.Orange : Skin.Muted, 37, y + 4);
                Draw(g, names[i], 12, Skin.Text, 69, y + 1);
                using (var f = Skin.Body(11))
                using (var b = new SolidBrush(Skin.Muted))
                    g.DrawString(hints[i], f, b, 69, y + 23);
            }

            Draw(g, L.T("ARQUIVOS PRESERVADOS"), 10, Skin.Cyan, 28, Height - 89);
            using (var f = Skin.Body(12))
            using (var b = new SolidBrush(Skin.Muted))
                g.DrawString(L.T("Seus pacotes originais\npermanecem intactos."), f, b, 28, Height - 66);
            Draw(g, "v0.7", 10, Skin.Muted, Width - 47, Height - 25);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                icon?.Dispose();
            base.Dispose(disposing);
        }
    }

    sealed class LanguagePicker : Panel
    {
        readonly EmberButton portuguese = new EmberButton { Text = "Português", Compact = true, Margin = Padding.Empty };
        readonly EmberButton english = new EmberButton { Text = "English", Compact = true, Margin = Padding.Empty };
        int selected = -1;
        public event EventHandler SelectedIndexChanged;
        public int SelectedIndex
        {
            get { return selected; }
            set
            {
                if (value != 0 && value != 1) throw new ArgumentOutOfRangeException("value");
                if (selected == value) return;
                selected = value;
                portuguese.Primary = value == 0;
                english.Primary = value == 1;
                portuguese.Invalidate(); english.Invalidate();
                SelectedIndexChanged?.Invoke(this, EventArgs.Empty);
            }
        }
        public LanguagePicker()
        {
            BackColor = Skin.Rail;
            Controls.AddRange(new Control[] { portuguese, english });
            portuguese.Click += (s, e) => SelectedIndex = 0;
            english.Click += (s, e) => SelectedIndex = 1;
            Resize += (s, e) =>
            {
                portuguese.Bounds = new Rectangle(0, 0, 92, Height);
                english.Bounds = new Rectangle(94, 0, Width - 94, Height);
            };
        }
    }

    public sealed class MainForm : Form
    {
        readonly TextBox baseFile = new TextBox(), updateFile = new TextBox(), destination = new TextBox(), basePassword = new TextBox(), updatePassword = new TextBox(), messages = new TextBox();
        readonly EmberButton start = new EmberButton(), stop = new EmberButton(), open = new EmberButton();
        readonly EmberProgress bar = new EmberProgress();
        readonly Label state = Skin.Label(L.T("PRONTO PARA PREPARAR"), 13, Skin.Muted, true), baseCount = Skin.Label(L.T("Selecione uma ou todas as partes do jogo."), 12, Skin.Muted), updateCount = Skin.Label(L.T("Selecione o update e suas partes, se houver."), 12, Skin.Muted);
        readonly List<Control> inputs = new List<Control>();
        readonly ToolTip tips = new ToolTip();
        readonly LanguagePicker language = new LanguagePicker();
        int baseResolvedCount = 1, updateResolvedCount = 1;
        CancellationTokenSource cancellation;
        bool running, settingSelection;
        string finished;
        string[] baseSelection, updateSelection;
        public MainForm()
        {
            Text = L.T("inFAMOUS Second Son — Preparador");
            ClientSize = new Size(1120, 800);
            MinimumSize = new Size(1020, 790);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Skin.Bg;
            ForeColor = Skin.Text;
            Font = Skin.Body(14);
            AutoScaleMode = AutoScaleMode.Dpi;
            string icon = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "infamous.ico");
            if (File.Exists(icon))
                Icon = new Icon(icon);
            var rail = new Rail
            {
                Dock = DockStyle.Left,
                Width = 224
            };
            var stage = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(28)
            };
            Controls.Add(stage);
            Controls.Add(rail);
            var languageLabel = Skin.Label("Idioma / Language", 11, Skin.Muted);
            languageLabel.Location = new Point(28, rail.Height - 153);
            languageLabel.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            language.Font = new Font("Segoe UI", 13, FontStyle.Regular, GraphicsUnit.Pixel);
            language.ForeColor = Skin.Text;
            language.Size = new Size(168, 34);
            language.Location = new Point(28, rail.Height - 131);
            language.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            language.AccessibleName = "Idioma / Language";
            language.SelectedIndex = L.Language == "en" ? 1 : 0;
            rail.Controls.AddRange(new Control[] { languageLabel, language });
            inputs.Add(language);
            language.SelectedIndexChanged += (s, e) =>
            {
                L.SetLanguage(language.SelectedIndex == 1 ? "en" : "pt-BR", true);
                L.Apply(this);
                RefreshCount(baseFile, baseCount, baseSelection, baseResolvedCount);
                RefreshCount(updateFile, updateCount, updateSelection, updateResolvedCount);
                string initial = "Escolha os arquivos da base, o update e uma pasta nova para começar.";
                if (messages.Text == initial || messages.Text == L.English[initial]) messages.Text = L.T(initial);
            };
            var layout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 8,
                Margin = Padding.Empty
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            foreach (float h in new[]
            {
                116,
                16,
                358,
                14,
                28,
                0,
                12,
                48
            }

            )
                layout.RowStyles.Add(h == 0 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.Absolute, h));
            stage.Controls.Add(layout);
            var hero = new Hero
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            var kicker = Skin.Label(L.T("PREPARE O SEU JOGO"), 12, Skin.Orange, true);
            kicker.Location = new Point(18, 13);
            var heading = new Label
            {
                Text = L.T("PRONTO PARA SEATTLE."),
                Font = Skin.Display(42),
                ForeColor = Skin.Text,
                BackColor = Color.Transparent,
                Location = new Point(15, 32),
                AutoSize = true,
                UseCompatibleTextRendering = true
            };
            var subtitle = Skin.Label("CUSA00223  /  BASE + UPDATE 1.07", 12, Skin.Muted);
            subtitle.Location = new Point(18, 88);
            hero.Controls.AddRange(new Control[] { kicker, heading, subtitle });
            layout.Controls.Add(hero, 0, 0);
            var card = new Surface
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            var rows = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty
            };
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 114));
            rows.RowStyles.Add(new RowStyle(SizeType.Absolute, 114));
            rows.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            rows.Controls.Add(FileRow(L.T("01  JOGO BASE"), baseFile, basePassword, baseCount, false), 0, 0);
            rows.Controls.Add(FileRow(L.T("02  UPDATE 1.07"), updateFile, updatePassword, updateCount, false), 0, 1);
            destination.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Game-CUSA00223");
            rows.Controls.Add(FileRow(L.T("03  PASTA FINAL"), destination, null, null, true), 0, 2);
            card.Controls.Add(rows);
            layout.Controls.Add(card, 0, 2);
            state.Dock = DockStyle.Fill;
            state.Padding = new Padding(0, 3, 0, 0);
            layout.Controls.Add(state, 0, 4);
            var logPanel = new Surface
            {
                Dock = DockStyle.Fill,
                Margin = Padding.Empty,
                Padding = new Padding(12)
            };
            var logLayout = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Margin = Padding.Empty
            };
            logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            logLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 4));
            logLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            logLayout.Controls.Add(Skin.Label(L.T("ANDAMENTO DA PREPARAÇÃO"), 10, Skin.Muted, true), 0, 0);
            bar.Dock = DockStyle.Fill;
            bar.Margin = Padding.Empty;
            logLayout.Controls.Add(bar, 0, 1);
            messages.Multiline = true;
            messages.ReadOnly = true;
            messages.ScrollBars = ScrollBars.Vertical;
            messages.Dock = DockStyle.Fill;
            messages.BackColor = Skin.Card;
            messages.ForeColor = Skin.Muted;
            messages.BorderStyle = BorderStyle.None;
            messages.Font = Skin.Body(12);
            messages.Margin = new Padding(0, 9, 0, 0);
            messages.Text = L.T("Escolha os arquivos da base, o update e uma pasta nova para começar.");
            logLayout.Controls.Add(messages, 0, 2);
            logPanel.Controls.Add(logLayout);
            layout.Controls.Add(logPanel, 0, 5);
            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.RightToLeft,
                WrapContents = false,
                Margin = Padding.Empty
            };
            start.Text = L.T("EXTRAIR E UNIR");
            start.Primary = true;
            start.Size = new Size(216, 46);
            start.Click += async (s, e) => await StartJob();
            stop.Text = L.T("Cancelar");
            stop.Size = new Size(96, 46);
            stop.Enabled = false;
            stop.Click += (s, e) =>
            {
                cancellation?.Cancel();
                stop.Enabled = false;
                state.Text = L.T("CANCELANDO E LIMPANDO TEMPORÁRIOS...");
            };
            open.Text = L.T("Abrir pasta final");
            open.Size = new Size(172, 46);
            open.Enabled = false;
            open.Click += (s, e) =>
            {
                if (finished != null)
                    Process.Start("explorer.exe", Engine.Quote(finished));
            };
            buttons.Controls.AddRange(new Control[] { start, stop, open });
            layout.Controls.Add(buttons, 0, 7);
            inputs.AddRange(new Control[] { baseFile, updateFile, destination, basePassword, updatePassword });
            baseFile.TextChanged += (s, e) =>
            {
                if (!settingSelection)
                {
                    baseSelection = null;
                    RefreshCount(baseFile, baseCount, null, 1);
                }
            };
            updateFile.TextChanged += (s, e) =>
            {
                if (!settingSelection)
                {
                    updateSelection = null;
                    RefreshCount(updateFile, updateCount, null, 1);
                }
            };
            FormClosing += (s, e) =>
            {
                if (running)
                {
                    e.Cancel = true;
                    cancellation.Cancel();
                    stop.Enabled = false;
                    state.Text = L.T("CANCELANDO. AGUARDE A LIMPEZA ANTES DE FECHAR.");
                }
            };
        }

        Control TextField(TextBox box)
        {
            var frame = new Panel
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(23, 26, 32),
                Padding = new Padding(10, 8, 8, 4),
                Margin = Padding.Empty
            };
            box.Dock = DockStyle.Fill;
            box.BackColor = frame.BackColor;
            box.ForeColor = Skin.Text;
            box.BorderStyle = BorderStyle.None;
            box.Font = Skin.Body(14);
            frame.Controls.Add(box);
            frame.Paint += (s, e) =>
            {
                using (var p = new Pen(box.Focused ? Skin.Orange : Skin.Line))
                    e.Graphics.DrawRectangle(p, 0, 0, frame.Width - 1, frame.Height - 1);
            };
            box.GotFocus += (s, e) => frame.Invalidate();
            box.LostFocus += (s, e) => frame.Invalidate();
            return frame;
        }

        Control FileRow(string caption, TextBox box, TextBox password, Label count, bool folder)
        {
            var row = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 3,
                Margin = new Padding(0, 0, 0, 8)
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 174));
            row.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
            row.RowStyles.Add(new RowStyle(SizeType.Absolute, 36));
            row.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            // Keep the source caption so dialogs also follow a later language switch.
            foreach (var pair in L.English) if (pair.Value == caption) { caption = pair.Key; break; }
            var title = Skin.Label(L.T(caption), 12, Skin.Text, true);
            row.Controls.Add(title, 0, 0);
            var badge = Skin.Label(folder ? L.T("PASTA NOVA") : "RAR  /  ZIP  /  PKG", 10, Skin.Muted);
            badge.Dock = DockStyle.Right;
            row.Controls.Add(badge, 1, 0);
            var field = TextField(box);
            field.Margin = new Padding(0, 0, 10, 0);
            row.Controls.Add(field, 0, 1);
            var browse = new EmberButton
            {
                Text = folder ? L.T("Escolher pasta") : L.T("Selecionar partes"),
                Dock = DockStyle.Fill,
                Margin = Padding.Empty
            };
            inputs.Add(browse);
            row.Controls.Add(browse, 1, 1);
            if (folder)
            {
                var hint = Skin.Label(L.T("O update será aplicado por cima da base, em uma pasta nova."), 12, Skin.Muted);
                hint.Padding = new Padding(0, 6, 0, 0);
                row.Controls.Add(hint, 0, 2);
                row.SetColumnSpan(hint, 2);
            }
            else
            {
                count.Dock = DockStyle.Fill;
                count.Padding = new Padding(0, 7, 0, 0);
                count.AutoEllipsis = true;
                count.AutoSize = false;
                row.Controls.Add(count, 0, 2);
                var pw = new TableLayoutPanel
                {
                    Dock = DockStyle.Top,
                    Height = 34,
                    ColumnCount = 2,
                    RowCount = 1,
                    Padding = Padding.Empty,
                    Margin = new Padding(0, 4, 0, 0),
                    AutoSize = false
                };
                pw.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                pw.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 64));
                pw.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
                var label = Skin.Label(L.T("Senha"), 12, Skin.Muted);
                label.Padding = new Padding(0, 7, 0, 0);
                pw.Controls.Add(label, 0, 0);
                password.UseSystemPasswordChar = true;
                pw.Controls.Add(TextField(password), 1, 0);
                row.Controls.Add(pw, 1, 2);
            }

            browse.Click += (s, e) =>
            {
                if (folder)
                {
                    using (var d = new FolderBrowserDialog
                    {
                        Description = L.T("Escolha onde criar a pasta final")
                    }

                    )
                        if (d.ShowDialog(this) == DialogResult.OK)
                            box.Text = Path.Combine(d.SelectedPath, "Game-CUSA00223");
                }
                else
                    using (var d = new OpenFileDialog
                    {
                        Title = L.T("Selecione uma ou todas as partes — ") + L.T(caption),
                        Filter = L.T("Arquivos de jogo|*.pkg;*.rar;*.zip;*.001;*.002;*.003;*.r00;*.z01|Todos os arquivos|*.*"),
                        Multiselect = true,
                        CheckFileExists = true
                    }

                    )
                        if (d.ShowDialog(this) == DialogResult.OK)
                        {
                            try
                            {
                                string[] selected = d.FileNames;
                                var resolved = Engine.ResolveSources(selected[0], selected);
                                settingSelection = true;
                                box.Text = resolved[0];
                                if (box == baseFile)
                                {
                                    baseSelection = selected;
                                    baseResolvedCount = resolved.Length;
                                }
                                else
                                {
                                    updateSelection = selected;
                                    updateResolvedCount = resolved.Length;
                                }
                                RefreshCount(box, count, selected, resolved.Length);
                                count.ForeColor = Skin.Cyan;
                                tips.SetToolTip(box, string.Join(Environment.NewLine, selected));
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show(this, ex.Message, L.T("Seleção de partes"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            finally
                            {
                                settingSelection = false;
                            }
                        }
            };
            return row;
        }

        void RefreshCount(TextBox box, Label count, string[] selection, int resolvedCount)
        {
            if (selection != null)
                count.Text = selection.Length + L.T(" arquivo(s) selecionado(s) · ") + (resolvedCount == 1 ? L.T("demais volumes lidos automaticamente") : L.T("ZIPs extraídos em sequência"));
            else if (!string.IsNullOrWhiteSpace(box.Text))
                count.Text = L.T("Caminho informado manualmente; demais volumes serão encontrados na pasta.");
            else
                count.Text = L.T(box == baseFile ? "Selecione uma ou todas as partes do jogo." : "Selecione o update e suas partes, se houver.");
        }

        void Ui(Action action)
        {
            if (IsDisposed)
                return;
            if (InvokeRequired)
                BeginInvoke(action);
            else
                action();
        }

        void Log(string text)
        {
            Ui(() => messages.AppendText(DateTime.Now.ToString("HH:mm:ss") + "  " + text + Environment.NewLine));
        }

        async Task StartJob()
        {
            var job = new Job
            {
                Base = baseFile.Text.Trim(),
                Update = updateFile.Text.Trim(),
                BaseFiles = baseSelection,
                UpdateFiles = updateSelection,
                Destination = destination.Text.Trim(),
                BasePassword = basePassword.Text,
                UpdatePassword = updatePassword.Text
            };
            running = true;
            start.Enabled = false;
            stop.Enabled = true;
            open.Enabled = false;
            foreach (var c in inputs)
                c.Enabled = false;
            messages.Clear();
            bar.Value = 0;
            cancellation = new CancellationTokenSource();
            state.Text = L.T("PREPARANDO...");
            state.ForeColor = Skin.Orange;
            try
            {
                var engine = new Engine(Log, (n, text) => Ui(() =>
                {
                    bar.Value = n;
                    state.Text = text.ToUpperInvariant();
                }), cancellation.Token);
                finished = await Task.Run(() => engine.Run(job));
                state.Text = L.T("CONCLUÍDO · SELECIONE A PASTA NO LAUNCHER DO JOGO");
                state.ForeColor = Color.FromArgb(53, 208, 127);
                open.Enabled = true;
                Log(L.T("Arquivos originais preservados."));
            }
            catch (OperationCanceledException)
            {
                state.Text = L.T("OPERAÇÃO CANCELADA");
                state.ForeColor = Skin.Yellow;
                Log(L.T("Cancelado. Arquivos de entrada preservados."));
            }
            catch (Exception ex)
            {
                state.Text = L.T("NÃO FOI POSSÍVEL CONCLUIR");
                state.ForeColor = Color.FromArgb(255, 77, 77);
                Log(L.T("ERRO: ") + ex.Message);
                MessageBox.Show(this, ex.Message, L.T("Preparação interrompida"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                running = false;
                start.Enabled = true;
                stop.Enabled = false;
                foreach (var c in inputs)
                    c.Enabled = true;
                cancellation.Dispose();
                cancellation = null;
                try
                {
                    string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Preparador-inFAMOUS", "logs");
                    Directory.CreateDirectory(dir);
                    string file = Path.Combine(dir, DateTime.Now.ToString("yyyyMMdd-HHmmss") + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".log");
                    File.WriteAllText(file, messages.Text);
                    Log("Log: " + file);
                }
                catch
                {
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                tips.Dispose();
            base.Dispose(disposing);
        }
    }
}
