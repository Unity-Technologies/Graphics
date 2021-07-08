using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDCamera>;

    static partial class HDCameraUI
    {
        partial class Output
        {
            public static readonly CED.IDrawer Drawer = CED.FoldoutGroup(
                CameraUI.Output.Styles.header,
                Expandable.Output,
                k_ExpandedState,
                FoldoutOption.Indent,
                CED.Group(
#if ENABLE_VR && ENABLE_XR_MANAGEMENT
                    Drawer_SectionXRRendering,
#endif
#if ENABLE_MULTIPLE_DISPLAYS
                    Drawer_Output_MultiDisplay,
#endif
                    CameraUI.Output.Drawer_Output_RenderTarget,
                    Drawer_Output_MSAA_Warning,
                    CameraUI.Output.Drawer_Output_Depth,
                    CameraUI.Output.Drawer_Output_NormalizedViewPort
                )
            );

            static void Drawer_Output_MSAA_Warning(SerializedHDCamera p, Editor owner)
            {
                // show warning if we have deferred but manual MSAA set
                // only do this if the m_TargetTexture has the same values across all target cameras
                if (!p.baseCameraSettings.targetTexture.hasMultipleDifferentValues)
                {
                    var targetTexture = p.baseCameraSettings.targetTexture.objectReferenceValue as RenderTexture;
                    if (targetTexture
                        && targetTexture.antiAliasing > 1
                        && p.frameSettings.litShaderMode == LitShaderMode.Deferred)
                    {
                        EditorGUILayout.HelpBox(Styles.msaaWarningMessage, MessageType.Warning, true);
                    }
                }
            }

#if ENABLE_VR && ENABLE_XR_MANAGEMENT
            static void Drawer_SectionXRRendering(SerializedHDCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.xrRendering, Styles.xrRenderingContent);
            }

#endif

#if ENABLE_MULTIPLE_DISPLAYS
            static void Drawer_Output_MultiDisplay(SerializedHDCamera p, Editor owner)
            {
                if (ModuleManager_ShouldShowMultiDisplayOption())
                {
                    var prevDisplay = p.baseCameraSettings.targetDisplay.intValue;
                    EditorGUILayout.IntPopup(p.baseCameraSettings.targetDisplay, DisplayUtility_GetDisplayNames(), DisplayUtility_GetDisplayIndices(), Styles.targetDisplay);
                    if (prevDisplay != p.baseCameraSettings.targetDisplay.intValue)
                        UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                }
            }

            static MethodInfo k_DisplayUtility_GetDisplayIndices = Type.GetType("UnityEditor.DisplayUtility,UnityEditor")
                .GetMethod("GetDisplayIndices");
            static int[] DisplayUtility_GetDisplayIndices()
            {
                return (int[])k_DisplayUtility_GetDisplayIndices.Invoke(null, null);
            }

            static MethodInfo k_DisplayUtility_GetDisplayNames = Type.GetType("UnityEditor.DisplayUtility,UnityEditor")
                .GetMethod("GetDisplayNames");
            static GUIContent[] DisplayUtility_GetDisplayNames()
            {
                return (GUIContent[])k_DisplayUtility_GetDisplayNames.Invoke(null, null);
            }

            static MethodInfo k_ModuleManager_ShouldShowMultiDisplayOption = Type.GetType("UnityEditor.Modules.ModuleManager,UnityEditor")
                .GetMethod("ShouldShowMultiDisplayOption", BindingFlags.Static | BindingFlags.NonPublic);
            static bool ModuleManager_ShouldShowMultiDisplayOption()
            {
                return (bool)k_ModuleManager_ShouldShowMultiDisplayOption.Invoke(null, null);
            }

#endif
        }
    }
}
