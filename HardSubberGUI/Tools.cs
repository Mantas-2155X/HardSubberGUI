using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using HardSubberGUI.Views;

namespace HardSubberGUI
{
	public static class Tools
	{
		public static readonly List<string> SupportedVideoFormats = new () { ".avi", ".mkv", ".m4v", ".mp4" };

		public static readonly string[] Escape = { " ", "[", "]", ",", ":", "'", ";", "(", ")" };

		public static async Task<string> PickVideoFile(Window window)
		{
			var filters = SupportedVideoFormats.Select(supportedVideoFormat => new FilePickerFileType(supportedVideoFormat)).ToList();
			
			var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {AllowMultiple = false, Title = "Select a video file", FileTypeFilter = filters});
			if (result == null || result.Count == 0)
				return "";

			return result[0].TryGetUri(out var uri) ? uri.ToString().Substring(7) : "";
		}

		public static async Task<string> PickDirectory(Window window)
		{
			var result = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {AllowMultiple = false, Title = "Select a directory"});
			if (result == null || result.Count == 0)
				return "";

			return result[0].TryGetUri(out var uri) ? uri.ToString().Substring(7) : "";
		}

		public static string GetffmpegPath()
		{
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					Arguments = "ffmpeg",
					UseShellExecute = false, 
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};
			
			process.StartInfo.FileName = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "where" : "which";
			process.Start();

			var path = "";
			while (!process.StandardOutput.EndOfStream)
			{
				path = process.StandardOutput.ReadLine();
			}

			return path ?? "";
		}

		public static List<string> ProcessFiles(Window window)
		{
			var inputValue = window.FindControl<TextBox>("InputControl");
			var outputValue = window.FindControl<TextBox>("OutputControl");

			if (string.IsNullOrEmpty(inputValue.Text))
				return null;

			var inputInfo = new FileInfo(inputValue.Text);
			
			if (string.IsNullOrEmpty(outputValue.Text))
				outputValue.Text = Path.Combine(inputInfo.Attributes.HasFlag(FileAttributes.Directory) ? inputValue.Text : inputInfo.FullName[..^inputInfo.Name.Length], "output");

			if (!Directory.Exists(outputValue.Text))
				Directory.CreateDirectory(outputValue.Text);

			var files = new List<string>();

			if (inputInfo.Attributes.HasFlag(FileAttributes.Directory))
				files.AddRange(from file in Directory.GetFiles(inputValue.Text) select new FileInfo(file) into info where SupportedVideoFormats.Contains(info.Extension) select info.FullName);
			else
				files.Add(inputValue.Text);

			return files.Count == 0 ? null : files.OrderBy(f => f).ToList();
		}

		public static void ActFile(string file, string output, bool processVideo, 
			int subtitleIndex = 0, int audioIndex = 0, int quality = 0, int resw = 0, int resh = 0, 
			bool hwaccel = false, bool colorspace = false, bool title = false, bool faststart = false, bool aac = false)
		{
			var info = new FileInfo(file);
			
			var shortName = info.Name[..^info.Extension.Length];
			
			var newName = info.FullName.Replace("'", "");
			if (newName != info.FullName)
				info.MoveTo(newName);
				
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = GetffmpegPath(),
					Arguments = "",
					UseShellExecute = true,
					CreateNoWindow = false,
					WindowStyle = ProcessWindowStyle.Normal
				}
			};

			process.StartInfo.Arguments += "-hide_banner -loglevel warning -stats ";
			
			if (hwaccel)
				process.StartInfo.Arguments += "-vaapi_device /dev/dri/renderD128 ";
			
			process.StartInfo.Arguments += $"-i '{info.FullName}' ";
			
			if (processVideo)
			{
				var scaleString = (resw > 0 && resh > 0) ? $"scale={resw}:{resh}," : "";
				var escaped = Escape.Aggregate(info.FullName, (current, str) => current.Replace(str, "\\\\\\" + str));

				if (hwaccel)
				{
					process.StartInfo.Arguments += $"-filter_complex {scaleString}subtitles={escaped}:stream_index={subtitleIndex},format=nv12,hwupload ";
					process.StartInfo.Arguments += "-c:v h264_vaapi ";
				}
				else
				{
					process.StartInfo.Arguments += $"-filter_complex {scaleString}subtitles={escaped}:stream_index={subtitleIndex} ";
					process.StartInfo.Arguments += "-c:v libx264 ";
				}
				
				process.StartInfo.Arguments += $"-map 0:a:{audioIndex} ";
				process.StartInfo.Arguments += $"-qp {quality} ";
			}
			else
			{
				process.StartInfo.Arguments += "-c copy ";
			}

			if (aac)
				process.StartInfo.Arguments += "-c:a aac ";

			if (colorspace)
				process.StartInfo.Arguments += "-color_primaries unknown -color_trc unknown -colorspace unknown ";
			
			if (title)
				process.StartInfo.Arguments += $"-metadata title='{shortName}' ";
			
			if (faststart)
				process.StartInfo.Arguments += "-movflags faststart ";
			
			process.StartInfo.Arguments += "-strict -2 ";
			process.StartInfo.Arguments += $"'{output}/{shortName}.mp4'";

			if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				var args = process.StartInfo.Arguments;
				args = $"-e \"{GetffmpegPath()} {args}\"";
				
				process.StartInfo.FileName = "/usr/bin/xterm";
				process.StartInfo.Arguments = args;
			}

			process.Start();
			MainWindow.Processes.Add(process);
			process.WaitForExit();
			MainWindow.Processes.Remove(process);
		}

		public static void ToggleControls(MainWindow window, bool value)
		{
			window.ColorspaceControl.IsEnabled = value;
			window.InputControl.IsEnabled = value;
			window.OutputControl.IsEnabled = value;
			window.QualityControl.IsEnabled = value;
			window.SimultaneousControl.IsEnabled = value;
			window.ApplySubsControl.IsEnabled = value;
			window.AudioIndexControl.IsEnabled = value;
			window.FastStartControl.IsEnabled = value;
			window.HardwareAccelerationControl.IsEnabled = value;
			window.MetadataTitleControl.IsEnabled = value;
			window.SubtitleIndexControl.IsEnabled = value;
			window.AACControl.IsEnabled = value;
			window.ResolutionOverrideHeightControl.IsEnabled = value;
			window.ResolutionOverrideWidthControl.IsEnabled = value;
			window.ExitControl.IsEnabled = value;
			window.InputDirectoryControl.IsEnabled = value;
			window.InputFileControl.IsEnabled = value;
			window.OutputDirectoryControl.IsEnabled = value;
		}
	}
}