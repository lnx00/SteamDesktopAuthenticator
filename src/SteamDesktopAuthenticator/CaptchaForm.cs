using System;
using System.Windows.Forms;

namespace SteamDesktopAuthenticator;

public partial class CaptchaForm : Form
{
    public bool Canceled;
    public string CaptchaGID = "";
    public string CaptchaURL = "";

    public CaptchaForm(string GID)
    {
        CaptchaGID = GID;
        CaptchaURL = "https://steamcommunity.com/public/captcha.php?gid=" + GID;
        InitializeComponent();
        pictureBoxCaptcha.Load(CaptchaURL);
    }

    public string CaptchaCode => txtBox.Text;

    private void btnAccept_Click(object sender, EventArgs e)
    {
        Canceled = false;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        Canceled = true;
        Close();
    }
}