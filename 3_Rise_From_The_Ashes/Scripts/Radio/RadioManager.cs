using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Rise.Radio;
using Audio; // Added for Manager access

public class RadioManager
{
    private static RadioManager _instance;
    private List<RadioSource> radioSources = new List<RadioSource>();
    
    // Central persistence store for block radios keyed by world+block coords
    private struct PersistedState
    {
        public bool IsOn;
        public string Clip;
        public float Time;
        public int PlaylistPos;
        public DateTime LastUpdated;
    }

    private readonly Dictionary<string, PersistedState> persistedStates = new Dictionary<string, PersistedState>();

    // Centralized "what is playing" state (single source of truth)
    private string currentClipName = string.Empty;
    private float currentClipStartTime = 0f;
    private float currentClipLength = 0f;

    public string CurrentClipName => currentClipName;
    public float CurrentClipElapsed => currentClipStartTime > 0f ? Time.time - currentClipStartTime : 0f;
    public float CurrentClipLength => currentClipLength;

    public void SetCurrentClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return;
        if (!string.Equals(currentClipName, clipName, StringComparison.Ordinal))
        {
            currentClipName = clipName;
            currentClipStartTime = Time.time;
            currentClipLength = 0f;            
        }
    }

    public void UpdateCurrentClipLength(float length)
    {
        if (length > 0f && (currentClipLength <= 0f || Math.Abs(currentClipLength - length) > 0.05f))
        {
            currentClipLength = length;            
        }
    }

    public void ClearCurrentClip()
    {
        if (!string.IsNullOrEmpty(currentClipName))
        {
            Log.Out($"[RM][CURRENT] Clearing current clip '{currentClipName}'");
        }
        currentClipName = string.Empty;
        currentClipStartTime = 0f;
        currentClipLength = 0f;
    }

    // Timing for periodic operations
    private float lastSyncTime = 0f;
    private const float SYNC_INTERVAL = 5f; // Sync every 5 seconds
    private float lastCleanupTime = 0f;
    private const float CLEANUP_INTERVAL = 30f; // Cleanup every 30 seconds
    private float lastTrackCheckTime = 0f;
    private const float TRACK_CHECK_INTERVAL = 3f; // Check every 3 seconds

    // Proximity activation timing
    private float lastProximityCheckTime = 0f;
    private const float PROXIMITY_CHECK_INTERVAL = 0.5f; // Half-second cadence
    private const float HEARING_RADIUS = 48f; // Within 48 meters radios should be audible
    
    // Initialization tracking
    private bool isInitializing = false;

    // Cleanup counter for persistent state maintenance
    private int radioBlockCleanupCounter = 0;

    // Watchdog
    private bool watchdogStarted = false;

    private RadioManager()
    {
        RadioDebug.D("RM", "CTOR");
        // If its part of the unit test then return.
        if (GameManager.Instance == null && ConnectionManager.Instance == null)
        {
            return;
        }

        if (GameManager.IsDedicatedServer)
        {
            Log.Out($"RadioManager Dedicated Server");
        }
        else if (ConnectionManager.Instance != null && ConnectionManager.Instance.IsSinglePlayer)
        {
            Log.Out($"RadioManager Singleplayer Server");
        }
    }

    public static RadioManager Instance
    {
        get
        {
            if (_instance == null)
                _instance = new RadioManager();

            return _instance;
        }
    }

    // =====================
    // Persistence utilities
    // =====================

    private static string GetWorldName()
    {
        try { return GamePrefs.GetString(EnumGamePrefs.GameName) ?? "unknown"; } catch { return "unknown"; }
    }

    private static Vector3i ToBlockPos(Vector3 pos)
    {
        return new Vector3i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
    }

    private string BuildRadioKey(Vector3 position)
    {
        var bp = ToBlockPos(position);
        return $"{GetWorldName()}|{bp.x}|{bp.y}|{bp.z}";
    }

    private bool PersistedStatesEqual(in PersistedState a, in PersistedState b)
    {
        return a.IsOn == b.IsOn && a.Clip == b.Clip && Mathf.Abs(a.Time - b.Time) < 0.01f && a.PlaylistPos == b.PlaylistPos;
    }

    private void DumpPersistedStates(string reason, string contextKey = null)
    {
        try
        {
            Log.Out($"[RM][Persist] SNAPSHOT ({reason}) count={persistedStates.Count}{(string.IsNullOrEmpty(contextKey) ? string.Empty : $", ctx={contextKey}")}");
            if (persistedStates.Count == 0) return;

            // stable ordering for easier diffs
            foreach (var kv in persistedStates.OrderBy(k => k.Key))
            {
                var st = kv.Value;
                Log.Out($"[RM][Persist]   {kv.Key} -> on={st.IsOn} clip='{st.Clip}' time={st.Time:F2}s posIx={st.PlaylistPos} updated={st.LastUpdated:HH:mm:ss}");
            }
        }
        catch (Exception e)
        {
            Log.Out($"[RM][Persist] SNAPSHOT error: {e.Message}");
        }
    }

    private void SavePersistentState(Vector3 position, bool isOn, string clip, float time, int playlistPos)
    {
        try
        {
            var bp = ToBlockPos(position);
            string key = BuildRadioKey(position);

            PersistedState newState = new PersistedState
            {
                IsOn = isOn,
                Clip = clip ?? string.Empty,
                Time = Mathf.Max(0f, time),
                PlaylistPos = Mathf.Max(0, playlistPos),
                LastUpdated = DateTime.Now
            };

            bool hadOld = persistedStates.TryGetValue(key, out var oldState);
            bool changed = !hadOld || !PersistedStatesEqual(oldState, newState);

            persistedStates[key] = newState;
            Log.Out($"[RM][Persist] SAVED key={key} world='{GetWorldName()}' bp={bp} on={isOn} clip='{clip}' time={time:F2}s pos={position}");
            RadioDebug.D("RM-PERSIST", $"saved key={key} on={isOn} clip='{clip}' t={time:F2} pos={position}");

            // Dump snapshot only when an actual state change occurred
            if (changed)
            {
                DumpPersistedStates("AfterSaveChanged", key);
            }
        }
        catch (Exception e)
        {
            Log.Out($"[RM][Persist] Error saving state: {e.Message}");
            RadioDebug.E("RM-PERSIST", "error saving state", e);
        }
    }

    public bool TryGetPersistentState(Vector3 position, out bool isOn, out string clip, out float time, out int playlistPos)
    {
        var bp = ToBlockPos(position);
        string key = BuildRadioKey(position);
        Log.Out($"[RM][Persist] TRYGET world='{GetWorldName()}' pos={position} bp={bp} key={key}");
        RadioDebug.D("RM-PERSIST", $"try get key={key}");
        if (persistedStates.TryGetValue(key, out var st))
        {
            isOn = st.IsOn;
            clip = st.Clip;
            time = st.Time;
            playlistPos = st.PlaylistPos;
            Log.Out($"[RM][Persist] FOUND key={key} on={isOn} clip='{clip}' time={time:F2}s posIndex={playlistPos}");

            // Dump snapshot when used for reload
            DumpPersistedStates("Load", key);
            return true;
        }
        isOn = false; clip = string.Empty; time = 0f; playlistPos = 0;
        return false;
    }

    // Public API for blocks to persist their state on toggle/unload
    public void SaveBlockPersistentState(Vector3 position, bool isOn, string clip, float time, int playlistPos)
    {
        SavePersistentState(position, isOn, clip, time, playlistPos);
    }

    // Explicitly remove state at a position (used when destroyed)
    public void ClearPersistentState(Vector3 position)
    {
        try
        {
            string key = BuildRadioKey(position);
            if (persistedStates.Remove(key))
            {
                Log.Out($"[RM][Persist] REMOVED key={key} due to destruction");
            }
            else
            {
                Log.Out($"[RM][Persist] Clear requested for missing key={key} (already removed)");
            }
        }
        catch (Exception e)
        {
            Log.Out($"[RM][Persist] Error clearing state: {e.Message}");
        }
    }

    private void CleanupOldPersistedStates()
    {
        try
        {
            // Persisted states are the source of truth, including for unloaded radios.
            // Do NOT time-clean entries; only remove when an actual destruction occurs via ClearPersistentState.
            Log.Out($"[RM][Persist] Skipping time-based cleanup; persistedStates count={persistedStates.Count}");
            RadioDebug.D("RM-PERSIST", "skip time cleanup");
        }
        catch (Exception e)
        {
            Log.Out($"[RM][Persist] Cleanup error: {e.Message}");
            RadioDebug.E("RM-PERSIST", "cleanup error", e);
        }
    }

    private void SavePersistentStateFor(RadioSource radioSource)
    {
        try
        {
            if (radioSource == null) return;
            string clip = radioSource.ClipName;
            float time = 0f;
            if (radioSource.AudioSourceObject != null && radioSource.AudioSourceObject.clip != null)
            {
                // prefer real audio clip+time
                clip = string.IsNullOrEmpty(clip) ? radioSource.AudioSourceObject.clip.name : clip;
                time = radioSource.AudioSourceObject.time;
                // Also update the central clip length if this matches our current clip
                if (!string.IsNullOrEmpty(currentClipName) && radioSource.AudioSourceObject.clip != null)
                {
                    string c = radioSource.AudioSourceObject.clip.name;
                    if (c == currentClipName || c.Contains(currentClipName) || currentClipName.Contains(c))
                    {
                        UpdateCurrentClipLength(radioSource.AudioSourceObject.clip.length);
                    }
                }
            }
            int playlistPos = 0;
            try { playlistPos = RadioPlaylistManager.Instance.PlaylistPosition; } catch { }

            SavePersistentState(radioSource.Position, radioSource.IsOn, clip, time, playlistPos);
        }
        catch (Exception e)
        {
            Log.Out($"[RM][Persist] Error saving state for radio {radioSource?.Name}: {e.Message}");
            RadioDebug.E("RM-PERSIST", $"save for={radioSource?.Name}", e);
        }
    }

    #region Radio Source Management

    public void AddRadio(Entity entity)
    {
        RadioDebug.Enter("RM");
        if (entity == null)
        {
            Log.Out("[RM] AddRadio(Entity) called with null entity");
            return;
        }
        Log.Out($"[RM] AddRadio(Entity) entityId={entity.entityId}");
        // Check if radio already exists
        if (radioSources.Any(r => r.EntityID == entity.entityId))
        {
            Log.Out($"[RM] Radio already exists for entity {entity.entityId}");
            return;
        }

        RadioSource source = new EntityRadioSource
        {
            Entity = entity,
            IsOn = false,
            ClipName = "",
            PlayListPosition = RadioPlaylistManager.Instance.PlaylistPosition,
            EntityID = entity.entityId,
            Name = entity.entityId.ToString()
        };

        radioSources.Add(source);
        Log.Out($"[RM] Added Entity Radio: {entity.entityId} | Total={radioSources.Count}");
        RadioDebug.D("RM", $"AddRadio(Entity) total={radioSources.Count}");
        EnsureWatchdogStarted();
    }

    public void AddRadio(RiseDrone entity)
    {
        RadioDebug.Enter("RM");
        if (entity == null)
        {
            Log.Out("[RM] AddRadio(Drone) called with null drone");
            return;
        }
        Log.Out($"[RM] AddRadio(Drone) entityId={entity.entityId}");
        // Check if radio already exists
        if (radioSources.Any(r => r.EntityID == entity.entityId))
        {
            Log.Out($"[RM] Radio already exists for drone {entity.entityId}");
            return;
        }

        RadioSource source = new DroneRadioSource
        {
            Drone = entity,
            IsOn = false,
            ClipName = "",
            PlayListPosition = RadioPlaylistManager.Instance.PlaylistPosition,
            EntityID = entity.entityId,
            Name = entity.entityId.ToString()
        };

        Log.Out("[RM] Added Drone : " + entity.entityId);
        radioSources.Add(source);
        Log.Out($"[RM] Total radios={radioSources.Count}");
        RadioDebug.D("RM", $"AddRadio(Drone) total={radioSources.Count}");
        EnsureWatchdogStarted();
    }

    public void AddRadio(RiseRadio block)
    {
        RadioDebug.Enter("RM");
        if (block == null)
        {
            Log.Out("[RM] AddRadio(Block) called with null block");
            return;
        }
        // Stable name based on world and integer block coordinates
        string radioName = BuildRadioKey(block.blockPosition);
        
        Log.Out($"[RM] Attempting to add radio: {radioName} (blockID={block.blockID} pos={block.blockPosition})");
        Log.Out($"[RM] Current radio sources count: {radioSources.Count}");
        
        // Check if radio already exists
        if (radioSources.Any(r => r.Name == radioName))
        {
            Log.Out($"[RM] Radio already exists for block {radioName}");
            return;
        }

        BlockRadioSource source = new BlockRadioSource
        {
            Block = block,
            IsOn = false,
            ClipName = "",
            PlayListPosition = RadioPlaylistManager.Instance.PlaylistPosition,
            EntityID = 0,
            Position = block.blockPosition,
            Name = radioName
        };

        radioSources.Add(source);
        Log.Out($"[RM] Added Radio : key={radioName} at position {block.blockPosition} | Total={radioSources.Count}");
        RadioDebug.D("RM", $"AddRadio(Block) key={radioName} total={radioSources.Count}");
        
        for (int i = 0; i < radioSources.Count; i++)
        {
            Log.Out($"[RM] Radio[{i}] Name={radioSources[i].Name}, EntityID={radioSources[i].EntityID}, Position={radioSources[i].Position}, IsOn={radioSources[i].IsOn}, Clip='{radioSources[i].ClipName}'");
        }

        // Invoke lifecycle hook so the dedicated AudioSource is created and auto-resume happens if needed
        try
        {
            source.OnBlockLoadedHook();
            // Post-load inspection
            var asrc = source.AudioSourceObject;
            Log.Out($"[RM] PostLoad state for {source.Name}: IsOn={source.IsOn} Clip='{source.ClipName}' HasAS={(asrc!=null)} Playing={(asrc!=null && asrc.isPlaying)} pos={source.Position}");
            if (asrc != null)
            {
                var ac = asrc.clip;
                Log.Out($"[RM]  -> AS clip='{(ac!=null?ac.name:"<null>")}' len={(ac!=null?ac.length:0f):F2} time={(ac!=null?asrc.time:0f):F2}");
            }
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error in BlockRadioSource.OnBlockLoadedHook: {e.Message}");
            RadioDebug.E("RM", "OnBlockLoadedHook", e);
        }

        EnsureWatchdogStarted();
    }

    public void RemoveRadio(RiseRadio block)
    {
        // Backwards compatibility: removal not specifying destruction uses unload semantics
        RemoveRadio(block, destroyed:false);
    }

    public void RemoveRadio(RiseRadio block, bool destroyed)
    {
        RadioDebug.Enter("RM");
        if (block == null)
        {
            Log.Out("[RM] RemoveRadio(Block) called with null block");
            return;
        }
        string radioName = BuildRadioKey(block.blockPosition);
        Log.Out($"[RM] RemoveRadio called for: {radioName} destroyed={destroyed}");
        
        RadioSource radioSource = radioSources.Find(radio => radio.Name == radioName);
        if (radioSource != null)
        {
            Log.Out($"[RM] Found radio source to remove: {radioSource.Name}");

            if (!destroyed)
            {
                // Persist full resume state before any cleanup/stopping (unload path)
                SavePersistentStateFor(radioSource);
            }
            
            // Unregister from coordinator
            if (!string.IsNullOrEmpty(radioSource.ClipName))
            {
                Log.Out($"[RM] Unregistering radio from coordinator: {radioName}");
                RadioCoordinator.Instance.UnregisterRadioForTrack(radioSource.ClipName, radioSource);
            }

            // If this is a block radio, call proper hook
            try
            {
                if (radioSource is BlockRadioSource brs)
                {
                    if (destroyed)
                    {
                        // Ensure OFF and dispose; also remove persistence
                        brs.OnBlockDestroyedHook();
                        ClearPersistentState(brs.Position);
                    }
                    else
                    {
                        brs.OnBlockUnloadedHook();
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out($"[RM] Error in BlockRadioSource removal hook: {e.Message}");
                RadioDebug.E("RM", "RemovalHook", e);
            }
            
            radioSources.Remove(radioSource);
            Log.Out($"[RM] Removed and cleaned up radio: {radioName}");
            RadioDebug.D("RM", $"Removed {radioName} remaining={radioSources.Count}");
        }
        else
        {
            Log.Out($"[RM] Warning: Radio source not found for removal: {radioName}");
            Log.Out($"[RM] Current radio sources count: {radioSources.Count}");
            for (int i = 0; i < Math.Min(radioSources.Count, 5); i++)
            {
                Log.Out($"[RM]   Radio {i}: {radioSources[i].Name}");
            }
        }
    }

    public void RemoveRadio(Entity entity)
    {
        RadioDebug.Enter("RM");
        if (entity == null)
        {
            Log.Out("[RM] RemoveRadio(Entity) called with null entity");
            return;
        }
        Log.Out($"[RM] RemoveRadio(Entity) entityId={entity.entityId}");
        RadioSource radioSource = radioSources.Find(radio => radio.EntityID == entity.entityId);
        if (radioSource != null)
        {
            // Persist before teardown
            SavePersistentStateFor(radioSource);

            if (!string.IsNullOrEmpty(radioSource.ClipName))
            {
                RadioCoordinator.Instance.UnregisterRadioForTrack(radioSource.ClipName, radioSource);
            }
            
            // Entities can be stopped outright
            string currentTrack = CurrentClipName;
            if (!string.IsNullOrEmpty(currentTrack))
            {
                try { radioSource.Stop(currentTrack); } catch { }
            }
            
            radioSources.Remove(radioSource);
            Log.Out($"[RM] Removed and cleaned up entity radio: {entity.entityId}");
            RadioDebug.D("RM", $"Removed entity={entity.entityId} remaining={radioSources.Count}");
        }
    }

    public object GetRadio(object radioObj)
    {
        RadioDebug.Enter("RM");
        Log.Out("[RM] Entered GetRadio");
        RadioSource _radioSource = null;

        try
        {
            if (radioObj == null)
            {
                Log.Out("[RM] GetRadio called with null object");
                return null;
            }

            if (radioObj is RiseDrone)
            {
                RiseDrone drone = radioObj as RiseDrone;
                Log.Out($"[RM] GetRadio(Drone) id={drone.entityId} name={drone.name} Count={radioSources.Count}");

                _radioSource = radioSources.FirstOrDefault(source => source.EntityID == drone.entityId);
                if (_radioSource != null)
                {
                    Log.Out($"[RM] RadioSource Found : {_radioSource.Name}");
                }
            }
            else if (radioObj is RiseRadio)
            {
                RiseRadio block = radioObj as RiseRadio;
                Log.Out($"[RM] GetRadio(Block) id={block.blockID} pos={block.blockPosition}");
                string radioName = BuildRadioKey(block.blockPosition);
                _radioSource = radioSources.Find(radioSource => radioSource.Name == radioName);
                if (_radioSource != null)
                {
                    Log.Out($"[RM] Block RadioSource Found : {_radioSource.Name}");
                }
                else
                {
                    Log.Out($"[RM] Block RadioSource NOT Found for: {radioName}");
                }
            }
            else if (radioObj is Entity)
            {
                Entity entity = radioObj as Entity;
                Log.Out($"[RM] GetRadio(Entity) id={entity.entityId}");
                _radioSource = radioSources.Find(radioSource => radioSource.EntityID == entity.entityId);
            }
        }
        catch (Exception e)
        {
            Log.Out("[RM] Error finding radio source.");
            Log.Out($"[RM] Exception : {e.Message}");
            RadioDebug.E("RM", "GetRadio error", e);
        }

        return _radioSource;
    }

    #endregion

    #region Radio Playback Control

    public void PlayRadio(object obj)
    {
        RadioDebug.Enter("RM");
        Log.Out($"[RM] === RadioManager.PlayRadio called ===");
        if (obj == null)
        {
            Log.Out("[RM] PlayRadio called with null target");
            return;
        }
        Log.Out($"[RM] RadioManager Playing Radio : {obj.GetType().Name}");
        RadioSource source = GetRadio(obj) as RadioSource;
        if (source != null)
        {
            Log.Out($"[RM] Source Found : {source.EntityID}");
            Log.Out($"[RM] Source Type : {source.GetType().Name}");
            Log.Out($"[RM] Source Name : {source.Name}");
            Log.Out($"[RM] Source Position : {source.Position}");

            // Only initialize once to prevent infinite loop
            if (!RadioTrackData.Instance.IsLoaded() && !isInitializing)
            {
                Log.Out("[RM] Force initializing RadioManager data...");
                isInitializing = true;
                Init();
                isInitializing = false;
            }

            // Debug: Check if track data is loaded
            Log.Out($"[RM] RadioTrackData.IsLoaded(): {RadioTrackData.Instance.IsLoaded()}");
            Log.Out($"[RM] Total tracks available: {RadioTrackData.Instance.GetTotalTrackCount()}");

            // Get current track from playlist manager
            string trackToPlay = RadioPlaylistManager.Instance.GetCurrentTrack();
            
            Log.Out($"[RM] === TRACK SELECTION DEBUG ===");
            Log.Out($"[RM] GetCurrentTrack() returned: '{trackToPlay}'");
            Log.Out($"[RM] Playlist count: {RadioPlaylistManager.Instance.PlaylistCount}");
            Log.Out($"[RM] Playlist position: {RadioPlaylistManager.Instance.PlaylistPosition}");
            
            if (string.IsNullOrEmpty(trackToPlay))
            {
                Log.Out("[RM] *** ERROR: No track available to play! ***");
                Log.Out("[RM] Attempting to force playlist creation...");
                
                // Force playlist creation
                RadioPlaylistManager.Instance.CreatePlaylist();
                trackToPlay = RadioPlaylistManager.Instance.GetCurrentTrack();
                
                Log.Out($"[RM] After forced playlist creation: '{trackToPlay}'");
                Log.Out($"[RM] Playlist count after force: {RadioPlaylistManager.Instance.PlaylistCount}");
                
                if (string.IsNullOrEmpty(trackToPlay))
                {
                    Log.Out("[RM] *** CRITICAL ERROR: Still no track available after forced creation! ***");
                    return;
                }
            }

            Log.Out($"[RM] === FINAL TRACK TO PLAY: '{trackToPlay}' ===");
            RadioDebug.D("RM", $"Play track='{trackToPlay}' src={source.Name}");

            // Set central current clip now
            SetCurrentClip(trackToPlay);

            // Set position for block radios
            if (obj is RiseRadio)
            {
                RiseRadio block = obj as RiseRadio;
                Log.Out("[RM] Playing Radio : " + block.blockID);
                source.Position = block.blockPosition;

                // Ensure dedicated AudioSource exists (in case of delayed creation)
                try
                {
                    if (source is BlockRadioSource brs)
                    {
                        Log.Out("[RM] Calling OnBlockLoadedHook for BlockRadioSource");
                        brs.OnBlockLoadedHook();
                    }
                }
                catch (Exception hookEx)
                {
                    Log.Out($"[RM] Error in OnBlockLoadedHook: {hookEx.Message}");
                    RadioDebug.E("RM", "OnBlockLoadedHook", hookEx);
                }
            }

            // Play via radio source API (deterministic path)
            try
            {
                Log.Out($"[RM] === ATTEMPTING TO PLAY TRACK: '{trackToPlay}' ===");
                var gm = GameManager.Instance;
                if (gm != null)
                {
                    gm.StartCoroutine(DelayedPlay(source, trackToPlay));
                }
                else
                {
                    // Fallback if GameManager not yet available
                    source.Play(trackToPlay);
                    if (source.IsOn && !string.IsNullOrEmpty(trackToPlay))
                    {
                        try { RadioCoordinator.Instance.RegisterRadioForTrack(trackToPlay, source); } catch { }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out($"[RM] *** ERROR playing radio source: {e.Message}");
                Log.Out($"[RM] Stack trace: {e.StackTrace}");
                RadioDebug.E("RM", "PlayRadio schedule error", e);
            }
        }
        else
        {
            Log.Out("[RM] *** ERROR: No radio source found! ***");
            if (obj is RiseRadio riseRadio)
            {
                Log.Out($"[RM] RiseRadio details - blockID: {riseRadio.blockID}, position: {riseRadio.blockPosition}");
                string expectedName = BuildRadioKey(riseRadio.blockPosition);
                Log.Out($"[RM] Expected radio name: {expectedName}");
                
                Log.Out($"[RM] Current radio sources ({radioSources.Count}):\n");
                for (int i = 0; i < radioSources.Count; i++)
                {
                    var rs = radioSources[i];
                    Log.Out($"[RM]   [{i}] Name: '{rs.Name}', EntityID: {rs.EntityID}, Position: {rs.Position}");
                }
            }
        }
        
        Log.Out($"[RM] === RadioManager.PlayRadio completed ===");
    }

    private System.Collections.IEnumerator DelayedPlay(RadioSource source, string trackToPlay)
    {
        yield return new WaitForSeconds(0.1f);
        
        Log.Out($"[RM] DelayedPlay executing for track: '{trackToPlay}'");
        RadioDebug.D("RM", $"DelayedPlay '{trackToPlay}' for {source?.Name}");
        
        Exception playException = null;
        
        try
        {
            source.Play(trackToPlay);
        }
        catch (Exception e)
        {
            playException = e;
        }
        
        if (playException != null)
        {
            Log.Out($"[RM] *** ERROR in DelayedPlay: {playException.Message}");
            Log.Out($"[RM] Stack trace: {playException.StackTrace}");
            RadioDebug.E("RM", "DelayedPlay error", playException);
            yield break;
        }
        
        // Wait a moment then check the results
        yield return new WaitForSeconds(0.5f);
        
        Log.Out($"[RM] After source.Play() call: IsOn={source.IsOn} Clip='{source.ClipName}' HasAS={(source.AudioSourceObject!=null)}");
        
        if (source.AudioSourceObject != null)
        {
            var clip = source.AudioSourceObject.clip;
            Log.Out($"[RM]  - AS.isPlaying={source.AudioSourceObject.isPlaying} clip={(clip!=null)} clipName='{(clip!=null?clip.name:"<null>")}' clipLen={(clip!=null?clip.length:0f):F2}");
            if (clip != null)
            {
                // Keep RM length in sync if we own this clip
                if (clip.name == currentClipName || clip.name.Contains(currentClipName) || currentClipName.Contains(clip.name))
                {
                    UpdateCurrentClipLength(clip.length);
                }
            }
        }
        
        // Register with coordinator
        if (source.IsOn && !string.IsNullOrEmpty(trackToPlay))
        {
            RadioCoordinator.Instance.RegisterRadioForTrack(trackToPlay, source);
            Log.Out("[RM] ✓ Radio registered with coordinator");
        }
        else
        {
            Log.Out("[RM] *** ERROR: Radio failed to turn on after play command ***");
        }
    }

    public void StopRadio(object obj)
    {
        RadioDebug.Enter("RM");
        Log.Out("[RM] === RadioManager.StopRadio called ===");
        if (obj == null)
        {
            Log.Out("[RM] StopRadio called with null target");
            return;
        }
        Log.Out("[RM] Stopping Radio : " + obj.GetType().Name);

        RadioSource _radioSource = GetRadio(obj) as RadioSource;

        if (_radioSource != null)
        {
            Log.Out("[RM] Source Found : " + _radioSource.EntityID);
            Log.Out($"[RM] Radio Source Name: {_radioSource.Name}");
            Log.Out($"[RM] Radio Source IsOn: {_radioSource.IsOn}");
            Log.Out($"[RM] Radio Source ClipName: {_radioSource.ClipName}");
            Log.Out($"[RM] Radio Source Position: {_radioSource.Position}");
            
            Log.Out($"[RM] Current playlist count: {RadioPlaylistManager.Instance.PlaylistCount}");
            Log.Out($"[RM] Playlist position: {RadioPlaylistManager.Instance.PlaylistPosition}");
            Log.Out($"[RM] Current track: {CurrentClipName}");
            
            // Persist current state before stopping
            SavePersistentStateFor(_radioSource);
            
            if (!string.IsNullOrEmpty(_radioSource.ClipName))
            {
                Log.Out($"[RM] Unregistering radio from track: {_radioSource.ClipName}");
                RadioCoordinator.Instance.UnregisterRadioForTrack(_radioSource.ClipName, _radioSource);
            }
            
            string currentTrack = CurrentClipName;
            if (!string.IsNullOrEmpty(currentTrack))
            {
                Log.Out($"[RM] Calling radioSource.Stop with track: {currentTrack}");
                _radioSource.Stop(currentTrack);
                Log.Out($"[RM] radioSource.Stop completed");
            }
            else
            {
                Log.Out("[RM] No valid track to stop - setting IsOn to false directly");
                _radioSource.IsOn = false;
            }
            
            _radioSource.ClipName = "";
            
            int remainingActiveRadios = radioSources.Count(r => r.IsOn);
            Log.Out($"[RM] Radio stopped successfully. Remaining radios on: {remainingActiveRadios}");
            RadioDebug.D("RM", $"StopRadio remaining={remainingActiveRadios}");
            
            if (remainingActiveRadios == 0)
            {
                // Clear canonical current clip and also clear playlist's notion for consistency
                ClearCurrentClip();
                RadioPlaylistManager.Instance.ClearCurrentTrack();
            }
        }
        else
        {
            Log.Out("[RM] No radio source found to stop!");
            
            Log.Out($"[RM] Current radio sources count: {radioSources.Count}");
            for (int i = 0; i < radioSources.Count; i++)
            {
                var radio = radioSources[i];
                Log.Out($"[RM] Radio {i}: Name={radio.Name}, EntityID: {radio.EntityID}, IsOn={radio.IsOn}");
            }
        }
        
        Log.Out("[RM] === RadioManager.StopRadio completed ===");
    }

    /// <summary>
    /// Force stops all active radios (for console command)
    /// </summary>
    public void ForceStopAllRadios()
    {
        try
        {
            RadioDebug.Enter("RM");
            Log.Out("[RM] === FORCE STOPPING ALL RADIOS ===");
            
            var activeRadios = radioSources.Where(r => r.IsOn).ToList();
            Log.Out($"[RM] Found {activeRadios.Count} active radios to stop");
            RadioDebug.D("RM", $"ForceStopAll count={activeRadios.Count}");
            
            string currentTrack = CurrentClipName;
            
            foreach (var radio in activeRadios)
            {
                try
                {
                    // Persist before stopping
                    SavePersistentStateFor(radio);

                    Log.Out($"[RM] Force stopping radio: {radio.Name}");
                    
                    if (!string.IsNullOrEmpty(radio.ClipName))
                    {
                        RadioCoordinator.Instance.UnregisterRadioForTrack(radio.ClipName, radio);
                    }
                    
                    if (!string.IsNullOrEmpty(currentTrack))
                    {
                        radio.Stop(currentTrack);
                    }
                    else
                    {
                        radio.IsOn = false;
                    }
                    
                    radio.ClipName = "";
                    
                    Log.Out($"[RM] ✓ Stopped radio: {radio.Name}");
                }
                catch (Exception e)
                {
                    Log.Out($"[RM] ✗ Error stopping radio {radio.Name}: {e.Message}");
                    RadioDebug.E("RM", $"ForceStopAll error for {radio.Name}", e);
                }
            }
            
            // Clear canonical and playlist states
            ClearCurrentClip();
            RadioPlaylistManager.Instance.ClearCurrentTrack();
            
            Log.Out($"[RM] === FORCE STOP COMPLETED ===");
            Log.Out($"[RM] Stopped {activeRadios.Count} radios");
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error in ForceStopAllRadios: {e.Message}");
            RadioDebug.E("RM", "ForceStopAll error", e);
        }
    }

    #endregion

    #region Track Management

    public void SkipToNextTrack()
    {
        try
        {
            RadioDebug.Enter("RM");
            Log.Out("[RM] === MANUAL TRACK SKIP REQUESTED ===");
            
            VerifyAndUpdateRadioStates();
            
            var currentlyOn = radioSources.Where(r => r.IsOn).ToList();            
            if (currentlyOn.Count == 0)
            {
                Log.Out("[RM] No active radios to skip");
                return;
            }
            
            string previousTrack = CurrentClipName;
            string nextTrack = RadioPlaylistManager.Instance.SkipToNextTrack();
            Log.Out($"[RM] SkipToNextTrack returned next='{nextTrack}' prev='{previousTrack}'");
            RadioDebug.D("RM", $"SkipToNext prev='{previousTrack}' next='{nextTrack}'");
            
            if (string.IsNullOrEmpty(nextTrack))
            {
                Log.Out("[RM] No next track available");
                return;
            }

            // Update centralized current clip
            SetCurrentClip(nextTrack);

            VerifyAndUpdateRadioStates();
            
            var radiosToChange = GetRadiosNeedingTrackChange(previousTrack);

            foreach (var r in radioSources)
            {
                if (r != null && r.IsParentValid() && r.IsOn && !radiosToChange.Any(x => x.Name == r.Name))
                {
                    radiosToChange.Add(r);
                }
            }
            
            Log.Out($"[RM] Radios selected for change: {radiosToChange.Count}");
            
            PerformDeterministicSwap(radiosToChange, previousTrack, nextTrack);
            
            Log.Out("[RM] Manual track skip initiated");
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error in manual track skip: {e.Message}");
            RadioDebug.E("RM", "SkipToNextTrack error", e);
        }
    }

    public float GetCurrentTrackRemainingTime()
    {
        try
        {
            var activeRadios = radioSources.Where(r => r.IsOn && r.AudioSourceObject != null && r.AudioSourceObject.clip != null).ToList();
            
            if (activeRadios.Count == 0)
            {
                return 0f;
            }
            
            float maxRemaining = 0f;
            foreach (var radio in activeRadios)
            {
                if (radio.AudioSourceObject.isPlaying)
                {
                    float remaining = radio.AudioSourceObject.clip.length - radio.AudioSourceObject.time;
                    if (remaining > maxRemaining)
                    {
                        maxRemaining = remaining;
                    }
                }
            }
            
            return maxRemaining;
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error getting current track remaining time: {e.Message}");
            return 0f;
        }
    }

    private void PerformTrackChange(List<RadioSource> activeRadios, string previousTrack, string nextTrack)
    {
        PerformDeterministicSwap(activeRadios, previousTrack, nextTrack);
    }

    private void PerformDeterministicSwap(List<RadioSource> activeRadios, string previousTrack, string nextTrack)
    {
        activeRadios = activeRadios.Where(r => r != null && r.IsParentValid()).ToList();
        if (activeRadios.Count == 0) return;

        double dspStart = AudioSettings.dspTime + 0.10; // 100ms ahead
        float syncT = ComputeGlobalSyncTime(nextTrack);

        Log.Out($"[RM] Deterministic swap -> next='{nextTrack}' radios={activeRadios.Count} dspStart={dspStart:F3} syncT={syncT:F2}");
        RadioDebug.D("RM", $"Swap prev='{previousTrack}' next='{nextTrack}' count={activeRadios.Count} syncT={syncT:F2}");

        foreach (var r in activeRadios)
        {
            try
            {
                r.SwapClip(nextTrack, syncT, dspStart);
            }
            catch (Exception e)
            {
                Log.Out($"[RM] SwapClip error on {r.Name}: {e.Message}");
            }
        }
    }

    private float ComputeGlobalSyncTime(string clip)
    {
        try
        {
            // Registry-first sync time from any radio already playing the target clip
            var cand = radioSources.FirstOrDefault(r => r != null && r.IsOn && r.AudioSourceObject != null && r.AudioSourceObject.clip != null &&
                                                        (r.AudioSourceObject.clip.name == clip || r.AudioSourceObject.clip.name.Contains(clip) || clip.Contains(r.AudioSourceObject.clip.name)));
            if (cand != null)
            {
                return Mathf.Clamp(cand.AudioSourceObject.time, 0f, cand.AudioSourceObject.clip.length - 0.05f);
            }
        }
        catch { }
        return 0f;
    }

    private float CurrentSyncTime()
    {
        string current = CurrentClipName;
        return string.IsNullOrEmpty(current) ? 0f : ComputeGlobalSyncTime(current);
    }

    public string GetCurrentTrackInfo()
    {
        try
        {
            int activeRadios = radioSources.Count(r => r.IsOn);
            string playlistInfo = RadioPlaylistManager.Instance.GetCurrentTrackInfo();
            
            return $"{playlistInfo} | Active Radios: {activeRadios}";
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error getting current track info: {e.Message}");
            return "Error retrieving track info";
        }
    }

    #endregion

    #region Coroutines

    private System.Collections.IEnumerator DelayedSync(string trackName, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        try
        {
            Log.Out($"[RM] Performing delayed sync for track: {trackName}");
            // registry-first sync: radios will self-sync via coordinator
            RadioCoordinator.Instance.ForceSyncTrack(trackName);
            Log.Out($"[RM] Delayed sync completed for track: {trackName}");
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error in delayed sync: {e.Message}");
        }
    }
    
    private System.Collections.IEnumerator DelayedSyncAfterAdvancement(string trackName, float delay)
    {
        Log.Out($"[RM] Starting delayed sync after advancement for track: {trackName} (delay: {delay}s)");
        yield return new WaitForSeconds(delay);
        
        try
        {
            Log.Out($"[RM] Performing post-advancement sync for track: {trackName}");
            RadioCoordinator.Instance.ForceSyncTrack(trackName);
            Log.Out($"[RM] Post-advancement sync completed for track: {trackName}");
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error in post-advancement sync: {e.Message}");
        }
    }

    private IEnumerator AudioWatchdog()
    {
        var wait = new WaitForSeconds(2f);
        while (true)
        {
            string current = CurrentClipName;
            float syncT = CurrentSyncTime();
            foreach (var r in radioSources.ToList())
            {
                try
                {
                    if (r == null || !r.IsParentValid()) continue;
                    if (!r.IsOn) continue;
                    var src = r.AudioSourceObject;
                    bool needsHeal = (src == null || !src || !src.isPlaying || (src.clip == null && !string.IsNullOrEmpty(current)));
                    if (!needsHeal && !string.IsNullOrEmpty(current) && src.clip != null)
                    {
                        string c = src.clip.name;
                        if (!(c == current || c.Contains(current) || current.Contains(c)))
                            needsHeal = true;
                    }

                    if (needsHeal)
                    {
                        Log.Out($"[RM][Watchdog] Healing radio {r.Name} (IsOn={r.IsOn})");
                        RadioDebug.D("RM-WATCH", $"heal {r.Name} track='{current}' t={syncT:F2}");
                        if (string.IsNullOrEmpty(current) && !string.IsNullOrEmpty(r.ClipName))
                        {
                            r.ReinitAndRestart(r.ClipName, 0f);
                        }
                        else if (!string.IsNullOrEmpty(current))
                        {
                            r.ReinitAndRestart(current, syncT);
                        }
                    }
                }
                catch (Exception e)
                {
                    Log.Out($"[RM][Watchdog] Error: {e.Message}");
                }
            }
            yield return wait;
        }
    }

    private void EnsureWatchdogStarted()
    {
        if (watchdogStarted) return;
        try
        {
            var gm = GameManager.Instance;
            if (gm == null)
            {
                Log.Out("[RM] GameManager not yet available; watchdog start deferred");
                return;
            }
            gm.StartCoroutine(AudioWatchdog());
            watchdogStarted = true;
            Log.Out("[RM] AudioWatchdog started");
            RadioDebug.D("RM", "Watchdog started");
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Failed to start AudioWatchdog: {e.Message}");
            RadioDebug.E("RM", "Watchdog start error", e);
        }
    }

    #endregion

    #region Initialization and Update

    public void Init()
    {
        try
        {
            RadioDebug.Enter("RM");
            Log.Out("[RM] RadioManager.Init() called");
            
            if (isInitializing)
            {
                Log.Out("[RM] RadioManager initialization already in progress");
                return;
            }
            
            isInitializing = true;
            
            RadioTrackData.Instance.LoadXmlRadioData();
            RadioPlaylistManager.Instance.CreatePlaylist();
            
            Log.Out("[RM] RadioManager initialization completed");
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error in RadioManager.Init(): {e.Message}");
            RadioDebug.E("RM", "Init error", e);
        }
        finally
        {
            isInitializing = false;
        }
    }
    
    public void Update()
    {
        RadioDebug.Enter("RM");
        var gm = GameManager.Instance;
        if (gm == null) return;
        var world = gm.World;
        if (world == null) return;
        if (world.worldTime <= 0) return;
        if (gm.IsPaused()) return;

        if (!RadioTrackData.Instance.IsLoaded() && !isInitializing)
        {
            Init();
        }

        ReapInvalidRadios();

        if (Time.time - lastProximityCheckTime >= PROXIMITY_CHECK_INTERVAL)
        {
            lastProximityCheckTime = Time.time;
            try { ProximityActivateRadios(); } catch (Exception e) { Log.Out($"[RM] Proximity activation error: {e.Message}"); }
        }
        
        if (Time.time - lastTrackCheckTime >= TRACK_CHECK_INTERVAL)
        {
            lastTrackCheckTime = Time.time;
            
            VerifyAndUpdateRadioStates();
            
            var activeRadios = radioSources.Where(r => r.IsOn).ToList();
            string currentTrack = CurrentClipName;            
            
            if (activeRadios.Count > 0)
            {
                if (!string.IsNullOrEmpty(currentTrack))
                {
                    bool shouldAdvance = RadioPlaylistManager.Instance.ShouldAdvanceTrack(activeRadios);
                    Log.Out($"[RM] ShouldAdvance={shouldAdvance}");
                    
                    if (shouldAdvance)
                    {
                        Log.Out("[RM] === TRACK ADVANCEMENT TRIGGERED ===");
                        Log.Out($"[RM] Current track: {currentTrack}");
                        Log.Out($"[RM] Active radios: {activeRadios.Count}");
                        
                        string previousTrack = currentTrack;
                        string nextTrack = RadioPlaylistManager.Instance.AdvanceToNextTrack();
                        Log.Out($"[RM] AdvanceToNextTrack returned '{nextTrack}' (prev='{previousTrack}')");
                        
                        if (!string.IsNullOrEmpty(nextTrack) && nextTrack != previousTrack)
                        {
                            Log.Out("[RM] === AUTO TRACK ADVANCEMENT ===");
                            Log.Out($"[RM] Advancing from '{previousTrack}' to '{nextTrack}'");
                            // Update centralized state first
                            SetCurrentClip(nextTrack);
                            var radiosToChange = GetRadiosNeedingTrackChange(previousTrack);
                            Log.Out($"[RM] Radios selected for change (auto): {radiosToChange.Count}");
                            PerformDeterministicSwap(radiosToChange, previousTrack, nextTrack);
                        }
                        else
                        {
                            Log.Out($"[RM] Track advancement failed - nextTrack: '{nextTrack}', previousTrack: '{previousTrack}'");
                        }
                    }
                }
                else
                {
                    if (Time.time % 30f < TRACK_CHECK_INTERVAL)
                    {
                        Log.Out($"[RM] Warning: {activeRadios.Count} active radios but no current track name");
                    }
                }
            }
            else
            {
                // No loaded active radios; consider headless advancement if logical radios exist
                if (!string.IsNullOrEmpty(currentTrack))
                {
                    bool anyPersistedOn = false;
                    try { anyPersistedOn = persistedStates.Any(kv => kv.Value.IsOn); } catch { anyPersistedOn = false; }

                    if (anyPersistedOn)
                    {
                        bool shouldAdvanceHeadless = RadioPlaylistManager.Instance.ShouldAdvanceWithoutRadios();
                        Log.Out($"[RM] Headless check: anyPersistedOn={anyPersistedOn} shouldAdvance={shouldAdvanceHeadless}");

                        if (shouldAdvanceHeadless)
                        {
                            string previousTrack = currentTrack;
                            string nextTrack = RadioPlaylistManager.Instance.AdvanceToNextTrack();
                            Log.Out($"[RM] Headless advancement: '{previousTrack}' -> '{nextTrack}'");
                            if (!string.IsNullOrEmpty(nextTrack))
                            {
                                SetCurrentClip(nextTrack);
                            }
                            // No radios to swap; playlist state advanced and will apply on next activation/load
                        }
                    }
                    else
                    {
                        Log.Out("[RM] No active or logical radios detected, preserving current track for proximity reactivation");
                    }
                }
            }
        }
        
        if (Time.time - lastSyncTime >= SYNC_INTERVAL)
        {
            lastSyncTime = Time.time;
            
            try
            {
                ReapInvalidRadios();
            }
            catch (Exception e)
            {
                Log.Out($"[RM] Error during radio cleanup: {e.Message}");
            }
        }
        
        if (Time.time - lastCleanupTime >= CLEANUP_INTERVAL)
        {
            lastCleanupTime = Time.time;
            
            try
            {
                RadioCoordinator.Instance.CleanupOrphanedRadios();
                UpdateRadioBlockPersistentStates();
                CleanupOldPersistedStates();
            }
            catch (Exception e)
            {
                Log.Out($"[RM] Error in periodic cleanup: {e.Message}");
            }
        }
    }
    
    #endregion

    #region Tile Entity State Management

    private void UpdateRadioBlockPersistentStates()
    {
        try
        {
            int updatedCount = 0;
            
            var radioBlocks = radioSources.Where(r => r is BlockRadioSource).Cast<BlockRadioSource>();
            
            foreach (var radioSource in radioBlocks)
            {
                try
                {
                    if (radioSource.Block != null && radioSource.IsOn)
                    {
                        SavePersistentStateFor(radioSource);
                        updatedCount++;
                    }
                }
                catch (Exception e)
                {
                    Log.Out($"[RM] Error updating persistent state for radio {radioSource.Name}: {e.Message}");
                }
            }
            
            if (updatedCount > 0)
            {
                Log.Out($"[RM] Updated persistent state for {updatedCount} active radio blocks");
                RadioDebug.D("RM-PERSIST", $"updated active blocks={updatedCount}");
            }
            
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error in UpdateRadioBlockPersistentStates: {e.Message}");
        }
    }

    #endregion

    #region Radio State Verification

    private void VerifyAndUpdateRadioStates()
    {
        try
        {
            int stateUpdates = 0;
            string currentTrack = CurrentClipName;
            
            foreach (var radio in radioSources)
            {
                try
                {
                    var src = radio.AudioSourceObject;
                    bool actuallyPlaying = (src != null && src.clip != null && src.isPlaying);

                    if (actuallyPlaying && !radio.IsOn)
                    {
                        radio.IsOn = true;
                        radio.ClipName = src.clip != null ? src.clip.name : radio.ClipName;
                        if (radio is BlockRadioSource blockRadio && blockRadio.Block != null)
                        {
                            blockRadio.Block.SetRadioOn(true);
                        }
                        // Keep central clip length up to date if this matches current
                        if (!string.IsNullOrEmpty(currentTrack) && src.clip != null)
                        {
                            string c = src.clip.name;
                            if (c == currentTrack || c.Contains(currentTrack) || currentTrack.Contains(c))
                            {
                                UpdateCurrentClipLength(src.clip.length);
                            }
                        }
                        stateUpdates++;
                    }
                    else if (!actuallyPlaying && radio.IsOn)
                    {
                        // Keep IsOn to allow proximity recovery; watchdog will heal
                    }
                }
                catch (Exception radioEx)
                {
                    Log.Out($"[RM] Error verifying state for radio {radio.Name}: {radioEx.Message}");
                }
            }
            
            if (stateUpdates > 0)
            {
                Log.Out($"[RM][STATE FIX] Updated IsOn state for {stateUpdates} radios");
                RadioDebug.D("RM", $"state fixes={stateUpdates}");
            }
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error in VerifyAndUpdateRadioStates: {e.Message}");
        }
    }

    private List<RadioSource> GetRadiosNeedingTrackChange(string previousTrack)
    {
        var result = new List<RadioSource>();
        try
        {
            Log.Out($"[RM] GetRadiosNeedingTrackChange: previousTrack='{previousTrack}', total radios={radioSources.Count}");
            
            var onRadios = radioSources.Where(r => r != null && r.IsParentValid() && r.IsOn).ToList();
            foreach (var radio in onRadios) result.Add(radio);

            if (!string.IsNullOrEmpty(previousTrack))
            {
                var clipNameRadios = radioSources.Where(r => r != null && r.IsParentValid() && 
                    !string.IsNullOrEmpty(r.ClipName) && 
                    (r.ClipName == previousTrack || r.ClipName.Contains(previousTrack) || previousTrack.Contains(r.ClipName)) &&
                    !result.Any(x => x.Name == r.Name)).ToList();
                
                foreach (var radio in clipNameRadios) result.Add(radio);
            }

            return result.Where(r => r != null && r.IsParentValid()).ToList();
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error collecting radios for track change: {e.Message}");
            Log.Out($"[RM] Stack trace: {e.StackTrace}");
            return new List<RadioSource>();
        }
    }

    #endregion

    #region Reaping

    private void ReapInvalidRadios()
    {
        try
        {
            if (radioSources.Count == 0) return;

            string currentTrack = CurrentClipName;
            var toRemove = new List<RadioSource>();

            foreach (var r in radioSources)
            {
                if (r == null || !r.IsParentValid())
                {
                    try
                    {
                        if (r != null)
                        {
                            if (r is BlockRadioSource brs)
                            {
                                // Treat unknown invalidation as an unload by default to preserve state
                                // Only an explicit destroyed path should clear persistence
                                if (brs.TemporarilyUnloaded)
                                {
                                    Log.Out($"[RM] Reaping temporarily unloaded radio (preserving state): {brs.Name}");
                                }
                                else
                                {
                                    Log.Out($"[RM] Reaping invalid block radio as UNLOAD (preserving state): {brs.Name}");
                                    try { brs.OnBlockUnloadedHook(); } catch {}
                                    // Do NOT ClearPersistentState here; preserve resume info
                                }
                            }
                            else
                            {
                                // Persist before teardown (captures real clip/time if available)
                                SavePersistentStateFor(r);

                                // Unregister from coordinator now (before any stop)
                                if (!string.IsNullOrEmpty(r?.ClipName))
                                {
                                    try { RadioCoordinator.Instance.UnregisterRadioForTrack(r.ClipName, r); } catch { }
                                }

                                // Non-block radios can be stopped outright
                                if (r.IsOn && !string.IsNullOrEmpty(currentTrack))
                                {
                                    try { r.Stop(currentTrack); } catch { }
                                }
                            }
                        }
                    }
                    finally
                    {
                        toRemove.Add(r);
                    }
                }
            }

            if (toRemove.Count > 0)
            {
                foreach (var r in toRemove)
                {
                    radioSources.Remove(r);
                }
                Log.Out($"[RM] Reaped {toRemove.Count} invalid radios");
                RadioDebug.D("RM", $"reaped={toRemove.Count}");
            }
        }
        catch (Exception e)
        {
            Log.Out($"[RM] Error in ReapInvalidRadios: {e.Message}");
        }
    }

    private void ProximityActivateRadios()
    {
        try
        {
            if (GameManager.IsDedicatedServer)
                return;

            var world = GameManager.Instance != null ? GameManager.Instance.World : null;
            if (world == null)
                return;

            EntityPlayerLocal player = world.GetPrimaryPlayer();
            if (player == null)
                return;

            Vector3 playerPos = player.position;
            
            string currentTrack = CurrentClipName;

            for (int i = 0; i < radioSources.Count; i++)
            {
                var r = radioSources[i];
                if (r == null || !r.IsParentValid()) continue;
                if (!r.IsOn) continue;

                string track = !string.IsNullOrEmpty(r.ClipName) ? r.ClipName : currentTrack;
                if (string.IsNullOrEmpty(track)) continue;

                float dist = Vector3.Distance(playerPos, r.Position);
                if (dist > HEARING_RADIUS) continue;

                var src = r.AudioSourceObject;
                bool needsReissue = (src == null || src.clip == null || !src.isPlaying);

                if (needsReissue)
                {
                    TryServerReissuePlay(r.Position, track);
                    RadioDebug.D("RM-PROX", $"reissue {r.Name} dist={dist:F1} track='{track}'");
                }
                else
                {
                    r.IsOn = true;
                }
            }
        }
        catch (Exception e)
        {
            Log.Out($"[RM] ProximityActivateRadios error: {e.Message}");
        }
    }

    private void TryServerReissuePlay(Vector3 position, string clip)
    {
        try
        {
            bool isServer = GameManager.IsDedicatedServer || (ConnectionManager.Instance != null && ConnectionManager.Instance.IsServer);
            bool isSingle = ConnectionManager.Instance != null && ConnectionManager.Instance.IsSinglePlayer;
            if (isServer || isSingle)
            {
                try { Manager.Stop(position, clip); } catch { }
                Manager.Play(position, clip);
            }
            else
            {
                Manager.Play(position, clip);
            }
        }
        catch (Exception e)
        {
            Log.Out($"[RM] TryServerReissuePlay error: {e.Message}");
        }
    }

    #endregion // Reaping
}