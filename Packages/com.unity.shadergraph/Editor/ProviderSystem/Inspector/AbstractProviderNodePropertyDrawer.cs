using System;
using System.Reflection;
using Unity.GraphAuthoring.Editor.ProviderSystem;
using UnityEditor.ShaderGraph.ProviderSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    abstract class AbstractProviderNodePropertyDrawer<T> : IPropertyDrawer, IGetNodePropertyDrawerPropertyData where T : IDefinition
    {
        Action m_SetNodesAsDirtyCallback;
        Action m_UpdateNodeViewsCallback;

        public Action inspectorUpdateDelegate { get; set; }

        void IGetNodePropertyDrawerPropertyData.GetPropertyData(Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            m_SetNodesAsDirtyCallback = setNodesAsDirtyCallback;
            m_UpdateNodeViewsCallback = updateNodeViewsCallback;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            var node = (AbstractProviderNode<T>)actualObject;
            var sheet = new PropertySheet(PropertyDrawerUtils.CreateLabel($"{node.name} Node", 0, FontStyle.Bold));
            PropertyDrawerUtils.AddDefaultNodeProperties(sheet, node, m_SetNodesAsDirtyCallback, m_UpdateNodeViewsCallback);
            if (!node.isOrphaned)
            {
                AddDescription(ProviderInspectorUtils.AddSection(sheet, "Description"), node);

                var settings = new VisualElement();
                AddSettings(settings, node, m_SetNodesAsDirtyCallback, m_UpdateNodeViewsCallback);
                if (settings.childCount > 0)
                    ProviderInspectorUtils.AddSection(sheet, "Settings").Add(settings);
            }
            ProviderInspectorUtils.AddMessages(sheet, node.messages);
            return sheet;
        }

        void IPropertyDrawer.DisposePropertyDrawer()
        {
        }

        void AddDescription(VisualElement section, AbstractProviderNode<T> node)
        {
            var provider = node.Provider;
            ProviderInspectorUtils.AddValue(section, "Key", provider.Info.ProviderKey);
            AddDescriptionValues(section, node);
            if (provider.Definition is IShaderFunction function)
                ProviderInspectorUtils.AddBlock(section, "Signature", ShaderObjectUtils.QualifySignature(function));
            AddDescriptionBlocks(section, node);
        }

        private protected virtual void AddDescriptionValues(VisualElement section, AbstractProviderNode<T> node)
        {
        }

        private protected virtual void AddDescriptionBlocks(VisualElement section, AbstractProviderNode<T> node)
        {
        }

        private protected virtual void AddSettings(VisualElement section, AbstractProviderNode<T> node, Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
        }
    }
}
