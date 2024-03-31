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
				ExtensionControl.ItemsSource = Tools.SupportedVideoFormats;
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
		}
		
		private void InputControl_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
		{
			if (SimultaneousControl == null)
				return;

			SimultaneousControl.IsEnabled = Directory.Exists(InputControl.Text);
		}
		
		private void ExtensionControl_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
		{
			if (HardwareAccelerationControl == null || ExtensionControl.SelectedItem == null || PGSSubsControl == null)
				return;

			HardwareAccelerationControl.IsEnabled = !ExtensionControl.SelectedItem!.ToString()!.Contains(".mkv") && !(bool)PGSSubsControl.IsChecked! && Tools.CurrentGPU != Tools.GPU.None;
			
			if (!HardwareAccelerationControl.IsEnabled)
				HardwareAccelerationControl.IsChecked = false;
		}
		
		private void PGSSubsControl_OnPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
		{
			if (HardwareAccelerationControl == null || ExtensionControl.SelectedItem == null || PGSSubsControl == null || ResolutionOverrideWidthControl == null || ResolutionOverrideHeightControl == null)
				return;
			
			HardwareAccelerationControl.IsEnabled = !ExtensionControl.SelectedItem!.ToString()!.Contains(".mkv") && !(bool)PGSSubsControl.IsChecked! && Tools.CurrentGPU != Tools.GPU.None;
			
			if (!HardwareAccelerationControl.IsEnabled)
				HardwareAccelerationControl.IsChecked = false;

			var pgs = (bool)PGSSubsControl.IsChecked!;
			ApplyResizeControl.IsEnabled = !pgs;
			
			if (!ApplyResizeControl.IsEnabled)
				ApplyResizeControl.IsChecked = false;
		}
		
		public void BackgroundTasks()
		{
			Tools.FfmpegPath = Tools.GetffmpegPath();
			
			Task.Run(() =>
			{
				var res = Tools.IsOutdated();
				if (res != null)
				{
					Dispatcher.UIThread.Post(() =>
					{
						var updateFound = new UpdateFoundWindow { DataContext = new UpdateFoundViewModel() };
						updateFound.Closed += delegate
						{
							IsEnabled = true;
						};

						updateFound.ChangesControl.Text = res;
						
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