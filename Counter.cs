using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;
//Counter object
public partial class Counter
{
    //To allow icon destroyal
    [DllImport("user32.dll")]
    private extern static bool DestroyIcon(IntPtr handle);

    //Counter number
    private int number;

    //Each counter has tray icon, logical icon, actual system counter and set of timers
    //TRAY ICON
    public readonly NotifyIcon TrayIcon;
    private readonly Bitmap bmp;
    private IntPtr h;
    private readonly Graphics gfx;
    private readonly DiskLed led;
    private SolidBrush brush;
    private string name;

    //SYSTEM COUNTER
    private PerformanceCounter PC;
    private readonly int[] val = new int[5];
    private int avg;

    //TIMERS
    private readonly Timer TimerCnt;
    private readonly Timer TimerIcon;

    //Settings struct
    public struct CounterSettings
    {
        public int Number;
        public string CategoryName;
        public string InstanceName;
        public string CounterName;
        public string ColorR;
        public string ColorG;
        public string ColorB;
        public string Shape;
    }

    //Create counter object with default constructor
    public Counter(CounterSettings cs)
    {
        //Save counter number
        number = cs.Number;

        //Create tray icon with context menu consisting of counter categories, types, etc.
        TrayIcon = new NotifyIcon()
        {
            Text = "Blink!",
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
                        new MenuItem("Horizontal bar", MenuCheckMark) {Name = ((int)DiskLed.shapes.BarHorizontal).ToString(), Tag = DiskLed.shapes.BarHorizontal},
                        new MenuItem("Triangle", MenuCheckMark) {Name = ((int)DiskLed.shapes.Triangle).ToString(), Tag = DiskLed.shapes.Triangle}
                    }
                    ) {Name = "MenuShape" },

                    //Add/remove counter
                    new MenuItem("-") {Name = "Separator"},
                    new MenuItem("Add counter", MenuAddCounter) { Tag = 1 },
                    new MenuItem("Remove counter", MenuRemoveCounter) {Name = "MenuRemove", Tag = 1 },

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

        //CATEGORIES
        if (cs.CategoryName != string.Empty && cs.CategoryName != null)
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuCategory"].MenuItems[cs.CategoryName], null);
        //INSTANCES
        if (cs.InstanceName != string.Empty && cs.InstanceName != null)
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuInstance"].MenuItems[cs.InstanceName], null);
        //COUNTERS
        if (cs.CounterName != string.Empty && cs.CounterName != null)
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuCounter"].MenuItems[cs.CounterName], null);

        //Create graphics context
        bmp = new Bitmap(32, 32);
        gfx = Graphics.FromImage(bmp);
        gfx.SmoothingMode = SmoothingMode.HighQuality;

        //Create LED object and get the color from ini file
        led = new DiskLed(gfx);

        //Now set led color
        if (cs.ColorR != string.Empty && cs.ColorG != string.Empty && cs.ColorB != string.Empty &&
            cs.ColorR != null && cs.ColorG != null && cs.ColorB != null)
        {
            //int r, g, b;
            int.TryParse(cs.ColorR, out int r);
            int.TryParse(cs.ColorG, out int g);
            int.TryParse(cs.ColorB, out int b);
            led.SetLedColor(Color.FromArgb(r, g, b));
        }
        else
            led.SetLedColor(Color.Lime);

        //Now for led shape
        if (cs.Shape != string.Empty && cs.Shape != null)
        {
            int.TryParse(cs.Shape, out int s);
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuShape"].MenuItems[s], null);
        }
        else
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuShape"].MenuItems[((int)DiskLed.shapes.Circle).ToString()], null);

        //Start with led off
        DrawTrayIcon(led.ledOFF, false);
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
                    COUNTERS.ini.Write("ledColorR" + number, led.ledON.R.ToString());
                    COUNTERS.ini.Write("ledColorG" + number, led.ledON.G.ToString());
                    COUNTERS.ini.Write("ledColorB" + number, led.ledON.B.ToString());
                }
                break;

            //Shape click
            case DiskLed.shapes sh:
                led.shape = sh;
                DrawTrayIcon(led.ledOFF, false);
                COUNTERS.ini.Write("ledShape" + number, ((int)led.shape).ToString());
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
                    name = PC.CategoryName + "\\" + PC.InstanceName + "\\" + PC.CounterName;
                    TrayIcon.Text = name;
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

            //Write changed counter settings to INI file
            COUNTERS.ini.Write("categoryName" + number, MenuItemName(TrayIcon.ContextMenu.MenuItems["MenuCategory"]));
            COUNTERS.ini.Write("instanceName" + number, MenuItemName(TrayIcon.ContextMenu.MenuItems["MenuInstance"]));
            COUNTERS.ini.Write("counterName" + number, MenuItemName(TrayIcon.ContextMenu.MenuItems["MenuCounter"]));
        }
    }

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

    //Get selected item name
    private string MenuItemName(MenuItem MenuItem)
    {
        foreach (MenuItem mi in MenuItem.MenuItems)
            if (mi.Checked)
                return mi.Name;
        return null;
    }

    //Get menu item index by name
    private int MenuItemIndex(string Name, MenuItem MenuItem)
    {
        for (int i = 0; i < MenuItem.MenuItems.Count; i++)
            if (MenuItem.MenuItems[i].Name == Name)
                return i;
        return -1;
    }

    //Disable icon perodically to make it blink
    private void TimerIcon_Tick(object s, EventArgs e)
    {
        DrawTrayIcon(led.ledOFF, false);
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

            TrayIcon.Text = name + Environment.NewLine + "Value = " + avg.ToString();
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
            case DiskLed.shapes.Triangle:
                gfx.FillPolygon(brush, DiskLed.boundsTriangle);
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

    //Add counter
    private void MenuAddCounter(object s, EventArgs e)
    {
        COUNTERS.counters.Add(new Counter(new Counter.CounterSettings { Number = COUNTERS.counters.Count + 1 }));

        if (COUNTERS.counters.Count > 1)
            foreach (Counter c in COUNTERS.counters)
                c.TrayIcon.ContextMenu.MenuItems["MenuRemove"].Enabled = true;

        COUNTERS.ini.Write("numberOfCounters", COUNTERS.counters.Count.ToString());
    }

    //Remove counter
    private void MenuRemoveCounter(object s, EventArgs e)
    {
        COUNTERS.counters.Remove(this);
        Dispose();
        
        if (COUNTERS.counters.Count == 1)
            foreach (Counter c in COUNTERS.counters)
                c.TrayIcon.ContextMenu.MenuItems["MenuRemove"].Enabled = false;

        COUNTERS.ini.Write("numberOfCounters", COUNTERS.counters.Count.ToString());
        COUNTERS.ini.DeleteKey("categoryName" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("instanceName" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("counterName" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("ledColorR" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("ledColorG" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("ledColorB" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("ledShape" + (COUNTERS.counters.Count + 1).ToString());
    }

    //Handle menu exit
    private void MenuExit(object s, EventArgs e)
    {
        foreach (Counter c in COUNTERS.counters)
            c.TrayIcon.Dispose();
        Application.Exit();
    }
}

//Dispose it properly
public partial class Counter : IDisposable
{
    bool disposed;

    //Dispose used resources
    public void Dispose()
    {
        if (!disposed)
        {
            TimerCnt.Enabled = false;
            TimerIcon.Enabled = false;
            TrayIcon.Dispose();
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}