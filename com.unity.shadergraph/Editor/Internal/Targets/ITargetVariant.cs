using System.Collections.Generic;

namespace UnityEditor.ShaderGraph.Internal
{
    interface ITargetVariant<T> : ITarget where T : ITarget
    {
        
    }
}
