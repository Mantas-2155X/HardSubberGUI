using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using HardSubberGUI.ViewModels;

namespace HardSubberGUI.Views
{
	public partial class MainWindow : Window
	{
		public static MainWindow Instance;
		
		public MainWindow()
		{
			Instance = this;
			
			InitializeComponent();

			Opened += delegate
			{
				ExtensionControl.Items = Tools.SupportedVideoFormats;
				ExtensionControl.SelectedIndex = 0;
			};
						
			var args = Environment.GetCommandLineArgs();
			if (args.Length == 2)
				InputControl.Text = args[1];

			HardwareAccelerationControl.IsEnabled = Tools.IsHardwareAccelerationSupported();
			
			BackgroundTasks();
		}
		
		private async void InputFile_OnClick(object? sender, RoutedEventArgs e)
		{
			InputControl.Text = await Tools.PickFile(this);
		}
		
		private async void InputDirectory_OnClick(object? sender, RoutedEventArgs e)
		{
			InputControl.Text = await Tools.PickDirectory(this);
		}
		
		private async void Output_OnClick(object? sender, RoutedEventArgs e)
		{
			OutputControl.Text = await Tools.PickDirectory(this);
		}
		
		public void Exit_OnClick(object? sender, RoutedEventArgs? e)
		{
			Environment.Exit(0);
		}

		private void Convert_OnClick(object? sender, RoutedEventArgs e)
		{
			var converting = new ConvertingWindow { DataContext = new ConvertingViewModel() };
			converting.ShowDialog(this);
		}
		
		public void BackgroundTasks()
		{
			Tools.FfmpegPath = Tools.GetffmpegPath();
			
			Task.Run(() =>
			{
				if (Tools.IsOutdated())
				{
					Dispatcher.UIThread.Post(() =>
					{
						var updateFound = new UpdateFoundWindow { DataContext = new UpdateFoundViewModel() };
						updateFound.ShowDialog(this);
					});
				}

				if (Tools.FfmpegPath == "")
				{
					Tools.DownloadFFmpeg(ConvertControl).ContinueWith(_ =>
					{
						Dispatcher.UIThread.Post(() =>
						{
							ConvertControl.Content = MainWindowViewModel.ConvertVideos;
							ConvertControl.IsEnabled = true;
						});
					});
				}
			});
		}
	}
}