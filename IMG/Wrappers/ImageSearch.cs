using IMG.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IMG.Wrappers
{
    public class ImageSearch : BindableObject
    {
        private ObservableCollection<Tag> include = new ObservableCollection<Tag>();
        private ObservableCollection<Tag> exclude = new ObservableCollection<Tag>();

        public ObservableCollection<Tag> Include
        {
            get { return include; }
            set 
            {
                if (include != value)
                {
                    include = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<Tag> Exclude
        {
            get { return exclude; }
            set
            {
                if (exclude != value)
                {
                    exclude = value;
                    OnPropertyChanged();
                }
            }
        }

    }
}
