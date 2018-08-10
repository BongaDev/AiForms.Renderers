using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using AiForms.Renderers;
using AiForms.Renderers.iOS.Cells;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.iOS;
using Specifics = Xamarin.Forms.PlatformConfiguration.iOSSpecific.ListView;
using System.Collections.Concurrent;
using CoreGraphics;

namespace AiForms.Renderers.iOS
{
    public class CollectionViewSource:UICollectionViewSource,IUICollectionViewDelegateFlowLayout
    {
        const int DefaultItemTemplateId = 1;
        GridCollectionView _collectionView;
        UICollectionView _uiCollectionView;
        IVisualElementRenderer _prototype;
        ITemplatedItemsView<Cell> TemplatedItemsView => _collectionView;
        Dictionary<DataTemplate, int> _templateToId = new Dictionary<DataTemplate, int>();
        ConcurrentDictionary<DataTemplate, int> _templateTypes = new ConcurrentDictionary<DataTemplate, int>();
        static int s_dataTemplateIncrementer = 2; // lets start at not 0 because

        public CollectionViewSource(CollectionView collectionView,UICollectionView uICollectionView)
        {
            _collectionView = (GridCollectionView)collectionView;
            _uiCollectionView = uICollectionView;
            Counts = new Dictionary<int, int>();
            _uiCollectionView.RegisterClassForCell(typeof(ViewCollectionCell), DefaultItemTemplateId.ToString());
        }

        public Dictionary<int, int> Counts { get; set; }

        public override nint NumberOfSections(UICollectionView collectionView)
        {
            if (_collectionView.IsGroupingEnabled)
                return TemplatedItemsView.TemplatedItems.Count;

            return 1;
        }

        public override nint GetItemsCount(UICollectionView collectionView, nint section)
        {
            int countOverride;
            if (Counts.TryGetValue((int)section, out countOverride))
            {
                Counts.Remove((int)section);
                return countOverride;
            }

            var templatedItems = _collectionView.TemplatedItems;
            if (_collectionView.IsGroupingEnabled)
            {
                var group = (IList)((IList)templatedItems)[(int)section];
                return group.Count;
            }

            return templatedItems.Count;
        }

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = collectionView.CellForItem(indexPath);

            if (cell == null)
                return;

            ContentCell formsCell = null;
            if ((_collectionView.CachingStrategy & ListViewCachingStrategy.RecycleElement) != 0)
                formsCell = (ContentCell)((INativeElementView)cell).Element;

           cell.BackgroundColor = UIColor.Clear;
                

            if (_collectionView.SelectionMode == ListViewSelectionMode.None)
                collectionView.DeselectItem(indexPath, false);

            //_selectionFromNative = true;

            //tableView.EndEditing(true);
            _collectionView.NotifyRowTapped(indexPath.Section, indexPath.Row, formsCell);
        }

