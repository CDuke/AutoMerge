using AutoMerge.Base;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge
{
	[TeamExplorerSection(SectionId, AutoMergePage.PageId, 20)]
	public class BranchesSection : TeamExplorerBaseSection
	{
		public const string SectionId = "36BF6F52-F4AC-44A0-9985-817B2A65B3B0";

		/// <summary>
		/// Constructor.
		/// </summary>
		public BranchesSection()
		{
			Title = "Branches";
			IsVisible = true;
			IsExpanded = true;
			IsBusy = false;
			SectionContent = new BranchesView();
			View.ParentSection = this; 
		}

		/// <summary>
		/// Get the view.
		/// </summary>
		protected BranchesView View
		{
			get { return SectionContent as BranchesView; }
		}
	}
}