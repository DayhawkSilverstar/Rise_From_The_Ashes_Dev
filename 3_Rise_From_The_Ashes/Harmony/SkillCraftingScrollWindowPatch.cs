using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HarmonyLib;

namespace Harmony
{
    [HarmonyPatch(typeof(XUiC_SkillCraftingInfoWindow), nameof(XUiC_SkillCraftingInfoWindow.Init))]
    public class SkillCraftingInfoWindowScroll_Patch
    {
        public static void Postfix(List<XUiC_SkillCraftingInfoEntry> ___levelEntries)
        {
            foreach (XUiC_SkillCraftingInfoEntry levelEntry in ___levelEntries)
            {
                levelEntry.OnScroll += Entry_OnScroll;
            }
        }

        private static void Entry_OnScroll(XUiController _sender, float _delta)
        {
            var skillCraftingInfoWindow = _sender.GetParentByType<XUiC_SkillCraftingInfoWindow>();
            var pager = skillCraftingInfoWindow.GetChildByType<XUiC_Paging>();

            if (_delta > 0f)
            {
                pager?.PageDown();
            }
            else
            {
                pager?.PageUp();
            }

            skillCraftingInfoWindow.SelectedData = null;
            skillCraftingInfoWindow.SelectedEntry = null;
        }
    }
}

