using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Scripting;

public class RiseBlockLoot : BlockLoot
{   
    private readonly BlockActivationCommand[] cmds =
    {
        new BlockActivationCommand("Search", "search",_enabled: false),
        new BlockActivationCommand("Search", "search",_enabled: false),
        new BlockActivationCommand("Take", "hand",_enabled: false)
    };

    private float TakeDelay = 0;
    private float AllowPickup = 0;

    public RiseBlockLoot()
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
#if DEBUG
        Log.Out("Command : {0}", _commandName);
#endif
        if (AllowPickup > 0)
        {
            if (_commandName == "Take")
            {
                TileEntityLootContainer tileEntityLootContainer = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityLootContainer;
                if (tileEntityLootContainer == null)
                {
                    return false;
                }

                if (tileEntityLootContainer.IsEmpty())
                {
                    TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                }
                else
                {
                    GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
                }
                return true;
            }
            else if (_commandName == "Search" | _commandName == "Open")
            {
#if DEBUG
                Log.Out("Open or Search");
#endif
                TileEntityLootContainer tileEntityLootContainer = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityLootContainer;
                if (tileEntityLootContainer != null)
                {
                    if (!tileEntityLootContainer.bWasTouched)
                    {
                        _player.SetCVar(".lootedContainer", 1f);
                    }
                }
                
                _commandName = "Search";
                
                base.OnBlockActivated(_commandName, _world, _cIdx, _blockPos, _blockValue, _player);
            }
        }
        else
        {
#if DEBUG
            Log.Out("Allow Pickup = false");
#endif                  
            base.OnBlockActivated(_commandName,_world,_cIdx,_blockPos,_blockValue,_player);
        }

        return false;
    }


    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        // TODO : Check to see if the person is in someone elses claim and not a friend. Don't allow them to pick it up then.
#if DEBUG
        Log.Out("Commands = " + cmds.Count().ToString());
#endif
        if (AllowPickup > 0)
        {
            cmds[1].enabled = true;
            cmds[2].enabled = TakeDelay > 0f;
        }
        else
        {
            cmds[0].enabled = true;
            cmds[1].enabled = false;
            _blockValue.Block.CanPickup = false;
        }

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
