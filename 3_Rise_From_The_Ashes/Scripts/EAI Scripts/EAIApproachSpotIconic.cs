using GamePath;
using UnityEngine;
using UnityEngine.Scripting;
using static BlockPlacement;

public class EAIApproachSpotIconic : EAIApproachSpot
{

    private int pathRecalculateTicks;
    private IconicZombie zombie;
    private bool hadPath;

    public override void Start()
    {
#if DEBUG
        Log.Out("EAIApproachSpotIconic : Start");
#endif
        base.Start();
    }

    public override void Init(EntityAlive _theEntity)
    {
#if DEBUG
        Log.Out("EAIApproachSpotIconic : Init");
#endif
        base.Init(_theEntity);
        zombie = _theEntity as IconicZombie;
    }

    public override bool Continue()
    {
#if DEBUG
        Log.Out("EAIApproachSpotIconic : Continue");       
#endif
        return base.Continue();
    }

    public override void Update()
    {
#if DEBUG
        Log.Out("EAIApproachSpotIconic : Update");
#endif
        base.Update();
    }



    public override bool CanExecute()
    {
#if DEBUG
        Log.Out("EAIApproachSpotIconic : CanExecute");
#endif
        bool result;        
        result = base.CanExecute();
        return result;
    }
}

