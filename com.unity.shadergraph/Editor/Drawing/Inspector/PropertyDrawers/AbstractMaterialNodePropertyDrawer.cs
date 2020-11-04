using System;
using System.Reflection;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Graphing.Util;
using UnityEngine;

namespace  UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(AbstractMaterialNode))]
    public class AbstractMaterialNodePropertyDrawer : IPropertyDrawer
    {
        public Action inspectorUpdateDelegate { get; set; }

        Action m_setNodesAsDirtyCallback;
        Action m_updateNodeViewsCallback;

        public void GetPropertyData(Action setNodesAsDirtyCallback, Action updateNodeViewsCallback)
        {
            m_setNodesAsDirtyCallback = setNodesAsDirtyCallback;
            m_updateNodeViewsCallback = updateNodeViewsCallback;
        }

        VisualElement CreateGUI(AbstractMaterialNode node, InspectableAttribute attribute, out VisualElement propertyVisualElement)
        {
            VisualElement nodeSettings = new VisualElement();
            var nameLabel = PropertyDrawerUtils.CreateLabel($"{node.name} Node", 0, FontStyle.Bold);
            nodeSettings.Add(nameLabel);
            if (node.sgVersion < node.latestVersion)
            {
                var help = HelpBoxRow.TryGetDeprecatedHelpBoxRow($"{node.name} Node", () =>
                {
                    m_setNodesAsDirtyCallback?.Invoke();
                    node.owner.owner.RegisterCompleteObjectUndo($"Update {node.name} Node");
                    node.ChangeVersion(node.latestVersion);
                    inspectorUpdateDelegate?.Invoke();
                    m_updateNodeViewsCallback?.Invoke();
                    node.Dirty(ModificationScope.Graph);
                });
            
                if (help != null)
                {
                    nodeSettings.Insert(0, help);
                }
            }
            EnumField precisionField = null;
            if(node.canSetPrecision)
            {
                precisionField = new EnumField(node.precision);
                var propertyRow = new PropertyRow(new Label("Precision"));
                propertyRow.Add(precisionField, (field) =>
                {
                    field.RegisterValueChangedCallback(evt =>
                    {
                        if (evt.newValue.Equals(node.precision))
                            return;

                        m_setNodesAsDirtyCallback();
                        node.owner.owner.RegisterCompleteObjectUndo("Change precision");
                        node.precision = (Precision)evt.newValue;
                        node.owner.ValidateGraph();
                        m_updateNodeViewsCallback();
                        node.Dirty(ModificationScope.Graph);
                    });
                });
                if (node is Serialization.MultiJsonInternal.UnknownNodeType)
                    precisionField.SetEnabled(false);
                nodeSettings.Add(propertyRow);
            }
            propertyVisualElement = precisionField;
            return nodeSettings;
        }
        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI(
                (AbstractMaterialNode) actualObject,
                attribute,
                out var propertyVisualElement);
        }
    }
}
