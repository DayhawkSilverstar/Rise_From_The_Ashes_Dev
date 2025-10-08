using System;
using UnityEngine;
using Rise.Radio;

namespace Rise.Radio
{
    /// <summary>
    /// Diagnostics-only updater for radio system. Auto-advancement is handled solely by RadioManager.
    /// </summary>
    public class RadioManagerUpdater : MonoBehaviour
    {
        private static RadioManagerUpdater _instance;
        
        // Monitoring settings
        private float lastCheckTime = 0f;
        private const float CHECK_INTERVAL = 5f; // Slower checks for diagnostics
        
        // Component lifecycle
        private bool isActive = false;
        private float startTime = 0f;

        public static RadioManagerUpdater Instance
        {
            get
            {
                if (_instance == null)
                {
                    GameObject updaterObject = new GameObject("RadioManagerUpdater");
                    _instance = updaterObject.AddComponent<RadioManagerUpdater>();
                    DontDestroyOnLoad(updaterObject);
                    Log.Out("[RFTA] RadioManagerUpdater GameObject created and set to DontDestroyOnLoad");
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                startTime = Time.time;
                Log.Out("[RFTA] RadioManagerUpdater.Awake() - Component initialized");
            }
            else if (_instance != this)
            {
                Log.Out("[RFTA] RadioManagerUpdater.Awake() - Duplicate instance destroyed");
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            try
            {
                isActive = true;
                Log.Out("[RFTA] RadioManagerUpdater.Start() - Diagnostics monitoring started");
                Log.Out($"[RFTA] RadioManagerUpdater will check radio status every {CHECK_INTERVAL} seconds");
            }
            catch (Exception e)
            {
                Log.Out($"[RFTA] Error in RadioManagerUpdater.Start(): {e.Message}");
            }
        }

        private void Update()
        {
            try
            {
                if (!isActive) return;

                // Diagnostics checks at regular intervals
                if (Time.time - lastCheckTime >= CHECK_INTERVAL)
                {
                    lastCheckTime = Time.time;
                    LogRadioDiagnostics();
                }
            }
            catch (Exception e)
            {
                Log.Out($"[RFTA] Error in RadioManagerUpdater.Update(): {e.Message}");
            }
        }

        /// <summary>
        /// Logs current radio status for diagnostics only
        /// </summary>
        private void LogRadioDiagnostics()
        {
            try
            {
                string currentTrack = RadioPlaylistManager.Instance.CurrentTrackName;
                string info = RadioManager.Instance.GetCurrentTrackInfo();
                Log.Out($"[RFTA] Diagnostics: {info}");
                if (!string.IsNullOrEmpty(currentTrack))
                {
                    float remaining = RadioManager.Instance.GetCurrentTrackRemainingTime();
                    Log.Out($"[RFTA] Track '{currentTrack}' approx. remaining: {remaining:F1}s");
                }
            }
            catch (Exception e)
            {
                Log.Out($"[RFTA] Error logging diagnostics: {e.Message}");
            }
        }

        /// <summary>
        /// Gets a list of currently active radios - disabled to avoid driving advancement here
        /// </summary>
        private System.Collections.Generic.List<RadioSource> GetActiveRadiosList()
        {
            // Intentionally return empty to avoid duplicate control
            return new System.Collections.Generic.List<RadioSource>();
        }

        /// <summary>
        /// Enables or disables the diagnostics monitoring
        /// </summary>
        public void SetActive(bool active)
        {
            isActive = active;
            string status = active ? "ENABLED" : "DISABLED";
            Log.Out($"[RFTA] RadioManagerUpdater diagnostics {status}");
        }

        /// <summary>
        /// Gets the current status of the updater
        /// </summary>
        public bool IsActive()
        {
            return isActive;
        }

        /// <summary>
        /// Provides diagnostic information about the updater
        /// </summary>
        public string GetDiagnostics()
        {
            try
            {
                float uptime = Time.time - startTime;
                string currentTrack = RadioPlaylistManager.Instance.CurrentTrackName;
                return $"RadioManagerUpdater Status:\n" +
                       $"  Active: {isActive}\n" +
                       $"  Uptime: {uptime:F1}s\n" +
                       $"  Current Track: '{currentTrack}'\n" +
                       $"  Check Interval: {CHECK_INTERVAL}s\n" +
                       $"  Last Check: {Time.time - lastCheckTime:F1}s ago";
            }
            catch (Exception e)
            {
                return $"Error getting diagnostics: {e.Message}";
            }
        }

        private void OnDestroy()
        {
            Log.Out("[RFTA] RadioManagerUpdater.OnDestroy() - Component destroyed");
            if (_instance == this)
            {
                _instance = null;
            }
        }

        /// <summary>
        /// Static method to create and initialize the RadioManagerUpdater
        /// </summary>
        public static void Initialize()
        {
            try
            {
                Log.Out("[RFTA] Initializing RadioManagerUpdater (diagnostics only)...");
                var updater = RadioManagerUpdater.Instance;
                updater.SetActive(true);
                Log.Out("[RFTA] RadioManagerUpdater initialized successfully");
            }
            catch (Exception e)
            {
                Log.Out($"[RFTA] Error initializing RadioManagerUpdater: {e.Message}");
            }
        }
    }
}