using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AForge.Imaging;
using System.Drawing;

namespace Webcam
{
    enum ValidLocation {FALSE = 0 , TRUE = 1};
    class CState : IState
    {
        public ValidLocation Validity; 
        public int CurrX;
        public int CurrY;

        public CState()
        {
            Validity = ValidLocation.FALSE;
            CurrX = 0;
            CurrY = 0;
        }

        #region IState Members

        public virtual void Execute(Bitmap Image)
        {
            return;
        }

        public virtual ValidLocation GetValidity()
        {
            return Validity;
        }

        public virtual System.Drawing.Point GetLocation()
        {
            return new System.Drawing.Point(CurrX,CurrY);
        }
        #endregion
    }

    class CSearchState : CState
    {
        public int MaxCycles = 2;
        public int TargetFoundCycle;
        public int GateLength = 120;
        public int ThreshY = 15;
        public int ThreshX = 30;
        public int ChangedPixelsX = 5;
        public CSearchState()
        {
            TargetFoundCycle = 0;
        }
        public override void Execute(Bitmap Image)
        {
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.MinHeight = 75;
            blobCounter.MinWidth = 75;
            blobCounter.CoupledSizeFiltering = true;
            blobCounter.ProcessImage(Image);
            Blob[] blobs = blobCounter.GetObjects(Image);
            int maxSize = 0;
            Blob maxObject = new Blob(0, new Rectangle(0, 0, 0, 0));
            // find biggest blob
            if (blobs != null)
            {
                foreach (Blob blob in blobs)
                {
                    int blobSize = blob.Rectangle.Width * blob.Rectangle.Height;

                    if (blobSize > maxSize)
                    {
                        maxSize = blobSize;
                        maxObject = blob;
                    }
                }
                if (maxSize > 100)
                {
                    if (Validity == ValidLocation.TRUE)
                    {
                        if (System.Math.Sqrt((CurrY - maxObject.Rectangle.Top) * (CurrY - maxObject.Rectangle.Top) + (CurrX - (maxObject.Rectangle.Left + maxObject.Rectangle.Right) / 2) * (CurrX - (maxObject.Rectangle.Left + maxObject.Rectangle.Right) / 2)) > 20)
                        {
                            Validity = ValidLocation.FALSE;
                            TargetFoundCycle = 0;
                            return;
                        }
                        else
                        {
                            TargetFoundCycle++;
                        }
                    }
                    CurrX = (maxObject.Rectangle.Left + maxObject.Rectangle.Right) / 2;
                    CurrY = maxObject.Rectangle.Top;
                    Validity = ValidLocation.TRUE;
                }
                else
                {
                    Validity = ValidLocation.FALSE;
                    TargetFoundCycle = 0;
                }
            }
            else
            {
                TargetFoundCycle = 0;
                Validity = ValidLocation.FALSE;
                return;
            }
        }
            
        
    }
    class CTrackState : CState
    {
        public int GateLengthX = 110;
        public int GateLengthY = 20;
        public int MaxX = 2;
        
        public CTrackState()
        {
        }
        public override void Execute(Bitmap Image)
        {
            BlobCounter blobCounter = new BlobCounter();
            blobCounter.MinHeight = 75;
            blobCounter.MinWidth = 75;
            blobCounter.CoupledSizeFiltering = true;
            blobCounter.ProcessImage(Image);
            Blob[] blobs = blobCounter.GetObjects(Image);
            int maxSize = 0;
            int TmpX = -100, TmpY = -100;
            Blob maxObject = new Blob(0, new Rectangle(0, 0, 0, 0));
            // find biggest blob
            if (blobs != null)
            {
                foreach (Blob blob in blobs)
                {
                    int blobSize = blob.Rectangle.Width * blob.Rectangle.Height;

                    if (blobSize > maxSize)
                    {
                        maxSize = blobSize;
                        maxObject = blob;
                    }
                }
                if (maxSize > 70)
                {
                    TmpX = (maxObject.Rectangle.Left + maxObject.Rectangle.Right) / 2;
                    TmpY = maxObject.Rectangle.Top;
                }
            }
            AForge.Imaging.VerticalIntensityStatistics VIS = new VerticalIntensityStatistics(Image);
            int[] HistVer = VIS.Gray.Values;
            AForge.Imaging.HorizontalIntensityStatistics HIS = new HorizontalIntensityStatistics(Image);
            int[] HistHor = HIS.Gray.Values;

            bool Found = false;
            for (int i = System.Math.Max(CurrY - GateLengthY / 2, 0); i <= System.Math.Min(CurrY + GateLengthY / 2, HistVer.Length - 2); i++)
            {
                if (((double)HistVer[i]) / 255 > 0)
                {
                    Found = true;
                    CurrY = i;
                    break;
                }
            }
            if (!Found)
            {
                Validity = ValidLocation.FALSE;
                return;
            }
            Found = false;
            for (int i = System.Math.Max(0, CurrX - GateLengthX / 2); i <= System.Math.Min(HistHor.Length - 1, CurrX + GateLengthX / 2); i++)
            {
                if (Image.GetPixel(i, CurrY).Name != "ff000000")
                {
                    Found = true;
                    CurrX = i;
                    break;
                }
            }
            if (!Found)
            {
                Validity = ValidLocation.FALSE;
                return;
            }
            /*if (System.Math.Sqrt((CurrX - TmpX) * (CurrX - TmpX) + (CurrY - TmpY) * (CurrY - TmpY)) > 80)
            {
                Validity = ValidLocation.FALSE;
                return;
            }
            else
                Validity = ValidLocation.TRUE;
            CurrX = TmpX;
            CurrY = TmpY;*/
            Validity = ValidLocation.TRUE;
        }
   
     
    }

    class CStateMgr
    {
        CState[] States;
        public CState CurrState;
        public ValidLocation Val = ValidLocation.FALSE;
        public CStateMgr()
        {
            States = new CState[2];
            CSearchState SS = new CSearchState();
            CTrackState TS = new CTrackState();
            States[0] = SS;
            States[1] = TS;
            CurrState = SS;
        }
        public void Execute(Bitmap Image)
        {
            CurrState.Execute(Image);
            if (CurrState == States[0]) //SS
            {
                Val = ValidLocation.FALSE;
                CSearchState Curr = CurrState as CSearchState;
                if (Curr.TargetFoundCycle >= Curr.MaxCycles)
                {
                    (States[1] as CTrackState).CurrX = Curr.CurrX;
                    (States[1] as CTrackState).CurrY = Curr.CurrY;
                    CurrState = States[1];
                    Curr.TargetFoundCycle = 0;
                }
            }
            else //TS
            {
                if (CurrState.Validity == ValidLocation.FALSE)
                {
                    CurrState = States[0];
                    Val = ValidLocation.FALSE;
                }
                else
                {
                    Val = ValidLocation.TRUE;
                }
            }
        }
    }
}
