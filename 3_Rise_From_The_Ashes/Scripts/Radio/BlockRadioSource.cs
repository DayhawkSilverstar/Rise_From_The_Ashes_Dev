using Audio;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static Audio.Manager;

namespace Rise.Radio
{
    public class BlockRadioSource : RadioSource
    {
        // Anchor GameObject and dedicated AudioSource for this radio block
        private GameObject _audioObject;
        private AudioSource _dedicatedAudioSource;
        private bool _isInitialized = false;
        private float _pendingSyncTime = 0f;

        // Mark radios that were unloaded due to chunk unload so the reaper does not permanently remove them
        public bool TemporarilyUnloaded { get; private set; }

        public BlockRadioSource()
        {
            IsOn = false;
            ClipName = "";
            PlayListPosition = 0;
            LastSyncTime = 0f;
            _isInitialized = false;
            _pendingSyncTime = 0f;
            TemporarilyUnloaded = false;
        }
        
        public RiseRadio Block { get; set; }

        /// <summary>
        /// Creates and initializes the dedicated AudioSource for this radio block
        /// This ensures the AudioSource is always available when the block is loaded
        /// </summary>
        public void OnBlockLoadedHook()
        {
            try
            {                
                RadioDebug.D("BRS", $"LoadedHook {Name} pos={Position}");

                // Extra diagnostics about block validity/type
                try
                {
                    var gm = GameManager.Instance;
                    var world = gm != null ? gm.World : null;
                    if (world != null)
                    {
                        var pos = new Vector3i(Position);
                        var bv = world.GetBlock(pos);                        
                    }
                }
                catch (Exception exB)
                {
                    Log.Out($"[BRS] Unable to query world block at {Position}: {exB.Message}");
                }
                
                // Clear temporary unload flag upon load
                TemporarilyUnloaded = false;
                
                // Create the dedicated audio source
                CreateDedicatedAudioSource();

                // Mark initialized BEFORE attempting playback/resume
                _isInitialized = true;                
                
                // Prefer persisted state from the block if available; fall back to RadioManager central store
                bool savedIsOn = false;
                string savedClip = string.Empty;
                float savedTime = 0f;
                int savedPos = 0;
                bool hasSavedState = false;

                try
                {
                    if (Block != null)
                    {
                        hasSavedState = Block.TryGetPersistentState(out savedIsOn, out savedClip, out savedTime, out savedPos);                        
                    }
                }
                catch (Exception exPS)
                {
                    Log.Out($"[BRS] Error reading Block persistent state: {exPS.Message}");
                }

                // Fallback to RadioManager persistence if block had no state
                if (!hasSavedState)
                {
                    try
                    {
                        bool rmOn; string rmClip; float rmTime; int rmPos;
                        if (RadioManager.Instance.TryGetPersistentState(Position, out rmOn, out rmClip, out rmTime, out rmPos))
                        {
                            hasSavedState = true;
                            savedIsOn = rmOn;
                            savedClip = rmClip;
                            savedTime = rmTime;
                            savedPos = rmPos;                            
                        }
                        else
                        {
                            Log.Out($"[BRS] No persisted state found in RadioManager for {Name}");
                        }
                    }
                    catch (Exception exRM)
                    {
                        Log.Out($"[BRS] Error reading RadioManager persistent state: {exRM.Message}");
                    }
                }

                // Determine if this radio should be ON from block state or saved state
                bool blockReportsOn = (Block != null && Block.IsRadioOn());
                bool shouldBeOn = blockReportsOn || this.IsOn || (hasSavedState && savedIsOn);
                               
                
                // If radio should be on at load, attempt to resume saved clip/time, otherwise current playlist
                if (shouldBeOn)
                {
                    IsOn = true; // keep logical ON even if audio is not yet available
                    if (Block != null)
                    {
                        try { Block.SetRadioOn(true); } catch {}
                    }

                    // Fetch current track from RadioManager first, then playlist
                    string currentTrack = string.Empty;
                    int currentPos = 0;
                    try
                    {
                        currentTrack = RadioManager.Instance.CurrentClipName;
                        if (string.IsNullOrEmpty(currentTrack))
                        {
                            currentTrack = RadioPlaylistManager.Instance.CurrentTrackName;
                            if (string.IsNullOrEmpty(currentTrack))
                                currentTrack = RadioPlaylistManager.Instance.GetCurrentTrack();
                            if (!string.IsNullOrEmpty(currentTrack))
                                RadioManager.Instance.SetCurrentClip(currentTrack);
                        }
                        currentPos = RadioPlaylistManager.Instance.PlaylistPosition;
                    }
                    catch { /* ignore */ }

                    // Decide whether to resume saved or adopt current playlist track
                    bool savedClipValid = hasSavedState && savedIsOn && !string.IsNullOrEmpty(savedClip);
                    bool matchesCurrent = savedClipValid && !string.IsNullOrEmpty(currentTrack) && string.Equals(savedClip, currentTrack, StringComparison.OrdinalIgnoreCase);
                    bool playlistPosDiffers = savedClipValid && savedPos != currentPos && !string.IsNullOrEmpty(currentTrack);

                    // If playlist advanced or saved clip differs from current, prefer current playlist + sync
                    if (!string.IsNullOrEmpty(currentTrack) && (playlistPosDiffers || (savedClipValid && !matchesCurrent) || !savedClipValid))
                    {
                        float syncTime = GetSyncTimeFromOtherRadios(currentTrack);                        
                        PlayInternal(currentTrack, Mathf.Max(0f, syncTime));
                    }
                    else if (savedClipValid)
                    {
                        RadioDebug.D("BRS", $"resume saved '{savedClip}' t={savedTime:F2}");
                        PlayInternal(savedClip, Mathf.Max(0f, savedTime));
                    }
                    else
                    {
                        // Fallback: try current track if available
                        if (!string.IsNullOrEmpty(currentTrack))
                        {
                            float syncTime = GetSyncTimeFromOtherRadios(currentTrack);                            
                            PlayInternal(currentTrack, Mathf.Max(0f, syncTime));
                        }
                        else
                        {
                            Log.Out("[BRS] No track available to resume");
                        }
                    }
                }
                else
                {
                    Log.Out("[BRS] Decision: shouldBeOn=false; not attempting resume");
                }
                                
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] OnBlockLoadedHook error: {e.Message}");
                Log.Out($"[BRS] Stack trace: {e.StackTrace}");
                RadioDebug.E("BRS", "OnBlockLoadedHook", e);
            }
        }

