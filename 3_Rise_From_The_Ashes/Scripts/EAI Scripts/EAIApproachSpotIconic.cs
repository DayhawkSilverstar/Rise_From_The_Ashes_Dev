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
    public int investigateTicks;

    private string TaskName => nameof(EAIApproachSpotIconic);

    public override void Init(EntityAlive _theEntity)
    {
        base.Init(_theEntity);
        MutexBits = 3;
        executeDelay = 0.1f;        
    }

    public override bool CanExecute()
    {
        if (!theEntity.HasInvestigatePosition)
        {
            return false;
        }

        if (theEntity.IsSleeping)
        {
            return false;
        }

        investigatePos = theEntity.InvestigatePosition;
        seekPos = theEntity.world.FindSupportingBlockPos(investigatePos);        
        return true;
    }

    public override void Start()
    {
        investigateTicks = 0;        
    }

    public override bool Continue()
    {
        if (++investigateTicks > 40)
        {
            investigateTicks = 0;
            if (!theEntity.HasInvestigatePosition)
            {
                return false;
            }

            // Check if investigate position changed significantly
            if ((investigatePos - theEntity.InvestigatePosition).sqrMagnitude >= 4f)
            {
                return false;
            }
        }

        // Check if we've reached the destination
        Vector3 diff = seekPos - theEntity.position;
        diff.y = 0f; // Only check horizontal distance
        if (diff.sqrMagnitude <= 4f)
        {
            theEntity.ClearInvestigatePosition();
            return false;
        }

        return true;
    }

    public override void Update()
    {
        // Recalculate seek position periodically in case investigate position changed
        if (investigateTicks % 10 == 0 && theEntity.HasInvestigatePosition)
        {
            investigatePos = theEntity.InvestigatePosition;
            seekPos = theEntity.world.FindSupportingBlockPos(investigatePos);
        }
        
        // Calculate direction to target (horizontal only)
        Vector3 direction = seekPos - theEntity.position;
        direction.y = 0f; // Ignore vertical component for truly dumb movement
        
        if (direction.sqrMagnitude > 0.01f)
        {
            direction.Normalize();
            
            // Apply movement directly without pathfinding
            theEntity.MoveEntityHeaded(direction, true);
            
            if (theEntity.IsScoutZombie)
            {
                AstarManager.Instance.AddLocationLine(theEntity.position, seekPos, 32);
            }
        }

        // Calculate if position is unreachable (blocked)
        theEntity.moveHelper.CalcIfUnreachablePos();

        // Look at the investigate position
        Vector3 lookPosition = investigatePos;
        lookPosition.y += 0.8f;
        theEntity.SetLookPosition(lookPosition);
    }

    public override void Reset()
    {
        theEntity.moveHelper.Stop();
        theEntity.SetLookPosition(Vector3.zero);
        manager.lookTime = 5f + base.RandomFloat * 3f;
        manager.interestDistance = 2f;        
    }

    public override string ToString()
    {
        float distance = (theEntity.position - seekPos).magnitude;
        return string.Format("{0}, (direct) dist {1}", base.ToString(), distance.ToCultureInvariantString("0.00"));
    }
}