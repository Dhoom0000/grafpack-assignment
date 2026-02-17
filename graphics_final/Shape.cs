using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace graphics_final
{
    abstract class Shape
    {
        public List<Point> vertices = new List<Point>();

        public int rotation = 0;
        public void rotate(int value)
        {
            Point center = vertices[0];

            float angleInRadians = value * (float)(Math.PI / 180);



            for (int i = 1; i < vertices.Count; i++)
            {
                Point vertex = vertices[i];
                float xTranslated = vertex.X - center.X;
                float yTranslated = vertex.Y - center.Y;

                float xRotated = xTranslated * (float)Math.Cos(angleInRadians) - yTranslated * (float)Math.Sin(angleInRadians);
                float yRotated = xTranslated * (float)Math.Sin(angleInRadians) + yTranslated * (float)Math.Cos(angleInRadians);

                vertices[i] = new Point((int)(xRotated + center.X), (int)(yRotated + center.Y));
            }

            rotation = value;
        }

        public abstract void draw(Graphics g);
        internal abstract Shape Clone();

        public bool isFilled = false;

        public Color strokeColor = Color.Black;
        public Color fillColor = Color.White;

    }

    class Square : Shape
    {
        
        public Square(List<Point> diagPoints)
        {
            Point oppPt, keyPt;
            double xDiff, yDiff, xMid, yMid;
            oppPt = diagPoints[0];
            keyPt = diagPoints[1];

            vertices = new List<Point>();

            xDiff = oppPt.X - keyPt.X;
            yDiff = oppPt.Y - keyPt.Y;
            xMid = (oppPt.X + keyPt.X) / 2;
            yMid = (oppPt.Y + keyPt.Y) / 2;


            vertices.Add(new Point((int)keyPt.X, (int)keyPt.Y));
            vertices.Add(new Point((int)(xMid + yDiff / 2), (int)(yMid - xDiff / 2)));
            vertices.Add(new Point((int)oppPt.X, (int)oppPt.Y));
            vertices.Add(new Point((int)(xMid - yDiff / 2), (int)(yMid + xDiff / 2)));
        }

        public override void draw(Graphics g)
        {
            if (vertices != null && vertices.Count > 3)
            {
                Point ptOne = vertices[0];
                Point ptTwo = vertices[1];
                Point ptThree = vertices[2];
                Point ptFour = vertices[3];
                Pen pen2 = new Pen(strokeColor);

                if (isFilled)
                {
                    Brush brush = new SolidBrush(fillColor);

                    g.FillPolygon(brush, vertices.ToArray());
                }

                // draw square
                g.DrawLine(pen2, ptOne.X, ptOne.Y, ptTwo.X, ptTwo.Y);
                g.DrawLine(pen2, ptTwo.X, ptTwo.Y, ptThree.X, ptThree.Y);
                g.DrawLine(pen2, ptThree.X, ptThree.Y, ptFour.X, ptFour.Y);
                g.DrawLine(pen2, ptFour.X, ptFour.Y, ptOne.X, ptOne.Y);
                
                return;
            }

            Console.WriteLine("Square : not enough vertices");

            
        }

        internal override Shape Clone()
        {
            var copiedVertices = new List<Point>(this.vertices);
            var square = new Square(new List<Point> { copiedVertices[0], copiedVertices[2] }) // Reconstruct using diagonal
            {
                rotation = this.rotation,
                isFilled = this.isFilled,
                strokeColor = this.strokeColor,
                fillColor = this.fillColor
            };
            return square;
        }

    }

    class Triangle : Shape
    {
        public Triangle(List<Point> vertexList)
        {
            vertices = new List<Point>(vertexList);
        }

        public override void draw(Graphics g)
        {
            if(vertices != null && vertices.Count > 2)
            {
                Pen pen = new Pen(strokeColor);

                if (isFilled)
                {
                    Brush brush = new SolidBrush(fillColor);

                    g.FillPolygon(brush, vertices.ToArray());
                }

                g.DrawLines(pen, new Point[] { vertices[0], vertices[1], vertices[2], vertices[0] });
            }
        }

        internal override Shape Clone()
        {
            var triangle = new Triangle(new List<Point>(this.vertices))
            {
                rotation = this.rotation,
                isFilled = this.isFilled,
                strokeColor = this.strokeColor,
                fillColor = this.fillColor
            };
            return triangle;
        }



    }

    class Circle : Shape
    {
        public Circle(List<Point> diameter)
        {
            vertices = new List<Point>(diameter);
        }

        public override void draw(Graphics g)
        {
            Point center = new Point();

            center.X = (vertices[0].X + vertices[1].X) / 2;
            center.Y = (vertices[0].Y + vertices[1].Y) / 2;

            int diameter = (int)Math.Sqrt(Math.Pow(vertices[1].X - vertices[0].X, 2) + Math.Pow(vertices[1].Y - vertices[0].Y, 2));

            Point topLeft = new Point(center.X - diameter / 2, center.Y - diameter / 2);

            Rectangle baseRect = new Rectangle(topLeft, new Size(diameter, diameter));

            Pen pen = new Pen(strokeColor);

            vertices.Add(new Point(baseRect.Left, baseRect.Top));
            vertices.Add(new Point(baseRect.Left, baseRect.Bottom));
            vertices.Add(new Point(baseRect.Right, baseRect.Top));
            vertices.Add(new Point(baseRect.Right, baseRect.Bottom));


            if (isFilled)
            {
                Brush brush = new SolidBrush(fillColor);

                baseRect = new Rectangle(vertices[2], new Size(-vertices[2].X + vertices[4].X, -vertices[2].Y + vertices[3].Y));

                g.FillEllipse(brush, baseRect);
            }

            g.DrawEllipse(pen, baseRect);
        }

        internal override Shape Clone()
        {
            var triangle = new Circle(new List<Point>(this.vertices))
            {
                rotation = this.rotation,
                isFilled = this.isFilled,
                strokeColor = this.strokeColor,
                fillColor = this.fillColor
            };
            return triangle;
        }

        

    }

    class Polygon : Shape
    {
        public Polygon(List<Point> vertexList)
        {
            vertices = new List<Point>(vertexList);
        }

        public override void draw(Graphics g)
        {
            Pen pen = new Pen(strokeColor);

            

            if (isFilled)
            {
                Brush brush = new SolidBrush(fillColor);

                g.FillPolygon(brush, vertices.ToArray());
            }

            g.DrawPolygon(pen, vertices.ToArray());
        }

        internal override Shape Clone()
        {
            var triangle = new Polygon(new List<Point>(this.vertices))
            {
                rotation = this.rotation,
                isFilled = this.isFilled,
                strokeColor = this.strokeColor,
                fillColor = this.fillColor
            };
            return triangle;
        }

    }

}
