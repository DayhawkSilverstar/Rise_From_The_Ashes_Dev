using UnityEngine;
using UnityEngine.Scripting;

// Client-side safe spawn action: mirrors networked flow but only spawns locally on the client
[Preserve]
public class ItemActionSpawnEntityClientOnly : ItemActionSpawnEntityNetworked
{
    protected override void DoSpawn(ItemActionData _actionData)
    {
        EntityAlive holdingEntity = _actionData.invData.holdingEntity;
        if (!holdingEntity || !holdingEntity.IsAttackValid())
            return;

        var data = (ItemActionDataSpawnEntity)_actionData;

        // Use the anchor player's location if provided; otherwise fall back to the holder's position
        Vector3 originPos = (data.anchorPlayer != null) ? data.anchorPlayer.position : holdingEntity.position;

        World world = GameManager.Instance.World;
        if (world == null)
            return;

        // Prefer engine helper to get a valid spawn 50m from origin; fallback to manual 50m ground snap
        Vector3 spawnPos = originPos;
        bool gotValid = world.GetRandomSpawnPositionMinMaxToPosition(
            originPos,
            50,         // minRange
            50,         // maxRange (same to try exact distance)
            0,          // minPlayerRange
            false,      // checkBedrolls
            out spawnPos,
            data.anchorPlayer != null ? data.anchorPlayer.entityId : -1,
            true,       // checkWater
            8,          // retryCount
            true,       // checkLandClaim
            EnumLandClaimOwner.None,
            false       // useSquareRadius
        );

        if (!gotValid)
        {
            float angleRad = UnityEngine.Random.Range(0f, Mathf.PI * 2f);
            Vector3 dirXZ = new Vector3(Mathf.Cos(angleRad), 0f, Mathf.Sin(angleRad));
            Vector3 targetXZ = originPos + dirXZ * 50f;
            float groundY = world.GetHeightAt(targetXZ.x, targetXZ.z);
            spawnPos = new Vector3(targetXZ.x, groundY + 1f, targetXZ.z);
        }

        // Determine intended target: if we have an anchor player, use that; else use holder's current attack target or holder (if player)
        EntityAlive intendedTarget = null;
        if (data.anchorPlayer != null)
        {
            intendedTarget = data.anchorPlayer;
        }
        else if (holdingEntity is EntityPlayer playerHolder)
        {
            intendedTarget = playerHolder;
        }
        else
        {
            intendedTarget = holdingEntity.GetAttackTarget();
        }

        // Calculate yaw so the spawned entity faces the intended target
        float yaw = holdingEntity.rotation.y;
        if (intendedTarget != null)
        {
            Vector3 toTarget = intendedTarget.position - spawnPos;
            toTarget.y = 0f;
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                yaw = Mathf.Atan2(toTarget.x, toTarget.z) * Mathf.Rad2Deg;
            }
        }

        int classId = EntityClass.GetId(entityToSpawn);
        if (classId < 0)
            return;

        // Create the entity with intended position and rotation (face the intended target)
        Entity newEntity = EntityFactory.CreateEntity(classId, spawnPos, new Vector3(0f, yaw, 0f));
        if (newEntity == null)
            return;

        newEntity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner);

        // Client-only path: do nothing if executing on server, spawn locally on client without networking
        bool isServer = SingletonMonoBehaviour<ConnectionManager>.Instance != null && SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        if (isServer)
        {
            // Do not spawn on server to avoid network propagation
            return;
        }

        // Spawn locally on the client
        world.SpawnEntityInWorld(newEntity);

        // Set the spawned entity's target accordingly
        if (newEntity is EntityAlive ea && intendedTarget != null)
        {
            ea.SetAttackTarget(intendedTarget, 600);
        }

        holdingEntity.PlayOneShot(soundAttack);
    }
}
