using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Windows.Forms;

internal static class NativeMethods
{
    public const int WM_NCLBUTTONDOWN = 0x00A1;
    public const int HTCAPTION = 0x0002;
    public const int SW_RESTORE = 9;
    public const int GCL_STYLE = -26;
    public const int CS_DROPSHADOW = 0x00020000;
    public const int EM_SETCUEBANNER = 0x1501;

    [DllImport("user32.dll")]
    public static extern bool ReleaseCapture();

    [DllImport("user32.dll")]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);

    [DllImport("user32.dll")]
    public static extern int GetClassLong(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    public static extern int SetClassLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern bool SetProcessDPIAware();

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    public static extern IntPtr FindWindow(string className, string windowName);

    [DllImport("user32.dll")]
    public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    public static extern bool SetForegroundWindow(IntPtr hWnd);
}

internal static class Ui
{
    public static float Scale = 1f;

    public static void Init()
    {
        try
        {
            using (Graphics g = Graphics.FromHwnd(IntPtr.Zero))
            {
                Scale = g.DpiX / 96f;
            }
        }
        catch
        {
            Scale = 1f;
        }
        if (Scale < 1f) Scale = 1f;
    }

    public static int Px(float value)
    {
        return (int)Math.Round(value * Scale);
    }

    public static Color Mix(Color a, Color b, float amount)
    {
        if (amount < 0f) amount = 0f;
        if (amount > 1f) amount = 1f;
        return Color.FromArgb(
            (int)Math.Round(a.R + (b.R - a.R) * amount),
            (int)Math.Round(a.G + (b.G - a.G) * amount),
            (int)Math.Round(a.B + (b.B - a.B) * amount));
    }

    public static Color Alpha(Color color, int alpha)
    {
        return Color.FromArgb(alpha, color.R, color.G, color.B);
    }

    public static GraphicsPath RoundRect(RectangleF rect, float radius)
    {
        GraphicsPath path = new GraphicsPath();
        float diameter = radius * 2f;
        if (diameter > rect.Width) diameter = rect.Width;
        if (diameter > rect.Height) diameter = rect.Height;
        if (diameter <= 0.1f)
        {
            path.AddRectangle(rect);
            path.CloseFigure();
            return path;
        }
        float x = rect.X;
        float y = rect.Y;
        float w = rect.Width;
        float h = rect.Height;
        path.AddArc(x, y, diameter, diameter, 180f, 90f);
        path.AddArc(x + w - diameter, y, diameter, diameter, 270f, 90f);
        path.AddArc(x + w - diameter, y + h - diameter, diameter, diameter, 0f, 90f);
        path.AddArc(x, y + h - diameter, diameter, diameter, 90f, 90f);
        path.CloseFigure();
        return path;
    }

    public static void FillRound(Graphics g, Brush brush, RectangleF rect, float radius)
    {
        using (GraphicsPath path = RoundRect(rect, radius))
        {
            g.FillPath(brush, path);
        }
    }

    public static void DrawRound(Graphics g, Pen pen, RectangleF rect, float radius)
    {
        using (GraphicsPath path = RoundRect(rect, radius))
        {
            g.DrawPath(pen, path);
        }
    }
}

internal sealed class WidgetTheme
{
    public Color Bg;
    public Color Surface;
    public Color SurfaceHover;
    public Color Border;
    public Color Row;
    public Color RowHover;
    public Color Text;
    public Color Muted;
    public Color Accent;
    public Color AccentSoft;
    public Color Danger;
    public Color CheckBorder;
}

internal static class WidgetThemes
{
    public static readonly WidgetTheme Quick = new WidgetTheme
    {
        Bg = Color.FromArgb(21, 22, 26),
        Surface = Color.FromArgb(29, 31, 37),
        SurfaceHover = Color.FromArgb(37, 40, 47),
        Border = Color.FromArgb(45, 48, 56),
        Row = Color.FromArgb(27, 29, 35),
        RowHover = Color.FromArgb(35, 38, 45),
        Text = Color.FromArgb(243, 241, 235),
        Muted = Color.FromArgb(139, 142, 151),
        Accent = Color.FromArgb(231, 168, 92),
        AccentSoft = Color.FromArgb(44, 34, 22),
        Danger = Color.FromArgb(224, 108, 108),
        CheckBorder = Color.FromArgb(79, 83, 94)
    };

    public static readonly WidgetTheme Weeder = new WidgetTheme
    {
        Bg = Color.FromArgb(18, 23, 19),
        Surface = Color.FromArgb(26, 32, 27),
        SurfaceHover = Color.FromArgb(35, 43, 37),
        Border = Color.FromArgb(42, 51, 44),
        Row = Color.FromArgb(25, 31, 26),
        RowHover = Color.FromArgb(33, 41, 35),
        Text = Color.FromArgb(240, 243, 236),
        Muted = Color.FromArgb(137, 150, 140),
        Accent = Color.FromArgb(116, 192, 142),
        AccentSoft = Color.FromArgb(27, 42, 32),
        Danger = Color.FromArgb(224, 122, 122),
        CheckBorder = Color.FromArgb(76, 90, 79)
    };
}

internal static class WidgetFonts
{
    private static readonly string FamilyName = PickFamily();

