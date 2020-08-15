using System.Collections.Generic;
using System.Windows.Forms;

//Main couter class
public class COUNTERS : ApplicationContext
{
    //Program entry point
    static void Main() => Application.Run(new COUNTERS());

    //List of counters
    public static List<Counter> Counters;

    //Settings in ini file
    public static readonly IniFile ini = new IniFile();

    //Main object initialize
    public COUNTERS()
    {
        //Create counters list first
        Counters = new List<Counter>();

        //Check if there are any counters saved in INI file
        //If not, create default one
        if (int.TryParse(ini.Read("numberOfCounters"), out int noOfCounters))
            for (int ix = 1; ix <= noOfCounters; ix++)
                CounterFromIni(ix);
        else
        {
            Counters.Add(new Counter(new CounterSettings { Number = 1 }));
            ini.Write("numberOfCounters", "1");
        }

        //'Remove' option is disabled by default.Enable if number of counters grows
        if (Counters.Count > 1)
            foreach (Counter c in Counters)
                c.TrayIcon.ContextMenuStrip.Items["MenuRemove"].Enabled = true;
    }

    public static void CounterFromIni(int CounterNumber)
    {
        //Get counter settings from INI file
        string sn = "Counter" + (Counters.Count + 1);
        CounterSettings cs = new CounterSettings()
        {
            Number = Counters.Count + 1,
            CategoryName = ini.Read(nameof(cs.CategoryName) + CounterNumber, sn),
            InstanceName = ini.Read(nameof(cs.InstanceName) + CounterNumber, sn),
            CounterName = ini.Read(nameof(cs.CounterName) + CounterNumber, sn),
            ColorR = ini.Read(nameof(cs.ColorR) + CounterNumber, sn),
            ColorG = ini.Read(nameof(cs.ColorG) + CounterNumber, sn),
            ColorB = ini.Read(nameof(cs.ColorB) + CounterNumber, sn),
            Shape = ini.Read(nameof(cs.Shape) + CounterNumber, sn),
            Blinker = ini.Read(nameof(cs.Blinker) + CounterNumber, sn),
            BlinkerType = ini.Read(nameof(cs.BlinkerType) + CounterNumber, sn),
            RefreshRate = ini.Read(nameof(cs.RefreshRate) + CounterNumber, sn)
        };

        Counters.Add(new Counter(cs));
    }
}