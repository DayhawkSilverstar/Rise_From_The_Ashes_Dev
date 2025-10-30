using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AI;
using static ReflectionManager;


public class IconicZombie : EntityZombie
{
    public float ZombieReach = 2f;
    
    private Entity targetField;
    
    /// <summary>
    /// Target property with automatic cleanup when target becomes invalid
    /// </summary>
    public Entity Target 
    { 
        get => targetField;
        set 
        {
            targetField = value;
            
            // Automatically clear both our target and attack target if the new value is invalid
            if (targetField != null && (targetField.IsDead() || targetField.IsMarkedForUnload()))
            {
                targetField = null;
                SetAttackTarget(null, 0);
            }
        }
    }

    private static List<Entity> list = new List<Entity>();

    private EAISetNearestEntityAsTargetSorter sorter;

    private float closeTargetDist;

    private bool bNeedToSee = true;

    private Vector3 LastTargetPos;

    // Speed arrays for different movement states
    private static float[] moveSpeeds = new float[5] { 0f, 0.35f, 0.7f, 1f, 1.35f };
    private static float[] moveRageSpeeds = new float[5] { 0.75f, 0.8f, 0.9f, 1.15f, 1.7f };
    private static float[] moveSuperRageSpeeds = new float[5] { 0.88f, 0.92f, 1f, 1.2f, 1.7f };

    public IconicZombie() : base()
    {
        
    }

    public override bool CanEntityJump()
    {        
        return false;
    }    

    public bool CanClimbLadders
    {
        get
        {
            return bCanClimbLadders;
        }
        set
        {
            bCanClimbLadders = value;
        }
    }

    HashSet<string> animalNames = new HashSet<string>
        {
            "animalChicken",
            "animalRabbit",
            "animalStag",
            "animalDoe",
            "animalBear",
            "animalWolf",
            "animalDireWolf",
            "animalCoyote",
            "animalMountainLion",
            "animalSnake",
            "animalBoar"
        };

    /// <summary>
    /// Override to prevent IconicZombies from being saved when spawned dynamically.
    /// This ensures they can be properly despawned when no players are online.
    /// </summary>
    public override bool IsSavedToFile()
    {
        // If spawned dynamically (via MinEventAction), don't save to file
        if (GetSpawnerSource() == EnumSpawnerSource.Dynamic)
        {
            return false;
        }
        
        // Otherwise use default behavior
        return base.IsSavedToFile();
    }

    /// <summary>
    /// Override to allow IconicZombies to despawn when no players are on the server.
    /// This prevents memory leaks when all players log off.
    /// </summary>
    public override bool canDespawn()
    {
        // If spawned dynamically, allow despawn when no players online
        if (GetSpawnerSource() == EnumSpawnerSource.Dynamic)
        {
            // If no players online, allow despawn immediately
            if (world.GetPlayers().Count == 0)
            {
                return true;
            }
            
            // If dead, allow normal despawn
            if (IsDead())
            {
                return true;
            }
        }
        
        // Otherwise use default behavior
        return base.canDespawn();
    }

    /// <summary>
    /// Override GetMoveSpeed to handle bloodmoon speed for walking
    /// </summary>
    public override float GetMoveSpeed()
    {
        if (IsBloodMoon)
        {
            // During bloodmoon, use the night walk speed
            return EffectManager.GetValue(PassiveEffects.WalkSpeed, null, moveSpeedNight, this);
        }

        if (IsAlert)
        {
            return GetMoveSpeedAggro() * 0.65f;
        }

        if (world.IsDark())
        {
            return EffectManager.GetValue(PassiveEffects.WalkSpeed, null, moveSpeedNight, this);
        }

        // Default walking speed
        return EffectManager.GetValue(PassiveEffects.CrouchSpeed, null, moveSpeed, this);
    }

