using System;
using System.Collections.Generic;
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

    //Counter number, taken from caller
    private readonly int Number;

    //Each counter has tray icon, logical icon, actual system counter and set of timers
    //TRAY ICON
    //TODO - drawing objects could be encapsulated in LED object?
    public readonly NotifyIcon TrayIcon;
    private readonly Bitmap Bitmap;
    private IntPtr BitmapHandle;
    private readonly Graphics GFX;
    private readonly DiskLed LED;
    private SolidBrush Brush;
    private string Name;

    //SYSTEM COUNTER
    private PerformanceCounter PC;

    //TIMERS
    private readonly Timer TimerPoll;
    private readonly Timer TimerBlink;

    //Settings struct, used by constructor
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
        public string Blinker;
        public string BlinkerType;
    }

    //Create counter object with default constructor
    public Counter(CounterSettings Settings)
    {
        //Save counter number
        Number = Settings.Number;

        //Create tray icon with context menu consisting of counter categories, types, etc.
        TrayIcon = new NotifyIcon()
        {
            Text = "Blink!",
            Visible = true,
            Icon = null,
            ContextMenuStrip = new ContextMenuStrip()
        };

        TrayIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
        {
            //Counter definition related
            new ToolStripMenuItem {Text = "CATEGORY", Name = "MenuCategory"},
            new ToolStripMenuItem {Text = "INSTANCE", Name = "MenuInstance"},
            new ToolStripMenuItem {Text = "COUNTER", Name = "MenuCounter"},

            //Settings
            new ToolStripSeparator() {Name = "Separator"},
            new ToolStripMenuItem("Blink", null, new ToolStripItem[]
            {
                new ToolStripMenuItem("ON", null, MenuCheckMark) {Name = DiskLed.Blinker.On.ToString(), Tag = DiskLed.Blinker.On},
                new ToolStripMenuItem("OFF", null, MenuCheckMark) {Name = DiskLed.Blinker.Off.ToString(), Tag = DiskLed.Blinker.Off}
            }
            ) {Name = "MenuBlinker" },

            new ToolStripMenuItem("Blink type", null, new ToolStripItem[]
            {
                new ToolStripMenuItem("Value blink", null, MenuCheckMark) {Name = DiskLed.BlinkerType.Value.ToString(), Tag = DiskLed.BlinkerType.Value},
                new ToolStripMenuItem("On/off blink", null, MenuCheckMark) {Name = DiskLed.BlinkerType.OnOff.ToString(), Tag = DiskLed.BlinkerType.OnOff}
            }
            ) {Name = "MenuBlinkerType" },

            new ToolStripMenuItem("Color...", null, MenuCheckMark) {Tag = new ColorDialog()},
            new ToolStripMenuItem("Shape", null, new ToolStripItem[]
            {
                new ToolStripMenuItem("Circle", null, MenuCheckMark) {Name = ((int)DiskLed.Shapes.Circle).ToString(), Tag = DiskLed.Shapes.Circle},
                new ToolStripMenuItem("Rectangle", null, MenuCheckMark) {Name = ((int)DiskLed.Shapes.Rectangle).ToString(), Tag = DiskLed.Shapes.Rectangle},
                new ToolStripMenuItem("Vertical bar", null, MenuCheckMark) {Name = ((int)DiskLed.Shapes.BarVertical).ToString(), Tag = DiskLed.Shapes.BarVertical},
                new ToolStripMenuItem("Horizontal bar", null, MenuCheckMark) {Name = ((int)DiskLed.Shapes.BarHorizontal).ToString(), Tag = DiskLed.Shapes.BarHorizontal},
                new ToolStripMenuItem("Triangle", null, MenuCheckMark) {Name = ((int)DiskLed.Shapes.Triangle).ToString(), Tag = DiskLed.Shapes.Triangle}
            }
            ) {Name = "MenuShape" },
            new ToolStripMenuItem("Refresh rate [ms]:") {Enabled = false},
            new ToolStripTextBox("MenuRefreshRate") {TextBoxTextAlign = HorizontalAlignment.Right},

            //Add/remove counter
            new ToolStripSeparator() {Name = "Separator"},
            new ToolStripMenuItem("Duplicate counter", null, MenuDuplicateCounter) {Tag = Number},
            new ToolStripMenuItem("Add counter", null, MenuAddCounter),
            new ToolStripMenuItem("Remove counter", null, MenuRemoveCounter) {Name = "MenuRemove", Enabled = false},

            //Exit app
            new ToolStripSeparator() {Name = "Separator"},
            new ToolStripMenuItem("Exit", null, MenuExit) {Name = "MenuExit"}
        });

        //Polling and blinking timers
        TimerPoll = new Timer
        {
            Enabled = false,
        };

        TimerBlink = new Timer
        {
            Enabled = false
        };

        TimerPoll.Tick += TimerPoll_Tick;
        TimerBlink.Tick += TimerBlink_Tick;
        TrayIcon.ContextMenuStrip.Items["MenuRefreshRate"].TextChanged += RefreshRate_TextChanged;
        TrayIcon.ContextMenuStrip.Items["MenuRefreshRate"].Text = "50";

        //Fill in list of performance categories available (eg. processor, disk, network, etc.)
        FillMenu(TrayIcon.ContextMenuStrip.Items["MenuCategory"], PerformanceCounterCategory.GetCategories());

        //Now 'click' each item in the menu if passed from INI file by caller
        //CATEGORIES
        if (Settings.CategoryName != string.Empty && Settings.CategoryName != null)
            MenuCheckMark((TrayIcon.ContextMenuStrip.Items["MenuCategory"] as ToolStripMenuItem).DropDownItems[Settings.CategoryName], null);
        //INSTANCES
        if (Settings.InstanceName != string.Empty && Settings.InstanceName != null)
            MenuCheckMark((TrayIcon.ContextMenuStrip.Items["MenuInstance"] as ToolStripMenuItem).DropDownItems[Settings.InstanceName], null);
        //COUNTERS
        if (Settings.CounterName != string.Empty && Settings.CounterName != null)
            MenuCheckMark((TrayIcon.ContextMenuStrip.Items["MenuCounter"] as ToolStripMenuItem).DropDownItems[Settings.CounterName], null);

        //Create graphics context
        Bitmap = new Bitmap(32, 32);
        GFX = Graphics.FromImage(Bitmap);
        GFX.SmoothingMode = SmoothingMode.HighQuality;

        //Create LED object and get its settings from ini file
        LED = new DiskLed(GFX);

        //Blinker
        if (Settings.Blinker != string.Empty && Settings.Blinker != null)
        {
            int.TryParse(Settings.Blinker, out int b);
            MenuCheckMark((TrayIcon.ContextMenuStrip.Items["MenuBlinker"] as ToolStripMenuItem).DropDownItems[b], null);
        }
        else
            MenuCheckMark((TrayIcon.ContextMenuStrip.Items["MenuBlinker"] as ToolStripMenuItem).DropDownItems[(int)DiskLed.Blinker.On], null);

        if (Settings.BlinkerType != string.Empty && Settings.BlinkerType != null)
        {
            int.TryParse(Settings.BlinkerType, out int bt);
            MenuCheckMark((TrayIcon.ContextMenuStrip.Items["MenuBlinkerType"] as ToolStripMenuItem).DropDownItems[bt], null);
        }
        else
            MenuCheckMark((TrayIcon.ContextMenuStrip.Items["MenuBlinkerType"] as ToolStripMenuItem).DropDownItems[(int)DiskLed.BlinkerType.Value], null);

        //Color
        if (Settings.ColorR != string.Empty && Settings.ColorG != string.Empty && Settings.ColorB != string.Empty &&
            Settings.ColorR != null && Settings.ColorG != null && Settings.ColorB != null)
        {
            //int r, g, b;
            int.TryParse(Settings.ColorR, out int r);
            int.TryParse(Settings.ColorG, out int g);
            int.TryParse(Settings.ColorB, out int b);
            LED.ColorOn = Color.FromArgb(r, g, b);
        }
        else
            LED.ColorOn = Color.Lime;

        //Shape
        if (Settings.Shape != string.Empty && Settings.Shape != null)
        {
            int.TryParse(Settings.Shape, out int s);
            MenuCheckMark((TrayIcon.ContextMenuStrip.Items["MenuShape"] as ToolStripMenuItem).DropDownItems[s], null);
        }
        else
            MenuCheckMark((TrayIcon.ContextMenuStrip.Items["MenuShape"] as ToolStripMenuItem).DropDownItems[((int)DiskLed.Shapes.Circle).ToString()], null);

        //Finally, draw LED with light off
        DrawTrayIcon(LED.ColorOff);
    }

    private void RefreshRate_TextChanged(object sender, EventArgs e)
    {
        TimerPoll.Interval = int.TryParse(TrayIcon.ContextMenuStrip.Items["MenuRefreshRate"].Text, out int i) ? ((i > 2) ? i : 50) : 50;
        TimerBlink.Interval = TimerPoll.Interval / 2;
    }

    //Set menu item check mark and execute action according to sender's TAG type
    private void MenuCheckMark(object MenuItem, EventArgs e)
    {
        //Place checkmark as default but some items dont need that
        bool placecheckmark = true;

        //Execute click action according to sender's tag value 
        switch ((MenuItem as ToolStripMenuItem).Tag)
        {
            //Color click - show color dialog, change the olor only if OK pressed inside the dialog
            case ColorDialog cd:
                placecheckmark = false;
                cd.SolidColorOnly = true;
                cd.Color = LED.ColorOn;
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    LED.ColorOn = cd.Color;
                    COUNTERS.ini.Write("ledColorR" + Number, LED.ColorOn.R.ToString());
                    COUNTERS.ini.Write("ledColorG" + Number, LED.ColorOn.G.ToString());
                    COUNTERS.ini.Write("ledColorB" + Number, LED.ColorOn.B.ToString());
                }
                break;

            //Shape click
            case DiskLed.Shapes sh:
                LED.Shape = sh;
                DrawTrayIcon(LED.ColorOff);
                COUNTERS.ini.Write("ledShape" + Number, ((int)LED.Shape).ToString());
                break;

            //Blinker click
            case DiskLed.Blinker b:
                LED.Blink = b;
                COUNTERS.ini.Write("ledBlinker" + Number, ((int)LED.Blink).ToString());
                break;

            //Blinker type click
            case DiskLed.BlinkerType bt:
                LED.BlinkType = bt;
                COUNTERS.ini.Write("ledBlinkerType" + Number, ((int)LED.BlinkType).ToString());
                break;

            //Category click - clean instances&counters submenus and get list of instances
            case PerformanceCounterCategory pcc:
                TimerPoll.Enabled = false;
                if (PC != null) PC.Dispose();
                (TrayIcon.ContextMenuStrip.Items["MenuInstance"] as ToolStripMenuItem).DropDownItems.Clear();
                (TrayIcon.ContextMenuStrip.Items["MenuCounter"] as ToolStripMenuItem).DropDownItems.Clear();
                FillMenu(TrayIcon.ContextMenuStrip.Items["MenuInstance"], //destination submenu
                    pcc.GetInstanceNames()); //filler objects - instance names
                TrayIcon.Text = pcc.CategoryName;
                break;

            //Instance click - clean counters submenu and fill it with fresh list
            case string inst:
                TimerPoll.Enabled = false;
                if (PC != null) PC.Dispose();
                (TrayIcon.ContextMenuStrip.Items["MenuCounter"] as ToolStripMenuItem).DropDownItems.Clear();
                FillMenu(TrayIcon.ContextMenuStrip.Items["MenuCounter"],
                         PerformanceCounterCategory.GetCategories()[MenuItemIndex(
                             MenuItemName(TrayIcon.ContextMenuStrip.Items["MenuCategory"]),
                             TrayIcon.ContextMenuStrip.Items["MenuCategory"])].GetCounters(inst));
                break;

            //Counter click - create actual counter
            case PerformanceCounter pc:
                try
                {
                    PC = pc;
                    Name = PC.CategoryName + "\\" + PC.InstanceName + "\\" + PC.CounterName;
                    TrayIcon.Text = Name;
                    TimerPoll.Enabled = true;
                }
                catch (Exception ex)
                {
                    TimerPoll.Enabled = false;
                    MessageBox.Show("Well, that's embarassing but something went wrong with counter creation" +
                        Environment.NewLine + ex.Message +
                        Environment.NewLine + MenuItem,
                        "Bye bye...");
                    Application.Exit();
                }
                break;

            default:
                throw new Exception("Counter menu click event failed :(" + Environment.NewLine + MenuItem);
        }

        //Place checkmark if desired
        if (placecheckmark)
        {
            //Go through menu items at the same level (all from sender's parent)
            //Don't look for currently checked item - just clear them all first...
            foreach (var mi in (MenuItem as ToolStripMenuItem).Owner.Items)
                (mi as ToolStripMenuItem).Checked = false;

            //...and then mark desired as checked
            (MenuItem as ToolStripMenuItem).Checked = true;

            //Write changed counter settings to INI file
            COUNTERS.ini.Write("categoryName" + Number, MenuItemName(TrayIcon.ContextMenuStrip.Items["MenuCategory"]));
            COUNTERS.ini.Write("instanceName" + Number, MenuItemName(TrayIcon.ContextMenuStrip.Items["MenuInstance"]));
            COUNTERS.ini.Write("counterName" + Number, MenuItemName(TrayIcon.ContextMenuStrip.Items["MenuCounter"]));
        }
    }

    //Fill counter menu list with desired object list
    private void FillMenu(ToolStripItem MenuItem, object[] Filler)
    {
        var mi = MenuItem as ToolStripMenuItem;
        mi.DropDownItems.Clear();

        //Filler determines type of items to be put in the menu
        switch (Filler)
        {
            //Counter category items
            case PerformanceCounterCategory[] pcc:
                foreach (PerformanceCounterCategory cat in pcc)
                    mi.DropDownItems.Add(new ToolStripMenuItem(cat.CategoryName, null, MenuCheckMark)
                    {
                        Name = cat.CategoryName,
                        Checked = false,
                        Tag = cat
                    });
                break;

            //Counter instance - this one has no special type, it is ordinary string
            case string[] s:
                List<string> sl = new List<string>(s);
                sl.Sort();
                foreach (string inst in sl)
                    mi.DropDownItems.Add(new ToolStripMenuItem(inst, null, MenuCheckMark)
                    {
                        Name = inst,
                        Checked = false,
                        Tag = inst
                    });
                sl.Clear();
                break;

            //Counter name
            case PerformanceCounter[] pc:
                foreach (PerformanceCounter cnt in pc)
                    mi.DropDownItems.Add(new ToolStripMenuItem(cnt.CounterName, null, MenuCheckMark)
                    {
                        Name = cnt.CounterName,
                        Checked = false,
                        Tag = cnt
                    });
                break;

            default:
                throw new Exception("Counter menu build failed :(");
        }
    }

    //Get selected menu item name
    private string MenuItemName(ToolStripItem MenuItem)
    {
        foreach (ToolStripMenuItem mi in (MenuItem as ToolStripMenuItem).DropDownItems)
            if (mi.Checked)
                return mi.Name;
        return null;
    }

    //Get menu item index by name
    private int MenuItemIndex(string Name, ToolStripItem MenuItem)
    {
        for (int i = 0; i < (MenuItem as ToolStripMenuItem).DropDownItems.Count; i++)
            if ((MenuItem as ToolStripMenuItem).DropDownItems[i].Name == Name)
                return i;
        return -1;
    }

    //Icon blink timer - draw "off" light to make it blink
    private void TimerBlink_Tick(object s, EventArgs e)
    {
        DrawTrayIcon(LED.ColorOff);
        TimerBlink.Enabled = false;
    }

    //Timer event for counter readout
    private void TimerPoll_Tick(object s, EventArgs e)
    {
        //Read latest value and get the average reading
        try
        {
            switch (LED.BlinkType)
            {
                //Value-based blinker
                case DiskLed.BlinkerType.Value:
                    int[] Value = new int[5];
                    int Average;

                    //Shift values left, make room for new readout
                    for (int i = 0; i < Value.Length - 1; i++)
                        Value[i] = Value[i + 1];
                    Value[Value.Length - 1] = (int)PC.NextValue();

                    //Calculate average value
                    int sum = 0;
                    for (int i = 0; i < Value.Length; i++)
                        sum += Value[i];
                    Average = sum / Value.Length;

                    //Now do some scaling - disk load is usually below 50% so pump it up a bit
                    if (Average <= 2) Average *= 10;
                    else if (Average <= 5) Average *= 7;
                    else if (Average <= 10) Average *= 4;
                    else if (Average <= 25) Average *= 2;
                    else if (Average <= 50) Average = (int)(Average * 1.5);
                    else if (Average <= 75) Average = (int)(Average * 1.25);
                    else if (Average <= 100) Average *= 1;
                    else if (Average > 100) Average = 100; //just in case for some reason calc goes out of bounds (eg. dummy readout out of scale?)

                    DrawTrayIcon(Color.FromArgb(
                        LED.ColorOn.R * Average / 100,
                        LED.ColorOn.G * Average / 100,
                        LED.ColorOn.B * Average / 100
                        ));

                    TrayIcon.Text = Name + Environment.NewLine + "Value = " + Average.ToString();
                    break;

                //On-off blinker
                case DiskLed.BlinkerType.OnOff:
                    if (PC.NextValue() > 0)
                        DrawTrayIcon(LED.ColorOn);
                    break;
                default:
                    throw new Exception("Counter blinker type invalid :(" + Environment.NewLine + LED.BlinkType);
            }
        }
        catch (Exception ex)
        {
            TimerPoll.Enabled = false;
            MessageBox.Show("Well, that's embarassing but something went wrong with counter readout" +
                Environment.NewLine + ex.Message,
                "Bye bye...");
            Application.Exit();
        }
    }

    //Draw tray icon light
    private void DrawTrayIcon(Color Color)
    {
        Brush = new SolidBrush(Color);
        DestroyIcon(BitmapHandle); //destroy current icon to avoid handle leaking

        //Draw desired led shape
        GFX.Clear(DiskLed.Background);
        switch (LED.Shape)
        {
            case DiskLed.Shapes.Circle:
                GFX.FillEllipse(Brush, DiskLed.Bounds.BoundsCircle);
                break;
            case DiskLed.Shapes.Rectangle:
                GFX.FillRectangle(Brush, DiskLed.Bounds.BoundsRectangle);
                break;
            case DiskLed.Shapes.BarVertical:
                GFX.FillRectangle(Brush, DiskLed.Bounds.BoundsBarVertical);
                break;
            case DiskLed.Shapes.BarHorizontal:
                GFX.FillRectangle(Brush, DiskLed.Bounds.BoundsBarHorizontal);
                break;
            case DiskLed.Shapes.Triangle:
                GFX.FillPolygon(Brush, DiskLed.Bounds.BoundsTriangle);
                break;
            default:
                break;
        }

        //Send drawn image to tray icon
        BitmapHandle = Bitmap.GetHicon();
        TrayIcon.Icon = Icon.FromHandle(BitmapHandle);

        //To blink or not to blink
        switch (LED.Blink)
        {
            case DiskLed.Blinker.On:
                TimerBlink.Enabled = true;
                break;
            case DiskLed.Blinker.Off:
                TimerBlink.Enabled = false;
                break;
            default:
                throw new Exception("Counter blinker state invalid :(" + Environment.NewLine + LED.Blink);
        }

    }

    //Duplicate counter
    private void MenuDuplicateCounter(object MenuItem, EventArgs e)
    {
        //TODO - this is nasty..... should be possible to take counter settings straight from counter.Settings or similar
        COUNTERS.counters.Add(new Counter(new Counter.CounterSettings
        {
            Number = COUNTERS.counters.Count + 1,
            CategoryName = PC.CategoryName,
            InstanceName = PC.InstanceName,
            CounterName = PC.CounterName,
            Shape = ((int)LED.Shape).ToString(),
            ColorR = LED.ColorOn.R.ToString(),
            ColorG = LED.ColorOn.G.ToString(),
            ColorB = LED.ColorOn.B.ToString(),
            Blinker = LED.Blink.ToString(),
            BlinkerType = LED.BlinkType.ToString()
        }));

        if (COUNTERS.counters.Count > 1)
            foreach (Counter c in COUNTERS.counters)
                c.TrayIcon.ContextMenuStrip.Items["MenuRemove"].Enabled = true;

        COUNTERS.ini.Write("numberOfCounters", COUNTERS.counters.Count.ToString());
    }

    //Add counter
    private void MenuAddCounter(object s, EventArgs e)
    {
        COUNTERS.counters.Add(new Counter(new Counter.CounterSettings { Number = COUNTERS.counters.Count + 1 }));

        if (COUNTERS.counters.Count > 1)
            foreach (Counter c in COUNTERS.counters)
                c.TrayIcon.ContextMenuStrip.Items["MenuRemove"].Enabled = true;

        COUNTERS.ini.Write("numberOfCounters", COUNTERS.counters.Count.ToString());
    }

    //Remove counter
    private void MenuRemoveCounter(object s, EventArgs e)
    {
        COUNTERS.counters.Remove(this);
        Dispose();

        if (COUNTERS.counters.Count == 1)
            foreach (Counter c in COUNTERS.counters)
                c.TrayIcon.ContextMenuStrip.Items["MenuRemove"].Enabled = false;

        COUNTERS.ini.Write("numberOfCounters", COUNTERS.counters.Count.ToString());
        COUNTERS.ini.DeleteKey("categoryName" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("instanceName" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("counterName" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("ledColorR" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("ledColorG" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("ledColorB" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("ledShape" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("ledBlinker" + (COUNTERS.counters.Count + 1).ToString());
        COUNTERS.ini.DeleteKey("ledBlinkerType" + (COUNTERS.counters.Count + 1).ToString());
    }

    //Exit click
    private void MenuExit(object s, EventArgs e)
    {
        foreach (Counter c in COUNTERS.counters)
            c.TrayIcon.Dispose();
        Application.Exit();
    }
}

//Object disposal
public partial class Counter : IDisposable
{
    bool disposed;

    //Dispose used resources
    public void Dispose()
    {
        if (!disposed)
        {
            TimerPoll.Enabled = false;
            TimerBlink.Enabled = false;
            TrayIcon.Dispose();
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }
}