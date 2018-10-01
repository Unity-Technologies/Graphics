namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using UnityEngine;
    using UnityEngine.Experimental.Rendering.HDPipeline;
    using UnityEngine.Rendering;
    using CED = CoreEditorDrawer<HDProbeUI, SerializedHDProbe>;

    partial class PlanarReflectionProbeUI : HDProbeUI
    {
        static readonly GUIContent overrideFieldOfViewContent = CoreEditorUtils.GetContent("Override Field Of View");
        static readonly GUIContent fieldOfViewSolidAngleContent = CoreEditorUtils.GetContent("Field Of View");

        public static CED.IDrawer Inspector;
       
        public static readonly CED.IDrawer SectionFoldoutCaptureSettings = CED.FoldoutGroup(
            "Capture Settings",
            (s, d, o) => s.isSectionExpandedCaptureSettings,
            FoldoutOption.Indent,
            CED.Action(Drawer_SectionCaptureSettings)
            );

        static PlanarReflectionProbeUI()
        {
            Inspector = CED.Group(
                  CED.Action(Drawer_Toolbars),
                  CED.space,
                  ProxyVolumeSettings,
                  CED.Select(
                      (s, d, o) => s.influenceVolume,
                      (s, d, o) => d.influenceVolume,
                      InfluenceVolumeUI.SectionFoldoutShapePlanar
                      ),
                  CED.Action(Drawer_DifferentShapeError),
                  SectionFoldoutCaptureSettings,
                  SectionFoldoutAdditionalSettings,
                  CED.Select(
                      (s, d, o) => s.frameSettings,
                      (s, d, o) => d.frameSettings,
                      FrameSettingsUI.Inspector
                      ),
                  CED.space,
                  CED.Action(Drawer_SectionBakeButton)
                  );
        }

        protected static void Drawer_SectionCaptureSettings(HDProbeUI s, SerializedHDProbe d, Editor o)
        {
            SerializedPlanarReflectionProbe serialized = (SerializedPlanarReflectionProbe)d;
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            GUI.enabled = false;
            EditorGUILayout.LabelField(
                CoreEditorUtils.GetContent("Probe Texture Size (Set By HDRP)"),
                CoreEditorUtils.GetContent(hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize.ToString()),
                EditorStyles.label);
            EditorGUILayout.Toggle(
                CoreEditorUtils.GetContent("Probe Compression (Set By HDRP)"),
                hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed);
            GUI.enabled = true;

            bool on = serialized.overrideFieldOfView.boolValue;
            EditorGUI.BeginChangeCheck();
            on = EditorGUILayout.Toggle(overrideFieldOfViewContent, on);
            if (on)
            {
                serialized.fieldOfViewOverride.floatValue = EditorGUILayout.FloatField(fieldOfViewSolidAngleContent, serialized.fieldOfViewOverride.floatValue);
            }
            if (EditorGUI.EndChangeCheck())
            {
                serialized.overrideFieldOfView.boolValue = on;
                serialized.Apply();
            }

            //GUI.enabled = false;
            //EditorGUILayout.LabelField(resolutionContent, CoreEditorUtils.GetContent(((int)hdrp.GetRenderPipelineSettings().lightLoopSettings.reflectionCubemapSize).ToString()));
            //EditorGUILayout.LabelField(shadowDistanceContent, EditorStyles.label);
            //EditorGUILayout.LabelField(cullingMaskContent, EditorStyles.label);
            //EditorGUILayout.LabelField(useOcclusionCullingContent, EditorStyles.label);
            //EditorGUILayout.LabelField(nearClipCullingContent, EditorStyles.label);
            //EditorGUILayout.LabelField(farClipCullingContent, EditorStyles.label);
            //GUI.enabled = true;
        }

        internal PlanarReflectionProbeUI()
        {
            toolBars = new[] { ToolBar.InfluenceShape | ToolBar.Blend };
        }

        public override void Update()
        {
            SerializedPlanarReflectionProbe serialized = data as SerializedPlanarReflectionProbe;
            isSectionExpandedCaptureMirrorSettings.target = serialized.isMirrored;
            isSectionExpandedCaptureStaticSettings.target = !serialized.isMirrored;
            base.Update();
        }
    }
}
