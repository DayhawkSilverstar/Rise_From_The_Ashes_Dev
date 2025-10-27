using System.Reflection;
using UnityEngine;
using RiseFromTheAshes.Harmony; // Add using directive for MainMenuMusicPatch

namespace Harmony
{
    public class RiseFromTheAshes : IModApi
    {
        public void InitMod(Mod _modInstance)
        {            

            var harmony = new HarmonyLib.Harmony(GetType().ToString());
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            
            // Load menu music if GameManager is already initialized
            if (GameManager.Instance != null)
            {
                LoadMenuMusic();
            }
            
            // Register for game events to ensure music is applied after loading
            ModEvents.GameStartDone.RegisterHandler(OnGameStartDone);
        }        

        private void OnGameStartDone(ref ModEvents.SGameStartDoneData data)
        {
            // Apply music after game is fully loaded
            LoadMenuMusic();
            
            // Initialize RadioManager and its updater for track progression
            InitializeRadioSystem();
        }

        private void LoadMenuMusic()
        {            
            
            // Use the shared method from MainMenuMusicPatch
            MainMenuMusicPatch.LoadAndApplyCustomMusic();
        }

        private void InitializeRadioSystem()
        {
            try
            {                
                
                // Initialize RadioManager instance
                var radioManager = RadioManager.Instance;
                if (radioManager != null)
                {                    
                    
                    // Create RadioManagerUpdater component for track progression
                    CreateRadioManagerUpdater();
                }
                else
                {
                    Log.Out("[RFTA] Failed to create RadioManager instance");
                }
            }
            catch (System.Exception e)
            {
                Log.Out($"[RFTA] Error initializing Radio System: {e.Message}");
                Log.Out($"[RFTA] Stack trace: {e.StackTrace}");
            }
        }

        private void CreateRadioManagerUpdater()
        {
            try
            {                
                
                // Create GameObject for RadioManager updates
                GameObject radioUpdaterObject = new GameObject("RadioManagerUpdater");
                radioUpdaterObject.AddComponent<RadioManagerUpdater>();
                UnityEngine.Object.DontDestroyOnLoad(radioUpdaterObject);
                                
            }
            catch (System.Exception e)
            {
                Log.Out($"[RFTA] Error creating RadioManagerUpdater: {e.Message}");
            }
        }
    }

    /// <summary>
    /// MonoBehaviour component to ensure RadioManager gets updated for track progression
    /// This is critical for automatic track advancement when songs finish playing
    /// </summary>
    public class RadioManagerUpdater : MonoBehaviour
    {
        private RadioManager radioManager;
        private static RadioManagerUpdater instance;
        private float lastUpdateTime = 0f;
        private const float UPDATE_INTERVAL = 1f; // Update every second
        
        void Start()
        {
            try
            {
                instance = this;
                radioManager = RadioManager.Instance;
                Log.Out("[RFTA] RadioManagerUpdater started - monitoring for track progression");
                
                // Log that track progression is now enabled
                Log.Out("[RFTA] ✅ TRACK PROGRESSION ENABLED - Radios will automatically advance tracks");
            }
            catch (System.Exception e)
            {
                Log.Out($"[RFTA] Error starting RadioManagerUpdater: {e.Message}");
            }
        }
        
        void Update()
        {
            // Update RadioManager for track progression (throttled to once per second)
            try
            {
                if (Time.time - lastUpdateTime >= UPDATE_INTERVAL)
                {
                    lastUpdateTime = Time.time;
                    
                    if (radioManager != null)
                    {
                        radioManager.Update();
                    }
                }
            }
            catch (System.Exception e)
            {
                // Only log critical errors to avoid spam
                if (!(e is System.NullReferenceException))
                {
                    Log.Out($"[RFTA] RadioManager update error: {e.Message}");
                }
            }
        }
        
        void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
            Log.Out("[RFTA] RadioManagerUpdater destroyed - track progression disabled");
        }
        
        /// <summary>
        /// Gets the current instance of the RadioManagerUpdater
        /// </summary>
        public static RadioManagerUpdater Instance
        {
            get { return instance; }
        }
    }
}