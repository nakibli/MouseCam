using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using WebCam_Capture;
using System.Collections.Generic;
using AForge.Imaging;
using AForge.Imaging.Filters;



namespace WinFormCharpWebCam
{
    //Design by Pongsakorn Poosankam
    class WebCam
    {
        public delegate void dChangeCursorEvent(int X, int Y);
        public event dChangeCursorEvent eChangedCursorEvent;

        private WebCamCapture webcam;
        private System.Windows.Forms.PictureBox _FrameImage;
        private System.Windows.Forms.PictureBox _CaptureImage;
        private System.Windows.Forms.PictureBox _CaptureImage2;
        private int FrameNumber = 1;

        private Webcam.CStateMgr StateMgr;
        private System.Drawing.Bitmap backgroundFrameGray = null;
        private System.Drawing.Bitmap CurrentFrameGray = null;
        private System.Drawing.Bitmap backgroundFrame = null;
        private System.Drawing.Bitmap CurrentFrame = null;
        private int Frames2Ignore = 20;
        private int HorTresh = 20;
        private int VerTresh = 20;
        private int LastX = -1;
        private int LastY = -1;
        // filters used to do image processing
        private GrayscaleBT709 grayscaleFilter = new GrayscaleBT709();
        private Difference differenceFilter = new Difference();
        private Threshold thresholdFilter = new Threshold(25);
        private Erosion erosionFilter = new Erosion();
        private Opening openingFilter = new Opening();
        private MoveTowards moveTowardsFilter = new MoveTowards();
        private BlobCounter blobCounter = new BlobCounter();
        public void InitializeWebCam(ref System.Windows.Forms.PictureBox ImageControl, ref System.Windows.Forms.PictureBox ImageCapture, ref System.Windows.Forms.PictureBox ImageCapture2)
        {
            StateMgr = new Webcam.CStateMgr();
            webcam = new WebCamCapture();
            webcam.FrameNumber = ((ulong)(0ul));
            webcam.TimeToCapture_milliseconds = FrameNumber;
            webcam.ImageCaptured += new WebCamCapture.WebCamEventHandler(webcam_ImageCaptured);
            _FrameImage = ImageControl;
            _CaptureImage = ImageCapture;
            _CaptureImage2 = ImageCapture2;
        }

