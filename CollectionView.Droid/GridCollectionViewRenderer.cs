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
                    _refresh.SetBackgroundColor(Android.Graphics.Color.Beige);
                    _recyclerView.SetBackgroundColor(Android.Graphics.Color.Red);
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
            if(changed){
                UpdateGridType(r - l);
            }

            base.OnLayout(changed, l, t, r, b);
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
                ColumnSpacing = (int)(Context.ToPixels(_gridCollectionView.ColumnSpacing) * (spanCount - 1) / spanCount / 2.0);
                RowHeight = GetUniformItemHeight(containerWidth, spanCount);
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

        int GetUniformItemHeight(int containerWidth, int columns)
        {
            float actualWidth = containerWidth - (float)_gridCollectionView.ColumnSpacing * (float)(columns - 1.0f);
            var itemWidth = (float)(actualWidth / (float)columns);
            var itemHeight = _gridCollectionView.IsSquare ? itemWidth : _gridCollectionView.ColumnHeight;
            return (int)(itemHeight + RowSpacing);
        }

        (int spanCount,int columnSpacing,int rowHeight) GetAutoSpacingItemSize(double containerWidth)
        {
            var columnWidth = Context.ToPixels(_gridCollectionView.ColumnWidth);
            var columnHeight = Context.ToPixels(_gridCollectionView.ColumnHeight);

            var itemWidth = Math.Min(containerWidth, columnWidth);
            var itemHeight = _gridCollectionView.IsSquare ? itemWidth : columnHeight;

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
            double bitSpace = 0;
            if(_gridCollectionView.SpacingType == SpacingType.Between)
            {
                contentWidth = itemWidth * columnCount;
                bitSpace = (containerWidth - contentWidth) / columnCount / 2.0f;
                return (columnCount, (int)bitSpace,(int)itemHeight + RowSpacing);
            }

            contentWidth = itemWidth * columnCount + spacing * (columnCount - 1f);
            var inset = (containerWidth - contentWidth) / 2.0f;
            _recyclerView.SetPadding((int)inset, 0, (int)inset, 0);
            bitSpace = spacing * (columnCount - 1) / columnCount / 2.0f;

            return (columnCount, (int)bitSpace,(int)itemHeight + RowSpacing);       
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
                SpanIndexCacheEnabled = true;
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
                //if(_gridCollectionView.GridType == GridType.UniformGrid)
                //{
                //    var orientation = _parent.Context.Resources.Configuration.Orientation;
                //    switch(orientation)
                //    {
                //        case Orientation.Portrait:
                //        case Orientation.Square:
                //        case Orientation.Undefined:
                //            return _gridCollectionView.PortraitColumns;
                //        case Orientation.Landscape:
                //            return _gridCollectionView.LandscapeColumns;
                //    }
                //}

                //return SpanSize;
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
            GridCollectionViewRenderer _parentRenderer;
            GridCollectionView _gridCollectionView => _parentRenderer._gridCollectionView;
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

                if (spanIndex == 0) {
                    outRect.Right = _parentRenderer.ColumnSpacing * 2;
                }
                else if (spanIndex == _spanCount - 1) {
                    outRect.Left = _parentRenderer.ColumnSpacing * 2;
                }
                else {
                    outRect.Right = _parentRenderer.ColumnSpacing;
                    outRect.Left = _parentRenderer.ColumnSpacing;
                }

                outRect.Bottom = _parentRenderer.RowSpacing;
            }
        }
    }
}
