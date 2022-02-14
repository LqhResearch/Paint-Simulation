using Guna.UI2.WinForms;
using PaintSimulation.shapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;

namespace PaintSimulation
{
    public partial class PaintMain : Form
    {
        #region Properties
        private List<Guna2ImageButton> buttons;
        private List<Shape> shapes = new List<Shape>();
        private CurrentShape currentShape = CurrentShape.NoDrawing;
        private Point previousPoint;
        private ShapeMode mode = ShapeMode.NoFill;
        private Brush brush = new SolidBrush(Color.Blue);
        private Pen framePen = new Pen(Color.Blue, 1)
        {
            DashPattern = new float[] { 3, 3, 3, 3 },
            DashStyle = DashStyle.Custom
        };
        private Shape selectedShape;
        private System.Drawing.Rectangle selectedRegion;
        private bool isMouseDown;
        private bool isDrawCurve;
        private bool isDrawPolygon;
        private bool isDrawBezier;
        private bool isMovingShape;
        private bool isControlKeyPress;
        private bool isMouseSelect;
        private int movingOffset;
        #endregion
        #region Constructor
        public PaintMain()
        {
            InitializeComponent();
        }

        private void PaintMain_Load(object sender, System.EventArgs e)
        {
            cboDashStyle.SelectedIndex = 0;
            buttons = new List<Guna2ImageButton> { btnLine, btnRectangle, btnEllipse, btnSquare, btnCircle, btnCurve, btnBezier, btnPolygon, btnSelect };

            Guna2HtmlToolTip toolTip = new Guna2HtmlToolTip();
            foreach (Control item in buttons)
                if (item.Tag != null)
                    toolTip.SetToolTip(item, item.Tag.ToString());

            foreach (Control item in grbColors.Controls)
                if (item.Tag != null)
                    toolTip.SetToolTip(item, item.Tag.ToString());
        }
        #endregion
        #region Common functions
        private void UncheckAll() // Bỏ chọn hết các button
        {
            buttons.ForEach(button => button.BackColor = Color.White);
        }

        private void EnableButtons() // Kích hoạt các button
        {
            buttons.ForEach(button => button.Enabled = true);
        }
        #endregion
        #region Find region
        private void FindCurveRegion(Curve curve) // Tìm cái khung chứa đường cong
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            curve.Points.ForEach(p =>
            {
                if (minX > p.X) minX = p.X;
                if (minY > p.Y) minY = p.Y;
                if (maxX < p.X) maxX = p.X;
                if (maxY < p.Y) maxY = p.Y;
            });
            curve.Begin = new Point(minX, minY);
            curve.End = new Point(maxX, maxY);
        }

