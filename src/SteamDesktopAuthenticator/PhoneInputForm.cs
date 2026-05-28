using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using SteamAuth;

namespace SteamDesktopAuthenticator;

public partial class PhoneInputForm : Form
{
    private SteamGuardAccount Account;
    public bool Canceled;
    public string CountryCode;
    public string PhoneNumber;

    public PhoneInputForm(SteamGuardAccount account)
    {
        Account = account;
        InitializeComponent();
    }

    private void btnSubmit_Click(object sender, EventArgs e)
    {
        PhoneNumber = txtPhoneNumber.Text;
        CountryCode = txtCountryCode.Text;

        if (PhoneNumber[0] != '+')
        {
            MessageBox.Show("Phone number must start with + and country code.", "Phone Number", MessageBoxButtons.OK,
                MessageBoxIcon.Error);
            return;
        }

        Close();
    }

    private void txtPhoneNumber_KeyPress(object sender, KeyPressEventArgs e)
    {
        // Allow pasting
        if (char.IsControl(e.KeyChar))
        {
            return;
        }

        // Only allow numbers, spaces, and +
        var regex = new Regex(@"[^0-9\s\+]");
        if (regex.IsMatch(e.KeyChar.ToString()))
        {
            e.Handled = true;
        }
    }

    private void txtCountryCode_KeyPress(object sender, KeyPressEventArgs e)
    {
        // Allow pasting
        if (char.IsControl(e.KeyChar))
        {
            return;
        }

        // Only allow letters
        var regex = new Regex(@"[^a-zA-Z]");
        if (regex.IsMatch(e.KeyChar.ToString()))
        {
            e.Handled = true;
        }
    }

    private void txtCountryCode_Leave(object sender, EventArgs e)
    {
        // Always uppercase
        txtCountryCode.Text = txtCountryCode.Text.ToUpper();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        Canceled = true;
        Close();
    }
}