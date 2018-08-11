using System;
using System.Collections.Generic;
using UnityEditor.AnimatedValues;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI
    {
        /*
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
        */

        //static readonly GUIContent bakeTypeContent = CoreEditorUtils.GetContent("Type|'Baked Cubemap' uses the 'Auto Baking' mode from the Lighting window. If it is enabled then baking is automatic otherwise manual bake is needed (use the bake button below). \n'Custom' can be used if a custom cubemap is wanted. \n'Realtime' can be used to dynamically re-render the cubemap during runtime (via scripting).");

        static readonly GUIContent proxyVolumeContent = CoreEditorUtils.GetContent("Proxy Volume");
        protected static readonly GUIContent useInfiniteProjectionContent = CoreEditorUtils.GetContent("Same As Influence Volume|If enabled, parallax correction will occure, causing reflections to appear to change based on the object's position within the probe's box, while still using a single probe as the source of the reflection. This works well for reflections on objects that are moving through enclosed spaces such as corridors and rooms. When disabled, the cubemap reflection will be treated as coming from infinitely far away. Note that this feature can be globally disabled from Graphics Settings -> Tier Settings");

        //static readonly GUIContent normalModeContent = CoreEditorUtils.GetContent("Normal|Normal parameters mode (only change for box shape).");
        // static readonly GUIContent advancedModeContent = CoreEditorUtils.GetContent("Advanced|Advanced parameters mode (only change for box shape).");

        protected static readonly GUIContent fieldCaptureTypeContent = CoreEditorUtils.GetContent("Type");
        protected static readonly GUIContent resolutionContent = CoreEditorUtils.GetContent("Resolution");
        protected static readonly GUIContent shadowDistanceContent = CoreEditorUtils.GetContent("Shadow Distance");
        protected static readonly GUIContent cullingMaskContent = CoreEditorUtils.GetContent("Culling Mask");
        protected static readonly GUIContent useOcclusionCullingContent = CoreEditorUtils.GetContent("Use Occlusion Culling");
        protected static readonly GUIContent nearClipCullingContent = CoreEditorUtils.GetContent("Near Clip");
        protected static readonly GUIContent farClipCullingContent = CoreEditorUtils.GetContent("Far Clip");

        static readonly GUIContent weightContent = CoreEditorUtils.GetContent("Weight|Blend weight applied on this reflection probe. This can be used for fading in or out a reflection probe.");
        static readonly GUIContent multiplierContent = CoreEditorUtils.GetContent("Intensity Multiplier|Allows you to boost or dimmer the reflected cubemap. Values above 1 will make reflections brighter and values under 1 will make reflections darker. Using values different than 1 is not physically correct.");
        static readonly GUIContent lightLayersContent = CoreEditorUtils.GetContent("Light Layers|Specifies the current light layers that the light affect. Corresponding renderer with the same flags will be lit by this light.");

        //static readonly GUIContent textureSizeContent = CoreEditorUtils.GetContent("Probe Texture Size (Set By HDRP)");
        //static readonly GUIContent compressionTextureContent = CoreEditorUtils.GetContent("Probe Compression (Set By HDRP)");
        

        const string mimapHelpBoxText = "No mipmaps in the cubemap, Smoothness value in Standard shader will be ignored.";
        const string noProxyHelpBoxText = "Influence shape will be used as Projection shape too.";
        const string noProxyInfiniteHelpBoxText = "Projection will be at infinite.";
        const string proxyInfluenceShapeMismatchHelpBoxText = "Proxy volume and influence volume have different shapes, this is not supported.";

        const string proxySettingsHeader = "Projection Settings";
        //influenceVolume have its own header
        protected const string captureSettingsHeader = "Capture Settings";
        const string additionnalSettingsHeader = "Custom Settings";

        static Dictionary<ToolBar, GUIContent> s_Toolbar_Contents = null;
        protected static Dictionary<ToolBar, GUIContent> toolbar_Contents
        {
            get
            {
                return s_Toolbar_Contents ?? (s_Toolbar_Contents = new Dictionary<ToolBar, GUIContent>
                {
                    { ToolBar.InfluenceShape,  EditorGUIUtility.IconContent("EditCollider", "|Modify the base shape. (SHIFT+1)") },
                    { ToolBar.Blend,  EditorGUIUtility.IconContent("PreMatCube", "|Modify the influence volume. (SHIFT+2)") },
                    { ToolBar.NormalBlend,  EditorGUIUtility.IconContent("SceneViewOrtho", "|Modify the influence normal volume. (SHIFT+3)") },
                    { ToolBar.CapturePosition,  EditorGUIUtility.IconContent("MoveTool", "|Change the Offset of the shape.") }
                });
            }
        }
    }
}
