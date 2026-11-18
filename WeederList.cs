using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

internal sealed class WeederForm : FloatingWidgetForm
{
    private readonly TodoListControl list;
    private readonly AddBar addBar;
    private readonly ActionButton action;
    private readonly Timer undoTimer;

    private List<TodoItem> undoItems;
    private int undoCount;

    public WeederForm()
        : base(WidgetThemes.Weeder, new WidgetStorage("weeder-list"),
            "除草器", "勾选后一键清除")
    {
        ClientSize = new Size(S(364), S(496));

        list = new TodoListControl(T, false, true);
        list.EmptyTitle = "没有杂草";
        list.EmptyHint = "添加事项后，勾选并一键清除";
        list.SetBounds(S(12), S(64), S(340), S(320));
        list.Changed += delegate { OnItemsChanged(); };
        Controls.Add(list);

        action = new ActionButton(T);
        action.Label = "暂无已勾选项";
        action.Enabled = false;
        action.SetBounds(S(12), S(392), S(340), S(40));
        action.Click += delegate { OnAction(); };
        Controls.Add(action);

        addBar = new AddBar(T, "添加事项，回车确认");
        addBar.SetBounds(S(12), S(440), S(340), S(46));
        addBar.AddRequested += delegate { AddItem(); };
        Controls.Add(addBar);

        undoTimer = new Timer();
        undoTimer.Interval = 5000;
        undoTimer.Tick += delegate
        {
            undoTimer.Stop();
            undoItems = null;
            undoCount = 0;
            UpdateChrome();
        };
    }

    protected override Point GetDefaultLocation()
    {
        Rectangle area = Screen.PrimaryScreen.WorkingArea;
        int x = area.Right - Width * 2 - S(36);
        if (x < area.Left + S(20)) x = area.Left + S(20);
        return new Point(x, area.Top + S(20));
    }

    protected override void OnStateReady()
    {
        list.Items = Storage.Items;
        UpdateChrome();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        addBar.FocusInput();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        undoTimer.Stop();
        undoTimer.Dispose();
        Storage.Save();
        base.OnFormClosing(e);
    }

    private void AddItem()
    {
        string raw = addBar.InputText;
        string text = raw == null ? "" : raw.Trim();
        if (text.Length == 0) return;
        if (text.Length > 240) text = text.Substring(0, 240);

        CancelUndo();
        Storage.Items.Add(new TodoItem(text, false));
        addBar.ClearInput();
        UpdateChrome();
        list.ScrollToEnd();
        Storage.Save();
        addBar.FocusInput();
    }

    private void OnItemsChanged()
    {
        CancelUndo();
        UpdateChrome();
        Storage.Save();
    }

    private void OnAction()
    {
        if (undoItems != null)
        {
            UndoClear();
            return;
        }
        ClearChecked();
    }

    private void ClearChecked()
    {
        int checkedCount = CountChecked();
        if (checkedCount == 0) return;

        undoItems = new List<TodoItem>();
        for (int i = 0; i < Storage.Items.Count; i++)
        {
            undoItems.Add(Storage.Items[i].Clone());
        }
        undoCount = checkedCount;

        Storage.Items.RemoveAll(delegate(TodoItem item) { return item.Done; });
        undoTimer.Stop();
        undoTimer.Start();

        list.RefreshItems(true);
        UpdateChrome();
        Storage.Save();
        addBar.FocusInput();
    }

    private void UndoClear()
    {
        if (undoItems == null) return;
        Storage.Items.Clear();
        for (int i = 0; i < undoItems.Count; i++)
        {
            Storage.Items.Add(undoItems[i].Clone());
        }
        undoItems = null;
        undoCount = 0;
        undoTimer.Stop();

        list.RefreshItems(true);
        UpdateChrome();
        Storage.Save();
        addBar.FocusInput();
    }

    private void CancelUndo()
    {
        if (undoItems == null) return;
        undoItems = null;
        undoCount = 0;
        undoTimer.Stop();
    }

    private int CountChecked()
    {
        int count = 0;
        for (int i = 0; i < Storage.Items.Count; i++)
        {
            if (Storage.Items[i].Done) count++;
        }
        return count;
    }

    private void UpdateChrome()
    {
        int total = Storage.Items.Count;
        int checkedCount = CountChecked();
        Header.CountText = total + " 项";

        if (undoItems != null)
        {
            action.Label = "已清除 " + undoCount + " 项 · 点此撤回";
            action.Enabled = true;
        }
        else if (checkedCount > 0)
        {
            action.Label = "一键清除已勾选 · " + checkedCount;
            action.Enabled = true;
        }
        else
        {
            action.Label = "暂无已勾选项";
            action.Enabled = false;
        }

        addBar.SetCue("添加事项，回车确认");
        addBar.CanAdd = true;
    }
}

internal static class WeederListProgram
{
    [STAThread]
    private static void Main()
    {
        try
        {
            NativeMethods.SetProcessDPIAware();
            if (!AppBootstrap.Claim("FloatingWeederListWidget_v1", "除草器")) return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Ui.Init();
            Application.Run(new WeederForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show("除草器启动失败：\r\n" + ex.Message,
                "除草器", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
