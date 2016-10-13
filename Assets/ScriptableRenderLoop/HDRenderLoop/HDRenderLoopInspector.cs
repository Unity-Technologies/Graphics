using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
using System.Reflection;
using System;

using UnityEditor;

//using EditorGUIUtility=UnityEditor.EditorGUIUtility;

namespace UnityEngine.ScriptableRenderLoop
{
    [CustomEditor(typeof(HDRenderLoop))]
    public class HDRenderLoopInspector : Editor
    {
        private class Styles
        {
            public readonly GUIContent debugParameters = new GUIContent("Debug Parameters");
            public readonly GUIContent debugViewMaterial = new GUIContent("DebugView Material", "Display various properties of Materials.");

            public readonly GUIContent displayOpaqueObjects = new GUIContent("Display Opaque Objects", "Toggle opaque objects rendering on and off.");
            public readonly GUIContent displayTransparentObjects = new GUIContent("Display Transparent Objects", "Toggle transparent objects rendering on and off.");
            public readonly GUIContent enableTonemap = new GUIContent("Enable Tonemap");
            public readonly GUIContent exposure = new GUIContent("Exposure");

            public bool isDebugViewMaterialInit = false;
            public GUIContent[] debugViewMaterialStrings = null;
            public int[] debugViewMaterialValues = null;
        }

        private static Styles s_Styles = null;
        private static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }

        const float kMaxExposure = 32.0f;

        void FillWithProperties(Type type, GUIContent[] debugViewMaterialStrings, int[] debugViewMaterialValues, bool isBSDFData, ref int index)
        {
            object[] attributes = type.GetCustomAttributes(true);
            // Get attribute to get the start number of the value for the enum
            GenerateHLSL attr = attributes[0] as GenerateHLSL;
            FieldInfo[] fields = type.GetFields();

            string subNamespace = type.Namespace.Substring(type.Namespace.LastIndexOf((".")) + 1);

            int localIndex = 0;
            foreach (var field in fields)
            {
                string name = field.Name;
                // Check if the display name have been override by the users
                if (Attribute.IsDefined(field, typeof(SurfaceDataAttributes)))
                {
                    var propertyAttr = (SurfaceDataAttributes[])field.GetCustomAttributes(typeof(SurfaceDataAttributes), false);
                    if (propertyAttr[0].displayName != "")
                    {
                        name = propertyAttr[0].displayName;
                    }
                }

                name = (isBSDFData ? "Engine/" : "") + subNamespace + "/" + name;

                debugViewMaterialStrings[index] = new GUIContent(name);
                debugViewMaterialValues[index] = attr.debugCounterStart + (int)localIndex;
                index++;
                localIndex++;
            }
        }

        void FillWithPropertiesEnum(Type type, GUIContent[] debugViewMaterialStrings, int[] debugViewMaterialValues, bool isBSDFData, ref int index)
        {
            String[] names = Enum.GetNames(type);

            int localIndex = 0;
            foreach (var value in Enum.GetValues(type))
            {
                string name = (isBSDFData ? "Engine/" : "") + names[localIndex];

                debugViewMaterialStrings[index] = new GUIContent(name);
                debugViewMaterialValues[index] = (int)value;
                index++;
                localIndex++;
            }
        }

        public override void OnInspectorGUI()
        {
            HDRenderLoop renderLoop = target as HDRenderLoop;
            if(renderLoop)
            {
                HDRenderLoop.DebugParameters debugParameters = renderLoop.debugParameters;

                EditorGUILayout.LabelField(styles.debugParameters);
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                if (!styles.isDebugViewMaterialInit)
                {
                    String[] varyingNames = Enum.GetNames(typeof(HDRenderLoop.DebugViewVaryingMode));
                    String[] GBufferNames = Enum.GetNames(typeof(HDRenderLoop.DebugViewGbufferMode));
                    
                    // +1 for the zero case
                    int num = 1 + varyingNames.Length 
                                + GBufferNames.Length
                                + typeof(Builtin.BuiltinData).GetFields().Length 
                                + typeof(Lit.SurfaceData).GetFields().Length 
                                + typeof(Lit.BSDFData).GetFields().Length;

                    styles.debugViewMaterialStrings = new GUIContent[num];
                    styles.debugViewMaterialValues = new int[num];

                    int index = 0;

                    // 0 is a reserved number
                    styles.debugViewMaterialStrings[0] = new GUIContent("None");
                    styles.debugViewMaterialValues[0] = 0;
                    index++;

                    FillWithPropertiesEnum(typeof(HDRenderLoop.DebugViewVaryingMode), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, ref index);
                    FillWithProperties(typeof(Builtin.BuiltinData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, ref index);
                    FillWithProperties(typeof(Lit.SurfaceData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, false, ref index);

                    // Engine
                    FillWithPropertiesEnum(typeof(HDRenderLoop.DebugViewGbufferMode), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, true, ref index);
                    FillWithProperties(typeof(Lit.BSDFData), styles.debugViewMaterialStrings, styles.debugViewMaterialValues, true, ref index);

                    styles.isDebugViewMaterialInit = true;
                }

                debugParameters.debugViewMaterial = EditorGUILayout.IntPopup(styles.debugViewMaterial, (int)debugParameters.debugViewMaterial, styles.debugViewMaterialStrings, styles.debugViewMaterialValues);

                EditorGUILayout.Space();
                debugParameters.enableTonemap = EditorGUILayout.Toggle(styles.enableTonemap, debugParameters.enableTonemap);
                debugParameters.exposure = Mathf.Max(Mathf.Min(EditorGUILayout.FloatField(styles.exposure, debugParameters.exposure), kMaxExposure), -kMaxExposure);

                EditorGUILayout.Space();
                debugParameters.displayOpaqueObjects = EditorGUILayout.Toggle(styles.displayOpaqueObjects, debugParameters.displayOpaqueObjects);
                debugParameters.displayTransparentObjects = EditorGUILayout.Toggle(styles.displayTransparentObjects, debugParameters.displayTransparentObjects);

                if(EditorGUI.EndChangeCheck())
                {
                    EditorUtility.SetDirty(renderLoop); // Repaint
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}
