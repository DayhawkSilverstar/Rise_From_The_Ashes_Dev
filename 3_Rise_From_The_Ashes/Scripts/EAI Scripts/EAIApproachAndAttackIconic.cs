using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;


    [Preserve]
public class EAIApproachAndAttackIconic : EAIBase
{
    private IconicZombie zombie; // Reference to the IconicZombie instance
    private float chaseTimeMax; // Maximum chase time

    // Struct to define target characteristics
    private struct TargetClass
    {
        public Type type; // Type of the target
        public float hearDistMax; // Maximum hearing distance
        public float seeDistMax; // Maximum seeing distance
        public float chaseTimeMax; // Maximum chase time
    }

    private const float cHearDistMax = 50f; // Constant for maximum hearing distance
    private List<TargetClass> targetClasses; // List of target classes

    public int AttackTimeout { get; private set; } // Property for attack timeout
    private int eatCount; // Counter for eating actions
    private int stopChasingTicks = 0; // Counter for stop chasing ticks

    // Constructor
    public EAIApproachAndAttackIconic() : base()
    {
#if DEBUG
        Log.Out("Created EAIApproachAndAttackIconic");
#endif
        // Call this so that way the underlining class does not throw a null reference exception.
        base.SetData(new DictionarySave<string, string>());
    }

    // Start method
    public override void Start()
    {
#if DEBUG
        Log.Out("EAIApproachAndAttackIconic : Start");
#endif
        zombie.IsEating = false; // Initialize eating state
        if (zombie.ChaseReturnLocation == Vector3.zero)
        {
            zombie.ChaseReturnLocation = (zombie.IsSleeper ? zombie.SleeperSpawnPosition : zombie.position);
        }
        AttackTimeout = 5; // Initialize attack timeout
    }

    // Initialization method
    public override void Init(EntityAlive _theEntity)
    {
#if DEBUG
        Log.Out("EAIApproachAndAttackIconic : Init");
#endif
        base.Init(_theEntity);
        zombie = _theEntity as IconicZombie; // Cast the entity to IconicZombie
        zombie.CanClimbLadders = false; // Disable ladder climbing
        zombie.blockedTime = 0;
    }

    // Method to check if the action can continue
    public override bool Continue()
    {
        if (zombie.sleepingOrWakingUp ||
            zombie.bodyDamage.CurrentStun != 0 ||
            zombie.Target == null)
        {
            return false;
        }

        if ((zombie.TargetAbove() | zombie.TargetBelow()) & zombie.TargetXZCheck())
        {
            return false;
        }

        return true;
    }

    // Update method
    public override void Update()
    {
#if DEBUG
        Log.Out("EAIApproachAndAttackIconic : Update");
#endif

        // if the target is null then return
        if (zombie.Target == null)
        {
            Log.Out("EAIApproachAndAttackIconic : Target is Null");
            return;
        }

        // Move towards the target if not in melee range
        if (!zombie.InMeleeRange(zombie.Target))
        {

#if DEBUG
            Log.Out("EAIApproachAndAttackIconic : Update - SetMoveTo");
            Log.Out("Target Position : " + zombie.Target.position.ToString());
#endif
            zombie.SetMoveTo(zombie.Target.position, true);

            zombie.CheckMovement();
            if (zombie.blockedTime > 0.3)
            {
                return;
            }
        }

        // See if the zombie can see the target. If not then stop.
        if (!zombie.CanEntityBeSeen(zombie.Target))
        {
#if DEBUG
            Log.Out("EAIApproachAndAttackIconic : Update - CanEntityBeSeen");
            Log.Out("Zombie cannot see the target");
#endif
            stopChasingTicks++;
            if (stopChasingTicks > 100)
            {
                stopChasingTicks = 0;
                Log.Out("EAIApproachAndAttackIconic : Update - Stop Chasing");
                zombie.Target = null;
                return;
            }
        }

        zombie.IsBreakingBlocks = false;
        zombie.IsBreakingDoors = false;

        // Rotate to the target position if the zombie has limbs and is not stunned
        if (zombie.bodyDamage.HasLimbs && zombie.bodyDamage.CurrentStun == 0)
        {
#if DEBUG
            Log.Out("EAIApproachAndAttackIconic : Update - Rotate towards Target");
#endif
            zombie.RotateTo(zombie.Target.position.x, zombie.Target.position.y, zombie.Target.position.z, 8f, 5f);
        }

        // If the target is dead then eat the target and return
        if (zombie.Target.IsDead() & zombie.GetDistanceSq(zombie.Target.position) < 1f)
        {
#if DEBUG
            Log.Out("EAIApproachAndAttackIconic : Update - Eat Target");
#endif
            EatTarget();
            return;
        }

        zombie.SleeperSupressLivingSounds = false;

        // Attack the target if within melee range
        if (!zombie.Target.IsDead() & zombie.InMeleeRange(zombie.Target))
        {
#if DEBUG
            Log.Out("EAIApproachAndAttackIconic : Update - Attack target :" + zombie.Target.GetDebugName());
#endif
            Attack();
        }
    }

