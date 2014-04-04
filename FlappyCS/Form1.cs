using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.Threading;
namespace FlappyCS
{
    public partial class Form1 : Form
    {
        private delegate void UpdateFormCallback();
        //Game Object Declarations.
        private Player p1;

        private Thread updateThread;
        private Thread FPSThread;
        private Thread CountDownThread;

        private bool paused = false;
        private bool once = false;
        private bool gameover = false;
        private int countdown = 4;

        //fonts
        private Font fnt = new Font("Arial", (float)23, FontStyle.Italic | FontStyle.Bold);
        private Font smfnt = new Font("Arial", (float)9, FontStyle.Italic | FontStyle.Bold);

        //fps
        private int FPSt = 0;
        private int FPSCount = 0;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            p1 = new Player(new Point(this.Size.Width, this.Size.Height));
            p1.Active = true;
            p1.Instance = new Rectangle(4, 4, 25, 25);
            p1.Location = new Point(125, 100);
            p1.OldLocation = p1.Location;
            updateThread = new Thread(new ThreadStart(UpdateThread)) { IsBackground = true };
            updateThread.Start();
            CountDownThread = new Thread(new ThreadStart(CountDown)) { IsBackground = true };
            CountDownThread.Start();
            FPSThread = new Thread(new ThreadStart(FPS)) { IsBackground = true };
            FPSThread.Start();
        }

        private void FPS()
        {
            while (!gameover)
            {
                FPSCount = FPSt;
                FPSt = 0;
                Thread.Sleep(1000);
            }
        }

        private void UpdateThread()
        {
            while (!gameover)
            {
                UpdateForm();
                Thread.Sleep(10);
            }
        }

        private void CountDown()
        {
            while (countdown > 0)
            {
                p1.Active = false;
                countdown--;
                Thread.Sleep(1000);
            }
        }

        private void UpdateForm()
        {
            if (this.InvokeRequired)
            {
                UpdateFormCallback d = new UpdateFormCallback(UpdateForm);
                try
                {
                    this.Invoke(d);
                }
                catch { }
            }
            else
            {
                this.Refresh();
            }
        }
        
        private void Form1_Paint(object sender, PaintEventArgs e)
        {
            FPSt++;
            if (p1.Instance != null)
            {
                p1.PaintPlayer(e.Graphics);
                p1.PaintObstacles(e.Graphics);
                e.Graphics.FillRectangle(new SolidBrush(Color.DimGray), new Rectangle(-1, -1, 75, 25));
                e.Graphics.DrawRectangle(Pens.Black, new Rectangle(-1, -1, 75, 25));
                e.Graphics.DrawString(string.Format("Score: {0}", p1.Score.ToString()), smfnt, new SolidBrush(Color.White), new PointF(8, 5));
                e.Graphics.DrawString(string.Format("FPS: {0}", FPSCount), smfnt, new SolidBrush(Color.Black), new PointF(this.Width - 70, 0));
                //fixed collision check
                if (p1.CheckCollision())
                {
                    //game over.
                    EndGame();
                }

                p1.OldLocation = p1.Location;

                if (countdown > 0)
                {
                    //draw new countdown.
                    e.Graphics.DrawString(countdown.ToString(), fnt, new SolidBrush(Color.Black), new PointF((this.Width / 2) - 30, this.Height / 2 - 50));
                }
                else if(!once)
                {
                    //active. //never activate again.
                    p1.Active = true;
                    once = true;
                }
            }
            if (paused)
            {
                //draw paused on the screen.
                e.Graphics.DrawString("Paused.", fnt, new SolidBrush(Color.Black), new PointF((this.Width / 2) - 80, this.Height / 2 - 30));
            }
            if (gameover)
            {
                e.Graphics.DrawString(string.Format("Final Score: {0}", p1.Score), fnt, new SolidBrush(Color.Black), new PointF(this.Width / 2 - 120, this.Height / 2 - 60));
                e.Graphics.DrawString("Game Over. Press Enter.", fnt, new SolidBrush(Color.Black), new PointF(50, this.Height / 2 - 30));
            }
        }

