using System.Collections.Generic;

public class ConsoleCmdRadioForceStop : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "radio_force_stop", "rstop" };
    }

    public override string getDescription()
    {
        return "Force stop all radios and clear any overlapping audio. Usage: radio_force_stop or rstop";
    }

    public override void Execute(List<string> _params, CommandSenderInfo _senderInfo)
    {
        try
        {
            // Get the RadioManager instance
            var radioManager = RadioManager.Instance;
            if (radioManager == null)
            {
                SingletonMonoBehaviour<SdtdConsole>.Instance.Output("RadioManager instance not found");
                return;
            }

            // Get current status before stopping
            string statusBefore = radioManager.GetCurrentTrackInfo();
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"Before: {statusBefore}");

            // Force stop all radios
            radioManager.ForceStopAllRadios();

            SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Force stopped all radios and cleared overlapping audio");
            
            // Wait a moment and get new status
            System.Threading.Thread.Sleep(100);
            string statusAfter = radioManager.GetCurrentTrackInfo();
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"After: {statusAfter}");
        }
        catch (System.Exception e)
        {
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"Error force stopping radios: {e.Message}");
        }
    }
}