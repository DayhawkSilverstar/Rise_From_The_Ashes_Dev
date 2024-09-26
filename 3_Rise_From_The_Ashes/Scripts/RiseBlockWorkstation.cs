using System;
using UnityEngine;
using UnityEngine.Scripting;

namespace RiseFromTheAshes
{
    public class RiseBlockWorkstation : BlockParticle
    {
        protected float TakeDelay;

        public WorkstationData WorkstationData;

        private string[] toolTransformNames;

        protected BlockActivationCommand[] cmds = new BlockActivationCommand[2]
   {
        new BlockActivationCommand("open", "campfire", _enabled: true),
        new BlockActivationCommand("take", "hand", _enabled: false)
   };

        public RiseBlockWorkstation()
        {
            HasTileEntity = true;
        }

        public override void Init()
        {
            base.Init();
            
            IsNotifyOnLoadUnload = true;
        }

        public override void OnBlockAdded(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
        {
            Log.Out($"RiseWorkstation - OnBlockAdded");

            base.OnBlockAdded(world, _chunk, _blockPos, _blockValue);
            if (!_blockValue.ischild)
            {
                RiseTileEntityWorkstation tileEntityWorkstation = new RiseTileEntityWorkstation(_chunk);
                tileEntityWorkstation.localChunkPos = World.toBlock(_blockPos);
                _chunk.AddTileEntity(tileEntityWorkstation);
            }

            
        }

        public override void OnBlockRemoved(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
        {
            Log.Out($"RiseWorkstation - OnBlockRemoved");

            base.OnBlockRemoved(world, _chunk, _blockPos, _blockValue);
            _chunk.RemoveTileEntityAt<RiseTileEntityWorkstation>((World)world, World.toBlock(_blockPos));
        }

        public override void PlaceBlock(WorldBase _world, BlockPlacement.Result _result, EntityAlive _ea)
        {
            Log.Out($"RiseWorkstation - PlaceBlock");

            base.PlaceBlock(_world, _result, _ea);
            RiseTileEntityWorkstation tileEntityWorkstation = (RiseTileEntityWorkstation)_world.GetTileEntity(_result.clrIdx, _result.blockPos);
            if (tileEntityWorkstation != null)
            {
                tileEntityWorkstation.IsPlayerPlaced = true;
            }
        }

        public override bool OnBlockActivated(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
        {
            Log.Out($"RiseWorkstation - OnBlockActivated");

            RiseTileEntityWorkstation tileEntityWorkstation = (RiseTileEntityWorkstation)_world.GetTileEntity(_cIdx, _blockPos);
            if (tileEntityWorkstation == null)
            {
                return false;
            }

            _player.AimingGun = false;
            Vector3i blockPos = tileEntityWorkstation.ToWorldPos();
            _world.GetGameManager().TELockServer(_cIdx, blockPos, tileEntityWorkstation.entityId, _player.entityId);
            return true;
        }

        public override void OnBlockValueChanged(WorldBase _world, Chunk _chunk, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue, BlockValue _newBlockValue)
        {
            Log.Out($"RiseWorkstation - OnBlockValueChanged");

            base.OnBlockValueChanged(_world, _chunk, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
            checkParticles(_world, _clrIdx, _blockPos, _newBlockValue);
        }

        public override byte GetLightValue(BlockValue _blockValue)
        {
            Log.Out($"RiseWorkstation - GetLightValue");

            return 0;
        }

        protected override void checkParticles(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
        {
            Log.Out($"RiseWorkstation - checkParticles");

            if (!_blockValue.ischild)
            {
                bool flag = _world.GetGameManager().HasBlockParticleEffect(_blockPos);
                if (_blockValue.meta != 0 && !flag)
                {
                    addParticles(_world, _clrIdx, _blockPos.x, _blockPos.y, _blockPos.z, _blockValue);
                }
                else if (_blockValue.meta == 0 && flag)
                {
                    removeParticles(_world, _blockPos.x, _blockPos.y, _blockPos.z, _blockValue);
                }
            }
        }

        public static bool IsLit(BlockValue _blockValue)
        {
            Log.Out($"RiseWorkstation - IsLit");

            return _blockValue.meta != 0;
        }

        public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
        {
            Log.Out($"RiseWorkstation - GetActivationText");

            return Localization.Get("useWorkstation");
        }

        public override bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
        {
            Log.Out($"RiseWorkstation - OnBlockActivated : " +  _commandName);

            if (!(_commandName == "open"))
            {
                if (_commandName == "take")
                {
                    TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                    return true;
                }

                return false;
            }

            return OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
        }

        public override bool HasBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
        {
           // Log.Out($"RiseWorkstation - HasBlockActivationCommands");

            return true;
        }

        public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
        {
            Log.Out($"RiseWorkstation - BlockActivationCommand");

            bool flag = _world.IsMyLandProtectedBlock(_blockPos, _world.GetGameManager().GetPersistentLocalPlayer());
            bool flag2 = false;

            try
            {
                TileEntity entity = _world.GetTileEntity(_clrIdx, _blockPos);
                Log.Out($"TileEntity : " + entity.GetTileEntityType().ToString());

                RiseTileEntityWorkstation tileEntityWorkstation = (RiseTileEntityWorkstation)entity;
                if (tileEntityWorkstation != null)
                {
                    flag2 = tileEntityWorkstation.IsPlayerPlaced;
                }
            }
            catch (Exception ex) 
            {
                Log.Out($"RiseWorkstation - BlockActivationCommand : ERROR " + ex.StackTrace);
                throw ex;
            }
 
            cmds[1].enabled = flag && flag2 && TakeDelay > 0f;
            return cmds;
        }

        public void UpdateVisible(RiseTileEntityWorkstation _te)
        {
            Transform transform = _te.GetChunk().GetBlockEntity(_te.ToWorldPos()).transform;
            if (!transform)
            {
                return;
            }

            ItemStack[] tools = _te.Tools;
            int num = Utils.FastMin(tools.Length, toolTransformNames.Length);
            for (int i = 0; i < num; i++)
            {
                Transform transform2 = transform.Find(toolTransformNames[i]);
                if ((bool)transform2)
                {
                    transform2.gameObject.SetActive(!tools[i].IsEmpty());
                }
            }

            Transform transform3 = transform.Find("craft");
            if ((bool)transform3)
            {
                transform3.gameObject.SetActive(_te.IsCrafting);
            }
        }

        private void TakeItemWithTimer(int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
        {
            Log.Out($"RiseWorkstation - TakeItemWithTimer");

            if (_blockValue.damage > 0)
            {
                GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
                return;
            }

            if (!(GameManager.Instance.World.GetTileEntity(_cIdx, _blockPos) as RiseTileEntityWorkstation).IsEmpty)
            {
                GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttWorkstationNotEmpty"), string.Empty, "ui_denied");
                return;
            }

            LocalPlayerUI playerUI = (_player as EntityPlayerLocal).PlayerUI;
            playerUI.windowManager.Open("timer", _bModal: true);
            XUiC_Timer childByType = playerUI.xui.GetChildByType<XUiC_Timer>();
            TimerEventData timerEventData = new TimerEventData();
            timerEventData.Data = new object[4] { _cIdx, _blockValue, _blockPos, _player };
            timerEventData.Event += EventData_Event;
            childByType.SetTimer(TakeDelay, timerEventData);
        }

        private void EventData_Event(TimerEventData timerData)
        {
            Log.Out($"RiseWorkstation - EventData_Event");

            World world = GameManager.Instance.World;
            object[] obj = (object[])timerData.Data;
            int clrIdx = (int)obj[0];
            BlockValue blockValue = (BlockValue)obj[1];
            Vector3i vector3i = (Vector3i)obj[2];
            BlockValue block = world.GetBlock(vector3i);
            EntityPlayerLocal entityPlayerLocal = obj[3] as EntityPlayerLocal;
            if (block.damage > 0)
            {
                GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
                return;
            }

            if (block.type != blockValue.type)
            {
                GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttBlockMissingPickup"), string.Empty, "ui_denied");
                return;
            }

            RiseTileEntityWorkstation tileEntityWorkstation = world.GetTileEntity(clrIdx, vector3i) as RiseTileEntityWorkstation;
            if (tileEntityWorkstation.IsUserAccessing())
            {
                GameManager.ShowTooltip(entityPlayerLocal, Localization.Get("ttCantPickupInUse"), string.Empty, "ui_denied");
                return;
            }

            LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
            HandleTakeInternalItems(tileEntityWorkstation, uIForPlayer);
            ItemStack itemStack = new ItemStack(block.ToItemValue(), 1);
            if (!uIForPlayer.xui.PlayerInventory.AddItem(itemStack))
            {
                uIForPlayer.xui.PlayerInventory.DropItem(itemStack);
            }

            world.SetBlockRPC(clrIdx, vector3i, BlockValue.Air);
        }

        protected void HandleTakeInternalItems(RiseTileEntityWorkstation te, LocalPlayerUI playerUI)
        {
            Log.Out($"RiseWorkstation - HandleTakeInternalItems");

            ItemStack[] output = te.Output;
            for (int i = 0; i < output.Length; i++)
            {
                if (!output[i].IsEmpty() && !playerUI.xui.PlayerInventory.AddItem(output[i]))
                {
                    playerUI.xui.PlayerInventory.DropItem(output[i]);
                }
            }

            output = te.Tools;
            for (int j = 0; j < output.Length; j++)
            {
                if (!output[j].IsEmpty() && !playerUI.xui.PlayerInventory.AddItem(output[j]))
                {
                    playerUI.xui.PlayerInventory.DropItem(output[j]);
                }
            }

            output = te.Fuel;
            for (int k = 0; k < output.Length; k++)
            {
                if (!output[k].IsEmpty() && !playerUI.xui.PlayerInventory.AddItem(output[k]))
                {
                    playerUI.xui.PlayerInventory.DropItem(output[k]);
                }
            }
        }

        public override void OnBlockEntityTransformBeforeActivated(WorldBase _world, Vector3i _blockPos, int _cIdx, BlockValue _blockValue, BlockEntityData _ebcd)
        {
            Log.Out($"RiseWorkstation - OnBlockEntityTransformBeforeActivated");

            base.OnBlockEntityTransformBeforeActivated(_world, _blockPos, _cIdx, _blockValue, _ebcd);
            RiseTileEntityWorkstation riseTileEntityWorkstation = _world.GetTileEntity(_cIdx, _blockPos) as RiseTileEntityWorkstation;
            if (riseTileEntityWorkstation != null)
            {
                UpdateVisible(riseTileEntityWorkstation);
            }
        }        
    }
}
