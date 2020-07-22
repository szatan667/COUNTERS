using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

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

    //Shapes
    public enum shapes
    {
        Circle,
        Rectangle,
        BarVertical,
        BarHorizontal
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
    }

    public void SetLedColor(Color c)
    {
        ledON = c;
    }
}

//Counter object
public class Counter
{
    //Each counter has tray icon, logical icon, actual system counter and set of timers
    //TRAY ICON
    private readonly NotifyIcon TrayIcon;
    private readonly Bitmap bmp;
    private IntPtr h;
    private readonly Graphics gfx;
    private readonly DiskLed led;
    private SolidBrush brush;

    //SYSTEM COUNTER
    private PerformanceCounter PC;
    private readonly int[] val = new int[5];
    private int avg;

    //TIMERS
    private readonly Timer TimerCnt;
    private readonly Timer TimerIcon;

    //Create counter object with default constructor
    public Counter()
    {

    }
}

//Main couter class
public class COUNTERS : ApplicationContext
{
    //Program entry point
    static void Main()
    {
        Application.Run(new COUNTERS());
    }

    //To allow icon destroyal
    [DllImport("user32.dll")]
    private extern static bool DestroyIcon(IntPtr handle);

    //Performance counter object to read disk activity
    private PerformanceCounter PC;
    private readonly int[] val = new int[5];
    private int avg;

    //Tray icon drawing variables
    private readonly NotifyIcon TrayIcon;
    private readonly Bitmap bmp;
    private IntPtr h;
    private readonly Graphics gfx;
    private readonly DiskLed led;
    private SolidBrush brush;

    //Timers - counter readout interval and icon blinking interval
    private readonly Timer TimerCnt;
    private readonly Timer TimerIcon;

    //Settings in ini file
    private readonly IniFile ini = new IniFile();

