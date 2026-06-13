using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.UIElements;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    [AttributeUsage(AttributeTargets.Property)]
    class GroupVariantControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (node is not ProviderNode pn)
                return null;
            return new GroupVariantControlView(pn);
        }
    }

    class GroupVariantControlView : VisualElement, AbstractMaterialNodeModificationListener
    {
        ProviderNode m_Node;
        DropdownField m_Dropdown;

        internal GroupVariantControlView(ProviderNode node)
        {
            m_Node = node;
            Rebuild();
        }

        public void OnNodeModified(ModificationScope scope)
        {
            if (scope == ModificationScope.Graph)
                Rebuild();
        }

        void Rebuild()
        {
            Clear();
            m_Dropdown = null;

            var candidates = m_Node.Header?.groupCandidates;
            if (candidates == null || candidates.Length == 0)
                return;

            var choices = new List<string>(candidates.Length);
            foreach (var c in candidates)
                choices.Add(c.label);

            m_Dropdown = new DropdownField(m_Node.Header.groupLabel ?? "Variant", choices, CurrentIndex());
            m_Dropdown.RegisterValueChangedCallback(OnSelectionChanged);
            Add(m_Dropdown);
        }

        int CurrentIndex()
        {
            var candidates = m_Node.Header?.groupCandidates;
            if (candidates == null) return 0;
            var key = m_Node.Provider?.ProviderKey;
            for (int i = 0; i < candidates.Length; i++)
                if (candidates[i].provider.ProviderKey == key)
                    return i;
            return 0;
        }

        void OnSelectionChanged(ChangeEvent<string> evt)
        {
            var candidates = m_Node.Header?.groupCandidates;
            if (candidates == null) return;
            int newIdx = m_Dropdown.index;
            if (candidates[newIdx].provider.ProviderKey == m_Node.Provider?.ProviderKey)
                return;

            m_Node.owner.owner.RegisterCompleteObjectUndo($"Change {m_Node.name} Variant");
            m_Node.InitializeFromProvider(candidates[newIdx].provider);
            m_Node.ValidateNode();
            m_Node.Dirty(ModificationScope.Graph);
        }
    }
}
