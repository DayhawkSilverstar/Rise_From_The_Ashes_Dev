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
