using HarmonyLib;
using Platform;
using System;
using UnityEngine;

public class RiseMasterBlock : Block
{
    public static string PropDamageResist = "DamageResist";

    private readonly BlockActivationCommand[] cmds =
    {
        new BlockActivationCommand("search", "search", false),
        new BlockActivationCommand("take", "hand", false)
    };

    private float TakeDelay = 0;
    private float AllowPickup = 0;
    private bool LootContainer = false;

    EntityPlayer localPlayer;

    public RiseMasterBlock()
    {
        HasTileEntity = true;
    }

    public override void Init()
    {
        base.Init();

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
    }

    // Only fires if IsNotifyOnLoadUnload is set to true
    public override void OnBlockUnloaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockUnloaded(_world, _clrIdx, _blockPos, _blockValue);
    }

    public override void OnBlockAdded(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        #region OnBlockAdded
            base.OnBlockAdded(world, _chunk, _blockPos, _blockValue);
        #endregion
    }

    // Display custom messages for turning on and off the music box, based on the block's name.
    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
        EntityAlive _entityFocusing)
    {
        #region GetActivationText
        return base.GetActivationText(_world, _blockValue, _clrIdx,_blockPos, _entityFocusing);
        #endregion
    }

    public override int OnBlockDamaged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints, int _entityIdThatDamaged, ItemActionAttack.AttackHitInfo _attackHitInfo, bool _bUseHarvestTool, bool _bBypassMaxDamage, int _recDepth = 0)
    {
        // Code for hardness inserted here.      
        int damage = _blockValue.damage;
        int num = damage + _damagePoints;
        int initialDamage = _damagePoints;
        int finalDamage = 0;
        UInt16 damageResist = 0;

        // num less than zero means it is being upgraded.
        if (num < 0)
        {
            // Determine if the player of the client is the same on doing the damage
            EntityPlayerLocal player = _world.GetPrimaryPlayer();
            Log.Out("Player ID : {0} and Damage ID : {1} ", player.entityId.ToString(), _entityIdThatDamaged.ToString());
            if (player.entityId == _entityIdThatDamaged)
            {
                BlockValue newBlock = _blockValue;

                /*
                // Change to the variant if possible
                switch (_blockValue.Block.blockMaterial.id)
                {
                    case "Mwood_shapes":
                        {
                            newBlock = new BlockValue((uint)GetBlockByName("woodShapes:VariantHelper").blockID);
                            break;
                        }
                }
                */

                foreach (SItemNameCount item in newBlock.Block.RepairItems)
                {
                    try
                    {
                        ItemValue itemValue = ItemClass.GetItem(item.ItemName);
                        double cnt = Math.Floor(item.Count / 2.0);
                        if (cnt == 0)
                        {
                            cnt = 1;
                        }
                        // Create an item stack containing the damaged block
                        var itemStack = new ItemStack(itemValue, (int)cnt);
                        // Add it to the players inventory. 
                        if (player.bag.AddItem(itemStack))
                        {
                            player.AddUIHarvestingItem(itemStack,true);
                            Log.Out("Item added to players bag : " + item.ItemName + " (" + cnt.ToString() + ")");
                        }
                        else if (player.inventory.AddItem(itemStack))
                        {
                            player.AddUIHarvestingItem(itemStack,true);
                            Log.Out("Item added to players toolbar : " + item.ItemName + " (" + cnt.ToString() + ")");
                        }
                        else
                        {
                            Log.Out("Item dropped : " + item.ItemName + " (" + cnt.ToString() + ")");
                            _world.GetGameManager().ItemDropServer(itemStack, player.position, Vector3.zero, player.entityId, 60.0f);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Out("Failed creating repair items");
                        Log.Out(ex.Message);
                    }
                }

                Log.Out("Player upgrades " + _blockValue.Block.GetBlockName() + " For : " + initialDamage.ToString());
                return base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, initialDamage, _entityIdThatDamaged, _attackHitInfo, _bUseHarvestTool, _bBypassMaxDamage, _recDepth);

            }
        }
        else
        {
            // <property name="DamageResist" value="1"/>            

            if (Properties.Values.ContainsKey(PropDamageResist))
            {
                UInt16.TryParse(Properties.Values[PropDamageResist], out damageResist);
            }

            finalDamage = initialDamage - damageResist;
            if (finalDamage < 0)
            {
                finalDamage = 0;
            }
        }

        if (initialDamage < 0)
        {
            Log.Out("Player repairs " + _blockValue.Block.GetBlockName() + " For : " + initialDamage.ToString());
            return base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, initialDamage, _entityIdThatDamaged, _attackHitInfo, _bUseHarvestTool, _bBypassMaxDamage, _recDepth);
        }

        EntityAlive ea = GameManager.Instance.World.GetEntity(_entityIdThatDamaged) as EntityAlive;
        if (ea != null)
        {
            ItemActionData itemActionData = ea.inventory.holdingItemData.actionData[0];
            if (itemActionData != null)
            {
                Log.Out("Weapon = " + ea.inventory.holdingItem.GetItemName());
                int maxDamage = (int)itemActionData.attackDetails.damage * 3;
                if (maxDamage < finalDamage)
                {
                    Log.Out("Final Damage :" + finalDamage.ToString() + " is greater than " + maxDamage.ToString());
                    finalDamage = maxDamage;
                }
            }
            
            Log.Out("Entity : " + ea.EntityName + " Damages : " + _blockValue.Block.GetBlockName() + " For : " + finalDamage.ToString() + "(" + initialDamage.ToString() + ") damage after damage resist value of (" + damageResist.ToString() + ")");
        }  
        else
        {
            Log.Out("Entity : " + _entityIdThatDamaged.ToString() + " Damages : " + _blockValue.Block.GetBlockName() + " For : " + finalDamage.ToString() + "(" + initialDamage.ToString() + ") damage after damage resist value of (" + damageResist.ToString() + ")");
        }

        int result = base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, finalDamage, _entityIdThatDamaged, _attackHitInfo, _bUseHarvestTool, _bBypassMaxDamage, _recDepth);

        return result;
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

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, GameRandom _rnd)
    {
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);


    }

    public override bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {
        Log.Out("Command : {0}", _commandName);
        if (AllowPickup > 0)
        {
            if (_commandName == "take")
            {
                TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                return true;
            }
            else if (_commandName == "Search")
            {
                Log.Out("Trigger Selected");
                return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
            }
        }

        return false;

    }

    public override bool HasBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        return true;
    }

    // We want to give the user the ability to pick up the blocks too, but the loot containers don't support that directly.
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

    // Handles what happens to the contents of the box when you pick up the block.
    private void EventData_Event(TimerEventData timerData)
    {
        #region EventData_Event

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

