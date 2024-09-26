using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class TraderSupplyQuest : BaseObjective
{
    private ItemValue expectedItem = ItemValue.None.Clone();

    private ItemClass expectedItemClass;

    private int itemCount;

    private int currentCount;

    protected bool KeepItems;

    public override ObjectiveValueTypes ObjectiveValueType => ObjectiveValueTypes.Number;

    public override void SetupObjective()
    {
        Log.Out("TraderSupplyQuest : SetupObjective");
        keyword = Localization.Get("ObjectiveFetch_keyword");
        expectedItem = ItemClass.GetItem(ID);
        expectedItemClass = ItemClass.GetItemClass(ID);
        itemCount = Convert.ToInt32(Value);
    }

    public override void SetupDisplay()
    {
        base.Description = string.Format(keyword, expectedItemClass.GetLocalizedItemName());
        StatusText = $"{currentCount}/{itemCount}";
    }

    public override void AddHooks()
    {
        Log.Out("TraderSupplyQuest : AddHooks");
        LocalPlayerUI.GetUIForPlayer(base.OwnerQuest.OwnerJournal.OwnerPlayer);
        XUiM_PlayerInventory playerInventory = LocalPlayerUI.GetUIForPlayer(base.OwnerQuest.OwnerJournal.OwnerPlayer).xui.PlayerInventory;
        playerInventory.Backpack.OnBackpackItemsChangedInternal += Backpack_OnBackpackItemsChangedInternal;
        playerInventory.Toolbelt.OnToolbeltItemsChangedInternal += Toolbelt_OnToolbeltItemsChangedInternal;
        Refresh();
    }

    public override void RemoveHooks()
    {
        Log.Out("TraderSupplyQuest : RemoveHooks");
        XUiM_PlayerInventory playerInventory = LocalPlayerUI.GetUIForPlayer(base.OwnerQuest.OwnerJournal.OwnerPlayer).xui.PlayerInventory;
        if (playerInventory != null)
        {
            playerInventory.Backpack.OnBackpackItemsChangedInternal -= Backpack_OnBackpackItemsChangedInternal;
            playerInventory.Toolbelt.OnToolbeltItemsChangedInternal -= Toolbelt_OnToolbeltItemsChangedInternal;
        }
    }

    private void Current_AddItem(ItemStack stack)
    {
        Log.Out("TraderSupplyQuest : Current_AddItem = " + stack.itemValue.type.ToString());
        if (!base.Complete && stack.itemValue.type == expectedItem.type)
        {
            if (base.CurrentValue + stack.count > itemCount)
            {
                base.CurrentValue = (byte)itemCount;
            }
            else
            {
                base.CurrentValue += (byte)stack.count;
            }

            Refresh();
        }
    }

    private void Backpack_OnBackpackItemsChangedInternal()
    {
        Log.Out("TraderSupplyQuest : Backpack_OnBackpackItemsChangedInternal");
        LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(base.OwnerQuest.OwnerJournal.OwnerPlayer);
        if (!base.Complete && uIForPlayer.xui.PlayerInventory != null)
        {
            Refresh();
        }
    }

    private void Toolbelt_OnToolbeltItemsChangedInternal()
    {
        Log.Out("TraderSupplyQuest : Toolbelt_OnToolbeltItemsChangedInternal");
        LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(base.OwnerQuest.OwnerJournal.OwnerPlayer);
        if (!base.Complete && uIForPlayer.xui.PlayerInventory != null)
        {
            Refresh();
        }
    }

    public override void Refresh()
    {
        if (!base.Complete)
        {
            XUiM_PlayerInventory playerInventory = LocalPlayerUI.GetUIForPlayer(base.OwnerQuest.OwnerJournal.OwnerPlayer).xui.PlayerInventory;
            currentCount = playerInventory.Backpack.GetItemCount(expectedItem);
            currentCount += playerInventory.Toolbelt.GetItemCount(expectedItem);
            if (currentCount > itemCount)
            {
                currentCount = itemCount;
            }

            SetupDisplay();
            if (currentCount != base.CurrentValue)
            {
                base.CurrentValue = (byte)currentCount;
            }

            base.Complete = currentCount >= itemCount && base.OwnerQuest.CheckRequirements();
            if (base.Complete)
            {
                base.OwnerQuest.CurrentState = Quest.QuestState.Completed;
            }
        }
    }

    public override void RemoveObjectives()
    {
        Log.Out("TraderSupplyQuest : RemoveObjectives");
        if (!KeepItems)
        {
            XUiM_PlayerInventory playerInventory = LocalPlayerUI.GetUIForPlayer(base.OwnerQuest.OwnerJournal.OwnerPlayer).xui.PlayerInventory;
            itemCount = playerInventory.Backpack.DecItem(expectedItem, itemCount);
            if (itemCount > 0)
            {
                playerInventory.Toolbelt.DecItem(expectedItem, itemCount);
            }
        }
    }

    public override BaseObjective Clone()
    {
        Log.Out("TraderSupplyQuest : Clone");
        TraderSupplyQuest objectiveTraderSupply = new TraderSupplyQuest();
        CopyValues(objectiveTraderSupply);
        objectiveTraderSupply.KeepItems = KeepItems;
        return objectiveTraderSupply;
    }

    public override string ParseBinding(string bindingName)
    {
        string iD = ID;
        string value = Value;
        if (!(bindingName == "items"))
        {
            if (bindingName == "itemswithcount")
            {
                ItemClass itemClass = ItemClass.GetItemClass(iD);
                int num = Convert.ToInt32(value);
                if (itemClass == null)
                {
                    return "INVALID";
                }

                return num + " " + itemClass.GetLocalizedItemName();
            }

            return "";
        }

        ItemClass itemClass2 = ItemClass.GetItemClass(iD);
        if (itemClass2 == null)
        {
            return "INVALID";
        }

        return itemClass2.GetLocalizedItemName();
    }
}
