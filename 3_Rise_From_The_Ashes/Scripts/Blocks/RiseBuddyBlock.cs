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
        timerEventData.Data = new object[4] { _cIdx, _blockValue, _blockPos, _player };
        timerEventData.Event += EventData_Event;
        childByType.SetTimer(TakeDelay, timerEventData);

        #endregion
    }

    private void EventData_Event(TimerEventData timerData)
    {
        #region EventData_Event
        Log.Out($"EventData");
        var world = GameManager.Instance.World;

        var array = (object[])timerData.Data;
        var clrIdx = (int)array[0];
        var blockValue = (BlockValue)array[1];
        var vector3i = (Vector3i)array[2];
        var block = world.GetBlock(vector3i);
        var entityPlayerLocal = array[3] as EntityPlayerLocal;

        // Pick up the item and put it inyor your inventory.
        var uiforPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
        var itemStack = new ItemStack(block.ToItemValue(), 1);
        if (!uiforPlayer.xui.PlayerInventory.AddItem(itemStack, true))
            uiforPlayer.xui.PlayerInventory.DropItem(itemStack);
        world.SetBlockRPC(clrIdx, vector3i, BlockValue.Air);

        #endregion
    }
}
