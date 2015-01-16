using System.Windows.Input;
using Microsoft.TeamFoundation.Controls;

namespace AutoMerge
{
    internal class Notification
    {
        public string Message { get; set; }

        public NotificationType NotificationType { get; set; }

        public ICommand Command { get; set; }
    }
}
