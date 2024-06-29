using System;
using HardSubberGUI.Enums;
using HardSubberGUI.Views;

namespace HardSubberGUI.Structs
{
	[Serializable]
	public struct SConversionOptions
	{
		public string OutputPath;
		
		public bool ApplyMetadataTitle;
		
		public bool ConvertAudio;
		public bool ConvertColorspace;

		public bool BurnSubsAndAudio;
		public bool UseHWAccel;
		public bool UsePGS;
		public bool FastStart;
		public bool Resize;
		public bool CloseAfterwards;
		public bool ExternalSubs;

		public ESupportedFormat Format;

		public int[] ResizeResolution;

		public int SubtitleIndex;
		public int AudioIndex;

		public int Quality;
		
		public int Threads;
		public int Simultaneous;

		public static SConversionOptions ReadFromUI()
		{
			var options = new SConversionOptions();
			options.OutputPath = MainWindow.Instance.OutputControl.Text;
			options.ApplyMetadataTitle = MainWindow.Instance.MetadataTitleControl.IsChecked.Value;
			options.ConvertAudio = MainWindow.Instance.AACControl.IsChecked.Value;
			options.ConvertColorspace = MainWindow.Instance.ColorspaceControl.IsChecked.Value;
			options.BurnSubsAndAudio = MainWindow.Instance.ApplySubsControl.IsChecked.Value;
			options.UseHWAccel = MainWindow.Instance.HardwareAccelerationControl.IsChecked.Value;
			options.FastStart = MainWindow.Instance.FastStartControl.IsChecked.Value;
			options.UsePGS = MainWindow.Instance.PGSSubsControl.IsChecked.Value;
			options.Resize = MainWindow.Instance.ApplyResizeControl.IsChecked.Value;
			options.CloseAfterwards = MainWindow.Instance.ExitAfterwardsControl.IsChecked.Value;
			options.ExternalSubs = MainWindow.Instance.ExternalSubsControl.IsChecked.Value;
			options.ResizeResolution = new[] { (int)MainWindow.Instance.ResolutionOverrideWidthControl.Value, (int)MainWindow.Instance.ResolutionOverrideHeightControl.Value };
			options.SubtitleIndex = (int)MainWindow.Instance.SubtitleIndexControl.Value;
			options.AudioIndex = (int)MainWindow.Instance.AudioIndexControl.Value;
			options.Quality = (int)MainWindow.Instance.QualityControl.Value;
			options.Threads = (int)MainWindow.Instance.ThreadsControl.Value;
			options.Simultaneous = (int)MainWindow.Instance.SimultaneousControl.Value;
			options.Format = (ESupportedFormat)MainWindow.Instance.ExtensionControl.SelectedIndex;

			return options;
		}

		public void ApplyToUI()
		{
			MainWindow.Instance.OutputControl.Text = OutputPath;
			MainWindow.Instance.MetadataTitleControl.IsChecked = ApplyMetadataTitle;
			MainWindow.Instance.AACControl.IsChecked = ConvertAudio;
			MainWindow.Instance.ColorspaceControl.IsChecked = ConvertColorspace;
			MainWindow.Instance.ApplySubsControl.IsChecked = BurnSubsAndAudio;
			MainWindow.Instance.HardwareAccelerationControl.IsChecked = UseHWAccel;
			MainWindow.Instance.FastStartControl.IsChecked = FastStart;
			MainWindow.Instance.PGSSubsControl.IsChecked = UsePGS;
			MainWindow.Instance.ApplyResizeControl.IsChecked = Resize;
			MainWindow.Instance.ExitAfterwardsControl.IsChecked = CloseAfterwards;
			MainWindow.Instance.ExternalSubsControl.IsChecked = ExternalSubs;
			MainWindow.Instance.ResolutionOverrideWidthControl.Value = ResizeResolution[0];
			MainWindow.Instance.ResolutionOverrideHeightControl.Value = ResizeResolution[1];
			MainWindow.Instance.SubtitleIndexControl.Value = SubtitleIndex;
			MainWindow.Instance.AudioIndexControl.Value = AudioIndex;
			MainWindow.Instance.QualityControl.Value = Quality;
			MainWindow.Instance.ThreadsControl.Value = Threads;
			MainWindow.Instance.SimultaneousControl.Value = Simultaneous;
			MainWindow.Instance.ExtensionControl.SelectedIndex = (int)Format;
		}
	}
}