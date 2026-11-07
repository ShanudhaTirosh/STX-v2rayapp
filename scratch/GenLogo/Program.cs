using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;

class Program
{
    static void Main()
    {
        int width = 432;
        int height = 432;
        using Bitmap bmp = new Bitmap(width, height);
        using Graphics g = Graphics.FromImage(bmp);
        
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        g.InterpolationMode = InterpolationMode.HighQualityBicubic;
        g.Clear(Color.Transparent);

        // Shield outer points
        PointF[] pts = new PointF[]
        {
            new PointF(216, 36),
            new PointF(376, 72),
            new PointF(376, 230),
            new PointF(216, 396),
            new PointF(56, 230),
            new PointF(56, 72)
        };

        using GraphicsPath path = new GraphicsPath();
        path.AddPolygon(pts);

        // Dark glass shield gradient
        using (LinearGradientBrush brush = new LinearGradientBrush(
            new PointF(0, 36),
            new PointF(0, 396),
            Color.FromArgb(240, 26, 26, 30),
            Color.FromArgb(250, 8, 8, 10)))
        {
            g.FillPath(brush, path);
        }

        // Inner highlight path
        PointF[] innerPts = new PointF[]
        {
            new PointF(216, 52),
            new PointF(358, 84),
            new PointF(358, 222),
            new PointF(216, 376),
            new PointF(74, 222),
            new PointF(74, 84)
        };

        using GraphicsPath innerPath = new GraphicsPath();
        innerPath.AddPolygon(innerPts);

        using (LinearGradientBrush innerBrush = new LinearGradientBrush(
            new PointF(0, 52),
            new PointF(0, 376),
            Color.FromArgb(45, 255, 255, 255),
            Color.FromArgb(8, 255, 255, 255)))
        {
            g.FillPath(innerBrush, innerPath);
        }

        // Crisp White / Silver Shield Border
        using (Pen borderPen = new Pen(Color.FromArgb(240, 255, 255, 255), 7f))
        {
            borderPen.LineJoin = LineJoin.Round;
            g.DrawPath(borderPen, path);
        }

        // Inner Accent Border
        using (Pen innerPen = new Pen(Color.FromArgb(70, 255, 255, 255), 2f))
        {
            innerPen.LineJoin = LineJoin.Round;
            g.DrawPath(innerPen, innerPath);
        }

        // Fine grid / network accents
        using (Pen gridPen = new Pen(Color.FromArgb(30, 255, 255, 255), 1.5f))
        {
            g.DrawLine(gridPen, 216, 52, 216, 376);
            g.DrawLine(gridPen, 74, 150, 358, 150);
            g.DrawLine(gridPen, 100, 270, 332, 270);
        }

        // "STX" Text
        using StringFormat sf = new StringFormat
        {
            Alignment = StringAlignment.Center,
            LineAlignment = StringAlignment.Center
        };

        string fontName = "Segoe UI";
        using (Font font = new Font(fontName, 68, FontStyle.Bold, GraphicsUnit.Pixel))
        using (SolidBrush shadow = new SolidBrush(Color.FromArgb(140, 0, 0, 0)))
        using (SolidBrush white = new SolidBrush(Color.FromArgb(255, 255, 255)))
        {
            g.DrawString("STX", font, shadow, 216, 204, sf);
            g.DrawString("STX", font, white, 216, 200, sf);
        }

        // Subtitle: "V 2 R A Y"
        using (Font subFont = new Font(fontName, 17, FontStyle.Bold, GraphicsUnit.Pixel))
        using (SolidBrush subWhite = new SolidBrush(Color.FromArgb(210, 255, 255, 255)))
        {
            g.DrawString("V 2 R A Y", subFont, subWhite, 216, 268, sf);
        }

        string root = Directory.GetCurrentDirectory();
        string outPng = Path.Combine(root, "wwwroot", "assets", "pxn-shield-logo-nHC0FFCc.png");
        bmp.Save(outPng, ImageFormat.Png);
        Console.WriteLine("Saved STX logo PNG: " + outPng);

        // Generate icon
        using Bitmap iconBmp = new Bitmap(bmp, 128, 128);
        IntPtr hIcon = iconBmp.GetHicon();
        using (Icon ico = Icon.FromHandle(hIcon))
        {
            string icoPath = Path.Combine(root, "src", "PxnShield", "Assets", "pxnshield.ico");
            using FileStream fs = File.Create(icoPath);
            ico.Save(fs);
        }
        Console.WriteLine("Saved STX ICO: src/PxnShield/Assets/pxnshield.ico");
    }
}
