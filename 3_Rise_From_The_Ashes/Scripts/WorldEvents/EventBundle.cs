using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class EventBundle
{
    public IMinEventAction EventAction { get; set; }
    public MinEventParams Params { get; set; }

    public ulong EventTime { get; set; }    
}

