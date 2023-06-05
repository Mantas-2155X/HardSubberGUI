using System;
using System.IO;
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

			var files = Tools.GetFiles(MainWindow.Instance.InputControl.Text!);
			if (files == null)
				return;

			ProgressControl.Value = 0;
			ProgressControl.Minimum = 0;
			ProgressControl.Maximum = files.Count;
			
			CancellationSource.Dispose();
			CancellationSource = new CancellationTokenSource();

			var options = new object[]
			{
				MainWindow.Instance.OutputControl.Text!, (bool)MainWindow.Instance.ApplySubsControl.IsChecked!, (int)MainWindow.Instance.SubtitleIndexControl.Value!, (int)MainWindow.Instance.AudioIndexControl.Value!,
				(int)MainWindow.Instance.QualityControl.Value!, (int)MainWindow.Instance.ResolutionOverrideWidthControl.Value!, (int)MainWindow.Instance.ResolutionOverrideHeightControl.Value!, (bool)MainWindow.Instance.HardwareAccelerationControl.IsChecked!,
				(bool)MainWindow.Instance.ColorspaceControl.IsChecked!, (bool)MainWindow.Instance.MetadataTitleControl.IsChecked!, (bool)MainWindow.Instance.FastStartControl.IsChecked!, (bool)MainWindow.Instance.AACControl.IsChecked!, -1, MainWindow.Instance.ExtensionControl.SelectedIndex, (bool)MainWindow.Instance.ApplyResizeControl.IsChecked!, (bool)MainWindow.Instance.PGSSubsControl.IsChecked!
			};

			var simul = Math.Clamp((int)MainWindow.Instance.SimultaneousControl.Value!, 1, files.Count);
			var threads = (int)MainWindow.Instance.ThreadsControl.Value! / simul;

			await Task.Run(() =>
			{
				Parallel.ForEach(files, new ParallelOptions {CancellationToken = CancellationSource.Token, MaxDegreeOfParallelism = simul}, file => 
				{
					try
					{
						Tools.ActFile(file, (string)options[0], (bool)options[1], (int)options[2], (int)options[3],
							(int)options[4], (int)options[5], (int)options[6], (bool)options[7], (bool)options[8],
							(bool)options[9], (bool)options[10], (bool)options[11], threads, (int)options[13],
							(bool)options[14], (bool)options[15]);
					}
					catch (Exception e)
					{
						Console.WriteLine($"Exception in {file}\n" + e);
					}

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