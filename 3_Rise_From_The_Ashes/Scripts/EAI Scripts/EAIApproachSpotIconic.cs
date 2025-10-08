using GamePath;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class EAIApproachSpotIconic : EAIBase
{
    [PublicizedFrom(EAccessModifier.Private)]
    public const float cInvestigateChangeDist = 2f;

    [PublicizedFrom(EAccessModifier.Private)]
    public const float cCloseDist = 2f;

    [PublicizedFrom(EAccessModifier.Private)]
    public const float cLookTimeMin = 5f;

    [PublicizedFrom(EAccessModifier.Private)]
    public const float cLookTimeMax = 8f;

    [PublicizedFrom(EAccessModifier.Private)]
    public Vector3 investigatePos;

    [PublicizedFrom(EAccessModifier.Private)]
    public Vector3 seekPos;

    [PublicizedFrom(EAccessModifier.Private)]
    public bool hadPath;

    [PublicizedFrom(EAccessModifier.Private)]
    public int investigateTicks;

    [PublicizedFrom(EAccessModifier.Private)]
    public int pathRecalculateTicks;

    private string TaskName => nameof(EAIApproachSpotIconic);

    public override void Init(EntityAlive _theEntity)
    {
        base.Init(_theEntity);
        MutexBits = 3;
        executeDelay = 0.1f;
        IconicLog.Info(theEntity, TaskName, $"Init: mutex={MutexBits} execDelay={executeDelay}");
    }

    public override bool CanExecute()
    {
        if (!theEntity.HasInvestigatePosition)
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: no investigate position");
            return false;
        }

        if (theEntity.IsSleeping)
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: sleeping");
            return false;
        }

        investigatePos = theEntity.InvestigatePosition;
        seekPos = theEntity.world.FindSupportingBlockPos(investigatePos);
        IconicLog.Debug(theEntity, TaskName, $"CanExecute=true: investigatePos={investigatePos} seekPos={seekPos}");
        return true;
    }

    public override void Start()
    {
        hadPath = false;
        updatePath();
        IconicLog.Info(theEntity, TaskName, $"Start: seekPos={seekPos}");
    }

    public override bool Continue()
    {
        PathEntity path = theEntity.navigator.getPath();
        if (hadPath && path == null)
        {
            IconicLog.Trace(theEntity, TaskName, "Continue=false: lost path");
            return false;
        }

        if (++investigateTicks > 40)
        {
            investigateTicks = 0;
            if (!theEntity.HasInvestigatePosition)
            {
                IconicLog.Trace(theEntity, TaskName, "Continue=false: investigate position cleared");
                return false;
            }

            if ((investigatePos - theEntity.InvestigatePosition).sqrMagnitude >= 4f)
            {
                IconicLog.Trace(theEntity, TaskName, "Continue=false: investigate target moved");
                return false;
            }
        }

        if ((seekPos - theEntity.position).sqrMagnitude <= 4f || (path != null && path.isFinished()))
        {
            theEntity.ClearInvestigatePosition();
            IconicLog.Trace(theEntity, TaskName, "Continue=false: reached seekPos or finished path");
            return false;
        }

        return true;
    }

    public override void Update()
    {
        if (theEntity.navigator.getPath() != null)
        {
            hadPath = true;
            theEntity.moveHelper.CalcIfUnreachablePos();
        }

        Vector3 lookPosition = investigatePos;
        lookPosition.y += 0.8f;
        theEntity.SetLookPosition(lookPosition);
        if (--pathRecalculateTicks <= 0)
        {
            updatePath();
        }
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public void updatePath()
    {
        if (theEntity.IsScoutZombie)
        {
            AstarManager.Instance.AddLocationLine(theEntity.position, seekPos, 32);
        }

        if (!PathFinderThread.Instance.IsCalculatingPath(theEntity.entityId))
        {
            pathRecalculateTicks = 40 + GetRandom(20);
            theEntity.FindPath(seekPos, theEntity.GetMoveSpeedAggro(), canBreak: true, this);
            IconicLog.Trace(theEntity, TaskName, $"updatePath: seekPos={seekPos} nextRecalcTicks={pathRecalculateTicks}");
        }
    }

    public override void Reset()
    {
        theEntity.moveHelper.Stop();
        theEntity.SetLookPosition(Vector3.zero);
        manager.lookTime = 5f + base.RandomFloat * 3f;
        manager.interestDistance = 2f;
        IconicLog.Info(theEntity, TaskName, $"Reset: lookTime={manager.lookTime:0.00} interest={manager.interestDistance}");
    }

    public override string ToString()
    {
        return string.Format("{0}, {1} dist{2}", base.ToString(), theEntity.navigator.noPathAndNotPlanningOne() ? "(-path)" : (theEntity.navigator.noPath() ? "(!path)" : ""), (theEntity.position - seekPos).magnitude.ToCultureInvariantString());
    }
}