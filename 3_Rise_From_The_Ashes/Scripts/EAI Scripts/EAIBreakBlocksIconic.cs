using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class EAIBreakBlocksIconic : EAIBase
{
    [PublicizedFrom(EAccessModifier.Private)]
    public const float cDamageBoostPerAlly = 0.2f;

    [PublicizedFrom(EAccessModifier.Private)]
    public int attackDelay;

    [PublicizedFrom(EAccessModifier.Private)]
    public float damageBoostPercent;

    [PublicizedFrom(EAccessModifier.Private)]
    public List<Entity> allies = new List<Entity>();

    private string TaskName => nameof(EAIBreakBlocksIconic);

    public EAIBreakBlocksIconic()
    {
        Log.Out("EAIBreakBlocksIconic Constructor");
    }

    public override void Init(EntityAlive _theEntity)
    {
        Log.Out("EAIBreakBlocksIconic Init");
        base.Init(_theEntity);
        MutexBits = 8;
        executeDelay = 0.15f;
        IconicLog.Info(theEntity, TaskName, $"Init: mutex={MutexBits} execDelay={executeDelay}");
    }

    public override bool CanExecute()
    {
        if (theEntity.bodyDamage.CurrentStun != 0)
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: stunned");
            return false;
        }

        EntityMoveHelper moveHelper = theEntity.moveHelper;
        if (moveHelper.BlockedTime < 0.35f || !moveHelper.CanBreakBlocks)
        {
            IconicLog.Trace(theEntity, TaskName, $"CanExecute=false: blockedTime={moveHelper.BlockedTime:0.00} canBreak={moveHelper.CanBreakBlocks}");
            return false;
        }

        if (theEntity.Jumping && !moveHelper.IsDestroyArea)
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: jumping and not destroy area");
            return false;
        }

        int num = ((theEntity.crouchType == 0 && theEntity.physicsHeight >= 1f) ? 7 : 5);
        if ((moveHelper.BlockedFlags & num) > 0)
        {
            Vector3i blockPos = moveHelper.HitInfo.hit.blockPos;
            if (theEntity.world.GetBlock(blockPos).isair)
            {
                IconicLog.Trace(theEntity, TaskName, "CanExecute=false: hit block is air");
                return false;
            }

            float num2 = moveHelper.CalcBlockedDistanceSq();
            float num3 = theEntity.m_characterController.GetRadius() + 0.7f;
            if (num2 <= num3 * num3)
            {
                IconicLog.Debug(theEntity, TaskName, $"CanExecute=true: flags=0x{moveHelper.BlockedFlags:X} distSq={num2:0.000} thr={num3 * num3:0.000} pos={blockPos}");
                return true;
            }
        }

        IconicLog.Trace(theEntity, TaskName, $"CanExecute=false: flags=0x{moveHelper.BlockedFlags:X}");
        return false;
    }

    public override void Start()
    {
        attackDelay = 1;
        Vector3i blockPos = theEntity.moveHelper.HitInfo.hit.blockPos;
        Block block = theEntity.world.GetBlock(blockPos).Block;
        if (block.HasTag(BlockTags.Door) || block.HasTag(BlockTags.ClosetDoor))
        {
            theEntity.IsBreakingDoors = true;
        }
        IconicLog.Info(theEntity, TaskName, $"Start: targetBlock={block.GetBlockName()} at {blockPos} breakingDoors={theEntity.IsBreakingDoors}");
    }

    public override bool Continue()
    {
        if (theEntity.onGround || theEntity.IsInElevator())
        {
            bool can = CanExecute();
            if (!can)
            {
                IconicLog.Trace(theEntity, TaskName, "Continue=false: CanExecute returned false");
            }
            return can;
        }

        IconicLog.Trace(theEntity, TaskName, "Continue=false: not onGround and not in elevator");
        return false;
    }

    public override void Update()
    {
        _ = theEntity.moveHelper;
        if (attackDelay > 0)
        {
            attackDelay--;
        }

        if (attackDelay <= 0)
        {
            IconicLog.Debug(theEntity, TaskName, "Update: triggering AttackBlock");
            AttackBlock();
        }
    }

    public override void Reset()
    {
        theEntity.IsBreakingBlocks = false;
        theEntity.IsBreakingDoors = false;
        IconicLog.Info(theEntity, TaskName, "Reset: cleared breaking flags");
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public void AttackBlock()
    {
        theEntity.SetLookPosition(Vector3.zero);
        if (!(theEntity.inventory.holdingItemData.actionData[0] is ItemActionAttackData itemActionAttackData))
        {
            IconicLog.Trace(theEntity, TaskName, "AttackBlock: no ItemActionAttackData, abort");
            return;
        }

        damageBoostPercent = 0f;
        if (theEntity is EntityZombie)
        {
            Bounds bb = new Bounds(theEntity.position, new Vector3(1.7f, 1.5f, 1.7f));
            theEntity.world.GetEntitiesInBounds(typeof(EntityZombie), bb, allies);
            for (int num = allies.Count - 1; num >= 0; num--)
            {
                if ((EntityZombie)allies[num] != theEntity)
                {
                    damageBoostPercent += 0.2f;
                }
            }

            IconicLog.Trace(theEntity, TaskName, $"AttackBlock: allies={allies.Count} dmgBoost={damageBoostPercent:0.00}");
            allies.Clear();
        }

        if (theEntity.Attack(_isReleased: false))
        {
            theEntity.IsBreakingBlocks = true;
            float num2 = 0.25f + base.RandomFloat * 0.8f;
            if (theEntity.moveHelper.IsUnreachableAbove)
            {
                num2 *= 0.5f;
            }

            attackDelay = (int)((num2 + 0.75f) * 20f);
            itemActionAttackData.hitDelegate = GetHitInfo;
            IconicLog.Debug(theEntity, TaskName, $"AttackBlock: swing start, nextDelayTicks={attackDelay}");
            theEntity.Attack(_isReleased: true);
        }
        else
        {
            IconicLog.Trace(theEntity, TaskName, "AttackBlock: Attack(false) failed");
        }
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public WorldRayHitInfo GetHitInfo(out float damageScale)
    {
        EntityMoveHelper moveHelper = theEntity.moveHelper;
        damageScale = moveHelper.DamageScale + damageBoostPercent;
        IconicLog.Trace(theEntity, TaskName, $"GetHitInfo: damageScale={damageScale:0.00} flags=0x{moveHelper.BlockedFlags:X}");
        return moveHelper.HitInfo;
    }
}