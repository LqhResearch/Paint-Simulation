using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PaintSimulation.shapes
{
    public class Curve : Shape
    {
        public List<Point> Points { get; set; } = new List<Point>();

        public Curve()
        {
            Name = "Curve";
        }

        protected override GraphicsPath GraphicsPath
        {
            get
            {
                GraphicsPath path = new GraphicsPath();
                path.AddCurve(Points.ToArray());
                return path;
            }
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
            for (int i = 0; i < Points.Count; i++)
                Points[i] = new Point(Points[i].X + distance.X, Points[i].Y + distance.Y);
        }

        public override object Clone()
        {
            Curve curve = new Curve
            {
                Begin = Begin,
                End = End,
                IsSelected = IsSelected,
                Name = Name,
                Color = Color,
                LineWidth = LineWidth
            };
            Points.ForEach(point => curve.Points.Add(point));
            return curve;
        }
    }
}