        private void EndGame()
        {
            gameover = true;
            FPSThread.Abort();
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            //update player location
            if (gameover && e.KeyCode == Keys.Enter)
            {
                gameover = false;
                countdown = 4;
                once = false;
                Form1_Load(this, EventArgs.Empty);
            }
            if (e.KeyCode == Keys.Space && p1.Instance != null && p1.Active)
            {
                p1.heightPoller += 80; //switched to smooth jumping. uncomment below line and comment out this one to go back to old jumping
                //p1.Location.Y -= 75;
                //p1.Rotation = 35;
                //this.Refresh();
            }
            else if (e.KeyCode == Keys.Q)
            {
                p1.Active = false;
                paused = true;
                if (MessageBox.Show("Are you sure you want to quit?", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    Environment.Exit(0);
                }
                p1.Active = true;
                paused = false;
            }
            else if (e.KeyCode == Keys.P && (p1.Active || paused))
            {
                //pause game.
                p1.Active = !p1.Active;
                paused = !p1.Active;
            }
        }

        private void Form1_MouseDown(object sender, MouseEventArgs e)
        {
            if (p1.Instance != null && p1.Active)
            {
                //instead store height poller
                p1.heightPoller += 80;
                //p1.Location.Y -= 75;
                
            }
        }

    }
    class Player
    {
        public Rectangle Instance;
        public Point Location;
        public Point OldLocation;
        public float Rotation = 0;
        public bool Active = false;
        public int Score = 0;
        public int heightPoller = 0;

        private float fallingRotation = 0;
        private float fallingSpeed = 0;

        private Matrix matrix = new Matrix();
        private Point maxSize;
        private Rectangle topBox;
        private Rectangle lowerBox;
        private Rectangle topBox2;
        private Rectangle lowerBox2;
        private Graphics p;
        private Bitmap graphics;

        private static List<Color> colors = new List<Color>()
        {
            Color.Purple,
            Color.Salmon,
            Color.RosyBrown,
            Color.PowderBlue,
            Color.Orange,
            Color.DarkOrange,
            Color.DarkGreen,
            Color.Magenta,
            Color.Lime,
            Color.LightSlateGray,
            Color.OrangeRed
        };

        public Player(Point MaxSize)
        {
            maxSize = new Point(MaxSize.Y - 38, MaxSize.X - 5);
            Active = true;
            graphics = new Bitmap(34, 34);
            p = Graphics.FromImage(graphics);
            topBox = new Rectangle(new Point(maxSize.X - 64, -1), new Size(64, r.Next(128, 320)));
            lowerBox = new Rectangle(new Point(maxSize.X - 64, topBox.Height + 128), new Size(64, maxSize.Y - topBox.Height + 300));
            topBox2 = new Rectangle(new Point(maxSize.X + (maxSize.X / 2) - 64, -1), new Size(64, r.Next(128, 320)));
            lowerBox2 = new Rectangle(new Point(maxSize.X + (maxSize.X / 2) - 64, topBox2.Height + 128), new Size(64, maxSize.Y - topBox2.Height + 300));
        }

        
        public void PaintPlayer(Graphics e)
        {
            p.Clear(Form.DefaultBackColor);
            if (Active)
            {
                fallingSpeed += (float)0.08;
                Location.Y += (int)(2 * fallingSpeed);
                //ADD MOMENTUM
            }
            Size rectangleSize = new Size(this.Instance.Width / 2, this.Instance.Height / 2);
            Size margin = new Size((this.Instance.Width - rectangleSize.Width) / 2, (this.Instance.Height - rectangleSize.Height) / 2);
            
            matrix = new Matrix();      
            if (heightPoller > 0 && Active)
            {
                //smooth jumping
                Location.Y -= 8;
                heightPoller -= 8;
                fallingSpeed = 0;
                if(Rotation <= 0)
                    Rotation = 1;
            }
            if (Rotation > 0)
            {
                fallingRotation = 0;
                matrix.RotateAt(Rotation - (Rotation * 2), new PointF(Instance.Left + ((float)Instance.Width / 2), Instance.Top + ((float)Instance.Height / 2)), MatrixOrder.Append);
                if (Active && heightPoller <= 0)
                    Rotation--;
                else if(Rotation < 30)
                    Rotation+= 2;
            }
            else
            {
                if (fallingRotation == 0) fallingRotation += (float)0.5;
                matrix.RotateAt(fallingRotation, new PointF(Instance.Left + ((float)Instance.Width / 2), Instance.Top + ((float)Instance.Height / 2)), MatrixOrder.Append);
                if(fallingRotation < 35 && fallingRotation != 0 && Active)
                    fallingRotation += (float)0.7;
            }
            p.Transform = matrix;
            p.FillRectangle(new SolidBrush(Color.DarkCyan), Instance);
            p.DrawRectangle(Pens.Black, Instance);
            e.DrawImage(graphics, Location);
        }
        private static Random r = new Random(Environment.TickCount);

