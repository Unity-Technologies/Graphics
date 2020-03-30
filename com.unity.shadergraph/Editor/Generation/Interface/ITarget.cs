using System;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI][Serializable]
    internal abstract class ITarget
    {
        public abstract string displayName { get; }
    }
}