        void webcam_ImageCaptured(object source, WebcamEventArgs e)
        {
            _FrameImage.Image = e.WebCamImage;
            Bitmap MaskImage = new Bitmap(640, 480);
            if (backgroundFrame == null)
            {
                Frames2Ignore--;
                if (Frames2Ignore == 0)
                {
                    backgroundFrame = (Bitmap)e.WebCamImage;
                    backgroundFrameGray = grayscaleFilter.Apply(backgroundFrame);
                }
                return;
            }

            //Save curent image
            CurrentFrame = (Bitmap)e.WebCamImage;
            CurrentFrameGray = grayscaleFilter.Apply(CurrentFrame);

            /*
            // create filter
            IFilter pixellateFilter = new Pixellate();
            // apply the filter
            backgroundFrame = pixellateFilter.Apply(backgroundFrame);
            backgroundFrameGray = grayscaleFilter.Apply(backgroundFrame);
            CurrentFrame = pixellateFilter.Apply(CurrentFrame);
            CurrentFrameGray = grayscaleFilter.Apply(CurrentFrame);*/

            MoveTowards moveTowardsFilter = new MoveTowards();
            moveTowardsFilter.OverlayImage = CurrentFrameGray;
            // move background towards current frame
            Bitmap tmp = moveTowardsFilter.Apply(backgroundFrameGray);
            // dispose old background
            backgroundFrame.Dispose();
            backgroundFrame = tmp;

            // create processing filters sequence

            FiltersSequence processingFilter = new FiltersSequence();
            processingFilter.Add(new Difference(backgroundFrameGray));
            processingFilter.Add(new Threshold(15));
            processingFilter.Add(new Opening());
            processingFilter.Add(new Edges());
            processingFilter.Add(new DifferenceEdgeDetector());

            // apply the filter

            Bitmap tmp1 = processingFilter.Apply(CurrentFrameGray);
            // extract red channel from the original image

            IFilter extrachChannel = new ExtractChannel(RGB.R);
            Bitmap redChannel = extrachChannel.Apply(backgroundFrame);

            //  merge red channel with moving object borders

            Merge mergeFilter = new Merge();
            mergeFilter.OverlayImage = tmp1;
            Bitmap tmp2 = mergeFilter.Apply(redChannel);
            // replace red channel in the original image

            ReplaceChannel replaceChannel = new ReplaceChannel(RGB.R, tmp2);
            replaceChannel.ChannelImage = tmp2;
            Bitmap tmp3 = replaceChannel.Apply(backgroundFrame);
            StateMgr.Execute(tmp1);
            if (eChangedCursorEvent != null && StateMgr.Val == Webcam.ValidLocation.TRUE)
            {
                //Console.WriteLine("X={0} , Y={1}", StateMgr.CurrState.CurrX, StateMgr.CurrState.CurrY);
                eChangedCursorEvent(StateMgr.CurrState.CurrX, StateMgr.CurrState.CurrY);
                for (int i = -4; i <= 4; i++)
                    for (int j = -4; j <= 4;j++ )
                        tmp3.SetPixel(StateMgr.CurrState.CurrX+i, StateMgr.CurrState.CurrY+j, Color.Blue);
                //eChangedCursorEvent(StateMgr.CurrState.CurrX, 100);
                //eChangedCursorEvent(100, StateMgr.CurrState.CurrY);
            }
            _CaptureImage.Image = tmp1;
            _CaptureImage2.Image = tmp3;
            backgroundFrame = (Bitmap)e.WebCamImage;
            backgroundFrameGray = grayscaleFilter.Apply(backgroundFrame);

        }
        void webcam_ImageCaptured_Back2(object source, WebcamEventArgs e)
        {
            _FrameImage.Image = e.WebCamImage;
            Bitmap MaskImage = new Bitmap(640, 480);
            if (backgroundFrame == null)
            {
                Frames2Ignore--;
                if (Frames2Ignore == 0)
                {
                    backgroundFrame = (Bitmap)e.WebCamImage;
                    backgroundFrameGray = grayscaleFilter.Apply(backgroundFrame);
                }
                return;
            }

            //Save curent image
            CurrentFrame = (Bitmap)e.WebCamImage;
            CurrentFrameGray = grayscaleFilter.Apply(CurrentFrame);

            /*
            // create filter
            IFilter pixellateFilter = new Pixellate();
            // apply the filter
            backgroundFrame = pixellateFilter.Apply(backgroundFrame);
            backgroundFrameGray = grayscaleFilter.Apply(backgroundFrame);
            CurrentFrame = pixellateFilter.Apply(CurrentFrame);
            CurrentFrameGray = grayscaleFilter.Apply(CurrentFrame);*/

            MoveTowards moveTowardsFilter = new MoveTowards();
            moveTowardsFilter.OverlayImage = CurrentFrameGray;
            // move background towards current frame
            Bitmap tmp = moveTowardsFilter.Apply(backgroundFrameGray);
            // dispose old background
            backgroundFrame.Dispose();
            backgroundFrame = tmp;

            // create processing filters sequence

            FiltersSequence processingFilter = new FiltersSequence();
            processingFilter.Add(new Difference(backgroundFrameGray));
            processingFilter.Add(new Threshold(15));
            processingFilter.Add(new Opening());
            processingFilter.Add(new Edges());
            processingFilter.Add(new DifferenceEdgeDetector());

            // apply the filter

            Bitmap tmp1 = processingFilter.Apply(CurrentFrameGray);
            // extract red channel from the original image

            /*IFilter extrachChannel = new ExtractChannel(RGB.R);
            Bitmap redChannel = extrachChannel.Apply(backgroundFrame);

            //  merge red channel with moving object borders

            Merge mergeFilter = new Merge();
            mergeFilter.OverlayImage = tmp1;
            Bitmap tmp2 = mergeFilter.Apply(redChannel);
            // replace red channel in the original image

            ReplaceChannel replaceChannel = new ReplaceChannel(RGB.R, tmp2);
            replaceChannel.ChannelImage = tmp2;
            Bitmap tmp3 = replaceChannel.Apply(backgroundFrame);

            ConnectedComponentsLabeling CCL = new ConnectedComponentsLabeling();
            CCL.MinWidth = 75;
            CCL.MinHeight = 75;
            CCL.CoupledSizeFiltering = true;
            Bitmap tmp4 = CCL.Apply(tmp1);

            blobCounter.MinHeight = 75;
            blobCounter.MinWidth = 75;
            blobCounter.CoupledSizeFiltering = true;
            blobCounter.ProcessImage(tmp1);
            Blob[] blobs = blobCounter.GetObjects(tmp1);
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
            }*/
            Bitmap Hor = new Bitmap(320, 240);
            Bitmap Ver = new Bitmap(320, 240);
            /*if (maxSize > 150)
            {
                AForge.Imaging.VerticalIntensityStatistics VIS = new VerticalIntensityStatistics(tmp1);
                int[] HistVer = VIS.Gray.Values;
                AForge.Imaging.HorizontalIntensityStatistics HIS = new HorizontalIntensityStatistics(tmp1);
                int[] HistHor = HIS.Gray.Values;
            }
             */
            AForge.Imaging.VerticalIntensityStatistics VIS = new VerticalIntensityStatistics(tmp1);
            int[] HistVer = VIS.Gray.Values;
            AForge.Imaging.HorizontalIntensityStatistics HIS = new HorizontalIntensityStatistics(tmp1);
            int[] HistHor = HIS.Gray.Values;
            //StateMgr.Execute(HistHor,HistVer);
            if (eChangedCursorEvent != null && StateMgr.Val == Webcam.ValidLocation.TRUE)
            {
                //Console.WriteLine("X={0} , Y={1}", StateMgr.CurrState.CurrX, StateMgr.CurrState.CurrY);
                eChangedCursorEvent(StateMgr.CurrState.CurrX, StateMgr.CurrState.CurrY);
                //eChangedCursorEvent(StateMgr.CurrState.CurrX, 100);
                //eChangedCursorEvent(100, StateMgr.CurrState.CurrY);
            }

            #region Paint Hist
            /*for (int x = 0; x < 320; x++)
                for (int y = 0; y < 240; y++)
                {
                    Hor.SetPixel(x, y, Color.White);
                    Ver.SetPixel(x, y, Color.White);
                }
            int Imax = -1, Max = -1;
            for (int i = 0; i < HistHor.Length; i++)
            {
                for (int y = 0; y < ((double)(HistHor[i]) / 255); y++)
                    Hor.SetPixel(i, y, Color.Black);
                if (HistHor[i] > 0)
                {
                    Imax = i;
                    Max = HistHor[i];
                }
            }
            int ImaxY = -1, MaxY = -1;
            for (int i = 0; i < HistVer.Length; i++)
            {
                for (int x = 0; x < ((double)(HistVer[i]) / 255); x++)
                    Ver.SetPixel(x, i, Color.Black);
                if (HistVer[i] > MaxY)
                {
                    ImaxY = i;
                    MaxY = HistVer[i];
                }
            }*/
            #endregion

            _CaptureImage.Image = Hor;
            _CaptureImage2.Image = Ver;
            backgroundFrame = (Bitmap)e.WebCamImage;
            backgroundFrameGray = grayscaleFilter.Apply(backgroundFrame);
        }
        void webcam_ImageCaptured_Back(object source, WebcamEventArgs e)
        {
            _FrameImage.Image = e.WebCamImage;
            Bitmap MaskImage = new Bitmap(640, 480);
            if (backgroundFrame == null)
            {
                Frames2Ignore--;
                if (Frames2Ignore == 0)
                {
                    backgroundFrame = (Bitmap)e.WebCamImage;
                    backgroundFrameGray = grayscaleFilter.Apply(backgroundFrame);
                }
                return;
            }

            //Save curent image
            CurrentFrame = (Bitmap)e.WebCamImage;
            CurrentFrameGray = grayscaleFilter.Apply(CurrentFrame);

            /*
            // create filter
            IFilter pixellateFilter = new Pixellate();
            // apply the filter
            backgroundFrame = pixellateFilter.Apply(backgroundFrame);
            backgroundFrameGray = grayscaleFilter.Apply(backgroundFrame);
            CurrentFrame = pixellateFilter.Apply(CurrentFrame);
            CurrentFrameGray = grayscaleFilter.Apply(CurrentFrame);*/

            MoveTowards moveTowardsFilter = new MoveTowards();
            moveTowardsFilter.OverlayImage = CurrentFrameGray;
            // move background towards current frame
            Bitmap tmp = moveTowardsFilter.Apply(backgroundFrameGray);
            // dispose old background
            backgroundFrame.Dispose();
            backgroundFrame = tmp;

            // create processing filters sequence

            FiltersSequence processingFilter = new FiltersSequence();
            processingFilter.Add(new Difference(backgroundFrameGray));
            processingFilter.Add(new Threshold(15));
            processingFilter.Add(new Opening());
            processingFilter.Add(new Edges());
            processingFilter.Add(new DifferenceEdgeDetector());

            // apply the filter

            Bitmap tmp1 = processingFilter.Apply(CurrentFrameGray);
            // extract red channel from the original image

            IFilter extrachChannel = new ExtractChannel(RGB.R);
            Bitmap redChannel = extrachChannel.Apply(backgroundFrame);
            
            //  merge red channel with moving object borders

            Merge mergeFilter = new Merge();
            mergeFilter.OverlayImage = tmp1;
            Bitmap tmp2 = mergeFilter.Apply(redChannel);
            // replace red channel in the original image

            ReplaceChannel replaceChannel = new ReplaceChannel(RGB.R,tmp2); 
            replaceChannel.ChannelImage = tmp2;
            Bitmap tmp3 = replaceChannel.Apply(backgroundFrame);

            ConnectedComponentsLabeling CCL = new ConnectedComponentsLabeling();
            CCL.MinWidth = 75;
            CCL.MinHeight = 75;
            CCL.CoupledSizeFiltering = true;
            Bitmap tmp4 = CCL.Apply(tmp1);

            blobCounter.MinHeight = 75;
            blobCounter.MinWidth = 75;
            blobCounter.CoupledSizeFiltering = true;
            blobCounter.ProcessImage(tmp1);
            Blob[] blobs = blobCounter.GetObjects(tmp1);
            int maxSize = 0;
            Blob maxObject = new Blob(0, new Rectangle(0, 0, 0, 0));
            // find biggest blob
            Bitmap Masked = new Bitmap(320, 240);
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

                for (int i = maxObject.Rectangle.Left; i < maxObject.Rectangle.Right; i++)
                {
                    for (int j = maxObject.Rectangle.Top; j < maxObject.Rectangle.Bottom; j++)
                    {
                        Masked.SetPixel(i, j, maxObject.Image.GetPixel(i - maxObject.Rectangle.Left, j - maxObject.Rectangle.Top));
                    }
                }
            }

