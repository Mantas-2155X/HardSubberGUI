using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia;
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

			HardwareAccelerationControl.IsEnabled = Tools.CurrentGPU != Tools.GPU.None;
			
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
			converting.Closed += delegate
			{
				IsEnabled = true;
			};
			
			IsEnabled = false;
			converting.ShowDialog(this);
		}
		
		private void ApplyResizeControl_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
		{
			if (ResolutionOverrideWidthControl == null || ResolutionOverrideHeightControl == null)
				return;

			ResolutionOverrideWidthControl.IsEnabled = (bool)ApplyResizeControl.IsChecked!;
			ResolutionOverrideHeightControl.IsEnabled = (bool)ApplyResizeControl.IsChecked!;
		}
		
		private void ApplySubsControl_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
		{
			if (SubtitleIndexControl == null || AudioIndexControl == null || QualityControl == null)
				return;
			
			SubtitleIndexControl.IsEnabled = (bool)ApplySubsControl.IsChecked!;
			AudioIndexControl.IsEnabled = (bool)ApplySubsControl.IsChecked!;
			QualityControl.IsEnabled = (bool)ApplySubsControl.IsChecked!;
		}
		
		private void InputControl_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
		{
			if (SimultaneousControl == null)
				return;

			SimultaneousControl.IsEnabled = Directory.Exists(InputControl.Text);
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
						updateFound.Closed += delegate
						{
							IsEnabled = true;
						};
						
						IsEnabled = false;
						updateFound.ShowDialog(this);
					});
				}

				if (Tools.FfmpegPath == "")
				{
					Tools.DownloadFFmpeg(ConvertControl).ContinueWith(_ =>
					{
						Console.WriteLine("Extracted");
						
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