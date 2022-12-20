using IMG.Models;
using IMG.SQLite;
using IMG.Wrappers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace IMG.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SearchPage : Page
    {

        private ObservableCollection<ImageData> ImageCol;
        //private ImageData fullScreenImage;
        private FullScreenImage fullScreenImage = new FullScreenImage();
        private int navIndex = -1;
        private List<Tag> tagsList;
        private ImageSearch search = new ImageSearch();
        private bool fullscreenMode = false;

        public event PropertyChangedEventHandler PropertyChanged;
        public SearchPage()
        {
            ImageCol = new ObservableCollection<ImageData>();

            this.InitializeComponent();

            ImageGrid.ItemsSource = ImageCol;
            tagsListView.ItemsSource = fullScreenImage.Image.Tags;
            searchInclude.ItemsSource = search.Include;
            searchExclude.ItemsSource = search.Exclude;
            //DataContext = fullScreenImage;
            Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        //async task to do when page is loaded
        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            tagsList = SQLiteConnector.GetTags();
        }

        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Only get results when it was a user typing,
            // otherwise assume the value got filled in by TextMemberPath
            // or the handler for SuggestionChosen.
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<string> result = tagsList.Select(o => o.Name).Where(x => x.StartsWith(sender.Text)).ToList();
                sender.ItemsSource = result;
            }
        }
        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.
            //give focus back to box after selecting
            //sender.Focus(FocusState.Programmatic);
        }

        private async void OnClickSearch(object sender, RoutedEventArgs e)
        {
            ImageCol.Clear();
            List<ImageData> imgs = await SQLiteConnector.searchImages(search.Include.Select(x => x.Name).ToList(), search.Exclude.Select(x => x.Name).ToList());
            
            await loadSearchResultsThumbnails(imgs);
        }

        private async Task loadSearchResultsThumbnails(List<ImageData> imgs)
        {
            try
            {
                StorageFolder imgsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("imgs");
                for (int i = 0; i < imgs.Count; i++)
                {
                    

                    StorageFolder subFolder = await imgsFolder.GetFolderAsync(imgs[i].Hash.Substring(0, 2));
                    StorageFile file = await subFolder.GetFileAsync(imgs[i].Hash + imgs[i].Extension);
                    using (Windows.Storage.Streams.IRandomAccessStream fileStream =
                            await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        // Set the image source to the selected bitmap.
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(fileStream);
                        bitmapImage.DecodePixelWidth = 150;
                        imgs[i].BitmapImage = bitmapImage;
                        ImageCol.Add(imgs[i]);

                    }
                }
            } catch ( Exception e )
            {
                string msg = e.Message;
            }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string cleanText = sender.Text.Trim();
            //double checking
            if (fullScreenImage.Image.Tags.Where(x => x.Name == cleanText).Count() > 0)
                return;

            if (SQLiteConnector.TagExist(cleanText))
            {
                fullScreenImage.Image.Tags.Add(new Tag(cleanText));
                sender.Text = "";
                return;
            }
        }

        private void AutoSuggestBoxInclude_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string cleanText = sender.Text.Trim();
            //double checking
            if (search.Include.Where(x => x.Name == cleanText).Count() > 0 || search.Exclude.Where(x => x.Name == cleanText).Count() > 0)
                return;

            if (SQLiteConnector.TagExist(cleanText))
            {
                search.Include.Add(new Tag(cleanText));
                sender.Text = "";
                return;
            }
        }

        private void AutoSuggestBoxExclude_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string cleanText = sender.Text.Trim();
            //double checking
            if (search.Include.Where(x => x.Name == cleanText).Count() > 0 || search.Exclude.Where(x => x.Name == cleanText).Count() > 0)
                return;

            if (SQLiteConnector.TagExist(cleanText))
            {
                search.Exclude.Add(new Tag(cleanText));
                sender.Text = "";
                return;
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double height = Window.Current.Bounds.Height - topUI.Height;
            if (height < 0)
                return;
            ImageGrid.Height = height;
            FullScreenPanel.Height = height;

            if (!fullscreenMode)
            {
                var panel = (ItemsWrapGrid)ImageGrid.ItemsPanelRoot;
                panel.MaximumRowsOrColumns = (int)(e.NewSize.Width - searchPanel.ActualWidth) / 110;
            }
            else
            {
                FullscreenImage_UI.Width = Window.Current.Bounds.Width - searchPanel.ActualWidth;
                FullscreenImage_UI.Height = Window.Current.Bounds.Height - topUI.ActualHeight;
            }
        }

        private void showFlyout(object sender, RightTappedRoutedEventArgs e)
        {
            if (ImageGrid.SelectedItems.Count == 0)
            {
                return;
            }

        }

        private void tagViewerRightTap(object sender, RightTappedRoutedEventArgs e)
        {
            if (tagsListView.SelectedIndex < 0)
                return;

            fullScreenImage.Image.Tags.Remove((Tag)tagsListView.SelectedItem);
        }


        public DependencyObject FindChild(DependencyObject o, Type childType)
        {
            DependencyObject foundChild = null;
            if (o != null)
            {
                int childrenCount = VisualTreeHelper.GetChildrenCount(o);
                for (int i = 0; i < childrenCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(o, i);
                    if (child.GetType() != childType)
                    {
                        foundChild = FindChild(child, childType);
                    }
                    else
                    {
                        foundChild = child;
                        break;
                    }
                }
            }
            return foundChild;
        }

        private async void ImageGrid_DoubleTap(object sender, DoubleTappedRoutedEventArgs e)
        {
            if (fullscreenMode || !enterFullScreenMode())
                return;

            await loadFullscreenImage(ImageGrid.SelectedIndex);

            Page_SizeChanged(null, null);
        }

        private void searchIncludeDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            search.Include.Remove((Tag)listView.SelectedItem);
        }

        private void searchExcludeDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            ListView listView = (ListView)sender;
            search.Exclude.Remove((Tag)listView.SelectedItem);
        }

        private async Task loadFullscreenImage(int index)
        {
            fullScreenImage.Image = ImageCol[index];
            StorageFolder imgsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("imgs");
            StorageFolder subFolder = await imgsFolder.GetFolderAsync(ImageCol[index].Hash.Substring(0, 2));
            StorageFile file = await subFolder.GetFileAsync(ImageCol[index].Hash + ImageCol[index].Extension);
            // Ensure the stream is disposed once the image is loaded
            using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            {
                // Set the image source to the selected bitmap
                BitmapImage bitmapImage = new BitmapImage();
                // Decode pixel sizes are optional

                await bitmapImage.SetSourceAsync(fileStream);
                FullscreenImage_UI.Source = bitmapImage;
            }
            tagsListView.ItemsSource = fullScreenImage.Image.Tags;
        }

        private void CoreWindow_CharacterReceived(CoreWindow sender, CharacterReceivedEventArgs args)
        {
            if (FullScreenPanel.Visibility == Visibility.Visible)
            {
                if (args.KeyCode == (int)VirtualKey.Escape) //ESC
                {
                    exitFullScreenMode();
                }
            }
        }
        private async void CoreWindow_KeyDown(CoreWindow sender, KeyEventArgs args)
        {
            if (FullScreenPanel.Visibility == Visibility.Visible)
            {
                if (args.VirtualKey == VirtualKey.Left)
                {
                    await NavigateFullScreen(Navigation.LEFT);
                }
                else if (args.VirtualKey == VirtualKey.Right)
                {
                    await NavigateFullScreen(Navigation.RIGHT);
                }
            }
        }

        private async Task NavigateFullScreen(Navigation navigation)
        {
            if (navIndex == -1)
            {
                return;
            }
            else if (ImageCol.Count == 1)
            {
                return;
            }
            else if (navIndex + (int)navigation < 0)
            {
                navIndex = ImageCol.Count - 1;
            }
            else if (navIndex + (int)navigation >= ImageCol.Count)
            {
                navIndex = 0;
            }
            else
            {
                navIndex += (int)navigation;
            }

            await loadFullscreenImage(navIndex);
            ImageViewScroller.ChangeView(0, 0, 1);//reset zoom and drag
        }

        private bool enterFullScreenMode()
        {
            if (ImageGrid.SelectedIndex == -1)
            {
                return false;
            }

            navIndex = ImageGrid.SelectedIndex;
            ImageGrid.Visibility = Visibility.Collapsed;
            searchPanel.Visibility = Visibility.Collapsed;
            FullScreenPanel.Visibility = Visibility.Visible;
            fullscreenMode = true;
            return true;
        }

        private void exitFullScreenMode()
        {
            ImageGrid.Visibility = Visibility.Visible;
            searchPanel.Visibility = Visibility.Visible;
            FullScreenPanel.Visibility = Visibility.Collapsed;
            ImageGrid.SelectedIndex = navIndex;
            fullscreenMode = false;
        }



        public void OnClickUpload(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(UploadPage));
        }

        private void sortParamChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ImageCol == null || ImageCol.Count == 0)
                return;

            int selectedParam = comboBoxSortBy.SelectedIndex;
            List<ImageData> temp;
            switch (selectedParam) 
            {
                case 0:
                    temp = ImageCol.OrderBy(x => x.Hash).ToList();
                    break;
                case 1:
                    temp = ImageCol.OrderBy(x => x.Name).ToList();
                    break;
                case 2:
                    temp = ImageCol.OrderBy(x => x.Size).ToList();
                    break;
                case 3:
                    temp = ImageCol.OrderBy(x => x.Tags.Count).ToList();
                    break;
                default:
                    temp = new List<ImageData>();
                    break;
            }

            ImageCol.Clear();
            if(comboBoxOrderBy.SelectedIndex != 1)
            {
                foreach (ImageData img in temp)
                {
                    ImageCol.Add(img);
                }
            }
            else
            {
                for(int i = temp.Count - 1; i >= 0; i--) 
                {
                    ImageCol.Add(temp[i]);
                }
            }
            
        }
    }
}
