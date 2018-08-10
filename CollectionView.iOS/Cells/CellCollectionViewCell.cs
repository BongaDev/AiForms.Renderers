using System;
using System.ComponentModel;
using UIKit;
using Xamarin.Forms;
using Xamarin.Forms.Platform.iOS;

namespace AiForms.Renderers.iOS.Cells
{
    public class CellCollectionViewCell:UICollectionViewCell,INativeElementView
    {
        Cell _cell;
        public Action<object, PropertyChangedEventArgs> PropertyChanged;
        bool _disposed;

        public CellCollectionViewCell()
        {
        }

        public Cell Cell
        {
            get { return _cell; }
            set
            {
                if (this._cell == value)
                    return;

                if (_cell != null)
                    Device.BeginInvokeOnMainThread(_cell.SendDisappearing);

                this._cell = value;
                _cell = value;

                if (_cell != null)
                    Device.BeginInvokeOnMainThread(_cell.SendAppearing);
            }
        }

        public Element Element => Cell;

        public void HandlePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, e);
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed)
                return;

            if (disposing)
            {
                PropertyChanged = null;
                _cell = null;
            }

            _disposed = true;

            base.Dispose(disposing);
        }
    }
}
