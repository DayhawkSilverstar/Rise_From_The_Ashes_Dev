using System;
using System.Collections.Generic;
using GamePath;
using UnityEngine;
using UnityEngine.Scripting;
using static XUiC_DropDown;

public class EAIBreakBlockAggressive : EAIBase
{

    private const float cDamageBoostPerAlly = 0.2f;

    private int attackDelay;

    private float damageBoostPercent;

    private List<Entity> allies = new List<Entity>();

    public override void Init(EntityAlive _theEntity)
    {
        base.Init(_theEntity);
        MutexBits = 8;
        executeDelay = 0.15f;
    }

    public override bool CanExecute()
    {
        Log.Out("BreakBlockAggresive - CanExecute");
        IconicZombie iz = theEntity as IconicZombie;
        if (iz.ExpiryTicks <= 0)
        {                        
            Log.Out("BreakBlockAggresive - Verify ExpiryTicks : " + iz.ExpiryTicks.ToString()); 
        }

        EntityMoveHelper moveHelper = theEntity.moveHelper;
        if ((theEntity.Jumping && !moveHelper.IsDestroyArea) || theEntity.bodyDamage.CurrentStun != 0)
        {
            return false;
        }

        if (moveHelper.BlockedTime > 0.05f && moveHelper.CanBreakBlocks)
        {
            if (moveHelper.HitInfo != null)
            {
                Vector3i blockPos = moveHelper.HitInfo.hit.blockPos;
                if (theEntity.world.GetBlock(blockPos).isair)
                {
                    return false;
                }
            }

            float num = moveHelper.CalcBlockedDistanceSq();
            float num2 = theEntity.m_characterController.GetRadius() + 0.6f;
            if (num <= num2 * num2)
            {
                return true;
            }
        }

        return false;
    }

    public float CalcBlockedDistanceSq()
    {
        EntityMoveHelper moveHelper = theEntity.moveHelper;
        Vector3 pos = moveHelper.HitInfo.hit.blockPos;
        Vector3 position = theEntity.position;
        float num = pos.x - position.x;
        float num2 = pos.z - position.z;
        return num * num + num2 * num2;
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
    }

    public override bool Continue()
    {
        if (theEntity.bodyDamage.CurrentStun != 0)
        {
            return false;
        }

        if (theEntity.onGround)
        {
            return CanExecute();
        }

        return false;
    }

    public override void Update()
    {
        Log.Out("BreakBlockAggresive - Update");
        _ = theEntity.moveHelper;
        if (attackDelay > 0)
        {
            attackDelay--;
        }

        if (attackDelay <= 0)
        {
            AttackBlock();
        }
    }

    public override void Reset()
    {
        theEntity.IsBreakingBlocks = false;
        theEntity.IsBreakingDoors = false;
    }

    private void AttackBlock()
    {
        Log.Out("BreakBlockAggresive - AttackBlock");
        theEntity.SetLookPosition(Vector3.zero);
        ItemActionAttackData itemActionAttackData = theEntity.inventory.holdingItemData.actionData[0] as ItemActionAttackData;
        if (itemActionAttackData == null)
        {
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

            allies.Clear();
        }

        if (theEntity.Attack(_bAttackReleased: false))
        {
            theEntity.IsBreakingBlocks = true;
            itemActionAttackData.hitDelegate = GetHitInfo;
            theEntity.Attack(_bAttackReleased: true);
            IconicZombie iz = theEntity as IconicZombie;
            iz.ExpiryTicks = 5;
        }
    }

    private WorldRayHitInfo GetHitInfo(out float damageScale)
    {
        EntityMoveHelper moveHelper = theEntity.moveHelper;
        damageScale = moveHelper.DamageScale + damageBoostPercent;
        return moveHelper.HitInfo;
    }
}
