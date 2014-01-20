using System;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge.Base 
{ 
	/// <summary>
	/// Team Explorer base navigation item class.
	/// </summary>
	public abstract class TeamExplorerBaseNavigationItem : TeamExplorerBase, ITeamExplorerNavigationItem
	{ 
		/// <summary>
		/// Constructor.
		/// </summary>
		protected TeamExplorerBaseNavigationItem(IServiceProvider serviceProvider)
		{
			ServiceProvider = serviceProvider;
		}

		#region ITeamExplorerNavigationItem

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
				_text = value;
				RaisePropertyChanged(() => Text);
			}
		}
		private string _text; 
 
		/// <summary>
		/// Get/set the item image.
		/// </summary>
		public System.Drawing.Image Image
		{
			get
			{
				return _image;
			}
			set
			{
				_image = value; RaisePropertyChanged(() => Image);
			}
		}
		private System.Drawing.Image _image; 
 
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
				_isVisible = value;
				RaisePropertyChanged(() => IsVisible);
			}
		}
		private bool _isVisible = true; 
 
		/// <summary> 
		/// Invalidate the item state. 
		/// </summary> 
		public virtual void Invalidate() 
		{ 
		} 
 
		/// <summary> 
		/// Execute the item action. 
		/// </summary> 
		public virtual void Execute() 
		{
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
		}
		#endregion 
	} 
} 