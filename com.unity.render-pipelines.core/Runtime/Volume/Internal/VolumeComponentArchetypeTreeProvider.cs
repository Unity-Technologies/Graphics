#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Extracts the tree structure to display for the add volume menu.
    /// </summary>
    class VolumeComponentArchetypeTreeProvider : VolumeComponentArchetypeExtension
    {
        public struct Factory : IVolumeComponentArchetypeExtensionFactory<VolumeComponentArchetypeTreeProvider>
        {
            public VolumeComponentArchetypeTreeProvider Create(VolumeComponentArchetype volumeComponentArchetype)
            {
                var root = new PathNode();

                if (volumeComponentArchetype.GetOrAddExtension<VolumeComponentArchetypePathAndType, VolumeComponentArchetypePathAndType.Factory>(out VolumeComponentArchetypePathAndType extension))
                {
                    // Build the tree
                    if (extension.volumeComponentPathAndTypes.Count > 0)
                    {
                        foreach (var (path, t) in extension.volumeComponentPathAndTypes)
                        {
                            // Prep the categories & types tree
                            AddNode(root, path, t);
                        }
                    }
                }
                return new VolumeComponentArchetypeTreeProvider(root);
            }

            static void AddNode(PathNode root, string path, Type type)
            {
                var current = root;
                var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var part in parts)
                {
                    var child = current.nodes.Find(x => x.name == part);

                    if (child == null)
                    {
                        child = new PathNode { name = part, type = type };
                        current.nodes.Add(child);
                    }

                    current = child;
                }
            }
        }

        internal class PathNode : IComparable<PathNode>
        {
            public List<PathNode> nodes = new List<PathNode>();
            public string name;
            public Type type;

            public int CompareTo(PathNode other)
            {
                return name.CompareTo(other.name);
            }
        }

        PathNode m_Root { get; }
        [NotNull]
        public PathNode root => m_Root;

        VolumeComponentArchetypeTreeProvider(PathNode rootArg)
        {
            m_Root = rootArg;
        }
    }

    static class VolumeComponentTypeSetTreeProviderExtension
    {
        public static bool GetOrAddTreeProvider(
            [DisallowNull] this VolumeComponentArchetype archetype,
            [NotNullWhen(true)] out VolumeComponentArchetypeTreeProvider extension)
            => archetype.GetOrAddExtension<VolumeComponentArchetypeTreeProvider, VolumeComponentArchetypeTreeProvider.Factory>(out extension);
    }
}

#endif
