using System;
using System.Linq;
using System.Reflection;
using UnityEngine.Experimental.UIElements;
using UnityEditor.ShaderGraph;

namespace UnityEditor.ShaderGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class PropertyControlAttribute : Attribute, IControlAttribute
    {
        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            return new PropertyControlView(node);
        }
    }

    public class PropertyControlView : VisualElement
    {
        PropertyNode m_Node;

        public PropertyControlView(AbstractMaterialNode node)
        {
            m_Node = (PropertyNode)node;

            Add(new IMGUIContainer(OnGUIHandler));
        }

        void OnGUIHandler()
        {
            var graph = m_Node.owner as AbstractMaterialGraph;
            var currentGUID = m_Node.propertyGuid;
            var properties = graph.properties.ToList();
            var propertiesGUID = properties.Select(x => x.guid).ToList();
            var currentSelectedIndex = propertiesGUID.IndexOf(currentGUID);
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                var value = EditorGUILayout.Popup(currentSelectedIndex, properties.Select(x => x.displayName).ToArray());
                if (changeCheckScope.changed)
                {
                    m_Node.owner.owner.RegisterCompleteObjectUndo("Change " + m_Node.name);
                    m_Node.propertyGuid = propertiesGUID[value];
                }
            }
        }
    }
}
