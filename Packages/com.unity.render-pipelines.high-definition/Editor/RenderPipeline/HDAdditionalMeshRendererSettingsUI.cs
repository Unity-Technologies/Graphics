using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDAdditionalMeshRendererSettings>;

    internal class HDAdditionalMeshRendererSettingsUI
    {
        public static class Styles
        {
            public static GUIContent lineRenderingHeaderContent { get; } = L10n.TextContent("High Quality Line Rendering", null, null, null);
            public static GUIContent enableLineRenderingContent { get; } = L10n.TextContent("Enable", null, null, null);
            public static GUIContent lineRenderingGroup { get; } = L10n.TextContent("Group", null, null, null);
            public static GUIContent lineRenderingLODMode { get; } = L10n.TextContent("LOD Mode", null, null, null);
            public static GUIContent lineRenderingLODFixed { get; } = L10n.TextContent("Fixed LOD", null, null, null);

            public static GUIContent lineRenderingCameraDistanceLODCurve { get; } = L10n.TextContent("Camera Distance LOD", null, null, null);

            public static GUIContent lineRenderingScreenCoverageLODCurve { get; } = L10n.TextContent("Screen Coverage LOD", null, null, null);
            public static GUIContent lineRenderingShadingSampleFraction { get; } = L10n.TextContent("Shading Fraction", null, null, null);
        }

        public enum Expandable
        {
            /// <summary> Line Rendering </summary>
            LineRendering = 1 << 0,
        }

        private static readonly ExpandedState<Expandable, HDAdditionalMeshRendererSettings> k_ExpandedState = new ExpandedState<Expandable, HDAdditionalMeshRendererSettings>(Expandable.LineRendering, "HDRP");

        internal static void Drawer_LineRendering(SerializedHDAdditionalMeshRendererSettings serialized, Editor owner)
        {
            var meshRendererExtension = serialized.serializedObject.targetObject as HDAdditionalMeshRendererSettings;
            var meshRenderer          = meshRendererExtension.GetComponent<MeshRenderer>();
            var meshFilter            = meshRendererExtension.GetComponent<MeshFilter>();
            var material              = meshRenderer.sharedMaterial;

            // Validate HDRP Asset.
            var currentAsset = HDRenderPipeline.currentAsset;
            if (!currentAsset?.currentPlatformRenderPipelineSettings.supportHighQualityLineRendering ?? false)
            {
                EditorGUILayout.Space();

                HDEditorUtils.QualitySettingsHelpBox("Enable 'High Quality Line Rendering' in your HDRP Asset to render lines with high quality anti-aliasing and transparency in your HDRP project.",
                    MessageType.Info,
                    HDRenderPipelineUI.ExpandableGroup.Rendering,
                    HDRenderPipelineUI.ExpandableRendering.HighQualityLineRendering, "m_RenderPipelineSettings.supportHighQualityLineRendering");

                return;
            }

            // Validate topology.
            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                if (meshFilter.sharedMesh.GetTopology(0) != MeshTopology.Lines)
                {
                    EditorGUILayout.HelpBox("A Mesh Filter with Line Topology is required to enable High Quality Line Rendering for this Mesh Renderer.", MessageType.Info);

                    return;
                }
            }

            // Validate material.
            if (material != null)
            {
                if (!HDAdditionalMeshRendererSettings.IsLineRenderingMaterial(material))
                {
                    EditorGUILayout.HelpBox(
                        "Enable 'Support High Quality Line Rendering' in your Material's Shader Graph inspector to render lines with high quality anti-aliasing and transparency in your HDRP project.",
                        MessageType.Error);

                    return;
                }
            }

            EditorGUILayout.PropertyField(serialized.enableHighQualityLineRendering, Styles.enableLineRenderingContent);

            if (serialized.enableHighQualityLineRendering.boolValue)
            {
                using (new EditorGUI.IndentLevelScope())
                {
                    EditorGUILayout.PropertyField(serialized.rendererGroup, Styles.lineRenderingGroup);
                    EditorGUILayout.PropertyField(serialized.rendererLODMode, Styles.lineRenderingLODMode);

                    if (serialized.rendererLODMode.intValue != 0)
                    {
                        using (new EditorGUI.IndentLevelScope())
                        {
                            switch (serialized.rendererLODMode.enumValueIndex)
                            {
                                case (int)LineRendering.RendererLODMode.Fixed:
                                    EditorGUILayout.PropertyField(serialized.rendererLODFixed, Styles.lineRenderingLODFixed);
                                    break;
                                case (int)LineRendering.RendererLODMode.CameraDistance:
                                    EditorGUILayout.PropertyField(serialized.rendererLODCameraDistanceCurve, Styles.lineRenderingCameraDistanceLODCurve);
                                    break;
                                case (int)LineRendering.RendererLODMode.ScreenCoverage:
                                    EditorGUILayout.PropertyField(serialized.rendererLODScreenCoverageCurve, Styles.lineRenderingScreenCoverageLODCurve);
                                    break;
                            }
                        }
                    }

                    EditorGUILayout.PropertyField(serialized.shadingSampleFraction, Styles.lineRenderingShadingSampleFraction);
                }
            }
        }

        internal static readonly CED.IDrawer SectionLineRendering = CED.FoldoutGroup(
            Styles.lineRenderingHeaderContent,
            Expandable.LineRendering,
            k_ExpandedState,
            FoldoutOption.Indent,
            CED.Group(
                Drawer_LineRendering
            )
        );

        internal static readonly CED.IDrawer[] Inspector = new[]
        {
            SectionLineRendering
        };
    }
}
