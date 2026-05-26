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

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct DISPLAY_DEVICE
		{
			public int cb;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
			public string DeviceName;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceString;
			public uint StateFlags;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceID;
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
			public string DeviceKey;
		}

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);
		
		public static string GetVideoController()
		{
			var dd = new DISPLAY_DEVICE();
			dd.cb = Marshal.SizeOf(dd);

			uint deviceIndex = 0;
			while (EnumDisplayDevices(null, deviceIndex, ref dd, 0))
			{
				// Look for the primary display adapter (usually the GPU)
				if ((dd.StateFlags & 0x00000001) != 0) // DISPLAY_DEVICE_PRIMARY_DEVICE
				{
					if (!string.IsNullOrWhiteSpace(dd.DeviceString))
						return dd.DeviceString.Trim();
				}
				deviceIndex++;
			}

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