using IMG.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.UI.Xaml.Media;
using Windows.UI;

namespace IMG.Wrappers
{
    public class TagWrapper : BindableObject
    {
        private Tag tag;

        public TagWrapper(Tag tag, bool isNewTag = false)
        { 
            this.tag = tag; 
            IsNewTag = isNewTag;
            isModified = false;
        }

        public Tag Tag
        {
            get { return tag; }
            set
            {
                if (value != tag)
                {
                    tag = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool isNewTag;

        public bool IsNewTag
        {
            get { return isNewTag; }
            set 
            {
                if (value != isNewTag)
                {
                    isNewTag = value;
                    OnPropertyChanged();
                    TagWrapperStatus = TagWrapperStatus.NEW;
                }
            }
        }

        private bool isModified;

        public bool IsModified
        {
            get { return isModified; }
            set
            {
                if (value != isModified)
                {
                    isModified = value;
                    OnPropertyChanged();
                    TagWrapperStatus = TagWrapperStatus.MODIFIED;
                }
            }
        }

        //can only go upward in int value
        private TagWrapperStatus status = TagWrapperStatus.NONE;

        public TagWrapperStatus TagWrapperStatus
        {
            get { return status; }
            set { 
                    if(value != status && (int)value > (int)status)
                    {
                    status = value;
                    OnPropertyChanged();
                    }
                }
        }

        public static Brush TagWrapperStatusToColor(TagWrapperStatus stat)
        {
            switch(stat)
            {
                case TagWrapperStatus.NONE:
                    return new SolidColorBrush(Colors.Transparent);
                case TagWrapperStatus.NEW:
                    return new SolidColorBrush(Colors.LightGreen);
                case TagWrapperStatus.MODIFIED:
                    return new SolidColorBrush(Colors.LightYellow);
            }
            return new SolidColorBrush(Colors.Transparent);
        }

    }

    public enum TagWrapperStatus
    {
        NONE = 0,
        NEW = 1,
        MODIFIED = 2,
    }
}
