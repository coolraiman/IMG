using IMG.Models;
using IMG.SQLite;
using IMG.Wrappers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace IMG.Dialog
{
    /// <summary>
    
    /// </summary>
    public sealed partial class TagDialog : ContentDialog, INotifyPropertyChanged
    {
        private ObservableCollection<TagWrapper> existingTags;
        private ObservableCollection<TagWrapper> toDeleteTags;
        private TagWrapper selectedTag;

        private TagWrapper SelectedTag
        {
            get { return selectedTag; }
            set 
            {
                if(selectedTag != value) 
                {
                    selectedTag = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            if (PropertyChanged != null)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public TagDialog()
        {
            
            // do not include protected tags in the tag manager
            existingTags = new ObservableCollection<TagWrapper>(
                SQLiteConnector.GetWrappedTags().Where(t => !t.Tag.ProtectedTag).OrderBy(x => x.Tag.Name));
            
            toDeleteTags = new ObservableCollection<TagWrapper>();
            this.InitializeComponent();
            ListViewToCreateKeep.ItemsSource = existingTags;

            ListViewToDelete.ItemsSource = toDeleteTags;
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            //SQLConnector still check if tag is protected on database side
            //unwrap tags
            List<Tag> deleteTags = toDeleteTags.Where(t => !t.Tag.ProtectedTag).Select(t => t.Tag).ToList();

            List<Tag> toCreateTags = existingTags.Where(t => t.IsNewTag || t.IsModified).Select(t => t.Tag).ToList();

            SQLiteConnector.CreateTags(toCreateTags);
            SQLiteConnector.DeleteTags(deleteTags);
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void toDeleteDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            TagWrapper tappedTag = ListViewToDelete.SelectedItem as TagWrapper;
            if (tappedTag == null)
                return;

            if (existingTags.IndexOf(tappedTag) >= 0)
                return;
            //if its false, something weird went on
            if (!toDeleteTags.Remove(tappedTag))
                return;

            existingTags.Add(tappedTag);
        }

        private void toCreateKeepDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            TagWrapper tappedTag = ListViewToCreateKeep.SelectedItem as TagWrapper;
            if (tappedTag == null)
                return;

            if (toDeleteTags.IndexOf(tappedTag) >= 0)
                return;
            //if its false, something weird went on
            if (!existingTags.Remove(tappedTag))
                return;

            toDeleteTags.Add(tappedTag);
        }

        private void ListViewCreateKeepSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            TagWrapper selectedTagWrapper = ListViewToCreateKeep.SelectedItem as TagWrapper;
            if (selectedTagWrapper == null)
            {
                TextBoxDescription.IsEnabled = false;
                return;
            }
            TextBoxDescription.IsEnabled = true;
            SelectedTag = selectedTagWrapper;
        }

        private void TextBoxCharacterReceived(UIElement sender, CharacterReceivedRoutedEventArgs args)
        {
            if(SelectedTag == null) 
                return;

            SelectedTag.IsModified = true;
        }

        private async void CreateTagTextBoxKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != Windows.System.VirtualKey.Enter)
                return;

            string text = CreateTagTextBox.Text;
            text.Trim();

            if (text.Length == 0)
                return;

            if(existingTags.FirstOrDefault(x => x.Tag.Name == text) != null)
            {
                MessageDialog md = new MessageDialog("Error, tag already exist");
                md.Commands.Add(new UICommand("OK", null));
                await md.ShowAsync();
                return;
            }

            TagWrapper tw = new TagWrapper(new Models.Tag(text), true);
            existingTags.Add(tw);
            existingTags.OrderBy(x => x.Tag.Name);

            ListViewToCreateKeep.SelectedItem = tw;
            CreateTagTextBox.Text = "";
        }

        private void SwapSelected_Click(object sender, RoutedEventArgs e)
        {
            //get selected items
            List<TagWrapper> leftSelected = ListViewToDelete.SelectedItems.Cast<TagWrapper>().ToList();
            List<TagWrapper> rightSelected = ListViewToCreateKeep.SelectedItems.Cast<TagWrapper>().ToList();
            //clear selections
            ListViewToDelete.SelectedItems.Clear();
            ListViewToCreateKeep.SelectedItems.Clear();
            //remove selected items from left list and add to right list
            foreach(TagWrapper tag in leftSelected) 
            {
                toDeleteTags.Remove(tag);
                existingTags.Add(tag);
            }
            //remove selected items from right list and add to left list
            foreach (TagWrapper tag in rightSelected)
            {
                existingTags.Remove(tag);
                toDeleteTags.Add(tag);
            }
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Only get results when it was a user typing,
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<string> result = existingTags.Select(o => o.Tag.Name).Where(x => x.Contains(sender.Text)).ToList();
                sender.ItemsSource = result;
            }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string cleanText = sender.Text.Trim();
            //is tag is not on existing list
            TagWrapper t = existingTags.FirstOrDefault(x => x.Tag.Name == cleanText);

            if (t == null)
                return;

            if (!existingTags.Remove(t))
                return;

            toDeleteTags.Add(t);

            sender.Text = "";
        }
    }
}
