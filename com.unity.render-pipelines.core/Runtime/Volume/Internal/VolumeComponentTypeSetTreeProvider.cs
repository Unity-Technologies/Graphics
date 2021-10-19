#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Extracts the tree structure to display for the add volume menu.
    /// </summary>
    class VolumeComponentTypeSetTreeProvider : VolumeComponentTypeSetExtension
    {
        public struct Factory : IVolumeComponentTypeSetExtensionFactory<VolumeComponentTypeSetTreeProvider>
        {
            public VolumeComponentTypeSetTreeProvider Create(VolumeComponentTypeSet volumeComponentTypeSet)
            {
                var root = new PathNode();

                if (volumeComponentTypeSet.GetOrAddExtension<VolumeComponentTypeSetPathAndType, VolumeComponentTypeSetPathAndType.Factory>(out VolumeComponentTypeSetPathAndType extension))
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
                return new VolumeComponentTypeSetTreeProvider(root);
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

        VolumeComponentTypeSetTreeProvider(PathNode rootArg)
        {
            m_Root = rootArg;
        }
    }

    static class VolumeComponentTypeSetTreeProviderExtension
    {
        public static bool GetOrAddTreeProvider(
            [DisallowNull] this VolumeComponentTypeSet typeSet,
            [NotNullWhen(true)] out VolumeComponentTypeSetTreeProvider extension)
            => typeSet.GetOrAddExtension<VolumeComponentTypeSetTreeProvider, VolumeComponentTypeSetTreeProvider.Factory>(out extension);
    }
}

#endif
