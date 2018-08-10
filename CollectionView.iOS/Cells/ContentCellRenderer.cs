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
using CoreGraphics;

[assembly: ExportRenderer(typeof(ContentCell), typeof(ContentCellRenderer))]
namespace AiForms.Renderers.iOS.Cells
{
    public class ContentCellRenderer:IRegisterable
    {
        static readonly BindableProperty RealCellProperty = BindableProperty.CreateAttached("RealCell", typeof(UICollectionViewCell), typeof(Cell), null);

        EventHandler _onForceUpdateSizeRequested;

        public virtual UICollectionViewCell GetCell(ContentCell item, ViewCollectionCell reusableCell, UICollectionView cv)
        {
            Performance.Start(out string reference);

            if(reusableCell.ContentCell != null)
            {
                reusableCell.ContentCell.PropertyChanged -= ViewCellPropertyChanged;
            }

            item.PropertyChanged += ViewCellPropertyChanged;
            reusableCell.ContentCell = item;

            SetRealCell(item, reusableCell);

            //WireUpForceUpdateSizeRequested(item, cell, cv);

            UpdateBackground(reusableCell, item);
            UpdateIsEnabled(reusableCell,item);

            Performance.Stop(reference);

            return reusableCell;
        }

        static void UpdateIsEnabled(ViewCollectionCell cell, ViewCell viewCell)
        {
            cell.UserInteractionEnabled = viewCell.IsEnabled;
        }

        void ViewCellPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var viewCell = (ViewCell)sender;
            var realCell = (ViewCollectionCell)GetRealCell(viewCell);

            if (e.PropertyName == Cell.IsEnabledProperty.PropertyName)
                UpdateIsEnabled(realCell, viewCell);
        }

        public virtual void SetBackgroundColor(UICollectionViewCell tableViewCell, Cell cell, UIColor color)
        {
            tableViewCell.BackgroundColor = color;
        }

        protected void UpdateBackground(UICollectionViewCell tableViewCell, Cell cell)
        {
            if (cell.GetIsGroupHeader<ItemsView<Cell>, Cell>())
            {
                if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
                    SetBackgroundColor(tableViewCell, cell, new UIColor(247f / 255f, 247f / 255f, 247f / 255f, 1));
            }
            else
            {
                // Must be set to a solid color or blending issues will occur
                var bgColor = UIColor.White;

                var element = cell.RealParent as VisualElement;
                if (element != null)
                    bgColor = element.BackgroundColor == Color.Default ? bgColor : element.BackgroundColor.ToUIColor();

                SetBackgroundColor(tableViewCell, cell, bgColor);
            }
        }

        //protected void WireUpForceUpdateSizeRequested(ICellController cell, UICollectionViewCell nativeCell, UICollectionView tableView)
        //{
        //    cell.ForceUpdateSizeRequested -= _onForceUpdateSizeRequested;

        //    _onForceUpdateSizeRequested = (sender, e) =>
        //    {
        //        var index = tableView?.IndexPathForCell(nativeCell) ?? (sender as Cell)?.GetIndexPath();
        //        if (index != null)
        //            tableView.ReloadRows(new[] { index }, UICollectionViewRowAnimation.None);
        //    };

        //    cell.ForceUpdateSizeRequested += _onForceUpdateSizeRequested;
        //}

        internal static UICollectionViewCell GetRealCell(BindableObject cell)
        {
            return (UICollectionViewCell)cell.GetValue(RealCellProperty);
        }

        internal static void SetRealCell(BindableObject cell, UICollectionViewCell renderer)
        {
            cell.SetValue(RealCellProperty, renderer);
        }
    }


}
