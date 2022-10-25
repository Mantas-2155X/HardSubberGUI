using System;
using System.Collections.Generic;
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

			Closed += delegate
			{
				if (CancelControl.IsEnabled)
					Cancel_OnClick(null, null);
			};
			
			Opened += delegate
			{
				ExtensionControl.Items = Tools.SupportedVideoFormats;
				ExtensionControl.SelectedIndex = 0;
			};
						
			var args = Environment.GetCommandLineArgs();
			if (args.Length == 2)
				InputControl.Text = args[1];
			
			Tools.ToggleControls(this, true);
			
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
		
		private void Exit_OnClick(object? sender, RoutedEventArgs? e)
		{
			Environment.Exit(0);
		}

		private void Cancel_OnClick(object? sender, RoutedEventArgs? e)
		{
			CancellationSource.Cancel();

			for (var index = 0; index < Tools.Processes.Count; index++)
			{
				var process = Tools.Processes[index];
				if (process != null && !process.HasExited)
				{
					Console.WriteLine("Killing worker " + index);
					process.Kill(Tools.IsWindows);
				}
			}
		}
		
		private async void Convert_OnClick(object? sender, RoutedEventArgs e)
		{
			var files = Tools.ProcessFiles(this);
			if (files == null)
				return;

			var workers = AvailableWorkers();

			CancelControl.IsEnabled = true;
			ConvertControl.IsEnabled = false;

			Tools.ToggleControls(this, false);
			
			CancellationSource.Dispose();
			CancellationSource = new CancellationTokenSource();

			var threadArray = Tools.DistributeInteger((int)ThreadsControl.Value!, workers).ToList();
			
			for (var i = 0; i < threadArray.Count; i++)
				if (threadArray[i] == 0)
					threadArray[i] = 1;

			await ProcessManager(workers, threadArray, files.ToArray()).ContinueWith(_ =>
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

		public int AvailableWorkers()
		{
			var workers = (bool)SimultaneousControl.IsChecked! ? Environment.ProcessorCount / 4 : 1;
			if (workers < 1)
				workers = 1;

			return workers;
		}
		
		public async Task ProcessManager(int workers, List<int> threadArray, string[] data)
		{
			var queue = new string[workers];
			for (var i = 0; i < queue.Length; i++) 
				queue[i] = data[i];

			await Task.Run(() => 
			{
				Console.WriteLine("Manager starting");

				var tasks = new Task[workers];
				for (var i = 0; i < workers; i++)
				{
					var idx = i;
					tasks[i] = Task.Run(() => ProcessWorker(idx, workers, queue, data, threadArray[idx]));
				}
	            
				Task.WaitAll(tasks, CancellationSource.Token);
			}).ContinueWith(t =>
			{
				Console.WriteLine("Manager finished");
			});
		}

		public Task ProcessWorker(int idx, int workers, string[] queue, string[] data, int threads)
		{
			var dataIndex = idx;
			while (queue[idx] != null && !CancellationSource.IsCancellationRequested)
			{
				Console.WriteLine("Worker " + idx + " processing " + data[dataIndex]);

				Tools.ActFile(data[dataIndex], OutputControl.Text, (bool)ApplySubsControl.IsChecked, (int)SubtitleIndexControl.Value, (int)AudioIndexControl.Value,
					(int)QualityControl.Value, (int)ResolutionOverrideWidthControl.Value, (int)ResolutionOverrideHeightControl.Value, (bool)HardwareAccelerationControl.IsChecked,
					(bool)ColorspaceControl.IsChecked, (bool)MetadataTitleControl.IsChecked, (bool)FastStartControl.IsChecked, (bool)AACControl.IsChecked, threads, ExtensionControl.SelectedIndex);
				
				dataIndex += workers;
				queue[idx] = data.Length > dataIndex ? data[dataIndex] : null;
			}

			return Task.CompletedTask;
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
					Tools.DownloadFFmpeg(this).ContinueWith(_ =>
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