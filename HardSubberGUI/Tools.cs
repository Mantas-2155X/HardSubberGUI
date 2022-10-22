using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Handlers;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using HardSubberGUI.ViewModels;
using HardSubberGUI.Views;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace HardSubberGUI
{
	public static class Tools
	{
		public static readonly string[] Escape = { " ", "[", "]", ",", ":", ";", "(", ")" };

		public static readonly List<string> SupportedVideoFormats = new () { ".avi", ".mkv", ".m4v", ".mp4" };
		public static readonly List<Process> Processes = new ();

		public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		public static string FfmpegPath = "";
		
		public static async Task<string> PickFile(Window window)
		{
			var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {AllowMultiple = false, Title = "Select a file"});
			if (result.Count == 0)
				return "";

			return result[0].TryGetUri(out var uri) ? uri.ToString().Substring(!IsWindows ? 7 : 8) : "";
		}

		public static async Task<string> PickDirectory(Window window)
		{
			var result = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {AllowMultiple = false, Title = "Select a directory"});
			if (result.Count == 0)
				return "";

			return result[0].TryGetUri(out var uri) ? uri.ToString().Substring(!IsWindows ? 7 : 8) : "";
		}

		public static void RunProcess(string executable, string arguments)
		{
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = executable,
					Arguments = arguments,
					UseShellExecute = false, 
					CreateNoWindow = true
				}
			};
			
			process.Start();
		}

		public static async Task DownloadFFmpeg(MainWindow window)
		{
			Console.WriteLine("System ffmpeg not found");
			
			Dispatcher.UIThread.Post(() =>
			{
				window.ConvertControl.Content = MainWindowViewModel.ConvertDownloadingffmpeg;
				window.ConvertControl.IsEnabled = false;
			});

			var workingDir = AppDomain.CurrentDomain.BaseDirectory;

			string fileName;
			string url;

			if (!IsWindows)
			{
				fileName = "ffmpeg-release-amd64-static.tar.xz";
				url = "https://johnvansickle.com/ffmpeg/releases/ffmpeg-release-amd64-static.tar.xz";
			}
			else
			{
				fileName = "ffmpeg-release-essentials.zip";
				url = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";
			}

			var downloadTo = Path.Combine(workingDir, fileName);
			var moveTo = Path.Combine(workingDir, "ffmpeg");

			if (!File.Exists(Path.Combine(workingDir, fileName)))
			{
				Console.WriteLine("Downloading ffmpeg");

				var handler = new HttpClientHandler { AllowAutoRedirect = true };
				var progress = new ProgressMessageHandler(handler);

				var previousFormatted = "";
				progress.HttpReceiveProgress += (_, args) =>
				{
					var percentage = (double)args.BytesTransferred / args.TotalBytes;
					var formatted = $"{percentage:P1}";

					if (previousFormatted == formatted)
						return;

					previousFormatted = formatted;
					
					Dispatcher.UIThread.Post(() =>
					{
						window.ConvertControl.Content = formatted;
					});
				};
				
				using var client = new HttpClient(progress);
				await using var s = await client.GetStreamAsync(url);
				
				await using var fs = new FileStream(downloadTo, FileMode.OpenOrCreate);
				await s.CopyToAsync(fs);
			}

			Console.WriteLine("Extracting..");
			
			if (!IsWindows)
			{
				Directory.CreateDirectory(moveTo);
				RunProcess("tar", $"-xf {downloadTo} -C {moveTo} --strip-components 1");

				FfmpegPath = Path.Combine(moveTo, "ffmpeg");
				return;
			}

			var root = "";

			await using (var stream = File.OpenRead(downloadTo))
			{
				var reader = ReaderFactory.Open(stream);
				while (reader.MoveToNextEntry())
				{
					if (root == "")
						root = reader.Entry.Key;
							
					if (reader.Entry.IsDirectory)
						continue;
							
					Console.WriteLine(reader.Entry.Key);
					reader.WriteEntryToDirectory(workingDir, new ExtractionOptions() { ExtractFullPath = true, Overwrite = true });
				}
			}
			
			Directory.Move(root, moveTo);
			
			FfmpegPath = Path.Combine(moveTo, "bin/ffmpeg.exe");
		}
		
		public static string GetffmpegPath()
		{
			var path = "";
			
			/*var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					Arguments = "ffmpeg",
					UseShellExecute = false, 
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};
			
			process.StartInfo.FileName = !IsWindows ? "which" : "where.exe";
			process.Start();
			
			while (!process.StandardOutput.EndOfStream)
			{
				path = process.StandardOutput.ReadLine();
			}*/

			if (path != "") 
				return path!;
			
			var exeName = !IsWindows ? "ffmpeg" : "bin/ffmpeg.exe";
			if (!File.Exists($"ffmpeg/{exeName}")) 
				return path;
				
			Console.WriteLine("Using local installation");
			path = $"{AppDomain.CurrentDomain.BaseDirectory}/ffmpeg/{exeName}";

			return path;
		}

		public static string Getlspci()
		{
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					Arguments = "-c \"lspci | grep VGA\"",
					UseShellExecute = false, 
					RedirectStandardOutput = true,
					CreateNoWindow = true
				}
			};
			
			process.StartInfo.FileName = "/bin/sh";
			process.Start();

			var str = "";
			while (!process.StandardOutput.EndOfStream)
			{
				str = process.StandardOutput.ReadLine();
			}

			return str ?? "";
		}
		
		public static bool IsHardwareAccelerationSupported()
		{
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux)) 
				return false;
			
			var lspci = Getlspci();
			return lspci.Contains("AMD");
		}

		//https://dotnetcodr.com/2015/11/03/divide-an-integer-into-groups-with-c/
		public static IEnumerable<int> DistributeInteger(int total, int divider)
		{
			if (divider == 0)
			{
				yield return 0;
			}
			else
			{
				var rest = total % divider;
				var result = total / (double)divider;
				
				for (var i = 0; i < divider; i++)
				{
					if (rest-- > 0)
						yield return (int)Math.Ceiling(result);
					else
						yield return (int)Math.Floor(result);
				}
			}
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
			window.HardwareAccelerationControl.IsEnabled = IsHardwareAccelerationSupported() && value;
			window.MetadataTitleControl.IsEnabled = value;
			window.SubtitleIndexControl.IsEnabled = value;
			window.AACControl.IsEnabled = value;
			window.ResolutionOverrideHeightControl.IsEnabled = value;
			window.ResolutionOverrideWidthControl.IsEnabled = value;
			window.ExitControl.IsEnabled = value;
			window.InputDirectoryControl.IsEnabled = value;
			window.InputFileControl.IsEnabled = value;
			window.OutputDirectoryControl.IsEnabled = value;
			window.ExitAfterwardsControl.IsEnabled = value;
		}
		
		public static List<string>? ProcessFiles(MainWindow window)
		{
			if (string.IsNullOrEmpty(window.InputControl.Text))
				return null;

			var inputInfo = new FileInfo(window.InputControl.Text);
			
			if (string.IsNullOrEmpty(window.OutputControl.Text))
				window.OutputControl.Text = Path.Combine(inputInfo.Attributes.HasFlag(FileAttributes.Directory) ? window.InputControl.Text : inputInfo.FullName[..^inputInfo.Name.Length], "output");

			if (!Directory.Exists(window.OutputControl.Text))
				Directory.CreateDirectory(window.OutputControl.Text);

			var files = new List<string>();

			if (inputInfo.Attributes.HasFlag(FileAttributes.Directory))
			{
				files.AddRange(from file in Directory.GetFiles(window.InputControl.Text) select new FileInfo(file) into info where SupportedVideoFormats.Contains(info.Extension) select info.FullName);
			}
			else
			{
				if (SupportedVideoFormats.Contains(inputInfo.Extension))
					files.Add(window.InputControl.Text);
			}

			return files.Count == 0 ? null : files.OrderBy(f => f).ToList();
		}

		public static void ActFile(string file, string output, bool processVideo, 
			int subtitleIndex = 0, int audioIndex = 0, int quality = 0, int resw = 0, int resh = 0, 
			bool hwaccel = false, bool colorspace = false, bool title = false, bool faststart = false, bool aac = false, int threads = 0)
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
				var escaped = info.FullName;
				
				if (!IsWindows)
				{
					escaped = Escape.Aggregate(info.FullName, (current, str) => current.Replace(str, "\\\\\\" + str));
				}
				else
				{
					escaped = info.FullName.Replace("\\", "/");
					
					escaped = escaped.Replace("[", "\\[");
					escaped = escaped.Replace("]", "\\]");
					
					escaped = escaped.Replace("(", "\\`(");
					escaped = escaped.Replace(")", "\\`)");
					escaped = escaped.Replace(" ", "\\` ");
					escaped = escaped.Replace(",", "\\`,");
					escaped = escaped.Replace(";", "\\`;");
					
					escaped = escaped.Replace(":", "\\\\`:");
				}

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
			
			if (threads > 0)
				process.StartInfo.Arguments += $"-threads {threads} ";
			
			process.StartInfo.Arguments += "-strict -2 ";
			process.StartInfo.Arguments += $"'{output}/{shortName}.mp4'";

			if (!IsWindows)
			{
				var args = process.StartInfo.Arguments;
				args = $"-e \"{FfmpegPath} {args}\"";
				
				process.StartInfo.FileName = "xterm";
				process.StartInfo.Arguments = args;
			}
			else
			{
				var args = process.StartInfo.Arguments;
				args = $"\"{FfmpegPath} {args}\"";
				
				process.StartInfo.FileName = "powershell.exe";
				process.StartInfo.Arguments = args;
			}

			process.Start();
			Processes.Add(process);
			process.WaitForExit();
			Processes.Remove(process);
		}
	}
}