        public override UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString elementKind, NSIndexPath indexPath)
        {
            if (!_collectionView.IsGroupingEnabled)
                return null;

            if(elementKind == "UICollectionElementKindSectionFooter")
            {
                return null;
            }

            //var cell = TemplatedItemsView.TemplatedItems[(int)indexPath.Section] as ContentCell;
            //ViewCollectionCell nativeCell;

            ContentCell cell;
            ViewCollectionCell nativeCell;

            Performance.Start(out string reference);

            var cachingStrategy = _collectionView.CachingStrategy;
            if (cachingStrategy == ListViewCachingStrategy.RetainElement) {
                cell = TemplatedItemsView.TemplatedItems[(int)indexPath.Section] as ContentCell;
                nativeCell = GetNativeHeaderCell(cell, indexPath);
            }
            else if ((cachingStrategy & ListViewCachingStrategy.RecycleElement) != 0) {

                var id = TemplateIdForPath(indexPath);

                nativeCell = collectionView.DequeueReusableSupplementaryView(UICollectionElementKindSection.Header,CollectionViewRenderer.SectionHeaderId, indexPath) as ViewCollectionCell;
                if (nativeCell.ContentCell == null) {
                    cell = TemplatedItemsView.TemplatedItems[(int)indexPath.Section] as ContentCell;

                    nativeCell = GetNativeHeaderCell(cell, indexPath);
                }
                else {
                    var templatedList = TemplatedItemsView.TemplatedItems.GetGroup(indexPath.Section);

                    cell = (ContentCell)((INativeElementView)nativeCell).Element;
                    cell.SendDisappearing();

                    templatedList.UpdateHeader(cell, indexPath.Section);
                    cell.SendAppearing();
                }
            }
            else {
                throw new NotSupportedException();
            }

            // TODO: いらんっぽい
            //var bgColor = collectionView.GetIndexPathsForSelectedItems() != null && collectionView.GetIndexPathsForSelectedItems().Equals(indexPath) ? UIColor.Clear : DefaultBackgroundColor;
            //SetCellBackgroundColor(nativeCell, bgColor);
            //PreserveActivityIndicatorState(cell);

            //nativeCell.ContentView.HeightAnchor.ConstraintEqualTo((System.nfloat)collectionView.Bounds.Width / 2.0f).Active = true;
            //nativeCell.ContentView.WidthAnchor.ConstraintEqualTo((System.nfloat)collectionView.Bounds.Width / 2.0f).Active = true;

            Performance.Stop(reference);
            return nativeCell;
        }

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            ContentCell cell;
            ViewCollectionCell nativeCell;

            Performance.Start(out string reference);

            var cachingStrategy = _collectionView.CachingStrategy;
            if (cachingStrategy == ListViewCachingStrategy.RetainElement)
            {
                cell = GetCellForPath(indexPath);
                nativeCell = GetNativeCell(cell,indexPath);
            }
            else if ((cachingStrategy & ListViewCachingStrategy.RecycleElement) != 0)
            {
                
                var id = TemplateIdForPath(indexPath);


                nativeCell = collectionView.DequeueReusableCell(id.ToString() ,indexPath) as ViewCollectionCell;
                if (nativeCell.ContentCell == null)
                {
                    cell = GetCellForPath(indexPath);

                    nativeCell = GetNativeCell(cell,indexPath, true, id.ToString());
                }
                else
                {
                    var templatedList = TemplatedItemsView.TemplatedItems.GetGroup(indexPath.Section);

                    cell = (ContentCell)((INativeElementView)nativeCell).Element;
                    cell.SendDisappearing();

                    templatedList.UpdateContent(cell, indexPath.Row);
                    cell.SendAppearing();
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            // TODO: いらんっぽい
            //var bgColor = collectionView.GetIndexPathsForSelectedItems() != null && collectionView.GetIndexPathsForSelectedItems().Equals(indexPath) ? UIColor.Clear : DefaultBackgroundColor;
            //SetCellBackgroundColor(nativeCell, bgColor);
            //PreserveActivityIndicatorState(cell);

            float itemWidth = 0;
            if (_collectionView.GridType == GridType.UniformGrid) {
                switch (UIApplication.SharedApplication.StatusBarOrientation) {
                    case UIInterfaceOrientation.Portrait:
                    case UIInterfaceOrientation.PortraitUpsideDown:
                    case UIInterfaceOrientation.Unknown:
                        itemWidth = (float)(collectionView.Frame.Width / (float)_collectionView.PortraitColumns);
                        break;
                    case UIInterfaceOrientation.LandscapeLeft:
                    case UIInterfaceOrientation.LandscapeRight:
                        itemWidth = (float)(collectionView.Frame.Width / (float)_collectionView.LandscapeColumns);
                        break;
                }
            }

            //nativeCell.LayoutMargins = new UIEdgeInsets(0, 0, 0, 0);
            //nativeCell.ContentView.HeightAnchor.ConstraintEqualTo(itemWidth).Active = true;
            //nativeCell.ContentView.WidthAnchor.ConstraintEqualTo(itemWidth).Active = true;

            Performance.Stop(reference);
            return nativeCell;
        }


        public CGSize CellSize { get; set; } 
            

        [Export("collectionView:layout:sizeForItemAtIndexPath:")]
        public CGSize GetSizeForItem(UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath)
        {
            //CGSize itemWidth;
            //if (_collectionView.GridType == GridType.UniformGrid) {
            //    switch (UIApplication.SharedApplication.StatusBarOrientation) {
            //        case UIInterfaceOrientation.Portrait:
            //        case UIInterfaceOrientation.PortraitUpsideDown:
            //        case UIInterfaceOrientation.Unknown:
            //            itemWidth =  (float)(collectionView.Frame.Width / (float)_collectionView.PortraitColumns);
            //            break;
            //        case UIInterfaceOrientation.LandscapeLeft:
            //        case UIInterfaceOrientation.LandscapeRight:
            //            itemWidth = (float)(collectionView.Frame.Width / (float)_collectionView.LandscapeColumns);
            //            break;
            //    }
            //}

            //return new CGSize(itemWidth, itemWidth);

            return CellSize;
        }


        ViewCollectionCell GetNativeHeaderCell(ContentCell cell, NSIndexPath indexPath)
        {
            var renderer = (ContentCellRenderer)Xamarin.Forms.Internals.Registrar.Registered.GetHandlerForObject<IRegisterable>(cell);

            var reusableCell = _uiCollectionView.DequeueReusableSupplementaryView(UICollectionElementKindSection.Header,CollectionViewRenderer.SectionHeaderId,indexPath) as ViewCollectionCell;

            var nativeCell = renderer.GetCell(cell, reusableCell, _uiCollectionView) as ViewCollectionCell;

            return nativeCell;
        }

        ViewCollectionCell GetNativeCell(ContentCell cell,NSIndexPath indexPath,bool recycleCells = false,string templateId = "")
        {
            var id = recycleCells ? templateId : cell.GetType().FullName;

            var renderer = (ContentCellRenderer)Xamarin.Forms.Internals.Registrar.Registered.GetHandlerForObject<IRegisterable>(cell);

            // UITableViewと違って初回でもインスタンスを返すので注意
            var reusableCell = _uiCollectionView.DequeueReusableCell(id,indexPath) as ViewCollectionCell;

            var nativeCell = renderer.GetCell(cell, reusableCell, _uiCollectionView ) as ViewCollectionCell;

            var cellWithContent = nativeCell;

            // Sometimes iOS for returns a dequeued cell whose Layer is hidden. 
            // This prevents it from showing up, so lets turn it back on!
            if (cellWithContent.Layer.Hidden)
                cellWithContent.Layer.Hidden = false;
            
            // Because the layer was hidden we need to layout the cell by hand
            if (cellWithContent != null)
                cellWithContent.LayoutSubviews();

            return nativeCell;
        }

        int TemplateIdForPath(NSIndexPath indexPath)
        {
            var itemTemplate = _collectionView.ItemTemplate;
            var selector = itemTemplate as DataTemplateSelector;
            if (selector == null)
                return DefaultItemTemplateId;

            var templatedList = GetTemplatedItemsListForPath(indexPath);
            var item = templatedList.ListProxy[indexPath.Row];

            itemTemplate = selector.SelectTemplate(item, _collectionView);
            int key;
            if (!_templateToId.TryGetValue(itemTemplate, out key))
            {
                s_dataTemplateIncrementer++;
                key = s_dataTemplateIncrementer;
                _templateToId[itemTemplate] = key;
                _uiCollectionView.RegisterClassForCell(typeof(ViewCollectionCell), key.ToString());
            }

            return key;
        }

        protected ContentCell GetCellForPath(NSIndexPath indexPath)
        {
            var templatedItems = GetTemplatedItemsListForPath(indexPath);
            var cell = templatedItems[indexPath.Row] as ContentCell;
            return cell;
        }

        protected ITemplatedItemsList<Cell> GetTemplatedItemsListForPath(NSIndexPath indexPath)
        {
            var templatedItems = TemplatedItemsView.TemplatedItems;
            if (_collectionView.IsGroupingEnabled)
                templatedItems = (ITemplatedItemsList<Cell>)((IList)templatedItems)[indexPath.Section];

            return templatedItems;
        }
    }
}
