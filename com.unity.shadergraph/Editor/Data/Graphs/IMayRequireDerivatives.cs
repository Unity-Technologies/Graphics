using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    interface IMayRequireDerivatives
    {
        IEnumerable<int> GetDifferentiatingInputSlotIds();
    }
}
