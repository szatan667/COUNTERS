﻿using System;
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
    public Color background = Color.FromArgb(0, 0, 0, 0); //background color
    public Color ledOFF = Color.Black;
    public Color ledON;
    public Rectangle pos;

    //Drawing area to icon size ratio
    private const double pr = 0.2;    //position to widht ratio
    private const double sr = 1 - pr; //size to width ratio

    public DiskLed(Graphics gfx)
    {
        pos = new Rectangle((int)(gfx.VisibleClipBounds.Width * pr / 2),
            (int)(gfx.VisibleClipBounds.Height * pr / 2),
            (int)(gfx.VisibleClipBounds.Width * sr),
            (int)(gfx.VisibleClipBounds.Height * sr));
    }

    public void SetLedColor(Color c)
    {
        ledON = c;
    }
}

//Main couter class
public partial class COUNTERSX : ApplicationContext
{
    //Program entry point
    static void Main()
    {
        Application.Run(new COUNTERSX());
    }

    //To allow icon destroyal
    [System.Runtime.InteropServices.DllImport("user32.dll")]
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
    public COUNTERSX()
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

                //Color selection
                new MenuItem("-") {Name = "Separator" },
                new MenuItem("Colors", new MenuItem[]
                {
                    new MenuItem("Red", MenuCheckMark) {Name = "ColorRed", Tag = Color.Red, RadioCheck = true },
                    new MenuItem("Green", MenuCheckMark) {Name = "ColorGreen", Tag = Color.Lime, RadioCheck = true, Checked = true },
                    new MenuItem("Blue", MenuCheckMark) {Name = "ColorBlue", Tag = Color.LightSkyBlue, RadioCheck = true },
                })
                {
                    Name = "MenuColors"
                },

                //Exit app
                new MenuItem("-") {Name = "Separator" },
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

        //CATEGORIES
        //Fill in list of performance objects in context menu (eg. processor, disk, network, etc.)
        FillMenu(TrayIcon.ContextMenu.MenuItems["MenuCategory"], PerformanceCounterCategory.GetCategories());

        //Now try to get counter from ini file
        //If config not present, don't create any counter yet, let user pick it
        string _catname = ini.Read("categoryName");
        string _instname = ini.Read("instanceName");
        string _cntname = ini.Read("counterName");

        if (_catname != string.Empty && _instname != string.Empty && _cntname != string.Empty)
        {
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuCategory"].MenuItems[_catname], null);

            //INSTANCES
            //Instance list is already filled in by menu click event
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuInstance"].MenuItems[_instname], null);

            //COUNTERS
            //Same for counters
            MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuCounter"].MenuItems[_cntname], null);
        }

        //Create graphics context
        bmp = new Bitmap(32, 32);
        gfx = Graphics.FromImage(bmp);
        gfx.SmoothingMode = SmoothingMode.HighQuality;

        //And lastly - new LED object, set default color (one of them has been checked already in constructor)
        led = new DiskLed(gfx);
        MenuCheckMark(TrayIcon.ContextMenu.MenuItems["MenuColors"].MenuItems[MenuItemName(TrayIcon.ContextMenu.MenuItems["MenuColors"])], null);
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
        //Go through menu items at the same level (all from sender's parent)
        //Don't look for currently checked item - just clear them all first...
        foreach (MenuItem mi in (s as MenuItem).Parent.MenuItems)
            mi.Checked = false;

        //...and then mark desired as checked
        (s as MenuItem).Checked = true;

        //Now execute click action according to sender 
        switch ((s as MenuItem).Tag)
        {
            //Color click
            case Color c:
                led.SetLedColor(c);
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
                    ini.Write("categoryName", MenuItemName(TrayIcon.ContextMenu.MenuItems["MenuCategory"]));
                    ini.Write("instanceName", MenuItemName(TrayIcon.ContextMenu.MenuItems["MenuInstance"]));
                    ini.Write("counterName", MenuItemName(TrayIcon.ContextMenu.MenuItems["MenuCounter"]));
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

        //MAJOR TODO - counter selection
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
        //brush.Dispose();
        brush = new SolidBrush(c);
        DestroyIcon(h); //destroy current icon to avoid handle leaking

        //Draw colored circle
        gfx.Clear(led.background);
        gfx.FillEllipse(brush, led.pos);

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