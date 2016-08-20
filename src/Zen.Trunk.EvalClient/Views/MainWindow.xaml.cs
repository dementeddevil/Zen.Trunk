namespace Zen.Trunk.EvalClient
{
	using System.ComponentModel;
	using System.Windows;
	using System.Windows.Controls;
	using Microsoft.Win32;
	using Zen.Composite.Presentation.UserInterface;
	using Zen.Trunk.EvalClient.ViewModels;
	using Zen.Trunk.Torrent.Client;

	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	[ViewModel(typeof(MainWindowViewModel))]
	public partial class MainWindow : ChildView
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private void OnAddTorrentFile(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openTorrent = new OpenFileDialog();
			openTorrent.Title = "Add Torrent File";
			openTorrent.Filter = "Torrent Files|*.torrent|All Files|*.*";
			bool? result = openTorrent.ShowDialog();
			if (!result.HasValue || !result.Value)
			{
				return;
			}

			// TODO: Get torrent settings
			// TODO: Determine whether to start immediately
			TorrentSettings settings = new TorrentSettings();
			settings.StartImmediately = true;

			MainWindowViewModel viewModel =
				(MainWindowViewModel)ViewModelService.GetViewModel(this);
			viewModel.OpenTorrentFile(openTorrent.FileName, settings);
		}

		private void OnAddTorrentUrl(object sender, RoutedEventArgs e)
		{
			// TODO: Prompt for torrent URI

			// TODO: Prompt for torrent settings

			// TODO: Create torrent and register
		}

		private void OnExit(object sender, RoutedEventArgs e)
		{
			// TODO: Prompt "Are you sure?" if allowed.
			Close();
		}
	}
}
