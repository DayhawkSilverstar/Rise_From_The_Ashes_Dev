// File: MinEventActionSetCVarFromPassive.cs
// Explicit-call version (no reflection, no auto-tags, no special cases)
// Logs only on exceptions. Designed for the full EffectManager.GetValue signature observed in your logs:
//   EffectManager.GetValue(PassiveEffects, ItemValue, float, EntityAlive, Recipe, FastTags<TagGroup.Global>, bool, bool, bool, bool, bool, int, bool, bool)
//
// If your local EffectManager has a different overload, adjust the argument list in the call accordingly.

using System;
using System.Xml.Linq;
using UnityEngine;

public class MinEventActionSetCVarFromPassive : MinEventActionBase
{
    private string _cvar = ".LootProb";
    private string _passiveName = "LootProb"; // MUST match an enum member of PassiveEffects
    private string _mode = "raw";             // raw | perc | perc_to_mult
    private float _base = 1f;                 // base value passed to EffectManager (for % math)
    private string _tags = null;              // CSV, e.g. "running,crouching"

    public override void Execute(MinEventParams _params)
    {
        var player = _params.Self as EntityPlayer;
        if (player == null) return;

        // Parse enum explicitly (no reflection)
        PassiveEffects effect;
        try
        {
            effect = (PassiveEffects)Enum.Parse(typeof(PassiveEffects), _passiveName, ignoreCase: false);
        }
        catch
        {
            return; // unknown passive; do nothing
        }

        float baseVal = (_base == 0f ? 1f : _base);

        // Parse FastTags<TagGroup.Global> from CSV
        FastTags<TagGroup.Global> fastTags = default;
        try
        {
            if (!string.IsNullOrEmpty(_tags))
                fastTags = FastTags<TagGroup.Global>.Parse(_tags);
        }
        catch (Exception e)
        {
            Log.Warning($"[RFA] SetCVarFromPassive: FastTags.Parse failed for '{_tags}': {e}");
        }

        float raw;
        try
        {
            // Explicit full signature call (matches the signature from your runtime logs)
            // Bool flags set to 'true' to include all standard sources; tweak if you need to exclude any.
            raw = EffectManager.GetValue(
                effect,
                default(ItemValue),          // original item value (none in this context)
                baseVal,                     // base for percent calculations
                player,                      // entity
                (Recipe)null,                // recipe context
                fastTags,                    // effect tags
                true,                        // include equipment
                true,                        // include holding item
                true,                        // include attributes
                true,                        // include skills
                true,                        // include perks
                1,                           // crafting tier
                true,                        // useMods
                false                        // useDurability
            );
        }
        catch (Exception e)
        {
            Log.Warning($"[RFA] SetCVarFromPassive: GetValue threw for '{_passiveName}': {e}");
            return;
        }

        float outVal = ConvertMode(raw, baseVal);
        try
        {
            player.Buffs.SetCustomVar(_cvar, outVal);
        }
        catch (Exception e)
        {
            Log.Warning($"[RFA] SetCVarFromPassive: SetCustomVar threw for '{_cvar}': {e}");
        }
    }

    private float ConvertMode(float value, float baseVal)
    {
        if (_mode == "perc") return value * 0.01f;
        if (_mode == "perc_to_mult") return baseVal * (1f + (value * 0.01f));
        return value; // raw
    }

    public override bool ParseXmlAttribute(XAttribute _attribute)
    {
        if (base.ParseXmlAttribute(_attribute)) return true;

        var n = _attribute.Name.LocalName;
        if (n == "cvar") { _cvar = _attribute.Value; return true; }
        if (n == "passive") { _passiveName = _attribute.Value; return true; }
        if (n == "mode") { _mode = _attribute.Value; return true; }
        if (n == "base") { float.TryParse(_attribute.Value, out _base); return true; }
        if (n == "tags") { _tags = string.IsNullOrWhiteSpace(_attribute.Value) ? null : _attribute.Value; return true; }
        return false;
    }

    public override bool CanExecute(MinEventTypes _eventType, MinEventParams _params)
    {
        return _params.Self is EntityPlayer;
    }
}
