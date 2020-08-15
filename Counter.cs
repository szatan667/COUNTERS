using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.Windows.Forms;

//Settings struct - use properties to store the value in runtime and save it to INI file
public struct CounterSettings
{
    public int Number;
    public string CategoryName
    {
        get { return categoryName ?? string.Empty; }
        set { categoryName = value; SaveIni(nameof(CategoryName), value); }
    }
    public string InstanceName
    {
        get { return instanceName ?? string.Empty; }
        set { instanceName = value; SaveIni(nameof(InstanceName), value); }
    }
    public string CounterName
    {
        get { return counterName ?? string.Empty; }
        set { counterName = value; SaveIni(nameof(CounterName), value); }
    }
    public string ColorR
    {
        get { return colorR ?? string.Empty; }
        set { colorR = value; SaveIni(nameof(ColorR), value); }
    }
    public string ColorG
    {
        get { return colorG ?? string.Empty; }
        set { colorG = value; SaveIni(nameof(ColorG), value); }
    }
    public string ColorB
    {
        get { return colorB ?? string.Empty; }
        set { colorB = value; SaveIni(nameof(ColorB), value); }
    }
    public string Shape
    {
        get { return shape ?? string.Empty; }
        set { shape = value; SaveIni(nameof(Shape), value); }
    }
    public string Blinker
    {
        get { return blinker ?? string.Empty; }
        set { blinker = value; SaveIni(nameof(Blinker), value); }
    }
    public string BlinkerType
    {
        get { return blinkerType ?? string.Empty; }
        set { blinkerType = value; SaveIni(nameof(BlinkerType), value); }
    }
    public string RefreshRate
    {
        get { return refreshRate ?? string.Empty; }
        set { refreshRate = value; SaveIni(nameof(refreshRate), value); }
    }

    private void SaveIni(string Setting, string Value)
    {
        COUNTERS.ini.Write(Setting, Value, "Counter" + Number);
    }

    private string categoryName;
    private string instanceName;
    private string counterName;
    private string colorR;
    private string colorG;
    private string colorB;
    private string shape;
    private string blinker;
    private string blinkerType;
    private string refreshRate;
}

//Counter object
public partial class Counter
{
    //To allow icon destroyal
    [DllImport("user32.dll")]
    private extern static bool DestroyIcon(IntPtr handle);

    //Each counter has tray icon, logical LED, actual system counter and set of timers
    //TRAY ICON
    //TODO - drawing objects could be encapsulated in LED object?
    public readonly NotifyIcon TrayIcon;
    private readonly Bitmap Bitmap;
    private IntPtr BitmapHandle;
    private readonly Graphics GFX;
    private readonly DiskLed LED;

    //SYSTEM COUNTER
    private PerformanceCounter PC;

    //TIMERS
    private readonly Timer TimerPoll;
    private readonly Timer TimerBlink;

    //Settings passed from outside world, usually read from INI file
    private CounterSettings Settings;

