// File: RfaExposePassiveEffectsAsCVars.cs
// Refs: Assembly-CSharp.dll, 0Harmony.dll, UnityEngine.dll
// Harmony 2.x (HarmonyLib) | C# 7.3 compatible

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

public class RfaCvarBootstrap : IModApi
{
    public void InitMod(Mod mod)
    {
        new HarmonyLib.Harmony("rfa.expose.passives.cvars").PatchAll();
        Log.Out("[RFA] Expose passives as CVars: bootstrap OK");
    }
}

[HarmonyPatch(typeof(EntityPlayerLocal))]
public static class Rfa_ExposePassives_Patch
{
    // — Config —
    private const string CvarNoise = "rfaNoiseMultiplier"; // final multiplier (1.0 + perc_add)
    private const string CvarLStage = "rfaLootStageBonus";  // perc_add sum (0.20 = +20%)
    private const string CvarLProb = "rfaLootProbBonus";   // points sum (20 = +20%)
    private const float UpdateHz = 1f;

    // — Throttle —
    private static readonly Dictionary<int, float> _lastUpdateById = new Dictionary<int, float>();

    // — Your enum —
    private static readonly Type PassiveEnumType = typeof(PassiveEffects);
    private static readonly object NoiseEnum = PassiveEffects.NoiseMultiplier;
    private static readonly object LootStageEnum = PassiveEffects.LootStage;
    private static readonly object LootProbEnum = PassiveEffects.LootProb;

    // — EffectManager.GetValue candidates —
    private static MethodInfo[] _miGetValueCandidates = new MethodInfo[0];
    private static bool _warnedNoCandidate;
    private static readonly Dictionary<Type, FieldInfo> _fastTagsNoneCache = new Dictionary<Type, FieldInfo>();

    static Rfa_ExposePassives_Patch()
    {
        ResolveCandidates();
    }

    [HarmonyPostfix]
    [HarmonyPatch("Update")]
    private static void Post_Update(EntityPlayerLocal __instance)
    {
        if (__instance == null) return;

        float now = Time.time;
        int id = __instance.entityId;
        float last;
        if (_lastUpdateById.TryGetValue(id, out last) && now - last < (1f / UpdateHz)) return;
        _lastUpdateById[id] = now;

        var buffs = __instance.Buffs; // strong field in your build
        if (buffs == null) return;

        if (_miGetValueCandidates == null || _miGetValueCandidates.Length == 0)
        {
            if (!_warnedNoCandidate)
            {
                Log.Warning("[RFA] No EffectManager.GetValue methods found; CVars will not update.");
                _warnedNoCandidate = true;
            }
            return;
        }

        try
        {
            float addNoise = TryGetAdd(__instance, NoiseEnum, 0f);
            float addLootStage = TryGetAdd(__instance, LootStageEnum, 0f);
            float addLootProb = TryGetAdd(__instance, LootProbEnum, 0f);

            float noiseMultiplier = 1f + addNoise;

            SetCVarOnBuffs(buffs, CvarNoise, noiseMultiplier);
            SetCVarOnBuffs(buffs, CvarLStage, addLootStage);
            SetCVarOnBuffs(buffs, CvarLProb, addLootProb);
        }
        catch (Exception e)
        {
            Log.Warning("[RFA] Failed to update passive CVars: " + e);
        }
    }

    // — Resolver: accept ANY public static GetValue on EffectManager —
    private static void ResolveCandidates()
    {
        try
        {
            Type emType = typeof(EffectManager);
            MethodInfo[] all = emType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            List<MethodInfo> list = new List<MethodInfo>();
            for (int i = 0; i < all.Length; i++)
            {
                MethodInfo m = all[i];
                if (m.Name == "GetValue")
                    list.Add(m);
            }
            _miGetValueCandidates = list.ToArray();
            Log.Out("[RFA] GetValue candidates: " + _miGetValueCandidates.Length);
        }
        catch (Exception e)
        {
            Log.Warning("[RFA] Could not enumerate EffectManager.GetValue methods: " + e);
            _miGetValueCandidates = new MethodInfo[0];
        }
    }

    // — Try each candidate until one works —
    private static float TryGetAdd(EntityAlive entity, object effectEnumValue, float baseValue)
    {
        for (int i = 0; i < _miGetValueCandidates.Length; i++)
        {
            MethodInfo m = _miGetValueCandidates[i];
            try
            {
                object[] args = BuildArgsForGetValue(m, entity, effectEnumValue, baseValue);
                object val = m.Invoke(null, args);
                return ToFloat(val, baseValue);
            }
            catch
            {
                // try next
            }
        }
        if (!_warnedNoCandidate)
        {
            Log.Warning("[RFA] No compatible EffectManager.GetValue overload could be invoked; CVars will stay static.");
            _warnedNoCandidate = true;
        }
        return baseValue;
    }

    // — Build args for ANY GetValue signature (handles your 14-param one) —
    private static object[] BuildArgsForGetValue(MethodInfo m, EntityAlive entity, object srcEnumVal, float baseValue)
    {
        ParameterInfo[] p = m.GetParameters();
        object[] args = new object[p.Length];

        string effectName = Enum.GetName(PassiveEnumType, srcEnumVal);

