using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiseFromTheAshes.Harmony
{
    /*
    [HarmonyPatch(typeof(GameStageDefinition))]
    [HarmonyPatch("CalcGameStageAround")]
    public class GameStagePatchCalcGameStage
    {              
        private static void Postfix(GameStageDefinition __instance, EntityPlayer player, ref int __result)
        {
            Log.Out("GameStagePatch Postfix-CalcGameStageAround");
            if (player != null)
            {                
                BiomeDefinition biomeName = GameManager.Instance.World.GetPlayers().Where((p) => p.entityId == GameManager.Instance.myPlayerId).FirstOrDefault().biomeStandingOn;
                if (biomeName?.LocalizedName != null)
                {
                    switch (biomeName.LocalizedName)
                    {
                        case "Pine Forest":
                            if (__result < 0)
                            {
                                __result = 1;
                            }
                            else if (__result > 50)
                            {
                                __result = 50;
                            }
                            break;
                        case "Burnt Forest":
                            if (__result < 50)
                            {
                                __result = 50;
                            }
                            else if (__result > 100)
                            {
                                __result = 100;
                            }
                            break;
                        case "Desert":
                            if (__result < 100)
                            {
                                __result = 100;
                            }
                            else if (__result > 150)
                            {
                                __result = 150;
                            }
                            break;
                        case "Snow":
                            if (__result < 150)
                            {
                                __result = 150;
                            }
                            else if (__result > 200)
                            {
                                __result = 200;
                            }
                            break;
                        case "Wasteland":
                            if (__result < 400)
                            {
                                __result = 400;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            Log.Out("GameStage adjusted to : {0}", __result);
        }
    }

    [HarmonyPatch(typeof(GameStageDefinition))]
    [HarmonyPatch("CalcPartyLevel")]
    public class GameStagePatchCalcPartyLevel
    {
        private static void Postfix(GameStageDefinition __instance, List<int> playerGameStages, ref int __result)
        {
            try
            {
                Log.Out("GameStagePatch Postfix-CalcPartyLevel");                
                EntityPlayer player = GameManager.Instance.World.GetLocalPlayers().FirstOrDefault();
                Log.Out("Player ID : {0}", player.entityId.ToString());
                if (player != null)
                {
                    
                    BiomeDefinition biomeName = GameManager.Instance.World.GetBiome((int)player.position.x, (int)player.position.z);
                    Log.Out("CalPartyLevel - BiomeName : " + biomeName.LocalizedName);
                    if (biomeName?.LocalizedName != null)
                    {
                        switch (biomeName.LocalizedName)
                        {
                            case "Pine Forest":
                                if (__result < 0)
                                {
                                    __result = 1;
                                }
                                else if (__result > 50)
                                {
                                    __result = 50;
                                }
                                break;
                            case "Burnt Forest":
                                if (__result < 50)
                                {
                                    __result = 50;
                                }
                                else if (__result > 100)
                                {
                                    __result = 100;
                                }
                                break;
                            case "Desert":
                                if (__result < 100)
                                {
                                    __result = 100;
                                }
                                else if (__result > 150)
                                {
                                    __result = 150;
                                }
                                break;
                            case "Snow":
                                if (__result < 150)
                                {
                                    __result = 150;
                                }
                                else if (__result > 200)
                                {
                                    __result = 200;
                                }
                                break;
                            case "Wasteland":
                                if (__result < 400)
                                {
                                    __result = 400;
                                }
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    Log.Out("Player is null");
                }
                Log.Out("PartyLevel adjusted to : {0}", __result);
            }
            catch (Exception ex)
            {
                Log.Out(ex.Message);
                Log.Out(ex.StackTrace);
            }
        }
    }

    [HarmonyPatch(typeof(GameStageDefinition))]
    [HarmonyPatch("GetStage")]
    public class GameStagePatchGetStage
    {
        private static void Prefix(GameStageDefinition __instance, int stage)
        {
            Log.Out("GameStagePatch Prefix-GetStage");
            EntityPlayer player = GameManager.Instance.World.GetLocalPlayers().FirstOrDefault();
            if (player != null)
            {
                BiomeDefinition biomeName = player.biomeStandingOn;
                if (biomeName?.LocalizedName != null)
                {
                    switch (biomeName.LocalizedName)
                    {
                        case "Pine Forest":
                            if (stage < 0)
                            {
                                stage = 1;
                            }
                            else if (stage > 50)
                            {
                                stage = 50;
                            }
                            break;
                        case "Burnt Forest":
                            if (stage < 50)
                            {
                                stage = 50;
                            }
                            else if (stage > 100)
                            {
                                stage = 100;
                            }
                            break;
                        case "Desert":
                            if (stage < 100)
                            {
                                stage = 100;
                            }
                            else if (stage > 150)
                            {
                                stage = 150;
                            }
                            break;
                        case "Snow":
                            if (stage < 150)
                            {
                                stage = 150;
                            }
                            else if (stage > 200)
                            {
                                stage = 200;
                            }
                            break;
                        case "Wasteland":
                            if (stage < 400)
                            {
                                stage = 400;
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            else
            {
                Log.Out("Player is null");
            }
            Log.Out("GameStage adjusted to : {0}", stage);
        }
    }

    */

}
