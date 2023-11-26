using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using StringBuilder = System.Text.StringBuilder;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// The default <see cref="VolumeComponentEditor"/> implementation for URP
    /// </summary>
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [CustomEditor(typeof(VolumeComponent), true)]
    public class UniversalRenderPipelineVolumeComponentEditor : VolumeComponentEditor
    {
        private VolumeRequiresRendererFeatures m_FeatureAttribute;

        /// <inheritdoc/>
        public override void OnEnable()
        {
            base.OnEnable();

            // Caching the attribute as UI code can be called multiple times in the same editor frame
            if (m_FeatureAttribute == null)
                m_FeatureAttribute = target.GetType().GetCustomAttribute<VolumeRequiresRendererFeatures>();
        }

        private string GetFeatureTypeNames(in HashSet<Type> types)
        {
            var typeNameString = new StringBuilder();

            foreach (var type in types)
                typeNameString.AppendFormat("\"{0}\" ", type.Name);

            return typeNameString.ToString();
        }

        /// <inheritdoc/>
        protected override void OnBeforeInspectorGUI()
        {
            if(m_FeatureAttribute != null)
            {
                var rendererFeatures = (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urpAsset && urpAsset.scriptableRendererData != null)
                    ? urpAsset.scriptableRendererData.rendererFeatures
                    : null;

                using (HashSetPool<Type>.Get(out var missingFeatureTypes))
                {
                    foreach (var elem in m_FeatureAttribute.TargetFeatureTypes)
                        missingFeatureTypes.Add(elem);

                    if (rendererFeatures != null)
                    {
                        foreach (var feature in rendererFeatures)
                        {
                            var featureType = feature.GetType();
                            if (missingFeatureTypes.Contains(featureType))
                            {
                                missingFeatureTypes.Remove(featureType);
                                if (missingFeatureTypes.Count == 0)
                                    break;
                            }
                        }
                    }

                    if (missingFeatureTypes.Count > 0)
                    {
                        CoreEditorUtils.DrawFixMeBox(string.Format("For this effect to work the {0}renderer feature(s) needs to be added and enabled on the active renderer asset",
                                GetFeatureTypeNames(in missingFeatureTypes)), MessageType.Warning, "Open", () =>
                        {
                            Selection.activeObject = UniversalRenderPipeline.asset.scriptableRendererData;
                            GUIUtility.ExitGUI();
                        });
                    }
                }
            }
        }
    }
}
