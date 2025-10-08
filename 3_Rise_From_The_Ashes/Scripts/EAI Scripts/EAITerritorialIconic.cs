using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class EAITerritorialIconic : EAIBase
{
    [PublicizedFrom(EAccessModifier.Private)]
    public Vector3 movePos;

    private string TaskName => nameof(EAITerritorialIconic);

    public EAITerritorialIconic()
    {
        MutexBits = 1;
    }

    public override void SetData(DictionarySave<string, string> data)
    {
        base.SetData(data);
        IconicLog.Info(theEntity, TaskName, "SetData: (no extra fields)");
    }

    public override bool CanExecute()
    {
        if (theEntity.isWithinHomeDistanceCurrentPosition())
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: already within home distance");
            return false;
        }

        ChunkCoordinates homePosition = theEntity.getHomePosition();
        Vector3 vector = RandomPositionGenerator.CalcTowards(theEntity, 5, 15, 7, homePosition.position.ToVector3());
        if (vector.Equals(Vector3.zero))
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: CalcTowards returned zero");
            return false;
        }

        movePos = vector;
        IconicLog.Debug(theEntity, TaskName, $"CanExecute=true: movePos={movePos}");
        return true;
    }

    public override bool Continue()
    {
        bool cont = !theEntity.getNavigator().noPathAndNotPlanningOne();
        if (!cont) IconicLog.Trace(theEntity, TaskName, "Continue=false: no path and not planning one");
        return cont;
    }

    public override void Start()
    {
        theEntity.FindPath(movePos, theEntity.GetMoveSpeed(), canBreak: false, this);
        IconicLog.Info(theEntity, TaskName, $"Start: path to {movePos}");
    }
}