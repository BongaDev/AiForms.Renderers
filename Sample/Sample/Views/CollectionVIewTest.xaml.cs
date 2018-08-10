using System;
using System.Collections.Generic;

using Xamarin.Forms;

namespace Sample.Views
{
    public partial class CollectionViewTest : ContentPage
    {
        public CollectionViewTest()
        {
            InitializeComponent();
        }

        void Handle_ItemTapped(object sender, Xamarin.Forms.ItemTappedEventArgs e)
        {
            DisplayAlert("", "Tapped!", "OK");
        }
    }
}
