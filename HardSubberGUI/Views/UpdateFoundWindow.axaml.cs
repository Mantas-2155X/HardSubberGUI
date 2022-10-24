using Avalonia.Controls;
using Avalonia.Interactivity;

namespace HardSubberGUI.Views
{
	public partial class UpdateFoundWindow : Window
	{
		public UpdateFoundWindow()
		{
			InitializeComponent();
		}

		private void Yes_OnClick(object? sender, RoutedEventArgs? e)
		{
			Tools.OpenURL("https://github.com/Mantas-2155X/HardSubberGUI/releases/latest");
			Close();
		}
		
		private void No_OnClick(object? sender, RoutedEventArgs? e)
		{
			Close();
		}
	}
}