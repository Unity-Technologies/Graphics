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
    class DrawRenderersCustomPassDrawer : CustomPassDrawer
    {
        private class Styles
        {
            public static float defaultLineSpace = EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            public static float reorderableListHandleIndentWidth = 12;
            public static float indentSpaceInPixels = 16;
            public static float helpBoxHeight = EditorGUIUtility.singleLineHeight * 2;
            public static GUIContent callback = new GUIContent("Event", "Chose the Callback position for this render pass object.");
            public static GUIContent enabled = new GUIContent("Enabled", "Enable or Disable the custom pass");

            //Headers
            public static GUIContent filtersHeader = new GUIContent("Filters", "Filters.");
            public static GUIContent renderHeader = new GUIContent("Overrides", "Different parts of the rendering that you can choose to override.");

            //Filters
            public static GUIContent renderQueueFilter = new GUIContent("Queue", "Filter the render queue range you want to render.");
            public static GUIContent layerMask = new GUIContent("Layer Mask", "Chose the Callback position for this render pass object.");
            public static GUIContent shaderPassFilter = new GUIContent("Shader Passes", "Chose the Callback position for this render pass object.");

            //Render Options
            public static GUIContent overrideMaterial = new GUIContent("Material", "Chose an override material, every renderer will be rendered with this material.");
            public static GUIContent overrideMaterialPass = new GUIContent("Pass Name", "The pass for the override material to use.");
            public static GUIContent sortingCriteria = new GUIContent("Sorting", "Sorting settings used to render objects in a certain order.");
            public static GUIContent shaderPass = new GUIContent("Shader Pass", "Sets which pass will be used to render the materials. If the pass does not exist, the material will not be rendered.");

            //Depth Settings
            public static GUIContent overrideDepth = new GUIContent("Override Depth", "Override depth state of the objects rendered.");
            public static GUIContent depthWrite = new GUIContent("Write Depth", "Choose to write depth to the screen.");
            public static GUIContent depthCompareFunction = new GUIContent("Depth Test", "Choose a new test setting for the depth.");

            //Camera Settings
            public static GUIContent overrideCamera = new GUIContent("Camera", "Override camera projections.");
            public static GUIContent cameraFOV = new GUIContent("Field Of View", "Field Of View to render this pass in.");
            public static GUIContent positionOffset = new GUIContent("Position Offset", "This Vector acts as a relative offset for the camera.");
            public static GUIContent restoreCamera = new GUIContent("Restore", "Restore to the original camera projection before this pass.");

            public static string unlitShaderMessage = "HDRP Unlit shaders will force the shader passes to \"ForwardOnly\"";
            public static string hdrpLitShaderMessage = "HDRP Lit shaders are not supported in a Custom Pass";
            public static string opaqueObjectWithDeferred = "Your HDRP settings do not support ForwardOnly, some objects might not render.";
            public static string objectRendererTwiceWithMSAA = "MSAA is enabled, re-rendering the same object twice will cause depth test artifacts in Before/After Post Process injection points";
        }

        //Headers and layout
        private int m_FilterLines = 2;
        private int m_MaterialLines = 2;

        // Foldouts
        SerializedProperty      m_FilterFoldout;
        SerializedProperty      m_RendererFoldout;
        SerializedProperty      m_PassFoldout;
        SerializedProperty      m_TargetDepthBuffer;

        // Filter
        SerializedProperty      m_RenderQueue;
        SerializedProperty      m_LayerMask;
        SerializedProperty      m_ShaderPasses;

        // Render
        SerializedProperty      m_OverrideMaterial;
        SerializedProperty      m_OverrideMaterialPassName;
        SerializedProperty      m_SortingCriteria;
        SerializedProperty      m_ShaderPass;

        // Override depth state
        SerializedProperty      m_OverrideDepthState;
        SerializedProperty      m_DepthCompareFunction;
        SerializedProperty      m_DepthWrite;

        ReorderableList         m_ShaderPassesList;

        CustomPassVolume        m_Volume;

        bool customDepthIsNone => (CustomPass.TargetBuffer)m_TargetDepthBuffer.intValue == CustomPass.TargetBuffer.None;

        protected override void Initialize(SerializedProperty customPass)
        {
            // Header bools
            m_FilterFoldout = customPass.FindPropertyRelative("filterFoldout");
            m_RendererFoldout = customPass.FindPropertyRelative("rendererFoldout");
            m_PassFoldout = customPass.FindPropertyRelative("passFoldout");
            m_TargetDepthBuffer = customPass.FindPropertyRelative("targetDepthBuffer");

            // Filter props
            m_RenderQueue = customPass.FindPropertyRelative("renderQueueType");
            m_LayerMask = customPass.FindPropertyRelative("layerMask");
            m_ShaderPasses = customPass.FindPropertyRelative("passNames");
            m_ShaderPass = customPass.FindPropertyRelative("shaderPass");

            // Render options
            m_OverrideMaterial = customPass.FindPropertyRelative("overrideMaterial");
            m_OverrideMaterialPassName = customPass.FindPropertyRelative("overrideMaterialPassName");
            m_SortingCriteria = customPass.FindPropertyRelative("sortingCriteria");

            // Depth options
            m_OverrideDepthState = customPass.FindPropertyRelative("overrideDepthState");
            m_DepthCompareFunction = customPass.FindPropertyRelative("depthCompareFunction");
            m_DepthWrite = customPass.FindPropertyRelative("depthWrite");

            m_Volume = customPass.serializedObject.targetObject as CustomPassVolume;

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
            if (ShowMsaaObjectInfo())
            {
                Rect helpBoxRect = rect;
                helpBoxRect.height = Styles.helpBoxHeight;
                EditorGUI.HelpBox(helpBoxRect, Styles.objectRendererTwiceWithMSAA, MessageType.Info);
                rect.y += Styles.helpBoxHeight;
            }

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
                m_SortingCriteria.intValue = (int)(SortingCriteria)EditorGUI.EnumFlagsField(rect, Styles.sortingCriteria, (SortingCriteria)m_SortingCriteria.intValue);
                // EditorGUI.PropertyField(rect, m_SortingCriteria, Styles.sortingCriteria);
                rect.y += Styles.defaultLineSpace;

                EditorGUI.indentLevel--;
            }
        }

        // Tell if we need to show a warning for rendering opaque object and we're in deferred.
        bool ShowOpaqueObjectWarning()
        {
            if (HDRenderPipeline.currentAsset == null)
                return false;

            // Only opaque objects are concerned
            RenderQueueRange currentRange = CustomPassUtils.GetRenderQueueRangeFromRenderQueueType((CustomPass.RenderQueueType)m_RenderQueue.intValue);
            var allOpaque = HDRenderQueue.k_RenderQueue_AllOpaque;
            bool customPassQueueContainsOpaqueObjects = currentRange.upperBound >= allOpaque.lowerBound && currentRange.lowerBound <= allOpaque.upperBound;
            if (!customPassQueueContainsOpaqueObjects)
                return false;

            // Only Deferred rendering
            if (HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportedLitShaderMode != RenderPipelineSettings.SupportedLitShaderMode.DeferredOnly)
                return false;

            return true;
        }

        // Tell if we need to show the MSAA message info
        bool ShowMsaaObjectInfo()
        {
            if (HDRenderPipeline.currentAsset == null || !HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.supportMSAA)
                return false;

            if (m_Volume.injectionPoint != CustomPassInjectionPoint.AfterPostProcess && m_Volume.injectionPoint != CustomPassInjectionPoint.BeforePostProcess)
                return false;

            return true;
        }

        void DoFilters(ref Rect rect)
        {
            m_FilterFoldout.boolValue = EditorGUI.Foldout(rect, m_FilterFoldout.boolValue, Styles.filtersHeader, true);
            rect.y += Styles.defaultLineSpace;
            if (m_FilterFoldout.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUI.BeginProperty(rect, Styles.renderQueueFilter, m_RenderQueue);
                // There is still a bug with SerializedReference and PropertyField so we can't use it yet
                // EditorGUI.PropertyField(rect, m_RenderQueue, Styles.renderQueueFilter);
                m_RenderQueue.intValue = (int)(CustomPass.RenderQueueType)EditorGUI.EnumPopup(rect, Styles.renderQueueFilter, (CustomPass.RenderQueueType)m_RenderQueue.intValue);
                EditorGUI.EndProperty();
                rect.y += Styles.defaultLineSpace;
                if (ShowOpaqueObjectWarning())
                {
                    Rect helpBoxRect = rect;
                    helpBoxRect.xMin += EditorGUI.indentLevel * Styles.indentSpaceInPixels;
                    helpBoxRect.height = Styles.helpBoxHeight;
                    EditorGUI.HelpBox(helpBoxRect, Styles.opaqueObjectWithDeferred, MessageType.Error);
                    rect.y += Styles.helpBoxHeight;
                }
                //Layer mask
                EditorGUI.PropertyField(rect, m_LayerMask, Styles.layerMask);
                rect.y += Styles.defaultLineSpace;
                EditorGUI.indentLevel--;
            }
        }

        void DoMaterialOverride(ref Rect rect)
        {
            //Override material
            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, m_OverrideMaterial, Styles.overrideMaterial);
            if (EditorGUI.EndChangeCheck())
            {
                var mat = m_OverrideMaterial.objectReferenceValue as Material;
                // Fixup pass name in case the shader/material changes
                if (mat != null && mat.FindPass(m_OverrideMaterialPassName.stringValue) == -1)
                    m_OverrideMaterialPassName.stringValue = mat.GetPassName(0);
            }

            rect.y += Styles.defaultLineSpace;
            EditorGUI.indentLevel++;
            if (m_OverrideMaterial.objectReferenceValue)
            {
                EditorGUI.BeginProperty(rect, Styles.overrideMaterialPass, m_OverrideMaterialPassName);
                {
                    var mat = m_OverrideMaterial.objectReferenceValue as Material;
                    EditorGUI.BeginChangeCheck();
                    int index = mat.FindPass(m_OverrideMaterialPassName.stringValue);
                    index = EditorGUI.IntPopup(rect, Styles.overrideMaterialPass, index, GetMaterialPassNames(mat), Enumerable.Range(0, mat.passCount).ToArray());
                    if (EditorGUI.EndChangeCheck())
                        m_OverrideMaterialPassName.stringValue = mat.GetPassName(index);
                }
                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.BeginProperty(rect, Styles.renderQueueFilter, m_RenderQueue);
                // There is still a bug with SerializedReference and PropertyField so we can't use it yet
                // EditorGUI.PropertyField(rect, m_ShaderPass, Styles.shaderPass);
                m_ShaderPass.intValue = (int)(DrawRenderersCustomPass.ShaderPass)EditorGUI.EnumPopup(rect, Styles.shaderPass, (DrawRenderersCustomPass.ShaderPass)m_ShaderPass.intValue);
                EditorGUI.EndProperty();
            }
            EditorGUI.indentLevel--;

            rect.y += Styles.defaultLineSpace;
            EditorGUI.BeginProperty(rect, Styles.overrideDepth, m_OverrideDepthState);
            {
                if (customDepthIsNone)
                {
                    using (new EditorGUI.DisabledScope(true))
                        EditorGUI.Toggle(rect, Styles.overrideDepth, false);
                }
                else
                {
                    EditorGUI.PropertyField(rect, m_OverrideDepthState, Styles.overrideDepth);
                }
            }
            EditorGUI.EndProperty();

            if (m_OverrideDepthState.boolValue && !customDepthIsNone)
            {
                EditorGUI.indentLevel++;
                rect.y += Styles.defaultLineSpace;
                EditorGUI.PropertyField(rect, m_DepthCompareFunction, Styles.depthCompareFunction);
                rect.y += Styles.defaultLineSpace;
                EditorGUI.PropertyField(rect, m_DepthWrite, Styles.depthWrite);
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
            float height = Styles.defaultLineSpace;

            height += ShowMsaaObjectInfo() ? Styles.helpBoxHeight : 0;

            if (m_FilterFoldout.boolValue)
            {
                height += Styles.defaultLineSpace * m_FilterLines;
                height += ShowOpaqueObjectWarning() ? Styles.helpBoxHeight : 0;
            }

            height += Styles.defaultLineSpace; // add line for overrides dropdown
            if (m_RendererFoldout.boolValue)
            {
                height += Styles.defaultLineSpace * m_MaterialLines;
                height += Styles.defaultLineSpace * (m_OverrideDepthState.boolValue && !customDepthIsNone ? 3 : 1);
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
