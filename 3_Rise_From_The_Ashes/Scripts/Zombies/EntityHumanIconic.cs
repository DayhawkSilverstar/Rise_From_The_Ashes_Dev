using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class EntityHumanIconic : EntityEnemyIconic
{
    public ulong timeToDie;

    private float moveSpeedRagePer;

    private float moveSpeedScaleTime;

    private float fallTime;

    private float fallThresholdTime;

    private static float[] moveSpeeds = new float[5] { 0f, 0.35f, 0.7f, 1f, 1.35f };

    private static float[] moveRageSpeeds = new float[5] { 0.75f, 0.8f, 0.9f, 1.15f, 1.7f };

    private static float[] moveSuperRageSpeeds = new float[5] { 0.88f, 0.92f, 1f, 1.2f, 1.7f };

    private static float[] rageChances = new float[6] { 0f, 0.15f, 0.3f, 0.35f, 0.4f, 0.5f };

    private static float[] superRageChances = new float[6] { 0f, 0.01f, 0.03f, 0.05f, 0.08f, 0.15f };

    protected override EnumPositionUpdateMovementType positionUpdateMovementType => EnumPositionUpdateMovementType.MoveTowards;

    public override bool IsRunning
    {
        get
        {
            EnumGamePrefs eProperty = EnumGamePrefs.ZombieMove;
            if (IsBloodMoon)
            {
                eProperty = EnumGamePrefs.ZombieBMMove;
            }
            else if (IsFeral)
            {
                eProperty = EnumGamePrefs.ZombieFeralMove;
            }
            else if (world.IsDark())
            {
                eProperty = EnumGamePrefs.ZombieMoveNight;
            }

            return GamePrefs.GetInt(eProperty) >= 2;
        }
    }

    protected override void Awake()
    {
        base.Awake();
    }

    protected override void InitCommon()
    {
        base.InitCommon();
        if (walkType == 21)
        {
            TurnIntoCrawler();
        }
    }

    public override void OnAddedToWorld()
    {
        base.OnAddedToWorld();
        timeToDie = world.worldTime + 1800 + (ulong)(22000f * rand.RandomFloat);
        if (!IsFeral || GetSpawnerSource() != EnumSpawnerSource.Biome)
        {
            return;
        }

        int num = (int)SkyManager.GetDawnTime();
        int num2 = (int)SkyManager.GetDuskTime();
        int num3 = GameUtils.WorldTimeToHours(WorldTimeBorn);
        if (num3 < num || num3 >= num2)
        {
            int num4 = GameUtils.WorldTimeToDays(world.worldTime);
            if (GameUtils.WorldTimeToHours(world.worldTime) >= num2)
            {
                num4++;
            }

            timeToDie = GameUtils.DayTimeToWorldTime(num4, num, 0);
        }
    }

    public override void OnUpdateLive()
    {
        base.OnUpdateLive();
        if (moveSpeedRagePer > 0f && bodyDamage.CurrentStun == EnumEntityStunType.None)
        {
            moveSpeedScaleTime -= 0.05f;
            if (moveSpeedScaleTime <= 0f)
            {
                StopRage();
            }
        }

        if (!isEntityRemote && !IsDead() && world.worldTime >= timeToDie && !attackTarget)
        {
            Kill(DamageResponse.New(_fatal: true));
        }

        if (!emodel)
        {
            return;
        }

        AvatarController avatarController = emodel.avatarController;
        if (!avatarController)
        {
            return;
        }

        bool flag = onGround || isSwimming || bInElevator;
        if (flag)
        {
            fallTime = 0f;
            fallThresholdTime = 0f;
            if (bInElevator)
            {
                fallThresholdTime = 0.6f;
            }
        }
        else
        {
            if (fallThresholdTime == 0f)
            {
                fallThresholdTime = 0.1f + rand.RandomFloat * 0.3f;
            }

            fallTime += 0.05f;
        }

        bool canFall = !emodel.IsRagdollActive && bodyDamage.CurrentStun == EnumEntityStunType.None && !isSwimming && !bInElevator && jumpState == JumpState.Off && !IsDead();
        if (fallTime <= fallThresholdTime)
        {
            canFall = false;
        }

        avatarController.SetFallAndGround(canFall, flag);
    }

    public override Ray GetLookRay()
    {
        return base.IsBreakingBlocks ? new Ray(position + new Vector3(0f, GetEyeHeight(), 0f), GetLookVector()) : ((GetWalkType() != 22) ? new Ray(getHeadPosition(), GetLookVector()) : new Ray(getHeadPosition(), GetLookVector()));
    }

    public override Vector3 GetLookVector()
    {
        if (lookAtPosition.Equals(Vector3.zero))
        {
            return base.GetLookVector();
        }

        return lookAtPosition - getHeadPosition();
    }

    public override float GetMoveSpeedAggro()
    {
        EnumGamePrefs eProperty = EnumGamePrefs.ZombieMove;
        if (IsBloodMoon)
        {
            eProperty = EnumGamePrefs.ZombieBMMove;
        }
        else if (IsFeral)
        {
            eProperty = EnumGamePrefs.ZombieFeralMove;
        }
        else if (world.IsDark())
        {
            eProperty = EnumGamePrefs.ZombieMoveNight;
        }

        int @int = GamePrefs.GetInt(eProperty);
        float num = moveSpeeds[@int];
        if (moveSpeedRagePer > 1f)
        {
            num = moveSuperRageSpeeds[@int];
        }
        else if (moveSpeedRagePer > 0f)
        {
            float num2 = moveRageSpeeds[@int];
            num = num * (1f - moveSpeedRagePer) + num2 * moveSpeedRagePer;
        }

        num = ((!(num < 1f)) ? (moveSpeedAggroMax * num) : (moveSpeedAggro * (1f - num) + moveSpeedAggroMax * num));
        return EffectManager.GetValue(PassiveEffects.RunSpeed, null, num, this);
    }

    protected override float getNextStepSoundDistance()
    {
        if (!IsRunning)
        {
            return 0.5f;
        }

        return 1.5f;
    }

    protected override void updateStepSound(float _distX, float _distZ)
    {
    }

    public override void MoveEntityHeaded(Vector3 _direction, bool _isDirAbsolute)
    {
        if (walkType == 21 && Jumping)
        {
            motion = accumulatedRootMotion;
            accumulatedRootMotion = Vector3.zero;
            IsRotateToGroundFlat = true;
            if (moveHelper != null)
            {
                Vector3 vector = moveHelper.JumpToPos - position;
                if (Utils.FastAbs(vector.y) < 0.2f)
                {
                    motion.y = vector.y * 0.2f;
                }

                if (Utils.FastAbs(vector.x) < 0.3f)
                {
                    motion.x = vector.x * 0.2f;
                }

                if (Utils.FastAbs(vector.z) < 0.3f)
                {
                    motion.z = vector.z * 0.2f;
                }

                if (vector.sqrMagnitude < 0.0100000007f)
                {
                    if ((bool)emodel && (bool)emodel.avatarController)
                    {
                        emodel.avatarController.StartAnimationJump(AnimJumpMode.Land);
                    }

                    Jumping = false;
                }
            }

            entityCollision(motion);
        }
        else
        {
            base.MoveEntityHeaded(_direction, _isDirAbsolute);
        }
    }

    protected override void UpdateJump()
    {
        if (walkType == 21 && !isSwimming)
        {
            FaceJumpTo();
            jumpState = JumpState.Climb;
            if (!emodel.avatarController || !emodel.avatarController.IsAnimationJumpRunning())
            {
                Jumping = false;
            }

            if (jumpTicks == 0 && accumulatedRootMotion.y > 0.005f)
            {
                jumpTicks = 30;
            }
        }
        else
        {
            base.UpdateJump();
            if (!isSwimming)
            {
                accumulatedRootMotion.y = 0f;
            }
        }
    }

    protected override void EndJump()
    {
        base.EndJump();
        IsRotateToGroundFlat = false;
    }

    protected override bool ExecuteFallBehavior(FallBehavior behavior, float _distance, Vector3 _fallMotion)
    {
        if (behavior == null || !emodel)
        {
            return false;
        }

        AvatarController avatarController = emodel.avatarController;
        if (!avatarController)
        {
            return false;
        }

        avatarController.UpdateInt("RandomSelector", rand.RandomRange(0, 64));
        switch (behavior.ResponseOp)
        {
            case FallBehavior.Op.None:
                avatarController.UpdateInt(AvatarController.jumpLandResponseHash, -1);
                break;
            case FallBehavior.Op.Land:
                avatarController.UpdateInt(AvatarController.jumpLandResponseHash, 0);
                break;
            case FallBehavior.Op.LandLow:
                avatarController.UpdateInt(AvatarController.jumpLandResponseHash, 1);
                break;
            case FallBehavior.Op.LandHard:
                avatarController.UpdateInt(AvatarController.jumpLandResponseHash, 2);
                break;
            case FallBehavior.Op.Stumble:
                avatarController.UpdateInt(AvatarController.jumpLandResponseHash, 3);
                break;
            case FallBehavior.Op.Ragdoll:
                emodel.DoRagdoll(rand.RandomFloat * 2f, EnumBodyPartHit.None, _fallMotion * 20f, Vector3.zero, isRemote: false);
                break;
        }

        if (attackTarget != null && behavior.RagePer.IsSet() && behavior.RageTime.IsSet() && StartRage(behavior.RagePer.Random(rand), behavior.RageTime.Random(rand)))
        {
            avatarController.StartAnimationRaging();
        }

        return true;
    }

    protected override bool ExecuteDestroyBlockBehavior(DestroyBlockBehavior behavior, ItemActionAttack.AttackHitInfo attackHitInfo)
    {
        if (behavior == null || attackHitInfo == null || moveHelper == null || emodel == null || emodel.avatarController == null)
        {
            return false;
        }

        if (walkType == 21)
        {
            return false;
        }

        moveHelper.ClearBlocked();
        moveHelper.ClearTempMove();
        emodel.avatarController.UpdateInt("RandomSelector", rand.RandomRange(0, 64));
        switch (behavior.ResponseOp)
        {
            case DestroyBlockBehavior.Op.Stumble:
                emodel.avatarController.BeginStun(EnumEntityStunType.StumbleBreakThrough, EnumBodyPartHit.LeftUpperLeg, Utils.EnumHitDirection.None, _criticalHit: false, 1f);
                SetStun(EnumEntityStunType.StumbleBreakThrough);
                bodyDamage.StunDuration = 1f;
                break;
            case DestroyBlockBehavior.Op.Ragdoll:
                emodel.avatarController.BeginStun(EnumEntityStunType.StumbleBreakThroughRagdoll, EnumBodyPartHit.LeftUpperLeg, Utils.EnumHitDirection.None, _criticalHit: false, 1f);
                SetStun(EnumEntityStunType.StumbleBreakThroughRagdoll);
                break;
        }

        if (attackTarget != null && behavior.RagePer.IsSet() && behavior.RageTime.IsSet())
        {
            StartRage(behavior.RagePer.Random(rand), behavior.RageTime.Random(rand));
        }

        return true;
    }

    public override int DamageEntity(DamageSource _damageSource, int _strength, bool _criticalHit, float impulseScale)
    {
        if (_damageSource.GetDamageType() == EnumDamageTypes.Falling)
        {
            _strength = (_strength + 1) / 2;
            int num = (GetMaxHealth() + 2) / 3;
            if (_strength > num)
            {
                _strength = num;
            }
        }

        return base.DamageEntity(_damageSource, _strength, _criticalHit, impulseScale);
    }

    public override void ProcessDamageResponseLocal(DamageResponse _dmResponse)
    {
        base.ProcessDamageResponseLocal(_dmResponse);
        if (isEntityRemote)
        {
            return;
        }

        int @int = GameStats.GetInt(EnumGameStats.GameDifficulty);
        float num = (float)_dmResponse.Strength / 40f;
        if (num > 1f)
        {
            num = Mathf.Pow(num, 0.29f);
        }

        float num2 = rageChances[@int] * num;
        if (rand.RandomFloat < num2)
        {
            if (rand.RandomFloat < superRageChances[@int])
            {
                StartRage(2f, 30f);
                PlayOneShot(GetSoundAlert());
            }
            else
            {
                StartRage(0.5f + rand.RandomFloat * 0.5f, 4f + rand.RandomFloat * 6f);
            }
        }
    }

    public bool StartRage(float speedPercent, float time)
    {
        if (speedPercent >= moveSpeedRagePer)
        {
            moveSpeedRagePer = speedPercent;
            moveSpeedScaleTime = time;
            return true;
        }

        return false;
    }

    public void StopRage()
    {
        moveSpeedRagePer = 0f;
        moveSpeedScaleTime = 0f;
    }

    public override void OnEntityDeath()
    {
        base.OnEntityDeath();
    }

    protected override Vector3i dropCorpseBlock()
    {
        if (lootContainer != null && lootContainer.IsUserAccessing())
        {
            return Vector3i.zero;
        }

        Vector3i vector3i = base.dropCorpseBlock();
        if (vector3i == Vector3i.zero)
        {
            return Vector3i.zero;
        }

        TileEntityLootContainer tileEntityLootContainer = world.GetTileEntity(0, vector3i) as TileEntityLootContainer;
        if (tileEntityLootContainer == null)
        {
            return Vector3i.zero;
        }

        if (lootContainer != null)
        {
            tileEntityLootContainer.CopyLootContainerDataFromOther(lootContainer);
        }
        else
        {
            tileEntityLootContainer.lootListName = lootListOnDeath;
            tileEntityLootContainer.SetContainerSize(LootContainer.GetLootContainer(lootListOnDeath).size);
        }

        tileEntityLootContainer.SetModified();
        return vector3i;
    }

    protected override void AnalyticsSendDeath(DamageResponse _dmResponse)
    {
        DamageSource source = _dmResponse.Source;
        if (source == null)
        {
            return;
        }

        string subKey;
        if (source.BuffClass != null)
        {
            subKey = source.BuffClass.Name;
        }
        else
        {
            if (source.ItemClass == null)
            {
                return;
            }

            subKey = source.ItemClass.Name;
        }

        GameSparksCollector.IncrementCounter(GameSparksCollector.GSDataKey.ZombiesKilledBy, subKey, 1);
    }

    protected override void TurnIntoCrawler()
    {
        BoxCollider component = base.gameObject.GetComponent<BoxCollider>();
        if ((bool)component)
        {
            component.center = new Vector3(0f, 0.35f, 0f);
            component.size = new Vector3(0.8f, 0.8f, 0.8f);
        }

        SetupBounds();
        boundingBox.center += position;
        bCanClimbLadders = false;
    }

    public override void BuffAdded(BuffValue _buff)
    {
        if (_buff.BuffClass.DamageType == EnumDamageTypes.Electrical)
        {
            Electrocuted = true;
        }
    }
}

