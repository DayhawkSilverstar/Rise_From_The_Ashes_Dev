using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RiseFromTheAshes
{
    public class RiseCampfire : RiseBlockWorkstation
    {
        public RiseCampfire() 
        {
        }   

        public override string GetActivationText(WorldBase _world, BlockValue _blockValue, int _clrIdx, Vector3i _blockPos, EntityAlive _entityFocusing)
        {
            return Localization.Get("useCampfire");
        }

    }
}
