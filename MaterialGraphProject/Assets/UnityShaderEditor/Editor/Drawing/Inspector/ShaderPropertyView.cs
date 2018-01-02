using System;
using UnityEditor.Experimental.UIElements;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Graphing;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing.Inspector
{
    public class ShaderPropertyView : VisualElement
    {
        Action m_ValueAction;
        public AbstractMaterialGraph graph { get; private set; }
        public IShaderProperty property { get; private set; }

        public ShaderPropertyView(AbstractMaterialGraph graph, IShaderProperty property)
        {
            this.graph = graph;
            this.property = property;


            var displayNameField = new TextField { name = "displayName", value = property.displayName };
            displayNameField.OnValueChanged(OnDisplayNameChanged);
            Add(displayNameField);

            m_ValueAction = null;
            if (property is FloatShaderProperty)
                m_ValueAction = FloatField;
            else if (property is Vector2ShaderProperty)
                m_ValueAction = Vector2Field;
            else if (property is Vector3ShaderProperty)
                m_ValueAction = Vector3Field;
            else if (property is Vector4ShaderProperty)
                m_ValueAction = Vector4Field;

            if (m_ValueAction != null)
            {
                Add(new IMGUIContainer(ValueField) { name = "value" });
            }
            else if (property is ColorShaderProperty)
            {
                var fProp = (ColorShaderProperty)property;
                var colorField = new ColorField { name = "value", value = fProp.value };
                colorField.OnValueChanged(OnColorChanged);
                Add(colorField);
            }
            else if (property is TextureShaderProperty)
            {
                var fProp = (TextureShaderProperty)property;
                var objectField = new ObjectField { name = "value", objectType = typeof(Texture), value = fProp.value.texture };
                objectField.OnValueChanged(OnTextureChanged);
                Add(objectField);
            }
            else if (property is CubemapShaderProperty)
            {
                var fProp = (CubemapShaderProperty)property;
                var objectField = new ObjectField { name = "value", objectType = typeof(Cubemap), value = fProp.value.cubemap };
                objectField.OnValueChanged(OnCubemapChanged);
                Add(objectField);
            }

            Add(new Button(OnClickRemove) { name = "remove", text = "Remove" });
        }

        void OnColorChanged(ChangeEvent<Color> evt)
        {
            var fProp = (ColorShaderProperty)property;
            if (evt.newValue != fProp.value)
            {
                fProp.value = evt.newValue;
                NotifyNodes();
            }
        }

        void OnTextureChanged(ChangeEvent<Object> evt)
        {
            var fProp = (TextureShaderProperty)property;
            var newValue = (Texture)evt.newValue;
            if (newValue != fProp.value.texture)
            {
                fProp.value.texture = newValue;
                NotifyNodes();
            }
        }

        void OnCubemapChanged(ChangeEvent<Object> evt)
        {
            var fProp = (CubemapShaderProperty)property;
            var newValue = (Cubemap)evt.newValue;
            if (newValue != fProp.value.cubemap)
            {
                fProp.value.cubemap = newValue;
                NotifyNodes();
            }
        }

        void OnDisplayNameChanged(ChangeEvent<string> evt)
        {
            if (evt.newValue != property.displayName)
            {
                property.displayName = evt.newValue;
                NotifyNodes();
            }
        }

        void OnClickRemove()
        {
            graph.owner.RegisterCompleteObjectUndo("Remove Property");
            graph.RemoveShaderProperty(property.guid);
            NotifyNodes();
        }

        void ValueField()
        {
            EditorGUI.BeginChangeCheck();
            m_ValueAction();
            if (EditorGUI.EndChangeCheck())
                NotifyNodes();
        }

        void NotifyNodes()
        {
            foreach (var node in graph.GetNodes<PropertyNode>())
                node.Dirty(ModificationScope.Node);
        }

        void FloatField()
        {
            var fProp = (FloatShaderProperty)property;
            fProp.value = EditorGUILayout.FloatField(fProp.value);
        }

        void Vector2Field()
        {
            var fProp = (Vector2ShaderProperty)property;
            fProp.value = EditorGUILayout.Vector2Field("", fProp.value);
        }

        void Vector3Field()
        {
            var fProp = (Vector3ShaderProperty)property;
            fProp.value = EditorGUILayout.Vector3Field("", fProp.value);
        }

        void Vector4Field()
        {
            var fProp = (Vector4ShaderProperty)property;
            fProp.value = EditorGUILayout.Vector4Field("", fProp.value);
        }
    }
}
