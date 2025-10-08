using System.Collections.Generic;
using System.Linq;
using Rise.Radio;

public class ConsoleCmdRadioStatus : ConsoleCmdAbstract
{
    public override string[] getCommands()
    {
        return new string[] { "radio_status", "rstatus" };
    }

    public override string getDescription()
    {
        return "Get information about active radios and current track. Usage: radio_status or rstatus";
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

            // Get current track info
            string trackInfo = radioManager.GetCurrentTrackInfo();
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"Track: {trackInfo}");

            // Get remaining time
            float remainingTime = radioManager.GetCurrentTrackRemainingTime();
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"Time remaining: {remainingTime:F1} seconds");

            // Count active radios (use reflection to access private field)
            var radioSourcesField = typeof(RadioManager).GetField("radioSources", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            if (radioSourcesField != null)
            {
                var radioSources = radioSourcesField.GetValue(radioManager) as System.Collections.Generic.List<RadioSource>;
                if (radioSources != null)
                {
                    int totalRadios = radioSources.Count;
                    int activeRadios = radioSources.Count(r => r.IsOn);
                    
                    SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"Radios: {activeRadios} active / {totalRadios} total");
                    
                    // List active radios
                    if (activeRadios > 0)
                    {
                        SingletonMonoBehaviour<SdtdConsole>.Instance.Output("Active radios:");
                        foreach (var radio in radioSources.Where(r => r.IsOn))
                        {
                            string radioType = radio.GetType().Name.Replace("RadioSource", "");
                            bool isPlaying = radio.IsPlaying();
                            SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"  - {radioType} {radio.Name}: {(isPlaying ? "Playing" : "On but not playing")}");
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            SingletonMonoBehaviour<SdtdConsole>.Instance.Output($"Error getting radio status: {e.Message}");
        }
    }
}