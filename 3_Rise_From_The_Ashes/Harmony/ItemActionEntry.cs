using HarmonyLib;
using System;

namespace RiseFromTheAshes.Harmony
{

    [HarmonyPatch(typeof(ItemActionEntrySell))]
    [HarmonyPatch("OnActivated")]
    public class OnActivatedSell
    {
        static float barterExpToCoinRatio = 252;        
        private static void Prefix(ItemActionEntrySell __instance)
        {
            Log.Out($"Running Sell Prefix");
            EntityPlayerLocal entityPlayer = __instance.ItemController.xui.playerUI.entityPlayer;
            XUiC_ItemStack xUiC_ItemStack = (XUiC_ItemStack)__instance.ItemController;

            ItemStack itemStack = xUiC_ItemStack.ItemStack.Clone();
            ItemClass forId = ItemClass.GetForId(xUiC_ItemStack.ItemStack.itemValue.type);
            int count2 = xUiC_ItemStack.InfoWindow.BuySellCounter.Count;

            int sellPrice = XUiM_Trader.GetSellPrice(__instance.ItemController.xui, itemStack.itemValue, count2, forId);
            XUiM_PlayerInventory playerInventory = __instance.ItemController.xui.PlayerInventory;
            
            float barterExpTotal = entityPlayer.GetCVar("$barterExpTotal");
            float dukeExp = sellPrice / barterExpToCoinRatio;
            float statSkillExp = entityPlayer.GetCVar("$BarterExp");
            float finalExp = dukeExp * statSkillExp ;
            barterExpTotal += finalExp;
            Log.Out("base exp from stat and perks: {0}, exp from dukes: {1}, final exp: {2}", statSkillExp, dukeExp,finalExp);            
            entityPlayer.SetCVar("$barterExpTotal", barterExpTotal);

        }
    }

    [HarmonyPatch(typeof(ItemActionEntryPurchase))]
    [HarmonyPatch("OnActivated")]
    public class OnActivatedPurchased
    {
        static float barterExpToCoinRatio = 5;
        static float statSkillBonus = 126;
        private static void Prefix(ItemActionEntryPurchase __instance)
        {
            try
            {
                Log.Out($"Running buying Prefix");
                EntityPlayerLocal entityPlayer = __instance.ItemController.xui.playerUI.entityPlayer;


                ItemStack itemStack = ((XUiC_TraderItemEntry)__instance.ItemController).Item.Clone();
                ItemClass forId = ItemClass.GetForId(itemStack.itemValue.type);
                ItemValue itemValue = itemStack.itemValue;                     
                
                int count = ((XUiC_TraderItemEntry)__instance.ItemController).InfoWindow.BuySellCounter.Count;

                int buyPrice = XUiM_Trader.GetBuyPrice(__instance.ItemController.xui, itemStack.itemValue, count, forId, ((XUiC_TraderItemEntry)__instance.ItemController).SlotIndex);
                XUiM_PlayerInventory playerInventory = __instance.ItemController.xui.PlayerInventory;

                float barterExpTotal = entityPlayer.GetCVar("$barterExpTotal");
                float dukeExp = buyPrice / barterExpToCoinRatio;
                float statSkillExp = entityPlayer.GetCVar("$BarterExp");
                float expRatio = statSkillExp / statSkillBonus;
                float expBonus = dukeExp * expRatio;
                float finalExp = dukeExp + expBonus;
                barterExpTotal += finalExp;
                Log.Out("base exp from stat and perks: {0}, exp from dukes: {1},  statPerk / 126 ratio: {2}, expBonus: {3}, final exp: {4}", statSkillExp, dukeExp, expRatio, expBonus, finalExp);
                entityPlayer.SetCVar("$barterExpTotal", barterExpTotal);
            }
            catch (Exception ex) 
            {
                Log.Out($"Exception: {0}", ex.StackTrace);
                throw ex;
            }

        }
    }

}
