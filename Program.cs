using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

//Disk acticity icon object
public class DiskLed
{
    //Available colors and drawing position
    public SolidBrush colorbg;
    public SolidBrush color0;
    public SolidBrush color25;
    public SolidBrush color50;
    public SolidBrush color75;
    public SolidBrush color100;
    public Rectangle pos;

    //Drawing area to icon size ratio
    private const double pr = 0.2;    //position to widht ratio
    private const double sr = 1 - pr; //size to width ratio

    public DiskLed(Graphics gfx)
    {
        //Define actual colors here
        colorbg = new SolidBrush(Color.FromArgb(0, 0, 0, 0));
        color0 = new SolidBrush(Color.FromArgb(0, 32, 0));
        color25 = new SolidBrush(Color.FromArgb(0, 64, 0));
        color50 = new SolidBrush(Color.FromArgb(0, 128, 0));
        color75 = new SolidBrush(Color.FromArgb(0, 192, 0));
        color100 = new SolidBrush(Color.FromArgb(0, 255, 0));
        
        pos = new Rectangle((int)(gfx.VisibleClipBounds.Width * pr / 2),
            (int)(gfx.VisibleClipBounds.Height * pr / 2),
            (int)(gfx.VisibleClipBounds.Width * sr),
            (int)(gfx.VisibleClipBounds.Height * sr));
    }
}

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
    PerformanceCounter pc;
    private readonly int[] val = new int[5];

    //Icon drawing variables
    private readonly NotifyIcon TrayIcon;
    private readonly Bitmap bmp;
    private IntPtr h;
    private readonly Graphics gfx;
    private readonly DiskLed led;

    //Timers - counter readout interval and icon blinking interval
    private readonly Timer TimerCnt;
    private readonly Timer TimerIcon;

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
                new MenuItem() {Text = "CATEGORY", Name = "MenuCategory"},
                new MenuItem() {Text = "INSTANCE", Name = "MenuInstance"},
                new MenuItem() {Text = "COUNTER", Name = "MenuCounter"},
                new MenuItem("-") {Name = "Separator" },
                new MenuItem("Exit", MenuExit)
                {
                    OwnerDraw = true,
                    Name = "MenuExit",
                    Tag = new Font("Anonymous Pro", 16, FontStyle.Bold)
                }
            })
        };

        //Counter polling timer
        TimerCnt = new Timer
        {
            Interval = 50,
            Enabled = true,
        };
        TimerCnt.Tick += TimerCnt_Tick;

        //Icon blink timer
        TimerIcon = new Timer
        {
            Interval = TimerCnt.Interval / 2,
            Enabled = false
        };
        TimerIcon.Tick += TimerIcon_Tick;

        //Register custom drawing routines menu events
        foreach (MenuItem mi in TrayIcon.ContextMenu.MenuItems)
        {
            mi.DrawItem += MenuItemDraw;
            mi.MeasureItem += MenuItemMeasure;
        }

        //CATEGORIES
        //Fill in list of performance objects in context menu (eg. processor, disk, network, etc.)
        int ix = 0;
        foreach (PerformanceCounterCategory cat in PerformanceCounterCategory.GetCategories())
        {
            TrayIcon.ContextMenu.MenuItems["MenuCategory"].MenuItems.Add(new MenuItem
            {
                Text = cat.CategoryName,
                Name = cat.CategoryName,
                Tag = ix++,
                RadioCheck = true,
                Checked = false
            });
        }

        //Pick physical disk by default
        foreach (MenuItem mi in TrayIcon.ContextMenu.MenuItems["MenuCategory"].MenuItems)
        {
            mi.Checked = false;
            if (mi.Name == "PhysicalDisk" ||
                mi.Name == "Fysisk disk" || 
                mi.Name == "Dysk fizyczny")
                mi.Checked = true;
        }

        //INSTANCES
        //Now fill instances list for selected category
        ix = 0;
        foreach (string inst in PerformanceCounterCategory.GetCategories()[
            MenuItemIndex("PhysicalDisk",TrayIcon.ContextMenu.MenuItems["MenuCategory"].MenuItems)
            ].GetInstanceNames())
        {
            TrayIcon.ContextMenu.MenuItems["MenuInstance"].MenuItems.Add(new MenuItem
            {
                Text = inst,
                Name = inst,
                Tag = ix++,
                RadioCheck = true,
                Checked = false
            });
        }

        //Pick total (all disks) by default
        foreach (MenuItem mi in TrayIcon.ContextMenu.MenuItems["MenuInstance"].MenuItems)
        {
            mi.Checked = false;
            if (mi.Name == "_Total")
                mi.Checked = true;
        }

        //COUNTERS
        //Fill list of available counters
        ix = 0;
        foreach (PerformanceCounter cnt in PerformanceCounterCategory.GetCategories()[
            MenuItemIndex("PhysicalDisk", TrayIcon.ContextMenu.MenuItems["MenuCategory"].MenuItems)
            ].GetCounters("_Total"))
        {
            TrayIcon.ContextMenu.MenuItems["MenuCounter"].MenuItems.Add(new MenuItem
            {
                Text = cnt.CounterName,
                Name = cnt.CounterName,
                Tag = ix++,
                RadioCheck = true,
                Checked = false
            });
        }

        //Pick disk time as default counter
        foreach (MenuItem mi in TrayIcon.ContextMenu.MenuItems["MenuCounter"].MenuItems)
        {
            if (mi.Name == "% Disk Time" ||
                mi.Name == "% Disktid" ||
                mi.Name == "% Czas dysku")
                mi.Checked = true;
        }

        //Now we are ready to create actual performance counter object
        try
        {
            pc = new PerformanceCounter(
                SelectedMenuItemName(TrayIcon.ContextMenu.MenuItems["MenuCategory"].MenuItems),
                SelectedMenuItemName(TrayIcon.ContextMenu.MenuItems["MenuCounter"].MenuItems),
                SelectedMenuItemName(TrayIcon.ContextMenu.MenuItems["MenuInstance"].MenuItems));
        }
        catch (Exception e) 
        {
            TimerCnt.Enabled = false;
            MessageBox.Show("Well, that's embarassing but something went wrong with counter creation" + 
                Environment.NewLine + e.Message,
                "Bye bye...");
            Application.Exit();
        }

        //Create graphics contex from tray icon?
        bmp = new Bitmap(32, 32);
        gfx = Graphics.FromImage(bmp);
        gfx.SmoothingMode = SmoothingMode.HighQuality;
        led = new DiskLed(gfx);

        DrawTrayIcon(led.color0, true);
    } //MAIN CONSTRUCTOR END

    //Get selected item name
    string SelectedMenuItemName(MenuItem.MenuItemCollection MenuItems)
    {
        foreach (MenuItem mi in MenuItems)
            if (mi.Checked)
                return mi.Name;
        return null; //in case nothing is selected, but should never happen
    }

    //Get menu item index by name
    int MenuItemIndex(string Name, MenuItem.MenuItemCollection MenuItems)
    {
        foreach (MenuItem mi in MenuItems)
            if (mi.Name == Name)
                return (int)mi.Tag;
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
            val[val.Length - 1] = (int)pc.NextValue();

            int sum = 0;
            for (int i = 0; i < val.Length; i++)
                sum += val[i];

            int avg = sum / val.Length;

            if (avg <= 1)
                DrawTrayIcon(led.color0, true);
            else if (avg <= 5)
                DrawTrayIcon(led.color25, true);
            else if (avg <= 15)
                DrawTrayIcon(led.color50, true);
            else if (avg <= 35)
                DrawTrayIcon(led.color75, true);
            else if (avg <= 100)
                DrawTrayIcon(led.color100, true);
        }
        catch (Exception ex)
        {
            TimerCnt.Enabled = false;
            MessageBox.Show("Well, that's embarassing but something went wrong with counter readout" +
                Environment.NewLine + ex.Message,
                "Bye bye...");
            Application.Exit();
        }
    }

    //Draw tray icon like LED light
    private void DrawTrayIcon(Brush b, bool t)
    {
        DestroyIcon(h); //destroy current icon to avoid handle leaking

        //Draw colored circle
        gfx.Clear(led.colorbg.Color);
        gfx.FillEllipse(b, led.pos);

        //Send drawn image to tray icon
        h = bmp.GetHicon();
        TrayIcon.Icon = Icon.FromHandle(h);

        //Enable or disable tray icon timer
        TimerIcon.Enabled = t;
    }

    //Disable icon perodically to make it blink
    private void TimerIcon_Tick(object s, EventArgs e)
    {
        DrawTrayIcon(led.color0, false);
    }

    //Custom menu item drawing - measure the area
    private void MenuItemMeasure(object s, MeasureItemEventArgs e)
    {
        e.ItemHeight = (int)e.Graphics.MeasureString((s as MenuItem).Text, (Font)(s as MenuItem).Tag).Height;
        e.ItemWidth = (int)e.Graphics.MeasureString((s as MenuItem).Text, (Font)(s as MenuItem).Tag).Width;
    }

    //Custom menu item drawing - draw item
    private void MenuItemDraw(object s, DrawItemEventArgs e)
    {
        //Mouse over
        if ((e.State & DrawItemState.Selected) != DrawItemState.None)
            e.Graphics.FillRectangle(new LinearGradientBrush(e.Bounds, Color.Red, Color.Orange, 75), e.Bounds);
        //Mouse out
        else
            e.Graphics.FillRectangle(new LinearGradientBrush(e.Bounds, Color.Green, Color.Gray, 115), e.Bounds);

        e.Graphics.DrawString((s as MenuItem).Text,
            (Font)(s as MenuItem).Tag,
            Brushes.White,
            e.Bounds);
    }
}