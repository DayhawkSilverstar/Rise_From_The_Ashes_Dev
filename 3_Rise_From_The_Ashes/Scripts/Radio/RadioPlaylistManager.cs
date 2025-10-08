using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Rise.Radio
{
    /// <summary>
    /// Handles playlist management and track advancement logic
    /// </summary>
    public class RadioPlaylistManager
    {
        private static RadioPlaylistManager _instance;
        private static System.Random rng = new System.Random();
        
        private List<Track> currentPlaylist = new List<Track>();
        private int playlistPosition = 0;
        private string currentTrackName = "";
        
        // Track completion tracking
        private float lastTrackCheckTime = 0f;
        private const float TRACK_CHECK_INTERVAL = 3f;
        private float currentTrackStartTime = 0f;
        private float currentTrackLength = 0f;
        
        // Track advancement control
        private bool isAdvancingTrack = false;
        private const float ADVANCEMENT_TIMEOUT = 5f;
        private float lastTrackAdvancementTime = 0f;

        public static RadioPlaylistManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new RadioPlaylistManager();
                return _instance;
            }
        }

        private RadioPlaylistManager()
        {
        }

        /// <summary>
        /// Creates a playlist from available tracks for the current game day
        /// Uses dynamic track loading and category-based filtering
        /// </summary>
        public void CreatePlaylist(string category = null)
        {
            try
            {
                Log.Out($"[Playlist] Creating radio playlist{(category != null ? $" for category: {category}" : "")}...");
                RadioDebug.D("PLAYLIST", $"CreatePlaylist cat={(category ?? "<all>")}");
                
                if (!RadioTrackData.Instance.IsLoaded())
                {
                    Log.Out("[Playlist] Track data not loaded, loading now...");
                    RadioTrackData.Instance.LoadXmlRadioData();
                }

                int currentDay = GameManager.Instance.World.worldTime > 0 
                    ? GameUtils.WorldTimeToDays(GameManager.Instance.World.worldTime) 
                    : 0;

                Log.Out($"[Playlist] Current game day: {currentDay}");

                // Use the new dynamic track loading method
                var availableTracks = RadioTrackData.Instance.GetDynamicTracksForDay(currentDay, category);
                Log.Out($"[Playlist] AvailableTracks (filtered): {availableTracks.Count}");
                
                if (availableTracks.Count == 0)
                {
                    Log.Out("[Playlist] No tracks available for current day/category, trying broader search...");
                    
                    // Fallback: try without category filter
                    if (category != null)
                    {
                        availableTracks = RadioTrackData.Instance.GetDynamicTracksForDay(currentDay, null);
                        Log.Out($"[Playlist] AvailableTracks (no category): {availableTracks.Count}");
                    }
                    
                    // Ultimate fallback: use all tracks regardless of day
                    if (availableTracks.Count == 0)
                    {
                        Log.Out("[Playlist] Still no tracks found, using all available tracks");
                        availableTracks = RadioTrackData.Instance.GetAllTracks();
                        Log.Out($"[Playlist] AvailableTracks (all): {availableTracks.Count}");
                    }
                }

                if (availableTracks.Count == 0)
                {
                    Log.Out("[Playlist] No tracks available at all! Attempting to refresh track data...");
                    
                    // Try refreshing the track data
                    RadioTrackData.Instance.RefreshTrackData();
                    availableTracks = RadioTrackData.Instance.GetAllTracks();
                    Log.Out($"[Playlist] AvailableTracks after refresh: {availableTracks.Count}");
                    
                    if (availableTracks.Count == 0)
                    {
                        Log.Out("[Playlist] Still no tracks after refresh - playlist creation failed");
                        return;
                    }
                }

                // Create playlist with track validation
                currentPlaylist = CreateValidatedPlaylist(availableTracks);
                playlistPosition = 0;

                Log.Out($"[Playlist] Created playlist with {currentPlaylist.Count} validated tracks");
                
                // Log first few tracks for debugging
                for (int i = 0; i < Math.Min(10, currentPlaylist.Count); i++)
                {
                    Log.Out($"[Playlist] Track {i + 1}: {currentPlaylist[i].name} (file: {currentPlaylist[i].file}, category: {currentPlaylist[i].category})");
                }
                
                if (currentPlaylist.Count > 10)
                {
                    Log.Out($"[Playlist] ... and {currentPlaylist.Count - 10} more tracks");
                }
            }
            catch (Exception e)
            {
                Log.Out($"[Playlist] Error creating playlist: {e.Message}");
                Log.Out($"[Playlist] Stack trace: {e.StackTrace}");
            }
        }

        /// <summary>
        /// Creates a validated playlist, ensuring all tracks have valid audio references
        /// </summary>
        private List<Track> CreateValidatedPlaylist(List<Track> inputTracks)
        {
            var validatedTracks = new List<Track>();
            int skippedTracks = 0;
            
            foreach (var track in inputTracks)
            {
                if (IsTrackPlayable(track))
                {
                    validatedTracks.Add(track);
                }
                else
                {
                    Log.Out($"[Playlist] Skipping unplayable track: {track.name} (file: {track.file})");
                    skippedTracks++;
                }
            }
            
            Log.Out($"[Playlist] Validation summary: kept {validatedTracks.Count}, skipped {skippedTracks}");
            
            // Shuffle the validated playlist
            return ShufflePlaylist(validatedTracks);
        }

        /// <summary>
        /// Validates that a track can actually be played
        /// </summary>
        private bool IsTrackPlayable(Track track)
        {
            try
            {
                // Basic validation
                if (string.IsNullOrEmpty(track.name) || string.IsNullOrEmpty(track.file))
                {
                    return false;
                }
                
                // Check if audio data exists for this track
                if (Audio.Manager.audioData != null)
                {
                    // Direct match
                    if (Audio.Manager.audioData.ContainsKey(track.file))
                    {
                        return true;
                    }
                    
                    // Partial match search
                    var audioKeys = Audio.Manager.audioData.Keys;
                    foreach (string key in audioKeys)
                    {
                        if (key.Contains(track.file) || track.file.Contains(key))
                        {
                            return true;
                        }
                    }
                }
                
                // If we can't verify, assume it's playable (will be caught during actual playback)
                return true;
            }
            catch (Exception e)
            {
                Log.Out($"[Playlist] Error validating track playability for {track.name}: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a category-specific playlist (for future expansion)
        /// </summary>
        public void CreateCategoryPlaylist(string category)
        {
            Log.Out($"[Playlist] Creating category-specific playlist: {category}");
            CreatePlaylist(category);
        }

        /// <summary>
        /// Refreshes the current playlist (useful after track data updates)
        /// </summary>
        public void RefreshPlaylist()
        {
            try
            {
                Log.Out("[Playlist] Refreshing current playlist...");
                
                string currentCategory = null; // Could be expanded to remember category
                string previousTrack = currentTrackName;
                
                CreatePlaylist(currentCategory);
                
                // Try to maintain position if the same track exists in the new playlist
                if (!string.IsNullOrEmpty(previousTrack))
                {
                    int newPosition = currentPlaylist.FindIndex(t => t.name == previousTrack);
                    if (newPosition >= 0)
                    {
                        playlistPosition = newPosition;
                        Log.Out($"[Playlist] Maintained playlist position at track: {previousTrack}");
                    }
                    else
                    {
                        Log.Out($"[Playlist] Previous track {previousTrack} not found in refreshed playlist, starting from beginning");
                        playlistPosition = 0;
                    }
                }
                
                Log.Out("[Playlist] Playlist refresh completed");
            }
            catch (Exception e)
            {
                Log.Out($"[Playlist] Error refreshing playlist: {e.Message}");
            }
        }

        /// <summary>
        /// Gets the current track with runtime validation
        /// </summary>
        public string GetCurrentTrack()
        {
            if (currentPlaylist.Count == 0)
            {
                Log.Out("[Playlist] No playlist available, creating new playlist...");
                CreatePlaylist();
            }

            if (currentPlaylist.Count == 0)
            {
                Log.Out("[Playlist] Failed to create playlist - no tracks available");
                return "";
            }

            if (playlistPosition >= currentPlaylist.Count)
            {
                Log.Out("[Playlist] Playlist position exceeded bounds, wrapping to beginning");
                playlistPosition = 0;
            }

            var selectedTrack = currentPlaylist[playlistPosition];
            
            // Validate the selected track before returning
            if (!IsTrackPlayable(selectedTrack))
            {
                Log.Out($"[Playlist] Selected track {selectedTrack.name} is not playable, skipping...");
                
                // Try to find the next playable track
                string nextPlayableTrack = FindNextPlayableTrack();
                if (!string.IsNullOrEmpty(nextPlayableTrack))
                {
                    return nextPlayableTrack;
                }
                
                Log.Out("[Playlist] No playable tracks found in playlist");
                return "";
            }

            currentTrackName = selectedTrack.name;
            currentTrackStartTime = Time.time;
            currentTrackLength = 0f;

            Log.Out($"[Playlist] Selected track: {currentTrackName} (file: {selectedTrack.file}) @ position {playlistPosition + 1}/{currentPlaylist.Count}");
            RadioDebug.D("PLAYLIST", $"Current '{currentTrackName}' pos={playlistPosition+1}/{currentPlaylist.Count}");
            return currentTrackName;
        }

        /// <summary>
        /// Finds the next playable track in the playlist
        /// </summary>
        private string FindNextPlayableTrack()
        {
            int startPosition = playlistPosition;
            int attempts = 0;
            
            do
            {
                playlistPosition = (playlistPosition + 1) % currentPlaylist.Count;
                attempts++;
                
                if (IsTrackPlayable(currentPlaylist[playlistPosition]))
                {
                    var track = currentPlaylist[playlistPosition];
                    currentTrackName = track.name;
                    currentTrackStartTime = Time.time;
                    currentTrackLength = 0f;
                    
                    Log.Out($"[Playlist] Found next playable track: {track.name} (file: {track.file}) @ position {playlistPosition + 1}/{currentPlaylist.Count}");
                    return track.name;
                }
                else
                {
                    Log.Out($"[Playlist] Skipped non-playable track at position {playlistPosition + 1}: {currentPlaylist[playlistPosition].name}");
                }
                
            } while (playlistPosition != startPosition && attempts < currentPlaylist.Count);
            
            Log.Out("[Playlist] No playable tracks found in entire playlist");
            return "";
        }

        private List<Track> ShufflePlaylist(List<Track> tracks)
        {
            var shuffled = tracks.ToList();
            
            for (int i = shuffled.Count - 1; i > 0; i--)
            {
                int randomIndex = rng.Next(i + 1);
                Track temp = shuffled[i];
                shuffled[i] = shuffled[randomIndex];
                shuffled[randomIndex] = temp;
            }
            
            return shuffled;
        }

        /// <summary>
        /// Checks if the current track has completed and should advance
        /// </summary>
        public bool ShouldAdvanceTrack(List<RadioSource> activeRadios)
        {
            try
            {
                // Diagnostics header
                Log.Out("[Advance] ShouldAdvanceTrack START");
                Log.Out($"[Advance] PlaylistPosition={playlistPosition} of {currentPlaylist.Count}, Current='{currentTrackName}', StartTime={currentTrackStartTime:F2}, Length={currentTrackLength:F2}, IsAdvancing={isAdvancingTrack}, LastAdvanceTs={lastTrackAdvancementTime:F2}");
                Log.Out($"[Advance] ActiveRadios={activeRadios.Count}");
                for (int i = 0; i < activeRadios.Count; i++)
                {
                    var r = activeRadios[i];
                    string type = r is BlockRadioSource ? "Block" : (r is DroneRadioSource ? "Drone" : (r is EntityRadioSource ? "Entity" : "Unknown"));
                    string clip = r.AudioSourceObject != null && r.AudioSourceObject.clip != null ? r.AudioSourceObject.clip.name : "<null>";
                    float time = r.AudioSourceObject != null && r.AudioSourceObject.clip != null ? r.AudioSourceObject.time : -1f;
                    float len = r.AudioSourceObject != null && r.AudioSourceObject.clip != null ? r.AudioSourceObject.clip.length : -1f;
                    Log.Out($"[Advance]   Radio[{i}] Name={r.Name} Type={type} IsOn={r.IsOn} HasAS={(r.AudioSourceObject!=null)} Clip='{clip}' Playing={(r.AudioSourceObject!=null && r.AudioSourceObject.isPlaying)} Time={time:F2}/{len:F2}");
                }

                // Don't check if advancement is already in progress
                if (isAdvancingTrack)
                {
                    Log.Out("[Advance] Locked: advancement already in progress");
                    return false;
                }
                
                // Only check if we have active radios and a current track
                if (activeRadios.Count == 0)
                {
                    Log.Out("[Advance] No active radios; not advancing");
                    return false;
                }
                if (string.IsNullOrEmpty(currentTrackName))
                {
                    Log.Out("[Advance] No current track name; not advancing");
                    return false;
                }
                
                // Check playing status
                int radiosStillPlaying = 0;
                int totalRadiosWithAudio = 0;
                float shortestRemainingTime = float.MaxValue;
                float longestPlayTime = 0f;
                int radiosWithValidAudio = 0;
                
                foreach (var radio in activeRadios)
                {
                    if (radio.AudioSourceObject != null && radio.AudioSourceObject.clip != null)
                    {
                        totalRadiosWithAudio++;
                        radiosWithValidAudio++;
                        
                        // Update track length if we don't have it
                        UpdateCurrentTrackLength(radio.AudioSourceObject.clip.length);
                        
                        if (radio.AudioSourceObject.isPlaying)
                        {
                            radiosStillPlaying++;
                            float remaining = radio.AudioSourceObject.clip.length - radio.AudioSourceObject.time;
                            float playTime = radio.AudioSourceObject.time;
                            
                            if (remaining < shortestRemainingTime)
                            {
                                shortestRemainingTime = remaining;
                            }
                            
                            if (playTime > longestPlayTime)
                            {
                                longestPlayTime = playTime;
                            }
                        }
                    }
                    else if (radio.IsOn)
                    {
                        // Radio claims to be on but has no audio - may need to advance
                        totalRadiosWithAudio++; // Count it for decision making
                    }
                }
                
                Log.Out($"[Advance] RadiosStillPlaying={radiosStillPlaying}, RadiosWithValidAudio={radiosWithValidAudio}, TotalWithAudio={totalRadiosWithAudio}, ShortestRemaining={(shortestRemainingTime==float.MaxValue? -1f : shortestRemainingTime):F2}, LongestPlayTime={longestPlayTime:F2}");
                
                // Case 1: No radios have audio sources but claim to be on (audio system issue)
                if (radiosWithValidAudio == 0 && activeRadios.Count > 0)
                {
                    int radiosClaimingOn = activeRadios.Count(r => r.IsOn);
                    float elapsed = currentTrackStartTime > 0f ? Time.time - currentTrackStartTime : 0f;
                    Log.Out($"[Advance] Case1: ValidAudio=0, RadiosClaimingOn={radiosClaimingOn}, Elapsed={elapsed:F2}");
                    if (radiosClaimingOn > 0)
                    {
                        if (elapsed > 30f)
                        {
                            Log.Out("[Advance] DECISION: Advance (Case1: 30s elapsed without valid audio)");
                            return true;
                        }
                    }
                    else
                    {
                        Log.Out("[Advance] DECISION: Advance (Case1: no valid audio and none claiming on)");
                        return true;
                    }
                }
                
                // Case 2: No radios are playing anymore
                if (radiosStillPlaying == 0 && radiosWithValidAudio > 0)
                {
                    Log.Out("[Advance] DECISION: Advance (Case2: none still playing)");
                    return true;
                }
                
                // Case 3: Track is near the end (within 5 seconds instead of 3)
                if (shortestRemainingTime <= 5f && shortestRemainingTime != float.MaxValue)
                {
                    Log.Out($"[Advance] DECISION: Advance (Case3: near end, remaining={shortestRemainingTime:F2}s)");
                    return true;
                }
                
                // Case 4: Time-based check - track has been playing long enough
                if (currentTrackLength > 0f)
                {
                    if (longestPlayTime >= (currentTrackLength - 5f))
                    {
                        Log.Out($"[Advance] DECISION: Advance (Case4a: most of track played, {longestPlayTime:F2}/{currentTrackLength:F2})");
                        return true;
                    }
                    
                    if (currentTrackStartTime > 0f)
                    {
                        float elapsed = Time.time - currentTrackStartTime;
                        if (elapsed >= (currentTrackLength - 3f))
                        {
                            Log.Out($"[Advance] DECISION: Advance (Case4b: elapsed >= length-3s, {elapsed:F2}/{currentTrackLength:F2})");
                            return true;
                        }
                    }
                }
                
                // Case 5: Emergency fallback - reduced from 5 minutes to 3 minutes
                if (currentTrackStartTime > 0f)
                {
                    float elapsed = Time.time - currentTrackStartTime;
                    if (elapsed > 180f)
                    {
                        Log.Out($"[Advance] DECISION: Advance (Case5: emergency timeout, elapsed={elapsed:F2}s)");
                        return true;
                    }
                }
                
                // Case 6: NEW - If track length is unknown but enough time has passed
                if (currentTrackLength <= 0f && currentTrackStartTime > 0f)
                {
                    float elapsed = Time.time - currentTrackStartTime;
                    if (elapsed > 240f)
                    {
                        Log.Out($"[Advance] DECISION: Advance (Case6: unknown length, elapsed={elapsed:F2}s)");
                        return true;
                    }
                }
                
                Log.Out("[Advance] DECISION: Do NOT advance (conditions not met)");
                return false;
            }
            catch (Exception e)
            {
                Log.Out($"[Advance] Error checking for track completion: {e.Message}");
                return false;
            }
        }

        /// <summary>
        /// Advances to the next track and returns it
        /// </summary>
        public string AdvanceToNextTrack()
        {
            try
            {
                Log.Out("[Advance] AdvanceToNextTrack START");
                Log.Out($"[Advance] Pre-advance state: PlaylistCount={currentPlaylist.Count}, Position={playlistPosition}, Current='{currentTrackName}', IsAdvancing={isAdvancingTrack}");
                
                if (isAdvancingTrack)
                {
                    if ((Time.time - lastTrackAdvancementTime) > ADVANCEMENT_TIMEOUT)
                    {
                        Log.Out("[Advance] Advancement timeout detected, forcing reset");
                        isAdvancingTrack = false;
                    }
                    else
                    {
                        Log.Out("[Advance] Track advancement already in progress, skipping");
                        return currentTrackName;
                    }
                }
                
                isAdvancingTrack = true;
                lastTrackAdvancementTime = Time.time;
                
                if (currentPlaylist.Count == 0)
                {
                    Log.Out("[Advance] Cannot advance playlist - no tracks available; creating playlist...");
                    CreatePlaylist();
                    
                    if (currentPlaylist.Count == 0)
                    {
                        Log.Out("[Advance] Still no tracks after creating playlist; abort");
                        isAdvancingTrack = false;
                        return "";
                    }
                }
                
                string previousTrack = currentTrackName;
                
                // Advance playlist position
                playlistPosition++;
                if (playlistPosition >= currentPlaylist.Count)
                {
                    playlistPosition = 0;
                    Log.Out("[Advance] Reached end of playlist, looping back to beginning");
                }
                
                string nextTrack = currentPlaylist[playlistPosition].name;
                string nextTrackFile = currentPlaylist[playlistPosition].file;
                currentTrackName = nextTrack;
                
                // Record when this new track started
                currentTrackStartTime = Time.time;
                currentTrackLength = 0f;

                Log.Out($"[Advance] Previous='{previousTrack}', Next='{nextTrack}', File='{nextTrackFile}', NewPosition={playlistPosition + 1}/{currentPlaylist.Count}");
                
                return nextTrack;
            }
            catch (Exception e)
            {
                Log.Out($"[Advance] Error advancing to next track: {e.Message}");
                return currentTrackName;
            }
            finally
            {
                // Always release the lock
                isAdvancingTrack = false;
                Log.Out("[Advance] AdvanceToNextTrack END");
            }
        }

        /// <summary>
        /// Manually skips to the next track (for console command)
        /// </summary>
        public string SkipToNextTrack()
        {
            try
            {
                Log.Out("[Advance] MANUAL TRACK SKIP REQUESTED");
                
                if (currentPlaylist.Count == 0)
                {
                    Log.Out("[Advance] No playlist available for skipping; creating playlist...");
                    CreatePlaylist();
                    
                    if (currentPlaylist.Count == 0)
                    {
                        Log.Out("[Advance] Still no playlist after loading - cannot skip");
                        return "";
                    }
                }
                
                Log.Out($"[Advance] Manually skipping from track: {currentTrackName}");
                
                bool wasAdvancing = isAdvancingTrack;
                if (wasAdvancing)
                {
                    Log.Out("[Advance] Overriding existing advancement lock for manual skip");
                }
                isAdvancingTrack = false; // Clear any existing lock
                
                string nextTrack = AdvanceToNextTrack();
                
                Log.Out("[Advance] Manual track skip completed");
                return nextTrack;
            }
            catch (Exception e)
            {
                Log.Out($"[Advance] Error in manual track skip: {e.Message}");
                // Clear lock on error
                isAdvancingTrack = false;
                return currentTrackName;
            }
        }

        /// <summary>
        /// Gets information about the current track and playlist
        /// </summary>
        public string GetCurrentTrackInfo()
        {
            try
            {
                if (string.IsNullOrEmpty(currentTrackName) || currentPlaylist.Count == 0)
                {
                    return "No track currently playing";
                }
                
                float elapsedTime = currentTrackStartTime > 0f ? Time.time - currentTrackStartTime : 0f;
                
                return $"Track: {currentTrackName} | Position: {playlistPosition + 1}/{currentPlaylist.Count} | Elapsed: {elapsedTime:F1}s | Length: {(currentTrackLength>0?currentTrackLength.ToString("F1"):"unknown")}s";
            }
            catch (Exception e)
            {
                Log.Out($"[Playlist] Error getting current track info: {e.Message}");
                return "Error retrieving track info";
            }
        }

        /// <summary>
        /// Updates the track length when audio source is available
        /// </summary>
        public void UpdateCurrentTrackLength(float length)
        {
            if (currentTrackLength <= 0f)
            {
                currentTrackLength = length;
                Log.Out($"[Playlist] Updated track length: {currentTrackName} = {length:F1}s");
            }
        }

        // Public accessors
        public string CurrentTrackName => currentTrackName;
        public int PlaylistPosition => playlistPosition;
        public int PlaylistCount => currentPlaylist.Count;
        public bool IsAdvancing => isAdvancingTrack;
        
        /// <summary>
        /// Clears current track state (when no radios are active)
        /// </summary>
        public void ClearCurrentTrack()
        {
            Log.Out("[Playlist] Clearing current track state");
            currentTrackName = "";
            currentTrackStartTime = 0f;
            currentTrackLength = 0f;
        }

        /// <summary>
        /// Determines if playlist should advance when there are no loaded active radios,
        /// using only time-based heuristics from the last known start time/length.
        /// </summary>
        public bool ShouldAdvanceWithoutRadios()
        {
            try
            {
                if (string.IsNullOrEmpty(currentTrackName)) return false;
                if (isAdvancingTrack) return false;

                // If we know the length, advance when elapsed exceeds length - small grace
                if (currentTrackLength > 0f && currentTrackStartTime > 0f)
                {
                    float elapsed = Time.time - currentTrackStartTime;
                    if (elapsed >= (currentTrackLength - 2f))
                    {
                        Log.Out($"[Advance] Headless: elapsed {elapsed:F2}/{currentTrackLength:F2} -> advance");
                        return true;
                    }
                }
                else if (currentTrackStartTime > 0f)
                {
                    // Unknown length: use a conservative timeout (4 minutes)
                    float elapsed = Time.time - currentTrackStartTime;
                    if (elapsed > 240f)
                    {
                        Log.Out($"[Advance] Headless: unknown length, elapsed={elapsed:F2} -> advance");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception e)
            {
                Log.Out($"[Advance] Error in ShouldAdvanceWithoutRadios: {e.Message}");
                return false;
            }
        }
    }
}