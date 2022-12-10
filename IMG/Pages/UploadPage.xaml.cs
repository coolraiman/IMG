using IMG.Models;
using IMG.SQLite;
using IMG.Utility;
using IMG.Wrappers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.FileProperties;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Automation.Peers;
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
    public sealed partial class UploadPage : Page
    {
        private ObservableCollection<ImageData> imageCol;
        //private ImageData fullScreenImage;
        private FullScreenImage fullScreenImage = new FullScreenImage();
        private int navIndex = -1;
        private List<Tag> tagsList;
        private List<ImageData> duplicates;

        public event PropertyChangedEventHandler PropertyChanged;

        public UploadPage()
        {
            imageCol = new ObservableCollection<ImageData>();
            duplicates = new List<ImageData>();
            this.InitializeComponent();

            ImageGrid.ItemsSource = imageCol;
            tagsListView.ItemsSource = fullScreenImage.Image.Tags;
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

        private async void TagMessageHandler(IUICommand command)
        {
            if (command.Label == "Create Tag")
            {
                Tag t = new Tag(AutoSuggestTag.Text.TrimEnd());
                if (SQLiteConnector.CreateTag(t))
                {
                    fullScreenImage.Image.Tags.Add(t);
                    AutoSuggestTag.Text = "";
                }
            }
            else if(command.Label == "Create and add description")
            {
                var dialog = new Dialog.TextInputContentDialog(AutoSuggestTag.Text);
                var result = await dialog.ShowAsync();
                if(result == ContentDialogResult.Primary)
                {
                    Tag t = new Tag(AutoSuggestTag.Text.TrimEnd(), dialog.Text.TrimEnd());
                    if(SQLiteConnector.CreateTag(t))
                    {
                        fullScreenImage.Image.Tags.Add(t);
                        AutoSuggestTag.Text = "";
                    }
                }
            }
        }

        private void showFlyout(object sender, RightTappedRoutedEventArgs e)
        {
            if (ImageGrid.SelectedItems.Count == 0)
            {
                return;
            }

            MenuFlyout gridFlyout = new MenuFlyout();
            MenuFlyoutItem removeFlyout = new MenuFlyoutItem { Text = "Remove" };
            removeFlyout.Click += removeSelected;

            MenuFlyoutItem addFlyout = new MenuFlyoutItem { Text = "Add to databse" };
            addFlyout.Click += flyOutAddSelected;

            gridFlyout.Items.Add(removeFlyout);
            gridFlyout.Items.Add(addFlyout);

            FrameworkElement senderElement = sender as FrameworkElement;

            gridFlyout.ShowAt(senderElement, e.GetPosition(sender as UIElement));

        }

        private void tagViewerRightTap(object sender, RightTappedRoutedEventArgs e)
        {
            if (tagsListView.SelectedIndex < 0)
                return;

            fullScreenImage.Image.Tags.Remove((Tag)tagsListView.SelectedItem);
        }

        private void removeSelected(object sender, RoutedEventArgs e)
        {
            //TODO add confirm prompt and more error catch
            while (ImageGrid.SelectedItems.Count > 0)
                imageCol.Remove((ImageData)ImageGrid.SelectedItems[0]);
        }

        private async void flyOutAddSelected(object sender, RoutedEventArgs e)
        {
            //TODO add confirm prompt and more error catch
            List<ImageData> addedElements = new List<ImageData>();
            StorageFolder imgsFolder = await ApplicationData.Current.LocalFolder.GetFolderAsync("imgs");
            using (SHA256 crypt = SHA256.Create())
            {
                for (int i = 0; i < ImageGrid.SelectedItems.Count; i++)
                {
                    ImageData img = ((ImageData)ImageGrid.SelectedItems[i]);
                    if (img.Duplicate)
                        continue;
                    else if (SQLiteConnector.findDuplicateFromHash(img.Hash))//duplicate hash
                    {
                        img.Duplicate = true;
                        continue;
                    }
                    var file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(((ImageData)(ImageGrid.SelectedItems[i])).FaToken);
                    // Ensure the stream is disposed once the image is loaded
                    using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {

                        StorageFolder subFolder = await imgsFolder.GetFolderAsync(img.Hash.Substring(0, 2));
                        
                        await file.CopyAsync(subFolder, img.Hash + img.Extension, NameCollisionOption.ReplaceExisting);
                        addedElements.Add(img);
                    }
                }
            }

            foreach(ImageData img in addedElements)
            {
                imageCol.Remove(img);
            }
            int rows = SQLiteConnector.addImages(addedElements);
            rows++;
        }

        private void ImageGridSizeChanged(object sender, Windows.UI.Xaml.SizeChangedEventArgs e)
        {
            var panel = (ItemsWrapGrid)ImageGrid.ItemsPanelRoot;
            panel.MaximumRowsOrColumns = (int)e.NewSize.Width / 120;
            
        }

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            double height = Window.Current.Bounds.Height - topUI.Height;
            if (height < 0)
                return;
            ImageGrid.Height = height;
            FullScreenPanel.Height = height;
        }

        private async void GetPhoto(object sender, RoutedEventArgs e)
        {
            // Set up the file picker.
            Windows.Storage.Pickers.FileOpenPicker openPicker =
                new Windows.Storage.Pickers.FileOpenPicker();
            openPicker.SuggestedStartLocation =
                Windows.Storage.Pickers.PickerLocationId.PicturesLibrary;
            openPicker.ViewMode =
                Windows.Storage.Pickers.PickerViewMode.Thumbnail;

            // Filter to include a sample subset of file types.
            openPicker.FileTypeFilter.Clear();
            openPicker.FileTypeFilter.Add(".bmp");
            openPicker.FileTypeFilter.Add(".png");
            openPicker.FileTypeFilter.Add(".jpeg");
            openPicker.FileTypeFilter.Add(".jpg");

            // Open the file picker.
            IReadOnlyList<Windows.Storage.StorageFile> files =
                await openPicker.PickMultipleFilesAsync();

            // 'file' is null if user cancels the file picker.
            if (files != null)
            {
                
                foreach (Windows.Storage.StorageFile file in files)
                {
                    //prevent duplicates
                    if (isDuplicateFile(file.Path))
                        continue;
                    //list of access tokens
                    BasicProperties fileProp = await file.GetBasicPropertiesAsync();

                    imageCol.Add(new ImageData()
                    {
                        Name = file.Name,
                        Hash = "",
                        Size = fileProp.Size,
                        Extension = file.FileType,
                        
                        FaToken = StorageApplicationPermissions.FutureAccessList.Add(file)
                    }) ;

                    // Open a stream for the selected files.
                    // The 'using' block ensures the stream is disposed
                    // after the image is loaded.

                    using (Windows.Storage.Streams.IRandomAccessStream fileStream =
                        await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        // Set the image source to the selected bitmap.
                        BitmapImage bitmapImage = new BitmapImage();
                        //file.Path = "D:\\Yuru Yuri\\0f60c26056fb2288b244fdd35d3d9a94.jpg"
                        await bitmapImage.SetSourceAsync(fileStream);

                        bitmapImage.DecodePixelWidth = 150;

                        //find the ImageComponent
                        Image img = (Image)FindChild(ImageGrid.ContainerFromIndex(ImageGrid.Items.Count - 1), typeof(Image));
                        //safety check
                        if (img != null)
                            img.Source = bitmapImage;
                    }
                }
            }

            await generateImageHash();
        }

        public void ScanDuplicate(object sender, RoutedEventArgs e)
        {
            duplicates = SQLiteConnector.findDuplicateFromList(imageCol.ToList());
            foreach(ImageData dup in duplicates)
            {
                dup.Duplicate = true;
            }
        }

        private async Task generateImageHash()
        {
            foreach (ImageData img in imageCol)
            {
                using (SHA256 crypt = SHA256.Create())
                {
                    var file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(img.FaToken);
                    // Ensure the stream is disposed once the image is loaded
                    using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        byte[] hashValue = crypt.ComputeHash(fileStream.AsStream());
                        string hexFile = BitConverter.ToString(hashValue);
                        img.Hash = hexFile;
                    }
                }
            }
        }

        public void RemoveDuplicate(object sender, RoutedEventArgs e)
        {
            foreach(ImageData img in duplicates)
            {
                imageCol.Remove(img);
            }
            duplicates = new List<ImageData>();
        }

        public void ClickSearch(object sender, RoutedEventArgs e)
        {
            this.Frame.Navigate(typeof(SearchPage));
        }

        public bool isDuplicateFile(string file)
        {
            for(int i = 0; i < imageCol.Count; i++)
            {
                if (file == imageCol[i].Hash)
                    return true;
            }
            return false;
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
        //1 or -1
        private async Task NavigateFullScreen(Navigation navigation)
        {
            if(navIndex == -1)
            {
                return;
            }
            else if(imageCol.Count == 1)
            {
                return;
            }
            else if(navIndex + (int)navigation < 0)
            {
                navIndex = imageCol.Count - 1;
            }
            else if(navIndex + (int)navigation >= imageCol.Count)
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
            if(ImageGrid.SelectedIndex == -1) 
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
            if(ImageGrid.Visibility == Visibility.Visible)
                return false;
            return true;
        }

        private async void MenuFlyoutDelete_Click(object sender, RoutedEventArgs e)
        {
            await FolderUtility.deleteAllImages();
            SQLiteConnector.initDatabase();
            this.Frame.Navigate(typeof(UploadPage));
        }

        private void MenuFlyoutExit_Click(object sender, RoutedEventArgs e)
        {
            CoreApplication.Exit();
        }
    }

    internal enum Navigation
    {
        LEFT = -1,
        RIGHT = 1
    }
}
