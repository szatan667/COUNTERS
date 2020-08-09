using System.Drawing;

//Drawing bounds for different shapes
public struct LedBounds
{
    public static Rectangle BoundsCircle;
    public static Rectangle BoundsRectangle;
    public static Rectangle BoundsBarVertical;
    public static Rectangle BoundsBarHorizontal;
    public static Point[] BoundsTriangle;
}

//Shapes
public enum LedShape
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

//Disk activity icon object
public class DiskLed
{
    //Default colors and drawing position
    public static readonly Color Background = Color.FromArgb(0, 0, 0, 0); //clear background
    public Color ColorOff = Color.Black;
    public Color ColorOn;
    public LedShape Shape;
    public Blinker Blink;
    public BlinkerType BlinkType;

    public DiskLed(Graphics gfx)
    {
        LedBounds.BoundsCircle = new Rectangle((int)(gfx.VisibleClipBounds.Width * 0.2 / 2),
            (int)(gfx.VisibleClipBounds.Height * 0.2 / 2),
            (int)(gfx.VisibleClipBounds.Width * 0.8),
            (int)(gfx.VisibleClipBounds.Height * 0.8));
        LedBounds.BoundsRectangle = LedBounds.BoundsCircle;
        LedBounds.BoundsBarVertical = new Rectangle((int)(gfx.VisibleClipBounds.Width * 0.5 / 2),
            (int)(gfx.VisibleClipBounds.Height * 0.1 / 2),
            (int)(gfx.VisibleClipBounds.Width * 0.5),
            (int)(gfx.VisibleClipBounds.Height * 0.9));
        LedBounds.BoundsBarHorizontal = new Rectangle((int)(gfx.VisibleClipBounds.Width * 0.1 / 2),
            (int)(gfx.VisibleClipBounds.Height * 0.5 / 2),
            (int)(gfx.VisibleClipBounds.Width * 0.9),
            (int)(gfx.VisibleClipBounds.Height * 0.5));
        LedBounds.BoundsTriangle = new Point[]
        {
            new Point((int)(gfx.VisibleClipBounds.Width - 1)/ 2, 0),
            new Point(0 , (int)gfx.VisibleClipBounds.Height - 1),
            new Point((int)gfx.VisibleClipBounds.Width - 1, (int)gfx.VisibleClipBounds.Height - 1)
        };
    }
}