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

    public override void Init(EntityAlive _theEntity)
    {
        base.Init(_theEntity);
        MutexBits = 1;
        IconicLog.Info(theEntity, TaskName, $"Init: mutex={MutexBits}");
    }

    public override void SetData(DictionarySave<string, string> data)
    {
        base.SetData(data);
        GetData(data, "exePer", ref executePercent);
        GetData(data, "fade", ref fade);
        GetData(data, "lookMin", ref lookMin);
        GetData(data, "lookMax", ref lookMax);
        IconicLog.Info(theEntity, TaskName, $"SetData: exePer={executePercent} fade={fade} lookMin={lookMin} lookMax={lookMax}");
    }

    public override bool CanExecute()
    {
        if (theEntity.sleepingOrWakingUp)
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: sleepingOrWakingUp");
            return false;
        }

        if (manager.lookTime > 0f)
        {
            IconicLog.Trace(theEntity, TaskName, $"CanExecute=false: lookTime={manager.lookTime:0.00}");
            return false;
        }

        if (fade == 1f && theEntity.GetTicksNoPlayerAdjacent() >= 120)
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: no player adjacent long enough");
            return false;
        }

        if (theEntity.bodyDamage.CurrentStun != 0)
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: stunned");
            return false;
        }

        bool isAlert = theEntity.IsAlert;
        if (!isAlert && executePercent * executeWaitTime <= base.RandomFloat)
        {
            IconicLog.Trace(theEntity, TaskName, $"CanExecute=false: random gate exePer={executePercent} wait={executeWaitTime:0.00}");
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
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: CalcInDir returned y=0");
            return false;
        }

        position = vector;
        IconicLog.Debug(theEntity, TaskName, $"CanExecute=true: wanderPos={position}");
        return true;
    }

    public override void Start()
    {
        time = 0f;
        theEntity.FindPath(position, theEntity.GetMoveSpeed(), canBreak: false, this);
        theEntity.renderFadeMax = fade;
        IconicLog.Info(theEntity, TaskName, $"Start: path to {position} fade={fade}");
    }

    public override bool Continue()
    {
        if (theEntity.bodyDamage.CurrentStun != 0)
        {
            IconicLog.Trace(theEntity, TaskName, "Continue=false: stunned");
            return false;
        }

        if (theEntity.moveHelper.BlockedTime > 0.3f)
        {
            IconicLog.Trace(theEntity, TaskName, $"Continue=false: blockedTime={theEntity.moveHelper.BlockedTime:0.00}");
            return false;
        }

        if (time > 30f)
        {
            IconicLog.Trace(theEntity, TaskName, "Continue=false: time exceeded");
            return false;
        }

        bool cont = !theEntity.navigator.noPathAndNotPlanningOne();
        if (!cont) IconicLog.Trace(theEntity, TaskName, "Continue=false: no path and not planning one");
        return cont;
    }

    public override void Update()
    {
        time += 0.05f;
    }

    public override void Reset()
    {
        manager.lookTime = base.Random.RandomRange(lookMin, lookMax);
        theEntity.moveHelper.Stop();
        theEntity.renderFadeMax = 1f;
        IconicLog.Info(theEntity, TaskName, $"Reset: nextLookTime={manager.lookTime:0.00}");
    }
}