using System;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using _ = CoreEditorUtils;
    using CED = CoreEditorDrawer<PlanarReflectionProbeUI, SerializedPlanarReflectionProbe>;

    partial class PlanarReflectionProbeUI
    {
        public static readonly CED.IDrawer Inspector;

        public static readonly CED.IDrawer SectionProbeModeSettings;
        public static readonly CED.IDrawer SectionProbeModeBakedSettings = CED.noop;
        public static readonly CED.IDrawer SectionProbeModeCustomSettings = CED.Action(Drawer_SectionProbeModeCustomSettings);
        public static readonly CED.IDrawer SectionProbeModeRealtimeSettings = CED.Action(Drawer_SectionProbeModeRealtimeSettings);
        public static readonly CED.IDrawer SectionBakeButton = CED.Action(Drawer_SectionBakeButton);

        public static readonly CED.IDrawer SectionFoldoutInfluenceSettings = CED.FoldoutGroup(
            "Influence Settings",
            (s, d, o) => s.isSectionExpandedInfluenceSettings,
            true,
            CED.Action(Drawer_SectionInfluenceSettings)
        );

        public static readonly CED.IDrawer SectionFoldoutCaptureSettings = CED.FoldoutGroup(
            "Capture Settings",
            (s, d, o) => s.isSectionExpandedCaptureSettings,
            true,
            CED.Action(Drawer_SectionCaptureSettings)
        );

        static PlanarReflectionProbeUI()
        {
            SectionProbeModeSettings = CED.Group(
                CED.Action(Drawer_FieldCaptureType),
                CED.FadeGroup(
                    (s, d, o, i) => s.IsSectionExpandedReflectionProbeMode((ReflectionProbeMode)i),
                    true,
                    SectionProbeModeBakedSettings,
                    SectionProbeModeRealtimeSettings,
                    SectionProbeModeCustomSettings
                )
            );

            Inspector = CED.Group(
                SectionProbeModeSettings,
                CED.space,
                CED.Action((s, d, o) => EditorGUILayout.LabelField(_.GetContent("Proxy Volume"), EditorStyles.boldLabel)),
                CED.Action(Drawer_FieldProxyVolumeReference),
                CED.space,
                CED.Action(Drawer_Toolbar),
                CED.space,
                CED.Select(
                    (s, d, o) => s.influenceVolume,
                    (s, d, o) => d.influenceVolume,
                    InfluenceVolumeUI.SectionFoldoutShape
                ),
                SectionFoldoutInfluenceSettings,
                SectionFoldoutCaptureSettings,
                CED.Select(
                    (s, d, o) => s.frameSettings,
                    (s, d, o) => d.frameSettings,
                    FrameSettingsUI.Inspector
                ),
                CED.space,
                CED.Action(Drawer_SectionBakeButton)
            );
        }

        const EditMode.SceneViewEditMode EditBaseShape = EditMode.SceneViewEditMode.ReflectionProbeBox;
        const EditMode.SceneViewEditMode EditInfluenceShape = EditMode.SceneViewEditMode.GridBox;
        const EditMode.SceneViewEditMode EditInfluenceNormalShape = EditMode.SceneViewEditMode.Collider;
        const EditMode.SceneViewEditMode EditCenter = EditMode.SceneViewEditMode.ReflectionProbeOrigin; 

        static void Drawer_SectionCaptureSettings(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            var hdrp = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            GUI.enabled = false;
            EditorGUILayout.LabelField(
                _.GetContent("Probe Texture Size (Set By HDRP)"),
                _.GetContent(hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionTextureSize.ToString()),
                EditorStyles.label);
            EditorGUILayout.Toggle(
                _.GetContent("Probe Compression (Set By HDRP)"),
                hdrp.renderPipelineSettings.lightLoopSettings.planarReflectionCacheCompressed);
            GUI.enabled = true;

            EditorGUILayout.PropertyField(d.captureLocalPosition, _.GetContent("Capture Local Position"));

            _.DrawMultipleFields(
                "Clipping Planes",
                new[] { d.captureNearPlane, d.captureFarPlane },
                new[] { _.GetContent("Near|The closest point relative to the camera that drawing will occur."), _.GetContent("Far|The furthest point relative to the camera that drawing will occur.\n") });
        }

        static void Drawer_SectionProbeModeCustomSettings(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            d.customTexture.objectReferenceValue = EditorGUILayout.ObjectField(_.GetContent("Capture"), d.customTexture.objectReferenceValue, typeof(Texture), false);
            var texture = d.customTexture.objectReferenceValue as Texture;
            if (texture != null && texture.dimension != TextureDimension.Tex2D)
                EditorGUILayout.HelpBox("Provided Texture is not a 2D Texture, it will be ignored", MessageType.Warning);
        }

        static void Drawer_SectionBakeButton(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            EditorReflectionSystemGUI.DrawBakeButton((ReflectionProbeMode)d.mode.intValue, d.target);
        }

        static void Drawer_SectionProbeModeRealtimeSettings(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.refreshMode, _.GetContent("Refresh Mode"));
        }

        static void Drawer_SectionInfluenceSettings(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.dimmer, _.GetContent("Dimmer"));
        }

        static void Drawer_FieldCaptureType(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.mode, _.GetContent("Type"));
        }

        static void Drawer_FieldProxyVolumeReference(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            EditorGUILayout.PropertyField(d.proxyVolumeReference, _.GetContent("Reference"));
        }

        static readonly EditMode.SceneViewEditMode[] k_Toolbar_SceneViewEditModes =
        {
            EditBaseShape,
            EditInfluenceShape,
            EditInfluenceNormalShape,
            EditCenter
        };
        static GUIContent[] s_Toolbar_Contents = null;
        static GUIContent[] toolbar_Contents
        {
            get
            {
                return s_Toolbar_Contents ?? (s_Toolbar_Contents = new[]
                {
                    EditorGUIUtility.IconContent("EditCollider", "|Modify the base shape. (SHIFT+1)"),
                    EditorGUIUtility.IconContent("PreMatCube", "|Modify the influence volume. (SHIFT+2)"),
                    EditorGUIUtility.IconContent("SceneViewOrtho", "|Modify the influence normal volume. (SHIFT+3)"),
                    EditorGUIUtility.IconContent("MoveTool", "|Move the capture position.")
                });
            }
        }
        static void Drawer_Toolbar(PlanarReflectionProbeUI s, SerializedPlanarReflectionProbe d, Editor o)
        {
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUI.changed = false;

            EditMode.DoInspectorToolbar(k_Toolbar_SceneViewEditModes, toolbar_Contents, GetBoundsGetter(o), o);

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }

        static Func<Bounds> GetBoundsGetter(Editor o)
        {
            return () =>
            {
                var bounds = new Bounds();
                foreach (Component targetObject in o.targets)
                {
                    var rp = targetObject.transform;
                    var b = rp.position;
                    bounds.Encapsulate(b);
                }
                return bounds;
            };
        }
    }
}
