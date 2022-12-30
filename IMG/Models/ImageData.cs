using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage.FileProperties;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;

namespace IMG.Models
{
    public class ImageData : BindableObject
    {
        public ImageData()
        {
            Tags = new ObservableCollection<Tag>();
        }

        public string Hash { get; set; }//key
        public string Extension { get; set; }
        public ulong Size { get; set; }
        public string Name { get; set; }

        private byte rating;
        public byte Rating { 
            get { return rating; }
            set { 
                if(value != rating)
                {
                    rating = value;
                    OnPropertyChanged();
                }
            } }

        private bool favorite;
        public bool Favorite { 
            get { return favorite; } 
            set {
                if (value != favorite)
                {
                    favorite = value;
                    OnPropertyChanged();
                }
            } }
        public int Views { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateTaken { get; set; }
        public uint Height { get; set; }
        public uint Width { get; set; }
        public PhotoOrientation Orientation {get; set;} //ushort
        public string CameraManufacturer { get; set;}
        public string CameraModel { get; set;}
        public double? Latitude { get; set;}
        public double? Longitude { get; set;}
        public string FaToken { get; set; }
        public ObservableCollection<string> tempTags { get; set; }
        public ObservableCollection<Tag> Tags { get; set; }
        public string File
        {
            get { return Hash + "." + Extension; }

        }

        private bool duplicate = false;
        public bool Duplicate
        {
            get { return duplicate; }
            set 
            {
                if(value !=duplicate)
                    duplicate = value;
                OnPropertyChanged(); 
            }
        }

        private BitmapImage bitmapImage;
        public BitmapImage BitmapImage
        {
            get { return bitmapImage; }
            set
            {
                if (value != bitmapImage)
                {
                    bitmapImage = value;
                }
            }
        }
    }
}