    //Main object initialize
    public COUNTERS()
    {
        //Create tray icon with context menu consisting of counter categories, types, etc.
        TrayIcon = new NotifyIcon()
        {
            Text = "Blink",
            Visible = true,
            Icon = null,
            ContextMenu = new ContextMenu(new MenuItem[]
            {
                //Counter definition related
                new MenuItem {Text = "CATEGORY", Name = "MenuCategory"},
                new MenuItem {Text = "INSTANCE", Name = "MenuInstance"},
                new MenuItem {Text = "COUNTER", Name = "MenuCounter"},

                //Settings
                new MenuItem("-") {Name = "Separator"},
                new MenuItem("Color...", MenuCheckMark) {Tag = new ColorDialog()},
                new MenuItem("Shape", new MenuItem[]
                {
                    new MenuItem("Circle", MenuCheckMark) {Name = ((int)DiskLed.shapes.Circle).ToString(), Tag = DiskLed.shapes.Circle},
                    new MenuItem("Rectangle", MenuCheckMark) {Name = ((int)DiskLed.shapes.Rectangle).ToString(), Tag = DiskLed.shapes.Rectangle},
                    new MenuItem("Vertical bar", MenuCheckMark) {Name = ((int)DiskLed.shapes.BarVertical).ToString(), Tag = DiskLed.shapes.BarVertical},
                    new MenuItem("Horizontal bar", MenuCheckMark) {Name = ((int)DiskLed.shapes.BarHorizontal).ToString(), Tag = DiskLed.shapes.BarHorizontal}
                }
                ) {Name = "MenuShape" },

                //Exit app
                new MenuItem("-") {Name = "Separator"},
                new MenuItem("Exit", MenuExit)
                {
                    DefaultItem = true,
                    Name = "MenuExit",
                    Tag = new Font("Anonymous Pro", 16, FontStyle.Bold)
                }
            })
        };

        //Counter polling timer
        TimerCnt = new Timer
        {
            Interval = 50,
            Enabled = false,
        };
        TimerCnt.Tick += TimerCnt_Tick;

        //Icon blink timer
        TimerIcon = new Timer
        {
            Interval = TimerCnt.Interval / 2, //blink twice as fast as readout goes
            Enabled = false
        };
        TimerIcon.Tick += TimerIcon_Tick;

        //Fill in list of performance objects available (eg. processor, disk, network, etc.)
        FillMenu(TrayIcon.ContextMenu.MenuItems["MenuCategory"], PerformanceCounterCategory.GetCategories());

        //Now try to get counter from ini file
        //If config not present, don't create any counter yet, let user pick it
        string _catname = ini.Read("categoryName");
        string _instname = ini.Read("instanceName");
        string _cntname = ini.Read("counterName");

        //CATEGORIES
        if (_catname != string.Empty)
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuCategory"].MenuItems[_catname], null);
        //INSTANCES
        if (_instname != string.Empty)
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuInstance"].MenuItems[_instname], null);
        //COUNTERS
        if (_cntname != string.Empty)
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuCounter"].MenuItems[_cntname], null);

        //Create graphics context
        bmp = new Bitmap(32, 32);
        gfx = Graphics.FromImage(bmp);
        gfx.SmoothingMode = SmoothingMode.HighQuality;

        //Create LED object and get the color from ini file
        led = new DiskLed(gfx);

        //Get led color from ini file or set default one
        string _r = ini.Read("ledColorR");
        string _g = ini.Read("ledColorG");
        string _b = ini.Read("ledColorB");
        if (_r != string.Empty && _g != string.Empty && _b != string.Empty)
        {
            //int r, g, b;
            int.TryParse(_r, out int r);
            int.TryParse(_g, out int g);
            int.TryParse(_b, out int b);
            led.SetLedColor(Color.FromArgb(r, g, b));
        }
        else
            led.SetLedColor(Color.Lime);

        //Now for led shape
        string _shape = ini.Read("ledShape");
        if (_shape != string.Empty)
        {
            int.TryParse(_shape, out int s);
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuShape"].MenuItems[s], null);
        }
        else
            led.shape = DiskLed.shapes.Circle;

        //Start with led off
        DrawTrayIcon(led.ledOFF, false);
    } //MAIN CONSTRUCTOR END

    //Fill counter menu list with desired object list
    private void FillMenu(MenuItem mi, object[] filler)
    {
        mi.MenuItems.Clear();

        switch (filler)
        {
            case PerformanceCounterCategory[] pcc:
                foreach (PerformanceCounterCategory cat in pcc)
                    mi.MenuItems.Add(new MenuItem(cat.CategoryName, MenuCheckMark)
                    {
                        Name = cat.CategoryName,
                        RadioCheck = true,
                        Checked = false,
                        Tag = cat
                    });
                break;

            case string[] s:
                foreach (string inst in s)
                    mi.MenuItems.Add(new MenuItem(inst, MenuCheckMark)
                    {
                        Name = inst,
                        RadioCheck = true,
                        Checked = false,
                        Tag = inst
                    });
                break;

            case PerformanceCounter[] pc:
                foreach (PerformanceCounter cnt in pc)
                    mi.MenuItems.Add(new MenuItem(cnt.CounterName, MenuCheckMark)
                    {
                        Name = cnt.CounterName,
                        RadioCheck = true,
                        Checked = false,
                        Tag = cnt
                    });
                break;

            default:
                throw new Exception("Counter menu build failed :(");
        }
    }

