using System;
using System.Collections.Generic;
using System.Linq;
using Audio;
using Platform;
using UnityEngine;
using UnityEngine.Scripting;
using static LightingAround;
using Rise.Radio;

[Preserve]
public class RiseRadio : RiseMasterBlock
{
    private readonly BlockActivationCommand[] cmds =
    {
        new BlockActivationCommand("Turn On", "on", false),
        new BlockActivationCommand("Turn On", "on", false),
        new BlockActivationCommand("Turn Off", "off", false),
        new BlockActivationCommand("Take", "hand", false)
    };

    private float TakeDelay = 0;
    private float AllowPickup = 0;
    private bool LootContainer = false;

    EntityPlayer localPlayer;

    bool radioOn = false;
    private RadioManager radioManager;
    public Vector3 blockPosition { get; set; }
    public string RadioName { get; set; }
    
    // Legacy AudioSource fields retained for compatibility but no longer used for playback
    public AudioSource AudioSourceObject { get; set; }
    private GameObject audioGameObject;
    private string currentPlayingClip = "";

    public RiseRadio()
    {
        HasTileEntity = true;
    }

    /// <summary>
    /// Build radio key using world position (consistent with RadioManager)
    /// </summary>
    private string BuildRadioKey(Vector3 position)
    {
        var bp = ToBlockPos(position);
        string worldName = GetWorldName();
        return $"{worldName}|{bp.x}|{bp.y}|{bp.z}";
    }

    private static string GetWorldName()
    {
        try { return GamePrefs.GetString(EnumGamePrefs.GameName) ?? "unknown"; } catch { return "unknown"; }
    }

    private static Vector3i ToBlockPos(Vector3 pos)
    {
        return new Vector3i(Mathf.RoundToInt(pos.x), Mathf.RoundToInt(pos.y), Mathf.RoundToInt(pos.z));
    }

    /// <summary>
    /// Expose a read API so the radio source can restore accurately.
    /// Now delegates to RadioManager central persistence store.
    /// </summary>
    public bool TryGetPersistentState(out bool isOn, out string clip, out float time, out int playlistPos)
    {
        try
        {
            if (radioManager == null) radioManager = RadioManager.Instance;
            bool found = radioManager.TryGetPersistentState(blockPosition, out isOn, out clip, out time, out playlistPos);
            return found;
        }
        catch
        {
            isOn = false; clip = ""; time = 0f; playlistPos = 0; return false;
        }
    }

    public override void Init()
    {
        base.Init();

        radioManager = RadioManager.Instance;
        TakeDelay = 2f;
        Properties.ParseFloat("AllowPickup", ref AllowPickup);
        Properties.ParseFloat("TakeDelay", ref TakeDelay);
        Properties.ParseBool("LootContainer", ref LootContainer);
        IsNotifyOnLoadUnload = true;
    }

