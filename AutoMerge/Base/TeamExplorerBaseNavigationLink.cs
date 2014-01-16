using System;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge.Base
{
	/// <summary> 
	/// Team Explorer base navigation link class. 
	/// </summary> 
	public class TeamExplorerBaseNavigationLink : TeamExplorerBase, ITeamExplorerNavigationLink
	{
		/// <summary> 
		/// Constructor. 
		/// </summary> 
		public TeamExplorerBaseNavigationLink(IServiceProvider serviceProvider)
		{
			ServiceProvider = serviceProvider;
		}

		#region ITeamExplorerNavigationLink

		/// <summary> 
		/// Get/set the item text. 
		/// </summary> 
		public string Text
		{
			get { return _text; }
			set { _text = value; RaisePropertyChanged("Text"); }
		}
		private string _text;

		/// <summary> 
		/// Get/set the IsEnabled flag. 
		/// </summary> 
		public bool IsEnabled
		{
			get { return _isEnabled; }
			set { _isEnabled = value; RaisePropertyChanged("IsEnabled"); }
		}
		private bool _isEnabled = true;

		/// <summary> 
		/// Get/set the IsVisible flag. 
		/// </summary> 
		public bool IsVisible
		{
			get { return _isVisible; }
			set { _isVisible = value; RaisePropertyChanged("IsVisible"); }
		}
		private bool _isVisible = true;

		/// <summary> 
		/// Invalidate the link state. 
		/// </summary> 
		public virtual void Invalidate()
		{
		}

		/// <summary> 
		/// Execute the link action. 
		/// </summary> 
		public virtual void Execute()
		{
		}

		#endregion
	}
}