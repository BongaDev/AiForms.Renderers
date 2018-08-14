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
                ClearPropertyChanged(reusableCell);
            }


            reusableCell.ContentCell = item;

            SetUpPropertyChanged(reusableCell);

            SetRealCell(item, reusableCell);

            //WireUpForceUpdateSizeRequested(item, cell, cv);

            reusableCell.UpdateNativeCell();

            Performance.Stop(reference);

            return reusableCell;
        }

        protected virtual void SetUpPropertyChanged(ViewCollectionCell nativeCell)
        {
            var formsCell = nativeCell.ContentCell as ContentCell;
            var parentElement = formsCell?.Parent as CollectionView;

            formsCell.PropertyChanged += nativeCell.CellPropertyChanged;

            if (parentElement != null) {
                parentElement.PropertyChanged += nativeCell.ParentPropertyChanged;
            }
        }

        protected virtual void ClearPropertyChanged(ViewCollectionCell nativeCell)
        {
            var formsCell = nativeCell.ContentCell as ContentCell;
            var parentElement = formsCell.Parent as CollectionView;

            formsCell.PropertyChanged -= nativeCell.CellPropertyChanged;
            if (parentElement != null) {
                parentElement.PropertyChanged -= nativeCell.ParentPropertyChanged;
            }
        }

        //protected void UpdateBackground(UICollectionViewCell tableViewCell, Cell cell)
        //{
        //    if (cell.GetIsGroupHeader<ItemsView<Cell>, Cell>())
        //    {
        //        if (UIDevice.CurrentDevice.CheckSystemVersion(7, 0))
        //            SetBackgroundColor(tableViewCell, cell, new UIColor(247f / 255f, 247f / 255f, 247f / 255f, 1));
        //    }
        //    else
        //    {
        //        // Must be set to a solid color or blending issues will occur
        //        var bgColor = UIColor.White;

        //        var element = cell.RealParent as VisualElement;
        //        if (element != null)
        //            bgColor = element.BackgroundColor == Color.Default ? bgColor : element.BackgroundColor.ToUIColor();

        //        SetBackgroundColor(tableViewCell, cell, bgColor);
        //    }
        //}

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
