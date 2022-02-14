using System.Drawing;
using System.Drawing.Drawing2D;

namespace PaintSimulation.shapes
{
    public class Line : Shape
    {
        public Line()
        {
            Name = "Line";
        }

        protected override GraphicsPath GraphicsPath
        {
            get
            {
                GraphicsPath path = new GraphicsPath();
                path.AddLine(Begin, End);
                return path;
            }
        }

        public override object Clone()
        {
            return new Line
            {
                Begin = Begin,
                End = End,
                LineWidth = LineWidth,
                Color = Color,
                IsSelected = IsSelected,
                Name = Name
            };
        }

        public override void Draw(Graphics graphics)
        {
            using (GraphicsPath path = GraphicsPath)
            {
                using (Pen pen = new Pen(Color, LineWidth) { DashStyle = DashStyle })
                {
                    graphics.DrawPath(pen, path);
                }
            }
        }

        public override bool IsHit(Point point)
        {
            bool res = false;
            using (GraphicsPath path = GraphicsPath)
            {
                using (Pen pen = new Pen(Color, LineWidth + 3))
                {
                    res = path.IsOutlineVisible(point, pen);
                }
            }
            return res;
        }

        public override void Move(Point distance)
        {
            Begin = new Point(Begin.X + distance.X, Begin.Y + distance.Y);
            End = new Point(End.X + distance.X, End.Y + distance.Y);
        }
    }
}