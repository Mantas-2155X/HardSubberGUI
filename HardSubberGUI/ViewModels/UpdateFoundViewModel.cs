﻿using Avalonia.Media;

namespace HardSubberGUI.ViewModels
{
	public class UpdateFoundViewModel : ViewModelBase
	{
		#region Tooltips

		public static string YesTooltip => "Open download URL";
		public static string NoTooltip => "Continue without updating";

		#endregion

		#region Labels

		public static string Text => "A new update is available.";

		#endregion
		
		#region Buttons

		public static string Yes => "Open Link";
		public static string No => "Ignore";

		#endregion
		
		#region Colors

		public static IBrush MainPanelColor => new SolidColorBrush(new Color(255, 50, 50, 50));
		public static IBrush SecondaryPanelColor => new SolidColorBrush(new Color(255, 100, 100, 100));

		#endregion
	}
}