    public static readonly Font Title = new Font(FamilyName, 11.2f, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font Subtitle = new Font(FamilyName, 8.1f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Count = new Font(FamilyName, 8.2f, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font Row = new Font(FamilyName, 10.1f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font RowDone = new Font(FamilyName, 10.1f, FontStyle.Strikeout, GraphicsUnit.Point);
    public static readonly Font Input = new Font(FamilyName, 9.7f, FontStyle.Regular, GraphicsUnit.Point);
    public static readonly Font Action = new Font(FamilyName, 9.3f, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font Empty = new Font(FamilyName, 9.8f, FontStyle.Bold, GraphicsUnit.Point);
    public static readonly Font EmptyHint = new Font(FamilyName, 8.4f, FontStyle.Regular, GraphicsUnit.Point);

    private static string PickFamily()
    {
        try
        {
            using (InstalledFontCollection fonts = new InstalledFontCollection())
            {
                bool yaheiUi = false;
                bool yahei = false;
                foreach (FontFamily family in fonts.Families)
                {
                    if (string.Equals(family.Name, "Microsoft YaHei UI", StringComparison.OrdinalIgnoreCase))
                    {
                        yaheiUi = true;
                        break;
                    }
                    if (string.Equals(family.Name, "Microsoft YaHei", StringComparison.OrdinalIgnoreCase))
                    {
                        yahei = true;
                    }
                }
                if (yaheiUi) return "Microsoft YaHei UI";
                if (yahei) return "Microsoft YaHei";
            }
        }
        catch
        {
        }
        return "Segoe UI";
    }
}

internal sealed class TodoItem
{
    public string Text;
    public bool Done;

    public TodoItem(string text, bool done)
    {
        Text = text;
        Done = done;
    }

    public TodoItem Clone()
    {
        return new TodoItem(Text, Done);
    }
}

internal sealed class WidgetStorage
{
    private const string Header = "floating-todo-widget-v1";
    private readonly string filePath;

    public readonly List<TodoItem> Items = new List<TodoItem>();
    public int X = int.MinValue;
    public int Y = int.MinValue;
    public bool Pinned = true;

    public WidgetStorage(string key)
    {
        string dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FloatingTodoWidgets");
        Directory.CreateDirectory(dir);
        filePath = Path.Combine(dir, key + ".dat");
    }

    public void Load()
    {
        Items.Clear();
        X = int.MinValue;
        Y = int.MinValue;
        Pinned = true;

        try
        {
            if (!File.Exists(filePath)) return;
            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);
            if (lines.Length == 0 || lines[0] != Header) return;

            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.StartsWith("x=", StringComparison.Ordinal))
                {
                    int value;
                    if (int.TryParse(line.Substring(2), out value)) X = value;
                }
                else if (line.StartsWith("y=", StringComparison.Ordinal))
                {
                    int value;
                    if (int.TryParse(line.Substring(2), out value)) Y = value;
                }
                else if (line.StartsWith("pinned=", StringComparison.Ordinal))
                {
                    Pinned = line.Substring(7) != "0";
                }
                else if (line.StartsWith("i=", StringComparison.Ordinal))
                {
                    string payload = line.Substring(2);
                    int bar = payload.IndexOf('|');
                    if (bar != 1) continue;
                    try
                    {
                        bool done = payload[0] == '1';
                        string text = Encoding.UTF8.GetString(
                            Convert.FromBase64String(payload.Substring(2)));
                        if (text.Trim().Length > 0)
                        {
                            Items.Add(new TodoItem(text, done));
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }
        catch
        {
        }
    }

    public void Save()
    {
        try
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(Header);
            builder.AppendLine("x=" + X.ToString());
            builder.AppendLine("y=" + Y.ToString());
            builder.AppendLine("pinned=" + (Pinned ? "1" : "0"));
            for (int i = 0; i < Items.Count; i++)
            {
                TodoItem item = Items[i];
                string encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(item.Text));
                builder.Append("i=");
                builder.Append(item.Done ? "1" : "0");
                builder.Append("|");
                builder.AppendLine(encoded);
            }
            File.WriteAllText(filePath, builder.ToString(), new UTF8Encoding(false));
        }
        catch
        {
        }
    }
}

internal static class AppBootstrap
{
    private static Mutex heldMutex;

    public static bool Claim(string mutexName, string windowTitle)
    {
        bool created;
        heldMutex = new Mutex(true, mutexName, out created);
        if (!created)
        {
            heldMutex.Dispose();
            heldMutex = null;
            IntPtr existing = NativeMethods.FindWindow(null, windowTitle);
            if (existing != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(existing, NativeMethods.SW_RESTORE);
                NativeMethods.SetForegroundWindow(existing);
            }
            return false;
        }
        return true;
    }
}

internal abstract class FloatingWidgetForm : Form
{
    protected readonly WidgetTheme T;
    protected readonly WidgetStorage Storage;
    protected readonly WidgetHeader Header;

    private bool pinned = true;

    protected FloatingWidgetForm(WidgetTheme theme, WidgetStorage storage, string title, string subtitle)
    {
        T = theme;
        Storage = storage;

        Text = title;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = true;
        MaximizeBox = false;
        MinimizeBox = false;
        AutoScaleMode = AutoScaleMode.None;
        BackColor = T.Bg;
        DoubleBuffered = true;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);

        Header = new WidgetHeader(T, title, subtitle);
        Header.PinClicked += delegate { SetPinned(!pinned, true); };
        Header.CloseClicked += delegate { Close(); };
        Controls.Add(Header);
    }

    protected int S(float value)
    {
        return Ui.Px(value);
    }

    public bool IsPinned
    {
        get { return pinned; }
    }

    protected void SetPinned(bool value, bool save)
    {
        pinned = value;
        TopMost = value;
        Header.Pinned = value;
        if (save)
        {
            Storage.Pinned = value;
            Storage.Save();
        }
    }

    protected virtual Point GetDefaultLocation()
    {
        Rectangle area = Screen.PrimaryScreen.WorkingArea;
        return new Point(area.Right - Width - S(20), area.Top + S(20));
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        try
        {
            int style = NativeMethods.GetClassLong(Handle, NativeMethods.GCL_STYLE);
            NativeMethods.SetClassLong(Handle, NativeMethods.GCL_STYLE,
                style | NativeMethods.CS_DROPSHADOW);
        }
        catch
        {
        }
    }

    protected override void OnLoad(EventArgs e)
    {
        base.OnLoad(e);
        Storage.Load();
        SetPinned(Storage.Pinned, false);
        Location = ResolveLocation();
        OnStateReady();
    }

    private Point ResolveLocation()
    {
        Point location;
        if (Storage.X == int.MinValue || Storage.Y == int.MinValue)
        {
            location = GetDefaultLocation();
        }
        else
        {
            location = new Point(Storage.X, Storage.Y);
            Rectangle desired = new Rectangle(location, Size);
            bool visible = false;
            foreach (Screen screen in Screen.AllScreens)
            {
                Rectangle intersect = Rectangle.Intersect(desired, screen.WorkingArea);
                if (intersect.Width >= S(60) && intersect.Height >= S(28))
                {
                    visible = true;
                    break;
                }
            }

            if (!visible)
            {
                location = GetDefaultLocation();
            }
            else
            {
                Rectangle area = Screen.FromRectangle(desired).WorkingArea;
                if (location.X + Width > area.Right) location.X = area.Right - Width;
                if (location.X < area.Left) location.X = area.Left;
                if (location.Y + Height > area.Bottom) location.Y = area.Bottom - Height;
                if (location.Y < area.Top) location.Y = area.Top;
            }
        }
        return location;
    }

    protected virtual void OnStateReady()
    {
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        Header.Bounds = new Rectangle(1, 1, Math.Max(1, ClientSize.Width - 2), S(56));
        if (Width > 0 && Height > 0)
        {
            using (GraphicsPath path = Ui.RoundRect(
                new RectangleF(0f, 0f, Width, Height), S(16)))
            {
                Region = new Region(path);
            }
        }
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        using (SolidBrush bg = new SolidBrush(T.Bg))
        {
            e.Graphics.FillRectangle(bg, ClientRectangle);
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        RectangleF border = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (Pen pen = new Pen(Ui.Mix(T.Border, T.Accent, 0.12f), 1f))
        {
            Ui.DrawRound(e.Graphics, pen, border, S(16));
        }
        base.OnPaint(e);
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        Storage.X = Left;
        Storage.Y = Top;
        Storage.Pinned = pinned;
        Storage.Save();
        base.OnFormClosing(e);
    }

    protected void Persist()
    {
        Storage.X = Left;
        Storage.Y = Top;
        Storage.Pinned = pinned;
        Storage.Save();
    }
}

internal sealed class WidgetHeader : Control
{
    private readonly WidgetTheme T;
    private readonly string title;
    private readonly string subtitle;
    private string countText = "";
    private bool pinned = true;
    private int hot;
    private int down;

    public event EventHandler PinClicked;
    public event EventHandler CloseClicked;

    public WidgetHeader(WidgetTheme theme, string title, string subtitle)
    {
        T = theme;
        this.title = title;
        this.subtitle = subtitle;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);
        BackColor = T.Bg;
        TabStop = false;
        Cursor = Cursors.Default;
    }

    public string TitleText
    {
        get { return title; }
    }

    public string CountText
    {
        get { return countText; }
        set
        {
            if (countText == value) return;
            countText = value == null ? "" : value;
            Invalidate();
        }
    }

    public bool Pinned
    {
        get { return pinned; }
        set
        {
            if (pinned == value) return;
            pinned = value;
            Invalidate();
        }
    }

    private int ButtonSize
    {
        get { return Ui.Px(28); }
    }

    private Rectangle PinRect
    {
        get
        {
            int size = ButtonSize;
            int y = (Height - size) / 2;
            int x = Width - Ui.Px(12) - size * 2 - Ui.Px(6);
            return new Rectangle(x, y, size, size);
        }
    }

    private Rectangle CloseRect
    {
        get
        {
            int size = ButtonSize;
            int y = (Height - size) / 2;
            int x = Width - Ui.Px(12) - size;
            return new Rectangle(x, y, size, size);
        }
    }

    private int HitTest(Point point)
    {
        if (CloseRect.Contains(point)) return 2;
        if (PinRect.Contains(point)) return 1;
        return 0;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        int next = HitTest(e.Location);
        if (next != hot)
        {
            hot = next;
            Cursor = hot == 0 ? Cursors.Default : Cursors.Hand;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (down == 0 && hot != 0)
        {
            hot = 0;
            Cursor = Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;
        int hit = HitTest(e.Location);
        if (hit == 0)
        {
            Form form = FindForm();
            if (form != null)
            {
                NativeMethods.ReleaseCapture();
                NativeMethods.SendMessage(form.Handle, NativeMethods.WM_NCLBUTTONDOWN,
                    (IntPtr)NativeMethods.HTCAPTION, IntPtr.Zero);
            }
            return;
        }
        down = hit;
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        int hit = HitTest(e.Location);
        int pressed = down;
        down = 0;
        Invalidate();
        if (pressed == 0 || pressed != hit) return;
        if (pressed == 1 && PinClicked != null) PinClicked(this, EventArgs.Empty);
        else if (pressed == 2 && CloseClicked != null) CloseClicked(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (SolidBrush bg = new SolidBrush(T.Bg))
        {
            g.FillRectangle(bg, ClientRectangle);
        }

        int accentX = Ui.Px(14);
        int accentY = Ui.Px(14);
        int accentW = Ui.Px(3);
        int accentH = Height - Ui.Px(28);
        if (accentH > Ui.Px(28)) accentH = Ui.Px(28);
        using (SolidBrush accent = new SolidBrush(T.Accent))
        {
            Ui.FillRound(g, accent,
                new RectangleF(accentX, accentY, accentW, accentH),
                accentW / 2f);
        }

        int textX = Ui.Px(25);
        int rightLimit = PinRect.Left - Ui.Px(10);
        int countWidth = 0;
        if (countText.Length > 0)
        {
            Size measured = TextRenderer.MeasureText(countText, WidgetFonts.Count,
                new Size(1000, 200), TextFormatFlags.NoPadding);
            countWidth = measured.Width + Ui.Px(16);
        }

        int titleWidth = rightLimit - countWidth - textX;
        if (titleWidth < Ui.Px(40)) titleWidth = Ui.Px(40);
        TextRenderer.DrawText(g, title, WidgetFonts.Title,
            new Rectangle(textX, Ui.Px(7), titleWidth, Ui.Px(23)),
            T.Text,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        TextRenderer.DrawText(g, subtitle, WidgetFonts.Subtitle,
            new Rectangle(textX, Ui.Px(29), titleWidth, Ui.Px(17)),
            T.Muted,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);

        if (countText.Length > 0)
        {
            Rectangle countRect = new Rectangle(
                rightLimit - countWidth,
                (Height - Ui.Px(19)) / 2,
                countWidth,
                Ui.Px(19));
            using (SolidBrush fill = new SolidBrush(T.Surface))
            using (Pen border = new Pen(T.Border, 1f))
            {
                Ui.FillRound(g, fill, countRect, countRect.Height / 2f);
                Ui.DrawRound(g, border, countRect, countRect.Height / 2f);
            }
            TextRenderer.DrawText(g, countText, WidgetFonts.Count, countRect,
                T.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }

        DrawButton(g, PinRect, 1, pinned);
        DrawButton(g, CloseRect, 2, false);

        using (Pen divider = new Pen(Ui.Alpha(T.Border, 150), 1f))
        {
            g.DrawLine(divider, Ui.Px(1), Height - 1, Width - Ui.Px(1), Height - 1);
        }
        base.OnPaint(e);
    }

    private void DrawButton(Graphics g, Rectangle rect, int kind, bool active)
    {
        bool isHot = hot == kind;
        bool isDown = down == kind;
        Color fill = kind == 2 && isHot
            ? Ui.Mix(T.Bg, T.Danger, 0.18f)
            : (isHot ? T.SurfaceHover : Color.Empty);
        if (isDown && kind == 2) fill = Ui.Mix(T.Bg, T.Danger, 0.28f);
        else if (isDown) fill = T.SurfaceHover;
        if (fill != Color.Empty)
        {
            using (SolidBrush brush = new SolidBrush(fill))
            {
                Ui.FillRound(g, brush, rect, Ui.Px(8));
            }
        }

        float cx = rect.X + rect.Width / 2f;
        float cy = rect.Y + rect.Height / 2f;
        float s = Ui.Px(1f);

        if (kind == 1)
        {
            Color color = active ? T.Accent : (isHot ? T.Text : T.Muted);
            if (active)
            {
                using (SolidBrush soft = new SolidBrush(T.AccentSoft))
                {
                    Ui.FillRound(g, soft, rect, Ui.Px(8));
                }
            }
            using (SolidBrush brush = new SolidBrush(color))
            using (Pen pen = new Pen(color, 1.7f * s))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.FillEllipse(brush, cx - 3.2f * s, cy - 7.0f * s, 6.4f * s, 6.4f * s);
                PointF[] body = new PointF[]
                {
                    new PointF(cx - 5.1f * s, cy - 0.8f * s),
                    new PointF(cx + 5.1f * s, cy - 0.8f * s),
                    new PointF(cx + 2.4f * s, cy + 3.7f * s),
                    new PointF(cx - 2.4f * s, cy + 3.7f * s)
                };
                g.FillPolygon(brush, body);
                g.DrawLine(pen, cx, cy + 3.5f * s, cx, cy + 7.2f * s);
            }
        }
        else
        {
            Color color = isHot ? T.Danger : T.Muted;
            using (Pen pen = new Pen(color, 1.7f * s))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, cx - 4.2f * s, cy - 4.2f * s, cx + 4.2f * s, cy + 4.2f * s);
                g.DrawLine(pen, cx + 4.2f * s, cy - 4.2f * s, cx - 4.2f * s, cy + 4.2f * s);
            }
        }
    }
}

internal sealed class AddBar : Control
{
    private readonly WidgetTheme T;
    private readonly TextBox input;
    private string cueText;
    private bool canAdd = true;
    private bool plusHot;
    private bool plusDown;

    public event EventHandler AddRequested;

    public AddBar(WidgetTheme theme, string initialCue)
    {
        T = theme;
        cueText = initialCue;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);
        BackColor = T.Bg;
        TabStop = false;

        input = new TextBox();
        input.BorderStyle = BorderStyle.None;
        input.BackColor = T.Surface;
        input.ForeColor = T.Text;
        input.Font = WidgetFonts.Input;
        input.AutoSize = false;
        input.ShortcutsEnabled = true;
        input.TextChanged += delegate { Invalidate(); };
        input.GotFocus += delegate { Invalidate(); };
        input.LostFocus += delegate { Invalidate(); };
        input.KeyDown += InputKeyDown;
        Controls.Add(input);
    }

    public bool CanAdd
    {
        get { return canAdd; }
        set
        {
            if (canAdd == value) return;
            canAdd = value;
            input.ReadOnly = !value;
            input.TabStop = value;
            input.ForeColor = value ? T.Text : T.Muted;
            input.Visible = value;
            plusHot = false;
            plusDown = false;
            Cursor = Cursors.Default;
            ApplyCue();
            Invalidate();
        }
    }

    public void SetCue(string cue)
    {
        cueText = cue;
        ApplyCue();
        Invalidate();
    }

    public void FocusInput()
    {
        if (!canAdd) return;
        input.Focus();
        input.Select(input.TextLength, 0);
    }

    public void ClearInput()
    {
        input.Text = "";
    }

    public string InputText
    {
        get { return input.Text; }
    }

    private Rectangle PlusRect
    {
        get
        {
            int size = Ui.Px(30);
            return new Rectangle(Width - Ui.Px(12) - size,
                (Height - size) / 2, size, size);
        }
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyCue();
        LayoutInput();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        LayoutInput();
    }

    private void LayoutInput()
    {
        if (input == null) return;
        int x = Ui.Px(14);
        int right = PlusRect.Left - Ui.Px(6);
        int width = right - x;
        if (width < Ui.Px(20)) width = Ui.Px(20);
        int textHeight = input.PreferredHeight;
        input.SetBounds(x, (Height - textHeight) / 2 + Ui.Px(1), width, textHeight);
    }

    private void ApplyCue()
    {
        try
        {
            IntPtr handle = input.Handle;
            NativeMethods.SendMessage(handle, NativeMethods.EM_SETCUEBANNER,
                (IntPtr)1, cueText);
        }
        catch
        {
        }
    }

    private void InputKeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode != Keys.Enter) return;
        e.SuppressKeyPress = true;
        e.Handled = true;
        TriggerAdd();
    }

    private void TriggerAdd()
    {
        if (!canAdd) return;
        if (input.Text.Trim().Length == 0) return;
        if (AddRequested != null) AddRequested(this, EventArgs.Empty);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        bool hot = canAdd && PlusRect.Contains(e.Location);
        if (hot != plusHot)
        {
            plusHot = hot;
            Cursor = hot ? Cursors.Hand : Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (plusHot)
        {
            plusHot = false;
            Cursor = Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !canAdd) return;
        if (PlusRect.Contains(e.Location))
        {
            plusDown = true;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;
        bool shouldAdd = plusDown && PlusRect.Contains(e.Location);
        plusDown = false;
        Invalidate();
        if (shouldAdd) TriggerAdd();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (SolidBrush bg = new SolidBrush(T.Bg))
        {
            g.FillRectangle(bg, ClientRectangle);
        }

        RectangleF field = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        using (SolidBrush fill = new SolidBrush(T.Surface))
        {
            Ui.FillRound(g, fill, field, Ui.Px(12));
        }
        Color borderColor = input.Focused && canAdd
            ? Ui.Mix(T.Border, T.Accent, 0.72f)
            : T.Border;
        using (Pen border = new Pen(borderColor, 1f))
        {
            Ui.DrawRound(g, border, field, Ui.Px(12));
        }

        Rectangle plus = PlusRect;
        Color plusFill = canAdd ? T.Accent : T.SurfaceHover;
        if (plusHot) plusFill = Ui.Mix(plusFill, Color.White, 0.10f);
        if (plusDown) plusFill = Ui.Mix(plusFill, Color.Black, 0.10f);
        using (SolidBrush fill = new SolidBrush(plusFill))
        {
            g.FillEllipse(fill, plus);
        }

        Color plusMark = canAdd ? T.Bg : T.Muted;
        float cx = plus.X + plus.Width / 2f;
        float cy = plus.Y + plus.Height / 2f;
        float arm = Ui.Px(5f);
        using (Pen pen = new Pen(plusMark, 1.9f * Ui.Scale))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            g.DrawLine(pen, cx - arm, cy, cx + arm, cy);
            g.DrawLine(pen, cx, cy - arm, cx, cy + arm);
        }

        if (!canAdd)
        {
            int labelX = Ui.Px(14);
            int labelRight = plus.Left - Ui.Px(8);
            Rectangle labelRect = new Rectangle(labelX, 0,
                Math.Max(Ui.Px(20), labelRight - labelX), Height);
            TextRenderer.DrawText(g, cueText, WidgetFonts.Input, labelRect, T.Muted,
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine |
                TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis |
                TextFormatFlags.NoPrefix);
        }
        base.OnPaint(e);
    }
}

internal sealed class ActionButton : Control
{
    private readonly WidgetTheme T;
    private string label = "";
    private bool hot;
    private bool down;

    public ActionButton(WidgetTheme theme)
    {
        T = theme;
        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw |
            ControlStyles.Selectable,
            true);
        BackColor = T.Bg;
        TabStop = true;
        Cursor = Cursors.Hand;
    }

    public string Label
    {
        get { return label; }
        set
        {
            if (label == value) return;
            label = value == null ? "" : value;
            Invalidate();
        }
    }

    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        hot = true;
        Invalidate();
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        hot = false;
        down = false;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left || !Enabled) return;
        down = true;
        Focus();
        Invalidate();
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        down = false;
        Invalidate();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        if (!Enabled) return;
        if (e.KeyCode == Keys.Space || e.KeyCode == Keys.Enter)
        {
            e.Handled = true;
            e.SuppressKeyPress = true;
            OnClick(EventArgs.Empty);
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (SolidBrush bg = new SolidBrush(T.Bg))
        {
            g.FillRectangle(bg, ClientRectangle);
        }

        RectangleF rect = new RectangleF(0.5f, 0.5f, Width - 1f, Height - 1f);
        Color fill;
        Color border;
        Color text;
        if (!Enabled)
        {
            fill = T.Surface;
            border = T.Border;
            text = T.Muted;
        }
        else
        {
            fill = T.AccentSoft;
            if (hot) fill = Ui.Mix(fill, T.Accent, 0.07f);
            if (down) fill = Ui.Mix(fill, T.Accent, 0.12f);
            border = Ui.Mix(T.Border, T.Accent, 0.55f);
            text = T.Accent;
        }

        using (SolidBrush brush = new SolidBrush(fill))
        {
            Ui.FillRound(g, brush, rect, Ui.Px(10));
        }
        using (Pen pen = new Pen(border, 1f))
        {
            Ui.DrawRound(g, pen, rect, Ui.Px(10));
        }
        TextRenderer.DrawText(g, label, WidgetFonts.Action, ClientRectangle, text,
            TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix);
        base.OnPaint(e);
    }
}

internal sealed class TodoListControl : Control
{
    private const int PartNone = 0;
    private const int PartCheck = 1;
    private const int PartDelete = 2;
    private const int PartBody = 3;
    private const int PartScroll = 4;

