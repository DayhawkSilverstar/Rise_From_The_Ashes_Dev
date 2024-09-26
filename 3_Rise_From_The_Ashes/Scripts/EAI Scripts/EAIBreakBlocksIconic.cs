using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class EAIBreakBlocksIconic : EAIBreakBlock
{
    private IconicZombie zombie;

    private const float cDamageBoostPerAlly = 0.2f;

    private int attackDelay;

    private float damageBoostPercent;

    private List<Entity> allies = new List<Entity>();

    public EAIBreakBlocksIconic() : base()
    {
#if DEBUG
        Log.Out("Created EAIBreakBlocksIconic");
#endif
    }

    public override void Start()
    {
#if DEBUG
        Log.Out("EAIBreakBlocksIconic : Start");
#endif
        attackDelay = 1;
        var blockPos = theEntity.moveHelper.HitInfo.hit.blockPos;
        var block = theEntity.world.GetBlock(blockPos).Block;
        if (block.HasTag(BlockTags.Door) || block.HasTag(BlockTags.ClosetDoor))
        {
            theEntity.IsBreakingDoors = true;
        }
    }

    public override void Init(EntityAlive _theEntity)
    {
#if DEBUG
        Log.Out("EAIBreakBlocksIconic : Init");
#endif
        zombie = _theEntity as IconicZombie;
        base.Init(_theEntity);
        MutexBits = 8;
        executeDelay = 0.15f;
    }

    public override bool Continue()
    {
#if DEBUG
        Log.Out("EAIBreakBlocksIconic : Continue");
#endif
        return theEntity.bodyDamage.CurrentStun == 0 && theEntity.onGround && CanExecute();
    }

    public override void Update()
    {
#if DEBUG
        Log.Out("EAIBreakBlocksIconic : Update");
#endif
        if (attackDelay > 0)
        {
            attackDelay--;
        }

        if (attackDelay <= 0)
        {
#if DEBUG
            Log.Out("Calling Attack Block");
#endif  
            AttackBlock();
        }
    }

    public override bool CanExecute()
    {
#if DEBUG
        Log.Out("EAIBreakBlocksIconic : CanExecute");
#endif
        // Get the move helper for the entity
        var moveHelper = theEntity.moveHelper;

        // Check if the target is above or below the zombie
        bool targetAbove = zombie.TargetAbove();
        bool targetBelow = zombie.TargetBelow();

#if DEBUG
        Log.Out("Target above: " + targetAbove + " Target below: " + targetBelow);
#endif

        // If the target is above or below, set the destroy area flag
        if (zombie.Target != null && (targetAbove || targetBelow))
        {
#if DEBUG
            if (targetAbove)
            {
                Log.Out("Target is above, so attack block.");
            }
            if (targetBelow)
            {
                Log.Out("Target is below, so attack block.");
            }
#endif
            moveHelper.IsDestroyArea = true;
        }

        // If the destroy area flag is not set or the entity is stunned, return false
        if (!moveHelper.IsDestroyArea || theEntity.bodyDamage.CurrentStun != 0)
        {
#if DEBUG
            Log.Out("EAIBreakBlocksIconic : DestroyArea = false or Stunned = true");
#endif
            return false;
        }

#if DEBUG
        Log.Out("EAIBreakBlocksIconic : CanBreakBlocks = " + moveHelper.CanBreakBlocks.ToString());
        Log.Out("EAIBreakBlocksIconic : BlockedTime = " + zombie.blockedTime.ToString());
#endif

        // If the entity has been blocked for more than 0.35 seconds and can break blocks
        if (moveHelper.CanBreakBlocks)
        {
#if DEBUG
            Log.Out("Can Break Blocks and Blocked Time is True");
#endif
            // Check if the block in front is air, if so, return false
            if (moveHelper.HitInfo != null)
            {
                var blockPos = moveHelper.HitInfo.hit.blockPos;
                if (theEntity.world.GetBlock(blockPos).isair)
                {
#if DEBUG
                    Log.Out("Block is air so attack block is false.");
#endif
                    return false;
                }

                // Calculate the blocked distance squared and the threshold distance squared
                var blockedDistanceSquared = moveHelper.CalcBlockedDistanceSq();
                var characterRadius = theEntity.m_characterController.GetRadius();
                var thresholdDistance = characterRadius + 0.6f;
                var thresholdDistanceSquared = thresholdDistance * thresholdDistance;

                // If the blocked distance is within the threshold, return true
                if (blockedDistanceSquared <= thresholdDistanceSquared)
                {
#if DEBUG
                    Log.Out("BREAK BLOCKS");
#endif
                    return true;
                }
            }
        }

#if DEBUG
        Log.Out("Target is not above or below");
#endif
        return false;
    }

    public override void Reset()
    {
        theEntity.IsBreakingBlocks = false;
        theEntity.IsBreakingDoors = false;
    }

    private new void AttackBlock()
    {
        // Set the look position of the entity to zero
        theEntity.SetLookPosition(Vector3.zero);

        // Get the attack data from the holding item
        var itemActionAttackData = theEntity.inventory.holdingItemData.actionData[0] as ItemActionAttackData;
        if (itemActionAttackData == null)
        {
#if DEBUG
            Log.Out("Missing ITEM attack data");
#endif
            return;
        }

        // Initialize damage boost percentage
        damageBoostPercent = 0f;

        // Check if the entity is a zombie
        if (theEntity is EntityZombie)
        {
            // Define the bounds to search for allies
            var bounds = new Bounds(theEntity.position, new Vector3(1.7f, 1.5f, 1.7f));
            theEntity.world.GetEntitiesInBounds(typeof(EntityZombie), bounds, allies);

            // Calculate damage boost based on the number of nearby allies
            for (int i = allies.Count - 1; i >= 0; i--)
            {
                if ((EntityZombie)allies[i] != theEntity)
                {
                    damageBoostPercent += 0.1f;
                    if (damageBoostPercent >= 0.5f)
                    {
                        break;
                    }
                }
            }

            // Clear the allies list
            allies.Clear();
        }

        // Perform the attack if possible
        if (theEntity.Attack(false))
        {
            theEntity.IsBreakingBlocks = true;

            // Calculate the attack delay
            var baseDelay = 0.25f + base.RandomFloat * 0.8f;
            if (theEntity.moveHelper.IsUnreachableAbove)
            {
                baseDelay *= 0.5f;
            }

            attackDelay = (int)((baseDelay + 0.75f) * 20f);
            itemActionAttackData.hitDelegate = GetHitInfo;

#if DEBUG
            Log.Out("Making the attack");
#endif

            // Perform the attack
            theEntity.Attack(true);
        }
    }

    private new WorldRayHitInfo GetHitInfo(out float damageScale)
    {
        var moveHelper = theEntity.moveHelper;
        damageScale = moveHelper.DamageScale + damageBoostPercent;
        return moveHelper.HitInfo;
    }
}
