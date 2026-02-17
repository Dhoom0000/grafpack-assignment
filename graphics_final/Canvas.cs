using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace graphics_final
{
    public partial class Canvas: Form
    {
        // This struct will store our colour values
        private struct Palette
        {
            public Color strokeColor;
            public Color fillColor;
            public Color selectColor;
            public Color canvasColor;
        }

        // Colour Palette variable
        Palette myColours = new Palette
        {
            strokeColor = Color.Black,
            fillColor = Color.Crimson,
            selectColor = Color.Blue,
            canvasColor = Color.White,
        };

        // List to store mouse down event Points :: To be used as vertices for shapes
        private List<Point> mouseDownStack = new List<Point>();
        private int clicknumber = 0; // to increment the amount of points clicked

        // Keeps track of all drawn shapes
        private List<Shape> shapeStack = new List<Shape>();

        // an enum type to store CRUD updates
        private enum Actions
        {
            Create,
            Update,
            Delete,
        };

        // store information about original and final shapes
        private struct Updates
        {
            public Actions action;
            public Shape initialShape;
            public Shape newShape;
        }

        // Updates variable
        private List<Updates> changes = new List<Updates>();

        private bool isSelecting = false; // to check if a shape is selected
        private bool isMoving = false; // to check if its possible to move selected shape

        private Shape selectedShape = null; // to store the last selected shape

        // to simplify actions based on menu selection :: for use in mouseclick events
        private enum MenuSelection
        {
            Square,
            Triangle,
            Circle,
            Polygon,
            Select,
            Transform,
            Colours,
        }

        // Menu selection varible
        private MenuSelection selectedOption = MenuSelection.Square;

        // stores whether its first time use by the user :: used to show tool tips
        private Dictionary<MenuSelection,bool> isFirstUse = new Dictionary<MenuSelection,bool>();

        private Bitmap backBuffer; // to be used as primary drawing board :: later drawn onto main canvas for smoothness

        private bool toBeFilled = false; // checks whether fill option is on/off

        private MainMenu menu; // our mainmenu for tools

        private Point initialMouseDownPosition = new Point(0, 0); // to compare and drag drop shapes


        public Canvas()
        {
            InitializeComponent();

            // clear all lists
            mouseDownStack.Clear();
            shapeStack.Clear();
            changes.Clear();
            isFirstUse.Clear();

            // init first-use for tools
            isFirstUse.Add(MenuSelection.Square, true);
            isFirstUse.Add(MenuSelection.Triangle, true);
            isFirstUse.Add(MenuSelection.Circle, true);
            isFirstUse.Add(MenuSelection.Polygon, true);
            isFirstUse.Add(MenuSelection.Select, true);
            isFirstUse.Add(MenuSelection.Transform, true);
            isFirstUse.Add(MenuSelection.Colours, true);

            

            // style the window
            this.WindowState = FormWindowState.Maximized;
            this.MaximizeBox = false;
            this.BackColor = myColours.canvasColor;
            this.Text = "MyCanvas";

            this.Size = new Size(Screen.FromControl(this).Bounds.Width, Screen.FromControl(this).Bounds.Height);

            // clear and init the back buffer
            backBuffer = new Bitmap(this.Width, this.Height);

            // create the main menu
            menu = new MainMenu();
            MenuItem createItem = new MenuItem();
            MenuItem squareItem = new MenuItem();
            MenuItem triangleItem = new MenuItem();
            MenuItem circleItem = new MenuItem();
            MenuItem polygonItem = new MenuItem();

            createItem.Text = "&Create";
            squareItem.Text = "&Square";
            triangleItem.Text = "&Triangle";
            circleItem.Text = "&Circle";
            polygonItem.Text = "&Polygon";

            createItem.MenuItems.Add(squareItem);
            createItem.MenuItems.Add(triangleItem);
            createItem.MenuItems.Add(circleItem);
            createItem.MenuItems.Add(polygonItem);


            MenuItem selectItem = new MenuItem();
            selectItem.Text = "&Select";


            MenuItem transformItem = new MenuItem();
            transformItem.Text = "&Transform";


            MenuItem deleteItem = new MenuItem();
            deleteItem.Text = "&Delete";


            MenuItem exitItem = new MenuItem();
            exitItem.Text = "&Exit";

            MenuItem undoItem = new MenuItem();
            undoItem.Text = "&Undo";

            MenuItem colorsItem = new MenuItem();
            colorsItem.Text = "&Colors";

            MenuItem helpItem = new MenuItem();
            helpItem.Text = "&Help";


            menu.MenuItems.Add(createItem);
            menu.MenuItems.Add(selectItem);
            menu.MenuItems.Add(transformItem);
            menu.MenuItems.Add(deleteItem);
            menu.MenuItems.Add(undoItem);
            menu.MenuItems.Add(colorsItem);
            menu.MenuItems.Add(helpItem);
            menu.MenuItems.Add(exitItem);
            this.Menu = menu;

            // create event listeners for the menu items
            squareItem.Click += SquareItem_Click;
            triangleItem.Click += TriangleItem_Click;
            circleItem.Click += CircleItem_Click;
            polygonItem.Click += PolygonItem_Click;
            selectItem.Click += SelectItem_Click;
            transformItem.Click += TransformItem_Click;
            deleteItem.Click += DeleteItem_Click;
            undoItem.Click += UndoItem_Click;
            exitItem.Click += ExitItem_Click;
            colorsItem.Click += ColorsItem_Click;
            helpItem.Click += HelpItem_Click;

            // add mouse and keyboard input event listeners
            this.MouseClick += Canvas_MouseClick;
            this.MouseDown += Canvas_MouseDown;
            this.MouseUp += Canvas_MouseUp;
            this.MouseMove += Canvas_MouseMove;
            this.KeyDown += Canvas_KeyDown;


        }

        private void HelpItem_Click(object sender, EventArgs e)
        {
            String helpMessage = "# Create Shapes:\n\t- Square: Create two points by clicking, to create a square on \n\tgiven diagonal. \n\t- Triangle: Create three vertices, to create a triangle. \n\t- Circle: Create two points tobe used as a diameter.\n\t- Polygon: Create as many points as you need, close the \n\tpolygon by clicking the first point.\n\n# Select: Click near any shape's vertices, or use Keyboard L/R keys to rotate through them.\n\n# Transform: Use number fields to change shape rotation and position, \nwith respect to the first vertex.\nAlternatively, use mouse to drag and drop `Selected` shape.\n\n# Delete: Delete a `Selected` shape.\n\n# Undo: Undo a Create/Delete/Update action.\n\n# Colours: Change foreground and background colours.\n\n# Help: Display this Help window. ";

            MessageBox.Show(helpMessage, "Help", MessageBoxButtons.OK);

        }

        private void Canvas_KeyDown(object sender, KeyEventArgs e)
        {
            if (isSelecting)
            {
                selectShapeAction(e);
                
            }
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (isMoving)
            {
                Point currentPosition = e.Location;

                int diffX = currentPosition.X - initialMouseDownPosition.X;
                int diffY = currentPosition.Y - initialMouseDownPosition.Y;

                if (selectedShape != null)
                {
                    moveShape(selectedShape, (diffX / 10), (diffY / 10));
                }

                redrawAll();

            }
        }

        private void Canvas_MouseUp(object sender, MouseEventArgs e)
        {
            isMoving = false;
        }

        private void Canvas_MouseDown(object sender, MouseEventArgs e)
        {
            if (selectedShape != null)
            {
                initialMouseDownPosition = e.Location;
                isMoving = true;
                
            }
            
        }

        private void Canvas_MouseClick(object sender, MouseEventArgs e)
        {
            // only do if mouse is left click
            if(e.Button == MouseButtons.Right)
            {
                return;
            }

            List<Point> previewPoints = new List<Point>();
            previewPoints.Add(new Point(e.X - 3, e.Y - 3));
            previewPoints.Add(new Point(e.X + 3, e.Y + 3));

            Graphics g = this.CreateGraphics();
            Circle pointsDot = new Circle(previewPoints);
            pointsDot.isFilled = true;
            pointsDot.strokeColor = myColours.strokeColor;
            pointsDot.fillColor = myColours.fillColor;
            pointsDot.draw(g);

            // reset selction value
            isSelecting = false;

            // switch case to perform action based on selected option
            switch (selectedOption)
            {
                case MenuSelection.Square:
                    addNewShape(e,selectedOption,2);
                    break;
                case MenuSelection.Triangle:
                    addNewShape(e, selectedOption, 3);
                    break;
                case MenuSelection.Circle:
                    addNewShape(e, selectedOption, 2);
                    break;
                case MenuSelection.Polygon:
                    addPolygon(e);
                    break;
                case MenuSelection.Select:
                    isSelecting = true;
                    selectShapeAction(e.Location);
                    break;
                case MenuSelection.Transform:
                    break;
                case MenuSelection.Colours:
                    break;
                default:
                    Console.WriteLine("Mouse down switch-case error");
                    break;
            }
        }

        private void showColoursForm()
        {
            Form coloursForm = new Form();
            Button strokeLabel = new Button();
            Button fillLabel = new Button();
            Button selectLabel = new Button();
            Button bgLabel = new Button();
            Button Ok = new Button();
            Button Cancel = new Button();
            CheckBox fillOn = new CheckBox();
            Label fillOnLabel = new Label();


            fillOnLabel.Text = "&Fill";
            fillOn.Checked = toBeFilled;

            strokeLabel.Text = "&Stroke Colour";
            fillLabel.Text = "&Fill Color";
            selectLabel.Text = "&Selection Colour";
            bgLabel.Text = "&Background";

            strokeLabel.ForeColor = GetContrastingTextColor(myColours.strokeColor);
            strokeLabel.BackColor = myColours.strokeColor;
            fillLabel.ForeColor = GetContrastingTextColor(myColours.fillColor);
            fillLabel.BackColor = myColours.fillColor;
            selectLabel.ForeColor = GetContrastingTextColor(myColours.selectColor);
            selectLabel.BackColor = myColours.selectColor;
            bgLabel.ForeColor = GetContrastingTextColor(myColours.canvasColor);
            bgLabel.BackColor = myColours.canvasColor;

            Point diffMover = new Point(10, 10);
            Ok.Text = "OK";
            Cancel.Text = "Cancel";
            Ok.DialogResult = DialogResult.OK;
            Cancel.DialogResult = DialogResult.Cancel;

            strokeLabel.SetBounds(diffMover.X, diffMover.Y, 150, 15);
            diffMover.Y += 15 + 10;
            fillLabel.SetBounds(diffMover.X, diffMover.Y, 150, 15);
            diffMover.Y += 15 + 10;
            selectLabel.SetBounds(diffMover.X,diffMover.Y,150,15);
            diffMover.Y += 15 + 10;
            bgLabel.SetBounds(diffMover.X, diffMover.Y, 150, 15);
            diffMover.Y += 15 + 15;

            diffMover.X += 10;
            fillOn.SetBounds(diffMover.X, diffMover.Y, 15, 15);
            diffMover.X += 15 + 5;
            fillOnLabel.SetBounds(diffMover.X, diffMover.Y, 75, 15);
            diffMover.X -= (15 + 5 + 10);
            diffMover.Y += 15 + 15;

            Ok.SetBounds(diffMover.X, diffMover.Y, 75, 15);
            diffMover.X += 75 + 5;
            Cancel.SetBounds(diffMover.X, diffMover.Y, 75, 15);

            strokeLabel.AutoSize = true;
            fillLabel.AutoSize = true;
            selectLabel.AutoSize = true;
            bgLabel.AutoSize = true;
            Ok.AutoSize = true;
            Cancel.AutoSize = true;
            fillOnLabel.AutoSize = true;
            fillOn.AutoSize = true;

            coloursForm.ClientSize = new Size(640, 360);
            coloursForm.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            coloursForm.AutoSize = true;
            coloursForm.FormBorderStyle = FormBorderStyle.FixedDialog;
            coloursForm.StartPosition = FormStartPosition.CenterScreen;
            coloursForm.MinimizeBox = false;
            coloursForm.MaximizeBox = false;

            coloursForm.Controls.Add(strokeLabel);
            coloursForm.Controls.Add(fillLabel);
            coloursForm.Controls.Add(selectLabel);
            coloursForm.Controls.Add(bgLabel);
            coloursForm.Controls.Add(Ok);
            coloursForm.Controls.Add(Cancel);
            coloursForm.Controls.Add(fillOn);
            coloursForm.Controls.Add(fillOnLabel);

            strokeLabel.Click += StrokeLabel_Click;
            fillLabel.Click += FillLabel_Click;
            selectLabel.Click += SelectLabel_Click;
            bgLabel.Click += BgLabel_Click;
            fillOn.CheckedChanged += FillOn_CheckedChanged;

            DialogResult result = coloursForm.ShowDialog();


        }

        private void FillOn_CheckedChanged(object sender, EventArgs e)
        {
            toBeFilled = !toBeFilled;
        }

        private void BgLabel_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            colorDialog.Color = myColours.canvasColor;
            DialogResult colorResult = colorDialog.ShowDialog();
            if (colorResult == DialogResult.OK)
            {
                myColours.canvasColor = colorDialog.Color;
                
            }

            redrawAll();
            Form parentForm = (sender as Button).Parent as Form;
            parentForm.Close();
        }

        private void SelectLabel_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            colorDialog.Color = myColours.selectColor;
            DialogResult colorResult = colorDialog.ShowDialog();
            if (colorResult == DialogResult.OK)
            {
                myColours.selectColor = colorDialog.Color;
            }

            Form parentForm = (sender as Button).Parent as Form;
            parentForm.Close();
        }

        private void FillLabel_Click(object sender, EventArgs e)
        {
            ColorDialog colorDialog = new ColorDialog();
            colorDialog.Color = myColours.fillColor;
            DialogResult colorResult = colorDialog.ShowDialog();
            if (colorResult == DialogResult.OK)
            {
                myColours.fillColor = colorDialog.Color;
            }

            Form parentForm = (sender as Button).Parent as Form;
            parentForm.Close();
        }

        private void StrokeLabel_Click(object sender, EventArgs e)
        {
            
            ColorDialog colorDialog = new ColorDialog();
            colorDialog.Color = myColours.strokeColor;
            DialogResult colorResult = colorDialog.ShowDialog();
            if(colorResult == DialogResult.OK)
            {
                myColours.strokeColor = colorDialog.Color;
            }

            Form parentForm = (sender as Button).Parent as Form;
            parentForm.Close();

        }

        public static Color GetContrastingTextColor(Color bgColor)
        {
            double brightness = bgColor.R * 0.299 + bgColor.G * 0.587 + bgColor.B * 0.114;
            return brightness > 128 ? Color.Black : Color.White;
        }

        private void showTransformForm()
        {

            if (selectedShape == null)
            {
                MessageBox.Show("Please select a shape.", "Tip", MessageBoxButtons.OK);
                redrawAll();
                return;
            }

            Form transformDialog = new Form();
            Label rotationLabel = new Label();
            Label moveLabel = new Label();
            NumericUpDown rotationInput = new NumericUpDown();
            NumericUpDown moveX = new NumericUpDown();
            NumericUpDown moveY = new NumericUpDown();
            Button Ok = new Button();
            Button Cancel = new Button();

            rotationInput.Minimum = -360;
            rotationInput.Maximum = 360;

            transformDialog.Text = "Transform";
            rotationLabel.Text = "Rotation (delta - in degrees)";
            rotationInput.Value = selectedShape.rotation;

            

            moveX.Minimum = 0;
            moveY.Minimum = 0;

            moveX.Maximum = this.ClientSize.Width;
            moveY.Maximum = this.ClientSize.Height;


            moveLabel.Text = "Move";
            if (selectedShape != null)
            {
                moveX.Value = selectedShape.vertices[0].X;
                moveY.Value = selectedShape.vertices[0].Y;
            }


            Ok.Text = "OK";
            Cancel.Text = "Cancel";
            Ok.DialogResult = DialogResult.OK;
            Cancel.DialogResult = DialogResult.Cancel;

            Point diffMover = new Point(10, 10);


            rotationLabel.SetBounds(diffMover.X, diffMover.Y, 150, 15);
            diffMover.Y += 15 + 5;
            rotationInput.SetBounds(diffMover.X, diffMover.Y, 150, 15);
            diffMover.Y += 15 + 10;

            moveLabel.SetBounds(diffMover.X, diffMover.Y, 150, 15);
            diffMover.Y += 15 + 5;
            moveX.SetBounds(diffMover.X, diffMover.Y, 75, 15);
            diffMover.X += 75 + 5;
            moveY.SetBounds(diffMover.X, diffMover.Y, 75, 15);

            diffMover.X -= 75 + 5;
            diffMover.Y += 15 + 10;
            Ok.SetBounds(diffMover.X, diffMover.Y, 75, 15);
            diffMover.X += 75 + 5;
            Cancel.SetBounds(diffMover.X, diffMover.Y, 75, 15);

            rotationInput.AutoSize = true;
            rotationLabel.AutoSize = true;
            moveLabel.AutoSize = true;
            moveX.AutoSize = true;
            moveY.AutoSize = true;
            Ok.AutoSize = true;
            Cancel.AutoSize = true;
            transformDialog.ClientSize = new Size(640, 360);
            transformDialog.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            transformDialog.AutoSize = true;
            transformDialog.FormBorderStyle = FormBorderStyle.FixedDialog;
            transformDialog.StartPosition = FormStartPosition.CenterScreen;
            transformDialog.MinimizeBox = false;
            transformDialog.MaximizeBox = false;

            transformDialog.Controls.Add(rotationLabel);
            transformDialog.Controls.Add(rotationInput);
            transformDialog.Controls.Add(moveLabel);
            transformDialog.Controls.Add(moveX);
            transformDialog.Controls.Add(moveY);
            transformDialog.Controls.Add(Ok);
            transformDialog.Controls.Add(Cancel);

            DialogResult result = transformDialog.ShowDialog();

            if (result == DialogResult.OK)
            {
                Updates update = new Updates();
                
                update.action = Actions.Update;
                update.initialShape = selectedShape.Clone();

                shapeStack.RemoveAt(shapeStack.FindIndex(shape => shape.Equals(selectedShape)));

                int Theta = ((int)rotationInput.Value);
                int deltaX = 0 - selectedShape.vertices[0].X + ((int)moveX.Value);
                int deltaY = 0 - selectedShape.vertices[0].Y + ((int)moveY.Value);
                rotateShape(selectedShape, Theta);
                moveShape(selectedShape, deltaX, deltaY);

                update.newShape = selectedShape.Clone();
                
                shapeStack.Add(update.newShape);

                changes.Add(update);

                redrawAll();

                

                if (changes.Count > 5)
                {
                    changes.RemoveAt(0);
                }

                selectedShape = null;
                
            }
            else
            {
                redrawAll();
                return;
            }
        }

        private void rotateShape(Shape selectedShape, int angle)
        {
            selectedShape.rotate(angle);
        }

        private void moveShape(Shape shape, int x, int y)
        {
            for(int i = 0; i < shape.vertices.Count; i++)
            {
                shape.vertices[i] = new Point(shape.vertices[i].X + x, shape.vertices[i].Y + y);
            }

            
        }

        private void selectShapeAction(KeyEventArgs e)
        {
            if(shapeStack.Count == 0)
            {
                return;
            }

            if(selectedShape == null)
            {
                selectedShape = shapeStack.First();
            }

            int index = (shapeStack.FindIndex(shape => shape == selectedShape));

            switch (e.KeyCode)
            {
                case Keys.Left:
                    if (index == 0)
                    {
                        selectedShape = shapeStack.Last();
                    } else
                    {
                        selectedShape = shapeStack[index - 1];
                    }
                    selectShapeAction(selectedShape.vertices[0]);
                    break;
                case Keys.Right:
                    if (index == shapeStack.Count-1)
                    {
                        selectedShape = shapeStack.First();
                    }
                    else
                    {
                        selectedShape = shapeStack[index + 1];
                    }
                    selectShapeAction(selectedShape.vertices[0]);
                    break;
                default:
                    break;
            }
        }

        private void selectShapeAction(Point p1)
        {
            redrawAll();

            Point nearestVertex = new Point(0, 0);

            if (shapeStack.Count > 0)
            {
                nearestVertex = shapeStack[0].vertices[0];
                selectedShape = shapeStack[0];
            }
            else
            {
                return;
            }

            foreach (Shape shape in shapeStack)
            {
                foreach (Point point in shape.vertices)
                {
                    double pointDistance = calcDistance(p1, point);
                    double lastDistance = calcDistance(p1, nearestVertex);

                    if (lastDistance > pointDistance)
                    {
                        nearestVertex = point;
                        selectedShape = shape;

                    }

                }
            }



            Graphics g = this.CreateGraphics();
            selectedShape.strokeColor = myColours.selectColor;
            selectedShape.draw(g);
            selectedShape.strokeColor = myColours.strokeColor;
            isSelecting = true;


        }


        private void addPolygon(MouseEventArgs e)
        {
            bool nearby = false;
            if (mouseDownStack.Count != 0)
            {
                bool Xnearby = (e.Location.X <= mouseDownStack[0].X + 10) && (e.Location.X >= mouseDownStack[0].X - 10);
                bool Ynearby = (e.Location.X <= mouseDownStack[0].Y + 10) && (e.Location.X >= mouseDownStack[0].Y - 10);

                nearby = Xnearby || Ynearby;
            }

            if (clicknumber != 0 && (nearby))
            {
                clicknumber = 0;
                addNewShape(e, MenuSelection.Polygon, 0);
            }
            else
            {
                mouseDownStack.Add(e.Location);
                clicknumber++;
            }


        }

        private void addNewShape(MouseEventArgs e, MenuSelection shapeSelect, int maxPoints)
        {
            if (clicknumber < maxPoints-1) // keep adding to points stack as neede :: here we need points depending on the shape
            {
                mouseDownStack.Add(new Point(e.X, e.Y));
                clicknumber++;
                return;
            }

            mouseDownStack.Add(new Point(e.X, e.Y));
            clicknumber = 0; // reset click number

            // initialise new shapes based on the selection
            switch (shapeSelect)
            {
                case MenuSelection.Square:
                    Square newSquare = new Square(mouseDownStack);
                    shapeStack.Add(newSquare); // add it to shape stack
                    break;
                case MenuSelection.Triangle:
                    Triangle newTriangle = new Triangle(mouseDownStack);
                    shapeStack.Add(newTriangle); // add it to shape stack
                    break;
                case MenuSelection.Circle:
                    Circle newCircle = new Circle(mouseDownStack);
                    shapeStack.Add(newCircle); // add it to shape stack
                    break;
                case MenuSelection.Polygon:
                    Polygon newPolygon = new Polygon(mouseDownStack);
                    shapeStack.Add(newPolygon); // add it to shape stack
                    break;
                default:
                    Console.WriteLine("shape selection switch-case error");
                    break;
            }



            // set object variable values for the last added shape
            shapeStack.Last().isFilled = toBeFilled;
            shapeStack.Last().strokeColor = myColours.strokeColor;
            shapeStack.Last().fillColor = myColours.fillColor;

            mouseDownStack.Clear(); // clear the points stack to be re-used

            redrawAll(); // redraw all shapes to the bitmap, and then screen

            // commit to the changes list
            Updates update = new Updates();
            update.action = Actions.Create;
            update.initialShape = null;
            update.newShape = shapeStack.Last();

            changes.Add(update);

            // only keep track of last 5 changes to save memory
            if (changes.Count > 5)
            {
                changes.RemoveAt(0);
            }

        }

        private void ColorsItem_Click(object sender, EventArgs e)
        {
            selectedOption = MenuSelection.Colours;

            mouseDownStack.Clear();
            clicknumber = 0;

            // Show tool tip for first use
            if (isFirstUse[selectedOption])
            {
                MessageBox.Show("Change Stroke, Fill, Select and Background colours", "Tip");
                isFirstUse[selectedOption] = false;
            }

            showColoursForm();
        }

        private void ExitItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("All progress will be lost.", "Are you sure?", MessageBoxButtons.OKCancel);

            if (result == DialogResult.OK)
            {
                Application.Exit();
            }
        }

        private void UndoItem_Click(object sender, EventArgs e)
        {
            if(changes.Count == 0)
            {
                return;
            }

            Updates lastChange = changes.Last();

            switch (lastChange.action)
            {
                case Actions.Create:
                    selectedShape = lastChange.newShape;
                    int index = shapeStack.FindIndex(shape => shape.Equals(selectedShape));
                    if (index < 0)
                    {
                        break;
                    }
                    shapeStack.RemoveAt(index);
                    selectedShape = null;
                    break;
                case Actions.Delete:
                    selectedShape = lastChange.initialShape;
                    shapeStack.Add(selectedShape.Clone());
                    break;
                case Actions.Update:
                    selectedShape = lastChange.newShape;
                    int index2 = shapeStack.FindIndex(shape => shape.Equals(selectedShape));
                    if (index2 < 0)
                    {
                        break;
                    }
                    shapeStack.RemoveAt(index2);
                    shapeStack.Add(lastChange.initialShape);
                    break;
                default:
                    Console.WriteLine("undo switch-case error");
                    break;
            }

            

            redrawAll();
            changes.Remove(lastChange);
            selectedShape = null;


        }

        private void DeleteItem_Click(object sender, EventArgs e)
        {
            if (!isSelecting || selectedShape == null)
            {
                return;
            }

            int index = shapeStack.FindIndex(shape => shape.Equals(selectedShape));
            shapeStack.RemoveAt(index);

            redrawAll();

            Updates update = new Updates();
            update.action = Actions.Delete;
            update.initialShape = selectedShape.Clone();
            update.newShape = null;

            changes.Add(update);

            if(changes.Count > 5)
            {
                changes.RemoveAt(0);
            }

            selectedShape = null;

        }

        private void TransformItem_Click(object sender, EventArgs e)
        {
            selectedOption = MenuSelection.Transform;

            mouseDownStack.Clear();
            clicknumber = 0;

            // Show tool tip for first use
            if (isFirstUse[selectedOption])
            {
                MessageBox.Show("Change Co-ordinates and Rotation of Selected Shape", "Tip");
                isFirstUse[selectedOption] = false;
            }

            showTransformForm();
        }

        private void SelectItem_Click(object sender, EventArgs e)
        {
            selectedOption = MenuSelection.Select;

            mouseDownStack.Clear();
            clicknumber = 0;

            // Show tool tip for first use
            if (isFirstUse[selectedOption])
            {
                MessageBox.Show("Click on shape vertices/edges, or use Keyboard Arrow Keys (< or >), to select shapes.", "Tip");
                isFirstUse[selectedOption] = false;
            }
        }

        private void PolygonItem_Click(object sender, EventArgs e)
        {
            selectedOption = MenuSelection.Polygon;

            mouseDownStack.Clear();
            clicknumber = 0;

            // Show tool tip for first use
            if (isFirstUse[selectedOption])
            {
                MessageBox.Show("Click at locations to create new vertices, click on initial point to close the polygon.", "Tip");
                isFirstUse[selectedOption] = false;
            }
        }

        private void CircleItem_Click(object sender, EventArgs e)
        {
            selectedOption = MenuSelection.Circle;

            mouseDownStack.Clear();
            clicknumber = 0;

            // Show tool tip for first use
            if (isFirstUse[selectedOption])
            {
                MessageBox.Show("Click at two locations (diameter) to create a circle.", "Tip");
                isFirstUse[selectedOption] = false;
            }
        }

        private void TriangleItem_Click(object sender, EventArgs e)
        {
            selectedOption = MenuSelection.Triangle;

            mouseDownStack.Clear();
            clicknumber = 0;

            // Show tool tip for first use
            if (isFirstUse[selectedOption])
            {
                MessageBox.Show("Click at three locations to create a triangle", "Tip");
                isFirstUse[selectedOption] = false;
            }
        }

        private void SquareItem_Click(object sender, EventArgs e)
        {
            selectedOption = MenuSelection.Square;

            mouseDownStack.Clear();
            clicknumber = 0;

            // Show tool tip for first use
            if (isFirstUse[selectedOption])
            {
                MessageBox.Show("Click at two locations (diagonal) to create a square", "Tip");
                isFirstUse[selectedOption] = false;
            }

            
        }

        private void redrawAll()
        {
            if (shapeStack == null && shapeStack.Count == 0)
            {
                return;
            }

            Graphics g = Graphics.FromImage(backBuffer);

            g.Clear(myColours.canvasColor);

            foreach (Shape shape in shapeStack)
            {
                shape.draw(g);
            }

            g = this.CreateGraphics();

            g.DrawImage(backBuffer, 0, 0);

            g.Dispose();


        }

        private double calcDistance(Point p1, Point p2)
        {
            double distance = Math.Sqrt(Math.Pow((p2.X - p1.X), 2) + Math.Pow((p2.Y - p1.Y), 2));
            return distance;
        }
    }
}
