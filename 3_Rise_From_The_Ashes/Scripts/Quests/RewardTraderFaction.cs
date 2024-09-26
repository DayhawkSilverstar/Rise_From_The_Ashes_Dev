using DynamicMusic;
using MusicUtils.Enums;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;


// For some reason this doesn't keep the faction points after stopping the game.
// Not going to use it atm.
public class RewardTraderFaction : BaseReward
{
    Vector3 boundSize = new Vector3(4, 4, 4);

    public RewardTraderFaction()
    {
        HiddenReward = true;
    }

    public override void SetupReward()
    {
        HiddenReward = true;
    }

    public override void GiveReward(EntityPlayer player)
    {
        Log.Out("RewardTraderFaction:GiveReward");
        var value = StringParsers.ParseSInt32(Value, 0, -1, NumberStyles.Integer);
        //byte traderID = player.
        var npcs = new List<Entity>();
        EntityNPC trader = null;
        GameManager.Instance.World.GetEntitiesInBounds(typeof(EntityNPC), new Bounds(GameManager.Instance.World.GetPrimaryPlayer().position, boundSize), npcs);
        if (npcs.Count > 0)
        {
            foreach (var npc in npcs)
            {
                if (npc is EntityNPC)
                {
                    trader = npc as EntityNPC;
                    switch (trader.npcID)
                    {
                        case "traitorjoel":
                        case "traderjen":
                        case "traderbob":
                        case "traderhugh":
                        case "traderrekt":
                            Log.Out("Trader Found: " + trader.npcID);
                            Log.Out("Trader Faction: " + trader.factionId);
                            Log.Out("Faction Value :" + player.QuestJournal.GetQuestFactionPoints(trader.factionId).ToString());
                            Log.Out("Adding Faction points for the quest :" + value.ToString());
                            player.QuestJournal.AddQuestFactionPoint(trader.factionId, value);
                            Log.Out("New Faction Value :" + player.QuestJournal.GetQuestFactionPoints(trader.factionId).ToString());
                            break;
                    }
                }
            }
        }
    }

    public override BaseReward Clone()
    {
        Log.Out("RewardTraderFaction:Clone");
        var rewardTraderFaction = new RewardTraderFaction();
        CopyValues(rewardTraderFaction);
        return rewardTraderFaction;
    }
}
