using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace AutoMerge
{
    public class AutoMergeListBox : ListBox
    {
        public AutoMergeListBox()
        {
            SelectionChanged += AutoMergeListBox_SelectionChanged;
        }

        private void AutoMergeListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            foreach(var removedItem in e.RemovedItems.Cast<ChangesetViewModel>())
            {
                SelectedItemsList.Remove(removedItem);
            }

            foreach(var addItem in e.AddedItems.Cast<ChangesetViewModel>())
            {
                SelectedItemsList.Add(addItem);
            }
        }

        public ObservableCollection<ChangesetViewModel> SelectedItemsList
        {
            get { return (ObservableCollection<ChangesetViewModel>)GetValue(SelectedItemsListProperty); }
            set { SetValue(SelectedItemsListProperty, value); }
        }

        public static readonly DependencyProperty SelectedItemsListProperty =
           DependencyProperty.Register("SelectedItemsList", typeof(ObservableCollection<ChangesetViewModel>), typeof(AutoMergeListBox), new PropertyMetadata(null));
    }
}
