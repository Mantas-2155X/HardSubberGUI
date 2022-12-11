using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace HardSubberGUI.Views
{
	public partial class ConvertingWindow : Window
	{
		public static CancellationTokenSource CancellationSource = new ();
		
		public ConvertingWindow()
		{
			InitializeComponent();

			Closed += delegate
			{
				Cancel_OnClick(null, null);
			};
			
			BeginConversion().ContinueWith(t =>
			{
				Dispatcher.UIThread.Post(Close);
			});
		}

		private void Cancel_OnClick(object? sender, RoutedEventArgs? e)
		{
			CancellationSource.Cancel();

			for (var index = Tools.Processes.Count - 1; index >= 0; index--)
			{
				try
				{
					if (Tools.Processes[index] == null || Tools.Processes[index].HasExited)
					{
						Console.WriteLine("Skipping killing worker " + index);
						continue;
					}
					
					Console.WriteLine("Killing worker " + index);
					Tools.Processes[index].Kill(Tools.IsWindows);
				}
				catch (Exception ex)
				{
					Console.WriteLine(ex);
				}
			}
			
			Close();
		}

		public async Task BeginConversion()
		{
			Tools.Processes.Clear();
			
			var inputInfo = new FileInfo(MainWindow.Instance.InputControl.Text!);
			
			if (string.IsNullOrEmpty(MainWindow.Instance.OutputControl.Text))
				MainWindow.Instance.OutputControl.Text = Path.Combine(inputInfo.Attributes.HasFlag(FileAttributes.Directory) ? MainWindow.Instance.InputControl.Text! : inputInfo.FullName[..^inputInfo.Name.Length], "output");

			if (!Directory.Exists(MainWindow.Instance.OutputControl.Text))
				Directory.CreateDirectory(MainWindow.Instance.OutputControl.Text);

			var files = Tools.ProcessFiles(MainWindow.Instance.InputControl.Text!);
			if (files == null)
				return;
			
			CancellationSource.Dispose();
			CancellationSource = new CancellationTokenSource();

			var threadArray = Tools.DistributeInteger((int)MainWindow.Instance.ThreadsControl.Value!, (int)MainWindow.Instance.SimultaneousControl.Value!).ToList();
			
			for (var i = 0; i < threadArray.Count; i++)
				if (threadArray[i] == 0)
					threadArray[i] = 1;
			
			ProgressControl.Value = 0;
			
			ProgressControl.Minimum = 0;
			ProgressControl.Maximum = files.Count;
			
			await ProcessManager((int)MainWindow.Instance.SimultaneousControl.Value!, threadArray, files.ToArray()).ContinueWith(_ =>
			{
				Dispatcher.UIThread.Post(() =>
				{
					if (!(bool)MainWindow.Instance.ExitAfterwardsControl.IsChecked! || CancellationSource.IsCancellationRequested)
						return;
					
					Close();
					MainWindow.Instance.Exit_OnClick(null, null);
				});
			});
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

				Tools.ActFile(data[dataIndex], MainWindow.Instance.OutputControl.Text!, (bool)MainWindow.Instance.ApplySubsControl.IsChecked!, (int)MainWindow.Instance.SubtitleIndexControl.Value!, (int)MainWindow.Instance.AudioIndexControl.Value!,
					(int)MainWindow.Instance.QualityControl.Value!, (int)MainWindow.Instance.ResolutionOverrideWidthControl.Value!, (int)MainWindow.Instance.ResolutionOverrideHeightControl.Value!, (bool)MainWindow.Instance.HardwareAccelerationControl.IsChecked!,
					(bool)MainWindow.Instance.ColorspaceControl.IsChecked!, (bool)MainWindow.Instance.MetadataTitleControl.IsChecked!, (bool)MainWindow.Instance.FastStartControl.IsChecked!, (bool)MainWindow.Instance.AACControl.IsChecked!, threads, MainWindow.Instance.ExtensionControl.SelectedIndex, (bool)MainWindow.Instance.ApplyResizeControl.IsChecked!, (bool)MainWindow.Instance.PGSSubsControl.IsChecked!);
				
				Dispatcher.UIThread.Post(delegate
				{
					ProgressControl.Value++;
				});
				
				dataIndex += workers;
				queue[idx] = data.Length > dataIndex ? data[dataIndex] : null;
			}

			return Task.CompletedTask;
		}
	}
}