    /// <summary>
    /// Override GetMoveSpeedAggro to handle bloodmoon and other aggressive movement speeds
    /// </summary>
    public override float GetMoveSpeedAggro()
    {
        // Determine which game preference to use based on current state
        EnumGamePrefs eProperty = EnumGamePrefs.ZombieMove;
        
        if (IsBloodMoon)
        {
            // Use bloodmoon speed setting
            eProperty = EnumGamePrefs.ZombieBMMove;
        }
        else if (IsFeral)
        {
            // Use feral speed setting
            eProperty = EnumGamePrefs.ZombieFeralMove;
        }
        else if (world.IsDark())
        {
            // Use night speed setting
            eProperty = EnumGamePrefs.ZombieMoveNight;
        }

        // Get the speed index from game preferences (0-4)
        int speedIndex = GamePrefs.GetInt(eProperty);
        
        // Get base speed from the speed array
        float speed = moveSpeeds[speedIndex];
        
        // Apply rage speed modifications if in rage mode
        // (This would require tracking rage state - currently not implemented)
        // For now, just use base speed
        
        // Scale between moveSpeedAggro and moveSpeedAggroMax based on speed index
        speed = (speed < 1f) 
            ? (moveSpeedAggro * (1f - speed) + moveSpeedAggroMax * speed)
            : (moveSpeedAggroMax * speed);
        
        // Apply passive effects
        return EffectManager.GetValue(PassiveEffects.RunSpeed, null, speed, this);
    }

    /// <summary>
    /// Override OnUpdateLive to ensure targets are always valid
    /// This prevents cycling issues where dead targets are kept
    /// </summary>
    public override void OnUpdateLive()
    {
        // Check and clear invalid targets BEFORE base update
        if (Target != null && (Target.IsDead() || Target.IsMarkedForUnload()))
        {
            Target = null;
            SetAttackTarget(null, 0);
        }
        
        // Also ensure attack target matches our target
        var attackTarget = GetAttackTarget();
        if (attackTarget != null && (attackTarget.IsDead() || attackTarget.IsMarkedForUnload()))
        {
            SetAttackTarget(null, 0);
        }
        
        base.OnUpdateLive();
    }

    public bool InMeleeRange(Entity target)
    {
        if (target == null)
        {
            return false;
        }

        // Get the distance between this entity and the target
        float distance = GetDistance(target);

        // Subtract the target's collider bounds extents magnitude from the distance
        // This effectively reduces the distance by the size of the target
        if (target.GetComponent<Collider>() != null)
        {
            distance -= target.GetComponent<Collider>().bounds.extents.magnitude;
        }

        // Check if the adjusted distance is less than the zombie's reach
        if (distance < ZombieReach)
        {
            return true;
        }

        return false;
    }

    public void SetMoveTo(Vector3 _pos, bool _canBreakBlocks)
    {
        bool targetAbove = TargetAbove();
        bool targetBelow = TargetBelow();

        
        if (targetAbove | targetBelow)
        {
            Vector3 originalPos = _pos;
            _pos = new Vector3(_pos.x, position.y, _pos.z);

        }

        this.moveHelper.SetMoveTo(_pos, _canBreakBlocks);
    }

