using Audio;
using Platform;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;


public class RiseDrone : EntityDrone
{
    bool radioOn = false;
    private RadioManager radioManager;    

    public RiseDrone()
    {
        
    }

    public override void Init(int _entityClass)
    {
        base.Init(_entityClass);
        radioManager = RadioManager.Instance;
        radioManager.AddRadio(this);
    }

    public override EntityActivationCommand[] GetActivationCommands(Vector3i _tePos, EntityAlive _entityFocusing)
    {
        bool flag = !IsDead();
        if (IsDead())
        {
            return new EntityActivationCommand[0];
        }

        bool flag2 = false;
        if (belongsToPlayerId(_entityFocusing.entityId))
        {
            flag2 = (_entityFocusing as EntityPlayerLocal).IsGodMode.Value && Debug.isDebugBuild;
            return new EntityActivationCommand[15]
            {
                new EntityActivationCommand("talk", "talk", flag && state != State.Shutdown),
                new EntityActivationCommand("service", "service", flag2),
                new EntityActivationCommand("repair", "wrench", (float)Health < base.Stats.Health.Max),
                new EntityActivationCommand("lock", "lock", !isLocked),
                new EntityActivationCommand("unlock", "unlock", isLocked),
                new EntityActivationCommand("keypad", "keypad", _enabled: true),
                new EntityActivationCommand("take", "hand", _enabled: true),
                new EntityActivationCommand("stay", "run_and_gun", flag && OrderState != Orders.Stay && state != State.Shutdown),
                new EntityActivationCommand("follow", "run", flag && OrderState != 0 && state != State.Shutdown),
                new EntityActivationCommand("heal", "cardio", flag && state != State.Shutdown && TargetCanBeHealed(_entityFocusing)),
                new EntityActivationCommand("storage", "loot_sack", _enabled: true),
                new EntityActivationCommand("drone_silent", isQuietMode ? "sight" : "stealth", _enabled: true),
                new EntityActivationCommand("drone_light", isFlashlightOn ? "electric_switch" : "lightbulb", isFlashlightAttached),
                new EntityActivationCommand("force_pickup", "store_all_up", flag2),
                new EntityActivationCommand("radio", "talk", flag && state != State.Shutdown)
            };
        }

        bool flag3 = !isLocked || belongsToPlayerId(_entityFocusing.entityId) || IsUserAllowed(PlatformManager.InternalLocalUserIdentifier);
        bool flag4 = isLocked && !IsUserAllowed(PlatformManager.InternalLocalUserIdentifier);
        bool flag5 = (float)Health < base.Stats.Health.Max;
        if (flag3 || flag4 || flag5 || flag2)
        {
            return new EntityActivationCommand[4]
            {
                new EntityActivationCommand("storage", "loot_sack", flag3),
                new EntityActivationCommand("keypad", "keypad", flag4),
                new EntityActivationCommand("repair", "wrench", flag5),
                new EntityActivationCommand("force_pickup", "store_all_up", flag2)
            };
        }

        PlaySound("ui_denied");
        return new EntityActivationCommand[0];
    }

    public override bool OnEntityActivated(int _indexInBlockActivationCommands, Vector3i _tePos, EntityAlive _entityFocusing)
    {
        EntityPlayerLocal entityPlayer = _entityFocusing as EntityPlayerLocal;
        LocalPlayerUI uIForPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayer);
        int requestType = -1;
        if (belongsToPlayerId(_entityFocusing.entityId))
        {
            switch (_indexInBlockActivationCommands)
            {
                case 0:
                    startDialog(_entityFocusing);
                    break;
                case 1:
                    requestType = _indexInBlockActivationCommands;
                    break;
                case 2:
                    doRepairAction(entityPlayer, uIForPlayer);
                    break;
                case 3:
                    PlaySound("misc/locking");
                    isLocked = !isLocked;
                    SendSyncData(2);
                    break;
                case 4:
                    PlaySound("misc/unlocking");
                    isLocked = !isLocked;
                    SendSyncData(2);
                    break;
                case 5:
                    doKeypadAction(uIForPlayer);
                    break;
                case 6:
                    pickup(_entityFocusing);
                    break;
                case 7:
                    SentryMode();
                    break;
                case 8:
                    FollowMode();
                    break;
                case 9:
                    HealOwner();
                    break;
                case 10:
                    requestType = _indexInBlockActivationCommands;
                    break;
                case 11:
                    isQuietMode = !isQuietMode;
                    idleLoop?.Stop(entityId);
                    idleLoop = null;
                    SendSyncData(32);
                    break;
                case 12:
                    doToggleLightAction();
                    break;
                case 13:
                    pickup(_entityFocusing);
                    break;
                case 14:
                    if (radioOn)
                    { 
                        radioOn = false;
                        radioManager.StopRadio(this);                        
                    }
                    else
                    {
                        radioOn = true;
                        radioManager.PlayRadio(this);                        
                    }
                                      
                    break;
            }
        }
        else
        {
            switch (_indexInBlockActivationCommands)
            {
                case 0:
                    requestType = 10;
                    break;
                case 1:
                    doKeypadAction(uIForPlayer);
                    break;
                case 2:
                    requestType = 2;
                    doRepairAction(entityPlayer, uIForPlayer);
                    break;
                case 3:
                    pickup(_entityFocusing);
                    break;
            }
        }

        processRequest(entityPlayer, requestType);
        return false;
    }
}

