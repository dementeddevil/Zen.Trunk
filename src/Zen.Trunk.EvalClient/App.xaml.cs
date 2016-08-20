using System.Windows;
using Microsoft.Practices.ServiceLocation;
using Microsoft.Practices.Unity;
using Zen.Composite.Presentation.Bootstrap;
using Zen.Trunk.EvalClient.Models;
using Zen.Trunk.EvalClient.Properties;
using Zen.Trunk.Torrent.Client;
using Zen.Trunk.Torrent.Client.Encryption;
using System;

namespace Zen.Trunk.EvalClient
{
	public class AppBootstrapper : UnityBootstrapper
	{
		protected override void ConfigureContainer()
		{
			base.ConfigureContainer();

			// Register torrent engine in unity container
			EngineSettings engineSettings = App.GetEngineSettingsFromRegistry();
			ClientEngine engine = new ClientEngine(engineSettings);
			Container.RegisterInstance<ClientEngine>(engine);

			// Register torrent engine model
			Container.RegisterType<TorrentEngineModel>(
				new ContainerControlledLifetimeManager());
		}

		protected override UIElement CreateShell()
		{
			return null;
		}
	}

	/// <summary>
	/// Interaction logic for App.xaml
	/// </summary>
	public partial class App : Application
	{
		protected override async void OnStartup(StartupEventArgs e)
		{
			base.OnStartup(e);

			// Boot the application
			AppBootstrapper bootstrapper = new AppBootstrapper();
			await bootstrapper.Run();
			
			// Load torrent model
			TorrentEngineModel model = ServiceLocator.Current
				.GetInstance<TorrentEngineModel>();
			model.LoadState();
		}

		protected override void OnExit(ExitEventArgs e)
		{
			// Save state first..
			TorrentEngineModel model = ServiceLocator.Current
				.GetInstance<TorrentEngineModel>();
			model.SaveState().Wait(TimeSpan.FromMinutes(1));

			base.OnExit(e);
		}

		public static EngineSettings GetEngineSettingsFromRegistry()
		{
			EngineSettings engineSettings =
				new EngineSettings
				{
					AllowedEncryption = (EncryptionTypes)Settings.Default.AllowedEncryption,
					PreferEncryption = Settings.Default.PreferEncryption,
					GlobalMaxConnections = Settings.Default.GlobalMaxConnections,
					GlobalMaxDownloadSpeed = Settings.Default.GlobalMaxDownloadSpeed,
					GlobalMaxUploadSpeed = Settings.Default.GlobalMaxUploadSpeed,
					GlobalMaxHalfOpenConnections = Settings.Default.GlobalMaxHalfOpenConnections,
					HaveSupressionEnabled = Settings.Default.HaveSuppressionEnabled,
					ListenPort = Settings.Default.ListenPort,
					MaxOpenFiles = Settings.Default.MaxOpenFiles,
					MaxReadRate = Settings.Default.MaxReadRate,
					MaxWriteRate = Settings.Default.MaxWriteRate,
					SavePath = Settings.Default.SavePath,
				};
			return engineSettings;
		}
	}
}
