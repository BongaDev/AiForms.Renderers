using System;
using Xamarin.Forms.Platform.Android;
using Android.Support.V4.Widget;
using Android.Content;
using Xamarin.Forms;
using AiForms.Renderers;
using AiForms.Renderers.Droid;
using Android.Support.V7.Widget;
using Xamarin.Forms.Internals;
using Android.Content.Res;
using Android.Views;

[assembly: ExportRenderer(typeof(GridCollectionView), typeof(GridCollectionViewRenderer))]
namespace AiForms.Renderers.Droid
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class GridCollectionViewRenderer:ViewRenderer<CollectionView,SwipeRefreshLayout>,SwipeRefreshLayout.IOnRefreshListener
    {
        GridCollectionViewAdapter _adapter;
        SwipeRefreshLayout _refresh;
        GridLayoutManager _layoutManager;
        IListViewController Controller => Element;
        ITemplatedItemsView<Cell> TemplatedItemsView => Element;
        GridCollectionView _gridCollectionView => (GridCollectionView)Element;
        RecyclerView _recyclerView;
        CollectionViewSpanSizeLookup _spanSizeLookup;
        GridCollectionItemDecoration _itemDecoration;
        bool _isRatioHeight => _gridCollectionView.ColumnHeight <= 5.0;
        bool _isAttached;

        public int RowSpacing { get; set; }
        public int ColumnSpacing { get; set; }
        public int GroupHeaderHeight { get; set; }
        public int RowHeight { get; set; }

        public GridCollectionViewRenderer(Context context):base(context)
        {
            AutoPackage = false;
        }

        void SwipeRefreshLayout.IOnRefreshListener.OnRefresh()
        {
            IListViewController controller = Element;
            controller.SendRefreshing();
        }      

        protected override void OnElementChanged(ElementChangedEventArgs<CollectionView> e)
        {
            base.OnElementChanged(e);

            if (e.OldElement != null) {
                ((IListViewController)e.OldElement).ScrollToRequested -= OnScrollToRequested;

                if (_adapter != null) {
                    _adapter.Dispose();
                    _adapter = null;
                }
            }

            if (e.NewElement != null) {
                if (_recyclerView == null) {
                    _recyclerView = new RecyclerView(Context);
                    _refresh = new SwipeRefreshLayout(Context);
                    _refresh.SetOnRefreshListener(this);
                    _refresh.AddView(_recyclerView, LayoutParams.MatchParent,LayoutParams.MatchParent);

                    //_refresh.SetBackgroundColor(Android.Graphics.Color.Beige);
                    //_recyclerView.SetBackgroundColor(Android.Graphics.Color.Red);
                    SetNativeControl(_refresh);
                }

                ((IListViewController)e.NewElement).ScrollToRequested += OnScrollToRequested;

                _layoutManager = new GridLayoutManager(Context,2);
                _spanSizeLookup = new CollectionViewSpanSizeLookup(this);
                _layoutManager.SetSpanSizeLookup(_spanSizeLookup);

                _recyclerView.Focusable = false;
                _recyclerView.DescendantFocusability = Android.Views.DescendantFocusability.AfterDescendants;
                _recyclerView.OnFocusChangeListener = this;
                _recyclerView.SetClipToPadding(false);

                _itemDecoration = new GridCollectionItemDecoration(Context, this);
                _recyclerView.AddItemDecoration(_itemDecoration);

                _adapter = new GridCollectionViewAdapter(Context, _gridCollectionView, _recyclerView,this);

                _recyclerView.SetAdapter(_adapter);

                _recyclerView.SetLayoutManager(_layoutManager);

                _adapter.IsAttachedToWindow = _isAttached;

                UpdateGroupHeaderHeight();

                _adapter?.NotifyDataSetChanged();
                //UpdateHeader();
                //UpdateFooter();
                //UpdateIsSwipeToRefreshEnabled();
                //UpdateFastScrollEnabled();
                //UpdateSelectionMode();
            }
        }



        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            System.Diagnostics.Debug.WriteLine($"In OnLayout {changed}");
            if(changed){
                UpdateGridType(r - l);
            }

            base.OnLayout(changed, l, t, r, b);
        }

        protected override void OnConfigurationChanged(Configuration newConfig)
        {
            base.OnConfigurationChanged(newConfig);
            System.Diagnostics.Debug.WriteLine($"In OnConfigurationChanged");
            UpdateGridType();

            Device.StartTimer(TimeSpan.FromMilliseconds(1000), () => {
                // HACK: run after a bit time because of not refreshing immediately
                _recyclerView.RemoveItemDecoration(_itemDecoration);
                _layoutManager.GetSpanSizeLookup().InvalidateSpanIndexCache();
                _recyclerView.AddItemDecoration(_itemDecoration);
                _adapter.NotifyDataSetChanged();
                RequestLayout();
                Invalidate();
                _recyclerView.RequestLayout();
                _recyclerView.Invalidate();
                _refresh.Invalidate();
                _refresh.RequestLayout();
                return false;
            });




            //_recyclerView.SetAdapter(null);
            //_recyclerView.SetLayoutManager(null);
            //_adapter.Dispose();
            //_adapter = null;
            //_recyclerView.Invalidate();
            //_adapter = new GridCollectionViewAdapter(Context, _gridCollectionView, _recyclerView, this);
            //_recyclerView.SetAdapter(_adapter);
            //_recyclerView.SetLayoutManager(_layoutManager);
            //_recyclerView.Invalidate();
            //_recyclerView.Visibility = ViewStates.Gone;
            //_recyclerView.Visibility = ViewStates.Visible;
            //_recyclerView.Invalidate();
            //_adapter.NotifyDataSetChanged();
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

        void UpdateIsRefreshing(bool isInitialValue = false)
        {
            if (_refresh != null) {
                var isRefreshing = Element.IsRefreshing;
                if (isRefreshing && isInitialValue) {
                    _refresh.Refreshing = false;
                    _refresh.Post(() => {
                        _refresh.Refreshing = true;
                    });
                }
                else
                    _refresh.Refreshing = isRefreshing;
            }
        }

        void UpdateGroupHeaderHeight()
        {
            if (_gridCollectionView.IsGroupingEnabled) {
                GroupHeaderHeight = (int)Context.ToPixels(_gridCollectionView.GroupHeaderHeight);
            }
        }

        void UpdateGridType(int containerWidth = 0)
        {
            containerWidth = containerWidth == 0 ? Width : containerWidth;
            if(containerWidth <= 0)
            {
                return;
            }
            _recyclerView.SetPadding(0,0,0,0);
            RowSpacing = (int)Context.ToPixels(_gridCollectionView.RowSpacing);

            int spanCount = 0;
            if (_gridCollectionView.GridType == GridType.UniformGrid) {
                var orientation = Context.Resources.Configuration.Orientation;
                switch (orientation) {
                    case Orientation.Portrait:
                    case Orientation.Square:
                    case Orientation.Undefined:
                        spanCount = _gridCollectionView.PortraitColumns;
                        break;
                    case Orientation.Landscape:
                        spanCount=  _gridCollectionView.LandscapeColumns;
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
            if(_isRatioHeight)
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

        (int spanCount,int columnSpacing,int rowHeight) GetAutoSpacingItemSize(double containerWidth)
        {
            var columnWidth = Context.ToPixels(_gridCollectionView.ColumnWidth);
            var columnHeight = Context.ToPixels(_gridCollectionView.ColumnHeight);

            var itemWidth = Math.Min(containerWidth, columnWidth);
            var itemHeight = CalcurateColumnHeight(itemWidth);

            var leftSize = containerWidth;
            var spacing = _gridCollectionView.SpacingType == SpacingType.Between ? 0 : Context.ToPixels(_gridCollectionView.ColumnSpacing);
            int columnCount = 0;
            do {
                leftSize -= itemWidth;
                if (leftSize < 0) {
                    break;
                }
                columnCount++;
                if (leftSize - spacing < 0) {
                    break;
                }
                leftSize -= spacing;
            } while (true);

            double contentWidth = 0;
            double columnSpacing = 0;
            if(_gridCollectionView.SpacingType == SpacingType.Between)
            {
                contentWidth = itemWidth * columnCount;
                columnSpacing = (containerWidth - contentWidth) / (columnCount - 1);
                return (columnCount, (int)columnSpacing,(int)itemHeight + RowSpacing);
            }

            contentWidth = itemWidth * columnCount + spacing * (columnCount - 1f);
            var inset = (containerWidth - contentWidth) / 2.0f;
            _recyclerView.SetPadding((int)inset, 0, (int)inset, 0);
            columnSpacing = spacing;

            return (columnCount, (int)columnSpacing,(int)itemHeight + RowSpacing);       
        }

        void OnScrollToRequested(object sender, ScrollToRequestedEventArgs e)
        {
            throw new NotImplementedException();
        }



        internal class CollectionViewSpanSizeLookup : GridLayoutManager.SpanSizeLookup
        {
            GridCollectionViewRenderer _parent;
            GridCollectionView _gridCollectionView => _parent._gridCollectionView;
            Context _context => _parent.Context;
            public int SpanSize { get; set; }
            public int SpanCount { get; set; }

            public CollectionViewSpanSizeLookup(GridCollectionViewRenderer parent)
            {
                _parent = parent;
                SpanIndexCacheEnabled = false;
            }


            public override int GetSpanSize(int position)
            {
                if (_parent._gridCollectionView.IsGroupingEnabled) {
                    var group = _parent.TemplatedItemsView.TemplatedItems.GetGroupIndexFromGlobal(position, out var row);
                    if (row == 0) {
                        return SpanSize;
                    }
                }

                return 1;
            }

            protected override void Dispose(bool disposing)
            {
                base.Dispose(disposing);
                if(disposing)
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
            Context _context;
            public GridCollectionItemDecoration(Context context, GridCollectionViewRenderer parentRenderer)
            {
                _parentRenderer = parentRenderer;
                _context = context;

            }

            public override void GetItemOffsets(Android.Graphics.Rect outRect, Android.Views.View view, RecyclerView parent, RecyclerView.State state)
            {
                var param = view.LayoutParameters as GridLayoutManager.LayoutParams;
                var spanIndex = param.SpanIndex;
                var spanSize = param.SpanSize;

                if(spanSize == _spanCount)
                {
                    if (_gridCollectionView.GridType == GridType.AutoSpacingGrid &&
                       _gridCollectionView.SpacingType == SpacingType.Center) {
                        var margin = _parentRenderer._recyclerView.PaddingLeft * -1;
                        var headparams = view.LayoutParameters as ViewGroup.MarginLayoutParams;
                        headparams.SetMargins(margin, headparams.TopMargin, margin, headparams.BottomMargin);
                        view.LayoutParameters = headparams;
                    }
                    return;
                }

                if(_spanCount == 1)
                {
                    return;
                }

                if(IncludeEdge)
                {
                    outRect.Left = _parentRenderer.ColumnSpacing - spanIndex * _parentRenderer.ColumnSpacing / _spanCount; // spacing - column * ((1f / spanCount) * spacing)
                    outRect.Right = (spanIndex + 1) * _parentRenderer.ColumnSpacing / _spanCount; // (column + 1) * ((1f / spanCount) * spacing)
                }
                else{
                    outRect.Left = spanIndex * _parentRenderer.ColumnSpacing / _spanCount; // column * ((1f / spanCount) * spacing)
                    outRect.Right = _parentRenderer.ColumnSpacing - (spanIndex + 1) * _parentRenderer.ColumnSpacing / _spanCount; // spacing - (column + 1) * ((1f /    spanCount) * spacing)
                }

           

                //if (spanIndex == 0) {
                //    outRect.Right = (int)Math.Round(_parentRenderer.ColumnBitSpacing * 2.0);
                //}
                //else if (spanIndex == _spanCount - 1) {
                //    outRect.Left = (int)Math.Round(_parentRenderer.ColumnBitSpacing * 2.0);
                //}
                //else {
                //    outRect.Right = (int)Math.Round(_parentRenderer.ColumnBitSpacing);
                //    outRect.Left = (int)Math.Round(_parentRenderer.ColumnBitSpacing);
                //}

                outRect.Bottom = _parentRenderer.RowSpacing;
            }
        }
    }
}
