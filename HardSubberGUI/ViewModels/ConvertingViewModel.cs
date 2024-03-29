﻿using Avalonia.Media;

namespace HardSubberGUI.ViewModels
{
	public class ConvertingViewModel : ViewModelBase
	{
		public static string ProgressFormat => "{0}/{3} Files Converted";
		
		#region Tooltips

		public static string CancelTooltip => "Cancel conversion";

		#endregion

		#region Labels

		public static string Text => "Converting...\nSee minimized terminals for more info.";

		#endregion
		
		#region Buttons

		public static string Cancel => "Cancel";

		#endregion
		
		#region Colors

		public static IBrush MainPanelColor => new SolidColorBrush(new Color(255, 50, 50, 50));

		#endregion
	}
}