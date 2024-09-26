using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace RiseFromTheAshes.Harmony
{
    
    //[HarmonyPatch(typeof(EntityMoveHelper))]
    //[HarmonyPatch("FindDestroyPos")]   
    //public class MoveHelperSnooping
    //{
    //    public static bool Prefix(ref Vector3 destroyPos, bool isLookFar, ref EntityMoveHelper __instance)
    //    {
    //        System.Diagnostics.StackTrace t = new System.Diagnostics.StackTrace();
    //        Log.Out("Prefix activated.   Running FindDestroyPos for : {0}", __instance.BlockedEntity.EntityName);
    //        Log.Out("Stack Trace         Running FindDestroyPos     : {0}", t.ToString());

    //        return false;
    //   }
    //}
    
}
