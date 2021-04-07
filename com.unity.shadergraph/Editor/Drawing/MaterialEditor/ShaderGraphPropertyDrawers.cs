using System;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderGraph.Drawing
{
    internal static class ShaderGraphPropertyDrawers
    {
        static Dictionary<MinimalCategoryData.GraphInputData, bool> s_CompoundPropertyFoldoutStates = new Dictionary<MinimalCategoryData.GraphInputData, bool>();

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
            AssertHelpers.IsNotNull(metadata, "Cannot draw ShaderGraph GUI on a non-ShaderGraph material", materialEditor.target);
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
            foreach(MinimalCategoryData mcd in categoryDatas)
            {
                DrawCategory(materialEditor, properties, mcd);
            }

            EditorGUILayout.Space();
            EditorGUILayout.Space();
            if (SupportedRenderingFeatures.active.editableMaterialRenderQueue)
            {
                materialEditor.RenderQueueField();
            }
            materialEditor.EnableInstancingField();
            materialEditor.DoubleSidedGIField();
        }

        private static void DrawCategory(MaterialEditor materialEditor, MaterialProperty[] properties, MinimalCategoryData minimalCategoryData)
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
                        DrawProperty(materialEditor, prop, propData);
                    }
                    else
                    {
                        DrawCompoundProperty(materialEditor, properties, propData);
                    }
                }
            }

            EditorGUILayout.EndFoldoutHeaderGroup();
        }

        private static void DrawCompoundProperty(MaterialEditor materialEditor, MaterialProperty[] properties, MinimalCategoryData.GraphInputData compoundPropertyData)
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
                        DrawProperty(materialEditor, property, subProperty);
                    }
                EditorGUI.indentLevel--;
            }

            if (exists)
                s_CompoundPropertyFoldoutStates[compoundPropertyData] = foldoutState;
            EditorGUI.indentLevel--;
        }


        private static void DrawProperty(MaterialEditor materialEditor, MaterialProperty property, MinimalCategoryData.GraphInputData inputData)
        {
            if(inputData.isKeyword)
            {
                switch (inputData.keywordType)
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
                switch (inputData.propertyType)
                {
                    case Internal.PropertyType.SamplerState:
                        DrawSamplerStateProperty(materialEditor, property);
                        break;
                    case Internal.PropertyType.Matrix4:
                        DrawMatrix4Property(materialEditor, property);
                        break;
                    case Internal.PropertyType.Matrix3:
                        DrawMatrix3Property(materialEditor, property);
                        break;
                    case Internal.PropertyType.Matrix2:
                        DrawMatrix2Property(materialEditor, property);
                        break;
                    case Internal.PropertyType.Texture2D:
                        DrawTexture2DProperty(materialEditor, property);
                        break;
                    case Internal.PropertyType.Texture2DArray:
                        DrawTexture2DArrayProperty(materialEditor, property);
                        break;
                    case Internal.PropertyType.Texture3D:
                        DrawTexture3DProperty(materialEditor, property);
                        break;
                    case Internal.PropertyType.Cubemap:
                        DrawCubemapProperty(materialEditor, property);
                        break;
                    case Internal.PropertyType.Gradient:
                        break;
                    case Internal.PropertyType.Vector4:
                        DrawVector4Property(materialEditor, property);
                        break;
                    case Internal.PropertyType.Vector3:
                        DrawVector3Property(materialEditor, property);
                        break;
                    case Internal.PropertyType.Vector2:
                        DrawVector2Property(materialEditor, property);
                        break;
                    case Internal.PropertyType.Float:
                        DrawFloatProperty(materialEditor, property);
                        break;
                    case Internal.PropertyType.Boolean:
                        DrawBooleanProperty(materialEditor, property);
                        break;
                    case Internal.PropertyType.VirtualTexture:
                        DrawVirtualTextureProperty(materialEditor, property);
                        break;
                    case Internal.PropertyType.Color:
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
            if(EditorGUI.EndChangeCheck())
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
            if(EditorGUI.EndChangeCheck())
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
