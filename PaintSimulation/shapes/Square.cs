using System;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PaintSimulation.shapes
{
    public class Square : Rectangle
    {
        public int Width { get; set; }

        public Square()
        {
            Name = "Square";
        }

        protected override GraphicsPath GraphicsPath
        {
            get
            {
                GraphicsPath path = new GraphicsPath();
                Width = ((Math.Abs(End.X - Begin.X)) + Math.Abs((End.Y - Begin.Y))) / 2;

                if (End.X < Begin.X && End.Y < Begin.Y)
                    path.AddRectangle(new System.Drawing.Rectangle(new Point(Begin.X - Width, Begin.Y - Width), new Size(Width, Width)));
                else if (Begin.X > End.X && Begin.Y < End.Y)
                    path.AddRectangle(new System.Drawing.Rectangle(new Point(Begin.X - Width, Begin.Y), new Size(Width, Width)));
                else if (Begin.X < End.X && Begin.Y > End.Y)
                    path.AddRectangle(new System.Drawing.Rectangle(new Point(Begin.X, End.Y), new Size(Width, Width)));
                else
                    path.AddRectangle(new System.Drawing.Rectangle(Begin, new Size(Width, Width)));
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
                    using (Brush brush = new SolidBrush(Color))
                    {
                        graphics.FillPath(brush, path);
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
        }

        public override object Clone()
        {
            return new Square
            {
                Begin = Begin,
                End = End,
                LineWidth = LineWidth,
                Color = Color,
                IsSelected = IsSelected,
                Name = Name,
                Width = Width
            };
        }
    }
}