using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SteamDesktopAuthenticator;

public partial class ListInputForm : Form
{
    private readonly List<string> Items;

    public int SelectedIndex;

    public ListInputForm(List<string> options)
    {
        Items = options;
        InitializeComponent();
    }

    private void ListInputForm_Load(object sender, EventArgs e)
    {
        foreach (var item in Items)
        {
            lbItems.Items.Add(item);
        }
    }

    private void btnAccept_Click(object sender, EventArgs e)
    {
        if (lbItems.SelectedIndex != -1)
        {
            SelectedIndex = lbItems.SelectedIndex;
            Close();
        }
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        Close();
    }
}