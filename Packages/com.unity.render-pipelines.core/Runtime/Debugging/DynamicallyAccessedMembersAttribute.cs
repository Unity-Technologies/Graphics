// TODO https://jira.unity3d.com/browse/VM-3582 Remove this file once Engine modules are built under net10
namespace System.Diagnostics.CodeAnalysis
{
    // Polyfill: netstandard2.1 lacks DynamicallyAccessedMembersAttribute (net5+); the linker matches it by name.
    // The engine modules also carry an internal copy, and net10 has the BCL one.
    // This assembly can see both, so it keeps its own copy, which takes precedence over imported ones (CS0436).
    // Without it, a net10 compile has two imported copies and fails with CS0433.
#pragma warning disable CS0436
    [AttributeUsage(AttributeTargets.GenericParameter | AttributeTargets.Parameter, Inherited = false)]
    sealed class DynamicallyAccessedMembersAttribute : Attribute
    {
        public DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes memberTypes)
        {
        }
    }
#pragma warning restore CS0436

    // Values must match the BCL enum.
    [Flags]
    enum DynamicallyAccessedMemberTypes
    {
        None = 0,
        PublicParameterlessConstructor = 1 << 0,
        PublicConstructors = (1 << 1) | PublicParameterlessConstructor,
        NonPublicConstructors = 1 << 2,
    }
}
