using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Notifications;

namespace IMG.Models
{
    public class Tag : BindableObject
    {
        private string name;
        private string description;

        public string Name 
        { 
            get { return name; } 
            set {if(value != name)
                {
                    name = value;
                    OnPropertyChanged();
                }
            } 
        }

        public string Description
        {
            get { return description; }
            set
            {
                if (value != description)
                {
                    description = value;
                    OnPropertyChanged();
                }
            }
        }

        public Tag(string Name, string Description = "")
        {
            this.Name = Name;
            this.Description = Description;
        }

    }
}
