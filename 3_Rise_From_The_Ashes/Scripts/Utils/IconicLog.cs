using UnityEngine;

public static class IconicLog
{
    public enum Level
    {
        Off = 0,
        Warn = 1,
        Info = 2,
        Debug = 3,
        Trace = 4
    }

    // Always return the most verbose level; no CVar dependency
    private static int GetLevel(EntityAlive e)
    {
        return (int)Level.Trace;
    }

    // Always allow logging for all levels
    private static bool ShouldLogLevel(EntityAlive e, Level level)
    {
        return true;
    }

    public static void Info(EntityAlive e, string task, string message)
    {
        if (!ShouldLogLevel(e, Level.Info)) return;
        int id = e != null ? e.entityId : -1;
        Log.Out($"[IconicAI][INFO] [{task}] id={id} {message}");
    }

    public static void Warn(EntityAlive e, string task, string message)
    {
        int id = e != null ? e.entityId : -1;
        Log.Warning($"[IconicAI][WARN] [{task}] id={id} {message}");
    }

    public static void Debug(EntityAlive e, string task, string message)
    {
        if (!ShouldLogLevel(e, Level.Debug)) return;
        int id = e != null ? e.entityId : -1;
        Log.Out($"[IconicAI][DEBUG] [{task}] id={id} {message}");
    }

    public static void Trace(EntityAlive e, string task, string message)
    {
        if (!ShouldLogLevel(e, Level.Trace)) return;
        int id = e != null ? e.entityId : -1;
        Log.Out($"[IconicAI][TRACE] [{task}] id={id} {message}");
    }
}