    //Sets menu item check mark and executes action according to item TAG type
    private void MenuCheckMark(object s, EventArgs e)
    {
        //Place checkmark as default
        bool _makecheck = true;

        //Execute click action according to sender 
        switch ((s as MenuItem).Tag)
        {
            //Custom color click - show color dialog, change the olor only if OK pressed inside the dialog
            case ColorDialog cd:
                _makecheck = false;
                cd.SolidColorOnly = true;
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    led.SetLedColor(cd.Color);
                    ini.Write("ledColorR", led.ledON.R.ToString());
                    ini.Write("ledColorG", led.ledON.G.ToString());
                    ini.Write("ledColorB", led.ledON.B.ToString());
                }
                break;

            //Shape click
            case DiskLed.shapes sh:
                led.shape = sh;
                ini.Write("ledShape", ((int)led.shape).ToString());
                break;

            //Category click - clean instances&counters submenus and get list of instances
            case PerformanceCounterCategory pcc:
                TimerCnt.Enabled = false;
                if (PC != null) PC.Dispose();
                TrayIcon.ContextMenu.MenuItems["MenuInstance"].MenuItems.Clear();
                TrayIcon.ContextMenu.MenuItems["MenuCounter"].MenuItems.Clear();
                FillMenu(TrayIcon.ContextMenu.MenuItems["MenuInstance"], pcc.GetInstanceNames());
                break;

            //Instance click - clean counters submenu and fill it with fresh list
            case string inst:
                TimerCnt.Enabled = false;
                if (PC != null) PC.Dispose();
                TrayIcon.ContextMenu.MenuItems["MenuCounter"].MenuItems.Clear();
                FillMenu(TrayIcon.ContextMenu.MenuItems["MenuCounter"],
                         PerformanceCounterCategory.GetCategories()[MenuItemIndex(
                             MenuItemName(TrayIcon.ContextMenu.MenuItems["MenuCategory"]),
                             TrayIcon.ContextMenu.MenuItems["MenuCategory"])].GetCounters(inst));
                break;

            //Counter click - take this counter as valid
            case PerformanceCounter pc:
                TimerCnt.Enabled = true;
                try
                {
                    PC = pc;
                }
                catch (Exception ex)
                {
                    TimerCnt.Enabled = false;
                    MessageBox.Show("Well, that's embarassing but something went wrong with counter creation" +
                        Environment.NewLine + ex.Message +
                        Environment.NewLine + s,
                        "Bye bye...");
                    Application.Exit();
                }
                break;

            default:
                throw new Exception("Counter menu click event failed :(" + Environment.NewLine + s);
        }

