using System.Drawing;

//Disk activity icon object
public class DiskLed
{
    //Default colors and drawing position
    public static readonly Color Background = Color.FromArgb(0, 0, 0, 0); //clear background
    public Color ColorOff = Color.Black;
    public Color ColorOn { get; set; }
    public Shapes Shape { get; set; }
    public Blinker Blink {get; set;}
    public BlinkerType BlinkType { get; set; }

    //Drawing bounds for different shapes
    public struct Bounds
    {
        public static Rectangle BoundsCircle;
        public static Rectangle BoundsRectangle;
        public static Rectangle BoundsBarVertical;
        public static Rectangle BoundsBarHorizontal;
        public static Point[] BoundsTriangle;
    }

    //Shapes
    public enum Shapes
    {
        Circle,
        Rectangle,
        BarVertical,
        BarHorizontal,
        Triangle
    }

    //Blink state
    public enum Blinker
    {
        On,
        Off
    }

    //Blink type
    public enum BlinkerType
    {
        Value,
        OnOff
    }

    public DiskLed(Graphics gfx)
    {
        Bounds.BoundsCircle = new Rectangle((int)(gfx.VisibleClipBounds.Width * 0.2 / 2),
            (int)(gfx.VisibleClipBounds.Height * 0.2 / 2),
            (int)(gfx.VisibleClipBounds.Width * 0.8),
            (int)(gfx.VisibleClipBounds.Height * 0.8));
        Bounds.BoundsRectangle = Bounds.BoundsCircle;
        Bounds.BoundsBarVertical = new Rectangle((int)(gfx.VisibleClipBounds.Width * 0.5 / 2),
            (int)(gfx.VisibleClipBounds.Height * 0.1 / 2),
            (int)(gfx.VisibleClipBounds.Width * 0.5),
            (int)(gfx.VisibleClipBounds.Height * 0.9));
        Bounds.BoundsBarHorizontal = new Rectangle((int)(gfx.VisibleClipBounds.Width * 0.1 / 2),
            (int)(gfx.VisibleClipBounds.Height * 0.5 / 2),
            (int)(gfx.VisibleClipBounds.Width * 0.9),
            (int)(gfx.VisibleClipBounds.Height * 0.5));
        Bounds.BoundsTriangle = new Point[]
        {
            new Point((int)(gfx.VisibleClipBounds.Width - 1)/ 2, 0),
            new Point(0 , (int)gfx.VisibleClipBounds.Height - 1),
            new Point((int)gfx.VisibleClipBounds.Width - 1, (int)gfx.VisibleClipBounds.Height - 1)
        };
    }
}