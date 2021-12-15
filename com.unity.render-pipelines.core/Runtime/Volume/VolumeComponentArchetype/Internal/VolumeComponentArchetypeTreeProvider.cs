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
            [return: NotNull]
            public VolumeComponentArchetypeTreeProvider Create([DisallowNull] VolumeComponentArchetype volumeComponentArchetype)
            {
                var root = new PathNode(null, default);

                if (volumeComponentArchetype.GetOrAddPathAndType(out var extension))
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

            static void AddNode(PathNode root, string path, VolumeComponentType type)
            {
                var current = root;
                var parts = path.Split(new[] { '/' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < parts.Length; i++)
                {
                    var part = parts[i];
                    var child = current.nodes.Find(x => x.name == part);

                    if (child == null)
                    {
                        child = new PathNode(part, i == parts.Length - 1 ? type : default);
                        current.nodes.Add(child);
                    }

                    current = child;
                }
            }
        }

        internal class PathNode : IEquatable<PathNode>
        {
            public List<PathNode> nodes { get; } = new List<PathNode>();
            public string name { get; }
            public VolumeComponentType type { get; }

            public PathNode(string name, VolumeComponentType type)
            {
                this.name = name;
                this.type = type;
            }

            public bool Equals(PathNode other)
            {
                if (ReferenceEquals(null, other)) return false;
                if (ReferenceEquals(this, other)) return true;
                return name == other.name && type.Equals(other.type);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                if (ReferenceEquals(this, obj)) return true;
                if (obj.GetType() != this.GetType()) return false;
                return Equals((PathNode) obj);
            }

            public override int GetHashCode()
            {
                return HashCode.Combine(name, type);
            }

            public static bool operator ==(PathNode l, PathNode r) => l?.Equals(r) ?? ReferenceEquals(null, r);
            public static bool operator !=(PathNode l, PathNode r) => !(l?.Equals(r) ?? ReferenceEquals(null, r));
        }

        [NotNull]
        public PathNode root { get; }

        VolumeComponentArchetypeTreeProvider([DisallowNull] PathNode rootArg)
        {
            root = rootArg;
        }
    }
}

#endif
