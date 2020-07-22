using System.Drawing;
//Disk activity icon object
public class DiskLed
{
    //Default colors and drawing position
    public static readonly Color background = Color.FromArgb(0, 0, 0, 0); //clear background
    public Color ledOFF = Color.Black;
    public Color ledON;
    public shapes shape;
    public static Rectangle boundsCircle;
    public static Rectangle boundsRectangle;
    public static Rectangle boundsBarVertical;
    public static Rectangle boundsBarHorizontal;
    public static Point[] boundsTriangle;

    //Shapes
    public enum shapes
    {
        Circle,
        Rectangle,
        BarVertical,
        BarHorizontal,
        Triangle
    }

    public DiskLed(Graphics gfx)
    {
        boundsCircle = new Rectangle((int)(gfx.VisibleClipBounds.Width * 0.2 / 2),
            (int)(gfx.VisibleClipBounds.Height * 0.2 / 2),
            (int)(gfx.VisibleClipBounds.Width * 0.8),
            (int)(gfx.VisibleClipBounds.Height * 0.8));
        boundsRectangle = boundsCircle;
        boundsBarVertical = new Rectangle((int)(gfx.VisibleClipBounds.Width * 0.5 / 2),
            (int)(gfx.VisibleClipBounds.Height * 0.1 / 2),
            (int)(gfx.VisibleClipBounds.Width * 0.5),
            (int)(gfx.VisibleClipBounds.Height * 0.9));
        boundsBarHorizontal = new Rectangle((int)(gfx.VisibleClipBounds.Width * 0.1 / 2),
            (int)(gfx.VisibleClipBounds.Height * 0.5 / 2),
            (int)(gfx.VisibleClipBounds.Width * 0.9),
            (int)(gfx.VisibleClipBounds.Height * 0.5));
        boundsTriangle = new Point[]
        {
            new Point((int)(gfx.VisibleClipBounds.Width - 1)/ 2, 0),
            new Point(0 , (int)gfx.VisibleClipBounds.Height - 1),
            new Point((int)gfx.VisibleClipBounds.Width - 1, (int)gfx.VisibleClipBounds.Height - 1)
        };
    }

    public void SetLedColor(Color c)
    {
        ledON = c;
    }
}