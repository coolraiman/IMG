using IMG.Dialog;
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
    /// The page to upload new images
    /// </summary>
    public sealed partial class UploadPage : Page
    {
        //image collection for the image gallery
        private ObservableCollection<ImageData> imageCol;
        
        private FullScreenImage fullScreenImage = new FullScreenImage();
        //fullscreen navigation index
        private int navIndex = -1;
        private List<Tag> tagsList;
        private List<ImageData> duplicates;
        private bool fullscreenMode = false;

        public event PropertyChangedEventHandler PropertyChanged;

        public UploadPage()
        {
            imageCol = new ObservableCollection<ImageData>();
            duplicates = new List<ImageData>();
            this.InitializeComponent();

            ImageGrid.ItemsSource = imageCol;
            tagsListView.ItemsSource = fullScreenImage.Image.Tags;
            //escape event
            Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
            //right and left arrow event
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
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                List<string> result = tagsList.Select(o => o.Name).Where(x => x.Contains(sender.Text)).ToList();
                sender.ItemsSource = result;
            }
        }
        private void AutoSuggestBox_SuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
        {
            // Set sender.Text. You can use args.SelectedItem to build your text string.
            //give focus back to box after selecting
            //sender.Focus(FocusState.Programmatic);
        }


        private async void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            string cleanText = sender.Text.Trim();
            //is tag already in image
            if (fullScreenImage.Image.Tags.Where(x => x.Name == cleanText).Count() > 0)
                return;
            //find the tag in the database
            if (SQLiteConnector.TagExist(cleanText))
            {//TODO swap to return tag from DB for data integrity
                fullScreenImage.Image.Tags.Add(new Tag(cleanText));
                sender.Text = "";
                return;
            }
            //build content dialog
            MessageDialog dialog = new MessageDialog("The tag does not exist, do you want to create it?");
            dialog.Commands.Add(new UICommand("yes"));
            dialog.Commands.Add(new UICommand("yes, add description"));
            dialog.Commands.Add(new UICommand("no"));
            dialog.DefaultCommandIndex = 0;
            dialog.CancelCommandIndex = 2;

            UICommand answer = (UICommand)await dialog.ShowAsync();
            await TagMessageHandler(answer);
        }
        //handle the answer of the tag creation dialog
        private async Task TagMessageHandler(IUICommand command)
        {
            if (command.Label == "yes")
            {
                Tag t = new Tag(AutoSuggestTag.Text.Trim());
                if (SQLiteConnector.CreateTag(t))
                {
                    fullScreenImage.Image.Tags.Add(t);
                    AutoSuggestTag.Text = "";
                    tagsList = SQLiteConnector.GetTags();
                }
            }
            else if(command.Label == "yes, add description")
            {
                var dialog = new Dialog.TextInputContentDialog(AutoSuggestTag.Text);
                var result = await dialog.ShowAsync();
                if(result == ContentDialogResult.Primary)
                {
                    Tag t = new Tag(AutoSuggestTag.Text.Trim(), dialog.Text.Trim());
                    if(SQLiteConnector.CreateTag(t))
                    {
                        fullScreenImage.Image.Tags.Add(t);
                        AutoSuggestTag.Text = "";
                        tagsList = SQLiteConnector.GetTags();
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
                panel.MaximumRowsOrColumns = (int)e.NewSize.Width / 110;
            }
            else
            {
                FullscreenImage_UI.Width = Window.Current.Bounds.Width - tagPanel.ActualWidth;
                FullscreenImage_UI.Height = Window.Current.Bounds.Height - topUI.ActualHeight;
            }
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
            openPicker.FileTypeFilter.Add(".gif");

            // Open the file picker.
            IReadOnlyList<Windows.Storage.StorageFile> files =
                await openPicker.PickMultipleFilesAsync();

            // 'file' is null if user cancels the file picker.
            if (files != null)
            {
                
                foreach (Windows.Storage.StorageFile file in files)
                {
                    string faToken = StorageApplicationPermissions.FutureAccessList.Add(file);
                    //prevent duplicates
                    if (isDuplicateFile(faToken))
                        continue;
                    //list of access tokens
                    BasicProperties fileProp = await file.GetBasicPropertiesAsync();
                    ImageProperties imgProps = await file.Properties.GetImagePropertiesAsync();
                    
                    imgProps.PeopleNames.ToList();
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

                        bitmapImage.DecodePixelWidth = 80;
                        imageCol.Add(new ImageData()
                        {
                            Name = file.Name,
                            Hash = "",
                            Size = fileProp.Size,
                            Extension = file.FileType,
                            Rating = 0,
                            Favorite = false,
                            Views = 0,
                            DateAdded = DateTime.Now,
                            DateTaken = imgProps.DateTaken.DateTime,
                            Height = imgProps.Height,
                            Width = imgProps.Width,
                            Orientation = imgProps.Orientation,
                            CameraManufacturer = imgProps.CameraManufacturer,
                            CameraModel = imgProps.CameraModel,
                            Latitude = imgProps.Latitude,
                            Longitude = imgProps.Longitude,
                            tempTags = new ObservableCollection<string>(imgProps.PeopleNames),
                            FaToken = faToken,
                            BitmapImage = bitmapImage
                        });
                        if (bitmapImage.IsAnimatedBitmap)
                        {
                            imageCol[imageCol.Count - 1].Tags.Add(tagsList[1]);
                            bitmapImage.AutoPlay = false;
                            bitmapImage.Stop();
                        }
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

        public bool isDuplicateFile(string token)
        {
            for(int i = 0; i < imageCol.Count; i++)
            {
                if (token == imageCol[i].FaToken)
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
            if (fullscreenMode || !enterFullScreenMode())
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
            else if(imageCol.Count == 0)
            {
                exitFullScreenMode();
                return;
            }
            else if(imageCol.Count == 1)
            {
                navigation = Navigation.NONE;
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
            topUI.Visibility = Visibility.Collapsed;
            FullScreenPanel.Visibility = Visibility.Visible;
            fullscreenMode = true;
            return true;
        }

        private void exitFullScreenMode() 
        {
            ImageGrid.Visibility = Visibility.Visible;
            topUI.Visibility = Visibility.Visible;
            FullScreenPanel.Visibility = Visibility.Collapsed;
            ImageGrid.SelectedIndex = navIndex;
            fullscreenMode = false;
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

        private void ConvertMetaDataToTag(object sender, RoutedEventArgs e)
        {
            foreach(ImageData img in imageCol)
            {
                foreach(string s in img.tempTags)
                {
                    int index = tagsList.FindIndex(t => t.Name == s);
                    if (index >= 0)
                    {
                        img.Tags.Add(tagsList[index]);
                    }
                    else
                    {
                        Tag t = new Models.Tag(s);
                        if(SQLiteConnector.CreateTag(t))
                        {
                            tagsList = SQLiteConnector.GetTags();
                        }
                    }
                }
            }
        }

        private async void DeleteFullscreenImage(object sender, RoutedEventArgs e)
        {
            imageCol.RemoveAt(navIndex);
            await NavigateFullScreen(Navigation.NONE);
        }

        private async void MenuFlyoutManageTags_Click(object sender, RoutedEventArgs e)
        {
            TagDialog dialog = new TagDialog();
            await dialog.ShowAsync();
        }
    }

    internal enum Navigation
    {
        LEFT = -1,
        RIGHT = 1,
        NONE = 0
    }
}
