#if UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEditor;

namespace UnityEngine.Rendering
{
    class VolumeComponentProviderExtension : IVolumeComponentSetExtension
    {
        struct BuiltinRenderPipeline { }

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

        PathNode m_Root;

        public PathNode root => m_Root;

        public void Initialize([DisallowNull] VolumeComponentSet volumeComponentSet)
        {
            var currentPipeline = RenderPipelineManager.currentPipeline?.GetType() ?? typeof(BuiltinRenderPipeline);

            var types = volumeComponentSet.baseComponentTypeArray;

            using (ListPool<(string, Type)>.Get(out var pathAndTypes))
            {
                // filter types based on attributes
                foreach (var type in types)
                {
                    var path = string.Empty;
                    var attrs = type.GetCustomAttributes(false);
                    var skipComponent = !IsSupportedOn.IsSupportedBy(type, currentPipeline);
                    if (skipComponent)
                        continue;

                    // Look for the attributes of this volume component and decide how is added and if it needs to be skipped
                    foreach (var attr in attrs)
                    {
                        switch (attr)
                        {
                            case VolumeComponentMenu attrMenu:
                            {
                                path = attrMenu.menu;
                                break;
                            }
                            case HideInInspector attrHide:
                            case ObsoleteAttribute attrDeprecated:
                                continue;
                        }
                    }

                    // If no attribute or in case something went wrong when grabbing it, fallback to a
                    // beautified class name
                    if (string.IsNullOrEmpty(path))
                        path = ObjectNames.NicifyVariableName(type.Name);

                    pathAndTypes.Add((path, type));
                }

                // Build the tree
                m_Root = new PathNode();
                if (pathAndTypes.Count > 0)
                {
                    foreach (var (path, t) in pathAndTypes)
                    {
                        // Prep the categories & types tree
                        AddNode(m_Root, path, t);
                    }
                }
            }
        }

        void AddNode(PathNode root, string path, Type type)
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
}

#endif
