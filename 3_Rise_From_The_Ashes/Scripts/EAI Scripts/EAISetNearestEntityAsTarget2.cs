using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class EAISetNearestEntityAsTarget2 : EAITarget
{
    // Structure to hold target class information
    private struct TargetClass
    {
        public Type type; // Type of the target entity
        public float hearDistMax; // Maximum hearing distance
        public float seeDistMax; // Maximum seeing distance
    }

    private const float cHearDistMax = 50f; // Default maximum hearing distance

    private List<TargetClass> targetClasses; // List of target classes

    private int playerTargetClassIndex = -1; // Index of player target class in the list

    private float closeTargetDist; // Distance to the closest target

    private EntityAlive closeTargetEntity; // Closest target entity

    private EntityAlive targetEntity; // Current target entity

    private EntityPlayer targetPlayer; // Current target player

    private Vector3 lastSeenPos; // Last seen position of the target

    private float findTime; // Time spent finding the target

    private float senseSoundTime; // Time for sensing sound

    private EAISetNearestEntityAsTargetSorter sorter; // Sorter for target entities

    private static List<Entity> list = new List<Entity>(); // List to hold entities

    // Initialize the AI with the given entity
    public override void Init(EntityAlive _theEntity)
    {
        Init(_theEntity, 25f, _bNeedToSee: true);
        MutexBits = 1;
        sorter = new EAISetNearestEntityAsTargetSorter(_theEntity);
    }

    // Set data for the AI from a dictionary
    public override void SetData(DictionarySave<string, string> data)
    {
        base.SetData(data);
        targetClasses = new List<TargetClass>();
        if (!data.TryGetValue("class", out var _value))
        {
            return;
        }

        string[] array = _value.Split(',');
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
    }

    // Set the target to only players within a certain distance
    public void SetTargetOnlyPlayers(float _distance)
    {
        targetClasses.Clear();
        TargetClass item = default(TargetClass);
        item.type = typeof(EntityPlayer);
        item.hearDistMax = _distance;
        item.seeDistMax = 0f - _distance;
        targetClasses.Add(item);
        playerTargetClassIndex = 0;
    }

    // Check if the AI can execute its behavior
    public override bool CanExecute()
    {
        if (theEntity.distraction != null)
        {
            return false;
        }

        FindTarget();
        if (!closeTargetEntity)
        {
            return false;
        }

        targetEntity = closeTargetEntity;
        targetPlayer = closeTargetEntity as EntityPlayer;
        return true;
    }

    // Find the closest target entity
    private void FindTarget()
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
                        if (theEntity.noisePlayerVolume >= theEntity.noiseWake)
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
#if DEBUG
                        Log.Out("Player {0}, living {1}, lost BM immunity", entityPlayer.GetDebugName(), entityPlayer.currentLife * 60f);
#endif
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
    }

    // Seek the noise made by a player
    private void SeekNoise(EntityPlayer player)
    {
        float magnitude = (player.position - theEntity.position).magnitude;
        if (playerTargetClassIndex >= 0)
        {
            float hearDistMax = targetClasses[playerTargetClassIndex].hearDistMax;
            hearDistMax *= theEntity.senseScale;
            hearDistMax *= player.DetectUsScale(theEntity);
            if (magnitude > hearDistMax)
            {
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
        float time = Time.time;
        if (senseSoundTime - time < 0f)
        {
            senseSoundTime = time + 10f + base.RandomFloat * 10f;
            theEntity.PlayOneShot(theEntity.soundSense);
        }
    }

    // Find the closest player target
    private void FindTargetPlayer(float seeDist)
    {
        if (theEntity.IsSleeperPassive)
        {
            return;
        }

        theEntity.world.GetEntitiesInBounds(typeof(EntityPlayer), BoundsUtils.ExpandBounds(theEntity.boundingBox, seeDist, seeDist, seeDist), list);
        if (theEntity.IsSleeping)
        {
            if (theEntity.IsSleeping)
            {
                return;
            }

            list.Sort(sorter);
            EntityPlayer entityPlayer = null;
            float num = float.MaxValue;
            bool flag = false;
            if (theEntity.noisePlayer != null)
            {
                if (theEntity.noisePlayerVolume >= theEntity.noiseWake)
                {
                    entityPlayer = theEntity.noisePlayer;
                    num = theEntity.noisePlayerDistance;
                }
                else if (theEntity.noisePlayerVolume >= theEntity.noiseGroan)
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

            return;
        }

        for (int j = 0; j < list.Count; j++)
        {
            EntityPlayer entityPlayer3 = (EntityPlayer)list[j];
            if (entityPlayer3.IsAlive() && !entityPlayer3.IsIgnoredByAI())
            {
                float distance2 = theEntity.GetDistance(entityPlayer3);
                if (distance2 < closeTargetDist && theEntity.CanSee(entityPlayer3) && theEntity.CanSeeStealth(distance2, entityPlayer3.Stealth.lightLevel))
                {
                    closeTargetDist = distance2;
                    closeTargetEntity = entityPlayer3;
                }
            }
        }

        list.Clear();
    }

    public override void Start()
    {
        theEntity.SetAttackTarget(targetEntity, 200);
        theEntity.ConditionalTriggerSleeperWakeUp();
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

            return false;
        }

        findTime += 0.05f;
        if (findTime > 2f)
        {
            findTime = 0f;
            FindTarget();
            if ((bool)closeTargetEntity && closeTargetEntity != targetEntity)
            {
                return false;
            }
        }

        if (theEntity.GetAttackTarget() != targetEntity)
        {
            return false;
        }

        if (check(targetEntity) && (targetPlayer == null || theEntity.CanSeeStealth(theEntity.GetDistance(targetEntity), targetPlayer.Stealth.lightLevel)))
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
        int num = theEntity.CalcInvestigateTicks(Constants.cEnemySenseMemory * 20, targetEntity);
        if (lastSeenPos != Vector3.zero)
        {
            theEntity.SetInvestigatePosition(lastSeenPos, num);
        }

        if ((bool)targetPlayer)
        {
            if (theEntity.entityType == EntityType.Zombie)
            {
                num /= 6;
            }

            theEntity.SetAlertTicks(num);
        }

        return false;
    }

    public override void Reset()
    {
        targetEntity = null;
        targetPlayer = null;
    }

    public override string ToString()
    {
        return string.Format("{0}, {1}", base.ToString(), targetEntity ? targetEntity.EntityName : "");
    }
}

