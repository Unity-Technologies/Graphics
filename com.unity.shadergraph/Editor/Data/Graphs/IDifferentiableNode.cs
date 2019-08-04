using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderGraph
{
    struct Derivative
    {
        public IReadOnlyList<int> FuncVariableInputSlotIds;

        // dF(I0, I1, .., IN)/dx = dF/dI0*dI0/dx + dF/dI1*dI1/dx + ... + dF/dIn*dIN/dx
        public Func<GenerationMode, string> Function;
    }

    // Declares that a node is differentiable. Used to obtain the ddx/ddy for a texture sampling node to use under conditional scope.
    interface IDifferentiable
    {
        Derivative GetDerivative(int outputSlotId);
    }
}
