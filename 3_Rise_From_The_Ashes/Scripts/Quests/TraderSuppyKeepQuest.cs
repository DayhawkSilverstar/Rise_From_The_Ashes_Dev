using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TraderSuppyKeepQuest : TraderSupplyQuest
{
    public TraderSuppyKeepQuest()
    {
        KeepItems = true;
    }

    public override BaseObjective Clone()
    {
        TraderSuppyKeepQuest objectiveFetchKeep = new TraderSuppyKeepQuest();
        CopyValues(objectiveFetchKeep);
        objectiveFetchKeep.KeepItems = KeepItems;
        return objectiveFetchKeep;
    }
}