    private readonly WidgetTheme T;
    private readonly bool allowReorder;
    private readonly bool allowScroll;
    private readonly int rowHeight;
    private readonly int rowGap;

    private List<TodoItem> items = new List<TodoItem>();
    private string emptyTitle = "";
    private string emptyHint = "";

    private int hotIndex = -1;
    private int hotPart = PartNone;
    private int pressedIndex = -1;
    private int pressedPart = PartNone;

    private bool pendingDrag;
    private bool dragging;
    private int dragIndex = -1;
    private int dropIndex = -1;
    private float dragStartY;
    private float dragOffsetY;
    private float dragY;

    private float scrollOffset;
    private bool scrollHot;
    private bool scrollDragging;
    private float scrollGrab;

    public event EventHandler Changed;

    public TodoListControl(WidgetTheme theme, bool allowReorder, bool allowScroll)
    {
        T = theme;
        this.allowReorder = allowReorder;
        this.allowScroll = allowScroll;
        rowHeight = Ui.Px(50);
        rowGap = Ui.Px(4);

        SetStyle(
            ControlStyles.AllPaintingInWmPaint |
            ControlStyles.OptimizedDoubleBuffer |
            ControlStyles.UserPaint |
            ControlStyles.ResizeRedraw,
            true);
        BackColor = T.Bg;
        TabStop = false;
    }

