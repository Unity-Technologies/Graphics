using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal static class ShaderGraphPropertyDrawers
    {

        public static void DrawShaderGraphGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Material m = materialEditor.target as Material;
            Shader s = m.shader;
            string path = AssetDatabase.GetAssetPath(s);
            ShaderGraphMetadata metadata = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if(obj is ShaderGraphMetadata meta)
                {
                    metadata = meta;
                    break;
                }
            }
            Debug.Assert(metadata != null, "Cannot draw ShaderGraph GUI on a non-ShaderGraph material", materialEditor.target);
            DrawShaderGraphGUI(materialEditor, properties, metadata.categoryDatas);
        }

        private static Rect GetRect(MaterialProperty prop)
        {
            return EditorGUILayout.GetControlRect(true, MaterialEditor.GetDefaultPropertyHeight(prop));
        }
        private static Rect GetRect()
        {
            return EditorGUILayout.GetControlRect();
        }

        private static MaterialProperty FindProperty(string propertyName, MaterialProperty[] properties)
        {
            foreach(var prop in properties)
            {
                if(prop.name == propertyName)
                {
                    return prop;
                }
            }
            throw new ArgumentException("no property was found with the name " + propertyName);
        }

        public static void DrawShaderGraphGUI(MaterialEditor materialEditor, MaterialProperty[] properties, IEnumerable<MinimalCategoryData> categoryDatas)
        {
            string s = "";
            foreach(MinimalCategoryData categoryData in categoryDatas)
            {
                s += $"{categoryData.categoryName}\n";
                foreach(MinimalCategoryData.PropertyData propData in categoryData.propertyDatas)
                {
                    s += $"\t{propData.referenceName}, {propData.valueType}\n";
                }
                s += "\n";
            }
            Debug.Log(s);

            foreach(MinimalCategoryData mcd in categoryDatas)
            {
                if (mcd.categoryName.Length > 0)
                {
                    EditorGUI.LabelField(GetRect(), mcd.categoryName, EditorStyles.boldLabel);
                }
                foreach(var propData in mcd.propertyDatas)
                {
                    MaterialProperty prop = FindProperty(propData.referenceName, properties);
                    DrawProperty(materialEditor, prop, propData);
                }
            }

            //materialEditor.PropertiesDefaultGUI(properties);
        }

        private static void DrawProperty(MaterialEditor materialEditor, MaterialProperty property, MinimalCategoryData.PropertyData propertyData)
        {
            switch (propertyData.valueType)
            {
                case ConcreteSlotValueType.SamplerState:
                    DrawSamplerStateProperty(property);
                    break;
                case ConcreteSlotValueType.Matrix4:
                    DrawMatrix4Property(property);
                    break;
                case ConcreteSlotValueType.Matrix3:
                    DrawMatrix3Property(property);
                    break;
                case ConcreteSlotValueType.Matrix2:
                    DrawMatrix2Property(property);
                    break;
                case ConcreteSlotValueType.Texture2D:
                    DrawTexture2DProperty(property);
                    break;
                case ConcreteSlotValueType.Texture2DArray:
                    DrawTexture2DArrayProperty(property);
                    break;
                case ConcreteSlotValueType.Texture3D:
                    DrawTexture3DProperty(property);
                    break;
                case ConcreteSlotValueType.Cubemap:
                    DrawCubemapProperty(property);
                    break;
                case ConcreteSlotValueType.Gradient:
                    break;
                case ConcreteSlotValueType.Vector4:
                    DrawVector4Property(property);
                    break;
                case ConcreteSlotValueType.Vector3:
                    DrawVector3Property(property);
                    break;
                case ConcreteSlotValueType.Vector2:
                    DrawVector2Property(property);
                    break;
                case ConcreteSlotValueType.Vector1:
                    if (propertyData.isKeyword)
                    {
                        materialEditor.ShaderProperty(property, property.displayName);
                    }
                    else
                    {
                        DrawFloatProperty(property);
                    }
                    break;
                case ConcreteSlotValueType.Boolean:
                    materialEditor.ShaderProperty(property, property.displayName);
                    break;
                case ConcreteSlotValueType.VirtualTexture:
                    DrawVirtualTextureProperty(property);
                    break;
            }
        }

        private static void DrawVirtualTextureProperty(MaterialProperty property)
        {
        }

        private static void DrawBooleanProperty(MaterialProperty property)
        {
        }

        private static void DrawFloatProperty(MaterialProperty property)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            float newValue = EditorGUILayout.FloatField(property.displayName, property.floatValue);
            EditorGUI.showMixedValue = false;
            if(EditorGUI.EndChangeCheck())
            {
                property.floatValue = newValue;
            }
        }

        private static void DrawVector2Property(MaterialProperty property)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            Vector2 newValue = EditorGUILayout.Vector2Field(property.displayName, new Vector2(property.vectorValue.x, property.vectorValue.y));
            EditorGUI.showMixedValue = false;
            if(EditorGUI.EndChangeCheck())
            {
                property.vectorValue = newValue;
            }
        }

        private static void DrawVector3Property(MaterialProperty property)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            Vector3 newValue = EditorGUILayout.Vector3Field(property.displayName, new Vector3(property.vectorValue.x, property.vectorValue.y, property.vectorValue.z));
            EditorGUI.showMixedValue = false;
            if(EditorGUI.EndChangeCheck())
            {
                property.vectorValue = newValue;
            }
        }

        private static void DrawVector4Property(MaterialProperty property)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            Vector4 newValue = EditorGUILayout.Vector4Field(property.displayName, property.vectorValue);
            EditorGUI.showMixedValue = false;
            if(EditorGUI.EndChangeCheck())
            {
                property.vectorValue = newValue;
            }
        }

        private static void DrawCubemapProperty(MaterialProperty property)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            Rect layout = GetRect(property);
            Object newValue = EditorGUI.ObjectField(layout, property.displayName, property.textureValue, typeof(Cubemap), false);
            EditorGUI.showMixedValue = false;
            if(EditorGUI.EndChangeCheck())
            {
                property.textureValue = newValue as Cubemap;
            }
        }

        private static void DrawTexture3DProperty(MaterialProperty property)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            Rect layout = GetRect(property);
            Object newValue = EditorGUI.ObjectField(layout, property.displayName, property.textureValue, typeof(Texture3D), false);
            EditorGUI.showMixedValue = false;
            if(EditorGUI.EndChangeCheck())
            {
                property.textureValue = newValue as Texture3D;
            }
        }

        private static void DrawTexture2DArrayProperty(MaterialProperty property)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            Rect layout = GetRect(property);
            Object newValue = EditorGUI.ObjectField(layout, property.displayName, property.textureValue, typeof(Texture2DArray), false);
            EditorGUI.showMixedValue = false;
            if(EditorGUI.EndChangeCheck())
            {
                property.textureValue = newValue as Texture2DArray;
            }
        }

        private static void DrawTexture2DProperty(MaterialProperty property)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;

            Rect layout = GetRect(property);
            Object newValue = EditorGUI.ObjectField(layout, property.displayName, property.textureValue, typeof(Texture2D), false);
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                property.textureValue = newValue as Texture2D;
            }
        }

        private static void DrawMatrix2Property(MaterialProperty property)
        {
            //we dont expose
        }

        private static void DrawMatrix3Property(MaterialProperty property)
        {
            //we dont expose
        }

        private static void DrawMatrix4Property(MaterialProperty property)
        {
            //we dont expose
        }

        private static void DrawSamplerStateProperty(MaterialProperty property)
        {
            //we dont expose
        }
    }
}
