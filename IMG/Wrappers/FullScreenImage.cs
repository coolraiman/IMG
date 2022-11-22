using IMG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IMG.Wrappers
{
    internal class FullScreenImage : BindableObject
    {
        ImageData image = new ImageData();

        public ImageData Image
        {
            get { return image; }
            set
            {
                if (value != image)
                {
                    image = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
