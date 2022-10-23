using System;
using System.Reflection;
using Avalonia.Media;

namespace HardSubberGUI.ViewModels
{
	public class MainWindowViewModel : ViewModelBase
	{
		public static string Version => "v" + Assembly.GetEntryAssembly()!.GetName().Version;
		public static int AvailableThreads => Environment.ProcessorCount;

		#region Tooltips

		public static string ExitTooltip => "Close HardSubberGUI";
		public static string CancelTooltip => "Cancel video conversion";
		public static string CovertTooltip => "Start video conversion";
		
		public static string PickFileTooltip => "Pick video file to convert";
		public static string PickDirectoryInputTooltip => "Pick directory containing videos to convert";
		public static string PickDirectoryOutputTooltip => "Pick directory where converted videos will be placed\nIf not specified: /output";
		
		public static string SubtitleIndexTooltip => "Subtitle index to apply";
		public static string AudioIndexTooltip => "Audio index to apply";
		public static string QualityTooltip => "Quality used for conversion\nRange: 0-51 (more-less)\nRecommended: 22-26";
		public static string ResolutionTooltip => "Resize video\nIf zero: do not resize";
		public static string ThreadsTooltip => "Limit CPU threads used for conversion";
		
		public static string ColorspaceTooltip => "Reset the videos colorspace\nCan fix not loading in cytu.be";
		public static string HardwareAccelerationTooltip => "Use GPU to accelerate conversion";
		public static string SimultaneousTooltip => "Convert multiple videos at once\nOnly available if converting a directory\nWorker count: threads / 4";
		public static string ExitAfterwardsTooltip => "Close HardSubberGUI after conversion is finished";
		public static string MetadataTitleTooltip => "Set the videos metadata title to filename\nFixes 'Raw Video' title in cytu.be";
		public static string FastStartTooltip => "Optimize for web playback\nCan fix not loading in cytu.be";
		public static string ApplySubsTooltip => "Apply subtitles, audio and resizing";
		public static string AACTooltip => "Convert audio codec to AAC\nCan fix not loading in cytu.be";
		
		#endregion
		
		#region Buttons

		public static string ConvertVideos => "Convert";
		public static string ConvertDownloadingffmpeg => "Downloading";
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
		public static string ResolutionOverride => "Resize Resolution";
		public static string Threads => "Limit CPU Threads";

		#endregion

		#region Colors

		public static IBrush MainPanelColor => new SolidColorBrush(new Color(255, 50, 50, 50));
		public static IBrush SecondaryTextColor => new SolidColorBrush(new Color(255, 155, 155, 155));

		#endregion
	}
}