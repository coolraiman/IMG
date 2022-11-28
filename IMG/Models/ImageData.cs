using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;

namespace IMG.Models
{
    public class ImageData :BindableObject
    {
        public ImageData() 
        {
            Tags = new ObservableCollection<Tag>();
        }

        public string Hash { get; set; }//key
        public string Extension { get; set; }
        public long Size { get; set; }
        public string Name { get; set; }
        public byte Rating { get; set; }
        public bool Favorite { get; set; }
        public int Views { get; set; }
        public DateTime DateAdded { get; set; }
        public string MetaData { get; set; }
        public double Height { get; set; }
        public double Width { get; set; }
        public string FaToken { get; set; }
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
    }
}
