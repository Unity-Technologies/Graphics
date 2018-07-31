using System;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI
    {
        static readonly Color k_GizmoThemeColorExtent = new Color(255f / 255f, 229f / 255f, 148f / 255f, 80f / 255f);
        static readonly Color k_GizmoThemeColorExtentFace = new Color(255f / 255f, 229f / 255f, 148f / 255f, 45f / 255f);
        static readonly Color k_GizmoThemeColorInfluenceBlend = new Color(83f / 255f, 255f / 255f, 95f / 255f, 75f / 255f);
        static readonly Color k_GizmoThemeColorInfluenceBlendFace = new Color(83f / 255f, 255f / 255f, 95f / 255f, 17f / 255f);
        static readonly Color k_GizmoThemeColorInfluenceNormalBlend = new Color(0f / 255f, 229f / 255f, 255f / 255f, 80f / 255f);
        static readonly Color k_GizmoThemeColorInfluenceNormalBlendFace = new Color(0f / 255f, 229f / 255f, 255f / 255f, 36f / 255f);
        static readonly Color k_GizmoThemeColorProjection = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        static readonly Color k_GizmoThemeColorProjectionFace = new Color(0x00 / 255f, 0xE5 / 255f, 0xFF / 255f, 0x20 / 255f);
        static readonly Color k_GizmoThemeColorDisabled = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);
        static readonly Color k_GizmoThemeColorDisabledFace = new Color(0x99 / 255f, 0x89 / 255f, 0x59 / 255f, 0x10 / 255f);

        static readonly Color[][] k_handlesColor = new Color[][]
        {
            new Color[]
            {
                Color.red,
                Color.green,
                Color.blue
            },
            new Color[]
            {
                new Color(.5f, 0f, 0f, 1f),
                new Color(0f, .5f, 0f, 1f),
                new Color(0f, 0f, .5f, 1f)
            }
        };

        static readonly GUIContent bakeTypeContent = CoreEditorUtils.GetContent("Type|'Baked Cubemap' uses the 'Auto Baking' mode from the Lighting window. If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n'Custom' can be used if a custom cubemap is wanted. \n'Realtime' can be used to dynamically re-render the cubemap during runtime (via scripting).");

        static readonly GUIContent proxyVolumeContent = CoreEditorUtils.GetContent("Proxy Volume");
        static readonly GUIContent paralaxCorrectionContent = CoreEditorUtils.GetContent("Parallax Correction|Parallax Correction causes reflections to appear to change based on the object's position within the probe's box, while still using a single probe as the source of the reflection. This works well for reflections on objects that are moving through enclosed spaces such as corridors and rooms. Setting Parallax Correction to False and the cubemap reflection will be treated as coming from infinitely far away. Note that this feature can be globally disabled from Graphics Settings -> Tier Settings");

        static readonly GUIContent normalModeContent = CoreEditorUtils.GetContent("Normal|Normal parameters mode (only change for box shape).");
        static readonly GUIContent advancedModeContent = CoreEditorUtils.GetContent("Advanced|Advanced parameters mode (only change for box shape).");

        static readonly GUIContent shapeContent = CoreEditorUtils.GetContent("Shape");
        static readonly GUIContent boxSizeContent = CoreEditorUtils.GetContent("Box Size|The size of the box in which the reflections will be applied to objects. The value is not affected by the Transform of the Game Object.");
        static readonly GUIContent sphereRadiusContent = CoreEditorUtils.GetContent("Radius");
        static readonly GUIContent offsetContent = CoreEditorUtils.GetContent("Offset|The center of the InfluenceVolume in which the reflections will be applied to objects. The value is relative to the position of the Game Object.");
        static readonly GUIContent blendDistanceContent = CoreEditorUtils.GetContent("Blend Distance|Area around the probe where it is blended with other probes. Only used in deferred probes.");
        static readonly GUIContent blendNormalDistanceContent = CoreEditorUtils.GetContent("Blend Normal Distance|Area around the probe where the normals influence the probe. Only used in deferred probes.");
        static readonly GUIContent faceFadeContent = CoreEditorUtils.GetContent("Face fade|Fade faces of the cubemap.");

        protected static readonly GUIContent fieldCaptureTypeContent = CoreEditorUtils.GetContent("Type");
        protected static readonly GUIContent resolutionContent = CoreEditorUtils.GetContent("Resolution");
        protected static readonly GUIContent shadowDistanceContent = CoreEditorUtils.GetContent("Shadow Distance");
        protected static readonly GUIContent cullingMaskContent = CoreEditorUtils.GetContent("Culling Mask");
        protected static readonly GUIContent useOcclusionCullingContent = CoreEditorUtils.GetContent("Use Occlusion Culling");
        protected static readonly GUIContent nearClipCullingContent = CoreEditorUtils.GetContent("Near Clip");
        protected static readonly GUIContent farClipCullingContent = CoreEditorUtils.GetContent("Far Clip");

        static readonly GUIContent weightContent = CoreEditorUtils.GetContent("Weight|Blend weight applied on this reflection probe. This can be used for fading in or out a reflection probe.");
        static readonly GUIContent multiplierContent = CoreEditorUtils.GetContent("Intensity Multiplier|Allows you to boost or dimmer the reflected cubemap. Values above 1 will make reflections brighter and values under 1 will make reflections darker. Using values different than 1 is not physically correct.");

        static readonly GUIContent textureSizeContent = CoreEditorUtils.GetContent("Probe Texture Size (Set By HDRP)");
        static readonly GUIContent compressionTextureContent = CoreEditorUtils.GetContent("Probe Compression (Set By HDRP)");
        

        const string mimapHelpBoxText = "No mipmaps in the cubemap, Smoothness value in Standard shader will be ignored.";
        const string noProxyHelpBoxText = "When no Proxy setted, Influence shape will be used as Proxy shape too.";
        const string proxyInfluenceShapeMismatchHelpBoxText = "Proxy volume and influence volume have different shapes, this is not supported.";

        const string proxySettingsHeader = "Proxy Volume";
        //influenceVolume have its own header
        const string captureSettingsHeader = "Capture Settings";
        const string additionnalSettingsHeader = "Custom Settings";

        static GUIContent[] s_InfluenceToolbar_Contents = null;
        protected static GUIContent[] influenceToolbar_Contents
        {
            get
            {
                return s_InfluenceToolbar_Contents ?? (s_InfluenceToolbar_Contents = new[]
                {
                    EditorGUIUtility.IconContent("EditCollider", "|Modify the base shape. (SHIFT+1)"),
                    EditorGUIUtility.IconContent("PreMatCube", "|Modify the influence volume. (SHIFT+2)"),
                    EditorGUIUtility.IconContent("SceneViewOrtho", "|Modify the influence normal volume. (SHIFT+3)"),
                });
            }
        }


        //[TODO]extract in HDReflectionProbe?
        static GUIContent[] s_CaptureToolbar_Contents = null;
        protected static GUIContent[] captureToolbar_Contents
        {
            get
            {
                return s_CaptureToolbar_Contents ?? (s_CaptureToolbar_Contents = new[]
                {
                    EditorGUIUtility.IconContent("MoveTool", "|Change the Offset of the shape.")
                });
            }
        }

    }
}
