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

    private const float VerticalDetectionThreshold = 2.5f; // Target must be at least this far above/below
    private const float HorizontalRangeThreshold = 2.5f; // Horizontal distance to be considered "directly under"

    private string TaskName => nameof(EAIBreakBlocksIconic);
    
    // STATE TRANSITION TRACKING: Detect if jitter correlates with EAI state changes
    private static bool enableStateTransitionLogging = true;

    // New: state for moving to and breaking a selected block
    private bool hasBlockTarget;
    private Vector3i currentBlockTarget;
    private Vector3 blockApproachPos;
    private float approachRecalcTimer;
    private const float ApproachRecalcInterval = 0.5f; // seconds

    // Optional: remember original target to keep focus after block is gone
    private EntityAlive savedAttackTarget;

    public EAIBreakBlocksIconic()
    {
        
    }

    public override void Init(EntityAlive _theEntity)
    {        
        base.Init(_theEntity);
        MutexBits = 8;
        executeDelay = 0.15f;
    }

    public override bool CanExecute()
    {
        if (IsEntityStunned())
            return false;

        // Check if we should break blocks to reach the target
        if (IsJumpingAbort())
            return false;

        // Continue if we already have a valid block target
        if (HandleExistingBlockTargetContinuation())
            return true;

        // Check if we have an attack target and prerequisites
        if (!MovementPrereqsSatisfied())
            return false;

        // Only skip if on same level AND not stuck (if stuck, we should break through)
        if (EarlyOutIfTargetIsOnSameLevel() && theEntity.moveHelper.BlockedTime < 1.0f)
            return false;

        SelectNearestBlock();

        if (hasBlockTarget)
            return true;
        
        return false;
    }

    public override void Start()
    {
        attackDelay = 1;
        EstablishBlockTargetFromHitInfo();

        //if (!hasBlockTarget)
        //{
        //    Log.Out($"[{TaskName}] id={theEntity.entityId} START - No valid block target in HitInfo");
        //    return;
        //}

        LogStartBreaking();
    }

    public override void Update()
    {
        if (hasBlockTarget)
        {
            if (HandleApproachOrStopForAttack())
                return;
        }

        TickAttackDelayAndMaybeLog();

        if (attackDelay <= 0)
        {            
            AttackBlock();
        }
    }

    public override bool Continue()
    {
        // Only skip if on same level AND not stuck (if stuck, we should continue breaking)
        if (EarlyOutIfTargetIsOnSameLevel() && theEntity.moveHelper.BlockedTime < 1.0f)
            return false;

        if (!IsOnGroundOrElevator())
            return false;

        if (hasBlockTarget)
            return IsBlockStillPresentAndLog();

        bool can = CanExecute();        
        return can;
    }

    private bool IsEntityStunned()
    {
        if (theEntity.bodyDamage.CurrentStun != 0)
        {            
            return true;
        }
        return false;
    }
