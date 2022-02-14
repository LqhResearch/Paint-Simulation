using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PaintSimulation.shapes
{
    public class Polygon : FillableShape
    {
        public List<Point> Points { get; set; } = new List<Point>();

        public Polygon()
        {
            Name = "Polygon";
        }

        protected override GraphicsPath GraphicsPath
        {
            get
            {
                GraphicsPath path = new GraphicsPath();
                if (Points.Count < 3)
                    path.AddLine(Points[0], Points[1]);
                else
                    path.AddPolygon(Points.ToArray());
                return path;
            }
        }

        public override void Draw(Graphics graphics)
        {
            using (GraphicsPath path = GraphicsPath)
            {
                if (!Fill)
                    using (Pen pen = new Pen(Color, LineWidth) { DashStyle = DashStyle })
                    {
                        graphics.DrawPath(pen, path);
                    }
                else
                {
                    using (Brush brush = new SolidBrush(Color))
                    {
                        if (Points.Count < 3)
                            using (Pen pen = new Pen(Color, LineWidth))
                            {
                                graphics.DrawPath(pen, path);
                            }
                        else
                            graphics.FillPath(brush, path);
                    }
                }
            }
        }

        public override bool IsHit(Point point)
        {
            bool res = false;
            using (GraphicsPath path = GraphicsPath)
            {
                if (!Fill)
                    using (Pen pen = new Pen(Color, LineWidth + 3))
                    {
                        res = path.IsOutlineVisible(point, pen);
                    }
                else
                    res = path.IsVisible(point);
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
            Polygon polygon = new Polygon
            {
                Begin = Begin,
                End = End,
                IsSelected = IsSelected,
                Name = Name,
                Color = Color,
                LineWidth = LineWidth
            };
            Points.ForEach(point => polygon.Points.Add(point));
            return polygon;
        }
    }
}