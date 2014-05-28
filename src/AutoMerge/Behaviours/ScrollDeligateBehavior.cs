using System.Windows;
using System.Windows.Input;
using System.Windows.Interactivity;


namespace AutoMerge.Behaviours
{
	public class ScrollDeligateBehavior : Behavior<FrameworkElement>
	{
		protected override void OnAttached()
		{
			base.OnAttached();
			AssociatedObject.PreviewMouseWheel += ScrollDeligatePreviewMouseWheel;
		}

		protected override void OnDetaching()
		{
			AssociatedObject.PreviewMouseWheel -= ScrollDeligatePreviewMouseWheel;
			base.OnDetaching();
		}

		private void ScrollDeligatePreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			if (!e.Handled)
			{
				var sourceElement = sender as FrameworkElement;
				if (sourceElement != null)
				{
					var uIElement = sourceElement.Parent as UIElement;
					if (uIElement != null)
					{
						e.Handled = true;
						uIElement.RaiseEvent(new MouseWheelEventArgs(e.MouseDevice, e.Timestamp, e.Delta)
						{
							RoutedEvent = UIElement.MouseWheelEvent,
							Source = this
						});
					}
				}
			}
		}
	}
}