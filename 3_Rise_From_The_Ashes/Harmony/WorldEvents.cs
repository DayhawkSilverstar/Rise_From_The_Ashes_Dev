using HarmonyLib;
using JetBrains.Annotations;
using System;
using UnityEngine.PlayerLoop;

namespace RiseFromTheAshes
{

    [HarmonyPatch(typeof(GameManager))]
    [HarmonyPatch("Update")]
    public class OnGameManager
    {
        private static void Postfix(GameManager __instance)
        {
            DynamicEventManager.Instance.Update();   
            RadioManager.Instance.Update();
        }        
    }
}