        /// <summary>
        /// Destroys the dedicated AudioSource and cleans up resources
        /// </summary>
        public void OnBlockUnloadedHook()
        {
            try
            {                
                RadioDebug.D("BRS", $"UnloadHook {Name}");
                
                // Mark as temporarily unloaded so the reaper doesn't permanently remove us
                TemporarilyUnloaded = true;                
                
                // Before tearing down, persist state centrally to ensure resume upon reload
                try
                {
                    float t = GetCurrentTime();
                    int pos = 0; try { pos = RadioPlaylistManager.Instance.PlaylistPosition; } catch {}                    
                    RadioManager.Instance.SaveBlockPersistentState(Position, IsOn, ClipName, t, pos);
                }
                catch (Exception persistEx)
                {
                    Log.Out($"[BRS] Persist-on-unload error: {persistEx.Message}");
                }
                
                // Stop any currently playing audio but DO NOT flip persistent power state
                if (!string.IsNullOrEmpty(ClipName))
                {                    
                    StopInternal(persistPowerOff: false);
                }

                // Destroy the dedicated audio source and anchor object
                DestroyDedicatedAudioSource();
                
                // Do not clear IsOn/ClipName here; preserve state for resume
                _isInitialized = false;
                                
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] OnBlockUnloadedHook error: {e.Message}");
            }
        }

        /// <summary>
        /// Explicit destruction hook for when the block is destroyed (not just unloaded).
        /// Ensures audio is stopped with power-off, anchor is disposed, and OFF is persisted.
        /// </summary>
        public void OnBlockDestroyedHook()
        {
            try
            {                

                // Unset temporary flag; this is a permanent removal
                TemporarilyUnloaded = false;

                // Stop and power off
                try
                {
                    StopInternal(persistPowerOff: true);
                }
                catch (Exception stopEx)
                {
                    Log.Out($"[BRS] Error during StopInternal on destroy: {stopEx.Message}");
                }

                // Persist OFF state explicitly to prevent auto-resume at this position
                try
                {
                    RadioManager.Instance.SaveBlockPersistentState(Position, false, string.Empty, 0f, 0);
                }
                catch (Exception persistEx)
                {
                    Log.Out($"[BRS] Persist OFF on destroy error: {persistEx.Message}");
                }

                // Dispose anchor/audio references
                DestroyDedicatedAudioSource();

                // Clear state
                IsOn = false;
                ClipName = string.Empty;
                _isInitialized = false;
                
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] OnBlockDestroyedHook error: {e.Message}");
            }
        }

        /// <summary>
        /// Creates the anchor GameObject for this radio block
        /// The actual AudioSource will be managed by the game's audio system
        /// </summary>
        private void CreateDedicatedAudioSource()
        {
            try
            {
                if (_audioObject == null)
                {
                    // Create anchor GameObject for position tracking
                    string name = $"BlockRadio_{(Block != null ? Block.blockID : EntityID)}_{Position}";
                    _audioObject = new GameObject(name);
                    _audioObject.transform.position = Position;
                    
                    // Attach lifecycle logger to help diagnose unexpected destruction/disable
                    _audioObject.AddComponent<RadioAnchorLifecycle>().Init(Name, Position);
                                       
                }
                else
                {
                    Log.Out($"[BRS] Anchor already exists for {Name} at {_audioObject.transform.position}");
                }

            }
            catch (Exception e)
            {
                Log.Out($"[BRS] Error creating anchor GameObject: {e.Message}");
            }
        }

        /// <summary>
        /// Destroys the anchor GameObject and cleans up audio resources
        /// </summary>
        private void DestroyDedicatedAudioSource()
        {
            try
            {
                // Stop the audio if it's playing
                if (AudioSourceObject != null)
                {
                    try
                    {
                        if (AudioSourceObject.isPlaying)
                        {                            
                            Manager.Stop(Position, ClipName);
                        }
                    }
                    catch (Exception stopEx)
                    {
                        Log.Out($"[BRS] Error stopping audio during cleanup: {stopEx.Message}");
                    }
                }

                if (_audioObject != null)
                {
                    UnityEngine.Object.Destroy(_audioObject);
                    _audioObject = null;                    
                }

                // Clear references
                _dedicatedAudioSource = null;
                AudioSourceObject = null;
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] Error destroying audio resources: {e.Message}");
            }
        }

        /// <summary>
        /// Gets sync time from other radios playing the same track
        /// </summary>
        private float GetSyncTimeFromOtherRadios(string trackName)
        {
            try
            {
                var existingSources = GetAudioSources(trackName);                
                if (existingSources.Count > 0)
                {
                    foreach (var s in existingSources)
                    {
                        if (s == null) continue;
                        string c = s.clip != null ? s.clip.name : "<null>";
                        float t = (s.clip != null) ? s.time : -1f;
                        Vector3 p = s.gameObject != null ? s.gameObject.transform.position : Vector3.zero;                        
                    }

                    var primary = existingSources.Where(s => s != null && s.isPlaying && s.clip != null)
                                                 .OrderByDescending(s => s.time)
                                                 .FirstOrDefault();
                    if (primary != null && primary.time > 0f)
                    {                        
                        return primary.time;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] Error getting sync time: {e.Message}");
            }
            
            return 0f;
        }

        /// <summary>
        /// Check if the block still exists in the world
        /// </summary>
        public override bool IsParentValid()
        {
            try
            {
                if (Block == null) return false;
                
                var gm = GameManager.Instance;
                var world = gm != null ? gm.World : null;
                if (world == null) return false;
                
                Vector3i blockPos = new Vector3i(Position);
                BlockValue blockValue = world.GetBlock(blockPos);
                bool valid = blockValue.type != 0; // 0 means air/empty block
                if (!valid)
                {
                    Log.Out($"[BRS] IsParentValid=false at {blockPos} for {Name} (type={blockValue.type})");
                }
                return valid;
            }
            catch (Exception ex)
            {
                Log.Out($"[BRS] IsParentValid exception for {Name}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Starts playing the specified audio clip
        /// </summary>
        public override void Play(string soundGroup)
        {
            PlayInternal(soundGroup, 0f);
        }

        /// <summary>
        /// Swap to a new clip deterministically using scheduled playback
        /// Ensure the previous clip is stopped and unregistered before starting the new one to avoid overlap.
        /// </summary>
        public override void SwapClip(string clipName, float startTimeSeconds, double dspStart)
        {
            try
            {
                if (!_isInitialized)
                {                    
                    return;
                }                

                // Stop and unregister previous clip if different to avoid overlap
                string previousClip = ClipName;
                if (!string.IsNullOrEmpty(previousClip) && previousClip != clipName)
                {
                    try { RadioCoordinator.Instance.UnregisterRadioForTrack(previousClip, this); } catch {}
                    try { Manager.Stop(Position, previousClip); } catch {}                    
                }

                // Request playback via game system (ensures source exists/pooled correctly)
                ClipName = clipName;
                Manager.Play(Position, clipName);
                Log.Out($"[BRS] SwapClip -> Manager.Play issued at {Position} for '{clipName}'");

                // Take control of the created source with retries and schedule
                GameManager gm = GameManager.Instance;
                if (gm != null)
                {
                    gm.StartCoroutine(SwapClipCo(clipName, startTimeSeconds, dspStart));
                }
                else
                {
                    Log.Out("[BRS] SwapClip warning: GameManager.Instance is null; cannot schedule coroutine");
                }
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] SwapClip error for {Name}: {e.Message}");
            }
        }

        private IEnumerator SwapClipCo(string clipName, float startTimeSeconds, double dspStart)
        {
            // allow Manager.Play to instantiate
            const int maxAttempts = 20; // ~1s total at 0.05s steps
            int attempts = 0;
            AudioSource src = null;

            while (attempts < maxAttempts)
            {
                src = GetAudioSource(Position, clipName);
                if (src != null && src.clip != null) break;
                if (attempts == 5)
                {
                    // if still missing after ~250ms, nudge the audio system once
                    try { Manager.Play(Position, clipName); } catch { }
                }
                attempts++;
                yield return new WaitForSeconds(0.05f);
            }

            if (src == null)
            {                
                yield break;
            }

            AudioSourceObject = src;
            _dedicatedAudioSource = src;

            try
            {                
                // Update RadioManager centralized length if this is the current clip
                if (src.clip != null)
                {
                    string cName = src.clip.name;
                    string central = RadioManager.Instance.CurrentClipName;
                    if (!string.IsNullOrEmpty(central) && (cName == central || cName.Contains(central) || central.Contains(cName)))
                    {
                        RadioManager.Instance.UpdateCurrentClipLength(src.clip.length);
                    }
                }
                // Fully release any currently playing state on this source and schedule aligned start
                src.Stop();
                if (src.clip != null)
                {
                    src.time = Mathf.Clamp(startTimeSeconds, 0f, src.clip.length - 0.05f);
                }
                src.PlayScheduled(dspStart);
                IsOn = true;
                // Register with coordinator after scheduling to enable sync
                try { RadioCoordinator.Instance.RegisterRadioForTrack(clipName, this); } catch {}                
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] SwapClipCo scheduling error for {Name}: {e.Message}");
            }
        }

        /// <summary>
        /// Recovery used by watchdog
        /// </summary>
        public override void ReinitAndRestart(string clipName, float startTimeSeconds)
        {
            try
            {                
                if (!_isInitialized)
                {
                    CreateDedicatedAudioSource();
                    _isInitialized = true;
                }

                // If we are switching clips, stop and unregister the old one first
                if (!string.IsNullOrEmpty(ClipName) && ClipName != clipName)
                {
                    try { RadioCoordinator.Instance.UnregisterRadioForTrack(ClipName, this); } catch {}
                    try { Manager.Stop(Position, ClipName); } catch {}                    
                }

                PlayInternal(clipName, startTimeSeconds);
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] ReinitAndRestart error for {Name}: {e.Message}");
            }
        }

        /// <summary>
        /// Internal play method with sync time support - Simplified to use game's audio system
        /// Ensures any previous clip at this position is stopped to avoid overlapping audio.
        /// </summary>
        private void PlayInternal(string soundGroup, float syncTime = 0f)
        {
            try
            {                
                RadioDebug.D("BRS", $"Play '{soundGroup}' syncT={syncTime:F2}");
                
                if (!_isInitialized)
                {
                    Log.Out("[BRS] Radio not initialized, cannot play");
                    return;
                }

                // If we were already playing something different, stop and unregister it first
                if (!string.IsNullOrEmpty(ClipName) && ClipName != soundGroup)
                {
                    try { RadioCoordinator.Instance.UnregisterRadioForTrack(ClipName, this); } catch {}
                    try { Manager.Stop(Position, ClipName); } catch {}                    
                }

                ClipName = soundGroup;
                IsOn = true; // Logical ON even if audio is not yet available

                // If a game-managed source already exists at our position for this clip, take control instead of spawning another
                var existingSrc = GetAudioSource(Position, soundGroup);
                if (existingSrc != null && existingSrc.clip != null)
                {
                    AudioSourceObject = existingSrc;
                    _dedicatedAudioSource = existingSrc;
                    if (syncTime > 0f)
                    {
                        float clamped = Mathf.Clamp(syncTime, 0f, existingSrc.clip.length - 0.1f);
                        existingSrc.time = clamped;                        
                    }
                    // Update RadioManager length if this is the central clip
                    string central = RadioManager.Instance.CurrentClipName;
                    if (!string.IsNullOrEmpty(central))
                    {
                        string cName = existingSrc.clip.name;
                        if (cName == central || cName.Contains(central) || central.Contains(cName))
                        {
                            RadioManager.Instance.UpdateCurrentClipLength(existingSrc.clip.length);
                        }
                    }
                    try { RadioCoordinator.Instance.RegisterRadioForTrack(soundGroup, this); } catch {}                    
                }
                else
                {
                    // Use the game's audio system directly
                    Manager.Play(Position, soundGroup);                    
                    
                    // Wait for the game to create the AudioSource, then take control of it (with retries)
                    GameManager gm = GameManager.Instance;
                    if (gm != null)
                    {
                        gm.StartCoroutine(TakeControlOfGameAudioSource(soundGroup, syncTime));
                        Log.Out($"[BRS] Requested audio playback from game system: {soundGroup}");
                    }
                    else
                    {
                        Log.Out("[BRS] Warning: GameManager.Instance is null; cannot start coroutine to take control");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] Error in PlayInternal: {e.Message}");
                // Do not flip IsOn=false here; defer to proximity watchdog to recover
            }
        }

        /// <summary>
        /// Coroutine to take control of the AudioSource created by the game's audio system
        /// </summary>
        private System.Collections.IEnumerator TakeControlOfGameAudioSource(string clipName, float syncTime = 0f)
        {
            const int maxAttempts = 20; // try for about a second total
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                yield return new WaitForSeconds(0.05f);
                
                try
                {
                    // Trace all sources the system sees for this clip (first attempts only)
                    if (attempts == 0)
                    {
                        var allForClip = GetAudioSources(clipName);                        
                        int idx = 0;
                        foreach (var s in allForClip)
                        {
                            if (s == null) continue;
                            string c = s.clip != null ? s.clip.name : "<null>";
                            float t = (s.clip != null) ? s.time : -1f;
                            Vector3 p = s.gameObject != null ? s.gameObject.transform.position : Vector3.zero;                            
                        }
                    }

                    // Find the AudioSource that the game created at our position
                    var gameAudioSource = GetAudioSource(clipName: clipName, position: Position);
                    if (gameAudioSource != null && gameAudioSource.clip != null)
                    {
                        // Take control of this AudioSource
                        AudioSourceObject = gameAudioSource;
                        _dedicatedAudioSource = gameAudioSource;                        
                        
                        // Apply sync time if needed
                        if (syncTime > 0f)
                        {
                            float clampedTime = Mathf.Clamp(syncTime, 0f, gameAudioSource.clip.length - 0.1f);
                            gameAudioSource.time = clampedTime;
                            
                        }
                        
                        IsOn = true;
                        LastSyncTime = Time.time;
                        
                        // Update block state
                        if (Block != null)
                        {
                            Block.SetRadioOn(true);
                        }
                        
                        // Inform RadioManager of length if this is the central clip
                        string central = RadioManager.Instance.CurrentClipName;
                        if (!string.IsNullOrEmpty(central))
                        {
                            string cName = gameAudioSource.clip.name;
                            if (cName == central || cName.Contains(central) || central.Contains(cName))
                            {
                                RadioManager.Instance.UpdateCurrentClipLength(gameAudioSource.clip.length);
                            }
                        }
                        
                        // Register with coordinator for sync/coalescing
                        try { RadioCoordinator.Instance.RegisterRadioForTrack(clipName, this); } catch {}
                                                
                        yield break;
                    }

                    // If half the attempts have passed and still no source, nudge the system once
                    if (attempts == 10)
                    {
                        try { Manager.Stop(Position, clipName); } catch { }
                        try { Manager.Play(Position, clipName); } catch { }                        
                    }
                }
                catch (Exception e)
                {
                    Log.Out($"[BRS] Error in TakeControlOfGameAudioSource attempt {attempts}: {e.Message}");
                }

                attempts++;
            }
            
            // Keep logical ON and clip name so proximity/watchdog can reissue playback later
            IsOn = true;
            ClipName = clipName;
        }

        /// <summary>
        /// Stops the currently playing audio
        /// </summary>
        public override void Stop(string soundGroup)
        {
            StopInternal(persistPowerOff: true);
        }

        /// <summary>
        /// Internal stop method. When persistPowerOff=false, only stop local audio but preserve logical power state/clip.
        /// </summary>
        private void StopInternal(bool persistPowerOff)
        {
            try
            {                

                // Stop using the game's audio system
                if (!string.IsNullOrEmpty(ClipName))
                {                    
                    Manager.Stop(Position, ClipName);
                }

                // Clear references
                if (AudioSourceObject != null)
                {                    
                    AudioSourceObject = null;
                }
                _dedicatedAudioSource = null;

                if (persistPowerOff)
                {
                    IsOn = false;
                    string previousClip = ClipName;
                    ClipName = "";

                    // Update block state if available
                    if (Block != null)
                    {
                        Block.SetRadioOn(false);
                    }
                }                
            }
            catch (Exception e)
            {                
                if (persistPowerOff)
                {
                    IsOn = false;
                    ClipName = "";
                }
            }
        }

        /// <summary>
        /// Changes the currently playing clip
        /// </summary>
        public void UpdateClip(string clipName)
        {
            try
            {                
                if (!_isInitialized)
                {                    
                    return;
                }

                // Get sync time from other radios playing this clip
                float syncTime = GetSyncTimeFromOtherRadios(clipName);

                // Stop current playback without flipping power state
                if (IsOn || !string.IsNullOrEmpty(ClipName))
                {
                    StopInternal(persistPowerOff: false);
                }

                // Start new clip
                PlayInternal(clipName, syncTime);                
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] Error updating clip: {e.Message}");
                // Do not force IsOn=false here; allow watchdog/proximity to recover
            }
        }

        /// <summary>
        /// Synchronizes this radio's playback time with other radios playing the same track
        /// </summary>
        public void SyncWithOtherRadios(string trackName)
        {
            try
            {
                if (!IsOn || _dedicatedAudioSource == null || _dedicatedAudioSource.clip == null)
                {                    
                    return;
                }

                float syncTime = GetSyncTimeFromOtherRadios(trackName);
                if (syncTime > 0f && Math.Abs(_dedicatedAudioSource.time - syncTime) > 0.5f)
                {
                    float clampedTime = Mathf.Clamp(syncTime, 0f, _dedicatedAudioSource.clip.length - 0.1f);
                    _dedicatedAudioSource.time = clampedTime;                    
                }
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] Error syncing with other radios: {e.Message}");
            }
        }

        /// <summary>
        /// Gets the current playback time
        /// </summary>
        public float GetCurrentTime()
        {
            try
            {
                if (_dedicatedAudioSource != null && _dedicatedAudioSource.clip != null)
                {
                    return _dedicatedAudioSource.time;
                }
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] Error getting current time: {e.Message}");
            }
            return 0f;
        }

        /// <summary>
        /// Sets the playback time
        /// </summary>
        public void SetCurrentTime(float time)
        {
            try
            {
                if (_dedicatedAudioSource != null && _dedicatedAudioSource.clip != null)
                {
                    float clampedTime = Mathf.Clamp(time, 0f, _dedicatedAudioSource.clip.length - 0.1f);
                    _dedicatedAudioSource.time = clampedTime;
                }
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] Error setting current time: {e.Message}");
            }
        }

        /// <summary>
        /// Gets the total length of the current clip
        /// </summary>
        public float GetClipLength()
        {
            try
            {
                if (_dedicatedAudioSource != null && _dedicatedAudioSource.clip != null)
                {
                    return _dedicatedAudioSource.clip.length;
                }
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] Error getting clip length: {e.Message}");
            }
            return 0f;
        }

        /// <summary>
        /// Checks if the radio is currently playing
        /// </summary>
        public bool IsCurrentlyPlaying()
        {
            try
            {
                return _dedicatedAudioSource != null && _dedicatedAudioSource.isPlaying;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the dedicated AudioSource (for external access)
        /// Note: This now returns the AudioSource managed by the game's audio system
        /// </summary>
        public AudioSource GetDedicatedAudioSource()
        {
            return AudioSourceObject;
        }

        /// <summary>
        /// Coroutine for delayed synchronization
        /// </summary>
        private System.Collections.IEnumerator DelayedSync(string trackName, float delay)
        {
            yield return new WaitForSeconds(delay);
            
            try
            {
                Log.Out($"[BRS] Performing delayed sync for track: {trackName}");
                SyncWithOtherRadios(trackName);
                
                // Also use the global sync method
                RadioSource.SyncAudioSource(trackName);
            }
            catch (Exception e)
            {
                Log.Out($"[BRS] Error in delayed sync: {e.Message}");
            }
        }

        // Legacy methods for compatibility with existing code
        public AudioSource FindAudioSource(string clipName)
        {
            return AudioSourceObject;
        }
    }

    /// <summary>
    /// Helper component to log lifecycle events for radio anchors.
    /// </summary>
    internal class RadioAnchorLifecycle : MonoBehaviour
    {
        private string _ownerName;
        private Vector3 _pos;

        public void Init(string ownerName, Vector3 pos)
        {
            _ownerName = ownerName;
            _pos = pos;
        }

        private void OnDisable()
        {
            Log.Out($"[BRS][Anchor] OnDisable owner={_ownerName} pos={_pos}");
            RadioDebug.D("BRS", $"Anchor Disable owner={_ownerName}");
        }

        private void OnDestroy()
        {
            Log.Out($"[BRS][Anchor] OnDestroy owner={_ownerName} pos={_pos}");
            RadioDebug.D("BRS", $"Anchor Destroy owner={_ownerName}");
        }
    }
}