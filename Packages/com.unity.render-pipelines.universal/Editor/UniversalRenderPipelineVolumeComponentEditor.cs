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
    /// The default <see cref="VolumeComponentEditor"/> implementation for the Universal Render Pipeline (URP).
    /// This editor manages custom UI for volume components in the URP context. It handles rendering feature checks
    /// and UI warnings for missing features needed by the volume component.
    /// </summary>
    /// <remarks>
    /// This editor class is designed to work with volume components in Unity's URP. It extends the <see cref="VolumeComponentEditor"/>
    /// to customize the inspector UI for volume components and add checks for required renderer features.
    ///
    /// When using this editor, ensure that the relevant renderer features for the volume are enabled in the active URP
    /// renderer asset. If any required features are missing, a warning will be displayed with an option to open the
    /// relevant renderer settings.
    /// </remarks>
    /// <seealso cref="VolumeComponentEditor"/>
    /// <seealso cref="UniversalRenderPipelineAsset"/>
    /// <seealso cref="GraphicsSettings"/>
    /// <seealso cref="CoreEditorUtils"/>
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [CustomEditor(typeof(VolumeComponent), true)]
    public class UniversalRenderPipelineVolumeComponentEditor : VolumeComponentEditor
    {
        private VolumeRequiresRendererFeatures m_FeatureAttribute;

        /// <summary>
        /// Initializes the editor. It caches the <see cref="VolumeRequiresRendererFeatures"/> attribute from the target component.
        /// </summary>
        /// <remarks>
        /// The <see cref="OnEnable"/> method caches the renderer features attribute for the volume component. This ensures
        /// that UI elements relying on these features are updated efficiently. This method is called when the editor is first enabled.
        /// </remarks>
        /// <example>
        /// The <see cref="OnEnable"/> method is automatically called when the editor is initialized, making it suitable
        /// for setting up references and caching.
        /// </example>
        public override void OnEnable()
        {
            base.OnEnable();

            // Caching the attribute as UI code can be called multiple times in the same editor frame
            if (m_FeatureAttribute == null)
                m_FeatureAttribute = target.GetType().GetCustomAttribute<VolumeRequiresRendererFeatures>();
        }

        /// <summary>
        /// Retrieves the names of the missing renderer features as a formatted string.
        /// </summary>
        /// <param name="types">A set of <see cref="Type"/> objects representing the missing renderer features.</param>
        /// <returns>A formatted string containing the names of the missing feature types.</returns>
        /// <remarks>
        /// This helper method generates a space-separated string of the feature types that need to be added to the active
        /// URP renderer asset for the volume component to work correctly. The names of the missing feature types are
        /// appended to the string for display in the warning message.
        /// </remarks>
        private string GetFeatureTypeNames(in HashSet<Type> types)
        {
            var typeNameString = new StringBuilder();

            foreach (var type in types)
                typeNameString.AppendFormat("\"{0}\" ", type.Name);

            return typeNameString.ToString();
        }

        /// <summary>
        /// Displays the UI for the volume component and checks for missing renderer features required by the volume.
        /// </summary>
        /// <remarks>
        /// This method draws the inspector UI for the volume component and checks whether the necessary renderer features
        /// are enabled in the active URP asset. If any required features are missing, a warning box will be displayed.
        /// The warning includes a button that opens the active renderer asset for quick modification.
        /// </remarks>
        /// <example>
        /// The <see cref="OnBeforeInspectorGUI"/> method is called before rendering the inspector GUI for the volume component,
        /// ensuring that feature checks are performed and UI warnings are shown before displaying the inspector.
        /// </example>
        protected override void OnBeforeInspectorGUI()
        {
            if (m_FeatureAttribute == null)
                return;

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
                    CoreEditorUtils.DrawFixMeBox(
                        $"For this effect to work, the following renderer feature(s) need to be added and enabled on the active renderer asset: {GetFeatureTypeNames(in missingFeatureTypes)}",
                        MessageType.Warning,
                        "Open",
                        () =>
                        {
                            Selection.activeObject = UniversalRenderPipeline.asset.scriptableRendererData;
                            GUIUtility.ExitGUI();
                        });
                }
            }
        }
    }
}
