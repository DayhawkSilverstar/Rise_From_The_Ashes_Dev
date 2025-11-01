using System;
using System.Xml;
using System.Xml.Linq;
using UnityEngine;
using UnityEngine.Scripting;

// Client-only variant: spawns entities locally on the client without networking
[Preserve]
public class MinEventActionSpawnEntityAroundPlayerClientOnly : MinEventActionRemoveBuff
{
    private string strCvar;
    private string strSpawnGroup = "";

    public override void Execute(MinEventParams _params)
    {
        Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: DoSpawn called");
        var world = GameManager.Instance.World;
        if (world == null)
        {
            Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: World Null");
            return;
        }

        // CRITICAL: Only spawn on client, never on server/dedicated
        // Hallucination zombies should only exist locally
        if (world.IsRemote())
        {
            Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: Skipping - running on dedicated server");
            return;
        }

        // For single-player or client in multiplayer, ensure we're the local player
        var localPlayer = world.GetPrimaryPlayer();
        if (localPlayer == null)
        {
            Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: No local player found");
            return;
        }

        // Anchor: prefer the AI holder's player target; otherwise closest player at params position
        EntityPlayer anchorPlayer = null;
        if (_params.Self is EntityAlive selfAlive)
        {
            anchorPlayer = selfAlive.GetAttackTarget() as EntityPlayer;
            if (anchorPlayer == null)
            {
                anchorPlayer = world.GetClosestPlayer(selfAlive.position, 200f, false);
            }
        }

        // Fall back to player near the event position
        if (anchorPlayer == null)
        {
            anchorPlayer = world.GetClosestPlayer(_params.Position, 200f, false);
        }

        // If no player anchor could be found, use original behavior's position
        Vector3 originPos = anchorPlayer != null ? anchorPlayer.position : _params.Position;

        // Compute spawn position: 50m in a random horizontal direction from the anchor
        float angleRad = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
        Vector3 dirXZ = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));
        Vector3 targetXZ = originPos + dirXZ * 50f;

        // Snap Y to ground and nudge up
        float groundY = world.GetHeightAt(targetXZ.x, targetXZ.z);
        Vector3 spawnPos = new Vector3(targetXZ.x, groundY + 1f, targetXZ.z);
        
        // VALIDATION: Ensure spawn position is safe
        // Check if position is inside solid block
        BlockValue blockAtSpawn = world.GetBlock(World.worldToBlockPos(spawnPos));
        if (blockAtSpawn.Block.IsCollideMovement)
        {
            // Try higher up
            spawnPos.y = groundY + 3f;
            Log.Warning($"MinEventActionSpawnEntityAroundPlayerClientOnly: Spawn pos was inside solid block, moved up to Y={spawnPos.y}");
        }

        Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: Get Spawn Group : " + strSpawnGroup);
        // Decide what to spawn from group
        int entityClassId = -1;
        if (!string.IsNullOrEmpty(strSpawnGroup))
        {
            int classIdTmp = 0;
            entityClassId = EntityGroups.GetRandomFromGroup(strSpawnGroup, ref classIdTmp);
        }
        if (entityClassId == -1)
        {
            Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: Entity Class ID -1");
            return;
        }

        // Face the anchor player if we have one
        float yaw = 0f;
        if (anchorPlayer != null)
        {
            Vector3 toPlayer = anchorPlayer.position - spawnPos;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude > 0.0001f)
            {
                yaw = Mathf.Atan2(toPlayer.x, toPlayer.z) * Mathf.Rad2Deg;
            }
        }

        var newEntity = EntityFactory.CreateEntity(entityClassId, spawnPos, new Vector3(0f, yaw, 0f)) as EntityAlive;
        if (newEntity == null)
        {
            Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: New Entity Null");
            return;
        }

        // CRITICAL FIX: Assign a client-local entity ID to prevent collision
        // Negative IDs are reserved for client-only entities
        // Use Math.Abs to ensure we always get a negative result
        int clientEntityId = -Math.Abs(EntityFactory.nextEntityID);
        EntityFactory.nextEntityID++;
        newEntity.entityId = clientEntityId;

        newEntity.SetSpawnerSource(EnumSpawnerSource.Dynamic);

        // CLIENT-LOCAL SPAWN: Add directly to world's entity dictionary without networking
        // This bypasses NetPackageEntitySpawn and prevents ID collisions
        try
        {
            // CRITICAL: Set the world reference before adding to dictionary
            // This is normally done by SpawnEntityInWorld but we're bypassing that
            newEntity.world = world;
            
            // Set the entity as client-controlled (like drones)
            newEntity.isEntityRemote = false;
            
            world.Entities.dict.Add(newEntity.entityId, newEntity);
            
            Log.Out($"MinEventActionSpawnEntityAroundPlayerClientOnly: Added to world dict. Entity ID: {newEntity.entityId}, Position: {spawnPos}");
            
            // Check for nulls before Init
            if (newEntity.gameObject == null)
            {
                Log.Error("MinEventActionSpawnEntityAroundPlayerClientOnly: newEntity.gameObject is NULL before Init!");
            }
            
            // DON'T call Init() - it expects networking setup we don't have
            // Instead, manually do the essentials
            try
            {
                // Call PostInit instead which doesn't require full networking
                newEntity.PostInit();
                Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: PostInit() completed successfully");
                
                // Manually spawn the entity in the world (no networking)
                newEntity.OnAddedToWorld();
                Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: OnAddedToWorld() completed");
                
                // CRITICAL PHYSICS FIX: Enable gravity and physics
                // This is what makes the entity fall to the ground
                if (newEntity.emodel != null && newEntity.emodel.avatarController != null)
                {
                    // Enable physics on the avatar controller
                    var avatarController = newEntity.emodel.avatarController;
                    
                    // Set onGround to false so the entity will fall
                    newEntity.onGround = false;
                    Log.Out($"MinEventActionSpawnEntityAroundPlayerClientOnly: Set onGround=false to enable falling");
                    
                    // Enable physics simulation via CharacterController (not Rigidbody for AI entities)
                    if (avatarController.transform != null)
                    {
                        var characterController = avatarController.GetComponent<CharacterController>();
                        if (characterController != null)
                        {
                            characterController.enabled = true;
                            Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: Enabled CharacterController for physics");
                        }
                        else
                        {
                            // Fallback to Rigidbody if available
                            var rigidbody = avatarController.GetComponent<Rigidbody>();
                            if (rigidbody != null)
                            {
                                rigidbody.isKinematic = false; // Enable physics
                                rigidbody.useGravity = true;   // Enable gravity
                                Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: Enabled Rigidbody physics and gravity");
                            }
                            else
                            {
                                Log.Warning("MinEventActionSpawnEntityAroundPlayerClientOnly: No CharacterController or Rigidbody found");
                            }
                        }
                    }
                }
                else
                {
                    Log.Warning("MinEventActionSpawnEntityAroundPlayerClientOnly: emodel or avatarController is null");
                }
                
                // Force the entity to spawn as active and alert
                // SetInvestigatePosition will wake the entity if sleeping
                newEntity.SetInvestigatePosition(anchorPlayer != null ? anchorPlayer.position : spawnPos, 600);
                
                // Trigger wake up animation if entity is sleeping
                newEntity.ConditionalTriggerSleeperWakeUp();
                
                Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: Set entity to active/alert state");
                
            }
            catch (System.Exception initEx)
            {
                Log.Error($"MinEventActionSpawnEntityAroundPlayerClientOnly: Init() failed - {initEx.GetType().Name}: {initEx.Message}");
                Log.Error($"MinEventActionSpawnEntityAroundPlayerClientOnly: Stack trace: {initEx.StackTrace}");
                throw; // Re-throw to be caught by outer catch
            }
            
            Log.Out($"MinEventActionSpawnEntityAroundPlayerClientOnly: After Init() - Health: {newEntity.Health}/{newEntity.GetMaxHealth()}, IsDead: {newEntity.IsDead()}, OnGround: {newEntity.onGround}");
            
            // Spawn the game object in the scene
            if (newEntity.gameObject == null)
            {
                Log.Error("MinEventActionSpawnEntityAroundPlayerClientOnly: newEntity.gameObject is NULL after Init!");
            }
            else
            {
                try
                {
                    newEntity.gameObject.SetActive(true);
                    Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: GameObject.SetActive(true) completed");
                }
                catch (System.Exception activateEx)
                {
                    Log.Error($"MinEventActionSpawnEntityAroundPlayerClientOnly: SetActive() failed - {activateEx.GetType().Name}: {activateEx.Message}");
                    throw;
                }
            }
            
            Log.Out($"MinEventActionSpawnEntityAroundPlayerClientOnly: Spawned CLIENT-LOCAL Entity ID {newEntity.entityId} (classId: {entityClassId}) at {spawnPos}");
        }
        catch (System.Exception ex)
        {
            Log.Error($"MinEventActionSpawnEntityAroundPlayerClientOnly: Failed to spawn entity - {ex.Message}");
            Log.Error($"MinEventActionSpawnEntityAroundPlayerClientOnly: Exception type: {ex.GetType().Name}");
            Log.Error($"MinEventActionSpawnEntityAroundPlayerClientOnly: Stack trace: {ex.StackTrace}");
            
            // Clean up on failure
            try
            {
                if (newEntity != null && world.Entities.dict.ContainsKey(newEntity.entityId))
                {
                    world.Entities.dict.Remove(newEntity.entityId);
                    Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: Removed failed entity from world dict");
                }
                
                if (newEntity != null && newEntity.gameObject != null)
                {
                    UnityEngine.Object.Destroy(newEntity.gameObject);
                    Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: Destroyed failed entity GameObject");
                }
            }
            catch (System.Exception cleanupEx)
            {
                Log.Error($"MinEventActionSpawnEntityAroundPlayerClientOnly: Cleanup failed - {cleanupEx.Message}");
            }
            
            return;
        }

        if (anchorPlayer != null)
        {
            Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: Set Attack Target to Anchor Player");
            newEntity.SetAttackTarget(anchorPlayer, 600);
        }
    }

    public override bool ParseXmlAttribute(XAttribute _attribute)
    {
        var flag = base.ParseXmlAttribute(_attribute);
        if (!flag)
        {
            var name = _attribute.Name.LocalName;
            if (name != null)
            {
                if (name == "SpawnGroup")
                {
                    strSpawnGroup = _attribute.Value;
                    return true;
                }

                if (name == "Cvar")
                {
                    strCvar = _attribute.Value;
                    return true;
                }
            }
        }

        return flag;
    }
}
