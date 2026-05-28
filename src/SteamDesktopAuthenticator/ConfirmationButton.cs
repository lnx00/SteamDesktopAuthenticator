using System.ComponentModel;
using System.Windows.Forms;
using SteamAuth;

namespace SteamDesktopAuthenticator;

public class ConfirmationButton : Button
{
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public Confirmation Confirmation { get; set; }
}