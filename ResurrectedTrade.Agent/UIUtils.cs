using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ResurrectedTrade.Agent
{
    public static class UIUtils
    {
        public static void ShowError(string title, string message)
        {
            MessageBox.Show(
                message, title, MessageBoxButtons.OK, MessageBoxIcon.Error
            );
        }

        public static void ShowException(string context, Exception exc)
        {
            ShowError(context, exc.ToString());
        }

        public static Icon ScaleColor(
            this Icon sourceIcon, float redTint, float greenTint, float blueTint
        )
        {
            var tinted = sourceIcon.ToBitmap().ScaleColor(redTint, greenTint, blueTint);
            return Icon.FromHandle(tinted.GetHicon());
        }

        public static Bitmap ScaleColor(
            this Bitmap sourceBitmap, float redTint, float greenTint, float blueTint
        )
        {
            BitmapData sourceData = sourceBitmap.LockBits(
                new Rectangle(
                    0, 0,
                    sourceBitmap.Width, sourceBitmap.Height
                ),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb
            );


            byte[] pixelBuffer = new byte[sourceData.Stride * sourceData.Height];


            Marshal.Copy(sourceData.Scan0, pixelBuffer, 0, pixelBuffer.Length);


            sourceBitmap.UnlockBits(sourceData);


            float blue = 0;
            float green = 0;
            float red = 0;


            for (int k = 0; k + 4 < pixelBuffer.Length; k += 4)
            {
                blue = pixelBuffer[k] * blueTint;
                green = pixelBuffer[k + 1] * greenTint;
                red = pixelBuffer[k + 2] * redTint;


                if (blue > 255)
                {
                    blue = 255;
                }


                if (green > 255)
                {
                    green = 255;
                }


                if (red > 255)
                {
                    red = 255;
                }


                pixelBuffer[k] = (byte)blue;
                pixelBuffer[k + 1] = (byte)green;
                pixelBuffer[k + 2] = (byte)red;
            }


            Bitmap resultBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);


            BitmapData resultData = resultBitmap.LockBits(
                new Rectangle(
                    0, 0,
                    resultBitmap.Width, resultBitmap.Height
                ),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb
            );


            Marshal.Copy(pixelBuffer, 0, resultData.Scan0, pixelBuffer.Length);
            resultBitmap.UnlockBits(resultData);


            return resultBitmap;
        }
    }
}
