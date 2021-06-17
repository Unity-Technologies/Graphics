using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal static class ShaderGraphPropertyDrawers
    {
        static Dictionary<MinimalCategoryData.GraphInputData, bool> s_CompoundPropertyFoldoutStates = new Dictionary<MinimalCategoryData.GraphInputData, bool>();

        public static void DrawShaderGraphGUI(MaterialEditor materialEditor, IEnumerable<MaterialProperty> properties)
        {
            Material m = materialEditor.target as Material;
            Shader s = m.shader;
            string path = AssetDatabase.GetAssetPath(s);
            ShaderGraphMetadata metadata = null;
            foreach (var obj in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (obj is ShaderGraphMetadata meta)
                {
                    metadata = meta;
                    break;
                }
            }
            AssertHelpers.IsNotNull(metadata, "Cannot draw ShaderGraph GUI on a non-ShaderGraph material: " + materialEditor.target);
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

        private static MaterialProperty FindProperty(string propertyName, IEnumerable<MaterialProperty> properties)
        {
            foreach (var prop in properties)
            {
                if (prop.name == propertyName)
                {
                    return prop;
                }
            }
            throw new ArgumentException("no property was found with the name " + propertyName);
        }

        public static void DrawShaderGraphGUI(MaterialEditor materialEditor, IEnumerable<MaterialProperty> properties, IEnumerable<MinimalCategoryData> categoryDatas)
        {
            foreach (MinimalCategoryData mcd in categoryDatas)
            {
                DrawCategory(materialEditor, properties, mcd);
            }
        }

        private static void DrawCategory(MaterialEditor materialEditor, IEnumerable<MaterialProperty> properties, MinimalCategoryData minimalCategoryData)
        {
            if (minimalCategoryData.categoryName.Length > 0)
            {
                minimalCategoryData.expanded = EditorGUILayout.BeginFoldoutHeaderGroup(minimalCategoryData.expanded, minimalCategoryData.categoryName);
            }
            else
            {
                // force draw if no category name to do foldout on
                minimalCategoryData.expanded = true;
            }

            if (minimalCategoryData.expanded)
            {
                foreach (var propData in minimalCategoryData.propertyDatas)
                {
                    if (propData.isCompoundProperty == false)
                    {
                        MaterialProperty prop = FindProperty(propData.referenceName, properties);
                        DrawMaterialProperty(materialEditor, prop, propData.propertyType, propData.isKeyword, propData.keywordType);
                    }
                    else
                    {
                        DrawCompoundProperty(materialEditor, properties, propData);
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawCompoundProperty(MaterialEditor materialEditor, IEnumerable<MaterialProperty> properties, MinimalCategoryData.GraphInputData compoundPropertyData)
        {
            EditorGUI.indentLevel++;

            bool foldoutState = true;
            var exists = s_CompoundPropertyFoldoutStates.ContainsKey(compoundPropertyData);
            if (!exists)
                s_CompoundPropertyFoldoutStates.Add(compoundPropertyData, true);
            else
                foldoutState = s_CompoundPropertyFoldoutStates[compoundPropertyData];

            foldoutState = EditorGUILayout.Foldout(foldoutState, compoundPropertyData.referenceName);
            if (foldoutState)
            {
                EditorGUI.indentLevel++;
                foreach (var subProperty in compoundPropertyData.subProperties)
                {
                    var property = FindProperty(subProperty.referenceName, properties);
                    DrawMaterialProperty(materialEditor, property, subProperty.propertyType);
                }
                EditorGUI.indentLevel--;
            }

            if (exists)
                s_CompoundPropertyFoldoutStates[compoundPropertyData] = foldoutState;
            EditorGUI.indentLevel--;
        }

        private static void DrawMaterialProperty(MaterialEditor materialEditor, MaterialProperty property, PropertyType propertyType, bool isKeyword = false, KeywordType keywordType = KeywordType.Boolean)
        {
            if (isKeyword)
            {
                switch (keywordType)
                {
                    case KeywordType.Boolean:
                        DrawBooleanKeyword(materialEditor, property);
                        break;
                    case KeywordType.Enum:
                        DrawEnumKeyword(materialEditor, property);
                        break;
                }
            }
            else
            {
                switch (propertyType)
                {
                    case PropertyType.SamplerState:
                        DrawSamplerStateProperty(materialEditor, property);
                        break;
                    case PropertyType.Matrix4:
                        DrawMatrix4Property(materialEditor, property);
                        break;
                    case PropertyType.Matrix3:
                        DrawMatrix3Property(materialEditor, property);
                        break;
                    case PropertyType.Matrix2:
                        DrawMatrix2Property(materialEditor, property);
                        break;
                    case PropertyType.Texture2D:
                        DrawTexture2DProperty(materialEditor, property);
                        break;
                    case PropertyType.Texture2DArray:
                        DrawTexture2DArrayProperty(materialEditor, property);
                        break;
                    case PropertyType.Texture3D:
                        DrawTexture3DProperty(materialEditor, property);
                        break;
                    case PropertyType.Cubemap:
                        DrawCubemapProperty(materialEditor, property);
                        break;
                    case PropertyType.Gradient:
                        break;
                    case PropertyType.Vector4:
                        DrawVector4Property(materialEditor, property);
                        break;
                    case PropertyType.Vector3:
                        DrawVector3Property(materialEditor, property);
                        break;
                    case PropertyType.Vector2:
                        DrawVector2Property(materialEditor, property);
                        break;
                    case PropertyType.Float:
                        DrawFloatProperty(materialEditor, property);
                        break;
                    case PropertyType.Boolean:
                        DrawBooleanProperty(materialEditor, property);
                        break;
                    case PropertyType.VirtualTexture:
                        DrawVirtualTextureProperty(materialEditor, property);
                        break;
                    case PropertyType.Color:
                        DrawColorProperty(materialEditor, property);
                        break;
                }
            }
        }

        private static void DrawColorProperty(MaterialEditor materialEditor, MaterialProperty property)
        {
            materialEditor.ShaderProperty(property, property.displayName);
        }

        private static void DrawEnumKeyword(MaterialEditor materialEditor, MaterialProperty property)
        {
            materialEditor.ShaderProperty(property, property.displayName);
        }

        private static void DrawBooleanKeyword(MaterialEditor materialEditor, MaterialProperty property)
        {
            materialEditor.ShaderProperty(property, property.displayName);
        }

        private static void DrawVirtualTextureProperty(MaterialEditor materialEditor, MaterialProperty property)
        {
        }

        private static void DrawBooleanProperty(MaterialEditor materialEditor, MaterialProperty property)
        {
            materialEditor.ShaderProperty(property, property.displayName);
        }

        private static void DrawFloatProperty(MaterialEditor materialEditor, MaterialProperty property)
        {
            materialEditor.ShaderProperty(property, property.displayName);
        }

        private static void DrawVector2Property(MaterialEditor materialEditor, MaterialProperty property)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            Vector2 newValue = EditorGUI.Vector2Field(GetRect(property), property.displayName, new Vector2(property.vectorValue.x, property.vectorValue.y));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                property.vectorValue = newValue;
            }
        }

        private static void DrawVector3Property(MaterialEditor materialEditor, MaterialProperty property)
        {
            EditorGUI.BeginChangeCheck();
            EditorGUI.showMixedValue = property.hasMixedValue;
            Vector3 newValue = EditorGUI.Vector3Field(GetRect(property), property.displayName, new Vector3(property.vectorValue.x, property.vectorValue.y, property.vectorValue.z));
            EditorGUI.showMixedValue = false;
            if (EditorGUI.EndChangeCheck())
            {
                property.vectorValue = newValue;
            }
        }

        private static void DrawVector4Property(MaterialEditor materialEditor, MaterialProperty property)
        {
            materialEditor.ShaderProperty(property, property.displayName);
        }

        private static void DrawCubemapProperty(MaterialEditor materialEditor, MaterialProperty property)
        {
            materialEditor.ShaderProperty(property, property.displayName);
        }

        private static void DrawTexture3DProperty(MaterialEditor materialEditor, MaterialProperty property)
        {
            materialEditor.ShaderProperty(property, property.displayName);
        }

        private static void DrawTexture2DArrayProperty(MaterialEditor materialEditor, MaterialProperty property)
        {
            materialEditor.ShaderProperty(property, property.displayName);
        }

        private static void DrawTexture2DProperty(MaterialEditor materialEditor, MaterialProperty property)
        {
            materialEditor.ShaderProperty(property, property.displayName);
        }

        private static void DrawMatrix2Property(MaterialEditor materialEditor, MaterialProperty property)
        {
            //we dont expose
        }

        private static void DrawMatrix3Property(MaterialEditor materialEditor, MaterialProperty property)
        {
            //we dont expose
        }

        private static void DrawMatrix4Property(MaterialEditor materialEditor, MaterialProperty property)
        {
            //we dont expose
        }

        private static void DrawSamplerStateProperty(MaterialEditor materialEditor, MaterialProperty property)
        {
            //we dont expose
        }
    }
}
