using System;
using Platform;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class RiseBlockLandClaim : BlockLandClaim
{
    private string activeBuff = string.Empty;
    private float buffRadius = 10f; // Default; will be overridden by GamePrefs
    private const int TICK_RATE = 20; // Ticks per second
    
    public RiseBlockLandClaim()
    {
        // Land claims already have IsNotifyOnLoadUnload = true from base class
        IsRandomlyTick = false; // We'll schedule ticking manually
    }

    public override void Init()
    {
        base.Init();

        // Parse the ActiveBuff property from blocks.xml
        if (Properties.Values.ContainsKey("ActiveBuff"))
        {
            activeBuff = Properties.Values["ActiveBuff"];
        }
        
        // Always sync buff radius with the game's LandClaim size setting
        // This ensures the aura radius matches the claim radius players configure in the game options/server settings
        try
        {
            int claimSize = GamePrefs.GetInt(EnumGamePrefs.LandClaimSize);
            if (claimSize > 0)
            {
                buffRadius = claimSize;
            }
        }
        catch
        {
            // Fallback to XML-specified radius if available
            if (Properties.Values.ContainsKey("BuffRadius"))
            {
                Properties.ParseFloat("BuffRadius", ref buffRadius);
            }
        }
    }
    
    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
               
        if (_world.IsRemote())
        {
            return; // Only schedule on server
        }
        
        // Schedule this block to tick every second (20 ticks)
        if (_world is World world)
        {
            world.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, blockID, TICK_RATE);
        }
    }
    
    public override void OnBlockRemoved(WorldBase world, Chunk _chunk, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockRemoved(world, _chunk, _blockPos, _blockValue);
        
        // Remove from tick schedule
        if (world is World w)
        {
            w.GetWBT().InvalidateScheduledBlockUpdate(_chunk.ClrIdx, _blockPos, blockID);
        }
    }

    public override void OnBlockValueChanged(WorldBase _world, Chunk _chunk, int _clrIdx, Vector3i _blockPos, BlockValue _oldBlockValue, BlockValue _newBlockValue)
    {
        base.OnBlockValueChanged(_world, _chunk, _clrIdx, _blockPos, _oldBlockValue, _newBlockValue);
        
        // If the block changed from primary to non-primary, stop ticking
        if (IsPrimary(_oldBlockValue) && !IsPrimary(_newBlockValue))
        {
            if (_world is World w)
            {
                w.GetWBT().InvalidateScheduledBlockUpdate(_clrIdx, _blockPos, blockID);
            }
        }
        // If it changed from non-primary to primary, start ticking
        else if (!IsPrimary(_oldBlockValue) && IsPrimary(_newBlockValue))
        {
            if (_world is World w2 && !_world.IsRemote())
            {
                w2.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, blockID, TICK_RATE);
            }
        }
    }
    
    public override ulong GetTickRate()
    {
        return TICK_RATE; // Return how often to tick
    }

    public override bool UpdateTick(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bRandomTick, ulong _ticksIfLoaded, GameRandom _rnd)
    {
        // Call base first
        bool baseResult = base.UpdateTick(_world, _clrIdx, _blockPos, _blockValue, _bRandomTick, _ticksIfLoaded, _rnd);

        // Only process on server side
        if (_world.IsRemote())
        {
            return baseResult;
        }

        // Only apply buff if configured
        if (string.IsNullOrEmpty(activeBuff))
        {
            return baseResult;
        }

        // Find nearby players and apply buff
        Vector3 blockCenter = new Vector3(_blockPos.x + 0.5f, _blockPos.y + 0.5f, _blockPos.z + 0.5f);
        
        if (_world is World world)
        {
            foreach (Entity entity in world.Entities.list)
            {
                if (entity is EntityPlayer player && !player.IsDead())
                {
                    float distance = Vector3.Distance(player.position, blockCenter);
                    
                    if (distance <= buffRadius)
                    {
                        // Apply the buff - this will refresh it if already active
                        player.Buffs.AddBuff(activeBuff);
                    }
                }
            }
        }
        
        // Reschedule for next tick
        if (_world is World w2)
        {
            w2.GetWBT().AddScheduledBlockUpdate(_clrIdx, _blockPos, blockID, TICK_RATE);
        }

        return baseResult;
    }
}
