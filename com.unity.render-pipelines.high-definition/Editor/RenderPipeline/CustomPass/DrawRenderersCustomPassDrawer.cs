// For now we disable the filtering by shader pass.
// #define SHOW_PASS_NAMES

using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using System.Linq;
using System;

namespace UnityEditor.Rendering.HighDefinition
{
    /// <summary>
    /// Custom drawer for the draw renderers pass
    /// </summary>
    [CustomPassDrawerAttribute(typeof(DrawRenderersCustomPass))]
    public class DrawRenderersCustomPassDrawer : CustomPassDrawer
    {
        private class Styles
        {
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static float reorderableListHandleIndentWidth = 12;
            public static float indentSpaceInPixels = 16;
            public static GUIContent callback = new GUIContent("Event", "Chose the Callback position for this render pass object.");
            public static GUIContent enabled = new GUIContent("Enabled", "Enable or Disable the custom pass");

            //Headers
            public static GUIContent filtersHeader = new GUIContent("Filters", "Filters.");
            public static GUIContent renderHeader = new GUIContent("Overrides", "Different parts fo the rendering that you can choose to override.");
            
            //Filters
            public static GUIContent renderQueueFilter = new GUIContent("Queue", "Filter the render queue range you want to render.");
            public static GUIContent layerMask = new GUIContent("Layer Mask", "Chose the Callback position for this render pass object.");
            public static GUIContent shaderPassFilter = new GUIContent("Shader Passes", "Chose the Callback position for this render pass object.");
            
            //Render Options
            public static GUIContent overrideMaterial = new GUIContent("Material", "Chose an override material, every renderer will be rendered with this material.");
            public static GUIContent overrideMaterialPass = new GUIContent("Pass Name", "The pass for the override material to use.");
            public static GUIContent sortingCriteria = new GUIContent("Sorting", "Sorting settings used to render objects in a certain order.");

		    //Depth Settings
		    public static GUIContent overrideDepth = new GUIContent("Override Depth", "Override depth state of the objects rendered.");
		    public static GUIContent depthWrite = new GUIContent("Write Depth", "Chose to write depth to the screen.");
		    public static GUIContent depthCompareFunction = new GUIContent("Depth Test", "Choose a new test setting for the depth.");

            //Camera Settings
            public static GUIContent overrideCamera = new GUIContent("Camera", "Override camera projections.");
            public static GUIContent cameraFOV = new GUIContent("Field Of View", "Field Of View to render this pass in.");
            public static GUIContent positionOffset = new GUIContent("Position Offset", "This Vector acts as a relative offset for the camera.");
            public static GUIContent restoreCamera = new GUIContent("Restore", "Restore to the original camera projection before this pass.");

            public static string unlitShaderMessage = "HDRP Unlit shaders will force the shader passes to \"ForwardOnly\"";
            public static string hdrpLitShaderMessage = "HDRP Lit shaders are not supported in a custom pass";
        }

        //Headers and layout
        private int m_FilterLines = 3;
        private int m_MaterialLines = 2;

        // Foldouts
        SerializedProperty      m_FilterFoldout;
        SerializedProperty      m_RendererFoldout;
        SerializedProperty      m_PassFoldout;

        // Filter
        SerializedProperty      m_RenderQueue;
        SerializedProperty      m_LayerMask;
        SerializedProperty      m_ShaderPasses;

        // Render
        SerializedProperty      m_OverrideMaterial;
        SerializedProperty      m_OverrideMaterialPass;
        SerializedProperty      m_SortingCriteria;
        
        // Override depth state
        SerializedProperty      m_OverrideDepthState;
        SerializedProperty      m_DepthCompareFunction;
        SerializedProperty      m_DepthWrite;

        ReorderableList         m_ShaderPassesList;

