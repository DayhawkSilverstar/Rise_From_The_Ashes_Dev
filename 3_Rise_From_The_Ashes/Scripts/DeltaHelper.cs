using System;
using System.Collections.Generic;
using System.IO;

namespace RiseFromTheAshes
{
    public class DeltaHelper
    {
        public short Multiplier;

        public float CraftingTimeLeft;

        public float OneItemCraftTime = -1f;

        public bool IsCrafting;

        public ItemValue RepairItem = ItemValue.None.Clone();

        public ushort AmountToRepair;

        public byte Quality;

        public int StartingEntityId = -1;

        private int recipeHashCode;

        private Recipe cachedRecipe;

        public Recipe Recipe
        {
            get
            {
                if (cachedRecipe == null)
                {
                    return cachedRecipe = CraftingManager.GetRecipe(recipeHashCode);
                }

                return cachedRecipe;
            }
            set
            {
                cachedRecipe = value;
                recipeHashCode = ((cachedRecipe != null) ? cachedRecipe.GetHashCode() : 0);
            }
        }

        public void WriteDelta(BinaryWriter _bw, RecipeQueueItem _last)
        {
           
        }

        public void ReadDelta(BinaryReader _br, RecipeQueueItem _last)
        {
            recipeHashCode = _br.ReadInt32();
            cachedRecipe = CraftingManager.GetRecipe(recipeHashCode);
            float num = _br.ReadSingle();
            CraftingTimeLeft = _last.CraftingTimeLeft + num;
            int num2 = _br.ReadInt16();
            Multiplier = (short)(_last.Multiplier + num2);
            IsCrafting = _br.ReadBoolean();
            if (_br.ReadBoolean())
            {
                RepairItem = ItemValue.ReadAndCreate(_br);
                AmountToRepair = _br.ReadUInt16();
            }

            Quality = _br.ReadByte();
            StartingEntityId = _br.ReadInt32();
            OneItemCraftTime = _br.ReadSingle();
            if (_br.ReadBoolean())
            {
                cachedRecipe = new Recipe();
                cachedRecipe.itemValueType = _br.ReadInt32();
                cachedRecipe.count = _br.ReadInt32();
                cachedRecipe.scrapable = true;
                int num3 = _br.ReadInt32();
                Recipe.ingredients = new List<ItemStack>();
                for (int i = 0; i < num3; i++)
                {
                    Recipe.ingredients.Add(new ItemStack().Read(_br));
                }

                cachedRecipe.craftingTime = _br.ReadSingle();
                cachedRecipe.craftExpGain = _br.ReadInt32();
            }
        }
    }
}
