using Audio;
using System;
using System.Linq;
using UnityEngine;
using static Audio.Manager;

namespace Rise.Radio
{
    public class EntityRadioSource : RadioSource
    {
        public EntityRadioSource()
        {
            IsOn = false;
            ClipName = "";
            PlayListPosition = 0;
            LastSyncTime = 0f;
        }
        
        public Entity Entity { get; set; }

        // Check if the entity still exists
        public override bool IsParentValid()
        {
            if (Entity == null) return false;
            
            try
            {
                Entity entity = GameManager.Instance.World.GetEntity(Entity.entityId);
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
                RadioDebug.D("ERS", $"Play '{soundGroup}'");
                ClipName = soundGroup;
                
                if (Entity == null)
                {
                    Log.Out("Entity is null, cannot play radio");
                    return;
                }
                
                // Use simple, direct call like working version
                Manager.Play(Entity, soundGroup);
                AudioSourceObject = GetAudioSource(Entity.entityId, soundGroup);
                
                if (AudioSourceObject != null)
                {
                    AudioSourceObject.dopplerLevel = 0;
                    
                    // CRITICAL FIX: Skip sync call for moving entities to prevent stuttering
                    // The audio will naturally start in sync since we just called Play
                    bool isMoving = Entity.motion.sqrMagnitude > 0.01f;
                    if (!isMoving)
                    {
                        SyncAudioSource(soundGroup);
                    }
                    else
                    {
                        Log.Out($"Entity radio skipping initial sync - entity moving (vel={Entity.motion.magnitude:F2})");
                    }
                    
                    IsOn = true;
                    Log.Out("Entity radio successfully started");
                }
                else
                {
                    Log.Out("Failed to find audio source for entity radio");
                    IsOn = false;
                }
            }
            catch (Exception e)
            {                
                Log.Out($"Exception : {e.Message}");
                RadioDebug.E("ERS", "Play error", e);
                IsOn = false;
            }
        }

        public override void Stop(string soundGroup)
        {
            try
            {
                IsOn = false;
                Manager.Stop(Entity.entityId, soundGroup);
                Log.Out("Entity radio stopped and cleaned up");
            }
            catch (Exception e)
            {
                Log.Out($"Error stopping entity radio: {e.Message}");
            }
        }
    }
}