using HarmonyLib;
using Platform;


public class RiseDeco : RiseMasterBlock
{
    private readonly BlockActivationCommand[] cmds =
    {        
        new BlockActivationCommand("Search", "search", false),
        new BlockActivationCommand("Take", "hand", false)
    };

    private float TakeDelay = 0;
    private float AllowPickup = 0;
    private bool LootContainer = false;

    EntityPlayer localPlayer;

    public RiseDeco()
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
        return base.GetActivationText(_world, _blockValue, _clrIdx,_blockPos, _entityFocusing);
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
        Log.Out("AllowPickup : {0}", AllowPickup);
        if (AllowPickup > 0)
        {
            if (_commandName == "Take")
            {
                TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                return true;
            }
        }

        return false;
    }

    public override void OnBlockPlaceBefore(WorldBase _world, ref BlockPlacement.Result _bpResult, EntityAlive _ea, GameRandom _rnd)
    {
        base.OnBlockPlaceBefore(_world, ref _bpResult, _ea, _rnd);

        localPlayer = _ea as EntityPlayer;
        Block block = _bpResult.blockValue.Block;
       
    }   
}

