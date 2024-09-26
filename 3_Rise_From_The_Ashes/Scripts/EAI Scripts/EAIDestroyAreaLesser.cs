using GamePath;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using WorldGenerationEngineFinal;
using static XUiC_DropDown;



public class EAIDestroyAreaLesser : EAIBase
{
    private struct DestroyData
    {
        public int offsetX;

        public int offsetZ;

        public int stepX;

        public int stepZ;

        public DestroyData(int _offsetX, int _offsetZ, int _stepX, int _stepZ)
        {
            offsetX = _offsetX;
            offsetZ = _offsetZ;
            stepX = _stepX;
            stepZ = _stepZ;
        }
    }

    private static DestroyData[] DestroyDataArray = new DestroyData[7]
   {
        new DestroyData(-1, 1, 1, 0),
        new DestroyData(1, 1, 0, -1),
        new DestroyData(1, -1, -1, 0),
        new DestroyData(-1, -1, 0, 1),
        new DestroyData(-1, 1, 1, 0),
        new DestroyData(1, 1, 0, -1),
        new DestroyData(1, -1, -1, 0)
   };

    private static int[] blockOpenOffsets = new int[8] { -1, 0, 1, 0, 0, 1, 0, -1 };

    private GameRandom random;

    private const float cDoneXZDistSq = 0.0004f;

    private const float cCheckBlockedDist = 0.35f;

    private const float cCheckBlockedRadius = 0.125f;

    private const float cCheckSidestepDist = 0.35f;

    private const float cCheckSidestepRadius = 0.1f;

    private const float cTempMoveDist = 0.4f;

    private const float cYawNextDist = 1.5f;

    private const float cMoveDirectDist = 0.65f;

    private const float cMoveSlowDist = 0.6f;

    private const float cDigXZDistSq = 0.0100000007f;

    private const float cDigDiagonalXZDistSq = 2.25f;

    private const float cDigAngleCos = 0.86f;

    private const float cJumpUpXZDistSq = 0.0400000028f;

    private const float cLadderXZDistSq = 0.108900011f;

    private const int cDestroyRadius = 11;

    private const float cUnreachJumpMin = 1.2f;

    private const int cCollisionMask = 1082195968;

    private enum eState
    {
        FindPath,
        HasPath,
        EndPath,
        Attack
    }

    private const float cDumbDistance = 9f;

    private Vector3 seekPos;

    private Vector3i seekBlockPos;

    private bool isLookFar;

    private bool isAtPathEnd;

    private float delayTime;

    private int attackTimeout;

    private eState state;

    private WorldRayHitInfo hitInfo = new WorldRayHitInfo();

    public override void Init(EntityAlive _theEntity)
    {
        base.Init(_theEntity);
        MutexBits = 3;
        executeDelay = 1f + base.RandomFloat * 0.9f;
    }

    public override bool CanExecute()
    {
        EntityMoveHelper moveHelper = theEntity.moveHelper;
        if (!moveHelper.CanBreakBlocks)
        {
            return false;
        }

        EntityAlive attackTarget = theEntity.GetAttackTarget();
        if (!attackTarget)
        {
            return false;
        }

        if (theEntity.bodyDamage.CurrentStun != 0)
        {
            return false;
        }

        bool flag = isLookFar;
        if (moveHelper.IsDestroyAreaTryUnreachable)
        {
            moveHelper.IsDestroyAreaTryUnreachable = false;
            float num = moveHelper.UnreachablePercent;
            if (num > 0f)
            {
                if (base.RandomFloat < num)
                {
                    flag = true;
                    num = 0f;
                }

                moveHelper.UnreachablePercent = num * 0.5f;
            }
        }

        if (manager.pathCostScale < 0.65f)
        {
            float num2 = (1f - manager.pathCostScale * 1.53846157f) * 0.6f;
            if (base.RandomFloat < num2)
            {
                PathEntity path = theEntity.navigator.getPath();
                if (path != null && path.NodeCountRemaining() > 18 && (attackTarget.position - theEntity.position).sqrMagnitude <= 81f)
                {
                    flag = true;
                }
            }
        }

        if (!flag && !moveHelper.IsUnreachableAbove)
        {
            return false;
        }

        Vector3 destroyPos = theEntity.position;
        Vector3 vector = (moveHelper.IsUnreachableSide ? moveHelper.UnreachablePos : attackTarget.position);
        Vector3 vector2 = destroyPos - vector;
        float sqrMagnitude = vector2.sqrMagnitude;
        if (sqrMagnitude > 25f)
        {
            destroyPos = vector + vector2 * (5f / Mathf.Sqrt(sqrMagnitude));
        }

        destroyPos.x += -3f + base.RandomFloat * 6f;
        destroyPos.z += -3f + base.RandomFloat * 6f;
        if (!moveHelper.FindDestroyPos(ref destroyPos, isLookFar))
        {
            return false;
        }

        seekPos = destroyPos;
        seekBlockPos = World.worldToBlockPos(destroyPos);
        isLookFar = false;
        state = eState.FindPath;
        theEntity.navigator.clearPath();
        theEntity.FindPath(destroyPos, theEntity.GetMoveSpeedAggro(), canBreak: true, this);
        moveHelper.IsDestroyArea = true;
        return true;
    }