    //Create counter object with desired settings
    public Counter(CounterSettings CounterSettings)
    {
        //Save counter number
        Settings = CounterSettings;

        //Create tray icon with context menu strip; actual icon is null - it will be drawn in runtime
        TrayIcon = new NotifyIcon()
        {
            Text = "(" + Settings.Number + ") " + "Blink!",
            Visible = true,
            Icon = null,
            ContextMenuStrip = new ContextMenuStrip()
        };

        //Create context menu
        TrayIcon.ContextMenuStrip.Items.AddRange(new ToolStripItem[]
        {
            //Information label - full counter path
            new ToolStripLabel("(" + Settings.Number + ") " + "Pick your counter...") {Name = "MenuCounterName", Enabled = false},

            //Counter consists of category, instance and counter name
            new ToolStripSeparator() {Name = "Separator"},
            new ToolStripMenuItem {Text = "CATEGORY", Name = "MenuCategory"},
            new ToolStripMenuItem {Text = "INSTANCE", Name = "MenuInstance"},
            new ToolStripMenuItem {Text = "COUNTER", Name = "MenuCounter"},

            //Settings section
            new ToolStripSeparator() {Name = "Separator"},
            new ToolStripMenuItem("Blink", null, new ToolStripItem[]
            {
                new ToolStripMenuItem("ON", null, MenuItemClick) {Name = Blinker.On.ToString(), Tag = Blinker.On},
                new ToolStripMenuItem("OFF", null, MenuItemClick) {Name = Blinker.Off.ToString(), Tag = Blinker.Off}
            }
            ) {Name = "MenuBlinker" },

            new ToolStripMenuItem("Blink type", null, new ToolStripItem[]
            {
                new ToolStripMenuItem("Value blink", null, MenuItemClick) {Name = BlinkerType.Value.ToString(), Tag = BlinkerType.Value},
                new ToolStripMenuItem("On/off blink", null, MenuItemClick) {Name = BlinkerType.OnOff.ToString(), Tag = BlinkerType.OnOff}
            }
            ) {Name = "MenuBlinkerType" },

            new ToolStripMenuItem("Color...", null, MenuItemClick) {Tag = new ColorDialog()},
            new ToolStripMenuItem("Shape", null, new ToolStripItem[]
            {
                new ToolStripMenuItem("Circle", null, MenuItemClick) {Name = ((int)LedShape.Circle).ToString(), Tag = LedShape.Circle},
                new ToolStripMenuItem("Rectangle", null, MenuItemClick) {Name = ((int)LedShape.Rectangle).ToString(), Tag = LedShape.Rectangle},
                new ToolStripMenuItem("Vertical bar", null, MenuItemClick) {Name = ((int)LedShape.BarVertical).ToString(), Tag = LedShape.BarVertical},
                new ToolStripMenuItem("Horizontal bar", null, MenuItemClick) {Name = ((int)LedShape.BarHorizontal).ToString(), Tag = LedShape.BarHorizontal},
                new ToolStripMenuItem("Triangle", null, MenuItemClick) {Name = ((int)LedShape.Triangle).ToString(), Tag = LedShape.Triangle}
            }
            ) {Name = "MenuShape" },
            new ToolStripLabel("Refresh rate [ms]:") {Enabled = false},
            new ToolStripTextBox("MenuRefreshRate") {TextBoxTextAlign = HorizontalAlignment.Right},

            //Add,remove or clone counter
            new ToolStripSeparator() {Name = "Separator"},
            new ToolStripMenuItem("Duplicate counter", null, MenuDuplicateCounter) {Tag = Settings.Number},
            new ToolStripMenuItem("Add counter", null, MenuAddCounter),
            new ToolStripMenuItem("Remove counter", null, MenuRemoveCounter) {Name = "MenuRemove", Enabled = false},

            //Exit app
            new ToolStripSeparator() {Name = "Separator"},
            new ToolStripMenuItem("Exit", null, MenuExit) {Name = "MenuExit"}
        });
        TrayIcon.ContextMenuStrip.Items["MenuExit"].Font = new Font(TrayIcon.ContextMenuStrip.Items["MenuExit"].Font, FontStyle.Bold);
        TrayIcon.ContextMenuStrip.Items["MenuRefreshRate"].TextChanged += RefreshRate_TextChanged;

        //Polling and blinking timers, disabled until actual counter is created
        TimerPoll = new Timer {Enabled = false};
        TimerBlink = new Timer {Enabled = false};
        TimerPoll.Tick += TimerPoll_Tick;
        TimerBlink.Tick += TimerBlink_Tick;

        //Now start creating menu items - fill in list of performance categories available (eg. processor, disk, network, etc.)
        FillMenu(TrayIcon.ContextMenuStrip.Items["MenuCategory"], PerformanceCounterCategory.GetCategories());

        //Now 'click' each item in the menu as passed from INI file by caller
        //CATEGORIES
        if (CounterSettings.CategoryName != string.Empty && CounterSettings.CategoryName != null)
            MenuItemClick((TrayIcon.ContextMenuStrip.Items["MenuCategory"] as ToolStripMenuItem).DropDownItems[CounterSettings.CategoryName], null);
        //INSTANCES
        if (CounterSettings.InstanceName != string.Empty && CounterSettings.InstanceName != null)
            MenuItemClick((TrayIcon.ContextMenuStrip.Items["MenuInstance"] as ToolStripMenuItem).DropDownItems[CounterSettings.InstanceName], null);
        //COUNTER NAMES
        if (CounterSettings.CounterName != string.Empty && CounterSettings.CounterName != null)
            MenuItemClick((TrayIcon.ContextMenuStrip.Items["MenuCounter"] as ToolStripMenuItem).DropDownItems[CounterSettings.CounterName], null);

        //Refresh time
        if (CounterSettings.RefreshRate != null && CounterSettings.RefreshRate != string.Empty)
            TrayIcon.ContextMenuStrip.Items["MenuRefreshRate"].Text = CounterSettings.RefreshRate;
        else
            TrayIcon.ContextMenuStrip.Items["MenuRefreshRate"].Text = "50";

        //Create graphics context and logical LED
        Bitmap = new Bitmap(32, 32);
        GFX = Graphics.FromImage(Bitmap);
        GFX.SmoothingMode = SmoothingMode.HighQuality;
        LED = new DiskLed(GFX);

        //BLINKER
        if (CounterSettings.Blinker != string.Empty && CounterSettings.Blinker != null)
        {
            int.TryParse(CounterSettings.Blinker, out int b);
            MenuItemClick((TrayIcon.ContextMenuStrip.Items["MenuBlinker"] as ToolStripMenuItem).DropDownItems[b], null);
        }
        else
            MenuItemClick((TrayIcon.ContextMenuStrip.Items["MenuBlinker"] as ToolStripMenuItem).DropDownItems[(int)Blinker.On], null);

        //BLINKER TYPE
        if (CounterSettings.BlinkerType != string.Empty && CounterSettings.BlinkerType != null)
        {
            int.TryParse(CounterSettings.BlinkerType, out int bt);
            MenuItemClick((TrayIcon.ContextMenuStrip.Items["MenuBlinkerType"] as ToolStripMenuItem).DropDownItems[bt], null);
        }
        else
            MenuItemClick((TrayIcon.ContextMenuStrip.Items["MenuBlinkerType"] as ToolStripMenuItem).DropDownItems[(int)BlinkerType.Value], null);

        //LED COLOR
        if (CounterSettings.ColorR != string.Empty && CounterSettings.ColorG != string.Empty && CounterSettings.ColorB != string.Empty &&
            CounterSettings.ColorR != null && CounterSettings.ColorG != null && CounterSettings.ColorB != null)
        {
            //int r, g, b;
            int.TryParse(CounterSettings.ColorR, out int r);
            int.TryParse(CounterSettings.ColorG, out int g);
            int.TryParse(CounterSettings.ColorB, out int b);
            LED.ColorOn = Color.FromArgb(r, g, b);
        }
        else
            LED.ColorOn = Color.Lime;
        Settings.ColorR = LED.ColorOn.R.ToString();
        Settings.ColorG = LED.ColorOn.G.ToString();
        Settings.ColorB = LED.ColorOn.B.ToString();

        //LED SHAPE
        if (CounterSettings.Shape != string.Empty && CounterSettings.Shape != null)
        {
            int.TryParse(CounterSettings.Shape, out int s);
            MenuItemClick((TrayIcon.ContextMenuStrip.Items["MenuShape"] as ToolStripMenuItem).DropDownItems[s], null);
        }
        else
            MenuItemClick((TrayIcon.ContextMenuStrip.Items["MenuShape"] as ToolStripMenuItem).DropDownItems[((int)LedShape.Circle).ToString()], null);

        //Finally, draw LED with light off
        DrawTrayIcon(LED.ColorOff);
    }