    public List<TodoItem> Items
    {
        get { return items; }
        set
        {
            items = value ?? new List<TodoItem>();
            scrollOffset = 0f;
            hotIndex = -1;
            pressedIndex = -1;
            pendingDrag = false;
            dragging = false;
            Invalidate();
        }
    }

    public string EmptyTitle
    {
        get { return emptyTitle; }
        set { emptyTitle = value ?? ""; Invalidate(); }
    }

    public string EmptyHint
    {
        get { return emptyHint; }
        set { emptyHint = value ?? ""; Invalidate(); }
    }

    public void ResetScroll()
    {
        scrollOffset = 0f;
        Invalidate();
    }

    public void RefreshItems(bool resetScroll)
    {
        if (resetScroll) scrollOffset = 0f;
        ClampScroll();
        if (hotIndex >= items.Count)
        {
            hotIndex = -1;
            hotPart = PartNone;
        }
        Invalidate();
    }

    public void ScrollToEnd()
    {
        if (!allowScroll) return;
        scrollOffset = MaxScroll;
        Invalidate();
    }

    private int ContentWidth
    {
        get { return Width - (allowScroll ? Ui.Px(8) : 0); }
    }

    private float RowPitch
    {
        get { return rowHeight + rowGap; }
    }

    private float ContentHeight
    {
        get
        {
            if (items.Count == 0) return 0f;
            return items.Count * rowHeight + (items.Count - 1) * rowGap;
        }
    }

