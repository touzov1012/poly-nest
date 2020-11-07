using ClipperLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PolyNesterDemo
{
    static class WPFHelper
    {
        public static Random r = new Random(7);

        public static Color Fade(this Color color, double scale)
        {
            return Color.FromArgb((byte)(color.A * scale), color.R, color.G, color.B);
        }

        public static Color RandomColor()
        {
            return Color.FromArgb(255, (byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255));
        }

        public static void AddNgon(this Canvas canvas, List<List<IntPoint>> ngon, Color color, double opacity = 1.0)
        {
            List<PathFigure> figs = new List<PathFigure>();

            for (int i = 0; i < ngon.Count; i++)
            {
                var current = ngon[i];
                if (current.Count < 3)
                    continue;

                var aspts = current.Select(p => new Point(p.X, p.Y)).ToArray();

                PathFigure fig = new PathFigure(aspts[0], aspts.Skip(1).Select(p => new LineSegment(p, true)), true);
                figs.Add(fig);
            }

            PathGeometry pg = new PathGeometry(figs, FillRule.EvenOdd, null);

            Path X = new Path();
            X.Data = pg;
            X.Fill = new SolidColorBrush(color.Fade(opacity * 0.5));
            X.Stroke = new SolidColorBrush(color.Fade(opacity));
            X.StrokeThickness = 1;

            canvas.Children.Add(X);
        }
    }
}
