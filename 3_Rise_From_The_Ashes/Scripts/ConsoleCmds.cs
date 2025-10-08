using System;
using System.Collections.Generic;

public class ConsoleCmd_RFAutoDrive : ConsoleCmdAbstract
{
    public override string getDescription() { return "Toggle/set RFA auto-drive for your current vehicle."; }
    public override string getHelp() { return "Usage: rfa.autodrive [0|1|toggle]"; }
    public override string[] getCommands() { return new[] { "rfa.autodrive" }; }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            // Resolve primary/local player WITHOUT touching _senderInfo (struct / no ExecutingPlayer)
            EntityPlayerLocal player = null;
            if (GameManager.Instance != null && GameManager.Instance.World != null)
                player = GameManager.Instance.World.GetPrimaryPlayer();

            if (player == null)
            {
                SdtdConsole.Instance.Output("No local/primary player.");
                return;
            }

            var vehicle = player.AttachedToEntity as Entity;
            if (vehicle == null)
            {
                SdtdConsole.Instance.Output("Player not in a vehicle.");
                return;
            }

            bool hasParam = _params != null && _params.Count > 0;
            if (!hasParam || string.Equals(_params[0], "toggle", StringComparison.OrdinalIgnoreCase))
            {
                MinEventActionAutoDrive.ToggleAutoDrive(vehicle.entityId);
                SdtdConsole.Instance.Output("rfa_autodrive: toggled");
                return;
            }

            bool on = (_params[0] == "1") || _params[0].Equals("on", StringComparison.OrdinalIgnoreCase);
            MinEventActionAutoDrive.SetAutoDrive(vehicle.entityId, on);
            SdtdConsole.Instance.Output("rfa_autodrive=" + (on ? "1" : "0"));
        }
        catch (Exception e)
        {
            Log.Error("[RFA] rfa.autodrive error: " + e);
        }
    }
}

public class ConsoleCmd_RFRoadFollow : ConsoleCmdAbstract
{
    public override string getDescription() { return "Toggle/set RFA road-follow stub for your current vehicle."; }
    public override string getHelp() { return "Usage: rfa.roadfollow [0|1|toggle]"; }
    public override string[] getCommands() { return new[] { "rfa.roadfollow" }; }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            EntityPlayerLocal player = null;
            if (GameManager.Instance != null && GameManager.Instance.World != null)
                player = GameManager.Instance.World.GetPrimaryPlayer();

            if (player == null)
            {
                SdtdConsole.Instance.Output("No local/primary player.");
                return;
            }

            var vehicle = player.AttachedToEntity as Entity;
            if (vehicle == null)
            {
                SdtdConsole.Instance.Output("Player not in a vehicle.");
                return;
            }

            bool hasParam = _params != null && _params.Count > 0;
            if (!hasParam || string.Equals(_params[0], "toggle", StringComparison.OrdinalIgnoreCase))
            {
                MinEventActionAutoDrive.ToggleRoadFollow(vehicle.entityId);
                SdtdConsole.Instance.Output("rfa_roadfollow: toggled");
                return;
            }

            bool on = (_params[0] == "1") || _params[0].Equals("on", StringComparison.OrdinalIgnoreCase);
            MinEventActionAutoDrive.SetRoadFollow(vehicle.entityId, on);
            SdtdConsole.Instance.Output("rfa_roadfollow=" + (on ? "1" : "0"));
        }
        catch (Exception e)
        {
            Log.Error("[RFA] rfa.roadfollow error: " + e);
        }
    }
}

