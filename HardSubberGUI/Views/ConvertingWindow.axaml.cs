using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using HardSubberGUI.Structs;

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
					Tools.Processes[index].Kill(OSTools.IsWindows);
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

			var files = FileTools.GetFiles(MainWindow.Instance.InputControl.Text!);
			if (files == null)
				return;

			ProgressControl.Value = 0;
			ProgressControl.Minimum = 0;
			ProgressControl.Maximum = files.Count;
			
			CancellationSource.Dispose();
			CancellationSource = new CancellationTokenSource();

			var conversionOptions = SConversionOptions.ReadFromUI();
			
			var simul = Math.Clamp((int)MainWindow.Instance.SimultaneousControl.Value!, 1, files.Count);
			var threads = (int)MainWindow.Instance.ThreadsControl.Value! / simul;

			await Task.Run(() =>
			{
				Parallel.ForEach(files, new ParallelOptions {CancellationToken = CancellationSource.Token, MaxDegreeOfParallelism = simul}, file => 
				{
					try
					{
						Tools.ActFile(file, conversionOptions, threads);
					}
					catch (Exception e)
					{
						Console.WriteLine($"Exception in {file}\n" + e);
					}

					Console.WriteLine($"Finished {file}");
					
					Dispatcher.UIThread.Post(delegate
					{
						ProgressControl.Value++;
					});
				});
			
				Dispatcher.UIThread.Post(delegate
				{
					if (!(bool)MainWindow.Instance.ExitAfterwardsControl.IsChecked! || CancellationSource.IsCancellationRequested)
						return;
					
					Close();
					MainWindow.Instance.Exit_OnClick(null, null);
				});
			});
		}
	}
}