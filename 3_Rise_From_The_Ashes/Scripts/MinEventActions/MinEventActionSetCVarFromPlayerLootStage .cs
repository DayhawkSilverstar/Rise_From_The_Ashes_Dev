using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using UnityEngine;

// action="SetCVarFromPlayerLootStage, Rise_From_The_Ashes"
public class MinEventActionSetCVarFromPlayerLootStage : MinEventActionBase
{
    private string _cvar = "LootStage";
    private bool _debug;

    private static MethodInfo s_lootStageMethod;
    private static bool s_scanned;

    public override bool ParseXmlAttribute(XAttribute a)
    {
        if (base.ParseXmlAttribute(a)) return true;
        var n = a.Name.LocalName;
        if (n == "cvar") { _cvar = a.Value; return true; }
        if (n == "debug") { bool.TryParse(a.Value, out _debug); return true; }
        return false;
    }

    public override bool CanExecute(MinEventTypes _eventType, MinEventParams _params)
    {
        return _params != null && _params.Self is EntityPlayer;
    }

    public override void Execute(MinEventParams _params)
    {
        var ep = _params.Self as EntityPlayer;
        if (ep == null || ep.Buffs == null) return;

        EnsureScan();

        if (s_lootStageMethod == null)
        {
            if (_debug) Log.Warning("[RFA] SetCVarFromPlayerLootStage: no usable loot stage method found.");
            return;
        }

        int stage;
        try
        {
            var args = BuildArgs(s_lootStageMethod, ep);
            var target = s_lootStageMethod.IsStatic ? null
                        : (s_lootStageMethod.DeclaringType.IsInstanceOfType(ep) ? (object)ep : null);

            var ret = s_lootStageMethod.Invoke(target, args);
            stage = Convert.ToInt32(ret);
        }
        catch (Exception e)
        {
            if (_debug) Log.Warning("[RFA] SetCVarFromPlayerLootStage: invoke failed: {0}", e.Message);
            return;
        }

        ep.Buffs.SetCustomVar(_cvar, stage, true);
        if (_debug) Log.Out("[RFA] SetCVarFromPlayerLootStage EXEC: {0}={1}", _cvar, stage);
    }

    // ---------- helpers ----------

    private static void EnsureScan()
    {
        if (s_scanned) return;
        s_scanned = true;

        try
        {
            var methods = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => t != null)
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
                .Where(m =>
                {
                    var n = m.Name.ToLowerInvariant();
                    return n.Contains("getlootstage") || n.Contains("calclootstage");
                })
                .ToArray();

            // Prefer an EntityPlayer instance method (any signature)
            s_lootStageMethod =
                methods.FirstOrDefault(m => !m.IsStatic && typeof(EntityPlayer).IsAssignableFrom(m.DeclaringType) && m.GetParameters().Length == 0) ??
                methods.FirstOrDefault(m => !m.IsStatic && typeof(EntityPlayer).IsAssignableFrom(m.DeclaringType)) ??
                // Fallback: any static that accepts an Entity/EntityPlayer somewhere
                methods.FirstOrDefault(m => m.IsStatic && m.GetParameters().Any(p =>
                    typeof(Entity).IsAssignableFrom(p.ParameterType) ||
                    p.ParameterType.Name.Contains("EntityPlayer")));

            var chosen = (s_lootStageMethod != null)
                ? (s_lootStageMethod.DeclaringType.FullName + "." + s_lootStageMethod.Name + "(" +
                   string.Join(",", s_lootStageMethod.GetParameters().Select(p => p.ParameterType.Name).ToArray()) + ")")
                : "none";

            Log.Out("[RFA] SetCVarFromPlayerLootStage: candidates={0}, chosen={1}", methods.Length, chosen);
        }
        catch (Exception e)
        {
            Log.Warning("[RFA] SetCVarFromPlayerLootStage: scan failed: {0}", e);
        }
    }

    private static object[] BuildArgs(MethodInfo m, EntityPlayer ep)
    {
        var ps = m.GetParameters();
        var args = new object[ps.Length];

        // Common sources we'll reuse
        Vector3 pos = ep.GetPosition();
        var bp = ep.GetBlockPosition(); // Vector3i struct-ish
        var world = ep.world;

        for (int i = 0; i < ps.Length; i++)
        {
            var pt = ps[i].ParameterType;

            // by-ref/out not supported => supply default
            if (pt.IsByRef)
            {
                var et = pt.GetElementType();
                args[i] = et.IsValueType ? Activator.CreateInstance(et) : null;
                continue;
            }

            if (typeof(EntityPlayer).IsAssignableFrom(pt) || typeof(EntityAlive).IsAssignableFrom(pt) || typeof(Entity).IsAssignableFrom(pt))
            { args[i] = ep; continue; }

            if ((pt.FullName ?? "").EndsWith(".World") || pt.Name == "World")
            { args[i] = world; continue; }

            if (pt.Name == "Vector3")
            { args[i] = pos; continue; }

            if (pt.Name == "Vector3i")
            {
                // Try (int,int,int) ctor
                try { args[i] = Activator.CreateInstance(pt, new object[] { bp.x, bp.y, bp.z }); }
                catch { args[i] = Activator.CreateInstance(pt); }
                continue;
            }

            if (pt == typeof(int)) { args[i] = 0; continue; }
            if (pt == typeof(float)) { args[i] = 0f; continue; }
            if (pt == typeof(double)) { args[i] = 0.0; continue; }
            if (pt == typeof(bool)) { args[i] = true; continue; } // permissive default
            if (pt == typeof(string)) { args[i] = null; continue; }

            if (pt.IsEnum)
            {
                args[i] = Enum.ToObject(pt, 0);
                continue;
            }

            // Nullable<T> => null is OK
            if (pt.IsGenericType && pt.GetGenericTypeDefinition() == typeof(Nullable<>))
            { args[i] = null; continue; }

            // anything else: default/null
            args[i] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
        }

        return args;
    }
}
