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
        int.TryParse(ini.Read("numberOfCounters"), out int noOfCounters);
        if (noOfCounters < 1)
        {
            Counters.Add(new Counter(new CounterSettings { Number = 1 }));
            ini.Write("numberOfCounters", "1");
        }
        else
            for (int ix = 1; ix <= noOfCounters; ix++)
                CounterFromIni(ix);

        //'Remove' option is disabled by default. Enable if number of counters grows
        if (Counters.Count > 1)
            foreach (Counter c in Counters)
                c.TrayIcon.ContextMenuStrip.Items["MenuRemove"].Enabled = true;
    }

    public static void CounterFromIni(int CounterNumber)
    {
        Counters.Add(new Counter(new CounterSettings
        {
            //Get counter settings from INI file
            Number = Counters.Count + 1,
            CategoryName = ini.Read("categoryName" + CounterNumber),
            InstanceName = ini.Read("instanceName" + CounterNumber),
            CounterName = ini.Read("counterName" + CounterNumber),
            ColorR = ini.Read("ledColorR" + CounterNumber),
            ColorG = ini.Read("ledColorG" + CounterNumber),
            ColorB = ini.Read("ledColorB" + CounterNumber),
            Shape = ini.Read("ledShape" + CounterNumber),
            Blinker = ini.Read("ledBlinker" + CounterNumber),
            BlinkerType = ini.Read("ledBlinkerType" + CounterNumber),
            RefreshRate = ini.Read("refreshRate" + CounterNumber)
        }));
    }
}