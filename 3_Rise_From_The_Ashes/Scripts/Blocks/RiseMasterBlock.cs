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

                foreach (SItemNameCount item in newBlock.Block.RepairItems)
                {
                    try
                    {
                        ItemValue itemValue = ItemClass.GetItem(item.ItemName);
                        double cnt = Math.Floor(item.Count / 2.0);
                        Log.Out("Item count : (" + cnt.ToString() + ")");
                        if (cnt == 0)
                        {
                            cnt = 1;
                        }
                        // Create an item stack containing the damaged block
                        var itemStack = new ItemStack(itemValue, (int)cnt);

                        Log.Out("Item Stack count " + itemStack.count.ToString());
                        
                        // Add it to the players inventory. 
                        if (player.bag.AddItem(itemStack))
                        {
                            Log.Out("Item added to players bag : " + item.ItemName + " (" + cnt.ToString() + ")");
                        }
                        else if (player.inventory.AddItem(itemStack))
                        {
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
                
                // Call base method which handles proper world updates and chunk tracking
                int result = base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, initialDamage, _entityIdThatDamaged, _attackHitInfo, _bUseHarvestTool, _bBypassMaxDamage, _recDepth);
                
                Log.Out("Upgrade completed - world updated at " + _blockPos.ToString());
                
                return result;
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
            
            // Let base class handle repairs properly
            int result = base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, initialDamage, _entityIdThatDamaged, _attackHitInfo, _bUseHarvestTool, _bBypassMaxDamage, _recDepth);
            
            Log.Out("Repair completed - world updated at " + _blockPos.ToString());
            
            return result;
        }

        EntityAlive ea = GameManager.Instance.World.GetEntity(_entityIdThatDamaged) as EntityAlive;
        if (ea != null)
        {
            ItemActionData itemActionData = ea.inventory.holdingItemData.actionData[0];
            if (itemActionData != null)
            {
                Log.Out("Weapon = " + ea.inventory.holdingItem.GetItemName());
                
                // FIX: Use initial damage (before resist) instead of base weapon damage for cap calculation
                // This allows power attacks to do their full damage
                // Cap at 5x the incoming damage to prevent exploits, but don't use base weapon damage
                int maxDamage = initialDamage * 5;
                
                if (maxDamage < finalDamage)
                {
                    Log.Out("Final Damage :" + finalDamage.ToString() + " is greater than " + maxDamage.ToString());
                    finalDamage = maxDamage;                 
                }
            }

            if (ea.Buffs.CVars.ContainsKey("$blockDamageDone"))
            {
                Log.Out("Setting $blockDamageDone :" + finalDamage.ToString());
                ea.Buffs.CVars["$blockDamageDone"] = finalDamage;
            }
            else
            {
                Log.Out("$blockDamageDone is not found.");
            }

            Log.Out("Entity : " + ea.EntityName + " Damages : " + _blockValue.Block.GetBlockName() + " For : " + finalDamage.ToString() + "(" + initialDamage.ToString() + ") damage after damage resist value of (" + damageResist.ToString() + ")");
        }  
        else
        {
            Log.Out("Entity : " + _entityIdThatDamaged.ToString() + " Damages : " + _blockValue.Block.GetBlockName() + " For : " + finalDamage.ToString() + "(" + initialDamage.ToString() + ") damage after damage resist value of (" + damageResist.ToString() + ")");
        }

        int result2 = base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, finalDamage, _entityIdThatDamaged, _attackHitInfo, _bUseHarvestTool, _bBypassMaxDamage, _recDepth);

        return result2;
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue,
        int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {        
        if (AllowPickup > 0)
        {
            cmds[0].enabled = true;
            cmds[1].enabled = TakeDelay > 0f;
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
        // CRITICAL FIX: Handle child blocks like base game does
        if (_blockValue.ischild)
        {
            Vector3i parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            BlockValue block = _world.GetBlock(parentPos);
            return OnBlockActivated(_commandName, _world, _cIdx, parentPos, block, _player);
        }

        Log.Out("Command : {0}", _commandName);
        
        // Handle custom "take" command with timer
        if (AllowPickup > 0 && _commandName == "take")
        {
            Log.Out("RiseMasterBlock - Trying to pick up a block.");
            TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
            return true;
        }

        // Let base handle all other commands (including search)
        // SIMPLIFIED: Removed useless search wrapper that just called the other overload
        // Per "Opportunities to Leverage Default 7DTD Code" document section 4.1
        return base.OnBlockActivated(_commandName, _world, _cIdx, _blockPos, _blockValue, _player);
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
        // CRITICAL FIX: Don't store cluster index - it becomes stale after delay
        timerEventData.Data = new object[3] { _blockValue, _blockPos, _player };
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
        var originalBlockValue = (BlockValue)array[0];
        var vector3i = (Vector3i)array[1];
        var entityPlayerLocal = array[2] as EntityPlayerLocal;
        
        // CRITICAL FIX: Validate block hasn't changed during timer delay
        var currentBlock = world.GetBlock(vector3i);
        if (currentBlock.type != originalBlockValue.type)
        {
            GameManager.ShowTooltip(entityPlayerLocal, "Block was modified during pickup", string.Empty, "ui_denied");
            Log.Out($"RiseMasterBlock - Block type changed during timer at {vector3i} (expected {originalBlockValue.type}, found {currentBlock.type})");
            return;
        }
        
        if (currentBlock.damage > 0)
        {
            GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
            Log.Out($"RiseMasterBlock - Block was damaged during timer at {vector3i}");
            return;
        }

        // Pick up the item and put it in your inventory.
        var uiforPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
        var itemStack = new ItemStack(currentBlock.ToItemValue(), 1);
        if (!uiforPlayer.xui.PlayerInventory.AddItem(itemStack, true))
            uiforPlayer.xui.PlayerInventory.DropItem(itemStack);
        
        // CRITICAL FIX: Use position-only SetBlockRPC - lets World compute current cluster index
        world.SetBlockRPC(vector3i, BlockValue.Air);
        
        Log.Out("RiseMasterBlock - Block picked up and world updated at " + vector3i.ToString());

        #endregion
    }
}

