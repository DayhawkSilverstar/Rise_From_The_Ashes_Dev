using Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static LightingAround;
using static ReflectionManager;


public class IconicZombie : EntityAlive
{

    // Structure to hold target class information
    private struct TargetClass
    {
        public Type type; // Type of the target entity
        public float hearDistMax; // Maximum hearing distance
        public float seeDistMax; // Maximum seeing distance
    }

    private List<TargetClass> targetClasses; // List of target classes
    private float senseSoundTime; // Time for sensing sound
    private int playerTargetClassIndex = -1; // Index of player target class in the list

    // The reach distance of the zombie for melee attacks
    public float ZombieReach = 2f;
    // The current target entity of the zombie
    public Entity Target { get; set; }

    // A static list to hold entities for various operations
    private static List<Entity> list = new List<Entity>();

    // A sorter to set the nearest entity as the target
    private EAISetNearestEntityAsTargetSorter sorter;

    // The distance to the closest target
    private float closeTargetDist;

    private Vector3 lastPosition;

    // A flag indicating whether the zombie needs to see the target
    private bool bNeedToSee = true;

    private float blocktime;

    // Override the method to determine if the zombie can jump
    public override bool CanEntityJump()
    {
        return false; // Zombies cannot jump
    }

    public float blockedTime
    {
        get
        {            
            return blocktime;
        }
        set
        {
            blocktime = value;
            SetCVar("blockedTime", blockedTime);
        }
    }

    public override void Init(int _entityClass)
    {
#if DEBUG       
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - Init");
#endif
        base.Init(_entityClass);
        constructEntityStats();
        switchModelView(EnumEntityModelView.ThirdPerson);
        InitPostCommon();
    }

    // Property to get or set whether the zombie can climb ladders
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

    // A set of animal names to identify animals in the game
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

    // Check if the target is within melee range
    public bool InMeleeRange(Entity target)
    {
        if (target == null)
        {
            return false; // Return false if there is no target
        }

        // Get the distance between this entity and the target
        float distance = GetDistance(target);

        // Subtract the target's collider bounds extents magnitude from the distance
        // This effectively reduces the distance by the size of the target
        if (target.GetComponent<Collider>() != null)
        {
            distance -= target.GetComponent<Collider>().bounds.extents.magnitude;
        }

#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - InMeleeRange Distance :" + distance.ToString());
        Log.Out("Iconic Zombie - InMeleeRange Reach :" + ZombieReach.ToString());
#endif

        // Check if the adjusted distance is less than the zombie's reach
        if (distance  < ZombieReach - 1.3f)
        {
#if DEBUG
            // Log the adjusted position for debugging purposes
            Log.Out("Iconic Zombie - InMeleeRange");
#endif
            return true; // Return true if the target is within melee range
        }

#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - Not In Range");
#endif
        return false; // Return false if the target is not within melee range
    }

    // Set the position to move to
    public void SetMoveTo(Vector3 _pos, bool _canBreakBlocks)
    {

#if DEBUG
        // Log the position for debugging purposes
        Log.Out("Iconic Zombie - SetMoveToPOS " + _pos.ToString());
#endif

        // Adjust the position if the target is above or below
        if (TargetAbove() | TargetBelow())
        {
            _pos = new Vector3(_pos.x, position.y, _pos.z);
#if DEBUG
            // Log the adjusted position for debugging purposes
            Log.Out("Iconic Zombie - SetMoveToPOS Adjusted" + _pos.ToString());
#endif
        }

        lastPosition = this.position;

        // Set the move position and whether the zombie can break blocks
        this.moveHelper.SetMoveTo(_pos, _canBreakBlocks);
    }

    public void CheckMovement()
    {
#if DEBUG
        // Log the position for debugging purposes
        Log.Out("Iconic Zombie - Vector Distance " + Vector3.Distance(this.position, lastPosition).ToString());
#endif
        if (Vector3.Distance(this.position, lastPosition) < 0.05)
        {
            blockedTime += Time.deltaTime;
        }
        else
        {
            blockedTime = 0;
        }
    }

    public List<Entity> GetEntities()
    {
        float seeDist = 10;
        // Get all players within the see distance
        return world.GetEntitiesInBounds(typeof(EntityPlayer), BoundsUtils.ExpandBounds(boundingBox, seeDist, seeDist, seeDist), list);
    }

