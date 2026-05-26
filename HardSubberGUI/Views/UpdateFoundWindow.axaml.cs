using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace HardSubberGUI.Views
{
	public partial class UpdateFoundWindow : Window
	{
		public UpdateFoundWindow()
		{
			InitializeComponent();
			TextOptions.SetTextRenderingMode(this, TextRenderingMode.Antialias);
		}

		private void Yes_OnClick(object? sender, RoutedEventArgs? e)
		{
			Tools.OpenURL("https://github.com/Mantas-2155X/HardSubberGUI/releases/latest");
			Environment.Exit(0);
		}
		
		private void No_OnClick(object? sender, RoutedEventArgs? e)
		{
			Close();
		}
	}
}