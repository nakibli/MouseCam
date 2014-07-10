using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Webcam
{
    interface IState
    {
        ValidLocation GetValidity();
        System.Drawing.Point GetLocation();
        void Execute(Bitmap Image);
    }
}
