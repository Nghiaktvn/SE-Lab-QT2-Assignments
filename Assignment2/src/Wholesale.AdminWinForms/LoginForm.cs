namespace Wholesale.AdminWinForms;

public sealed class LoginForm : Form
{
    public LoginForm()
    {
        Text = "Northstar | Admin sign in"; Size = new(940, 610); MinimumSize = MaximumSize = Size; StartPosition = FormStartPosition.CenterScreen; BackColor = UiTheme.Background; Font = new("Segoe UI", 11);
        var story = new Panel { Dock = DockStyle.Left, Width = 380, BackColor = Color.FromArgb(250, 242, 232), Padding = new(26) }; Controls.Add(story);
        story.Controls.Add(new Label { Text = "NORTHSTAR", Location = new(28, 32), AutoSize = true, Font = new("Segoe UI", 17, FontStyle.Bold), ForeColor = UiTheme.Ink });
        story.Controls.Add(new Label { Text = "BACK-OFFICE WORKSPACE", Location = new(30, 90), AutoSize = true, Font = new("Segoe UI", 9, FontStyle.Bold), ForeColor = UiTheme.Green });
        story.Controls.Add(new Label { Text = "Good business.\nGreat supply.", Location = new(27, 123), Size = new(315, 112), Font = new("Georgia", 30), ForeColor = UiTheme.Ink });
        story.Controls.Add(new Label { Text = "Your warehouse, connected.\nSupport partners and keep every order moving.", Location = new(30, 238), Size = new(315, 56), ForeColor = UiTheme.Muted, Font = new("Segoe UI", 10) });
        var animation = new LabUi.MotionBanner { Location = new(22, 316), Size = new(336, 205), BackColor = Color.White }; story.Controls.Add(animation); animation.LoadArt(Path.Combine(AppContext.BaseDirectory, "assets", "northstar-welcome-30s.gif"), Path.Combine(AppContext.BaseDirectory, "assets", "northstar-3d-hero.png"), !SystemInformation.TerminalServerSession);
        story.Controls.Add(new Label { Text = "Thoughtful essentials. One connected workspace.", Location = new(30, 544), Size = new(320, 22), Font = new("Segoe UI", 9), ForeColor = UiTheme.Muted });
        var panel = new Panel { Dock = DockStyle.Fill, BackColor = Color.FromArgb(255, 253, 250) }; Controls.Add(panel); panel.BringToFront();
        panel.Controls.Add(new Label { Text = "ADMINISTRATOR SIGN IN", Location = new(46, 45), AutoSize = true, ForeColor = UiTheme.Green, Font = new("Segoe UI", 9, FontStyle.Bold) });
        panel.Controls.Add(new Label { Text = "Welcome back.", Location = new(42, 82), AutoSize = true, ForeColor = UiTheme.Ink, Font = new("Segoe UI", 27, FontStyle.Bold) });
        panel.Controls.Add(new Label { Text = "Sign in to your wholesale workspace.", Location = new(46, 137), AutoSize = true, ForeColor = UiTheme.Muted });
        panel.Controls.Add(new Label { Text = "Username", Location = new(46, 198), AutoSize = true, ForeColor = UiTheme.Ink });
        var user = new TextBox { Location = new(46, 225), Width = 440, Height = 34, Text = "admin", MaxLength = 80, BorderStyle = BorderStyle.FixedSingle, Font = new("Segoe UI", 11) }; panel.Controls.Add(user);
        panel.Controls.Add(new Label { Text = "Password", Location = new(46, 278), AutoSize = true, ForeColor = UiTheme.Ink });
        var pass = new TextBox { Location = new(46, 305), Width = 335, Height = 34, UseSystemPasswordChar = true, MaxLength = 256, BorderStyle = BorderStyle.FixedSingle, Font = new("Segoe UI", 11) }; panel.Controls.Add(pass);
        var reveal = new CheckBox { Text = "Show", Location = new(397, 310), AutoSize = true, ForeColor = UiTheme.Muted, Font = new("Segoe UI", 9) }; reveal.CheckedChanged += (_, _) => pass.UseSystemPasswordChar = !reveal.Checked; panel.Controls.Add(reveal);
        var message = new Label { Location = new(46, 359), Size = new(440, 42), ForeColor = Color.Firebrick, Font = new("Segoe UI", 9) }; panel.Controls.Add(message);
        var go = UiTheme.Button("Sign in   →", async (_, _) => { if (string.IsNullOrWhiteSpace(user.Text) || string.IsNullOrWhiteSpace(pass.Text)) { message.Text = "Enter your username and password."; return; } var name = user.Text.Trim(); var password = pass.Text; panel.Enabled = false; UseWaitCursor = true; try { if (await Task.Run(() => new SqlAdminStore().AuthenticateAdmin(name, password))) { DialogResult = DialogResult.OK; Close(); } else { message.Text = "Sign in failed. Check your credentials and account status."; pass.Clear(); } } catch { message.Text = "Database unavailable. Check SQL Server and App.config."; } finally { panel.Enabled = true; UseWaitCursor = false; } }, true); go.SetBounds(46, 411, 440, 48); go.AutoSize = false; panel.Controls.Add(go); AcceptButton = go;
        panel.Controls.Add(new Label { Text = "Admin access only · Accounts lock after 5 failed attempts.", Location = new(46, 482), Size = new(440, 36), Font = new("Segoe UI", 9), ForeColor = UiTheme.Muted });
    }
}
