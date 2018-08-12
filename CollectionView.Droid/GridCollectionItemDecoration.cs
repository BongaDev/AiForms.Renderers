using System;
using Android.Graphics;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Content;
using Xamarin.Forms.Platform.Android;

namespace AiForms.Renderers.Droid
{
    public class GridCollectionItemDecoration:RecyclerView.ItemDecoration
    {
        GridCollectionView _gridCollectionView;
        Context _context;
        public GridCollectionItemDecoration(Context context, GridCollectionView gridCollectionView)
        {
            _gridCollectionView = gridCollectionView;
            _context = context;
        }

        public override void GetItemOffsets(Rect outRect, View view, RecyclerView parent, RecyclerView.State state)
        {
            //base.GetItemOffsets(outRect, view, parent, state);
            if(parent.GetChildViewHolder(view).GetType() == typeof(HeaderViewHolder)){
                return;
            }

            var param = view.LayoutParameters as GridLayoutManager.LayoutParams;
            var spanIndex = param.SpanIndex;
            var spanSize = param.SpanSize;

            var spacingX = (int)_context.ToPixels(_gridCollectionView.ColumnSpacing);
            var spacingY = (int)_context.ToPixels(_gridCollectionView.RowSpacing);

            if(spanIndex == 0 && spanSize > 1)
            {
                outRect.Right = spacingX;
            }
        }
    }
}