    public override void Start()
    {
        isAtPathEnd = false;
        delayTime = 3f;
        attackTimeout = 0;
    }

    public void Stop()
    {
        delayTime = 0f;
    }

    public override bool Continue()
    {
        if (theEntity.bodyDamage.CurrentStun != 0)
        {
            return false;
        }

        if (delayTime <= 0f)
        {
            return false;
        }

        EntityMoveHelper moveHelper = theEntity.moveHelper;
        if (state == eState.FindPath && theEntity.navigator.HasPath())
        {
            moveHelper.CalcIfUnreachablePos();
            if (moveHelper.IsUnreachableAbove || moveHelper.IsUnreachableSide)
            {
                isLookFar = true;
                return false;
            }

            moveHelper.IsUnreachableAbove = true;
            state = eState.HasPath;
            delayTime = 15f;
            theEntity.navigator.ShortenEnd(0.2f);
        }

        if (state == eState.HasPath)
        {
            PathEntity path = theEntity.navigator.getPath();
            if (path != null && path.NodeCountRemaining() <= 1)
            {
                state = eState.EndPath;
                delayTime = 5f + base.RandomFloat * 5f;
                isAtPathEnd = true;
            }
        }

        if (state == eState.EndPath && !moveHelper.IsBlocked)
        {
            if (!Voxel.BlockHit(hitInfo, seekBlockPos))
            {
                return false;
            }

            state = eState.Attack;
            theEntity.SeekYawToPos(seekPos, 10f);
        }

        if (!isAtPathEnd && theEntity.navigator.noPathAndNotPlanningOne())
        {
            return false;
        }

        return true;
    }

    public override void Update()
    {
        delayTime -= 0.05f;
        if (state != eState.Attack || --attackTimeout > 0)
        {
            return;
        }

        ItemActionAttackData itemActionAttackData = theEntity.inventory.holdingItemData.actionData[0] as ItemActionAttackData;
        if (itemActionAttackData != null)
        {
            theEntity.SetLookPosition(Vector3.zero);
            if (theEntity.Attack(_bAttackReleased: false))
            {
                attackTimeout = theEntity.GetAttackTimeoutTicks();
                itemActionAttackData.hitDelegate = GetHitInfo;
                theEntity.Attack(_bAttackReleased: true);
                state = eState.EndPath;
            }
        }
    }

    private WorldRayHitInfo GetHitInfo(out float damageScale)
    {
        damageScale = 1f;
        return hitInfo;
    }

    public override void Reset()
    {
        EntityMoveHelper moveHelper = theEntity.moveHelper;
        moveHelper.Stop();
        moveHelper.IsUnreachableAbove = false;
        moveHelper.IsDestroyArea = false;
    }

    public override string ToString()
    {
        return string.Format("{0}, {1}, delayTime {2}", base.ToString(), state.ToStringCached(), delayTime.ToCultureInvariantString("0.00"));
    }

