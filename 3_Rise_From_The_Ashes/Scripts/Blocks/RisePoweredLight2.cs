using UnityEngine;
using UnityEngine.Scripting;


public class RisePoweredLight : BlockPoweredLight
{
    private bool isRuntimeSwitch;

    private readonly BlockActivationCommand[] cmds =
    {
        new BlockActivationCommand("light", "electric_switch", _enabled: true),
        new BlockActivationCommand("search", "search", _enabled: true),
        new BlockActivationCommand("take", "hand", _enabled: true)
    };

    public RisePoweredLight()
    {
        HasTileEntity = true;
    }

    public override void Init()
    {
        base.Init();
        if (Properties.Values.ContainsKey("RuntimeSwitch"))
        {
            isRuntimeSwitch = StringParsers.ParseBool(Properties.Values["RuntimeSwitch"]);
        }
    }
   

    public override bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityAlive _player)
    {
        if (!(_commandName == "light"))
        {
            if (_commandName == "take")
            {
                TakeItemWithTimer(_cIdx, _blockPos, _blockValue, _player);
                return true;
            }
        }
        else
        {
            TileEntityPoweredBlock tileEntityPoweredBlock = (TileEntityPoweredBlock)_world.GetTileEntity(_cIdx, _blockPos);
            if (!_world.IsEditor() && tileEntityPoweredBlock != null)
            {
                tileEntityPoweredBlock.IsToggled = !tileEntityPoweredBlock.IsToggled;
            }
        }

        return false;
    }

    private bool updateLightState(WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, bool _bSwitchLight = false)
    {
        ChunkCluster chunkCluster = _world.ChunkClusters[_cIdx];
        if (chunkCluster == null)
        {
            return false;
        }

        IChunk chunkFromWorldPos = chunkCluster.GetChunkFromWorldPos(_blockPos);
        if (chunkFromWorldPos == null)
        {
            return false;
        }

        BlockEntityData blockEntity = chunkFromWorldPos.GetBlockEntity(_blockPos);
        if (blockEntity == null || !blockEntity.bHasTransform)
        {
            return false;
        }

        bool flag = (_blockValue.meta & 2) != 0;
        TileEntityPoweredBlock tileEntityPoweredBlock = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityPoweredBlock;
        if (tileEntityPoweredBlock != null)
        {
            flag = flag && tileEntityPoweredBlock.IsToggled;
        }

        if (_bSwitchLight)
        {
            flag = !flag;
            _blockValue.meta = (byte)((_blockValue.meta & 0xFFFFFFFDu) | (flag ? 2u : 0u));
            _world.SetBlockRPC(_cIdx, _blockPos, _blockValue);
        }

        Transform transform = blockEntity.transform.Find("MainLight");
        if ((bool)transform)
        {
            LightLOD component = transform.GetComponent<LightLOD>();
            if ((bool)component)
            {
                component.SwitchOnOff(flag, _blockPos);
            }
        }

        transform = blockEntity.transform.Find("Point light");
        if (transform != null)
        {
            LightLOD component2 = transform.GetComponent<LightLOD>();
            if (component2 != null)
            {
                component2.SwitchOnOff(flag, _blockPos);
            }
        }

        return true;
    }   
}