using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    using CED = CoreEditorDrawer<UniversalRenderPipelineSerializedCamera>;

    static partial class UniversalRenderPipelineCameraUI
    {
        public partial class Rendering
        {
            static bool s_PostProcessingWarningShown = false;

            static readonly CED.IDrawer PostProcessingWarningInit = CED.Group(
                (serialized, owner) => s_PostProcessingWarningShown = false
            );

            private static readonly CED.IDrawer PostProcessingWarningDrawer = CED.Conditional(
                (serialized, owner) => IsAnyRendererHasPostProcessingEnabled(serialized, UniversalRenderPipeline.asset) && serialized.renderPostProcessing.boolValue,
                (serialized, owner) =>
                {
                    EditorGUILayout.HelpBox(Styles.disabledPostprocessing, MessageType.Warning);
                    s_PostProcessingWarningShown = true;
                });

            private static readonly CED.IDrawer PostProcessingAAWarningDrawer = CED.Conditional(
                (serialized, owner) => !s_PostProcessingWarningShown && IsAnyRendererHasPostProcessingEnabled(serialized, UniversalRenderPipeline.asset) && (AntialiasingMode)serialized.antialiasing.intValue != AntialiasingMode.None,
                (serialized, owner) =>
                {
                    EditorGUILayout.HelpBox(Styles.disabledPostprocessing, MessageType.Warning);
                    s_PostProcessingWarningShown = true;
                });

            private static readonly CED.IDrawer PostProcessingStopNaNsWarningDrawer = CED.Conditional(
                (serialized, owner) => !s_PostProcessingWarningShown && IsAnyRendererHasPostProcessingEnabled(serialized, UniversalRenderPipeline.asset) && serialized.stopNaNs.boolValue,
                (serialized, owner) =>
                {
                    EditorGUILayout.HelpBox(Styles.disabledPostprocessing, MessageType.Warning);
                    s_PostProcessingWarningShown = true;
                });

            private static readonly CED.IDrawer PostProcessingDitheringWarningDrawer = CED.Conditional(
                (serialized, owner) => !s_PostProcessingWarningShown && IsAnyRendererHasPostProcessingEnabled(serialized, UniversalRenderPipeline.asset) && serialized.dithering.boolValue,
                (serialized, owner) =>
                {
                    EditorGUILayout.HelpBox(Styles.disabledPostprocessing, MessageType.Warning);
                    s_PostProcessingWarningShown = true;
                });

            static readonly CED.IDrawer BaseCameraRenderTypeDrawer = CED.Conditional(
                (serialized, owner) => (CameraRenderType)serialized.cameraType.intValue == CameraRenderType.Base,
                CED.Group(
                    DrawerRenderingRenderPostProcessing
                    ),
                PostProcessingWarningDrawer,
                CED.Group(
                    DrawerRenderingAntialiasing
                    ),
                PostProcessingAAWarningDrawer,
                CED.Conditional(
                    (serialized, owner) => !serialized.antialiasing.hasMultipleDifferentValues,
                    CED.Group(
                        GroupOption.Indent,
                        CED.Conditional(
                            (serialized, owner) => (AntialiasingMode)serialized.antialiasing.intValue ==
                            AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                            CED.Group(
                                DrawerRenderingSMAAQuality
                            )
                        )
                    )
                    ),
                CED.Group(
                    CameraUI.Rendering.Drawer_Rendering_StopNaNs
                    ),
                PostProcessingStopNaNsWarningDrawer,
                CED.Conditional(
                    (serialized, owner) => serialized.stopNaNs.boolValue && CoreEditorUtils.buildTargets.Contains(GraphicsDeviceType.OpenGLES2),
                    (serialized, owner) => EditorGUILayout.HelpBox(Styles.stopNaNsMessage, MessageType.Warning)
                    ),
                CED.Group(
                    CameraUI.Rendering.Drawer_Rendering_Dithering
                    ),
                PostProcessingDitheringWarningDrawer,
                CED.Group(
                    DrawerRenderingRenderShadows,
                    DrawerRenderingPriority,
                    DrawerRenderingOpaqueTexture,
                    DrawerRenderingDepthTexture
                )
            );

            static readonly CED.IDrawer OverlayCameraRenderTypeDrawer = CED.Conditional(
                (serialized, owner) => (CameraRenderType)serialized.cameraType.intValue == CameraRenderType.Overlay,
                CED.Group(
                    DrawerRenderingRenderPostProcessing
                    ),
                PostProcessingWarningDrawer,
                CED.Group(
                    DrawerRenderingClearDepth,
                    DrawerRenderingRenderShadows
                )
            );

            public static readonly CED.IDrawer Drawer = CED.FoldoutGroup(
                CameraUI.Rendering.Styles.header,
                Expandable.Rendering,
                k_ExpandedState,
                FoldoutOption.Indent,
                PostProcessingWarningInit,
                CED.Group(
                    DrawerRenderingRenderer
                    ),
                BaseCameraRenderTypeDrawer,
                OverlayCameraRenderTypeDrawer,
                CED.Group(
                    CameraUI.Rendering.Drawer_Rendering_CullingMask,
                    CameraUI.Rendering.Drawer_Rendering_OcclusionCulling
                )
            );

            public static readonly CED.IDrawer DrawerPreset = CED.FoldoutGroup(
                CameraUI.Rendering.Styles.header,
                Expandable.Rendering,
                k_ExpandedState,
                FoldoutOption.Indent,
                CED.Group(
                    CameraUI.Rendering.Drawer_Rendering_CullingMask,
                    CameraUI.Rendering.Drawer_Rendering_OcclusionCulling
                )
            );

            static void DrawerRenderingRenderer(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                var rpAsset = UniversalRenderPipeline.asset;

                int selectedRendererOption = p.renderer.intValue;
                EditorGUI.BeginChangeCheck();

                Rect controlRect = EditorGUILayout.GetControlRect(true);
                EditorGUI.BeginProperty(controlRect, Styles.rendererType, p.renderer);

                EditorGUI.showMixedValue = p.renderer.hasMultipleDifferentValues;
                int selectedRenderer = EditorGUI.IntPopup(controlRect, Styles.rendererType, selectedRendererOption, rpAsset.rendererDisplayList, rpAsset.rendererIndexList);
                EditorGUI.EndProperty();
                if (!rpAsset.ValidateRendererDataList())
                {
                    EditorGUILayout.HelpBox(Styles.noRendererError, MessageType.Error);
                }
                else if (!rpAsset.ValidateRendererData(selectedRendererOption))
                {
                    EditorGUILayout.HelpBox(Styles.missingRendererWarning, MessageType.Warning);
                    var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
                    if (GUI.Button(rect, Styles.selectRenderPipelineAsset))
                    {
                        Selection.activeObject = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(AssetDatabase.GetAssetPath(UniversalRenderPipeline.asset));
                    }
                    GUILayout.Space(5);
                }

                if (EditorGUI.EndChangeCheck())
                    p.renderer.intValue = selectedRenderer;
            }

            static bool IsAnyRendererHasPostProcessingEnabled(UniversalRenderPipelineSerializedCamera p, UniversalRenderPipelineAsset rpAsset)
            {
                int selectedRendererOption = p.renderer.intValue;

                if (selectedRendererOption < -1 || selectedRendererOption > rpAsset.m_RendererDataList.Length || p.renderer.hasMultipleDifferentValues)
                    return false;

                var rendererData = selectedRendererOption == -1 ? rpAsset.scriptableRendererData : rpAsset.m_RendererDataList[selectedRendererOption];

                var forwardRendererData = rendererData as UniversalRendererData;
                if (forwardRendererData != null && forwardRendererData.postProcessData == null)
                    return true;

                var renderer2DData = rendererData as UnityEngine.Rendering.Universal.Renderer2DData;
                return renderer2DData != null && renderer2DData.postProcessData == null;
            }

            static void DrawerRenderingAntialiasing(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                Rect antiAliasingRect = EditorGUILayout.GetControlRect();
                EditorGUI.BeginProperty(antiAliasingRect, Styles.antialiasing, p.antialiasing);
                {
                    EditorGUI.BeginChangeCheck();
                    int selectedValue = (int)(AntialiasingMode)EditorGUI.EnumPopup(antiAliasingRect, Styles.antialiasing, (AntialiasingMode)p.antialiasing.intValue);
                    if (EditorGUI.EndChangeCheck())
                        p.antialiasing.intValue = selectedValue;
                }
                EditorGUI.EndProperty();
            }

            static void DrawerRenderingClearDepth(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.clearDepth, Styles.clearDepth);
            }

            static void DrawerRenderingRenderShadows(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.renderShadows, Styles.renderingShadows);
            }

            static void DrawerRenderingSMAAQuality(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.antialiasingQuality, Styles.antialiasingQuality);

                if (CoreEditorUtils.buildTargets.Contains(GraphicsDeviceType.OpenGLES2))
                    EditorGUILayout.HelpBox(Styles.SMAANotSupported, MessageType.Warning);
            }

            static void DrawerRenderingRenderPostProcessing(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.renderPostProcessing, Styles.renderPostProcessing);
            }

            static void DrawerRenderingPriority(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.baseCameraSettings.depth, Styles.priority);
            }

            static void DrawerRenderingDepthTexture(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.renderDepth, Styles.requireDepthTexture);
            }

            static void DrawerRenderingOpaqueTexture(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                EditorGUILayout.PropertyField(p.renderOpaque, Styles.requireOpaqueTexture);
            }
        }
    }
}
