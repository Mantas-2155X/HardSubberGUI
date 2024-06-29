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
using Avalonia.Threading;
using HardSubberGUI.ViewModels;
using System.IO.Compression;
using HardSubberGUI.Enums;
using HardSubberGUI.Structs;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace HardSubberGUI
{
	public static class Tools
	{
		public static readonly List<Process> Processes = new ();

		public static string FfmpegPath = "";
		public static string ConversionOptionsPath = "ConversionOptions.json";

		public static string GetSupportedFormatString(ESupportedFormat format)
		{
			return $".{format.ToString()}";
		}

		public static List<string> GetSupportedFormatStrings()
		{
			var list = new List<string>();
			
			for (var i = 0; i < Enum.GetValues(typeof(ESupportedFormat)).Length; i++)
				list.Add(GetSupportedFormatString((ESupportedFormat)i));
			
			return list;
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

			if (!OSTools.IsWindows)
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
			
			if (!OSTools.IsWindows)
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
		
		public static string GetffmpegPath()
		{
			var exeName = !OSTools.IsWindows ? "ffmpeg" : "bin/ffmpeg.exe";
			var exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, $"ffmpeg/{exeName}");
			
			if (File.Exists(exePath))
			{
				Console.WriteLine("Using local ffmpeg installation");
				return exePath;
			}

			var process = RunProcess(!OSTools.IsWindows ? "which" : "where.exe", "ffmpeg", false, true, true);
			var path = "";
			
			while (!process.StandardOutput.EndOfStream)
				path = process.StandardOutput.ReadLine();
			
			if (OSTools.IsWindows && !path!.Contains(".exe"))
				return "";
			
			return path!;
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
		
		public static void ActFile(string file, SConversionOptions conversionOptions, int threads)
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
			
			if (conversionOptions.UseHWAccel)
			{
				switch (OSTools.CurrentGPU)
				{
					case EGPU.AMD:
					{
						if (!OSTools.IsWindows)
							process.StartInfo.Arguments += "-vaapi_device /dev/dri/renderD128 ";
						break;
					}
					case EGPU.NVIDIA:
					{
						if (OSTools.IsWindows)
							process.StartInfo.Arguments += "-vsync 0 ";
						else
							throw new Exception("ERR_HWACCEL_NOTSUPPORTED");
						break;
					}
				}
			}
			
			process.StartInfo.Arguments += $"-i '{info.FullName}' ";
			
			var subsFilter = "";
			var streamIndex = "";
			
			var scaleString = conversionOptions.Resize ? $"scale={conversionOptions.ResizeResolution[0]}:{conversionOptions.ResizeResolution[1]}," : "";
			
			var escaped = info.FullName;
			escaped = !OSTools.IsWindows ? FileTools.Escape.Aggregate(info.FullName, (current, str) => current.Replace(str, "\\\\\\" + str)) : FileTools.EscapeWindowsString(escaped);

			if (conversionOptions.ExternalSubs)
			{
				var newPath = escaped[..^info.Extension.Length];
				
				if (File.Exists(newPath + ".srt"))
				{
					escaped = newPath + ".srt";
					subsFilter = "subtitles";
				}
				else if (File.Exists(newPath + ".ass"))
				{
					escaped = newPath + ".ass";
					subsFilter = "ass";
				}
			}
			else
			{
				subsFilter = "subtitles";
				streamIndex = $":stream_index={conversionOptions.SubtitleIndex}";
			}
			
			if (conversionOptions.UseHWAccel)
			{
				switch (OSTools.CurrentGPU)
				{
					case EGPU.AMD:
					{
						if (OSTools.IsWindows)
						{
							if (conversionOptions.BurnSubsAndAudio)
								process.StartInfo.Arguments += $"-filter_complex {scaleString}{subsFilter}={escaped}{streamIndex},format=nv12 ";
							else
								process.StartInfo.Arguments += $"-filter_complex {scaleString}format=nv12 ";
							
							process.StartInfo.Arguments += "-c:v h264_amf ";
						}
						else
						{
							if (conversionOptions.BurnSubsAndAudio)
								process.StartInfo.Arguments += $"-filter_complex {scaleString}{subsFilter}={escaped}{streamIndex},format=nv12,hwupload ";
							else
								process.StartInfo.Arguments += $"-filter_complex {scaleString}format=nv12,hwupload ";

							process.StartInfo.Arguments += "-c:v h264_vaapi ";
						}
						break;
					}
					case EGPU.NVIDIA:
					{
						if (conversionOptions.BurnSubsAndAudio)	
							process.StartInfo.Arguments += $"-filter_complex {scaleString}{subsFilter}={escaped}{streamIndex},format=nv12,hwupload_cuda ";
						else
							process.StartInfo.Arguments += $"-filter_complex {scaleString}format=nv12,hwupload_cuda ";
						
						process.StartInfo.Arguments += "-c:v h264_nvenc ";
						break;
					}
					case EGPU.None:
					{
						throw new Exception("ERR_HWACCEL_NOTSUPPORTED");
					}
				}
			}
			else
			{
				if (conversionOptions.BurnSubsAndAudio)
				{
					if (conversionOptions.UsePGS)
						process.StartInfo.Arguments += $"-filter_complex [0:v][0:s:{conversionOptions.SubtitleIndex}]overlay[v] -map [v] ";
					else
						process.StartInfo.Arguments += $"-filter_complex {scaleString}{subsFilter}={escaped}:stream_index={conversionOptions.SubtitleIndex} ";
				}

				process.StartInfo.Arguments += "-c:v libx264 ";
			}

			if (conversionOptions.BurnSubsAndAudio)
				process.StartInfo.Arguments += $"-map 0:a:{conversionOptions.AudioIndex} ";

			process.StartInfo.Arguments += $"-qp {conversionOptions.Quality} ";

			if (conversionOptions.ConvertAudio)
				process.StartInfo.Arguments += "-c:a aac ";

			if (conversionOptions.ConvertColorspace)
				process.StartInfo.Arguments += "-color_primaries unknown -color_trc unknown -colorspace unknown ";
			
			if (conversionOptions.ApplyMetadataTitle)
				process.StartInfo.Arguments += $"-metadata title='{shortName}' ";
			
			if (conversionOptions.FastStart)
				process.StartInfo.Arguments += "-movflags faststart ";
			
			if (threads > 0)
				process.StartInfo.Arguments += $"-threads {threads} ";
			
			process.StartInfo.Arguments += "-strict -2 ";
			process.StartInfo.Arguments += $"'{conversionOptions.OutputPath}/{shortName}{GetSupportedFormatString(conversionOptions.Format)}'";
			
			if (!OSTools.IsWindows)
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

		public static void SaveConversionOptions()
		{
			var conversionOptions = SConversionOptions.ReadFromUI();
			
			var json = JsonConvert.SerializeObject(conversionOptions);
			if (json == null)
			{
				Console.WriteLine("Failed to serialize Conversion Options");
				return;
			}
			
			var workingDir = AppDomain.CurrentDomain.BaseDirectory;
			var optionsPath = Path.Combine(workingDir, ConversionOptionsPath);
			
			File.WriteAllText(optionsPath, json);
			Console.WriteLine("Saved current Conversion Options");
		}

		public static void LoadConversionOptions()
		{
			var workingDir = AppDomain.CurrentDomain.BaseDirectory;
			var optionsPath = Path.Combine(workingDir, ConversionOptionsPath);
			
			if (!File.Exists(optionsPath))
				return;

			var text = File.ReadAllText(optionsPath);
			
			var conversionOptions = JsonConvert.DeserializeObject<SConversionOptions>(text);
			conversionOptions.ApplyToUI();
			
			Console.WriteLine("Applied saved Conversion Options");
		}
	}
}