    //Update timers according to GUI input
    private void RefreshRate_TextChanged(object sender, EventArgs e)
    {
        //Minimum for poll timer is 2 ms, because blink timer is always 1/2 of that
        //In case input is not parseable, it is 50 ms by default
        TimerPoll.Interval = int.TryParse(TrayIcon.ContextMenuStrip.Items["MenuRefreshRate"].Text, out int i) ? ((i > 2) ? i : 50) : 50;
        TimerBlink.Interval = TimerPoll.Interval / 2;
        Settings.RefreshRate = ((ToolStripTextBox)sender).Text;
    }

    //Generic menu click handler - execute action according to sender's TAG type
    private void MenuItemClick(object MenuItem, EventArgs e)
    {
        //Place checkmark as default but some items dont need that
        bool placecheckmark = true;

        //Temporary counter name string
        string cnt_name = TrayIcon.Text;

        //Execute click action according to sender's tag type
        //Tag value stores actual value which menu item represents (see menu definition)
        //Each time settings item is clicked, selected value is saved to ini file
        switch ((MenuItem as ToolStripMenuItem).Tag)
        {
            //Color click - show color dialog, change color only if OK pressed inside the dialog
            case ColorDialog cd:
                placecheckmark = false;
                cd.SolidColorOnly = true;
                cd.Color = LED.ColorOn;
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    LED.ColorOn = cd.Color;
                    Settings.ColorR = LED.ColorOn.R.ToString();
                    Settings.ColorG = LED.ColorOn.G.ToString();
                    Settings.ColorB = LED.ColorOn.B.ToString();
                }
                break;

            //Shape click
            case LedShape sh:
                LED.Shape = sh;
                DrawTrayIcon(LED.ColorOff);
                Settings.Shape = ((int)LED.Shape).ToString();
                break;

            //Blinker click
            case Blinker b:
                LED.Blink = b;
                Settings.Blinker = ((int)LED.Blink).ToString();
                break;

            //Blinker type click
            case BlinkerType bt:
                LED.BlinkType = bt;
                Settings.BlinkerType = ((int)LED.BlinkType).ToString();
                break;

            //Category click - clean instances&counters submenus and get list of instances
            case PerformanceCounterCategory pcc:
                TimerPoll.Enabled = false;
                if (PC != null) PC.Dispose();
                (TrayIcon.ContextMenuStrip.Items["MenuInstance"] as ToolStripMenuItem).DropDownItems.Clear();
                (TrayIcon.ContextMenuStrip.Items["MenuCounter"] as ToolStripMenuItem).DropDownItems.Clear();
                FillMenu(TrayIcon.ContextMenuStrip.Items["MenuInstance"], //destination submenu
                    pcc.GetInstanceNames()); //filler objects - instance names
                cnt_name = "(" + Settings.Number + ") " + pcc.CategoryName;

                break;

            //Instance click - clean counter names submenu and fill it with fresh list
            case string inst:
                TimerPoll.Enabled = false;
                if (PC != null) PC.Dispose();
                (TrayIcon.ContextMenuStrip.Items["MenuCounter"] as ToolStripMenuItem).DropDownItems.Clear();
                FillMenu(TrayIcon.ContextMenuStrip.Items["MenuCounter"],
                         PerformanceCounterCategory.GetCategories()[SelectedMenuItemIndex(
                             TrayIcon.ContextMenuStrip.Items["MenuCategory"],
                             SelectedMenuItemName(TrayIcon.ContextMenuStrip.Items["MenuCategory"]))].GetCounters(inst));
                cnt_name += "\\" + inst;
                break;

            //Counter name click - create actual counter
            case PerformanceCounter pc:
                try
                {
                    PC = pc;
                    TimerPoll.Enabled = true;
                    cnt_name = "(" + Settings.Number + ") " + PC.CategoryName + "\\" + PC.InstanceName + "\\" + PC.CounterName;
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

            //Should never happen
            default:
                throw new Exception("Counter menu click event failed :(" + Environment.NewLine + MenuItem);
        }

        //Place checkmark if desired
        if (placecheckmark)
        {
            //Go through menu items at the same level (all from sender's owner)
            //Don't look for currently checked item - just clear them all first...
            foreach (object mi in (MenuItem as ToolStripMenuItem).Owner.Items)
                (mi as ToolStripMenuItem).Checked = false;

            //...and then mark desired as checked
            (MenuItem as ToolStripMenuItem).Checked = true;

            //Write changed counter settings to INI file
            Settings.CategoryName = SelectedMenuItemName(TrayIcon.ContextMenuStrip.Items["MenuCategory"]);
            Settings.InstanceName = SelectedMenuItemName(TrayIcon.ContextMenuStrip.Items["MenuInstance"]);
            Settings.CounterName = SelectedMenuItemName(TrayIcon.ContextMenuStrip.Items["MenuCounter"]);
        }
        TrayIcon.Text = cnt_name.Substring(0, (cnt_name.Length > 63) ? 63 : cnt_name.Length);
        TrayIcon.ContextMenuStrip.Items["MenuCounterName"].Text = cnt_name;
    }

