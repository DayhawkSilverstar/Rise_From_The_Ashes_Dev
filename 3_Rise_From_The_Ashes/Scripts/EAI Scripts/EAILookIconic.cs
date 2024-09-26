using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class EAILookIconic : EAILook
{
    public IconicZombie zombie;

    public override void Init(EntityAlive _theEntity)
    {
#if DEBUG
        Log.Out("EAILookIconic : Init");
#endif
        base.Init(_theEntity);
        MutexBits = 1;
        zombie = _theEntity as IconicZombie;
    }

    public override void Start()
    {
#if DEBUG
        Log.Out("EAILookIconic : Start");
#endif
        base.Start();
    }

    public override bool CanExecute()
    {
#if DEBUG
        Log.Out("EAILookIconic : CanExecute");
#endif
        bool result;        
        if (zombie.Target != null)
        {
            result = false;
        }
        
        if (zombie.IsBreakingBlocks)
        {
            result = false;
        }

        return false;
    }

    public override bool Continue()
    {
#if DEBUG
        Log.Out("EAILookIconic : Continue");   
#endif
        return CanExecute();
    }

    public override void Update()
    {
#if DEBUG
        Log.Out("EAILookIconic : Update");
#endif
        base.Update();
    }

}