    // Only fires if IsNotifyOnLoadUnload is set to true
    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);

        blockPosition = _blockPos.ToVector3();        
        RadioName = BuildRadioKey(blockPosition); // Use consistent world position key

        
        // Ensure we have a RadioManager instance
        if (radioManager == null)
        {
            radioManager = RadioManager.Instance;
        }
        
        // Restore radio state from RadioManager persistence
        try
        {
            bool isOn; string clip; float t; int pos;
            if (radioManager.TryGetPersistentState(blockPosition, out isOn, out clip, out t, out pos))
            {
                radioOn = isOn;
                currentPlayingClip = clip;
            }
            else
            {
                radioOn = false;
            }
        }
        catch (Exception ex)
        {
            Log.Out($"Error restoring state from RadioManager: {ex.Message}");
        }
        
        // Register with RadioManager (which creates the BlockRadioSource and its dedicated AudioSource)
        radioManager.AddRadio(this);
        
        // Do NOT start playback directly here; BlockRadioSource will resume/sync in its load hook
        if (radioOn)
        {
            Log.Out($"Radio {blockID} is marked as ON, BlockRadioSource will resume if needed");
        }
        else
        {
            Log.Out($"Radio {blockID} is marked as OFF");
        }
    }

    // Only fires if IsNotifyOnLoadUnload is set to true
    public override void OnBlockUnloaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockUnloaded(_world, _clrIdx, _blockPos, _blockValue);
        
        // Store the specific position for this block before cleanup
        Vector3 unloadPosition = _blockPos.ToVector3();
        string unloadRadioName = BuildRadioKey(unloadPosition); // Use consistent world position key
        
       
        bool isOurBlock = (blockPosition == unloadPosition) || 
                         (Vector3.Distance(blockPosition, unloadPosition) < 0.1f);
        Log.Out($"Is our block: {isOurBlock}");
        
        if (isOurBlock)
        {
            // Save current radio state to RadioManager BEFORE stopping/cleanup
            float currentPlaybackTime = 0f;
            int currentPlaylistPosition = 0;
            string clip = currentPlayingClip;
            
            try
            {
                var rs = radioManager != null ? radioManager.GetRadio(this) as RadioSource : null;
                if (rs != null)
                {
                    // Pull authoritative clip/time from RadioSource if available
                    if (!string.IsNullOrEmpty(rs.ClipName))
                    {
                        clip = rs.ClipName;
                    }
                    if (rs.AudioSourceObject != null && rs.AudioSourceObject.clip != null)
                    {
                        currentPlaybackTime = rs.AudioSourceObject.time;
                    }
                }
                currentPlaylistPosition = RadioPlaylistManager.Instance.PlaylistPosition;
            }
            catch { }
            
            try
            {
                radioManager.SaveBlockPersistentState(blockPosition, radioOn, clip, currentPlaybackTime, currentPlaylistPosition);                
            }
            catch (Exception ex)
            {
                Log.Out($"Error persisting to RadioManager on unload: {ex.Message}");
            }
            
            // Remove from RadioManager (which will destroy the dedicated AudioSource via hook)
            if (radioManager != null)
            {
                Log.Out($"Removing radio from RadioManager: {unloadRadioName}");
                radioManager.RemoveRadio(this, destroyed:false);
            }
        }
        else
        {
            Log.Out($"Not our block, skipping cleanup for position mismatch");
        }
    }
    
    // The following legacy AudioSource management methods are no-ops now,
    // kept for backward compatibility with any external references.
    private void CreateAudioSource() { }
    private void CleanupAudioSource() { }
    
    /// <summary>
    /// Plays an audio clip using RadioManager/BlockRadioSource. Kept for compatibility.
    /// </summary>
    public void PlayAudioClip(String clipName, float startTime = 0f)
    {
        try
        {
            currentPlayingClip = clipName;
            if (radioManager == null) radioManager = RadioManager.Instance;
            // Ensure registered
            radioManager.AddRadio(this);
            // Let RadioManager choose current playlist track; or directly update clip through BlockRadioSource
            var rs = radioManager.GetRadio(this) as BlockRadioSource;
            if (rs != null)
            {
                if (!string.IsNullOrEmpty(clipName))
                {
                    rs.UpdateClip(clipName);
                }
                else
                {
                    radioManager.PlayRadio(this);
                }
            }
        }
        catch (Exception e)
        {
            Log.Out($"Error in PlayAudioClip (compat): {e.Message}");
        }
    }
    
    /// <summary>
    /// Stops audio via manager (compat shim)
    /// </summary>
    public void StopAudioClip()
    {
        try
        {
            if (radioManager == null) radioManager = RadioManager.Instance;
            radioManager.StopRadio(this);
            currentPlayingClip = "";
        }
        catch (Exception e)
        {
            Log.Out($"Error in StopAudioClip (compat): {e.Message}");
        }
    }
    
    public void ForceLocalPlayback(String clipName, float startTime = 0f)
    {
        // Delegate to BlockRadioSource
        PlayAudioClip(clipName, startTime);
    }
    
    private AudioClip LoadAudioClip(String clipName)
    {
        // No longer used here
        return null;
    }
    
    public bool IsAudioPlaying()
    {
        try
        {
            var rs = radioManager != null ? radioManager.GetRadio(this) as RadioSource : null;
            return rs != null && rs.AudioSourceObject != null && rs.AudioSourceObject.isPlaying;
        }
        catch { return false; }
    }
    
    public float GetAudioTime()
    {
        try
        {
            var rs = radioManager != null ? radioManager.GetRadio(this) as RadioSource : null;
            return (rs != null && rs.AudioSourceObject != null) ? rs.AudioSourceObject.time : 0f;
        }
        catch { return 0f; }
    }
    
    public void SetAudioTime(float time)
    {
        try
        {
            var rs = radioManager != null ? radioManager.GetRadio(this) as RadioSource : null;
            if (rs != null && rs.AudioSourceObject != null && rs.AudioSourceObject.clip != null)
            {
                rs.AudioSourceObject.time = Mathf.Clamp(time, 0f, rs.AudioSourceObject.clip.length - 0.1f);
            }
        }
        catch { }
    }

    public bool IsRadioOn()
    {
        return radioOn;
    }

    public void SetRadioOn(bool isOn)
    {
        bool stateChanged = (radioOn != isOn);
        radioOn = isOn;               
        
        if (stateChanged)
        {
            try
            {
                float currentTime = 0f;
                int playlistPos = 0;
                string clip = radioManager.GetCurrentTrackInfo();
                
                try
                {
                    var rs = radioManager != null ? radioManager.GetRadio(this) as RadioSource : null;
                    if (isOn && rs != null)
                    {
                        if (!string.IsNullOrEmpty(rs.ClipName))
                            clip = rs.ClipName;
                        if (rs.AudioSourceObject != null && rs.AudioSourceObject.clip != null)
                        {
                            currentTime = rs.AudioSourceObject.time;
                        }
                        playlistPos = RadioPlaylistManager.Instance.PlaylistPosition;
                    }
                }
                catch { }
                
                if (radioManager == null) radioManager = RadioManager.Instance;
                radioManager.SaveBlockPersistentState(blockPosition, isOn, clip, currentTime, playlistPos);                
            }
            catch (Exception e)
            {
                Log.Out($"Error updating persistent radio state via RadioManager: {e.Message}");
            }
        }
    }

    // Legacy key method retained for compatibility; no longer used for persistence
    private string GetRadioStateKey()
    {
        // Return world position-based key for consistency
        return BuildRadioKey(blockPosition);
    }
    
    private System.Collections.IEnumerator DelayedRadioStart(float delay)
    {
        // No longer used; BlockRadioSource handles resume/sync on load
        yield return null;
    }

    private void UpdatePersistentState(string clipName, float playbackTime)
    {
        try
        {
            if (radioOn)
            {
                int currentPlaylistPos = RadioPlaylistManager.Instance.PlaylistPosition;
                if (radioManager == null) radioManager = RadioManager.Instance;
                radioManager.SaveBlockPersistentState(blockPosition, radioOn, clipName, playbackTime, currentPlaylistPos);
            }
        }
        catch (Exception e)
        {
            Log.Out($"Error updating persistent state via RadioManager: {e.Message}");
        }
    }
    
    public void UpdatePersistentState()
    {
        try
        {
            var rs = radioManager != null ? radioManager.GetRadio(this) as RadioSource : null;
            if (radioOn && rs != null && rs.AudioSourceObject != null && rs.AudioSourceObject.clip != null)
            {
                // Prefer rs.ClipName over local currentPlayingClip
                string clip = string.IsNullOrEmpty(rs.ClipName) ? currentPlayingClip : rs.ClipName;
                UpdatePersistentState(clip, rs.AudioSourceObject.time);
            }
        }
        catch (Exception e)
        {
            Log.Out($"Error in periodic persistent state update: {e.Message}");
        }
    }

    public static void CleanupOldPersistentStates()
    {
        // No-op: RadioManager owns persistence and cleans up internally.
        Log.Out("RiseRadio.CleanupOldPersistentStates called; RadioManager handles persistence cleanup");
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
        EntityAlive _entityFocusing)
    {
        return base.GetActivationText(_world, _blockValue, _clrIdx, _blockPos, _entityFocusing);
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue,
        int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        if (AllowPickup > 0)
        {
            cmds[1].enabled = true;
            cmds[2].enabled = TakeDelay > 0f;
        }
        else
        {
            _blockValue.Block.CanPickup = false;
        }

        return cmds;
    }

    public override bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {        
        switch (_commandName)
        {
            case "trigger":
                XUiC_TriggerProperties.Show(((EntityPlayerLocal)_player).PlayerUI.xui, _cIdx, _blockPos, _showTriggers: false, _showTriggeredBy: true);
                break;
            case "Take":
                TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                return true;
            case "Turn On":
                blockPosition = _blockPos.ToVector3();
                Log.Out("Turn On blockPosition : {0}", blockPosition);
                
                // Safety check: ensure radio is registered before trying to play
                EnsureRadioIsRegistered();
                
                SetRadioOn(true);
                radioManager.PlayRadio(this);
                return true;
            case "Turn Off":
                blockPosition = _blockPos.ToVector3();
                Log.Out("Turn Off blockPosition : {0}", blockPosition);
                
                // Safety check: ensure radio is registered before trying to stop
                EnsureRadioIsRegistered();
                
                SetRadioOn(false);
                radioManager.StopRadio(this);
                return true;
        }

        return base.OnBlockActivated(_commandName, _world, _cIdx, _blockPos, _blockValue, _player);
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, GameRandom _rnd)
    {
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);

        localPlayer = _ea as EntityPlayer;
        Block block = _bpResult.blockValue.Block;
        
        // Set up the block position for the newly placed block
        blockPosition = _bpResult.blockPos.ToVector3();
        RadioName = BuildRadioKey(blockPosition); // Use consistent world position key
        
        
        // Ensure we have a RadioManager instance
        if (radioManager == null)
        {
            radioManager = RadioManager.Instance;
            Log.Out("RadioManager instance obtained in OnBlockPlaceBefore");
        }
        
        // Newly placed radios should start OFF, and clear any stale persisted state for this position
        radioOn = false;
        currentPlayingClip = string.Empty;
        try { radioManager.ClearPersistentState(blockPosition); } catch { }
        
        // Register the newly placed radio with RadioManager (BlockRadioSource will create its AudioSource)
        radioManager.AddRadio(this);        
    }

    private void EnsureRadioIsRegistered()
    {
        try
        {
            if (radioManager == null)
            {
                radioManager = RadioManager.Instance;                
            }
            
            // Check if this radio is already registered using world position key
            string radioName = BuildRadioKey(blockPosition);
            var existingRadio = radioManager.GetRadio(this);
            
            if (existingRadio == null)
            {
                Log.Out($"Radio not found in RadioManager, registering now: {radioName}");
                
                // Update the RadioName property
                RadioName = radioName;
                
                // Register with RadioManager
                radioManager.AddRadio(this);
                Log.Out($"Radio {blockID} registered via EnsureRadioIsRegistered");
            }
            else
            {
                Log.Out($"Radio already registered: {radioName}");
            }
        }
        catch (Exception e)
        {
            Log.Out($"Error in EnsureRadioIsRegistered: {e.Message}");
        }
    }

    public new void TakeItemWithTimer(int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        Log.Out("Trying to pick up a block.");
        if (_blockValue.damage > 0)
        {
            GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
            return;
        }

        LocalPlayerUI playerUI = (_player as EntityPlayerLocal).PlayerUI;
        playerUI.windowManager.Open("timer", _bModal: true);
        XUiC_Timer childByType = playerUI.xui.GetChildByType<XUiC_Timer>();
        TimerEventData timerEventData = new TimerEventData();
        // FIXED - Don't store cluster index in timer data; only store blockValue, position, player
        // World will compute correct cluster index when SetBlockRPC is called after timer expires
        timerEventData.Data = new object[3] { _blockValue, _blockPos, _player };
        timerEventData.Event += EventData_Event;
        childByType.SetTimer(TakeDelay, timerEventData);
    }

    private void EventData_Event(TimerEventData timerData)
    {
        var world = GameManager.Instance.World;

        var array = (object[])timerData.Data;
        // FIXED - Array now has 3 elements (no cluster index), updated indexing accordingly
        var originalBlockValue = (BlockValue)array[0];
        var vector3i = (Vector3i)array[1];
        var entityPlayerLocal = array[2] as EntityPlayerLocal;
        
        // CRITICAL FIX: Validate block hasn't changed during timer delay
        var currentBlock = world.GetBlock(vector3i);
        if (currentBlock.type != originalBlockValue.type)
        {
            GameManager.ShowTooltip(entityPlayerLocal, "Block was modified during pickup", string.Empty, "ui_denied");
            Log.Out($"RiseRadio - Block type changed during timer at {vector3i} (expected {originalBlockValue.type}, found {currentBlock.type})");
            return;
        }
        
        if (currentBlock.damage > 0)
        {
            GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
            Log.Out($"RiseRadio - Block was damaged during timer at {vector3i}");
            return;
        }

        // Store the radio info before cleanup using world position key
        string pickupRadioName = BuildRadioKey(vector3i.ToVector3());
        Log.Out($"RiseRadio - EventData_Event called for radio pickup: {pickupRadioName}");

        // Stop via RadioManager before destroying the block
        if (radioOn)
        {
            Log.Out($"RiseRadio - Radio is ON during pickup, stopping via RadioManager");
            try { radioManager.StopRadio(this); } catch { }
            SetRadioOn(false);
        }
        
        // Remove from RadioManager; treat pickup as destruction so AudioSource is disposed and state cleared
        if (radioManager != null && Vector3.Distance(blockPosition, vector3i.ToVector3()) < 0.1f)
        {
            Log.Out($"RiseRadio - Removing radio from RadioManager during pickup: {pickupRadioName}");
            radioManager.RemoveRadio(this, destroyed:true);
        }

        // Pick up the item and put it in your inventory.
        var uiforPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
        var itemStack = new ItemStack(currentBlock.ToItemValue(), 1);
        if (!uiforPlayer.xui.PlayerInventory.AddItem(itemStack, true))
            uiforPlayer.xui.PlayerInventory.DropItem(itemStack);
        
        // CRITICAL FIX: Use position-only SetBlockRPC - lets World compute current cluster index
        // This ensures correct cluster even if chunks reloaded or player moved during timer
        world.SetBlockRPC(vector3i, BlockValue.Air);
        
        Log.Out($"RiseRadio - Radio pickup completed: {pickupRadioName}");
    }
}