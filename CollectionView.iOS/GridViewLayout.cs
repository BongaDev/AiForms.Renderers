using System;
using CoreGraphics;
using UIKit;
namespace AiForms.Renderers.iOS
{
    public class GridViewLayout:UICollectionViewFlowLayout
    {
        public GridViewLayout()
        {
        }

        public override CGSize ItemSize { get => base.ItemSize; set => base.ItemSize = value; }

        public override CGSize CollectionViewContentSize => base.CollectionViewContentSize;
    }
}
