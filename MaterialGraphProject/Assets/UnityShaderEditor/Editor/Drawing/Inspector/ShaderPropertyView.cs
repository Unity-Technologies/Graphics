using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Inspector
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

            m_ValueAction = null;
            if (property is FloatShaderProperty)
                m_ValueAction = FloatField;
            else if (property is Vector2ShaderProperty)
                m_ValueAction = Vector2Field;
            else if (property is Vector3ShaderProperty)
                m_ValueAction = Vector3Field;
            else if (property is Vector4ShaderProperty)
                m_ValueAction = Vector4Field;
            else if (property is ColorShaderProperty)
                m_ValueAction = ColorField;
            else if (property is TextureShaderProperty)
                m_ValueAction = TextureField;
            Assert.IsNotNull(m_ValueAction);

            Add(new IMGUIContainer(DisplayNameField) { name = "displayName" });
            Add(new IMGUIContainer(ValueField) { name = "value" });
            Add(new Button(OnClickRemove) { name = "remove", text = "Remove" });
        }

        void OnClickRemove()
        {
            graph.RemoveShaderProperty(property.guid);
            NotifyNodes();
        }

        void DisplayNameField()
        {
            EditorGUI.BeginChangeCheck();
            property.displayName = EditorGUILayout.DelayedTextField(property.displayName);
            if (EditorGUI.EndChangeCheck())
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
                node.onModified(node, ModificationScope.Node);
        }

        void FloatField()
        {
            var fProp = (FloatShaderProperty) property;
            fProp.value = EditorGUILayout.FloatField(fProp.value);
        }

        void Vector2Field()
        {
            var fProp = (Vector2ShaderProperty) property;
            fProp.value = EditorGUILayout.Vector2Field("", fProp.value);
        }

        void Vector3Field()
        {
            var fProp = (Vector3ShaderProperty) property;
            fProp.value = EditorGUILayout.Vector3Field("", fProp.value);
        }

        void Vector4Field()
        {
            var fProp = (Vector4ShaderProperty) property;
            fProp.value = EditorGUILayout.Vector4Field("", fProp.value);
        }

        void ColorField()
        {
            var fProp = (ColorShaderProperty) property;
            fProp.value = EditorGUILayout.ColorField("", fProp.value);
        }

        void TextureField()
        {
            var fProp = (TextureShaderProperty) property;
            fProp.value.texture = EditorGUILayout.MiniThumbnailObjectField(new GUIContent("Texture"), fProp.value.texture, typeof(Texture), null) as Texture;
        }
    }
}
