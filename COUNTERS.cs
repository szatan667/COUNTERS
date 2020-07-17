﻿using System;
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
        readonly int[] val = new int[5];

        //Window initialization
        public COUNTERS()
        {
            InitializeComponent();

            trayIcon = new NotifyIcon()
            {
                Text = "Blink",
                Visible = true,
                Icon = Properties.Resources.new0,
                ContextMenu = new ContextMenu(new MenuItem[]
                {
                   new MenuItem("Exit", MenuExit)
                   {
                       OwnerDraw = true,
                       Tag = new Font("Anonymous Pro", 16, FontStyle.Bold)
                   }
                })
            };

            //Register menu events
            trayIcon.ContextMenu.MenuItems[0].DrawItem += MenuItemDraw;
            trayIcon.ContextMenu.MenuItems[0].MeasureItem += MenuItemMeasure;
            trayIcon.DoubleClick += TrayIcon_MouseDoubleClick;
            Application.ApplicationExit += Application_ApplicationExit;

            //Fill in object list
            foreach (PerformanceCounterCategory cat in PerformanceCounterCategory.GetCategories())
                comboCategory.Items.Add(cat.CategoryName);

            //comboCategory.Sorted = true;
            this.Icon = Properties.Resources.new0;
            trayIcon.Icon = Properties.Resources.new0;

            //Try to pick total disk time by default
            //(will work for English only probably) 
            if ((comboCategory.SelectedIndex = comboCategory.FindString("PhysicalDisk")) == -1)
                if ((comboCategory.SelectedIndex = comboCategory.FindString("Fysisk disk")) == -1)
                    comboCategory.SelectedIndex = comboCategory.FindString("Dysk fizyczny");
            comboInstance.SelectedIndex = comboInstance.FindString("_Total");
        }

        //App exit event
        private void Application_ApplicationExit(object sender, EventArgs e)
        {
            trayIcon.Dispose();
        }

        //Handle menu exit
        private void MenuExit(object s, EventArgs e)
        {
            Application.Exit();
        }

        //Draw current readout on screen - here progress bar
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

                if (avg >= 0 && avg <= 100)
                {
                    progressCnt.Value = avg;
                    labelValue.Text = avg.ToString();
                }

                if (avg <= 1)
                {
                    Icon = Properties.Resources.new0;
                    trayIcon.Icon = Properties.Resources.new0;
                    timerIcon.Enabled = true;
                }
                else if (avg <= 5)
                {
                    Icon = Properties.Resources.new25;
                    trayIcon.Icon = Properties.Resources.new25;
                    timerIcon.Enabled = true;
                }
                else if (avg <= 15)
                {
                    Icon = Properties.Resources.new50;
                    trayIcon.Icon = Properties.Resources.new50;
                    timerIcon.Enabled = true;
                }
                else if (avg <= 35)
                {
                    Icon = Properties.Resources.new75;
                    trayIcon.Icon = Properties.Resources.new75;
                    timerIcon.Enabled = true;
                }
                else if (avg <= 100)
                {
                    Icon = Properties.Resources.new100;
                    trayIcon.Icon = Properties.Resources.new100;
                    timerIcon.Enabled = true;
                }
            }
            catch (Exception) { }
        }

        //Disable icon perodically to make it blink
        private void TimerIcon_Tick(object s, EventArgs e)
        {
            Icon = Properties.Resources.new0;
            trayIcon.Icon = Properties.Resources.new0;
            timerIcon.Enabled = false;
        }

        //Create counters list when category has been picked
        private void ComboCategory_SelectedIndexChanged(object s, EventArgs e)
        {
            pc = null;
            progressCnt.Value = 0;
            comboInstance.Items.Clear();
            comboInstance.SelectedIndex = -1;
            comboCounter.Items.Clear();
            comboCounter.SelectedIndex = -1;

            foreach (var instance in PerformanceCounterCategory.GetCategories()[comboCategory.SelectedIndex].GetInstanceNames())
                comboInstance.Items.Add(instance);

            if (comboInstance.Items.Count > 0)
            {
                if ((comboInstance.SelectedIndex = comboInstance.FindString("_Total")) == -1)
                    comboInstance.SelectedIndex = 0;

                foreach (var counter in PerformanceCounterCategory.GetCategories()[comboCategory.SelectedIndex].GetCounters(comboInstance.SelectedItem.ToString()))
                    comboCounter.Items.Add(counter.CounterName);

                if ((comboCounter.SelectedIndex = comboCounter.FindString("% Disk Time")) == -1)
                    if ((comboCounter.SelectedIndex = comboCounter.FindString("% Disktid")) == -1)
                        if ((comboCounter.SelectedIndex = comboCounter.FindString("% Czas dysku")) == -1)
                            comboCounter.SelectedIndex = 0;
            }
            else
            {
                comboCategory.SelectedIndex = 0;
            }
        }

        private void ComboInstance_SelectedIndexChanged(object s, EventArgs e)
        {
            //createCounter();
            //textCntDesc.Text = "lalamido";
        }

        private void ComboCounter_SelectedIndexChanged(object s, EventArgs e)
        {
            CreateCounter();
            textCntDesc.Text = pc.CounterHelp;
        }

        //Create actual counter
        private void CreateCounter()
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
        private void TimerMinimize_Tick(object s, EventArgs e)
        {
            WindowState = FormWindowState.Minimized;
            Hide();
            timerMinimize.Enabled = false;
            timerMinimize.Dispose();
        }

        private void TrayIcon_MouseDoubleClick(object s, EventArgs e)
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