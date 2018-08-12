using System;
using System.Linq;
using Android.Content;
using Android.Views;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.Android;

namespace AiForms.Renderers.Droid.Cells
{
    public class ContentCellContainer:ViewGroup, INativeElementView
    {
        // Get internal members
        static Type DefaultRenderer = typeof(Platform).Assembly.GetType("Xamarin.Forms.Platform.Android.Platform+DefaultRenderer");

        public ContentViewHolder ViewHolder { get; set; }

        Xamarin.Forms.View _parent;
        BindableProperty _rowHeight;
        BindableProperty _unevenRows;
        IVisualElementRenderer _view;
        ContentCell _contentCell;

        GestureDetector _longPressGestureDetector;
        ListViewRenderer _listViewRenderer;
        bool _watchForLongPress;

        public bool IsEmpty { get; private set; }

        public ContentCellContainer(Context context):base(context)
        {
            IsEmpty = true;
        }

        public void SetCellData(IVisualElementRenderer view, ContentCell contentCell,
                                    Xamarin.Forms.View parent,
                                    BindableProperty unevenRows, BindableProperty rowHeight)
        {
            IsEmpty = false;
            _view = view;
            _parent = parent;
            _unevenRows = unevenRows;
            _rowHeight = rowHeight;
            _contentCell = contentCell;
            AddView(view.View);
            UpdateIsEnabled();
        }

        protected bool ParentHasUnevenRows {
            get { return (bool)_parent.GetValue(_unevenRows); }
        }

        protected int ParentRowHeight {
            get { return (int)_parent.GetValue(_rowHeight); }
        }

        public Element Element {
            get { return _contentCell; }
        }

        public override bool OnInterceptTouchEvent(MotionEvent ev)
        {
            if (!Enabled)
                return true;

            return base.OnInterceptTouchEvent(ev);
        }

        //public override bool DispatchTouchEvent(MotionEvent e)
        //{
        //    // Give the child controls a shot at the event (in case they've get Tap gestures and such
        //    var handled = base.DispatchTouchEvent(e);

        //    if (_watchForLongPress) {
        //        // Feed the gestue through the LongPress detector; for this to wor we *must* return true 
        //        // afterward (or the LPGD goes nuts and immediately fires onLongPress)
        //        LongPressGestureDetector.OnTouchEvent(e);
        //        return true;
        //    }

        //    return handled;
        //}



        public void Update(ContentCell cell)
        {
            Performance.Start(out string reference);
            var renderer = GetChildAt(0) as IVisualElementRenderer;
            var viewHandlerType = Registrar.Registered.GetHandlerTypeForObject(cell.View) ?? DefaultRenderer;
            var reflectableType = renderer as System.Reflection.IReflectableType;
            var rendererType = reflectableType != null ? reflectableType.GetTypeInfo().AsType() : (renderer != null ? renderer.GetType() : typeof(System.Object));
            if (renderer != null && rendererType == viewHandlerType) {
                Performance.Start(reference, "Reuse");
                _contentCell = cell;

                cell.View.DisableLayout = true;
                foreach (VisualElement c in cell.View.Descendants())
                    c.DisableLayout = true;

                Performance.Start(reference, "Reuse.SetElement");
                renderer.SetElement(cell.View);
                Performance.Stop(reference, "Reuse.SetElement");

                Platform.SetRenderer(cell.View, _view);

                cell.View.DisableLayout = false;
                foreach (VisualElement c in cell.View.Descendants())
                    c.DisableLayout = false;

                var viewAsLayout = cell.View as Layout;
                if (viewAsLayout != null)
                    viewAsLayout.ForceLayout();

                Invalidate();

                Performance.Stop(reference, "Reuse");
                Performance.Stop(reference);
                return;
            }

            RemoveView(_view.View);
            Platform.SetRenderer(_contentCell.View, null);
            _contentCell.View.IsPlatformEnabled = false;
            _view.View.Dispose();

            _contentCell = cell;
            _view = Platform.CreateRendererWithContext(_contentCell.View, Context);

            Platform.SetRenderer(_contentCell.View, _view);
            AddView(_view.View);

            UpdateIsEnabled();
            //UpdateWatchForLongPress();

            Performance.Stop(reference);
        }

        public void UpdateIsEnabled()
        {
            Enabled = _contentCell.IsEnabled;
        }

        protected override void OnLayout(bool changed, int l, int t, int r, int b)
        {
            if(IsEmpty)
            {
                return;
            }
            Performance.Start(out string reference);

            double width = Context.FromPixels(r - l);
            double height = Context.FromPixels(b - t);

            Performance.Start(reference, "Element.Layout");
            var orientation = Context.Resources.Configuration.Orientation;
            System.Diagnostics.Debug.WriteLine($"{orientation} BoxSize:{width} / {height}");
            Xamarin.Forms.Layout.LayoutChildIntoBoundingRegion(_view.Element, new Rectangle(0, 0, width, height));
            Performance.Stop(reference, "Element.Layout");

            _view.UpdateLayout();
            Performance.Stop(reference);
        }

        protected override void OnMeasure(int widthMeasureSpec, int heightMeasureSpec)
        {
            Performance.Start(out string reference);

            int width = MeasureSpec.GetSize(widthMeasureSpec);

            SetMeasuredDimension(width, ViewHolder.RowHeight);

            Performance.Stop(reference);
        }

        //void UpdateWatchForLongPress()
        //{
        //    var vw = _view.Element as Xamarin.Forms.View;
        //    if (vw == null) {
        //        return;
        //    }

        //    // If the view cell has any context actions and the View itself has any Tap Gestures, they're going
        //    // to conflict with one another - the Tap Gesture handling will prevent the ListViewAdapter's
        //    // LongClick handling from happening. So we need to watch locally for LongPress and if we see it,
        //    // trigger the LongClick manually.
        //    _watchForLongPress = _contentCell.ContextActions.Count > 0
        //        && HasTapGestureRecognizers(vw);
        //}

        static bool HasTapGestureRecognizers(Xamarin.Forms.View view)
        {
            return view.GestureRecognizers.Any(t => t is TapGestureRecognizer)
                || view.LogicalChildren.OfType<Xamarin.Forms.View>().Any(HasTapGestureRecognizers);
        }

        //void TriggerLongClick()
        //{
        //    ListViewRenderer?.LongClickOn(this);
        //}

        //internal class LongPressGestureListener : Java.Lang.Object, GestureDetector.IOnGestureListener
        //{
        //    readonly Action _onLongClick;

        //    internal LongPressGestureListener(Action onLongClick)
        //    {
        //        _onLongClick = onLongClick;
        //    }

        //    internal LongPressGestureListener(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership)
        //    {
        //    }

        //    public bool OnDown(MotionEvent e)
        //    {
        //        return true;
        //    }

        //    public bool OnFling(MotionEvent e1, MotionEvent e2, float velocityX, float velocityY)
        //    {
        //        return false;
        //    }

        //    public void OnLongPress(MotionEvent e)
        //    {
        //        _onLongClick();
        //    }

        //    public bool OnScroll(MotionEvent e1, MotionEvent e2, float distanceX, float distanceY)
        //    {
        //        return false;
        //    }

        //    public void OnShowPress(MotionEvent e)
        //    {

        //    }

        //    public bool OnSingleTapUp(MotionEvent e)
        //    {
        //        return false;
        //    }
        //}
    }
}
