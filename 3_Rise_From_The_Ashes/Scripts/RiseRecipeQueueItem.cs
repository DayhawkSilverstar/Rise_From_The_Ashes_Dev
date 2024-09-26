using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiseFromTheAshes.Scripts
{
    public class RiseRecipeQueueItem : RecipeQueueItem
    {
        private int recipeHashCode;

        private Recipe cachedRecipe;

        private RecipeQueueItem lastQueueItem;

        public RiseRecipeQueueItem()
        {
            
        }

        public void Import(RecipeQueueItem item)
        {
            lastQueueItem = item;
        }

        public void ReadDelta(BinaryReader _br, RecipeQueueItem _last)
        {
            Log.Out($"Reading Delta");
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

            base.Recipe = cachedRecipe;
        }

        public void WriteDelta(BinaryWriter _bw)
        {
            Log.Out($"Writing Delta");
            RecipeQueueItem _last = lastQueueItem;
            _bw.Write((Recipe != null) ? Recipe.GetHashCode() : 0);
            if (Multiplier < 0)
            {
                Log.Error("Multiplier is less than 0!");
                Log.Out(Environment.StackTrace);
                Multiplier = 0;
            }

            _bw.Write(CraftingTimeLeft - _last.CraftingTimeLeft);
            _last.CraftingTimeLeft += CraftingTimeLeft - _last.CraftingTimeLeft;
            _bw.Write((short)(Multiplier - _last.Multiplier));
            _last.Multiplier += (short)(Multiplier - _last.Multiplier);
            _bw.Write(IsCrafting);
            bool flag = RepairItem != null && !RepairItem.Equals(ItemValue.None);
            _bw.Write(flag);
            if (flag)
            {
                RepairItem.Write(_bw);
                _bw.Write(AmountToRepair);
            }

            _bw.Write(Quality);
            _bw.Write(StartingEntityId);
            _bw.Write(OneItemCraftTime);
            _bw.Write(Recipe != null && Recipe.scrapable);
            if (Recipe != null && Recipe.scrapable)
            {
                _bw.Write(Recipe.itemValueType);
                _bw.Write(Recipe.count);
                _bw.Write(Recipe.scrapable);
                _bw.Write(Recipe.ingredients.Count);
                for (int i = 0; i < Recipe.ingredients.Count; i++)
                {
                    Recipe.ingredients[i].Write(_bw);
                }

                _bw.Write(Recipe.craftingTime);
                _bw.Write(Recipe.craftExpGain);
            }
        }
    }
}
