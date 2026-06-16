#if SURFACE_CACHE

using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(SurfaceCacheGIVolumeOverride))]
    internal class SurfaceCacheGIVolumeEditor : VolumeComponentEditor
    {
        protected SurfaceCacheGIVolumeOverride.PresetQuality m_Quality;
        private bool m_QualityDirty = true;
        protected SerializedDataParameter m_Enabled;
        protected SerializedDataParameter m_MultiBounce;
        protected SerializedDataParameter m_BouncePatchAllocation;
        protected SerializedDataParameter m_SampleCount;

        protected SerializedDataParameter m_TemporalSmoothing;
        protected SerializedDataParameter m_SpatialFilterEnabled;
        protected SerializedDataParameter m_SpatialSampleCount;
        protected SerializedDataParameter m_SpatialRadius;
        protected SerializedDataParameter m_TemporalPostFilter;

        protected SerializedDataParameter m_LookupSampleCount;
        protected SerializedDataParameter m_UpsamplingKernelSize;
        protected SerializedDataParameter m_UpsamplingSampleCount;

        protected SerializedDataParameter m_VolumeSize;
        protected SerializedDataParameter m_VolumeResolution;
        protected SerializedDataParameter m_VolumeCascadeCount;

        protected SerializedDataParameter m_CascadeMovement;

        protected SerializedDataParameter m_DefragCount;
        protected SerializedDataParameter m_RenderingLayerMask;

        static GUIContent s_Enabled = EditorGUIUtility.TrTextContent("Enabled", "Enables Surface Cache Global Illumination. When disabled, the feature performs no work.");
        static GUIContent s_Quality = EditorGUIUtility.TrTextContent("Quality", "Quality preset for Surface Cache GI. Select Custom to manually adjust individual parameters.");
        static GUIContent s_MultiBounce = EditorGUIUtility.TrTextContent("Multi Bounce", "Enable multi-bounce global illumination for more accurate light propagation.");
        static GUIContent s_BouncePatchAllocation = EditorGUIUtility.TrTextContent("Bounce Patch Allocation", "When enabled, new patches are allocated at ray hit locations when multi-bounce cache lookups fail");
        static GUIContent s_SampleCount = EditorGUIUtility.TrTextContent("Sample Count", "Number of samples used for GI estimation. Higher values improve quality at performance cost.");
        static GUIContent s_TemporalSmoothing = EditorGUIUtility.TrTextContent("Temporal Smoothing", "Temporal smoothing for patch data. Higher values produce more stable results but slower response to lighting changes.");
        static GUIContent s_SpatialFilterEnabled = EditorGUIUtility.TrTextContent("Spatial Filter", "Enables spatial filtering across patches. This reduces noise but may also increase leaking.");
        static GUIContent s_SpatialSampleCount = EditorGUIUtility.TrTextContent("Spatial Sample Count", "Number of samples for spatial filtering. Higher values improve quality at performance cost.");
        static GUIContent s_SpatialRadius = EditorGUIUtility.TrTextContent("Spatial Radius", "Radius used for the spatial filtering kernel. Larger values reduces noise but may cause over-blurring.");
        static GUIContent s_TemporalPostFilter = EditorGUIUtility.TrTextContent("Temporal Post Filter", "Enable temporal post-filtering for additional stability.");
        static GUIContent s_LookupSampleCount = EditorGUIUtility.TrTextContent("Lookup Sample Count", "Number of samples for screen-space lookups.");
        static GUIContent s_UpsamplingKernelSize = EditorGUIUtility.TrTextContent("Upsampling Kernel Size", "Kernel size for upsampling filtering.");
        static GUIContent s_UpsamplingSampleCount = EditorGUIUtility.TrTextContent("Upsampling Sample Count", "Number of samples for upsampling. Higher values improve quality at performance cost.");
        static GUIContent s_VolumeSize = EditorGUIUtility.TrTextContent("Size", "Size of the surface cache volume in world units. Can be changed at runtime without a performance hitch.");
        static GUIContent s_VolumeResolution = EditorGUIUtility.TrTextContent("Resolution", "Spatial resolution of the volume grid. Higher values improve spatial detail but use more memory. Changing at runtime can cause a performance hitch if internal buffers are reallocated.");
        static GUIContent s_VolumeCascadeCount = EditorGUIUtility.TrTextContent("Cascade Count", "Number of volume cascades. More cascades extend the volume's effective range. Changing at runtime can cause a performance hitch if internal buffers are reallocated.");
        static GUIContent s_CascadeMovement = EditorGUIUtility.TrTextContent("Cascade Movement", "Enable volume cascades to follow the camera.");

        // Section header labels with tooltips
        static GUIContent s_LightTransportHeader = EditorGUIUtility.TrTextContent("Light Transport", "Controls how many rays are cast per patch to estimate indirect lighting. More samples reduce variance but increase GPU cost per frame.");
        static GUIContent s_PatchFilteringHeader = EditorGUIUtility.TrTextContent("Patch Filtering", "Controls how patch irradiance data is filtered over time and space. These settings trade temporal stability and spatial smoothness against responsiveness to lighting changes and light leaking.");
        static GUIContent s_ScreenFilteringHeader = EditorGUIUtility.TrTextContent("Screen Filtering", "Controls how the low-resolution patch irradiance is resolved and upsampled to full screen resolution. These settings affect the final image quality and sharpness of the GI contribution.");
        static GUIContent s_VolumeConfigurationHeader = EditorGUIUtility.TrTextContent("Volume Configuration", "Defines the spatial extent and grid density of the surface cache volume. Size can be changed freely at runtime. Resolution and cascade count changes reallocate internal buffers, which may cause a brief hitch.");
        static GUIContent s_VolumeBehaviorHeader = EditorGUIUtility.TrTextContent("Volume Behavior", "Controls how the volume cascades move relative to the camera during gameplay.");
        static GUIContent s_DefragCount = EditorGUIUtility.TrTextContent("Defrag Count", "Number of surface cache patches to defragment per frame. Higher values reduce memory fragmentation at a small per-frame cost.");
        static GUIContent s_RenderingLayerMask = EditorGUIUtility.TrTextContent("Rendering Layer Mask", "Only renderers whose Rendering Layer Mask intersects this mask contribute to Surface Cache GI.");

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SurfaceCacheGIVolumeOverride>(serializedObject);

            m_Enabled = Unpack(o.Find(obj => obj.enabled));
            m_MultiBounce = Unpack(o.Find(obj => obj.multiBounce));
            m_BouncePatchAllocation = Unpack(o.Find(obj => obj.bouncePatchAllocation));
            m_SampleCount = Unpack(o.Find(obj => obj.sampleCount));
            m_TemporalSmoothing = Unpack(o.Find(obj => obj.temporalSmoothing));
            m_SpatialFilterEnabled = Unpack(o.Find(obj => obj.spatialFilterEnabled));
            m_SpatialSampleCount = Unpack(o.Find(obj => obj.spatialSampleCount));
            m_SpatialRadius = Unpack(o.Find(obj => obj.spatialRadius));
            m_TemporalPostFilter = Unpack(o.Find(obj => obj.temporalPostFilter));
            m_LookupSampleCount = Unpack(o.Find(obj => obj.lookupSampleCount));
            m_UpsamplingKernelSize = Unpack(o.Find(obj => obj.upsamplingKernelSize));
            m_UpsamplingSampleCount = Unpack(o.Find(obj => obj.upsamplingSampleCount));
            m_VolumeSize = Unpack(o.Find(obj => obj.volumeSize));
            m_VolumeResolution = Unpack(o.Find(obj => obj.volumeResolution));
            m_VolumeCascadeCount = Unpack(o.Find(obj => obj.volumeCascadeCount));
            m_CascadeMovement = Unpack(o.Find(obj => obj.cascadeMovement));
            m_DefragCount = Unpack(o.Find(obj => obj.defragCount));
            m_RenderingLayerMask = Unpack(o.Find(obj => obj.renderingLayerMask));

            // Re-detect preset when property changed
            ((SurfaceCacheGIVolumeOverride)target).propertyChanged += MarkQualityDirty;
        }

        public override void OnDisable()
        {
            ((SurfaceCacheGIVolumeOverride)target).propertyChanged -= MarkQualityDirty;
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Enabled, s_Enabled);
            EditorGUI.BeginDisabledGroup(!m_Enabled.value.boolValue);

            // Quality preset dropdown. Switching to a named preset writes its values into the
            // backing fields so users can see the preset values and make fine adjustments from there.
            var quality = (SurfaceCacheGIVolumeOverride.PresetQuality)EditorGUILayout.EnumPopup(s_Quality, m_Quality, (e) => (SurfaceCacheGIVolumeOverride.PresetQuality)e != SurfaceCacheGIVolumeOverride.PresetQuality.Custom, false);
            if (m_Quality != quality && quality != SurfaceCacheGIVolumeOverride.PresetQuality.Custom)
            {
                Undo.RecordObjects(serializedObject.targetObjects, "Applied preset");
                m_Quality = quality;
                foreach (var targetObj in serializedObject.targetObjects)
                {
                    (targetObj as SurfaceCacheGIVolumeOverride).ApplyPreset(m_Quality);
                }
                serializedObject.Update();
            }
            else if (m_QualityDirty)
            {
                m_Quality = (serializedObject.targetObject as SurfaceCacheGIVolumeOverride).GetPresetQuality();
                m_QualityDirty = false;
            }

            // Light Transport section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_LightTransportHeader, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            PropertyField(m_MultiBounce, s_MultiBounce);
            PropertyField(m_BouncePatchAllocation, s_BouncePatchAllocation);
            PropertyField(m_SampleCount, s_SampleCount);

            // Patch Filtering section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_PatchFilteringHeader, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            PropertyField(m_TemporalSmoothing, s_TemporalSmoothing);
            PropertyField(m_SpatialFilterEnabled, s_SpatialFilterEnabled);
            using (new IndentLevelScope())
            {
                PropertyField(m_SpatialSampleCount, s_SpatialSampleCount);
                PropertyField(m_SpatialRadius, s_SpatialRadius);
            }
            PropertyField(m_TemporalPostFilter, s_TemporalPostFilter);

            // Screen Filtering section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_ScreenFilteringHeader, EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            PropertyField(m_LookupSampleCount, s_LookupSampleCount);
            PropertyField(m_UpsamplingKernelSize, s_UpsamplingKernelSize);
            PropertyField(m_UpsamplingSampleCount, s_UpsamplingSampleCount);

            // Volume Configuration section (always editable, not affected by quality preset)
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_VolumeConfigurationHeader, EditorStyles.boldLabel);
            PropertyField(m_VolumeSize, s_VolumeSize);
            PropertyField(m_VolumeResolution, s_VolumeResolution);
            PropertyField(m_VolumeCascadeCount, s_VolumeCascadeCount);

            // Volume Behavior (always editable, not affected by quality preset)
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_VolumeBehaviorHeader, EditorStyles.boldLabel);
            PropertyField(m_CascadeMovement, s_CascadeMovement);

            if (showAdditionalProperties)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
                PropertyField(m_DefragCount, s_DefragCount);
                PropertyField(m_RenderingLayerMask, s_RenderingLayerMask);
            }

            EditorGUI.EndDisabledGroup();
        }

        private void MarkQualityDirty()
        {
            m_QualityDirty = true;
        }
    }
}

#endif