    public void FindTargetPlayer(float seeDist)
    {
        seeDist *= 2;        

        // If it's a blood moon, increase the see distance
        if (IsBloodMoon)
        {
            seeDist *= 10;
        }

        // If the zombie is a passive sleeper, return
        if (IsSleeperPassive)
        {
            return;
        }

        // Get all players within the see distance
        world.GetEntitiesInBounds(typeof(EntityPlayer), BoundsUtils.ExpandBounds(boundingBox, seeDist, seeDist, seeDist), list);

        // If the zombie is sleeping and not a decoy
        if (IsSleeping)
        {
            // Sort the players by distance
            list.Sort(sorter);

            // Initialize the closest player and distance
            EntityPlayer closestPlayer = null;
            float closestDistance = float.MaxValue;

            // Initialize the flag for whether the zombie should groan
            bool shouldGroan = false;

            // If there's a noise player and the volume is above the wake threshold
            if (noisePlayer != null && noisePlayerVolume >= sleeperNoiseToWake)
            {
                // Set the closest player and distance to the noise player and distance
                closestPlayer = noisePlayer;
                closestDistance = noisePlayerDistance;
            }
            // If the volume is above the groan threshold
            else if (noisePlayerVolume >= sleeperNoiseToSense)
            {
                // Set the flag for whether the zombie should groan
                shouldGroan = true;
            }

            // For each player in the list
            foreach (EntityPlayer player in list)
            {
                // If the zombie can see the player and the player is not ignored by AI
                if (CanSee(player) && !player.IsIgnoredByAI())
                {
                    // Calculate the distance to the player and the sleeper disturbed level
                    float distance = GetDistance(player);
                    int sleeperDisturbedLevel = GetSleeperDisturbedLevel(distance, player.Stealth.lightLevel);

                    // If the sleeper disturbed level is at least 2 and the distance is less than the closest distance
                    if (sleeperDisturbedLevel >= 2 && distance < closestDistance)
                    {
                        // Set the closest player and distance to the player and distance
                        closestPlayer = player;
                        closestDistance = distance;
                    }
                    // If the sleeper disturbed level is at least 1
                    else if (sleeperDisturbedLevel >= 1)
                    {
                        // Set the flag for whether the zombie should groan
                        shouldGroan = true;
                    }
                }
            }

            // Clear the list
            list.Clear();

            // If there's a closest player
            if (closestPlayer != null)
            {
                // Set the close target distance and target to the closest distance and player
                closeTargetDist = closestDistance;
                Target = closestPlayer;
            }
            // If the zombie should groan
            else if (shouldGroan)
            {
                // Make the zombie groan
                Groan();
            }
            else
            {
                // Make the zombie snore
                Snore();
            }

            return;
        }

        // For each player in the list
        foreach (EntityPlayer player in list)
        {
            // If the player is alive and not ignored by AI
            if (player.IsAlive() && !player.IsIgnoredByAI())
            {
                // Calculate the distance to the player
                float distance = GetDistance(player);

                // If it's a blood moon or the zombie can see the player and the player is not stealthy and the distance is less than the close target distance
                if ((IsBloodMoon || CanSee(player) && CanSeeStealth(distance, player.Stealth.lightLevel)) && distance < closeTargetDist)
                {
                    // Set the close target distance and target to the distance and player
                    closeTargetDist = distance;
                    Target = player;
                }
            }
        }

        // Clear the list
        list.Clear();
    }

    public override float GetSeeDistance()
    {
        senseScale = 3f;
        if (IsSleeping)
        {
            sightRange = sleeperSightRange;
            return sleeperSightRange;
        }

        sightRange = sightRangeBase;
        
        float num = EAIManager.CalcSenseScale();
        senseScale = 1f + num * 10;
        sightRange = sightRangeBase * senseScale;        

        return sightRange;
    }

    public void FindTarget()
    {
        // Initialize the closest target distance to the maximum possible value
        closeTargetDist = float.MaxValue;

        // Get the see distance of the zombie
        float seeDistance = GetSeeDistance();

        // Get all players within a 30x4x30 box centered on the zombie
        world.GetEntitiesInBounds(typeof(EntityPlayer), BoundsUtils.ExpandBounds(boundingBox, 30f, 4f, 30f), list);

        // Track if we found any valid target
        bool foundValidTarget = false;

        // For each player in the list
        foreach (EntityAlive entityAlive in list)
        {
            // If the player is not a drone and passes the check
            if (!(entityAlive is EntityDrone) && check(entityAlive))
            {
                // Add the player to the see cache
                SetCanSee(entityAlive);

                // Calculate the distance to the player
                float distance = GetDistance(entityAlive);

                // If the distance is less than the closest target distance
                if (distance < closeTargetDist)
                {
                    // Set the closest target distance, target, and last target position to the distance, player, and player's position
                    closeTargetDist = distance;
                    Target = entityAlive;
                    LastTargetPos = entityAlive.position;
                    foundValidTarget = true;
                }

                // Do not break here; continue to scan all candidates to find the closest
            }
        }

        // Clear the list
        list.Clear();
        
        // If we didn't find a valid target, clear our target
        if (!foundValidTarget)
        {
            Target = null;
        }
    }

