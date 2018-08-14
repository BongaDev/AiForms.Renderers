using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows.Input;
using AiForms.Renderers;
using AiForms.Renderers.iOS;
using AiForms.Renderers.iOS.Cells;
using CoreGraphics;
using Foundation;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.iOS;
using RectangleF = CoreGraphics.CGRect;

[assembly: ExportRenderer(typeof(GridCollectionView), typeof(CollectionViewRenderer))]
namespace AiForms.Renderers.iOS
{
    [Foundation.Preserve(AllMembers = true)]
    public class CollectionViewRenderer : ViewRenderer<CollectionView, UICollectionView>
    {
        public const string SectionHeaderId = "SectionHeader";

        UICollectionViewFlowLayout _viewLayout;
        UICollectionView _collectionView;
        UIRefreshControl _refreshControl;
        CollectionViewSource _dataSource;
        KeyboardInsetTracker _insetTracker;
        RectangleF _previousFrame;
        bool _disposed;
        ITemplatedItemsView<Cell> TemplatedItemsView => Element;
        GridCollectionView _gridCollectionView => (GridCollectionView)Element;
        bool _isRatioHeight => _gridCollectionView.ColumnHeight <= 5.0;
        ICommand _tapCommand => _gridCollectionView.ItemTapCommand;
        ICommand _longTapCommand => _gridCollectionView.ItemLongTapCommand;


        protected override void OnElementChanged(ElementChangedEventArgs<CollectionView> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                var templatedItems = ((ITemplatedItemsView<Cell>)e.OldElement).TemplatedItems;
                templatedItems.CollectionChanged -= OnCollectionChanged;
                templatedItems.GroupedCollectionChanged -= OnGroupedCollectionChanged;
            }

            if (e.NewElement != null)
            {
                _viewLayout = new UICollectionViewFlowLayout();
                _viewLayout.ScrollDirection = UICollectionViewScrollDirection.Vertical;
                _viewLayout.SectionInset = new UIEdgeInsets(0, 0, 0, 0);
                _viewLayout.MinimumLineSpacing = 0.0f;
                _viewLayout.MinimumInteritemSpacing = 0.0f;
                _viewLayout.EstimatedItemSize = UICollectionViewFlowLayout.AutomaticSize;
                _viewLayout.SectionHeadersPinToVisibleBounds = true; // fix header cell


                _refreshControl = new UIRefreshControl();
                _refreshControl.ValueChanged += RefreshControl_ValueChanged;

                _collectionView = new UICollectionView(CGRect.Empty, _viewLayout);
                _collectionView.RegisterClassForCell(typeof(ContentCellContainer), typeof(ContentCell).FullName);
                _collectionView.RegisterClassForSupplementaryView(typeof(ContentCellContainer), UICollectionElementKindSection.Header, SectionHeaderId);

                _collectionView.AllowsSelection = true;

                SetNativeControl(_collectionView);

                _insetTracker = new KeyboardInsetTracker(_collectionView, () => Control.Window, insets => Control.ContentInset = Control.ScrollIndicatorInsets = insets, point =>
                {
                    var offset = Control.ContentOffset;
                    offset.Y += point.Y;
                    Control.SetContentOffset(offset, true);
                });


                var templatedItems = ((ITemplatedItemsView<Cell>)e.NewElement).TemplatedItems;

                templatedItems.CollectionChanged += OnCollectionChanged;
                templatedItems.GroupedCollectionChanged += OnGroupedCollectionChanged;


                _dataSource = new CollectionViewSource(e.NewElement, _collectionView);
                _collectionView.Source = _dataSource;

                UpdateBackgroundColor();
                UpdateRowSpacing();
                UpdatePullToRefreshEnabled();
                UpdatePullToRefreshColor();
            }
        }

        public override void LayoutSubviews()
        {
            base.LayoutSubviews();

            UpdateGridType();

            UpdateGroupHeaderHeight();

            if (_previousFrame != Frame)
            {
                _previousFrame = Frame;
                _insetTracker?.UpdateInsets();
            }
        }

