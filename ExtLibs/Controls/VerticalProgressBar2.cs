﻿using System.Windows.Forms;
namespace VPS.Controls
{

    public class VerticalProgressBar2 : HorizontalProgressBar2
    {
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.TranslateTransform(0, e.Graphics.ClipBounds.Height);
            e.Graphics.RotateTransform(270);
            e.Graphics.ScaleTransform((float)this.Height / (float)this.Width, (float)this.Width / (float)this.Height);
            base.OnPaint(e);
        }
        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
        }
    }
}
