using HarmonyLib;
using Platform;

public class RiseLight : BlockLight
{
    public new BlockActivationCommand[] cmds = new BlockActivationCommand[2]
    {
        new BlockActivationCommand("light", "electric_switch", _enabled: true),
        new BlockActivationCommand("take", "hand", _enabled: true),
    };

    private float TakeDelay = 0;
    private float AllowPickup = 0;
    private bool LootContainer = false;

    EntityPlayer localPlayer;

    public RiseLight()
    {
        HasTileEntity = true;
    }

    public override void Init()
    {
        base.Init();

        TakeDelay = 2f;
        Properties.ParseFloat("AllowPickup", ref AllowPickup);
        Properties.ParseFloat("TakeDelay", ref TakeDelay);
        Properties.ParseBool("LootContainer", ref LootContainer);
        IsNotifyOnLoadUnload = true;
    }

    // Only fires if IsNotifyOnLoadUnload is set to true
    public override void OnBlockLoaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockLoaded(_world, _clrIdx, _blockPos, _blockValue);
    }

    // Only fires if IsNotifyOnLoadUnload is set to true
    public override void OnBlockUnloaded(WorldBase _world, int _clrIdx, Vector3i _blockPos, BlockValue _blockValue)
    {
        base.OnBlockUnloaded(_world, _clrIdx, _blockPos, _blockValue);
    }

    // Display custom messages for turning on and off the music box, based on the block's name.
    public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos,
        EntityAlive _entityFocusing)
    {
        #region GetActivationText
        return base.GetActivationText(_world, _blockValue, _clrIdx, _blockPos, _entityFocusing);
        #endregion
    }

    public override BlockActivationCommand[] GetBlockActivationCommands(WorldBase _world, BlockValue _blockValue,
        int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
    {

        // TODO : Check to see if the person is in someone elses claim and not a friend. Don't allow them to pick it up then.
        if (AllowPickup > 0)
        {
            cmds[0].enabled = true;
            cmds[1].enabled = TakeDelay > 0f;
        }
        else
        {
            _blockValue.Block.CanPickup = false;
        }

        return cmds;
    }

    public override bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {
        Log.Out("Command : {0}", _commandName);

        switch (_commandName)
        {
            case "take":
                {
                    Log.Out("RiseLight - Trying to pick up a light block.");
                    TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                    return true;
                }

            case "light":
                if (_world.IsEditor() && updateLightState(_world, _cIdx, _blockPos, _blockValue, _bSwitchLight: true, _enableState: false))
                {
                    return true;
                }

                break;
            case "edit":
                {
                    TileEntityLight te = (TileEntityLight)_world.GetTileEntity(_cIdx, _blockPos);
                    if (_world.IsEditor())
                    {
                        XUiC_LightEditor.Open(_player.PlayerUI, te, _blockPos, _world as World, _cIdx, this);
                        return true;
                    }

                    break;
                }
            case "trigger":
                XUiC_TriggerProperties.Show(_player.PlayerUI.xui, _cIdx, _blockPos, _showTriggers: false, _showTriggeredBy: true);
                break;
        }

        return false;
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, GameRandom _rnd)
    {
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);

        localPlayer = _ea as EntityPlayer;
        Block block = _bpResult.blockValue.Block;

    }

    // We want to give the user the ability to pick up the blocks too, but the loot containers don't support that directly.
    public void TakeItemWithTimer(int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        #region TakeItemWithTimer
        Log.Out("Trying to pick up a block.");
        if (_blockValue.damage > 0)
        {
            GameManager.ShowTooltip(_player as EntityPlayerLocal, Localization.Get("ttRepairBeforePickup"), string.Empty, "ui_denied");
            return;
        }

        LocalPlayerUI playerUI = (_player as EntityPlayerLocal).PlayerUI;
        playerUI.windowManager.Open("timer", _bModal: true);
        XUiC_Timer childByType = playerUI.xui.GetChildByType<XUiC_Timer>();
        TimerEventData timerEventData = new TimerEventData();
        timerEventData.Data = new object[4] { _cIdx, _blockValue, _blockPos, _player };
        timerEventData.Event += EventData_Event;
        childByType.SetTimer(TakeDelay, timerEventData);

        #endregion
    }

    // Handles what happens to the contents of the box when you pick up the block.
    private void EventData_Event(TimerEventData timerData)
    {
        #region EventData_Event

        var world = GameManager.Instance.World;

        var array = (object[])timerData.Data;
        var clrIdx = (int)array[0];
        var blockValue = (BlockValue)array[1];
        var vector3i = (Vector3i)array[2];
        var block = world.GetBlock(vector3i);
        var entityPlayerLocal = array[3] as EntityPlayerLocal;

        // Pick up the item and put it inyor your inventory.
        var uiforPlayer = LocalPlayerUI.GetUIForPlayer(entityPlayerLocal);
        var itemStack = new ItemStack(block.ToItemValue(), 1);
        if (!uiforPlayer.xui.PlayerInventory.AddItem(itemStack, true))
            uiforPlayer.xui.PlayerInventory.DropItem(itemStack);
        world.SetBlockRPC(clrIdx, vector3i, BlockValue.Air);

        #endregion
    }
}