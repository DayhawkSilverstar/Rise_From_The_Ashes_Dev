using HarmonyLib;
using System;

namespace RiseFromTheAshes
{

    [HarmonyPatch(typeof(ItemActionEntryCraft))]
    [HarmonyPatch("OnActivated")]
    public class OnActivatedCrafting
    {
        private static void Prefix(ItemActionEntryCraft __instance)
        {
            Log.Out($"Crafting Prefix");
            EntityPlayerLocal entityPlayer = __instance.ItemController.xui.playerUI.entityPlayer;
            Recipe recipe = ((XUiC_RecipeEntry)__instance.ItemController).Recipe;

            Log.Out(entityPlayer.EntityName + $" crafted " + recipe.GetName());
            Log.Out($"Entity ID : " + entityPlayer.entityId.ToString());            

        }
    }
}
