using System;
using System.ComponentModel;
using Android.Content;
using Android.Widget;
using AView = Android.Views.View;
using Object = Java.Lang.Object;
using Xamarin.Forms.Internals;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using AiForms.Renderers;
using AiForms.Renderers.Droid.Cells;

[assembly: ExportRenderer(typeof(ContentCell), typeof(ContentCellRenderer))]
namespace AiForms.Renderers.Droid.Cells
{
    public class ContentCellRenderer:IRegisterable
    {
        static readonly PropertyChangedEventHandler PropertyChangedHandler = OnGlobalCellPropertyChanged;

        static readonly BindableProperty RendererProperty = BindableProperty.CreateAttached("Renderer", typeof(ContentCellRenderer), typeof(ContentCell), null);

        EventHandler _onForceUpdateSizeRequested;

        public View ParentView { get; set; }

        protected Cell Cell { get; set; }

        public AView GetCell(Cell item, AView convertView, Android.Views.ViewGroup parent, Context context)
        {
            Performance.Start(out string reference);

            Cell = item;
            Cell.PropertyChanged -= PropertyChangedHandler;
            var nativeView = convertView as ContentCellContainer;

            SetRenderer(Cell, this);

            if (!nativeView.IsEmpty) {
                Object tag = convertView.Tag;
                ContentCellRenderer renderer = (tag as RendererHolder)?.Renderer;

                Cell oldCell = renderer?.Cell;

                if (oldCell != null) {
                    ((ICellController)oldCell).SendDisappearing();

                    if (Cell != oldCell) {
                        SetRenderer(oldCell, null);
                    }
                }
            }

            AView view = GetCellCore(item, convertView, parent, context);

            WireUpForceUpdateSizeRequested(item, view);

            var holder = view.Tag as RendererHolder;
            if (holder == null)
                view.Tag = new RendererHolder(this);
            else
                holder.Renderer = this;

            Cell.PropertyChanged += PropertyChangedHandler;
            ((ICellController)Cell).SendAppearing();

            Performance.Stop(reference);

            return view;
        }

        protected virtual AView GetCellCore(Cell item, AView convertView, Android.Views.ViewGroup parent, Context context)
        {
            Performance.Start(out string reference, "GetCellCore");
            var cell = (ContentCell)item;

            var container = convertView as ContentCellContainer;
            if (!container.IsEmpty) {
                container.Update(cell);
                Performance.Stop(reference);
                return container;
            }

            BindableProperty unevenRows = null, rowHeight = null;

            unevenRows = Xamarin.Forms.ListView.HasUnevenRowsProperty;
            rowHeight = Xamarin.Forms.ListView.RowHeightProperty;

            if (cell.View == null)
                throw new InvalidOperationException($"ViewCell must have a {nameof(cell.View)}");

            IVisualElementRenderer view = Platform.CreateRendererWithContext(cell.View, context);
            Platform.SetRenderer(cell.View, view);
            cell.View.IsPlatformEnabled = true;
            container.SetCellData(view, cell, ParentView, unevenRows, rowHeight);

            Performance.Stop(reference, "GetCellCore");

            return container;
        }

        protected virtual void OnCellPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
        }

        protected void WireUpForceUpdateSizeRequested(Cell cell, AView nativeCell)
        {
            ICellController cellController = cell;
            cellController.ForceUpdateSizeRequested -= _onForceUpdateSizeRequested;

            _onForceUpdateSizeRequested = (sender, e) => {
                // RenderHeight may not be changed, but that's okay, since we
                // don't actually use the height argument in the OnMeasure override.
                nativeCell.Measure(nativeCell.Width, (int)cell.RenderHeight);
                nativeCell.SetMinimumHeight(nativeCell.MeasuredHeight);
                nativeCell.SetMinimumWidth(nativeCell.MeasuredWidth);
            };

            cellController.ForceUpdateSizeRequested += _onForceUpdateSizeRequested;
        }

        internal static ContentCellRenderer GetRenderer(BindableObject cell)
        {
            return (ContentCellRenderer)cell.GetValue(RendererProperty);
        }

        internal static void SetRenderer(BindableObject cell, ContentCellRenderer renderer)
        {
            cell.SetValue(RendererProperty, renderer);
        }

        static void OnGlobalCellPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var cell = (Cell)sender;
            ContentCellRenderer renderer = GetRenderer(cell);
            if (renderer == null) {
                cell.PropertyChanged -= PropertyChangedHandler;
                return;
            }

            renderer.OnCellPropertyChanged(sender, e);
        }

        class RendererHolder : Object
        {
            readonly WeakReference<ContentCellRenderer> _rendererRef;

            public RendererHolder(ContentCellRenderer renderer)
            {
                _rendererRef = new WeakReference<ContentCellRenderer>(renderer);
            }

            public ContentCellRenderer Renderer {
                get {
                    ContentCellRenderer renderer;
                    return _rendererRef.TryGetTarget(out renderer) ? renderer : null;
                }
                set { _rendererRef.SetTarget(value); }
            }
        }
    }
}
