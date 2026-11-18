using System;
using System.Drawing;
using System.Windows.Forms;

internal sealed class QuickListForm : FloatingWidgetForm
{
    private const int MaxItems = 5;
    private readonly TodoListControl list;
    private readonly AddBar addBar;

    public QuickListForm()
        : base(WidgetThemes.Quick, new WidgetStorage("quick-list"),
            "快捷清单", "拖动排序 · 上限 5 项")
    {
        ClientSize = new Size(S(364), S(394));

        list = new TodoListControl(T, true, false);
        list.EmptyTitle = "清单是空的";
        list.EmptyHint = "在下方添加待办，拖动行可排序";
        list.SetBounds(S(12), S(64), S(340), S(266));
        list.Changed += delegate { OnItemsChanged(); };
        Controls.Add(list);

        addBar = new AddBar(T, "添加待办，回车确认");
        addBar.SetBounds(S(12), S(338), S(340), S(46));
        addBar.AddRequested += delegate { AddItem(); };
        Controls.Add(addBar);
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
        Storage.Save();
        base.OnFormClosing(e);
    }

    private void AddItem()
    {
        if (Storage.Items.Count >= MaxItems) return;
        string text = Clean(Storage.Items, addBar.InputText);
        if (text.Length == 0) return;
        Storage.Items.Add(new TodoItem(text, false));
        addBar.ClearInput();
        UpdateChrome();
        list.RefreshItems(false);
        Storage.Save();
        addBar.FocusInput();
    }

    private static string Clean(System.Collections.Generic.List<TodoItem> items, string raw)
    {
        if (raw == null) return "";
        string text = raw.Trim();
        if (text.Length > 120) text = text.Substring(0, 120);
        return text;
    }

    private void OnItemsChanged()
    {
        UpdateChrome();
        Storage.Save();
    }

    private void UpdateChrome()
    {
        int count = Storage.Items.Count;
        Header.CountText = count + " / " + MaxItems;
        bool canAdd = count < MaxItems;
        addBar.SetCue(canAdd ? "添加待办，回车确认" : "已达 5 项上限");
        addBar.CanAdd = canAdd;
    }
}

internal static class QuickListProgram
{
    [STAThread]
    private static void Main()
    {
        try
        {
            NativeMethods.SetProcessDPIAware();
            if (!AppBootstrap.Claim("FloatingQuickListWidget_v1", "快捷清单")) return;
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Ui.Init();
            Application.Run(new QuickListForm());
        }
        catch (Exception ex)
        {
            MessageBox.Show("快捷清单启动失败：\r\n" + ex.Message,
                "快捷清单", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
