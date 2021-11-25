using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    using IProvider = FilterWindow.IProvider;
    using Element = FilterWindow.Element;
    using GroupElement = FilterWindow.GroupElement;

    class VolumeComponentProvider : IProvider
    {
        class VolumeComponentElement : Element
        {
            public Type type;

            public VolumeComponentElement(int level, string label, Type type)
            {
                this.level = level;
                this.type = type;
                // TODO: Add support for custom icons
                content = new GUIContent(label);
            }
        }

        class PathNode : IComparable<PathNode>
        {
            public List<PathNode> nodes = new List<PathNode>();
            public string name;
            public Type type;

            public int CompareTo(PathNode other)
            {
                return name.CompareTo(other.name);
            }
        }

        public Vector2 position { get; set; }

        VolumeProfile m_Target;
        VolumeComponentListEditor m_TargetEditor;

        public VolumeComponentProvider(VolumeProfile target, VolumeComponentListEditor targetEditor)
        {
            m_Target = target;
            m_TargetEditor = targetEditor;
        }

        public void CreateComponentTree(List<Element> tree)
        {
            var currentPipeline = RenderPipelineManager.currentPipeline;
            if (currentPipeline == null)
            {
                tree.Add(new GroupElement(0, "No SRP in use"));
                return;
            }

            tree.Add(new GroupElement(0, "Volume Overrides"));

            var volumeComponentTypesFiltered =
                VolumeManager.GetSupportedVolumeComponents(currentPipeline.GetType());

            if (volumeComponentTypesFiltered.Any())
            {
                var rootNode = new PathNode();

                foreach (var (path, t) in volumeComponentTypesFiltered)
                {
                    // Skip components that have already been added to the volume
                    if (m_Target.Has(t))
                        continue;

                    // Prep the categories & types tree
                    AddNode(rootNode, path, t);
                }

                // Recursively add all elements to the tree
                Traverse(rootNode, 1, tree);
            }
        }

        public bool GoToChild(Element element, bool addIfComponent)
        {
            if (element is VolumeComponentElement volumeComponentElement)
            {
                m_TargetEditor.AddComponent(volumeComponentElement.type);
                return true;
            }

            return false;
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

        void Traverse(PathNode node, int depth, List<Element> tree)
        {
            node.nodes.Sort();

            foreach (var n in node.nodes)
            {
                if (n.nodes.Count > 0) // Group
                {
                    tree.Add(new GroupElement(depth, n.name));
                    Traverse(n, depth + 1, tree);
                }
                else // Element
                {
                    tree.Add(new VolumeComponentElement(depth, n.name, n.type));
                }
            }
        }
    }
}
