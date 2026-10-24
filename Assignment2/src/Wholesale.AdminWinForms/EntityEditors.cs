namespace Wholesale.AdminWinForms;

internal sealed class ItemEditor : Form
{
    private readonly TextBox name = new() { MaxLength = 120 }, size = new() { MaxLength = 30 }; private readonly NumericUpDown price = new() { DecimalPlaces = 2, Maximum = 999999999, Minimum = .01m, ThousandsSeparator = true }, stock = new() { Maximum = 1000000000, ThousandsSeparator = true }; private readonly int id; private readonly byte[]? version;
    public ItemRow Result => new(id, name.Text.Trim(), size.Text.Trim(), price.Value, (int)stock.Value, version);
    public ItemEditor(ItemRow? item = null)
    {
        id = item?.Id ?? 0; version = item?.Version; Text = item is null ? "Add product" : "Edit product"; Size = new(560, 385); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        var g = NewGrid(); Controls.Add(g); AddField(g, "Product name", name, 0); AddField(g, "Pack size", size, 1); AddField(g, "Unit price (VND)", price, 2); AddField(g, "Stock quantity", stock, 3);
        name.Text = item?.Name ?? ""; size.Text = item?.Size ?? ""; price.Value = item?.Price ?? 50000; stock.Value = item?.Stock ?? 0;
        var save = UiTheme.Button("Save product", (_, _) => { if (string.IsNullOrWhiteSpace(name.Text) || string.IsNullOrWhiteSpace(size.Text)) { MessageBox.Show("Product name and pack size are required."); return; } DialogResult = DialogResult.OK; }, true); save.Dock = DockStyle.Bottom; Controls.Add(save); AcceptButton = save; UiTheme.Dialog(this);
    }
    internal static TableLayoutPanel NewGrid() => new() { Dock = DockStyle.Fill, ColumnCount = 2, Padding = new(18), ColumnStyles = { new(SizeType.Absolute, 160), new(SizeType.Percent, 100) } };
    internal static void AddField(TableLayoutPanel g, string label, Control input, int row) { g.RowCount = Math.Max(g.RowCount, row + 1); g.RowStyles.Add(new(SizeType.Absolute, 48)); g.Controls.Add(new Label { Text = label, Anchor = AnchorStyles.Left, AutoSize = true }, 0, row); input.Dock = DockStyle.Fill; g.Controls.Add(input, 1, row); }
}
internal sealed class AgentEditor : Form
{
    private readonly TextBox name = new() { MaxLength = 120 }, address = new() { MaxLength = 250 }, phone = new() { MaxLength = 25 }, username = new() { MaxLength = 80 }, email = new() { MaxLength = 254 }, password = new() { UseSystemPasswordChar = true, MaxLength = 256 }; private readonly int id;
    public AgentRow Result => new(id, name.Text.Trim(), address.Text.Trim(), phone.Text.Trim()); public string UserName => username.Text.Trim(); public string Email => email.Text.Trim(); public string Password => password.Text;
    public AgentEditor(AgentRow? agent = null)
    {
        id = agent?.Id ?? 0; Text = agent is null ? "Register agent & portal account" : "Edit agent company"; Size = new(620, agent is null ? 520 : 335); StartPosition = FormStartPosition.CenterParent; FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        var g = ItemEditor.NewGrid(); Controls.Add(g); ItemEditor.AddField(g, "Company name", name, 0); ItemEditor.AddField(g, "Delivery address", address, 1); ItemEditor.AddField(g, "Phone", phone, 2);
        name.Text = agent?.Name ?? ""; address.Text = agent?.Address ?? ""; phone.Text = agent?.Phone ?? "";
        if (agent is null) { ItemEditor.AddField(g, "Portal username", username, 3); ItemEditor.AddField(g, "Email", email, 4); ItemEditor.AddField(g, "Password (8+ chars)", password, 5); }
        var save = UiTheme.Button(agent is null ? "Create company & account" : "Save company", (_, _) => { if (string.IsNullOrWhiteSpace(name.Text) || string.IsNullOrWhiteSpace(address.Text) || string.IsNullOrWhiteSpace(phone.Text)) { MessageBox.Show("Company, delivery address, and phone are required."); return; } if (id == 0 && (string.IsNullOrWhiteSpace(UserName) || !System.Net.Mail.MailAddress.TryCreate(Email, out _) || Password.Length < 8)) { MessageBox.Show("Enter a username, valid email, and password containing at least 8 characters."); return; } DialogResult = DialogResult.OK; }, true); save.Dock = DockStyle.Bottom; Controls.Add(save); AcceptButton = save; UiTheme.Dialog(this);
    }
}
internal sealed class StockEditor : Form
{
    private readonly NumericUpDown delta = new() { Minimum = -1000000000, Maximum = 1000000000, Value = 10, ThousandsSeparator = true }; public int Delta => (int)delta.Value;
    public StockEditor(ItemRow item) { Text = "Adjust stock"; Size = new(560, 260); StartPosition = FormStartPosition.CenterParent; var g = ItemEditor.NewGrid(); Controls.Add(g); ItemEditor.AddField(g, item.Name, new Label { Text = $"Current stock: {item.Stock:N0}", AutoSize = true }, 0); ItemEditor.AddField(g, "Change (+ / −)", delta, 1); var save = UiTheme.Button("Apply stock adjustment", (_, _) => { if (Delta == 0) { MessageBox.Show("Enter a non-zero adjustment."); return; } DialogResult = DialogResult.OK; }, true); save.Dock = DockStyle.Bottom; Controls.Add(save); AcceptButton = save; UiTheme.Dialog(this); }
}
internal sealed class TextEntry : Form
{
    private readonly TextBox input = new() { MaxLength = 80 }; public string Value => input.Text.Trim();
    public TextEntry(string title, string label) { Text = title; Size = new(530, 210); StartPosition = FormStartPosition.CenterParent; var g = ItemEditor.NewGrid(); Controls.Add(g); ItemEditor.AddField(g, label, input, 0); var save = UiTheme.Button("Confirm", (_, _) => { if (Value.Length > 0) DialogResult = DialogResult.OK; }, true); save.Dock = DockStyle.Bottom; Controls.Add(save); AcceptButton = save; UiTheme.Dialog(this); }
}
