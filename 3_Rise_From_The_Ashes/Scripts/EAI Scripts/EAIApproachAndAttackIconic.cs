using System;
using System.Collections.Generic;
using GamePath;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class EAIApproachAndAttackIconic : EAIBase
{
    [PublicizedFrom(EAccessModifier.Private)]
    public struct TargetClass
    {
        public Type type;

        public float chaseTimeMax;
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public const float cSleeperChaseTime = 90f;

    [PublicizedFrom(EAccessModifier.Private)]
    public List<TargetClass> targetClasses;

    [PublicizedFrom(EAccessModifier.Private)]
    public float chaseTimeMax;

    [PublicizedFrom(EAccessModifier.Private)]
    public bool hasHome;

    [PublicizedFrom(EAccessModifier.Private)]
    public bool isGoingHome;

    [PublicizedFrom(EAccessModifier.Private)]
    public float homeTimeout;

    [PublicizedFrom(EAccessModifier.Private)]
    public EntityAlive entityTarget;

    [PublicizedFrom(EAccessModifier.Private)]
    public Vector3 entityTargetPos;

    [PublicizedFrom(EAccessModifier.Private)]
    public Vector3 entityTargetVel;

    [PublicizedFrom(EAccessModifier.Private)]
    public int attackTimeout;

    [PublicizedFrom(EAccessModifier.Private)]
    public int pathCounter;

    [PublicizedFrom(EAccessModifier.Private)]
    public Vector2 seekPosOffset;

    [PublicizedFrom(EAccessModifier.Private)]
    public bool isTargetToEat;

    [PublicizedFrom(EAccessModifier.Private)]
    public bool isEating;

    [PublicizedFrom(EAccessModifier.Private)]
    public int eatCount;

    [PublicizedFrom(EAccessModifier.Private)]
    public EAIBlockingTargetTask blockTargetTask;

    [PublicizedFrom(EAccessModifier.Private)]
    public int relocateTicks;

    private static readonly List<Entity> TmpEntities = new List<Entity>();

    private string TaskName => nameof(EAIApproachAndAttackIconic);

    public EAIApproachAndAttackIconic()
    {
        Log.Out("EAIApproachAndAttackIconic Constructor");
        chaseTimeMax = 30f;
        seekPosOffset = Vector2.zero;
        targetClasses = new List<TargetClass>();
    }

    public override void Init(EntityAlive _theEntity)
    {
        Log.Out("EAIApproachAndAttackIconic Init");
        base.Init(_theEntity);
        MutexBits = 3;
        executeDelay = 0.1f;
        IconicLog.Info(theEntity, TaskName, $"Init: mutex={MutexBits} execDelay={executeDelay}");
    }

    public override void SetData(DictionarySave<string, string> data)
    {
        base.SetData(data);
        targetClasses = new List<TargetClass>();
        if (!data.TryGetValue("class", out var _value))
        {
            IconicLog.Warn(theEntity, TaskName, "SetData: no 'class' value provided");
            return;
        }

        string[] array = _value.Split(',', (char)StringSplitOptions.None);
        for (int i = 0; i < array.Length; i += 2)
        {
            TargetClass item = default(TargetClass);
            item.type = EntityFactory.GetEntityType(array[i]);
            item.chaseTimeMax = 0f;
            if (i + 1 < array.Length)
            {
                item.chaseTimeMax = StringParsers.ParseFloat(array[i + 1]);
            }

            targetClasses.Add(item);
            if (item.type == typeof(EntityEnemyAnimal))
            {
                item.type = typeof(EntityAnimalSnake);
                targetClasses.Add(item);
            }
        }
        IconicLog.Info(theEntity, TaskName, $"SetData: targetClasses={targetClasses.Count}");
    }

    public void SetTargetOnlyPlayers()
    {
        targetClasses.Clear();
        TargetClass item = default(TargetClass);
        item.type = typeof(EntityPlayer);
        targetClasses.Add(item);
        IconicLog.Info(theEntity, TaskName, "SetTargetOnlyPlayers");
    }

    public override bool CanExecute()
    {
        if (theEntity.sleepingOrWakingUp || theEntity.bodyDamage.CurrentStun != 0 || (theEntity.Jumping && !theEntity.isSwimming))
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: sleeping/stunned/jumping");
            return false;
        }

        entityTarget = theEntity.GetAttackTarget();
        if (entityTarget == null)
        {
            // Fallback: if no EAITarget task has set an attack target, try to auto-acquire a valid player target
            if (!TryAutoAcquireTarget())
            {
                IconicLog.Trace(theEntity, TaskName, "CanExecute=false: no attack target");
                return false;
            }
            entityTarget = theEntity.GetAttackTarget();
        }

        Type type = entityTarget.GetType();
        if (targetClasses != null && targetClasses.Count > 0)
        {
            for (int i = 0; i < targetClasses.Count; i++)
            {
                TargetClass targetClass = targetClasses[i];
                if (targetClass.type != null && targetClass.type.IsAssignableFrom(type))
                {
                    chaseTimeMax = targetClass.chaseTimeMax;
                    IconicLog.Debug(theEntity, TaskName, $"CanExecute=true: target={entityTarget.EntityName} chaseTimeMax={chaseTimeMax}");
                    return true;
                }
            }

            IconicLog.Trace(theEntity, TaskName, $"CanExecute=false: target type {type.Name} not in configured classes ({targetClasses.Count})");
            return false;
        }

        // No target classes configured -> accept any target we acquired (default behavior)
        IconicLog.Debug(theEntity, TaskName, $"CanExecute=true: target={entityTarget.EntityName} (no class filter)");
        return true;
    }

    private bool TryAutoAcquireTarget()
    {
        // Use see distance and sense scale similarly to EAITarget logic
        float seeDist = theEntity.GetSeeDistance();
        TmpEntities.Clear();
        theEntity.world.GetEntitiesInBounds(typeof(EntityPlayer), BoundsUtils.ExpandBounds(theEntity.boundingBox, seeDist, seeDist, seeDist), TmpEntities);

        EntityPlayer best = null;
        float bestDist = float.MaxValue;
        for (int i = 0; i < TmpEntities.Count; i++)
        {
            var p = TmpEntities[i] as EntityPlayer;
            if (p == null || !p.IsAlive() || p.IsIgnoredByAI())
                continue;

            float dist = theEntity.GetDistance(p);
            // Must see and pass stealth
            if (theEntity.CanSee(p) && theEntity.CanSeeStealth(dist, p.Stealth.lightLevel))
            {
                if (dist < bestDist)
                {
                    best = p;
                    bestDist = dist;
                }
            }
        }

        TmpEntities.Clear();
        if (best != null)
        {
            theEntity.SetAttackTarget(best, 200);
            IconicLog.Info(theEntity, TaskName, $"Auto-acquired target: {best.EntityName} dist={bestDist:0.00}");
            return true;
        }

        return false;
    }

    public override void Start()
    {
        entityTargetPos = entityTarget.position;
        entityTargetVel = Vector3.zero;
        isTargetToEat = entityTarget.IsDead();
        isEating = false;
        theEntity.IsEating = false;
        homeTimeout = (theEntity.IsSleeper ? 90f : chaseTimeMax);
        hasHome = homeTimeout > 0f;
        isGoingHome = false;
        if (theEntity.ChaseReturnLocation == Vector3.zero)
        {
            theEntity.ChaseReturnLocation = (theEntity.IsSleeper ? theEntity.SleeperSpawnPosition : theEntity.position);
        }

        pathCounter = 0;
        relocateTicks = 0;
        attackTimeout = 5;
        IconicLog.Info(theEntity, TaskName, $"Start: target={entityTarget?.EntityName} dead={isTargetToEat} homeTimeout={homeTimeout}");
    }

    public override bool Continue()
    {
        if (theEntity.sleepingOrWakingUp || theEntity.bodyDamage.CurrentStun != 0)
        {
            IconicLog.Trace(theEntity, TaskName, "Continue=false: sleeping/stunned");
            return false;
        }

        EntityAlive attackTarget = theEntity.GetAttackTarget();
        if (isGoingHome)
        {
            if (!attackTarget)
            {
                bool cont = theEntity.ChaseReturnLocation != Vector3.zero;
                if (!cont) IconicLog.Trace(theEntity, TaskName, "Continue=false: goingHome done");
                return cont;
            }

            IconicLog.Trace(theEntity, TaskName, "Continue=false: goingHome but attack target present");
            return false;
        }

        if (!attackTarget)
        {
            IconicLog.Trace(theEntity, TaskName, "Continue=false: lost attack target");
            return false;
        }

        if (attackTarget != entityTarget)
        {
            IconicLog.Trace(theEntity, TaskName, "Continue=false: attack target changed");
            return false;
        }

        if (attackTarget.IsDead() != isTargetToEat)
        {
            IconicLog.Trace(theEntity, TaskName, "Continue=false: dead/eat state changed");
            return false;
        }

        return true;
    }

    public override void Reset()
    {
        theEntity.IsEating = false;
        theEntity.moveHelper.Stop();
        if (blockTargetTask != null)
        {
            blockTargetTask.canExecute = false;
        }
        IconicLog.Info(theEntity, TaskName, "Reset: stop movement and clear blockTargetTask");
    }

    public override void Update()
    {
        if (hasHome && !isTargetToEat)
        {
            if (isGoingHome)
            {
                Vector3 vector = theEntity.ChaseReturnLocation - theEntity.position;
                float y = vector.y;
                vector.y = 0f;
                if (vector.sqrMagnitude <= 0.160000011f && Utils.FastAbs(y) < 2f)
                {
                    Vector3 chaseReturnLocation = theEntity.ChaseReturnLocation;
                    chaseReturnLocation.y = theEntity.position.y;
                    theEntity.SetPosition(chaseReturnLocation);
                    theEntity.ChaseReturnLocation = Vector3.zero;
                    if (theEntity.IsSleeper)
                    {
                        theEntity.ResumeSleeperPose();
                    }
                }
                else if (--pathCounter <= 0 && !PathFinderThread.Instance.IsCalculatingPath(theEntity.entityId))
                {
                    pathCounter = 60;
                    float moveSpeed = theEntity.GetMoveSpeedAggro() * 0.8f;
                    theEntity.FindPath(theEntity.ChaseReturnLocation, moveSpeed, canBreak: false, this);
                }

                IconicLog.Trace(theEntity, TaskName, $"Update: goingHome pathCounter={pathCounter}");
                return;
            }

            homeTimeout -= 0.05f;
            if (homeTimeout <= 0f)
            {
                if (blockTargetTask == null)
                {
                    List<EAIBlockingTargetTask> targetTasks = manager.GetTargetTasks<EAIBlockingTargetTask>();
                    if (targetTasks != null)
                    {
                        blockTargetTask = targetTasks[0];
                    }
                }

                if (blockTargetTask != null)
                {
                    blockTargetTask.canExecute = true;
                }

                theEntity.SetAttackTarget(null, 0);
                theEntity.SetLookPosition(Vector3.zero);
                theEntity.PlayGiveUpSound();
                pathCounter = 0;
                isGoingHome = true;
                IconicLog.Info(theEntity, TaskName, "Update: timeout reached, goingHome");
                return;
            }
        }

        if (entityTarget == null)
        {
            IconicLog.Trace(theEntity, TaskName, "Update: no entityTarget");
            return;
        }

        if (relocateTicks > 0)
        {
            if (!theEntity.navigator.noPathAndNotPlanningOne())
            {
                relocateTicks--;
                theEntity.moveHelper.SetFocusPos(entityTarget.position);
                IconicLog.Trace(theEntity, TaskName, $"Update: relocating ticksLeft={relocateTicks}");
                return;
            }

            relocateTicks = 0;
        }

        Vector3 vector2 = entityTarget.position;
        if (isTargetToEat)
        {
            vector2 = entityTarget.getBellyPosition();
        }

        Vector3 vector3 = vector2 - entityTargetPos;
        if (vector3.sqrMagnitude < 1f)
        {
            entityTargetVel = entityTargetVel * 0.7f + vector3 * 0.3f;
        }

        entityTargetPos = vector2;
        attackTimeout--;
        if (isEating)
        {
            if (theEntity.bodyDamage.HasLimbs)
            {
                theEntity.RotateTo(vector2.x, vector2.y, vector2.z, 8f, 5f);
            }

            if (attackTimeout <= 0)
            {
                attackTimeout = 25 + GetRandom(10);
                if ((eatCount & 1) == 0)
                {
                    theEntity.PlayOneShot("eat_player");
                    entityTarget.DamageEntity(DamageSource.eat, 35, _criticalHit: false);
                }

                Vector3 pos = new Vector3(0f, 0.04f, 0.08f);
                ParticleEffect pe = new ParticleEffect("blood_eat", pos, 1f, Color.white, null, theEntity.entityId, ParticleEffect.Attachment.Head);
                GameManager.Instance.SpawnParticleEffectServer(pe, theEntity.entityId);
                eatCount++;
            }

            IconicLog.Trace(theEntity, TaskName, $"Update: eating timeout={attackTimeout} eatCount={eatCount}");
            return;
        }

        theEntity.moveHelper.CalcIfUnreachablePos();
        float num;
        float num2;
        if (!isTargetToEat)
        {
            ItemValue holdingItemItemValue = theEntity.inventory.holdingItemItemValue;
            int holdingItemIdx = theEntity.inventory.holdingItemIdx;
            ItemAction itemAction = holdingItemItemValue.ItemClass.Actions[holdingItemIdx];
            num = 1.095f;
            if (itemAction != null)
            {
                num = itemAction.Range;
                if (num == 0f)
                {
                    num = EffectManager.GetItemValue(PassiveEffects.MaxRange, holdingItemItemValue);
                }

                num2 = num;
            }

            num2 = Utils.FastMax(0.7f, num - 0.35f);
        }
        else
        {
            num = theEntity.GetHeight() * 0.9f;
            num2 = num - 0.05f;
        }

        float num3 = num2 * num2;
        float num4 = 4f;
        if (theEntity.IsFeral)
        {
            num4 = 8f;
        }

        num4 = base.RandomFloat * num4;
        float targetXZDistanceSq = GetTargetXZDistanceSq(num4);
        float num5 = vector2.y - theEntity.position.y;
        float num6 = Utils.FastAbs(num5);
        bool flag = targetXZDistanceSq <= num3 && num6 < 1f;
        if (!flag)
        {
            if (num6 < 3f && !PathFinderThread.Instance.IsCalculatingPath(theEntity.entityId))
            {
                PathEntity path = theEntity.navigator.getPath();
                if (path != null && path.NodeCountRemaining() <= 2)
                {
                    pathCounter = 0;
                }
            }

            if (--pathCounter <= 0 && theEntity.CanNavigatePath() && !PathFinderThread.Instance.IsCalculatingPath(theEntity.entityId))
            {
                pathCounter = 6 + GetRandom(10);
                Vector3 moveToLocation = GetMoveToLocation(num2);
                if (moveToLocation.y - theEntity.position.y < -8f)
                {
                    pathCounter += 40;
                    if (base.RandomFloat < 0.2f)
                    {
                        seekPosOffset.x += base.RandomFloat * 0.6f - 0.3f;
                        seekPosOffset.y += base.RandomFloat * 0.6f - 0.3f;
                    }

                    moveToLocation.x += seekPosOffset.x;
                    moveToLocation.z += seekPosOffset.y;
                }
                else
                {
                    float num7 = (moveToLocation - theEntity.position).magnitude - 5f;
                    if (num7 > 0f)
                    {
                        if (num7 > 60f)
                        {
                            num7 = 60f;
                        }

                        pathCounter += (int)(num7 / 20f * 20f);
                    }
                }

                theEntity.FindPath(moveToLocation, theEntity.GetMoveSpeedAggro(), canBreak: true, this);
                IconicLog.Trace(theEntity, TaskName, $"Update: pathing moveTo={moveToLocation} pathCounter={pathCounter}");
            }
        }

        if (theEntity.Climbing)
        {
            IconicLog.Trace(theEntity, TaskName, "Update: climbing");
            return;
        }

        bool flag2 = theEntity.CanSee(entityTarget);
        theEntity.SetLookPosition((flag2 && !theEntity.IsBreakingBlocks) ? entityTarget.getHeadPosition() : Vector3.zero);
        if (!flag)
        {
            if (theEntity.navigator.noPathAndNotPlanningOne() && pathCounter > 0 && num5 < 2.1f)
            {
                Vector3 moveToLocation2 = GetMoveToLocation(num2);
                theEntity.moveHelper.SetMoveTo(moveToLocation2, _canBreakBlocks: true);
                IconicLog.Trace(theEntity, TaskName, $"Update: nopath moveTo={moveToLocation2}");
            }
        }
        else
        {
            theEntity.moveHelper.Stop();
            pathCounter = 0;
        }

        float num8 = (isTargetToEat ? num : (num - 0.1f));
        float num9 = num8 * num8;
        if (targetXZDistanceSq > num9 || num5 < -1.25f || num5 - theEntity.GetHeight() > 0.65f)
        {
            IconicLog.Trace(theEntity, TaskName, $"Update: not in attack window distSq={targetXZDistanceSq:0.000} yh={num5:0.00}");
            return;
        }

        theEntity.IsBreakingBlocks = false;
        theEntity.IsBreakingDoors = false;
        if (theEntity.bodyDamage.HasLimbs && !theEntity.Electrocuted)
        {
            theEntity.RotateTo(vector2.x, vector2.y, vector2.z, 30f, 30f);
        }

        if (isTargetToEat)
        {
            isEating = true;
            theEntity.IsEating = true;
            attackTimeout = 20;
            eatCount = 0;
            IconicLog.Info(theEntity, TaskName, "Update: begin eating");
            return;
        }

        if (theEntity.GetDamagedTarget() == entityTarget || (entityTarget != null && entityTarget.GetDamagedTarget() == theEntity))
        {
            homeTimeout = (theEntity.IsSleeper ? 90f : chaseTimeMax);
            if (blockTargetTask != null)
            {
                blockTargetTask.canExecute = false;
            }

            theEntity.ClearDamagedTarget();
            if ((bool)entityTarget)
            {
                entityTarget.ClearDamagedTarget();
            }
        }

        if (attackTimeout > 0)
        {
            IconicLog.Trace(theEntity, TaskName, $"Update: attackTimeout={attackTimeout}");
            return;
        }

        if (manager.groupCircle > 0f)
        {
            Entity targetIfAttackedNow = theEntity.GetTargetIfAttackedNow();
            if (targetIfAttackedNow != entityTarget && (!entityTarget.AttachedToEntity || entityTarget.AttachedToEntity != targetIfAttackedNow))
            {
                if (targetIfAttackedNow != null)
                {
                    relocateTicks = 46;
                    Vector3 vector4 = (theEntity.position - vector2).normalized * (num8 + 1.1f);
                    float num10 = base.RandomFloat * 28f + 18f;
                    if (base.RandomFloat < 0.5f)
                    {
                        num10 = 0f - num10;
                    }

                    vector4 = Quaternion.Euler(0f, num10, 0f) * vector4;
                    Vector3 targetPos = vector2 + vector4;
                    theEntity.FindPath(targetPos, theEntity.GetMoveSpeedAggro(), canBreak: true, this);
                    IconicLog.Trace(theEntity, TaskName, $"Update: groupCircle relocate to {targetPos}");
                }

                return;
            }
        }

        theEntity.SleeperSupressLivingSounds = false;
        if (theEntity.Attack(_isReleased: false))
        {
            attackTimeout = theEntity.GetAttackTimeoutTicks();
            IconicLog.Debug(theEntity, TaskName, $"Update: attack swing, next timeout={attackTimeout}");
            theEntity.Attack(_isReleased: true);
        }
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public float GetTargetXZDistanceSq(float estimatedTicks)
    {
        Vector3 vector = entityTarget.position;
        vector += entityTargetVel * estimatedTicks;
        if (isTargetToEat)
        {
            vector = entityTarget.getBellyPosition();
        }

        Vector3 vector2 = theEntity.position + theEntity.motion * estimatedTicks - vector;
        vector2.y = 0f;
        return vector2.sqrMagnitude;
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public Vector3 GetMoveToLocation(float maxDist)
    {
        Vector3 pos = entityTarget.position;
        pos += entityTargetVel * 6f;
        if (isTargetToEat)
        {
            pos = entityTarget.getBellyPosition();
        }

        pos = entityTarget.world.FindSupportingBlockPos(pos);
        if (maxDist > 0f)
        {
            Vector3 vector = new Vector3(theEntity.position.x, pos.y, theEntity.position.z);
            Vector3 vector2 = pos - vector;
            float magnitude = vector2.magnitude;
            if (magnitude < 3f)
            {
                if (magnitude <= maxDist)
                {
                    float num = pos.y - theEntity.position.y;
                    if (num < -3f || num > 1.5f)
                    {
                        return pos;
                    }

                    return vector;
                }

                vector2 *= maxDist / magnitude;
                Vector3 vector3 = pos - vector2;
                vector3.y += 0.51f;
                Vector3i pos2 = World.worldToBlockPos(vector3);
                BlockValue block = entityTarget.world.GetBlock(pos2);
                Block block2 = block.Block;
                if (block2.PathType <= 0)
                {
                    if (Physics.Raycast(vector3 - Origin.position, Vector3.down, out var hitInfo, 1.02f, 1082195968))
                    {
                        vector3.y = hitInfo.point.y + Origin.position.y;
                        return vector3;
                    }

                    if (block2.IsElevator(block.rotation))
                    {
                        vector3.y = pos.y;
                        return vector3;
                    }
                }
            }
        }

        return pos;
    }

    public override string ToString()
    {
        ItemValue holdingItemItemValue = theEntity.inventory.holdingItemItemValue;
        int holdingItemIdx = theEntity.inventory.holdingItemIdx;
        ItemAction itemAction = holdingItemItemValue.ItemClass.Actions[holdingItemIdx];
        float num = 1.095f;
        if (!isTargetToEat && itemAction != null)
        {
            num = itemAction.Range;
            if (num == 0f)
            {
                num = EffectManager.GetItemValue(PassiveEffects.MaxRange, holdingItemItemValue);
            }
        }

        float value = (isTargetToEat ? num : (num - 0.1f));
        float targetXZDistanceSq = GetTargetXZDistanceSq(0f);
        return string.Format("{0}, {1}{2}{3}{4}{5} dist {6} rng {7} timeout {8}", base.ToString(), entityTarget ? entityTarget.EntityName : "", theEntity.CanSee(entityTarget) ? "(see)" : "", theEntity.navigator.noPathAndNotPlanningOne() ? "(-path)" : (theEntity.navigator.noPath() ? "(!path)" : ""), isTargetToEat ? "(eat)" : "", isGoingHome ? "(home)" : "", Mathf.Sqrt(targetXZDistanceSq).ToCultureInvariantString("0.000"), value.ToCultureInvariantString("0.000"), homeTimeout.ToCultureInvariantString("0.00"));
    }
}