    public void FindTargetPlayer(float seeDist)
    {
#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - FindTargetPlayer");
#endif
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
            if (noisePlayer != null && noisePlayerVolume >= noiseWake)
            {
                // Set the closest player and distance to the noise player and distance
                closestPlayer = noisePlayer;
                closestDistance = noisePlayerDistance;
            }
            // If the volume is above the groan threshold
            else if (noisePlayerVolume >= noiseGroan)
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

    public void FindTarget()
    {
#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - FindTarget");
#endif
        // Initialize the closest target distance to the maximum possible value
        closeTargetDist = float.MaxValue;

        // Get the see distance of the zombie
        float seeDistance = GetSeeDistance();

        // Get all players within a 30x4x30 box centered on the zombie
        world.GetEntitiesInBounds(typeof(EntityPlayer), BoundsUtils.ExpandBounds(boundingBox, 30f, 4f, 30f), list);

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
                }

                // Break the loop
                break;
            }
        }

        // Clear the list
        list.Clear();
    }

    protected bool check(EntityAlive entity)
    {
#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - check");
#endif
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
#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - FindTargetLivingAnimal");
#endif
        // Initialize the closest target distance to the maximum possible value
        closeTargetDist = float.MaxValue;

        // Get all animals within a 30x4x30 box centered on the zombie
        world.GetEntitiesInBounds(typeof(EntityAnimal), BoundsUtils.ExpandBounds(boundingBox, 30f, 4f, 30f), list);

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
                }

                // Break the loop
                break;
            }
        }

        // Clear the list
        list.Clear();
    }

    // Check if the given entity is a zombie animal
    public bool IsZombieAnimal(EntityAlive entityAnimal)
    {
#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - IsZombieAnimal");
#endif
        string entityName = entityAnimal.EntityName;
        // Log the entity name for debugging purposes
        // Log.Out("EAISingleTask : IsZombieAnimal - " +  entityName);
        switch (entityName)
        {
            case "animalZombieDog":
            case "animalZombieBear":
                {
                    return true; // Return true if the entity is a zombie dog or bear
                }
        }

        return false; // Return false if the entity is not a zombie animal
    }

    // Check if the given entity is an animal
    public bool IsAnimal(Entity entity)
    {
#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - IsAnimal");
#endif
        string entityName = entity.GetDebugName();
        // Check if the entity name is in the list of animal names
        if (animalNames.Contains(entityName))
        {
            return true; // Return true if the entity is an animal
        }

        return false; // Return false if the entity is not an animal
    }

    // Check if the target is within a certain distance in the X and Z axes
    public bool TargetXZCheck()
    {
#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - TargetXZCheck");
#endif
        if (this.Target != null)
        {
            // Check if the target is within 1.5 units in the X and Z axes
            if (Math.Abs(this.position.x - this.Target.position.x) < 1.5f & Math.Abs(this.position.z - this.Target.position.z) < 1.5f)
            {
                return true; // Return true if the target is within the specified distance
            }
        }
        return false; // Return false if there is no target or the target is not within the specified distance
    }

    // Check if the target is above the current entity
    public bool TargetAbove()
    {
#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - TargetAbove Check");
#endif
        if (this.Target != null)
        {
            // Return false if the target is an animal
            if (animalNames.Contains(Target.GetDebugName()))
            {
                return false;
            }
        }
        // Return true if the target is above the current entity and within the specified distance in the X and Z axes
        if (this.Target != null)
        {
            if (TargetXZCheck() && this.Target.position.y - 0.5f > this.position.y)
            {
#if DEBUG
                // Log the adjusted position for debugging purposes
                Log.Out("Iconic Zombie - Target pos Y :" + this.Target.position.y);
                Log.Out("Iconic Zombie - Zombie pos Y :" + this.position.y);
#endif
                return true;
            }
        }

        return false;
    }

    // Check if the target is below the current entity
    public bool TargetBelow()
    {
#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - TargetBelow Check");
#endif
        if (this.Target != null)
        {
            // Return false if the target is an animal
            if (animalNames.Contains(Target.GetDebugName()))
            {
                return false;
            }
        }
        // Return true if the target is below the current entity and within the specified distance in the X and Z axes
        return this.Target != null && TargetXZCheck() && this.Target.position.y <= this.position.y;
    }

    public void AttackBlock()
    {
#if DEBUG
        // Log the adjusted position for debugging purposes
        Log.Out("Iconic Zombie - Attacking Block");
#endif
        Attack(true);
    }

    // Seek the noise made by a player
    public void SeekNoise(EntityPlayer player)
    {
        float magnitude = (player.position - this.position).magnitude;
        if (playerTargetClassIndex >= 0)
        {
            float hearDistMax = targetClasses[playerTargetClassIndex].hearDistMax;
            hearDistMax *= this.senseScale;
            hearDistMax *= player.DetectUsScale(this);
            if (magnitude > hearDistMax)
            {
                return;
            }
        }

        magnitude *= 0.9f;
        if (magnitude > aiManager.noiseSeekDist)
        {
            magnitude = aiManager.noiseSeekDist;
        }

        if (this.IsBloodMoon)
        {
            magnitude = aiManager.noiseSeekDist * 0.25f;
        }

        Vector3 breadcrumbPos = player.GetBreadcrumbPos(magnitude * aiManager.random.RandomFloat);
        int ticks = this.CalcInvestigateTicks((int)(30f + aiManager.random.RandomFloat * 30f) * 20, player);
        this.SetInvestigatePosition(breadcrumbPos, ticks);
        float time = Time.time;
        if (senseSoundTime - time < 0f)
        {
            senseSoundTime = time + 10f + aiManager.random.RandomFloat * 10f;
            this.PlayOneShot(this.soundSense);
        }
    }
}


