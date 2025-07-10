using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class RiseBlockCarExplodeLoot : BlockCarExplodeLoot
{
   
    private float TakeDelay = 0;
    private float AllowPickup = 0;

    public RiseBlockCarExplodeLoot()
    {
        HasTileEntity = true;
    }

    public override void Init()
    {
        base.Init();

    }

    public override bool OnBlockActivated(string _commandName, WorldBase _world, int _cIdx, Vector3i _blockPos, BlockValue _blockValue, EntityPlayerLocal _player)
    {

        Log.Out("RiseBlockCarExplodeLoot - OnBlockActivated");
        // If there's no transform, no sense on keeping going for this class.
        //var _ebcd = _world.GetChunkFromWorldPos(_blockPos).GetBlockEntity(_blockPos);
        //if (_ebcd == null || _ebcd.transform == null)
        //    return false;

        switch (_commandName)
        {        
            case "Search":
                Log.Out("RiseBlockCarExplodeLoot - Trying to loot a loot block.");
                TileEntityLootContainer tileEntityLootContainer = _world.GetTileEntity(_cIdx, _blockPos) as TileEntityLootContainer;
                if (tileEntityLootContainer != null)
                {
                    if (!tileEntityLootContainer.bWasTouched)
                    {
                        _player.SetCVar(".lootedContainer", 1f);
                    }
                }
                base.OnBlockActivated(_world, _cIdx, _blockPos, _blockValue, _player);
                return true;
        }


        return false;
    }
}
