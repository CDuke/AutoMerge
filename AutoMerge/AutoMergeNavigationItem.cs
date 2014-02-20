using System;
using System.ComponentModel.Composition;
using System.Drawing;
using AutoMerge.Base;
using Microsoft.TeamFoundation.Controls;
using Microsoft.VisualStudio.Shell;

namespace AutoMerge
{
	[TeamExplorerNavigationItem(LinkId, 210)]
	public class AutoMergeNavigationItem : TeamExplorerBaseNavigationItem
	{
		#region Members

		public const string LinkId = "02A9D8B3-287B-4C55-83E7-7BFDB435546D";

		#endregion

		/// <summary>
		/// Constructor.
		/// </summary>
		[ImportingConstructor]
		public AutoMergeNavigationItem([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
			: base(serviceProvider)
		{
			Text = Resources.AutoMergePageName;
			Image = Resources.MergeImage;
			IsVisible = true;
		}

		/// <summary> 
		/// Execute the link action.
		/// </summary> 
		public override void Execute()
		{
			// Navigate to the recent changes page 
			var teamExplorer = GetService<ITeamExplorer>();
			if (teamExplorer != null)
			{
				teamExplorer.NavigateToPage(new Guid(AutoMergePage.PageId), null);
			}
		}

		public override void Invalidate()
		{
			base.Invalidate();
			IsVisible = HasCollection();
		}

		private bool HasCollection()
		{
			return CurrentContext != null
				&& CurrentContext.HasCollection;
		}
	}
}