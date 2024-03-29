﻿using Avalonia;
using Avalonia.ReactiveUI;
using System;
using System.IO;
using HardSubberGUI.Views;

namespace HardSubberGUI
{
	public static class Program
	{
		[STAThread]
		public static void Main(string[] args)
		{
			var logsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
			Directory.CreateDirectory(logsDir);
			
			var outputfilestream = new FileStream(Path.Combine(logsDir, "output.log"), FileMode.Create);
			var outputstreamwriter = new StreamWriter(outputfilestream);
			outputstreamwriter.AutoFlush = true;
			Console.SetOut(outputstreamwriter);
			
			var errorfilestream = new FileStream(Path.Combine(logsDir, "error.log"), FileMode.Create);
			var errorstreamwriter = new StreamWriter(errorfilestream);
			errorstreamwriter.AutoFlush = true;
			Console.SetError(errorstreamwriter);

			BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
		}

		public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().UseReactiveUI();
	}
}