            /*Bitmap Hor = new Bitmap(320, 240);
            Bitmap Ver = new Bitmap(320, 240);
            if (maxSize > 150)
            {
                AForge.Imaging.VerticalIntensityStatistics VIS = new VerticalIntensityStatistics(tmp1);
                int[] HistVer = VIS.Gray.Values;
                AForge.Imaging.HorizontalIntensityStatistics HIS = new HorizontalIntensityStatistics(tmp1);
                int[] HistHor = HIS.Gray.Values;

                for (int x=0;x<320;x++)
                    for (int y = 0; y < 240; y++)
                    {
                        Hor.SetPixel(x, y, Color.White);
                        Ver.SetPixel(x, y, Color.White);
                    }
                int Imax = -1, Max = -1;
                for (int i = 0; i < HistHor.Length; i++)
                {
                    for (int y = 0; y < ((double)(HistHor[i]) / 255) ; y++)
                        Hor.SetPixel(i, y, Color.Black);
                        if (HistHor[i] > 0)
                        {
                            Imax = i;
                            Max = HistHor[i];
                        }
                }
                int ImaxY = -1, MaxY = -1;
                for (int i = 0; i < HistVer.Length; i++)
                {
                    for (int x = 0; x < ((double)(HistVer[i]) / 255) ; x++)
                        Ver.SetPixel(x, i, Color.Black);
                    if (HistVer[i] > MaxY)
                    {
                        ImaxY = i;
                        MaxY = HistVer[i];
                    }
                }
            }
            
            */
           /* blobCounter.MinHeight = 75;
            blobCounter.MinWidth = 75;
            blobCounter.CoupledSizeFiltering = true;
            blobCounter.ProcessImage(tmp1);

            Blob[] blobs = blobCounter.GetObjects(tmp1);
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
                
                if (maxObject.Rectangle.Height > 90 && maxObject.Rectangle.Width > 30)
                {
                    AForge.Imaging.VerticalIntensityStatistics VIS = new VerticalIntensityStatistics(maxObject.Image);
                    int[] HistVer = VIS.Gray.Values;
                    AForge.Imaging.HorizontalIntensityStatistics HIS = new HorizontalIntensityStatistics(maxObject.Image);
                    int[] HistHor = HIS.Gray.Values;

                    int Imax = -1, Max = -1;
                    for (int i = 0; i < HistHor.Length; i++)
                    {
                        if (HistHor[i] > 0)
                        {
                            Imax = i;
                            Max = HistHor[i];
                            break;
                        }
                    }
                    int ImaxY = -1, MaxY = -1;
                    for (int i = 0; i < HistVer.Length; i++)
                    {
                        if (HistVer[i] > MaxY)
                        {
                            ImaxY = i;
                            MaxY = HistVer[i];
                        }
                    }
                    //Imax = 0;
                    ImaxY = 0;

                    Console.WriteLine("X={0},Y={1}", maxObject.Rectangle.X, maxObject.Rectangle.Y);
                    if (eChangedCursorEvent != null && maxSize != 0)
                        eChangedCursorEvent(maxObject.Rectangle.X + Imax, maxObject.Rectangle.Y + ImaxY);
                    LastX = maxObject.Rectangle.X;
                    LastY = maxObject.Rectangle.Y;
                }*/
                /*else if (LastX != -1 && LastY != -1 && maxSize > 0)
                {
                    //Calc distance from LastX,LastY
                    double distX = System.Math.Pow(maxObject.Rectangle.X - LastX, 2);
                    double distY = System.Math.Pow(maxObject.Rectangle.Y - LastY, 2);
                    double dist = System.Math.Pow(distX + distY, 0.5);
                    if (dist < 15)
                    {
                        Console.WriteLine("X={0},Y={1}", maxObject.Rectangle.X, maxObject.Rectangle.Y);
                        if (eChangedCursorEvent != null && maxSize != 0)
                            eChangedCursorEvent(maxObject.Rectangle.X, maxObject.Rectangle.Y);
                        LastX = maxObject.Rectangle.X;
                        LastY = maxObject.Rectangle.Y;
                    }
                    else
                    {
                        LastX = -1;
                        LastY = -1;
                    }
                }
                else
                {
                    LastX = -1;
                    LastY = -1;
                }*/
            //}
            _CaptureImage.Image = maxObject.Image;
            //_CaptureImage.Image = tmp3;
            _CaptureImage2.Image = tmp4;
            backgroundFrame = (Bitmap)e.WebCamImage;
            backgroundFrameGray = grayscaleFilter.Apply(backgroundFrame);
        }

        public void Start()
        {
            webcam.TimeToCapture_milliseconds = FrameNumber;
            webcam.Start(0);
        }

        public void Stop()
        {
            webcam.Stop();
        }

        public void Continue()
        {
            // change the capture time frame
            webcam.TimeToCapture_milliseconds = FrameNumber;

            // resume the video capture from the stop
            webcam.Start(this.webcam.FrameNumber);
        }

        public void ResolutionSetting()
        {
            webcam.Config();
        }

        public void AdvanceSetting()
        {
            webcam.Config2();
        }

    }
}