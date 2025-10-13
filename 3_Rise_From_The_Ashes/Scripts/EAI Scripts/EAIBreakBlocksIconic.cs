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
    private const float HorizontalRangeThreshold = 10f; // Horizontal distance must be within this

    private string TaskName => nameof(EAIBreakBlocksIconic);

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
        Log.Out($"[{TaskName}] Constructor called");
    }

    public override void Init(EntityAlive _theEntity)
    {
        Log.Out($"[{TaskName}] Init - EntityID: {_theEntity.entityId}, Name: {_theEntity.EntityName}");
        base.Init(_theEntity);
        MutexBits = 8;
        executeDelay = 0.15f;
    }

    public override bool CanExecute()
    {
        if (IsEntityStunned())
            return false;

        if (EarlyOutIfTargetIsOnSameLevel())
            return false;

        // Check if we have an attack target
        if (!MovementPrereqsSatisfied())
            return false;

        // Check if we should break blocks to reach the target
        if (IsJumpingAbort())
            return false;

        // Continue if we already have a valid block target
        if (HandleExistingBlockTargetContinuation())
            return true;

        SelectNearestBlock();

        if (hasBlockTarget)
            return true;
        
        return false;
    }

    public override void Start()
    {
        attackDelay = 1;
        EstablishBlockTargetFromHitInfo();

        if (!hasBlockTarget)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} START - No valid block target in HitInfo");
            return;
        }

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
            Log.Out($"[{TaskName}] id={theEntity.entityId} Update - Calling AttackBlock");
            AttackBlock();
        }
    }

    public override bool Continue()
    {
        if (EarlyOutIfTargetIsOnSameLevel())
            return false;

        if (!IsOnGroundOrElevator())
            return false;

        if (hasBlockTarget)
            return IsBlockStillPresentAndLog();

        bool can = CanExecute();
        Log.Out($"[{TaskName}] id={theEntity.entityId} Continue={can}");
        return can;
    }

    private bool IsEntityStunned()
    {
        if (theEntity.bodyDamage.CurrentStun != 0)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute=FALSE - Entity is stunned");
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
            Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute=TRUE - Continuing toward current block target {currentBlockTarget}");
            return true;
        }

        Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute: clearing stale block target {currentBlockTarget} (air)");
        hasBlockTarget = false;
        return false;
    }

    // Check if movement conditions are met to consider breaking blocks
    private bool MovementPrereqsSatisfied()
    {
        var moveHelper = theEntity.moveHelper;

        if (CheckIfUnderThePlayer())
        {            
            return true;
        }    

        
        // Must have an attack target
        if (moveHelper.BlockedTime < 0.35f || !moveHelper.CanBreakBlocks)
        {
            if (moveHelper.BlockedTime < 0.35f)
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} MovementPrereqsSatisfied CanExecute=FALSE - BlockedTime too short: {moveHelper.BlockedTime:F2}s");
            }
            else
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} MovementPrereqsSatisfied CanExecute=FALSE - CanBreakBlocks is false");
            }
            return false;
        }

        return true;
    }

    private bool IsJumpingAbort()
    {
        var moveHelper = theEntity.moveHelper;
        if (theEntity.Jumping && !moveHelper.IsDestroyArea)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute=FALSE - Entity is jumping");
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

        // Height does not matter; check horizontal (XZ) within 10 blocks using axis bounds
        float dx = Mathf.Abs(zombiePos.x - playerPos.x);
        float dz = Mathf.Abs(zombiePos.z - playerPos.z);
        bool within10 = (dx <= 10f) && (dz <= 10f);
        if (!within10)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} CheckIfUnderThePlayer=FALSE - Horizontal dx={dx:F2}, dz={dz:F2} > 10");
            return false;
        }
        
        Log.Out($"[{TaskName}] id={theEntity.entityId} CheckIfUnderThePlayer=TRUE - Horizontal dx={dx:F2}, dz={dz:F2} <= 10 (vertical ignored)");
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
            Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute=FALSE - Target is on same level (verticalDiff={verticalDiff:F2})");
            return true;
        }
        return false;
    }

    private bool EvaluateBlockAndMaybeSelect()
    {
        EntityMoveHelper moveHelper = theEntity.moveHelper;



        Vector3i blockPos = moveHelper.HitInfo.hit.blockPos;
        BlockValue blockValue = theEntity.world.GetBlock(blockPos);

        Log.Out($"[{TaskName}] id={theEntity.entityId} EvaluateBlockAndMaybeSelect Blocked by block at {blockPos}, IsAir: {blockValue.isair}, Block: {blockValue.Block.GetBlockName()}");

        if (blockValue.isair)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} EvaluateBlockAndMaybeSelect CanExecute=FALSE - Block is air");
            return false;
        }

        // Check if the block is breakable
        float num2 = moveHelper.CalcBlockedDistanceSq();
        float num3 = theEntity.m_characterController.GetRadius() + 0.7f;
        float requiredDistSq = num3 * num3;
        float requiredDistSqWithSlack = requiredDistSq * 1.44f; // ~20% linear slack (since squared)

        Log.Out($"[{TaskName}] id={theEntity.entityId} EvaluateBlockAndMaybeSelect Distance check - BlockedDistSq: {num2:F2}, Required: {requiredDistSq:F2}, WithSlack: {requiredDistSqWithSlack:F2}");

        // If within required distance, select the block and proceed
        if (num2 <= requiredDistSqWithSlack)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} EvaluateBlockAndMaybeSelect CanExecute=TRUE - Calling SelectNearestBlock (within slack)");
            SelectNearestBlock();
            return true;
        }

        Log.Out($"[{TaskName}] id={theEntity.entityId} EvaluateBlockAndMaybeSelect Distance slightly too far - attempting SelectNearestBlock to run CenterHeightSearch anyway");
        SelectNearestBlock();
        if (moveHelper.HitInfo.bHitValid)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} EvaluateBlockAndMaybeSelect CanExecute=TRUE - Proceeding with selected block despite distance");
            return true;
        }

        Log.Out($"[{TaskName}] id={theEntity.entityId} EvaluateBlockAndMaybeSelect CanExecute=FALSE - Block too far away and no valid selection found");
        return false;
    }

    /// <summary>
    /// Checks if the zombie should enter block breaking mode when target is above/below
    /// and zombie is within horizontal range.
    /// </summary>
    private bool CheckVerticalBlockBreaking()
    {
        Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute=TRUE - Vertical block breaking mode activated");

        // Get the attack target
        EntityAlive attackTarget = theEntity.GetAttackTarget();
        if (attackTarget == null || !attackTarget.IsAlive())
        {
            return false;
        }

        Vector3 zombiePos = theEntity.position;
        Vector3 targetPos = attackTarget.position;

        // Calculate horizontal (XZ) distance
        Vector3 xzDiff = targetPos - zombiePos;
        xzDiff.y = 0f;
        float horizontalDist = xzDiff.magnitude;

        // Calculate vertical (Y) distance
        float verticalDist = targetPos.y - zombiePos.y;
        float absVerticalDist = Mathf.Abs(verticalDist);

        Log.Out($"[{TaskName}] id={theEntity.entityId} VerticalCheck - HorzDist: {horizontalDist:F2}, VertDist: {verticalDist:F2}");

        // Check if target is significantly above/below AND we're close horizontally
        if (absVerticalDist >= VerticalDetectionThreshold && horizontalDist <= HorizontalRangeThreshold)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} ✓ Target is {(verticalDist > 0 ? "ABOVE" : "BELOW")} and within horizontal range!");

            // Find a block to break between zombie and target
            if (FindVerticalBlockToBreak(zombiePos, targetPos, verticalDist > 0))
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} ✓ Found vertical block to break!");
                return true;
            }
            else
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} ✗ No breakable block found in vertical path");
            }
        }

        return false;
    }

    /// <summary>
    /// Finds a block to break when target is above or below the zombie.
    /// Returns true if a valid block is found and sets it in moveHelper.HitInfo.
    /// </summary>
    private bool FindVerticalBlockToBreak(Vector3 zombiePos, Vector3 targetPos, bool targetIsAbove)
    {
        World world = theEntity.world;
        EntityMoveHelper moveHelper = theEntity.moveHelper;

        // Start scanning from zombie's position
        Vector3i scanPos = World.worldToBlockPos(zombiePos);

        // Scan direction (up or down)
        int yDirection = targetIsAbove ? 1 : -1;

        // Scan vertically looking for solid blocks
        int scanSteps = Mathf.Min((int)Mathf.Abs(targetPos.y - zombiePos.y) + 2, 10);

        Log.Out($"[{TaskName}] id={theEntity.entityId} FindVerticalBlock - Start: {scanPos}, Direction: {(targetIsAbove ? "UP" : "DOWN")}, Steps: {scanSteps}");

        for (int step = 1; step <= scanSteps; step++)
        {
            Vector3i checkPos = new Vector3i(scanPos.x, scanPos.y + (yDirection * step), scanPos.z);
            BlockValue blockValue = world.GetBlock(checkPos);

            if (!blockValue.isair && blockValue.Block.IsMovementBlocked(world, checkPos, blockValue, BlockFace.None))
            {
                // Check if the block is breakable
                if (blockValue.Block.MaxDamage > 0 && !blockValue.Block.IsTerrainDecoration)
                {
                    Log.Out($"[{TaskName}] id={theEntity.entityId} Found breakable block: {blockValue.Block.GetBlockName()} at {checkPos}");

                    // Set this as the target block in moveHelper.HitInfo
                    moveHelper.HitInfo.hit.blockPos = checkPos;
                    moveHelper.HitInfo.hit.pos = new Vector3(checkPos.x + 0.5f, checkPos.y + 0.5f, checkPos.z + 0.5f);
                    moveHelper.HitInfo.bHitValid = true;

                    return true;
                }
                else
                {
                    Log.Out($"[{TaskName}] id={theEntity.entityId} Block {blockValue.Block.GetBlockName()} at {checkPos} is not breakable");
                }
            }
        }

        Log.Out($"[{TaskName}] id={theEntity.entityId} No breakable blocks found in vertical scan");
        return false;
    }

    /// <summary>
    /// Selects the nearest block to attack via a horizontal voxel raycast fan (Voxel.GetNextBlockHit): 20m range, ±45° arc.
    /// Falls back to previous logic if nothing is found.
    /// </summary>
    private void SelectNearestBlock()
    {
        EntityMoveHelper moveHelper = theEntity.moveHelper;
        Log.Out($"[{TaskName}] id={theEntity.entityId} SelectNearestBlock called");
        Vector3 entityPos = theEntity.position;
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
        TryVoxelArcForBlock(start, fwd3, 20f, 45f, out bestPos, out bestDistSq);      
    }

    // Restored helper: original horizontal handling with center-height search fallback
    private void HandleOnlyHorizontalBlock(EntityMoveHelper moveHelper)
    {
        Vector3 entityPos = theEntity.position;
        Vector3i horizPos = moveHelper.HitInfo.hit.blockPos;
        float horizCenterY = horizPos.y + 0.5f;
        float entityCenterY = entityPos.y; // entity position is roughly center height

        bool footLevel = (entityCenterY - horizCenterY) > 0.6f;
        Log.Out($"[{TaskName}] id={theEntity.entityId} Foot-level check - entityCenterY={entityCenterY:F2}, horizCenterY={horizCenterY:F2}, diff={(entityCenterY - horizCenterY):F2}, footLevel={footLevel}");

        if (footLevel)
        {
            TryCenterHeightSearchOrFallback(moveHelper, horizPos);
        }
        else
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} Skipping CenterHeightSearch - not foot-level");
            Log.Out($"[{TaskName}] id={theEntity.entityId} ✓ SELECTED: Only horizontal block available at {moveHelper.HitInfo.hit.blockPos}");
        }
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

    private void TryCenterHeightSearchOrFallback(EntityMoveHelper moveHelper, Vector3i horizPos)
    {
        Log.Out($"[{TaskName}] id={theEntity.entityId} Invoking CenterHeightSearch (only horizontal present and foot-level=true)");
        Vector3i bestPos;
        if (TryFindCenterHeightBlockWithinRange(10f, out bestPos))
        {
            moveHelper.HitInfo.hit.blockPos = bestPos;
            moveHelper.HitInfo.hit.pos = new Vector3(bestPos.x + 0.5f, bestPos.y + 0.5f, bestPos.z + 0.5f);
            moveHelper.HitInfo.bHitValid = true;
            Log.Out($"[{TaskName}] id={theEntity.entityId} ✓ SELECTED: Nearest center-height block within 10m at {bestPos}");
        }
        else
        {
            Vector3i abovePos = new Vector3i(horizPos.x, horizPos.y + 1, horizPos.z);
            BlockValue aboveBlock = theEntity.world.GetBlock(abovePos);
            if (!aboveBlock.isair && aboveBlock.Block.MaxDamage > 0 && !aboveBlock.Block.IsTerrainDecoration && aboveBlock.Block.IsMovementBlocked(theEntity.world, abovePos, aboveBlock, BlockFace.None))
            {
                moveHelper.HitInfo.hit.blockPos = abovePos;
                moveHelper.HitInfo.hit.pos = new Vector3(abovePos.x + 0.5f, abovePos.y + 0.5f, abovePos.z + 0.5f);
                moveHelper.HitInfo.bHitValid = true;
                Log.Out($"[{TaskName}] id={theEntity.entityId} ✓ SELECTED: Adjusted to above block (center/head height) at {abovePos}");
            }
            else
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} Using horizontal block; no valid nearby center-height block and above block not usable");
            }
        }
    }

    /// <summary>
    /// Calculate 3D distance from entity position to block position
    /// </summary>
    private float GetBlockDistance(Vector3 entityPos, Vector3i blockPos)
    {
        Vector3 blockWorldPos = new Vector3(
            blockPos.x + 0.5f,
            blockPos.y + 0.5f,
            blockPos.z + 0.5f
        );

        float distance = (blockWorldPos - entityPos).magnitude;
        Log.Out($"[{TaskName}] id={theEntity.entityId} GetBlockDistance - From {entityPos} to {blockPos} = {distance:F2}");
        return distance;
    }

 

    private void EstablishBlockTargetFromHitInfo()
    {
        Log.Out($"[{TaskName}] id={theEntity.entityId} EstablishBlockTargetFromHitInfo called");
        var hitPos = theEntity.moveHelper.HitInfo.hit.blockPos;
        var bv = theEntity.world.GetBlock(hitPos);
        if (!bv.isair && bv.Block.MaxDamage > 0)
        {
            SetBlockTarget(hitPos);
            Log.Out($"[{TaskName}] id={theEntity.entityId} Block target established at {hitPos}");
        }
    }

    private void LogStartBreaking()
    {
        BlockValue blockValue = theEntity.world.GetBlock(currentBlockTarget);
        Block block = blockValue.Block;

        Log.Out($"[{TaskName}] id={theEntity.entityId} === START BREAKING ===");
        Log.Out($"[{TaskName}] id={theEntity.entityId} Target block: {block.GetBlockName()} at {currentBlockTarget}");
        Log.Out($"[{TaskName}] id={theEntity.entityId} Block health: {blockValue.Block.MaxDamage - blockValue.damage}/{blockValue.Block.MaxDamage}");

        if (block.HasTag(BlockTags.Door) || block.HasTag(BlockTags.ClosetDoor))
        {
            theEntity.IsBreakingDoors = true;
            Log.Out($"[{TaskName}] id={theEntity.entityId} Block is a DOOR - IsBreakingDoors set to true");
        }
    }



    private bool IsOnGroundOrElevator()
    {
        bool onGroundOrElevator = theEntity.onGround || theEntity.IsInElevator();
        if (!onGroundOrElevator)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} Continue=FALSE - Not on ground or in elevator");
        }
        return onGroundOrElevator;
    }

    private bool IsBlockStillPresentAndLog()
    {
        var bv = theEntity.world.GetBlock(currentBlockTarget);
        bool stillThere = !bv.isair;
        Log.Out($"[{TaskName}] id={theEntity.entityId} Continue={(stillThere ? "TRUE" : "FALSE")} - Block target {(stillThere ? "present" : "destroyed")} at {currentBlockTarget}");
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
            if (UnityEngine.Time.frameCount % 30 == 0)
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} MovingToBlock - dist={distToBlock:F2} approach=({blockApproachPos.x:F1},{blockApproachPos.y:F1},{blockApproachPos.z:F1}) range={attackRange:F2}");
            }
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
            if (attackDelay % 20 == 0) // Log every second
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} Update - Attack delay: {attackDelay} ticks ({attackDelay / 20f:F1}s)");
            }
        }
    }

    public override void Reset()
    {
        Log.Out($"[{TaskName}] id={theEntity.entityId} === RESET - Stopping block breaking ===");
        theEntity.IsBreakingBlocks = false;
        theEntity.IsBreakingDoors = false;

        // Clear current block target and restore original target state
        hasBlockTarget = false;
        savedAttackTarget = null;
    }

   
    private bool EarlyOutIfTargetDestroyed()
    {
        if (!hasBlockTarget)
            return false;

        var bv = theEntity.world.GetBlock(currentBlockTarget);
        if (bv.isair || bv.damage >= bv.Block.MaxDamage)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} AttackBlock - Target destroyed at {currentBlockTarget}, finishing");
            hasBlockTarget = false;
            return true;
        }
        return false;
    }

    private bool TryGetItemActionAttackData(out ItemActionAttackData itemActionAttackData)
    {
        itemActionAttackData = null;
        theEntity.SetLookPosition(Vector3.zero);
        var data = theEntity.inventory.holdingItemData.actionData[0] as ItemActionAttackData;
        if (data == null)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} AttackBlock FAILED - No ItemActionAttackData");
            return false;
        }
        itemActionAttackData = data;
        return true;
    }

    private void ApplyDamageBoostFromAllies()
    {
        damageBoostPercent = 0f;
        if (theEntity is EntityZombie)
        {
            Bounds bb = new Bounds(theEntity.position, new Vector3(1.7f, 1.5f, 1.7f));
            theEntity.world.GetEntitiesInBounds(typeof(EntityZombie), bb, allies);
            int allyCount = 0;
            for (int num = allies.Count - 1; num >= 0; num--)
            {
                if ((EntityZombie)allies[num] != theEntity)
                {
                    damageBoostPercent += 0.2f;
                    allyCount++;
                }
            }

            if (allyCount > 0)
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} Found {allyCount} nearby allies - Damage boost: +{damageBoostPercent * 100:F0}%");
            }

            allies.Clear();
        }
    }

    private bool PerformAttack(ItemActionAttackData itemActionAttackData)
    {
        if (!theEntity.Attack(_isReleased: false))
            return false;

        theEntity.IsBreakingBlocks = true;
        float num2 = 0.25f + base.RandomFloat * 0.8f;
        if (theEntity.moveHelper.IsUnreachableAbove)
        {
            num2 *= 0.5f;
            Log.Out($"[{TaskName}] id={theEntity.entityId} Target unreachable above - Attack speed halved");
        }

        attackDelay = (int)((num2 + 0.75f) * 20f);
        itemActionAttackData.hitDelegate = GetHitInfo;

        Vector3i targetBlock = hasBlockTarget ? currentBlockTarget : theEntity.moveHelper.HitInfo.hit.blockPos;
        Log.Out($"[{TaskName}] id={theEntity.entityId} ⚔ ATTACKING block at {targetBlock} - Next attack in {attackDelay / 20f:F1}s");

        theEntity.Attack(_isReleased: true);
        return true;
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
            Log.Out($"[{TaskName}] id={theEntity.entityId} GetHitInfo - tag was null/empty; set to 'B_Mesh' to avoid NRE in ItemAction.GetDismemberChance");
        }

        Log.Out($"[{TaskName}] id={theEntity.entityId} GetHitInfo - DamageScale: {damageScale:F2} (Base: {moveHelper.DamageScale:F2} + Boost: {damageBoostPercent:F2})");
        Log.Out($"[{TaskName}] id={theEntity.entityId} GetHitInfo - Target: {moveHelper.HitInfo.hit.blockPos}, Tag:'{moveHelper.HitInfo.tag}'");

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

        Log.Out($"[{TaskName}] id={theEntity.entityId} SetBlockTarget - {blockPos}, approach=({blockApproachPos.x:F1},{blockApproachPos.y:F1},{blockApproachPos.z:F1}), Tag:'{theEntity.moveHelper.HitInfo.tag}'");
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

    // Scans within a horizontal radius for the nearest breakable, movement-blocking block whose
    // vertical center is near the entity's center height.
    private bool TryFindCenterHeightBlockWithinRange(float maxHorizontalMeters, out Vector3i bestPos)
    {
        const float centerYTol = 0.45f; // how close the block center Y must be to entity center Y
        bestPos = default(Vector3i);

        Vector3 entityPos = theEntity.position;
        Vector3i entityBlock = World.worldToBlockPos(entityPos);
        World world = theEntity.world;

        float bestDistSq = float.MaxValue;
        int r = Mathf.FloorToInt(Mathf.Max(1f, maxHorizontalMeters));
        float maxHorzDistSq = maxHorizontalMeters * maxHorizontalMeters;

        Log.Out($"[{TaskName}] id={theEntity.entityId} CenterHeightSearch START - pos={entityPos} block={entityBlock} r={r} tolY={centerYTol:F2} maxHorz={maxHorizontalMeters:F1}m");

        // Iterate candidates in a square and select by true Euclidean distance (XZ), capped at radius
        for (int dz = -r; dz <= r; dz++)
        {
            for (int dx = -r; dx <= r; dx++)
            {
                // Skip far outside circle to reduce checks
                float x = entityBlock.x + dx + 0.5f;
                float z = entityBlock.z + dz + 0.5f;
                float dxw = x - entityPos.x;
                float dzw = z - entityPos.z;
                float horzDistSq = dxw * dxw + dzw * dzw;
                if (horzDistSq > maxHorzDistSq)
                    continue;

                // Only consider blocks whose center Y is near the entity center Y
                int by = Mathf.FloorToInt(entityPos.y); // center around entity Y
                for (int oy = -1; oy <= 1; oy++)
                {
                    int y = by + oy;
                    float blockCenterY = y + 0.5f;
                    if (Mathf.Abs(blockCenterY - entityPos.y) > centerYTol)
                        continue;

                    Vector3i pos = new Vector3i(entityBlock.x + dx, y, entityBlock.z + dz);

                    BlockValue bv = world.GetBlock(pos);
                    if (bv.isair)
                    {
                        Log.Out($"[{TaskName}] id={theEntity.entityId} CenterHeightSearch skip AIR at {pos}");
                        continue;
                    }

                    Block block = bv.Block;
                    if (block == null)
                    {
                        Log.Out($"[{TaskName}] id={theEntity.entityId} CenterHeightSearch skip NULL-BLOCK at {pos}");
                        continue;
                    }

                    bool blocksMove = block.IsMovementBlocked(world, pos, bv, BlockFace.None);
                    bool breakable = block.MaxDamage > 0 && !block.IsTerrainDecoration;

                    if (!blocksMove)
                    {
                        Log.Out($"[{TaskName}] id={theEntity.entityId} CenterHeightSearch skip NON-BLOCKING {block.GetBlockName()} at {pos}");
                        continue;
                    }
                    if (!breakable)
                    {
                        Log.Out($"[{TaskName}] id={theEntity.entityId} CenterHeightSearch skip UNBREAKABLE/DECO {block.GetBlockName()} at {pos}");
                        continue;
                    }

                    Log.Out($"[{TaskName}] id={theEntity.entityId} CenterHeightSearch candidate {block.GetBlockName()} at {pos} distSq={horzDistSq:F2}");

                    // Favor the true nearest
                    if (horzDistSq < bestDistSq)
                    {
                        bestDistSq = horzDistSq;
                        bestPos = pos;
                        Log.Out($"[{TaskName}] id={theEntity.entityId} CenterHeightSearch NEW-BEST {block.GetBlockName()} at {bestPos} dist={Mathf.Sqrt(bestDistSq):F2}m");
                    }
                }
            }
        }

        bool found = bestDistSq != float.MaxValue;
        if (found)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} CenterHeightSearch DONE - Best={bestPos} dist={Mathf.Sqrt(bestDistSq):F2}m");
        }
        else
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} CenterHeightSearch DONE - No suitable block found");
        }
        return found;
    }

    // ===== New helpers: Arc voxel raycast on horizontal plane using Voxel.GetNextBlockHit =====
    private bool TryVoxelArcForBlock(Vector3 start, Vector3 fwd3, float maxDist, float halfAngleDeg, out Vector3i bestPos, out float bestDistSq)
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
            BlockValue bv = theEntity.world.GetBlock(pos);
            Log.Out(TaskName + $" id={theEntity.entityId} VoxelRay angle={angleDeg:F1}° hit block at {pos} - {bv.Block.GetBlockName()}");
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