    //Fill counter submenus with desired list
    private void FillMenu(ToolStripItem MenuItem, object[] Filler)
    {
        ToolStripMenuItem mi = MenuItem as ToolStripMenuItem;
        mi.DropDownItems.Clear();

        //MenuItem is destination menu; Filler determines type of items to be put in the menu
        switch (Filler)
        {
            //Counter category items
            case PerformanceCounterCategory[] pcc:
                foreach (PerformanceCounterCategory cat in pcc)
                    mi.DropDownItems.Add(new ToolStripMenuItem(cat.CategoryName, null, MenuItemClick)
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
                    mi.DropDownItems.Add(new ToolStripMenuItem(inst, null, MenuItemClick)
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
                    mi.DropDownItems.Add(new ToolStripMenuItem(cnt.CounterName, null, MenuItemClick)
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
    private string SelectedMenuItemName(ToolStripItem MenuItem)
    {
        foreach (ToolStripMenuItem mi in (MenuItem as ToolStripMenuItem).DropDownItems)
            if (mi.Checked)
                return mi.Name;
        return null;
    }

    //Get menu item index by name
    private int SelectedMenuItemIndex(ToolStripItem MenuItem, string Name)
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
                //Value-based blinker - average over couple of readouts
                case BlinkerType.Value:
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
                    break;

                //On-off blinker - blink if counter reports something else than zero
                case BlinkerType.OnOff:
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
        using (SolidBrush b = new SolidBrush(Color))
        {
            //Destroy current icon to avoid handle leaking and GDI errors
            DestroyIcon(BitmapHandle);

            //Draw desired led shape
            GFX.Clear(DiskLed.Background);
            switch (LED.Shape)
            {
                case LedShape.Circle:
                    GFX.FillEllipse(b, LedBounds.BoundsCircle);
                    break;
                case LedShape.Rectangle:
                    GFX.FillRectangle(b, LedBounds.BoundsRectangle);
                    break;
                case LedShape.BarVertical:
                    GFX.FillRectangle(b, LedBounds.BoundsBarVertical);
                    break;
                case LedShape.BarHorizontal:
                    GFX.FillRectangle(b, LedBounds.BoundsBarHorizontal);
                    break;
                case LedShape.Triangle:
                    GFX.FillPolygon(b, LedBounds.BoundsTriangle);
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
                case Blinker.On:
                    TimerBlink.Enabled = true;
                    break;
                case Blinker.Off:
                    TimerBlink.Enabled = false;
                    break;
                default:
                    throw new Exception("Counter blinker state invalid :(" + Environment.NewLine + LED.Blink);
            }
        }
    }

    //Duplicate existing counter
    private void MenuDuplicateCounter(object MenuItem, EventArgs e)
    {
        CounterSettings cs = Settings;
        cs.Number++;
        COUNTERS.Counters.Add(new Counter(cs));

        if (COUNTERS.Counters.Count > 1)
            foreach (Counter c in COUNTERS.Counters)
                c.TrayIcon.ContextMenuStrip.Items["MenuRemove"].Enabled = true;

        COUNTERS.ini.Write("numberOfCounters", COUNTERS.Counters.Count.ToString());
    }

    //Add new counter
    private void MenuAddCounter(object s, EventArgs e)
    {
        COUNTERS.Counters.Add(new Counter(new CounterSettings { Number = COUNTERS.Counters.Count + 1 }));

        if (COUNTERS.Counters.Count > 1)
            foreach (Counter c in COUNTERS.Counters)
                c.TrayIcon.ContextMenuStrip.Items["MenuRemove"].Enabled = true;

        COUNTERS.ini.Write("numberOfCounters", COUNTERS.Counters.Count.ToString());
    }

    //Remove current counter
    private void MenuRemoveCounter(object s, EventArgs e)
    {
        COUNTERS.Counters.Remove(this);
        Dispose();

        if (COUNTERS.Counters.Count == 1)
            foreach (Counter c in COUNTERS.Counters)
                c.TrayIcon.ContextMenuStrip.Items["MenuRemove"].Enabled = false;

        COUNTERS.ini.Write("numberOfCounters", COUNTERS.Counters.Count.ToString());
        COUNTERS.ini.DeleteSection("Counter" + Settings.Number);
    }

    //Exit click
    private void MenuExit(object s, EventArgs e)
    {
        foreach (Counter c in COUNTERS.Counters)
            c.Dispose();

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