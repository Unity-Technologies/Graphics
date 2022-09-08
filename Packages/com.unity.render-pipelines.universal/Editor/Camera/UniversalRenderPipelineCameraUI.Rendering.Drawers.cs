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
                        new[]{
                            CED.Conditional(
                                (serialized, owner) => (AntialiasingMode)serialized.antialiasing.intValue ==
                                AntialiasingMode.SubpixelMorphologicalAntiAliasing,
                                CED.Group(
                                    DrawerRenderingSMAAQuality
                                )),
#if URP_EXPERIMENTAL_TAA_ENABLE
                            CED.Conditional(
                                (serialized, owner) => (AntialiasingMode)serialized.antialiasing.intValue ==
                                AntialiasingMode.TemporalAntiAliasing,
                                CED.Group(
                                    DrawerRenderingTAAQuality
                                ))
#endif
                        }
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
                    DrawerRenderingRenderShadows,
                    DrawerRenderingDepthTexture
                )
            );

            public static readonly CED.IDrawer Drawer;

            public static readonly CED.IDrawer DrawerPreset;

            static Rendering()
            {
                Drawer = CED.FoldoutGroup(
                    CameraUI.Rendering.Styles.header,
                    Expandable.Rendering,
                    k_ExpandedState,
                    FoldoutOption.Indent,
                    (serialized, owner) => s_PostProcessingWarningShown = false,
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

                DrawerPreset = CED.FoldoutGroup(
                    CameraUI.Rendering.Styles.header,
                    Expandable.Rendering,
                    k_ExpandedState,
                    FoldoutOption.Indent,
                    CED.Group(
                        CameraUI.Rendering.Drawer_Rendering_CullingMask,
                        CameraUI.Rendering.Drawer_Rendering_OcclusionCulling
                    )
                );
            }

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

                if (selectedRendererOption < -1 || selectedRendererOption >= rpAsset.m_RendererDataList.Length || p.renderer.hasMultipleDifferentValues)
                    return false;

                var rendererData = selectedRendererOption == -1 ? rpAsset.m_RendererData : rpAsset.m_RendererDataList[selectedRendererOption];

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

#if URP_EXPERIMENTAL_TAA_ENABLE
            static void DrawerRenderingTAAQuality(UniversalRenderPipelineSerializedCamera p, Editor owner)
            {
                var urpCamEditor = owner as UniversalRenderPipelineCameraEditor;
                if (urpCamEditor?.camera?.TryGetComponent<UniversalAdditionalCameraData>(out var urpAddCamData) != null)
                {
                    ref var taa = ref urpAddCamData.taaSettings;

                    var result = (TemporalAAQuality)EditorGUILayout.EnumPopup(Styles.antialiasingQuality, taa.quality);
                    if(result != taa.quality)
                        taa.quality = result;

                    taa.frameInfluence = EditorGUILayout.Slider( nameof(taa.frameInfluence), taa.frameInfluence, 0, 1);

                    var rect = GUILayoutUtility.GetRect(Styles.taaResetHistory, GUIStyle.none);
                    rect.x = rect.x + 32;
                    rect.width = rect.width - 32;
                    if (GUI.Button(rect, Styles.taaResetHistory))
                        taa.resetHistoryFrames += 2; // XR both eyes
                }
            }
#endif

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
