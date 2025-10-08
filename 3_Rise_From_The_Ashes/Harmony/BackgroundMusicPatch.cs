using System;
using HarmonyLib;
using UnityEngine;

namespace RiseFromTheAshes.Harmony
{
    // Additional patches to ensure background music system works correctly with our custom music
    
    // Patch to handle when BackgroundMusicMono tries to play music
    [HarmonyPatch(typeof(BackgroundMusicMono))]
    [HarmonyPatch("Play")]
    public class BackgroundMusicPlayPatch
    {
        private static void Prefix(BackgroundMusicMono __instance)
        {
            try
            {
                // Ensure our custom music is set in GameManager before playing
                if (MainMenuMusicPatch.CustomMenuMusic != null && GameManager.Instance != null)
                {
                    Log.Out("[RFTA] BackgroundMusicMono.Play called - ensuring custom music is set");
                    GameManager.Instance.BackgroundMusicClip = MainMenuMusicPatch.CustomMenuMusic;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[RFTA] Exception in BackgroundMusicMono.Play patch: " + ex.Message);
            }
        }
    }

    // Patch to handle BackgroundMusicMono Update method
    [HarmonyPatch(typeof(BackgroundMusicMono))]
    [HarmonyPatch("Update")]
    public class BackgroundMusicUpdatePatch
    {
        private static bool hasLoggedOnce = false;
        
        private static void Prefix(BackgroundMusicMono __instance)
        {
            try
            {
                // Only log once to avoid spam, but ensure our music is always set
                if (!hasLoggedOnce && MainMenuMusicPatch.CustomMenuMusic != null)
                {
                    Log.Out("[RFTA] BackgroundMusicMono.Update - custom music monitoring active");
                    hasLoggedOnce = true;
                }

                // Continuously ensure our custom music is set (lightweight check)
                if (MainMenuMusicPatch.CustomMenuMusic != null && 
                    GameManager.Instance != null && 
                    GameManager.Instance.BackgroundMusicClip != MainMenuMusicPatch.CustomMenuMusic)
                {
                    GameManager.Instance.BackgroundMusicClip = MainMenuMusicPatch.CustomMenuMusic;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[RFTA] Exception in BackgroundMusicMono.Update patch: " + ex.Message);
            }
        }
    }

    // Patch XUiC_MainMenu.OnClose to prevent music from being reset when menu closes
    [HarmonyPatch(typeof(XUiC_MainMenu))]
    [HarmonyPatch("OnClose")]
    public class MainMenuClosePatch
    {
        private static void Postfix(XUiC_MainMenu __instance)
        {
            try
            {
                Log.Out("[RFTA] XUiC_MainMenu.OnClose - maintaining custom music");
                
                // Reapply our custom music even when main menu closes
                if (MainMenuMusicPatch.CustomMenuMusic != null && GameManager.Instance != null)
                {
                    GameManager.Instance.BackgroundMusicClip = MainMenuMusicPatch.CustomMenuMusic;
                }
            }
            catch (Exception ex)
            {
                Log.Error("[RFTA] Exception in XUiC_MainMenu.OnClose patch: " + ex.Message);
            }
        }
    }

    // Patch GameManager.StartGame to ensure our music persists after starting a game
    [HarmonyPatch(typeof(GameManager))]
    [HarmonyPatch("StartGame")]
    public class GameManagerStartGamePatch
    {
        private static void Postfix(GameManager __instance)
        {
            try
            {
                Log.Out("[RFTA] GameManager.StartGame postfix - reapplying custom music after game start");
                MainMenuMusicPatch.LoadAndApplyCustomMusic();
            }
            catch (Exception ex)
            {
                Log.Error("[RFTA] Exception in GameManager.StartGame patch: " + ex.Message);
            }
        }
    }

    // Patch to handle potential music resets
    [HarmonyPatch(typeof(GameManager))]
    [HarmonyPatch("ResetGame")]
    public class GameManagerResetGamePatch
    {
        private static void Postfix(GameManager __instance)
        {
            try
            {
                Log.Out("[RFTA] GameManager.ResetGame postfix - reapplying custom music after reset");
                MainMenuMusicPatch.LoadAndApplyCustomMusic();
            }
            catch (Exception ex)
            {
                Log.Error("[RFTA] Exception in GameManager.ResetGame patch: " + ex.Message);
            }
        }
    }
}