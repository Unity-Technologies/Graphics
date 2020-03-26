using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal interface ITarget
    {
        string displayName { get; }
    }
}
