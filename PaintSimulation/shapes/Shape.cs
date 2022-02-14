using System.Drawing;
using System.Drawing.Drawing2D;

namespace PaintSimulation.shapes
{
    public abstract class Shape
    {
        public Point Begin { get; set; }
        public Point End { get; set; }
        public Color Color { get; set; }
        public int LineWidth { get; set; }
        public bool IsSelected { get; set; }
        public string Name { get; protected set; }
        public DashStyle DashStyle { get; set; } = DashStyle.Solid;
        protected abstract GraphicsPath GraphicsPath { get; }
        public abstract bool IsHit(Point point);
        public abstract void Draw(Graphics graphics);
        public abstract void Move(Point distance);
        public abstract object Clone();
        public override string ToString()
        {
            return $"{Name} [({Begin.X}, {Begin.Y}); ({End.X}, {End.Y})]";
        }
    }
}
