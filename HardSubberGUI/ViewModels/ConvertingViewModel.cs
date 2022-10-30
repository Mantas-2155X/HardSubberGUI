using Avalonia.Media;

namespace HardSubberGUI.ViewModels
{
	public class ConvertingViewModel : ViewModelBase
	{
		public static string ProgressFormat => "{0}/{3} Files";
		
		#region Tooltips

		public static string CancelTooltip => "Cancel conversion";

		#endregion

		#region Labels

		public static string Text => "Converting...";

		#endregion
		
		#region Buttons

		public static string Cancel => "Cancel";

		#endregion
		
		#region Colors

		public static IBrush MainPanelColor => new SolidColorBrush(new Color(255, 50, 50, 50));

		#endregion
	}
}