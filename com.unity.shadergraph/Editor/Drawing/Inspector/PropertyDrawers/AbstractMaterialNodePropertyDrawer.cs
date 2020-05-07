using System;
using System.Reflection;
using Data.Interfaces;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using UnityEditor.Graphing.Util;
using UnityEngine;

namespace Drawing.Inspector.PropertyDrawers
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
            var defaultRow = new PropertyRow(PropertyDrawerUtils.CreateLabel(attribute.labelName));
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

                defaultRow.Add(precisionField);
                defaultRow.styleSheets.Add(Resources.Load<StyleSheet>("Styles/PropertyRow"));
            }
            propertyVisualElement = precisionField;
            return defaultRow;
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
