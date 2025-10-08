using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rise.Radio
{
    /// <summary>
    /// Simplified coordinator that works with game's audio system
    /// </summary>
    public class RadioCoordinator
    {
        private static RadioCoordinator _instance;
        private Dictionary<string, List<RadioSource>> trackToRadiosMap;
        private float lastGlobalSyncTime;
        
        public static RadioCoordinator Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new RadioCoordinator();
                return _instance;
            }
        }
        
        private RadioCoordinator()
        {
            trackToRadiosMap = new Dictionary<string, List<RadioSource>>();
            lastGlobalSyncTime = 0f;
        }
        
        public void RegisterRadioForTrack(string trackName, RadioSource radio)
        {
            if (!trackToRadiosMap.ContainsKey(trackName))
            {
                trackToRadiosMap[trackName] = new List<RadioSource>();
            }
            
            if (!trackToRadiosMap[trackName].Contains(radio))
            {
                trackToRadiosMap[trackName].Add(radio);
                Log.Out($"Registered radio {radio.Name} for track {trackName}. Total radios for this track: {trackToRadiosMap[trackName].Count}");
                RadioDebug.D("COORD", $"Register '{trackName}' radio={radio.Name} count={trackToRadiosMap[trackName].Count}");

                // If there are multiple radios for this track, trigger sync after a brief delay
                // This allows other radios to register before syncing
                if (trackToRadiosMap[trackName].Count > 1)
                {
                    Log.Out($"Multiple radios detected for {trackName}, scheduling delayed sync");
                    GameManager.Instance.StartCoroutine(DelayedSyncForTrack(trackName, 0.3f));
                }
            }
        }
        
        public void UnregisterRadioForTrack(string trackName, RadioSource radio)
        {
            if (trackToRadiosMap.ContainsKey(trackName))
            {
                trackToRadiosMap[trackName].Remove(radio);
                if (trackToRadiosMap[trackName].Count == 0)
                {
                    trackToRadiosMap.Remove(trackName);
                }
                Log.Out($"Unregistered radio {radio.Name} from track {trackName}");
                RadioDebug.D("COORD", $"Unregister '{trackName}' radio={radio.Name}");
            }
        }
        
        /// <summary>
        /// Delayed sync coroutine to allow multiple radios to register before syncing
        /// </summary>
        private System.Collections.IEnumerator DelayedSyncForTrack(string trackName, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            try
            {
                if (trackToRadiosMap.ContainsKey(trackName) && trackToRadiosMap[trackName].Count > 1)
                {
                    // Check how many are actually playing
                    var validRadios = trackToRadiosMap[trackName].Where(r => r != null && r.IsOn && r.IsParentValid()).ToList();
                    Log.Out($"Delayed sync for {trackName}: {validRadios.Count}/{trackToRadiosMap[trackName].Count} valid radios");
                    
                    if (validRadios.Count > 1)
                    {
                        RadioSource.SyncAudioSource(trackName);
                        Log.Out($"Performed delayed sync for {trackName} with {validRadios.Count} radios");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out($"Error in delayed sync for {trackName}: {e.Message}");
            }
        }
        
        // Use working synchronization approach
        public void SynchronizeAllTracks()
        {
            try
            {
                foreach (var kvp in trackToRadiosMap)
                {
                    string trackName = kvp.Key;
                    var radios = kvp.Value;
                    
                    // Remove invalid radios
                    radios.RemoveAll(r => r == null || !r.IsOn || !r.IsParentValid());
                    
                    if (radios.Count > 1)
                    {
                        RadioSource.SyncAudioSource(trackName);
                    }
                }
                
                lastGlobalSyncTime = Time.time;
            }
            catch (Exception e)
            {
                Log.Out($"Error in coordinator update: {e.Message}");
            }
        }
        
        // Cleanup method for orphaned audio sources
        public void CleanupOrphanedRadios()
        {
            try
            {
                List<string> tracksToRemove = new List<string>();
                
                foreach (var kvp in trackToRadiosMap)
                {
                    string trackName = kvp.Key;
                    var radios = kvp.Value;
                    
                    // Remove radios whose parent entities/blocks no longer exist
                    radios.RemoveAll(r => r == null || !r.IsParentValid());
                    
                    if (radios.Count == 0)
                    {
                        tracksToRemove.Add(trackName);
                    }
                }
                
                // Remove empty tracks
                foreach (string track in tracksToRemove)
                {
                    Log.Out($"Removing empty track: {track}");
                    trackToRadiosMap.Remove(track);
                }
                
                if (tracksToRemove.Count > 0)
                {
                    Log.Out($"Cleaned up {tracksToRemove.Count} orphaned radio tracks");
                }
            }
            catch (Exception e)
            {
                Log.Out($"Error cleaning up orphaned radios: {e.Message}");
            }
        }

        /// <summary>
        /// Gets the number of active radios for a specific track
        /// </summary>
        public int GetActiveRadioCount(string trackName)
        {
            if (!trackToRadiosMap.ContainsKey(trackName))
                return 0;
                
            return trackToRadiosMap[trackName].Count(r => r != null && r.IsOn && r.IsParentValid());
        }

        /// <summary>
        /// Gets all active tracks being played
        /// </summary>
        public List<string> GetActiveTracks()
        {
            var activeTracks = new List<string>();
            
            foreach (var kvp in trackToRadiosMap)
            {
                if (kvp.Value.Any(r => r != null && r.IsOn && r.IsParentValid()))
                {
                    activeTracks.Add(kvp.Key);
                }
            }
            
            return activeTracks;
        }
        
        /// <summary>
        /// Force sync a specific track immediately (for debugging/manual sync)
        /// </summary>
        public void ForceSyncTrack(string trackName)
        {
            try
            {
                if (trackToRadiosMap.ContainsKey(trackName))
                {
                    var validRadios = trackToRadiosMap[trackName].Where(r => r != null && r.IsOn && r.IsParentValid()).ToList();
                    Log.Out($"Force syncing {trackName} with {validRadios.Count} radios");
                    RadioDebug.D("COORD", $"ForceSync '{trackName}' count={validRadios.Count}");
                    
                    if (validRadios.Count > 1)
                    {
                        RadioSource.SyncAudioSource(trackName);
                        Log.Out($"Force sync completed for {trackName}");
                    }
                    else
                    {
                        Log.Out($"Not enough valid radios for {trackName} sync ({validRadios.Count})");
                    }
                }
                else
                {
                    Log.Out($"Track {trackName} not found in coordinator");
                }
            }
            catch (Exception e)
            {
                Log.Out($"Error in force sync for {trackName}: {e.Message}");
            }
        }
    }
}