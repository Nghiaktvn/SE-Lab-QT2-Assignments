namespace Wholesale.AdminWinForms;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        if (args.Contains("--login-button-audit"))
        {
            using var form = new LoginForm();
            IEnumerable<Control> Descendants(Control parent) { foreach (Control child in parent.Controls) { yield return child; foreach (var nested in Descendants(child)) yield return nested; } }
            var checks = new List<string>();
            var output = Path.GetFullPath(args[Array.IndexOf(args, "--login-button-audit") + 1]);
            using var timeout = new System.Windows.Forms.Timer { Interval = 10000 };
            timeout.Tick += (_, _) => { timeout.Stop(); form.Close(); };
            form.Shown += (_, _) =>
            {
                var controls = Descendants(form).ToArray();
                var password = controls.OfType<TextBox>().Single(x => x.UseSystemPasswordChar);
                var reveal = controls.OfType<CheckBox>().Single(x => x.Text == "Show");
                password.Text = Environment.GetEnvironmentVariable("WHOLESALE_CAPTURE_PASSWORD") ?? "";
                reveal.Checked = true; if (!password.UseSystemPasswordChar) checks.Add("Show password checkbox");
                reveal.Checked = false; if (password.UseSystemPasswordChar) checks.Add("Hide password checkbox");
                timeout.Start(); controls.OfType<Button>().Single(x => x.Text == "Sign in   →").PerformClick();
            };
            form.FormClosed += (_, _) =>
            {
                timeout.Stop();
                if (form.DialogResult == DialogResult.OK) checks.Add("Admin sign-in button authenticated SQL account");
                Directory.CreateDirectory(Path.GetDirectoryName(output)!);
                File.WriteAllText(output, System.Text.Json.JsonSerializer.Serialize(new { status = form.DialogResult == DialogResult.OK && checks.Count == 3 ? "PASS" : "FAIL", checks }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
            };
            Application.Run(form);
            return;
        }
        if (args.Contains("--capture-login"))
        {
            using var form = new LoginForm();
            form.Shown += (_, _) =>
            {
                using var bitmap = new Bitmap(form.Width, form.Height);
                form.DrawToBitmap(bitmap, new Rectangle(0, 0, form.Width, form.Height));
                bitmap.Save(Path.GetFullPath(args[Array.IndexOf(args, "--capture-login") + 1]));
                form.Close();
            };
            Application.Run(form);
            return;
        }
        if (args.Contains("--capture") || args.Contains("--button-audit"))
        {
            if (!new SqlAdminStore().AuthenticateAdmin(Environment.GetEnvironmentVariable("WHOLESALE_CAPTURE_USER") ?? "", Environment.GetEnvironmentVariable("WHOLESALE_CAPTURE_PASSWORD") ?? "")) return;
            Application.Run(new DashboardForm(args));
            return;
        }
        while (true)
        {
            using var login = new LoginForm();
            if (login.ShowDialog() != DialogResult.OK) return;
            using var dashboard = new DashboardForm();
            Application.Run(dashboard);
        }
    }
}
