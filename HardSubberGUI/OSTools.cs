using System;
using System.Runtime.InteropServices;
using HardSubberGUI.Enums;

namespace HardSubberGUI
{
	public static class OSTools
	{
		public static readonly EGPU CurrentGPU = GetCurrentGPU();
		public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		
		public static string Getlspci()
		{
			var process = Tools.RunProcess("/bin/sh", "-c \"lspci | grep VGA\"", false, true, true);
			var str = "";
			
			while (!process.StandardOutput.EndOfStream)
				str += process.StandardOutput.ReadLine();
			
			return str;
		}

		public static string GetVideoController()
		{
			using var searcher = new System.Management.ManagementObjectSearcher(
				"root\\CIMV2", 
				"SELECT Description FROM Win32_VideoController"
			);

			foreach (var obj in searcher.Get())
				return obj["Description"]?.ToString()?.Trim() ?? "Unknown";

			return "";
		}
		
		public static EGPU GetCurrentGPU()
		{
			var data = !RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? Getlspci() : GetVideoController();
			Console.WriteLine("GetCurrentGPU: " + data);
			
			if (data.Contains("AMD", StringComparison.InvariantCultureIgnoreCase) || data.Contains("Radeon", StringComparison.InvariantCultureIgnoreCase))
			{
				Console.WriteLine("Using AMD GPU");
				return EGPU.AMD;
			}
				
			if (data.Contains("NVIDIA", StringComparison.InvariantCultureIgnoreCase))
			{
				Console.WriteLine("Using NVIDIA GPU");
				return EGPU.NVIDIA;
			}

			Console.WriteLine("No supported GPU found");
			return EGPU.None;
		}
	}
}