using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public virtual void Execute(int[] HistHor, int[] HistVer)
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
        public override void Execute(int[] HistHor, int[] HistVer)
        {
            int Max = HistHor[0];
            int MaxIndex = 0;
            for (int i = 0; i < HistHor.Length; i++)
            {
                if (HistHor[i] > Max)
                {
                    Max = HistHor[i];
                    MaxIndex = i;
                }
            }
            if (Validity == ValidLocation.TRUE)
            {
                if (((double)Max) < ChangedPixelsX || System.Math.Abs(CurrX - MaxIndex) > ThreshX)
                {
                    Validity = ValidLocation.FALSE;
                    TargetFoundCycle = 0;
                    return;
                }
            }
            CurrX = MaxIndex;
            Validity = ValidLocation.TRUE;
            bool Found = false;
            for (int i = 0; i <= HistVer.Length - 1; i++)
            {
                if (HistVer[i] > 0)
                {
                    Found = true;
                    CurrY = i;
                    break;
                }
            }
            if (Found)
            {
                TargetFoundCycle++;
            }
            else
            {
                TargetFoundCycle = 0;
                Validity = ValidLocation.FALSE;
            }
        }
        
    }
    class CTrackState : CState
    {
        public int GateLengthX = 50;
        public int GateLengthY = 50;
        public int MaxX = 2;
        
        public CTrackState()
        {
        }
        public override void Execute(int[] HistHor, int[] HistVer)
        {
            int Max = 0;
            int MaxIndexX = 0;
            for (int i = System.Math.Max(0,CurrX - GateLengthX / 2); i <= System.Math.Min(HistHor.Length - 1, CurrX + GateLengthX / 2); i++)
            {
                if (HistHor[i] > Max)
                {
                    Max = HistHor[i];
                    MaxIndexX = i;
                }
            }
            if (((double)Max)/255 >= MaxX)
            {
                //Track on left edge
                int LeftIndex = -1;
                for (int i = MaxIndexX; i >= 0; i--)
                {
                    if (HistHor[i] == 0)
                    {
                        LeftIndex = i;
                        break;
                    }
                }
                if (LeftIndex == -1)
                    CurrX = 0;
                else
                    CurrX = LeftIndex;
                bool Found = false;
                for (int i = System.Math.Min(CurrY +GateLengthY / 2,HistVer.Length - 2); i >= System.Math.Max(CurrY - GateLengthY / 2,0); i--)
                //for (int i = HistVer.Length - 2; i >= 0; i-- )
                {
                    if (((double)HistVer[i]) / 255 == 0)
                    {
                        Found = true;
                        CurrY = i;
                        break;
                    }
                }
                if (Found)
                    Validity = ValidLocation.TRUE;
                else
                    Validity = ValidLocation.FALSE;
            }
            else
            {
                Validity = ValidLocation.FALSE;
            }
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
        public void Execute(int[] HistHor, int[] HistVer)
        {
            CurrState.Execute(HistHor, HistVer);
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
