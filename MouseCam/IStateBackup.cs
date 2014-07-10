using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Webcam
{
    interface IState
    {
        ValidLocation GetValidity();
        System.Drawing.Point GetLocation();
        void Execute(int[] HistHor, int[] HistVer);
    }
}
