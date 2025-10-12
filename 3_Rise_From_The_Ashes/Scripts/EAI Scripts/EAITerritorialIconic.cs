using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class EAITerritorialIconic : EAIBase
{
    [PublicizedFrom(EAccessModifier.Private)]
    public Vector3 movePos;

    [PublicizedFrom(EAccessModifier.Private)]
    public int moveUpdateTicks;

    private string TaskName => nameof(EAITerritorialIconic);

    public EAITerritorialIconic()
    {
        MutexBits = 1;
    }

    public override void SetData(DictionarySave<string, string> data)
    {
        base.SetData(data);
        Log.Out($"[{TaskName}] SetData: (no extra fields)");
    }

    public override bool CanExecute()
    {
        if (theEntity.isWithinHomeDistanceCurrentPosition())
        {
            return false;
        }

        ChunkCoordinates homePosition = theEntity.getHomePosition();
        Vector3 vector = RandomPositionGenerator.CalcTowards(theEntity, 5, 15, 7, homePosition.position.ToVector3());
        if (vector.Equals(Vector3.zero))
        {
            return false;
        }

        movePos = vector;
        Log.Out($"[{TaskName}] id={theEntity?.entityId ?? -1} CanExecute=true: movePos={movePos}");
        return true;
    }

    public override bool Continue()
    {
        // Check if we've reached the destination (within 2 blocks horizontal distance)
        Vector3 diff = movePos - theEntity.position;
        diff.y = 0f; // Only check horizontal distance
        
        bool shouldContinue = diff.sqrMagnitude > 4f; // 2 blocks squared
        
        if (!shouldContinue)
        {
            Log.Out($"[{TaskName}] id={theEntity?.entityId ?? -1} Continue=false: reached destination");
        }
        
        return shouldContinue;
    }

    public override void Start()
    {
        moveUpdateTicks = 0;
        Log.Out($"[{TaskName}] id={theEntity?.entityId ?? -1} Start: direct move to {movePos}");
    }

    public override void Update()
    {
        // Calculate direction to target (horizontal only)
        Vector3 direction = movePos - theEntity.position;
        direction.y = 0f; // Ignore vertical component for truly dumb movement
        
        if (direction.sqrMagnitude > 0.01f)
        {
            direction.Normalize();
            
            // Apply movement directly without pathfinding
            // Use MoveEntityHeaded for direct control
            theEntity.MoveEntityHeaded(direction, true);
        }

        // Calculate if position is unreachable (blocked)
        theEntity.moveHelper.CalcIfUnreachablePos();
    }

    public override void Reset()
    {
        theEntity.moveHelper.Stop();
        Log.Out($"[{TaskName}] id={theEntity?.entityId ?? -1} Reset: stopped movement");
    }

    public override string ToString()
    {
        float distance = (theEntity.position - movePos).magnitude;
        return string.Format("{0}, (direct) dist {1}", base.ToString(), distance.ToCultureInvariantString("0.00"));
    }
}