private bool HandleExistingBlockTargetContinuation()
    {
        if (!hasBlockTarget)
            return false;

        var bv = theEntity.world.GetBlock(currentBlockTarget);
        if (!bv.isair)
        {            
            return true;
        }
        
        hasBlockTarget = false;
        return false;
    }

    // Check if movement conditions are met to consider breaking blocks
    private bool MovementPrereqsSatisfied()
    {
        var moveHelper = theEntity.moveHelper;

        // Must have the ability to break blocks
        if (!moveHelper.CanBreakBlocks)
        {            
            return false;
        }

        // Break blocks if we're under the player (within 2.5 blocks horizontally)
        bool isUnderPlayer = CheckIfUnderThePlayer();
        if (isUnderPlayer)
        {
            return true;
        }

        // NEW: Check if stuck moving forward with player above within close range
        if (IsStuckMovingForwardWithPlayerAbove())
        {
            return true;
        }

        // OR break blocks if we've been blocked/stuck for 1 second
        if (moveHelper.BlockedTime >= 1.0f)
        {
            return true;
        }

        return false;
    }

    private bool IsJumpingAbort()
    {
        var moveHelper = theEntity.moveHelper;
        if (theEntity.Jumping && !moveHelper.IsDestroyArea)
        {            
            return true;
        }
        return false;
    }


    private bool CheckIfUnderThePlayer()
    {
        EntityAlive attackTarget = theEntity.GetAttackTarget();
        if (attackTarget == null || !attackTarget.IsAlive() || !(attackTarget is EntityPlayer))
        {
            return false;
        }
     
        Vector3 zombiePos = theEntity.position;
        Vector3 playerPos = attackTarget.position;

        // Check if directly under: horizontal (XZ) within 2.5 blocks
        float dx = Mathf.Abs(zombiePos.x - playerPos.x);
        float dz = Mathf.Abs(zombiePos.z - playerPos.z);
        bool directlyUnder = (dx <= 2.5f) && (dz <= 2.5f);
        if (!directlyUnder)
        {            
            return false;
        }
                
        return true;
        
    }

    /// <summary>
    /// Checks if the zombie is stuck moving forward with the player above them.
    /// Returns true if: player is within 3 blocks horizontally (X/Z), 
    /// player is 2+ blocks above vertically, and zombie is stuck/blocked.
    /// </summary>
    private bool IsStuckMovingForwardWithPlayerAbove()
    {
        EntityAlive attackTarget = theEntity.GetAttackTarget();
        if (attackTarget == null || !attackTarget.IsAlive() || !(attackTarget is EntityPlayer))
        {
            return false;
        }

        var moveHelper = theEntity.moveHelper;
        
        // Check if zombie is stuck/blocked moving forward (at least 0.5 seconds)
        if (moveHelper.BlockedTime < 0.5f)
        {
            return false;
        }

        Vector3 zombiePos = theEntity.position;
        Vector3 playerPos = attackTarget.position;

        // Check horizontal distance (X/Z plane) - within 3 blocks
        float dx = Mathf.Abs(zombiePos.x - playerPos.x);
        float dz = Mathf.Abs(zombiePos.z - playerPos.z);
        if (dx > 3f || dz > 3f)
        {
            return false;
        }

        // Check vertical distance - player must be at least 2 blocks above
        float verticalDiff = playerPos.y - zombiePos.y;
        if (verticalDiff < 2f)
        {
            return false;
        }

        // All conditions met: stuck, player nearby horizontally, player above
        return true;
    }
  
    private bool EarlyOutIfTargetIsOnSameLevel()
    {
        EntityAlive attackTarget = theEntity.GetAttackTarget();
        if (attackTarget == null || !attackTarget.IsAlive())
        {
            return false;
        }
        float verticalDiff = Mathf.Abs(attackTarget.position.y - theEntity.position.y);
        if (verticalDiff < 2f)
        {            
            return true;
        }
        return false;
    }
  
    /// <summary>
    /// Selects the nearest block to attack via a horizontal voxel raycast fan (Voxel.GetNextBlockHit): 20m range, ±45° arc.
    /// Only selects blocks that are above the entity's feet but below their head when under the player.
    /// When stuck on same level, allows blocks at any height.
    /// Falls back to previous logic if nothing is found.
    /// </summary>
    private void SelectNearestBlock()
    {
        EntityMoveHelper moveHelper = theEntity.moveHelper;        
        Vector3 entityPos = theEntity.position;
        
        // Calculate entity's vertical bounds (feet to head)
        float feetY = entityPos.y;
        float headY = entityPos.y + theEntity.GetEyeHeight();
        
        // If we're stuck on the same level as the player, allow any height for block selection
        bool isStuckOnSameLevel = EarlyOutIfTargetIsOnSameLevel() && moveHelper.BlockedTime >= 1.0f;
        if (isStuckOnSameLevel)
        {
            // Expand vertical range to allow breaking blocks at any height in front
            feetY = entityPos.y - 2f; // Allow blocks below
            headY = entityPos.y + 3f; // Allow blocks above head
        }
        
        int by = Mathf.FloorToInt(entityPos.y);
        // Fix the ray height at center of the entity's block for a horizontal sweep
        Vector3 start = new Vector3(entityPos.x, by + 0.5f, entityPos.z);

        // Use forward on XZ plane
        Vector3 fwd3 = theEntity.GetForwardVector();
        fwd3.y = 0f;
        if (fwd3.sqrMagnitude < 0.0001f)
        {
            fwd3 = theEntity.transform.forward;
            fwd3.y = 0f;
        }
        if (fwd3.sqrMagnitude < 0.0001f)
        {
            fwd3 = Vector3.forward; // hard fallback
        }
        fwd3.Normalize();

        Vector3i bestPos;
        float bestDistSq;
        TryVoxelArcForBlock(start, fwd3, 20f, 45f, feetY, headY, out bestPos, out bestDistSq);      
    }  

    [PublicizedFrom(EAccessModifier.Private)]
    public void AttackBlock()
    {
        theEntity.SetLookPosition(Vector3.zero);
        if (!(theEntity.inventory.holdingItemData.actionData[0] is ItemActionAttackData itemActionAttackData))
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
            theEntity.Attack(_isReleased: true);
        }
    }

   
    private void EstablishBlockTargetFromHitInfo()
    {        
        var hitPos = theEntity.moveHelper.HitInfo.hit.blockPos;
        var bv = theEntity.world.GetBlock(hitPos);
        if (!bv.isair && bv.Block.MaxDamage > 0)
        {
            SetBlockTarget(hitPos);            
        }
    }

    private void LogStartBreaking()
    {
        BlockValue blockValue = theEntity.world.GetBlock(currentBlockTarget);
        Block block = blockValue.Block;

        if (block.HasTag(BlockTags.Door) || block.HasTag(BlockTags.ClosetDoor))
        {
            theEntity.IsBreakingDoors = true;            
        }
    }



    private bool IsOnGroundOrElevator()
    {
        bool onGroundOrElevator = theEntity.onGround || theEntity.IsInElevator();
        return onGroundOrElevator;
    }

    private bool IsBlockStillPresentAndLog()
    {
        var bv = theEntity.world.GetBlock(currentBlockTarget);
        bool stillThere = !bv.isair;        
        return stillThere;
    }



    private bool HandleApproachOrStopForAttack()
    {
        // Recompute approach pos occasionally (in case entity/block moved)
        approachRecalcTimer -= 0.05f;
        if (approachRecalcTimer <= 0f)
        {
            approachRecalcTimer = ApproachRecalcInterval;
            blockApproachPos = ComputeApproachPosition(currentBlockTarget);
        }

        Vector3 blockCenter = GetBlockCenter(currentBlockTarget);
        float attackRange = GetBlockAttackRange();
        float distToBlock = Vector3.Distance(theEntity.position, blockCenter);

        // Always look at the block while we are targeting it
        theEntity.SetLookPosition(blockCenter);

        if (distToBlock > attackRange * 0.95f)
        {
            // Move closer to the block
            theEntity.moveHelper.SetMoveTo(blockApproachPos, true); // allow breaking while approaching          
            return true;
        }

        // Close enough to attack; stop movement for accuracy
        theEntity.moveHelper.Stop();
        return false;
    }

    private void TickAttackDelayAndMaybeLog()
    {
        if (attackDelay > 0)
        {
            attackDelay--;          
        }
    }

    public override void Reset()
    {
        // STATE TRANSITION LOG: Task ending
        //if (enableStateTransitionLogging && !theEntity.isEntityRemote)
        //{
        //    Log.Out($"[EAI-STATE] Entity:{theEntity.entityId} BreakBlocks STOPPING");
        //}
        
        theEntity.IsBreakingBlocks = false;
        theEntity.IsBreakingDoors = false;

        // Clear current block target and restore original target state
        hasBlockTarget = false;
        savedAttackTarget = null;
    }


    [PublicizedFrom(EAccessModifier.Private)]
    public WorldRayHitInfo GetHitInfo(out float damageScale)
    {
        EntityMoveHelper moveHelper = theEntity.moveHelper;
        damageScale = moveHelper.DamageScale + damageBoostPercent;

        // Ensure hit info is set to the selected block target
        if (hasBlockTarget)
        {
            moveHelper.HitInfo.hit.blockPos = currentBlockTarget;
            moveHelper.HitInfo.hit.pos = GetBlockCenter(currentBlockTarget);
            moveHelper.HitInfo.bHitValid = true;
        }

        // SAFETY: Vanilla ItemAction.GetDismemberChance calls Extensions.ContainsCaseInsensitive on hitInfo.tag.
        // If the tag is null, it can throw a NullReferenceException. Ensure it's always populated for block attacks.
        if (string.IsNullOrEmpty(moveHelper.HitInfo.tag))
        {
            moveHelper.HitInfo.tag = "B_Mesh"; // consistent with Voxel.BlockHit tagging for blocks            
        }

        return moveHelper.HitInfo;
    }

    // ===== Helpers for block targeting and approach =====

    private void SetBlockTarget(Vector3i blockPos)
    {
        currentBlockTarget = blockPos;
        hasBlockTarget = true;
        blockApproachPos = ComputeApproachPosition(blockPos);
        approachRecalcTimer = ApproachRecalcInterval;

        savedAttackTarget = theEntity.GetAttackTarget();

        // Set move helper hit info to ensure attacks use this block
        theEntity.moveHelper.HitInfo.hit.blockPos = blockPos;
        theEntity.moveHelper.HitInfo.hit.pos = GetBlockCenter(blockPos);
        theEntity.moveHelper.HitInfo.bHitValid = true;

        // Also ensure tag is populated for safety
        if (string.IsNullOrEmpty(theEntity.moveHelper.HitInfo.tag))
        {
            theEntity.moveHelper.HitInfo.tag = "B_Mesh";
        }
        
    }

    private Vector3 ComputeApproachPosition(Vector3i blockPos)
    {
        // Approach slightly in front of the block center, on current Y level for better navigation
        Vector3 center = GetBlockCenter(blockPos);
        Vector3 toBlock = center - theEntity.position;
        toBlock.y = 0f;
        if (toBlock.sqrMagnitude < 0.0001f)
        {
            toBlock = theEntity.transform.forward;
        }
        toBlock.Normalize();

        float radius = theEntity.m_characterController.GetRadius();
        float desiredDist = Mathf.Max(0.9f, radius + 0.6f);

        Vector3 approach = center - toBlock * desiredDist;
        // Keep horizontal pursuit style: flatten Y for movement
        approach.y = theEntity.position.y;
        return approach;
    }

    private Vector3 GetBlockCenter(Vector3i blockPos)
    {
        return new Vector3(blockPos.x + 0.5f, blockPos.y + 0.5f, blockPos.z + 0.5f);
    }

    private float GetBlockAttackRange()
    {
        // Mirror normal melee range logic
        ItemValue holdingItemItemValue = theEntity.inventory.holdingItemItemValue;
        int holdingItemIdx = theEntity.inventory.holdingItemIdx;
        ItemAction itemAction = holdingItemItemValue.ItemClass.Actions[holdingItemIdx];
        float range = 1.095f;
        if (itemAction != null)
        {
            range = itemAction.Range;
            if (range == 0f)
            {
                range = EffectManager.GetItemValue(PassiveEffects.MaxRange, holdingItemItemValue);
            }
        }
        float effective = Utils.FastMax(0.7f, range - 0.35f);
        return effective;
    }

 
    private bool TryVoxelArcForBlock(Vector3 start, Vector3 fwd3, float maxDist, float halfAngleDeg, float feetY, float headY, out Vector3i bestPos, out float bestDistSq)
    {
        bestPos = default(Vector3i);
        bestDistSq = float.MaxValue;
        bool foundAny = false;

        const int rayCount = 13; // ~7.5° increments over ±45°
        float totalArc = halfAngleDeg * 2f;
        float stepDeg = (rayCount > 1) ? (totalArc / (rayCount - 1)) : totalArc;
        float startDeg = -halfAngleDeg;

        for (int i = 0; i < rayCount; i++)
        {
            float angleDeg = startDeg + i * stepDeg;
            float angleRad = angleDeg * Mathf.Deg2Rad;
            Vector2 dirXZ = RotateXZ(new Vector2(fwd3.x, fwd3.z), angleRad);
            if (dirXZ.sqrMagnitude < 1e-6f)
                continue;

            Vector3 dir = new Vector3(dirXZ.x, 0f, dirXZ.y).normalized;

            Ray ray = new Ray(start, dir);
            // Hitmask: NotMoveable (movement-collidable blocks)
            int hitMask = Voxel.HM_NotMoveable;
            bool hit = Voxel.GetNextBlockHit(theEntity.world, ray, maxDist, hitMask, false);
            if (!hit || !Voxel.voxelRayHitInfo.bHitValid)
                continue;

            Vector3i pos = Voxel.voxelRayHitInfo.hit.blockPos;
            
            // IMPORTANT: Only target blocks that are above feet but below head
            float blockY = pos.y + 0.5f; // center of block
            if (blockY < feetY || blockY > headY)
            {
                continue; // Skip blocks outside vertical range
            }
            
            BlockValue bv = theEntity.world.GetBlock(pos);            
            Block block = bv.Block;
            if (bv.isair || block == null)
                continue;

            // Ensure breakable and movement-blocking
            if (block.MaxDamage <= 0 || block.IsTerrainDecoration || !block.IsMovementBlocked(theEntity.world, pos, bv, BlockFace.None))
                continue;

            currentBlockTarget = pos; // set current target for logging
            hasBlockTarget = true;

            // Use hit position if present, otherwise fallback to block center
            Vector3 hitPoint = Voxel.voxelRayHitInfo.hit.pos != Vector3.zero ? Voxel.voxelRayHitInfo.hit.pos : new Vector3(pos.x + 0.5f, start.y, pos.z + 0.5f);
            float d2 = (hitPoint - start).sqrMagnitude;
            if (d2 < bestDistSq)
            {
                bestDistSq = d2;
                bestPos = pos;
                foundAny = true;
                theEntity.lookAtPosition = hitPoint; // look at the hit point
            }
        }

        return foundAny;
    }

    private static Vector2 RotateXZ(Vector2 v, float radians)
    {
        float c = Mathf.Cos(radians);
        float s = Mathf.Sin(radians);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}