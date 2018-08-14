using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using AiForms.Renderers.Droid.Cells;
using Android.Content;
using Android.Support.V7.Widget;
using Android.Views;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.Android;
using AView = Android.Views.View;

namespace AiForms.Renderers.Droid
{
    [Android.Runtime.Preserve(AllMembers = true)]
    public class GridCollectionViewAdapter : RecyclerView.Adapter, AView.IOnClickListener, AView.IOnLongClickListener
    {
        public bool IsAttachedToWindow { get; set; }
        public const int DefaultGroupHeaderTemplateId = 1000;
        public const int DefaultItemTemplateId = 1;

        int _listCount = -1; // -1 we need to get count from the list
        int _dataTemplateIncrementer = 2;  // DataTemplate count is limited until 2-999
        int _headerTemplateIncrementer = 1001; // more than or equal to 1000 is group header.

        Context _context;
        RecyclerView _recyclerView;
        GridCollectionView _collectionView;
        GridCollectionViewRenderer _gridRenderer;
        List<ViewHolder> _viewHolders = new List<ViewHolder>();
        Dictionary<DataTemplate, int> _templateToId = new Dictionary<DataTemplate, int>();

        IListViewController Controller => _collectionView;
        ITemplatedItemsView<Cell> TemplatedItemsView => _collectionView;

        public GridCollectionViewAdapter(Context context, GridCollectionView gridCollectionView, RecyclerView recyclerView, GridCollectionViewRenderer renderer)
        {
            _context = context;
            _collectionView = gridCollectionView;
            _recyclerView = recyclerView;
            _gridRenderer = renderer;

            var templatedItems = ((ITemplatedItemsView<Cell>)gridCollectionView).TemplatedItems;
            templatedItems.CollectionChanged += OnCollectionChanged;
            templatedItems.GroupedCollectionChanged += OnGroupedCollectionChanged;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var templatedItems = TemplatedItemsView.TemplatedItems;
                templatedItems.CollectionChanged -= OnCollectionChanged;
                templatedItems.GroupedCollectionChanged -= OnGroupedCollectionChanged;

                _context = null;
                _recyclerView = null;
                _collectionView = null;
                _gridRenderer = null;
                _templateToId = null;

                foreach (var holder in _viewHolders)
                {
                    holder.Dispose();
                }

                _viewHolders = null;
            }
            base.Dispose(disposing);
        }

        public void OnClick(AView v)
        {
            var container = v as ContentCellContainer;
            var formsCell = container.Element as ContentCell;

            if (!formsCell.IsEnabled)
            {
                return;
            }


            var position = _recyclerView.GetChildAdapterPosition(v);
            var group = TemplatedItemsView.TemplatedItems.GetGroupIndexFromGlobal(position, out int row);

            if (_collectionView.ItemTapCommand != null && _collectionView.ItemTapCommand.CanExecute(formsCell.BindingContext))
            {
                _collectionView.ItemTapCommand.Execute(formsCell.BindingContext);
            }

            Controller.NotifyRowTapped(group, row - 1, formsCell);

        }

