using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

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
				var cancel = this.FindControl<Button>("CancelControl");
				if (cancel != null && cancel.IsEnabled)
					Cancel_OnClick(null, null);
			};
			
			Tools.ToggleControls(this, true);
			
			var args = Environment.GetCommandLineArgs();
			if (args.Length < 2)
				return;
			
			this.FindControl<TextBox>("InputControl").Text = args[1];
		}

		private async void InputFile_OnClick(object? sender, RoutedEventArgs e)
		{
			var label = this.FindControl<TextBox>("InputControl");
			label.Text = await Tools.PickFile(this);
		}
		
		private async void InputDirectory_OnClick(object? sender, RoutedEventArgs e)
		{
			var label = this.FindControl<TextBox>("InputControl");
			label.Text = await Tools.PickDirectory(this);
		}
		
		private async void Output_OnClick(object? sender, RoutedEventArgs e)
		{
			var label = this.FindControl<TextBox>("OutputControl");
			label.Text = await Tools.PickDirectory(this);
		}
		
		private void Exit_OnClick(object? sender, RoutedEventArgs e)
		{
			Environment.Exit(0);
		}

		private void Cancel_OnClick(object? sender, RoutedEventArgs e)
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
			
			var outputValue = this.FindControl<TextBox>("OutputControl").Text;

			var subtitleIndexValue = (int)this.FindControl<NumericUpDown>("SubtitleIndexControl").Value;
			var audioIndexValue = (int)this.FindControl<NumericUpDown>("AudioIndexControl").Value;
			var qualityValue = (int)this.FindControl<NumericUpDown>("QualityControl").Value;
			var resolutionOverrideWidthValue = (int)this.FindControl<NumericUpDown>("ResolutionOverrideWidthControl").Value;
			var resolutionOverrideHeightValue = (int)this.FindControl<NumericUpDown>("ResolutionOverrideHeightControl").Value;

			var hardwareAccelerationValue = (bool)this.FindControl<ToggleSwitch>("HardwareAccelerationControl").IsChecked;
			var colorspaceValue = (bool)this.FindControl<ToggleSwitch>("ColorspaceControl").IsChecked;
			var simultaneousValue = (bool)this.FindControl<ToggleSwitch>("SimultaneousControl").IsChecked;
			var metadataTitleValue = (bool)this.FindControl<ToggleSwitch>("MetadataTitleControl").IsChecked;
			var fastStartValue = (bool)this.FindControl<ToggleSwitch>("FastStartControl").IsChecked;
			var applySubsValue = (bool)this.FindControl<ToggleSwitch>("ApplySubsControl").IsChecked;
			var aacValue = (bool)this.FindControl<ToggleSwitch>("AACControl").IsChecked;
			var exitValue = (bool)this.FindControl<ToggleSwitch>("ExitAfterwardsControl").IsChecked;

			var cancel = this.FindControl<Button>("CancelControl");
			var convert = this.FindControl<Button>("ConvertControl");
			
			var workers = simultaneousValue ? Environment.ProcessorCount / 4 : 1;
			
			if (workers < 1)
				workers = 1;

			cancel.IsEnabled = true;
			convert.IsEnabled = false;

			Tools.ToggleControls(this, false);
			
			CancellationSource.Dispose();
			CancellationSource = new CancellationTokenSource();
			
			await Task.Run(() =>
			{
				try
				{
					Parallel.ForEach(files, new ParallelOptions { MaxDegreeOfParallelism = workers, CancellationToken = CancellationSource.Token }, s =>
					{
						Tools.ActFile(s, outputValue, applySubsValue, subtitleIndexValue, audioIndexValue,
							qualityValue,
							resolutionOverrideWidthValue, resolutionOverrideHeightValue, hardwareAccelerationValue,
							colorspaceValue, metadataTitleValue, fastStartValue, aacValue);
					});
				}
				catch (TaskCanceledException ex)
				{
					Console.WriteLine(ex);
				}
			}).ContinueWith((t) =>
			{
				Dispatcher.UIThread.Post(() =>
				{
					cancel.IsEnabled = false;
					convert.IsEnabled = true;
					
					Tools.ToggleControls(this, true);
					
					if (exitValue && !CancellationSource.IsCancellationRequested)
						Exit_OnClick(null, null);
				});
			});
		}
	}
}