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
	}
}