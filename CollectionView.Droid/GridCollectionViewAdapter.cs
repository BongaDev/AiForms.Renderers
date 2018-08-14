using System;
using Android.Support.V7.Widget;
using Android.Views;
using AView = Android.Views.View;
using Android.Content;
using System.Collections.Generic;
using Xamarin.Forms;
using System.Collections.Specialized;
using Android.Widget;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.Android;
using AiForms.Renderers.Droid.Cells;

namespace AiForms.Renderers.Droid
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class GridCollectionViewAdapter:RecyclerView.Adapter,AView.IOnClickListener,AView.IOnLongClickListener
    {
        public bool IsAttachedToWindow { get; set; }

        static readonly object DefaultItemTypeOrDataTemplate = new object();
        public const int DefaultGroupHeaderTemplateId = 1000;
        public const int DefaultItemTemplateId = 1;
        int _listCount = -1; // -1 we need to get count from the list
        Dictionary<object, Cell> _prototypicalCellByTypeOrDataTemplate;
        int _dataTemplateIncrementer = 2;  // DataTemplate count is limited until 2-999
        int _headerTemplateIncrementer = 1001; // more than or equal to 1000 is group header.

        Context _context;
        RecyclerView _recyclerView;
        GridCollectionView _collectionView;
        GridCollectionViewRenderer _gridRenderer;

        IListViewController Controller => _collectionView;
        ITemplatedItemsView<Cell> TemplatedItemsView => _collectionView;
        List<ViewHolder> _viewHolders = new List<ViewHolder>();

        readonly Dictionary<DataTemplate, int> _templateToId = new Dictionary<DataTemplate, int>();

        public GridCollectionViewAdapter(Context context,GridCollectionView gridCollectionView,RecyclerView recyclerView,GridCollectionViewRenderer renderer)
        {
            _context = context;
            _collectionView = gridCollectionView;
            _recyclerView = recyclerView;
            _gridRenderer = renderer;

            _prototypicalCellByTypeOrDataTemplate = new Dictionary<object, Cell>();

            var templatedItems = ((ITemplatedItemsView<Cell>)gridCollectionView).TemplatedItems;
            templatedItems.CollectionChanged += OnCollectionChanged;
            templatedItems.GroupedCollectionChanged += OnGroupedCollectionChanged;
        }

        void OnGroupedCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnDataChanged();
        }

        void OnCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            OnDataChanged();
        }

        public void OnDataChanged()
        {
            InvalidateCount();
            //if (ActionModeContext != null && !TemplatedItemsView.TemplatedItems.Contains(ActionModeContext))
                //CloseContextActions();

            if (IsAttachedToWindow)
                NotifyDataSetChanged();
            else {
                // In a TabbedPage page with two pages, Page A and Page B with ListView, if A changes B's ListView,
                // we need to reset the ListView's adapter to reflect the changes on page B
                // If there header and footer are present at the reset time of the adapter
                // they will be DOUBLE added to the ViewGround (the ListView) causing indexes to be off by one. 

                // 意味不なので放置
                //_realListView.RemoveHeaderView(HeaderView);
                //_realListView.RemoveFooterView(FooterView);
                //_realListView.Adapter = _realListView.Adapter;
                //_realListView.AddHeaderView(HeaderView);
                //_realListView.AddFooterView(FooterView);
            }
        }


        protected virtual void InvalidateCount()
        {
            _listCount = -1;
        }

        void UpdateItems(NotifyCollectionChangedEventArgs e, int section, bool resetWhenGrouped)
        {
            // TODO: セルの更新通知、少し後で実装する
            // https://qiita.com/sutchan/items/e7d68a208f71b3b95f8f
            // https://qiita.com/ralph/items/e56844976117d9883e34
            // adapterのpositionとsection/rowをキャッシュする必要がありそう
        }

        public override int ItemCount{
            get{             
                if (_listCount == -1) {
                    var templatedItems = TemplatedItemsView.TemplatedItems;
                    int count = templatedItems.Count;

                    if (_collectionView.IsGroupingEnabled) {
                        for (var i = 0; i < templatedItems.Count; i++)
                            count += templatedItems.GetGroup(i).Count;
                    }

                    _listCount = count;
                }
                return _listCount;
            }
        }

        public override long GetItemId(int position)
        {
            return position;
        }


        public override int GetItemViewType(int position)
        {
            var group = 0;
            var row = 0;
            bool isHeader = false;
            DataTemplate itemTemplate;
            if (!_collectionView.IsGroupingEnabled)
                itemTemplate = _collectionView.ItemTemplate;
            else {
                group = TemplatedItemsView.TemplatedItems.GetGroupIndexFromGlobal(position, out row);
                if (row == 0) {
                    isHeader = true;
                    itemTemplate = _collectionView.GroupHeaderTemplate;
                    if (itemTemplate == null)
                        return DefaultGroupHeaderTemplateId;
                }
                else {
                    itemTemplate = _collectionView.ItemTemplate;
                    row--;
                }
            }

            if (itemTemplate == null)
                return DefaultItemTemplateId;

            if (itemTemplate is DataTemplateSelector selector) {
                object item = null;

                if (_collectionView.IsGroupingEnabled) {
                    if (TemplatedItemsView.TemplatedItems.GetGroup(group).ListProxy.Count > 0)
                        item = TemplatedItemsView.TemplatedItems.GetGroup(group).ListProxy[row];
                }
                else {
                    if (TemplatedItemsView.TemplatedItems.ListProxy.Count > 0)
                        item = TemplatedItemsView.TemplatedItems.ListProxy[position];
                }

                itemTemplate = selector.SelectTemplate(item, _collectionView);
            }

            // check again to guard against DataTemplateSelectors that return null
            if (itemTemplate == null)
                return DefaultItemTemplateId;

            if (!_templateToId.TryGetValue(itemTemplate, out int key)) {
                if(isHeader){
                    _headerTemplateIncrementer++;
                    key = _headerTemplateIncrementer;
                }
                else{
                    _dataTemplateIncrementer++;
                    key = _dataTemplateIncrementer;
                }
                _templateToId[itemTemplate] = key;
            }

            return key;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            var container = new ContentCellContainer(_context);
            var viewHolder = new ContentViewHolder(_gridRenderer, container);
            if(viewType < DefaultGroupHeaderTemplateId)
            {
                viewHolder.ItemView.SetOnClickListener(this);
            }           

            _viewHolders.Add(viewHolder);

            return viewHolder;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            ContentCell cell = null;
            Android.Views.View nativeCell = null;

            Performance.Start(out string reference);

            var container = holder.ItemView as ContentCellContainer;


            ListViewCachingStrategy cachingStrategy = Controller.CachingStrategy;
            //var nextCellIsHeader = false;
            if (cachingStrategy == ListViewCachingStrategy.RetainElement || container.IsEmpty) {
                cell = (ContentCell)GetCellFromPosition(position);
            }

            var cellIsBeingReused = false;
            if (container.ChildCount > 0) {
                cellIsBeingReused = true;

                //nativeCell = container.GetChildAt(0);
            }

            //if (holder.ItemViewType < DefaultGroupHeaderTemplateId) {
            //    container.RowHeight = _gridRenderer.RowHeight;
            //    container.LayoutParameters.Height = _gridRenderer.RowHeight;
            //    container.SetMinimumHeight(_gridRenderer.RowHeight);
            //    container.RequestLayout();
            //}

            // いらんっぽい
            //else {
            //    layout = new ConditionalFocusLayout(_context) { Orientation = Orientation.Vertical };
            //    _layoutsCreated.Add(layout);
            //}

            if (((cachingStrategy & ListViewCachingStrategy.RecycleElement) != 0) && !container.IsEmpty) {
                var boxedCell = container as INativeElementView;
                if (boxedCell == null) {
                    throw new InvalidOperationException($"View for cell must implement {nameof(INativeElementView)} to enable recycling.");
                }
                cell = (ContentCell)boxedCell.Element;

                // We are going to re-set the Platform here because in some cases (headers mostly) its possible this is unset and
                // when the binding context gets updated the measure passes will all fail. By applying this here the Update call
                // further down will result in correct layouts.
                cell.Platform = _collectionView.Platform;

                ICellController cellController = cell;
                cellController.SendDisappearing();

                int row = position;
                var group = 0;
                var templatedItems = TemplatedItemsView.TemplatedItems;
                if (_collectionView.IsGroupingEnabled)
                    group = templatedItems.GetGroupIndexFromGlobal(position, out row);

                var templatedList = templatedItems.GetGroup(group);

                if (_collectionView.IsGroupingEnabled) {
                    if (row == 0)
                        templatedList.UpdateHeader(cell, group);
                    else
                        templatedList.UpdateContent(cell, row - 1);
                }
                else
                    templatedList.UpdateContent(cell, row);

                cellController.SendAppearing();

                // コンテキストメニュー関連なので多分いらん
                //if (cell.BindingContext == ActionModeObject) {
                //    ActionModeContext = cell;
                //    ContextView = layout;
                //}

                // Selected関連 今はスルー
                //if (ReferenceEquals(_collectionView.SelectedItem, cell.BindingContext))
                //    Select(_collectionView.IsGroupingEnabled ? row - 1 : row, layout);
                //else if (cell.BindingContext == ActionModeObject)
                //    SetSelectedBackground(layout, true);
                //else
                    //UnsetSelectedBackground(layout);

                Performance.Stop(reference);

                _recyclerView.RequestLayout();
                _recyclerView.Invalidate();


                return;
            }

            AView view = GetCell(cell, container, _recyclerView, _context, _collectionView);


            Performance.Start(reference, "AddView");

            if (cellIsBeingReused) {
                if (container != view) {
                    //container.RemoveViewAt(0);
                    //container.AddView(view, 0);
                    holder.ItemView = view;
                }
            }
            else
                //container.AddView(view, 0);
                holder.ItemView = view;

            Performance.Stop(reference, "AddView");

            //bool isHeader = cell.GetIsGroupHeader<ItemsView<Cell>, Cell>();

            //AView bline;

            // 選択関連
            //if ((bool)cell.GetValue(IsSelectedProperty))
            //    Select(position, layout);
            //else
                //UnsetSelectedBackground(layout);

            // EditTextのフォーカス問題のやつ 今はスルー 必要ならLinearLayoutを継承して作る
            // がこのRecyclerViewの用途でEditor使うことなんてないので優先度低
            //layout.ApplyTouchListenersToSpecialCells(cell);

            Performance.Stop(reference);
        }

        AView GetCell(Cell item,AView convertView,ViewGroup parent,Context context, Xamarin.Forms.View view)
        {

            var renderer = ContentCellRenderer.GetRenderer(item);
            if (renderer == null) {
                renderer = Registrar.Registered.GetHandlerForObject<ContentCellRenderer>(item);
                renderer.ParentView = view;
            }

            return renderer.GetCell(item, convertView, parent, context);
        }


        public Cell GetCellFromPosition(int position)
        {
            var templatedItems = TemplatedItemsView.TemplatedItems;
            var templatedItemsCount = templatedItems.Count;

            if (!_collectionView.IsGroupingEnabled) {
                return templatedItems[position];
            }

            var i = 0;
            var global = 0;
            for (; i < templatedItemsCount; i++) {
                var group = templatedItems.GetGroup(i);

                if (global == position) {
                    //Always create a new cell if we are using the RecycleElement strategy
                    var recycleElement = (_collectionView.CachingStrategy & ListViewCachingStrategy.RecycleElement) != 0;
                    var headerCell = recycleElement ? GetNewGroupHeaderCell(group) : group.HeaderContent;
                    return headerCell;
                }

                global++;

                if (global + group.Count < position) {
                    global += group.Count;
                    continue;
                }

                for (var g = 0; g < group.Count; g++) {
                    if (global == position) {
                        return group[g];
                    }

                    global++;
                }
            }

            return null;
        }

        Cell GetNewGroupHeaderCell(ITemplatedItemsList<Cell> group)
        {
            var groupHeaderCell = _collectionView.TemplatedItems.GroupHeaderTemplate?.CreateContent(group.ItemsSource,_collectionView) as Cell;

            if (groupHeaderCell != null) {
                groupHeaderCell.BindingContext = group.ItemsSource;
            }
            else {
                groupHeaderCell = new TextCell();
                groupHeaderCell.SetBinding(TextCell.TextProperty, nameof(group.Name));
                groupHeaderCell.BindingContext = group;
            }

            groupHeaderCell.Parent = _collectionView;
            groupHeaderCell.SetIsGroupHeader<ItemsView<Cell>, Cell>(true);
            return groupHeaderCell;
        }

        void AView.IOnClickListener.OnClick(AView v)
        {
            var container = v as ContentCellContainer;
            var formsCell = container.Element as ContentCell;

            if(!formsCell.IsEnabled){
                return;
            }


            var position = _recyclerView.GetChildAdapterPosition(v);
            var group = TemplatedItemsView.TemplatedItems.GetGroupIndexFromGlobal(position, out int row);

            Controller.NotifyRowTapped(group, row - 1, formsCell);

        }

        public bool OnLongClick(AView v)
        {
            
            return false;
        }
    }

    [Android.Runtime.Preserve(AllMembers = true)]
    public class ViewHolder : RecyclerView.ViewHolder
    {
        public ViewHolder(AView view) : base(view) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                ItemView?.Dispose();
                ItemView = null;
            }
            base.Dispose(disposing);
        }
    }


    [Android.Runtime.Preserve(AllMembers = true)]
    internal class HeaderViewHolder : ViewHolder
    {
        public HeaderViewHolder(AView view,double height) : base(view)
        {
            var container = view as ContentCellContainer;
            container.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, (int)height);

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                
            }
            base.Dispose(disposing);
        }
    }

    [Android.Runtime.Preserve(AllMembers = true)]
    public class ContentViewHolder : ViewHolder
    {
        GridCollectionViewRenderer _renderer;
        public bool IsHeader => ItemViewType >= GridCollectionViewAdapter.DefaultGroupHeaderTemplateId;
        public int RowHeight => IsHeader ? _renderer.GroupHeaderHeight : _renderer.RowHeight;

        public ContentViewHolder(GridCollectionViewRenderer renderer, AView view) : base(view)
        {
            _renderer = renderer;

            view.LayoutParameters = new ViewGroup.LayoutParams(ViewGroup.LayoutParams.MatchParent, RowHeight);
            var container = view as ContentCellContainer;
            container.ViewHolder = this;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) {
                ItemView.SetOnClickListener(null);
                _renderer = null;
            }
            base.Dispose(disposing);
        }

    }
}
