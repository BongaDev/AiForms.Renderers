using System;
using System.ComponentModel;
using AiForms.Renderers;
using AiForms.Renderers.Droid;
using Android.Content;
using Android.Content.Res;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Views;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;

[assembly: ExportRenderer(typeof(GridCollectionView), typeof(GridCollectionViewRenderer))]
namespace AiForms.Renderers.Droid
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class GridCollectionViewRenderer : ViewRenderer<CollectionView, SwipeRefreshLayout>, SwipeRefreshLayout.IOnRefreshListener
    {
        GridCollectionViewAdapter _adapter;
        SwipeRefreshLayout _refresh;
        GridLayoutManager _layoutManager;
        RecyclerView _recyclerView;
        CollectionViewSpanSizeLookup _spanSizeLookup;
        GridCollectionItemDecoration _itemDecoration;
        IListViewController Controller => Element;
        ITemplatedItemsView<Cell> TemplatedItemsView => Element;
        GridCollectionView _gridCollectionView => (GridCollectionView)Element;
        bool _isRatioHeight => _gridCollectionView.ColumnHeight <= 5.0;
        bool _isAttached;
        bool _disposed;

        public int RowSpacing { get; set; }
        public int ColumnSpacing { get; set; }
        public int GroupHeaderHeight { get; set; }
        public int RowHeight { get; set; }

        public GridCollectionViewRenderer(Context context) : base(context)
        {
            AutoPackage = false;
        }

        protected override void Dispose(bool disposing)
        {
            if(_disposed)
            {
                return;
            }

            if(disposing)
            {
                _recyclerView?.SetAdapter(null);
                _recyclerView?.RemoveItemDecoration(_itemDecoration);
                _layoutManager?.SetSpanSizeLookup(null);

                _adapter?.Dispose();
                _adapter = null;

                _layoutManager?.Dispose();
                _layoutManager = null;

                _spanSizeLookup?.Dispose();
                _spanSizeLookup = null;

                _itemDecoration?.Dispose();
                _itemDecoration = null;


                _recyclerView?.Dispose();
                _recyclerView = null;
            }

            _disposed = true;

            base.Dispose(disposing);
        }

        protected override void OnElementChanged(ElementChangedEventArgs<CollectionView> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null)
            {
                if (_adapter != null)
                {
                    _adapter?.Dispose();
                    _adapter = null;
                }
            }

            if (e.NewElement != null)
            {
                if (_recyclerView == null)
                {
                    _recyclerView = new RecyclerView(Context);
                    _refresh = new SwipeRefreshLayout(Context);
                    _refresh.SetOnRefreshListener(this);
                    _refresh.AddView(_recyclerView, LayoutParams.MatchParent, LayoutParams.MatchParent);

                    SetNativeControl(_refresh);
                }

                _layoutManager = new GridLayoutManager(Context, 2);
                _spanSizeLookup = new CollectionViewSpanSizeLookup(this);
                _layoutManager.SetSpanSizeLookup(_spanSizeLookup);

                _recyclerView.Focusable = false;
                _recyclerView.DescendantFocusability = Android.Views.DescendantFocusability.AfterDescendants;
                _recyclerView.OnFocusChangeListener = this;
                _recyclerView.SetClipToPadding(false);

                _itemDecoration = new GridCollectionItemDecoration(this);
                _recyclerView.AddItemDecoration(_itemDecoration);

                _adapter = new GridCollectionViewAdapter(Context, _gridCollectionView, _recyclerView, this);

                _recyclerView.SetAdapter(_adapter);
                _recyclerView.SetLayoutManager(_layoutManager);

                _adapter.IsAttachedToWindow = _isAttached;

                UpdateGroupHeaderHeight();
                UpdatePullToRefreshEnabled();
                UpdatePullToRefreshColor();
            }
        }

        protected override void OnElementPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            base.OnElementPropertyChanged(sender, e);
            if (e.PropertyName == Xamarin.Forms.ListView.IsGroupingEnabledProperty.PropertyName)
            {
                RefreshAll();
            }
            else if (e.PropertyName == GridCollectionView.GroupHeaderHeightProperty.PropertyName)
            {
                UpdateGroupHeaderHeight();
                RefreshAll();
            }
            else if (e.PropertyName == GridCollectionView.GridTypeProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.PortraitColumnsProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.LandscapeColumnsProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.ColumnSpacingProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.RowSpacingProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.ColumnHeightProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.ColumnWidthProperty.PropertyName ||
                     e.PropertyName == GridCollectionView.SpacingTypeProperty.PropertyName)
            {
                UpdateGridType();
                RefreshAll();
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

        protected virtual void RefreshAll()
        {
            
            _recyclerView.RemoveItemDecoration(_itemDecoration);
            _layoutManager.GetSpanSizeLookup().InvalidateSpanIndexCache();

            _adapter.OnDataChanged();
            _recyclerView.AddItemDecoration(_itemDecoration);
            RequestLayout();
            Invalidate();
            //_recyclerView.RequestLayout();
            //_recyclerView.Invalidate();
            //_refresh.Invalidate();
            //_refresh.RequestLayout();
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            System.Diagnostics.Debug.WriteLine($"In OnLayout {changed}");
            if (changed)
            {
                UpdateGridType(r - l);
            }

            base.OnLayout(changed, l, t, r, b);
        }

        protected override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            System.Diagnostics.Debug.WriteLine($"In OnConfigurationChanged");
            UpdateGridType();

            // HACK: run after a bit time because of not refreshing immediately
            Device.StartTimer(TimeSpan.FromMilliseconds(1000), () =>
            {
                RefreshAll();
                return false;
            });
        }

        void SwipeRefreshLayout.IOnRefreshListener.OnRefresh()
        {
            IListViewController controller = Element;
            controller.SendRefreshing();
        }

        protected override void OnAttachedToWindow()
        {
            base.OnAttachedToWindow();

            _isAttached = true;
            _adapter.IsAttachedToWindow = _isAttached;
            UpdateIsRefreshing(isInitialValue: true);
        }

        protected override void OnDetachedFromWindow()
        {
            base.OnDetachedFromWindow();

            _isAttached = false;
            _adapter.IsAttachedToWindow = _isAttached;
        }

        void UpdatePullToRefreshColor()
        {
            if (!_gridCollectionView.PullToRefreshColor.IsDefault)
            {
                var color = _gridCollectionView.PullToRefreshColor.ToAndroid();
                _refresh.SetColorSchemeColors(color, color, color, color);
            }
        }

        void UpdatePullToRefreshEnabled()
        {
            if (_refresh != null)
                _refresh.Enabled = Element.IsPullToRefreshEnabled && (Element as IListViewController).RefreshAllowed;
        }

        void UpdateIsRefreshing(bool isInitialValue = false)
        {
            if (_refresh != null)
            {
                var isRefreshing = Element.IsRefreshing;
                if (isRefreshing && isInitialValue)
                {
                    _refresh.Refreshing = false;
                    _refresh.Post(() =>
                    {
                        _refresh.Refreshing = true;
                    });
                }
                else
                    _refresh.Refreshing = isRefreshing;
            }
        }

        void UpdateGroupHeaderHeight()
        {
            if (_gridCollectionView.IsGroupingEnabled)
            {
                GroupHeaderHeight = (int)Context.ToPixels(_gridCollectionView.GroupHeaderHeight);
            }
        }

        void UpdateGridType(int containerWidth = 0)
        {
            containerWidth = containerWidth == 0 ? Width : containerWidth;
            if (containerWidth <= 0)
            {
                return;
            }
            _recyclerView.SetPadding(0, 0, 0, 0);
            RowSpacing = (int)Context.ToPixels(_gridCollectionView.RowSpacing);

            int spanCount = 0;
            if (_gridCollectionView.GridType == GridType.UniformGrid)
            {
                var orientation = Context.Resources.Configuration.Orientation;
                switch (orientation)
                {
                    case Orientation.Portrait:
                    case Orientation.Square:
                    case Orientation.Undefined:
                        spanCount = _gridCollectionView.PortraitColumns;
                        break;
                    case Orientation.Landscape:
                        spanCount = _gridCollectionView.LandscapeColumns;
                        break;
                }
                ColumnSpacing = (int)(Context.ToPixels(_gridCollectionView.ColumnSpacing));
                RowHeight = GetUniformItemHeight(containerWidth, spanCount);
                System.Diagnostics.Debug.WriteLine($"Decided RowHeight {RowHeight}");
            }
            else
            {
                var autoSpacingSize = GetAutoSpacingItemSize(containerWidth);
                spanCount = autoSpacingSize.spanCount;
                ColumnSpacing = autoSpacingSize.columnSpacing;
                RowHeight = autoSpacingSize.rowHeight;
            }

            _layoutManager.SpanCount = spanCount;
            _spanSizeLookup.SpanSize = spanCount;
        }

        double CalcurateColumnHeight(double itemWidth)
        {
            if (_isRatioHeight)
            {
                return itemWidth * _gridCollectionView.ColumnHeight;
            }

            return Context.ToPixels(_gridCollectionView.ColumnHeight);
        }

        int GetUniformItemHeight(int containerWidth, int columns)
        {
            float actualWidth = containerWidth - (float)_gridCollectionView.ColumnSpacing * (float)(columns - 1.0f);
            var itemWidth = (float)(actualWidth / (float)columns);
            var itemHeight = CalcurateColumnHeight(itemWidth);
            var devSpacing = (double)ColumnSpacing * (columns - 1.0) / (double)columns;
            return (int)(itemHeight - devSpacing + RowSpacing);
        }

        (int spanCount, int columnSpacing, int rowHeight) GetAutoSpacingItemSize(double containerWidth)
        {
            var columnWidth = Context.ToPixels(_gridCollectionView.ColumnWidth);
            var columnHeight = Context.ToPixels(_gridCollectionView.ColumnHeight);

            var itemWidth = Math.Min(containerWidth, columnWidth);
            var itemHeight = CalcurateColumnHeight(itemWidth);

            var leftSize = containerWidth;
            var spacing = _gridCollectionView.SpacingType == SpacingType.Between ? 0 : Context.ToPixels(_gridCollectionView.ColumnSpacing);
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

            double contentWidth = 0;
            double columnSpacing = 0;
            if (_gridCollectionView.SpacingType == SpacingType.Between)
            {
                contentWidth = itemWidth * columnCount;
                columnSpacing = (containerWidth - contentWidth) / (columnCount - 1);
                return (columnCount, (int)columnSpacing, (int)itemHeight + RowSpacing);
            }

            contentWidth = itemWidth * columnCount + spacing * (columnCount - 1f);
            var inset = (containerWidth - contentWidth) / 2.0f;
            _recyclerView.SetPadding((int)inset, 0, (int)inset, 0);
            columnSpacing = spacing;

            return (columnCount, (int)columnSpacing, (int)itemHeight + RowSpacing);
        }


        internal class CollectionViewSpanSizeLookup : GridLayoutManager.SpanSizeLookup
        {
            GridCollectionViewRenderer _parent;
            GridCollectionView _gridCollectionView => _parent._gridCollectionView;

            public int SpanSize { get; set; }
            public int SpanCount { get; set; }

            public CollectionViewSpanSizeLookup(GridCollectionViewRenderer parent)
            {
                _parent = parent;
                SpanIndexCacheEnabled = false;
            }

            public override int GetSpanSize(int position)
            {
                if (_parent._gridCollectionView.IsGroupingEnabled)
                {
                    var group = _parent.TemplatedItemsView.TemplatedItems.GetGroupIndexFromGlobal(position, out var row);
                    if (row == 0)
                    {
                        return SpanSize;
                    }
                }

                return 1;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if (disposing)
                {
                    _parent = null;
                }
            }
        }

        internal class GridCollectionItemDecoration : RecyclerView.ItemDecoration
        {
            public bool IncludeEdge { get; set; }

            GridCollectionViewRenderer _parentRenderer;
            GridCollectionView _gridCollectionView => _parentRenderer._gridCollectionView;
            CollectionViewSpanSizeLookup _spanLookUp => _parentRenderer._spanSizeLookup;
            int _spanCount => _parentRenderer._layoutManager.SpanCount;

            public GridCollectionItemDecoration(GridCollectionViewRenderer parentRenderer)
            {
                _parentRenderer = parentRenderer;
            }

            protected override void Dispose(bool disposing)
            {
                if(disposing)
                {
                    _parentRenderer = null;
                }
                base.Dispose(disposing);
            }

            public override void GetItemOffsets(Android.Graphics.Rect outRect, Android.Views.View view, RecyclerView parent, RecyclerView.State state)
            {
                var param = view.LayoutParameters as GridLayoutManager.LayoutParams;
                var spanIndex = param.SpanIndex;
                var spanSize = param.SpanSize;

                if (spanSize == _spanCount)
                {
                    if (_gridCollectionView.GridType == GridType.AutoSpacingGrid &&
                       _gridCollectionView.SpacingType == SpacingType.Center)
                    {
                        var margin = _parentRenderer._recyclerView.PaddingLeft * -1;
                        var headparams = view.LayoutParameters as ViewGroup.MarginLayoutParams;
                        headparams.SetMargins(margin, headparams.TopMargin, margin, headparams.BottomMargin);
                        view.LayoutParameters = headparams;
                    }
                    return;
                }

                if (_spanCount == 1)
                {
                    return;
                }

                if (IncludeEdge)
                {
                    outRect.Left = _parentRenderer.ColumnSpacing - spanIndex * _parentRenderer.ColumnSpacing / _spanCount; // spacing - column * ((1f / spanCount) * spacing)
                    outRect.Right = (spanIndex + 1) * _parentRenderer.ColumnSpacing / _spanCount; // (column + 1) * ((1f / spanCount) * spacing)
                }
                else
                {
                    outRect.Left = spanIndex * _parentRenderer.ColumnSpacing / _spanCount; // column * ((1f / spanCount) * spacing)
                    outRect.Right = _parentRenderer.ColumnSpacing - (spanIndex + 1) * _parentRenderer.ColumnSpacing / _spanCount; // spacing - (column + 1) * ((1f /    spanCount) * spacing)
                }

                outRect.Bottom = _parentRenderer.RowSpacing;
            }
        }
    }
}
