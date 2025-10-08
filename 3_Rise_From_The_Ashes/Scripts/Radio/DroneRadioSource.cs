using Audio;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using static Audio.Manager;

namespace Rise.Radio
{
    public class DroneRadioSource : RadioSource
    {
        public DroneRadioSource()
        {
            IsOn = false;
            ClipName = "";
            PlayListPosition = 0;
            LastSyncTime = 0f;
        }

        public RiseDrone Drone { get; set; }

        // Check if the drone entity still exists
        public override bool IsParentValid()
        {
            if (Drone == null) return false;
            
            try
            {
                Entity entity = GameManager.Instance.World.GetEntity(Drone.entityId);
                return entity != null && !entity.IsDead();
            }
            catch
            {
                return false;
            }
        }

        public override void Play(string soundGroup)
        {
            try
            {
                Log.Out("DroneRadioSource Playing Radio : " + soundGroup);
                RadioDebug.D("DRS", $"Play '{soundGroup}'");
                ClipName = soundGroup;
                Entity entity = GameManager.Instance.World.GetEntity(Drone.entityId);
                
                if (entity == null)
                {
                    Log.Out("Entity is null, cannot play drone radio");
                    return;
                }
                
                Log.Out("Entity Name : " + entity.name);
                
                // FIXED: Use simple, direct call like working version
                Manager.Play(entity, soundGroup);
                AudioSourceObject = GetAudioSource(Drone.entityId, soundGroup);
                
                if (AudioSourceObject != null)
                {
                    AudioSourceObject.dopplerLevel = 0;
                    SyncAudioSource(soundGroup);
                    IsOn = true;

                    // Update drone radio state
                    Drone.SetRadioOn(true);
                    
                    Log.Out("Drone radio successfully started");
                }
                else
                {
                    Log.Out("Failed to find audio source for drone radio");
                    IsOn = false;
                }
            }
            catch (Exception e)
            {
                Log.Out("Error playing drone radio source.");
                Log.Out($"Exception : {e.Message}");
                RadioDebug.E("DRS", "Play error", e);
                IsOn = false;
            }
        }

        public override void Stop(string soundGroup)
        {
            try
            {
                IsOn = false;
                Manager.Stop(Drone.entityId, soundGroup);

                // Update drone radio state
                if (Drone != null)
                {
                    Drone.SetRadioOn(false);
                }
                
                Log.Out("Drone radio stopped and cleaned up");
            }
            catch (Exception e)
            {
                Log.Out($"Error stopping drone radio: {e.Message}");
            }
        }

        public AudioSource FindAudioSource(string clipName)
        {
            try
            {
                if (Drone?.transform != null)
                {
                    foreach (AudioSource source in Drone.transform.GetComponentsInChildren<AudioSource>())
                    {
                        if (source.clip != null && source.clip.name == clipName)
                        {
                            return source;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Log.Out($"Error finding drone audio source: {e.Message}");
            }
            return null;
        }
    }
}