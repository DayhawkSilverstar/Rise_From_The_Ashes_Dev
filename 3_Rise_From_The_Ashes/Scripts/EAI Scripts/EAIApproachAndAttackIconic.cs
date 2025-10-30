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

    private const float VerticalStopThreshold = 1.25f;
    private const float VerticalXZRange = 1.5f;
    private const float AttackVerticalRange = 3.5f;

    // Position tracking for loitering detection
    private const float LoiterDetectionRadius = 3f;
    private const float LoiterDetectionTime = 2f;
    private Vector3 loiterStartPosition;
    private float loiterTimer;
    private bool isLoitering;

    // Ally spawn call after sustained targeting
    private float allyCallTimer;
    private float allyCallThresholdSeconds;

    // Resilience to resets
    private int allyTargetEntityId = -1;
    private float allyTimerLastValidTime = -999f;
    private const float AllyTimerGraceWindow = 5f;

    private int zombiesSpawned = 0;
    private int maxZombiesToSpawn = 2;

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

    // TARGETED LOGGING: Track only essential movement data to identify jitter cause
    private static bool enableJitterTracking = true;
    private Vector3 lastFramePosition = Vector3.zero;
    private int moveCommandsThisFrame = 0;
    private int currentFrame = -1;
    
    // ANIMATOR DIAGNOSTIC: Track animator parameter changes for directional movement issue
    private float lastSpeedForward = 0f;
    private float lastSpeedStrafe = 0f;
    
    // STATE TRANSITION TRACKING: Detect if jitter correlates with EAI state changes
    private static bool enableStateTransitionLogging = true;
    private bool wasExecutingLastFrame = false;
    
    // STATE CYCLING PREVENTION: Prevent rapid START/STOP thrashing
    private float lastStopTime = -999f;
    private const float MinRestartDelay = 0.5f; // Prevent restart for 0.5s after stopping
    private int consecutiveStops = 0;
    private const int MaxConsecutiveStops = 3; // If stopping 3+ times in quick succession, something is wrong

    private static readonly List<Entity> TmpEntities = new List<Entity>();

    private string TaskName => nameof(EAIApproachAndAttackIconic);

    public EAIApproachAndAttackIconic()
    {        
        chaseTimeMax = 30f;
        seekPosOffset = Vector2.zero;
        targetClasses = new List<TargetClass>();
    }

    public override void Init(EntityAlive _theEntity)
    {        
        base.Init(_theEntity);
        MutexBits = 3;
        executeDelay = 0.1f;
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
    }

    public void SetTargetOnlyPlayers()
    {
        targetClasses.Clear();
        TargetClass item = default(TargetClass);
        item.type = typeof(EntityPlayer);
        targetClasses.Add(item);        
    }

    public override bool CanExecute()
    {        
        if (theEntity.sleepingOrWakingUp || theEntity.bodyDamage.CurrentStun != 0 || (theEntity.Jumping && !theEntity.isSwimming))
        {
            return false;
        }
        
        // STATE CYCLING PREVENTION: Prevent rapid restart after stopping
        float timeSinceStop = Time.time - lastStopTime;
        if (timeSinceStop < MinRestartDelay)
        {
            if (enableStateTransitionLogging && !theEntity.isEntityRemote)
            {
                Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} Prevented rapid restart (only {timeSinceStop:F2}s since stop, need {MinRestartDelay}s)");
            }
            return false;
        }

        entityTarget = theEntity.GetAttackTarget();
        if (entityTarget == null)
        {            
            // Fallback: if no EAITarget task has set an attack target, try to auto-acquire a valid player target
            if (!TryAutoAcquireTarget())
            {
                return false;
            }
            entityTarget = theEntity.GetAttackTarget();
        }
        
        // Additional validation: ensure target is actually valid
        if (entityTarget == null || entityTarget.IsDead() || entityTarget.IsMarkedForUnload())
        {
            if (enableStateTransitionLogging && !theEntity.isEntityRemote)
            {
                Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} CanExecute failed: invalid target (null:{entityTarget == null}, dead:{entityTarget?.IsDead()}, unload:{entityTarget?.IsMarkedForUnload()})");
            }
            return false;
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
                    
                    // Reset cycling counter on successful CanExecute
                    consecutiveStops = 0;
                    return true;
                }
            }
            
            return false;
        }

        // Reset cycling counter on successful CanExecute
        consecutiveStops = 0;
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
            return true;
        }

        return false;
    }

    public override void Start()
    {
        // STATE TRANSITION LOG: Starting task
        if (enableStateTransitionLogging && !theEntity.isEntityRemote)
        {
            Log.Out($"[EAI-STATE] Entity:{theEntity.entityId} ApproachAndAttack STARTING");
        }
        
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

        // Resilient ally call timer handling
        int newTargetId = entityTarget != null ? entityTarget.entityId : -1;
        bool sameTargetWithinGrace = (newTargetId == allyTargetEntityId) && (Time.time - allyTimerLastValidTime <= AllyTimerGraceWindow);
        if (!sameTargetWithinGrace)
        {
            // New or different target, or grace expired -> reset
            allyCallTimer = 0f;
        }
        allyTargetEntityId = newTargetId;

        // Compute a dynamic ally-call threshold so it can actually fire before we give up and go home.
        // We aim for 60s, but clamp to be <= homeTimeout (if any) and not less than 10s.
        if (hasHome)
        {
            float desired = 60f;
            if (zombiesSpawned == 1)
                desired = 120f;
            // Leave a small margin before going home so the event can fire
            float maxBeforeHome = Mathf.Max(0f, homeTimeout - 1.5f);
            allyCallThresholdSeconds = Mathf.Max(10f, Mathf.Min(desired, maxBeforeHome));
        }
        else
        {
            allyCallThresholdSeconds = 60f;
            if (zombiesSpawned == 1)
                allyCallThresholdSeconds = 120f;

        }
        
        // TARGETED: Initialize position tracking
        lastFramePosition = theEntity.position;
        wasExecutingLastFrame = true;
    }

    public override bool Continue()
    {
        if (theEntity.sleepingOrWakingUp || theEntity.bodyDamage.CurrentStun != 0)
        {
            if (enableStateTransitionLogging && !theEntity.isEntityRemote)
            {
                Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} Continue=false: sleeping/stunned");
            }
            return false;
        }

        EntityAlive attackTarget = theEntity.GetAttackTarget();
        
        // First validate our cached target is still valid
        if (entityTarget != null && (entityTarget.IsDead() || entityTarget.IsMarkedForUnload()))
        {
            if (enableStateTransitionLogging && !theEntity.isEntityRemote)
            {
                Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} Continue=false: cached target {entityTarget.entityId} became invalid (dead:{entityTarget.IsDead()}, unload:{entityTarget.IsMarkedForUnload()})");
            }
            entityTarget = null;
            return false;
        }
        
        // CYCLING FIX: Only restore target if it was cleared externally AND our cached target is still valid
        if (attackTarget == null && entityTarget != null && !entityTarget.IsDead() && !entityTarget.IsMarkedForUnload())
        {
            if (enableStateTransitionLogging && !theEntity.isEntityRemote)
            {
                Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} Restoring target {entityTarget.entityId} that was cleared externally");
            }
            theEntity.SetAttackTarget(entityTarget, 200);
            attackTarget = entityTarget;
        }
        
        // Validate attack target
        if (attackTarget == null || attackTarget.IsDead() || attackTarget.IsMarkedForUnload())
        {
            if (enableStateTransitionLogging && !theEntity.isEntityRemote)
            {
                string reason = attackTarget == null ? "null" : (attackTarget.IsDead() ? "dead" : "unloading");
                Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} Continue=false: invalid target ({reason})");
            }
            return false;
        }
        
        if (isGoingHome)
        {
            if (!attackTarget)
            {
                bool shouldContinue = theEntity.ChaseReturnLocation != Vector3.zero;
                if (enableStateTransitionLogging && !theEntity.isEntityRemote && !shouldContinue)
                {
                    Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} Continue=false: going home but no return location");
                }
                return shouldContinue;
            }
            
            if (enableStateTransitionLogging && !theEntity.isEntityRemote)
            {
                Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} Continue=false: going home but have target");
            }
            return false;
        }

        if (!attackTarget)
        {
            if (enableStateTransitionLogging && !theEntity.isEntityRemote)
            {
                Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} Continue=false: no attack target");
            }
            return false;
        }

        if (attackTarget != entityTarget)
        {
            // Be resilient: if the logical target is the same (entityId), keep going and update reference
            if (entityTarget != null && attackTarget.entityId == entityTarget.entityId)
            {
                entityTarget = attackTarget;
            }
            else
            {
                if (enableStateTransitionLogging && !theEntity.isEntityRemote)
                {
                    Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} Continue=false: target changed from {entityTarget?.entityId ?? -1} to {attackTarget.entityId}");
                }
                return false;
            }
        }

        if (attackTarget.IsDead() != isTargetToEat)
        {
            if (enableStateTransitionLogging && !theEntity.isEntityRemote)
            {
                Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} Continue=false: target death state changed (isDead:{attackTarget.IsDead()}, isTargetToEat:{isTargetToEat})");
            }
            return false;
        }

        return true;
    }

    public override void Reset()
    {
        // Track consecutive stops for cycling detection
        consecutiveStops++;
        lastStopTime = Time.time;
        
        // STATE TRANSITION LOG: Task ending
        if (enableStateTransitionLogging && !theEntity.isEntityRemote && wasExecutingLastFrame)
        {
            if (consecutiveStops >= MaxConsecutiveStops)
            {
                Log.Error($"[EAI-CYCLING] Entity:{theEntity.entityId} ApproachAndAttack STOPPING (#{consecutiveStops} consecutive stops - CYCLING DETECTED!)");
            }
            else
            {
                Log.Out($"[EAI-STATE] Entity:{theEntity.entityId} ApproachAndAttack STOPPING");
            }
        }
        
        theEntity.IsEating = false;
        theEntity.moveHelper.Stop();
        if (blockTargetTask != null)
        {
            blockTargetTask.canExecute = false;
        }
        
        // Reset loiter detection
        isLoitering = false;
        loiterTimer = 0f;

        // Preserve ally timer through brief resets; record last valid time to honor grace window
        allyTimerLastValidTime = Mathf.Max(allyTimerLastValidTime, Time.time);
        
        wasExecutingLastFrame = false;
    }

    public override void Update()
    {
        // CRITICAL: Exit immediately if breaking blocks AND in range to prevent movement command conflicts
        // EAIBreakBlocksIconic handles its own movement via SetMoveTo() when approaching
        // Only skip movement if we're BOTH breaking blocks AND close enough to attack
        if (theEntity.IsBreakingBlocks)
        {
            // Check if we're actually in range to attack the block
            // If not, we should still allow movement (EAIBreakBlocksIconic handles it)
            var moveHelper = theEntity.moveHelper;
            if (moveHelper != null && moveHelper.HitInfo != null && moveHelper.HitInfo.bHitValid)
            {
                Vector3 blockPos = moveHelper.HitInfo.hit.pos;
                float distToBlock = Vector3.Distance(theEntity.position, blockPos);
                
                // Get attack range
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
                
                // Only exit if we're in range - otherwise continue to allow movement
                if (distToBlock <= range)
                {
                    return;
                }
            }
        }
        
        // TARGETED: Track frame changes and movement commands
        int frame = Time.frameCount;
        if (frame != currentFrame)
        {
            // TARGETED: Log if multiple movement commands in previous frame
            if (enableJitterTracking && !theEntity.isEntityRemote && moveCommandsThisFrame > 1)
            {
                Log.Warning($"[JITTER] Entity:{theEntity.entityId} Frame:{currentFrame} MultipleMoveCmds:{moveCommandsThisFrame}");
            }
            
            currentFrame = frame;
            moveCommandsThisFrame = 0;
        }
        
        // Skip AI movement for remote entities
        if (theEntity.isEntityRemote)
        {
            return;
        }

        // Ally call timer tracking
        var currentTarget = theEntity.GetAttackTarget();
        
        // First validate our cached target
        if (entityTarget != null && (entityTarget.IsDead() || entityTarget.IsMarkedForUnload()))
        {
            entityTarget = null;
            
            // Clear IconicZombie.Target if applicable
            if (theEntity is IconicZombie iconicZombie)
            {
                iconicZombie.Target = null;
            }
        }
        
        // CYCLING FIX: Only restore target if cached target is still valid
        if (currentTarget == null && entityTarget != null && !entityTarget.IsDead() && !entityTarget.IsMarkedForUnload())
        {
            if (enableStateTransitionLogging && !theEntity.isEntityRemote)
            {
                Log.Warning($"[EAI-CYCLING] Entity:{theEntity.entityId} Update: Restoring cleared target {entityTarget.entityId}");
            }
            theEntity.SetAttackTarget(entityTarget, 200);
            currentTarget = entityTarget;
        }
        
        if (currentTarget != null && !currentTarget.IsDead() && !currentTarget.IsMarkedForUnload())
        {
            allyCallTimer += Time.deltaTime;
            allyTimerLastValidTime = Time.time;
            allyTargetEntityId = currentTarget.entityId;

            if (allyCallTimer >= allyCallThresholdSeconds && zombiesSpawned < maxZombiesToSpawn && !theEntity.IsDead())
            {
                zombiesSpawned++;
                theEntity.FireEvent(MinEventTypes.onSelfAction2Start, true);
                allyCallTimer = 0f;
                allyTimerLastValidTime = Time.time;
            }
        }
        else
        {
            if (Time.time - allyTimerLastValidTime > AllyTimerGraceWindow)
            {
                allyCallTimer = 0f;
                allyTargetEntityId = -1;
            }
        }

        if (theEntity.IsBreakingBlocks)
        {
            return;
        }
        
        bool didMove = false;
        
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
                    Vector3 homeDir = theEntity.ChaseReturnLocation - theEntity.position;
                    homeDir.y = 0f;
                    if (homeDir.sqrMagnitude > 0.01f)
                    {
                        homeDir.Normalize();
                        MoveEntityHeaded(homeDir, true);
                        didMove = true;
                    }
                }

                if (didMove && theEntity.RootMotion && !theEntity.isEntityRemote)
                {
                    theEntity.SetMovementState();
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

        Vector3 xzDiff = vector2 - theEntity.position;
        xzDiff.y = 0f;
        float xzDistSq = xzDiff.sqrMagnitude;

        bool isCloseHorizontally = xzDistSq < (VerticalXZRange * VerticalXZRange);
        bool isTargetVerticallyDistant = num6 > VerticalStopThreshold;
        
        UpdateLoiterDetection(isCloseHorizontally, isTargetVerticallyDistant);
        
        bool shouldStop = targetXZDistanceSq <= num3 && num6 < 1f;
        
        if (!shouldStop)
        {
            Vector3 targetHorizontalPos = GetMoveToLocation(num2, isTargetVerticallyDistant);
            Vector3 moveDirection = targetHorizontalPos - theEntity.position;
            moveDirection.y = 0f;
            
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                moveDirection.Normalize();
                
                // JITTER FIX: Rotate to face movement direction BEFORE moving
                // This ensures zombie always moves "forward" in local space, preventing strafe animation issues
                // BUT: Don't rotate if IsBreakingBlocks - let EAIBreakBlocksIconic control rotation
                if (theEntity.bodyDamage.HasLimbs && !theEntity.Electrocuted && !theEntity.IsBreakingBlocks)
                {
                    Vector3 targetPos = entityTarget.position;
                    theEntity.RotateTo(targetPos.x, targetPos.y, targetPos.z, 45f, 45f);
                }
                
                theEntity.moveHelper.SetMoveTo(targetHorizontalPos, true);
                
                if (isLoitering)
                {
                    theEntity.moveHelper.BlockedTime = Mathf.Max(theEntity.moveHelper.BlockedTime, 0.4f);
                }
                
                MoveEntityHeaded(moveDirection, true);
                didMove = true;
            }
        }
        else
        {
            theEntity.moveHelper.Stop();
            pathCounter = 0;
        }

        // Look at target when stopped or close
        if ((shouldStop || isCloseHorizontally) && !theEntity.IsBreakingBlocks)
        {
            theEntity.SetLookPosition(entityTarget.getHeadPosition());
        }

        if (theEntity.Climbing)
        {
            if (didMove && theEntity.RootMotion && !theEntity.isEntityRemote)
            {
                theEntity.SetMovementState();
            }
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
        }
        else if (targetXZDistanceSq <= num9 && num5 >= -1.25f && num5 - theEntity.GetHeight() <= 0.65f)
        {
            // Normal attack range (at same level)
            canAttemptAttack = true;            
        }

        if (!canAttemptAttack)
        {
            if (didMove && theEntity.RootMotion && !theEntity.isEntityRemote)
            {
                theEntity.SetMovementState();
            }
            return;
        }

        // Only clear breaking flags if we are not actively breaking (avoid fighting EAIBreakBlocks)
        if (!theEntity.IsBreakingBlocks)
        {
            theEntity.IsBreakingBlocks = false;
            theEntity.IsBreakingDoors = false;
        }
        
        // Rotation is now handled during movement, no need to rotate again here
        // This prevents fighting between movement direction and attack rotation

        if (isTargetToEat)
        {
            isEating = true;
            theEntity.IsEating = true;
            attackTimeout = 20;
            eatCount = 0;
            if (didMove && theEntity.RootMotion && !theEntity.isEntityRemote)
            {
                theEntity.SetMovementState();
            }
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
            if (didMove && theEntity.RootMotion && !theEntity.isEntityRemote)
            {
                theEntity.SetMovementState();
            }
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
                        didMove = true;
                    }
                }

                if (didMove && theEntity.RootMotion && !theEntity.isEntityRemote)
                {
                    theEntity.SetMovementState();
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
        
        if (didMove && theEntity.RootMotion && !theEntity.isEntityRemote)
        {
            theEntity.SetMovementState();
        }
        
        // TARGETED: Log position changes to detect jitter
        if (enableJitterTracking && !theEntity.isEntityRemote)
        {
            Vector3 positionDelta = theEntity.position - lastFramePosition;
            float deltaDistance = positionDelta.magnitude;
            
            // Only log if there's significant unexpected movement (potential jitter)
            if (deltaDistance > 0.5f && !didMove)
            {
                Log.Warning($"[JITTER] Entity:{theEntity.entityId} UnexpectedMove Delta:{positionDelta.ToString("F3")} Dist:{deltaDistance:F3}");
            }
            
            lastFramePosition = theEntity.position;
        }
    }

    private void UpdateLoiterDetection(bool isCloseHorizontally, bool isTargetVerticallyDistant)
    {
        if (!isCloseHorizontally || !isTargetVerticallyDistant)
        {
            isLoitering = false;
            loiterTimer = 0f;
            loiterStartPosition = theEntity.position;
            return;
        }

        Vector3 currentPos = theEntity.position;
        Vector3 xzDiff = currentPos - loiterStartPosition;
        xzDiff.y = 0f;
        float distanceMoved = xzDiff.magnitude;

        if (distanceMoved <= LoiterDetectionRadius)
        {
            loiterTimer += 0.05f;

            if (loiterTimer >= LoiterDetectionTime && !isLoitering)
            {
                isLoitering = true;                
            }
        }
        else
        {
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
        Vector3 pos = entityTarget.position + entityTargetVel * 0.5f;
        if (isTargetToEat)
        {
            pos = entityTarget.getBellyPosition();
        }

        pos.y = theEntity.position.y;
        
        return pos;
    }

    public virtual void MoveEntityHeaded(Vector3 _direction, bool _isDirAbsolute)
    {
        // TARGETED: Track movement command count
        moveCommandsThisFrame++;
        
        if (theEntity.AttachedToEntity != null)
        {
            return;
        }

        if (theEntity.jumpIsMoving)
        {
            theEntity.JumpMove();
            return;
        }

        // TARGETED: Log root motion application
        if (theEntity.RootMotion)
        {
            bool flag = (bool)theEntity.emodel && theEntity.emodel.IsRagdollActive;
            
            if (enableJitterTracking && !theEntity.isEntityRemote && theEntity.accumulatedRootMotion.magnitude > 0.01f)
            {
                // Only log significant root motion changes
                Vector3 before = theEntity.motion;
                
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
                
                Vector3 motionDelta = theEntity.motion - before;
                if (motionDelta.magnitude > 0.1f)
                {
                    Log.Out($"[JITTER] Entity:{theEntity.entityId} RootMotion Applied:{theEntity.accumulatedRootMotion.ToString("F3")} MotionDelta:{motionDelta.ToString("F3")}");
                }
            }
            else
            {
                // Normal path without logging
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

        // Remote entities don't need to replicate speeds since they're synced via network
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

        // JITTER FIX: Convert all movement to forward speed
        // Since zombies always RotateTo() the target, they should never strafe
        // This fixes the X-axis movement stutter where speedStrafe was being used
        
        // Safety check: ensure we have valid direction
        float dirX = _direction.x;
        float dirZ = _direction.z;
        if (float.IsNaN(dirX)) dirX = 0f;
        if (float.IsNaN(dirZ)) dirZ = 0f;
        
        float movementSpeed = Mathf.Sqrt(dirX * dirX + dirZ * dirZ) * num3;
        
        if (theEntity.lerpForwardSpeed)
        {
            if (Utils.FastAbs(theEntity.speedForwardTarget - movementSpeed) > 0.05f)
            {
                theEntity.speedForwardTargetStep = Utils.FastAbs(movementSpeed - theEntity.speedForward) / 0.18f;
            }
            theEntity.speedForwardTarget = movementSpeed;
        }
        else
        {
            theEntity.speedForward = movementSpeed;
        }

        // Always zero strafe - zombie rotates to face direction, then moves forward
        theEntity.speedStrafe = 0f;
        theEntity.ReplicateSpeeds();
        
        // ANIMATOR DIAGNOSTIC: Log to verify fix is working
        if (enableJitterTracking && !theEntity.isEntityRemote)
        {
            float speedForwardDelta = Mathf.Abs(theEntity.speedForward - lastSpeedForward);
            float speedStrafeDelta = Mathf.Abs(theEntity.speedStrafe - lastSpeedStrafe);
            
            // Log when there's significant movement to verify strafe is now always zero
            if (speedForwardDelta > 0.1f || speedStrafeDelta > 0.01f)
            {
                Vector3 worldDirection = _direction;
                if (!_isDirAbsolute)
                {
                    worldDirection = theEntity.transform.TransformDirection(_direction);
                }
                
                // Identify which axis is dominant
                float absX = Mathf.Abs(_direction.x);
                float absZ = Mathf.Abs(_direction.z);
                string dominantAxis = absX > absZ ? "X-AXIS" : "Z-AXIS";
                
                Log.Out($"[ANIMATOR-FIXED] Entity:{theEntity.entityId} {dominantAxis} Forward:{theEntity.speedForward:F3} Strafe:{theEntity.speedStrafe:F3} " +
                       $"LocalDir:({_direction.x:F2},{_direction.z:F2}) Magnitude:{movementSpeed:F3}");
            }
            
            lastSpeedForward = theEntity.speedForward;
            lastSpeedStrafe = theEntity.speedStrafe;
        }
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
    
    // TARGETED: Simple toggle for jitter tracking
    public static void EnableJitterTracking(bool enable)
    {
        enableJitterTracking = enable;
    }
}