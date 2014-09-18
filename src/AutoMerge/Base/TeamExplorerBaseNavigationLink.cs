using System;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge.Base
{
	/// <summary>
	/// Team Explorer base navigation link class.
	/// </summary>
	public abstract class TeamExplorerBaseNavigationLink : TeamExplorerBase, ITeamExplorerNavigationLink
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		protected TeamExplorerBaseNavigationLink(IServiceProvider serviceProvider)
		{
			ServiceProvider = serviceProvider;
		}

		#region ITeamExplorerNavigationLink

		/// <summary>
		/// Get/set the item text.
		/// </summary>
		public string Text
		{
			get
			{
				return _text;
			}
			set
			{
			    SetProperty(ref _text, value);
			}
		}
		private string _text;

		/// <summary>
		/// Get/set the IsEnabled flag.
		/// </summary>
		public bool IsEnabled
		{
			get
			{
				return _isEnabled;
			}
			set
			{
                SetProperty(ref _isEnabled, value);
			}
		}
		private bool _isEnabled = true;

		/// <summary>
		/// Get/set the IsVisible flag.
		/// </summary>
		public bool IsVisible
		{
			get
			{
				return _isVisible;
			}
			set
			{
                SetProperty(ref _isVisible, value);
			}
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

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
	}
}
