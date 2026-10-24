using System.Diagnostics;
namespace Wholesale.AdminWinForms;

public sealed class DashboardForm : Form
{
    private readonly SqlAdminStore store = new();
    private List<ItemRow> items = []; private List<AgentRow> agents = []; private List<OrderRow> orders = [];
    private readonly DataGridView itemGrid = UiTheme.Grid(), agentGrid = UiTheme.Grid(), orderGrid = UiTheme.Grid();
    private readonly TabControl tabs = new() { Dock = DockStyle.Fill, Appearance = TabAppearance.FlatButtons, SizeMode = TabSizeMode.Fixed, ItemSize = new(0, 1) };
    private readonly Label status = new() { Dock = DockStyle.Bottom, Height = 32, TextAlign = ContentAlignment.MiddleLeft, Padding = new(20, 0, 0, 0), ForeColor = UiTheme.Muted };
    private readonly FlowLayoutPanel metrics = new() { Dock = DockStyle.Top, Height = 115, Padding = new(0, 10, 0, 10), WrapContents = false };
    private readonly TextBox search = new() { Width = 300, PlaceholderText = "Search items or agents…", Margin = new(0, 9, 18, 0) };
    private readonly ComboBox orderStatus = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 170, Margin = new(0, 9, 0, 0) };
    private readonly RichTextBox report = new() { Dock = DockStyle.Fill, ReadOnly = true, BorderStyle = BorderStyle.None, BackColor = Color.White, Font = new("Consolas", 11), ForeColor = UiTheme.Ink };
    private readonly ComboBox reportAgent = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 230, DisplayMember = "Name" };
    private readonly DateTimePicker from = new() { Format = DateTimePickerFormat.Short, Width = 130, Value = DateTime.Today.AddMonths(-1) }, to = new() { Format = DateTimePickerFormat.Short, Width = 130, Value = DateTime.Today };
    private readonly string[] args; private bool busy; private readonly Dictionary<string, Image> photoCache = [];
    public DashboardForm(string[]? args = null)
    {
        this.args = args ?? []; Text = "Northstar Wholesale | Back office"; Size = new(1380, 980); MinimumSize = new(1150, 760); StartPosition = FormStartPosition.CenterScreen; BackColor = UiTheme.Background; Font = new("Segoe UI", 10); AutoScaleMode = AutoScaleMode.Dpi;
        var shell = new Panel { Dock = DockStyle.Fill, Padding = new(28, 22, 28, 18) }; Controls.Add(shell); Controls.Add(status);
        var sidebar = new Panel { Dock = DockStyle.Left, Width = 205, BackColor = UiTheme.Ink }; Controls.Add(sidebar);
        sidebar.Controls.Add(new Label { Text = "✳ NORTHSTAR", ForeColor = Color.White, Font = new("Segoe UI", 17, FontStyle.Bold), Location = new(18, 29), AutoSize = true });
        sidebar.Controls.Add(new Label { Text = "WAREHOUSE WORKSPACE", ForeColor = Color.FromArgb(171, 196, 182), Font = new("Segoe UI", 8), Location = new(21, 68), AutoSize = true });
        var names = new[] { "▦  Items & inventory", "◉  Agent partners", "▤  Incoming orders", "↗  Sales dashboard" };
        for (var i = 0; i < names.Length; i++) { var index = i; var b = UiTheme.Button(names[i], (_, _) => { tabs.SelectedIndex = index; Filter(); }); b.SetBounds(12, 130 + i * 61, 181, 48); b.AutoSize = false; b.TextAlign = ContentAlignment.MiddleLeft; b.BackColor = UiTheme.Ink; b.ForeColor = Color.White; b.FlatAppearance.BorderSize = 0; sidebar.Controls.Add(b); }
        sidebar.Controls.Add(new Label { Text = "ADMIN ACCESS\n\nClassroom demonstration\nConnected to SQL Server", Location = new(20, 480), Size = new(170, 100), ForeColor = Color.FromArgb(171, 196, 182), Font = new("Segoe UI", 9) });
        var logout = UiTheme.Button("Sign out", (_, _) => Close()); logout.SetBounds(18, 620, 165, 42); logout.AutoSize = false; sidebar.Controls.Add(logout);
        shell.Controls.Add(tabs); shell.Controls.Add(metrics);
        var top = new Panel { Dock = DockStyle.Top, Height = 94 }; shell.Controls.Add(top);
        top.Controls.Add(new Label { Text = "Your warehouse, in view.", Location = new(0, 0), AutoSize = true, Font = new("Segoe UI", 26, FontStyle.Bold), ForeColor = UiTheme.Ink });
        top.Controls.Add(new Label { Text = "Manage stock, support your partners, and keep orders moving.", Location = new(2, 52), AutoSize = true, ForeColor = UiTheme.Muted });
        var refresh = UiTheme.Button("↻ Refresh", async (_, _) => await Reload(), true); refresh.SetBounds(950, 10, 125, 44); refresh.Anchor = AnchorStyles.Top | AnchorStyles.Right; refresh.AutoSize = false; top.Controls.Add(refresh);
        var ai=UiTheme.Button("✦ AI guide",(_,_)=>{var entries=new List<Lab.Intelligence.KnowledgeEntry>{
          new("stock","stock inventory low stock ton kho sap het",string.Join("\n",items.Where(x=>x.Stock<=10).Select(x=>$"{x.Name}: {x.Stock} units · {x.Price:N0} VND")) is {Length:>0} low?low:"No active products are at the low-stock threshold."),
          new("orders","order status dispatch processing shipping trang thai don hang",$"Snapshot: {orders.Count(x=>x.Status is "Pending" or "Processing")} open orders. Select an order to advance Pending → Processing → Shipped."),
          new("cancel","cancel restore stock huy don hoan kho","Only Pending or Processing orders can be cancelled. All quantities are returned in one transaction. A paid classroom order is marked Refunded in that same transaction. A repeated cancellation is rejected."),
          new("manifest","print invoice manifest pdf in phieu giao hang","Select an order, then Export manifest / PDF. The export includes delivery address, quantities and totals. Use your browser's Print / Save PDF."),
          new("account","agent register login locked tai khoan dang nhap khoa","Register agent creates the company and its portal login together. Use Unlock account for an Agent locked after five incorrect passwords.")
        };entries.AddRange(items.Select(x=>new Lab.Intelligence.KnowledgeEntry("product-"+x.Id,x.Name,$"{x.Name} · {x.Size} · {x.Price:N0} VND · {x.Stock} units (last refresh).")));using var guide=new LabUi.AssistantDialog("Northstar · Local AI guide",entries);guide.ShowDialog(this);});ai.SetBounds(790,10,130,44);ai.AutoSize=false;ai.Anchor=AnchorStyles.Top|AnchorStyles.Right;top.Controls.Add(ai);
        var colors=new ComboBox{Location=new(790,58),Width=130,DropDownStyle=ComboBoxStyle.DropDownList,Anchor=AnchorStyles.Top|AnchorStyles.Right};colors.Items.AddRange(["Forest","Sunset","Lilac"]);colors.SelectedIndex=0;top.Controls.Add(colors);colors.SelectedIndexChanged+=(_,_)=>{UiTheme.Green=colors.SelectedIndex switch{1=>Color.FromArgb(166,76,54),2=>Color.FromArgb(107,79,153),_=>Color.FromArgb(36,113,93)};void Apply(Control root){foreach(Control c in root.Controls){if(c is Button b&&b.Tag as string=="primary")b.BackColor=UiTheme.Green;Apply(c);}}Apply(this);};

        var itemTab = Page("Items", itemGrid); var itemBar = Bar(itemTab);
        itemBar.Controls.Add(UiTheme.Button("+ Add product", (_, _) => { using var e = new ItemEditor(); if (e.ShowDialog(this) == DialogResult.OK) Run(() => store.SaveItem(e.Result)); }, true));
        itemBar.Controls.Add(UiTheme.Button("Edit product", (_, _) => { if (itemGrid.CurrentRow?.DataBoundItem is ItemRow row) { using var e = new ItemEditor(row); if (e.ShowDialog(this) == DialogResult.OK) Run(() => store.SaveItem(e.Result)); } }));
        itemBar.Controls.Add(UiTheme.Button("Adjust stock", (_, _) => { if (itemGrid.CurrentRow?.DataBoundItem is ItemRow row) { using var e = new StockEditor(row); if (e.ShowDialog(this) == DialogResult.OK) Run(() => store.ChangeStock(row.Id, e.Delta)); } }));
        itemBar.Controls.Add(UiTheme.Button("Deactivate", (_, _) => { if (itemGrid.CurrentRow?.DataBoundItem is ItemRow row && Confirm($"Deactivate {row.Name}? Historical invoices are retained.")) Run(() => store.DeactivateItem(row.Id)); }));
        var agentTab = Page("Agents", agentGrid); var agentBar = Bar(agentTab);
        agentBar.Controls.Add(UiTheme.Button("+ Register agent", (_, _) => { using var e = new AgentEditor(); if (e.ShowDialog(this) == DialogResult.OK) Run(() => store.RegisterAgent(e.Result, e.UserName, e.Email, e.Password)); }, true));
        agentBar.Controls.Add(UiTheme.Button("Edit company", (_, _) => { if (agentGrid.CurrentRow?.DataBoundItem is AgentRow row) { using var e = new AgentEditor(row); if (e.ShowDialog(this) == DialogResult.OK) Run(() => store.SaveAgent(e.Result)); } }));
        agentBar.Controls.Add(UiTheme.Button("Deactivate", (_, _) => { if (agentGrid.CurrentRow?.DataBoundItem is AgentRow row && Confirm($"Deactivate {row.Name}? Agents with open orders must be resolved first.")) Run(() => store.DeactivateAgent(row.Id)); }));
        agentBar.Controls.Add(UiTheme.Button("Unlock account", (_, _) => { using var e = new TextEntry("Unlock agent account", "Agent username"); if (e.ShowDialog(this) == DialogResult.OK) Run(() => store.UnlockUser(e.Value)); }));
        var orderTab = Page("Orders", orderGrid); var orderBar = Bar(orderTab);
        orderBar.Controls.Add(UiTheme.Button("Advance status →", (_, _) => { if (orderGrid.CurrentRow?.DataBoundItem is OrderRow row) { var next = row.Status switch { "Pending" => "Processing", "Processing" => "Shipped", _ => null }; if (next is null) { status.Text = "This order has reached its final status."; return; } if (Confirm($"Move order #{row.Id} from {row.Status} to {next}?")) Run(() => store.AdvanceOrder(row.Id, row.Status, next)); } }, true));
        orderBar.Controls.Add(UiTheme.Button("Cancel & restore stock", (_, _) => { if (orderGrid.CurrentRow?.DataBoundItem is OrderRow row && Confirm($"Cancel order #{row.Id}? Reserved quantities will be returned to stock. Any paid classroom payment will be marked refunded.")) Run(() => store.AdvanceOrder(row.Id, row.Status, "Cancelled")); }));
        orderBar.Controls.Add(UiTheme.Button("Order details", (_, _) => { if (orderGrid.CurrentRow?.DataBoundItem is OrderRow row) ShowManifest(row.Id); }));
        orderBar.Controls.Add(UiTheme.Button("Export manifest / PDF", async (_, _) => { if (orderGrid.CurrentRow?.DataBoundItem is OrderRow row) await ExportManifest(row.Id); }));
        var reportTab = new TabPage("Dashboard") { BackColor = Color.White, Padding = new(20) }; reportTab.Controls.Add(report); tabs.TabPages.Add(reportTab);
        var reportFilters = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 65, WrapContents = false }; reportTab.Controls.Add(reportFilters);
        reportFilters.Controls.Add(reportAgent); reportFilters.Controls.Add(from); reportFilters.Controls.Add(to); reportFilters.Controls.Add(UiTheme.Button("Apply filters", async (_, _) => await RefreshReport(), true));
        var tools = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 53, WrapContents = false }; shell.Controls.Add(tools); tools.BringToFront(); tools.Controls.Add(search);
        orderStatus.Items.AddRange(["All order statuses", "Pending", "Processing", "Shipped", "Cancelled"]); orderStatus.SelectedIndex = 0; tools.Controls.Add(orderStatus);
        search.TextChanged += (_, _) => Filter(); orderStatus.SelectedIndexChanged += (_, _) => Filter(); tabs.SelectedIndexChanged += (_, _) => { orderStatus.Visible = tabs.SelectedIndex == 2; search.Visible = tabs.SelectedIndex != 3; }; orderStatus.Visible = false;
        shell.Controls.Clear();
        var welcome = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(238, 228, 250), Margin = new(0, 0, 0, 10) };
        welcome.Controls.Add(new Label { Text = "Good things are on the move.", Location = new(20, 16), AutoSize = true, Font = new("Segoe UI", 19, FontStyle.Bold), ForeColor = UiTheme.Ink }); welcome.Controls.Add(new Label { Text = "A happy warehouse starts with a clear view of every order.", Location = new(22, 57), AutoSize = true, ForeColor = UiTheme.Muted, Font = new("Segoe UI", 10) });
        var motion = new LabUi.MotionBanner { Dock = DockStyle.Right, Width = 340 }; welcome.Controls.Add(motion); motion.LoadArt(Path.Combine(AppContext.BaseDirectory, "assets", "northstar-welcome-30s.gif"), Path.Combine(AppContext.BaseDirectory, "assets", "northstar-3d-hero.png"), !SystemInformation.TerminalServerSession);
        var pause = UiTheme.Button("Pause / Play", (_, _) => motion.Toggle()); pause.Size = new(120, 27); pause.MinimumSize = new(120, 27); pause.AutoSize = false; pause.Font = new("Segoe UI", 8); pause.Location = new(460, 65); welcome.Controls.Add(pause);
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 5, ColumnStyles = { new(SizeType.Percent, 100) }, RowStyles = { new(SizeType.Absolute, 94), new(SizeType.Absolute, 115), new(SizeType.Absolute, 115), new(SizeType.Absolute, 53), new(SizeType.Percent, 100) } }; shell.Controls.Add(content); top.Dock = metrics.Dock = tools.Dock = tabs.Dock = DockStyle.Fill; content.Controls.Add(top, 0, 0); content.Controls.Add(welcome, 0, 1); content.Controls.Add(metrics, 0, 2); content.Controls.Add(tools, 0, 3); content.Controls.Add(tabs, 0, 4);
        var photoColumn = new DataGridViewImageColumn { Name = "Photo", HeaderText = "", ImageLayout = DataGridViewImageCellLayout.Zoom, FillWeight = 35, DefaultCellStyle = new DataGridViewCellStyle { NullValue = null } }; itemGrid.Columns.Add(photoColumn); itemGrid.RowTemplate.Height = 65;
        itemGrid.CellFormatting += (_, e) => { if (itemGrid.Columns[e.ColumnIndex].Name == "Photo" && itemGrid.Rows[e.RowIndex].DataBoundItem is ItemRow row) { var n = row.Name.ToLowerInvariant(); var f = n.Contains("coffee") ? "coffee" : n.Contains("tea") ? "tea" : n.Contains("cashew") ? "cashew" : n.Contains("rice") ? "rice" : n.Contains("mango") ? "mango" : n.Contains("coconut") ? "coconut" : n.Contains("oil") ? "oil" : n.Contains("noodle") ? "noodles" : n.Contains("sugar") ? "sugar" : n.Contains("salt") ? "salt" : null; if (f is not null) { if (!photoCache.ContainsKey(f)) { var path = Path.Combine(AppContext.BaseDirectory, "assets", "products", f + ".jpg"); if (File.Exists(path)) { using var original=Image.FromFile(path); photoCache[f]=new Bitmap(original,new Size(160,Math.Max(1,original.Height*160/original.Width))); } } if (photoCache.TryGetValue(f, out var image)) { e.Value = image; e.FormattingApplied = true; } } } };
        FormClosed += (_, _) => { foreach (var image in photoCache.Values) image.Dispose(); };
        itemGrid.DataBindingComplete += (_, _) => { if (itemGrid.Columns["Version"] is { } col) col.Visible = false; FormatGrid(itemGrid); };
        agentGrid.DataBindingComplete += (_, _) => FormatGrid(agentGrid); orderGrid.DataBindingComplete += (_, _) => FormatGrid(orderGrid);
        itemGrid.CellFormatting += (_, e) => { if (itemGrid.Columns[e.ColumnIndex].Name == "Stock" && e.Value is int n && n <= 10) e.CellStyle!.ForeColor = Color.FromArgb(163, 106, 30); };
        FormClosing += (_, e) => { if (busy) { e.Cancel = true; status.Text = "Please wait for the current database operation to finish."; } };
        Shown += async (_, _) => { await Reload(); if (this.args.Contains("--button-audit")) { await AuditButtons(); return; } if (this.args.Contains("--capture")) { var i = Array.IndexOf(this.args, "--capture"); var dir = Path.GetFullPath(this.args[i + 1]); Directory.CreateDirectory(dir); for (var t = 0; t < tabs.TabCount; t++) { tabs.SelectedIndex = t; await Task.Delay(150); using var b = new Bitmap(Width, Height); DrawToBitmap(b, new Rectangle(0, 0, Width, Height)); b.Save(Path.Combine(dir, new[] { "admin-inventory.png", "admin-agents.png", "admin-orders.png", "admin-analytics.png" }[t])); } Close(); } };
    }
    private TabPage Page(string name, DataGridView grid) { var p = new TabPage(name) { BackColor = Color.White, Padding = new(10) }; p.Controls.Add(grid); tabs.TabPages.Add(p); return p; }
    private static FlowLayoutPanel Bar(TabPage p) { var bar = new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 65, Padding = new(5, 12, 0, 0), AutoScroll = true, WrapContents = false }; var grid = p.Controls[0]; p.Controls.Remove(grid); var layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, ColumnStyles = { new(SizeType.Percent, 100) }, RowStyles = { new(SizeType.Percent, 100), new(SizeType.Absolute, 65) } }; p.Controls.Add(layout); bar.Dock = DockStyle.Fill; layout.Controls.Add(grid, 0, 0); layout.Controls.Add(bar, 0, 1); return bar; }
    private static void FormatGrid(DataGridView grid) { foreach (DataGridViewColumn col in grid.Columns) { if (col.Name == "Price" || col.Name == "Total") col.DefaultCellStyle.Format = "N0"; if (col.Name == "Date") col.DefaultCellStyle.Format = "dd MMM yyyy HH:mm"; if (col.Name == "PaymentStatus") col.HeaderText = "Payment"; if (col.Name == "Id") { col.FillWeight = 35; col.HeaderText = "ID"; } } }
    private bool Confirm(string text) => MessageBox.Show(this, text, "Confirm change", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
    private void Filter() { var term = search.Text.Trim(); itemGrid.DataSource = items.Where(x => (x.Name + " " + x.Size + " " + x.Id).Contains(term, StringComparison.OrdinalIgnoreCase)).ToList(); agentGrid.DataSource = agents.Where(x => (x.Name + " " + x.Phone + " " + x.Address).Contains(term, StringComparison.OrdinalIgnoreCase)).ToList(); orderGrid.DataSource = orders.Where(x => (x.Agent + " " + x.Id).Contains(term, StringComparison.OrdinalIgnoreCase) && (orderStatus.SelectedIndex <= 0 || x.Status == orderStatus.Text)).ToList(); }
    private async Task Reload()
    {
        if (busy) return; busy = true; UseWaitCursor = true; status.Text = "Refreshing SQL Server data…";
        try { var data = await Task.Run(() => (store.GetItems(), store.GetAgents(), store.GetOrders())); items = data.Item1; agents = data.Item2; orders = data.Item3; Filter(); metrics.Controls.Clear(); Metric("ACTIVE PRODUCTS", items.Count.ToString(), "Current inventory"); Metric("LOW STOCK", items.Count(x => x.Stock <= 10).ToString(), "Products at ≤ 10 units"); Metric("OPEN ORDERS", orders.Count(x => x.Status is "Pending" or "Processing").ToString(), "Awaiting dispatch"); Metric("AGENT PARTNERS", agents.Count.ToString(), "Active companies"); var selected = (reportAgent.SelectedItem as AgentRow)?.Id ?? 0; reportAgent.DataSource = new[] { new AgentRow(0, "All agents", "", "") }.Concat(agents).ToList(); reportAgent.SelectedItem = ((List<AgentRow>)reportAgent.DataSource).FirstOrDefault(x => x.Id == selected) ?? reportAgent.Items[0]; status.Text = $"Connected · Updated {DateTime.Now:HH:mm:ss} · {items.Count} products / {orders.Count} orders"; }
        catch (Exception ex) { Error(ex); }
        finally { busy = false; UseWaitCursor = false; }
        await RefreshReport();
    }
    private void Metric(string caption, string value, string note) { var p = new Panel { Width = 205, Height = 92, BackColor = Color.White, Margin = new(0, 0, 14, 0) }; p.Controls.Add(new Label { Text = caption, Location = new(15, 10), AutoSize = true, Font = new("Segoe UI", 8, FontStyle.Bold), ForeColor = UiTheme.Muted }); p.Controls.Add(new Label { Text = value, Location = new(14, 29), AutoSize = true, Font = new("Segoe UI", 25, FontStyle.Bold), ForeColor = UiTheme.Ink }); p.Controls.Add(new Label { Text = note, Location = new(15, 70), AutoSize = true, Font = new("Segoe UI", 8), ForeColor = UiTheme.Muted }); metrics.Controls.Add(p); }
    private async void Run(Action action) { if (busy) return; busy = true; UseWaitCursor = true; try { await Task.Run(action); status.Text = "Change saved successfully."; } catch (Exception ex) { Error(ex); } finally { busy = false; UseWaitCursor = false; } await Reload(); }
    private async Task RefreshReport() { if (from.Value.Date > to.Value.Date) { status.Text = "Choose a start date on or before the end date."; return; } try { var id = (reportAgent.SelectedItem as AgentRow)?.Id ?? 0; var f = from.Value; var t = to.Value; report.Text = await Task.Run(() => store.ReportFiltered(id == 0 ? null : id, f, t)); } catch (Exception ex) { Error(ex); } }
    private void Error(Exception ex) { status.Text = "Operation could not be completed."; MessageBox.Show(this, ex.Message, "Northstar", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    private async Task AuditButtons()
    {
        var checks = new List<string>();
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); checks.Add(name); }
        IEnumerable<Control> Descendants(Control parent) { foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
        Button Find(string text) => Descendants(this).OfType<Button>().First(b => b.Text == text);
        void OpenAndDismiss(string buttonText, string title)
        {
            var opened = false;
            using var timer = new System.Windows.Forms.Timer { Interval = 80 };
            timer.Tick += (_, _) => { foreach (var form in OwnedForms) if (form.Text == title) { opened = true; form.DialogResult = DialogResult.Cancel; form.Close(); timer.Stop(); break; } };
            timer.Start(); Find(buttonText).PerformClick(); timer.Stop();
            Check(opened, buttonText + " opens " + title);
        }
        var index = Array.IndexOf(args, "--button-audit");
        try
        {
            Check(items.Count > 0 && agents.Count > 0 && orders.Count > 0, "Dashboard loaded SQL data");
            foreach (var (name, tab) in new[] { ("▦  Items & inventory", 0), ("◉  Agent partners", 1), ("▤  Incoming orders", 2), ("↗  Sales dashboard", 3) })
            { Find(name).PerformClick(); Check(tabs.SelectedIndex == tab, name + " navigation"); }
            Find("Pause / Play").PerformClick(); Find("Pause / Play").PerformClick(); checks.Add("Pause and resume animation buttons");
            OpenAndDismiss("✦ AI guide", "Northstar · Local AI guide");
            var colors = Descendants(this).OfType<ComboBox>().First(c => c.Items.Count == 3 && c.Items[0]?.ToString() == "Forest");
            colors.SelectedIndex = 1; Check(UiTheme.Green == Color.FromArgb(166, 76, 54), "Sunset color selection"); colors.SelectedIndex = 0;
            Find("↻ Refresh").PerformClick(); for (var i = 0; i < 80 && busy; i++) await Task.Delay(50);
            Check(!busy && items.Count > 0, "Refresh button reloads SQL data");
            Find("▦  Items & inventory").PerformClick(); search.Text = items[0].Name;
            Check(itemGrid.Rows.Count > 0, "Product search filter"); search.Clear();
            OpenAndDismiss("+ Add product", "Add product");
            OpenAndDismiss("Edit product", "Edit product");
            OpenAndDismiss("Adjust stock", "Adjust stock");
            Find("◉  Agent partners").PerformClick();
            OpenAndDismiss("+ Register agent", "Register agent & portal account");
            OpenAndDismiss("Edit company", "Edit agent company");
            OpenAndDismiss("Unlock account", "Unlock agent account");
            Find("▤  Incoming orders").PerformClick();
            orderStatus.SelectedIndex = 1; Check(orderGrid.Rows.Cast<DataGridViewRow>().All(r => r.DataBoundItem is not OrderRow o || o.Status == "Pending"), "Order status filter"); orderStatus.SelectedIndex = 0;
            OpenAndDismiss("Order details", $"Order #{orders[0].Id} / Shipping manifest");
            Find("↗  Sales dashboard").PerformClick();
            from.Value = DateTime.Today.AddYears(-2); to.Value = DateTime.Today;
            Find("Apply filters").PerformClick(); for (var i = 0; i < 80 && string.IsNullOrWhiteSpace(report.Text); i++) await Task.Delay(50);
            Check(!string.IsNullOrWhiteSpace(report.Text), "Apply dashboard filters button");
            foreach (var label in new[] { "Deactivate", "Advance status →", "Cancel & restore stock", "Export manifest / PDF" })
                Check(Descendants(this).OfType<Button>().Any(b => b.Text == label), label + " control present; business path checked by SQL harness");
            Find("Sign out").PerformClick(); Check(IsDisposed, "Sign out button closes Admin workspace");
            var output = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllText(output, System.Text.Json.JsonSerializer.Serialize(new { status = "PASS", checks }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            var output = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllText(output, System.Text.Json.JsonSerializer.Serialize(new { status = "FAIL", checks, error = ex.Message }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        finally { Close(); }
    }
    private async Task ExportManifest(int id) { try { using var dialog = new SaveFileDialog { Filter = "HTML document (*.html)|*.html", FileName = $"ShippingManifest-{id:D5}.html" }; if (dialog.ShowDialog(this) != DialogResult.OK) return; var html = await Task.Run(() => store.ManifestHtml(id)); await File.WriteAllTextAsync(dialog.FileName, html); Process.Start(new ProcessStartInfo(dialog.FileName) { UseShellExecute = true }); status.Text = "Manifest exported. Choose Print / Save as PDF in your browser."; } catch (Exception ex) { Error(ex); } }
    private void ShowManifest(int id) { try { using var dialog = new Form { Text = $"Order #{id} / Shipping manifest", Size = new(800, 700), StartPosition = FormStartPosition.CenterParent }; dialog.Controls.Add(new RichTextBox { Text = store.Manifest(id), ReadOnly = true, Dock = DockStyle.Fill, Font = new("Consolas", 11), BackColor = Color.White, BorderStyle = BorderStyle.None }); var print = UiTheme.Button("Print preview", (_, _) => PrintManifest(store.Manifest(id)), true); print.Dock = DockStyle.Bottom; dialog.Controls.Add(print); dialog.ShowDialog(this); } catch (Exception ex) { Error(ex); } }
    private static void PrintManifest(string text) { using var doc = new System.Drawing.Printing.PrintDocument { DocumentName = "Northstar shipping manifest" }; int offset = 0; doc.BeginPrint += (_, _) => offset = 0; doc.PrintPage += (_, e) => { using var f = new Font("Consolas", 11); e.Graphics!.MeasureString(text[offset..], f, e.MarginBounds.Size, StringFormat.GenericTypographic, out var chars, out var lines); e.Graphics!.DrawString(text.Substring(offset, chars), f, Brushes.Black, e.MarginBounds, StringFormat.GenericTypographic); offset += chars; e.HasMorePages = offset < text.Length; }; using var preview = new PrintPreviewDialog { Document = doc, Width = 900, Height = 760 }; preview.ShowDialog(); }
}
