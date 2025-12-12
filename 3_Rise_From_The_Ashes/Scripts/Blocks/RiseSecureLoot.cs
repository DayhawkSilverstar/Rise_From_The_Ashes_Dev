using System;
using System.Collections.Generic;
using Audio;
using Platform;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class RiseSecureLoot : BlockSecureLoot
{   

    private BlockActivationCommand[] cmds = new BlockActivationCommand[6]
    {
        new BlockActivationCommand("pick", "unlock", _enabled: false),
        new BlockActivationCommand("Search", "search", _enabled: false),
        new BlockActivationCommand("lock", "lock", _enabled: false),
        new BlockActivationCommand("unlock", "unlock", _enabled: false),
        new BlockActivationCommand("keypad", "keypad", _enabled: false),
        new BlockActivationCommand("trigger", "wrench", _enabled: true)
    };

    public override bool AllowBlockTriggers => true;

    public RiseSecureLoot()
    {
        HasTileEntity = true;
    }

    public override void Init()
    {
        base.Init();
        if (!Properties.Values.ContainsKey(PropLootList))
        {
            throw new Exception("Block with name " + GetBlockName() + " doesnt have a loot list");
        }

        lootList = Properties.Values[PropLootList];
        if (Properties.Values.ContainsKey(PropLockPickTime))
        {
            lockPickTime = StringParsers.ParseFloat(Properties.Values[PropLockPickTime]);
        }
        else
        {
            lockPickTime = 15f;
        }

        if (Properties.Values.ContainsKey(PropLockPickItem))
        {
            lockPickItem = Properties.Values[PropLockPickItem];
        }

        if (Properties.Values.ContainsKey(PropLockPickBreakChance))
        {
            lockPickBreakChance = StringParsers.ParseFloat(Properties.Values[PropLockPickBreakChance]);
        }
        else
        {
            lockPickBreakChance = 0f;
        }

        Properties.ParseFloat(PropLootStageMod, ref LootStageMod);
        Properties.ParseFloat(PropLootStageBonus, ref LootStageBonus);
        Properties.ParseString(PropOnLockPickSuccessEvent, ref lockPickSuccessEvent);
        Properties.ParseString(PropOnLockPickFailedEvent, ref lockPickFailedEvent);
    }

    public override void PlaceBlock(WorldBase _world, BlockPlacement.Result _result, EntityAlive _ea)
    {
        base.PlaceBlock(_world, _result, _ea);
        if (_world.GetTileEntity(_result.clrIdx, _result.blockPos) is TileEntitySecureLootContainer tileEntitySecureLootContainer)
        {
            tileEntitySecureLootContainer.SetEmpty();
            if (_ea != null && _ea.entityType == EntityType.Player)
            {
                tileEntitySecureLootContainer.bPlayerStorage = true;
                tileEntitySecureLootContainer.SetOwner(PlatformManager.InternalLocalUserIdentifier);
                tileEntitySecureLootContainer.SetLocked(_isLocked: false);
            }
        }
    }

    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        TileEntitySecureLootContainer tileEntitySecureLootContainer = _world.GetTileEntity(_clrIdx, _blockPos) as TileEntitySecureLootContainer;
        PlayerActionsLocal playerInput = ((EntityPlayerLocal)_entityFocusing).playerInput;
        string arg = playerInput.Activate.GetBindingXuiMarkupString() + playerInput.PermanentActions.Activate.GetBindingXuiMarkupString();
        if (tileEntitySecureLootContainer != null)
        {
            string arg2 = _blockValue.Block.GetLocalizedBlockName();
            if (!tileEntitySecureLootContainer.IsLocked())
            {
                return string.Format(Localization.Get("tooltipUnlocked"), arg, arg2);
            }

            if (lockPickItem == null && !tileEntitySecureLootContainer.LocalPlayerIsOwner())
            {
                return string.Format(Localization.Get("tooltipJammed"), arg, arg2);
            }

            return string.Format(Localization.Get("tooltipLocked"), arg, arg2);
        }

        return "";
    }

    public override bool HasBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        return _world.GetTileEntity(_clrIdx, _blockPos) is TileEntitySecureLootContainer;
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {
        if (!(_world.GetTileEntity(_clrIdx, _blockPos) is TileEntitySecureLootContainer tileEntitySecureLootContainer))
        {
            return Array.Empty<BlockActivationCommand>();
        }

        PlatformUserIdentifierAbs internalLocalUserIdentifier = PlatformManager.InternalLocalUserIdentifier;
        PersistentPlayerData playerData = _world.GetGameManager().GetPersistentPlayerList().GetPlayerData(tileEntitySecureLootContainer.GetOwner());
        bool flag = tileEntitySecureLootContainer.LocalPlayerIsOwner();
        bool flag2 = !flag && playerData != null && playerData.ACL != null && playerData.ACL.Contains(internalLocalUserIdentifier);
        cmds[1].enabled = true;
        cmds[2].enabled = !tileEntitySecureLootContainer.IsLocked() && (flag || flag2);
        cmds[3].enabled = tileEntitySecureLootContainer.IsLocked() && flag;
        cmds[4].enabled = (!tileEntitySecureLootContainer.IsUserAllowed(internalLocalUserIdentifier) && tileEntitySecureLootContainer.HasPassword() && tileEntitySecureLootContainer.IsLocked()) || flag;
        cmds[0].enabled = lockPickItem != null && tileEntitySecureLootContainer.IsLocked() && !flag;
        cmds[5].enabled = _world.IsEditor() && !GameUtils.IsWorldEditor();
        return cmds;
    }

    public override void OnBlockRemoved(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockRemoved(world, _chunk, _blockPos, _blockValue);
    }

    public override DestroyedResult OnBlockDestroyedBy(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _entityId, bool _bUseHarvestTool)
    {
        if (_world.GetTileEntity(_clrIdx, _blockPos) is TileEntitySecureLootContainer tileEntitySecureLootContainer)
        {
            tileEntitySecureLootContainer.OnDestroy();
        }

        return DestroyedResult.Downgrade;
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
                Log.Out("RiseSecureLoot - Trying to search.");
                if (!tileEntitySecureLootContainer.IsLocked() || tileEntitySecureLootContainer.IsUserAllowed(PlatformManager.InternalLocalUserIdentifier))
                {
                    TileEntityLootContainer tileEntityLootContainer = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityLootContainer;
                    if (tileEntityLootContainer != null)
                    {
                        if (!tileEntityLootContainer.bWasTouched)
                        {
                            _player.SetCVar(".lootedContainer", 1f);
                        }
                    }
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
            case "pick":
                {
                    LocalPlayerUI playerUI = (_player as EntityPlayerLocal).PlayerUI;
                    ItemValue item = ItemClass.GetItem(lockPickItem);
                    if (playerUI.xui.PlayerInventory.GetItemCount(item) == 0)
                    {
                        playerUI.xui.CollectedItemList.AddItemStack(new ItemStack(item, 0), _bAddOnlyIfNotExisting: true);
                        GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttLockpickMissing"));
                        return true;
                    }

                    _player.AimingGun = false;
                    Vector3i blockPos = tileEntitySecureLootContainer.ToWorldPos();
                    tileEntitySecureLootContainer.bWasTouched = tileEntitySecureLootContainer.bTouched;
                    _world.GetGameManager().TELockServer(_cIdx, blockPos, tileEntitySecureLootContainer.entityId, _player.entityId, "lockpick");
                    _player.SetCVar(".pickLockAttempt", 1);
                    
                    // CRITICAL FIX: Don't store cluster index in timer data - it becomes stale during long lockpick
                    // Timer data now: [blockPos, player, item] - removed _cIdx and _blockValue
                    Log.Out($"RiseSecureLoot - Starting lockpick timer at {blockPos} (duration={lockPickTime}s)");
                    
                    return true;
                }
            case "trigger":
                XUiC_TriggerProperties.Show(((EntityPlayerLocal)_player).PlayerUI.xui, _cIdx, _blockPos, _showTriggers: false, _showTriggeredBy: true);
                return true;
            default:
                return false;
        }
    }   

    private void EventData_CloseEvent(TimerEventData timerData)
    {
        // Server-only block state update after lockpick failure
        // Only the server should manage tile entity unlock state
        if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            return;
        }
        
        object[] array = (object[])timerData.Data;
        // Note: array[0] no longer contains cluster index - removed for safety
        Vector3i vector3i = (Vector3i)array[1];
        EntityPlayerLocal entityPlayerLocal = array[2] as EntityPlayerLocal;
        ItemValue itemValue = array[3] as ItemValue;
        LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
        Manager.BroadcastPlayByLocalPlayer(vector3i.ToVector3() + Vector3.one * 0.5f, "Misc/locked");
        ItemStack itemStack = new ItemStack(itemValue, 1);
        uIForPlayer.xui.PlayerInventory.RemoveItem(itemStack);
        GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttLockpickBroken"));
        uIForPlayer.xui.CollectedItemList.RemoveItemStack(itemStack);
        
        // Re-query world for tile entity without stale cluster index
        World world = GameManager.Instance.World;
        if (world != null)
        {
            // Let GetTileEntity compute the current cluster index
            if (world.GetTileEntity(vector3i) is TileEntitySecureLootContainer tileEntitySecureLootContainer)
            {
                tileEntitySecureLootContainer.PickTimeLeft = Mathf.Max(lockPickTime * 0.25f, timerData.timeLeft);
                if (lockPickFailedEvent != null)
                {
                    GameEventManager.Current.HandleAction(lockPickFailedEvent, null, entityPlayerLocal, twitchActivated: false, vector3i);
                }

                ResetEventData(timerData);
                
                // Use position-only pattern - pass cluster 0, World computes current cluster
                world.GetGameManager().TEUnlockServer(0, vector3i, tileEntitySecureLootContainer.EntityId, false);
                
                Log.Out($"RiseSecureLoot - Lockpick failed at {vector3i}");
            }
        }
    }

    private void EventData_Event(TimerEventData timerData)
    {
        // Server-only block downgrade after successful lockpick
        // Only the server should decide the final block state
        if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            return;
        }
        
        World world = GameManager.Instance.World;
        object[] obj = (object[])timerData.Data;
        // Note: obj[0] no longer contains cluster index - removed for safety
        Vector3i vector3i = (Vector3i)obj[1];
        EntityPlayerLocal entityPlayerLocal = obj[2] as EntityPlayerLocal;
        
        // Re-query current block state instead of using captured value (may be stale)
        BlockValue block = world.GetBlock(vector3i);
        
        // Let GetTileEntity compute the current cluster index
        if (world.GetTileEntity(vector3i) is TileEntitySecureLootContainer tileEntitySecureLootContainer)
        {
            LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
            if (!LockpickDowngradeBlock.isair)
            {
                // Compute the downgraded block
                BlockValue lockpickDowngradeBlock = LockpickDowngradeBlock;
                lockpickDowngradeBlock = BlockPlaceholderMap.Instance.Replace(lockpickDowngradeBlock, world.GetGameRandom(), vector3i.x, vector3i.z);
                lockpickDowngradeBlock.rotation = block.rotation;
                lockpickDowngradeBlock.meta = block.meta;
                
                // Use position-only SetBlockRPC - World computes current cluster index internally
                // This is critical because the cluster index from timer start is stale after 15+ seconds
                world.SetBlockRPC(vector3i, lockpickDowngradeBlock, lockpickDowngradeBlock.Block.Density);
                
                Log.Out($"RiseSecureLoot - Lockpick success, downgraded to {lockpickDowngradeBlock.Block.GetBlockName()} at {vector3i}");
            }

            Manager.BroadcastPlayByLocalPlayer(vector3i.ToVector3() + Vector3.one * 0.5f, "Misc/unlocking");
            if (lockPickSuccessEvent != null)
            {
                GameEventManager.Current.HandleAction(lockPickSuccessEvent, null, entityPlayerLocal, twitchActivated: false, vector3i);
            }

            ResetEventData(timerData);
            
            // Use position-only pattern - pass cluster 0, World computes current cluster
            world.GetGameManager().TEUnlockServer(0, vector3i, tileEntitySecureLootContainer.EntityId, false);
        }
    }

    public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {
        if (_blockValue.ischild)
        {
            Vector3i parentPos = _blockValue.Block.multiBlockPos.GetParentPos(_blockPos, _blockValue);
            BlockValue block = _world.GetBlock(parentPos);
            return OnBlockActivated(_world, _cIdx, parentPos, block, _player);
        }

        if (!(_world.GetTileEntity(_cIdx, _blockPos) is TileEntitySecureLootContainer tileEntitySecureLootContainer))
        {
            return false;
        }

        _player.AimingGun = false;
        Vector3i blockPos = tileEntitySecureLootContainer.ToWorldPos();
        tileEntitySecureLootContainer.bWasTouched = tileEntitySecureLootContainer.bTouched;
        _world.GetGameManager().TELockServer(_cIdx, blockPos, tileEntitySecureLootContainer.entityId, _player.entityId);
        return true;
    }

    public override bool IsWaterBlocked(IBlockAccess _world, Vector3i _blockPos, BlockValue _blockValue, BlockFaceFlag _sides)
    {
        return true;
    }

    private void ResetEventData(TimerEventData timerData)
    {
        timerData.AlternateEvent -= EventData_CloseEvent;
        timerData.CloseEvent -= EventData_CloseEvent;
        timerData.Event -= EventData_Event;
    }


    public override void OnTriggered(EntityPlayer _player, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, List<BlockChangeInfo> _blockChanges, BlockTrigger _triggeredBy)
    {
        // Call base implementation first (vanilla pattern)
        base.OnTriggered(_player, _world, _cIdx, _blockPos, _blockValue, _blockChanges, _triggeredBy);
        
        // Server-only block downgrade logic
        // Only the server should decide the final block state after trigger
        if (!SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
        {
            return;
        }
        
        if (!LockpickDowngradeBlock.isair)
        {
            // Compute the downgraded block
            BlockValue lockpickDowngradeBlock = LockpickDowngradeBlock;
            lockpickDowngradeBlock = BlockPlaceholderMap.Instance.Replace(lockpickDowngradeBlock, _world.GetGameRandom(), _blockPos.x, _blockPos.z);
            lockpickDowngradeBlock.rotation = _blockValue.rotation;
            lockpickDowngradeBlock.meta = _blockValue.meta;
            
            // Use position-only SetBlockRPC - World computes current cluster index internally
            // This is server-authoritative and safe even if _cIdx becomes stale
            _world.SetBlockRPC(_blockPos, lockpickDowngradeBlock, lockpickDowngradeBlock.Block.Density);
            
            Log.Out($"RiseSecureLoot - Trigger activated, downgraded to {lockpickDowngradeBlock.Block.GetBlockName()} at {_blockPos}");
        }

        Manager.BroadcastPlayByLocalPlayer(_blockPos.ToVector3() + Vector3.one * 0.5f, "Misc/unlocking");
    }
}

