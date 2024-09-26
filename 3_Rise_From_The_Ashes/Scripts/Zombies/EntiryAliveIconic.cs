using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Audio;
using GamePath;
using Platform;
using UAI;
using UnityEngine;

public abstract class EntityAliveIconic : EntityAlive
{
    //public new MoveHelperIconic moveHelper;


    //private struct WeightBehavior
    //{
    //    public float weight;

    //    public int index;
    //}

    //private struct ImpactData
    //{
    //    public int count;
    //}



    //private int pendingSleepTrigger = -1;

    //private Vector3 sleeperLookDir;

    //private float sleeperViewAngle;

    //private Vector2 sightLightThreshold;

    //private Vector2 sightWakeThresholdAtRange;

    //private Vector2 sightGroanThresholdAtRange;

    //private bool isSnore;

    //private bool isGroan;

    //private bool isGroanSilent;

    //private float groanChance;

    //private int snoreGroanCD;

    //private const int kSnoreGroanMinCD = 20;

    //private const float cSwimGravityPer = 0.025f;

    //private const float cSwimDragY = 0.91f;

    //private const float cSwimDrag = 0.91f;

    //private const float cSwimAnimDelay = 6f;

    //private bool jumpIsMoving;

    //private int ticksNoPlayerAdjacent;

    //private EntityAlive revengeEntity;

    //private int revengeTimer;

    //private bool targetAlertChanged;

    //private int alertTicks;

    //private static string notAlertedId = "_notAlerted";

    //private int notAlertDelayTicks;

    //private bool isAlert;

    //private Vector3 investigatePos;

    //private int investigatePositionTicks;

    //private bool isInvestigateAlert;

    //private bool hasAI;

    //private Context utilityAIContext;

    //private float aiActiveDelay;

    //private bool m_isBreakingBlocks;

    //private bool m_isEating;

    //private EntityLookHelper lookHelper;

    //private Vector3 lastTargetPos;

    //private EntityAlive damagedTarget;

    //private int attackTargetTime;

    //private EntitySeeCache seeCache;

    //private ChunkCoordinates homePosition;

    //private int maximumHomeDistance;

    //private float jumpMovementFactor = 0.02f;

    //private float nextStepDistance;

    //private float nextSwimDistance;

    //private bool bParachuteWearing;

    //private bool bAimingGun;

    //private bool bJumping;

    //private bool bClimbing;

    //private int ticksToCheckSeenByPlayer;

    //private bool wasSeenByPlayer;

    //private ItemValue handItem;

    //private int livingSoundTicks;

    //private int soundRandomTime;

    //private int soundAlertTime;

    //private string soundJump;

    //private string soundRandom;

    //private string soundHurt;

    //private string soundHurtSmall;

    //private string soundDrownPain;

    //private string soundDeath;

    //private string soundAttack;

    //private string soundAlert;

    //private string soundStamina;

    //private string soundLiving;

    //private int soundLivingID = -1;

    //private string soundSpawn;

    //private string soundLand;

    //private string soundFootstepModifier;

    //private string soundGiveUp;

    //private string soundSleeperGroan;

    //private string soundSleeperSnore;

    //private float weight;

    //private float pushFactor;

    //private float maxViewAngle;

    //private BlockValue corpseBlockValue;

    //private float corpseBlockChance;

    //private int attackTimeoutDay;

    //private int attackTimeoutNight;

    //private string particleOnDeath;

    //private string particleOnDestroy;

    //private bool isMoveDirAbsolute;

    //private Vector3i blockPosStandingOn;

    //private bool blockStandingOnChanged;

    //private string rightHandTransformName;

    //private EntityStats entityStats;

    //private float proneRefillRate;

    //private float kneelRefillRate;

    //private float proneRefillCounter;

    //private float kneelRefillCounter;

    //private int deathHealth;

    //private bool stompsSpikes;

    //private bool isDancing;

    //private float lastTimeTraderStationChecked;

    //private bool lerpForwardSpeed;

    //private float speedForwardTarget;

    //private readonly List<FallBehavior> fallBehaviors = new List<FallBehavior>();

    //private bool disableFallBehaviorUntilOnGround;

    //private readonly List<DestroyBlockBehavior> _destroyBlockBehaviors = new List<DestroyBlockBehavior>();

    //private DynamicRagdollFlags _dynamicRagdoll;

    //private float _dynamicRagdollStunTime;

    //private Vector3 _dynamicRagdollRootMotion;

    //private readonly List<Vector3> _ragdollPositionsPrev = new List<Vector3>();

    //private readonly List<Vector3> _ragdollPositionsCur = new List<Vector3>();

    //private EModelBase.HeadStates currentHeadState;

    //private static readonly List<WeightBehavior> weightBehaviorTemp = new List<WeightBehavior>();

    //private bool bPlayHurtSound;

    //private int despawnDelayCounter;

    //private bool isDespawnWhenPlayerFar;

    //private bool bLastAttackReleased;

    //private bool wasOnGround = true;

    //private float landWaterLevel;

    //private bool m_addedToWorld;

    //private int saveHoldingItemIdxBeforeAttach;

    //private float impactSoundTime;

    //private Dictionary<Transform, ImpactData> impacts = new Dictionary<Transform, ImpactData>();

    //private Dictionary<string, Transform> particles = new Dictionary<string, Transform>();

    //private Dictionary<string, Transform> parts = new Dictionary<string, Transform>();

    //private static readonly float[] moveSpeedRandomness = new float[6] { 0.2f, 1f, 1.1f, 1.2f, 1.35f, 1.5f };

    //[SerializeField]
    //private List<OwnedEntityData> ownedEntities = new List<OwnedEntityData>();

    //public override bool JetpackActive
    //{
    //    get
    //    {
    //        return bJetpackActive;
    //    }
    //    set
    //    {
    //        if (value != bJetpackActive)
    //        {
    //            bJetpackActive = value;
    //            bEntityAliveFlagsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override bool JetpackWearing
    //{
    //    get
    //    {
    //        return bJetpackWearing;
    //    }
    //    set
    //    {
    //        if (value != bJetpackWearing)
    //        {
    //            bJetpackWearing = value;
    //            bEntityAliveFlagsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override bool ParachuteWearing
    //{
    //    get
    //    {
    //        return bParachuteWearing;
    //    }
    //    set
    //    {
    //        if (value != bParachuteWearing)
    //        {
    //            bParachuteWearing = value;
    //            bEntityAliveFlagsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override bool ApproachingEnemy
    //{
    //    get
    //    {
    //        return bApproachingEnemy;
    //    }
    //    set
    //    {
    //        if (value != bApproachingEnemy)
    //        {
    //            bApproachingEnemy = value;
    //            bEntityAliveFlagsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override bool ApproachingPlayer
    //{
    //    get
    //    {
    //        return bApproachingPlayer;
    //    }
    //    set
    //    {
    //        if (value != bApproachingPlayer)
    //        {
    //            bApproachingPlayer = value;
    //            bEntityAliveFlagsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override bool AimingGun
    //{
    //    get
    //    {
    //        if (emodel.avatarController != null && emodel.avatarController.TryGetBool(AvatarController.isAimingHash, out bAimingGun))
    //        {
    //            return bAimingGun;
    //        }

    //        return false;
    //    }
    //    set
    //    {
    //        bool aimingGun = AimingGun;
    //        if (value != aimingGun)
    //        {
    //            if (emodel.avatarController != null)
    //            {
    //                emodel.avatarController.UpdateBool(AvatarController.isAimingHash, value);
    //            }

    //            updateCameraPosition(_bLerpPosition: true);
    //        }

    //        if (this is EntityPlayerLocal && inventory != null)
    //        {
    //            inventory.holdingItem.Actions[1]?.AimingSet(inventory.holdingItemData.actionData[1], value, aimingGun);
    //        }
    //    }
    //}

    //public override bool MovementRunning
    //{
    //    get
    //    {
    //        return bMovementRunning;
    //    }
    //    set
    //    {
    //        if (value != bMovementRunning)
    //        {
    //            bMovementRunning = value;
    //            bPlayerStatsChanged = !isEntityRemote;
    //        }
    //    }
    //}

    //public override bool Crouching
    //{
    //    get
    //    {
    //        return bCrouching;
    //    }
    //    set
    //    {
    //        if (value != bCrouching)
    //        {
    //            bCrouching = value;
    //            if (emodel.avatarController != null)
    //            {
    //                emodel.avatarController.SetCrouching(value);
    //            }

    //            CurrentStanceTag = (bCrouching ? StanceTagCrouching : StanceTagStanding);
    //            Buffs.SetCustomVar("_crouching", bCrouching ? 1 : 0);
    //        }
    //    }
    //}

    //public override bool Jumping
    //{
    //    get
    //    {
    //        if (bJumping)
    //        {
    //            return EffectManager.GetValue(PassiveEffects.JumpStrength, null, 1f, this) != 0f;
    //        }

    //        return false;
    //    }
    //    set
    //    {
    //        if (value != bJumping)
    //        {
    //            bJumping = value;
    //            if (Jumping)
    //            {
    //                StartJump();
    //                CurrentMovementTag &= MovementTagIdle;
    //                CurrentMovementTag |= MovementTagJumping;
    //            }
    //            else
    //            {
    //                EndJump();
    //                CurrentMovementTag &= MovementTagJumping;
    //                bJumping = false;
    //            }

    //            bPlayerStatsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override bool RightArmAnimationAttack
    //{
    //    get
    //    {
    //        if (emodel.avatarController != null)
    //        {
    //            return emodel.avatarController.IsAnimationAttackPlaying();
    //        }

    //        return false;
    //    }
    //    set
    //    {
    //        if (emodel.avatarController != null && value && !emodel.avatarController.IsAnimationAttackPlaying())
    //        {
    //            emodel.avatarController.StartAnimationAttack();
    //        }
    //    }
    //}

    //public override bool RightArmAnimationUse
    //{
    //    get
    //    {
    //        if (emodel.avatarController != null)
    //        {
    //            return emodel.avatarController.IsAnimationUsePlaying();
    //        }

    //        return false;
    //    }
    //    set
    //    {
    //        if (emodel.avatarController != null && value != emodel.avatarController.IsAnimationUsePlaying())
    //        {
    //            emodel.avatarController.StartAnimationUse();
    //        }
    //    }
    //}

    //public override bool SpecialAttack
    //{
    //    get
    //    {
    //        if (emodel.avatarController != null)
    //        {
    //            return emodel.avatarController.IsAnimationSpecialAttackPlaying();
    //        }

    //        return false;
    //    }
    //    set
    //    {
    //        if (emodel.avatarController != null && value != emodel.avatarController.IsAnimationSpecialAttackPlaying())
    //        {
    //            bPlayerStatsChanged |= !isEntityRemote;
    //            emodel.avatarController.StartAnimationSpecialAttack(value, 0);
    //        }
    //    }
    //}

    //public override bool SpecialAttack2
    //{
    //    get
    //    {
    //        if (emodel.avatarController != null)
    //        {
    //            return emodel.avatarController.IsAnimationSpecialAttack2Playing();
    //        }

    //        return false;
    //    }
    //    set
    //    {
    //        if (emodel.avatarController != null && value)
    //        {
    //            bPlayerStatsChanged |= !isEntityRemote;
    //            emodel.avatarController.StartAnimationSpecialAttack2();
    //        }
    //    }
    //}

    //public override bool Raging
    //{
    //    get
    //    {
    //        if (emodel.avatarController != null)
    //        {
    //            return emodel.avatarController.IsAnimationRagingPlaying();
    //        }

    //        return false;
    //    }
    //    set
    //    {
    //        if (emodel.avatarController != null && value && !emodel.avatarController.IsAnimationRagingPlaying())
    //        {
    //            emodel.avatarController.StartAnimationRaging();
    //        }
    //    }
    //}

    //public override bool Electrocuted
    //{
    //    get
    //    {
    //        if (emodel != null && emodel.avatarController != null)
    //        {
    //            return emodel.avatarController.GetAnimationElectrocuteRemaining() > 0f;
    //        }

    //        return false;
    //    }
    //    set
    //    {
    //        if (emodel != null && emodel.avatarController != null && value != emodel.avatarController.GetAnimationElectrocuteRemaining() > 0.4f)
    //        {
    //            bPlayerStatsChanged |= !isEntityRemote;
    //            if (value)
    //            {
    //                emodel.avatarController.StartAnimationElectrocute(0.6f);
    //            }
    //        }
    //    }
    //}

    //public override bool HarvestingAnimation
    //{
    //    get
    //    {
    //        if (emodel.avatarController != null)
    //        {
    //            return emodel.avatarController.IsAnimationHarvestingPlaying();
    //        }

    //        return false;
    //    }
    //    set
    //    {
    //        emodel.avatarController.UpdateBool("Harvesting", value);
    //    }
    //}

    //public override int Died
    //{
    //    get
    //    {
    //        return died;
    //    }
    //    set
    //    {
    //        if (value != died)
    //        {
    //            died = value;
    //            bPlayerStatsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override int Score
    //{
    //    get
    //    {
    //        return score;
    //    }
    //    set
    //    {
    //        if (value != score)
    //        {
    //            score = value;
    //            bPlayerStatsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override int KilledZombies
    //{
    //    get
    //    {
    //        return killedZombies;
    //    }
    //    set
    //    {
    //        if (value != killedZombies)
    //        {
    //            killedZombies = value;
    //            bPlayerStatsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override int KilledPlayers
    //{
    //    get
    //    {
    //        return killedPlayers;
    //    }
    //    set
    //    {
    //        if (value != killedPlayers)
    //        {
    //            killedPlayers = value;
    //            bPlayerStatsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override int TeamNumber
    //{
    //    get
    //    {
    //        return teamNumber;
    //    }
    //    set
    //    {
    //        if (value != teamNumber)
    //        {
    //            teamNumber = value;
    //            bPlayerStatsChanged |= !isEntityRemote;
    //            if (!isEntityRemote)
    //            {
    //                //GameManager.Instance.GameMessage(EnumGameMessages.ChangedTeam, this, null);
    //            }
    //        }
    //    }
    //}

    //public override string EntityName
    //{
    //    get
    //    {
    //        return entityName;
    //    }
    //    set
    //    {
    //        if (!value.Equals(entityName))
    //        {
    //            entityName = value;
    //            bPlayerStatsChanged |= !isEntityRemote;
    //            HandleSetNavName();
    //        }
    //    }
    //}

    //public override int DeathHealth
    //{
    //    get
    //    {
    //        return deathHealth;
    //    }
    //    set
    //    {
    //        if (value != deathHealth)
    //        {
    //            deathHealth = value;
    //            bPlayerStatsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override bool Spawned
    //{
    //    get
    //    {
    //        return bSpawned;
    //    }
    //    set
    //    {
    //        if (value != bSpawned)
    //        {
    //            bSpawned = value;
    //            onSpawnStateChanged();
    //            bEntityAliveFlagsChanged |= !isEntityRemote;
    //        }
    //    }
    //}

    //public override EntityBedrollPositionList SpawnPoints => spawnPoints;

    //public override int Health
    //{
    //    get
    //    {
    //        return (int)Stats.Health.Value;
    //    }
    //    set
    //    {
    //        Stats.Health.Value = value;
    //    }
    //}

    //public override float Stamina
    //{
    //    get
    //    {
    //        return Stats.Stamina.Value;
    //    }
    //    set
    //    {
    //        Stats.Stamina.Value = value;
    //    }
    //}

    //public override float Water
    //{
    //    get
    //    {
    //        return Stats.Water.Value;
    //    }
    //    set
    //    {
    //        Stats.Water.Value = value;
    //    }
    //}

    //public override EModelBase.HeadStates CurrentHeadState
    //{
    //    get
    //    {
    //        return currentHeadState;
    //    }
    //    set
    //    {
    //        if (value != currentHeadState)
    //        {
    //            currentHeadState = value;
    //            bPlayerStatsChanged |= !isEntityRemote;
    //        }

    //        emodel.ForceHeadState(value);
    //    }
    //}

    //public override float MaxVelocity => 5f;

    //public override bool IsImmuneToLegDamage
    //{
    //    get
    //    {
    //        EntityClass entityClass = EntityClass.list[base.entityClass];
    //        if (GetWalkType() != 21 && bodyDamage.HasLeftLeg && bodyDamage.HasRightLeg)
    //        {
    //            if (entityClass.LowerLegDismemberThreshold <= 0f)
    //            {
    //                return entityClass.UpperLegDismemberThreshold <= 0f;
    //            }

    //            return false;
    //        }

    //        return true;
    //    }
    //}

    //public override bool IsAlert
    //{
    //    get
    //    {
    //        if (isEntityRemote)
    //        {
    //            return bReplicatedAlertFlag;
    //        }

    //        return isAlert;
    //    }
    //}

    //public override bool IsRunning
    //{
    //    get
    //    {
    //        if (!IsBloodMoon)
    //        {
    //            return world.IsDark();
    //        }

    //        return true;
    //    }
    //}

    //public new void BeginDynamicRagdoll(DynamicRagdollFlags flags, FloatRange stunTime)
    //{
    //    _dynamicRagdoll = flags;
    //    _dynamicRagdollRootMotion = Vector3.zero;
    //    _dynamicRagdollStunTime = stunTime.Random(rand);
    //}

    //public new void ActivateDynamicRagdoll()
    //{
    //    if (!_dynamicRagdoll.HasFlag(DynamicRagdollFlags.Active))
    //    {
    //        return;
    //    }

    //    DynamicRagdollFlags dynamicRagdoll = _dynamicRagdoll;
    //    _dynamicRagdoll = DynamicRagdollFlags.None;
    //    Vector3 forceVec = _dynamicRagdollRootMotion * 20f;
    //    bodyDamage.StunDuration = _dynamicRagdollStunTime;
    //    emodel.DoRagdoll(_dynamicRagdollStunTime, EnumBodyPartHit.None, forceVec, Vector3.zero, isRemote: true);
    //    if (dynamicRagdoll.HasFlag(DynamicRagdollFlags.UseBoneVelocities) && _ragdollPositionsPrev.Count == _ragdollPositionsCur.Count)
    //    {
    //        List<Vector3> list = new List<Vector3>();
    //        for (int i = 0; i < _ragdollPositionsPrev.Count; i++)
    //        {
    //            Vector3 vector = _ragdollPositionsCur[i] - _ragdollPositionsPrev[i];
    //            list.Add(vector * 20f);
    //        }

    //        emodel.ApplyRagdollVelocities(list);
    //    }
    //}

    //protected override void Awake()
    //{
    //    base.Awake();
    //    entityName = base.transform.name;
    //    MinEventContext.Self = this;
    //    seeCache = new EntitySeeCache(this);
    //    maximumHomeDistance = -1;
    //    homePosition = new ChunkCoordinates(0, 0, 0);
    //    if (SingletonMonoBehaviour<ConnectionManager>.Instance.IsServer)
    //    {
    //        hasAI = true;
    //        navigator = AstarManager.CreateNavigator(this);
    //        aiManager = new EAIManager(this);
    //        lookHelper = new EntityLookHelper(this);
    //        moveHelper = new MoveHelperIconic(this);
    //    }

    //    equipment = new Equipment(this);
    //    favoriteEquipment = new Equipment(this);
    //    InitInventory();
    //    if (bag == null)
    //    {
    //        bag = new Bag(this);
    //    }

    //    stepHeight = 0.52f;
    //    livingSoundTicks = GetSoundRandomTicks() - rand.RandomRange(GetSoundRandomTicks() / 2);
    //    spawnPoints = new EntityBedrollPositionList(this);
    //    CreationTimeSinceLevelLoad = Time.timeSinceLevelLoad;
    //    Buffs = new EntityBuffs(this);
    //    droppedBackpackPositions = new List<Vector3i>();
    //}

    //public override void Init(int _entityClass)
    //{
    //    base.Init(_entityClass);
    //    constructEntityStats();
    //    SwitchModelView(EnumEntityModelView.ThirdPerson);
    //    InitPostCommon();
    //}

    //public override void InitFromPrefab(int _entityClass)
    //{
    //    base.InitFromPrefab(_entityClass);
    //    SwitchModelView(EnumEntityModelView.ThirdPerson);
    //    ReassignEquipmentTransforms();
    //    InitPostCommon();
    //}

    //private void InitPostCommon()
    //{
    //    if (GameManager.IsDedicatedServer)
    //    {
    //        Transform modelTransform = emodel.GetModelTransform();
    //        if ((bool)modelTransform)
    //        {
    //            ServerHelper.SetupForServer(modelTransform.gameObject);
    //        }
    //    }

    //    AddCharacterController();
    //    wasSeenByPlayer = false;
    //    ticksToCheckSeenByPlayer = 20;
    //    if (EntityClass.list[entityClass].UseAIPackages)
    //    {
    //        hasAI = true;
    //        AIPackages = new List<string>();
    //        AIPackages.AddRange(EntityClass.list[entityClass].AIPackages);
    //        utilityAIContext = new Context(this);
    //    }

    //    List<string> buffs = EntityClass.list[entityClass].Buffs;
    //    if (buffs == null)
    //    {
    //        return;
    //    }

    //    for (int i = 0; i < buffs.Count; i++)
    //    {
    //        string text = buffs[i];
    //        if (!Buffs.HasBuff(text))
    //        {
    //            Buffs.AddBuff(text);
    //        }
    //    }
    //}

    //public override void PostInit()
    //{
    //    base.PostInit();
    //    ApplySpawnState();
    //    LODGroup componentInChildren = emodel.GetModelTransform().GetComponentInChildren<LODGroup>();
    //    if ((bool)componentInChildren)
    //    {
    //        LOD[] lODs = componentInChildren.GetLODs();
    //        lODs[lODs.Length - 1].screenRelativeTransitionHeight = 0.003f;
    //        componentInChildren.SetLODs(lODs);
    //    }

    //    disableFallBehaviorUntilOnGround = true;
    //}

    //private void ApplySpawnState()
    //{
    //    if (Health <= 0 && isEntityRemote)
    //    {
    //        ClientKill(DamageResponse.New(_fatal: true));
    //    }

    //    ExecuteDismember(restoreState: true);
    //}

    //public override void InitInventory()
    //{
    //    if (inventory == null)
    //    {
    //        inventory = new Inventory(GameManager.Instance, this);
    //    }
    //}

    //public override void SwitchModelView(EnumEntityModelView modelView)
    //{
    //    emodel.SwitchModelAndView(modelView == EnumEntityModelView.FirstPerson, IsMale);
    //    ReassignEquipmentTransforms();
    //}

    //public override void ReassignEquipmentTransforms()
    //{
    //    equipment.InitializeEquipmentTransforms();
    //}