        public bool OnLongClick(AView v)
        {
            var container = v as ContentCellContainer;
            var formsCell = container.Element as ContentCell;

            if (!formsCell.IsEnabled)
            {
                return true;
            }

            if (_collectionView.ItemLongTapCommand == null)
            {
                return false;
            }
            if (_collectionView.ItemLongTapCommand != null && _collectionView.ItemLongTapCommand.CanExecute(formsCell.BindingContext))
            {
                _collectionView.ItemLongTapCommand.Execute(formsCell.BindingContext);
            }
            return true;
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

            if (IsAttachedToWindow)
                NotifyDataSetChanged();
            else
            {
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

        public override int ItemCount
        {
            get
            {
                if (_listCount == -1)
                {
                    var templatedItems = TemplatedItemsView.TemplatedItems;
                    int count = templatedItems.Count;

                    if (_collectionView.IsGroupingEnabled)
                    {
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
            else
            {
                group = TemplatedItemsView.TemplatedItems.GetGroupIndexFromGlobal(position, out row);
                if (row == 0)
                {
                    isHeader = true;
                    itemTemplate = _collectionView.GroupHeaderTemplate;
                    if (itemTemplate == null)
                        return DefaultGroupHeaderTemplateId;
                }
                else
                {
                    itemTemplate = _collectionView.ItemTemplate;
                    row--;
                }
            }

            if (itemTemplate == null)
                return DefaultItemTemplateId;

            if (itemTemplate is DataTemplateSelector selector)
            {
                object item = null;

                if (_collectionView.IsGroupingEnabled)
                {
                    if (TemplatedItemsView.TemplatedItems.GetGroup(group).ListProxy.Count > 0)
                        item = TemplatedItemsView.TemplatedItems.GetGroup(group).ListProxy[row];
                }
                else
                {
                    if (TemplatedItemsView.TemplatedItems.ListProxy.Count > 0)
                        item = TemplatedItemsView.TemplatedItems.ListProxy[position];
                }

                itemTemplate = selector.SelectTemplate(item, _collectionView);
            }

            // check again to guard against DataTemplateSelectors that return null
            if (itemTemplate == null)
                return DefaultItemTemplateId;

            if (!_templateToId.TryGetValue(itemTemplate, out int key))
            {
                if (isHeader)
                {
                    _headerTemplateIncrementer++;
                    key = _headerTemplateIncrementer;
                }
                else
                {
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
            if (viewType < DefaultGroupHeaderTemplateId)
            {
                viewHolder.ItemView.SetOnClickListener(this);
                viewHolder.ItemView.SetOnLongClickListener(this);
            }

            _viewHolders.Add(viewHolder);

            return viewHolder;
        }


        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            ContentCell cell = null;

            Performance.Start(out string reference);

            var container = holder.ItemView as ContentCellContainer;

            ListViewCachingStrategy cachingStrategy = Controller.CachingStrategy;
            if (cachingStrategy == ListViewCachingStrategy.RetainElement || container.IsEmpty)
            {
                cell = (ContentCell)GetCellFromPosition(position);
            }

            var cellIsBeingReused = false;
            if (container.ChildCount > 0)
            {
                cellIsBeingReused = true;
            }

            if (((cachingStrategy & ListViewCachingStrategy.RecycleElement) != 0) && !container.IsEmpty)
            {
                var boxedCell = container as INativeElementView;
                if (boxedCell == null)
                {
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

                if (_collectionView.IsGroupingEnabled)
                {
                    if (row == 0)
                        templatedList.UpdateHeader(cell, group);
                    else
                        templatedList.UpdateContent(cell, row - 1);
                }
                else
                    templatedList.UpdateContent(cell, row);

                cellController.SendAppearing();

                Performance.Stop(reference);

                _recyclerView.RequestLayout();
                _recyclerView.Invalidate();

                return;
            }

            AView view = GetCell(cell, container, _recyclerView, _context, _collectionView);


            Performance.Start(reference, "AddView");

            if (cellIsBeingReused)
            {
                if (container != view)
                {
                    holder.ItemView = view;
                }
            }
            else
                holder.ItemView = view;

            Performance.Stop(reference, "AddView");


            // EditTextのフォーカス問題のやつ 今はスルー 必要ならLinearLayoutを継承して作る
            // がこのRecyclerViewの用途でEditor使うことなんてないので優先度低
            //layout.ApplyTouchListenersToSpecialCells(cell);

            Performance.Stop(reference);
        }

        AView GetCell(ContentCell item, ContentCellContainer convertView, ViewGroup parent, Context context, Xamarin.Forms.View view)
        {

            var renderer = ContentCellRenderer.GetRenderer(item);
            if (renderer == null)
            {
                renderer = Registrar.Registered.GetHandlerForObject<ContentCellRenderer>(item);
            }


            return renderer.GetCell(item, convertView, parent, context);
        }


        public Cell GetCellFromPosition(int position)
        {
            var templatedItems = TemplatedItemsView.TemplatedItems;
            var templatedItemsCount = templatedItems.Count;

            if (!_collectionView.IsGroupingEnabled)
            {
                return templatedItems[position];
            }

            var i = 0;
            var global = 0;
            for (; i < templatedItemsCount; i++)
            {
                var group = templatedItems.GetGroup(i);

                if (global == position)
                {
                    //Always create a new cell if we are using the RecycleElement strategy
                    var recycleElement = (_collectionView.CachingStrategy & ListViewCachingStrategy.RecycleElement) != 0;
                    var headerCell = recycleElement ? GetNewGroupHeaderCell(group) : group.HeaderContent;
                    return headerCell;
                }

                global++;

                if (global + group.Count < position)
                {
                    global += group.Count;
                    continue;
                }

                for (var g = 0; g < group.Count; g++)
                {
                    if (global == position)
                    {
                        return group[g];
                    }

                    global++;
                }
            }

            return null;
        }

        Cell GetNewGroupHeaderCell(ITemplatedItemsList<Cell> group)
        {
            var groupHeaderCell = _collectionView.TemplatedItems.GroupHeaderTemplate?.CreateContent(group.ItemsSource, _collectionView) as Cell;

            if (groupHeaderCell != null)
            {
                groupHeaderCell.BindingContext = group.ItemsSource;
            }
            else
            {
                groupHeaderCell = new TextCell();
                groupHeaderCell.SetBinding(TextCell.TextProperty, nameof(group.Name));
                groupHeaderCell.BindingContext = group;
            }

            groupHeaderCell.Parent = _collectionView;
            groupHeaderCell.SetIsGroupHeader<ItemsView<Cell>, Cell>(true);
            return groupHeaderCell;
        }


    }

    [Android.Runtime.Preserve(AllMembers = true)]
    public class ViewHolder : RecyclerView.ViewHolder
    {
        public ViewHolder(AView view) : base(view) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ItemView?.Dispose();
                ItemView = null;
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
            if (disposing)
            {
                ItemView?.SetOnClickListener(null);
                ItemView?.SetOnLongClickListener(null);
                _renderer = null;
            }
            base.Dispose(disposing);
        }

    }
}
