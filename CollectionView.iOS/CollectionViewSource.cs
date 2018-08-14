using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using AiForms.Renderers.iOS.Cells;
using CoreGraphics;
using Foundation;
using ObjCRuntime;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.iOS;

namespace AiForms.Renderers.iOS
{
    [Foundation.Preserve(AllMembers = true)]
    public class CollectionViewSource : UICollectionViewSource, IUICollectionViewDelegateFlowLayout
    {
        static int s_dataTemplateIncrementer = 2; // lets start at not 0 because

        public CGSize CellSize { get; set; }
        public Dictionary<int, int> Counts { get; set; }
        const int DefaultItemTemplateId = 1;
        bool _isLongTap;
        bool _disposed;
        GridCollectionView _collectionView;
        UICollectionView _uiCollectionView;
        Dictionary<DataTemplate, int> _templateToId = new Dictionary<DataTemplate, int>();
        ITemplatedItemsView<Cell> TemplatedItemsView => _collectionView;


        public CollectionViewSource(CollectionView collectionView, UICollectionView uICollectionView)
        {
            _collectionView = (GridCollectionView)collectionView;
            _uiCollectionView = uICollectionView;
            Counts = new Dictionary<int, int>();
            _uiCollectionView.RegisterClassForCell(typeof(ContentCellContainer), DefaultItemTemplateId.ToString());
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            if(disposing)
            {
                Counts = null;
                _templateToId = null;
                _collectionView = null;
                _uiCollectionView = null;
               
            }

            _disposed = true;

            base.Dispose(disposing);
        }