    private float MaxScroll
    {
        get
        {
            if (!allowScroll) return 0f;
            float max = ContentHeight - Height;
            return max > 0f ? max : 0f;
        }
    }

    private RectangleF RowRect(int index)
    {
        return new RectangleF(0f, index * RowPitch - scrollOffset,
            ContentWidth, rowHeight);
    }

    private RectangleF CheckRect(int index)
    {
        RectangleF row = RowRect(index);
        int box = Ui.Px(18);
        int x = (int)Math.Round(row.X) + (allowReorder ? Ui.Px(34) : Ui.Px(12));
        return new RectangleF(x, row.Y + (rowHeight - box) / 2f, box, box);
    }

    private RectangleF DeleteRect(int index)
    {
        RectangleF row = RowRect(index);
        int size = Ui.Px(28);
        return new RectangleF(row.Right - Ui.Px(9) - size,
            row.Y + (rowHeight - size) / 2f, size, size);
    }

    private RectangleF GripRect(int index)
    {
        RectangleF row = RowRect(index);
        return new RectangleF(row.X + Ui.Px(10),
            row.Y + Ui.Px(10), Ui.Px(12), rowHeight - Ui.Px(20));
    }

    private int HitRow(Point point)
    {
        if (items.Count == 0) return -1;
        float adjusted = point.Y + scrollOffset;
        if (adjusted < 0f) return -1;
        int index = (int)Math.Floor(adjusted / RowPitch);
        if (index < 0 || index >= items.Count) return -1;
        if (!RowRect(index).Contains(point.X, point.Y)) return -1;
        return index;
    }

