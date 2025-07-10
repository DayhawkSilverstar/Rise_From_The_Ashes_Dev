using System.Reflection;
using UnityEngine;

namespace Harmony
{
    public class RiseFromTheAshes : IModApi
    {
        public void InitMod(Mod _modInstance)
        {
            Log.Out(" Loading Patch: " + GetType());

            var harmony = new HarmonyLib.Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
        }        

        private void LoadMenuMusic()
        {
            Log.Out("Loading menu music");
            const string UriMenuMusic = "#@modfolder(Rise_From_The_Ashes):Resources/RiseFromTheAshes.unity3d?Rise_from_the_Ashes";
            AudioClip audioClip = DataLoader.LoadAsset<AudioClip>(UriMenuMusic);

            if (audioClip != null)
            {
                GameManager.Instance.BackgroundMusicClip = audioClip;
            }
            else
            {
                Log.Warning("Could not load menu music file: " + UriMenuMusic);
            }
        }
    }
}