using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;

[Preserve]
public class EAISetNearestEntityAsTargetIconic : EAITarget
{
    [PublicizedFrom(EAccessModifier.Private)]
    public struct TargetClass
    {
        public Type type;

        public float hearDistMax;

        public float seeDistMax;
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public const float cHearDistMax = 50f;

    [PublicizedFrom(EAccessModifier.Private)]
    public List<TargetClass> targetClasses;

    [PublicizedFrom(EAccessModifier.Private)]
    public int playerTargetClassIndex = -1;

    [PublicizedFrom(EAccessModifier.Private)]
    public float closeTargetDist;

    [PublicizedFrom(EAccessModifier.Private)]
    public EntityAlive closeTargetEntity;

    [PublicizedFrom(EAccessModifier.Private)]
    public EntityAlive targetEntity;

    [PublicizedFrom(EAccessModifier.Private)]
    public EntityPlayer targetPlayer;

    [PublicizedFrom(EAccessModifier.Private)]
    public Vector3 lastSeenPos;

    [PublicizedFrom(EAccessModifier.Private)]
    public float findTime;

    [PublicizedFrom(EAccessModifier.Private)]
    public float senseSoundTime;

    [PublicizedFrom(EAccessModifier.Private)]
    public EAISetNearestEntityAsTargetSorter sorter;

    [PublicizedFrom(EAccessModifier.Private)]
    public static List<Entity> list = new List<Entity>();

    private string TaskName => nameof(EAISetNearestEntityAsTargetIconic);

    public override void Init(EntityAlive _theEntity)
    {
        Init(_theEntity, 25f, _bNeedToSee: true);
        MutexBits = 1;
        sorter = new EAISetNearestEntityAsTargetSorter(_theEntity);
        IconicLog.Info(theEntity, TaskName, $"Init: seeDist={25f} needToSee={true}");
    }

    public override void SetData(DictionarySave<string, string> data)
    {
        base.SetData(data);
        targetClasses = new List<TargetClass>();
        if (!data.TryGetValue("class", out var _value))
        {
            IconicLog.Warn(theEntity, TaskName, "SetData: no 'class' specified");
            return;
        }

        string[] array = _value.Split(',', (char)StringSplitOptions.None);
        TargetClass item = default(TargetClass);
        for (int i = 0; i < array.Length; i += 3)
        {
            item.type = EntityFactory.GetEntityType(array[i]);
            item.hearDistMax = 0f;
            if (i + 1 < array.Length)
            {
                item.hearDistMax = StringParsers.ParseFloat(array[i + 1]);
            }

            if (item.hearDistMax == 0f)
            {
                item.hearDistMax = 50f;
            }

            item.seeDistMax = 0f;
            if (i + 2 < array.Length)
            {
                item.seeDistMax = StringParsers.ParseFloat(array[i + 2]);
            }

            if (item.type == typeof(EntityPlayer))
            {
                playerTargetClassIndex = targetClasses.Count;
            }

            targetClasses.Add(item);
        }
        IconicLog.Info(theEntity, TaskName, $"SetData: targetClasses={targetClasses.Count} playerIndex={playerTargetClassIndex}");
    }

    public void SetTargetOnlyPlayers(float _distance)
    {
        targetClasses.Clear();
        TargetClass item = default(TargetClass);
        item.type = typeof(EntityPlayer);
        item.hearDistMax = _distance;
        item.seeDistMax = 0f - _distance;
        targetClasses.Add(item);
        playerTargetClassIndex = 0;
        IconicLog.Info(theEntity, TaskName, $"SetTargetOnlyPlayers: dist={_distance}");
    }

    public override bool CanExecute()
    {
        if (theEntity.distraction != null)
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: distraction present");
            return false;
        }

        FindTarget();
        if (!closeTargetEntity)
        {
            IconicLog.Trace(theEntity, TaskName, "CanExecute=false: no closeTargetEntity");
            return false;
        }

