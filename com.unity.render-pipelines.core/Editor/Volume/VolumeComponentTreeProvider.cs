using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.Rendering
{
    using IProvider = FilterWindow.IProvider;
    using Element = FilterWindow.Element;
    using GroupElement = FilterWindow.GroupElement;

    /// <summary>
    /// Build a tree for the add volume override menu.
    ///
    /// Don't show a volume if it is already in the stack
    /// </summary>
    class VolumeComponentTreeProvider : IProvider
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

        public Vector2 position { get; set; }

        VolumeProfile m_Target;
        VolumeComponentListEditor m_TargetEditor;

        public VolumeComponentTreeProvider(VolumeProfile target, VolumeComponentListEditor targetEditor)
        {
            m_Target = target;
            m_TargetEditor = targetEditor;
        }

        public void CreateComponentTree(List<Element> tree)
        {
            tree.Add(new GroupElement(0, "Volume Overrides"));

            if (!VolumeManager.instance.baseComponentArchetype
                .GetOrAddTreeProvider(out var extension))
                return;

            // Recursively add all elements to the tree
            Traverse(extension.root, 1, tree);
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

        void Traverse(VolumeComponentArchetypeTreeProvider.PathNode node, int depth, List<Element> tree)
        {
            node.nodes.Sort();

            foreach (var n in node.nodes)
            {
                if (n.nodes.Count > 0) // Group
                {
                    tree.Add(new GroupElement(depth, n.name));
                    Traverse(n, depth + 1, tree);
                }
                else if (!m_Target.Has(n.type)) // Element
                {
                    tree.Add(new VolumeComponentElement(depth, n.name, n.type));
                }
            }
        }
    }
}
