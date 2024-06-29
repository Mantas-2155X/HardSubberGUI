using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace HardSubberGUI
{
	public static class FileTools
	{
		public static readonly string[] Escape = { " ", "[", "]", ",", ":", ";", "(", ")" };

		public static async Task<string> PickFile(Window window)
		{
			var result = await window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions {AllowMultiple = false, Title = "Select a file"});
			if (result.Count == 0)
				return "";

			var url = HttpUtility.UrlDecode(result[0].Path.ToString());
			return url.Substring(!OSTools.IsWindows ? 7 : 8);
		}

		public static async Task<string> PickDirectory(Window window)
		{
			var result = await window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions {AllowMultiple = false, Title = "Select a directory"});
			if (result.Count == 0)
				return "";

			var url = HttpUtility.UrlDecode(result[0].Path.ToString());
			return url.Substring(!OSTools.IsWindows ? 7 : 8);
		}

		public static List<string>? GetFiles(string input)
		{
			if (string.IsNullOrEmpty(input))
				return null;

			var formats = Tools.GetSupportedFormatStrings();
			var inputInfo = new FileInfo(input);
			
			var files = new List<string>();
			if (inputInfo.Attributes.HasFlag(FileAttributes.Directory))
			{
				files.AddRange(from file in Directory.GetFiles(input) select new FileInfo(file) into info where formats.Contains(info.Extension) select info.FullName);
			}
			else
			{
				if (formats.Contains(inputInfo.Extension))
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
	}
}