using AutoMerge.Prism.Events;

namespace AutoMerge.Events
{
	internal static class EventAggregatorFactory
	{
		private static readonly IEventAggregator _eventAggregator = new EventAggregator();

		public static IEventAggregator Get()
		{
			return _eventAggregator;
		}
	}
}