    private static bool InflatedContains(RectangleF rect, Point point, int amount)
    {
        RectangleF inflated = rect;
        inflated.Inflate(amount, amount);
        return inflated.Contains(point.X, point.Y);
    }

    private int PartAt(Point point, int index)
    {
        if (index < 0) return PartNone;
        if (InflatedContains(CheckRect(index), point, Ui.Px(5))) return PartCheck;
        if (DeleteRect(index).Contains(point.X, point.Y)) return PartDelete;
        return PartBody;
    }

    private RectangleF ScrollThumbRect()
    {
        if (MaxScroll <= 0f) return RectangleF.Empty;
        float trackHeight = Height;
        float content = ContentHeight;
        float thumbHeight = content <= 0f ? trackHeight : trackHeight * trackHeight / content;
        float minimum = Ui.Px(26);
        if (thumbHeight < minimum) thumbHeight = minimum;
        if (thumbHeight > trackHeight) thumbHeight = trackHeight;
        float top = 0f;
        if (MaxScroll > 0f)
        {
            top = (scrollOffset / MaxScroll) * (trackHeight - thumbHeight);
        }
        return new RectangleF(Width - Ui.Px(6), top, Ui.Px(3), thumbHeight);
    }

    private bool IsScrollHit(Point point)
    {
        if (MaxScroll <= 0f) return false;
        return point.X >= Width - Ui.Px(13);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);

        if (scrollDragging)
        {
            float thumbHeight = ScrollThumbRect().Height;
            float track = Height - thumbHeight;
            float top = e.Y - scrollGrab;
            if (top < 0f) top = 0f;
            if (top > track) top = track;
            float ratio = track <= 0f ? 0f : top / track;
            scrollOffset = ratio * MaxScroll;
            Invalidate();
            return;
        }

        if (pendingDrag && !dragging && e.Button == MouseButtons.Left)
        {
            if (Math.Abs(e.Y - dragStartY) > Ui.Px(4))
            {
                dragging = true;
                dropIndex = ComputeDropIndex(e.Y);
                hotIndex = -1;
                hotPart = PartNone;
                Cursor = Cursors.SizeNS;
                Invalidate();
            }
        }

        if (dragging)
        {
            dragY = e.Y - dragOffsetY;
            dropIndex = ComputeDropIndex(e.Y);
            Invalidate();
            return;
        }

        int index = -1;
        int part = PartNone;
        bool scroll = IsScrollHit(e.Location);
        if (scroll)
        {
            part = PartScroll;
            index = -1;
        }
        else
        {
            index = HitRow(e.Location);
            if (index >= 0) part = PartAt(e.Location, index);
        }

        bool hotChanged = index != hotIndex || part != hotPart || scroll != scrollHot;
        hotIndex = index;
        hotPart = part;
        scrollHot = scroll;
        if (hotChanged)
        {
            if (part == PartScroll) Cursor = Cursors.Hand;
            else if (part == PartCheck || part == PartDelete) Cursor = Cursors.Hand;
            else if (part == PartBody && allowReorder) Cursor = Cursors.SizeNS;
            else Cursor = Cursors.Default;
            Invalidate();
        }
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        if (dragging || scrollDragging) return;
        hotIndex = -1;
        hotPart = PartNone;
        scrollHot = false;
        Cursor = Cursors.Default;
        Invalidate();
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        base.OnMouseDown(e);
        if (e.Button != MouseButtons.Left) return;

        if (IsScrollHit(e.Location))
        {
            RectangleF thumb = ScrollThumbRect();
            if (thumb == RectangleF.Empty) return;
            scrollGrab = e.Y - thumb.Y;
            if (scrollGrab < 0f || scrollGrab > thumb.Height)
            {
                scrollGrab = thumb.Height / 2f;
                float track = Height - thumb.Height;
                float top = e.Y - scrollGrab;
                if (top < 0f) top = 0f;
                if (top > track) top = track;
                scrollOffset = track <= 0f ? 0f : (top / track) * MaxScroll;
            }
            scrollDragging = true;
            Capture = true;
            Invalidate();
            return;
        }

        int index = HitRow(e.Location);
        if (index < 0) return;
        int part = PartAt(e.Location, index);
        pressedIndex = index;
        pressedPart = part;

        if (part == PartCheck || part == PartDelete)
        {
            Capture = true;
            Invalidate();
            return;
        }

        if (part == PartBody && allowReorder)
        {
            pendingDrag = true;
            dragging = false;
            dragIndex = index;
            dragStartY = e.Y;
            dragOffsetY = e.Y - RowRect(index).Y;
            dropIndex = index;
            Capture = true;
            Cursor = Cursors.SizeNS;
            Invalidate();
        }
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        base.OnMouseUp(e);
        if (e.Button != MouseButtons.Left) return;

        if (scrollDragging)
        {
            scrollDragging = false;
            Capture = false;
            Invalidate();
            return;
        }

