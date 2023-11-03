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
            HydrometHost HServer = HydrometHost.PN;
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

            var bmp = SKBitmap.Decode(args[0]);
            var imageInfo = new SKImageInfo
            {
                Width = bmp.Width,
                Height = bmp.Height,
                ColorType = SKImageInfo.PlatformColorType,
                ColorSpace = SKColorSpace.CreateSrgb(),
                AlphaType = SKAlphaType.Premul
            };
            var surface = SKSurface.Create(imageInfo);
            var canvas = surface.Canvas;
            canvas.DrawBitmap(bmp, bmp.Info.Rect);

            WriteDate(date, canvas);

            //Read the config file
            string[] lines = File.ReadAllLines(args[2]);
            for (int i = 0; i < lines.Length; i++)
            {
                var cfg = new ConfigLine(lines[i]);

                if (cfg.IsCFS)
                {
                    DrawCFS(HServer, date, cfg, canvas);
                }
                else if (cfg.IsTeacup)
                {
                    DrawTeacup(HServer, date, cfg, canvas);
                }
                else if (cfg.IsLine)
                {
                    DrawLine(HServer, date, cfg, canvas);
                }
            }

            //save the image file
            var canvasImage = surface.Snapshot();
            var canvasBitmap = SKBitmap.FromImage(canvasImage);
            using var sr = File.OpenWrite(args[1]);
            canvasBitmap.Encode(sr, SKEncodedImageFormat.Png, 100);
        }


        private static void DrawLine(HydrometHost HServer, DateTime date, 
            ConfigLine cfg, SKCanvas canvas)
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

            var textPaint = new SKPaint
            {
                Color = SKColors.Red,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 8,
                IsAntialias = true,
            };
            canvas.DrawText(Text, Location, textPaint);
        }

        private static void WriteDate(DateTime date, SKCanvas canvas)
        {
            string firstText = date.ToString("MM/dd/yyyy");
            //Location of the date
            SKPoint firstLocation = new SKPoint(2f, 10f);

            var textPaint = new SKPaint
            {
                Color = SKColors.Blue,
                Typeface = SKTypeface.FromFamilyName("Courier New"),
                TextSize = 11,
                IsAntialias = true,
            };
            canvas.DrawText(firstText, firstLocation, textPaint);
        }

        private static void DrawCFS(HydrometHost HServer, DateTime date, 
            ConfigLine cfg, SKCanvas canvas)
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
            SKPoint Location = new SKPoint(cfg.col, cfg.row + 12);

            var textPaint = new SKPaint
            {
                Color = SKColors.Red,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 8,
                IsAntialias = true,
                TextScaleX = 1.25f,
            };

            var rect = new SKRect();
            textPaint.MeasureText(Text, ref rect);
            var textRec = new SKPaint
            {
                Color = SKColors.White
            };
            rect.Offset(Location);
            rect.Inflate(2, 2);

            canvas.DrawRect(rect, textRec);
            canvas.DrawText(Text, Location, textPaint);
        }

        private static void DrawTeacup(HydrometHost HServer, DateTime date, 
            ConfigLine cfg, SKCanvas canvas)
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
            
            //Setting the points of the trapezoid
            SKPoint point1 = new SKPoint(cfg.col, cfg.row); //lower left
            SKPoint point2 = new SKPoint(cfg.col + 10 * cfg.size, cfg.row); //lower right
            SKPoint point3 = new SKPoint(cfg.col + 20 * cfg.size, cfg.row - 20 * cfg.size); //upper right
            SKPoint point4 = new SKPoint(cfg.col - 10 * cfg.size, cfg.row - 20 * cfg.size); //upper left
            SKPoint[] curvePoints = { point1, point2, point3, point4, point1 };
            
            //setting points of percent full
            SKPoint point1f = new SKPoint(cfg.col, cfg.row); //lower left
            SKPoint point2f = new SKPoint(cfg.col + 10 * cfg.size, cfg.row); //lower right
            SKPoint point3f = new SKPoint(cfg.col + 10 * cfg.size + cfg.size * full * 10 / 100, cfg.row - 20 * cfg.size * full / 100); //upper right
            SKPoint point4f = new SKPoint(cfg.col - 10 * cfg.size * full / 100, cfg.row - 20 * cfg.size * full / 100); //upper left
            SKPoint[] fullPoints = { point1f, point2f, point3f, point4f, point1f };

            var poly1Paint = new SKPaint
            {
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 2,
                Color = SKColors.Blue,
            };
            canvas.DrawPoints(SKPointMode.Polygon, curvePoints, poly1Paint);

            var poly2Paint = new SKPaint
            {
                Style = SKPaintStyle.StrokeAndFill,
                StrokeWidth = 2,
                Color = SKColors.Blue,
            };

            // no clue why path works to fill in percent full and DrawPoints
            // failed to fill in the trapezoid
            var path = new SKPath();
            path.MoveTo(point1f);
            path.LineTo(point2f);
            path.LineTo(point3f);
            path.LineTo(point4f);
            path.LineTo(point1f);
            path.Close();
            canvas.DrawPath(path, poly2Paint);

            var textPaint = new SKPaint
            {
                Color = SKColors.Red,
                Typeface = SKTypeface.FromFamilyName("Arial"),
                TextSize = 8,
                IsAntialias = true,
                TextScaleX = 1.25f,
            };
            canvas.DrawText(cfg.ResName, new SKPoint(cfg.col + 2, cfg.row + 10), textPaint);
            canvas.DrawText($"{number}/{cfg.capacity}", new SKPoint(cfg.col + 2, cfg.row + 18), textPaint);
            canvas.DrawText($"{Percent}% Full", new SKPoint(cfg.col + 2, cfg.row + 26), textPaint);
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
