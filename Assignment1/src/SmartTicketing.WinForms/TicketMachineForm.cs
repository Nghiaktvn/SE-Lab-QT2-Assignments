using QRCoder;
using SmartTicketing.Domain;
namespace SmartTicketing.WinForms;

public sealed class TicketMachineForm : Form
{
    private static Color Ink = Color.FromArgb(25, 53, 56), Green = Color.FromArgb(36, 113, 93), Muted = Color.FromArgb(103, 122, 121);
    private readonly ComboBox destination = new(), payment = new(), outcome = new();
    private readonly Label status = new(), fare = new(), step = new(), receiptText = new(), receiptTitle = new();
    private readonly PictureBox paymentQr = new(), ticketCode = new();
    private readonly Button start = new(), pay = new(), reset = new(), print = new(), reconcile = new(), refund = new();
    private readonly Panel receipt = new();
    private List<Destination> routes = [];
    private TicketStore? store;
    private TicketTransaction? current;
    private bool busy, ready, saveFailed;
    private enum StatusTone { Normal, Success, Warning, Error }
    private StatusTone statusTone;
    private bool NeedsResolution => saveFailed || current?.Status is TransactionStatus.PaymentUnknown or TransactionStatus.CompensationRequired;
    private string? currentReceipt;
    private readonly string[] args;

