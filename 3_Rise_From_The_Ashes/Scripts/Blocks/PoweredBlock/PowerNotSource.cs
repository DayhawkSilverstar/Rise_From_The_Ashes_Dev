using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiseFromTheAshes.Scripts.Blocks
{
    using System.Collections.Generic;
    using UnityEngine;

    // Emits power ONLY when its (single) parent is NOT powered.
    public class PowerNotSource : PowerSource
    {
        // How much power the NOT gate "sources" downstream when input is off.
        public ushort OutputPower = 10;

        // Allow exactly one parent (single input)
        public override int InputCount => 1;

        // Serialize as an existing source type to keep saves compatible.
        // (PowerManager.Write writes the enum; on load the engine will create a PowerGenerator,
        //  but we'll upgrade those to PowerNotSource in our bootstrap below.)
        public override PowerItemTypes PowerItemType => PowerItemTypes.Generator;

        public override void Update()
        {
            // If there's a parent and it's powered, NOT should be OFF (no output)
            bool inputOn = Parent != null && Parent.IsPowered;

            if (inputOn)
            {
                if (isPowered) { HandleDisconnect(); } // drop our children
                isPowered = false;
                return;
            }

            // Input is OFF -> we act as a small source
            isPowered = true;
            ushort budget = OutputPower;

            // Push power to children like any source
            for (int i = 0; i < Children.Count && budget > 0; i++)
            {
                Children[i].HandlePowerReceived(ref budget);
            }
        }

        // Optional: block chaining beyond our own children when we are "off"
        public override bool PowerChildren() => isPowered; // Only traverse when we actively source
    }
}
