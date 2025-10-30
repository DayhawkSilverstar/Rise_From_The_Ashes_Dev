using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class EAIWanderIconic : EAIBase
{
    [PublicizedFrom(EAccessModifier.Private)]
    public float fade = 1f;

    [PublicizedFrom(EAccessModifier.Private)]
    public float lookMin = 0.5f;

    [PublicizedFrom(EAccessModifier.Private)]
    public float lookMax = 5f;

    [PublicizedFrom(EAccessModifier.Private)]
    public float executePercent = 0.2f;

    [PublicizedFrom(EAccessModifier.Private)]
    public Vector3 position;

    [PublicizedFrom(EAccessModifier.Private)]
    public float time;

    private string TaskName => nameof(EAIWanderIconic);
    
    // STATE TRANSITION TRACKING
    private static bool enableStateTransitionLogging = true;

    public override void Init(EntityAlive _theEntity)
    {
        base.Init(_theEntity);
        MutexBits = 1;
        Log.Out($"[{TaskName}] id={_theEntity.entityId} Init: mutex={MutexBits}");
    }

    public override void SetData(DictionarySave<string, string> data)
    {
        base.SetData(data);
        GetData(data, "exePer", ref executePercent);
        GetData(data, "fade", ref fade);
        GetData(data, "lookMin", ref lookMin);
        GetData(data, "lookMax", ref lookMax);
        Log.Out($"[{TaskName}] SetData: exePer={executePercent} fade={fade} lookMin={lookMin} lookMax={lookMax}");
    }

    public override bool CanExecute()
    {
        if (theEntity.sleepingOrWakingUp)
        {
            return false;
        }

        if (manager.lookTime > 0f)
        {
            return false;
        }

        if (fade == 1f && theEntity.GetTicksNoPlayerAdjacent() >= 120)
        {
            return false;
        }

        if (theEntity.bodyDamage.CurrentStun != 0)
        {
            return false;
        }

        bool isAlert = theEntity.IsAlert;
        if (!isAlert && executePercent * executeWaitTime <= base.RandomFloat)
        {
            return false;
        }

        int minXZ = 1;
        int num = (int)manager.interestDistance;
        if (isAlert)
        {
            minXZ = 2;
            num *= 2;
        }

        Vector3 dirV = ((base.RandomFloat < 0.6f) ? theEntity.GetForwardVector() : base.Random.RandomOnUnitCircleXZ);
        Vector3 vector = RandomPositionGenerator.CalcInDir(theEntity, minXZ, num, num, dirV, 90f);
        if (vector.y == 0f)
        {
            return false;
        }

        position = vector;
        Log.Out($"[{TaskName}] id={theEntity?.entityId ?? -1} CanExecute=true: position={position} alert={isAlert}");
        return true;
    }

    public override void Start()
    {
        // STATE TRANSITION LOG
        if (enableStateTransitionLogging && !theEntity.isEntityRemote)
        {
            Log.Out($"[EAI-STATE] Entity:{theEntity.entityId} Wander STARTING");
        }
        
        time = 0f;
        theEntity.renderFadeMax = fade;
        Log.Out($"[{TaskName}] id={theEntity?.entityId ?? -1} Start: direct move to {position}");
    }

    public override bool Continue()
    {
        if (theEntity.bodyDamage.CurrentStun != 0)
        {
            return false;
        }

        if (theEntity.moveHelper.BlockedTime > 0.3f)
        {
            return false;
        }

        if (time > 30f)
        {
            return false;
        }

        // Check if we've reached the destination (within 2 blocks horizontal distance)
        Vector3 diff = position - theEntity.position;
        diff.y = 0f; // Only check horizontal distance
        
        bool shouldContinue = diff.sqrMagnitude > 4f; // 2 blocks squared
        
        if (!shouldContinue)
        {
            Log.Out($"[{TaskName}] id={theEntity?.entityId ?? -1} Continue=false: reached destination");
        }
        
        return shouldContinue;
    }

    public override void Update()
    {
        time += 0.05f;
        
        // Calculate direction to target (horizontal only)
        Vector3 direction = position - theEntity.position;
        direction.y = 0f; // Ignore vertical component for truly dumb movement
        
        if (direction.sqrMagnitude > 0.01f)
        {
            direction.Normalize();
            
            // Apply movement directly without pathfinding
            theEntity.MoveEntityHeaded(direction, true);
        }

        // Calculate if position is unreachable (blocked)
        theEntity.moveHelper.CalcIfUnreachablePos();
    }

    public override void Reset()
    {
        // STATE TRANSITION LOG
        if (enableStateTransitionLogging && !theEntity.isEntityRemote)
        {
            Log.Out($"[EAI-STATE] Entity:{theEntity.entityId} Wander STOPPING");
        }
        
        manager.lookTime = base.Random.RandomRange(lookMin, lookMax);
        theEntity.moveHelper.Stop();
        theEntity.renderFadeMax = 1f;
        Log.Out($"[{TaskName}] id={theEntity?.entityId ?? -1} Reset: lookTime={manager.lookTime:0.00}");
    }

    public override string ToString()
    {
        float distance = (theEntity.position - position).magnitude;
        return string.Format("{0}, (direct) dist {1} time {2:0.0}", base.ToString(), distance.ToCultureInvariantString("0.00"), time);
    }
}