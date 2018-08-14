using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using Reactive.Bindings;
using Prism.Services;
using System.Threading.Tasks;

namespace Sample.ViewModels
{
    public class CollectionViewTestViewModel
    {
        //public ObservableCollection<PhotoItem> ItemsSource { get; set; }
        public ObservableCollection<PhotoGroup> ItemsSource { get; set; }
        public ReactiveCommand TapCommand { get; } = new ReactiveCommand();
        public ReactiveCommand LongTapCommand { get; } = new ReactiveCommand();
        public ReactiveProperty<bool> IsRefreshing { get; } = new ReactiveProperty<bool>(false);
        public AsyncReactiveCommand RefreshCommand { get; } = new AsyncReactiveCommand();

        public CollectionViewTestViewModel(IPageDialogService pageDialog)
        {
            ItemsSource = new ObservableCollection<PhotoGroup>();
            //ItemsSource = new ObservableCollection<PhotoItem>();

            var list1 = new List<PhotoItem>();
            for (var i = 0; i < 20; i++) {
                list1.Add(new PhotoItem {
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

            ItemsSource.Add(new PhotoGroup(list1) { Head = "AAA" });
            ItemsSource.Add(new PhotoGroup(list2) { Head = "BBB" });


            TapCommand.Subscribe(async item => {
                var photo = item as PhotoItem;
                await pageDialog.DisplayAlertAsync("", $"Tap {photo.Title}", "OK");
            });

            LongTapCommand.Subscribe(async item => {
                var photo = item as PhotoItem;
                await pageDialog.DisplayAlertAsync("", $"LongTap {photo.Title}", "OK");
            });

            RefreshCommand.Subscribe(async _ => {
                await Task.Delay(3000);
                IsRefreshing.Value = false;
            });
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
