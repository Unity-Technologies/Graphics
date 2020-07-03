using System;

namespace UnityEditor.ShaderGraph
{
    [GenerationAPI] // TODO: Public
    internal abstract class SubTarget
    {
        internal abstract Type targetType { get; }
        public string displayName { get; set; }
        public abstract void Setup(ref TargetSetupContext context);
    }

    [GenerationAPI] // TODO: Public
    internal abstract class SubTarget<T> : SubTarget where T : Target
    {
        internal override Type targetType => typeof(T);
    }
}
