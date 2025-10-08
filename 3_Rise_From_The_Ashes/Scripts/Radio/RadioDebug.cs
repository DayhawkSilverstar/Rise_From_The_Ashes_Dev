using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Rise.Radio
{
    /// <summary>
    /// Centralized, easily removable radio debug logger.
    /// Usage: RadioDebug.D("TAG", "message");
    /// All output is compiled out unless the RFTA_RADIO_DEBUG symbol is defined.
    /// </summary>
    public static class RadioDebug
    {
        [Conditional("RFTA_RADIO_DEBUG")]
        public static void D(string tag, string message, [CallerMemberName] string member = null)
        {
            try
            {
                string time = Application.isPlaying ? Time.time.ToString("F3") : DateTime.Now.ToString("HH:mm:ss.fff");
                // global::Log.Out($"[RADDBG][{tag}][{member}] t={time} {message}");
            }
            catch
            {
                // Swallow any logging exceptions in debug logger
            }
        }

        [Conditional("RFTA_RADIO_DEBUG")]
        public static void E(string tag, string message, Exception ex = null, [CallerMemberName] string member = null)
        {
            try
            {
                string time = Application.isPlaying ? Time.time.ToString("F3") : DateTime.Now.ToString("HH:mm:ss.fff");
                if (ex == null)
                {
                    // global::Log.Out($"[RADDBG][{tag}][{member}] t={time} ERROR: {message}");
                }
                else
                {
                    // global::Log.Out($"[RADDBG][{tag}][{member}] t={time} ERROR: {message} :: {ex.Message}\n{ex.StackTrace}");
                }
            }
            catch { }
        }

        [Conditional("RFTA_RADIO_DEBUG")]
        public static void Enter(string tag, [CallerMemberName] string member = null)
        {
            try
            {
                string time = Application.isPlaying ? Time.time.ToString("F3") : DateTime.Now.ToString("HH:mm:ss.fff");
                // global::Log.Out($"[RADDBG][{tag}][{member}] t={time} ENTER");
            }
            catch { }
        }

        [Conditional("RFTA_RADIO_DEBUG")]
        public static void Exit(string tag, [CallerMemberName] string member = null)
        {
            try
            {
                string time = Application.isPlaying ? Time.time.ToString("F3") : DateTime.Now.ToString("HH:mm:ss.fff");
                // global::Log.Out($"[RADDBG][{tag}][{member}] t={time} EXIT");
            }
            catch { }
        }
    }
}
