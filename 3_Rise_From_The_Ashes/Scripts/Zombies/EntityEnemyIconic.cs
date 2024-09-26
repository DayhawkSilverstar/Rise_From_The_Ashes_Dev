using UnityEngine;

public abstract class EntityEnemyIconic : EntityAliveIconic
{
    public bool IsHordeZombie;

    private int ticksUntilVisible = 2;

    public override void Init(int _entityClass)
    {
        base.Init(_entityClass);
        emodel.SetVisible(_bVisible: false);
    }

    public override void InitFromPrefab(int _entityClass)
    {
        base.InitFromPrefab(_entityClass);
        emodel.SetVisible(_bVisible: false);
    }

    public override void PostInit()
    {
        base.PostInit();
        if (!isEntityRemote)
        {
            IsBloodMoon = world.aiDirector.BloodMoonComponent.BloodMoonActive;
        }
    }

    public override void VisiblityCheck(float _distanceSqr, bool _isZoom)
    {
        bool bVisible = ticksUntilVisible <= 0 && _distanceSqr < (float)(_isZoom ? 14400 : 8100);
        emodel.SetVisible(bVisible);
    }

    public override void OnUpdateLive()
    {
        base.OnUpdateLive();
        if (ticksUntilVisible > 0)
        {
            ticksUntilVisible--;
        }
    }

    public override bool IsDrawMapIcon()
    {
        return true;
    }

    public override Vector3 GetMapIconScale()
    {
        return new Vector3(0.75f, 0.75f, 1f);
    }

    public override bool IsSavedToFile()
    {
        if (GetSpawnerSource() != EnumSpawnerSource.Dynamic || IsDead())
        {
            return base.IsSavedToFile();
        }

        return false;
    }

    protected override bool canDespawn()
    {
        if (!IsHordeZombie || world.GetPlayers().Count == 0)
        {
            return base.canDespawn();
        }

        return false;
    }

    protected override bool isRadiationSensitive()
    {
        return false;
    }

    protected override bool isDetailedHeadBodyColliders()
    {
        return true;
    }

    protected override bool isGameMessageOnDeath()
    {
        return false;
    }

    protected override void OnEntityTargeted(EntityAlive target)
    {
        base.OnEntityTargeted(target);
        if (!isEntityRemote && GetSpawnerSource() != EnumSpawnerSource.Dynamic && target is EntityPlayer)
        {
            world.aiDirector.NotifyIntentToAttack(this, target as EntityPlayer);
        }
    }

    public override int DamageEntity(DamageSource _damageSource, int _strength, bool _criticalHit, float _impulseScale)
    {
        return base.DamageEntity(_damageSource, _strength, _criticalHit, _impulseScale);
    }
}

