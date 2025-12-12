using HarmonyLib;
using Platform;
using System;
using UnityEngine;
using static System.Net.Mime.MediaTypeNames;

public class RiseBuilding : RiseMasterBlock
{    

    public RiseBuilding()
    {
        HasTileEntity = true;
    }

    public override void Init()
    {
        base.Init();
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

    public override void OnBlockValueChanged(WorldBase _world, Chunk _chunk, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue, BlockValue _newBlockValue)
    {
        //Log.Out($"OnBlockValueChanged");
        base.OnBlockValueChanged(_world,_chunk,_clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        
        // Block value changes are automatically tracked by the game engine
        Log.Out("RiseBuilding - Block value changed at " + _blockPos.ToString());
    }

    public override int DamageBlock(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints, int _entityIdThatDamaged, ItemActionAttack.AttackHitInfo _attackHitInfo = null, bool _bUseHarvestTool = false, bool _bBypassMaxDamage = false)
    {
        return OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, _damagePoints, _entityIdThatDamaged, _attackHitInfo, _bUseHarvestTool, _bBypassMaxDamage);
    }

    public override int OnBlockDamaged(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, int _damagePoints, int _entityIdThatDamaged, ItemActionAttack.AttackHitInfo _attackHitInfo, bool _bUseHarvestTool, bool _bBypassMaxDamage, int _recDepth = 0)
    {
        int result = base.OnBlockDamaged(_world, _clrIdx, _blockPos, _blockValue, _damagePoints, _entityIdThatDamaged, _attackHitInfo, _bUseHarvestTool, _bBypassMaxDamage, _recDepth);

        return result;  
    }

    // Display custom messages for turning on and off the music box, based on the block's name.
    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
        EntityAlive _entityFocusing)
    {
        #region GetActivationText
        return base.GetActivationText(_world, _blockValue, _clrIdx,_blockPos, _entityFocusing);
        #endregion
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, GameRandom _rnd)
    {
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);        
        EntityPlayer localPlayer = _ea as EntityPlayer;
        if (localPlayer != null)
        {
            Block block = _bpResult.blockValue.Block;
            Log.Out($"Placing Block : " + block.GetBlockName());

            if (!block.IsDecoration)
            {
                if (localPlayer.Buffs.CVars.ContainsKey("$constructionExpTotal"))
                {
                    var constructionExpstatPerk = localPlayer.Buffs.CVars["$ConstructionExp"];
                    var constructionExpTotal = localPlayer.Buffs.CVars["$constructionExpTotal"];
                    Log.Out("Construction Exp : {0}", constructionExpstatPerk);
                    Log.Out("Construction Exp Total : {0}", constructionExpTotal);
                    localPlayer.Buffs.CVars["$constructionExpTotal"] += constructionExpstatPerk;
                }
            }
        }
    }
}

