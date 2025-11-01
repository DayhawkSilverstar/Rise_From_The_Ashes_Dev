using HarmonyLib;
using System;

namespace RiseFromTheAshes.Harmony
{
    /// <summary>
    /// Harmony patch to prevent EntityAlive.Update() from executing on entities that are being destroyed.
    /// This prevents NullReferenceExceptions during game shutdown when hallucination zombies (and other client-local entities)
    /// are being cleaned up.
    /// 
    /// Also includes diagnostic logging for hallucination zombies to track their behavior.
    /// </summary>
    [HarmonyPatch(typeof(EntityAlive))]
    [HarmonyPatch("Update")]
    public class EntityAliveUpdatePatch
    {
        private static int logCounter = 0;
        private static float lastLogTime = 0f;
        
        /// <summary>
        /// Prefix patch that runs BEFORE EntityAlive.Update()
        /// Returns false to skip the original method if entity is being destroyed
        /// </summary>
        [HarmonyPrefix]
        public static bool Prefix(EntityAlive __instance)
        {
            try
            {
                // Skip update if entity is marked for unload (being destroyed/removed)
                if (__instance.IsMarkedForUnload())
                {
                    return false; // Skip original Update()
                }
                
                // Skip update if world is null (game shutting down)
                if (__instance.world == null)
                {
                    return false; // Skip original Update()
                }
                
                // Skip update if GameManager is shutting down
                if (GameManager.Instance == null || GameManager.Instance.World == null)
                {
                    return false; // Skip original Update()
                }
                
                // DIAGNOSTIC: Log hallucination zombie activity every 5 seconds
                if (__instance.entityId < 0) // Client-local entity (hallucination)
                {
                    float currentTime = UnityEngine.Time.time;
                    if (currentTime - lastLogTime >= 5f)
                    {
                        lastLogTime = currentTime;
                        
                        var target = __instance.GetAttackTarget();
                        var pos = __instance.GetPosition();
                        var isAlive = __instance.IsAlive();
                        var health = __instance.Health;
                        
                        Log.Out($"[HALLUCINATION] Entity {__instance.entityId} ({__instance.EntityName}): " +
                               $"Pos=({pos.x:F1},{pos.y:F1},{pos.z:F1}), " +
                               $"Alive={isAlive}, Health={health}, " +
                               $"HasTarget={target != null}, " +
                               $"TargetName={target?.EntityName ?? "none"}, " +
                               $"IsOnGround={__instance.onGround}");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log what's causing the exception so we can debug
                // Only log for client-local entities (negative IDs) to avoid spam
                if (__instance.entityId < 0)
                {
                    Log.Warning($"[EntityAliveUpdatePatch] Exception checking entity {__instance.entityId} ({__instance.EntityName}): {ex.Message}");
                }
                
                // If any error occurs in our check, skip the update to be safe
                return false;
            }
            
            // Entity is valid, allow Update() to proceed
            return true;
        }
    }
    
    /// <summary>
    /// Patch EntityAlive.OnUpdateLive to log hallucination zombie AI state
    /// </summary>
    [HarmonyPatch(typeof(EntityAlive))]
    [HarmonyPatch("OnUpdateLive")]
    public class EntityAliveOnUpdateLivePatch
    {
        private static float lastLogTime = 0f;
        
        [HarmonyPostfix]
        public static void Postfix(EntityAlive __instance)
        {
            try
            {
                // Only log for hallucination zombies (negative IDs)
                if (__instance.entityId >= 0) return;
                
                float currentTime = UnityEngine.Time.time;
                if (currentTime - lastLogTime < 5f) return; // Log every 5 seconds
                
                lastLogTime = currentTime;
                
                // Log AI state
                var aiManager = __instance.aiManager;
                if (aiManager != null)
                {
                    Log.Out($"[HALLUCINATION-AI] Entity {__instance.entityId}: " +
                           $"AIActive=True, " +
                           $"Alert={__instance.IsAlert}, " +
                           $"Sleeping={__instance.IsSleeping}");
                }
                else
                {
                    Log.Warning($"[HALLUCINATION-AI] Entity {__instance.entityId}: AI MANAGER IS NULL!");
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"[EntityAliveOnUpdateLivePatch] Exception: {ex.Message}");
            }
        }
    }
}
