using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class EAIBreakBlocksIconic : EAIBase
{
    public const float cDamageBoostPerAlly = 0.2f;
    public const float cMaxDamageBoostPercent = 0.6f; // Cap total ally damage boost at +60%

    public int attackDelay;
    public float damageBoostPercent;
    public List<Entity> allies = new List<Entity>();

    private const float VerticalDetectionThreshold = 2.5f;
    private const float HorizontalRangeThreshold = 2.5f;

    private string TaskName => nameof(EAIBreakBlocksIconic);

    private bool hasBlockTarget;
    private Vector3i currentBlockTarget;
    private Vector3 blockApproachPos;
    private float approachRecalcTimer;
    private const float ApproachRecalcInterval = 0.5f;

    private EntityAlive savedAttackTarget;

    public override void Init(EntityAlive _theEntity)
    {
        base.Init(_theEntity);
        MutexBits = 8;
        executeDelay = 0.15f;
    }

    public override bool CanExecute()
    {
        if (IsEntityStunned()) return false;
        if (IsJumpingAbort()) return false;

        if (HandleExistingBlockTargetContinuation())
            return true;

        if (!MovementPrereqsSatisfied())
            return false;

        if (EarlyOutIfTargetIsOnSameLevel() && theEntity.moveHelper.BlockedTime < 1.0f)
            return false;

        SelectNearestBlock();
        return hasBlockTarget;
    }

    public override void Start()
    {
        attackDelay = 1;
        EstablishBlockTargetFromHitInfo();
        LogStartBreaking();
    }

    public override void Update()
    {
        if (hasBlockTarget)
        {
            if (HandleApproachOrStopForAttack()) return;
        }

        if (attackDelay > 0) attackDelay--;

        if (attackDelay <= 0)
            AttackBlock();
    }

    public override bool Continue()
    {
        if (EarlyOutIfTargetIsOnSameLevel() && theEntity.moveHelper.BlockedTime < 1.0f)
            return false;

        if (!IsOnGroundOrElevator())
            return false;

        if (hasBlockTarget)
            return IsBlockStillPresentAndLog();

        return CanExecute();
    }

    private bool IsEntityStunned()
    {
        return theEntity.bodyDamage.CurrentStun != 0;
    }

    private bool HandleExistingBlockTargetContinuation()
    {
        if (!hasBlockTarget) return false;

        var bv = theEntity.world.GetBlock(currentBlockTarget);
        if (!bv.isair) return true;

        hasBlockTarget = false;
        return false;
    }

    private bool MovementPrereqsSatisfied()
    {
        var moveHelper = theEntity.moveHelper;

        if (!moveHelper.CanBreakBlocks)
            return false;

        // NEW: if target is unreachable and above, break blocks immediately
        if (moveHelper.IsUnreachableAbove)
            return true;

        if (CheckIfUnderThePlayer())
            return true;

        if (IsStuckMovingForwardWithPlayerAbove())
            return true;

        if (moveHelper.BlockedTime >= 1.0f)
            return true;

        return false;
    }

    private bool IsJumpingAbort()
    {
        return theEntity.Jumping && !theEntity.moveHelper.IsDestroyArea;
    }

    private bool CheckIfUnderThePlayer()
    {
        EntityAlive attackTarget = theEntity.GetAttackTarget();
        if (!(attackTarget is EntityPlayer)) return false;

        Vector3 zp = theEntity.position;
        Vector3 pp = attackTarget.position;

        float dx = Mathf.Abs(zp.x - pp.x);
        float dz = Mathf.Abs(zp.z - pp.z);
        return dx <= HorizontalRangeThreshold && dz <= HorizontalRangeThreshold;
    }

    private bool IsStuckMovingForwardWithPlayerAbove()
    {
        EntityAlive attackTarget = theEntity.GetAttackTarget();
        if (!(attackTarget is EntityPlayer)) return false;

        var move = theEntity.moveHelper;
        if (move.BlockedTime < 0.5f) return false;

        Vector3 zp = theEntity.position;
        Vector3 pp = attackTarget.position;

        if (Mathf.Abs(zp.x - pp.x) > 3f || Mathf.Abs(zp.z - pp.z) > 3f) return false;

        // Use VerticalDetectionThreshold instead of hard-coded 2f
        float verticalDiff = pp.y - zp.y;
        return verticalDiff >= VerticalDetectionThreshold;
    }

    private bool EarlyOutIfTargetIsOnSameLevel()
    {
        EntityAlive attackTarget = theEntity.GetAttackTarget();
        if (attackTarget == null) return false;

        float v = Mathf.Abs(attackTarget.position.y - theEntity.position.y);
        return v < VerticalDetectionThreshold;
    }

    private void SelectNearestBlock()
    {
        Vector3 pos = theEntity.position;
        float feetY = pos.y;
        float headY = pos.y + theEntity.GetEyeHeight();

        if (EarlyOutIfTargetIsOnSameLevel() && theEntity.moveHelper.BlockedTime >= 1.0f)
        {
            feetY = pos.y - 2f;
            headY = pos.y + 3f;
        }

        int by = Mathf.FloorToInt(pos.y);
        Vector3 start = new Vector3(pos.x, by + 0.5f, pos.z);

        Vector3 fwd = theEntity.GetForwardVector();
        fwd.y = 0f;
        if (fwd.sqrMagnitude < 0.01f)
        {
            fwd = theEntity.transform.forward;
            fwd.y = 0f;
        }
        fwd.Normalize();

        Vector3i bestPos;
        float bestDist;
        TryVoxelArcForBlock(start, fwd, 20f, 45f, feetY, headY, out bestPos, out bestDist);
    }

    public void AttackBlock()
    {
        theEntity.SetLookPosition(Vector3.zero);
        if (!(theEntity.inventory.holdingItemData.actionData[0] is ItemActionAttackData data))
            return;

        damageBoostPercent = 0f;

        if (theEntity is EntityZombie)
        {
            Bounds bb = new Bounds(theEntity.position, new Vector3(1.7f, 1.5f, 1.7f));
            theEntity.world.GetEntitiesInBounds(typeof(EntityZombie), bb, allies);

            for (int i = allies.Count - 1; i >= 0; i--)
            {
                if (allies[i] != theEntity)
                    damageBoostPercent += cDamageBoostPerAlly;
            }

            // Clamp total ally boost
            if (damageBoostPercent > cMaxDamageBoostPercent)
                damageBoostPercent = cMaxDamageBoostPercent;

            allies.Clear();
        }

        if (theEntity.Attack(false))
        {
            theEntity.IsBreakingBlocks = true;

            float t = 0.25f + RandomFloat * 0.8f;
            if (theEntity.moveHelper.IsUnreachableAbove) t *= 0.5f;

            attackDelay = (int)((t + 0.75f) * 20f);
            data.hitDelegate = GetHitInfo;
            theEntity.Attack(true);
        }
    }

    private void EstablishBlockTargetFromHitInfo()
    {
        var hp = theEntity.moveHelper.HitInfo.hit.blockPos;
        var bv = theEntity.world.GetBlock(hp);
        if (!bv.isair && bv.Block.MaxDamage > 0)
            SetBlockTarget(hp);
    }

    private void LogStartBreaking()
    {
        BlockValue bv = theEntity.world.GetBlock(currentBlockTarget);
        Block b = bv.Block;

        if (b.HasTag(BlockTags.Door) || b.HasTag(BlockTags.ClosetDoor))
            theEntity.IsBreakingDoors = true;
    }

    private bool IsOnGroundOrElevator()
    {
        return theEntity.onGround || theEntity.IsInElevator();
    }

    private bool IsBlockStillPresentAndLog()
    {
        return !theEntity.world.GetBlock(currentBlockTarget).isair;
    }

    private bool HandleApproachOrStopForAttack()
    {
        approachRecalcTimer -= 0.05f;
        if (approachRecalcTimer <= 0f)
        {
            approachRecalcTimer = ApproachRecalcInterval;
            blockApproachPos = ComputeApproachPosition(currentBlockTarget);
        }

        Vector3 center = GetBlockCenter(currentBlockTarget);
        float dist = Vector3.Distance(theEntity.position, center);
        float attackRange = GetBlockAttackRange();

        theEntity.SetLookPosition(center);

        if (dist > attackRange * 0.95f)
        {
            theEntity.moveHelper.SetMoveTo(blockApproachPos, true);
            return true;
        }

        theEntity.moveHelper.Stop();
        return false;
    }

    public override void Reset()
    {
        theEntity.IsBreakingBlocks = false;
        theEntity.IsBreakingDoors = false;

        hasBlockTarget = false;
        savedAttackTarget = null;
    }

    public WorldRayHitInfo GetHitInfo(out float damageScale)
    {
        var move = theEntity.moveHelper;
        damageScale = move.DamageScale + damageBoostPercent;

        if (hasBlockTarget)
        {
            move.HitInfo.hit.blockPos = currentBlockTarget;
            move.HitInfo.hit.pos = GetBlockCenter(currentBlockTarget);
            move.HitInfo.bHitValid = true;
        }

        if (string.IsNullOrEmpty(move.HitInfo.tag))
            move.HitInfo.tag = "B_Mesh";

        return move.HitInfo;
    }

    private void SetBlockTarget(Vector3i pos)
    {
        currentBlockTarget = pos;
        hasBlockTarget = true;
        blockApproachPos = ComputeApproachPosition(pos);
        approachRecalcTimer = ApproachRecalcInterval;

        savedAttackTarget = theEntity.GetAttackTarget();

        var hit = theEntity.moveHelper.HitInfo;
        hit.hit.blockPos = pos;
        hit.hit.pos = GetBlockCenter(pos);
        hit.bHitValid = true;

        if (string.IsNullOrEmpty(hit.tag))
            hit.tag = "B_Mesh";
    }

    private Vector3 ComputeApproachPosition(Vector3i pos)
    {
        Vector3 center = GetBlockCenter(pos);
        Vector3 dir = center - theEntity.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) dir = theEntity.transform.forward;
        dir.Normalize();

        float radius = theEntity.m_characterController.GetRadius();
        float dist = Mathf.Max(0.9f, radius + 0.6f);

        Vector3 a = center - dir * dist;
        a.y = theEntity.position.y;
        return a;
    }

    private Vector3 GetBlockCenter(Vector3i pos)
    {
        return new Vector3(pos.x + 0.5f, pos.y + 0.5f, pos.z + 0.5f);
    }

    private float GetBlockAttackRange()
    {
        ItemValue iv = theEntity.inventory.holdingItemItemValue;
        int idx = theEntity.inventory.holdingItemIdx;
        ItemAction act = iv.ItemClass.Actions[idx];

        float r = act?.Range ?? EffectManager.GetItemValue(PassiveEffects.MaxRange, iv);
        r = Mathf.Max(0.7f, (r == 0 ? 1.095f : r) - 0.35f);
        return r;
    }

    private bool TryVoxelArcForBlock(
        Vector3 start, Vector3 fwd, float maxDist, float halfAngle,
        float feetY, float headY, out Vector3i bestPos, out float bestDist)
    {
        bestPos = default;
        bestDist = float.MaxValue;
        bool found = false;
        Vector3 bestLook = Vector3.zero;

        const int rays = 13;
        float arc = halfAngle * 2f;
        float step = arc / (rays - 1);
        float begin = -halfAngle;

        for (int i = 0; i < rays; i++)
        {
            float ang = begin + i * step;
            float rad = ang * Mathf.Deg2Rad;

            Vector2 dirXZ = RotateXZ(new Vector2(fwd.x, fwd.z), rad);
            if (dirXZ.sqrMagnitude < 1e-6f) continue;

            Vector3 dir = new Vector3(dirXZ.x, 0, dirXZ.y).normalized;

            Ray r = new Ray(start, dir);
            int hitMask = Voxel.HM_NotMoveable;

            if (!Voxel.GetNextBlockHit(theEntity.world, r, maxDist, hitMask, false))
                continue;
            if (!Voxel.voxelRayHitInfo.bHitValid)
                continue;

            Vector3i pos = Voxel.voxelRayHitInfo.hit.blockPos;
            float by = pos.y + 0.5f;
            if (by < feetY || by > headY) continue;

            BlockValue bv = theEntity.world.GetBlock(pos);
            Block b = bv.Block;
            if (bv.isair || b == null) continue;
            if (b.MaxDamage <= 0 || b.IsTerrainDecoration ||
                !b.IsMovementBlocked(theEntity.world, pos, bv, BlockFace.None))
                continue;

            Vector3 hp = Voxel.voxelRayHitInfo.hit.pos;
            if (hp == Vector3.zero)
                hp = new Vector3(pos.x + 0.5f, start.y, pos.z + 0.5f);

            float d2 = (hp - start).sqrMagnitude;
            if (d2 < bestDist)
            {
                bestDist = d2;
                bestPos = pos;
                bestLook = hp;
                found = true;
            }
        }

        if (found)
        {
            SetBlockTarget(bestPos);
            theEntity.lookAtPosition = bestLook != Vector3.zero ? bestLook : GetBlockCenter(bestPos);
        }

        return found;
    }

    private static Vector2 RotateXZ(Vector2 v, float rad)
    {
        float c = Mathf.Cos(rad);
        float s = Mathf.Sin(rad);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }
}
