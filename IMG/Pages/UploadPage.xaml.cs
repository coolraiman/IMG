using IMG.Models;
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
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI.Core;
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
    public sealed partial class UploadPage : Page
    {
        private ObservableCollection<ImageData> imageCol;
        //private ImageData fullScreenImage;
        private FullScreenImage fullScreenImage = new FullScreenImage();
        private int navIndex = -1;

        public event PropertyChangedEventHandler PropertyChanged;

        public UploadPage()
        {
            imageCol = new ObservableCollection<ImageData>();
            this.InitializeComponent();

            ImageGrid.ItemsSource = imageCol;
            DataContext = fullScreenImage;
            Window.Current.CoreWindow.CharacterReceived += CoreWindow_CharacterReceived;
            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;
        }

        private void onTagBoxEnter(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key != VirtualKey.Enter)
                return;

            fullScreenImage.Image.Tags.Add(new Tag(0, TagTextBox.Text));
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

                    var file = await StorageApplicationPermissions.FutureAccessList.GetFileAsync(((ImageData)(ImageGrid.SelectedItems[i])).FaToken);
                    // Ensure the stream is disposed once the image is loaded
                    using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
                    {
                        byte[] hashValue = crypt.ComputeHash(fileStream.AsStream());
                        string hexFile = BitConverter.ToString(hashValue);

                        try
                        {
                            StorageFolder subFolder = await imgsFolder.GetFolderAsync(hexFile.Substring(0, 2));
                            await file.CopyAsync(subFolder, hexFile + ((ImageData)(ImageGrid.SelectedItems[i])).Extension, NameCollisionOption.FailIfExists);
                            addedElements.Add((ImageData)ImageGrid.SelectedItems[i]);

                        }
                        catch (System.Exception ex)
                        {
                            //TODO add red border
                        }
                    }

                }
            }

            foreach(ImageData img in addedElements)
            {
                imageCol.Remove(img);
            }
            int rows = await SQLite.SQLiteConnector.addImages(addedElements);
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
                    //faTokens.Add(Windows.Storage.AccessCache.StorageApplicationPermissions.FutureAccessList.Add(file));
                    imageCol.Add(new ImageData()
                    {
                        Name = file.Name,
                        Hash = file.Path, //temp
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
    }

    internal enum Navigation
    {
        LEFT = -1,
        RIGHT = 1
    }
}
