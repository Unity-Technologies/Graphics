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
        protected SerializedDataParameter m_PatchWarping;

        protected SerializedDataParameter m_DefragCount;
        protected SerializedDataParameter m_RenderingLayerMask;

        // The Focus Target is serialized on the owning Volume's GameObject (Volume.m_SceneObjectReference), not in
        // the profile, so it is edited through this SerializedObject rather than a SerializedDataParameter.
        const string k_ReferenceProp = "m_SceneObjectReference";
        const string k_OverrideStateProp = "m_OverrideState";
        const string k_ValueProp = "m_Value";
        // Mirror VolumeComponentEditor's override-checkbox gutter so the Focus Target row aligns with parameter rows.
        const float k_OverrideCheckboxWidth = 14f;
        const float k_OverrideCheckboxOffset = 9f;
        SerializedObject m_VolumeSerializedObject;

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
        static GUIContent s_PatchWarping = EditorGUIUtility.TrTextContent("Patch Warping", "Reduces flickering on flat surfaces lined up with the voxel grid (e.g. a floor at height 0). When off, no warping is applied.");
        static GUIContent s_FocusTarget = EditorGUIUtility.TrTextContent("Focus Target", "GameObject the Surface Cache GI cascades center on. When unset, cascades follow the active camera. Stored on the Volume component, not in the shared Volume Profile.");
        static GUIContent s_FocusTargetProfile = EditorGUIUtility.TrTextContent("Focus Target", "GameObject the Surface Cache GI cascades center on. When unset, cascades follow the active camera. Requires a scene object reference, so it cannot be stored in the Volume Profile. Assign it on the Volume component in your scene.");
        static GUIContent s_FocusTargetProfileValue = EditorGUIUtility.TrTextContent("Scene object, set per Volume");

        // Section header labels with tooltips
        static GUIContent s_LightTransportHeader = EditorGUIUtility.TrTextContent("Light Transport", "Controls how many rays are cast per patch to estimate indirect lighting. More samples reduce variance but increase GPU cost per frame.");
        static GUIContent s_PatchFilteringHeader = EditorGUIUtility.TrTextContent("Patch Filtering", "Controls how patch irradiance data is filtered over time and space. These settings trade temporal stability and spatial smoothness against responsiveness to lighting changes and light leaking.");
        static GUIContent s_ScreenFilteringHeader = EditorGUIUtility.TrTextContent("Screen Filtering", "Controls how the low-resolution patch irradiance is resolved and upsampled to full screen resolution. These settings affect the final image quality and sharpness of the GI contribution.");
        static GUIContent s_VolumeConfigurationHeader = EditorGUIUtility.TrTextContent("Volume Configuration", "Defines the spatial extent and grid density of the surface cache volume. Size can be changed freely at runtime. Resolution and cascade count changes reallocate internal buffers, which may cause a brief hitch.");
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
            m_PatchWarping = Unpack(o.Find(obj => obj.patchWarping));
            m_DefragCount = Unpack(o.Find(obj => obj.defragCount));
            m_RenderingLayerMask = Unpack(o.Find(obj => obj.renderingLayerMask));

            // Re-detect preset when property changed
            ((SurfaceCacheGIVolumeOverride)target).propertyChanged += MarkQualityDirty;
        }

        public override void OnDisable()
        {
            ((SurfaceCacheGIVolumeOverride)target).propertyChanged -= MarkQualityDirty;

            if (m_VolumeSerializedObject != null)
            {
                m_VolumeSerializedObject.Dispose();
                m_VolumeSerializedObject = null;
            }
        }

        public override void OnInspectorGUI()
        {
            PropertyField(m_Enabled, s_Enabled);
            EditorGUI.BeginDisabledGroup(m_Enabled.overrideState.boolValue && !m_Enabled.value.boolValue);

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
            DrawFocusTarget();

            if (showAdditionalProperties)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
                PropertyField(m_RenderingLayerMask, s_RenderingLayerMask);
                PropertyField(m_DefragCount, s_DefragCount);
                PropertyField(m_PatchWarping, s_PatchWarping);
            }

            EditorGUI.EndDisabledGroup();
        }

        // Focus Target is bound to the owning Volume's GameObject instead of the shared profile. When editing
        // the Profile asset there is no GameObject to store Focus Target on and nothing is drawn.
        void DrawFocusTarget()
        {
            const float gutter = k_OverrideCheckboxWidth + k_OverrideCheckboxOffset;

            // When not in scene context, display a read-only UI.
            if (volume == null)
            {
                using (new EditorGUI.DisabledScope(true))
                {
                    EditorGUIUtility.labelWidth -= gutter + 3 + 3;
                    // Override checkboxes are shown in Asset editor but not in Graphics Settings - special handling
                    // needed to align the property label correctly.
                    if (enableOverrides)
                    {
                        EditorGUILayout.BeginHorizontal();
                        GUILayout.Space(3f);
                        GUILayoutUtility.GetRect(gutter, EditorGUIUtility.singleLineHeight, GUILayout.ExpandWidth(false));
                        EditorGUILayout.LabelField(s_FocusTargetProfile, s_FocusTargetProfileValue);
                        EditorGUILayout.EndHorizontal();
                    }
                    else
                    {
                        EditorGUILayout.LabelField(s_FocusTargetProfile, s_FocusTargetProfileValue);
                    }
                    EditorGUIUtility.labelWidth += gutter + 3 + 3;
                }
                return;
            }

            if (m_VolumeSerializedObject == null || m_VolumeSerializedObject.targetObject != volume)
                m_VolumeSerializedObject = new SerializedObject(volume);

            m_VolumeSerializedObject.Update();
            var reference = m_VolumeSerializedObject.FindProperty(k_ReferenceProp);
            var overrideState = reference.FindPropertyRelative(k_OverrideStateProp);
            var value = reference.FindPropertyRelative(k_ValueProp);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginHorizontal();
            // The whole hand-drawn Focus Target row sits 3px left of the generated parameter rows; inset it
            // so the checkbox, label, and field all line up with the rows above and below.
            GUILayout.Space(3f);

            float rowHeight = EditorGUI.GetPropertyHeight(value) + EditorGUIUtility.standardVerticalSpacing;
            var toggleSize = CoreEditorStyles.smallTickbox.CalcSize(GUIContent.none);
            var toggleRect = GUILayoutUtility.GetRect(gutter, rowHeight, GUILayout.ExpandWidth(false));
            toggleRect.yMin += rowHeight * 0.5f - toggleSize.y * 0.5f;
            toggleRect.xMin += k_OverrideCheckboxOffset;
            overrideState.boolValue = GUI.Toggle(toggleRect, overrideState.boolValue, GUIContent.none, CoreEditorStyles.smallTickbox);

            EditorGUIUtility.labelWidth -= gutter + 3 + 3;
            using (new EditorGUI.DisabledScope(!overrideState.boolValue))
            {
                // GetControlRect reserves the same rect EditorGUILayout.PropertyField uses, so the label/field
                // split and width stay correct; nudge it up 3px to cancel the vertical offset this custom row adds.
                Rect fieldRowRect = EditorGUILayout.GetControlRect(GUILayout.ExpandWidth(true));
                fieldRowRect.y -= 2f;
                EditorGUI.PropertyField(fieldRowRect, value, s_FocusTarget);
            }
            EditorGUIUtility.labelWidth += gutter + 3 + 3;

            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
                m_VolumeSerializedObject.ApplyModifiedProperties();
        }

        private void MarkQualityDirty()
        {
            m_QualityDirty = true;
        }
    }
}

#endif
