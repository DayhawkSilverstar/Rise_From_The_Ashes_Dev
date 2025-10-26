using System.Xml;
using System.Xml.Linq;
using UnityEngine;


//        <triggered_effect trigger="onProjectileImpact" action="SpawnEntityAtPoint, SCore" SpawnGroup="ZombiesBurntForest" />
public class MinEventActionSpawnEntityAroundPlayer : MinEventActionRemoveBuff
{
    private string strCvar;
    private string strSpawnGroup = "";

    public override void Execute(MinEventParams _params)
    {
        var world = GameManager.Instance.World;
        if (world == null)
            return;

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

        // Decide what to spawn from group
        int entityClassId = -1;
        if (!string.IsNullOrEmpty(strSpawnGroup))
        {
            int classIdTmp = 0;
            entityClassId = EntityGroups.GetRandomFromGroup(strSpawnGroup, ref classIdTmp);
        }
        if (entityClassId == -1)
            return;

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
            return;

        newEntity.SetSpawnerSource(EnumSpawnerSource.StaticSpawner);

        bool isServer = SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        if (isServer)
        {
            world.SpawnEntityInWorld(newEntity);
            if (anchorPlayer != null)
            {
                newEntity.SetAttackTarget(anchorPlayer, 600);
            }
        }
        else
        {
            var ecd = new EntityCreationData(newEntity) { id = -1 };
            GameManager.Instance.RequestToSpawnEntityServer(ecd);
            newEntity.OnEntityUnload();
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