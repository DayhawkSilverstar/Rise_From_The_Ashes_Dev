using System;
using System.Collections.Generic;

public class DynamicEventManager
{
    static DynamicEventManager _instance;
    Dictionary<ulong, EventBundle> _actions = new Dictionary<ulong, EventBundle>();
    protected List<EntityPlayer> targets = new List<EntityPlayer>();
    bool printOnce = false;

    private DynamicEventManager()
    {
        // If its part of the unit test then return.
        if (GameManager.Instance == null & ConnectionManager.Instance == null)
        {
            return;
        }

        if (GameManager.IsDedicatedServer)
        {
            Log.Out($"DynamicEventManager Dedicated Server");
        }
        else if (ConnectionManager.Instance.IsSinglePlayer)
        {
            Log.Out($"DynamicEventManager Singleplayer Server");
        }

        LoadEvents();
    }

    public static DynamicEventManager Instance 
    {
        get 
        { 
            if (_instance == null)  
                _instance = new DynamicEventManager();

            return _instance; 
        }
    }
    
    public void Update()
    {
        if (GameManager.Instance.World != null)
        {
            {
                if (GameManager.Instance.World.worldTime > 0)
                {
                    if (!printOnce)
                    {
                        PrintEntities();
                        printOnce = true;
                    }

                   // Log.Out($"World Time : " + GameManager.Instance.World.worldTime.ToString());
                    if (!GameManager.Instance.IsPaused())
                    {
                        CheckForEvents();
                    }
                }
            }
        }
    }

    private void LoadEvents()
    {        
      
    }

    private void PrintEntities()
    {
        Log.Out("Printing Entities");                  
    }

    public ulong GetTicksFromDate(int day, int hour, int min)
    {
        ulong ticks = 0;
        day -= 1;

        ticks += Convert.ToUInt64(day * 24000);
        ticks += Convert.ToUInt64(hour * 1000);
        ticks += Convert.ToUInt64((min * 1.6666666666666666666666666)*10);

        return ticks;
    }

    public string GetDateFromTicks(ulong  ticks) 
    {
        ulong tempTicks = ticks;
        ulong days = (tempTicks / 24000) + 1;
        tempTicks -= (days - 1) * 24000;
        ulong hours = tempTicks / 1000;
        tempTicks -= hours * 1000;
        ulong mins = Convert.ToUInt64((tempTicks * 0.6) / 10);

        return string.Format("{0} {1}:{2}", days, hours, mins);
    }

    private void CheckForEvents()
    {        
        targets.Clear();
        GameManager.Instance.World.Players.list.CopyTo(targets);
        List<ulong> removeList = new List<ulong>();
        foreach (var action in _actions)
        {
            if (action.Key <= GameManager.Instance.World.worldTime)
            {
                EventBundle eventBundle = action.Value as EventBundle;                
                removeList.Add(action.Key);               
            }
        }

        foreach (ulong key in removeList)
        {
            Log.Out($"Removed : " + key.ToString());
            _actions.Remove(key);
        }
    }
}

