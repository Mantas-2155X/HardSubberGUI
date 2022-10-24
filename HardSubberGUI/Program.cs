using Avalonia;
using Avalonia.ReactiveUI;
using System;
using HardSubberGUI.Views;

namespace HardSubberGUI
{
	public static class Program
	{
		[STAThread]
		public static void Main(string[] args) => BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

		public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<App>().UsePlatformDetect().LogToTrace().UseReactiveUI();
	}
}