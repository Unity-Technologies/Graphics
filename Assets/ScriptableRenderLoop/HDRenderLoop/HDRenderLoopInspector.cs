using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;
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
            public readonly GUIContent materialDebugMode = new GUIContent("Material Debug Mode", "Display various properties of Materials.");
            public readonly GUIContent gBufferDebugMode = new GUIContent("GBuffer Debug Mode", "Display various properties of contained in the GBuffer.");

            public readonly GUIContent displayOpaqueObjects = new GUIContent("Display Opaque Objects", "Toggle opaque objects rendering on and off.");
            public readonly GUIContent displayTransparentObjects = new GUIContent("Display Transparent Objects", "Toggle transparent objects rendering on and off.");
            public readonly GUIContent enableTonemap = new GUIContent("Enable Tonemap");
            public readonly GUIContent exposure = new GUIContent("Exposure");

            public GUIContent[] materialDebugStrings = null;
            public int[] materialDebugValues = null;

            public readonly GUIContent[] gBufferDebugStrings = {	new GUIContent("None"),
                                                                    new GUIContent("Diffuse Color"),
                                                                    new GUIContent("Normal"),
                                                                    new GUIContent("Depth"),
                                                                    new GUIContent("Baked Diffuse"),
                                                                    new GUIContent("Specular Color"),
                                                                    new GUIContent("Specular Occlusion"),
                                                                    new GUIContent("Smoothness"),
                                                                    new GUIContent("MaterialId")
                                                                };
            public readonly int[] gBufferDebugValues = {	(int)HDRenderLoop.GBufferDebugMode.None,
                                                            (int)HDRenderLoop.GBufferDebugMode.DiffuseColor,
                                                            (int)HDRenderLoop.GBufferDebugMode.Normal,
                                                            (int)HDRenderLoop.GBufferDebugMode.Depth,
                                                            (int)HDRenderLoop.GBufferDebugMode.BakedDiffuse,
                                                            (int)HDRenderLoop.GBufferDebugMode.SpecularColor,
                                                            (int)HDRenderLoop.GBufferDebugMode.SpecularOcclustion,
                                                            (int)HDRenderLoop.GBufferDebugMode.Smoothness,
                                                            (int)HDRenderLoop.GBufferDebugMode.MaterialId
                                                        };
        }

        private static Styles s_Styles = null;
        private static Styles styles { get { if (s_Styles == null) s_Styles = new Styles(); return s_Styles; } }

        const float kMaxExposure = 32.0f;

        public override void OnInspectorGUI()
        {
            HDRenderLoop renderLoop = target as HDRenderLoop;
            if(renderLoop)
            {
                HDRenderLoop.DebugParameters debugParameters = renderLoop.debugParameters;

                EditorGUILayout.LabelField(styles.debugParameters);
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();

                if (styles.materialDebugStrings == null)
                {
                    String[] names = Enum.GetNames(typeof(HDRenderLoop.MaterialDebugMode));

                    styles.materialDebugStrings = new GUIContent[names.Length];
                    styles.materialDebugValues = new int[names.Length];

                    int index = 0;
                    foreach (var value in Enum.GetValues(typeof(HDRenderLoop.MaterialDebugMode)))
                    {
                        styles.materialDebugStrings[index] = new GUIContent(names[index]);
                        styles.materialDebugValues[index] = (int)value;
                        index++;
                    }
                }         

                debugParameters.gBufferDebugMode = (HDRenderLoop.GBufferDebugMode)EditorGUILayout.IntPopup(styles.gBufferDebugMode, (int)debugParameters.gBufferDebugMode, styles.gBufferDebugStrings, styles.gBufferDebugValues);
                debugParameters.materialDebugMode = (HDRenderLoop.MaterialDebugMode)EditorGUILayout.IntPopup(styles.materialDebugMode, (int)debugParameters.materialDebugMode, styles.materialDebugStrings, styles.materialDebugValues);

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
