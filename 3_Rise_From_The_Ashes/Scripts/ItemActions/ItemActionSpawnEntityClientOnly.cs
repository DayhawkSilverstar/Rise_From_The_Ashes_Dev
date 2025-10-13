using UnityEngine;
using UnityEngine.Scripting;

// Client-side safe spawn action: always routes through server
[Preserve]
public class ItemActionSpawnEntityClientOnly : ItemActionSpawnEntityNetworked
{
    protected override void DoSpawn(ItemActionData _actionData)
    {
        EntityAlive holdingEntity = _actionData.invData.holdingEntity;
        if (!holdingEntity || !holdingEntity.IsAttackValid())
            return;

        Vector3 spawnPos = holdingEntity.getHeadPosition();
        spawnPos += holdingEntity.qrotation * entityOffset;

        int classId = EntityClass.GetId(entityToSpawn);
        if (classId < 0)
            return;

        // Create the entity with intended position and rotation
        Entity newEntity = EntityFactory.CreateEntity(classId, spawnPos, new Vector3(0f, holdingEntity.rotation.y, 0f));
        if (newEntity == null)
            return;

        newEntity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner);

        // Always request server to spawn, even if local host
        var ecd = new EntityCreationData(newEntity) { id = -1 };
        GameManager.Instance.RequestToSpawnEntityServer(ecd);
        newEntity.OnEntityUnload();

        if (newEntity is EntityAlive ea)
        {
            ea.SetAttackTarget(holdingEntity.GetAttackTarget(), 600);
        }

        holdingEntity.PlayOneShot(soundAttack);
    }
}
