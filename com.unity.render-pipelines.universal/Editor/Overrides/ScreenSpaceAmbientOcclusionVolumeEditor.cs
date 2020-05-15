using System;
using UnityEngine.Rendering.Universal;
using UnityEditor.Experimental.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [VolumeComponentEditor(typeof(ScreenSpaceAmbientOcclusionVolume))]
    sealed class ScreenSpaceAmbientOcclusionVolumeEditor : VolumeComponentEditor
    {
        // Private Fields
        private UniversalRenderPipelineAsset asset = UniversalRenderPipeline.asset;
        private SerializedDataParameter m_Quality;
        private SerializedDataParameter m_Downsample;
        //private SerializedDataParameter m_DepthSource;
        private SerializedDataParameter m_NormalSamples;
        private SerializedDataParameter m_Intensity;
        private SerializedDataParameter m_Radius;
        private SerializedDataParameter m_SampleCount;
        private SerializedDataParameter m_BlurPasses;

        // Constants
        private const string k_Message = "Remember to make sure your renderer has Screen Space Ambient Occlusion renderer feature.";

        public override void OnEnable()
        {
            PropertyFetcher<ScreenSpaceAmbientOcclusionVolume> volume = new PropertyFetcher<ScreenSpaceAmbientOcclusionVolume>(serializedObject);

            m_Quality = Unpack(volume.Find(x => x.Quality));
            m_Downsample = Unpack(volume.Find(x => x.Downsample));
            //m_DepthSource = Unpack(volume.Find(x => x.DepthSource));
            m_NormalSamples = Unpack(volume.Find(x => x.NormalSamples));
            m_Intensity = Unpack(volume.Find(x => x.Intensity));
            m_Radius = Unpack(volume.Find(x => x.Radius));
            m_SampleCount = Unpack(volume.Find(x => x.SampleCount));
            m_BlurPasses = Unpack(volume.Find(x => x.BlurPasses));
        }

        private void DrawPropertyFields()
        {
            PropertyField(m_Quality, ScreenSpaceAmbientOcclusionEditor.Styles.Quality);
            PropertyField(m_Downsample, ScreenSpaceAmbientOcclusionEditor.Styles.Downsample);
            //PropertyField(m_DepthSource, ScreenSpaceAmbientOcclusionFeatureEditor.Styles.DepthSource);
            //if (m_DepthSource == DepthSource.Depth)
            {
                PropertyField(m_NormalSamples, ScreenSpaceAmbientOcclusionEditor.Styles.NormalSamples);
            }
            PropertyField(m_Intensity, ScreenSpaceAmbientOcclusionEditor.Styles.Intensity);
            PropertyField(m_Radius, ScreenSpaceAmbientOcclusionEditor.Styles.Radius);
            PropertyField(m_SampleCount, ScreenSpaceAmbientOcclusionEditor.Styles.SampleCount);
            PropertyField(m_BlurPasses, ScreenSpaceAmbientOcclusionEditor.Styles.BlurPasses);
        }

        private void OnInspectorGUIAsset(ScreenSpaceAmbientOcclusion.Quality prevQuality)
        {
            if (asset == null)
            {
                asset = UniversalRenderPipeline.asset;
                if (asset == null)
                {
                    EditorGUILayout.HelpBox("Missing Universal Render Pipeline Asset", MessageType.Warning);
                    return;
                }
            }

            EditorGUI.BeginChangeCheck();

            // Get the current settings
            ScreenSpaceAmbientOcclusion.Parameters prevParams = new ScreenSpaceAmbientOcclusion.Parameters
            {
                Downsample = m_Downsample.value.boolValue,
                NormalSamples = (ScreenSpaceAmbientOcclusion.NormalSamples) m_NormalSamples.value.enumValueIndex,
                Intensity = m_Intensity.value.floatValue,
                Radius = m_Radius.value.floatValue,
                SampleCount = m_SampleCount.value.intValue,
                BlurPasses = m_BlurPasses.value.intValue
            };

            // Copy the settings from the asset to our properties
            ScreenSpaceAmbientOcclusion.Parameters parameters = asset.GetSSAOParameters(prevQuality);
            m_Downsample.value.boolValue = parameters.Downsample;
            m_NormalSamples.value.enumValueIndex = (int) parameters.NormalSamples;
            m_Intensity.value.floatValue = parameters.Intensity;
            m_Radius.value.floatValue = parameters.Radius;
            m_SampleCount.value.intValue = parameters.SampleCount;
            m_BlurPasses.value.intValue = parameters.BlurPasses;

            // Draw the property fields
            DrawPropertyFields();

            // Did the user change anything?
            if (EditorGUI.EndChangeCheck())
            {
                ScreenSpaceAmbientOcclusion.Quality curQuality = (ScreenSpaceAmbientOcclusion.Quality) m_Quality.value.enumValueIndex;

                // If user made a change to something other than the quality setting...
                if (prevQuality == curQuality)
                {
                    // Check if the user changed a property and ignore changes to toggling a property on/off
                    bool hasChangedAParam = m_Downsample.value.boolValue != parameters.Downsample;
                    hasChangedAParam |= m_NormalSamples.value.enumValueIndex != (int) parameters.NormalSamples;
                    hasChangedAParam |= Math.Abs(m_Intensity.value.floatValue - parameters.Intensity) > 0.01f;
                    hasChangedAParam |= Math.Abs(m_Radius.value.floatValue - parameters.Radius) > 0.01f;
                    hasChangedAParam |= m_SampleCount.value.intValue != parameters.SampleCount;
                    hasChangedAParam |= m_BlurPasses.value.intValue != parameters.BlurPasses;

                    if (hasChangedAParam)
                    {
                        m_Quality.overrideState.boolValue = true;
                        m_Quality.value.enumValueIndex = (int) ScreenSpaceAmbientOcclusion.Quality.Custom;
                        return;
                    }
                }
            }

            // We revert the values so the user can switch to
            // custom quality without losing his changes...
            m_Downsample.value.boolValue = prevParams.Downsample;
            m_NormalSamples.value.enumValueIndex = (int) prevParams.NormalSamples;
            m_Intensity.value.floatValue = prevParams.Intensity;
            m_Radius.value.floatValue = prevParams.Radius;
            m_SampleCount.value.intValue = prevParams.SampleCount;
            m_BlurPasses.value.intValue = prevParams.BlurPasses;
        }

        private void OnInspectorGUICustom()
        {
            // Draw the property fields
            DrawPropertyFields();
        }

        public override void OnInspectorGUI()
        {
            // Store the current quality setting
            ScreenSpaceAmbientOcclusion.Quality prevQuality = (ScreenSpaceAmbientOcclusion.Quality) m_Quality.value.enumValueIndex;
            m_Quality.overrideState.boolValue = true;

            // Draw based on the quality setting
            if (prevQuality == ScreenSpaceAmbientOcclusion.Quality.Custom)
            {
                OnInspectorGUICustom();
            }
            else
            {
                OnInspectorGUIAsset(prevQuality);
            }

            // If the user changed the quality setting or is in custom mode, then we need to update the overrides
            ScreenSpaceAmbientOcclusion.Quality curQuality = (ScreenSpaceAmbientOcclusion.Quality) m_Quality.value.enumValueIndex;
            bool isInCustomQuality = curQuality == ScreenSpaceAmbientOcclusion.Quality.Custom;
            bool updateOverrides = isInCustomQuality || prevQuality != curQuality;
            if (updateOverrides)
            {
                m_Downsample.overrideState.boolValue = isInCustomQuality;
                m_NormalSamples.overrideState.boolValue = isInCustomQuality;
                m_Intensity.overrideState.boolValue = isInCustomQuality;
                m_Radius.overrideState.boolValue = isInCustomQuality;
                m_SampleCount.overrideState.boolValue = isInCustomQuality;
                m_BlurPasses.overrideState.boolValue = isInCustomQuality;
            }

            EditorGUILayout.Space(5f);
            EditorGUILayout.HelpBox(k_Message, MessageType.Info);
        }
    }
}