        public override nint NumberOfSections(UICollectionView collectionView)
        {
            if (_collectionView.IsGroupingEnabled)
            {
                return TemplatedItemsView.TemplatedItems.Count;
            }
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

        public override void ItemHighlighted(UICollectionView collectionView, NSIndexPath indexPath)
        {
            _isLongTap = false;
            var cell = collectionView.CellForItem(indexPath);
            (cell as ContentCellContainer)?.SelectedAnimation(0.4, 0, 0.5);
        }

        public override void ItemUnhighlighted(UICollectionView collectionView, NSIndexPath indexPath)
        {
            if (_isLongTap)
            {
                return;
            }
            var cell = collectionView.CellForItem(indexPath);
            (cell as ContentCellContainer)?.SelectedAnimation(0.4, 0.5, 0);
        }

        public override bool ShouldShowMenu(UICollectionView collectionView, NSIndexPath indexPath)
        {
            // Detected long tap
            if (_collectionView.ItemLongTapCommand == null)
            {
                return false;
            }

            _isLongTap = true;
            var cell = collectionView.CellForItem(indexPath) as ContentCellContainer;
            var formsCell = cell.ContentCell;

            if (_collectionView.ItemLongTapCommand != null && _collectionView.ItemLongTapCommand.CanExecute(formsCell.BindingContext))
            {
                _collectionView.ItemLongTapCommand.Execute(formsCell.BindingContext);
            }

            (cell as ContentCellContainer)?.SelectedAnimation(1.0, 0.5, 0);

            return true;
        }

        public override bool CanPerformAction(UICollectionView collectionView, Selector action, NSIndexPath indexPath, NSObject sender)
        {
            return false;
        }

        public override void PerformAction(UICollectionView collectionView, Selector action, NSIndexPath indexPath, NSObject sender){}

        public override void ItemSelected(UICollectionView collectionView, NSIndexPath indexPath)
        {
            var cell = collectionView.CellForItem(indexPath) as ContentCellContainer;

            if (cell == null)
                return;

            var formsCell = cell.ContentCell;

            if (_collectionView.ItemTapCommand != null && _collectionView.ItemTapCommand.CanExecute(formsCell.BindingContext))
            {
                _collectionView.ItemTapCommand.Execute(formsCell.BindingContext);
            }

            _collectionView.NotifyRowTapped(indexPath.Section, indexPath.Row, formsCell);

            collectionView.DeselectItem(indexPath, false);
        }

        public override UICollectionReusableView GetViewForSupplementaryElement(UICollectionView collectionView, NSString elementKind, NSIndexPath indexPath)
        {
            if (!_collectionView.IsGroupingEnabled)
                return null;

            if (elementKind == "UICollectionElementKindSectionFooter")
            {
                return null;
            }

            ContentCell cell;
            ContentCellContainer nativeCell;

            Performance.Start(out string reference);

            var cachingStrategy = _collectionView.CachingStrategy;
            if (cachingStrategy == ListViewCachingStrategy.RetainElement)
            {
                cell = TemplatedItemsView.TemplatedItems[(int)indexPath.Section] as ContentCell;
                nativeCell = GetNativeHeaderCell(cell, indexPath);
            }
            else if ((cachingStrategy & ListViewCachingStrategy.RecycleElement) != 0)
            {

                var id = TemplateIdForPath(indexPath);

                nativeCell = collectionView.DequeueReusableSupplementaryView(UICollectionElementKindSection.Header, CollectionViewRenderer.SectionHeaderId, indexPath) as ContentCellContainer;
                if (nativeCell.ContentCell == null)
                {
                    cell = TemplatedItemsView.TemplatedItems[(int)indexPath.Section] as ContentCell;

                    nativeCell = GetNativeHeaderCell(cell, indexPath);
                }
                else
                {
                    var templatedList = TemplatedItemsView.TemplatedItems.GetGroup(indexPath.Section);

                    cell = (ContentCell)((INativeElementView)nativeCell).Element;
                    cell.SendDisappearing();

                    templatedList.UpdateHeader(cell, indexPath.Section);
                    cell.SendAppearing();
                }
            }
            else
            {
                throw new NotSupportedException();
            }

            Performance.Stop(reference);
            return nativeCell;
        }

        public override UICollectionViewCell GetCell(UICollectionView collectionView, NSIndexPath indexPath)
        {
            ContentCell cell;
            ContentCellContainer nativeCell;

            Performance.Start(out string reference);

            var cachingStrategy = _collectionView.CachingStrategy;
            if (cachingStrategy == ListViewCachingStrategy.RetainElement)
            {
                cell = GetCellForPath(indexPath);
                nativeCell = GetNativeCell(cell, indexPath);
            }
            else if ((cachingStrategy & ListViewCachingStrategy.RecycleElement) != 0)
            {

                var id = TemplateIdForPath(indexPath);


                nativeCell = collectionView.DequeueReusableCell(id.ToString(), indexPath) as ContentCellContainer;
                if (nativeCell.ContentCell == null)
                {
                    cell = GetCellForPath(indexPath);

                    nativeCell = GetNativeCell(cell, indexPath, true, id.ToString());
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

            Performance.Stop(reference);
            return nativeCell;
        }

        [Export("collectionView:layout:sizeForItemAtIndexPath:")]
        public CGSize GetSizeForItem(UICollectionView collectionView, UICollectionViewLayout layout, NSIndexPath indexPath)
        {
            return CellSize;
        }

        ContentCellContainer GetNativeHeaderCell(ContentCell cell, NSIndexPath indexPath)
        {
            var renderer = (ContentCellRenderer)Xamarin.Forms.Internals.Registrar.Registered.GetHandlerForObject<IRegisterable>(cell);

            var reusableCell = _uiCollectionView.DequeueReusableSupplementaryView(UICollectionElementKindSection.Header, CollectionViewRenderer.SectionHeaderId, indexPath) as ContentCellContainer;

            var nativeCell = renderer.GetCell(cell, reusableCell, _uiCollectionView) as ContentCellContainer;

            return nativeCell;
        }

        ContentCellContainer GetNativeCell(ContentCell cell, NSIndexPath indexPath, bool recycleCells = false, string templateId = "")
        {
            var id = recycleCells ? templateId : cell.GetType().FullName;

            var renderer = (ContentCellRenderer)Xamarin.Forms.Internals.Registrar.Registered.GetHandlerForObject<IRegisterable>(cell);

            // UITableViewと違って初回でもインスタンスを返すので注意
            var reusableCell = _uiCollectionView.DequeueReusableCell(id, indexPath) as ContentCellContainer;

            var nativeCell = renderer.GetCell(cell, reusableCell, _uiCollectionView) as ContentCellContainer;

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
                _uiCollectionView.RegisterClassForCell(typeof(ContentCellContainer), key.ToString());
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
