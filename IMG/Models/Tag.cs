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
        private bool protectedTag;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Name"></param>
        /// <param name="Description"></param>
        /// <param name="protectedTag">do not set to true outside of SQLConnector, will do nothing</param>
        public Tag(string Name, string Description = "", bool protectedTag = false)
        {
            this.Name = Name;
            this.Description = Description;
            this.protectedTag = protectedTag;
        }

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

        public bool ProtectedTag
        {
            get { return protectedTag; }
        }

        

    }
}
