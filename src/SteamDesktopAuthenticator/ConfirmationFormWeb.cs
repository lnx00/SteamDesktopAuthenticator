using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Threading.Tasks;
using System.Windows.Forms;
using SteamAuth;

namespace SteamDesktopAuthenticator;

public partial class ConfirmationFormWeb : Form
{
    private readonly SteamGuardAccount steamAccount;

    public ConfirmationFormWeb(SteamGuardAccount steamAccount)
    {
        InitializeComponent();
        this.steamAccount = steamAccount;
        Text = string.Format("Trade Confirmations - {0}", steamAccount.AccountName);
    }

    private async Task LoadData()
    {
        splitContainer1.Panel2.Controls.Clear();

        // Check for a valid refresh token first
        if (steamAccount.Session.IsRefreshTokenExpired())
        {
            MessageBox.Show("Your session has expired. Use the login again button under the selected account menu.",
                "Trade Confirmations", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Close();
            return;
        }

        // Check for a valid access token, refresh it if needed
        if (steamAccount.Session.IsAccessTokenExpired())
        {
            try
            {
                await steamAccount.Session.RefreshAccessToken();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Steam Login Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
                return;
            }
        }

        try
        {
            var confirmations = await steamAccount.FetchConfirmationsAsync();

            if (confirmations == null || confirmations.Length == 0)
            {
                var errorLabel = new Label
                {
                    Text = "Nothing to confirm/cancel", AutoSize = true, ForeColor = Color.Black,
                    Location = new Point(150, 20)
                };
                splitContainer1.Panel2.Controls.Add(errorLabel);
            }

            foreach (var confirmation in confirmations)
            {
                var panel = new Panel { Dock = DockStyle.Top, Height = 120 };
                panel.Paint += (s, e) =>
                {
                    using (var brush = new LinearGradientBrush(panel.ClientRectangle, Color.Black, Color.DarkCyan, 90F))
                    {
                        e.Graphics.FillRectangle(brush, panel.ClientRectangle);
                    }
                };

                if (!string.IsNullOrEmpty(confirmation.Icon))
                {
                    var pictureBox = new PictureBox
                        { Width = 60, Height = 60, Location = new Point(20, 20), SizeMode = PictureBoxSizeMode.Zoom };
                    try
                    {
                        pictureBox.Load(confirmation.Icon);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Failed to load avatar: " + ex.Message);
                    }

                    panel.Controls.Add(pictureBox);
                }

                var nameLabel = new Label
                {
                    Text = $"{confirmation.Headline}\n{confirmation.Creator.ToString()}",
                    AutoSize = true,
                    ForeColor = Color.Snow,
                    Location = new Point(90, 20),
                    BackColor = Color.Transparent
                };
                panel.Controls.Add(nameLabel);

                var acceptButton = new ConfirmationButton
                {
                    Text = confirmation.Accept,
                    Location = new Point(90, 50),
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderSize = 0 },
                    BackColor = Color.Black,
                    ForeColor = Color.Snow,
                    Confirmation = confirmation
                };
                acceptButton.Click += btnAccept_Click;
                panel.Controls.Add(acceptButton);

                var cancelButton = new ConfirmationButton
                {
                    Text = confirmation.Cancel,
                    Location = new Point(180, 50),
                    FlatStyle = FlatStyle.Flat,
                    FlatAppearance = { BorderSize = 0 },
                    BackColor = Color.Black,
                    ForeColor = Color.Snow,
                    Confirmation = confirmation
                };
                cancelButton.Click += btnCancel_Click;
                panel.Controls.Add(cancelButton);

                var summaryLabel = new Label
                {
                    Text = string.Join("\n", confirmation.Summary),
                    AutoSize = true,
                    ForeColor = Color.Snow,
                    Location = new Point(90, 80),
                    BackColor = Color.Transparent
                };
                panel.Controls.Add(summaryLabel);

                splitContainer1.Panel2.Controls.Add(panel);
            }
        }
        catch (Exception ex)
        {
            var errorLabel = new Label
            {
                Text = "Something went wrong:\n" + ex.Message, AutoSize = true, ForeColor = Color.Red,
                Location = new Point(20, 20)
            };
            splitContainer1.Panel2.Controls.Add(errorLabel);
        }
    }

    private async void btnAccept_Click(object sender, EventArgs e)
    {
        var button = (ConfirmationButton)sender;
        var confirmation = button.Confirmation;
        var result = await steamAccount.AcceptConfirmation(confirmation);

        await LoadData();
    }

    private async void btnCancel_Click(object sender, EventArgs e)
    {
        var button = (ConfirmationButton)sender;
        var confirmation = button.Confirmation;
        var result = await steamAccount.DenyConfirmation(confirmation);

        await LoadData();
    }


    private async void btnRefresh_Click(object sender, EventArgs e)
    {
        btnRefresh.Enabled = false;
        btnRefresh.Text = "Refreshing...";

        await LoadData();

        btnRefresh.Enabled = true;
        btnRefresh.Text = "Refresh";
    }

    private async void ConfirmationFormWeb_Shown(object sender, EventArgs e)
    {
        btnRefresh.Enabled = false;
        btnRefresh.Text = "Refreshing...";

        await LoadData();

        btnRefresh.Enabled = true;
        btnRefresh.Text = "Refresh";
    }
}