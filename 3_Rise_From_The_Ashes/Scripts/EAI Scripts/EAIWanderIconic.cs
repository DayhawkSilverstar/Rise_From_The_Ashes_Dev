using System;
using System.Collections.Generic;
using UnityEngine;

public class EAIWanderIconic : EAIWander
{
    
    
    public IconicZombie zombie;
    private const float cLookTimeMax = 3f;
    private Vector3 position;
    private float time;

    public override void Start()
    {
#if DEBUG
        Log.Out("EAIWanderIconic : Start");
#endif
        time = 0f;
        position = zombie.position;
    }

    public override void Init(EntityAlive _theEntity)
    {
#if DEBUG
        Log.Out("EAIWanderIconic : Init");
#endif
        base.Init(_theEntity);
        MutexBits = 1;
        zombie = _theEntity as IconicZombie;
    }


    public override bool CanExecute()
    {
#if DEBUG
        Log.Out("EAIWanderIconic : CanExecute");
#endif
        if (zombie.sleepingOrWakingUp)
        {
            //Log.Out("EAIWanderIconic : CanExecute - false");
            return false;
        }
     
        if (zombie.bodyDamage.CurrentStun != 0)
        {
           // Log.Out("EAIWanderIconic : CanExecute - false");
            return false;
        }

        foreach (EntityPlayer entity in zombie.GetEntities())
        {
            zombie.SeekNoise(entity);
        }        
        
        if (zombie.Target == null)
        {
            return false;
        }
              
        return true;
    }

    public override bool Continue()
    {
#if DEBUG
        Log.Out("EAIWanderIconic : Continue");
#endif
        if (zombie.sleepingOrWakingUp || zombie.bodyDamage.CurrentStun != 0)
        {
            return false;
        }

        if (zombie.Target != null)
        {
            return false;
        }        

        return true;
    }

    public override void Update()
    {      
        if (zombie.GetDistanceSq(position) < 2f)
        {
            float distance = UnityEngine.Random.Range(3f, 10f);
            float angle = UnityEngine.Random.Range(0f, 360f);
            Vector3 direction = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle));
            position = zombie.position + direction * distance;
        }

        if (zombie.Target != null)
        {
            return;
        }   

        // if the zombie has limbs and is not stunned then rotate to the target position.
        if (zombie.bodyDamage.HasLimbs && zombie.bodyDamage.CurrentStun == 0)
        {
            zombie.RotateTo(position.x, position.y, position.z, 8f, 5f);
        }

        zombie.SleeperSupressLivingSounds = false;

        zombie.SetMoveTo(position, true);
    }

    public override void Reset()
    {
#if DEBUG
        Log.Out("EAIWanderIconic : Reset");
#endif
        manager.lookTime = base.RandomFloat * 3f;
        zombie.moveHelper.Stop();
    }
}

