using System.Collections.Generic;
using UnityEngine;

public struct SkillTags
{
    public const string backpacking = "skillBackpacking";
    public const string parkour = "skillParkour";
    public const string swimming = "skillSwimming";
    public const string barter = "skillBarter";
    public const string leadership = "skillLeadership";
    public const string butchering = "skillButchering";
    public const string foraging = "skillForaging";
    public const string lockpicking = "skillLockpicking";
    public const string stealth = "skillStealth";
    public const string leatherWorking = "skillleatherWorking";
    public const string tracking = "skillTracking";
    public const string woodcutting = "skillWoodCutting";
    public const string chemistry = "skillChemistry";
    public const string civilEngineer = "skillCivilEngineer";
    public const string electricalEngineer = "skillElectricalEngineer";
    public const string mechanicalEngineer = "skillMechanicalEngineer";
    public const string medical = "skillMedical";
    public const string softwareEngineer = "skillSoftwareEngineer";
    public const string automotive = "skillAutomotive";
    public const string blacksmithing = "skillBlacksmithing";
    public const string cooking = "skillCooking";
    public const string construction = "skillConstruction";
    public const string demolitions = "skillDemolitions";
    public const string farming = "skillFarming";
    public const string mining = "skillMining";
}

public struct StatTags
{
    public const string strength = "statStrength";
    public const string dexterity = "statDexterity";
    public const string fortitude = "statFortitude";
    public const string intelligence = "statIngelligence";
    public const string wisdom = "statWisdom";
    public const string charisma = "statCharisma";
}

public struct MeleeTags
{
    public const string blades = "meleeBlades";
    public const string clubs = "meleeClubs";
    public const string fists = "meleeFists";
    public const string improvised = "meleeImprovised";
    public const string spears = "meleeSpears";
}

public struct RangedTags
{
    public const string bows = "rangedBows";
    public const string machineguns = "rangedMachineguns";
    public const string pistols = "rangedPistols";
    public const string rifles = "rangedRifles";
    public const string shotguns = "rangedShotguns";
}


public class RiseHelp
{
    public enum Check
    {
        DamagedBlocks,
        Storage,
        Any
    }

    public static List<Vector3> GetBlocks(Vector3i startPos, string _targetTypes = "",
           bool ignoreTouch = false)
    {
        var paths = new List<Vector3>();
        var blockPosition = startPos;
        var chunkX = World.toChunkXZ(blockPosition.x);
        var chunkZ = World.toChunkXZ(blockPosition.z);
        World world = GameManager.Instance.World;

        if (string.IsNullOrEmpty(_targetTypes) || _targetTypes.ToLower().Contains("basic"))
            _targetTypes = "LandClaim, Loot, VendingMachine, Forge, Campfire, Workstation, PowerSource";
        for (var i = -1; i < 2; i++)
        {
            for (var j = -1; j < 2; j++)
            {
                var chunk = (Chunk)world.GetChunkSync(chunkX + j, chunkZ + i);
                if (chunk == null) continue;

                var tileEntities = chunk.GetTileEntities();
                foreach (var tileEntity in tileEntities.list)
                {
                    foreach (var filterTypeFull in _targetTypes.Split(','))
                    {
                        // Check if the filter type includes a :, which may indicate we want a precise block.
                        var filterType = filterTypeFull;
                        var blockNames = "";
                        if (filterTypeFull.Contains(":"))
                        {
                            filterType = filterTypeFull.Split(':')[0];
                            blockNames = filterTypeFull.Split(':')[1];
                        }

                        // Parse the filter type and verify if the tile entity is in the filter.
                        var targetType = EnumUtils.Parse<TileEntityType>(filterType, true);
                        if (tileEntity.GetTileEntityType() != targetType) continue;

                        switch (tileEntity.GetTileEntityType())
                        {
                            case TileEntityType.None:
                                continue;
                            // If the loot containers were already touched, don't path to them.
                            case TileEntityType.Loot:
                                if (((TileEntityLootContainer)tileEntity).bTouched && ignoreTouch == false)
                                    continue;
                                break;
                            case TileEntityType.SecureLoot:
                                if (((TileEntitySecureLootContainer)tileEntity).bTouched && ignoreTouch == false)
                                    continue;
                                break;
                        }

                        // Search for the tile entity's block name to see if its filtered.
                        if (!string.IsNullOrEmpty(blockNames))
                        {
                            if (!blockNames.Contains(tileEntity.blockValue.Block.GetBlockName()))
                                continue;
                        }

                        var position = tileEntity.ToWorldPos().ToVector3();
                        paths.Add(position);
                    }
                }
            }
        }

        return paths;
    }

    public List<Vector3i> GetBlocks(Chunk chunk, int searchSizeXZ,int searchSizeY, Vector3i startPos, Check Check)
    {
        List<Vector3i> positions = new List<Vector3i>();
        Vector3i vector3i = default(Vector3i);
        Vector3i newVector3i = default(Vector3i);
        for (int i = -8; i < 8; i++)
        {
            for (int j = -8; j < 8; j++)
            {
                for (int k = -searchSizeY; k < searchSizeY; k++)
                {
                    vector3i.x = i;
                    vector3i.y = k;
                    vector3i.z = j;
                    newVector3i = startPos + vector3i;
                    BlockValue blockValue = GameManager.Instance.World.GetBlock(newVector3i);

                    switch (Check)
                    {
                        case Check.DamagedBlocks:
                            if (!blockValue.Block.shape.IsTerrain() && !blockValue.isair)
                            {
                                if (blockValue.damage > 0)
                                {
                                    positions.Add(World.worldToBlockPos(newVector3i));
                                }
                            }
                            break;                        
                        case Check.Storage:
                            if (blockValue.Block is BlockSecureLoot)
                            {  
                                AddBlockToPositions(positions, newVector3i, blockValue);                               
                            }
                            break;
                        case Check.Any:
                            positions.Add(World.worldToBlockPos(newVector3i));
                            break;
                    }                    
                }
            }
        }

        return positions;
    }    

    private void AddBlockToPositions(List<Vector3i> positions, Vector3i blockPos, BlockValue blockValue)
    {
#if DEBUG
        Log.Out("Block name : " + blockValue.Block.GetBlockName() + " damage = " + blockValue.damage);
        Log.Out("Block Pos : " + World.worldToBlockPos(blockPos));
#endif
#if DEBUG
        Log.Out("World POS : " + blockPos.ToString());
#endif
        positions.Add(World.worldToBlockPos(blockPos));
    }   
}



