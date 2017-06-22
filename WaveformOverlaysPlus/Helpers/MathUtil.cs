using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Shapes;

namespace WaveformOverlaysPlus.Helpers
{
    public static class MathUtil
    {
        public static bool DoLinesIntersect(Line line1, Line line2)
        {
            var line1_StartPoint = new Point(line1.X1, line1.Y1);
            var line1_EndPoint = new Point(line1.X2, line1.Y2);
            var line2_StartPoint = new Point(line2.X1, line2.Y1);
            var line2_EndPoint = new Point(line2.X2, line2.Y2);

            return CrossProduct(line1_StartPoint, line1_EndPoint, line2_StartPoint) != CrossProduct(line1_StartPoint, line1_EndPoint, line2_EndPoint) ||
                   CrossProduct(line2_StartPoint, line2_EndPoint, line1_StartPoint) != CrossProduct(line2_StartPoint, line2_EndPoint, line1_EndPoint);
        }

        public static double CrossProduct(Point p1, Point p2, Point p3)
        {
            return (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y);
        }
    }
}