        public override SizeRequest GetDesiredSize(double widthConstraint, double heightConstraint)
        {
            return Control.GetSizeRequest(widthConstraint, heightConstraint, 50, 50);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _insetTracker?.Dispose();
                _insetTracker = null;

                foreach (UIView subview in Subviews)
                {
                    DisposeSubviews(subview);
                }

                if (Element != null)
                {
                    var templatedItems = TemplatedItemsView.TemplatedItems;
                    templatedItems.CollectionChanged -= OnCollectionChanged;
                    templatedItems.GroupedCollectionChanged -= OnGroupedCollectionChanged;
                }

                _dataSource?.Dispose();
                _dataSource = null;

                _viewLayout?.Dispose();
                _viewLayout = null;

                if (_refreshControl != null)
                {
                    _refreshControl.ValueChanged -= RefreshControl_ValueChanged;
                    _refreshControl.Dispose();
                    _refreshControl = null;
                }

                _collectionView?.Dispose();
                _collectionView = null;


            }

            _disposed = true;

            base.Dispose(disposing);
        }

        void DisposeSubviews(UIView view)
        {
            var ver = view as IVisualElementRenderer;

            if (ver == null)
            {
                // VisualElementRenderers should implement their own dispose methods that will appropriately dispose and remove their child views.
                // Attempting to do this work twice could cause a SIGSEGV (only observed in iOS8), so don't do this work here.
                // Non-renderer views, such as separator lines, etc., can be removed here.
                foreach (UIView subView in view.Subviews)
                {
                    DisposeSubviews(subView);
                }

                view.RemoveFromSuperview();
            }

            view.Dispose();
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == Xamarin.Forms.ListView.IsGroupingEnabledProperty.PropertyName)
            {
                Control.ReloadData();
            }
            else if (e.PropertyName == GridCollectionView.GroupHeaderHeightProperty.PropertyName)
            {
                UpdateGroupHeaderHeight();
                _viewLayout.InvalidateLayout();
            }
            else if (e.PropertyName == GridCollectionView.GridTypeProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.PortraitColumnsProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.LandscapeColumnsProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.ColumnSpacingProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.ColumnHeightProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.SpacingTypeProperty.PropertyName)
            {
                UpdateGridType();
                _viewLayout.InvalidateLayout();
            }
            else if (e.PropertyName == GridCollectionView.RowSpacingProperty.PropertyName)
            {
                UpdateRowSpacing();
                _viewLayout.InvalidateLayout();
            }
            else if (e.PropertyName == GridCollectionView.ColumnWidthProperty.PropertyName)
            {
                if (_gridCollectionView.GridType != GridType.UniformGrid)
                {
                    UpdateGridType();
                    _viewLayout.InvalidateLayout();
                }
            }
            else if (e.PropertyName == ListView.IsPullToRefreshEnabledProperty.PropertyName)
            {
                UpdatePullToRefreshEnabled();
            }
            else if (e.PropertyName == GridCollectionView.PullToRefreshColorProperty.PropertyName)
            {
                UpdatePullToRefreshColor();
            }
            else if (e.PropertyName == Xamarin.Forms.ListView.IsRefreshingProperty.PropertyName)
            {
                UpdateIsRefreshing();
            }
            else if (e.PropertyName == VisualElement.BackgroundColorProperty.PropertyName)
            {
                UpdateBackgroundColor();
            }

        }

        void RefreshControl_ValueChanged(object sender, EventArgs e)
        {
            if (_refreshControl.Refreshing)
            {
                _gridCollectionView.SendRefreshing();
            }
            _gridCollectionView.IsRefreshing = _refreshControl.Refreshing;
        }

        void UpdateBackgroundColor()
        {
            if(Element.BackgroundColor.IsDefault)
            {
                Control.BackgroundColor = UIColor.Clear; 
            }
            else{
                Control.BackgroundColor = Element.BackgroundColor.ToUIColor();
            }
        }

        void UpdateIsRefreshing()
        {
            var refreshing = Element.IsRefreshing;
            if (_gridCollectionView == null)
            {
                return;
            }
            if (refreshing)
            {
                if (!_refreshControl.Refreshing)
                {
                    _refreshControl.BeginRefreshing();
                }
            }
            else
            {
                _refreshControl.EndRefreshing();
            }

        }

        void UpdatePullToRefreshColor()
        {
            if (!_gridCollectionView.PullToRefreshColor.IsDefault)
            {
                _refreshControl.TintColor = _gridCollectionView.PullToRefreshColor.ToUIColor();
            }
        }

        void UpdatePullToRefreshEnabled()
        {
            _refreshControl.Enabled = Element.IsPullToRefreshEnabled && (Element as IListViewController).RefreshAllowed;
            if (_refreshControl.Enabled)
            {
                _collectionView.RefreshControl = _refreshControl;
            }
            else
            {
                _collectionView.RefreshControl = null;
            }
        }

        void UpdateRowSpacing()
        {
            _viewLayout.MinimumLineSpacing = (System.nfloat)_gridCollectionView.RowSpacing;
        }

        void UpdateGroupHeaderHeight()
        {
            if (_gridCollectionView.IsGroupingEnabled)
            {
                // TODO: 細かいサイズ調整をする場合はサブクラスで対応する
                _viewLayout.HeaderReferenceSize = new CGSize(Bounds.Width, _gridCollectionView.GroupHeaderHeight);
            }
        }

        void UpdateGridType()
        {
            _viewLayout.SectionInset = new UIEdgeInsets(0, 0, 0, 0); // Reset insets
            CGSize itemSize = CGSize.Empty;

            if (_gridCollectionView.GridType == GridType.UniformGrid)
            {
                switch (UIApplication.SharedApplication.StatusBarOrientation)
                {
                    case UIInterfaceOrientation.Portrait:
                    case UIInterfaceOrientation.PortraitUpsideDown:
                    case UIInterfaceOrientation.Unknown:
                        itemSize = GetUniformItemSize(_gridCollectionView.PortraitColumns);

                        break;
                    case UIInterfaceOrientation.LandscapeLeft:
                    case UIInterfaceOrientation.LandscapeRight:
                        itemSize = GetUniformItemSize(_gridCollectionView.LandscapeColumns);
                        break;
                }
                _viewLayout.MinimumInteritemSpacing = (System.nfloat)_gridCollectionView.ColumnSpacing;
            }
            else
            {
                itemSize = GetAutoSpacingItemSize();
            }

            _dataSource.CellSize = itemSize;
        }

        double CalcurateColumnHeight(double itemWidth)
        {
            if (_isRatioHeight)
            {
                return itemWidth * _gridCollectionView.ColumnHeight;
            }

            return _gridCollectionView.ColumnHeight;
        }

        CGSize GetUniformItemSize(int columns)
        {
            var hackWidth = _gridCollectionView.ColumnSpacing > 0 ? 0.1f : 0f;
            float width = (float)Frame.Width - (float)_gridCollectionView.ColumnSpacing * (float)(columns - 1.0f);
            var itemWidth = (float)(width / (float)columns) - hackWidth; // because item is sometimes overflowed.
            var itemHeight = CalcurateColumnHeight(itemWidth);
            return new CGSize(itemWidth, itemHeight);
        }

        CGSize GetAutoSpacingItemSize()
        {
            var hackWidth = _gridCollectionView.ColumnSpacing > 0 ? 0.1f : 0f;
            var itemWidth = (float)Math.Min(Frame.Width, _gridCollectionView.ColumnWidth) - hackWidth;
            var itemHeight = CalcurateColumnHeight(itemWidth);
            if (_gridCollectionView.SpacingType == SpacingType.Between)
            {
                return new CGSize(itemWidth, itemHeight);
            }


            var leftSize = (float)Frame.Width;
            var spacing = (float)_gridCollectionView.ColumnSpacing;
            int columnCount = 0;
            do
            {
                leftSize -= itemWidth;
                if (leftSize < 0)
                {
                    break;
                }
                columnCount++;
                if (leftSize - spacing < 0)
                {
                    break;
                }
                leftSize -= spacing;
            } while (true);

            var contentWidth = itemWidth * columnCount + spacing * (columnCount - 1f);

            var inset = (Frame.Width - contentWidth) / 2.0f;

            _viewLayout.SectionInset = new UIEdgeInsets(0, inset, 0, inset);

            return new CGSize(itemWidth, itemHeight);
        }


        void OnGroupedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            var til = (TemplatedItemsList<ItemsView<Cell>, Cell>)sender;

            var templatedItems = TemplatedItemsView.TemplatedItems;
            var groupIndex = templatedItems.IndexOf(til.HeaderContent);
            UpdateItems(e, groupIndex, false);
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            UpdateItems(e, 0, true);
        }

        void UpdateItems(NotifyCollectionChangedEventArgs e, int section, bool resetWhenGrouped)
        {
            var exArgs = e as NotifyCollectionChangedEventArgsEx;
            if (exArgs != null)
                _dataSource.Counts[section] = exArgs.Count;

            // This means the UITableView hasn't rendered any cells yet
            // so there's no need to synchronize the rows on the UITableView
            if (Control.IndexPathsForVisibleItems == null && e.Action != NotifyCollectionChangedAction.Reset)
                return;

            var groupReset = resetWhenGrouped && Element.IsGroupingEnabled;

            // We can't do this check on grouped lists because the index doesn't match the number of rows in a section.
            // Likewise, we can't do this check on lists using RecycleElement because the number of rows in a section will remain constant because they are reused.
            if (!groupReset && Element.CachingStrategy == ListViewCachingStrategy.RetainElement)
            {
                var lastIndex = Control.NumberOfItemsInSection(section);
                if (e.NewStartingIndex > lastIndex || e.OldStartingIndex > lastIndex)
                    throw new ArgumentException(
                        $"Index '{Math.Max(e.NewStartingIndex, e.OldStartingIndex)}' is greater than the number of rows '{lastIndex}'.");
            }

            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:

                    // TODO: これの必要性がまだ不明なので保留
                    // UpdateEstimatedRowHeight();
                    if (e.NewStartingIndex == -1 || groupReset)
                    {
                        goto case NotifyCollectionChangedAction.Reset;
                    }

                    Control.PerformBatchUpdates(() =>
                    {
                        Control.InsertItems(GetPaths(section, e.NewStartingIndex, e.NewItems.Count));
                    }, (finished) =>
                    {

                    });

                    break;

                case NotifyCollectionChangedAction.Remove:
                    if (e.OldStartingIndex == -1 || groupReset)
                    {
                        goto case NotifyCollectionChangedAction.Reset;
                    }
                    Control.PerformBatchUpdates(() =>
                    {
                        Control.DeleteItems(GetPaths(section, e.OldStartingIndex, e.OldItems.Count));
                    }, (finished) =>
                    {
                        // TODO: 意味不なので放置
                        //if (_estimatedRowHeight && TemplatedItemsView.TemplatedItems.Count == 0)
                        //InvalidateCellCache();
                    });

                    break;

                case NotifyCollectionChangedAction.Move:
                    if (e.OldStartingIndex == -1 || e.NewStartingIndex == -1 || groupReset)
                    {
                        goto case NotifyCollectionChangedAction.Reset;
                    }
                    Control.PerformBatchUpdates(() =>
                    {
                        for (var i = 0; i < e.OldItems.Count; i++)
                        {
                            var oldi = e.OldStartingIndex;
                            var newi = e.NewStartingIndex;

                            if (e.NewStartingIndex < e.OldStartingIndex)
                            {
                                oldi += i;
                                newi += i;
                            }

                            Control.MoveItem(NSIndexPath.FromRowSection(oldi, section), NSIndexPath.FromRowSection(newi, section));
                        }
                    }, (finished) =>
                    {
                        // TODO: 意味不なので放置
                        //if (_estimatedRowHeight && e.OldStartingIndex == 0)
                        //InvalidateCellCache();
                    });

                    break;

                case NotifyCollectionChangedAction.Replace:
                    if (e.OldStartingIndex == -1 || groupReset)
                    {
                        goto case NotifyCollectionChangedAction.Reset;
                    }

                    Control.PerformBatchUpdates(() =>
                    {
                        Control.ReloadItems(GetPaths(section, e.OldStartingIndex, e.OldItems.Count));
                    }, (finished) =>
                    {
                        //if (_estimatedRowHeight && e.OldStartingIndex == 0)
                        //InvalidateCellCache();
                    });

                    break;

                case NotifyCollectionChangedAction.Reset:
                    //InvalidateCellCache();
                    Control.ReloadData();
                    return;
            }
        }

        NSIndexPath[] GetPaths(int section, int index, int count)
        {
            var paths = new NSIndexPath[count];
            for (var i = 0; i < paths.Length; i++)
            {
                paths[i] = NSIndexPath.FromRowSection(index + i, section);
            }

            return paths;
        }
    }
}
