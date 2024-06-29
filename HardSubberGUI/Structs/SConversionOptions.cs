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

		public ESupportedFormat Format;

		public int[] ResizeResolution;

		public int SubtitleIndex;
		public int AudioIndex;

		public int Quality;
		
		public int Threads;

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
			options.ResizeResolution = new[] { (int)MainWindow.Instance.ResolutionOverrideWidthControl.Value, (int)MainWindow.Instance.ResolutionOverrideHeightControl.Value };
			options.SubtitleIndex = (int)MainWindow.Instance.SubtitleIndexControl.Value;
			options.AudioIndex = (int)MainWindow.Instance.AudioIndexControl.Value;
			options.Quality = (int)MainWindow.Instance.QualityControl.Value;
			options.Threads = (int)MainWindow.Instance.ThreadsControl.Value;
			options.Format = (ESupportedFormat)MainWindow.Instance.ExtensionControl.SelectedIndex;

			return options;
		}
	}
}