        protected override void Initialize(SerializedProperty customPass)
        {
            // Header bools
            m_FilterFoldout = customPass.FindPropertyRelative("filterFoldout");
            m_RendererFoldout = customPass.FindPropertyRelative("rendererFoldout");
            m_PassFoldout = customPass.FindPropertyRelative("passFoldout");

            // Filter props
            m_RenderQueue = customPass.FindPropertyRelative("renderQueueType");
            m_LayerMask = customPass.FindPropertyRelative("layerMask");
            m_ShaderPasses = customPass.FindPropertyRelative("passNames");

            // Render options
            m_OverrideMaterial = customPass.FindPropertyRelative("overrideMaterial");
            m_OverrideMaterialPass = customPass.FindPropertyRelative("overrideMaterialPassIndex");
            m_SortingCriteria = customPass.FindPropertyRelative("sortingCriteria");

            // Depth options
            m_OverrideDepthState = customPass.FindPropertyRelative("overrideDepthState");
            m_DepthCompareFunction = customPass.FindPropertyRelative("depthCompareFunction");
            m_DepthWrite = customPass.FindPropertyRelative("depthWrite");

            m_ShaderPassesList = new ReorderableList(null, m_ShaderPasses, true, true, true, true);

            m_ShaderPassesList.drawElementCallback =
            (Rect rect, int index, bool isActive, bool isFocused) =>
            {
                var element = m_ShaderPassesList.serializedProperty.GetArrayElementAtIndex(index);
                var propRect = new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight);
                var labelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = 50;
                element.stringValue = EditorGUI.TextField(propRect, "Name", element.stringValue);
                EditorGUIUtility.labelWidth = labelWidth;
            };

            m_ShaderPassesList.drawHeaderCallback = (Rect testHeaderRect) => {
                EditorGUI.LabelField(testHeaderRect, Styles.shaderPassFilter);
            };
        }

        protected override void DoPassGUI(SerializedProperty customPass, Rect rect)
        {
            DoFilters(ref rect);

            m_RendererFoldout.boolValue = EditorGUI.Foldout(rect, m_RendererFoldout.boolValue, Styles.renderHeader, true);
            rect.y += Styles.defaultLineSpace;
            if (m_RendererFoldout.boolValue)
            {
                EditorGUI.indentLevel++;
                //Override material
                DoMaterialOverride(ref rect);
                rect.y += Styles.defaultLineSpace;

#if SHOW_PASS_NAMES
                DoShaderPassesList(ref rect);
#endif

                // TODO: remove all this code when the fix for SerializedReference lands
                // EditorGUI.PropertyField(rect, m_SortingCriteria, Styles.sortingCriteria);
                m_SortingCriteria.intValue = (int)(SortingCriteria)EditorGUI.EnumPopup(rect, Styles.sortingCriteria, (SortingCriteria)m_SortingCriteria.intValue);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.indentLevel--;
            }
        }

        void DoFilters(ref Rect rect)
        {
            m_FilterFoldout.boolValue = EditorGUI.Foldout(rect, m_FilterFoldout.boolValue, Styles.filtersHeader, true);
            rect.y += Styles.defaultLineSpace;
            if (m_FilterFoldout.boolValue)
            {
                EditorGUI.indentLevel++;
                //Render queue filter
                // EditorGUI.PropertyField(rect, m_RenderQueue, Styles.renderQueueFilter);
                // TODO: remove all this code when the fix for SerializedReference lands
                m_RenderQueue.intValue = (int)(CustomPass.RenderQueueType)EditorGUI.EnumPopup(rect, Styles.renderQueueFilter, (CustomPass.RenderQueueType)m_RenderQueue.intValue);
                rect.y += Styles.defaultLineSpace;
                //Layer mask
                EditorGUI.PropertyField(rect, m_LayerMask, Styles.layerMask);
                rect.y += Styles.defaultLineSpace;
                //Shader pass list
                EditorGUI.indentLevel--;
            }
        }

        GUIContent[] GetMaterialPassNames(Material mat)
        {
            GUIContent[] passNames = new GUIContent[mat.passCount];

            for (int i = 0; i < mat.passCount; i++)
            {
                string passName = mat.GetPassName(i);
                passNames[i] = new GUIContent(string.IsNullOrEmpty(passName) ? i.ToString() : passName);
            }
            
            return passNames;
        }

