using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;

namespace Sample.ViewModels
{
    public class CollectionViewTestViewModel
    {
        public ObservableCollection<PhotoItem> ItemsSource { get; set; }
        //public ObservableCollection<PhotoGroup> ItemsSource { get; set; }

        public CollectionViewTestViewModel()
        {
            //ItemsSource = new ObservableCollection<PhotoGroup>();
            ItemsSource = new ObservableCollection<PhotoItem>();

            var list1 = new List<PhotoItem>();
            for (var i = 0; i < 20; i++) {
                ItemsSource.Add(new PhotoItem {
                    PhotoUrl = $"https://kamusoft.jp/openimage/nativecell/{i + 1}.jpg",
                    Title = $"Title {i + 1}",
                    Category = "AAA",
                });
            }
            var list2 = new List<PhotoItem>();
            for (var i = 0; i < 20; i++) {
                list2.Add(new PhotoItem {
                    PhotoUrl = $"https://kamusoft.jp/openimage/nativecell/{i + 1}.jpg",
                    Title = $"Title {i + 1}",
                    Category = "BBB",
                });
            }

            //ItemsSource.Add(new PhotoGroup(list1) { Head = "AAA" });
            //ItemsSource.Add(new PhotoGroup(list2) { Head = "BBB" });

        }

        public class PhotoGroup:ObservableCollection<PhotoItem>
        {
            public string Head { get; set; }
            public PhotoGroup(IEnumerable<PhotoItem> list):base(list){}
        }

        public class PhotoItem
        {
            public string PhotoUrl { get; set; }
            public string Title { get; set; }
            public string Category { get; set; }

        }
    }
}
