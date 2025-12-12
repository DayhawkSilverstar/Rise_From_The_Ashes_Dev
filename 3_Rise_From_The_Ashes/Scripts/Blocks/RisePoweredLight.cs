using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class RisePoweredLight : BlockPoweredLight
{
    public new BlockActivationCommand[] cmds = new BlockActivationCommand[2]
    {
        new BlockActivationCommand("light", "electric_switch", _enabled: true),
        new BlockActivationCommand("take", "hand", _enabled: true)
    };

    private float TakeDelay = 0;
    private float AllowPickup = 0;
    private bool LootContainer = false;
    private bool isRuntimeSwitch;

    EntityPlayer localPlayer;

    public RisePoweredLight()
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

    // Display custom messages for turning on and off the music box, based on the block's name.
    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        if (isRuntimeSwitch)
        {
            TileEntityPoweredBlock tileEntityPoweredBlock = (TileEntityPoweredBlock)_world.GetTileEntity(_clrIdx, _blockPos);
            if (tileEntityPoweredBlock != null)
            {
                PlayerActionsLocal playerInput = ((EntityPlayerLocal)_entityFocusing).playerInput;
                string arg = playerInput.Activate.GetBindingXuiMarkupString() + playerInput.PermanentActions.Activate.GetBindingXuiMarkupString();
                if (tileEntityPoweredBlock.IsToggled)
                {
                    return string.Format(Localization.Get("useSwitchLightOff"), arg);
                }

                return string.Format(Localization.Get("useSwitchLightOn"), arg);
            }
        }
        else if (_world.IsMyLandProtectedBlock(_blockPos, _world.GetGameManager().GetPersistentLocalPlayer()) && TakeDelay > 0f)
        {
            Block block = _blockValue.Block;
        }

        return null;
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue,
        int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        Log.Out("RisePoweredLight BlockComand");
        cmds[0].enabled = true;
        cmds[1].enabled = TakeDelay > 0f;
        return cmds;
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

        Log.Out("RisePoweredLight Command : {0}", _commandName);
        if (!(_commandName == "light"))
        {
            if (_commandName == "take")
            {
                Log.Out("RisePoweredLight - Trying to pick up a powered light block.");
                TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                return true;
            }
        }
        else
        {
            // CRITICAL FIX: Proper light toggle with server authority via SetBlockRPC
            TileEntityPoweredBlock tileEntityPoweredBlock = (TileEntityPoweredBlock)_world.GetTileEntity(_cIdx, _blockPos);
            if (!_world.IsEditor() && tileEntityPoweredBlock != null)
            {
                // Toggle the tile entity state
                tileEntityPoweredBlock.IsToggled = !tileEntityPoweredBlock.IsToggled;
                
                // Update block metadata and sync to server/clients
                BlockValue updatedBlockValue = _blockValue;
                bool newLightState = tileEntityPoweredBlock.IsToggled && (_blockValue.meta & 2) != 0;
                updatedBlockValue.meta = (byte)((_blockValue.meta & 0xFFFFFFFDu) | (newLightState ? 2u : 0u));
                
                // CRITICAL FIX: Use position-only SetBlockRPC for server authority
                _world.SetBlockRPC(_blockPos, updatedBlockValue);
                
                Log.Out($"RisePoweredLight - Light toggled at {_blockPos} (state={newLightState})");
            }
        }

        return false;
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, GameRandom _rnd)
    {
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);

        localPlayer = _ea as EntityPlayer;
        Block block = _bpResult.blockValue.Block;
    }

    private bool updateLightState(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bSwitchLight = false)
    {
        ChunkCluster chunkCluster = _world.ChunkClusters[_cIdx];
        if (chunkCluster == null)
        {
            return false;
        }

        IChunk chunkFromWorldPos = chunkCluster.GetChunkFromWorldPos(_blockPos);
        if (chunkFromWorldPos == null)
        {
            return false;
        }

        BlockEntityData blockEntity = chunkFromWorldPos.GetBlockEntity(_blockPos);
        if (blockEntity == null || !blockEntity.bHasTransform)
        {
            return false;
        }

        bool flag = (_blockValue.meta & 2) != 0;
        if (_world.GetTileEntity(_cIdx, _blockPos) is TileEntityPoweredBlock tileEntityPoweredBlock)
        {
            flag = flag && tileEntityPoweredBlock.IsToggled;
        }

        if (_bSwitchLight)
        {
            flag = !flag;
            _blockValue.meta = (byte)((_blockValue.meta & 0xFFFFFFFDu) | (flag ? 2u : 0u));
            
            // CRITICAL FIX: Use position-only SetBlockRPC for consistency and safety
            _world.SetBlockRPC(_blockPos, _blockValue);
            
            Log.Out($"RisePoweredLight - updateLightState: Light state changed at {_blockPos} (state={flag})");
        }

        Transform transform = blockEntity.transform.Find("MainLight");
        if ((bool)transform)
        {
            LightLOD component = transform.GetComponent<LightLOD>();
            if ((bool)component)
            {
                component.SwitchOnOff(flag);
            }
        }

        transform = blockEntity.transform.Find("Point light");
        if (transform != null)
        {
            LightLOD component2 = transform.GetComponent<LightLOD>();
            if (component2 != null)
            {
                component2.SwitchOnOff(flag);
            }
        }

        return true;
    }

    public void TakeItemWithTimer(int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        #region TakeItemWithTimer
        Log.Out("RisePoweredLight - Trying to pick up a block.");
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
            Log.Out($"RisePoweredLight - Block type changed during timer at {vector3i} (expected {originalBlockValue.type}, found {currentBlock.type})");
            return;
        }
        
        if (currentBlock.damage > 0)
        {
            GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
            Log.Out($"RisePoweredLight - Block was damaged during timer at {vector3i}");
            return;
        }

        // Pick up the item and put it in your inventory.
        var uiforPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
        var itemStack = new ItemStack(currentBlock.ToItemValue(), 1);
        if (!uiforPlayer.xui.PlayerInventory.AddItem(itemStack, true))
            uiforPlayer.xui.PlayerInventory.DropItem(itemStack);
        
        // CRITICAL FIX: Use position-only SetBlockRPC - lets World compute current cluster index
        world.SetBlockRPC(vector3i, BlockValue.Air);
        
        Log.Out("RisePoweredLight - Block picked up and world updated at " + vector3i.ToString());

        #endregion
    }
}
