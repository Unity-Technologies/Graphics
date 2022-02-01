using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// A context for volume components
    /// </summary>
    // Do not expose, context is a workaround to a bad UX, VolumeProfile should only accept a specific archetype
    class VolumeComponentContext
    {
        // what archetype to use when displaying an Add Volume Override menu
        [NotNull]
        [ExcludeFromCodeCoverage] // trivial
        public VolumeComponentArchetype contextualArchetype { get; private set; } = VolumeComponentArchetype.Empty;

        HashSet<VolumeComponentArchetype> s_Includes = new HashSet<VolumeComponentArchetype>();
        HashSet<VolumeComponentArchetype> s_Excludes = new HashSet<VolumeComponentArchetype>();

        [ExcludeFromCodeCoverage] // trivial
        public VolumeComponentContext()
        {
            AddExcludeArchetype(VolumeComponentArchetype.FromFilterCached(IsVisibleVolumeComponentFilter.FromIsVisible(false)));
        }

        [ExcludeFromCodeCoverage] // trivial wraps of `VolumeComponentArchetype.FromIncludeExclude`
        public void AddIncludeArchetype([DisallowNull] VolumeComponentArchetype archetype)
        {
            s_Includes.Add(archetype);
            contextualArchetype = VolumeComponentArchetype.FromIncludeExclude(s_Includes, s_Excludes);
        }

        [ExcludeFromCodeCoverage] // trivial wraps of `VolumeComponentArchetype.FromIncludeExclude`
        public void RemoveIncludeArchetype([DisallowNull] VolumeComponentArchetype archetype)
        {
            s_Includes.Remove(archetype);
            contextualArchetype = VolumeComponentArchetype.FromIncludeExclude(s_Includes, s_Excludes);
        }

        [ExcludeFromCodeCoverage] // trivial wraps of `VolumeComponentArchetype.FromIncludeExclude`
        public void AddExcludeArchetype([DisallowNull] VolumeComponentArchetype archetype)
        {
            s_Excludes.Add(archetype);
            contextualArchetype = VolumeComponentArchetype.FromIncludeExclude(s_Includes, s_Excludes);
        }

        [ExcludeFromCodeCoverage] // trivial wraps of `VolumeComponentArchetype.FromIncludeExclude`
        public void RemoveExcludeArchetype([DisallowNull] VolumeComponentArchetype archetype)
        {
            s_Excludes.Remove(archetype);
            contextualArchetype = VolumeComponentArchetype.FromIncludeExclude(s_Includes, s_Excludes);
        }
    }
}
