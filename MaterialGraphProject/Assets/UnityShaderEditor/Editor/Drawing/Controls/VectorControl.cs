using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Drawing.Controls
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MultiFloatControlAttribute : Attribute, IControlAttribute
    {
        string m_Label;
        string m_SubLabel1;
        string m_SubLabel2;
        string m_SubLabel3;
        string m_SubLabel4;

        public MultiFloatControlAttribute(string label = null, string subLabel1 = "X", string subLabel2 = "Y", string subLabel3 = "Z", string subLabel4 = "W")
        {
            m_SubLabel1 = subLabel1;
            m_SubLabel2 = subLabel2;
            m_SubLabel3 = subLabel3;
            m_SubLabel4 = subLabel4;
            m_Label = label;
        }

        public VisualElement InstantiateControl(AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            if (!MultiFloatControlView.validTypes.Contains(propertyInfo.PropertyType))
                return null;
            return new MultiFloatControlView(m_Label, m_SubLabel1, m_SubLabel2, m_SubLabel3, m_SubLabel4, node, propertyInfo);
        }
    }

    public class MultiFloatControlView : VisualElement
    {
        public static Type[] validTypes = { typeof(float), typeof(Vector2), typeof(Vector3), typeof(Vector4) };

        AbstractMaterialNode m_Node;
        PropertyInfo m_PropertyInfo;
        float[] m_Values;
        GUIContent[] m_Labels;
        GUIContent m_Label;
        float m_Height;
        Action m_Read;
        Action m_Write;

        public MultiFloatControlView(string label, string subLabel1, string subLabel2, string subLabel3, string subLabel4, AbstractMaterialNode node, PropertyInfo propertyInfo)
        {
            int components;
            SerializedPropertyType serializedPropertyType;
            if (propertyInfo.PropertyType == typeof(float))
            {
                components = 1;
                serializedPropertyType = SerializedPropertyType.Float;
                m_Read = ReadFloat;
                m_Write = WriteFloat;
            }
            else if (propertyInfo.PropertyType == typeof(Vector2))
            {
                components = 2;
                serializedPropertyType = SerializedPropertyType.Vector2;
                m_Read = ReadVector2;
                m_Write = WriteVector2;
            }
            else if (propertyInfo.PropertyType == typeof(Vector3))
            {
                components = 3;
                serializedPropertyType = SerializedPropertyType.Vector3;
                m_Read = ReadVector3;
                m_Write = WriteVector3;
            }
            else if (propertyInfo.PropertyType == typeof(Vector4))
            {
                components = 4;
                serializedPropertyType = SerializedPropertyType.Vector4;
                m_Read = ReadVector4;
                m_Write = WriteVector4;
            }
            else
            {
                throw new ArgumentException("Property must be of type float, Vector2, Vector3 or Vector4.", "propertyInfo");
            }

            m_Label = new GUIContent(label ?? ObjectNames.NicifyVariableName(propertyInfo.Name));
            m_Node = node;
            m_PropertyInfo = propertyInfo;
            m_Values = new float[components];
            m_Labels = new GUIContent[components];
            m_Labels[0] = new GUIContent(subLabel1);
            if (components > 1)
                m_Labels[1] = new GUIContent(subLabel2);
            if (components > 2)
                m_Labels[2] = new GUIContent(subLabel3);
            if (components > 3)
                m_Labels[3] = new GUIContent(subLabel4);
            m_Height = EditorGUI.GetPropertyHeight(serializedPropertyType, m_Label);

            Add(new IMGUIContainer(OnGUIHandler));
        }

        void ReadFloat()
        {
            var value = (float)m_PropertyInfo.GetValue(m_Node, null);
            m_Values[0] = value;
        }

        void ReadVector2()
        {
            var value = (Vector2)m_PropertyInfo.GetValue(m_Node, null);
            m_Values[0] = value.x;
            m_Values[1] = value.y;
        }

        void ReadVector3()
        {
            var value = (Vector3)m_PropertyInfo.GetValue(m_Node, null);
            m_Values[0] = value.x;
            m_Values[1] = value.y;
            m_Values[2] = value.z;
        }

        void ReadVector4()
        {
            var value = (Vector4)m_PropertyInfo.GetValue(m_Node, null);
            m_Values[0] = value.x;
            m_Values[1] = value.y;
            m_Values[2] = value.z;
            m_Values[3] = value.w;
        }

        void WriteFloat()
        {
            m_PropertyInfo.SetValue(m_Node, m_Values[0], null);
        }

        void WriteVector2()
        {
            m_PropertyInfo.SetValue(m_Node, new Vector2(m_Values[0], m_Values[1]), null);
        }

        void WriteVector3()
        {
            m_PropertyInfo.SetValue(m_Node, new Vector3(m_Values[0], m_Values[1], m_Values[2]), null);
        }

        void WriteVector4()
        {
            m_PropertyInfo.SetValue(m_Node, new Vector4(m_Values[0], m_Values[1], m_Values[2], m_Values[3]), null);
        }

        void OnGUIHandler()
        {
            m_Read();
            using (var changeCheckScope = new EditorGUI.ChangeCheckScope())
            {
                var position = EditorGUILayout.GetControlRect(true, m_Height, EditorStyles.numberField);
                EditorGUI.MultiFloatField(position, m_Label, m_Labels, m_Values);
                if (changeCheckScope.changed)
                    m_Write();
            }
        }
    }
}