    // Method to check if the action can be executed
    public override bool CanExecute()
    {
#if DEBUG
        Log.Out("EAIApproachAndAttackIconic : CanExecute");
#endif
        if (zombie.distraction != null)
        {
            return false;
        }

        // Check if the zombie can SEE a player
        zombie.FindTargetPlayer(zombie.GetSeeDistance());
        if (!zombie.Target)
        {
#if DEBUG
            Log.Out("EAIApproachAndAttackIconic : Can't Find Player");
#endif
            zombie.FindTargetLivingAnimal();
            if (zombie.Target)
            {
#if DEBUG
                Log.Out("EAIApproachAndAttackIconic : Found A living animal");
#endif
                zombie.IsBreakingBlocks = false;
                return true;
            }

#if DEBUG
            Log.Out("EAIApproachAndAttackIconic : No Living Animals");
#endif
            return false;
        }
#if DEBUG
        Log.Out("EAIApproachAndAttackIconic : Found Player : " + zombie.Target.ToString());
#endif

        return true;
    }

    // Method to handle the attack logic
    public void Attack()
    {
#if DEBUG
        Log.Out("EAIApproachAndAttackIconic - Attack : " + AttackTimeout.ToString());
#endif
        AttackTimeout--;
        if (AttackTimeout > 0)
        {
            return;
        }


        if (zombie.Target != null)
        {
            DoAttack();
        }
        else
        {
            if (zombie.IsAnimal(zombie.Target))
            {
                DoAttack();
            }
        }
    }

    // Method to perform the attack
    public void DoAttack()
    {
        zombie.SleeperSupressLivingSounds = false;
        if (zombie.Attack(_bAttackReleased: false))
        {
#if DEBUG
            Log.Out("EAISingleTask - Attack - Making an attack");
#endif
            zombie.Attack(_bAttackReleased: true);
            zombie.IsBreakingBlocks = false;
            AttackTimeout = zombie.GetAttackTimeoutTicks();
            return;
        }
    }

    // Method to set the target to only players
    public new void SetTargetOnlyPlayers()
    {
#if DEBUG
        Log.Out("EAIApproachAndAttackIconic - SetTargetOnlyPlayers");
#endif
        zombie.FindTargetPlayer(60f);
    }

    // Method to handle eating the target
    private void EatTarget()
    {
        Vector3 vector2 = zombie.position;
        vector2 = zombie.Target.getBellyPosition();
        if (zombie.IsEating == false)
        {
            // Log.Out("EAISingleTask - TryEating : setting to true");
            zombie.IsEating = true;
        }

        if (zombie.Target.IsDespawned)
        {
            zombie.Target = null;
            zombie.IsEating = false;
        }

        AttackTimeout--;
        if (zombie.IsEating)
        {
#if DEBUG
            Log.Out("EAIApproachAndAttackIconic - EatTarget : " + AttackTimeout.ToString());
#endif
            if (zombie.bodyDamage.HasLimbs)
            {
                zombie.RotateTo(vector2.x, vector2.y, vector2.z, 8f, 5f);
            }

            if (AttackTimeout <= 0)
            {
                AttackTimeout = 25 + UnityEngine.Random.Range(0, 10);
                if ((eatCount & 1) == 0)
                {
                    zombie.PlayOneShot("eat_player");
#if DEBUG
                    Log.Out(zombie.EntityName + " is eating " + zombie.Target.GetDebugName());
#endif
                    zombie.Target.DamageEntity(DamageSource.eat, 35, _criticalHit: false);
                }

                Vector3 pos = new Vector3(0f, 0.04f, 0.08f);
                ParticleEffect pe = new ParticleEffect("blood_eat", pos, 1f, Color.white, null, zombie.entityId, ParticleEffect.Attachment.Head);
                GameManager.Instance.SpawnParticleEffectServer(pe, zombie.entityId);
                eatCount++;
            }

            return;
        }
    }
}