    //public override void CopyPropertiesFromEntityClass()
    //{
    //    base.CopyPropertiesFromEntityClass();
    //    EntityClass entityClass = EntityClass.list[base.entityClass];
    //    string @string = entityClass.Properties.GetString(EntityClass.PropHandItem);
    //    if (@string.Length > 0)
    //    {
    //        handItem = ItemClass.GetItem(@string);
    //        if (handItem.IsEmpty())
    //        {
    //            throw new Exception("Item with name '" + @string + "' not found!");
    //        }
    //    }
    //    else
    //    {
    //        handItem = ItemClass.GetItem("meleeHandPlayer").Clone();
    //    }

    //    if (inventory != null)
    //    {
    //        inventory.SetBareHandItem(handItem);
    //    }

    //    rightHandTransformName = "Gunjoint";
    //    entityClass.Properties.ParseString(EntityClass.PropRightHandJointName, ref rightHandTransformName);


    //    factionId = 0;
    //    factionRank = 0;
    //    if (EntityClass.list[base.entityClass].Properties.Contains("Faction"))
    //    {
    //        Faction factionByName = FactionManager.Instance.GetFactionByName(EntityClass.list[base.entityClass].Properties.Values["Faction"]);
    //        if (factionByName != null)
    //        {
    //            factionId = factionByName.ID;
    //            if (EntityClass.list[base.entityClass].Properties.Contains("FactionRank"))
    //            {
    //                factionRank = StringParsers.ParseUInt8(EntityClass.list[base.entityClass].Properties.Values["FactionRank"]);
    //            }
    //        }
    //    }



