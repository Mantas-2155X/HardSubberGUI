using Avalonia.Media;

namespace HardSubberGUI.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		#region Buttons

		public static string ConvertVideos => "Convert";
		public static string Cancel => "Cancel";
		public static string PickFile => "Pick File";
		public static string PickDirectory => "Pick Directory";
		public static string Exit => "Exit";

		#endregion

		#region Toggles

		public static string HardwareAcceleration => "Hardware Acceleration";
		public static string AAC => "Convert Audio to AAC";
		public static string MetadataTitle => "Apply Metadata Title";
		public static string FastStart => "Apply Faststart";
		public static string Subs => "Apply Subtitles";
		public static string Colorspace => "Reset Colorspace";
		public static string Simultaneous => "Simultaneous Conversion";
		public static string ExitAfterwards => "Close after conversion";

		#endregion
		
		#region Labels

		public static string Input => "Input Path";
		public static string Output => "Output Path";
		
		public static string SubtitleIndex => "Subtitle Index";
		public static string AudioIndex => "Audio Index";
		public static string Quality => "Quality";
		public static string ResolutionOverride => "Resolution Override";

		#endregion

		#region Colors

		public static IBrush MainPanelColor => new SolidColorBrush(new Color(255, 50, 50, 50));

		#endregion
	}
}