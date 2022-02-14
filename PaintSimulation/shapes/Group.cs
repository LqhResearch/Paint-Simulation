using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace PaintSimulation.shapes
{
    public class Group : Shape, IEnumerable
    {
        private List<Shape> Shapes = new List<Shape>();

        public Group()
        {
            Name = "Group";
        }

        public Shape this[int index]
        {
            get => Shapes[index];
            set => Shapes[index] = value;
        }

        public void Add(Shape shape)
        {
            Shapes.Add(shape);
        }

        private GraphicsPath[] GraphicsPaths
        {
            get
            {
                GraphicsPath[] paths = new GraphicsPath[Shapes.Count];

                for (int i = 0; i < Shapes.Count; i++)
                {
                    GraphicsPath path = new GraphicsPath();
                    if (Shapes[i] is Line line)
                        path.AddLine(line.Begin, line.End);
                    else if (Shapes[i] is Rectangle rect)
                    {
                        if (rect is Square square)
                        {
                            int a = ((square.End.X - square.Begin.X) + (square.End.Y - square.Begin.Y)) / 2;
                            path.AddRectangle(new System.Drawing.Rectangle(square.Begin.X, square.Begin.Y, a, a));
                        }
                        else
                            path.AddRectangle(new RectangleF(rect.Begin.X, rect.Begin.Y, rect.End.X - rect.Begin.X, rect.End.Y - rect.Begin.Y));
                    }
                    else if (Shapes[i] is Ellipse ellipse)
                    {
                        if (ellipse is Circle circle)
                        {
                            int r = ((circle.End.X - circle.Begin.X) + (circle.End.Y - circle.Begin.Y)) / 2;
                            path.AddEllipse(new System.Drawing.Rectangle(circle.Begin.X, circle.Begin.Y, r, r));
                        }
                        else
                            path.AddEllipse(new System.Drawing.Rectangle(ellipse.Begin.X, ellipse.Begin.Y, ellipse.End.X - ellipse.Begin.X, ellipse.End.Y - ellipse.Begin.Y));
                    }
                    else if (Shapes[i] is Curve curve)
                        path.AddCurve(curve.Points.ToArray());
                    else if (Shapes[i] is Polygon polygon)
                        path.AddPolygon(polygon.Points.ToArray());
                    else if (Shapes[i] is Group group)
                        for (int j = 0; j < group.GraphicsPaths.Length; j++)
                        {
                            path.AddPath(group.GraphicsPaths[j], false);
                        }
                    paths[i] = path;
                }
                return paths;
            }
        }

        public override void Draw(Graphics graphics)
        {
            GraphicsPath[] paths = GraphicsPaths;
            for (int i = 0; i < paths.Length; i++)
            {
                using (GraphicsPath path = paths[i])
                {
                    if (Shapes[i] is FillableShape shape)
                    {
                        if (shape.Fill)
                        {
                            using (Brush brush = new SolidBrush(shape.Color))
                            {
                                graphics.FillPath(brush, path);
                            }
                        }
                        else
                        {
                            using (Pen pen = new Pen(shape.Color, shape.LineWidth) { DashStyle = shape.DashStyle })
                            {
                                graphics.DrawPath(pen, path);
                            }
                        }
                    }
                    else if (Shapes[i] is Group group)
                    {
                        group.Draw(graphics);
                    }
                    else
                    {
                        using (Pen pen = new Pen(Shapes[i].Color, Shapes[i].LineWidth) { DashStyle = Shapes[i].DashStyle })
                        {
                            graphics.DrawPath(pen, path);
                        }
                    }
                }
            }
        }

        public override bool IsHit(Point point)
        {
            GraphicsPath[] paths = GraphicsPaths;
            for (int i = 0; i < paths.Length; i++)
            {
                using (GraphicsPath path = paths[i])
                {
                    if (Shapes[i] is FillableShape shape)
                    {
                        if (shape.Fill)
                        {
                            using (Brush brush = new SolidBrush(shape.Color))
                            {
                                if (path.IsVisible(point))
                                {
                                    return true;
                                }
                            }
                        }
                        else
                        {
                            using (Pen pen = new Pen(shape.Color, shape.LineWidth + 3))
                            {
                                if (path.IsOutlineVisible(point, pen))
                                {
                                    return true;
                                }
                            }
                        }
                    }
                    else if (!(Shapes[i] is Group))
                    {
                        using (Pen pen = new Pen(Shapes[i].Color, Shapes[i].LineWidth + 3))
                        {
                            if (path.IsOutlineVisible(point, pen))
                            {
                                return true;
                            }
                        }
                    }
                    else if (Shapes[i] is Group group)
                    {
                        return group.IsHit(point);
                    }
                }
            }

            return false;
        }

        public override void Move(Point distance)
        {
            for (int i = 0; i < Shapes.Count; i++)
            {
                if (Shapes[i] is Curve curve)
                {
                    curve.Begin = new Point(curve.Begin.X + distance.X, curve.Begin.Y + distance.Y);
                    curve.End = new Point(curve.End.X + distance.X, curve.End.Y + distance.Y);

                    for (int j = 0; j < curve.Points.Count; j++)
                    {
                        curve.Points[j] = new Point(curve.Points[j].X + distance.X, curve.Points[j].Y + distance.Y);
                    }
                }
                else if (Shapes[i] is Polygon polygon)
                {
                    polygon.Begin = new Point(polygon.Begin.X + distance.X, polygon.Begin.Y + distance.Y);
                    polygon.End = new Point(polygon.End.X + distance.X, polygon.End.Y + distance.Y);

                    for (int j = 0; j < polygon.Points.Count; j++)
                    {
                        polygon.Points[j] = new Point(polygon.Points[j].X + distance.X, polygon.Points[j].Y + distance.Y);
                    }
                }
                else if (Shapes[i] is Group group)
                {
                    group.Move(distance);
                }
                else
                {
                    Shapes[i].Begin = new Point(Shapes[i].Begin.X + distance.X, Shapes[i].Begin.Y + distance.Y);
                    Shapes[i].End = new Point(Shapes[i].End.X + distance.X, Shapes[i].End.Y + distance.Y);
                }
            }
            Begin = new Point(Begin.X + distance.X, Begin.Y + distance.Y);
            End = new Point(End.X + distance.X, End.Y + distance.Y);
        }

        public override object Clone()
        {
            Group group = new Group
            {
                Begin = Begin,
                End = End,
                IsSelected = IsSelected,
                Name = Name,
                Color = Color.FromName(Color.Name),
                LineWidth = LineWidth
            };
            for (int i = 0; i < Shapes.Count; i++)
            {
                group.Shapes.Add(Shapes[i].Clone() as Shape);
            }
            return group;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Shapes.GetEnumerator();
        }

        public int Count => Shapes.Count;

        protected override GraphicsPath GraphicsPath => null;
    }
}
