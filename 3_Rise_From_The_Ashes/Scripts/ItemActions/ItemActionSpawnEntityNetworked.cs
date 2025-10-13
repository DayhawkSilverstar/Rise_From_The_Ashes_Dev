using UnityEngine;
using UnityEngine.Scripting;

// Network-safe spawn action: works when executed on server or client
[Preserve]
public class ItemActionSpawnEntityNetworked : ItemAction
{
    [PublicizedFrom(EAccessModifier.Protected)]
    public class ItemActionDataSpawnEntity : ItemActionAttackData
    {
        public enum State
        {
            None,
            Anim,
            Spawn,
            End
        }

        public State state;
        public float stateTime;

        // Anchor player to base the spawn location on (if available)
        public EntityPlayer anchorPlayer;

        public ItemActionDataSpawnEntity(ItemInventoryData _invData, int _indexInEntityOfAction)
            : base(_invData, _indexInEntityOfAction)
        {
        }
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public int animType;

    [PublicizedFrom(EAccessModifier.Private)]
    public float animWait;

    [PublicizedFrom(EAccessModifier.Private)]
    public string soundWarn;

    [PublicizedFrom(EAccessModifier.Private)]
    public string soundAttack;

    [PublicizedFrom(EAccessModifier.Private)]
    public string entityToSpawn;

    [PublicizedFrom(EAccessModifier.Private)]
    public Vector3 entityOffset;

    public override ItemActionData CreateModifierData(ItemInventoryData _invData, int _indexInEntityOfAction)
    {
        return new ItemActionDataSpawnEntity(_invData, _indexInEntityOfAction);
    }

    public override void ReadFrom(DynamicProperties _props)
    {
        base.ReadFrom(_props);
        animType = _props.GetInt("AnimType");
        animWait = _props.GetFloat("AnimWait");
        soundWarn = _props.GetString("SoundWarn");
        soundAttack = _props.GetString("SoundAttack");
        entityToSpawn = _props.GetString("Entity");
        _props.ParseVec("EntityOffset", ref entityOffset);
    }

    public override void StartHolding(ItemActionData _actionData) { }

    public override void StopHolding(ItemActionData _actionData) { }

    public override void OnHoldingUpdate(ItemActionData _actionData)
    {
        var data = (ItemActionDataSpawnEntity)_actionData;
        data.stateTime += 0.05f;
        switch (data.state)
        {
            case ItemActionDataSpawnEntity.State.Anim:
                if (!(data.stateTime < animWait))
                {
                    data.state = ItemActionDataSpawnEntity.State.Spawn;
                }
                break;
            case ItemActionDataSpawnEntity.State.Spawn:
                DoSpawn(data);
                data.state = ItemActionDataSpawnEntity.State.End;
                break;
        }
    }

    public override void CancelAction(ItemActionData _actionData)
    {
        ((ItemActionDataSpawnEntity)_actionData).state = ItemActionDataSpawnEntity.State.None;
    }

    public override void ExecuteAction(ItemActionData _actionData, bool _bReleased)
    {
        EntityAlive holdingEntity = _actionData.invData.holdingEntity;
        if (!holdingEntity)
        {
            return;
        }

        var data = (ItemActionDataSpawnEntity)_actionData;
        if (!_bReleased)
        {
            if (data.state == ItemActionDataSpawnEntity.State.None)
            {
                // Establish the anchor player for spawning:
                // - If the holder is a player, use them.
                // - Else, if the holder (AI) has a player attack target, use that.
                data.anchorPlayer = holdingEntity as EntityPlayer;
                if (data.anchorPlayer == null)
                {
                    var atk = holdingEntity.GetAttackTarget();
                    data.anchorPlayer = atk as EntityPlayer;
                }

                data.state = ItemActionDataSpawnEntity.State.Anim;
                data.stateTime = 0f;
                holdingEntity.StartAnimAction(animType + 3000);
                holdingEntity.PlayOneShot(soundWarn);
            }
        }
        else
        {
            data.state = ItemActionDataSpawnEntity.State.None;
        }
    }

    public override bool IsActionRunning(ItemActionData _actionData)
    {
        return ((ItemActionDataSpawnEntity)_actionData).state != ItemActionDataSpawnEntity.State.None;
    }

    // Performs the actual spawn. If running on a client, requests the server to spawn.
    protected virtual void DoSpawn(ItemActionData _actionData)
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

        // Server vs Client path
        bool isServer = SingletonMonoBehaviour<ConnectionManager>.Instance != null && SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer;
        if (isServer)
        {
            world.SpawnEntityInWorld(newEntity);
        }
        else
        {
            var ecd = new EntityCreationData(newEntity) { id = -1 };
            GameManager.Instance.RequestToSpawnEntityServer(ecd);
            newEntity.OnEntityUnload();
        }

        // Set the spawned entity's target accordingly
        if (newEntity is EntityAlive ea && intendedTarget != null)
        {
            ea.SetAttackTarget(intendedTarget, 600);
        }

        holdingEntity.PlayOneShot(soundAttack);
    }
}
