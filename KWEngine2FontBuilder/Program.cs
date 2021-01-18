using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KWEngine2FontBuilder
{
    class Program
    {
        static void Main(string[] args)
        {
            V2_1();
            V2_2();
            V2_3();

        }

        static void V2_1()
        {
            //Anonymous.ttf
            int maxDim = 16384;
            int cellSize = 128;
            int fontSize = cellSize / 2;

            int offsetX = -11;
            int offsetY = 22;

            Bitmap bitmap = new Bitmap(maxDim, cellSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            PrivateFontCollection fc = new PrivateFontCollection();
            fc.AddFontFile("Anonymous.ttf");
            FontFamily fontFamily = fc.Families[0];

            Font font = new Font(fontFamily, fontSize, FontStyle.Bold);

            Graphics g = Graphics.FromImage(bitmap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            Brush brush = new SolidBrush(Color.White);
            Pen p = new Pen(Color.Red);
            p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;

            for (int i = 32; i < 384; i++)
            {
                //g.DrawRectangle(p, (i-32) * (cellSize / 2f), 0, cellSize / 2f, cellSize);
                g.DrawString(((char)i).ToString(), font, brush, (i - 32) * (cellSize / 2f) + offsetX, offsetY);
                if((i-32) * (cellSize / 2) >= maxDim)
                {
                    break;
                }
            }
            g.Dispose();
            bitmap.Save("test.png", System.Drawing.Imaging.ImageFormat.Png);
            bitmap.Dispose();

        }

        static void V2_2()
        {
            //Anonymous.ttf
            int maxDim = 16384;
            int cellSize = 128;
            int fontSize = cellSize / 2;

            int offsetX = -17;
            int offsetY = 22;

            Bitmap bitmap = new Bitmap(maxDim, cellSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            PrivateFontCollection fc = new PrivateFontCollection();
            fc.AddFontFile("MajorMonoDisplay.ttf");
            FontFamily fontFamily = fc.Families[0];

            Font font = new Font(fontFamily, fontSize, FontStyle.Bold);

            Graphics g = Graphics.FromImage(bitmap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            Brush brush = new SolidBrush(Color.White);
            Pen p = new Pen(Color.Red);
            p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;

            for (int i = 32; i < 384; i++)
            {
                //g.DrawRectangle(p, (i-32) * (cellSize / 2f), 0, cellSize / 2f, cellSize);
                g.DrawString(((char)i).ToString(), font, brush, (i - 32) * (cellSize / 2f) + offsetX, offsetY);
                if ((i - 32) * (cellSize / 2) >= maxDim)
                {
                    break;
                }
            }
            g.Dispose();
            bitmap.Save("test2.png", System.Drawing.Imaging.ImageFormat.Png);
            bitmap.Dispose();

            //Console.ReadLine();
        }

        static void V2_3()
        {
            int maxDim = 16384;
            int cellSize = 128;
            int fontSize = cellSize / 2;

            int offsetX = -12;
            int offsetY = 4;

            Bitmap bitmap = new Bitmap(maxDim, cellSize, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            PrivateFontCollection fc = new PrivateFontCollection();
            fc.AddFontFile("NovaMono.ttf");
            FontFamily fontFamily = fc.Families[0];

            Font font = new Font(fontFamily, fontSize, FontStyle.Bold);

            Graphics g = Graphics.FromImage(bitmap);
            g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            Brush brush = new SolidBrush(Color.White);
            Pen p = new Pen(Color.Red);
            p.Alignment = System.Drawing.Drawing2D.PenAlignment.Center;

            for (int i = 32; i < 384; i++)
            {
                //g.DrawRectangle(p, (i-32) * (cellSize / 2f), 0, cellSize / 2f, cellSize);
                g.DrawString(((char)i).ToString(), font, brush, (i - 32) * (cellSize / 2f) + offsetX, offsetY);
                if ((i - 32) * (cellSize / 2) >= maxDim)
                {
                    break;
                }
            }
            g.Dispose();
            bitmap.Save("test3.png", System.Drawing.Imaging.ImageFormat.Png);
            bitmap.Dispose();

            //Console.ReadLine();
        }
    }
}
