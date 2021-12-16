using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
        internal class VolumeComponentElement : Element
        {
            public VolumeComponentType type;

            public VolumeComponentElement(int level, string label, VolumeComponentType type)
            {
                this.level = level;
                this.type = type;
                // TODO: Add support for custom icons
                content = new GUIContent(label);
            }
        }

        [ExcludeFromCodeCoverage] // trivial
        public Vector2 position { get; set; }

        VolumeProfile m_Target;
        VolumeComponentListEditor m_TargetEditor;
        readonly VolumeComponentArchetype m_ArchetypeOverride;

        /// <summary>
        ///
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetEditor"></param>
        /// <param name="archetypeOverride">
        /// Use this value to override which archetype to use to create the tree.
        /// By default it will use the <see cref="VolumeComponentUserExperience.displayedArchetype"/>
        /// </param>
        public VolumeComponentTreeProvider(
            [DisallowNull] VolumeProfile target,
            [DisallowNull] VolumeComponentListEditor targetEditor,
            [AllowNull] VolumeComponentArchetype archetypeOverride)
        {
            m_Target = target;
            m_TargetEditor = targetEditor;
            m_ArchetypeOverride = archetypeOverride;
        }

        public void CreateComponentTree([DisallowNull] List<Element> tree)
        {
            tree.Add(new GroupElement(0, "Volume Overrides"));

            var archetype = m_ArchetypeOverride ?? VolumeComponentUserExperience.displayedArchetype;
            if (!archetype.GetOrAddTreeProvider(out var extension))
                return;

            // Recursively add all elements to the tree
            Traverse(extension.root, 1, tree);
        }

        public bool GoToChild([DisallowNull] Element element, bool addIfComponent)
        {
            if (element is VolumeComponentElement volumeComponentElement)
            {
                m_TargetEditor.AddComponent(volumeComponentElement.type);
                return true;
            }

            return false;
        }

        void Traverse(
            [DisallowNull] VolumeComponentArchetypeTreeProvider.PathNode node,
            int depth,
            [DisallowNull] List<Element> tree
        )
        {
            node.nodes.Sort();

            foreach (var n in node.nodes)
            {
                if (n.nodes.Count > 0) // Group
                {
                    tree.Add(new GroupElement(depth, n.name));
                    Traverse(n, depth + 1, tree);
                }
                else if (!m_Target.Has(n.type.AsType())) // Element
                {
                    tree.Add(new VolumeComponentElement(depth, n.name, n.type));
                }
            }
        }
    }
}
