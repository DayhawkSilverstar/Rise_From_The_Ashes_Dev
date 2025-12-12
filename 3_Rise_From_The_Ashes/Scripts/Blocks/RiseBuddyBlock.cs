using Audio;
using Platform;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using UnityEngine;


public class RiseBuddyBlock : BlockSecureLoot
{
    private float TakeDelay = 0;
    private float AllowPickup = 0;

    private BlockActivationCommand[] cmds = new BlockActivationCommand[5]
    {        
        new BlockActivationCommand("Search", "search", _enabled: true),
        new BlockActivationCommand("lock", "lock", _enabled: true),
        new BlockActivationCommand("unlock", "unlock", _enabled: true),
        new BlockActivationCommand("keypad", "keypad", _enabled: true),
        new BlockActivationCommand("Take", "hand",_enabled: true)
    };
    
    public override void Init()
    {
        base.Init();
        TakeDelay = 2f;
        Properties.ParseFloat("AllowPickup", ref AllowPickup);
        Properties.ParseFloat("TakeDelay", ref TakeDelay);
    }    

    public override bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {
        if (_blockValue.ischild)
        {
            Vector3i parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            BlockValue block = _world.GetBlock(parentPos);
            return OnBlockActivated(_commandName, _world, _cIdx, parentPos, block, _player);
        }

        if (!(_world.GetTileEntity(_cIdx, _blockPos) is TileEntitySecureLootContainer tileEntitySecureLootContainer))
        {
            return false;
        }

        switch (_commandName)
        {
            case "Search":
                if (!tileEntitySecureLootContainer.IsLocked() || tileEntitySecureLootContainer.IsUserAllowed(PlatformManager.InternalLocalUserIdentifier))
                {
                    return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
                }

                Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/locked");
                return false;
            case "lock":
                tileEntitySecureLootContainer.SetLocked(_isLocked: true);
                Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/locking");
                GameManager.ShowTooltip(_player as EntityPlayerLocal, "containerLocked");
                return true;
            case "unlock":
                tileEntitySecureLootContainer.SetLocked(_isLocked: false);
                Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/unlocking");
                GameManager.ShowTooltip(_player as EntityPlayerLocal, "containerUnlocked");
                return true;
            case "keypad":
                {
                    LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(_player as EntityPlayerLocal);
                    if (uIForPlayer != null)
                    {
                        XUiC_KeypadWindow.Open(uIForPlayer, tileEntitySecureLootContainer);
                    }

                    return true;
                }          
            case "trigger":
                XUiC_TriggerProperties.Show(((EntityPlayerLocal)_player).PlayerUI.xui, _cIdx, _blockPos, _showTriggers: false, _showTriggeredBy: true);
                return true;
            case "Take":
                TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                return true;
            default:
                return false;
        }
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        return cmds;
    }

    public void TakeItemWithTimer(int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        #region TakeItemWithTimer
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
        // CRITICAL FIX: Don't store cluster index - it becomes stale after 2-second delay
        // Store only: BlockValue (for validation), position, player
        timerEventData.Data = new object[3] { _blockValue, _blockPos, _player };
        timerEventData.Event += EventData_Event;
        childByType.SetTimer(TakeDelay, timerEventData);

        #endregion
    }

    private void EventData_Event(TimerEventData timerData)
    {
        #region EventData_Event
        Log.Out($"RiseBuddyBlock - EventData_Event triggered");
        var world = GameManager.Instance.World;

        var array = (object[])timerData.Data;
        var originalBlockValue = (BlockValue)array[0];  // For validation only
        var vector3i = (Vector3i)array[1];              // Position only
        var entityPlayerLocal = array[2] as EntityPlayerLocal;

        // CRITICAL FIX: Validate block hasn't changed during 2-second timer delay
        var currentBlock = world.GetBlock(vector3i);
        
        // Validate block type matches
        if (currentBlock.type != originalBlockValue.type)
        {
            GameManager.ShowTooltip(entityPlayerLocal, "Block was modified during pickup", string.Empty, "ui_denied");
            Log.Out($"[RiseBuddyBlock] - Block type changed during timer at {vector3i} (expected {originalBlockValue.type}, found {currentBlock.type})");
            return;
        }
        
        // Validate block not damaged
        if (currentBlock.damage > 0)
        {
            GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
            Log.Out($"[RiseBuddyBlock] - Block was damaged during timer at {vector3i}");
            return;
        }

        // Pick up the item based on CURRENT block state
        var uiforPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
        var itemStack = new ItemStack(currentBlock.ToItemValue(), 1);
        if (!uiforPlayer.xui.PlayerInventory.AddItem(itemStack, true))
            uiforPlayer.xui.PlayerInventory.DropItem(itemStack);
        
        // CRITICAL FIX: Use position-only SetBlockRPC - World computes current cluster index internally
        world.SetBlockRPC(vector3i, BlockValue.Air);
        
        Log.Out("[RiseBuddyBlock] - Block picked up and world updated at " + vector3i.ToString());

        #endregion
    }
}
