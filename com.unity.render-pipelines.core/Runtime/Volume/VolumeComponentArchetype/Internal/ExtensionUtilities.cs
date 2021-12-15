using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    static class VolumeComponentTypeSetDefaultStateExtension
    {
        public static bool GetOrAddDefaultState(
            [DisallowNull] this VolumeComponentArchetype archetype,
            [NotNullWhen(true)] out VolumeComponentArchetypeDefaultState extension)
            => archetype.GetOrAddExtension<VolumeComponentArchetypeDefaultState, VolumeComponentArchetypeDefaultState.Factory>(out extension);

        public static bool GetDefaultState(
            [DisallowNull] this VolumeComponentArchetype archetype,
            [NotNullWhen(true)] out VolumeComponentArchetypeDefaultState extension)
            => archetype.GetExtension<VolumeComponentArchetypeDefaultState, VolumeComponentArchetypeDefaultState.Factory>(out extension);
    }
    static class VolumeComponentTypeSetPathAndTypeExtension
    {
        public static bool GetOrAddPathAndType(
            [DisallowNull] this VolumeComponentArchetype archetype,
            [NotNullWhen(true)] out VolumeComponentArchetypePathAndType extension)
            => archetype.GetOrAddExtension<VolumeComponentArchetypePathAndType, VolumeComponentArchetypePathAndType.Factory>(out extension);

        public static bool GetPathAndType(
            [DisallowNull] this VolumeComponentArchetype archetype,
            [NotNullWhen(true)] out VolumeComponentArchetypePathAndType extension)
            => archetype.GetExtension<VolumeComponentArchetypePathAndType, VolumeComponentArchetypePathAndType.Factory>(out extension);
    }
    static class VolumeComponentTypeSetTreeProviderExtension
    {
        public static bool GetOrAddTreeProvider(
            [DisallowNull] this VolumeComponentArchetype archetype,
            [NotNullWhen(true)] out VolumeComponentArchetypeTreeProvider extension)
            => archetype.GetOrAddExtension<VolumeComponentArchetypeTreeProvider, VolumeComponentArchetypeTreeProvider.Factory>(out extension);

        public static bool GetTreeProvider(
            [DisallowNull] this VolumeComponentArchetype archetype,
            [NotNullWhen(true)] out VolumeComponentArchetypeTreeProvider extension)
            => archetype.GetExtension<VolumeComponentArchetypeTreeProvider, VolumeComponentArchetypeTreeProvider.Factory>(out extension);
    }
}
