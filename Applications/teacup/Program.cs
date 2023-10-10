using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Reclamation.Core;
using Reclamation.TimeSeries.Hydromet;
using SkiaSharp;

namespace Teacup
{
    class Program
    {
        static void Main(string[] args)
        {
            Logger.EnableLogger(); // for debug info
            HydrometHost HServer = HydrometHost.PNLinux;
            DateTime date = DateTime.Now.AddDays(-1).Date;

            if (args.Length != 3 && args.Length != 4)
            {
                Console.WriteLine("Usage: TeaCup.exe infile outfile configfile  [date: mm/dd/yyyy]  ");
                return;
            }

            if (args.Length == 4)//Given a date by the user
            {
                date = Convert.ToDateTime(args[3]);
            }
            //Read the config file
            string[] lines = File.ReadAllLines(args[2]);
            if (args[2].Contains("yak"))
                HServer = HydrometHost.Yakima;

            var bmp = SKBitmap.Decode(args[0]);
            var canvas = new SKCanvas(bmp);

            WriteDate(date, bmp, canvas);

            for (int i = 0; i < lines.Length; i++)
            {
                var cfg = new ConfigLine(lines[i]);

                if (cfg.IsCFS)
                {
                    DrawCFS(HServer, date, bmp, cfg, canvas);
                }
                else if (cfg.IsTeacup)
                {
                    DrawTeacup(HServer, date, bmp, cfg, canvas);
                }
                else if (cfg.IsLine)
                {
                    DrawLine(HServer, date, bmp, cfg, canvas);

                }
            }
            bmp.CopyTo(SKBitmap.Decode(args[1]));//save the image file 
        }


        private static void DrawLine(HydrometHost HServer, DateTime date, 
            SKBitmap bmp, ConfigLine cfg, SKCanvas canvas)
        {
            string number = "";
            double value = ReadHydrometValue(cfg.cbtt, cfg.pcode, date, HServer );
            //check for missing values and set the output number of digits to report
            if ((cfg.units == "Feet") || (cfg.units == "%"))
            {
                number = value.ToString("F2");
            }
            else
            {
                number = value.ToString("F0");
            }
            if (value == 998877)
            {
                number = "MISSING";
            }
            
            string Text = cfg.ResName + "  " + number + " " + cfg.units;
            SKPoint Location = new SKPoint(cfg.col, cfg.row);
            SKRect rect1 = new SKRect(cfg.col, cfg.row + 2, 90, 10);
            
            var rect1Paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 7,
            };
            canvas.DrawRect(rect1, rect1Paint);

            var textPaint = new SKPaint
            {
                Color = SKColors.Red,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 7,
            };
            canvas.DrawText(Text, Location, textPaint);


            //using (Graphics graphics = Graphics.FromImage(bmp))
            //{
            //    using (Font arialFont = new Font("Arial", 7))
            //    {
            //        graphics.FillRectangle(Brushes.White, rect1);
            //        graphics.DrawString(Text, arialFont, Brushes.Red, Location);
            //    }
            //}
        }

        private static void WriteDate(DateTime date, SKBitmap bmp, SKCanvas canvas)
        {
            string firstText = date.ToString("MM/dd/yyyy");
            //Location of the date
            SKPoint firstLocation = new SKPoint(2f, 3f);
            //load the image file  
            SKRect rect = new SKRect(0, 0, 100, 20);

            //Fill the background and draw the string
            var rectPaint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White,
                Typeface = SKTypeface.FromFamilyName("Carbon"),
                TextSize = 10,
            };
            canvas.DrawRect(rect, rectPaint);

            var textPaint = new SKPaint
            {
                Color = SKColors.Blue,
                Typeface = SKTypeface.FromFamilyName("Carbon"),
                TextSize = 10,
            };
            canvas.DrawText(firstText, firstLocation, textPaint);

