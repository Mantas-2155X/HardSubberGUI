using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using HardSubberGUI.ViewModels;

namespace HardSubberGUI.Views
{
	public partial class MainWindow : Window
	{
		public static CancellationTokenSource CancellationSource = new ();

		public MainWindow()
		{
			InitializeComponent();
			
			Tools.ToggleControls(this, true);
			Tools.FfmpegPath = Tools.GetffmpegPath();

			if (Tools.FfmpegPath == "")
			{
				Tools.DownloadFFmpeg(this).ContinueWith(t =>
				{
					if (Tools.FfmpegPath == "")
					{
						Exit_OnClick(null, null);
					}
					else
					{
						Dispatcher.UIThread.Post(() =>
						{
							ConvertControl.Content = MainWindowViewModel.ConvertVideos;
							ConvertControl.IsEnabled = true;
						});
					}
				});
			}

			var args = Environment.GetCommandLineArgs();
			if (args.Length == 2)
				InputControl.Text = args[1];
			
			Closed += delegate
			{
				if (CancelControl.IsEnabled)
					Cancel_OnClick(null, null);
			};
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
		
		private void Exit_OnClick(object? sender, RoutedEventArgs? e)
		{
			Environment.Exit(0);
		}

		private void Cancel_OnClick(object? sender, RoutedEventArgs? e)
		{
			CancellationSource.Cancel();

			for (var index = Tools.Processes.Count - 1; index >= 0; index--)
			{
				var process = Tools.Processes[index];
				if (process != null && !process.HasExited)
					process.Kill(Tools.IsWindows);
			}
		}
		
		private async void Convert_OnClick(object? sender, RoutedEventArgs e)
		{
			var files = Tools.ProcessFiles(this);
			if (files == null)
				return;
			
			var workers = (bool)SimultaneousControl.IsChecked! ? Environment.ProcessorCount / 4 : 1;
			
			if (workers < 1)
				workers = 1;

			CancelControl.IsEnabled = true;
			ConvertControl.IsEnabled = false;

			Tools.ToggleControls(this, false);
			
			CancellationSource.Dispose();
			CancellationSource = new CancellationTokenSource();

			var threadArray = Tools.DistributeInteger((int)ThreadsControl.Value!, workers).ToList();

			await Task.Run(() =>
			{
				try
				{
					var idx = -1;
					Parallel.For(0, files.Count, new ParallelOptions { MaxDegreeOfParallelism = workers, CancellationToken = CancellationSource.Token }, i =>
					{
						Interlocked.Increment(ref idx);
						Tools.ActFile(files[i], OutputControl.Text, (bool)ApplySubsControl.IsChecked, (int)SubtitleIndexControl.Value, (int)AudioIndexControl.Value,
							(int)QualityControl.Value, (int)ResolutionOverrideWidthControl.Value, (int)ResolutionOverrideHeightControl.Value, (bool)HardwareAccelerationControl.IsChecked,
							(bool)ColorspaceControl.IsChecked, (bool)MetadataTitleControl.IsChecked, (bool)FastStartControl.IsChecked, (bool)AACControl.IsChecked, threadArray[idx]);
					});
				}
				catch (TaskCanceledException ex)
				{
					Console.WriteLine(ex);
				}
			}).ContinueWith(_ =>
			{
				Dispatcher.UIThread.Post(() =>
				{
					CancelControl.IsEnabled = false;
					ConvertControl.IsEnabled = true;
					
					Tools.ToggleControls(this, true);

					if ((bool)ExitAfterwardsControl.IsChecked! && !CancellationSource.IsCancellationRequested)
						Exit_OnClick(null, null);
				});
			});
		}
	}
}