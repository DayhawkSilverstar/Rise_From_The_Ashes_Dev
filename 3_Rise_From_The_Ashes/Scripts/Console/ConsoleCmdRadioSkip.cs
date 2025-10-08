using System.Collections.Generic;

public class ConsoleCmdRadioSkip : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "radio_skip", "rskip" };
    }

    public override string getDescription()
    {
        return "Skip to the next track on all active radios. Usage: radio_skip or rskip";
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

            // Get current track info before skipping
            string currentInfo = radioManager.GetCurrentTrackInfo();
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"Current: {currentInfo}");

            // Skip to next track
            radioManager.SkipToNextTrack();

            // Wait a moment and get new track info
            System.Threading.Thread.Sleep(100);
            string newInfo = radioManager.GetCurrentTrackInfo();
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"Skipped to: {newInfo}");
            
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Successfully skipped to next radio track");
        }
        catch (System.Exception e)
        {
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"Error skipping radio track: {e.Message}");
        }
    }
}