    //    maxViewAngle = 180f;
    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropMaxViewAngle))
    //    {
    //        maxViewAngle = StringParsers.ParseFloat(entityClass.Properties.Values[EntityClass.PropMaxViewAngle]);
    //    }

    //    sightRangeBase = entityClass.SightRange;
    //    sightLightThreshold = entityClass.sightLightThreshold;
    //    SetSleeperSight(-1f, -1f);
    //    sightWakeThresholdAtRange = new Vector2(rand.RandomRange(entityClass.WakeMin.x, entityClass.WakeMin.y), rand.RandomRange(entityClass.WakeMax.y, entityClass.WakeMax.y));
    //    sightGroanThresholdAtRange = new Vector2(rand.RandomRange(entityClass.GroanMin.x, entityClass.GroanMin.y), rand.RandomRange(entityClass.GroanMax.y, entityClass.GroanMax.y));
    //    noiseWake = rand.RandomRange(entityClass.NoiseWake.x, entityClass.NoiseWake.y);
    //    noiseGroan = rand.RandomRange(entityClass.NoiseGroan.x, entityClass.NoiseGroan.y);
    //    smellWake = rand.RandomRange(entityClass.SmellWake.x, entityClass.SmellWake.y);
    //    smellGroan = rand.RandomRange(entityClass.SmellGroan.x, entityClass.SmellGroan.y);
    //    groanChance = entityClass.GroanChance;
    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropWeight))
    //    {
    //        weight = Mathf.Max(StringParsers.ParseFloat(entityClass.Properties.Values[EntityClass.PropWeight]), 0.5f);
    //    }
    //    else
    //    {
    //        weight = 1f;
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropPushFactor))
    //    {
    //        pushFactor = StringParsers.ParseFloat(entityClass.Properties.Values[EntityClass.PropPushFactor]);
    //    }
    //    else
    //    {
    //        pushFactor = 1f;
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropTimeStayAfterDeath))
    //    {
    //        timeStayAfterDeath = (int)(StringParsers.ParseFloat(entityClass.Properties.Values[EntityClass.PropTimeStayAfterDeath]) * 20f);
    //    }
    //    else
    //    {
    //        timeStayAfterDeath = 100;
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropIsMale))
    //    {
    //        IsMale = StringParsers.ParseBool(entityClass.Properties.Values[EntityClass.PropIsMale]);
    //    }
    //    else
    //    {
    //        IsMale = true;
    //    }

    //    if (aiManager != null)
    //    {
    //        aiManager.CopyPropertiesFromEntityClass(entityClass);
    //    }

    //    IsFeral = entityClass.Tags.Test_Bit(FeralTagBit);
    //    moveSpeed = 1f;
    //    entityClass.Properties.ParseFloat(EntityClass.PropMoveSpeed, ref moveSpeed);
    //    moveSpeedNight = moveSpeed;
    //    entityClass.Properties.ParseFloat(EntityClass.PropMoveSpeedNight, ref moveSpeedNight);
    //    moveSpeedAggro = moveSpeed;
    //    moveSpeedAggroMax = moveSpeed;
    //    entityClass.Properties.ParseVec(EntityClass.PropMoveSpeedAggro, ref moveSpeedAggro, ref moveSpeedAggroMax);
    //    moveSpeedPanic = 1f;
    //    moveSpeedPanicMax = 1f;
    //    entityClass.Properties.ParseFloat(EntityClass.PropMoveSpeedPanic, ref moveSpeedPanic);
    //    if (moveSpeedPanic != 1f)
    //    {
    //        moveSpeedPanicMax = moveSpeedPanic;
    //    }

    //    entityClass.Properties.ParseFloat(EntityClass.PropSwimSpeed, ref swimSpeed);
    //    entityClass.Properties.ParseVec(EntityClass.PropSwimStrokeRate, ref swimStrokeRate);
    //    Vector2 optionalValue = Vector2.negativeInfinity;
    //    entityClass.Properties.ParseVec(EntityClass.PropMoveSpeedRand, ref optionalValue);
    //    if (optionalValue.x > -1f)
    //    {
    //        float num = rand.RandomRange(optionalValue.x, optionalValue.y);
    //        int @int = GameStats.GetInt(EnumGameStats.GameDifficulty);
    //        num *= moveSpeedRandomness[@int];
    //        if (moveSpeedAggro < 1f)
    //        {
    //            moveSpeedAggro += num;
    //            if (moveSpeedAggro < 0.1f)
    //            {
    //                moveSpeedAggro = 0.1f;
    //            }

    //            if (moveSpeedAggro > moveSpeedAggroMax)
    //            {
    //                moveSpeedAggro = moveSpeedAggroMax;
    //            }
    //        }
    //    }

    //    walkType = GetSpawnWalkType(entityClass);
    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropCanClimbVertical))
    //    {
    //        bCanClimbVertical = StringParsers.ParseBool(entityClass.Properties.Values[EntityClass.PropCanClimbVertical]);
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropCanClimbLadders))
    //    {
    //        bCanClimbLadders = StringParsers.ParseBool(entityClass.Properties.Values[EntityClass.PropCanClimbLadders]);
    //    }

    //    Vector2 optionalValue2 = new Vector2(1.9f, 2.1f);
    //    entityClass.Properties.ParseVec(EntityClass.PropJumpMaxDistance, ref optionalValue2);
    //    jumpMaxDistance = rand.RandomRange(optionalValue2.x, optionalValue2.y);
    //    jumpDelay = 1f;
    //    entityClass.Properties.ParseFloat(EntityClass.PropJumpDelay, ref jumpDelay);
    //    jumpDelay *= 20f;
    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundRandomTime))
    //    {
    //        soundRandomTime = (int)(StringParsers.ParseFloat(entityClass.Properties.Values[EntityClass.PropSoundRandomTime]) * 20f);
    //    }
    //    else
    //    {
    //        soundRandomTime = 500;
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundAlertTime))
    //    {
    //        soundAlertTime = (int)(StringParsers.ParseFloat(entityClass.Properties.Values[EntityClass.PropSoundAlertTime]) * 20f);
    //    }
    //    else
    //    {
    //        soundAlertTime = 500;
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundRandom))
    //    {
    //        soundRandom = entityClass.Properties.Values[EntityClass.PropSoundRandom];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundJump))
    //    {
    //        soundJump = entityClass.Properties.Values[EntityClass.PropSoundJump];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundHurt))
    //    {
    //        soundHurt = entityClass.Properties.Values[EntityClass.PropSoundHurt];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundHurtSmall))
    //    {
    //        soundHurtSmall = entityClass.Properties.Values[EntityClass.PropSoundHurtSmall];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundDrownPain))
    //    {
    //        soundDrownPain = entityClass.Properties.Values[EntityClass.PropSoundDrownPain];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundDrownDeath))
    //    {
    //        soundDrownDeath = entityClass.Properties.Values[EntityClass.PropSoundDrownDeath];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundWaterSurface))
    //    {
    //        soundWaterSurface = entityClass.Properties.Values[EntityClass.PropSoundWaterSurface];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundDeath))
    //    {
    //        soundDeath = entityClass.Properties.Values[EntityClass.PropSoundDeath];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundAttack))
    //    {
    //        soundAttack = entityClass.Properties.Values[EntityClass.PropSoundAttack];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundAlert))
    //    {
    //        soundAlert = entityClass.Properties.Values[EntityClass.PropSoundAlert];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundSense))
    //    {
    //        soundSense = entityClass.Properties.Values[EntityClass.PropSoundSense];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundStamina))
    //    {
    //        soundStamina = entityClass.Properties.Values[EntityClass.PropSoundStamina];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundLiving))
    //    {
    //        soundLiving = entityClass.Properties.Values[EntityClass.PropSoundLiving];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundSpawn))
    //    {
    //        soundSpawn = entityClass.Properties.Values[EntityClass.PropSoundSpawn];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundLand))
    //    {
    //        soundLand = entityClass.Properties.Values[EntityClass.PropSoundLand];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundFootstepModifier))
    //    {
    //        soundFootstepModifier = entityClass.Properties.Values[EntityClass.PropSoundFootstepModifier];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundGiveUp))
    //    {
    //        soundGiveUp = entityClass.Properties.Values[EntityClass.PropSoundGiveUp];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundSleeperGroan))
    //    {
    //        soundSleeperGroan = entityClass.Properties.Values[EntityClass.PropSoundSleeperGroan];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropSoundSleeperSnore))
    //    {
    //        soundSleeperSnore = entityClass.Properties.Values[EntityClass.PropSoundSleeperSnore];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropAttackTimeoutDay))
    //    {
    //        attackTimeoutDay = (int)(StringParsers.ParseFloat(entityClass.Properties.Values[EntityClass.PropAttackTimeoutDay]) * 20f);
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropAttackTimeoutNight))
    //    {
    //        attackTimeoutNight = (int)(StringParsers.ParseFloat(entityClass.Properties.Values[EntityClass.PropAttackTimeoutNight]) * 20f);
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropParticleOnDeath))
    //    {
    //        particleOnDeath = entityClass.Properties.Values[EntityClass.PropParticleOnDeath];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropParticleOnDestroy))
    //    {
    //        particleOnDestroy = entityClass.Properties.Values[EntityClass.PropParticleOnDestroy];
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropCorpseBlock))
    //    {
    //        corpseBlockValue = Block.GetBlockValue(entityClass.Properties.Values[EntityClass.PropCorpseBlock]);
    //    }

    //    corpseBlockChance = 1f;
    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropCorpseBlockChance))
    //    {
    //        corpseBlockChance = StringParsers.ParseFloat(entityClass.Properties.Values[EntityClass.PropCorpseBlockChance]);
    //    }

    //    ExperienceValue = 20;
    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropExperienceGain))
    //    {
    //        ExperienceValue = (int)StringParsers.ParseFloat(entityClass.Properties.Values[EntityClass.PropExperienceGain]);
    //    }

    //    if (entityClass.Properties.Values.ContainsKey(EntityClass.PropStompsSpikes))
    //    {
    //        stompsSpikes = StringParsers.ParseBool(entityClass.Properties.Values[EntityClass.PropStompsSpikes]);
    //    }

    //    proneRefillRate = rand.RandomRange(entityClass.KnockdownProneRefillRate.x, entityClass.KnockdownProneRefillRate.y);
    //    kneelRefillRate = rand.RandomRange(entityClass.KnockdownKneelRefillRate.x, entityClass.KnockdownKneelRefillRate.y);
    //    GameMode gameModeForId = GameMode.GetGameModeForId(GameStats.GetInt(EnumGameStats.GameModeId));
    //    if (gameModeForId != null && entityClass.Properties.Values.ContainsKey(EntityClass.PropItemsOnEnterGame + "." + gameModeForId.GetTypeName()))
    //    {
    //        string[] array = entityClass.Properties.Values[EntityClass.PropItemsOnEnterGame + "." + gameModeForId.GetTypeName()].Split(',', (char)StringSplitOptions.None);
    //        foreach (string text in array)
    //        {
    //            ItemStack itemStack = ItemStack.FromString(text.Trim());
    //            if (itemStack.itemValue.IsEmpty())
    //            {
    //                throw new Exception("Item with name '" + text + "' not found in class " + EntityClass.list[base.entityClass].entityClassName);
    //            }

    //            itemsOnEnterGame.Add(itemStack);
    //        }
    //    }

    //    distractionResistance = EffectManager.GetValue(PassiveEffects.DistractionResistance, null, 0f, this);
    //    distractionResistanceWithTarget = EffectManager.GetValue(PassiveEffects.DistractionResistance, null, 0f, this, null, DistractionResistanceWithTargetTags);
    //    DynamicProperties dynamicProperties = entityClass.Properties.Classes[EntityClass.PropFallLandBehavior];
    //    if (dynamicProperties != null)
    //    {
    //        foreach (KeyValuePair<string, string> item in dynamicProperties.Data.Dict)
    //        {
    //            string key = item.Key;
    //            DictionarySave<string, string> dictionarySave = dynamicProperties.ParseKeyData(key);
    //            if (dictionarySave == null)
    //            {
    //                continue;
    //            }

    //            FloatRange floatRange = default(FloatRange);
    //            FloatRange ragePer = default(FloatRange);
    //            FloatRange rageTime = default(FloatRange);
    //            IntRange difficulty = new IntRange(0, 10);
    //            if (!dictionarySave.TryGetValue("anim", out var _value) || !Enum.TryParse<FallBehavior.Op>(_value, out var result))
    //            {
    //                Log.Error("Expected 'anim' parameter as float for FallBehavior " + key + ", skipping");
    //                continue;
    //            }

    //            float _result = 0f;
    //            if (!dictionarySave.TryGetValue("weight", out _value) || !StringParsers.TryParseFloat(_value, out _result))
    //            {
    //                Log.Error("Expected 'weight' parameter as float for FallBehavior " + key + ", skipping");
    //            }
    //            else if (dictionarySave.TryGetValue("height", out _value))
    //            {
    //                if (StringParsers.TryParseRange(_value, out var _result2, float.MaxValue))
    //                {
    //                    floatRange = _result2;
    //                    if (dictionarySave.TryGetValue("ragePer", out _value))
    //                    {
    //                        if (!StringParsers.TryParseRange(_value, out FloatRange _result3, (float?)null))
    //                        {
    //                            Log.Error("Expected 'ragePer' parameter as range(min,min-max) " + key + ", skipping");
    //                            continue;
    //                        }

    //                        ragePer = _result3;
    //                    }

    //                    if (dictionarySave.TryGetValue("rageTime", out _value))
    //                    {
    //                        if (!StringParsers.TryParseRange(_value, out FloatRange _result4, (float?)null))
    //                        {
    //                            Log.Error("Expected 'rageTime' parameter as range(min,min-max) " + key + ", skipping");
    //                            continue;
    //                        }

    //                        rageTime = _result4;
    //                    }

    //                    if (dictionarySave.TryGetValue("difficulty", out _value))
    //                    {
    //                        if (!StringParsers.TryParseRange(_value, out var _result5, null))
    //                        {
    //                            Log.Error("Expected 'difficulty' parameter as range(min,min-max) " + key + ", skipping");
    //                            continue;
    //                        }

    //                        difficulty = _result5;
    //                    }

    //                    fallBehaviors.Add(new FallBehavior(key, result, floatRange, _result, ragePer, rageTime, difficulty));
    //                }
    //                else
    //                {
    //                    Log.Error("Expected 'height' parameter as range(min,min-max) " + key + ", skipping");
    //                }
    //            }
    //            else
    //            {
    //                Log.Error("Expected 'height' parameter for FallBehavior " + key + ", skipping");
    //            }
    //        }
    //    }

    //    DynamicProperties dynamicProperties2 = entityClass.Properties.Classes[EntityClass.PropDestroyBlockBehavior];
    //    if (dynamicProperties2 == null)
    //    {
    //        return;
    //    }

    //    DestroyBlockBehavior.Op[] array2 = Enum.GetValues(typeof(DestroyBlockBehavior.Op)) as DestroyBlockBehavior.Op[];
    //    for (int j = 0; j < array2.Length; j++)
    //    {
    //        string text2 = array2[j].ToStringCached();
    //        DictionarySave<string, string> dictionarySave2 = dynamicProperties2.ParseKeyData(array2[j].ToStringCached());
    //        if (dictionarySave2 == null)
    //        {
    //            continue;
    //        }

    //        FloatRange ragePer2 = default(FloatRange);
    //        FloatRange rageTime2 = default(FloatRange);
    //        IntRange difficulty2 = new IntRange(0, 10);
    //        if (!dictionarySave2.TryGetValue("weight", out var _value2) || !StringParsers.TryParseFloat(_value2, out var _result6))
    //        {
    //            Log.Error($"Expected 'weight' parameter as float for FallBehavior {array2[j]}, skipping");
    //            continue;
    //        }

    //        if (dictionarySave2.TryGetValue("ragePer", out _value2))
    //        {
    //            if (!StringParsers.TryParseRange(_value2, out FloatRange _result7, (float?)null))
    //            {
    //                Log.Error($"Expected 'ragePer' parameter as range(min,min-max) {array2[j]}, skipping");
    //                continue;
    //            }

    //            ragePer2 = _result7;
    //        }

    //        if (dictionarySave2.TryGetValue("rageTime", out _value2))
    //        {
    //            if (!StringParsers.TryParseRange(_value2, out FloatRange _result8, (float?)null))
    //            {
    //                Log.Error($"Expected 'rageTime' parameter as range(min,min-max) {array2[j]}, skipping");
    //                continue;
    //            }

    //            rageTime2 = _result8;
    //        }

    //        if (dictionarySave2.TryGetValue("difficulty", out _value2))
    //        {
    //            if (!StringParsers.TryParseRange(_value2, out var _result9, null))
    //            {
    //                Log.Error("Expected 'difficulty' parameter as range(min,min-max) " + text2 + ", skipping");
    //                continue;
    //            }

    //            difficulty2 = _result9;
    //        }

    //        _destroyBlockBehaviors.Add(new DestroyBlockBehavior(text2, array2[j], _result6, ragePer2, rageTime2, difficulty2));
    //    }
    //}

    //public static int GetSpawnWalkType(EntityClass _entityClass)
    //{
    //    int optionalValue = 0;
    //    _entityClass.Properties.ParseInt(EntityClass.PropWalkType, ref optionalValue);
    //    return optionalValue;
    //}

    //public override void SetSleeper()
    //{
    //    IsSleeper = true;
    //    aiManager.pathCostScale += 0.2f;
    //}

    //public void SetSleeperSight(float angle, float range)
    //{
    //    if (angle < 0f)
    //    {
    //        angle = maxViewAngle;
    //    }

    //    sleeperViewAngle = angle;
    //    if (range < 0f)
    //    {
    //        range = Mathf.Max(3f, sightRangeBase * 0.2f);
    //    }

    //    sleeperSightRange = range;
    //}

    //public void SetSleeperHearing(float percent)
    //{
    //    if (percent < 0.001f)
    //    {
    //        percent = 0.001f;
    //    }

    //    percent = 1f / percent;
    //    noiseGroan *= percent;
    //    noiseWake *= percent;
    //}

    //public int GetSleeperDisturbedLevel(float dist, float lightLevel)
    //{
    //    float num = dist / sightRangeBase;
    //    if (num <= 1f)
    //    {
    //        float num2 = Mathf.Lerp(sightWakeThresholdAtRange.x, sightWakeThresholdAtRange.y, num);
    //        if (lightLevel > num2)
    //        {
    //            return 2;
    //        }

    //        float num3 = Mathf.Lerp(sightGroanThresholdAtRange.x, sightGroanThresholdAtRange.y, num);
    //        if (lightLevel > num3)
    //        {
    //            return 1;
    //        }
    //    }

    //    return 0;
    //}

    //public void GetSleeperDebugScale(float dist, out float wake, out float groan)
    //{
    //    float t = dist / sightRangeBase;
    //    wake = Mathf.Lerp(sightWakeThresholdAtRange.x, sightWakeThresholdAtRange.y, t);
    //    groan = Mathf.Lerp(sightGroanThresholdAtRange.x, sightGroanThresholdAtRange.y, t);
    //}

    //public void TriggerSleeperPose(int _pose, bool _returningToSleep = false)
    //{
    //    if (!IsDead())
    //    {
    //        if (emodel != null && emodel.avatarController != null)
    //        {
    //            emodel.avatarController.TriggerSleeperPose(_pose, _returningToSleep);
    //            pendingSleepTrigger = -1;
    //        }
    //        else
    //        {
    //            pendingSleepTrigger = _pose;
    //        }

    //        lastSleeperPose = _pose;
    //        IsSleeping = true;
    //        SleeperSupressLivingSounds = true;
    //        sleeperLookDir = Quaternion.AngleAxis(rotation.y, Vector3.up) * SleeperSpawnLookDir;
    //    }
    //}

    //public void ResumeSleeperPose()
    //{
    //    TriggerSleeperPose(lastSleeperPose, _returningToSleep: true);
    //}

    //public void ConditionalTriggerSleeperWakeUp()
    //{
    //    if (IsSleeping && !IsDead())
    //    {
    //        IsSleeping = false;
    //        IsSleeperPassive = false;
    //        emodel.avatarController.TriggerSleeperPose(-1);
    //        if (aiManager != null)
    //        {
    //            aiManager.SleeperWokeUp();
    //        }

    //        if (!world.IsRemote())
    //        {
    //            SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageSleeperWakeup>().Setup(entityId));
    //        }
    //    }
    //}

    //public void SetSleeperActive()
    //{
    //    if (IsSleeperPassive)
    //    {
    //        IsSleeperPassive = false;
    //        if (!world.IsRemote())
    //        {
    //            SingletonMonoBehaviour<ConnectionManager>.Instance.SendPackage(NetPackageManager.GetPackage<NetPackageSleeperPassiveChange>().Setup(entityId));
    //        }
    //    }
    //}

    //protected override void constructEntityStats()
    //{
    //    entityStats = new EntityStats(this);
    //}

    //protected override ItemValue GetHandItem()
    //{
    //    return handItem;
    //}

    //public bool IsHoldingLight()
    //{
    //    return inventory.IsFlashlightOn;
    //}

    //public void CycleActivatableItems()
    //{
    //}

    //public List<ItemValue> GetActivatableItemPool()
    //{
    //    List<ItemValue> list = new List<ItemValue>();
    //    CollectActivatableItems(list);
    //    return list;
    //}

    //public void CollectActivatableItems(List<ItemValue> _pool)
    //{
    //    if (inventory != null)
    //    {
    //        GetActivatableItems(inventory.holdingItemItemValue, _pool);
    //    }

    //    if (equipment != null)
    //    {
    //        ItemValue[] items = equipment.GetItems();
    //        for (int i = 0; i < items.Length; i++)
    //        {
    //            GetActivatableItems(items[i], _pool);
    //        }
    //    }
    //}

    //private static void GetActivatableItems(ItemValue _item, List<ItemValue> _itemPool)
    //{
    //    if (_item.ItemClass == null)
    //    {
    //        return;
    //    }

    //    if (_item.ItemClass.HasTrigger(MinEventTypes.onSelfItemActivate))
    //    {
    //        _itemPool.Add(_item);
    //    }

    //    for (int i = 0; i < _item.Modifications.Length; i++)
    //    {
    //        if (_item.Modifications[i] != null && _item.Modifications[i].ItemClass != null && _item.Modifications[i].ItemClass.HasTrigger(MinEventTypes.onSelfItemActivate))
    //        {
    //            _itemPool.Add(_item.Modifications[i]);
    //        }
    //    }
    //}

    //public override void OnUpdatePosition(float _partialTicks)
    //{
    //    base.OnUpdatePosition(_partialTicks);
    //    prevRotation = rotation;
    //    Vector3 zero = Vector3.zero;
    //    for (int i = 0; i < lastTickPos.Length - 1; i++)
    //    {
    //        zero.x += lastTickPos[i].x - lastTickPos[i + 1].x;
    //        zero.z += lastTickPos[i].z - lastTickPos[i + 1].z;
    //    }

    //    zero += position - lastTickPos[0];
    //    zero /= (float)lastTickPos.Length;
    //    if (AttachedToEntity == null)
    //    {
    //        updateStepSound(zero.x, zero.z);
    //    }

    //    if (!RootMotion && !isEntityRemote)
    //    {
    //        updateSpeedForwardAndStrafe(zero, _partialTicks);
    //    }
    //}

    //public void Snore()
    //{
    //    if (!isSnore && isGroan && snoreGroanCD <= 0)
    //    {
    //        isSnore = true;
    //        isGroan = false;
    //        snoreGroanCD = rand.RandomRange(20, 21);
    //        if (soundSleeperSnore != null && !isGroanSilent)
    //        {
    //            Manager.BroadcastPlay(soundSleeperSnore);
    //        }
    //    }
    //}

    //public void Groan()
    //{
    //    if (isGroan || snoreGroanCD > 0)
    //    {
    //        return;
    //    }

    //    isGroan = true;
    //    isSnore = false;
    //    snoreGroanCD = rand.RandomRange(20, 21);
    //    if (groanChance >= 1f || rand.RandomFloat <= groanChance)
    //    {
    //        isGroanSilent = false;
    //        if (soundSleeperGroan != null)
    //        {
    //            Manager.BroadcastPlay(soundSleeperGroan);
    //        }
    //    }
    //    else
    //    {
    //        isGroanSilent = true;
    //    }
    //}

    //public override void OnUpdateEntity()
    //{
    //    base.OnUpdateEntity();
    //    Buffs.SetCustomVar("_underwater", inWaterPercent);
    //    if (Buffs != null)
    //    {
    //        Buffs.Update(Time.deltaTime);
    //    }

    //    OnUpdateLive();
    //    if (!IsSleeping)
    //    {
    //        bag.OnUpdate();
    //        if (inventory != null)
    //        {
    //            inventory.OnUpdate();
    //        }
    //    }

    //    if (Health <= 0)
    //    {
    //        if (!IsDead() && !isEntityRemote && !IsGodMode.Value)
    //        {
    //            if (Buffs.HasBuff("drowning"))
    //            {
    //                DamageEntity(DamageSource.suffocating, 1, _criticalHit: false);
    //            }
    //            else
    //            {
    //                DamageEntity(DamageSource.disease, 1, _criticalHit: false);
    //            }
    //        }
    //    }
    //    else if (bPlayHurtSound)
    //    {
    //        string text = GetSoundHurt(woundedDamageSource, woundedStrength);
    //        if (text != null)
    //        {
    //            PlayOneShot(text);
    //        }
    //    }

    //    bPlayHurtSound = false;
    //    bBeenWounded = false;
    //    woundedStrength = 0;
    //    woundedDamageSource = null;
    //    if (snoreGroanCD > 0)
    //    {
    //        snoreGroanCD--;
    //    }

    //    if (!IsDead() && !isEntityRemote)
    //    {
    //        if (IsSleeping && pendingSleepTrigger > -1)
    //        {
    //            TriggerSleeperPose(pendingSleepTrigger);
    //        }

    //        if (isRadiationSensitive() && biomeStandingOn != null && biomeStandingOn.m_RadiationLevel > 0 && !IsGodMode.Value && world.worldTime % 20uL == 0L)
    //        {
    //            DamageEntity(DamageSource.radiation, biomeStandingOn.m_RadiationLevel, _criticalHit: false);
    //        }

    //        if (attackingTime <= 0 && --livingSoundTicks <= 0 && bodyDamage.CurrentStun == EnumEntityStunType.None && !SleeperSupressLivingSounds)
    //        {
    //            if (!ApproachingEnemy)
    //            {
    //                if (GetSoundRandom() != null)
    //                {
    //                    livingSoundTicks = GetSoundRandomTicks();
    //                    PlayOneShot(GetSoundRandom());
    //                }
    //            }
    //            else if (GetSoundAlert() != null)
    //            {
    //                livingSoundTicks = GetSoundAlertTicks();
    //                if (!IsScoutZombie)
    //                {
    //                    PlayOneShot(GetSoundAlert());
    //                }

    //                if (targetAlertChanged)
    //                {
    //                    OnEntityTargeted(attackTarget);
    //                    targetAlertChanged = false;
    //                }
    //            }
    //        }
    //    }

    //    if (hasBeenAttackedTime > 0)
    //    {
    //        hasBeenAttackedTime--;
    //    }

    //    if (painResistPercent > 0f)
    //    {
    //        painResistPercent -= 0.01f;
    //    }

    //    if (attackingTime > 0)
    //    {
    //        attackingTime--;
    //        if (attackingTime == 0)
    //        {
    //            if (attackTarget != null)
    //            {
    //                LastTargetPos = attackTarget.GetPosition();
    //            }

    //            ApproachingEnemy = false;
    //            ApproachingPlayer = false;
    //        }
    //    }

    //    if (investigatePositionTicks > 0 && --investigatePositionTicks == 0)
    //    {
    //        ResetDespawnTime();
    //    }

    //    bool flag = IsDead();
    //    if (alertEnabled)
    //    {
    //        isAlert = bReplicatedAlertFlag;
    //        if (!isEntityRemote)
    //        {
    //            if (alertTicks > 0)
    //            {
    //                alertTicks--;
    //            }

    //            isAlert = !flag && (alertTicks > 0 || ApproachingPlayer || (bool)attackTarget || (HasInvestigatePosition && isInvestigateAlert));
    //            if (bReplicatedAlertFlag != isAlert)
    //            {
    //                bReplicatedAlertFlag = isAlert;
    //                bPlayerStatsChanged = true;
    //            }
    //        }

    //        if (!isAlert && !flag)
    //        {
    //            Buffs.SetCustomVar(notAlertedId, 1f);
    //            notAlertDelayTicks = 4;
    //        }
    //        else
    //        {
    //            if (notAlertDelayTicks > 0)
    //            {
    //                notAlertDelayTicks--;
    //            }

    //            if (notAlertDelayTicks == 0)
    //            {
    //                Buffs.SetCustomVar(notAlertedId, 0f);
    //            }
    //        }
    //    }

    //    if (flag)
    //    {
    //        OnDeathUpdate();
    //    }

    //    if (revengeEntity != null)
    //    {
    //        if (!revengeEntity.IsAlive())
    //        {
    //            SetRevengeTarget(null);
    //        }
    //        else if (revengeTimer > 0)
    //        {
    //            revengeTimer--;
    //        }
    //        else
    //        {
    //            SetRevengeTarget(null);
    //        }
    //    }
    //}

    //public override void KillLootContainer()
    //{
    //    if (!isEntityRemote && IsDead() && !corpseBlockValue.isair && deathUpdateTime < timeStayAfterDeath)
    //    {
    //        deathUpdateTime = timeStayAfterDeath - 1;
    //    }

    //    base.KillLootContainer();
    //}

    //public override void Kill(DamageResponse _dmResponse)
    //{
    //    NotifySleeperDeath();
    //    if (AttachedToEntity != null)
    //    {
    //        Detach();
    //    }

    //    if (deathUpdateTime == 0)
    //    {
    //        string text = GetSoundDeath(_dmResponse.Source);
    //        if (text != null)
    //        {
    //            PlayOneShot(text);
    //        }
    //    }

    //    if (IsDead())
    //    {
    //        SetDead();
    //        return;
    //    }

    //    ClientKill(_dmResponse);
    //    base.Kill(_dmResponse);
    //}

    //public override void SetDead()
    //{
    //    base.SetDead();
    //    Stats.Health.Value = 0f;
    //}

    //public new void NotifySleeperDeath()
    //{
    //    if (!isEntityRemote && IsSleeper)
    //    {
    //        world.NotifySleeperVolumesEntityDied(this);
    //    }
    //}

    //protected override void ClientKill(DamageResponse _dmResponse)
    //{
    //    lastHitDirection = Utils.EnumHitDirection.Back;
    //    if (entityThatKilledMe == null && _dmResponse.Source != null)
    //    {
    //        Entity entity = ((_dmResponse.Source.getEntityId() != -1) ? world.GetEntity(_dmResponse.Source.getEntityId()) : null);
    //        if (Spawned && entity is EntityAlive)
    //        {
    //            entityThatKilledMe = (EntityAlive)entity;
    //        }
    //    }

    //    if (!IsDead())
    //    {
    //        SetDead();
    //        if (Buffs != null)
    //        {
    //            Buffs.OnDeath(entityThatKilledMe, _dmResponse.Source != null && _dmResponse.Source.damageType == EnumDamageTypes.Crushing, (_dmResponse.Source == null) ? FastTags.Parse("crushing") : _dmResponse.Source.DamageTypeTag);
    //        }

    //        if (Progression != null)
    //        {
    //            Progression.OnDeath();
    //        }


    //        HandleClientDeath((_dmResponse.Source != null) ? _dmResponse.Source.BlockPosition : GetBlockPosition());
    //        OnEntityDeath();
    //        emodel.OnDeath(_dmResponse, world.ChunkClusters[0]);

    //    }
    //}

    //protected override void HandleClientDeath(Vector3i attackPos)
    //{
    //}

    //protected override void OnEntityTargeted(EntityAlive target)
    //{
    //}

    //public override void SetHoldingItemTransform(Transform _transform)
    //{
    //    emodel.SetInRightHand(_transform);
    //}

    //public override void OnHoldingItemChanged()
    //{
    //}

    //protected override void updateCameraPosition(bool _bLerpPosition)
    //{
    //}

    //public new float GetWetnessPercentage()
    //{
    //    float num = inWaterPercent;
    //    if (Stats.AmountEnclosed < WeatherParams.EnclosureDetectionThreshold)
    //    {
    //        num = Mathf.Max(num, WeatherManager.Instance.GetCurrentRainfallValue());
    //        num = Mathf.Max(num, WeatherManager.Instance.GetCurrentSnowfallValue());
    //    }

    //    return num;
    //}

    //protected override void OnHeadUnderwaterStateChanged(bool _bUnderwater)
    //{
    //    base.OnHeadUnderwaterStateChanged(_bUnderwater);
    //    if (_bUnderwater)
    //    {
    //        FireEvent(MinEventTypes.onSelfWaterSubmerge);
    //    }
    //    else
    //    {
    //        FireEvent(MinEventTypes.onSelfWaterSurface);
    //    }
    //}

    //public override bool CanNavigatePath()
    //{
    //    if (!onGround && !isSwimming && !bInElevator)
    //    {
    //        return Climbing;
    //    }

    //    return true;
    //}

    //public override void StartSpecialAttack(int _animType)
    //{
    //    if (emodel.avatarController != null && !emodel.avatarController.IsAnimationSpecialAttackPlaying())
    //    {
    //        bPlayerStatsChanged |= !isEntityRemote;
    //        emodel.avatarController.StartAnimationSpecialAttack(_b: true, _animType);
    //    }
    //}

    //public override void StartHarvestingAnim()
    //{
    //    if (emodel != null && emodel.avatarController != null)
    //    {
    //        emodel.avatarController.StartAnimationHarvesting();
    //    }
    //}

    //public override void SetVehicleAnimation(int _animHash, bool _isActive)
    //{
    //    if ((bool)emodel && (bool)emodel.avatarController)
    //    {
    //        emodel.avatarController.SetVehicleAnimation(_animHash, _isActive);
    //    }
    //}

    //public override bool IsSpawned()
    //{
    //    return bSpawned;
    //}

    //public override void RemoveIKTargets()
    //{
    //    emodel.RemoveIKController();
    //}

    //public override void SetIKTargets(List<IKController.Target> targets)
    //{
    //    IKController iKController = emodel.AddIKController();
    //    if ((bool)iKController)
    //    {
    //        iKController.SetTargets(targets);
    //    }
    //}

    //public override List<Vector3i> GetDroppedBackpackPositions()
    //{
    //    return droppedBackpackPositions;
    //}

    //public override Vector3i GetLastDroppedBackpackPosition()
    //{
    //    if (droppedBackpackPositions == null)
    //    {
    //        return Vector3i.zero;
    //    }

    //    if (droppedBackpackPositions.Count == 0)
    //    {
    //        return Vector3i.zero;
    //    }

    //    return droppedBackpackPositions[0];
    //}

    //public override bool EqualsDroppedBackpackPositions(Vector3i position)
    //{
    //    if (droppedBackpackPositions != null)
    //    {
    //        foreach (Vector3i droppedBackpackPosition in droppedBackpackPositions)
    //        {
    //            if (position.Equals(droppedBackpackPosition))
    //            {
    //                return true;
    //            }
    //        }
    //    }

    //    return false;
    //}

    //public override void SetDroppedBackpackPositions(List<Vector3i> positions)
    //{
    //    droppedBackpackPositions.Clear();
    //    droppedBackpackPositions.AddRange(positions);
    //}

    //public override int GetMaxHealth()
    //{
    //    return (int)Stats.Health.Max;
    //}

    //public override int GetMaxStamina()
    //{
    //    return (int)Stats.Stamina.Max;
    //}

    //public override int GetMaxWater()
    //{
    //    return (int)Stats.Water.Max;
    //}

    //public override float GetStaminaMultiplier()
    //{
    //    return 1f;
    //}

    //protected override void SetMovementState()
    //{
    //    float num = speedStrafe;
    //    if (num >= 1234f)
    //    {
    //        num = 0f;
    //    }

    //    float num2 = speedForward * speedForward + num * num;
    //    MovementState = ((num2 > moveSpeedAggro * moveSpeedAggro) ? 3 : ((num2 > moveSpeed * moveSpeed) ? 2 : ((num2 > 0.001f) ? 1 : 0)));
    //}

    //public override void OnUpdateLive()
    //{
    //    Stats.Health.RegenerationAmount = 0f;
    //    Stats.Stamina.RegenerationAmount = 0f;
    //    if (!isEntityRemote && !IsDead())
    //    {
    //        Stats.Update(0.05f, world.worldTime);
    //    }

    //    if (jumpTicks > 0)
    //    {
    //        jumpTicks--;
    //    }

    //    if (attackTargetTime > 0)
    //    {
    //        attackTargetTime--;
    //        if (attackTarget != null && attackTargetTime == 0)
    //        {
    //            attackTarget = null;
    //            if (!isEntityRemote)
    //            {
    //                world.entityDistributer.SendPacketToTrackedPlayersAndTrackedEntity(entityId, -1, NetPackageManager.GetPackage<NetPackageSetAttackTarget>().Setup(entityId, -1));
    //            }
    //        }
    //    }

    //    updateCurrentBlockPosAndValue();
    //    if (AttachedToEntity == null)
    //    {
    //        if (isEntityRemote)
    //        {
    //            if (RootMotion)
    //            {
    //                MoveEntityHeaded(Vector3.zero, _isDirAbsolute: false);
    //            }
    //        }
    //        else
    //        {
    //            if (Health <= 0)
    //            {
    //                bJumping = false;
    //                bClimbing = false;
    //                moveDirection = Vector3.zero;
    //            }
    //            else if (!world.IsRemote() && !IsDead() && !IsClientControlled() && hasAI)
    //            {
    //                updateTasks();
    //            }

    //            noisePlayer = null;
    //            noisePlayerDistance = 0f;
    //            noisePlayerVolume = 0f;
    //            if (bJumping)
    //            {
    //                UpdateJump();
    //            }
    //            else
    //            {
    //                jumpTicks = 0;
    //            }

    //            float num = landMovementFactor;
    //            landMovementFactor *= GetSpeedModifier();
    //            MoveEntityHeaded(moveDirection, isMoveDirAbsolute);
    //            landMovementFactor = num;
    //        }

    //        if (moveDirection.x > 0f || moveDirection.z > 0f)
    //        {
    //            if (bMovementRunning)
    //            {
    //                CurrentMovementTag = MovementTagRunning;
    //            }
    //            else
    //            {
    //                CurrentMovementTag = MovementTagWalking;
    //            }
    //        }
    //        else
    //        {
    //            CurrentMovementTag = MovementTagIdle;
    //        }
    //    }

    //    if (bodyDamage.CurrentStun != 0 && !emodel.IsRagdollActive && !IsDead())
    //    {
    //        if (bodyDamage.CurrentStun == EnumEntityStunType.Getup)
    //        {
    //            if (!emodel.avatarController || !emodel.avatarController.IsAnimationStunRunning())
    //            {
    //                ClearStun();
    //            }
    //        }
    //        else
    //        {
    //            bodyDamage.StunDuration -= 0.05f;
    //            if (bodyDamage.StunDuration <= 0f)
    //            {
    //                SetStun(EnumEntityStunType.Getup);
    //                if ((bool)emodel.avatarController)
    //                {
    //                    emodel.avatarController.EndStun();
    //                }
    //            }
    //        }
    //    }

    //    proneRefillCounter += 0.05f * proneRefillRate;
    //    while (proneRefillCounter >= 1f)
    //    {
    //        bodyDamage.StunProne = Mathf.Max(0, bodyDamage.StunProne - 1);
    //        proneRefillCounter -= 1f;
    //    }

    //    kneelRefillCounter += 0.05f * kneelRefillRate;
    //    while (kneelRefillCounter >= 1f)
    //    {
    //        bodyDamage.StunKnee = Mathf.Max(0, bodyDamage.StunKnee - 1);
    //        kneelRefillCounter -= 1f;
    //    }

    //    EntityPlayer primaryPlayer = world.GetPrimaryPlayer();
    //    if (primaryPlayer != null && primaryPlayer != this)
    //    {
    //        if (--ticksToCheckSeenByPlayer <= 0)
    //        {
    //            wasSeenByPlayer = primaryPlayer.CanSee(this);
    //            if (wasSeenByPlayer)
    //            {
    //                ticksToCheckSeenByPlayer = 200;
    //            }
    //            else
    //            {
    //                ticksToCheckSeenByPlayer = 20;
    //            }
    //        }
    //        else if (wasSeenByPlayer)
    //        {
    //            primaryPlayer.SetCanSee(this);
    //        }
    //    }

    //    if (onGround)
    //    {
    //        disableFallBehaviorUntilOnGround = false;
    //    }

    //    UpdateDynamicRagdoll();
    //    checkForTeleportOutOfTraderArea();
    //}


    //private Vector3 GetTeleportPosition(Vector3 _position, Vector3 _direction, float _directionIncrease = 5f, int _maxAttempts = 20)
    //{
    //    Vector3 result = _position;
    //    Vector3 _position2 = _position;
    //    bool flag = false;
    //    int num = 0;
    //    while (!flag && num < _maxAttempts)
    //    {
    //        flag = world.GetRandomSpawnPositionMinMaxToPosition(_position, 5, 10, 1, _checkBedrolls: false, out _position2, _isPlayer: true, _checkWater: true, 20);
    //        _position += _direction * _directionIncrease;
    //        num++;
    //    }

    //    if (flag)
    //    {
    //        return _position2;
    //    }

    //    Log.Warning("Trader teleport: Could not find a valid teleport position, returning original position");
    //    return result;
    //}

    //protected override void StartJump()
    //{
    //    jumpState = JumpState.Leap;
    //    jumpStateTicks = 0;
    //    jumpDistance = 1f;
    //    jumpHeightDiff = 0f;
    //    disableFallBehaviorUntilOnGround = true;
    //    if (isSwimming)
    //    {
    //        jumpState = JumpState.SwimStart;
    //        if (emodel.avatarController != null)
    //        {
    //            emodel.avatarController.SetSwim(_enable: true);
    //        }
    //    }
    //    else if (emodel.avatarController != null)
    //    {
    //        emodel.avatarController.StartAnimationJump(AnimJumpMode.Start);
    //    }
    //}

    //public override void SetJumpDistance(float _distance, float _heightDiff)
    //{
    //    jumpDistance = _distance;
    //    jumpHeightDiff = _heightDiff;
    //}

    //public override void SetSwimValues(float _durationTicks, Vector3 _motion)
    //{
    //    jumpSwimDurationTicks = Mathf.Clamp(_durationTicks / swimSpeed - 6f, 3f, 20f);
    //    jumpSwimMotion = _motion;
    //}

    //protected override void UpdateJump()
    //{
    //    if (IsFlyMode.Value)
    //    {
    //        Jumping = false;
    //        return;
    //    }

    //    jumpStateTicks++;
    //    switch (jumpState)
    //    {
    //        case JumpState.Leap:
    //            if (accumulatedRootMotion.y > 0.005f || (float)jumpStateTicks >= jumpDelay)
    //            {
    //                StartJumpMotion();
    //                jumpTicks = 200;
    //                jumpState = JumpState.Air;
    //                jumpStateTicks = 0;
    //                jumpIsMoving = true;
    //            }

    //            break;
    //        case JumpState.Air:
    //            if (onGround || (motionMultiplier < 0.45f && jumpStateTicks > 40))
    //            {
    //                jumpState = JumpState.Land;
    //                jumpStateTicks = 0;
    //                jumpIsMoving = false;
    //            }

    //            break;
    //        case JumpState.Land:
    //            if (jumpStateTicks > 5)
    //            {
    //                Jumping = false;
    //            }

    //            break;
    //        case JumpState.SwimStart:
    //            if ((float)jumpStateTicks > 6f)
    //            {
    //                jumpTicks = 100;
    //                jumpState = JumpState.Swim;
    //                jumpStateTicks = 0;
    //                jumpIsMoving = true;
    //                StartJumpSwimMotion();
    //            }

    //            break;
    //        case JumpState.Swim:
    //            if (!isSwimming || (float)jumpStateTicks >= jumpSwimDurationTicks)
    //            {
    //                Jumping = false;
    //            }

    //            break;
    //    }
    //}

    //protected override void StartJumpSwimMotion()
    //{
    //    if (inWaterPercent > 0.65f)
    //    {
    //        float num = Mathf.Sqrt(jumpSwimMotion.x * jumpSwimMotion.x + jumpSwimMotion.z * jumpSwimMotion.z) + 0.001f;
    //        float min = Mathf.Lerp(-0.6f, -0.05f, num * 0.8f);
    //        jumpSwimMotion.y = Utils.FastClamp(jumpSwimMotion.y, min, 1f);
    //        float num2 = jumpSwimDurationTicks;
    //        float num3 = (num2 - 1f) * world.Gravity * 0.025f * 0.4999f;
    //        num3 /= Mathf.Pow(0.91f, (num2 - 3f) * 0.91f * 0.115f);
    //        float t = (num2 - 1f) / 15f;
    //        float num4 = Mathf.LerpUnclamped(0.46f, 0.418600023f, t);
    //        float num5 = Mathf.Pow(0.91f, (num2 - 1f) * num4);
    //        float num6 = 1f / num2 / num5;
    //        num3 += jumpSwimMotion.y * num6;
    //        num6 /= Utils.FastMax(1f, num);
    //        motion.x = jumpSwimMotion.x * num6;
    //        motion.z = jumpSwimMotion.z * num6;
    //        motion.y = num3;
    //    }
    //    else
    //    {
    //        motion.y = 0f;
    //    }
    //}


    //protected override void StartJumpMotion()
    //{
    //    SetAirBorne(_b: true);
    //    float num = (int)(5f + Mathf.Pow(jumpDistance * 8f, 0.5f));
    //    motion = GetForwardVector() * (jumpDistance / num);
    //    float num2 = num * world.Gravity * 0.5f;
    //    motion.y = Utils.FastMax(num2 * 0.5f, num2 + jumpHeightDiff / num);
    //}

    //private void JumpMove()
    //{
    //    accumulatedRootMotion = Vector3.zero;
    //    Vector3 vector = motion;
    //    entityCollision(motion);
    //    motion.x = vector.x;
    //    motion.z = vector.z;
    //    if (motion.y != 0f)
    //    {
    //        motion.y = vector.y;
    //    }

    //    if (jumpState == JumpState.Air)
    //    {
    //        motion.y -= world.Gravity;
    //        return;
    //    }

    //    motion.x *= 0.91f;
    //    motion.z *= 0.91f;
    //    motion.y -= world.Gravity * 0.025f;
    //    motion.y *= 0.91f;
    //}

    //protected override void EndJump()
    //{
    //    jumpState = JumpState.Off;
    //    jumpIsMoving = false;
    //}

    //protected override bool CalcIfSwimming()
    //{
    //    float num = ((onGround || Jumping) ? 0.7f : 0.5f);
    //    return inWaterPercent >= num;
    //}

    //public override void SwimChanged()
    //{
    //    if ((bool)emodel.avatarController)
    //    {
    //        emodel.avatarController.SetSwim(isSwimming);
    //    }
    //}

    //protected override void Update()
    //{
    //    base.Update();
    //    if (!isEntityRemote && RootMotion && lerpForwardSpeed)
    //    {
    //        if (speedForwardTarget > 0f)
    //        {
    //            speedForward = Mathf.Lerp(speedForward, speedForwardTarget, Time.deltaTime * 5f);
    //        }
    //        else
    //        {
    //            speedForward = Mathf.Lerp(speedForward, speedForwardTarget, Time.deltaTime * 3.8f);
    //        }
    //    }

    //    if (isHeadUnderwater != (Buffs.GetCustomVar("_underwater") == 1f))
    //    {
    //        Buffs.SetCustomVar("_underwater", isHeadUnderwater ? 1 : 0);
    //    }

    //    MinEventContext.Area = boundingBox;
    //    MinEventContext.Biome = biomeStandingOn;
    //    MinEventContext.ItemValue = inventory.holdingItemItemValue;
    //    MinEventContext.BlockValue = blockValueStandingOn;
    //    MinEventContext.ItemInventoryData = inventory.holdingItemData;
    //    MinEventContext.Position = position;
    //    MinEventContext.Seed = entityId + Mathf.Abs(GameManager.Instance.World.Seed);
    //    MinEventContext.Transform = base.transform;
    //    FastTags.CombineTags(EntityClass.list[entityClass].Tags, inventory.holdingItem.ItemTags, CurrentStanceTag, CurrentMovementTag, ref MinEventContext.Tags);
    //    if (Progression != null)
    //    {
    //        Progression.Update();
    //    }
    //}

    //public override void OnDeathUpdate()
    //{
    //    if (deathUpdateTime < timeStayAfterDeath)
    //    {
    //        deathUpdateTime++;
    //    }

    //    int deadBodyHitPoints = EntityClass.list[entityClass].DeadBodyHitPoints;
    //    if (deadBodyHitPoints > 0 && DeathHealth <= -deadBodyHitPoints)
    //    {
    //        deathUpdateTime = timeStayAfterDeath;
    //    }

    //    if (deathUpdateTime >= timeStayAfterDeath && !isEntityRemote && !markedForUnload)
    //    {
    //        dropCorpseBlock();
    //        if (particleOnDestroy != null && particleOnDestroy.Length > 0)
    //        {
    //            float lightBrightness = world.GetLightBrightness(GetBlockPosition());
    //            world.GetGameManager().SpawnParticleEffectServer(new ParticleEffect(particleOnDestroy, getHeadPosition(), lightBrightness, Color.white, null, null, _OLDCreateColliders: false), entityId);
    //        }
    //    }
    //}

    //protected override Vector3i dropCorpseBlock()
    //{
    //    if (corpseBlockValue.isair)
    //    {
    //        return Vector3i.zero;
    //    }

    //    if (rand.RandomFloat > corpseBlockChance)
    //    {
    //        return Vector3i.zero;
    //    }

    //    Vector3i vector3i = World.worldToBlockPos(position);
    //    while (vector3i.y < 254 && (float)vector3i.y - position.y < 3f && !corpseBlockValue.Block.CanPlaceBlockAt(world, 0, vector3i, corpseBlockValue))
    //    {
    //        vector3i += Vector3i.up;
    //    }

    //    if (vector3i.y >= 254)
    //    {
    //        return Vector3i.zero;
    //    }

    //    if ((float)vector3i.y - position.y >= 2.1f)
    //    {
    //        return Vector3i.zero;
    //    }

    //    world.SetBlockRPC(vector3i, corpseBlockValue);
    //    return vector3i;
    //}

    //public new void NotifyRootMotion(Animator animator)
    //{
    //    accumulatedRootMotion += animator.deltaPosition;
    //}

    //protected new void DefaultMoveEntity(Vector3 _direction, bool _isDirAbsolute)
    //{
    //    float num = 0.91f;
    //    if (onGround)
    //    {
    //        num = 0.546f;
    //    }

    //    if (!RootMotion || (!onGround && jumpTicks > 0))
    //    {
    //        float num2;
    //        if (onGround)
    //        {
    //            num2 = landMovementFactor;
    //            float num3 = 0.163f / (num * num * num);
    //            num2 *= num3;
    //        }
    //        else
    //        {
    //            num2 = jumpMovementFactor;
    //        }

    //        Move(_direction, _isDirAbsolute, num2, MaxVelocity);
    //    }

    //    if (Climbing)
    //    {
    //        fallDistance = 0f;
    //        entityCollision(motion);
    //        distanceClimbed += motion.magnitude;
    //        if (distanceClimbed > 0.5f)
    //        {
    //            internalPlayStepSound();
    //            distanceClimbed = 0f;
    //        }
    //    }
    //    else
    //    {
    //        if (IsInElevator())
    //        {
    //            if (!RootMotion)
    //            {
    //                float num4 = 0.15f;
    //                if (motion.x < 0f - num4)
    //                {
    //                    motion.x = 0f - num4;
    //                }

    //                if (motion.x > num4)
    //                {
    //                    motion.x = num4;
    //                }

    //                if (motion.z < 0f - num4)
    //                {
    //                    motion.z = 0f - num4;
    //                }

    //                if (motion.z > num4)
    //                {
    //                    motion.z = num4;
    //                }
    //            }

    //            fallDistance = 0f;
    //        }

    //        if (IsSleeping)
    //        {
    //            motion.x = 0f;
    //            motion.z = 0f;
    //        }

    //        entityCollision(motion);
    //    }

    //    if (isSwimming)
    //    {
    //        motion.x *= 0.91f;
    //        motion.z *= 0.91f;
    //        motion.y -= world.Gravity * 0.025f;
    //        motion.y *= 0.91f;
    //        return;
    //    }

    //    motion.x *= num;
    //    motion.z *= num;
    //    if (!bInElevator)
    //    {
    //        motion.y -= world.Gravity;
    //    }

    //    motion.y *= 0.98f;
    //}

    //public override void MoveEntityHeaded(Vector3 _direction, bool _isDirAbsolute)
    //{
    //    if (AttachedToEntity != null)
    //    {
    //        return;
    //    }

    //    if (jumpIsMoving)
    //    {
    //        JumpMove();
    //        return;
    //    }

    //    if (RootMotion)
    //    {
    //        if (isEntityRemote && bodyDamage.CurrentStun == EnumEntityStunType.None && !IsDead() && (!(emodel != null) || !(emodel.avatarController != null) || !emodel.avatarController.IsAnimationHitRunning()))
    //        {
    //            accumulatedRootMotion = Vector3.zero;
    //            return;
    //        }

    //        bool flag = (bool)emodel && emodel.IsRagdollActive;
    //        if (isSwimming && !flag)
    //        {
    //            motion += accumulatedRootMotion * 0.001f;
    //        }
    //        else if (onGround || jumpTicks > 0)
    //        {
    //            if (flag)
    //            {
    //                motion.x = 0f;
    //                motion.z = 0f;
    //            }
    //            else
    //            {
    //                float y = motion.y;
    //                motion = accumulatedRootMotion;
    //                motion.y += y;
    //            }
    //        }

    //        accumulatedRootMotion = Vector3.zero;
    //    }

    //    if (IsFlyMode.Value)
    //    {
    //        EntityPlayerLocal primaryPlayer = GameManager.Instance.World.GetPrimaryPlayer();
    //        float num = ((primaryPlayer != null) ? primaryPlayer.GodModeSpeedModifier : 1f);
    //        float num2 = 2f * (MovementRunning ? 0.35f : 0.12f) * num;
    //        if (!RootMotion)
    //        {
    //            Move(_direction, _isDirAbsolute, GetPassiveEffectSpeedModifier() * num2, GetPassiveEffectSpeedModifier() * num2);
    //        }

    //        if (!IsNoCollisionMode.Value)
    //        {
    //            entityCollision(motion);
    //            motion *= ConditionalScalePhysicsMulConstant(0.546f);
    //        }
    //        else
    //        {
    //            SetPosition(position + motion);
    //            motion = Vector3.zero;
    //        }
    //    }
    //    else
    //    {
    //        DefaultMoveEntity(_direction, _isDirAbsolute);
    //    }

    //    if (isEntityRemote || !RootMotion)
    //    {
    //        return;
    //    }

    //    float num3 = landMovementFactor;
    //    num3 *= 2.5f;
    //    if (inWaterPercent > 0.3f)
    //    {
    //        if (num3 > 0.01f)
    //        {
    //            float t = (inWaterPercent - 0.3f) * 1.42857146f;
    //            num3 = Mathf.Lerp(num3, 0.01f + (num3 - 0.01f) * 0.1f, t);
    //        }

    //        if (isSwimming)
    //        {
    //            num3 = landMovementFactor * 5f;
    //        }
    //    }

    //    float magnitude = _direction.magnitude;
    //    magnitude = Mathf.Max(magnitude, 1f);
    //    float num4 = num3 / magnitude;
    //    if (lerpForwardSpeed)
    //    {
    //        speedForwardTarget = _direction.z * num4;
    //    }
    //    else
    //    {
    //        speedForward = _direction.z * num4;
    //    }

    //    speedStrafe = _direction.x * num4;
    //    SetMovementState();
    //    ReplicateSpeeds();
    //}

    //public new float GetPassiveEffectSpeedModifier()
    //{
    //    if (IsCrouching)
    //    {
    //        if (MovementRunning)
    //        {
    //            return EffectManager.GetValue(PassiveEffects.WalkSpeed, null, Constants.cPlayerSpeedModifierWalking, this);
    //        }

    //        return EffectManager.GetValue(PassiveEffects.CrouchSpeed, null, Constants.cPlayerSpeedModifierCrouching, this);
    //    }

    //    if (MovementRunning)
    //    {
    //        return EffectManager.GetValue(PassiveEffects.RunSpeed, null, Constants.cPlayerSpeedModifierRunning, this);
    //    }

    //    return EffectManager.GetValue(PassiveEffects.WalkSpeed, null, Constants.cPlayerSpeedModifierWalking, this);
    //}

    //public new void SetMoveForward(float _moveForward)
    //{
    //    moveDirection.x = 0f;
    //    moveDirection.z = _moveForward;
    //    isMoveDirAbsolute = false;
    //    Climbing = false;
    //    lerpForwardSpeed = true;
    //    motion.x = 0f;
    //    motion.z = 0f;
    //    accumulatedRootMotion.x = 0f;
    //    accumulatedRootMotion.z = 0f;
    //    if (bInElevator)
    //    {
    //        motion.y = 0f;
    //    }
    //}

    //public new void SetMoveForwardWithModifiers(float _speedModifier, float _speedScale, bool _climb)
    //{
    //    moveDirection.x = 0f;
    //    moveDirection.z = 1f;
    //    isMoveDirAbsolute = false;
    //    Climbing = _climb;
    //    lerpForwardSpeed = true;
    //    float num = speedModifier;
    //    speedModifier = _speedModifier * _speedScale;
    //    if (num > 0.2f)
    //    {
    //        num = speedModifier / num;
    //        accumulatedRootMotion.x *= num;
    //        accumulatedRootMotion.z *= num;
    //    }
    //}

    //public new void AddMotion(float dir, float speed)
    //{
    //    float f = dir * ((float)Math.PI / 180f);
    //    accumulatedRootMotion.x += Mathf.Sin(f) * speed;
    //    accumulatedRootMotion.z += Mathf.Cos(f) * speed;
    //}

    //public new void MakeMotionMoveToward(float x, float z, float minMotion, float maxMotion)
    //{
    //    if (RootMotion)
    //    {
    //        float num = Mathf.Sqrt(x * x + z * z);
    //        if (num > 0f)
    //        {
    //            num = Utils.FastClamp(Mathf.Sqrt(accumulatedRootMotion.x * accumulatedRootMotion.x + accumulatedRootMotion.z * accumulatedRootMotion.z), minMotion, maxMotion) / num;
    //            if (num < 1f)
    //            {
    //                x *= num;
    //                z *= num;
    //            }
    //        }

    //        accumulatedRootMotion.x = x;
    //        accumulatedRootMotion.z = z;
    //    }
    //    else
    //    {
    //        moveDirection.x = x;
    //        moveDirection.z = z;
    //        isMoveDirAbsolute = true;
    //    }
    //}

    //public new bool IsInFrontOfMe(Vector3 _position)
    //{
    //    Vector3 headPosition = getHeadPosition();
    //    Vector3 dir = _position - headPosition;
    //    Vector3 forwardVector = GetForwardVector();
    //    float angleBetween = Utils.GetAngleBetween(dir, forwardVector);
    //    float num = GetMaxViewAngle() * 0.5f;
    //    if (angleBetween < 0f - num || angleBetween > num)
    //    {
    //        return false;
    //    }

    //    return true;
    //}

    //public new bool IsInViewCone(Vector3 _position)
    //{
    //    Vector3 headPosition = getHeadPosition();
    //    Vector3 dir = _position - headPosition;
    //    Vector3 lookVector;
    //    float num;
    //    if (IsSleeping)
    //    {
    //        lookVector = sleeperLookDir;
    //        num = sleeperViewAngle;
    //    }
    //    else
    //    {
    //        lookVector = GetLookVector();
    //        num = GetMaxViewAngle();
    //    }

    //    num *= 0.5f;
    //    float angleBetween = Utils.GetAngleBetween(dir, lookVector);
    //    if (angleBetween < 0f - num || angleBetween > num)
    //    {
    //        return false;
    //    }

    //    return true;
    //}

    //public new void DrawViewCone()
    //{
    //    Vector3 lookVector;
    //    float num;
    //    if (IsSleeping)
    //    {
    //        lookVector = sleeperLookDir;
    //        num = sleeperViewAngle;
    //    }
    //    else
    //    {
    //        lookVector = GetLookVector();
    //        num = GetMaxViewAngle();
    //    }

    //    lookVector *= GetSeeDistance();
    //    num *= 0.5f;
    //    Vector3 start = getHeadPosition() - Origin.position;
    //    Debug.DrawRay(start, lookVector, new Color(0.9f, 0.9f, 0.5f), 0.1f);
    //    Vector3 dir = Quaternion.Euler(0f, 0f - num, 0f) * lookVector;
    //    Debug.DrawRay(start, dir, new Color(0.6f, 0.6f, 0.3f), 0.1f);
    //    Vector3 dir2 = Quaternion.Euler(0f, num, 0f) * lookVector;
    //    Debug.DrawRay(start, dir2, new Color(0.6f, 0.6f, 0.3f), 0.1f);
    //}

    //public new bool CanSee(Vector3 _pos)
    //{
    //    Vector3 headPosition = getHeadPosition();
    //    Vector3 direction = _pos - headPosition;
    //    float seeDistance = GetSeeDistance();
    //    if (direction.magnitude > seeDistance)
    //    {
    //        return false;
    //    }

    //    if (!IsInViewCone(_pos))
    //    {
    //        return false;
    //    }

    //    Ray ray = new Ray(headPosition, direction);
    //    ray.origin += direction.normalized * 0.2f;
    //    int modelLayer = GetModelLayer();
    //    SetModelLayer(2);
    //    bool result = true;
    //    if (Voxel.Raycast(world, ray, seeDistance, bHitTransparentBlocks: false, bHitNotCollidableBlocks: false))
    //    {
    //        result = false;
    //    }

    //    SetModelLayer(modelLayer);
    //    return result;
    //}

    //public new bool CanEntityBeSeen(Entity _other)
    //{
    //    Vector3 headPosition = getHeadPosition();
    //    Vector3 headPosition2 = _other.getHeadPosition();
    //    Vector3 direction = headPosition2 - headPosition;
    //    float magnitude = direction.magnitude;
    //    float num = GetSeeDistance();
    //    EntityPlayer entityPlayer = _other as EntityPlayer;
    //    if ((object)entityPlayer != null)
    //    {
    //        num *= entityPlayer.DetectUsScale(this);
    //    }

    //    if (magnitude > num)
    //    {
    //        return false;
    //    }

    //    if (!IsInViewCone(headPosition2))
    //    {
    //        return false;
    //    }

    //    Ray ray = new Ray(headPosition, direction);
    //    ray.origin += direction.normalized * 0.2f;
    //    int modelLayer = GetModelLayer();
    //    SetModelLayer(2);
    //    bool num2 = Voxel.Raycast(world, ray, num, bHitTransparentBlocks: false, bHitNotCollidableBlocks: false);
    //    bool result = false;
    //    if (num2)
    //    {
    //        if (Voxel.voxelRayHitInfo.tag == "E_Vehicle")
    //        {
    //            EntityVehicle entityVehicle = EntityVehicle.FindCollisionEntity(Voxel.voxelRayHitInfo.transform);
    //            if ((bool)entityVehicle && entityVehicle.IsAttached(_other))
    //            {
    //                result = true;
    //            }
    //        }
    //        else
    //        {
    //            if (Voxel.voxelRayHitInfo.tag.StartsWith("E_BP_"))
    //            {
    //                Voxel.voxelRayHitInfo.transform = GameUtils.GetHitRootTransform(Voxel.voxelRayHitInfo.tag, Voxel.voxelRayHitInfo.transform);
    //            }

    //            if (_other.transform == Voxel.voxelRayHitInfo.transform)
    //            {
    //                result = true;
    //            }
    //        }
    //    }

    //    SetModelLayer(modelLayer);
    //    return result;
    //}

    //public override float GetSeeDistance()
    //{
    //    senseScale = 1f;
    //    if (IsSleeping)
    //    {
    //        sightRange = sleeperSightRange;
    //        return sleeperSightRange;
    //    }

    //    sightRange = sightRangeBase;
    //    if (aiManager != null)
    //    {
    //        float num = EAIManager.CalcSenseScale();
    //        senseScale = 1f + num * aiManager.feralSense;
    //        sightRange = sightRangeBase * senseScale;
    //    }

    //    return sightRange;
    //}

    //public new bool CanSeeStealth(float dist, float lightLevel)
    //{
    //    float t = dist / sightRange;
    //    float num = Mathf.Lerp(sightLightThreshold.x, sightLightThreshold.y, t);
    //    if (lightLevel > num)
    //    {
    //        return true;
    //    }

    //    return false;
    //}

    //public new float GetSeeStealthDebugScale(float dist)
    //{
    //    float t = dist / sightRange;
    //    return Mathf.Lerp(sightLightThreshold.x, sightLightThreshold.y, t);
    //}

    //public override bool IsAlive()
    //{
    //    return !IsDead();
    //}

    //public override void SetAlive()
    //{
    //    IsDead();
    //    base.SetAlive();
    //    if (!isEntityRemote)
    //    {
    //        Stats.ResetStats();
    //    }

    //    Stats.Health.MaxModifier = 0f;
    //    Health = (int)Stats.Health.ModifiedMax;
    //    Stamina = Stats.Stamina.ModifiedMax;
    //    deathUpdateTime = 0;
    //    bDead = false;
    //    RecordedDamage.Fatal = false;
    //    emodel.SetAlive();
    //}

    //public new float YawForTarget(Entity _otherEntity)
    //{
    //    return YawForTarget(_otherEntity.GetPosition());
    //}

    //public new float YawForTarget(Vector3 target)
    //{
    //    float num = target.x - position.x;
    //    return 0f - (float)(Math.Atan2(target.z - position.z, num) * 180.0 / Math.PI) + 90f;
    //}

    //public new void RotateTo(Entity _otherEntity, float _dYaw, float _dPitch)
    //{
    //    float num = _otherEntity.position.x - position.x;
    //    float num2 = _otherEntity.position.z - position.z;
    //    float num3;
    //    if (_otherEntity is EntityAlive)
    //    {
    //        EntityAlive entityAlive = (EntityAlive)_otherEntity;
    //        num3 = position.y + GetEyeHeight() - (entityAlive.position.y + entityAlive.GetEyeHeight());
    //    }
    //    else
    //    {
    //        num3 = (_otherEntity.boundingBox.min.y + _otherEntity.boundingBox.max.y) / 2f - (position.y + GetEyeHeight());
    //    }

    //    float num4 = Mathf.Sqrt(num * num + num2 * num2);
    //    float intendedRotation = 0f - (float)(Math.Atan2(num2, num) * 180.0 / Math.PI) + 90f;
    //    float intendedRotation2 = (float)(0.0 - Math.Atan2(num3, num4) * 180.0 / Math.PI);
    //    rotation.x = UpdateRotation(rotation.x, intendedRotation2, _dPitch);
    //    rotation.y = UpdateRotation(rotation.y, intendedRotation, _dYaw);
    //}

    //public new void RotateTo(float _x, float _y, float _z, float _dYaw, float _dPitch)
    //{
    //    float num = _x - position.x;
    //    float num2 = _z - position.z;
    //    float num3 = Mathf.Sqrt(num * num + num2 * num2);
    //    float intendedRotation = 0f - (float)(Math.Atan2(num2, num) * 180.0 / Math.PI) + 90f;
    //    rotation.y = UpdateRotation(rotation.y, intendedRotation, _dYaw);
    //    if (_dPitch > 0f)
    //    {
    //        float intendedRotation2 = (float)(0.0 - Math.Atan2(_y - position.y, num3) * 180.0 / Math.PI);
    //        rotation.x = 0f - UpdateRotation(rotation.x, intendedRotation2, _dPitch);
    //    }
    //}

    //public override float GetEyeHeight()
    //{
    //    if (walkType == 21)
    //    {
    //        return 0.15f;
    //    }

    //    if (walkType == 22)
    //    {
    //        return 0.6f;
    //    }

    //    if (!IsCrouching)
    //    {
    //        return base.height * 0.8f;
    //    }

    //    return base.height * 0.5f;
    //}

    //public override float GetSpeedModifier()
    //{
    //    return speedModifier;
    //}

    //protected override void fallHitGround(float _distance, Vector3 _fallMotion)
    //{
    //    base.fallHitGround(_distance, _fallMotion);
    //    if (_distance > 2f)
    //    {
    //        int num = (int)((0f - _fallMotion.y - 0.85f) * 160f);
    //        if (num > 0)
    //        {
    //            DamageEntity(DamageSource.fall, num, _criticalHit: false);
    //        }

    //        PlayHitGroundSound();
    //    }

    //    if (!IsDead() && !emodel.IsRagdollActive && (disableFallBehaviorUntilOnGround || !ChooseFallBehavior(_distance, _fallMotion)) && (bool)emodel && (bool)emodel.avatarController)
    //    {
    //        emodel.avatarController.StartAnimationJump(AnimJumpMode.Land);
    //    }

    //    if (aiManager != null)
    //    {
    //        aiManager.FallHitGround(_distance);
    //    }
    //}

    //public new bool NotifyDestroyedBlock(ItemActionAttack.AttackHitInfo attackHitInfo)
    //{
    //    if (attackHitInfo != null && moveHelper != null && moveHelper.IsBlocked)
    //    {
    //        if (moveHelper.HitInfo != null && moveHelper.HitInfo.hit.blockPos == attackHitInfo.hitPosition)
    //        {
    //            moveHelper.ClearBlocked();
    //        }

    //        if (_destroyBlockBehaviors.Count == 0)
    //        {
    //            return false;
    //        }

    //        float num = 0f;
    //        weightBehaviorTemp.Clear();
    //        int @int = GameStats.GetInt(EnumGameStats.GameDifficulty);
    //        WeightBehavior item = default(WeightBehavior);
    //        for (int i = 0; i < _destroyBlockBehaviors.Count; i++)
    //        {
    //            DestroyBlockBehavior destroyBlockBehavior = _destroyBlockBehaviors[i];
    //            if (@int >= destroyBlockBehavior.Difficulty.min && @int <= destroyBlockBehavior.Difficulty.max)
    //            {
    //                item.weight = destroyBlockBehavior.Weight + num;
    //                item.index = i;
    //                weightBehaviorTemp.Add(item);
    //                num += destroyBlockBehavior.Weight;
    //            }
    //        }

    //        bool result = false;
    //        if (num > 0f)
    //        {
    //            DestroyBlockBehavior destroyBlockBehavior2 = null;
    //            float num2 = rand.RandomFloat * num;
    //            for (int j = 0; j < weightBehaviorTemp.Count; j++)
    //            {
    //                if (num2 <= weightBehaviorTemp[j].weight)
    //                {
    //                    destroyBlockBehavior2 = _destroyBlockBehaviors[weightBehaviorTemp[j].index];
    //                    break;
    //                }
    //            }

    //            if (destroyBlockBehavior2 != null)
    //            {
    //                result = ExecuteDestroyBlockBehavior(destroyBlockBehavior2, attackHitInfo);
    //            }
    //        }

    //        return result;
    //    }

    //    return false;
    //}

    //private bool ChooseFallBehavior(float _distance, Vector3 _fallMotion)
    //{
    //    if (fallBehaviors.Count == 0)
    //    {
    //        return false;
    //    }

    //    float num = 0f;
    //    weightBehaviorTemp.Clear();
    //    int @int = GameStats.GetInt(EnumGameStats.GameDifficulty);
    //    WeightBehavior item = default(WeightBehavior);
    //    for (int i = 0; i < fallBehaviors.Count; i++)
    //    {
    //        FallBehavior fallBehavior = fallBehaviors[i];
    //        if (!(_distance < fallBehavior.Height.min) && !(_distance > fallBehavior.Height.max) && @int >= fallBehavior.Difficulty.min && @int <= fallBehavior.Difficulty.max)
    //        {
    //            item.weight = fallBehavior.Weight + num;
    //            item.index = i;
    //            weightBehaviorTemp.Add(item);
    //            num += fallBehavior.Weight;
    //        }
    //    }

    //    bool result = false;
    //    if (num > 0f)
    //    {
    //        FallBehavior fallBehavior2 = null;
    //        float num2 = rand.RandomFloat * num;
    //        for (int j = 0; j < weightBehaviorTemp.Count; j++)
    //        {
    //            if (num2 <= weightBehaviorTemp[j].weight)
    //            {
    //                fallBehavior2 = fallBehaviors[weightBehaviorTemp[j].index];
    //                break;
    //            }
    //        }

    //        if (fallBehavior2 != null)
    //        {
    //            result = ExecuteFallBehavior(fallBehavior2, _distance, _fallMotion);
    //        }
    //    }

    //    return result;
    //}

    //private void PlayHitGroundSound()
    //{
    //    if (soundLand == null || soundLand.Length == 0)
    //    {
    //        PlayOneShot("entityhitsground");
    //    }
    //    else
    //    {
    //        PlayOneShot(soundLand);
    //    }
    //}

    //public new int CalculateBlockDamage(BlockDamage block, int defaultBlockDamage, out bool bypassMaxDamage)
    //{
    //    if (stompsSpikes && block.HasTag(BlockTags.Spike))
    //    {
    //        bypassMaxDamage = true;
    //        return 999;
    //    }

    //    bypassMaxDamage = false;
    //    return defaultBlockDamage;
    //}

    //public override int DamageEntity(DamageSource _damageSource, int _strength, bool _criticalHit, float _impulseScale = 1f)
    //{
    //    EnumDamageSource source = _damageSource.GetSource();
    //    if (_damageSource.IsIgnoreConsecutiveDamages() && source != EnumDamageSource.Internal)
    //    {
    //        if (damageSourceTimeouts.ContainsKey(source) && GameTimer.Instance.ticks - damageSourceTimeouts[source] < 30)
    //        {
    //            return -1;
    //        }

    //        damageSourceTimeouts[source] = GameTimer.Instance.ticks;
    //    }

    //    EntityAlive entityAlive = world.GetEntity(_damageSource.getEntityId()) as EntityAlive;
    //    if (!FriendlyFireCheck(entityAlive))
    //    {
    //        return -1;
    //    }

    //    bool flag = _damageSource.GetDamageType() == EnumDamageTypes.Heat;
    //    if (!flag && (bool)entityAlive && (entityFlags & entityAlive.entityFlags & EntityFlags.Zombie) != 0)
    //    {
    //        return -1;
    //    }

    //    if (IsGodMode.Value)
    //    {
    //        return -1;
    //    }

    //    if (!IsDead() && (bool)entityAlive)
    //    {
    //        float value = EffectManager.GetValue(PassiveEffects.DamageBonus, null, 0f, entityAlive);
    //        if (value > 0f)
    //        {
    //            _damageSource.DamageMultiplier = value;
    //            _damageSource.BonusDamageType = EnumDamageBonusType.Sneak;
    //        }
    //    }

    //    float value2 = EffectManager.GetValue(PassiveEffects.GeneralDamageResist, null, 0f, this);
    //    float num = (float)_strength * value2 + accumulatedDamageResisted;
    //    int num2 = (int)num;
    //    accumulatedDamageResisted = num - (float)num2;
    //    _strength -= num2;
    //    DamageResponse dmResponse = damageEntityLocal(_damageSource, _strength, _criticalHit, _impulseScale);
    //    NetPackage package = NetPackageManager.GetPackage<NetPackageDamageEntity>().Setup(entityId, dmResponse);
    //    if (world.IsRemote())
    //    {
    //        SingletonMonoBehaviour<ConnectionManager>.Instance.SendToServer(package);
    //    }
    //    else
    //    {
    //        int excludePlayer = -1;
    //        if (!flag && _damageSource.CreatorEntityId != -2)
    //        {
    //            excludePlayer = _damageSource.getEntityId();
    //            if (_damageSource.CreatorEntityId != -1)
    //            {
    //                Entity entity = world.GetEntity(_damageSource.CreatorEntityId);
    //                if ((bool)entity && !entity.isEntityRemote)
    //                {
    //                    excludePlayer = -1;
    //                }
    //            }
    //        }

    //        world.entityDistributer.SendPacketToTrackedPlayersAndTrackedEntity(entityId, excludePlayer, package);
    //    }

    //    return dmResponse.ModStrength;
    //}

    //public override void SetDamagedTarget(EntityAlive _attackTarget)
    //{
    //    damagedTarget = _attackTarget;
    //}

    //public override void ClearDamagedTarget()
    //{
    //    damagedTarget = null;
    //}

    //public new EntityAlive GetDamagedTarget()
    //{
    //    return damagedTarget;
    //}

    //public override bool IsDead()
    //{
    //    if (!base.IsDead())
    //    {
    //        return RecordedDamage.Fatal;
    //    }

    //    return true;
    //}

    //protected override DamageResponse damageEntityLocal(DamageSource _damageSource, int _strength, bool _criticalHit, float impulseScale)
    //{
    //    DamageResponse _dmResponse = default(DamageResponse);
    //    _dmResponse.Source = _damageSource;
    //    _dmResponse.Strength = _strength;
    //    _dmResponse.Critical = _criticalHit;
    //    _dmResponse.HitDirection = Utils.EnumHitDirection.None;
    //    _dmResponse.MovementState = MovementState;
    //    _dmResponse.Random = rand.RandomFloat;
    //    _dmResponse.ImpulseScale = impulseScale;
    //    _dmResponse.HitBodyPart = _damageSource.GetEntityDamageBodyPart(this);
    //    _dmResponse.ArmorSlot = _damageSource.GetEntityDamageEquipmentSlot(this);
    //    _dmResponse.ArmorSlotGroup = _damageSource.GetEntityDamageEquipmentSlotGroup(this);
    //    if (_strength > 0)
    //    {
    //        _dmResponse.HitDirection = (_damageSource.Equals(DamageSource.fall) ? Utils.EnumHitDirection.Back : ((Utils.EnumHitDirection)Utils.Get4HitDirectionAsInt(_damageSource.getDirection(), GetLookVector())));
    //    }

    //    if (!GameManager.IsDedicatedServer && _damageSource.damageSource != EnumDamageSource.Internal && GameManager.Instance != null)
    //    {
    //        World world = GameManager.Instance.World;
    //        if (world != null && _damageSource.getEntityId() == world.GetPrimaryPlayerId())
    //        {
    //            Transform hitTransform = emodel.GetHitTransform(_damageSource);
    //            Vector3 vector = ((!hitTransform) ? emodel.transform.position : hitTransform.position);
    //            bool num = world.GetPrimaryPlayer().inventory.holdingItem.HasAnyTags(FastTags.Parse("ranged"));
    //            float magnitude = (world.GetPrimaryPlayer().GetPosition() - vector).magnitude;
    //            if (num && magnitude > HitSoundDistance)
    //            {
    //                Manager.PlayInsidePlayerHead("HitEntitySound");
    //            }

    //            if (ShowDebugDisplayHit)
    //            {
    //                Transform transform = (hitTransform ? hitTransform : emodel.transform);
    //                Vector3 vector2 = Camera.main.transform.position;
    //                DebugLines.CreateAttached("EntityDamage" + entityId, transform, vector2 + Origin.position, _damageSource.getHitTransformPosition(), new Color(0.3f, 0f, 0.3f), new Color(1f, 0f, 1f), DebugDisplayHitSize * 2f, DebugDisplayHitSize, DebugDisplayHitTime);
    //                DebugLines.CreateAttached("EntityDamage2" + entityId, transform, _damageSource.getHitTransformPosition(), transform.position + Origin.position, new Color(0f, 0f, 0.5f), new Color(0.3f, 0.3f, 1f), DebugDisplayHitSize * 2f, DebugDisplayHitSize, DebugDisplayHitTime);
    //            }
    //        }
    //    }

    //    MinEventContext.Other = base.world.GetEntity(_dmResponse.Source.getEntityId()) as EntityAlive;
    //    if (_damageSource.AffectedByArmor())
    //    {
    //        equipment.CalcDamage(ref _dmResponse.Strength, ref _dmResponse.ArmorDamage, _dmResponse.Source.DamageTypeTag, MinEventContext.Other, _dmResponse.Source.AttackingItem);
    //    }

    //    float num2 = GetDamageFraction(_dmResponse.Strength);
    //    if (_dmResponse.Fatal || _dmResponse.Strength >= Health)
    //    {
    //        if ((_dmResponse.HitBodyPart & EnumBodyPartHit.Head) > EnumBodyPartHit.None)
    //        {
    //            if (num2 >= 0.2f)
    //            {
    //                _dmResponse.Source.DismemberChance = Utils.FastMax(_dmResponse.Source.DismemberChance * 0.5f, 0.3f);
    //            }
    //        }
    //        else if (num2 >= 0.12f)
    //        {
    //            _dmResponse.Source.DismemberChance = Utils.FastMax(_dmResponse.Source.DismemberChance * 0.5f, 0.5f);
    //        }

    //        num2 = 1f;
    //    }

    //    CheckDismember(ref _dmResponse, num2);
    //    int num3 = bodyDamage.StunKnee;
    //    int num4 = bodyDamage.StunProne;
    //    if ((_dmResponse.HitBodyPart & EnumBodyPartHit.Head) > EnumBodyPartHit.None && _dmResponse.Dismember)
    //    {
    //        if (Health > 0)
    //        {
    //            _dmResponse.Strength = Health;
    //        }
    //    }
    //    else if (_damageSource.CanStun && GetWalkType() != 21 && bodyDamage.CurrentStun != EnumEntityStunType.Prone)
    //    {
    //        if ((_dmResponse.HitBodyPart & (EnumBodyPartHit.Arms | EnumBodyPartHit.Torso | EnumBodyPartHit.Head)) > EnumBodyPartHit.None)
    //        {
    //            num4 += _strength;
    //        }
    //        else if (_dmResponse.HitBodyPart.IsLeg())
    //        {
    //            num3 += _strength * ((!_criticalHit) ? 1 : 2);
    //        }
    //    }

    //    if ((!_dmResponse.HitBodyPart.IsLeg() || !_dmResponse.Dismember) && GetWalkType() != 21 && !sleepingOrWakingUp)
    //    {
    //        EntityClass entityClass = EntityClass.list[base.entityClass];
    //        if (GetDamageFraction(num4) >= entityClass.KnockdownProneDamageThreshold && entityClass.KnockdownProneDamageThreshold > 0f)
    //        {
    //            if (bodyDamage.CurrentStun != EnumEntityStunType.Prone)
    //            {
    //                _dmResponse.Stun = EnumEntityStunType.Prone;
    //                _dmResponse.StunDuration = rand.RandomRange(entityClass.KnockdownProneStunDuration.x, entityClass.KnockdownProneStunDuration.y);
    //            }
    //        }
    //        else if (GetDamageFraction(num3) >= entityClass.KnockdownKneelDamageThreshold && entityClass.KnockdownKneelDamageThreshold > 0f && bodyDamage.CurrentStun != EnumEntityStunType.Prone)
    //        {
    //            _dmResponse.Stun = EnumEntityStunType.Kneel;
    //            _dmResponse.StunDuration = rand.RandomRange(entityClass.KnockdownKneelStunDuration.x, entityClass.KnockdownKneelStunDuration.y);
    //        }
    //    }

    //    bool flag = false;
    //    int num5 = _dmResponse.Strength + _dmResponse.ArmorDamage / 2;
    //    if (num5 > 0 && !IsGodMode.Value && _dmResponse.Stun == EnumEntityStunType.None && !sleepingOrWakingUp)
    //    {
    //        flag = _dmResponse.Strength < Health;
    //        if (flag)
    //        {
    //            flag = GetWalkType() == 21 || !_dmResponse.Dismember || !_dmResponse.HitBodyPart.IsLeg();
    //        }

    //        if (flag && _dmResponse.Source.GetDamageType() != EnumDamageTypes.Bashing)
    //        {
    //            flag = num5 >= 6;
    //        }

    //        if (_dmResponse.Source.GetDamageType() == EnumDamageTypes.BarbedWire)
    //        {
    //            flag = true;
    //        }
    //    }

    //    _dmResponse.PainHit = flag;
    //    if (_dmResponse.Strength >= Health)
    //    {
    //        _dmResponse.Fatal = true;
    //    }

    //    if (_dmResponse.Fatal)
    //    {
    //        _dmResponse.Stun = EnumEntityStunType.None;
    //    }

    //    if (isEntityRemote)
    //    {
    //        _dmResponse.ModStrength = 0;
    //    }
    //    else
    //    {
    //        if (Health <= _dmResponse.Strength)
    //        {
    //            _strength -= Health;
    //        }

    //        _dmResponse.ModStrength = _strength;
    //    }

    //    if (_dmResponse.Dismember)
    //    {
    //        EntityAlive entityAlive = base.world.GetEntity(_dmResponse.Source.getEntityId()) as EntityAlive;
    //        if (entityAlive != null)
    //        {
    //            entityAlive.FireEvent(MinEventTypes.onDismember);
    //        }
    //    }

    //    if (MinEventContext.Other != null)
    //    {
    //        MinEventContext.Other.MinEventContext.DamageResponse = _dmResponse;
    //        float value = EffectManager.GetValue(PassiveEffects.HealthSteal, null, 0f, MinEventContext.Other);
    //        if (value != 0f)
    //        {
    //            int num6 = (int)((float)num5 * value);
    //            if (num6 + MinEventContext.Other.Health <= 0)
    //            {
    //                num6 = (MinEventContext.Other.Health - 1) * -1;
    //            }

    //            MinEventContext.Other.AddHealth(num6);
    //            if (num6 < 0 && MinEventContext.Other is EntityPlayerLocal)
    //            {
    //                ((EntityPlayerLocal)MinEventContext.Other).ForceBloodSplatter();
    //            }
    //        }
    //    }

    //    if (_dmResponse.Source.BuffClass == null)
    //    {
    //        MinEventContext.DamageResponse = _dmResponse;
    //        FireEvent(MinEventTypes.onOtherAttackedSelf);
    //    }

    //    ProcessDamageResponseLocal(_dmResponse);
    //    return _dmResponse;
    //}

    //public override void ProcessDamageResponse(DamageResponse _dmResponse)
    //{
    //    base.ProcessDamageResponse(_dmResponse);
    //    ProcessDamageResponseLocal(_dmResponse);
    //    if (!world.IsRemote())
    //    {
    //        Entity entity = world.GetEntity(_dmResponse.Source.getEntityId());
    //        if ((bool)entity && !entity.isEntityRemote && isEntityRemote && this is EntityPlayer)
    //        {
    //            world.entityDistributer.SendPacketToTrackedPlayers(entityId, entityId, NetPackageManager.GetPackage<NetPackageDamageEntity>().Setup(entityId, _dmResponse));
    //        }
    //        else
    //        {
    //            world.entityDistributer.SendPacketToTrackedPlayersAndTrackedEntity(entityId, _dmResponse.Source.getEntityId(), NetPackageManager.GetPackage<NetPackageDamageEntity>().Setup(entityId, _dmResponse));
    //        }
    //    }
    //}

    //public override void ProcessDamageResponseLocal(DamageResponse _dmResponse)
    //{
    //    if (emodel == null)
    //    {
    //        return;
    //    }

    //    if (_dmResponse.Source.BonusDamageType != 0)
    //    {
    //        EntityPlayerLocal primaryPlayer = world.GetPrimaryPlayer();
    //        if ((bool)primaryPlayer && primaryPlayer.entityId == _dmResponse.Source.getEntityId())
    //        {
    //            switch (_dmResponse.Source.BonusDamageType)
    //            {
    //                case EnumDamageBonusType.Sneak:
    //                    primaryPlayer.NotifySneakDamage(_dmResponse.Source.DamageMultiplier);
    //                    break;
    //                case EnumDamageBonusType.Stun:
    //                    primaryPlayer.NotifyDamageMultiplier(_dmResponse.Source.DamageMultiplier);
    //                    break;
    //            }
    //        }
    //    }

    //    EntityAlive entityAlive = world.GetEntity(_dmResponse.Source.getEntityId()) as EntityAlive;
    //    if (entityAlive != null)
    //    {
    //        entityAlive.SetDamagedTarget(this);
    //    }

    //    if (IsSleeperPassive)
    //    {
    //        world.CheckSleeperVolumeNoise(position);
    //    }

    //    ConditionalTriggerSleeperWakeUp();
    //    SleeperSupressLivingSounds = false;
    //    bPlayHurtSound = false;
    //    if (AttachedToEntity != null && !_dmResponse.Source.bIsDamageTransfer && _dmResponse.Source.GetSource() != EnumDamageSource.Internal)
    //    {
    //        _dmResponse.Source.bIsDamageTransfer = true;
    //        AttachedToEntity.DamageEntity(_dmResponse.Source, _dmResponse.Strength, _dmResponse.Critical, _dmResponse.ImpulseScale);
    //        return;
    //    }

    //    if (equipment != null && _dmResponse.ArmorDamage > 0)
    //    {
    //        List<ItemValue> armor = equipment.GetArmor();
    //        if (armor.Count > 0)
    //        {
    //            float num = (float)_dmResponse.ArmorDamage / (float)armor.Count;
    //            if (num < 1f && num != 0f)
    //            {
    //                num = 1f;
    //            }

    //            for (int i = 0; i < armor.Count; i++)
    //            {
    //                armor[i].UseTimes += EffectManager.GetValue(PassiveEffects.DegradationPerUse, armor[i], num, this, null, armor[i].ItemClass.ItemTags);
    //            }
    //        }
    //    }

    //    ApplyLocalBodyDamage(_dmResponse);
    //    lastHitRanged = false;
    //    bool flag = EffectManager.GetValue(PassiveEffects.NegateDamageSelf, null, 0f, this, null, FastTags.Parse(_dmResponse.HitBodyPart.ToString())) > 0f || EffectManager.GetValue(PassiveEffects.NegateDamageOther, (entityAlive != null) ? entityAlive.inventory.holdingItemItemValue : null, 0f, entityAlive) > 0f;
    //    if (_dmResponse.Dismember && !flag)
    //    {
    //        lastHitImpactDir = _dmResponse.Source.getDirection();
    //        if (entityAlive != null)
    //        {
    //            lastHitEntityFwd = entityAlive.GetForwardVector();
    //        }

    //        if (_dmResponse.Source.ItemClass != null && _dmResponse.Source.ItemClass.HasAnyTags(DismembermentManager.rangedTags))
    //        {
    //            lastHitRanged = true;
    //        }

    //        if (_dmResponse.Source.ItemClass != null)
    //        {
    //            float strength = (float)_dmResponse.ModStrength / (float)GetMaxHealth();
    //            lastHitForce = DismembermentManager.GetImpactForce(_dmResponse.Source.ItemClass, strength);
    //        }

    //        ExecuteDismember(restoreState: false);
    //    }

    //    bool flag2 = _dmResponse.Stun != EnumEntityStunType.None;
    //    bool flag3 = bodyDamage.CurrentStun != EnumEntityStunType.None;
    //    if (!flag && _dmResponse.Fatal && isEntityRemote)
    //    {
    //        ClientKill(_dmResponse);
    //    }
    //    else if (emodel.avatarController != null && flag2)
    //    {
    //        if (_dmResponse.Stun == EnumEntityStunType.Prone)
    //        {
    //            if (bodyDamage.CurrentStun == EnumEntityStunType.None)
    //            {
    //                if ((_dmResponse.Critical && _dmResponse.Source.damageType == EnumDamageTypes.Bashing) || rand.RandomFloat < 0.6f)
    //                {
    //                    DoRagdoll(_dmResponse);
    //                }
    //                else
    //                {
    //                    emodel.avatarController.BeginStun(EnumEntityStunType.Prone, _dmResponse.HitBodyPart, _dmResponse.HitDirection, _dmResponse.Critical, _dmResponse.Random);
    //                }

    //                SetStun(EnumEntityStunType.Prone);
    //                bodyDamage.StunDuration = _dmResponse.StunDuration;
    //            }
    //            else if (bodyDamage.CurrentStun != EnumEntityStunType.Prone)
    //            {
    //                DoRagdoll(_dmResponse);
    //                SetStun(EnumEntityStunType.Prone);
    //                bodyDamage.StunDuration = _dmResponse.StunDuration * 0.5f;
    //            }
    //        }
    //        else if (_dmResponse.Stun == EnumEntityStunType.Kneel)
    //        {
    //            bool flag4 = false;
    //            if (bodyDamage.CurrentStun == EnumEntityStunType.None)
    //            {
    //                if (_dmResponse.Critical || rand.RandomFloat < 0.25f)
    //                {
    //                    flag4 = true;
    //                }
    //                else
    //                {
    //                    SetStun(EnumEntityStunType.Kneel);
    //                    emodel.avatarController.BeginStun(EnumEntityStunType.Kneel, _dmResponse.HitBodyPart, _dmResponse.HitDirection, _dmResponse.Critical, _dmResponse.Random);
    //                }
    //            }
    //            else if (bodyDamage.CurrentStun == EnumEntityStunType.Kneel)
    //            {
    //                flag4 = true;
    //            }

    //            if (flag4)
    //            {
    //                DoRagdoll(_dmResponse);
    //                SetStun(EnumEntityStunType.Prone);
    //            }

    //            bodyDamage.StunDuration = _dmResponse.StunDuration;
    //        }
    //    }
    //    else if (emodel.avatarController != null && _dmResponse.PainHit && !flag3)
    //    {
    //        float painResistPerHit = EntityClass.list[entityClass].PainResistPerHit;
    //        if (painResistPerHit >= 0f)
    //        {
    //            painResistPercent = Utils.FastMin(painResistPercent + painResistPerHit, 3f);
    //            float duration = ((painResistPercent >= 1f) ? Mathf.Lerp(0.6f, 0.15f, (painResistPercent - 1f) * 0.5f) : float.MaxValue);
    //            emodel.avatarController.StartAnimationHit(_dmResponse.HitBodyPart, (int)_dmResponse.HitDirection, (int)((float)_dmResponse.Strength * 100f / (float)GetMaxHealth()), _dmResponse.Critical, _dmResponse.MovementState, _dmResponse.Random, duration);
    //        }
    //    }

    //    if (bodyDamage.CurrentStun == EnumEntityStunType.None)
    //    {
    //        if (_dmResponse.Source.CanStun)
    //        {
    //            if ((_dmResponse.HitBodyPart & (EnumBodyPartHit.Arms | EnumBodyPartHit.Torso | EnumBodyPartHit.Head)) > EnumBodyPartHit.None)
    //            {
    //                bodyDamage.StunProne += _dmResponse.Strength;
    //            }
    //            else if (_dmResponse.HitBodyPart.IsLeg())
    //            {
    //                bodyDamage.StunKnee += _dmResponse.Strength;
    //            }
    //        }
    //    }
    //    else
    //    {
    //        bodyDamage.StunProne = 0;
    //        bodyDamage.StunKnee = 0;
    //    }

    //    bool flag5 = Health <= 0;
    //    if (Health <= 0 && deathUpdateTime > 0)
    //    {
    //        DeathHealth -= _dmResponse.Strength;
    //    }

    //    int num2 = _dmResponse.Strength;
    //    if (EffectManager.GetValue(PassiveEffects.HeadShotOnly, null, 0f, GameManager.Instance.World.GetEntity(_dmResponse.Source.getEntityId()) as EntityAlive) > 0f && (_dmResponse.HitBodyPart & EnumBodyPartHit.Head) == 0)
    //    {
    //        num2 = 0;
    //        _dmResponse.Fatal = false;
    //    }

    //    if (flag)
    //    {
    //        num2 = 0;
    //        _dmResponse.Fatal = false;
    //    }

    //    if (isEntityRemote)
    //    {
    //        Health -= num2;
    //        RecordedDamage = _dmResponse;
    //        return;
    //    }

    //    if (!IsGodMode.Value)
    //    {
    //        Health -= num2;
    //        if (_dmResponse.Fatal && Health > 0)
    //        {
    //            Health = 0;
    //        }

    //        hasBeenAttackedTime = 0;
    //        if (_dmResponse.PainHit)
    //        {
    //            hasBeenAttackedTime = GetMaxAttackTime();
    //        }
    //    }

    //    bPlayHurtSound = (bBeenWounded = num2 > 0);
    //    if (bBeenWounded)
    //    {
    //        setBeenAttacked();
    //        MinEventContext.Other = GameManager.Instance.World.GetEntity(_dmResponse.Source.getEntityId()) as EntityAlive;
    //        FireEvent(MinEventTypes.onOtherDamagedSelf);
    //    }

    //    if (num2 > woundedStrength)
    //    {
    //        woundedStrength = _dmResponse.Strength;
    //        woundedDamageSource = _dmResponse.Source;
    //    }

    //    lastHitDirection = _dmResponse.HitDirection;
    //    if (Health <= 0)
    //    {
    //        _dmResponse.Source.getDirection();
    //        _dmResponse.Strength += Health;
    //        Entity entity = ((_dmResponse.Source.getEntityId() != -1) ? world.GetEntity(_dmResponse.Source.getEntityId()) : null);
    //        if (Spawned && !flag5)
    //        {
    //            if (entity is EntityAlive)
    //            {
    //                entityThatKilledMe = (EntityAlive)entity;
    //            }
    //            else
    //            {
    //                entityThatKilledMe = null;
    //            }
    //        }

    //        Kill(_dmResponse);
    //        if (!_dmResponse.Fatal && world.IsRemote())
    //        {
    //            DamageEntity(DamageSource.disease, 1, _criticalHit: false);
    //        }
    //    }

    //    Entity entity2 = ((_dmResponse.Source.getEntityId() != -1) ? world.GetEntity(_dmResponse.Source.getEntityId()) : null);
    //    if (entity2 != null && entity2 != this)
    //    {
    //        if (entity2 is EntityAlive && !entity2.IsIgnoredByAI())
    //        {
    //            SetRevengeTarget((EntityAlive)entity2);
    //            if (aiManager != null)
    //            {
    //                aiManager.DamagedByEntity();
    //            }
    //        }

    //        if (entity2 is EntityPlayer)
    //        {
    //            ((EntityPlayer)entity2).FireEvent(MinEventTypes.onCombatEntered);
    //        }

    //        FireEvent(MinEventTypes.onCombatEntered);
    //    }

    //    if (_dmResponse.Strength > 0 && _dmResponse.Source.GetDamageType() == EnumDamageTypes.Electrical)
    //    {
    //        Electrocuted = true;
    //    }

    //    RecordedDamage = _dmResponse;
    //}

    //public new EntityAlive GetRevengeTarget()
    //{
    //    return revengeEntity;
    //}

    //public new void SetRevengeTarget(EntityAlive _other)
    //{
    //    revengeEntity = _other;
    //    revengeTimer = ((!(revengeEntity == null)) ? 500 : 0);
    //}

    //public new void SetRevengeTimer(int ticks)
    //{
    //    revengeTimer = ticks;
    //}

    //public override bool CanBePushed()
    //{
    //    return !IsDead();
    //}

    //public override bool CanCollideWith(Entity _other)
    //{
    //    if (!IsDead() && !(_other is EntityItem))
    //    {
    //        return !(_other is EntitySupplyCrate);
    //    }

    //    return false;
    //}

    //public override bool CanCollideWithBlocks()
    //{
    //    if (IsSleeping)
    //    {
    //        return false;
    //    }

    //    return true;
    //}

    //public new void DoRagdoll(DamageResponse _dmResponse)
    //{
    //    emodel.DoRagdoll(_dmResponse, _dmResponse.StunDuration);
    //}

    //public new void AddScore(int _diedMySelfTimes, int _zombieKills, int _playerKills, int _otherTeamnumber, int _conditions)
    //{
    //    KilledZombies += _zombieKills;
    //    KilledPlayers += _playerKills;
    //    Died += _diedMySelfTimes;
    //    Score += _zombieKills * GameStats.GetInt(EnumGameStats.ScoreZombieKillMultiplier) + _playerKills * GameStats.GetInt(EnumGameStats.ScorePlayerKillMultiplier) + _diedMySelfTimes * GameStats.GetInt(EnumGameStats.ScoreDiedMultiplier);
    //    if (Score < 0)
    //    {
    //        Score = 0;
    //    }

    //    if (this is EntityPlayerLocal)
    //    {
    //        if (_diedMySelfTimes > 0)
    //        {
    //            PlatformManager.NativePlatform.AchievementManager?.SetAchievementStat(EnumAchievementDataStat.Deaths, _diedMySelfTimes);
    //        }

    //        if (_zombieKills > 0)
    //        {
    //            PlatformManager.NativePlatform.AchievementManager?.SetAchievementStat(EnumAchievementDataStat.ZombiesKilled, _zombieKills);
    //        }

    //        if (_playerKills > 0)
    //        {
    //            PlatformManager.NativePlatform.AchievementManager?.SetAchievementStat(EnumAchievementDataStat.PlayersKilled, _playerKills);
    //        }

    //        if (((uint)_conditions & 2u) != 0)
    //        {
    //            PlatformManager.NativePlatform.AchievementManager?.SetAchievementStat(EnumAchievementDataStat.KilledWith44Magnum, 1);
    //        }
    //    }
    //}

    //public override void AwardKill(EntityAlive killer)
    //{
    //    if (!(killer != null) || !(killer != this))
    //    {
    //        return;
    //    }

    //    int num = 0;
    //    int num2 = 0;
    //    int conditions = 0;
    //    switch (entityType)
    //    {
    //        case EntityType.Player:
    //            num2++;
    //            break;
    //        case EntityType.Zombie:
    //            num++;
    //            break;
    //    }

    //    EntityPlayer entityPlayer = killer as EntityPlayer;
    //    if ((bool)entityPlayer)
    //    {
    //        GameManager.Instance.AwardKill(killer, this);
    //        if (entityPlayer.inventory.IsHoldingGun() && entityPlayer.inventory.holdingItem.Name.Equals("gunHandgunT2Magnum44"))
    //        {
    //            conditions = 2;
    //        }

    //        GameManager.Instance.AddScoreServer(killer.entityId, num, num2, TeamNumber, conditions);
    //    }
    //}

    //public override void OnEntityDeath()
    //{
    //    if (deathUpdateTime != 0)
    //    {
    //        return;
    //    }

    //    AddScore(1, 0, 0, -1, 0);
    //    if (soundLiving != null && soundLivingID >= 0)
    //    {
    //        Manager.Stop(entityId, soundLiving);
    //        soundLivingID = -1;
    //    }

    //    if ((bool)AttachedToEntity)
    //    {
    //        Detach();
    //    }

    //    if (!isEntityRemote)
    //    {
    //        AwardKill(entityThatKilledMe);
    //        if (particleOnDeath != null && particleOnDeath.Length > 0)
    //        {
    //            float lightBrightness = world.GetLightBrightness(GetBlockPosition());
    //            world.GetGameManager().SpawnParticleEffectServer(new ParticleEffect(particleOnDeath, getHeadPosition(), lightBrightness, Color.white, null, null, _OLDCreateColliders: false), entityId);
    //        }

    //        if (isGameMessageOnDeath())
    //        {
    //            GameManager.Instance.GameMessage(EnumGameMessages.EntityWasKilled, this, entityThatKilledMe);
    //        }

    //        if (entityThatKilledMe != null)
    //        {
    //            Log.Out("Entity {0} {1} killed by {2} {3}", GetDebugName(), entityId, entityThatKilledMe.GetDebugName(), entityThatKilledMe.entityId);
    //        }
    //        else
    //        {
    //            Log.Out("Entity {0} {1} killed", GetDebugName(), entityId);
    //        }

    //        ModEvents.EntityKilled.Invoke(this, entityThatKilledMe);
    //        dropItemOnDeath();
    //        entityThatKilledMe = null;
    //    }
    //}

    //public override void PlayGiveUpSound()
    //{
    //    if (soundGiveUp != null)
    //    {
    //        PlayOneShot(soundGiveUp);
    //    }
    //}

    //public override Vector3 GetCameraLook(float _t)
    //{
    //    return GetLookVector();
    //}

    //public new Vector3 GetForwardVector()
    //{
    //    float num = Mathf.Cos(rotation.y * 0.0175f - (float)Math.PI);
    //    float num2 = Mathf.Sin(rotation.y * 0.0175f - (float)Math.PI);
    //    float num3 = 0f - Mathf.Cos(0f);
    //    float y = Mathf.Sin(0f);
    //    return new Vector3(num2 * num3, y, num * num3);
    //}

    //public new Vector2 GetForwardVector2()
    //{
    //    float f = rotation.y * ((float)Math.PI / 180f);
    //    return new Vector2(y: Mathf.Cos(f), x: Mathf.Sin(f));
    //}

    //public override Vector3 GetLookVector()
    //{
    //    float num = Mathf.Cos(rotation.y * 0.0175f - (float)Math.PI);
    //    float num2 = Mathf.Sin(rotation.y * 0.0175f - (float)Math.PI);
    //    float num3 = 0f - Mathf.Cos(rotation.x * 0.0175f);
    //    float y = Mathf.Sin(rotation.x * 0.0175f);
    //    return new Vector3(num2 * num3, y, num * num3);
    //}

    //public override Vector3 GetLookVector(Vector3 _altLookVector)
    //{
    //    return GetLookVector();
    //}

    //protected override int GetSoundRandomTicks()
    //{
    //    return rand.RandomRange(soundRandomTime / 2, soundRandomTime);
    //}

    //protected override int GetSoundAlertTicks()
    //{
    //    return rand.RandomRange(soundAlertTime / 2, soundAlertTime);
    //}

    //protected override string GetSoundRandom()
    //{
    //    return soundRandom;
    //}

    //protected override string GetSoundJump()
    //{
    //    return soundJump;
    //}

    //protected override string GetSoundHurt(DamageSource _damageSource, int _damageStrength)
    //{
    //    return soundHurt;
    //}

    //protected new string GetSoundHurtSmall()
    //{
    //    return soundHurtSmall;
    //}

    //protected string GetSoundHurt()
    //{
    //    return soundHurt;
    //}

    //protected string GetSoundDrownPain()
    //{
    //    return soundDrownPain;
    //}

    //protected override string GetSoundDeath(DamageSource _damageSource)
    //{
    //    return soundDeath;
    //}

    //protected override string GetSoundAttack()
    //{
    //    return soundAttack;
    //}

    //public override string GetSoundAlert()
    //{
    //    return soundAlert;
    //}

    //protected override string GetSoundStamina()
    //{
    //    return soundStamina;
    //}

    //public override Ray GetLookRay()
    //{
    //    return new Ray(position + new Vector3(0f, GetEyeHeight(), 0f), GetLookVector());
    //}

    //protected override void dropItemOnDeath()
    //{
    //    for (int i = 0; i < inventory.GetItemCount(); i++)
    //    {
    //        ItemStack item = inventory.GetItem(i);
    //        ItemClass forId = ItemClass.GetForId(item.itemValue.type);
    //        if (forId != null && forId.CanDrop())
    //        {
    //            world.GetGameManager().ItemDropServer(item, position, new Vector3(0.5f, 0f, 0.5f), -1, Constants.cItemDroppedOnDeathLifetime);
    //            inventory.SetItem(i, ItemValue.None.Clone(), 0);
    //        }
    //    }

    //    inventory.SetFlashlight(on: false);
    //    equipment.DropItems();
    //    if (world.IsDark())
    //    {
    //        lootDropProb *= 1f;
    //    }

    //    if ((bool)entityThatKilledMe)
    //    {
    //        lootDropProb = EffectManager.GetValue(PassiveEffects.LootDropProb, entityThatKilledMe.inventory.holdingItemItemValue, lootDropProb, entityThatKilledMe);
    //    }

    //    if (lootDropProb > rand.RandomFloat)
    //    {
    //        GameManager.Instance.DropContentOfLootContainerServer(BlockValue.Air, new Vector3i(position), entityId);
    //    }
    //}

    //public override Vector3 GetDropPosition()
    //{
    //    if (ParachuteWearing || JetpackWearing)
    //    {
    //        return base.transform.position + base.transform.forward - Vector3.up * 0.3f + Origin.position;
    //    }

    //    return base.transform.position + base.transform.forward + Vector3.up + Origin.position;
    //}

    //public override void OnFired()
    //{
    //    if (emodel.avatarController != null)
    //    {
    //        emodel.avatarController.StartAnimationFiring();
    //    }
    //}

    //public override void OnReloadStart()
    //{
    //    if (emodel.avatarController != null)
    //    {
    //        emodel.avatarController.StartAnimationReloading();
    //    }
    //}

    //public override void OnReloadEnd()
    //{
    //}

    //public override bool WillForceToFollow(EntityAlive _other)
    //{
    //    return false;
    //}

    //public void AddHealth(int _v)
    //{
    //    if (Health > 0)
    //    {
    //        Health += _v;
    //    }
    //}

    //public void AddStamina(float _v)
    //{
    //    if (Health > 0)
    //    {
    //        Stats.Stamina.Value += _v;
    //    }
    //}

    //public void AddWater(float _v)
    //{
    //    Stats.Water.Value += _v;
    //}

    //public int GetTicksNoPlayerAdjacent()
    //{
    //    return ticksNoPlayerAdjacent;
    //}

    //public bool CanSee(EntityAlive _other)
    //{
    //    return seeCache.CanSee(_other);
    //}

    //public void SetCanSee(EntityAlive _other)
    //{
    //    seeCache.SetCanSee(_other);
    //}

    //protected override void updateTasks()
    //{
    //    if (GamePrefs.GetBool(EnumGamePrefs.DebugStopEnemiesMoving))
    //    {
    //        SetMoveForwardWithModifiers(0f, 0f, _climb: false);
    //        if (aiManager != null)
    //        {
    //            aiManager.UpdateDebugName();
    //        }

    //        return;
    //    }

    //    CheckDespawn();
    //    seeCache.ClearIfExpired();
    //    bool useAIPackages = EntityClass.list[entityClass].UseAIPackages;
    //    aiActiveDelay -= aiActiveScale;
    //    if (aiActiveDelay <= 0f)
    //    {
    //        aiActiveDelay = 1f;
    //        if (!useAIPackages)
    //        {
    //            aiManager.Update();
    //        }
    //        else
    //        {
    //            UAIBase.Update(utilityAIContext);
    //        }
    //    }

    //    PathInfo path = PathFinderThread.Instance.GetPath(entityId);
    //    if (path.path != null)
    //    {
    //        bool flag = true;
    //        if (!useAIPackages)
    //        {
    //            flag = aiManager.CheckPath(path);
    //        }

    //        if (flag)
    //        {
    //            navigator.SetPath(path, path.speed);
    //        }
    //    }

    //    navigator.UpdateNavigation();
    //    moveHelper.UpdateMoveHelper();
    //    lookHelper.onUpdateLook();
    //    if (distraction != null && (distraction.IsDead() || distraction.IsMarkedForUnload()))
    //    {
    //        distraction = null;
    //    }

    //    if (pendingDistraction != null && (pendingDistraction.IsDead() || pendingDistraction.IsMarkedForUnload()))
    //    {
    //        pendingDistraction = null;
    //    }
    //}

    //public PathNavigate getNavigator()
    //{
    //    return navigator;
    //}

    //public void FindPath(Vector3 targetPos, float moveSpeed, bool canBreak, EAIBase behavior)
    //{
    //    Vector3 vector = targetPos - position;
    //    if (vector.x * vector.x + vector.z * vector.z > 1225f)
    //    {
    //        if (vector.y > 45f)
    //        {
    //            targetPos.y = position.y + 45f;
    //        }
    //        else if (vector.y < -45f)
    //        {
    //            targetPos.y = position.y - 45f;
    //        }
    //    }

    //    PathFinderThread.Instance.FindPath(this, targetPos, moveSpeed, canBreak, behavior);
    //}

    //public bool isWithinHomeDistanceCurrentPosition()
    //{
    //    return isWithinHomeDistance(Utils.Fastfloor(position.x), Utils.Fastfloor(position.y), Utils.Fastfloor(position.z));
    //}

    //public bool isWithinHomeDistance(int _x, int _y, int _z)
    //{
    //    if (maximumHomeDistance < 0)
    //    {
    //        return true;
    //    }

    //    return homePosition.getDistanceSquared(_x, _y, _z) < (float)(maximumHomeDistance * maximumHomeDistance);
    //}

    //public void setHomeArea(Vector3i _pos, int _maxDistance)
    //{
    //    homePosition.position = _pos;
    //    maximumHomeDistance = _maxDistance;
    //}

    //public ChunkCoordinates getHomePosition()
    //{
    //    return homePosition;
    //}

    //public int getMaximumHomeDistance()
    //{
    //    return maximumHomeDistance;
    //}

    //public void detachHome()
    //{
    //    maximumHomeDistance = -1;
    //}

    //public bool hasHome()
    //{
    //    return maximumHomeDistance >= 0;
    //}

    //protected override bool canDespawn()
    //{
    //    if (!IsClientControlled() && GetSpawnerSource() != EnumSpawnerSource.StaticSpawner)
    //    {
    //        return !IsSleeping;
    //    }

    //    return false;
    //}

    //public void ResetDespawnTime()
    //{
    //    ticksNoPlayerAdjacent = 0;
    //    seeCache.SetLastTimePlayerSeen();
    //}

    //public void CheckDespawn()
    //{
    //    if (isEntityRemote)
    //    {
    //        return;
    //    }

    //    if (!CanUpdateEntity() && bIsChunkObserver && world.GetClosestPlayer(this, -1f, _isDead: false) == null)
    //    {
    //        MarkToUnload();
    //    }
    //    else
    //    {
    //        if (!canDespawn() || ++despawnDelayCounter < 20)
    //        {
    //            return;
    //        }

    //        despawnDelayCounter = 0;
    //        ticksNoPlayerAdjacent += 20;
    //        EnumSpawnerSource enumSpawnerSource = GetSpawnerSource();
    //        EntityPlayer closestPlayer = world.GetClosestPlayer(this, -1f, _isDead: false);
    //        switch (enumSpawnerSource)
    //        {
    //            case EnumSpawnerSource.Dynamic:
    //                if (!closestPlayer)
    //                {
    //                    if (!world.GetClosestPlayer(this, -1f, _isDead: true))
    //                    {
    //                        Despawn();
    //                    }

    //                    return;
    //                }

    //                break;
    //            case EnumSpawnerSource.Biome:
    //                if (!world.GetClosestPlayer(this, 130f, _isDead: false))
    //                {
    //                    if ((bool)world.GetClosestPlayer(this, 20f, _isDead: true))
    //                    {
    //                        isDespawnWhenPlayerFar = true;
    //                    }
    //                    else if (isDespawnWhenPlayerFar)
    //                    {
    //                        Despawn();
    //                    }
    //                }

    //                break;
    //        }

    //        if (!closestPlayer)
    //        {
    //            return;
    //        }

    //        float sqrMagnitude = (closestPlayer.position - position).sqrMagnitude;
    //        if (sqrMagnitude < 6400f)
    //        {
    //            ticksNoPlayerAdjacent = 0;
    //        }

    //        long num = long.MaxValue;
    //        if (seeCache.GetLastTimePlayerSeen() > 0f)
    //        {
    //            num = (int)(Time.time - seeCache.GetLastTimePlayerSeen());
    //        }

    //        switch (enumSpawnerSource)
    //        {
    //            case EnumSpawnerSource.Dynamic:
    //                if (sqrMagnitude > 2304f && num > 60 && GetAttackTarget() == null && !HasInvestigatePosition)
    //                {
    //                    Despawn();
    //                }
    //                else if (ticksNoPlayerAdjacent > 1800)
    //                {
    //                    Despawn();
    //                }

    //                break;
    //            case EnumSpawnerSource.Biome:
    //                if (ticksNoPlayerAdjacent > 100 && sqrMagnitude > 16384f)
    //                {
    //                    Despawn();
    //                }
    //                else if (ticksNoPlayerAdjacent > 1800)
    //                {
    //                    Despawn();
    //                }

    //                break;
    //            case EnumSpawnerSource.StaticSpawner:
    //                break;
    //        }
    //    }
    //}

    //private void Despawn()
    //{
    //    IsDespawned = true;
    //    MarkToUnload();
    //}

    //public void ForceDespawn()
    //{
    //    Despawn();
    //}

    //public EntityAlive GetAttackTarget()
    //{
    //    return attackTarget;
    //}

    //public override Vector3 GetAttackTargetHitPosition()
    //{
    //    return attackTarget.getChestPosition();
    //}

    //public EntityAlive GetAttackTargetLocal()
    //{
    //    if (isEntityRemote)
    //    {
    //        return attackTargetClient;
    //    }

    //    return attackTarget;
    //}

    //public void SetAttackTarget(EntityAlive _attackTarget, int _attackTargetTime)
    //{
    //    if (_attackTarget == attackTarget)
    //    {
    //        attackTargetTime = _attackTargetTime;
    //        return;
    //    }

    //    if (!ApproachingEnemy && (bool)_attackTarget && GetSoundAlert() != null)
    //    {
    //        livingSoundTicks = rand.RandomRange(5, 20);
    //    }

    //    ApproachingEnemy = _attackTarget != null;
    //    ApproachingPlayer = _attackTarget is EntityPlayer;
    //    targetAlertChanged = ApproachingEnemy;
    //    if (ApproachingEnemy)
    //    {
    //        investigatePositionTicks = 0;
    //    }

    //    if (!isEntityRemote)
    //    {
    //        world.entityDistributer.SendPacketToTrackedPlayersAndTrackedEntity(entityId, -1, NetPackageManager.GetPackage<NetPackageSetAttackTarget>().Setup(entityId, _attackTarget ? _attackTarget.entityId : (-1)));
    //    }

    //    attackTarget = _attackTarget;
    //    attackTargetTime = _attackTargetTime;
    //}

    //public void SetAttackTargetClient(EntityAlive _attackTarget)
    //{
    //    attackTargetClient = _attackTarget;
    //}

    //public int GetInvestigatePositionTicks()
    //{
    //    return investigatePositionTicks;
    //}

    //public void ClearInvestigatePosition()
    //{
    //    investigatePos = Vector3.zero;
    //    investigatePositionTicks = 0;
    //    ResetDespawnTime();
    //}

    //public int CalcInvestigateTicks(int _ticks, EntityAlive _investigateEntity)
    //{
    //    return (int)EffectManager.GetValue(PassiveEffects.EnemySearchDuration, null, _ticks, _investigateEntity, null, EntityClass.list[entityClass].Tags);
    //}

    //public void SetInvestigatePosition(Vector3 pos, int ticks, bool isAlert = true)
    //{
    //    investigatePos = pos;
    //    investigatePositionTicks = ticks;
    //    isInvestigateAlert = isAlert;
    //}

    //public int GetAlertTicks()
    //{
    //    return alertTicks;
    //}

    //public void SetAlertTicks(int ticks)
    //{
    //    alertTicks = ticks;
    //}

    //public EntitySeeCache GetEntitySenses()
    //{
    //    return seeCache;
    //}

    //public override float GetMoveSpeed()
    //{
    //    if (IsBloodMoon || world.IsDark())
    //    {
    //        return EffectManager.GetValue(PassiveEffects.WalkSpeed, null, moveSpeedNight, this);
    //    }

    //    return EffectManager.GetValue(PassiveEffects.CrouchSpeed, null, moveSpeed, this);
    //}

    //public override float GetMoveSpeedAggro()
    //{
    //    if (IsBloodMoon || world.IsDark())
    //    {
    //        return EffectManager.GetValue(PassiveEffects.RunSpeed, null, moveSpeedAggroMax, this);
    //    }

    //    return EffectManager.GetValue(PassiveEffects.WalkSpeed, null, moveSpeedAggro, this);
    //}

    //public float GetMoveSpeedPanic()
    //{
    //    return EffectManager.GetValue(PassiveEffects.RunSpeed, null, moveSpeedPanic, this);
    //}

    //public override float GetWeight()
    //{
    //    return weight;
    //}

    //public override float GetPushFactor()
    //{
    //    return pushFactor;
    //}

    //public override bool CanEntityJump()
    //{
    //    return true;
    //}

    //public void SetMaxViewAngle(float _angle)
    //{
    //    maxViewAngle = _angle;
    //}

    //public override float GetMaxViewAngle()
    //{
    //    return maxViewAngle;
    //}

    //public int GetModelLayer()
    //{
    //    return emodel.GetModelTransform().gameObject.layer;
    //}

    //public override void SetModelLayer(int _layerId, bool force = false, string[] excludeTags = null)
    //{
    //    Utils.SetLayerRecursively(emodel.GetModelTransform().gameObject, _layerId);
    //}

    //public override void SetColliderLayer(int _layerId, bool _force = false)
    //{
    //    Utils.SetColliderLayerRecursively(emodel.GetModelTransform().gameObject, _layerId);
    //}

    //public override void SetEntityName(string _name)
    //{
    //    EntityName = _name;
    //    if (NavObject != null)
    //    {
    //        NavObject.name = _name;
    //    }
    //}

    //protected override int GetMaxAttackTime()
    //{
    //    return 10;
    //}

    //public int GetAttackTimeoutTicks()
    //{
    //    if (!world.IsDark())
    //    {
    //        return attackTimeoutDay;
    //    }

    //    return attackTimeoutNight;
    //}

    //public override string GetLootList()
    //{
    //    if (!string.IsNullOrEmpty(lootListOnDeath) && IsDead())
    //    {
    //        return lootListOnDeath;
    //    }

    //    return base.GetLootList();
    //}

    //public override void MarkToUnload()
    //{
    //    base.MarkToUnload();
    //    deathUpdateTime = timeStayAfterDeath;
    //}

    //public override bool IsMarkedForUnload()
    //{
    //    if (base.IsMarkedForUnload())
    //    {
    //        return deathUpdateTime >= timeStayAfterDeath;
    //    }

    //    return false;
    //}

    //public override bool IsAttackValid()
    //{
    //    if (!(this is EntityPlayer))
    //    {
    //        if (Electrocuted)
    //        {
    //            return false;
    //        }

    //        if (bodyDamage.CurrentStun == EnumEntityStunType.Kneel || bodyDamage.CurrentStun == EnumEntityStunType.Prone)
    //        {
    //            return false;
    //        }
    //    }

    //    if (emodel != null && emodel.avatarController != null && emodel.avatarController.IsAttackPrevented())
    //    {
    //        return false;
    //    }

    //    if (IsDead())
    //    {
    //        return false;
    //    }

    //    if (painResistPercent >= 1f)
    //    {
    //        return true;
    //    }

    //    if (hasBeenAttackedTime <= 0)
    //    {
    //        if (!(emodel.avatarController == null))
    //        {
    //            return !emodel.avatarController.IsAnimationHitRunning();
    //        }

    //        return true;
    //    }

    //    return false;
    //}

    //public override bool IsAttackImpact()
    //{
    //    if ((bool)emodel && (bool)emodel.avatarController)
    //    {
    //        return emodel.avatarController.IsAttackImpact();
    //    }

    //    return false;
    //}

    //public override bool Attack(bool _bAttackReleased)
    //{
    //    if (!_bAttackReleased)
    //    {
    //        if ((bool)emodel && (bool)emodel.avatarController && emodel.avatarController.IsAnimationAttackPlaying())
    //        {
    //            return false;
    //        }

    //        if (!IsAttackValid())
    //        {
    //            return false;
    //        }
    //    }

    //    if (bLastAttackReleased && GetSoundAttack() != null)
    //    {
    //        PlayOneShot(GetSoundAttack());
    //    }

    //    bLastAttackReleased = _bAttackReleased;
    //    attackingTime = 60;
    //    inventory.holdingItem.Actions[0]?.ExecuteAction(inventory.holdingItemData.actionData[0], _bAttackReleased);
    //    return true;
    //}

    //public bool Use(bool _bUseReleased)
    //{
    //    if (!_bUseReleased && !IsAttackValid())
    //    {
    //        return false;
    //    }

    //    attackingTime = 60;
    //    if (inventory.holdingItem.Actions[1] != null)
    //    {
    //        inventory.holdingItem.Actions[1].ExecuteAction(inventory.holdingItemData.actionData[1], _bUseReleased);
    //    }

    //    return true;
    //}

    //public new Entity GetTargetIfAttackedNow()
    //{
    //    if (!IsAttackValid())
    //    {
    //        return null;
    //    }

    //    ItemClass holdingItem = inventory.holdingItem;
    //    if (holdingItem != null)
    //    {
    //        int holdingItemIdx = inventory.holdingItemIdx;
    //        ItemAction itemAction = holdingItem.Actions[holdingItemIdx];
    //        if (itemAction != null)
    //        {
    //            WorldRayHitInfo executeActionTarget = itemAction.GetExecuteActionTarget(inventory.holdingItemData.actionData[holdingItemIdx]);
    //            if (executeActionTarget != null && executeActionTarget.bHitValid && (bool)executeActionTarget.transform)
    //            {
    //                float num = itemAction.Range;
    //                if (num == 0f)
    //                {
    //                    ItemValue holdingItemItemValue = inventory.holdingItemItemValue;
    //                    num = EffectManager.GetItemValue(PassiveEffects.MaxRange, holdingItemItemValue);
    //                }

    //                num += 0.3f;
    //                if (executeActionTarget.hit.distanceSq <= num * num)
    //                {
    //                    Transform hitRootTransform = executeActionTarget.transform;
    //                    if (executeActionTarget.tag.StartsWith("E_BP_"))
    //                    {
    //                        hitRootTransform = GameUtils.GetHitRootTransform(Voxel.voxelRayHitInfo.tag, executeActionTarget.transform);
    //                    }

    //                    if (hitRootTransform != null)
    //                    {
    //                        Entity component = hitRootTransform.GetComponent<Entity>();
    //                        if ((bool)component)
    //                        {
    //                            return component;
    //                        }
    //                    }

    //                    if (executeActionTarget.tag == "E_Vehicle")
    //                    {
    //                        return EntityVehicle.FindCollisionEntity(hitRootTransform);
    //                    }
    //                }
    //            }
    //        }
    //    }

    //    return null;
    //}

    //public override float GetBlockDamageScale()
    //{
    //    EnumGamePrefs eProperty = EnumGamePrefs.BlockDamageAI;
    //    if (IsBloodMoon)
    //    {
    //        eProperty = EnumGamePrefs.BlockDamageAIBM;
    //    }

    //    return (float)GamePrefs.GetInt(eProperty) * 0.01f;
    //}

    //public override void PlayStepSound()
    //{
    //    internalPlayStepSound();
    //}

    //protected override void updateStepSound(float _distX, float _distZ)
    //{
    //    if (blockValueStandingOn.isair)
    //    {
    //        return;
    //    }

    //    if (!onGround || isHeadUnderwater)
    //    {
    //        distanceSwam += Mathf.Sqrt(_distX * _distX + _distZ * _distZ);
    //        if (distanceSwam > nextSwimDistance)
    //        {
    //            nextSwimDistance += 1f;
    //            if (nextSwimDistance < distanceSwam || nextSwimDistance > distanceSwam + 1f)
    //            {
    //                nextSwimDistance = distanceSwam + 1f;
    //            }

    //            internalPlayStepSound();
    //        }

    //        return;
    //    }

    //    distanceWalked += Mathf.Sqrt(_distX * _distX + _distZ * _distZ);
    //    if (distanceWalked > nextStepDistance)
    //    {
    //        nextStepDistance += getNextStepSoundDistance();
    //        if (nextStepDistance < distanceWalked || nextStepDistance > distanceWalked + getNextStepSoundDistance())
    //        {
    //            nextStepDistance = distanceWalked + getNextStepSoundDistance();
    //        }

    //        internalPlayStepSound();
    //    }
    //}

    //protected new void updatePlayerLandSound(float _distXZ, float _diffY)
    //{
    //    if (blockValueStandingOn.isair)
    //    {
    //        return;
    //    }

    //    if (_distXZ >= 0.025f || Utils.FastAbs(_diffY) >= 0.015f)
    //    {
    //        float num = inWaterPercent * 2f;
    //        float x = num - landWaterLevel;
    //        landWaterLevel = num;
    //        float num2 = Utils.FastAbs(x);
    //        if (num > 0f)
    //        {
    //            num2 = Utils.FastMax(num2, _distXZ);
    //        }

    //        if (num2 >= 0.02f)
    //        {
    //            float volumeScale = Utils.FastMin(num2 * 2.2f + 0.01f, 1f);
    //            Manager.Play(this, "player_swim", volumeScale);
    //        }
    //    }

    //    if (isHeadUnderwater)
    //    {
    //        wasOnGround = true;
    //    }
    //    else
    //    {
    //        wasOnGround = onGround;
    //    }
    //}

    //protected new void updateCurrentBlockPosAndValue()
    //{
    //    Vector3i blockPosition = GetBlockPosition();
    //    BlockValue block = world.GetBlock(blockPosition);
    //    if (block.isair)
    //    {
    //        blockPosition.y--;
    //        block = world.GetBlock(blockPosition);
    //    }

    //    if (block.ischild)
    //    {
    //        blockPosition += block.parent;
    //        block = world.GetBlock(blockPosition);
    //    }

    //    if (blockPosStandingOn != blockPosition || !blockValueStandingOn.Equals(block) || (onGround && !wasOnGround))
    //    {
    //        blockPosStandingOn = blockPosition;
    //        blockValueStandingOn = block;
    //        blockStandingOnChanged = !world.IsRemote();
    //        BiomeDefinition biome = world.GetBiome(blockPosStandingOn.x, blockPosStandingOn.z);
    //        if (biomeStandingOn != biome && (biomeStandingOn == null || biome == null || biomeStandingOn.m_Id != biome.m_Id))
    //        {
    //            //WeatherManager.Instance.HandleBiomeChanging(this as EntityPlayer, biomeStandingOn, biome);
    //            onNewBiomeEntered(biome);
    //        }
    //    }

    //    CalcIfInElevator();
    //    Block block2 = blockValueStandingOn.Block;
    //    if (block2.BuffsWhenWalkedOn != null && block2.UseBuffsWhenWalkedOn(world, blockPosStandingOn, blockValueStandingOn))
    //    {
    //        bool flag = true;
    //        TileEntityWorkstation tileEntityWorkstation = world.GetTileEntity(0, blockPosStandingOn) as TileEntityWorkstation;
    //        if (tileEntityWorkstation != null)
    //        {
    //            flag = tileEntityWorkstation.IsBurning;
    //        }

    //        if (flag)
    //        {
    //            for (int i = 0; i < block2.BuffsWhenWalkedOn.Length; i++)
    //            {
    //                string text = block2.BuffsWhenWalkedOn[i];
    //                if (!Buffs.HasBuff(text))
    //                {
    //                    Buffs.AddBuff(text);
    //                }
    //            }
    //        }
    //    }

    //    if (!onGround || IsFlyMode.Value)
    //    {
    //        return;
    //    }

    //    if (block2.MovementFactor != 1f)
    //    {
    //        SetMotionMultiplier(EffectManager.GetValue(PassiveEffects.MovementFactorMultiplier, null, block2.MovementFactor, this));
    //    }

    //    if (!blockStandingOnChanged)
    //    {
    //        return;
    //    }

    //    blockStandingOnChanged = false;
    //    if (!blockValueStandingOn.isair)
    //    {
    //        block2.OnEntityWalking(world, blockPosStandingOn.x, blockPosStandingOn.y, blockPosStandingOn.z, blockValueStandingOn, this);
    //        if (GameManager.bPhysicsActive && !blockValueStandingOn.ischild && world.GetStability(blockPosStandingOn) == 0 && Block.CanFallBelow(world, blockPosStandingOn.x, blockPosStandingOn.y, blockPosStandingOn.z))
    //        {
    //            Log.Warning("EntityAlive {0} AddFallingBlock stab 0 happens?", EntityName);
    //            world.AddFallingBlock(blockPosStandingOn);
    //        }
    //    }

    //    BlockValue block3 = world.GetBlock(blockPosStandingOn + Vector3i.up);
    //    if (!block3.isair)
    //    {
    //        block3.Block.OnEntityWalking(world, blockPosStandingOn.x, blockPosStandingOn.y + 1, blockPosStandingOn.z, block3, this);
    //    }
    //}

    //private void CalcIfInElevator()
    //{
    //    ChunkCluster chunkCache = world.ChunkCache;
    //    Vector3i pos = new Vector3i(blockPosStandingOn.x, Utils.Fastfloor(boundingBox.min.y), blockPosStandingOn.z);
    //    BlockValue block = chunkCache.GetBlock(pos);
    //    Block block2 = block.Block;
    //    bInElevator = block2.IsElevator(block.rotation);
    //    pos.y++;
    //    block = chunkCache.GetBlock(pos);
    //    block2 = block.Block;
    //    bInElevator |= block2.IsElevator(block.rotation);
    //}

    //protected override float getNextStepSoundDistance()
    //{
    //    return 1.5f;
    //}

    //protected override void onNewBiomeEntered(BiomeDefinition _biome)
    //{
    //    biomeStandingOn = _biome;
    //}

    //protected override void updateSpeedForwardAndStrafe(Vector3 _dist, float _partialTicks)
    //{
    //    if (isEntityRemote && _partialTicks > 1f)
    //    {
    //        _dist /= _partialTicks;
    //    }

    //    speedForward *= 0.5f;
    //    speedStrafe *= 0.5f;
    //    speedVertical *= 0.5f;
    //    if (Mathf.Abs(_dist.x) > 0.001f || Mathf.Abs(_dist.z) > 0.001f)
    //    {
    //        float num = Mathf.Sin((0f - rotation.y) * (float)Math.PI / 180f);
    //        float num2 = Mathf.Cos((0f - rotation.y) * (float)Math.PI / 180f);
    //        speedForward += num2 * _dist.z - num * _dist.x;
    //        speedStrafe += num2 * _dist.x + num * _dist.z;
    //    }

    //    if (Mathf.Abs(_dist.y) > 0.001f)
    //    {
    //        speedVertical += _dist.y;
    //    }

    //    SetMovementState();
    //}

    //public override void SetLookPosition(Vector3 _lookPos)
    //{
    //    if (!((lookAtPosition - _lookPos).sqrMagnitude < 0.0016f))
    //    {
    //        lookAtPosition = _lookPos;
    //        if (world.entityDistributer != null)
    //        {
    //            world.entityDistributer.SendPacketToTrackedPlayers(entityId, (world.GetPrimaryPlayer() != null) ? world.GetPrimaryPlayer().entityId : (-1), NetPackageManager.GetPackage<NetPackageEntityLookAt>().Setup(entityId, _lookPos));
    //        }

    //        if (emodel.avatarController != null)
    //        {
    //            emodel.avatarController.SetLookPosition(_lookPos);
    //        }
    //    }
    //}

    //protected override bool isRadiationSensitive()
    //{
    //    return true;
    //}

    //public override bool IsAimingGunPossible()
    //{
    //    return true;
    //}

    //public new int GetDeathTime()
    //{
    //    return deathUpdateTime;
    //}

    //public new void SetDeathTime(int _deathTime)
    //{
    //    deathUpdateTime = _deathTime;
    //}

    //public new int GetTimeStayAfterDeath()
    //{
    //    return timeStayAfterDeath;
    //}

    //public new bool IsCorpse()
    //{
    //    if ((bool)emodel && emodel.IsRagdollDead && (float)deathUpdateTime > 70f)
    //    {
    //        return true;
    //    }

    //    return false;
    //}

    //public override void OnEntityUnload()
    //{
    //    if (navigator != null)
    //    {
    //        navigator.SetPath(null, 0f);
    //        navigator = null;
    //    }

    //    base.OnEntityUnload();
    //    lookHelper = null;
    //    moveHelper = null;
    //    seeCache = null;
    //}

    //private float GetDamageFraction(float _damage)
    //{
    //    return _damage / (float)GetMaxHealth();
    //}

    //private float GetDismemberChance(ref DamageResponse _dmResponse, float damagePer)
    //{
    //    EnumBodyPartHit hitBodyPart = _dmResponse.HitBodyPart;
    //    EntityClass entityClass = EntityClass.list[base.entityClass];
    //    float originalValue = 0f;
    //    switch (hitBodyPart.ToPrimary())
    //    {
    //        case BodyPrimaryHit.Head:
    //            originalValue = entityClass.DismemberMultiplierHead;
    //            break;
    //        case BodyPrimaryHit.LeftUpperArm:
    //        case BodyPrimaryHit.RightUpperArm:
    //        case BodyPrimaryHit.LeftLowerArm:
    //        case BodyPrimaryHit.RightLowerArm:
    //            originalValue = entityClass.DismemberMultiplierArms;
    //            break;
    //        case BodyPrimaryHit.LeftUpperLeg:
    //        case BodyPrimaryHit.RightUpperLeg:
    //        case BodyPrimaryHit.LeftLowerLeg:
    //        case BodyPrimaryHit.RightLowerLeg:
    //            originalValue = entityClass.DismemberMultiplierLegs;
    //            break;
    //    }

    //    originalValue = EffectManager.GetValue(PassiveEffects.DismemberSelfChance, null, originalValue, this);
    //    float result = _dmResponse.Source.DismemberChance * damagePer * originalValue;
    //    EntityPlayerLocal entityPlayerLocal = world.GetEntity(_dmResponse.Source.getEntityId()) as EntityPlayerLocal;
    //    if ((bool)entityPlayerLocal && entityPlayerLocal.DebugDismembermentChance)
    //    {
    //        result = 1f;
    //    }

    //    return result;
    //}

    //public override void CheckDismember(ref DamageResponse _dmResponse, float damagePer)
    //{
    //    bool flag = _dmResponse.HitBodyPart.IsLeg();
    //    if (flag && IsAlive() && (bodyDamage.CurrentStun != 0 || sleepingOrWakingUp))
    //    {
    //        flag = false;
    //        return;
    //    }

    //    float dismemberChance = GetDismemberChance(ref _dmResponse, damagePer);
    //    if (dismemberChance > 0f && rand.RandomFloat <= dismemberChance)
    //    {
    //        _dmResponse.Dismember = true;
    //        if (flag)
    //        {
    //            _dmResponse.TurnIntoCrawler = true;
    //        }
    //    }
    //    else
    //    {
    //        if (!flag)
    //        {
    //            return;
    //        }

    //        EntityClass entityClass = EntityClass.list[base.entityClass];
    //        if (entityClass.LegCrawlerThreshold > 0f && GetDamageFraction(_dmResponse.Strength) >= entityClass.LegCrawlerThreshold)
    //        {
    //            _dmResponse.TurnIntoCrawler = true;
    //        }

    //        if (bodyDamage.ShouldBeCrawler || _dmResponse.TurnIntoCrawler || !(entityClass.LegCrippleScale > 0f))
    //        {
    //            return;
    //        }

    //        float num = GetDamageFraction(_dmResponse.Strength) * entityClass.LegCrippleScale;
    //        if (num >= 0.05f)
    //        {
    //            if ((bodyDamage.Flags & 0x1000) == 0 && _dmResponse.HitBodyPart.IsLeftLeg() && rand.RandomFloat < num)
    //            {
    //                _dmResponse.CrippleLegs = true;
    //            }

    //            if ((bodyDamage.Flags & 0x2000) == 0 && _dmResponse.HitBodyPart.IsRightLeg() && rand.RandomFloat < num)
    //            {
    //                _dmResponse.CrippleLegs = true;
    //            }
    //        }
    //    }
    //}

    //private void ApplyLocalBodyDamage(DamageResponse _dmResponse)
    //{
    //    EnumBodyPartHit enumBodyPartHit = _dmResponse.HitBodyPart;
    //    bodyDamage.bodyPartHit = enumBodyPartHit;
    //    bodyDamage.damageType = _dmResponse.Source.damageType;
    //    if (_dmResponse.Dismember)
    //    {
    //        if (DismembermentManager.DebugBodyPartHit != 0)
    //        {
    //            enumBodyPartHit = DismembermentManager.DebugBodyPartHit;
    //        }

    //        if ((enumBodyPartHit & EnumBodyPartHit.Head) > EnumBodyPartHit.None)
    //        {
    //            bodyDamage.Flags |= 1u;
    //        }

    //        if ((enumBodyPartHit & EnumBodyPartHit.LeftUpperArm) > EnumBodyPartHit.None)
    //        {
    //            bodyDamage.Flags |= 2u;
    //        }

    //        if ((enumBodyPartHit & EnumBodyPartHit.LeftLowerArm) > EnumBodyPartHit.None)
    //        {
    //            bodyDamage.Flags |= 4u;
    //        }

    //        if ((enumBodyPartHit & EnumBodyPartHit.RightUpperArm) > EnumBodyPartHit.None)
    //        {
    //            bodyDamage.Flags |= 8u;
    //        }

    //        if ((enumBodyPartHit & EnumBodyPartHit.RightLowerArm) > EnumBodyPartHit.None)
    //        {
    //            bodyDamage.Flags |= 16u;
    //        }

    //        if ((enumBodyPartHit & EnumBodyPartHit.LeftUpperLeg) > EnumBodyPartHit.None)
    //        {
    //            bodyDamage.Flags |= 32u;
    //            bodyDamage.ShouldBeCrawler = true;
    //        }

    //        if ((enumBodyPartHit & EnumBodyPartHit.LeftLowerLeg) > EnumBodyPartHit.None)
    //        {
    //            bodyDamage.Flags |= 64u;
    //            bodyDamage.ShouldBeCrawler = true;
    //        }

    //        if ((enumBodyPartHit & EnumBodyPartHit.RightUpperLeg) > EnumBodyPartHit.None)
    //        {
    //            bodyDamage.Flags |= 128u;
    //            bodyDamage.ShouldBeCrawler = true;
    //        }

    //        if ((enumBodyPartHit & EnumBodyPartHit.RightLowerLeg) > EnumBodyPartHit.None)
    //        {
    //            bodyDamage.Flags |= 256u;
    //            bodyDamage.ShouldBeCrawler = true;
    //        }
    //    }

    //    if (_dmResponse.TurnIntoCrawler)
    //    {
    //        bodyDamage.ShouldBeCrawler = true;
    //    }

    //    if (_dmResponse.CrippleLegs)
    //    {
    //        if (_dmResponse.HitBodyPart.IsLeftLeg())
    //        {
    //            bodyDamage.Flags |= 4096u;
    //        }

    //        if (_dmResponse.HitBodyPart.IsRightLeg())
    //        {
    //            bodyDamage.Flags |= 8192u;
    //        }
    //    }
    //}

    //protected new void ExecuteDismember(bool restoreState)
    //{
    //    if (!(emodel == null) && !(emodel.avatarController == null))
    //    {
    //        emodel.avatarController.DismemberLimb(bodyDamage, restoreState);
    //        if (bodyDamage.ShouldBeCrawler)
    //        {
    //            SetupCrawlerState(restoreState);
    //        }
    //    }
    //}

    //private void SetupCrawlerState(bool restoreState)
    //{
    //    if (!IsDead())
    //    {
    //        emodel.avatarController.TurnIntoCrawler(restoreState);
    //        SetMaxHeight(0.5f);
    //        ItemValue itemValue = null;
    //        if (EntityClass.list[entityClass].Properties.Values.ContainsKey(EntityClass.PropHandItemCrawler))
    //        {
    //            itemValue = ItemClass.GetItem(EntityClass.list[entityClass].Properties.Values[EntityClass.PropHandItemCrawler]);
    //            if (itemValue.IsEmpty())
    //            {
    //                itemValue = null;
    //            }
    //        }

    //        if (itemValue == null)
    //        {
    //            itemValue = ItemClass.GetItem("meleeHandZombie02");
    //        }

    //        inventory.SetBareHandItem(itemValue);
    //        TurnIntoCrawler();
    //    }

    //    walkType = 21;
    //}

    //protected override void TurnIntoCrawler()
    //{
    //}

    //public new void ClearStun()
    //{
    //    bodyDamage.CurrentStun = EnumEntityStunType.None;
    //    bodyDamage.StunDuration = 0f;
    //    SetCVar("_stunned", 0f);
    //}

    //public new void SetStun(EnumEntityStunType stun)
    //{
    //    bodyDamage.CurrentStun = stun;
    //    SetCVar("_stunned", 1f);
    //}

    //protected override void onSpawnStateChanged()
    //{
    //    if (m_addedToWorld)
    //    {
    //        StartStopLivingSound();
    //    }
    //}

    //private void StartStopLivingSound()
    //{
    //    if (soundLiving != null)
    //    {
    //        if (Spawned)
    //        {
    //            if (!IsDead() && Health > 0)
    //            {
    //                Manager.Play(this, soundLiving);
    //                soundLivingID = 0;
    //            }
    //        }
    //        else if (soundLivingID >= 0)
    //        {
    //            Manager.Stop(entityId, soundLiving);
    //            soundLivingID = -1;
    //        }
    //    }

    //    if (Spawned && soundSpawn != null && !SleeperSupressLivingSounds)
    //    {
    //        PlayOneShot(soundSpawn);
    //    }
    //}

    //public new void HeadHeightFixedUpdate()
    //{
    //    if (physicsBaseHeight <= 1.3f)
    //    {
    //        return;
    //    }

    //    float num = physicsBaseHeight;
    //    if (IsInElevator())
    //    {
    //        num *= 1.06f;
    //    }

    //    if (emodel.IsRagdollMovement || bodyDamage.CurrentStun == EnumEntityStunType.Prone)
    //    {
    //        num = physicsBaseHeight * 0.08f;
    //    }

    //    float num2 = m_characterController.GetRadius() * 0.9f;
    //    float num3 = num2 + 0.5f;
    //    float num4 = num + 0.22f - num3 - num2;
    //    Vector3 vector = PhysicsTransform.position;
    //    vector.y += num3;
    //    RaycastHit val = default(RaycastHit);
    //    bool flag = Physics.SphereCast(vector, num2, Vector3.up, out val, num4, 1083277312);
    //    if (flag)
    //    {
    //        Transform transform = val.transform;
    //        if ((bool)transform && transform.CompareTag("Physics"))
    //        {
    //            Entity component = transform.GetComponent<Entity>();
    //            if (component != null)
    //            {
    //                component.PhysicsPush(transform.forward * 0.1f * Time.fixedDeltaTime, val.point, affectLocalPlayerController: true);
    //            }

    //            return;
    //        }

    //        if (world.GetBlock(new Vector3i(val.point + Origin.position)).Block.Damage <= 0f)
    //        {
    //            num = num3 + val.distance - 0.16f;
    //        }
    //    }

    //    if (num < physicsHeight)
    //    {
    //        if (IsInElevator())
    //        {
    //            return;
    //        }

    //        num = Mathf.MoveTowards(physicsHeight, num, 0.03333333f);
    //    }
    //    else
    //    {
    //        num = Mathf.MoveTowards(physicsHeight, num, 0.0142857144f);
    //    }

    //    SetHeight(num);
    //    if (flag && num <= physicsBaseHeight * 0.75f)
    //    {
    //        int num5 = 22;
    //        if (walkType != num5 && walkType != 21)
    //        {
    //            walkTypeBeforeCrouch = walkType;
    //            SetWalkType(num5);
    //        }
    //    }
    //    else if (walkTypeBeforeCrouch != 0 && num >= physicsBaseHeight)
    //    {
    //        SetWalkType(walkTypeBeforeCrouch);
    //        walkTypeBeforeCrouch = 0;
    //    }
    //}

    //private void SetWalkType(int _walkType)
    //{
    //    walkType = _walkType;
    //    emodel.avatarController.SetWalkType(_walkType, _trigger: true);
    //}

    //public new int GetWalkType()
    //{
    //    return walkType;
    //}

    //public new bool IsWalkTypeACrawl()
    //{
    //    return walkType >= 20;
    //}

    //public new string GetRightHandTransformName()
    //{
    //    return rightHandTransformName;
    //}

    //protected override bool isGameMessageOnDeath()
    //{
    //    return true;
    //}

    //public override float GetLightBrightness()
    //{
    //    Vector3i blockPosition = GetBlockPosition();
    //    Vector3i blockPos = blockPosition;
    //    blockPos.y += Mathf.RoundToInt(base.height + 0.5f);
    //    float num = Utils.FastMax(world.GetLightBrightness(blockPosition), world.GetLightBrightness(blockPos));
    //    if (equipment.IsItemActive())
    //    {
    //        num = Utils.FastMax(0.3f, num);
    //    }

    //    return num;
    //}

    //public override float GetLightLevel()
    //{
    //    EntityAlive entityAlive = AttachedToEntity as EntityAlive;
    //    if ((bool)entityAlive)
    //    {
    //        return entityAlive.GetLightLevel();
    //    }

    //    return inventory.GetLightLevel();
    //}

    //public override int AttachToEntity(Entity _other, int slot = -1)
    //{
    //    slot = base.AttachToEntity(_other, slot);
    //    if (slot >= 0)
    //    {
    //        CurrentMovementTag = MovementTagIdle;
    //        saveInventory = null;
    //        if (_other is EntityAlive && _other.GetAttachedToInfo(slot).bReplaceLocalInventory)
    //        {
    //            saveInventory = inventory;
    //            saveHoldingItemIdxBeforeAttach = inventory.holdingItemIdx;
    //            inventory.SetHoldingItemIdxNoHolsterTime(inventory.DUMMY_SLOT_IDX);
    //            inventory = ((EntityAlive)_other).inventory;
    //        }

    //        bPlayerStatsChanged |= !isEntityRemote;
    //    }

    //    return slot;
    //}

    //public override void Detach()
    //{
    //    if (saveInventory != null)
    //    {
    //        inventory = saveInventory;
    //        inventory.SetHoldingItemIdxNoHolsterTime(saveHoldingItemIdxBeforeAttach);
    //        saveInventory = null;
    //    }

    //    base.Detach();
    //    bPlayerStatsChanged |= !isEntityRemote;
    //}

    //public override void Write(BinaryWriter _bw, bool _bNetworkWrite)
    //{
    //    base.Write(_bw, _bNetworkWrite);
    //    _bw.Write(deathHealth);
    //}

    //public override void Read(byte _version, BinaryReader _br)
    //{
    //    base.Read(_version, _br);
    //    if (_version > 24)
    //    {
    //        deathHealth = _br.ReadInt32();
    //    }
    //}

    //public override string ToString()
    //{
    //    return $"[type={GetType().Name}, name={GameUtils.SafeStringFormat(EntityName)}, id={entityId}]";
    //}

    //public new float GetCVar(string _varName)
    //{
    //    if (Buffs == null)
    //    {
    //        return 0f;
    //    }

    //    return Buffs.GetCustomVar(_varName);
    //}

    //public new void SetCVar(string _varName, float _value)
    //{
    //    if (Buffs != null)
    //    {
    //        Buffs.SetCustomVar(_varName, _value);
    //    }
    //}



    //public new void AddParticle(string _name, Transform _t)
    //{
    //    if (particles.ContainsKey(_name))
    //    {
    //        particles[_name] = _t;
    //    }
    //    else
    //    {
    //        particles.Add(_name, _t);
    //    }
    //}

    //public new bool RemoveParticle(string _name)
    //{
    //    if (particles.TryGetValue(_name, out var value))
    //    {
    //        particles.Remove(_name);
    //        if ((bool)value)
    //        {
    //            UnityEngine.Object.Destroy(value.gameObject);
    //        }

    //        return true;
    //    }

    //    return false;
    //}

    //public new bool HasParticle(string _name)
    //{
    //    if (particles.TryGetValue(_name, out var _))
    //    {
    //        return true;
    //    }

    //    return false;
    //}

    //public new void AddPart(string _name, Transform _t)
    //{
    //    if (parts.ContainsKey(_name))
    //    {
    //        parts[_name] = _t;
    //    }
    //    else
    //    {
    //        parts.Add(_name, _t);
    //    }
    //}

    //public new void RemovePart(string _name)
    //{
    //    if (parts.TryGetValue(_name, out var value))
    //    {
    //        parts.Remove(_name);
    //        if ((bool)value)
    //        {
    //            value.gameObject.name = ".";
    //            UnityEngine.Object.Destroy(value.gameObject);
    //        }
    //    }
    //}

    //public new void SetPartActive(string _name, bool isActive)
    //{
    //    if (parts.TryGetValue(_name, out var value) && (bool)value)
    //    {
    //        value.gameObject.SetActive(isActive);
    //    }
    //}

    //public new void AddOwnedEntity(OwnedEntityData _entityData)
    //{
    //    if (_entityData != null)
    //    {
    //        ownedEntities.Add(_entityData);
    //    }
    //}

    //public new void AddOwnedEntity(Entity _entity)
    //{
    //    if (ownedEntities.Find((OwnedEntityData e) => e.Id == _entity.entityId) == null)
    //    {
    //        AddOwnedEntity(new OwnedEntityData(_entity));
    //    }
    //}

    //public new void RemoveOwnedEntity(OwnedEntityData _entityData)
    //{
    //    if (_entityData != null)
    //    {
    //        ownedEntities.Remove(_entityData);
    //    }
    //}

    //public new void RemoveOwnedEntity(int _entityId)
    //{
    //    RemoveOwnedEntity(ownedEntities.Find((OwnedEntityData e) => e.Id == _entityId));
    //}

    //public new void RemoveOwnedEntity(Entity _entity)
    //{
    //    RemoveOwnedEntity(_entity.entityId);
    //}

    //public new OwnedEntityData GetOwnedEntity(int _entityId)
    //{
    //    return ownedEntities.Find((OwnedEntityData e) => e.Id == _entityId);
    //}

    //public new OwnedEntityData[] GetOwnedEntityClass(string name)
    //{
    //    List<OwnedEntityData> list = new List<OwnedEntityData>();
    //    for (int i = 0; i < ownedEntities.Count; i++)
    //    {
    //        OwnedEntityData ownedEntityData = ownedEntities[i];
    //        if (EntityClass.list[ownedEntityData.ClassId].entityClassName.ContainsCaseInsensitive(name))
    //        {
    //            list.Add(ownedEntityData);
    //        }
    //    }

    //    return list.ToArray();
    //}

    //public new bool HasOwnedEntity(int _entityId)
    //{
    //    return GetOwnedEntity(_entityId) != null;
    //}

    //public new OwnedEntityData[] GetOwnedEntities()
    //{
    //    return ownedEntities.ToArray();
    //}

    //public new void ClearOwnedEntities()
    //{
    //    ownedEntities.Clear();
    //}

    //public new void HandleSetNavName()
    //{
    //    if (NavObject != null)
    //    {
    //        NavObject.name = entityName;
    //    }
    //}

    //private void UpdateDynamicRagdoll()
    //{
    //    if (_dynamicRagdoll.HasFlag(DynamicRagdollFlags.Active))
    //    {
    //        if (accumulatedRootMotion != Vector3.zero)
    //        {
    //            _dynamicRagdollRootMotion = accumulatedRootMotion;
    //        }

    //        if (_dynamicRagdoll.HasFlag(DynamicRagdollFlags.UseBoneVelocities))
    //        {
    //            _ragdollPositionsPrev.Clear();
    //            _ragdollPositionsCur.CopyTo(_ragdollPositionsPrev);
    //            emodel.CaptureRagdollPositions(_ragdollPositionsCur);
    //        }

    //        if (_dynamicRagdoll.HasFlag(DynamicRagdollFlags.RagdollOnFall) && !onGround)
    //        {
    //            ActivateDynamicRagdoll();
    //        }
    //    }
    //}

    //protected override void AnalyticsSendDeath(DamageResponse _dmResponse)
    //{
    //}

    //public override string MakeDebugNameInfo()
    //{
    //    return string.Empty;
    //}


    //public new EModelBase.HeadStates GetHeadState()
    //{
    //    if (base.EntityClass.CanBigHead)
    //    {
    //        return emodel.HeadState;
    //    }

    //    return EModelBase.HeadStates.Standard;
    //}


    //public new void SetDancing(bool enabled)
    //{
    //    if (base.EntityClass.DanceTypeID != 0)
    //    {
    //        IsDancing = enabled;
    //    }
    //    else
    //    {
    //        IsDancing = false;
    //    }
    //}

    //public new void SetSpawnByData(int newSpawnByID, string newSpawnByName)
    //{
    //    spawnById = newSpawnByID;
    //    spawnByName = newSpawnByName;
    //    bPlayerStatsChanged |= !isEntityRemote;
    //}


}

