using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// The Content Dialog item template is documented at https://go.microsoft.com/fwlink/?LinkId=234238

namespace IMG.Dialog
{
    /// <summary>
    /// Simple dialog to add a description to a tagg
    /// </summary>
    public sealed partial class TextInputContentDialog : ContentDialog
    {
        public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
            "Text", typeof(string), typeof(TextInputContentDialog), new PropertyMetadata(default(string)));

        private string title;

        public TextInputContentDialog(string title)
        {
            this.title = title;
            DataContext= this;
            this.InitializeComponent();
            
        }
        public string Text
        {
            get { return MainTextBox.Text; }
            set { MainTextBox.Text = value; }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }
    }
}