        void DoMaterialOverride(ref Rect rect)
        {
            //Override material
            EditorGUI.BeginChangeCheck();
            // TODO: remove all this code when the fix for SerializedReference lands
            m_OverrideMaterial.objectReferenceValue = EditorGUI.ObjectField(rect, Styles.overrideMaterial, m_OverrideMaterial.objectReferenceValue, typeof(Material), false);
            // EditorGUI.PropertyField(rect, m_OverrideMaterial, Styles.overrideMaterial);
            if (EditorGUI.EndChangeCheck())
            {
                var mat = m_OverrideMaterial.objectReferenceValue as Material;
                if (mat != null && m_OverrideMaterialPass.intValue >= mat.passCount)
                    m_OverrideMaterialPass.intValue = mat.passCount - 1;
            }

            if (m_OverrideMaterial.objectReferenceValue)
            {
                var mat = m_OverrideMaterial.objectReferenceValue as Material;
                rect.y += Styles.defaultLineSpace;
                EditorGUI.indentLevel++;
                EditorGUI.BeginChangeCheck();
                m_OverrideMaterialPass.intValue = EditorGUI.IntPopup(rect, Styles.overrideMaterialPass, m_OverrideMaterialPass.intValue, GetMaterialPassNames(mat), Enumerable.Range(0, mat.passCount).ToArray());
                if (EditorGUI.EndChangeCheck())
                    m_OverrideMaterialPass.intValue = Mathf.Max(0, m_OverrideMaterialPass.intValue);
                EditorGUI.indentLevel--;
            }

            rect.y += Styles.defaultLineSpace;
            m_OverrideDepthState.boolValue = EditorGUI.Toggle(rect, Styles.overrideDepth, m_OverrideDepthState.boolValue);

            if (m_OverrideDepthState.boolValue)
            {
                EditorGUI.indentLevel++;
                rect.y += Styles.defaultLineSpace;
                m_DepthCompareFunction.intValue = (int)(CompareFunction)EditorGUI.EnumPopup(rect, Styles.depthCompareFunction, (CompareFunction)m_DepthCompareFunction.intValue);
                rect.y += Styles.defaultLineSpace;
                m_DepthWrite.boolValue = EditorGUI.Toggle(rect, Styles.depthWrite, m_DepthWrite.boolValue);
                EditorGUI.indentLevel--;
            }
        }

        void DoShaderPassesList(ref Rect rect)
        {
            Rect shaderPassesRect = rect;
            shaderPassesRect.x += EditorGUI.indentLevel * Styles.indentSpaceInPixels;
            shaderPassesRect.width -= EditorGUI.indentLevel * Styles.indentSpaceInPixels;

            var mat = m_OverrideMaterial.objectReferenceValue as Material;
            // We only draw the shader passes if we don't know which type of shader is used (aka user shaders)
            if (IsUnlitShader())
            {
                EditorGUI.HelpBox(shaderPassesRect, Styles.unlitShaderMessage, MessageType.Info);
                rect.y += Styles.defaultLineSpace;
            }
            else if (IsHDRPShader())
            {
                // Lit HDRP shader not supported
                EditorGUI.HelpBox(shaderPassesRect, Styles.hdrpLitShaderMessage, MessageType.Warning);
                rect.y += Styles.defaultLineSpace;
            }
            else
            {
                m_ShaderPassesList.DoList(shaderPassesRect);
                rect.y += m_ShaderPassesList.GetHeight();
            }
        }

        bool IsUnlitShader()
        {
            var mat = m_OverrideMaterial.objectReferenceValue as Material;
            return HDShaderUtils.IsUnlitHDRPShader(mat?.shader);
        }

        bool IsHDRPShader()
        {
            var mat = m_OverrideMaterial.objectReferenceValue as Material;
            return HDShaderUtils.IsHDRPShader(mat?.shader);
        }

        protected override float GetPassHeight(SerializedProperty customPass)
        {
            float height = Styles.defaultLineSpace * (m_FilterFoldout.boolValue ? m_FilterLines : 1);

            height += Styles.defaultLineSpace; // add line for overrides dropdown
            if (m_RendererFoldout.boolValue)
            {
                height += Styles.defaultLineSpace * (m_OverrideMaterial.objectReferenceValue != null ? m_MaterialLines : 1);
                height += Styles.defaultLineSpace * (m_OverrideDepthState.boolValue ? 3 : 1);
                var mat = m_OverrideMaterial.objectReferenceValue as Material;

#if SHOW_PASS_NAMES
                if (IsHDRPShader())
                    height += Styles.defaultLineSpace; // help box
                else
                    height += m_ShaderPassesList.GetHeight(); // shader passes list
#endif

                height += Styles.defaultLineSpace; // sorting criteria;
            }

            return height;
        }
    }
}