        private bool scoreGiven = false;

        private Color currentColor = colors[r.Next(0, colors.Count - 1)];
        private Color currentColor2 = colors[r.Next(0, colors.Count - 1)];

        public void PaintObstacles(Graphics e)
        {
            //move Rectangles.
            if (Active)
            {
                topBox.X -= 2;
                lowerBox.X -= 2;
                topBox2.X -= 2;
                lowerBox2.X -= 2;
            }
            e.FillRectangle(new SolidBrush(currentColor), topBox);
            e.FillRectangle(new SolidBrush(currentColor), lowerBox);
            e.DrawRectangle(Pens.Black, topBox);
            e.DrawRectangle(Pens.Black, lowerBox);

            e.FillRectangle(new SolidBrush(currentColor2), topBox2);
            e.FillRectangle(new SolidBrush(currentColor2), lowerBox2);
            e.DrawRectangle(Pens.Black, topBox2);
            e.DrawRectangle(Pens.Black, lowerBox2);
            //calculate where they should be at, start at the max length of the form.
            if (topBox.X <= -64)
            {
                //update.
                //+1 point.
                scoreGiven = false;
                topBox.X = maxSize.X;
                topBox.Height = r.Next(90, 350);
                currentColor = colors[r.Next(0, colors.Count - 1)];
                //new Rectangle(new Point(maxSize.X, -1), new Size(64, r.Next(90, 350)));
            }

            if (lowerBox.X <= -64)
            {
                lowerBox.X = maxSize.X;
                lowerBox.Height = maxSize.Y - topBox.Height + 300;
                lowerBox.Y = topBox.Height + 128;
                //new Rectangle(new Point(maxSize.X, topBox.Height + 128), new Size(64, maxSize.Y - topBox.Height + 300));
            }

            if (topBox2.X <= -64)
            {
                //update.
                //+1 point.
                scoreGiven = false;
                topBox2.X = maxSize.X;
                topBox2.Height = r.Next(90, 350);
                //new Rectangle(new Point(maxSize.X, -1), new Size(64, r.Next(90, 350)));
                currentColor2 = colors[r.Next(0, colors.Count - 1)];
            }

            if (lowerBox2.X <= -64)
            {
                lowerBox2.X = maxSize.X;
                lowerBox2.Height = maxSize.Y - topBox2.Height + 300;
                lowerBox2.Y = topBox2.Height + 128;
                //new Rectangle(new Point(maxSize.X, topBox.Height + 128), new Size(64, maxSize.Y - topBox.Height + 300));
            }
            
            //check score.
            if (Location.X > topBox.X + topBox.Width && !scoreGiven)
            {
                Score++;
                scoreGiven = true;
            }
            if (Location.X > topBox2.X + topBox2.Width && !scoreGiven)
            {
                Score++;
                scoreGiven = true;
            }
        }

        public bool CheckCollision()
        {
            if ((OldLocation.Y + Instance.Height) >= maxSize.Y || OldLocation.Y <= 0)
            {
                //ded.
                Active = false;
                fallingRotation = 0;
                //activate main form's shit and show score.
                return true;
            }
            //check top box collision.
            Rectangle collide = new Rectangle(OldLocation.X, OldLocation.Y, Instance.Width, Instance.Height);
            if (topBox.IntersectsWith(collide))
            {
                Active = false;
                fallingRotation = 0;
                return true;
            }
            if (lowerBox.IntersectsWith(collide))
            {
                Active = false;
                fallingRotation = 0;
                return true;
            }
            if (topBox2.IntersectsWith(collide))
            {
                Active = false;
                fallingRotation = 0;
                return true;
            }
            if (lowerBox2.IntersectsWith(collide))
            {
                Active = false;
                fallingRotation = 0;
                return true;
            }
            return false;
        }
    }
    class Wall
    {

    }
}
