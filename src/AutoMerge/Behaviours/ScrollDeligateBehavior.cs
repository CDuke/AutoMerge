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
			AssociatedObject.PreviewMouseWheel += AssociatedObject_PreviewMouseWheel;
		}

		protected override void OnDetaching()
		{
			AssociatedObject.PreviewMouseWheel -= AssociatedObject_PreviewMouseWheel;
			base.OnDetaching();
		}

		private void AssociatedObject_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
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