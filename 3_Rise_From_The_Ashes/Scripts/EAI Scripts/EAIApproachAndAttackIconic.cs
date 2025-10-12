using GamePath;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using static EntityVehicle;

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

    private const float VerticalStopThreshold = 1.25f; // stop moving if height difference is larger than this
    private const float VerticalXZRange = 1.5f; // how close in X/Z before we consider target above/below
    private const float AttackVerticalRange = 3.5f; // can attack/break blocks if target is within this vertical range

    // Position tracking for loitering detection
    private const float LoiterDetectionRadius = 3f; // If zombie stays within this radius...
    private const float LoiterDetectionTime = 2f; // ...for this many seconds, it's loitering
    private Vector3 loiterStartPosition;
    private float loiterTimer;
    private bool isLoitering;

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
        Log.Out($"[{TaskName}] id={_theEntity.entityId} Init: mutex={MutexBits} execDelay={executeDelay}");
    }

    public override void SetData(DictionarySave<string, string> data)
    {
        base.SetData(data);
        targetClasses = new List<TargetClass>();
        if (!data.TryGetValue("class", out var _value))
        {
            Log.Warning($"[{TaskName}] id={theEntity?.entityId ?? -1} SetData: no 'class' value provided");
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
        Log.Out($"[{TaskName}] id={theEntity?.entityId ?? -1} SetData: targetClasses={targetClasses.Count}");
    }

    public void SetTargetOnlyPlayers()
    {
        targetClasses.Clear();
        TargetClass item = default(TargetClass);
        item.type = typeof(EntityPlayer);
        targetClasses.Add(item);
        Log.Out($"[{TaskName}] id={theEntity?.entityId ?? -1} SetTargetOnlyPlayers");
    }

    public override bool CanExecute()
    {
        Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute called");
        
        if (theEntity.sleepingOrWakingUp || theEntity.bodyDamage.CurrentStun != 0 || (theEntity.Jumping && !theEntity.isSwimming))
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute=FALSE - sleeping/stunned/jumping");
            return false;
        }

        entityTarget = theEntity.GetAttackTarget();
        if (entityTarget == null)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} No attack target set, trying auto-acquire");
            // Fallback: if no EAITarget task has set an attack target, try to auto-acquire a valid player target
            if (!TryAutoAcquireTarget())
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute=FALSE - No target and auto-acquire failed");
                return false;
            }
            entityTarget = theEntity.GetAttackTarget();
        }

        Type type = entityTarget.GetType();
        Log.Out($"[{TaskName}] id={theEntity.entityId} Target: {entityTarget.EntityName} ({type.Name})");
        
        if (targetClasses != null && targetClasses.Count > 0)
        {
            for (int i = 0; i < targetClasses.Count; i++)
            {
                TargetClass targetClass = targetClasses[i];
                if (targetClass.type != null && targetClass.type.IsAssignableFrom(type))
                {
                    chaseTimeMax = targetClass.chaseTimeMax;
                    Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute=TRUE - Target matches class {targetClass.type.Name}");
                    return true;
                }
            }

            Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute=FALSE - Target type doesn't match any configured classes");
            return false;
        }

        // No target classes configured -> accept any target we acquired (default behavior)
        Log.Out($"[{TaskName}] id={theEntity.entityId} CanExecute=TRUE - No target classes configured");
        return true;
    }

    private bool TryAutoAcquireTarget()
    {
        // Use see distance and sense scale similarly to EAITarget logic
        var seeDist = theEntity.GetSeeDistance();
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
            Log.Out($"[{TaskName}] id={theEntity.entityId} Auto-acquired target: {best.EntityName} dist={bestDist:0.00}");
            return true;
        }

        return false;
    }

    public override void Start()
    {
        Log.Out($"[{TaskName}] id={theEntity.entityId} START - Target: {entityTarget.EntityName}");
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
        
        // Initialize loiter detection
        loiterStartPosition = theEntity.position;
        loiterTimer = 0f;
        isLoitering = false;
        
        Log.Out($"[{TaskName}] id={theEntity.entityId} START - isTargetToEat={isTargetToEat}, hasHome={hasHome}");
    }

    public override bool Continue()
    {
        if (theEntity.sleepingOrWakingUp || theEntity.bodyDamage.CurrentStun != 0)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} Continue=FALSE - sleeping/stunned");
            return false;
        }

        EntityAlive attackTarget = theEntity.GetAttackTarget();
        
        // Add null check and validation for attack target
        if (attackTarget == null || attackTarget.IsDead() || attackTarget.IsMarkedForUnload())
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} Continue=FALSE - No valid attack target");
            return false;
        }
        
        if (isGoingHome)
        {
            if (!attackTarget)
            {
                bool shouldContinue = theEntity.ChaseReturnLocation != Vector3.zero;
                Log.Out($"[{TaskName}] id={theEntity.entityId} Continue={shouldContinue} - Going home");
                return shouldContinue;
            }

            Log.Out($"[{TaskName}] id={theEntity.entityId} Continue=FALSE - Was going home but have new target");
            return false;
        }

        if (!attackTarget)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} Continue=FALSE - No attack target");
            return false;
        }

        if (attackTarget != entityTarget)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} Continue=FALSE - Attack target changed");
            return false;
        }

        if (attackTarget.IsDead() != isTargetToEat)
        {
            Log.Out($"[{TaskName}] id={theEntity.entityId} Continue=FALSE - Target death state changed");
            return false;
        }

        return true;
    }

    public override void Reset()
    {
        Log.Out($"[{TaskName}] id={theEntity.entityId} RESET");
        theEntity.IsEating = false;
        theEntity.moveHelper.Stop();
        if (blockTargetTask != null)
        {
            blockTargetTask.canExecute = false;
        }
        
        // Reset loiter detection
        isLoitering = false;
        loiterTimer = 0f;
    }

    public override void Update()
    {
        // Log every 2 seconds
        if (UnityEngine.Time.frameCount % 120 == 0)
        {
            if (entityTarget != null)
            {
                Vector3 targetPos = entityTarget.position;
                float dist = Vector3.Distance(theEntity.position, targetPos);
                float yDiff = targetPos.y - theEntity.position.y;
                Log.Out($"[{TaskName}] id={theEntity.entityId} Update - Target: {entityTarget.EntityName}, Dist: {dist:F2}, YDiff: {yDiff:F2}, IsGoingHome: {isGoingHome}, Loitering: {isLoitering}");
            }
        }
        
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
                else
                {
                    // Direct movement back home - no pathfinding
                    Vector3 homeDir = theEntity.ChaseReturnLocation - theEntity.position;
                    homeDir.y = 0f; // Ignore vertical
                    if (homeDir.sqrMagnitude > 0.01f)
                    {
                        homeDir.Normalize();
                        MoveEntityHeaded(homeDir, true);
                    }
                }

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
                return;
            }
        }

        if (entityTarget == null)
        {
            return;
        }

        if (relocateTicks > 0)
        {
            // Keep focus on target while relocating directly
            relocateTicks--;
            theEntity.moveHelper.SetFocusPos(entityTarget.position);
            return;
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

        // Calculate horizontal distance
        Vector3 xzDiff = vector2 - theEntity.position;
        xzDiff.y = 0f;
        float xzDistSq = xzDiff.sqrMagnitude;
        float xzDist = Mathf.Sqrt(xzDistSq);

        // Check if we're close horizontally and target is above/below
        bool isCloseHorizontally = xzDistSq < (VerticalXZRange * VerticalXZRange);
        bool isTargetVerticallyDistant = num6 > VerticalStopThreshold;
        
        // LOITER DETECTION: Track if zombie is staying in a small area
        UpdateLoiterDetection(isCloseHorizontally, isTargetVerticallyDistant);
        
        // SIMPLIFIED MOVEMENT LOGIC: Always move directly toward target's horizontal position
        // Stop only when we're very close horizontally AND close vertically (normal attack range)
        bool shouldStop = targetXZDistanceSq <= num3 && num6 < 1f;
        
        if (!shouldStop)
        {
            // Direct horizontal pursuit - moveHelper will detect blocks and trigger EAIBreakBlocksIconic
            Vector3 targetHorizontalPos = GetMoveToLocation(num2, isTargetVerticallyDistant);
            Vector3 moveDirection = targetHorizontalPos - theEntity.position;
            moveDirection.y = 0f; // Always flatten to horizontal movement
            
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                moveDirection.Normalize();
                
                // Enable block breaking mode when moving - moveHelper will track blocked state
                // Also enable if loitering (stuck in small area)
                theEntity.moveHelper.SetMoveTo(targetHorizontalPos, true); // true = can break blocks
                
                // If loitering, artificially increase BlockedTime to trigger break blocks
                if (isLoitering)
                {
                    theEntity.moveHelper.BlockedTime = Mathf.Max(theEntity.moveHelper.BlockedTime, 0.4f);
                    
                    if (UnityEngine.Time.frameCount % 60 == 0)
                    {
                        Log.Out($"[{TaskName}] id={theEntity.entityId} LOITERING DETECTED - Forcing BlockedTime={theEntity.moveHelper.BlockedTime:F2}");
                    }
                }
                
                MoveEntityHeaded(moveDirection, true);
                
                if (UnityEngine.Time.frameCount % 60 == 0) // Log every second
                {
                    Log.Out($"[{TaskName}] id={theEntity.entityId} DirectMove: xzDist={xzDist:F2} yDiff={num5:F2} target=({targetHorizontalPos.x:F1},{targetHorizontalPos.y:F1},{targetHorizontalPos.z:F1})");
                }
            }
        }
        else
        {
            theEntity.moveHelper.Stop();
            pathCounter = 0;
            
            if (UnityEngine.Time.frameCount % 60 == 0)
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} Stopped: InRange xzDist={xzDist:F2} yDiff={num5:F2}");
            }
        }

        // Look at target when stopped or close
        if (shouldStop || isCloseHorizontally)
        {
            theEntity.SetLookPosition(entityTarget.getHeadPosition());
        }

        if (theEntity.Climbing)
        {
            return;
        }

        // Modified attack range check: allow attacking if we're close horizontally, even if target is above/below
        float num8 = (isTargetToEat ? num : (num - 0.1f));
        float num9 = num8 * num8;
        
        // Check if we can attack: either in normal range OR close horizontally and within vertical attack range
        bool canAttemptAttack = false;
        if (isCloseHorizontally && num6 <= AttackVerticalRange)
        {
            // Close horizontally and target is within vertical attack range (above or below)
            canAttemptAttack = true;
            Log.Out($"[{TaskName}] id={theEntity.entityId} CanAttack: vertical | xzDist={xzDist:F2} yDelta={num5:F2}");
        }
        else if (targetXZDistanceSq <= num9 && num5 >= -1.25f && num5 - theEntity.GetHeight() <= 0.65f)
        {
            // Normal attack range (at same level)
            canAttemptAttack = true;
            Log.Out($"[{TaskName}] id={theEntity.entityId} CanAttack: normal range");
        }

        if (!canAttemptAttack)
        {
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
                    Vector3 relocateDir = (vector2 + vector4) - theEntity.position;
                    relocateDir.y = 0f;
                    if (relocateDir.sqrMagnitude > 0.01f)
                    {
                        relocateDir.Normalize();
                        MoveEntityHeaded(relocateDir, true);
                    }
                }

                return;
            }
        }

        theEntity.SleeperSupressLivingSounds = false;
        if (theEntity.Attack(_isReleased: false))
        {
            attackTimeout = theEntity.GetAttackTimeoutTicks();
            theEntity.Attack(_isReleased: true);
        }
    }

    /// <summary>
    /// Detects if zombie is loitering (stuck in small area) which indicates it needs to break blocks
    /// </summary>
    private void UpdateLoiterDetection(bool isCloseHorizontally, bool isTargetVerticallyDistant)
    {
        // Only track loitering when close to target horizontally and target is above/below
        if (!isCloseHorizontally || !isTargetVerticallyDistant)
        {
            // Reset if we're not in a loiter scenario
            if (isLoitering || loiterTimer > 0.1f)
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} Loiter reset - not in scenario");
            }
            isLoitering = false;
            loiterTimer = 0f;
            loiterStartPosition = theEntity.position;
            return;
        }

        // Calculate how far zombie has moved from loiter start position (XZ only)
        Vector3 currentPos = theEntity.position;
        Vector3 xzDiff = currentPos - loiterStartPosition;
        xzDiff.y = 0f;
        float distanceMoved = xzDiff.magnitude;

        if (distanceMoved <= LoiterDetectionRadius)
        {
            // Still within loiter radius, accumulate time
            loiterTimer += 0.05f; // Update is called every 0.05s based on homeTimeout

            if (loiterTimer >= LoiterDetectionTime && !isLoitering)
            {
                isLoitering = true;
                Log.Out($"[{TaskName}] id={theEntity.entityId} ✓ LOITERING DETECTED - Stayed within {LoiterDetectionRadius:F1}m for {loiterTimer:F2}s at ({loiterStartPosition.x:F1},{loiterStartPosition.y:F1},{loiterStartPosition.z:F1})");
            }
        }
        else
        {
            // Moved too far, reset tracking
            if (loiterTimer > 0.5f) // Only log if we were tracking for a while
            {
                Log.Out($"[{TaskName}] id={theEntity.entityId} Loiter reset - moved {distanceMoved:F2}m from start");
            }
            loiterStartPosition = currentPos;
            loiterTimer = 0f;
            isLoitering = false;
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
    public Vector3 GetMoveToLocation(float maxDist, bool targetIsVerticallyDistant = false)
    {
        // DIRECT PURSUIT: Move to player's horizontal (X/Z) position
        Vector3 pos = entityTarget.position + entityTargetVel * 0.5f;
        if (isTargetToEat)
        {
            pos = entityTarget.getBellyPosition();
        }

        // Always flatten Y to zombie's current level for horizontal pursuit
        // This makes the zombie move directly under/to the player's XZ position
        pos.y = theEntity.position.y;
        
        return pos;
    }

    public virtual void MoveEntityHeaded(Vector3 _direction, bool _isDirAbsolute)
    {
        if (theEntity.AttachedToEntity != null)
        {
            return;
        }

        if (theEntity.jumpIsMoving)
        {
            theEntity.JumpMove();
            return;
        }

        if (theEntity.RootMotion)
        {
            if (theEntity.isEntityRemote && theEntity.bodyDamage.CurrentStun == EnumEntityStunType.None && !theEntity.IsDead() && (!(theEntity.emodel != null) || !(theEntity.emodel.avatarController != null) || !theEntity.emodel.avatarController.IsAnimationHitRunning()))
            {
                theEntity.accumulatedRootMotion = Vector3.zero;
                return;
            }

            bool flag = (bool)theEntity.emodel && theEntity.emodel.IsRagdollActive;
            if (theEntity.isSwimming && !flag)
            {
                theEntity.motion += theEntity.accumulatedRootMotion * 0.001f;
            }
            else if (theEntity.onGround || theEntity.jumpTicks > 0)
            {
                if (flag)
                {
                    theEntity.motion.x = 0f;
                    theEntity.motion.z = 0f;
                }
                else
                {
                    float y = theEntity.motion.y;
                    theEntity.motion = theEntity.accumulatedRootMotion;
                    theEntity.motion.y += y;
                }
            }

            theEntity.accumulatedRootMotion = Vector3.zero;
        }

        if (theEntity.IsFlyMode.Value)
        {
            EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
            float num = ((primaryPlayer != null) ? primaryPlayer.GodModeSpeedModifier : 1f);
            float num2 = 2f * (theEntity.MovementRunning ? 0.35f : 0.12f) * num;
            if (!theEntity.RootMotion)
            {
                theEntity.Move(_direction, _isDirAbsolute, theEntity.GetPassiveEffectSpeedModifier() * num2, theEntity.GetPassiveEffectSpeedModifier() * num2);
            }

            if (!theEntity.IsNoCollisionMode.Value)
            {
                theEntity.entityCollision(theEntity.motion);
                theEntity.motion *= theEntity.ConditionalScalePhysicsMulConstant(0.546f);
            }
            else
            {
                theEntity.SetPosition(theEntity.position + theEntity.motion);
                theEntity.motion = Vector3.zero;
            }
        }
        else
        {
            theEntity.DefaultMoveEntity(_direction, _isDirAbsolute);
        }

        if (theEntity.isEntityRemote || !theEntity.RootMotion)
        {
            return;
        }

        float num3 = theEntity.landMovementFactor;
        num3 *= 2.5f;
        if (theEntity.inWaterPercent > 0.3f)
        {
            if (num3 > 0.01f)
            {
                float t = (theEntity.inWaterPercent - 0.3f) * 1.42857146f;
                num3 = Mathf.Lerp(num3, 0.01f + (num3 - 0.01f) * 0.1f, t);
            }

            if (theEntity.isSwimming)
            {
                num3 = theEntity.landMovementFactor * 5f;
            }
        }

        float magnitude = _direction.magnitude;
        if (magnitude > 1f)
        {
            num3 /= magnitude;
        }

        float num4 = _direction.z * num3;
        if (theEntity.lerpForwardSpeed)
        {
            if (Utils.FastAbs(theEntity.speedForwardTarget - num4) > 0.05f)
            {
                theEntity.speedForwardTargetStep = Utils.FastAbs(num4 - theEntity.speedForward) / 0.18f;
            }

            theEntity.speedForwardTarget = num4;
        }
        else
        {
            theEntity.speedForward = num4;
        }

        theEntity.speedStrafe = _direction.x * num3;
        theEntity.SetMovementState();
        theEntity.ReplicateSpeeds();
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
        return string.Format("{0}, {1}{2}{3}{4}{5} dist {6} rng {7} timeout {8}", base.ToString(), entityTarget ? entityTarget.EntityName : "", theEntity.CanSee(entityTarget) ? "(see)" : "", "(direct)", isTargetToEat ? "(eat)" : "", isGoingHome ? "(home)" : "", Mathf.Sqrt(targetXZDistanceSq).ToCultureInvariantString("0.000"), value.ToCultureInvariantString("0.000"), homeTimeout.ToCultureInvariantString("0.00"));
    }
}