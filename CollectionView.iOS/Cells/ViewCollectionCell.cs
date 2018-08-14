using System;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Internals;
using Xamarin.Forms.Platform.iOS;
using AiForms.Renderers;
using RectangleF = CoreGraphics.CGRect;
using SizeF = CoreGraphics.CGSize;
using System.ComponentModel;
using AiForms.Renderers.iOS.Cells;
using System.Reflection;
using CoreGraphics;
using System.Threading.Tasks;
using Foundation;
using System.Windows.Input;

namespace AiForms.Renderers.iOS.Cells
{
    [Foundation.Preserve(AllMembers = true)]
    public class ViewCollectionCell : UICollectionViewCell, INativeElementView
    {
        // Get internal members
        static BindableProperty RendererProperty = (BindableProperty)typeof(Platform).GetField("RendererProperty", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic).GetValue(null);
        static Type DefaultRenderer = typeof(Platform).Assembly.GetType("Xamarin.Forms.Platform.iOS.Platform+DefaultRenderer");
        static Type ModalWrapper = typeof(Platform).Assembly.GetType("Xamarin.Forms.Platform.iOS.ModalWrapper");
        static MethodInfo ModalWapperDispose = ModalWrapper.GetMethod("Dispose");

        WeakReference<IVisualElementRenderer> _rendererRef;
        ContentCell _contentCell;
        UIView _selectedForegroundView;

        Element INativeElementView.Element => ContentCell;
        CollectionView CellParent => ContentCell.Parent as CollectionView;
        internal bool SupressSeparator { get; set; }
        bool _disposed;

        public ViewCollectionCell(){}

        public ViewCollectionCell(IntPtr handle):base(handle)
        {
            _selectedForegroundView = new UIView();

            AddSubview(_selectedForegroundView);

            _selectedForegroundView.TranslatesAutoresizingMaskIntoConstraints = false;
            _selectedForegroundView.TopAnchor.ConstraintEqualTo(TopAnchor).Active = true;
            _selectedForegroundView.LeftAnchor.ConstraintEqualTo(LeftAnchor).Active = true;
            _selectedForegroundView.BottomAnchor.ConstraintEqualTo(BottomAnchor).Active = true;
            _selectedForegroundView.RightAnchor.ConstraintEqualTo(RightAnchor).Active = true;

            _selectedForegroundView.Alpha = 0;
        }

        public ContentCell ContentCell
        {
            get { return _contentCell; }
            set
            {
                if (_contentCell == value)
                    return;
                UpdateCell(value);
            }
        }

        public override void PrepareForReuse()
        {
            base.PrepareForReuse();
        }

        public override void LayoutSubviews()
        {
            if(ContentCell == null)
            {
                return;
            }
            Performance.Start(out string reference);

            //This sets the content views frame.
            base.LayoutSubviews();

            var contentFrame = ContentView.Frame;
            var view = ContentCell.View;

            Layout.LayoutChildIntoBoundingRegion(view, contentFrame.ToRectangle());

            if (_rendererRef == null)
                return;

            IVisualElementRenderer renderer;
            if (_rendererRef.TryGetTarget(out renderer))
                renderer.NativeView.Frame = view.Bounds.ToRectangleF();

            Performance.Stop(reference);
        }

