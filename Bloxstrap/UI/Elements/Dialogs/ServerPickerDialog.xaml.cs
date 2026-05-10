using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;

using Bloxstrap.Models.APIs.Roblox;
using Bloxstrap.UI.Elements.Base;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class ServerPickerDialog : WpfUiWindow
    {
        public string? SelectedServerId { get; private set; }

        public ServerPickerDialog(IReadOnlyList<GameServerEntry> servers)
        {
            InitializeComponent();
            ServersList.ItemsSource = servers;
            if (servers.Count > 0)
                ServersList.SelectedIndex = 0;
        }

        private void Join_Click(object sender, RoutedEventArgs e)
        {
            if (ServersList.SelectedItem is not GameServerEntry entry)
                return;

            SelectedServerId = entry.Id;
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ServersList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            Join_Click(sender, e);
        }
    }
}