    [Conditional("DEBUG_AIDESTROY")]
    private void LogDestroy(string _format = "", params object[] _args)
    {
        _format = $"{GameManager.frameCount} EAIDestroyArea {theEntity.EntityName} {theEntity.entityId}, {_format}";
        Log.Warning(_format, _args);
    }

    public bool FindDestroyPos(ref Vector3 destroyPos, bool isLookFar)
    {
        Log.Out("Destroy Area Lesser - FindDestroyPos");
        int num = int.MaxValue;
        Vector3i vector3i = Vector3i.zero;
        ChunkCluster chunkCache = theEntity.world.ChunkCache;
        Vector3i vector3i2 = World.worldToBlockPos(destroyPos);
        int i = 1;
        int num2 = 1;
        if (isLookFar)
        {
            i = random.RandomRange(5, 11);
            num2 = -1;
            vector3i2.y -= 2;
        }

        bool flag = false;
        int num3 = random.RandomRange(0, 4);
        Vector3i vector3i3 = default(Vector3i);
        for (; i >= 1 && i <= 11; i += num2)
        {
            int num4 = i * 2;
            for (int j = -2; j <= 2; j++)
            {
                vector3i3.y = vector3i2.y + j;
                for (int k = 0; k < 4; k++)
                {
                    DestroyData destroyData = DestroyDataArray[k + num3];
                    int num5 = destroyData.offsetX * i;
                    int num6 = destroyData.offsetZ * i;
                    vector3i3.x = vector3i2.x + num5;
                    vector3i3.z = vector3i2.z + num6;
                    for (int l = 0; l < num4; l++)
                    {
                        BlockValue block = chunkCache.GetBlock(vector3i3);
                        if (!block.isair)
                        {
                            Block block2 = block.Block;
                            if (block2.IsMovementBlocked(theEntity.world, vector3i3, block, BlockFace.None) && block2.StabilitySupport)
                            {
                                Vector3i vector3i4 = vector3i3;
                                vector3i4.y++;
                                BlockValue block3 = chunkCache.GetBlock(vector3i4);
                                if (!block3.isair)
                                {
                                    Block block4 = block3.Block;
                                    if (block4.IsMovementBlocked(theEntity.world, vector3i4, block3, BlockFace.None) && block4.StabilitySupport)
                                    {
                                        bool flag2 = false;
                                        int num7 = block2.MaxDamagePlusDowngrades - block.damage;
                                        if (block2.shape.IsTerrain())
                                        {
                                            num7 *= 50;
                                        }
                                        else
                                        {
                                            flag2 = true;
                                        }

                                        int num8 = block4.MaxDamagePlusDowngrades - block3.damage;
                                        if (block4.shape.IsTerrain())
                                        {
                                            num8 *= 50;
                                        }
                                        else
                                        {
                                            flag2 = true;
                                        }

                                        num7 += num8;
                                        if (num7 < num && (!flag || flag2) && IsABlockSideOpen(vector3i3))
                                        {
                                            flag = flag2;
                                            num = num7;
                                            vector3i = vector3i3;
                                        }
                                    }
                                }
                            }
                        }

                        vector3i3.x += destroyData.stepX;
                        vector3i3.z += destroyData.stepZ;
                    }
                }
            }

            if (flag)
            {
                break;
            }
        }

        if (num > 999999)
        {
            return false;
        }

        destroyPos = vector3i.ToVector3CenterXZ();
        destroyPos.y += 1f;
        Log.Out("Destroy Area Lesser - FindDestroyPos - Exit");
        return true;
    }

    private bool IsABlockSideOpen(Vector3i checkPos)
    {
        ChunkCluster chunkCache = theEntity.world.ChunkCache;
        for (int i = 0; i < blockOpenOffsets.Length; i += 2)
        {
            Vector3i vector3i = checkPos;
            vector3i.x += blockOpenOffsets[i];
            vector3i.z += blockOpenOffsets[i + 1];
            BlockValue block = chunkCache.GetBlock(vector3i);
            if (!block.Block.IsMovementBlocked(theEntity.world, vector3i, block, BlockFace.None))
            {
                return true;
            }
        }

        return false;
    }
}


