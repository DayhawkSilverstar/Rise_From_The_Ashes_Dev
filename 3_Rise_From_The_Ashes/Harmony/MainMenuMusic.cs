using System;
using System.Collections.Generic;
using Audio;
using HarmonyLib;
using UnityEngine;

namespace RiseFromTheAshes.Harmony
{
    // This class replaces the main menu music with our custom track
    [HarmonyPatch(typeof(XUiC_MainMenu))]
    [HarmonyPatch("OnOpen")]
    public class MainMenuMusicPatch
    {
        // Audio resource path - adjust this to your actual file path in your mod folder
        private const string CustomMenuMusicPath = "#@modfolder(Rise_From_The_Ashes):Resources/RiseFromTheAshes.unity3d?MenuSong";
        
        // Keep a reference to our loaded audio clip
        public static AudioClip CustomMenuMusic = null;
        
        // Store the original music clip so we can restore it if needed
        public static AudioClip OriginalMenuMusic = null;
        
        // The prefix runs before the original method
        // We'll use this to replace the default menu music with our custom track
        private static void Prefix(XUiC_MainMenu __instance)
        {
            LoadAndApplyCustomMusic();
        }
        
        // Helper method to load and apply our custom music
        public static void LoadAndApplyCustomMusic()
        {
            // Only load the audio clip once and cache it
            if (CustomMenuMusic == null)
            {
                try
                {
                    CustomMenuMusic = DataLoader.LoadAsset<AudioClip>(CustomMenuMusicPath);

                    if (CustomMenuMusic == null)
                    {
                        Log.Warning("[RFTA] Failed to load custom music from path");
                        return; // Exit if we couldn't load the music
                    }
                }
                catch (Exception ex)
                {
                    Log.Error("[RFTA] Exception when loading custom menu music: " + ex.Message);
                    return; // Exit if we encountered an exception
                }
            }
            
            // Only proceed if we have a valid audio clip
            if (CustomMenuMusic != null && GameManager.Instance != null)
            {
                try
                {
                    // Store the original clip for restoration if needed (only once)
                    if (OriginalMenuMusic == null)
                    {
                        OriginalMenuMusic = GameManager.Instance.BackgroundMusicClip;
                    }
                    
                    // Replace the game's background music with our custom track
                    GameManager.Instance.BackgroundMusicClip = CustomMenuMusic;
                                       
                }
                catch (Exception ex)
                {
                    Log.Error("[RFTA] Exception when applying custom menu music: " + ex.Message);
                }
            }
        }
    }
    
    // This patch ensures our custom music is loaded at game startup
    [HarmonyPatch(typeof(GameManager))]
    [HarmonyPatch("Awake")]
    public class GameManagerAwakePatch
    {
        private static void Postfix(GameManager __instance)
        {
            try
            {
                // Load our custom music
                MainMenuMusicPatch.LoadAndApplyCustomMusic();
            }
            catch (Exception ex)
            {
                Log.Warning("[RFTA] Error in GameManagerAwakePatch: " + ex.Message);
            }
        }
    }
}