        //Place checkmark if desired
        if (_makecheck)
        {
            //Go through menu items at the same level (all from sender's parent)
            //Don't look for currently checked item - just clear them all first...
            foreach (MenuItem mi in (s as MenuItem).Parent.MenuItems)
                mi.Checked = false;

            //...and then mark desired as checked
            (s as MenuItem).Checked = true;
            ini.Write("categoryName", MenuItemName(TrayIcon.ContextMenu.MenuItems["MenuCategory"]));
            ini.Write("instanceName", MenuItemName(TrayIcon.ContextMenu.MenuItems["MenuInstance"]));
            ini.Write("counterName", MenuItemName(TrayIcon.ContextMenu.MenuItems["MenuCounter"]));
        }
    }

    //Get selected item name
    string MenuItemName(MenuItem MenuItem)
    {
        foreach (MenuItem mi in MenuItem.MenuItems)
            if (mi.Checked)
                return mi.Name;
        return null;
    }

    //Get menu item index by name
    int MenuItemIndex(string Name, MenuItem MenuItem)
    {
        for (int i = 0; i < MenuItem.MenuItems.Count; i++)
            if (MenuItem.MenuItems[i].Name == Name)
                return i;
        return -1;
    }

    //Handle menu exit
    private void MenuExit(object s, EventArgs e)
    {
        TrayIcon.Dispose();
        Application.Exit();
    }

    //Process timer readout - tested with precentage values only
    private void TimerCnt_Tick(object s, EventArgs e)
    {
        //Read latest value and get the average reading
        try
        {
            //Shift values left, make room for new readout
            for (int i = 0; i < val.Length - 1; i++)
                val[i] = val[i + 1];
            val[val.Length - 1] = (int)PC.NextValue();

            int sum = 0;
            for (int i = 0; i < val.Length; i++)
                sum += val[i];

            avg = sum / val.Length;

            //Now do some scaling - disk load is usually below 50% so pump it up a bit
            if (avg <= 2) avg *= 10;
            else if (avg <= 5) avg *= 7;
            else if (avg <= 10) avg *= 4;
            else if (avg <= 25) avg *= 2;
            else if (avg <= 50) avg = (int)(avg * 1.5);
            else if (avg <= 75) avg = (int)(avg * 1.25);
            else if (avg <= 100) avg *= 1;
            else if (avg > 100) avg = 100; //just in case if for some reason calc goes out of bounds (eg. dummy readout out of scale?)

            DrawTrayIcon(Color.FromArgb(
                led.ledON.R * avg / 100,
                led.ledON.G * avg / 100,
                led.ledON.B * avg / 100
                ), true);
        }
        catch (Exception ex)
        {
            TimerCnt.Enabled = false;
            MessageBox.Show("Well, that's embarassing but something went wrong with counter readout" +
                Environment.NewLine + ex.Message +
                Environment.NewLine + avg +
                Environment.NewLine + val[0] +
                Environment.NewLine + val[1] +
                Environment.NewLine + val[2] +
                Environment.NewLine + val[3] +
                Environment.NewLine + val[4],
                "Bye bye...");
            Application.Exit();
        }
    }

    //Draw tray icon like LED light
    private void DrawTrayIcon(Color c, bool t)
    {
        brush = new SolidBrush(c);
        DestroyIcon(h); //destroy current icon to avoid handle leaking

        //Draw desired led shape
        gfx.Clear(DiskLed.background);
        switch (led.shape)
        {
            case DiskLed.shapes.Circle:
                gfx.FillEllipse(brush, DiskLed.boundsCircle);
                break;
            case DiskLed.shapes.Rectangle:
                gfx.FillRectangle(brush, DiskLed.boundsRectangle);
                break;
            case DiskLed.shapes.BarVertical:
                gfx.FillRectangle(brush, DiskLed.boundsBarVertical);
                break;
            case DiskLed.shapes.BarHorizontal:
                gfx.FillRectangle(brush, DiskLed.boundsBarHorizontal);
                break;
            default:
                break;
        }

        //Send drawn image to tray icon
        h = bmp.GetHicon();
        TrayIcon.Icon = Icon.FromHandle(h);

        //Enable or disable tray icon timer
        TimerIcon.Enabled = t;
    }

    //Disable icon perodically to make it blink
    private void TimerIcon_Tick(object s, EventArgs e)
    {
        DrawTrayIcon(led.ledOFF, false);
    }
}

//INI file handler
public class IniFile
{
    readonly string Path;
    readonly string EXE = Assembly.GetExecutingAssembly().GetName().Name;

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    static extern long WritePrivateProfileString(string Section, string Key, string Value, string FilePath);

    [DllImport("kernel32", CharSet = CharSet.Unicode)]
    static extern int GetPrivateProfileString(string Section, string Key, string Default, StringBuilder RetVal, int Size, string FilePath);

    public IniFile(string IniPath = null)
    {
        Path = new FileInfo(IniPath ?? EXE + ".ini").FullName.ToString();
    }

    public string Read(string Key, string Section = null)
    {
        var RetVal = new StringBuilder(255);
        GetPrivateProfileString(Section ?? EXE, Key, "", RetVal, 255, Path);
        return RetVal.ToString();
    }

    public void Write(string Key, string Value, string Section = null)
    {
        WritePrivateProfileString(Section ?? EXE, Key, Value, Path);
    }

    public void DeleteKey(string Key, string Section = null)
    {
        Write(Key, null, Section ?? EXE);
    }

    public void DeleteSection(string Section = null)
    {
        Write(null, null, Section ?? EXE);
    }

    public bool KeyExists(string Key, string Section = null)
    {
        return Read(Key, Section).Length > 0;
    }
}