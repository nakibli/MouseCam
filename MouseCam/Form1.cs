using System;
using System.Linq;
using System.Text;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using System.ComponentModel;
using System.Collections.Generic;



namespace WinFormCharpWebCam
{
    public partial class mainWinForm : Form
    {
        public mainWinForm()
        {
            InitializeComponent();
        }
        WebCam webcam;
        private void mainWinForm_Load(object sender, EventArgs e)
        {
            //Cursor.Position = new Point(1280 ,800);
            webcam = new WebCam();
            webcam.InitializeWebCam(ref imgVideo, ref imgCapture,ref pictureBox1);
            webcam.eChangedCursorEvent +=new WebCam.dChangeCursorEvent(MoveCursor);
        }

        private void bntStart_Click(object sender, EventArgs e)
        {
            webcam.Start();
        }

        private void bntStop_Click(object sender, EventArgs e)
        {
            webcam.Stop();
        }

        private void bntContinue_Click(object sender, EventArgs e)
        {
            webcam.Continue();
        }

        private void bntCapture_Click(object sender, EventArgs e)
        {
            imgCapture.Image = imgVideo.Image;
        }

        private void bntSave_Click(object sender, EventArgs e)
        {
            Helper.SaveImageCapture(imgCapture.Image);
        }

        private void bntVideoFormat_Click(object sender, EventArgs e)
        {
            webcam.ResolutionSetting();
        }

        private void bntVideoSource_Click(object sender, EventArgs e)
        {
            webcam.AdvanceSetting();
        }
        public void MoveCursor(int X, int Y)
        {
            
            int NewX = (int)(((float)(X) / 320) * 1280);
            int NewY = (int)(((float)(Y) / 240) * 800);
            //int NewX = 1280 - (int)(((float)(X) / 300) * 1280);
            //int NewY = (int)(((float)(Y) / 200) * 800);
            Cursor.Position = new Point(imgVideo.Location.X + NewX,imgVideo.Location.Y + NewY);
        }


    }
}