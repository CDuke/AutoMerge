using Microsoft.VisualStudio.Shell;
using VsEnvironmetnColors = Microsoft.VisualStudio.PlatformUI.EnvironmentColors;

namespace AutoMerge
{
	internal static class EnvironmentColors
	{
		public static ThemeResourceKey ToolWindowTextBrushKey
		{
			get
			{
				return VsEnvironmetnColors.ToolWindowTextBrushKey;
			}
		}

		public static ThemeResourceKey SystemHighlightBrushKey
		{
			get
			{
				return VsEnvironmetnColors.SystemHighlightBrushKey;
			}
		}

		public static ThemeResourceKey ComboBoxItemMouseOverBackgroundBrushKey
		{
			get { return VsEnvironmetnColors.ComboBoxItemMouseOverBackgroundBrushKey; }
		}

		public static ThemeResourceKey ToolboxContentMouseOverBrushKey
		{
			get { return VsEnvironmetnColors.ToolboxContentMouseOverBrushKey; }
		}
	}
}