        if (dragging)
        {
            int from = dragIndex;
            int moved = dragIndex;
            if (dragIndex >= 0 && dragIndex < items.Count)
            {
                TodoItem draggedItem = items[dragIndex];
                items.RemoveAt(dragIndex);
                int insert = dropIndex;
                if (insert < 0) insert = 0;
                if (insert > items.Count) insert = items.Count;
                if (insert == dragIndex) insert = Math.Min(insert, items.Count);
                items.Insert(insert, draggedItem);
                moved = insert;
            }

            dragging = false;
            pendingDrag = false;
            dragIndex = -1;
            pressedIndex = -1;
            pressedPart = PartNone;
            Capture = false;
            Cursor = Cursors.Default;
            Invalidate();
            if (moved != from) RaiseChanged();
            return;
        }

        int index = HitRow(e.Location);
        int part = PartAt(e.Location, index);
        int hitIndex = pressedIndex;
        int hitPart = pressedPart;
        pendingDrag = false;
        pressedIndex = -1;
        pressedPart = PartNone;
        Capture = false;

        if (hitIndex >= 0 && hitIndex == index && hitPart == part)
        {
            if (part == PartCheck)
            {
                if (hitIndex < items.Count)
                {
                    items[hitIndex].Done = !items[hitIndex].Done;
                    Invalidate();
                    RaiseChanged();
                }
            }
            else if (part == PartDelete)
            {
                if (hitIndex < items.Count)
                {
                    items.RemoveAt(hitIndex);
                    ClampScroll();
                    hotIndex = -1;
                    hotPart = PartNone;
                    Invalidate();
                    RaiseChanged();
                }
            }
        }
        else
        {
            Invalidate();
        }
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        base.OnMouseWheel(e);
        if (!allowScroll || MaxScroll <= 0f) return;
        int steps = e.Delta / 120;
        if (steps == 0) steps = e.Delta > 0 ? 1 : -1;
        scrollOffset -= steps * Ui.Px(54);
        ClampScroll();
        Invalidate();
        hotIndex = -1;
        hotPart = PartNone;
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        ClampScroll();
    }

    private void ClampScroll()
    {
        float max = MaxScroll;
        if (scrollOffset < 0f) scrollOffset = 0f;
        if (scrollOffset > max) scrollOffset = max;
    }

    private int ComputeDropIndex(float mouseY)
    {
        int target = 0;
        for (int i = 0; i < items.Count; i++)
        {
            if (i == dragIndex) continue;
            float middle = RowRect(i).Y + rowHeight / 2f;
            if (mouseY > middle) target++;
        }
        return target;
    }

    private float DropLineY(int target)
    {
        List<int> remaining = new List<int>();
        for (int i = 0; i < items.Count; i++)
        {
            if (i != dragIndex) remaining.Add(i);
        }
        if (remaining.Count == 0) return Height / 2f;
        if (target <= 0)
        {
            return RowRect(remaining[0]).Y - rowGap / 2f;
        }
        if (target >= remaining.Count)
        {
            int last = remaining[remaining.Count - 1];
            return RowRect(last).Bottom + rowGap / 2f;
        }
        int before = remaining[target];
        return RowRect(before).Y - rowGap / 2f;
    }

    private void RaiseChanged()
    {
        if (Changed != null) Changed(this, EventArgs.Empty);
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        Graphics g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        using (SolidBrush bg = new SolidBrush(T.Bg))
        {
            g.FillRectangle(bg, ClientRectangle);
        }

        if (items.Count == 0)
        {
            DrawEmptyState(g);
        }
        else
        {
            for (int i = 0; i < items.Count; i++)
            {
                RectangleF row = RowRect(i);
                if (row.Bottom < -rowHeight || row.Y > Height + rowHeight) continue;
                if (dragging && i == dragIndex)
                {
                    RectangleF slot = new RectangleF(row.X + 1f, row.Y + 1f,
                        row.Width - 2f, row.Height - 2f);
                    using (Pen dashed = new Pen(Ui.Alpha(T.Border, 210), 1f))
                    {
                        dashed.DashStyle = DashStyle.Dash;
                        Ui.DrawRound(g, dashed, slot, Ui.Px(10));
                    }
                    continue;
                }
                DrawRow(g, i, row, false);
            }
        }

        if (dragging)
        {
            if (dropIndex >= 0)
            {
                float y = DropLineY(dropIndex);
                float x1 = Ui.Px(12);
                float x2 = Math.Max(x1 + Ui.Px(20), ContentWidth - Ui.Px(12));
                using (Pen line = new Pen(T.Accent, Math.Max(1.5f, 1.7f * Ui.Scale)))
                {
                    line.StartCap = LineCap.Round;
                    line.EndCap = LineCap.Round;
                    g.DrawLine(line, x1, y, x2, y);
                }
                using (SolidBrush dot = new SolidBrush(T.Accent))
                {
                    float d = Ui.Px(5);
                    g.FillEllipse(dot, x1 - d / 2f, y - d / 2f, d, d);
                    g.FillEllipse(dot, x2 - d / 2f, y - d / 2f, d, d);
                }
            }

            if (dragIndex >= 0 && dragIndex < items.Count)
            {
                RectangleF ghost = RowRect(dragIndex);
                ghost.Y = dragY;
                using (SolidBrush shadow = new SolidBrush(Color.FromArgb(75, 0, 0, 0)))
                {
                    RectangleF shadowRect = ghost;
                    shadowRect.Offset(Ui.Px(1), Ui.Px(4));
                    Ui.FillRound(g, shadow, shadowRect, Ui.Px(10));
                }
                DrawRow(g, dragIndex, ghost, true);
            }
        }

        DrawScrollbar(g);
        base.OnPaint(e);
    }

