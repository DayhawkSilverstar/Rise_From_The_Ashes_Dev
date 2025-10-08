using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static ReflectionManager;


public class IconicZombie : EntityZombie
{
    public float ZombieReach = 2f;
    public Entity Target { get; set; }

    private static List<Entity> list = new List<Entity>();

    private EAISetNearestEntityAsTargetSorter sorter;

    private float closeTargetDist;

    private bool bNeedToSee = true;

    private Vector3 LastTargetPos;

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
#if DEBUG
        Log.Out("Iconic Zombie - SetMoveToPOS " + _pos.ToString());
#endif
        
        if (TargetAbove() | TargetBelow())
        {
            _pos = new Vector3(_pos.x, position.y, _pos.z);
#if DEBUG
            Log.Out("Iconic Zombie - SetMoveToPOS Adjusted" + _pos.ToString());
#endif
        }

        this.moveHelper.SetMoveTo(_pos, _canBreakBlocks);       
    }

    public void FindTargetPlayer(float seeDist)
    {
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

    public void FindTarget()
    {
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

                // Do not break here; continue to scan all candidates to find the closest
            }
        }

        // Clear the list
        list.Clear();
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

                // Do not break here; continue to scan all candidates to find the closest
            }
        }

        // Clear the list
        list.Clear();
    }

    public bool IsZombieAnimal(EntityAlive entityAnimal)
    {
        string entityName = entityAnimal.EntityName;
        // Log.Out("EAISingleTask : IsZombieAnimal - " +  entityName);
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
        if (this.Target != null)
        {
            if (Math.Abs(this.position.x - this.Target.position.x) < 1.5f & Math.Abs(this.position.z - this.Target.position.z) < 1.5f)
            {
                return true;
            }
        }
        return false;
    }

    public bool TargetAbove()
    {
        if (this.Target != null)
        {
            if (animalNames.Contains(Target.GetDebugName()))
            {
                return false;
            }
        }
        return this.Target != null && TargetXZCheck() && this.Target.position.y >= this.position.y;
    }

    public bool TargetBelow()
    {
        if (this.Target != null)
        {
            if (animalNames.Contains(Target.GetDebugName()))
            {
                return false;
            }
        }
        return this.Target != null && TargetXZCheck() && this.Target.position.y <= this.position.y;
    }
}
