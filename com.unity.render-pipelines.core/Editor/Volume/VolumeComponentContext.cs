using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    /// <summary>
    /// A context for volume components
    /// </summary>
    class VolumeComponentContext
    {
        // what archetype to use when displaying an Add Volume Override menu
        [NotNull]
        public VolumeComponentArchetype contextualArchetype { get; private set; } = VolumeComponentArchetype.Empty;

        HashSet<VolumeComponentArchetype> s_Includes = new HashSet<VolumeComponentArchetype>();
        HashSet<VolumeComponentArchetype> s_Excludes = new HashSet<VolumeComponentArchetype>();

        public VolumeComponentContext()
        {
            AddExcludeArchetype(VolumeComponentArchetype.FromFilter(IsVisibleVolumeComponentFilter.FromIsVisible(false)));
        }

        public void AddIncludeArchetype([DisallowNull] VolumeComponentArchetype archetype)
        {
            s_Includes.Add(archetype);
            contextualArchetype = VolumeComponentArchetype.FromIncludeExclude(s_Includes, s_Excludes);
        }

        public void RemoveIncludeArchetype([DisallowNull] VolumeComponentArchetype archetype)
        {
            s_Includes.Remove(archetype);
            contextualArchetype = VolumeComponentArchetype.FromIncludeExclude(s_Includes, s_Excludes);
        }

        public void AddExcludeArchetype([DisallowNull] VolumeComponentArchetype archetype)
        {
            s_Excludes.Add(archetype);
            contextualArchetype = VolumeComponentArchetype.FromIncludeExclude(s_Includes, s_Excludes);
        }

        public void RemoveExcludeArchetype([DisallowNull] VolumeComponentArchetype archetype)
        {
            s_Excludes.Remove(archetype);
            contextualArchetype = VolumeComponentArchetype.FromIncludeExclude(s_Includes, s_Excludes);
        }
    }
}
