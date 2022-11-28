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
        private ObservableCollection<ImageData> imageCol;
        //private ImageData fullScreenImage;
        private FullScreenImage fullScreenImage = new FullScreenImage();
        private int navIndex = -1;
        private List<Tag> tagsList;
        private ImageSearch search = new ImageSearch();

        public event PropertyChangedEventHandler PropertyChanged;
        public SearchPage()
        {
            imageCol = new ObservableCollection<ImageData>();

            this.InitializeComponent();

            ImageGrid.ItemsSource = imageCol;
            tagsListView.ItemsSource = fullScreenImage.Image.Tags;
            searchInclude.ItemsSource = search.Include;
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
            //imageCol.Clear();
            List<ImageData> imgs = await SQLiteConnector.searchImages(search.Include.Select(x => x.Name).ToList(), new List<string>());
            
            await loadSearchResultsThumbnails(imgs);
        }

        private async Task loadSearchResultsThumbnails(List<ImageData> imgs)
        {
            try
            {
                StorageFolder imgsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("imgs");
                for (int i = 0; i < imgs.Count; i++)
                {
                    imageCol.Add(imgs[i]);

                    StorageFolder subFolder = await imgsFolder.GetFolderAsync(imageCol[i].Hash.Substring(0, 2));
                    StorageFile file = await subFolder.GetFileAsync(imgs[i].Hash + imgs[i].Extension);
                    using (Windows.Storage.Streams.IRandomAccessStream fileStream =
                            await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        // Set the image source to the selected bitmap.
                        BitmapImage bitmapImage = new BitmapImage();
                        await bitmapImage.SetSourceAsync(fileStream);
                        bitmapImage.DecodePixelWidth = 150;

                        //find the ImageComponent
                        Image imgUI = (Image)FindChild(ImageGrid.ContainerFromIndex(ImageGrid.Items.Count - 1), typeof(Image));
                        //safety check
                        if (imgUI != null)
                            imgUI.Source = bitmapImage;
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
            if (search.Include.Where(x => x.Name == cleanText).Count() > 0)
                return;

            if (SQLiteConnector.TagExist(cleanText))
            {
                search.Include.Add(new Tag(cleanText));
                sender.Text = "";
                return;
            }
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double height = Window.Current.Bounds.Height - topUI.Height;
            if (height < 0)
                return;
            //ImageGrid.Height = height;
            //FullScreenPanel.Height = height;
        }

        private void ImageGridSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            var panel = (ItemsWrapGrid)ImageGrid.ItemsPanelRoot;
            panel.MaximumRowsOrColumns = (int)e.NewSize.Width / 120;

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
            if (isFullscreenMode() || !enterFullScreenMode())
                return;

            await loadFullscreenImage(ImageGrid.SelectedIndex);

            Page_SizeChanged(null, null);
        }

        private async Task loadFullscreenImage(int index)
        {
            fullScreenImage.Image = imageCol[index];
            var file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(imageCol[index].FaToken);
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
            else if (imageCol.Count == 1)
            {
                return;
            }
            else if (navIndex + (int)navigation < 0)
            {
                navIndex = imageCol.Count - 1;
            }
            else if (navIndex + (int)navigation >= imageCol.Count)
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
            FullScreenPanel.Visibility = Visibility.Visible;
            return true;
        }

        private void exitFullScreenMode()
        {
            ImageGrid.Visibility = Visibility.Visible;
            FullScreenPanel.Visibility = Visibility.Collapsed;
            ImageGrid.SelectedIndex = navIndex;
        }

        private bool isFullscreenMode()
        {
            if (ImageGrid.Visibility == Visibility.Visible)
                return false;
            return true;
        }
    }
}