    public TicketMachineForm(string[]? args = null)
    {
        this.args = args ?? [];
        Text = "MetroGo | Smart ticketing"; Size = new(1180, 1040); MinimumSize = new(1180, 930); StartPosition = FormStartPosition.CenterScreen;
        Font = new("Segoe UI", 11); BackColor = Color.FromArgb(255, 249, 239); AutoScaleMode = AutoScaleMode.Dpi; AutoScroll = true;
        var header = new Panel { Dock = DockStyle.Top, Height = 88, BackColor = Ink };
        header.Controls.Add(Label("✳  METROGO", 26, 24, 25, Color.White, FontStyle.Bold));
        header.Controls.Add(Label("SMART TICKETING  /  SINGLE JOURNEY", 440, 35, 9, Color.FromArgb(188, 211, 199)));
        Controls.Add(header);
        var palette=new ComboBox{Location=new(970,30),Width=155,DropDownStyle=ComboBoxStyle.DropDownList};palette.Items.AddRange(["Forest","Sunset","Lilac"]);palette.SelectedIndex=0;header.Controls.Add(palette);

        var body = new TableLayoutPanel { Location = new(30, 260), Size = new(1100, 680), ColumnCount = 2, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, ColumnStyles = { new(SizeType.Percent, 60), new(SizeType.Percent, 40) } };
        Controls.Add(body);
        var welcome = new Panel { Location = new(34, 112), Size = new(1095, 125), BackColor = Color.FromArgb(255, 237, 216), Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
        welcome.Controls.Add(Label("A little ticket. A big adventure.", 24, 23, 24, Ink, FontStyle.Bold)); welcome.Controls.Add(Label("Pick a stop, choose your way to pay, and let the good journey begin.", 26, 60, 10, Muted));
        var motion = new LabUi.MotionBanner { Location = new(717, 0), Size = new(375, 125), Anchor = AnchorStyles.Top | AnchorStyles.Right }; welcome.Controls.Add(motion); motion.LoadArt(Path.Combine(AppContext.BaseDirectory, "assets", "metrogo-welcome-30s.gif"), Path.Combine(AppContext.BaseDirectory, "assets", "metrogo-3d-hero.png"), !SystemInformation.TerminalServerSession);
        var pause = new Button { Text = "Pause animation", Location = new(582, 81), Size = new(120, 27), FlatStyle = FlatStyle.Flat, BackColor = Color.White, Font = new("Segoe UI", 8) }; pause.Click += (_, _) => { motion.Toggle(); pause.Text = motion.Playing ? "Pause animation" : "Play animation"; }; welcome.Controls.Add(pause); Controls.Add(welcome);
        var aiHelp=new Button{Text="✦ Travel helper",Location=new(420,81),Size=new(150,27),FlatStyle=FlatStyle.Flat,BackColor=Color.White,Font=new("Segoe UI",8)};aiHelp.Click+=(_,_)=>{using var guide=new LabUi.AssistantDialog("MetroGo · Local AI guide",[
          new("payment","payment card wallet bank qr thanh toan the vi ngan hang","Card, bank QR and wallet are classroom simulators. Choose a method, Continue, then press Confirm payment. No real money is transferred."),
          new("timeout","timeout unknown reconcile pending khong ro doi soat","An unknown payment cannot issue a ticket or start a new charge. Use Reconcile payment to resolve the same reference."),
          new("print","print ticket printer refund in ve may in hoan tien","Open Preview / Print after confirmation. A simulated printer fault allows recovery of the same ticket or a demo refund."),
          new("route","route destination fare price ticket gia ve diem den tuyen",destination.SelectedItem is Destination d?$"Selected destination: {d.Name}. Single-journey fare: {d.Fare:N0} VND. The fare is fixed when payment starts.":"Choose an active destination to see its fare."),
          new("decline","declined rejected retry tu choi thu lai","A declined payment issues no usable ticket. Start a new attempt or choose a different method.")
        ]);guide.ShowDialog(this);};welcome.Controls.Add(aiHelp);
        var left = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Margin = new(0, 0, 22, 0), Padding = new(30) };
        body.Controls.Add(left, 0, 0);
        var sectionLabel=Label("YOUR JOURNEY STARTS HERE", 28, 24, 10, Green, FontStyle.Bold);sectionLabel.Tag="accent";left.Controls.Add(sectionLabel);
        left.Controls.Add(Label("Where are you heading?", 28, 52, 25, Ink, FontStyle.Bold));
        step.SetBounds(28, 104, 550, 26); step.Text = "01  ROUTE     →     02  PAYMENT     →     03  TICKET"; step.Font = new("Segoe UI", 10, FontStyle.Bold); step.ForeColor = Green; left.Controls.Add(step);
        left.Controls.Add(Label("DESTINATION", 28, 155, 9, Muted, FontStyle.Bold));
        StyleSelect(destination, 28, 178, 340); destination.DisplayMember = "Name"; destination.SelectedIndexChanged += (_, _) => { if (destination.SelectedItem is Destination d) fare.Text = $"{d.Fare:N0}\nVND"; }; left.Controls.Add(destination);
        fare.SetBounds(385, 173, 180, 70); fare.Font = new("Segoe UI", 17, FontStyle.Bold); fare.ForeColor = Green;fare.Tag="accent"; left.Controls.Add(fare);
        left.Controls.Add(Label("PAYMENT METHOD", 28, 235, 9, Muted, FontStyle.Bold)); StyleSelect(payment, 28, 257, 530);
        payment.Items.AddRange(["Credit / Debit Card", "Bank transfer QR", "Digital Wallet (QR)"]); payment.SelectedIndex = 0; left.Controls.Add(payment);
        left.Controls.Add(Label("PAYMENT SIMULATOR", 28, 310, 9, Muted, FontStyle.Bold)); StyleSelect(outcome, 28, 332, 530);
        outcome.Items.AddRange(["Approved — successful journey", "Declined — no charge, no ticket", "Timeout — reconcile before continuing", "Printer fault — recover or refund"]); outcome.SelectedIndex = 0; left.Controls.Add(outcome);
        SetupButton(start, "Continue to payment →", 28, 389, 530, true); start.Enabled = false; start.Click += async (_, _) => { if (ready) Begin(); else await LoadRoutes(); }; left.Controls.Add(start);
        paymentQr.SetBounds(28, 450, 115, 115); paymentQr.SizeMode = PictureBoxSizeMode.Zoom; left.Controls.Add(paymentQr);
        status.SetBounds(160, 456, 390, 102); status.Font = new("Segoe UI", 11); status.ForeColor = Muted; status.Text = "Connecting to the route catalog…"; left.Controls.Add(status);
        SetupButton(pay, "Confirm payment & issue ticket", 28, 583, 300, true); pay.Enabled = false; pay.AccessibleName = "Confirm simulated payment and issue ticket"; pay.AccessibleDescription = "Completes the classroom payment simulation and saves one ticket."; pay.Click += async (_, _) => await Complete(); left.Controls.Add(pay);
        SetupButton(reconcile, "Reconcile payment", 342, 583, 216, false); reconcile.Visible = false; reconcile.Click += async (_, _) => await Reconcile(); left.Controls.Add(reconcile);
        receipt.Dock = DockStyle.Fill; receipt.BackColor = Color.FromArgb(235, 240, 229); receipt.Padding = new(24); body.Controls.Add(receipt, 1, 0);
        var receiptLabel=Label("YOUR TRAVEL PASS", 25, 25, 10, Green, FontStyle.Bold);receiptLabel.Tag="accent";receipt.Controls.Add(receiptLabel);
        receiptTitle.SetBounds(25, 56, 350, 78); receiptTitle.Text = "A little closer\nto your destination."; receiptTitle.Font = new("Georgia", 20); receiptTitle.ForeColor = Ink; receipt.Controls.Add(receiptTitle);
        receiptText.SetBounds(25, 155, 350, 190); receiptText.Text = "Your ticket will appear here after\nconfirmed payment.\n\nChoose a route to get started."; receiptText.Font = new("Segoe UI", 11); receiptText.ForeColor = Muted; receipt.Controls.Add(receiptText);
        ticketCode.SetBounds(100, 350, 170, 170); ticketCode.SizeMode = PictureBoxSizeMode.Zoom; receipt.Controls.Add(ticketCode);
        SetupButton(print, "Preview / Print ticket", 25, 538, 350, true); print.Enabled = false; print.Click += (_, _) => Print(); receipt.Controls.Add(print);
        SetupButton(reset, "New journey / Cancel", 25, 594, 230, false); reset.Enabled = false; reset.Click += (_, _) => Reset(); receipt.Controls.Add(reset);
        SetupButton(refund, "Demo refund", 263, 594, 112, false); refund.Visible = false; refund.Click += async (_, _) => await Refund(); receipt.Controls.Add(refund);
        Controls.Add(Label("CLASSROOM SIMULATOR  ·  No real payment is processed.  ·  Need help? Contact the station operator.", 34, 955, 10, Muted));
        palette.SelectedIndexChanged+=(_,_)=>
        {
            Green=palette.SelectedIndex switch{1=>Color.FromArgb(166,76,54),2=>Color.FromArgb(107,79,153),_=>Color.FromArgb(36,113,93)};
            var tint=palette.SelectedIndex switch{1=>Color.FromArgb(250,233,225),2=>Color.FromArgb(241,234,250),_=>Color.FromArgb(255,237,216)};
            var receiptTint=palette.SelectedIndex switch{1=>Color.FromArgb(246,233,227),2=>Color.FromArgb(238,232,248),_=>Color.FromArgb(235,240,229)};
            welcome.BackColor=tint;receipt.BackColor=receiptTint;
            void Apply(Control root){foreach(Control c in root.Controls){if(c is Button b){var primary=b.Tag as string=="primary";b.BackColor=!b.Enabled?Color.FromArgb(223,231,218):primary?Green:Color.White;b.ForeColor=!b.Enabled?Muted:primary?Color.White:Ink;b.FlatAppearance.MouseOverBackColor=primary?ControlPaint.Light(Green,0.12f):Color.FromArgb(241,246,239);b.FlatAppearance.MouseDownBackColor=primary?ControlPaint.Dark(Green,0.08f):Color.FromArgb(231,239,229);}if(c is Label l&&l.Tag as string=="accent")l.ForeColor=Green;Apply(c);}}
            Apply(this);
            ApplyStatusTone();
        };
        var selectedPalette=palette.SelectedIndex;palette.SelectedIndex=-1;palette.SelectedIndex=selectedPalette;
        Shown += async (_, _) =>
        {
            await LoadRoutes();
            if (this.args.Contains("--button-audit")) { await AuditButtons(pause, palette, aiHelp); return; }
            if (ready && this.args.Contains("--demo-scenario")) { var n = Array.IndexOf(this.args, "--demo-scenario"); payment.SelectedIndex = 2; outcome.SelectedIndex = this.args[n + 1] switch { "declined" => 1, "timeout" => 2, "printer" => 3, _ => 0 }; Begin(); await Complete(); if (this.args.Contains("--resolve-demo")) await Reconcile(); if (this.args.Contains("--refund-demo")) await Refund(); }
            if (ready && this.args.Contains("--demo-wallet")) { payment.SelectedIndex = 2; Begin(); await Complete(); }
            if (ready && this.args.Contains("--demo-wallet-start")) { payment.SelectedIndex = 2; Begin(); }
            if (this.args.Contains("--capture")) { await Task.Delay(200); var i = Array.IndexOf(this.args, "--capture"); using var bmp = new Bitmap(Width, Height); DrawToBitmap(bmp, new Rectangle(0, 0, Width, Height)); bmp.Save(Path.GetFullPath(this.args[i + 1])); Close(); }
        };
        FormClosing += (_, e) => { if (this.args.Contains("--capture")) return; if (busy) { e.Cancel = true; return; } if (NeedsResolution) { e.Cancel = MessageBox.Show("This payment needs reconciliation. Closing will not create another charge. Close and ask the operator to check its reference?", "Unresolved payment", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes; } };
    }
    private static Label Label(string text, int x, int y, float size, Color color, FontStyle style = FontStyle.Regular) => new() { Text = text, Location = new(x, y), AutoSize = true, Font = new("Segoe UI", size, style), ForeColor = color };
    private void SetStatus(string message, StatusTone tone = StatusTone.Normal) { status.Text = message; statusTone = tone; ApplyStatusTone(); }
    private void ApplyStatusTone() => status.ForeColor = statusTone switch { StatusTone.Success => Green, StatusTone.Warning => Color.FromArgb(145, 91, 24), StatusTone.Error => Color.FromArgb(170, 63, 58), _ => Muted };
    private static void StyleSelect(ComboBox box, int x, int y, int width) { box.SetBounds(x, y, width, 40); box.DropDownStyle = ComboBoxStyle.DropDownList; box.Font = new("Segoe UI", 12); box.DrawMode = DrawMode.OwnerDrawFixed; box.ItemHeight = 27; box.DrawItem += (_, e) => { e.DrawBackground(); if (e.Index >= 0) { var text = box.Items[e.Index] is Destination d ? d.Name : box.Items[e.Index]?.ToString() ?? ""; TextRenderer.DrawText(e.Graphics, text, box.Font, e.Bounds, Ink, TextFormatFlags.VerticalCenter | TextFormatFlags.Left); } e.DrawFocusRectangle(); }; box.IntegralHeight = false; box.DropDownHeight = 300; }
    private static void SetupButton(Button b, string text, int x, int y, int w, bool primary) { b.Tag=primary?"primary":"secondary";b.Text = text; b.UseMnemonic = false; b.SetBounds(x, y, w, 44); b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderColor = Color.FromArgb(190, 207, 193); b.FlatAppearance.BorderSize = primary ? 0 : 1;b.FlatAppearance.MouseOverBackColor=primary?ControlPaint.Light(Green,0.12f):Color.FromArgb(241,246,239);b.FlatAppearance.MouseDownBackColor=primary?ControlPaint.Dark(Green,0.08f):Color.FromArgb(231,239,229); b.BackColor = primary ? Green : Color.White; b.ForeColor = primary ? Color.White : Ink; b.Font = new("Segoe UI", 10, FontStyle.Bold); b.Cursor = Cursors.Hand; b.EnabledChanged += (_, _) => { b.BackColor = b.Enabled ? (primary ? Green : Color.White) : Color.FromArgb(223, 231, 218); b.ForeColor = b.Enabled ? (primary ? Color.White : Ink) : Muted; }; }
    private async Task LoadRoutes()
    {
        if (busy) return;
        busy = true; start.Enabled = false; SetStatus("Connecting to the route catalog…");
        try
        {
            store = args.Contains("--button-audit") ? new TicketStore("offline-button-audit", false) : new TicketStore(); routes = await store.Routes(); destination.DataSource = routes; ready = routes.Count > 0;
            start.Text = ready ? "Continue to payment →" : "Reload destinations";
            SetStatus(ready ? $"Ready · {routes.Count} destinations\n{(store.Persist ? "Connected to SQL Server" : "Offline demonstration")}" : "No active destinations. Ask the station operator, then reload.", ready ? StatusTone.Success : StatusTone.Warning);
        }
        catch (Exception) { ready = false; start.Text = "Retry connection →"; SetStatus("Route catalog unavailable.\nAsk the operator to check the connection, then retry.", StatusTone.Error); }
        finally { busy = false; start.Enabled = true; }
    }
    private void Begin()
    {
        if (busy || !ready || destination.SelectedItem is not Destination d) return;
        if (NeedsResolution) return;
        ClearTicket(); saveFailed = false; current = new(d, (PaymentMethod)payment.SelectedIndex); SetSelection(false); start.Enabled = false; reset.Enabled = true; pay.Enabled = true; pay.Text = "Confirm payment"; reconcile.Visible = false;
        step.Text = "01  ROUTE     →     02  PAYMENT •     →     03  TICKET";
        if (current.PaymentMethod != PaymentMethod.Card) { SetImage(paymentQr, Qr($"DEMO-METROGO|{current.IdempotencyKey}|{current.FareSnapshot:0.00}|{current.PaymentMethod}")); SetStatus($"Scan the demo QR, then confirm.\n{current.PaymentMethod} · {current.FareSnapshot:N0} VND"); }
        else { SetImage(paymentQr, null); SetStatus($"Card terminal ready (simulated).\nTotal: {current.FareSnapshot:N0} VND"); }
    }
    private async Task Complete()
    {
        if (current?.Status == TransactionStatus.Pending && DateTimeOffset.UtcNow - current.CreatedAt > TimeSpan.FromMinutes(3)) { current.Cancel(); SetImage(paymentQr, null); SetStatus("Payment QR expired. Start a new journey.", StatusTone.Warning); SetSelection(true); start.Enabled = true; pay.Enabled = false; return; }
        if (busy || current is null) return; busy = true; pay.Enabled = false; reset.Enabled = false; SetStatus(saveFailed ? "Retrying the same save reference…" : "Confirming simulated payment…");
        try
        {
            if (!saveFailed)
            {
                await Task.Delay(450);
                if (outcome.SelectedIndex == 1) { current.MarkDeclined(); SetImage(paymentQr, null); SetStatus("Payment declined. No ticket issued.\nStart a new attempt or choose another method.", StatusTone.Warning); SetSelection(true); start.Enabled = true; return; }
                if (outcome.SelectedIndex == 2) { current.MarkUnknown(); SetImage(paymentQr, null); SetStatus("Payment result unknown. No ticket issued.\nReconcile this reference before retrying.", StatusTone.Warning); reconcile.Visible = true; reset.Enabled = false; return; }
                current.MarkPaid("SIM-" + current.IdempotencyKey);
            }
            var expiry = new DateTimeOffset(DateTime.Today.AddDays(1), TimeZoneInfo.Local.GetUtcOffset(DateTime.Today.AddDays(1)));
            var ticket = current.IssueTicket(expiry);
            await store!.Save(current); saveFailed = false;
            if (outcome.SelectedIndex == 3) { await store.Update(current, "Failed"); current.PrinterFailed(); SetImage(paymentQr, null); SetStatus("Printer fault simulated. Payment saved.\nRecover this same ticket or request a demo refund.", StatusTone.Warning); reconcile.Text = "Recover same ticket"; reconcile.Visible = true; refund.Visible = true; return; }
            ShowTicket(ticket);
        }
        catch (Exception ex) { saveFailed = true; pay.Text = "Retry saving ticket"; pay.Enabled = true; SetImage(paymentQr, null); SetStatus("Save was not confirmed. No ticket displayed.\nRetry uses the same payment reference.", StatusTone.Error); MessageBox.Show(ex.Message, "Ticket save", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        finally { busy = false; reset.Enabled = !NeedsResolution; }
    }
    private async Task Reconcile()
    {
        if (busy || current is null) return;
        if (current.Status == TransactionStatus.CompensationRequired)
        {
            busy = true; reconcile.Enabled = false; try { await store!.Update(current, "Pending"); current.Reprint(); ShowTicket(current.Ticket!); reset.Enabled = true; } catch (Exception ex) { MessageBox.Show(ex.Message, "Recovery"); } finally { busy = false; reconcile.Enabled = true; }
            return;
        }
        if (current.Status != TransactionStatus.PaymentUnknown) return;
        current.MarkPaid("SIM-" + current.IdempotencyKey); saveFailed = true; reconcile.Visible = false; await Complete();
    }
    private async Task Refund()
    {
        if (busy || current?.Status != TransactionStatus.CompensationRequired) return;
        busy = true; refund.Enabled = false; try { await store!.Update(current, "Failed", true); current.Refund(); ClearTicket(); receiptTitle.Text = "Demo refund recorded."; SetStatus("Refund simulation completed.\nNo external money movement.", StatusTone.Success); reconcile.Visible = false; refund.Visible = false; reset.Enabled = true; SetSelection(true); start.Enabled = true; } catch (Exception ex) { MessageBox.Show(ex.Message, "Refund"); } finally { busy = false; refund.Enabled = true; }
    }
    private void ShowTicket(Ticket t)
    {
        receiptTitle.Text = "You're ready to go.";
        currentReceipt = $"METROGO / SINGLE JOURNEY\n\nDestination: {current!.Destination.Name}\nFare: {current.FareSnapshot:N0} VND\nPayment status: PAID (classroom demo)\nPayment method: {current.PaymentMethod}\nIssued: {current.CreatedAt.LocalDateTime:dd MMM yyyy HH:mm}\nValid until: {t.ValidUntil.LocalDateTime:dd MMM HH:mm}\nTicket: {t.Barcode}\nReference: {current.IdempotencyKey}";
        receiptText.Text = $"DESTINATION\n{current.Destination.Name}\n\n{current.FareSnapshot:N0} VND · {current.PaymentMethod}\nPAID · CLASSROOM DEMO\nIssued: {current.CreatedAt.LocalDateTime:dd MMM yyyy HH:mm}\nValid until: {t.ValidUntil.LocalDateTime:dd MMM HH:mm}\n\n{t.Barcode}";
        receiptText.Font = new("Segoe UI", 10); SetImage(ticketCode, Qr(t.Barcode)); SetImage(paymentQr, null); print.Enabled = true; pay.Enabled = false; pay.Text = "Payment confirmed"; reconcile.Visible = false; refund.Visible = false; reset.Text = "New journey";
        SetStatus(store!.Persist ? "Payment confirmed · Ticket saved to SQL Server.\nPreview or print your ticket." : "Payment confirmed · Offline demonstration only.", StatusTone.Success); step.Text = "01  ROUTE     →     02  PAYMENT PAID     →     03  TICKET ✓"; SetSelection(true); start.Text = "Start another journey →"; start.Enabled = true; reset.Enabled = true;
    }
    private void SetSelection(bool enabled) { destination.Enabled = payment.Enabled = outcome.Enabled = enabled; }
    private void Reset()
    {
        if (busy) return;
        if (NeedsResolution) { SetStatus("Resolve the current payment before starting again.", StatusTone.Warning); return; }
        if (current?.Status == TransactionStatus.Pending) current.Cancel(); current = null; saveFailed = false; ClearTicket(); SetImage(paymentQr, null); SetSelection(true); pay.Enabled = false; pay.Text = "Confirm payment & issue ticket"; reconcile.Visible = false; refund.Visible = false; reset.Text = "New journey / Cancel"; reset.Enabled = false; start.Text = "Continue to payment →"; start.Enabled = ready; SetStatus("Ready. Select your next destination."); step.Text = "01  ROUTE     →     02  PAYMENT     →     03  TICKET";
    }
    private void ClearTicket() { currentReceipt = null; SetImage(ticketCode, null); print.Enabled = false; receiptTitle.Text = "A little closer\nto your destination."; receiptText.Text = "Your ticket will appear here after\nconfirmed payment.\n\nChoose a route to get started."; }
    private static Bitmap Qr(string text) { using var g = new QRCodeGenerator(); using var d = g.CreateQrCode(text, QRCodeGenerator.ECCLevel.Q); using var q = new QRCode(d); return q.GetGraphic(6); }
    private static void SetImage(PictureBox b, Image? image) { var old = b.Image; b.Image = image; b.Visible = image is not null; old?.Dispose(); }
    private async Task AuditButtons(Button pause, ComboBox palette, Button aiHelp)
    {
        var checks = new List<string>();
        void Check(bool condition, string name) { if (!condition) throw new InvalidOperationException(name); checks.Add(name); }
        async Task WaitFor(Func<bool> predicate) { for (var i = 0; i < 80; i++) { if (predicate()) return; await Task.Delay(50); } throw new TimeoutException("Button action did not complete."); }
        var index = Array.IndexOf(args, "--button-audit");
        try
        {
            Check(ready && start.Enabled && routes.Count > 0, "Offline route selection ready");
            pause.PerformClick(); Check(pause.Text == "Play animation", "Pause animation button");
            pause.PerformClick(); Check(pause.Text == "Pause animation", "Resume animation button");
            palette.SelectedIndex = 1; Check(Green == Color.FromArgb(166, 76, 54), "Sunset color selection"); palette.SelectedIndex = 0;
            var guideOpened = false;
            using (var timer = new System.Windows.Forms.Timer { Interval = 80 })
            {
                timer.Tick += (_, _) => { foreach (var form in OwnedForms) if (form.Text == "MetroGo · Local AI guide") { guideOpened = true; form.Close(); timer.Stop(); break; } };
                timer.Start(); aiHelp.PerformClick(); timer.Stop();
            }
            Check(guideOpened, "Travel helper button opens guide");
            start.PerformClick(); Check(pay.Enabled && reset.Enabled && !destination.Enabled, "Continue to payment button");
            reset.PerformClick(); Check(!pay.Enabled && start.Enabled && destination.Enabled, "Cancel journey button");
            payment.SelectedIndex = 1; start.PerformClick(); Check(paymentQr.Visible, "Bank transfer QR selection"); reset.PerformClick();
            payment.SelectedIndex = 2; start.PerformClick(); Check(paymentQr.Visible, "Digital wallet QR selection"); reset.PerformClick();
            payment.SelectedIndex = 0; outcome.SelectedIndex = 0; start.PerformClick(); pay.PerformClick();
            await WaitFor(() => !busy && current?.Status == TransactionStatus.Completed);
            Check(print.Enabled && currentReceipt is not null && !paymentQr.Visible, "Approved card payment issues paid ticket");
            reset.PerformClick();
            outcome.SelectedIndex = 1; start.PerformClick(); pay.PerformClick();
            await WaitFor(() => !busy && current?.Status == TransactionStatus.Declined);
            Check(!print.Enabled && !paymentQr.Visible && start.Enabled, "Declined payment button state");
            outcome.SelectedIndex = 2; start.PerformClick(); pay.PerformClick();
            await WaitFor(() => !busy && current?.Status == TransactionStatus.PaymentUnknown);
            Check(reconcile.Visible && !reset.Enabled && !paymentQr.Visible, "Timeout requires reconciliation");
            reconcile.PerformClick(); await WaitFor(() => !busy && print.Enabled);
            Check(current?.Status == TransactionStatus.Completed && currentReceipt is not null, "Reconcile same payment and issue ticket button");
            Check(print.Enabled && ticketCode.Visible, "Print preview button available for paid ticket");
            reset.PerformClick(); Check(!print.Enabled && current is null, "New journey clears prior ticket");
            outcome.SelectedIndex = 3; start.PerformClick(); pay.PerformClick();
            await WaitFor(() => !busy && current?.Status == TransactionStatus.CompensationRequired);
            Check(reconcile.Visible && refund.Visible && !print.Enabled, "Printer fault exposes recovery and refund buttons");
            refund.PerformClick(); await WaitFor(() => !busy && current?.Status == TransactionStatus.Refunded);
            Check(!refund.Visible && start.Enabled && status.Text.Contains("Refund"), "Demo refund button");
            reset.PerformClick(); outcome.SelectedIndex = 3; start.PerformClick(); pay.PerformClick();
            await WaitFor(() => !busy && current?.Status == TransactionStatus.CompensationRequired);
            reconcile.PerformClick(); await WaitFor(() => !busy && current?.Status == TransactionStatus.Completed);
            Check(print.Enabled && !reconcile.Visible && !refund.Visible, "Recover same ticket button");
            var output = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllText(output, System.Text.Json.JsonSerializer.Serialize(new { status = "PASS", checks, persist = store!.Persist }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            var output = Path.GetFullPath(args[index + 1]); Directory.CreateDirectory(Path.GetDirectoryName(output)!);
            File.WriteAllText(output, System.Text.Json.JsonSerializer.Serialize(new { status = "FAIL", checks, error = ex.Message }, new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        }
        finally { Close(); }
    }
    private void Print()
    {
        if (currentReceipt is null || ticketCode.Image is null) return;
        try
        {
            using var doc = new System.Drawing.Printing.PrintDocument { DocumentName = "MetroGo ticket" };
            doc.PrintPage += (_, e) => { using var f = new Font("Segoe UI", 12); e.Graphics!.DrawString(currentReceipt, f, Brushes.Black, new RectangleF(e.MarginBounds.Left, e.MarginBounds.Top, 500, 300)); e.Graphics.DrawImage(ticketCode.Image, e.MarginBounds.Left, e.MarginBounds.Top + 310, 180, 180); };
            using var preview = new PrintPreviewDialog { Document = doc, Width = 900, Height = 760 }; preview.ShowDialog(this);
        }
        catch (Exception ex) { MessageBox.Show(ex.Message, "Printer unavailable", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }
    protected override void Dispose(bool disposing) { if (disposing) { paymentQr.Image?.Dispose(); ticketCode.Image?.Dispose(); } base.Dispose(disposing); }
}
