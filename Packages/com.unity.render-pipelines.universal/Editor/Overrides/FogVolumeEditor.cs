#if VOLUMETRIC_FOG

using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(FogVolumeComponent))]
    sealed class FogVolumeEditor : VolumeComponentEditor
    {
        SerializedDataParameter m_Color;
        SerializedDataParameter m_MeanFreePath;
        SerializedDataParameter m_BaseHeight;
        SerializedDataParameter m_MaximumHeight;
        SerializedDataParameter m_MaxFogDistance;
        SerializedDataParameter m_Anisotropy;
        SerializedDataParameter m_DepthExtent;
        SerializedDataParameter m_ScreenResolutionPercentage;
        SerializedDataParameter m_VolumeSliceCount;
        SerializedDataParameter m_SliceDistributionUniformity;
        SerializedDataParameter m_DenoisingMode;
        SerializedDataParameter m_VolumetricLightingDensityCutoff;
        SerializedDataParameter m_EnableLightCookies;
        SerializedDataParameter m_LightFilter;
        SerializedDataParameter m_MultipleScatteringIntensity;

        public override void OnEnable()
        {
            var o = new PropertyFetcher<FogVolumeComponent>(serializedObject);

            m_Color = Unpack(o.Find(x => x.color));
            m_MeanFreePath = Unpack(o.Find(x => x.meanFreePath));
            m_BaseHeight = Unpack(o.Find(x => x.baseHeight));
            m_MaximumHeight = Unpack(o.Find(x => x.maximumHeight));
            m_MaxFogDistance = Unpack(o.Find(x => x.maxFogDistance));
            m_Anisotropy = Unpack(o.Find(x => x.anisotropy));
            m_DepthExtent = Unpack(o.Find(x => x.depthExtent));
            m_ScreenResolutionPercentage = Unpack(o.Find(x => x.screenResolutionPercentage));
            m_VolumeSliceCount = Unpack(o.Find(x => x.volumeSliceCount));
            m_SliceDistributionUniformity = Unpack(o.Find(x => x.sliceDistributionUniformity));
            m_DenoisingMode = Unpack(o.Find(x => x.denoisingMode));
            m_VolumetricLightingDensityCutoff = Unpack(o.Find(x => x.volumetricLightingDensityCutoff));
            m_EnableLightCookies = Unpack(o.Find(x => x.enableLightCookies));
            m_LightFilter = Unpack(o.Find(x => x.lightFilter));
            m_MultipleScatteringIntensity = Unpack(o.Find(x => x.multipleScatteringIntensity));
        }

        public override void OnInspectorGUI()
        {
            bool isGlobal = volume == null || volume.isGlobal;

            if (isGlobal)
            {
                // Hide these properties on local volumes, to prevent confusion. To override these locally, the user
                // will always want to use a local fog MonoBehavior, not a local volume.
                PropertyField(m_Color);
                PropertyField(m_MeanFreePath);
            }

            PropertyField(m_Anisotropy);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Lighting", EditorStyles.boldLabel);
            PropertyField(m_LightFilter);
            PropertyField(m_MultipleScatteringIntensity);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Quality", EditorStyles.boldLabel);
            PropertyField(m_DenoisingMode);
            PropertyField(m_VolumetricLightingDensityCutoff);
            PropertyField(m_EnableLightCookies);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("V-Buffer Resolution & Range", EditorStyles.boldLabel);
            PropertyField(m_ScreenResolutionPercentage);
            PropertyField(m_VolumeSliceCount);
            PropertyField(m_DepthExtent);
            PropertyField(m_SliceDistributionUniformity);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Height Falloff & Range", EditorStyles.boldLabel);
            PropertyField(m_BaseHeight);
            PropertyField(m_MaximumHeight);
            PropertyField(m_MaxFogDistance);
        }
    }
}

#endif
