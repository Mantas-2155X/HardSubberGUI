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
using System.IO.Compression;
using System.Web;
using Newtonsoft.Json.Linq;

namespace HardSubberGUI
{
	public static class Tools
	{
		public static readonly string[] Escape = { " ", "[", "]", ",", ":", ";", "(", ")" };

		public static readonly List<string> SupportedVideoFormats = new () { ".mp4", ".m4v", ".mkv", ".avi" };
		public static readonly List<Process> Processes = new ();

		public static readonly GPU CurrentGPU = GetCurrentGPU();
		public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		public static string FfmpegPath = "";
		
		public static async Task<string> PickFile(Window window)
		{
			var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {AllowMultiple = false, Title = "Select a file"});
			if (result.Count == 0)
				return "";

			var url = HttpUtility.UrlDecode(result[0].Path.ToString());
			return url.Substring(!IsWindows ? 7 : 8);
		}

		public static async Task<string> PickDirectory(Window window)
		{
			var result = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {AllowMultiple = false, Title = "Select a directory"});
			if (result.Count == 0)
				return "";

			var url = HttpUtility.UrlDecode(result[0].Path.ToString());
			return url.Substring(!IsWindows ? 7 : 8);
		}

		public static Process RunProcess(string executable, string arguments, bool shell = false, bool nowindow = true, bool redirect = false)
		{
			var process = new Process
			{
				StartInfo = new ProcessStartInfo
				{
					FileName = executable,
					Arguments = arguments,
					UseShellExecute = shell, 
					RedirectStandardOutput = redirect,
					CreateNoWindow = nowindow
				}
			};
			
			process.Start();
			return process;
		}

		public static async Task DownloadFFmpeg(Button progressControl)
		{
			Console.WriteLine("System ffmpeg not found");
			
			Dispatcher.UIThread.Post(() =>
			{
				progressControl.Content = MainWindowViewModel.ConvertDownloadingffmpeg;
				progressControl.IsEnabled = false;
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

			if (File.Exists(downloadTo))
				File.Delete(downloadTo);

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
					progressControl.Content = formatted;
				});
			};
				
			using var client = new HttpClient(progress);
			await using var s = await client.GetStreamAsync(url);
				
			await using var fs = new FileStream(downloadTo, FileMode.OpenOrCreate);
			await s.CopyToAsync(fs);
			await s.DisposeAsync();
			s.Close();

			await fs.DisposeAsync();
			fs.Close();
			
			client.Dispose();
			
			Console.WriteLine("Extracting..");
			
			if (!IsWindows)
			{
				Directory.CreateDirectory(moveTo);
				RunProcess("tar", $"-xf {downloadTo} -C {moveTo} --strip-components 1");

				FfmpegPath = Path.Combine(moveTo, "ffmpeg");
				return;
			}

			var root = "";

			try
			{
				ZipFile.ExtractToDirectory(downloadTo, workingDir);

				var file = ZipFile.OpenRead(downloadTo);
				
				var entry = file.Entries.FirstOrDefault();
				if (entry != null)
					root = entry.FullName;
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
			
			if (root != "")
				Directory.Move(root, moveTo);
			
			FfmpegPath = Path.Combine(moveTo, "bin/ffmpeg.exe");
		}

		public static void OpenURL(string url)
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				url = url.Replace("&", "^&");
				Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			{
				Process.Start("xdg-open", url);
			}
			else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
			{
				Process.Start("open", url);
			}
		}
		
		public static string GetffmpegPath()
		{
			var exeName = !IsWindows ? "ffmpeg" : "bin/ffmpeg.exe";
			var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"ffmpeg/{exeName}");
			
			if (File.Exists(exePath))
			{
				Console.WriteLine("Using local ffmpeg installation");
				return exePath;
			}

			var process = RunProcess(!IsWindows ? "which" : "where.exe", "ffmpeg", false, true, true);
			var path = "";
			
			while (!process.StandardOutput.EndOfStream)
				path = process.StandardOutput.ReadLine();
			
			if (IsWindows && !path!.Contains(".exe"))
				return "";
			
			return path!;
		}

		public static string Getlspci()
		{
			var process = RunProcess("/bin/sh", "-c \"lspci | grep VGA\"", false, true, true);
			var str = "";
			
			while (!process.StandardOutput.EndOfStream)
				str += process.StandardOutput.ReadLine();
			
			return str;
		}

		public static string GetVideoController()
		{
			var process = RunProcess("wmic", "PATH Win32_videocontroller GET description", false, true, true);
			var str = "";
			
			while (!process.StandardOutput.EndOfStream)
				str += process.StandardOutput.ReadLine();
			
			return str;
		}

		public static string? IsOutdated()
		{
			using var client = new HttpClient();
			client.DefaultRequestHeaders.UserAgent.TryParseAdd("check-outdated");
			
			var obj = JObject.Parse(client.GetStringAsync("https://api.github.com/repos/Mantas-2155X/HardSubberGUI/releases/latest").Result);
			if (obj == null)
				return null;

			return MainWindowViewModel.Version != obj["tag_name"].ToString() ? obj["body"].ToString() : null;
		}

		public static GPU GetCurrentGPU()
		{
			var data = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Getlspci() : GetVideoController();
			Console.WriteLine("GetCurrentGPU: " + data);
			
			if (data.Contains("AMD", StringComparison.InvariantCultureIgnoreCase) || data.Contains("Radeon", StringComparison.InvariantCultureIgnoreCase))
			{
				Console.WriteLine("Using AMD GPU");
				return GPU.AMD;
			}
				
			if (data.Contains("NVIDIA", StringComparison.InvariantCultureIgnoreCase))
			{
				Console.WriteLine("Using NVIDIA GPU");
				return GPU.NVIDIA;
			}

			Console.WriteLine("No supported GPU found");
			return GPU.None;
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
		
		public static List<string>? GetFiles(string input)
		{
			if (string.IsNullOrEmpty(input))
				return null;

			var inputInfo = new FileInfo(input);
			
			var files = new List<string>();
			if (inputInfo.Attributes.HasFlag(FileAttributes.Directory))
			{
				files.AddRange(from file in Directory.GetFiles(input) select new FileInfo(file) into info where SupportedVideoFormats.Contains(info.Extension) select info.FullName);
			}
			else
			{
				if (SupportedVideoFormats.Contains(inputInfo.Extension))
					files.Add(input);
			}

			return files.Count == 0 ? null : files.OrderBy(f => f).ToList();
		}

		public static string EscapeWindowsString(string input)
		{
			return input.
				Replace("\\", "/").
				Replace("[", "\\[").
				Replace("]", "\\]").
				Replace("(", "\\`(").
				Replace(")", "\\`)").
				Replace(" ", "\\` ").
				Replace(",", "\\`,").
				Replace(";", "\\`;").
				Replace(":", "\\\\`:");
		}
		
		public static void ActFile(string file, string output, bool processVideo, 
			int subtitleIndex = 0, int audioIndex = 0, int quality = 0, int resw = 0, int resh = 0, 
			bool hwaccel = false, bool colorspace = false, bool title = false, bool faststart = false, bool aac = false, int threads = 0, int format = 0, bool resize = false, bool pgs = false)
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
					WindowStyle = ProcessWindowStyle.Minimized,
					CreateNoWindow = false
				}
			};
			
			process.StartInfo.Arguments += "-hide_banner -loglevel warning -stats ";
			
			if (hwaccel)
			{
				switch (CurrentGPU)
				{
					case GPU.AMD:
					{
						if (!IsWindows)
							process.StartInfo.Arguments += "-vaapi_device /dev/dri/renderD128 ";
						break;
					}
					case GPU.NVIDIA:
					{
						if (IsWindows)
							process.StartInfo.Arguments += "-vsync 0 ";
						else
							throw new Exception("ERR_HWACCEL_NOTSUPPORTED");
						break;
					}
				}
			}
			
			process.StartInfo.Arguments += $"-i '{info.FullName}' ";
			
			var scaleString = resize ? $"scale={resw}:{resh}," : "";

			var escaped = info.FullName;
			escaped = !IsWindows ? Escape.Aggregate(info.FullName, (current, str) => current.Replace(str, "\\\\\\" + str)) : EscapeWindowsString(escaped);

			if (hwaccel)
			{
				switch (CurrentGPU)
				{
					case GPU.AMD:
					{
						if (IsWindows)
						{
							if (processVideo)
								process.StartInfo.Arguments += $"-filter_complex {scaleString}subtitles={escaped}:stream_index={subtitleIndex},format=nv12 ";
							else
								process.StartInfo.Arguments += $"-filter_complex {scaleString}format=nv12 ";
							
							process.StartInfo.Arguments += "-c:v h264_amf ";
						}
						else
						{
							if (processVideo)
								process.StartInfo.Arguments += $"-filter_complex {scaleString}subtitles={escaped}:stream_index={subtitleIndex},format=nv12,hwupload ";
							else
								process.StartInfo.Arguments += $"-filter_complex {scaleString}format=nv12,hwupload ";

							process.StartInfo.Arguments += "-c:v h264_vaapi ";
						}
						break;
					}
					case GPU.NVIDIA:
					{
						if (processVideo)	
							process.StartInfo.Arguments += $"-filter_complex {scaleString}subtitles={escaped}:stream_index={subtitleIndex},format=nv12,hwupload_cuda ";
						else
							process.StartInfo.Arguments += $"-filter_complex {scaleString}format=nv12,hwupload_cuda ";
						
						process.StartInfo.Arguments += "-c:v h264_nvenc ";
						break;
					}
					case GPU.None:
					{
						throw new Exception("ERR_HWACCEL_NOTSUPPORTED");
					}
				}
			}
			else
			{
				if (processVideo)
				{
					if (pgs)
						process.StartInfo.Arguments += $"-filter_complex [0:v][0:s:{subtitleIndex}]overlay[v] -map [v] ";
					else
						process.StartInfo.Arguments += $"-filter_complex {scaleString}subtitles={escaped}:stream_index={subtitleIndex} ";
				}

				process.StartInfo.Arguments += "-c:v libx264 ";
			}

			if (processVideo)
				process.StartInfo.Arguments += $"-map 0:a:{audioIndex} ";

			process.StartInfo.Arguments += $"-qp {quality} ";

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
			process.StartInfo.Arguments += $"'{output}/{shortName}{SupportedVideoFormats[format]}'";
			
			if (!IsWindows)
			{
				var args = process.StartInfo.Arguments;
				args = $"-iconic -e \"{FfmpegPath} {args}\"";
				
				process.StartInfo.FileName = "xterm";
				process.StartInfo.Arguments = args;
			}
			else
			{
				var args = process.StartInfo.Arguments;
				args = $"\"& '{FfmpegPath}' {args}\"";
				
				process.StartInfo.FileName = "powershell.exe";
				process.StartInfo.Arguments = args;
			}

			Console.WriteLine(process.StartInfo.Arguments);
			
			Processes.Add(process);
			
			process.Start();
			process.WaitForExit();
		}

		public enum GPU
		{
			None,
			NVIDIA,
			AMD
		}
	}
}