        public virtual void CellPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == Cell.IsEnabledProperty.PropertyName)
                UpdateIsEnabled();
        }

        public virtual void ParentPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == CollectionView.SelectedColorProperty.PropertyName)
                UpdateSelectedColor();
            
        }

        public virtual void UpdateNativeCell()
        {
            BackgroundColor = UIColor.Clear;
            UpdateSelectedColor();
            UpdateIsEnabled();
        }

        void UpdateSelectedColor()
        {
            if (CellParent != null && !CellParent.SelectedColor.IsDefault) {
                _selectedForegroundView.BackgroundColor = CellParent.SelectedColor.ToUIColor();
            }
        }


        public virtual async void SelectedAnimation(double duration, double start = 1, double end = 0)
        {
            //BringSubviewToFront(_selectedForegroundView);
            //_selectedForegroundView.Hidden = false;
            _selectedForegroundView.Alpha = (float)start;
            await AnimateAsync(duration, () => {
                _selectedForegroundView.Alpha = (float)end;
            });
            //_selectedForegroundView.Hidden = true;
        }



        protected virtual void UpdateIsEnabled()
        {
            UserInteractionEnabled = ContentCell.IsEnabled;
        }

        //public override SizeF SizeThatFits(SizeF size)
        //{
        //    Performance.Start(out string reference);

        //    IVisualElementRenderer renderer;
        //    if (!_rendererRef.TryGetTarget(out renderer))
        //        return base.SizeThatFits(size);

        //    if (renderer.Element == null)
        //        return SizeF.Empty;

        //    double width = size.Width;
        //    var height = size.Height > 0 ? size.Height : double.PositiveInfinity;
        //    var result = renderer.Element.Measure(width, height, MeasureFlags.IncludeMargins);

        //    // make sure to add in the separator if needed
        //    var finalheight = (float)result.Request.Height + (SupressSeparator ? 0f : 1f) / UIScreen.MainScreen.Scale;

        //    Performance.Stop(reference);

        //    return new SizeF(size.Width, finalheight);
        //}

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                ContentCell.PropertyChanged -= CellPropertyChanged;
                CellParent.PropertyChanged -= ParentPropertyChanged;

                IVisualElementRenderer renderer;
                if (_rendererRef != null && _rendererRef.TryGetTarget(out renderer) && renderer.Element != null)
                {
                    var platform = renderer.Element.Platform as Platform;
                    if (platform != null)
                        DisposeModelAndChildrenRenderers(renderer.Element);

                    _rendererRef = null;
                }

                _contentCell = null;
            }

            _disposed = true;

            base.Dispose(disposing);
        }



        IVisualElementRenderer GetNewRenderer()
        {
            if (_contentCell.View == null)
                throw new InvalidOperationException($"ViewCell must have a {nameof(_contentCell.View)}");

            var newRenderer = Platform.CreateRenderer(_contentCell.View);
            _rendererRef = new WeakReference<IVisualElementRenderer>(newRenderer);
            ContentView.AddSubview(newRenderer.NativeView);
            return newRenderer;
        }

        void UpdateCell(ContentCell cell)
        {
            Performance.Start(out string reference);

            if (_contentCell != null)
                Device.BeginInvokeOnMainThread(_contentCell.SendDisappearing);

            this._contentCell = cell;
            _contentCell = cell;

            Device.BeginInvokeOnMainThread(_contentCell.SendAppearing);

            IVisualElementRenderer renderer;
            if (_rendererRef == null || !_rendererRef.TryGetTarget(out renderer))
                renderer = GetNewRenderer();
            else
            {
                if (renderer.Element != null && renderer == Platform.GetRenderer(renderer.Element))
                    renderer.Element.ClearValue(RendererProperty);

                var type = Xamarin.Forms.Internals.Registrar.Registered.GetHandlerTypeForObject(this._contentCell.View);
                var reflectableType = renderer as System.Reflection.IReflectableType;
                var rendererType = reflectableType != null ? reflectableType.GetTypeInfo().AsType() : renderer.GetType();
                if (rendererType == type || (renderer.GetType() == DefaultRenderer) && type == null)
                    renderer.SetElement(this._contentCell.View);
                else
                {
                    //when cells are getting reused the element could be already set to another cell
                    //so we should dispose based on the renderer and not the renderer.Element
                    var platform = renderer.Element.Platform as Platform;
                    DisposeRendererAndChildren(renderer);
                    renderer = GetNewRenderer();
                }
            }

            Platform.SetRenderer(this._contentCell.View, renderer);
            Performance.Stop(reference);
        }

        void DisposeModelAndChildrenRenderers(Element view)
        {
            IVisualElementRenderer renderer;
            foreach (VisualElement child in view.Descendants())
            {
                renderer = Platform.GetRenderer(child);
                child.ClearValue(RendererProperty);

                if (renderer != null)
                {
                    renderer.NativeView.RemoveFromSuperview();
                    renderer.Dispose();
                }
            }

            renderer = Platform.GetRenderer((VisualElement)view);
            if (renderer != null)
            {
                if (renderer.ViewController != null)
                {
                    if(renderer.ViewController.ParentViewController.GetType() == ModalWrapper)
                    {
                        var modalWrapper = Convert.ChangeType(renderer.ViewController.ParentViewController, ModalWrapper);
                        ModalWapperDispose.Invoke(modalWrapper, new object[]{});
                    }
                }

                renderer.NativeView.RemoveFromSuperview();
                renderer.Dispose();
            }

            view.ClearValue(RendererProperty);
        }

        void DisposeRendererAndChildren(IVisualElementRenderer rendererToRemove)
        {
            if (rendererToRemove == null)
                return;

            if (rendererToRemove.Element != null && Platform.GetRenderer(rendererToRemove.Element) == rendererToRemove)
                rendererToRemove.Element.ClearValue(RendererProperty);

            var subviews = rendererToRemove.NativeView.Subviews;
            for (var i = 0; i < subviews.Length; i++)
            {
                var childRenderer = subviews[i] as IVisualElementRenderer;
                if (childRenderer != null)
                    DisposeRendererAndChildren(childRenderer);
            }

            rendererToRemove.NativeView.RemoveFromSuperview();
            rendererToRemove.Dispose();
        }
    }
}
