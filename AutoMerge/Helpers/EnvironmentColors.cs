using Microsoft.VisualStudio.PlatformUI;
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

		public static ThemeResourceKey ToolboxDisabledContentTextBrushKey
		{
			get
			{
				return VsEnvironmetnColors.ToolboxDisabledContentTextBrushKey;
			}
		}

		public static ThemeResourceKey ToolboxContentMouseOverBrushKey
		{
			get
			{
				return VsEnvironmetnColors.ToolboxContentMouseOverBrushKey;
			}
		}

		public static ThemeResourceKey SelectedItemActiveBrushKey
		{
			get
			{
				return TreeViewColors.SelectedItemActiveBrushKey;
			}
		}

		public static ThemeResourceKey SelectedItemActiveTextBrushKey
		{
			get
			{
				return TreeViewColors.SelectedItemActiveTextBrushKey;
			}
		}

		public static ThemeResourceKey SelectedItemInactiveBrushKey
		{
			get
			{
				return TreeViewColors.SelectedItemInactiveBrushKey;
			}
		}

		public static ThemeResourceKey SelectedItemInactiveTextBrushKey
		{
			get
			{
				return TreeViewColors.SelectedItemInactiveTextBrushKey;
			}
		}
	}
}