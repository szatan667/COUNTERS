using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace COUNTERS
{
    public partial class COUNTERS : Form
    {
        private readonly NotifyIcon trayIcon;

        PerformanceCounter pc;
        PerformanceCounterCategory[] categories;
        int[] val = new int[5];

        //Window initialization
        public COUNTERS()
        {
            InitializeComponent();

            trayIcon = new NotifyIcon()
            {
                Text = "Blink",
                Visible = true,
                Icon = Properties.Resources.green0,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                   new MenuItem("Exit", menuExit)
                   {
                       OwnerDraw = true,
                       Tag = new Font("Anonymous Pro", 16, FontStyle.Bold)
                   }
                })
            };

            //Register menu events
            trayIcon.ContextMenu.MenuItems[0].DrawItem += MenuItemDraw;
            trayIcon.ContextMenu.MenuItems[0].MeasureItem += MenuItemMeasure;
            trayIcon.DoubleClick += trayIcon_MouseDoubleClick;

            //Fill in object list
            foreach (PerformanceCounterCategory cat in categories = PerformanceCounterCategory.GetCategories())
                comboCategory.Items.Add(cat.CategoryName);

            //comboCategory.Sorted = true;
            this.Icon = Properties.Resources.green0;
            trayIcon.Icon = Properties.Resources.green0;

            //Try to pick total disk time by default
            //(will work for English only probably) 
            if ((comboCategory.SelectedIndex = comboCategory.FindString("PhysicalDisk")) == -1)
                if ((comboCategory.SelectedIndex = comboCategory.FindString("Fysisk disk")) == -1)
                    comboCategory.SelectedIndex = comboCategory.FindString("Dysk fizyczny");
            comboInstance.SelectedIndex = comboInstance.FindString("_Total");
        }

        //Handle menu exit
        private void menuExit(object s, EventArgs e)
        {
            Application.Exit();
        }

        //Draw current readout on screen - here progress bar
        private void timerCnt_Tick(object s, EventArgs e)
        {
            //Read latest value and get the average reading
            if (pc != null)
            {
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

                    if (avg >= 0 && avg <= 100)
                    {
                        progressCnt.Value = avg;
                        labelValue.Text = avg.ToString();
                    }

                    if (avg <= 3)
                    {
                        Icon = Properties.Resources.green0;
                        trayIcon.Icon = Properties.Resources.green0;
                    }
                    else if (avg <= 25)
                    {
                        Icon = Properties.Resources.green25;
                        trayIcon.Icon = Properties.Resources.green25;
                    }
                    else if (avg <= 50)
                    {
                        Icon = Properties.Resources.green50;
                        trayIcon.Icon = Properties.Resources.green50;
                    }
                    else if (avg <= 75)
                    {
                        Icon = Properties.Resources.green75;
                        trayIcon.Icon = Properties.Resources.green75;
                    }
                    else if (avg <= 100)
                    {
                        Icon = Properties.Resources.green100;
                        trayIcon.Icon = Properties.Resources.green100;
                    }
                }
                catch (Exception) { }
            }
        }

        //Disable icon perodically to make it blink
        private void timerIcon_Tick(object s, EventArgs e)
        {
            Icon = Properties.Resources.green0;
            trayIcon.Icon = Properties.Resources.green0;
        }

        //Create counters list when category has been picked
        private void comboCategory_SelectedIndexChanged(object s, EventArgs e)
        {
            pc = null;
            progressCnt.Value = 0;
            comboInstance.Items.Clear();
            comboInstance.SelectedIndex = -1;
            
            foreach (var inst in categories[comboCategory.SelectedIndex].GetInstanceNames())
                comboInstance.Items.Add(inst);
            comboInstance.SelectedIndex = comboInstance.FindString("_Total");

            PerformanceCounter[] counters = categories[comboCategory.SelectedIndex].GetCounters(comboInstance.SelectedItem.ToString());
            foreach (var c in counters)
                comboCounter.Items.Add(c.CounterName);

            if ((comboCounter.SelectedIndex = comboCounter.FindString("% Disk Time")) == -1)
                if ((comboCounter.SelectedIndex = comboCounter.FindString("% Disktid")) == -1)
                    comboCounter.SelectedIndex = comboCounter.FindString("% Czas dysku");
        }

        private void comboInstance_SelectedIndexChanged(object s, EventArgs e)
        {
            //createCounter();
            //textCntDesc.Text = "lalamido";
        }

        private void comboCounter_SelectedIndexChanged(object s, EventArgs e)
        {
            createCounter();
            textCntDesc.Text = pc.CounterHelp;
        }

        //Create actual counter
        private void createCounter()
        {
            try
            {
                pc = new PerformanceCounter(comboCategory.SelectedItem.ToString(),
                    comboCounter.SelectedItem.ToString(),
                    comboInstance.SelectedItem.ToString());
            }
            catch (Exception) { }
        }

        //Go to tray on startup
        private void timerMinimize_Tick(object s, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Hide();
            timerMinimize.Enabled = false;
            timerMinimize.Dispose();
        }

        private void trayIcon_MouseDoubleClick(object s, EventArgs e)
        {
            if (WindowState != FormWindowState.Minimized)
            {
                WindowState = FormWindowState.Minimized;
                Hide();
            }
            else
            {
                Show();
                WindowState = FormWindowState.Normal;
            }
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
}