    private void DrawRow(Graphics g, int index, RectangleF row, bool ghost)
    {
        TodoItem item = items[index];
        bool isHot = !ghost && index == hotIndex;

        Color fill = isHot ? T.RowHover : T.Row;
        if (ghost) fill = T.AccentSoft;
        if (item.Done && !ghost) fill = isHot ? T.RowHover : Ui.Mix(T.Row, T.Bg, 0.16f);

        using (SolidBrush brush = new SolidBrush(fill))
        {
            Ui.FillRound(g, brush, row, Ui.Px(10));
        }
        if (ghost)
        {
            using (Pen border = new Pen(Ui.Mix(T.Accent, T.Bg, 0.18f), 1.2f))
            {
                Ui.DrawRound(g, border, row, Ui.Px(10));
            }
        }

        if (allowReorder)
        {
            Color gripColor = ghost ? T.Muted : Ui.Mix(T.Row, T.Muted, 0.42f);
            using (SolidBrush brush = new SolidBrush(gripColor))
            {
                float dot = Math.Max(1.5f, 1.7f * Ui.Scale);
                float gx = row.X + Ui.Px(14);
                float gy = row.Y + row.Height / 2f - Ui.Px(6);
                for (int c = 0; c < 2; c++)
                {
                    for (int rr = 0; rr < 3; rr++)
                    {
                        g.FillEllipse(brush,
                            gx + c * Ui.Px(4) - dot / 2f,
                            gy + rr * Ui.Px(6) - dot / 2f,
                            dot, dot);
                    }
                }
            }
        }

        RectangleF check = CheckRect(index);
        bool checkHot = isHot && hotPart == PartCheck && !ghost;
        if (item.Done)
        {
            using (SolidBrush fillBrush = new SolidBrush(ghost ? T.Accent : T.Accent))
            {
                Ui.FillRound(g, fillBrush, check, Ui.Px(5));
            }
            Color mark = Ui.Mix(T.Bg, Color.White, 0.88f);
            using (Pen pen = new Pen(mark, Math.Max(1.8f, 2.0f * Ui.Scale)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                float x = check.X;
                float y = check.Y;
                g.DrawLine(pen, x + Ui.Px(4.2f), y + Ui.Px(9.2f),
                    x + Ui.Px(7.5f), y + Ui.Px(12.4f));
                g.DrawLine(pen, x + Ui.Px(7.5f), y + Ui.Px(12.4f),
                    x + Ui.Px(13.6f), y + Ui.Px(5.4f));
            }
        }
        else
        {
            Color borderColor = checkHot ? T.Accent : T.CheckBorder;
            using (Pen pen = new Pen(borderColor, Math.Max(1.2f, 1.3f * Ui.Scale)))
            {
                Ui.DrawRound(g, pen, check, Ui.Px(5));
            }
        }

        Font font = item.Done ? WidgetFonts.RowDone : WidgetFonts.Row;
        Color textColor = item.Done
            ? Ui.Mix(T.Row, T.Muted, 0.82f)
            : T.Text;
        if (ghost) textColor = T.Text;

        int textX = (int)Math.Round(check.Right + Ui.Px(10));
        int textRight = (int)Math.Round(row.Right - Ui.Px(38));
        Rectangle textRect = new Rectangle(textX,
            (int)Math.Round(row.Y + Ui.Px(6)),
            Math.Max(Ui.Px(10), textRight - textX),
            (int)Math.Round(row.Height - Ui.Px(12)));
        TextRenderer.DrawText(g, item.Text, font, textRect, textColor,
            TextFormatFlags.NoPadding | TextFormatFlags.SingleLine |
            TextFormatFlags.EndEllipsis | TextFormatFlags.VerticalCenter |
            TextFormatFlags.NoPrefix);

        if (!ghost)
        {
            RectangleF del = DeleteRect(index);
            bool deleteHot = isHot && hotPart == PartDelete;
            Color delColor = deleteHot
                ? T.Danger
                : Ui.Mix(fill, T.Muted, 0.74f);
            float cx = del.X + del.Width / 2f;
            float cy = del.Y + del.Height / 2f;
            float arm = Ui.Px(3.8f);
            using (Pen pen = new Pen(delColor, Math.Max(1.3f, 1.4f * Ui.Scale)))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap = LineCap.Round;
                g.DrawLine(pen, cx - arm, cy - arm, cx + arm, cy + arm);
                g.DrawLine(pen, cx + arm, cy - arm, cx - arm, cy + arm);
            }
        }
    }

    private void DrawEmptyState(Graphics g)
    {
        float iconSize = Ui.Px(42);
        float cx = ContentWidth / 2f;
        float centerY = Height / 2f - Ui.Px(12);
        RectangleF icon = new RectangleF(cx - iconSize / 2f,
            centerY - iconSize - Ui.Px(6), iconSize, iconSize);

        using (SolidBrush fill = new SolidBrush(T.Surface))
        {
            g.FillEllipse(fill, icon);
        }
        using (Pen border = new Pen(T.Border, 1f))
        {
            g.DrawEllipse(border, icon);
        }
        Color mark = Ui.Mix(T.Surface, T.Accent, 0.9f);
        using (Pen pen = new Pen(mark, Math.Max(1.8f, 2f * Ui.Scale)))
        {
            pen.StartCap = LineCap.Round;
            pen.EndCap = LineCap.Round;
            float x = icon.X;
            float y = icon.Y;
            g.DrawLine(pen, x + icon.Width * 0.29f, y + icon.Height * 0.53f,
                x + icon.Width * 0.45f, y + icon.Height * 0.68f);
            g.DrawLine(pen, x + icon.Width * 0.45f, y + icon.Height * 0.68f,
                x + icon.Width * 0.72f, y + icon.Height * 0.34f);
        }

        if (emptyTitle.Length > 0)
        {
            int width = ContentWidth;
            TextRenderer.DrawText(g, emptyTitle, WidgetFonts.Empty,
                new Rectangle(0, (int)Math.Round(centerY + Ui.Px(2)), width, Ui.Px(22)),
                T.Text,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top |
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
        if (emptyHint.Length > 0)
        {
            int width = ContentWidth;
            TextRenderer.DrawText(g, emptyHint, WidgetFonts.EmptyHint,
                new Rectangle(0, (int)Math.Round(centerY + Ui.Px(25)), width, Ui.Px(19)),
                T.Muted,
                TextFormatFlags.HorizontalCenter | TextFormatFlags.Top |
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine);
        }
    }

    private void DrawScrollbar(Graphics g)
    {
        if (!allowScroll || MaxScroll <= 0f) return;
        RectangleF track = new RectangleF(Width - Ui.Px(6), 0f,
            Ui.Px(3), Height);
        using (SolidBrush brush = new SolidBrush(Ui.Alpha(T.Muted, 28)))
        {
            Ui.FillRound(g, brush, track, track.Width / 2f);
        }
        RectangleF thumb = ScrollThumbRect();
        Color color = scrollHot || scrollDragging
            ? Ui.Mix(T.Muted, T.Accent, 0.42f)
            : Ui.Mix(T.Bg, T.Muted, 0.46f);
        using (SolidBrush brush = new SolidBrush(color))
        {
            Ui.FillRound(g, brush, thumb, thumb.Width / 2f);
        }
    }
}
