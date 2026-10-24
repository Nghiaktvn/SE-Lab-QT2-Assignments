using Lab.Intelligence;

namespace LabUi;

internal sealed class AssistantDialog : Form
{
    private readonly IReadOnlyList<KnowledgeEntry> knowledge;
    private readonly RichTextBox answer = new()
    {
        Dock = DockStyle.Fill,
        ReadOnly = true,
        BorderStyle = BorderStyle.None,
        BackColor = Color.FromArgb(253, 251, 247),
        Font = new Font("Segoe UI", 11),
        Text = "✦ Welcome to the MetroGo 3D Guide.\n\nAsk about fares, payment methods, ticket printing or error recovery.\n\nQuick topics: fare · card · wallet · qr · timeout · refund"
    };
    private readonly TextBox input = new()
    {
        Dock = DockStyle.Fill,
        MaxLength = 300,
        Font = new Font("Segoe UI", 11),
        PlaceholderText = "Ask a question about your trip…"
    };
    private readonly Button ask = new()
    {
        Text = "Ask ✦",
        Dock = DockStyle.Fill,
        BackColor = Color.FromArgb(36, 113, 93),
        ForeColor = Color.White,
        FlatStyle = FlatStyle.Flat,
        Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
        Cursor = Cursors.Hand
    };

    public AssistantDialog(string title, IReadOnlyList<KnowledgeEntry> entries)
    {
        knowledge = entries;
        Text = title;
        Size = new Size(620, 520);
        MinimumSize = new Size(540, 440);
        StartPosition = FormStartPosition.CenterParent;
        BackColor = Color.White;
        Font = new Font("Segoe UI", 10);
        Padding = new Padding(20);

        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 60,
            BackColor = Color.FromArgb(246, 240, 230),
            Padding = new Padding(12)
        };
        var headerTitle = new Label
        {
            Text = "✦ METROGO 3D ASSISTANT",
            Font = new Font("Segoe UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(36, 113, 93),
            Dock = DockStyle.Top,
            Height = 22
        };
        var headerSub = new Label
        {
            Text = "Smart local guide for passengers & station operators",
            Font = new Font("Segoe UI", 8.5f),
            ForeColor = Color.FromArgb(103, 122, 121),
            Dock = DockStyle.Bottom,
            Height = 18
        };
        header.Controls.Add(headerTitle);
        header.Controls.Add(headerSub);
        Controls.Add(header);

        var bodyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0, 10, 0, 10) };
        bodyPanel.Controls.Add(answer);
        Controls.Add(bodyPanel);
        bodyPanel.BringToFront();

        var bottomContainer = new Panel { Dock = DockStyle.Bottom, Height = 95 };

        var chipFlow = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 32,
            WrapContents = false,
            AutoScroll = false
        };
        string[] quickChips = ["Fare rates", "Payment QR", "Timeout help", "Printer recovery"];
        string[] chipQueries = ["route", "payment qr", "timeout reconcile", "print refund"];

        for (int i = 0; i < quickChips.Length; i++)
        {
            var q = chipQueries[i];
            var chip = new Button
            {
                Text = quickChips[i],
                AutoSize = true,
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 8.5f),
                BackColor = Color.FromArgb(245, 248, 244),
                ForeColor = Color.FromArgb(36, 113, 93),
                Cursor = Cursors.Hand,
                Margin = new Padding(0, 0, 8, 0)
            };
            chip.FlatAppearance.BorderColor = Color.FromArgb(210, 225, 215);
            chip.Click += async (_, _) => await AskQuery(q);
            chipFlow.Controls.Add(chip);
        }
        bottomContainer.Controls.Add(chipFlow);

        var bar = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            Height = 44,
            ColumnCount = 2,
            ColumnStyles = { new ColumnStyle(SizeType.Percent, 100), new ColumnStyle(SizeType.Absolute, 105) }
        };
        bar.Controls.Add(input, 0, 0);

        ask.FlatAppearance.BorderSize = 0;
        bar.Controls.Add(ask, 1, 0);

        bottomContainer.Controls.Add(bar);
        Controls.Add(bottomContainer);

        async Task AskQuery(string question)
        {
            question = question.Trim();
            if (question.Length == 0) return;
            ask.Enabled = false;
            try
            {
                var reply = await Task.Run(() => KnowledgeAssistant.Ask(question, knowledge));
                if (!IsDisposed) answer.Text = reply.Answer;
            }
            finally
            {
                if (!IsDisposed)
                {
                    ask.Enabled = true;
                    input.Focus();
                }
            }
        }

        ask.Click += async (_, _) => await AskQuery(input.Text);
        AcceptButton = ask;

        var note = new Label
        {
            Dock = DockStyle.Bottom,
            Height = 22,
            Text = "Local offline semantic guide · Safe classroom demonstration.",
            ForeColor = Color.DimGray,
            Font = new Font("Segoe UI", 8)
        };
        Controls.Add(note);
    }
}
