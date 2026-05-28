using System;
using System.Windows.Forms;

namespace SteamDesktopAuthenticator;

public partial class InputForm : Form
{
    public bool Canceled;
    private bool userClosed = true;

    public InputForm(string label, bool password = false)
    {
        InitializeComponent();
        labelText.Text = label;

        if (password)
        {
            txtBox.PasswordChar = '*';
        }
    }

    private void btnAccept_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(txtBox.Text))
        {
            Canceled = true;
            userClosed = false;
            Close();
        }
        else
        {
            Canceled = false;
            userClosed = false;
            Close();
        }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        Canceled = true;
        userClosed = false;
        Close();
    }

    private void InputForm_FormClosing(object sender, FormClosingEventArgs e)
    {
        if (userClosed)
        {
            // Set Canceled = true when the user hits the X button.
            Canceled = true;
        }
    }
}