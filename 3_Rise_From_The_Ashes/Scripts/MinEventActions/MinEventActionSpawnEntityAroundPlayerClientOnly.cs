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

        newEntity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner);

        // Spawn locally on client only (no server request)
        world.SpawnEntityInWorld(newEntity);
        Log.Out("MinEventActionSpawnEntityAroundPlayerClientOnly: Spawned Entity ID " + entityClassId);

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
