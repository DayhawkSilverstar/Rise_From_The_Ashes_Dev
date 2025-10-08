using RiseFromTheAshes.Scripts.Blocks;
using System.Collections.Generic;
using UnityEngine;

// Keep this class name unique in your mod namespace.
public class TileEntityLogicGateNot : TileEntityPoweredBlock
{
    public TileEntityLogicGateNot(Chunk _chunk) : base(_chunk) { }

    // When the block is placed, ensure we create our NOT power item and register it.
    public override void CreateWireDataFromPowerItem()
    {
        base.CreateWireDataFromPowerItem();
        // If PowerManager already has a node at our position, try to upgrade it
        var pm = PowerManager.Instance;
        var pos = this.ToWorldPos();
        var node = pm.GetPowerItemByWorldPos(pos);

        if (node is not PowerNotSource)
        {
            // Build & insert our custom node
            var notNode = new PowerNotSource
            {
                BlockID = (ushort)GameManager.Instance.World.GetBlockSDX(new Vector3i(pos)).type,
                Position = pos
            };

            // If engine already added a vanilla node at this pos, replace it
            if (node != null)
            {
                var parent = node.Parent;
                var children = new List<PowerItem>(node.Children);

                pm.RemovePowerNode(node);
                pm.AddPowerNode(notNode, parent);
                foreach (var c in children) pm.SetParent(c, notNode);
            }
            else
            {
                pm.AddPowerNode(notNode, null);
            }

            // Link TE
            notNode.AddTileEntity(this);
        }
    }
}
