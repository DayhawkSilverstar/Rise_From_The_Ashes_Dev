using Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static Audio.Manager;

namespace Rise.Radio
{
    public abstract class RadioSource
    {
        public int EntityID { get; set; }
        public string Name { get; set; }
        public bool IsOn { get; set; }
        public string ClipName { get; set; }
        public Vector3 Position { get; set; }
        public int PlayListPosition { get; set; }
        
        // Use AudioSource like the working version
        public AudioSource AudioSourceObject { get; set; }

        // New properties for improved synchronization
        public float LastSyncTime { get; set; }
        public bool IsValidAudioSource { get; private set; }

        public abstract void Play(string soundGroup);
        public abstract void Stop(string soundGroup);
        
        /// <summary>
        /// Swap to a new clip deterministically on this radio, scheduling the start for a shared DSP time.
        /// Base implementation falls back to Stop/Play; specific sources should override for tighter control.
        /// </summary>
        public virtual void SwapClip(string clipName, float startTimeSeconds, double dspStart)
        {
            try
            {
                RadioDebug.D("RS", $"SwapClip '{clipName}' t={startTimeSeconds:F2} dsp={dspStart:F3}");
                // Default: stop and play normally; derived classes will implement deterministic scheduling
                if (IsOn && !string.IsNullOrEmpty(ClipName))
                {
                    Stop(ClipName);
                }
                ClipName = clipName;
                Play(clipName);
            }
            catch (Exception e)
            {
                Log.Out($"[RS] SwapClip fallback error for {Name}: {e.Message}");
                RadioDebug.E("RS", "SwapClip fallback error", e);
            }
        }

        /// <summary>
        /// Recovery path used by watchdog to recreate a source and resume the expected clip at a sync time.
        /// </summary>
        public virtual void ReinitAndRestart(string clipName, float startTimeSeconds)
        {
            try
            {
                RadioDebug.D("RS", $"ReinitAndRestart '{clipName}' t={startTimeSeconds:F2}");
                ClipName = clipName;
                Play(clipName);
                // attempt to seek after play if we have a clip
                if (AudioSourceObject != null && AudioSourceObject.clip != null)
                {
                    float clamped = Mathf.Clamp(startTimeSeconds, 0f, AudioSourceObject.clip.length - 0.05f);
                    AudioSourceObject.time = clamped;
                }
                IsOn = true;
            }
            catch (Exception e)
            {
                Log.Out($"[RS] ReinitAndRestart error for {Name}: {e.Message}");
                RadioDebug.E("RS", "ReinitAndRestart error", e);
            }
        }
        