            //using (Graphics graphics = Graphics.FromImage(bmp))
            //{
            //    using (Font TNRFont = new Font("Carbon", 10))
            //    {
            //        graphics.FillRectangle(Brushes.White, rect);
            //        graphics.DrawString(firstText, TNRFont, Brushes.Blue, firstLocation);
            //    }
            //}
        }

        private static void DrawCFS(HydrometHost HServer, DateTime date, 
            SKBitmap bmp, ConfigLine cfg, SKCanvas canvas)
        {
            string number;
            double value = ReadHydrometValue(cfg.cbtt, cfg.pcode, date, HServer);
            //check for missing values and set the output number of digits to report
            if (value == 998877)
            {
                number = "MISSING";
            }
            else
            {
                number = value.ToString("F0");
            }
            string Text = cfg.cbtt + "  " + number + " " + cfg.type;
            SKPoint Location = new SKPoint(cfg.col, cfg.row + 5);
            SKRect rect1 = new SKRect(cfg.col, cfg.row + 7, 85, 10);

            //Fill the background and draw the string
            var rect1Paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 7,
            };
            canvas.DrawRect(rect1, rect1Paint);

            var textPaint = new SKPaint
            {
                Color = SKColors.Red,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 7,
            };
            canvas.DrawText(Text, Location, textPaint);

            //using (Graphics graphics = Graphics.FromImage(bmp))
            //{
            //    using (Font arialFont = new Font("Arial", 7))
            //    {
            //        graphics.FillRectangle(Brushes.White, rect1);
            //        graphics.DrawString(Text, arialFont, Brushes.Red, Location);
            //    }
            //}
        }

        private static void DrawTeacup(HydrometHost HServer, DateTime date, 
            SKBitmap bmp, ConfigLine cfg, SKCanvas canvas)
        {
            string number = "";
            string Percent;
            double percent;
            double value = ReadHydrometValue(cfg.cbtt, cfg.pcode, date, HServer);
            //Determine the percent full

            if (value == 998877)
            {
                percent = 0;
            }
            else
                percent = value / cfg.capacity;

            if (percent >= 1)
            {
                percent = 1;
            }
            else if (percent <= 0)
            {
                percent = 0;
            }

            double area = 400 * cfg.size * cfg.size + 3200 * cfg.size * cfg.size * percent;
            area = Math.Sqrt(area) - 20 * cfg.size;
            area = area / (40 * cfg.size);
            if (area >= 1.000)
            {
                area = 1.000;
            }
            if (area <= 0.000)
            {
                area = 0.000;
            }
            Int32 full = Convert.ToInt32(area * 100);
            //check for missing values and set the output number of digits to report
            if (value == 998877)
            {
                number = "MISSING";
                Percent = "MISSING";
            }
            else
            {
                number = value.ToString("F0");
                Percent = (percent * 100).ToString("F0");
            }
            
            //Create Isosceles trapizoid
            string Text = cfg.ResName + "\n" + number + "/" + cfg.capacity + "\n" + Percent + "% Full";
            SKPoint Location = new SKPoint(cfg.col, cfg.row);
            
            //Setting the points of the trapezoid
            SKPoint point1 = new SKPoint(cfg.col, cfg.row); //lower left
            SKPoint point2 = new SKPoint(cfg.col + 10 * cfg.size, cfg.row); //lower right
            SKPoint point3 = new SKPoint(cfg.col + 20 * cfg.size, cfg.row - 20 * cfg.size); //upper right
            SKPoint point4 = new SKPoint(cfg.col - 10 * cfg.size, cfg.row - 20 * cfg.size); //upper left
            SKPoint[] curvePoints = { point1, point2, point3, point4 };
            
            //setting points of percent full
            SKPoint point1f = new SKPoint(cfg.col, cfg.row); //lower left
            SKPoint point2f = new SKPoint(cfg.col + 10 * cfg.size, cfg.row); //lower right
            SKPoint point3f = new SKPoint(cfg.col + 10 * cfg.size + cfg.size * full * 10 / 100, cfg.row - 20 * cfg.size * full / 100); //upper right
            SKPoint point4f = new SKPoint(cfg.col - 10 * cfg.size * full / 100, cfg.row - 20 * cfg.size * full / 100); //upper left
            SKPoint[] fullPoints = { point1f, point2f, point3f, point4f };

            var poly1Paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.White,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 8,
            };
            canvas.DrawPoints(SKPointMode.Polygon, curvePoints, poly1Paint);

            var poly2Paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                Color = SKColors.Blue,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 8,
            };
            canvas.DrawPoints(SKPointMode.Polygon, curvePoints, poly2Paint);

            var textPaint = new SKPaint
            {
                Color = SKColors.Red,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 8,
            };
            canvas.DrawText(Text, Location, textPaint);

            var poly3Paint = new SKPaint
            {
                Style = SKPaintStyle.Fill,
                Color = SKColors.Blue,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 8,
            };
            canvas.DrawPoints(SKPointMode.Polygon, fullPoints, poly2Paint);

            ////Create Graphics
            //using (Graphics graphics = Graphics.FromImage(bmp))
            //{
            //    using (Font arialFont = new Font("Arial", 8))
            //    {
            //        graphics.FillPolygon(whiteBrush, curvePoints);
            //        graphics.DrawPolygon(bluePen, curvePoints);
            //        graphics.DrawString(Text, arialFont, Brushes.Red, Location);
            //        graphics.FillPolygon(blueBrush, fullPoints);
            //    }
            //}
        }

        private static double ReadHydrometValue(string cbtt, string pcode, DateTime date,HydrometHost server) 
        {
            //Get hydromet data
            HydrometDailySeries s = new HydrometDailySeries(cbtt, pcode,server);
            s.Read(date, date);
            double value = 998877;
            if (s.Count > 0 && !s[0].IsMissing)
                value = s[0].Value;

            return value;
        }


    }
}