        targetEntity = closeTargetEntity;
        targetPlayer = closeTargetEntity as EntityPlayer;
        IconicLog.Debug(theEntity, TaskName, $"CanExecute=true: target={targetEntity?.EntityName} dist={closeTargetDist:0.00}");
        return true;
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public void FindTarget()
    {
        closeTargetDist = float.MaxValue;
        closeTargetEntity = null;
        float seeDistance = theEntity.GetSeeDistance();
        for (int i = 0; i < targetClasses.Count; i++)
        {
            TargetClass targetClass = targetClasses[i];
            float num = seeDistance;
            if (targetClass.seeDistMax != 0f)
            {
                float v = ((targetClass.seeDistMax < 0f) ? (0f - targetClass.seeDistMax) : (targetClass.seeDistMax * theEntity.senseScale));
                num = Utils.FastMin(num, v);
            }

            if (targetClass.type == typeof(EntityPlayer))
            {
                FindTargetPlayer(num);
                if ((bool)theEntity.noisePlayer && theEntity.noisePlayer != closeTargetEntity)
                {
                    if ((bool)closeTargetEntity)
                    {
                        if (theEntity.noisePlayerVolume >= theEntity.sleeperNoiseToWake)
                        {
                            Vector3 position = theEntity.noisePlayer.position;
                            float magnitude = (theEntity.position - position).magnitude;
                            if (magnitude < closeTargetDist)
                            {
                                closeTargetDist = magnitude;
                                closeTargetEntity = theEntity.noisePlayer;
                            }
                        }
                    }
                    else if (!theEntity.IsSleeping)
                    {
                        SeekNoise(theEntity.noisePlayer);
                    }
                }

                if ((bool)closeTargetEntity)
                {
                    EntityPlayer entityPlayer = (EntityPlayer)closeTargetEntity;
                    if (entityPlayer.IsBloodMoonDead && entityPlayer.currentLife >= 0.5f)
                    {
                        Log.Out("Player {0}, living {1}, lost BM immunity", entityPlayer.GetDebugName(), entityPlayer.currentLife * 60f);
                        entityPlayer.IsBloodMoonDead = false;
                    }
                }
            }
            else
            {
                if (theEntity.IsSleeping || theEntity.HasInvestigatePosition)
                {
                    continue;
                }

                theEntity.world.GetEntitiesInBounds(targetClass.type, BoundsUtils.ExpandBounds(theEntity.boundingBox, num, 4f, num), list);
                list.Sort(sorter);
                for (int j = 0; j < list.Count; j++)
                {
                    EntityAlive entityAlive = (EntityAlive)list[j];
                    if (!(entityAlive is EntityDrone) && check(entityAlive))
                    {
                        float distance = theEntity.GetDistance(entityAlive);
                        if (distance < closeTargetDist)
                        {
                            closeTargetDist = distance;
                            closeTargetEntity = entityAlive;
                            lastSeenPos = entityAlive.position;
                        }

                        break;
                    }
                }

                list.Clear();
            }
        }
        IconicLog.Trace(theEntity, TaskName, $"FindTarget: result target={closeTargetEntity?.EntityName} dist={closeTargetDist:0.00}");
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public void SeekNoise(EntityPlayer player)
    {
        float magnitude = (player.position - theEntity.position).magnitude;
        if (playerTargetClassIndex >= 0)
        {
            float hearDistMax = targetClasses[playerTargetClassIndex].hearDistMax;
            hearDistMax *= theEntity.senseScale;
            hearDistMax *= player.DetectUsScale(theEntity);
            if (magnitude > hearDistMax)
            {
                IconicLog.Trace(theEntity, TaskName, $"SeekNoise: too far magnitude={magnitude:0.0} max={hearDistMax:0.0}");
                return;
            }
        }

        magnitude *= 0.9f;
        if (magnitude > manager.noiseSeekDist)
        {
            magnitude = manager.noiseSeekDist;
        }

        if (theEntity.IsBloodMoon)
        {
            magnitude = manager.noiseSeekDist * 0.25f;
        }

        Vector3 breadcrumbPos = player.GetBreadcrumbPos(magnitude * base.RandomFloat);
        int ticks = theEntity.CalcInvestigateTicks((int)(30f + base.RandomFloat * 30f) * 20, player);
        theEntity.SetInvestigatePosition(breadcrumbPos, ticks);
        PlaySoundSenseNoise();
        IconicLog.Debug(theEntity, TaskName, $"SeekNoise: breadcrumb={breadcrumbPos} ticks={ticks}");
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public void PlaySoundSenseNoise()
    {
        float time = Time.time;
        if (senseSoundTime - time < 0f)
        {
            senseSoundTime = time + 10f + base.RandomFloat * 10f;
            theEntity.PlayOneShot(theEntity.soundSense);
            IconicLog.Trace(theEntity, TaskName, $"PlaySoundSenseNoise: nextTime={senseSoundTime:0.00}");
        }
    }

    [PublicizedFrom(EAccessModifier.Private)]
    public void FindTargetPlayer(float seeDist)
    {
        if (theEntity.IsSleeperPassive)
        {
            IconicLog.Trace(theEntity, TaskName, "FindTargetPlayer: sleeper passive");
            return;
        }

        theEntity.world.GetEntitiesInBounds(typeof(EntityPlayer), BoundsUtils.ExpandBounds(theEntity.boundingBox, seeDist, seeDist, seeDist), list);
        if (theEntity.IsSleeping)
        {
            list.Sort(sorter);
            EntityPlayer entityPlayer = null;
            float num = float.MaxValue;
            bool flag = false;
            if (theEntity.noisePlayer != null)
            {
                if (theEntity.noisePlayerVolume >= theEntity.sleeperNoiseToWake)
                {
                    entityPlayer = theEntity.noisePlayer;
                    num = theEntity.noisePlayerDistance;
                }
                else if (theEntity.noisePlayerVolume >= theEntity.sleeperNoiseToSense)
                {
                    flag = true;
                }
            }

            for (int i = 0; i < list.Count; i++)
            {
                EntityPlayer entityPlayer2 = (EntityPlayer)list[i];
                if (!theEntity.CanSee(entityPlayer2) || entityPlayer2.IsIgnoredByAI())
                {
                    continue;
                }

                float distance = theEntity.GetDistance(entityPlayer2);
                int sleeperDisturbedLevel = theEntity.GetSleeperDisturbedLevel(distance, entityPlayer2.Stealth.lightLevel);
                if (sleeperDisturbedLevel >= 2)
                {
                    if (distance < num)
                    {
                        entityPlayer = entityPlayer2;
                        num = distance;
                    }
                }
                else if (sleeperDisturbedLevel >= 1)
                {
                    flag = true;
                }
            }

            list.Clear();
            if (entityPlayer != null)
            {
                closeTargetDist = num;
                closeTargetEntity = entityPlayer;
            }
            else if (flag)
            {
                theEntity.Groan();
            }
            else
            {
                theEntity.Snore();
            }

            IconicLog.Trace(theEntity, TaskName, $"FindTargetPlayer(sleep): target={(entityPlayer!=null ? entityPlayer.EntityName : "<none>")} dist={num:0.00} flag={flag}");
            return;
        }

        for (int j = 0; j < list.Count; j++)
        {
            EntityPlayer entityPlayer3 = (EntityPlayer)list[j];
            if (entityPlayer3.IsAlive() && !entityPlayer3.IsIgnoredByAI())
            {
                float seeDistance = manager.GetSeeDistance(entityPlayer3);
                if (seeDistance < closeTargetDist && theEntity.CanSee(entityPlayer3) && theEntity.CanSeeStealth(seeDistance, entityPlayer3.Stealth.lightLevel))
                {
                    closeTargetDist = seeDistance;
                    closeTargetEntity = entityPlayer3;
                }
            }
        }

        list.Clear();
        IconicLog.Trace(theEntity, TaskName, $"FindTargetPlayer: result={(closeTargetEntity!=null ? closeTargetEntity.EntityName : "<none>")} dist={closeTargetDist:0.00}");
    }

    public override void Start()
    {
        theEntity.SetAttackTarget(targetEntity, 200);
        theEntity.ConditionalTriggerSleeperWakeUp();
        PlaySoundSenseNoise();
        IconicLog.Info(theEntity, TaskName, $"Start: target={targetEntity?.EntityName}");
        base.Start();
    }

    public override bool Continue()
    {
        if (targetEntity.IsDead() || theEntity.distraction != null)
        {
            if (theEntity.GetAttackTarget() == targetEntity)
            {
                theEntity.SetAttackTarget(null, 0);
            }

            IconicLog.Trace(theEntity, TaskName, "Continue=false: target dead or distraction");
            return false;
        }

        findTime += 0.05f;
        if (findTime > 2f)
        {
            findTime = 0f;
            FindTarget();
            if ((bool)closeTargetEntity && closeTargetEntity != targetEntity)
            {
                IconicLog.Trace(theEntity, TaskName, "Continue=false: switched to closer target");
                return false;
            }
        }

        if (theEntity.GetAttackTarget() != targetEntity)
        {
            IconicLog.Trace(theEntity, TaskName, "Continue=false: attack target mismatch");
            return false;
        }

        if (check(targetEntity) && (targetPlayer == null || theEntity.CanSeeStealth(manager.GetSeeDistance(targetEntity), targetPlayer.Stealth.lightLevel)))
        {
            theEntity.SetAttackTarget(targetEntity, 600);
            lastSeenPos = targetEntity.position;
            return true;
        }

        if (theEntity.GetDistanceSq(lastSeenPos) < 2.25f)
        {
            lastSeenPos = Vector3.zero;
        }

        theEntity.SetAttackTarget(null, 0);
        int ticks = theEntity.CalcInvestigateTicks(Constants.cEnemySenseMemory * 20, targetEntity);
        if (lastSeenPos != Vector3.zero)
        {
            theEntity.SetInvestigatePosition(lastSeenPos, ticks);
        }

        IconicLog.Trace(theEntity, TaskName, "Continue=false: lost target, set investigate position");
        return false;
    }

    public override void Reset()
    {
        targetEntity = null;
        targetPlayer = null;
        IconicLog.Info(theEntity, TaskName, "Reset: cleared target references");
    }

    public override string ToString()
    {
        return string.Format("{0}, {1}", base.ToString(), targetEntity ? targetEntity.EntityName : "");
    }
}