    protected bool check(EntityAlive entity)
    {
        // If the entity is null, this entity, not alive, or ignored by AI, return false
        if (entity == null || entity == this || !entity.IsAlive() || entity.IsIgnoredByAI())
        {
            return false;
        }

        // Convert the entity's position from world coordinates to block coordinates
        Vector3i entityPosition = World.worldToBlockPos(entity.position);

        // If the entity is not within the home distance, return false
        if (!isWithinHomeDistance(entityPosition.x, entityPosition.y, entityPosition.z))
        {
            return false;
        }

        // If this entity needs to see the other entity and can't see it, return false
        if (bNeedToSee && !CanSee(entity))
        {
            return false;
        }

        // If the entity is a player and this entity can't see it due to stealth, return false
        if (entity is EntityPlayer player && !CanSeeStealth(GetDistance(player), player.Stealth.lightLevel))
        {
            return false;
        }

        // If none of the conditions are met, return true
        return true;
    }

    public void FindTargetLivingAnimal()
    {
        // Initialize the closest target distance to the maximum possible value
        closeTargetDist = float.MaxValue;

        // Get all animals within a 30x4x30 box centered on the zombie
        world.GetEntitiesInBounds(typeof(EntityAnimal), BoundsUtils.ExpandBounds(boundingBox, 30f, 4f, 30f), list);

        // Track if we found any valid target
        bool foundValidTarget = false;

        // For each animal in the list
        foreach (EntityAlive entityAlive in list)
        {
            // If the animal is not a zombie animal, not a drone, and passes the check
            if (!IsZombieAnimal(entityAlive) && !(entityAlive is EntityDrone) && check(entityAlive))
            {
                // Calculate the distance to the animal
                float distance = GetDistance(entityAlive);

                // If the distance is less than the closest target distance
                if (distance < closeTargetDist)
                {
                    // Set the closest target distance, target, and last target position to the distance, animal, and animal's position
                    closeTargetDist = distance;
                    Target = entityAlive;
                    LastTargetPos = entityAlive.position;
                    foundValidTarget = true;
                }

                // Do not break here; continue to scan all candidates to find the closest
            }
        }

        // Clear the list
        list.Clear();
        
        // If we didn't find a valid target, clear our target
        if (!foundValidTarget)
        {
            Target = null;
        }
    }

    public bool IsZombieAnimal(EntityAlive entityAnimal)
    {
        string entityName = entityAnimal.EntityName;
        switch (entityName)
        {
            case "animalZombieDog":
            case "animalZombieBear":
                {
                    return true;
                }
        }

        return false;
    }

    public bool IsAnimal(Entity entity)
    {
       string entityName = entity.GetDebugName();
       if (animalNames.Contains(entityName))
        {
            return true;
        }

        return false;
    }

    public bool TargetXZCheck()
    {
        if (this.Target != null && this.Target.IsAlive() && !this.Target.IsMarkedForUnload())
        {
            float xDiff = Math.Abs(this.position.x - this.Target.position.x);
            float zDiff = Math.Abs(this.position.z - this.Target.position.z);
            bool result = xDiff < 1.5f && zDiff < 1.5f;

            
            return result;
        }

        return false;
    }

    public bool TargetAbove()
    {
        if (this.Target != null && this.Target.IsAlive() && !this.Target.IsMarkedForUnload())
        {
            if (animalNames.Contains(Target.GetDebugName()))
            {
                return false;
            }
            
            bool xzCheck = TargetXZCheck();
            bool yCheck = this.Target.position.y >= this.position.y;
            bool result = xzCheck && yCheck;
            
            float yDiff = this.Target.position.y - this.position.y;
            
            return result;
        }
        
        return false;
    }

    public bool TargetBelow()
    {
        if (this.Target != null && this.Target.IsAlive() && !this.Target.IsMarkedForUnload())
        {
            if (animalNames.Contains(Target.GetDebugName()))
            {
                return false;
            }
            
            bool xzCheck = TargetXZCheck();
            bool yCheck = this.Target.position.y <= this.position.y;
            bool result = xzCheck && yCheck;
            
            float yDiff = this.position.y - this.Target.position.y;
            
            return false;
        }
        
        return false;
    }
}