        private void FindPolygonRegion(Polygon polygon) // Tìm cái khung chứa đa giác này
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            polygon.Points.ForEach(p =>
            {
                if (minX > p.X) minX = p.X;
                if (minY > p.Y) minY = p.Y;
                if (maxX < p.X) maxX = p.X;
                if (maxY < p.Y) maxY = p.Y;
            });
            polygon.Begin = new Point(minX, minY);
            polygon.End = new Point(maxX, maxY);
        }

        private void FindGroupRegion(Group group) // Tìm cái khung chứa group này
        {
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            for (int i = 0; i < group.Count; i++)
            {
                Shape shape = group[i];

                if (shape is Curve curve)
                    FindCurveRegion(curve);
                else if (shape is Polygon polygon)
                    FindPolygonRegion(polygon);

                if (shape.Begin.X < minX) minX = shape.Begin.X;
                if (shape.End.X < minX) minX = shape.End.X;

                if (shape.Begin.Y < minY) minY = shape.Begin.Y;
                if (shape.End.Y < minY) minY = shape.End.Y;

                if (shape.Begin.X > maxX) maxX = shape.Begin.X;
                if (shape.End.X > maxX) maxX = shape.End.X;

                if (shape.Begin.Y > maxY) maxY = shape.Begin.Y;
                if (shape.End.Y > maxY) maxY = shape.End.Y;
            }
            group.Begin = new Point(minX, minY);
            group.End = new Point(maxX, maxY);
        }
        #endregion
        #region Move selected shapes
        private void MoveShape(Action<int> action) // Di chuyển các hình đã chọn
        {
            for (int i = 0; i < shapes.Count; i++)
                if (shapes[i].IsSelected)
                    action(i);
            picPaint.Invalidate();
        }

        private void ToUp(int index) // Di chuyển hình ở vị trí index lên trên
        {
            Shape shape = shapes[index];
            if (shape is Curve curve)
            {
                for (int j = 0; j < curve.Points.Count; j++)
                    curve.Points[j] = new Point(curve.Points[j].X, curve.Points[j].Y - movingOffset);
            }
            else if (shape is Polygon polygon)
            {
                for (int j = 0; j < polygon.Points.Count; j++)
                    polygon.Points[j] = new Point(polygon.Points[j].X, polygon.Points[j].Y - movingOffset);
            }
            else if (shape is Group group)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    Shape s = group[i];
                    if (s is Curve c)
                    {
                        for (int j = 0; j < c.Points.Count; j++)
                            c.Points[j] = new Point(c.Points[j].X, c.Points[j].Y - movingOffset);
                    }
                    else if (s is Polygon p)
                    {
                        for (int j = 0; j < p.Points.Count; j++)
                            p.Points[j] = new Point(p.Points[j].X, p.Points[j].Y - movingOffset);
                    }
                    s.Begin = new Point(s.Begin.X, s.Begin.Y - movingOffset);
                    s.End = new Point(s.End.X, s.End.Y - movingOffset);
                }
            }
            shape.Begin = new Point(shape.Begin.X, shape.Begin.Y - movingOffset);
            shape.End = new Point(shape.End.X, shape.End.Y - movingOffset);
        }

        private void ToDown(int index) // Di chuyển hình ở vị trí index xuống dưới
        {
            Shape shape = shapes[index];
            if (shape is Curve curve)
            {
                for (int j = 0; j < curve.Points.Count; j++)
                    curve.Points[j] = new Point(curve.Points[j].X, curve.Points[j].Y + movingOffset);
            }
            else if (shape is Polygon polygon)
            {
                for (int j = 0; j < polygon.Points.Count; j++)
                    polygon.Points[j] = new Point(polygon.Points[j].X, polygon.Points[j].Y + movingOffset);
            }
            else if (shape is Group group)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    Shape s = group[i];
                    if (s is Curve c)
                    {
                        for (int j = 0; j < c.Points.Count; j++)
                            c.Points[j] = new Point(c.Points[j].X, c.Points[j].Y + movingOffset);
                    }
                    else if (s is Polygon p)
                    {
                        for (int j = 0; j < p.Points.Count; j++)
                            p.Points[j] = new Point(p.Points[j].X, p.Points[j].Y + movingOffset);
                    }
                    s.Begin = new Point(s.Begin.X, s.Begin.Y + movingOffset);
                    s.End = new Point(s.End.X, s.End.Y + movingOffset);
                }
            }
            shape.Begin = new Point(shape.Begin.X, shape.Begin.Y + movingOffset);
            shape.End = new Point(shape.End.X, shape.End.Y + movingOffset);
        }

        private void ToLeft(int index) // Di chuyển hình ở vị trí index sang trái
        {
            Shape shape = shapes[index];
            if (shape is Curve curve)
            {
                for (int j = 0; j < curve.Points.Count; j++)
                    curve.Points[j] = new Point(curve.Points[j].X - movingOffset, curve.Points[j].Y);
            }
            else if (shape is Polygon polygon)
            {
                for (int j = 0; j < polygon.Points.Count; j++)
                    polygon.Points[j] = new Point(polygon.Points[j].X - movingOffset, polygon.Points[j].Y);
            }
            else if (shape is Group group)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    Shape s = group[i];
                    if (s is Curve c)
                    {
                        for (int j = 0; j < c.Points.Count; j++)
                            c.Points[j] = new Point(c.Points[j].X - movingOffset, c.Points[j].Y);
                    }
                    else if (s is Polygon p)
                    {
                        for (int j = 0; j < p.Points.Count; j++)
                            p.Points[j] = new Point(p.Points[j].X - movingOffset, p.Points[j].Y);
                    }
                    s.Begin = new Point(s.Begin.X - movingOffset, s.Begin.Y);
                    s.End = new Point(s.End.X - movingOffset, s.End.Y);
                }
            }
            shape.Begin = new Point(shape.Begin.X - movingOffset, shape.Begin.Y);
            shape.End = new Point(shape.End.X - movingOffset, shape.End.Y);
        }

        private void ToRight(int index) // Di chuyển hình ở vị trí index sang phải
        {
            Shape shape = shapes[index];
            if (shape is Curve curve)
            {
                for (int j = 0; j < curve.Points.Count; j++)
                    curve.Points[j] = new Point(curve.Points[j].X + movingOffset, curve.Points[j].Y);
            }
            else if (shape is Polygon polygon)
            {
                for (int j = 0; j < polygon.Points.Count; j++)
                    polygon.Points[j] = new Point(polygon.Points[j].X + movingOffset, polygon.Points[j].Y);
            }
            else if (shape is Group group)
            {
                for (int i = 0; i < group.Count; i++)
                {
                    Shape s = group[i];
                    if (s is Curve c)
                    {
                        for (int j = 0; j < c.Points.Count; j++)
                            c.Points[j] = new Point(c.Points[j].X + movingOffset, c.Points[j].Y);
                    }
                    else if (s is Polygon p)
                    {
                        for (int j = 0; j < p.Points.Count; j++)
                            p.Points[j] = new Point(p.Points[j].X + movingOffset, p.Points[j].Y);
                    }
                    s.Begin = new Point(s.Begin.X + movingOffset, s.Begin.Y);
                    s.End = new Point(s.End.X + movingOffset, s.End.Y);
                }
            }
            shape.Begin = new Point(shape.Begin.X + movingOffset, shape.Begin.Y);
            shape.End = new Point(shape.End.X + movingOffset, shape.End.Y);
        }
        #endregion
        #region Button shapes
        private void btnLine_Click(object sender, EventArgs e)
        {
            shapes.ForEach(shape => shape.IsSelected = false);
            picPaint.Invalidate();

            for (int i = 0; i < clbShapes.Items.Count; i++)
                clbShapes.SetItemChecked(i, false);

            if (btnLine.BackColor == Color.Silver)
            {
                UncheckAll();
                currentShape = CurrentShape.NoDrawing;
                picPaint.Cursor = Cursors.Default;
                btnSelect.BackColor = Color.Silver;
            }
            else
            {
                UncheckAll();
                picPaint.Cursor = Cursors.Cross;
                currentShape = CurrentShape.Line;
                btnLine.BackColor = Color.Silver;
            }
        }

        private void btnRectangle_Click(object sender, EventArgs e)
        {
            shapes.ForEach(shape => shape.IsSelected = false);
            picPaint.Invalidate();

            for (int i = 0; i < clbShapes.Items.Count; i++)
                clbShapes.SetItemChecked(i, false);

            if (btnRectangle.BackColor == Color.Silver)
            {
                UncheckAll();
                currentShape = CurrentShape.NoDrawing;
                picPaint.Cursor = Cursors.Default;
                btnSelect.BackColor = Color.Silver;
            }
            else
            {
                UncheckAll();
                picPaint.Cursor = Cursors.Cross;
                currentShape = CurrentShape.Rectangle;
                btnRectangle.BackColor = Color.Silver;
            }
        }

        private void btnEllipse_Click(object sender, EventArgs e)
        {
            shapes.ForEach(shape => shape.IsSelected = false);
            picPaint.Invalidate();

            for (int i = 0; i < clbShapes.Items.Count; i++)
                clbShapes.SetItemChecked(i, false);

            if (btnEllipse.BackColor == Color.Silver)
            {
                UncheckAll();
                currentShape = CurrentShape.NoDrawing;
                picPaint.Cursor = Cursors.Default;
                btnSelect.BackColor = Color.Silver;
            }
            else
            {
                UncheckAll();
                picPaint.Cursor = Cursors.Cross;
                currentShape = CurrentShape.Ellipse;
                btnEllipse.BackColor = Color.Silver;
            }
        }

        private void btnSquare_Click(object sender, EventArgs e)
        {
            shapes.ForEach(shape => shape.IsSelected = false);
            picPaint.Invalidate();

            for (int i = 0; i < clbShapes.Items.Count; i++)
                clbShapes.SetItemChecked(i, false);

            if (btnSquare.BackColor == Color.Silver)
            {
                UncheckAll();
                currentShape = CurrentShape.NoDrawing;
                picPaint.Cursor = Cursors.Default;
                btnSelect.BackColor = Color.Silver;
            }
            else
            {
                UncheckAll();
                picPaint.Cursor = Cursors.Cross;
                currentShape = CurrentShape.Square;
                btnSquare.BackColor = Color.Silver;
            }
        }

        private void btnCircle_Click(object sender, EventArgs e)
        {
            shapes.ForEach(shape => shape.IsSelected = false);
            picPaint.Invalidate();

            for (int i = 0; i < clbShapes.Items.Count; i++)
                clbShapes.SetItemChecked(i, false);

            if (btnCircle.BackColor == Color.Silver)
            {
                UncheckAll();
                currentShape = CurrentShape.NoDrawing;
                picPaint.Cursor = Cursors.Default;
                btnSelect.BackColor = Color.Silver;
            }
            else
            {
                UncheckAll();
                picPaint.Cursor = Cursors.Cross;
                currentShape = CurrentShape.Circle;
                btnCircle.BackColor = Color.Silver;
            }
        }

        private void btnCurve_Click(object sender, EventArgs e)
        {
            shapes.ForEach(shape => shape.IsSelected = false);
            picPaint.Invalidate();

            for (int i = 0; i < clbShapes.Items.Count; i++)
                clbShapes.SetItemChecked(i, false);

            if (btnCurve.BackColor == Color.Silver)
            {
                UncheckAll();
                currentShape = CurrentShape.NoDrawing;
                picPaint.Cursor = Cursors.Default;
                btnSelect.BackColor = Color.Silver;
            }
            else
            {
                UncheckAll();
                picPaint.Cursor = Cursors.Cross;
                currentShape = CurrentShape.Curve;
                btnCurve.BackColor = Color.Silver;
            }
        }

        private void btnBezier_Click(object sender, EventArgs e)
        {
            shapes.ForEach(shape => shape.IsSelected = false);
            picPaint.Invalidate();

            for (int i = 0; i < clbShapes.Items.Count; i++)
                clbShapes.SetItemChecked(i, false);

            if (btnBezier.BackColor == Color.Silver)
            {
                UncheckAll();
                currentShape = CurrentShape.NoDrawing;
                picPaint.Cursor = Cursors.Default;
                btnSelect.BackColor = Color.Silver;
            }
            else
            {
                UncheckAll();
                picPaint.Cursor = Cursors.Cross;
                currentShape = CurrentShape.Bezier;
                btnBezier.BackColor = Color.Silver;
            }
        }

        private void btnPolygon_Click(object sender, EventArgs e)
        {
            shapes.ForEach(shape => shape.IsSelected = false);
            picPaint.Invalidate();

            for (int i = 0; i < clbShapes.Items.Count; i++)
                clbShapes.SetItemChecked(i, false);

            if (btnPolygon.BackColor == Color.Silver)
            {
                UncheckAll();
                currentShape = CurrentShape.NoDrawing;
                picPaint.Cursor = Cursors.Default;
                btnSelect.BackColor = Color.Silver;
            }
            else
            {
                UncheckAll();
                picPaint.Cursor = Cursors.Cross;
                currentShape = CurrentShape.Polygon;
                btnPolygon.BackColor = Color.Silver;
            }
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            shapes.ForEach(shape => shape.IsSelected = false);
            picPaint.Invalidate();

            for (int i = 0; i < clbShapes.Items.Count; i++)
                clbShapes.SetItemChecked(i, false);

            currentShape = CurrentShape.NoDrawing;
            UncheckAll();
            btnSelect.BackColor = Color.Silver;
            picPaint.Cursor = Cursors.Default;
        }
        #endregion
        #region Handle button
        private void btnGroup_Click(object sender, EventArgs e)
        {
            if (shapes.Count(shape => shape.IsSelected) > 1)
            {
                Group group = new Group();
                for (int i = 0; i < shapes.Count; i++)
                {
                    if (shapes[i].IsSelected)
                    {
                        group.Add(shapes[i]);
                        shapes.RemoveAt(i);
                        clbShapes.Items.RemoveAt(i--);
                    }
                }
                FindGroupRegion(group);
                shapes.Add(group);
                clbShapes.Items.Add(group);
                group.IsSelected = true;
                picPaint.Invalidate();
            }
        }

        private void btnUngroup_Click(object sender, EventArgs e)
        {
            if (shapes.Find(shape => shape.IsSelected) is Group selectedGroup)
            {
                foreach (Shape shape in selectedGroup)
                {
                    shapes.Add(shape);
                    clbShapes.Items.Add(shape.ToString());
                }
                shapes.Remove(selectedGroup);
                clbShapes.Items.Clear();
                foreach (Shape shape in shapes)
                    clbShapes.Items.Add(shape.ToString());
            }
            picPaint.Invalidate();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            bool isFillMode = ckbFillShape.Checked == true;
            for (int i = 0; i < shapes.Count; i++)
            {
                if (shapes[i].IsSelected)
                {
                    shapes.RemoveAt(i);
                    clbShapes.Items.RemoveAt(i--);
                }
            }
            picPaint.Invalidate();
            if (isFillMode)
                ckbFillShape.Checked = true;
        }

        private void btnColor_Click(object sender, EventArgs e)
        {
            ColorDialog color = new ColorDialog();
            if (color.ShowDialog() == DialogResult.OK)
                btnColor.FillColor = color.Color;

            shapes.FindAll(shape => shape.IsSelected).ForEach(shape =>
            {
                if (shape is Group group)
                {
                    foreach (Shape s in group)
                        s.Color = btnColor.BackColor;
                }
                else
                    shape.Color = btnColor.BackColor;
            });
            picPaint.Invalidate();
        }

        private void btnColors_Click(object sender, EventArgs e)
        {
            btnColor.FillColor = ((Guna2Button)sender).FillColor;
            btnColor.BorderThickness = ((Guna2Button)sender).BorderThickness;
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }
        #endregion
        #region Form PaintMain
        private void PaintMain_KeyDown(object sender, KeyEventArgs e)
        {
            isControlKeyPress = e.Control;
            picPaint.Focus();

            movingOffset = e.Control == true ? 1 : 5;

            if (e.KeyCode == Keys.Up)
                MoveShape(ToUp);
            else if (e.KeyCode == Keys.Down)
                MoveShape(ToDown);
            else if (e.KeyCode == Keys.Left)
                MoveShape(ToLeft);
            else if (e.KeyCode == Keys.Right)
                MoveShape(ToRight);
            else if (e.KeyCode == Keys.Delete)
                btnDelete.PerformClick();
        }

        private void PaintMain_KeyUp(object sender, KeyEventArgs e)
        {
            isControlKeyPress = e.Control;
        }
        #endregion
        #region PictureBox picPaint
        private void picPaint_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            shapes.ForEach(shape =>
            {
                if (shape.IsSelected)
                {
                    shape.Draw(e.Graphics);
                    if (shape is Ellipse || shape is Group)
                        e.Graphics.DrawRectangle(framePen, new System.Drawing.Rectangle(shape.Begin.X, shape.Begin.Y, shape.End.X - shape.Begin.X, shape.End.Y - shape.Begin.Y));
                    else if (shape is Curve curve)
                    {
                        for (int i = 0; i < curve.Points.Count; i++)
                            e.Graphics.FillEllipse(brush, new System.Drawing.Rectangle(curve.Points[i].X - 4, curve.Points[i].Y - 4, 8, 8));
                    }
                    else if (shape is Polygon polygon)
                    {
                        for (int i = 0; i < polygon.Points.Count; i++)
                            e.Graphics.FillEllipse(brush, new System.Drawing.Rectangle(polygon.Points[i].X - 4, polygon.Points[i].Y - 4, 8, 8));
                    }
                    else
                    {
                        e.Graphics.FillEllipse(brush, new System.Drawing.Rectangle(shape.Begin.X - 4, shape.Begin.Y - 4, 8, 8));
                        e.Graphics.FillEllipse(brush, new System.Drawing.Rectangle(shape.End.X - 4, shape.End.Y - 4, 8, 8));
                    }
                }
                else
                    shape.Draw(e.Graphics);
            });

            if (isMouseSelect)
                e.Graphics.DrawRectangle(framePen, selectedRegion);
        }

        private void picPaint_MouseDown(object sender, MouseEventArgs e)
        {
            if (currentShape == CurrentShape.NoDrawing)
            {
                if (isControlKeyPress)
                {
                    for (int i = 0; i < shapes.Count; i++)
                    {
                        if (shapes[i].IsHit(e.Location))
                        {
                            shapes[i].IsSelected = !shapes[i].IsSelected;
                            clbShapes.SetItemChecked(i, shapes[i].IsSelected);
                            picPaint.Invalidate();
                            break;
                        }
                    }
                }
                else
                {
                    shapes.ForEach(shape => shape.IsSelected = false);
                    picPaint.Invalidate();

                    for (int i = 0; i < clbShapes.Items.Count; i++)
                        clbShapes.SetItemChecked(i, false);

                    for (int i = 0; i < shapes.Count; i++)
                    {
                        if (shapes[i].IsHit(e.Location))
                        {
                            selectedShape = shapes[i];
                            shapes[i].IsSelected = true;
                            clbShapes.SetItemChecked(i, true);

                            if (!(shapes[i] is Group))
                            {
                                trbSize.Value = shapes[i].LineWidth;
                                btnColor.BackColor = shapes[i].Color;
                                lblWidth.Text = trbSize.Value.ToString();
                            }
                            picPaint.Invalidate();
                            break;
                        }
                    }

                    if (selectedShape != null)
                    {
                        isMovingShape = true;
                        previousPoint = e.Location;
                    }
                    else
                    {
                        isMouseSelect = true;
                        selectedRegion = new System.Drawing.Rectangle(e.Location, new Size(0, 0));
                    }
                }
            }
            else
            {
                isMouseDown = true;
                shapes.ForEach(shape => shape.IsSelected = false);
                if (currentShape == CurrentShape.Line)
                {
                    Line line = new Line
                    {
                        Begin = e.Location,
                        LineWidth = trbSize.Value,
                        Color = btnColor.FillColor,
                        DashStyle = (DashStyle)cboDashStyle.SelectedIndex
                    };
                    shapes.Add(line);
                }
                else if (currentShape == CurrentShape.Rectangle)
                {
                    shapes.Rectangle rectangle = new shapes.Rectangle
                    {
                        Begin = e.Location,
                        LineWidth = trbSize.Value,
                        Color = btnColor.FillColor,
                        DashStyle = (DashStyle)cboDashStyle.SelectedIndex
                    };
                    if (mode == ShapeMode.Fill)
                        rectangle.Fill = true;
                    shapes.Add(rectangle);
                }
                else if (currentShape == CurrentShape.Ellipse)
                {
                    Ellipse ellipse = new Ellipse
                    {
                        Begin = e.Location,
                        LineWidth = trbSize.Value,
                        Color = btnColor.FillColor,
                        DashStyle = (DashStyle)cboDashStyle.SelectedIndex
                    };

                    if (mode == ShapeMode.Fill)
                        ellipse.Fill = true;
                    shapes.Add(ellipse);
                }
                else if (currentShape == CurrentShape.Square)
                {
                    Square square = new Square
                    {
                        Begin = e.Location,
                        LineWidth = trbSize.Value,
                        Color = btnColor.FillColor,
                        DashStyle = (DashStyle)cboDashStyle.SelectedIndex
                    };
                    if (mode == ShapeMode.Fill)
                        square.Fill = true;
                    shapes.Add(square);
                }
                else if (currentShape == CurrentShape.Circle)
                {
                    Circle circle = new Circle
                    {
                        Begin = e.Location,
                        LineWidth = trbSize.Value,
                        Color = btnColor.FillColor,
                        DashStyle = (DashStyle)cboDashStyle.SelectedIndex
                    };
                    if (mode == ShapeMode.Fill)
                        circle.Fill = true;
                    shapes.Add(circle);
                }
                else if (currentShape == CurrentShape.Polygon)
                {
                    if (!isDrawPolygon)
                    {
                        Polygon polygon = new Polygon
                        {
                            LineWidth = trbSize.Value,
                            Color = btnColor.FillColor,
                            DashStyle = (DashStyle)cboDashStyle.SelectedIndex
                        };
                        if (mode == ShapeMode.Fill)
                            polygon.Fill = true;
                        polygon.Points.Add(e.Location);
                        polygon.Points.Add(e.Location);
                        shapes.Add(polygon);
                        isDrawPolygon = true;
                    }
                    else
                    {
                        Polygon polygon = shapes[shapes.Count - 1] as Polygon;
                        polygon.Points[polygon.Points.Count - 1] = e.Location;
                        polygon.Points.Add(e.Location);
                    }
                    isMouseDown = false;
                }
                else if (currentShape == CurrentShape.Curve)
                {
                    if (!isDrawCurve)
                    {
                        Curve curve = new Curve
                        {
                            LineWidth = trbSize.Value,
                            Color = btnColor.FillColor,
                            DashStyle = (DashStyle)cboDashStyle.SelectedIndex
                        };

                        curve.Points.Add(e.Location);
                        curve.Points.Add(e.Location);
                        shapes.Add(curve);
                        isDrawCurve = true;
                    }
                    else
                    {
                        Curve curve = shapes[shapes.Count - 1] as Curve;
                        curve.Points[curve.Points.Count - 1] = e.Location;
                        curve.Points.Add(e.Location);
                    }
                    isMouseDown = false;
                }
                else if (currentShape == CurrentShape.Bezier)
                {
                    if (!isDrawBezier)
                    {
                        Curve bezier = new Curve
                        {
                            LineWidth = trbSize.Value,
                            Color = btnColor.FillColor,
                            DashStyle = (DashStyle)cboDashStyle.SelectedIndex
                        };
                        bezier.Points.Add(e.Location);
                        bezier.Points.Add(e.Location);
                        shapes.Add(bezier);
                        isDrawBezier = true;
                    }
                    else
                    {
                        Curve bezier = shapes[shapes.Count - 1] as Curve;
                        if (bezier.Points.Count < 4)
                        {
                            bezier.Points[bezier.Points.Count - 1] = e.Location;
                            bezier.Points.Add(e.Location);
                        }
                        else
                        {
                            isDrawBezier = false;
                            FindCurveRegion(bezier);
                        }
                    }
                    isMouseDown = false;
                }
            }
        }

        private void picPaint_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMouseDown)
            {
                shapes[shapes.Count - 1].End = e.Location;
                picPaint.Invalidate();
            }
            else if (isMovingShape)
            {
                Point d = new Point(e.X - previousPoint.X, e.Y - previousPoint.Y);
                selectedShape.Move(d);
                previousPoint = e.Location;
                picPaint.Invalidate();
            }
            else if (currentShape == CurrentShape.NoDrawing)
            {
                if (isMouseSelect)
                {
                    selectedRegion.Width = e.Location.X - selectedRegion.X;
                    selectedRegion.Height = e.Location.Y - selectedRegion.Y;
                    picPaint.Invalidate();
                }
                else
                {
                    if (shapes.Exists(shape => shape.IsHit(e.Location)))
                        picPaint.Cursor = Cursors.SizeAll;
                    else
                        picPaint.Cursor = Cursors.Default;
                }
            }

            if (isDrawPolygon)
            {
                Polygon polygon = shapes[shapes.Count - 1] as Polygon;
                polygon.Points[polygon.Points.Count - 1] = e.Location;
                picPaint.Invalidate();
            }
            else if (isDrawCurve)
            {
                Curve curve = shapes[shapes.Count - 1] as Curve;
                curve.Points[curve.Points.Count - 1] = e.Location;
                picPaint.Invalidate();
            }
            else if (isDrawBezier)
            {
                Curve bezier = shapes[shapes.Count - 1] as Curve;
                bezier.Points[bezier.Points.Count - 1] = e.Location;
                picPaint.Invalidate();
            }
        }

        private void picPaint_MouseUp(object sender, MouseEventArgs e)
        {
            isMouseDown = false;
            if (isMovingShape)
            {
                isMovingShape = false;
                selectedShape = null;
            }
            else if (isMouseSelect)
            {
                isMouseSelect = false;
                for (int i = 0; i < shapes.Count; i++)
                {
                    clbShapes.SetItemChecked(i, false);
                    shapes[i].IsSelected = false;
                    if (shapes[i].Begin.X >= selectedRegion.X && shapes[i].End.X <= selectedRegion.X + selectedRegion.Width && shapes[i].Begin.Y >= selectedRegion.Y && shapes[i].End.Y <= selectedRegion.Y + selectedRegion.Height)
                    {
                        clbShapes.SetItemChecked(i, true);
                        shapes[i].IsSelected = true;
                    }
                }
                picPaint.Invalidate();
            }

            try
            {
                Shape shape = shapes[shapes.Count - 1];
                // Đổi 2 điểm lại cho thuận nếu bị ngược
                if (shape.Begin.X > shape.End.X || (shape.Begin.X == shape.End.X && shape.Begin.Y > shape.End.Y))
                {
                    Point temp = shape.Begin;
                    shape.Begin = shape.End;
                    shape.End = temp;
                }

                if (shape is Circle circle)
                    circle.End = new Point(circle.Begin.X + circle.Diameter, circle.Begin.Y + circle.Diameter);
                else if (shape is Square square)
                {
                    if (square.Begin.X < square.End.X && square.Begin.Y > square.End.Y)
                    {
                        square.Begin = new Point(square.Begin.X, square.End.Y);
                        square.End = new Point(square.Begin.X + square.Width, square.Begin.Y + square.Width);
                    }
                    else
                        square.End = new Point(square.Begin.X + square.Width, square.Begin.Y + square.Width);
                }
                else if (shape is shapes.Rectangle rect)
                {
                    if (rect.Begin.X < rect.End.X && rect.Begin.Y > rect.End.Y)
                    {
                        Point begin = rect.Begin, end = rect.End;
                        rect.Begin = new Point(begin.X, end.Y);
                        rect.End = new Point(end.X, begin.Y);
                    }
                }

                if (currentShape != CurrentShape.NoDrawing)
                {
                    if (shape is Curve)
                    {
                        if (currentShape == CurrentShape.Curve && !isDrawCurve) // Chỉ ghi khi đã vẽ xong đường cong đó
                            clbShapes.Items.Add(shape.ToString());
                        else if (currentShape == CurrentShape.Bezier && !isDrawBezier)
                            clbShapes.Items.Add(shape.ToString());
                    }
                    else if (shape is Polygon) // Ngược lại nếu là đa giác
                    {
                        if (!isDrawPolygon)
                            clbShapes.Items.Add(shape.ToString());
                    }
                    else // Ngược lại không là đường cong thì ghi bình thường
                        clbShapes.Items.Add(shape.ToString());
                }
            }
            catch { }
        }

        private void picPaint_DoubleClick(object sender, EventArgs e)
        {
            if (isDrawPolygon)
            {
                isDrawPolygon = false;
                Polygon polygon = shapes[shapes.Count - 1] as Polygon;
                polygon.Points.RemoveAt(polygon.Points.Count - 1);
                picPaint.Invalidate();
                FindPolygonRegion(polygon);
            }
            else if (isDrawCurve)
            {
                isDrawCurve = false;
                Curve curve = shapes[shapes.Count - 1] as Curve;
                curve.Points.RemoveAt(curve.Points.Count - 1);
                curve.Points.RemoveAt(curve.Points.Count - 1);
                picPaint.Invalidate();
                FindCurveRegion(curve);
            }
        }
        #endregion

        private void cboDashStyle_SelectedIndexChanged(object sender, EventArgs e)
        {
            shapes.FindAll(shape => shape.IsSelected).ForEach(shape =>
            {
                shape.DashStyle = (DashStyle)cboDashStyle.SelectedIndex;
            });
            picPaint.Invalidate();
        }

        private void clbShapes_SelectedIndexChanged(object sender, EventArgs e)
        {
            UncheckAll();
            EnableButtons();
            btnSelect.BackColor = Color.Silver;
            picPaint.Cursor = Cursors.Default;
            currentShape = CurrentShape.NoDrawing;
            trbSize.Enabled = true;

            if (clbShapes.SelectedIndex < 0)
                return;

            for (int i = 0; i < clbShapes.Items.Count; i++)
                shapes[i].IsSelected = clbShapes.GetItemChecked(i);
            picPaint.Invalidate();
        }

        private void ckbFillShape_CheckedChanged(object sender, EventArgs e)
        {
            btnSelect.PerformClick();
            if (!ckbFillShape.Checked)
            {
                mode = ShapeMode.NoFill;
                EnableButtons();
                trbSize.Enabled = true;
                cboDashStyle.Enabled = true;
            }
            else
            {
                mode = ShapeMode.Fill;
                trbSize.Enabled = false;
                btnLine.Enabled = btnCurve.Enabled = btnBezier.Enabled = false;
                cboDashStyle.Enabled = false;
            }
        }

        private void ckbSelectAll_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < clbShapes.Items.Count; i++)
            {
                clbShapes.SetItemChecked(i, ckbSelectAll.Checked);
                shapes[i].IsSelected = ckbSelectAll.Checked;
            }
            picPaint.Invalidate();
        }

        private void tgwDarkMode_CheckedChanged(object sender, EventArgs e)
        {
            // Group shapes
            foreach (Guna2ImageButton item in buttons)
                item.BackColor = Color.White;

            // Group colors
            lblColor.BackColor = Color.White;

            // Group styles
            ckbFillShape.BackColor = Color.White;
            lblSize.BackColor = trbSize.BackColor = lblWidth.BackColor = Color.White;

            // Group modes
            lblDarkMode.BackColor = tgwDarkMode.BackColor = Color.White;

            // Gruop exit
            btnExit.BackColor = Color.White;

            if (tgwDarkMode.Checked)
            {
                pnlMenu.BackColor = pnlControl.BackColor = Color.Black;

                ckbSelectAll.ForeColor = Color.White;
                clbShapes.BackColor = Color.Black;
                for (int i = 0; i < clbShapes.Items.Count; i++)
                    clbShapes.ForeColor = Color.White;
            }
            else
            {
                pnlMenu.BackColor = pnlControl.BackColor = Color.White;

                ckbSelectAll.ForeColor = Color.Black;
                clbShapes.BackColor = Color.White;
                for (int i = 0; i < clbShapes.Items.Count; i++)
                    clbShapes.ForeColor = Color.Black;
            }
        }

        private void trbSize_Scroll(object sender, ScrollEventArgs e)
        {
            lblWidth.Text = trbSize.Value == 1 ? "Default" : trbSize.Value.ToString() + "px";
            shapes.FindAll(shape => shape.IsSelected).ForEach(shape =>
            {
                if (shape is Group group)
                {
                    foreach (Shape s in group)
                        s.LineWidth = trbSize.Value;
                }
                else
                    shape.LineWidth = trbSize.Value;
            });
            picPaint.Invalidate();
        }
    }
}