        /// <summary>
        /// Returns a list of AudioSources that are playing the specified clip with thread safety.
        /// </summary>
        /// <param name="ClipName"></param>
        /// <returns>List</returns>
        public static List<AudioSource> GetAudioSources(string ClipName)
        {
            RadioDebug.D("RS", $"GetAudioSources '{ClipName}'");
            List<AudioSource> sources = new List<AudioSource>();
            
            try
            {
                // Guard against audio system not yet initialized or shutting down
                if (Manager.playingAudioSources == null)
                {
                    Log.Out("[RS] playingAudioSources is null (audio system not ready or shutting down)");
                    return sources;
                }

                // Use the game's playingAudioSources like the working version
                lock (Manager.playingAudioSources)
                {
                    Log.Out("[RS] playingAudioSources.Count=" + Manager.playingAudioSources.Count);
                    foreach (AudioSource source in Manager.playingAudioSources)
                    {
                        if (source != null && source.clip != null)
                        {
                            string sName = source.name;
                            string cName = source.clip.name;
                            if (cName == ClipName || cName.Contains(ClipName) || ClipName.Contains(cName))
                            {
                                Log.Out($"[RS]  MATCH src='{sName}' clip='{cName}'");
                                sources.Add(source);
                            }
                            else
                            {
                                // noisy diagnostic, keep only in debug wrapper
                                RadioDebug.D("RS", $"skip src='{sName}' clip='{cName}'");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out("[RS] Error getting audio sources list: " + e.Message);
                RadioDebug.E("RS", "GetAudioSources error", e);
            }
            
            Log.Out($"[RS] GetAudioSources result count={sources.Count}");
            return sources;
        }

        /// <summary>
        /// Returns the AudioSource that is playing the specified clip at the specified position with improved tolerance.
        /// /// </summary>
        /// <param name="position"></param>
        /// <param name="clipName"></param>
        /// <returns>AudioSource</returns>
        public static AudioSource GetAudioSource(Vector3 position, string clipName)
        {
            try
            {
                RadioDebug.D("RS", $"GetAudioSource pos={position} clip='{clipName}'");
                var sources = GetAudioSources(clipName);
                Log.Out($"[RS] Filtered sources count: {sources.Count}");

                // Use audio origin (static struct/type exposed by Manager)
                Vector3 originPos = Origin.position;

                foreach (AudioSource source in sources)
                {
                    if (source == null || source.transform == null) continue;

                    Vector3 srcPos = source.transform.position + originPos;
                    float distance = Vector3.Distance(srcPos, position);
                    bool near = distance < 4f;
                    Log.Out($"[RS]  cand src='{source.name}' worldPos={srcPos} dist={distance:F2} near={near} playing={source.isPlaying}");
                    
                    if (near)
                    {
                        Log.Out($"[RS]  -> return src='{source.name}'");
                        return source;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out("[RS] Error getting audio source: " + e.Message);
                RadioDebug.E("RS", "GetAudioSource(pos) error", e);
            }

            Log.Out("[RS] No Audio Source Found by position");
            return null;
        }

        /// <summary>
        /// Returns the AudioSource that is playing the specified clip with the specified entityID with improved matching.
        /// /// </summary>
        /// <param name="entityID"></param>
        /// <param name="clipName"></param>
        /// <returns>AudioSource</returns>
        public static AudioSource GetAudioSource(int entityID, string clipName)
        {
            try
            {
                RadioDebug.D("RS", $"GetAudioSource eid={entityID} clip='{clipName}'");
                Entity entity = GameManager.Instance.World.GetEntity(entityID);
                
                List<AudioSource> audioSources = GetAudioSources(clipName);
                
                if (audioSources.Count == 0)
                {
                    Log.Out("[RS] No Audio Sources Found for clip");
                    return null;
                }

                if (entity == null)
                {
                    Log.Out("[RS] Entity is null");
                    return null;
                }

                Log.Out("[RS] Entity Name: " + entity.name + " pos=" + entity.position);

                Vector3 originPos = Origin.position;

                foreach (AudioSource source in audioSources)
                {
                    if (source == null || source.transform == null) continue;

                    Vector3 sourceWorldPos = source.transform.position + originPos;
                    float distance = Vector3.Distance(entity.position, sourceWorldPos);
                    bool near = distance < 4f;
                    Log.Out($"[RS]  cand src='{source.name}' worldPos={sourceWorldPos} dist={distance:F2} near={near} playing={source.isPlaying}");
                    
                    if (near)
                    {
                        Log.Out($"[RS]  -> return src='{source.name}'");
                        return source;
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out("[RS] Error getting audio source: " + e.Message);
                RadioDebug.E("RS", "GetAudioSource(eid) error", e);
            }

            Log.Out("[RS] No Audio Source Found by entity");
            return null;
        }

        /// <summary>
        /// Checks the audio source to see if it is playing with validation.
        /// /// </summary>
        /// <returns>Bool</returns>
        public bool IsPlaying()
        {
            if (AudioSourceObject != null)
            {
                return AudioSourceObject.isPlaying;
            }
            return false;
        }

        /// <summary>
        /// Improved synchronization like working version but with better error handling
        /// IMPORTANT: Skip sync for moving entities to prevent stuttering
        /// </summary>
        /// <param name="ClipName"></param>
        public static void SyncAudioSource(String ClipName)
        {
            try
            {
                float latestPlayTime = 0;
                List<AudioSource> sources = new List<AudioSource>();
                AudioSource sourcePrimary = null;
                
                if (Manager.playingAudioSources == null)
                {
                    Log.Out("[RS] SyncAudioSource: playingAudioSources is null; skipping sync");
                    return;
                }
                
                lock (Manager.playingAudioSources)
                {
                    foreach (AudioSource source in Manager.playingAudioSources)
                    {
                        if (source != null && source.clip != null && source.isPlaying)
                        {
                            if (source.clip.name == ClipName || source.clip.name.Contains(ClipName) || ClipName.Contains(source.clip.name))
                            {
                                sources.Add(source);
                                if (source.time >= latestPlayTime)
                                {
                                    latestPlayTime = source.time;
                                    sourcePrimary = source;
                                    Log.Out("[RS] Primary Source : " + source.name + " t=" + latestPlayTime.ToString("F2"));
                                }
                            }
                        }
                    }
                }

                Log.Out($"[RS] SyncAudioSource clip='{ClipName}' candidates={sources.Count} latest={latestPlayTime:F2}");

                if (sourcePrimary == null || sources.Count <= 1)
                {
                    Log.Out("[RS] Sync not needed (none or single source)");
                    return;
                }

                // Validate the sync time is within reasonable bounds
                if (sourcePrimary.clip != null && latestPlayTime > sourcePrimary.clip.length)
                {
                    Log.Out("[RS] Warning: Primary source time exceeds clip length, skipping sync");
                    return;
                }

                foreach (AudioSource source in sources)
                {
                    if (source == sourcePrimary) 
                    {
                        Log.Out($"[RS] primary '{source.name}' time={source.time:F2}");
                        continue;
                    }

                    try
                    {
                        // CRITICAL FIX: Check if this audio source is attached to a moving entity
                        // Skip sync to prevent stuttering from audio time changes during position updates
                        bool isEntityAttached = false;
                        Entity attachedEntity = null;
                        
                        if (source.transform != null)
                        {
                            // Check if parent has Entity component (indicates entity-attached audio)
                            Transform current = source.transform;
                            while (current != null && attachedEntity == null)
                            {
                                attachedEntity = current.GetComponent<Entity>();
                                if (attachedEntity != null) break;
                                current = current.parent;
                            }
                            
                            if (attachedEntity != null)
                            {
                                isEntityAttached = true;
                                // Skip sync if entity is actively moving (velocity check)
                                if (attachedEntity.motion.sqrMagnitude > 0.01f)
                                {
                                    Log.Out($"[RS] skip sync '{source.name}' - entity moving (vel={attachedEntity.motion.magnitude:F2})");
                                    continue;
                                }
                            }
                        }
                        
                        // Only sync if the difference is significant
                        float timeDifference = Mathf.Abs(source.time - latestPlayTime);
                        
                        // Use larger threshold for entity-attached sources to reduce interference
                        float syncThreshold = isEntityAttached ? 0.5f : 0.1f;
                        
                        if (timeDifference > syncThreshold)
                        {
                            Log.Out($"[RS] sync '{source.name}' from {source.time:F2} -> {latestPlayTime:F2} (diff {timeDifference:F2})");
                            source.time = latestPlayTime;
                        }
                        else
                        {
                            Log.Out($"[RS] skip sync '{source.name}' diff={timeDifference:F2}");
                        }
                    }
                    catch (Exception syncEx)
                    {
                        Log.Out($"[RS] Error syncing source {source.name}: {syncEx.Message}");
                        RadioDebug.E("RS", $"Error syncing {source.name}", syncEx);
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out("[RS] Error syncing audio source: " + e.Message);
                RadioDebug.E("RS", "SyncAudioSource error", e);
            }
        }

        /// <summary>
        /// Check if the parent entity/block still exists and is valid
        /// </summary>
        public virtual bool IsParentValid()
        {
            // Default implementation - should be overridden by derived classes
            return true;
        }
    }
}