        for (int i = 0; i < p.Length; i++)
        {
            Type pt = p[i].ParameterType;
            string pn = p[i].Name ?? string.Empty;

            // 1) Passive effect enum / underlying integral
            if (pt.IsEnum)
            {
                if (pt == PassiveEnumType) { args[i] = srcEnumVal; continue; }
                try { args[i] = Enum.Parse(pt, effectName, true); continue; } catch { }
            }
            if (pt == typeof(byte) || pt == typeof(sbyte) || pt == typeof(short) || pt == typeof(ushort) ||
                pt == typeof(int) || pt == typeof(uint) || pt == typeof(long) || pt == typeof(ulong))
            {
                try { args[i] = Convert.ChangeType(srcEnumVal, pt); continue; } catch { }
            }

            // 2) Original item value, recipe — pass null (we only want the entity’s totals)
            if (pt.Name == "ItemValue" || pt.FullName == "ItemValue") { args[i] = null; continue; }
            if (pn.IndexOf("recipe", StringComparison.OrdinalIgnoreCase) >= 0) { args[i] = null; continue; }

            // 3) EntityAlive / Entity
            if (typeof(EntityAlive).IsAssignableFrom(pt)) { args[i] = entity; continue; }
            if (typeof(Entity).IsAssignableFrom(pt)) { args[i] = entity; continue; }

            // 4) Base float param
            if (pt == typeof(float)) { args[i] = baseValue; continue; }

            // 5) FastTags (generic or otherwise) -> default(FastTags<...>) or none/None
            if (pt.Name.IndexOf("FastTags", StringComparison.OrdinalIgnoreCase) >= 0
                || (pt.IsGenericType && pt.GetGenericTypeDefinition().Name.StartsWith("FastTags", StringComparison.OrdinalIgnoreCase)))
            {
                args[i] = GetFastTagsDefaultOrNone(pt);
                continue;
            }

            // 6) Known defaults to match your method’s defaults
            if (pt == typeof(bool)) { args[i] = true; continue; }          // calcEquipment/holding/progression/buffs/challenges/useMods/_useDurability => true
            if (pt == typeof(int))
            {
                // craftingTier default is 1 in your method
                args[i] = (pn.Equals("craftingTier", StringComparison.OrdinalIgnoreCase)) ? 1 : 0;
                continue;
            }

            // 7) Default everything else
            if (pt == typeof(double)) { args[i] = 0.0; continue; }
            if (pt == typeof(string)) { args[i] = null; continue; }
            if (pt.IsValueType) { args[i] = Activator.CreateInstance(pt); continue; }
            args[i] = null;
        }

        return args;
    }

    private static object GetFastTagsDefaultOrNone(Type fastTagsType)
    {
        try
        {
            FieldInfo fi;
            if (_fastTagsNoneCache.TryGetValue(fastTagsType, out fi) && fi != null)
                return fi.GetValue(null);

            // Try fields "none" or "None"
            fi = fastTagsType.GetField("none", BindingFlags.Public | BindingFlags.Static)
                 ?? fastTagsType.GetField("None", BindingFlags.Public | BindingFlags.Static);

            if (fi != null)
            {
                _fastTagsNoneCache[fastTagsType] = fi;
                return fi.GetValue(null);
            }
        }
        catch { }
        // default(FastTags<...>)
        return fastTagsType.IsValueType ? Activator.CreateInstance(fastTagsType) : null;
    }

    private static float ToFloat(object obj, float deflt)
    {
        if (obj == null) return deflt;
        try { return Convert.ToSingle(obj); } catch { return deflt; }
    }

    // — CVars writer (reflection only) —
    private static void SetCVarOnBuffs(object entityBuffs, string key, float value)
    {
        if (entityBuffs == null) return;
        Type buffsType = entityBuffs.GetType();

        // buffs.CVars.Set(string,float)
        PropertyInfo cvarsProp = buffsType.GetProperty("CVars", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        object cvarsObj = cvarsProp != null ? cvarsProp.GetValue(entityBuffs, null) : null;
        if (cvarsObj != null)
        {
            MethodInfo setMi = cvarsObj.GetType().GetMethod("Set", BindingFlags.Public | BindingFlags.Instance, null,
                                                            new Type[] { typeof(string), typeof(float) }, null);
            if (setMi != null) { setMi.Invoke(cvarsObj, new object[] { key, value }); return; }
        }

        // Legacy: buffs.SetCustomVar(string,float,bool)
        MethodInfo legacyMi = buffsType.GetMethod("SetCustomVar", BindingFlags.Public | BindingFlags.Instance, null,
                                                  new Type[] { typeof(string), typeof(float), typeof(bool) }, null);
        if (legacyMi != null) { legacyMi.Invoke(entityBuffs, new object[] { key, value, true }); return; }

        // Last-resort: indexer on CVars
        try
        {
            if (cvarsObj != null)
            {
                PropertyInfo idx = cvarsObj.GetType().GetProperty("Item", BindingFlags.Public | BindingFlags.Instance);
                if (idx != null) idx.SetValue(cvarsObj, value, new object[] { key });
            }
        }
        catch { /* ignore */ }
    }
}
