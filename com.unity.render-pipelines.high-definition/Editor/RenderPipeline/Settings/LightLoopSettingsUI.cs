using UnityEditor.AnimatedValues;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    using CED = CoreEditorDrawer<SerializedLightLoopSettings>;
    

    static class LightLoopSettingsUI
    {
        enum Expandable
        {
            LightLoop = 1 << 0,
        }

        readonly static ExpandedState<Expandable, LightLoopSettings> k_ExpandedState = new ExpandedState<Expandable, LightLoopSettings>(Expandable.LightLoop, "HDRP");
        
        static readonly GUIContent lightLoopSettingsHeaderContent = EditorGUIUtility.TrTextContent("Light Loop Settings");

        static readonly GUIContent tileAndClusterContent = EditorGUIUtility.TrTextContent("Enable Deferred Tile And Cluster");
        static readonly GUIContent fptlForForwardOpaqueContent = EditorGUIUtility.TrTextContent("FPTL For Forward Opaque");
        static readonly GUIContent bigTilePrepassContent = EditorGUIUtility.TrTextContent("Big Tile Prepass");
        static readonly GUIContent computeLightEvaluationContent = EditorGUIUtility.TrTextContent("Compute Light Evaluation");
        static readonly GUIContent computeLightVariantsContent = EditorGUIUtility.TrTextContent("Compute Light Variants");
        static readonly GUIContent computeMaterialVariantsContent = EditorGUIUtility.TrTextContent("Compute Material Variants");
        
        public static CED.IDrawer SectionLightLoopSettings(bool withOverride)
        {
            return CED.FoldoutGroup(
                lightLoopSettingsHeaderContent,
                Expandable.LightLoop,
                k_ExpandedState,
                FoldoutOption.Indent | FoldoutOption.Boxed,
                CED.Group(250, (serialized, owner) => Drawer_SectionLightLoopSettings(serialized, owner, withOverride))
            );
        }

        static void Drawer_SectionLightLoopSettings(SerializedLightLoopSettings serialized, Editor owner, bool withOverride)
        {
            //disable temporarily as FrameSettings are not supported for Baked probe at the moment
            using (new EditorGUI.DisabledScope((owner is IHDProbeEditor) && (owner as IHDProbeEditor).GetTarget(owner.target).mode != ProbeSettings.Mode.Realtime || (owner is HDRenderPipelineEditor) && HDRenderPipelineUI.selectedFrameSettings == HDRenderPipelineUI.SelectedFrameSettings.BakedOrCustomReflection))
            {
                //RenderPipelineSettings hdrpSettings = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).renderPipelineSettings;
                OverridableSettingsArea area = new OverridableSettingsArea(6);
                FrameSettings defaultFrameSettings = FrameSettingsUI.GetDefaultFrameSettingsFor(owner);

                area.Add(serialized.enableFptlForForwardOpaque, fptlForForwardOpaqueContent, () => serialized.overridesFptlForForwardOpaque, a => serialized.overridesFptlForForwardOpaque = a, defaultValue: defaultFrameSettings.lightLoopSettings.enableFptlForForwardOpaque);
                area.Add(serialized.enableBigTilePrepass, bigTilePrepassContent, () => serialized.overridesBigTilePrepass, a => serialized.overridesBigTilePrepass = a, defaultValue: defaultFrameSettings.lightLoopSettings.enableBigTilePrepass);

                area.Add(serialized.enableDeferredTileAndCluster, tileAndClusterContent, () => serialized.overridesTileAndCluster, a => serialized.overridesTileAndCluster = a);

                if (serialized.enableDeferredTileAndCluster.boolValue)
                {
                    area.Add(serialized.enableComputeLightEvaluation, computeLightEvaluationContent, () => serialized.overridesComputeLightEvaluation, a => serialized.overridesComputeLightEvaluation = a, defaultValue: defaultFrameSettings.lightLoopSettings.enableComputeLightEvaluation, indent: 1);
                    if (serialized.enableComputeLightEvaluation.boolValue)
                    {
                        area.Add(serialized.enableComputeLightVariants, computeLightVariantsContent, () => serialized.overridesComputeLightVariants, a => serialized.overridesComputeLightVariants = a, defaultValue: defaultFrameSettings.lightLoopSettings.enableComputeLightVariants, indent: 2);
                        area.Add(serialized.enableComputeMaterialVariants, computeMaterialVariantsContent, () => serialized.overridesComputeMaterialVariants, a => serialized.overridesComputeMaterialVariants = a, defaultValue: defaultFrameSettings.lightLoopSettings.enableComputeMaterialVariants, indent: 2);
                    }
                }

                area.Draw(withOverride);
            }
        }
    }
}
