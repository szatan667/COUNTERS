using System.Collections.Generic;
using System.Windows.Forms;

//Main couter class
public class COUNTERS : ApplicationContext
{
    //Program entry point
    static void Main()
    {
        Application.Run(new COUNTERS());
    }

    //List of counters
    public static List<Counter> counters;

    //Settings in ini file
    public static readonly IniFile ini = new IniFile();

    //Main object initialize
    public COUNTERS()
    {
        //Create counters list first
        counters = new List<Counter>();

        //Check if there are any counters saved in INI file
        //If not, create default one
        int.TryParse(ini.Read("numberOfCounters"), out int noOfCounters);
        if (noOfCounters < 1)
        {
            counters.Add(new Counter(new Counter.CounterSettings { Number = 1 }));
            ini.Write("numberOfCounters", "1");
        }
        else
            for (int ix = 1; ix <= noOfCounters; ix++)
                counters.Add(new Counter(new Counter.CounterSettings
                {
                    //Get counter settings from INI file
                    Number = ix,
                    CategoryName = ini.Read("categoryName" + ix),
                    InstanceName = ini.Read("instanceName" + ix),
                    CounterName = ini.Read("counterName" + ix),
                    ColorR = ini.Read("ledColorR" + ix),
                    ColorG = ini.Read("ledColorG" + ix),
                    ColorB = ini.Read("ledColorB" + ix),
                    Shape = ini.Read("ledShape" + ix)
                }));
    }
}