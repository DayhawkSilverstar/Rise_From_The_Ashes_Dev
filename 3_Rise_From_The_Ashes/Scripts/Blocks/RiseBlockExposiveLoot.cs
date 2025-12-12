using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Scripting;


public class RiseBlockExplosiveLoot : BlockCarExplodeLoot
{
    private new readonly BlockActivationCommand[] cmds =
    {
        new BlockActivationCommand("Search", "search",_enabled: false),
        new BlockActivationCommand("Take", "hand",_enabled: false)
    };

    private float TakeDelay = 0;
    private float AllowPickup = 0;

    public RiseBlockExplosiveLoot()
    {
        HasTileEntity = true;
    }

    public override void Init()
    {
        base.Init();

        TakeDelay = 2f;
        Properties.ParseFloat("AllowPickup", ref AllowPickup);
        Properties.ParseFloat("TakeDelay", ref TakeDelay);

    }

    public override bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {

        Log.Out("RiseBlockLoot - OnBlockActivated");
        // If there's no transform, no sense on keeping going for this class.
        //var _ebcd = _world.GetChunkFromWorldPos(_blockPos).GetBlockEntity(_blockPos);
        //if (_ebcd == null || _ebcd.transform == null)
        //    return false;

        switch (_commandName)
        {
            case "Take":
                Log.Out("RiseBlockLoot - Trying to pick up a block.");
                TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                return true;
            case "Search":
                Log.Out("RiseBlockLoot - Trying to loot a loot block.");
                TileEntityLootContainer tileEntityLootContainer = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityLootContainer;
                if (tileEntityLootContainer != null)
                {
                    if (!tileEntityLootContainer.bWasTouched)
                    {
                        _player.SetCVar(".lootedContainer", 1f);
                    }
                }
                Log.Out("RiseBlockLoot - Command:" + _commandName);
                base.OnBlockActivated(_commandName, _world, _cIdx, _blockPos, _blockValue, _player);
                return true;
        }


        return false;
    }


    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        cmds[0].enabled = true;
        cmds[1].enabled = TakeDelay > 0f;
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
        // CRITICAL FIX: Don't store cluster index - it becomes stale after delay
        timerEventData.Data = new object[3] { _blockValue, _blockPos, _player };
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
        var originalBlockValue = (BlockValue)array[0];
        var vector3i = (Vector3i)array[1];
        var entityPlayerLocal = array[2] as EntityPlayerLocal;
        
        // CRITICAL FIX: Validate block hasn't changed during timer delay
        var currentBlock = world.GetBlock(vector3i);
        if (currentBlock.type != originalBlockValue.type)
        {
            GameManager.ShowTooltip(entityPlayerLocal, "Block was modified during pickup", string.Empty, "ui_denied");
            Log.Out($"RiseBlockExplosiveLoot - Block type changed during timer at {vector3i} (expected {originalBlockValue.type}, found {currentBlock.type})");
            return;
        }
        
        if (currentBlock.damage > 0)
        {
            GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
            Log.Out($"RiseBlockExplosiveLoot - Block was damaged during timer at {vector3i}");
            return;
        }

        // Pick up the item and put it in your inventory.
        var uiforPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
        var itemStack = new ItemStack(currentBlock.ToItemValue(), 1);
        if (!uiforPlayer.xui.PlayerInventory.AddItem(itemStack, true))
            uiforPlayer.xui.PlayerInventory.DropItem(itemStack);
        
        // CRITICAL FIX: Use position-only SetBlockRPC - lets World compute current cluster index
        world.SetBlockRPC(vector3i, BlockValue.Air);
        
        Log.Out("RiseBlockExplosiveLoot - Block picked up and world updated at " + vector3i.ToString());

        #endregion
    }
}
