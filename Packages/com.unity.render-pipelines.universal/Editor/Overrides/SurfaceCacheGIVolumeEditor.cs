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
        protected SerializedDataParameter m_Intensity;
        protected SerializedDataParameter m_LightTransportMultiBounce;
        protected SerializedDataParameter m_LightTransportBouncePatchAllocation;
        protected SerializedDataParameter m_VolumeDistanceFallback;
        protected SerializedDataParameter m_LightTransportSampleCount;

        protected SerializedDataParameter m_PatchFilteringTemporalSmoothing;
        protected SerializedDataParameter m_PatchFilteringSpatialFilterEnabled;
        protected SerializedDataParameter m_PatchFilteringSpatialSampleCount;
        protected SerializedDataParameter m_PatchFilteringSpatialRadius;
        protected SerializedDataParameter m_PatchFilteringPostTemporalEnabled;

        protected SerializedDataParameter m_ScreenFilteringLookupSampleCount;
        protected SerializedDataParameter m_ScreenFilteringDenoisingPassCount;

        protected SerializedDataParameter m_VolumeSize;
        protected SerializedDataParameter m_VolumeResolution;
        protected SerializedDataParameter m_VolumeCascadeCount;
        protected SerializedDataParameter m_AdvancedPatchWarpingEnabled;

        protected SerializedDataParameter m_AdvancedDefragCount;
        protected SerializedDataParameter m_AdvancedRenderingLayerMask;

        // The Focus Target is serialized on the owning Volume's GameObject (Volume.m_SceneObjectReference), not in
        // the profile, so it is edited through this SerializedObject rather than a SerializedDataParameter.
        const string k_ReferenceProp = "m_SceneObjectReference";
        const string k_OverrideStateProp = "m_OverrideState";
        const string k_ValueProp = "m_Value";
        // Mirror VolumeComponentEditor's override-checkbox gutter so the Focus Target row aligns with parameter rows.
        const float k_OverrideCheckboxWidth = 14f;
        const float k_OverrideCheckboxOffset = 9f;
        SerializedObject m_VolumeSerializedObject;

        static GUIContent s_State = EditorGUIUtility.TrTextContent("State", "When Enabled, Surface Cache Global Illumination calculates indirect lighting at any Intensity value. When Disabled, it does not.");
        static GUIContent s_Intensity = EditorGUIUtility.TrTextContent("Intensity", "Scales the indirect lighting from Surface Cache Global Illumination. At 0 it adds no indirect lighting to the scene.");
        static GUIContent s_Quality = EditorGUIUtility.TrTextContent("Quality", "Quality preset for Surface Cache GI. Select Custom to manually adjust individual parameters.");
        static GUIContent s_LightTransportMultiBounce = EditorGUIUtility.TrTextContent("Multi Bounce", "Enable multi-bounce global illumination for more accurate light propagation.");
        static GUIContent s_LightTransportBouncePatchAllocation = EditorGUIUtility.TrTextContent("Bounce Patch Allocation", "When enabled, new patches are allocated at ray hit locations when multi-bounce cache lookups fail");
        static GUIContent s_LightTransportSampleCount = EditorGUIUtility.TrTextContent("Sample Count", "Number of samples used for GI estimation. Higher values improve quality at performance cost.");
        static GUIContent s_PatchFilteringTemporalSmoothing = EditorGUIUtility.TrTextContent("Temporal Smoothing", "Temporal smoothing for patch data. Higher values produce more stable results but slower response to lighting changes.");
        static GUIContent s_PatchFilteringSpatialFilterEnabled = EditorGUIUtility.TrTextContent("Spatial Filter", "Enables spatial filtering across patches. This reduces noise but may also increase leaking.");
        static GUIContent s_PatchFilteringSpatialSampleCount = EditorGUIUtility.TrTextContent("Spatial Sample Count", "Number of samples for spatial filtering. Higher values improve quality at performance cost.");
        static GUIContent s_PatchFilteringSpatialRadius = EditorGUIUtility.TrTextContent("Spatial Radius", "Radius used for the spatial filtering kernel. Larger values reduces noise but may cause over-blurring.");
        static GUIContent s_PatchFilteringPostTemporalEnabled = EditorGUIUtility.TrTextContent("Temporal Post Filter", "Enable temporal post-filtering for additional stability.");
        static GUIContent s_ScreenFilteringLookupSampleCount = EditorGUIUtility.TrTextContent("Lookup Sample Count", "Number of samples for screen-space lookups.");
        static GUIContent s_ScreenFilteringDenoisingPassCount = EditorGUIUtility.TrTextContent("Denoising Pass Count", "Number of denoising passes. More passes reduce noise at a performance cost.");
        static GUIContent s_VolumeSize = EditorGUIUtility.TrTextContent("Size", "Size of the surface cache volume in world units. Can be changed at runtime without a performance hitch.");
        static GUIContent s_VolumeResolution = EditorGUIUtility.TrTextContent("Resolution", "Spatial resolution of the volume grid. Higher values improve spatial detail but use more memory. Changing at runtime can cause a performance hitch if internal buffers are reallocated.");
        static GUIContent s_VolumeCascadeCount = EditorGUIUtility.TrTextContent("Cascade Count", "Number of volume cascades. More cascades extend the volume's effective range. Changing at runtime can cause a performance hitch if internal buffers are reallocated.");
        static GUIContent s_VolumeDistanceFallback = EditorGUIUtility.TrTextContent("Distance Fallback", "When enabled, a realtime global probe calculated from the environment light will be applied in the far distance.");
        static GUIContent s_PatchWarping = EditorGUIUtility.TrTextContent("Patch Warping", "Reduces flickering on flat surfaces lined up with the voxel grid (e.g. a floor at height 0). When off, no warping is applied.");
        static GUIContent s_FocusTarget = EditorGUIUtility.TrTextContent("Focus Target", "GameObject the Surface Cache GI cascades center on. When unset, cascades follow the active camera. Stored on the Volume component, not in the shared Volume Profile.");
        static GUIContent s_FocusTargetProfile = EditorGUIUtility.TrTextContent("Focus Target", "GameObject the Surface Cache GI cascades center on. When unset, cascades follow the active camera. Requires a scene object reference, so it cannot be stored in the Volume Profile. Assign it on the Volume component in your scene.");
        static GUIContent s_FocusTargetProfileValue = EditorGUIUtility.TrTextContent("Scene object, set per Volume");

        // Section header labels with tooltips
        static GUIContent s_LightTransportHeader = EditorGUIUtility.TrTextContent("Light Transport", "Controls how many rays are cast per patch to estimate indirect lighting. More samples reduce variance but increase GPU cost per frame.");
        static GUIContent s_PatchFilteringHeader = EditorGUIUtility.TrTextContent("Patch Filtering", "Controls how patch irradiance data is filtered over time and space. These settings trade temporal stability and spatial smoothness against responsiveness to lighting changes and light leaking.");
        static GUIContent s_ScreenFilteringHeader = EditorGUIUtility.TrTextContent("Screen Filtering", "Controls how the low-resolution patch irradiance is resolved and upsampled to full screen resolution. These settings affect the final image quality and sharpness of the GI contribution.");
        static GUIContent s_VolumeHeader = EditorGUIUtility.TrTextContent("Volume", "Defines the spatial extent and grid density of the surface cache volume. Size can be changed freely at runtime. Resolution and cascade count changes reallocate internal buffers, which may cause a brief hitch.");
        static GUIContent s_DefragCount = EditorGUIUtility.TrTextContent("Defrag Count", "Number of surface cache patches to defragment per frame. Higher values reduce memory fragmentation at a small per-frame cost.");
        static GUIContent s_RenderingLayerMask = EditorGUIUtility.TrTextContent("Rendering Layer Mask", "Only renderers whose Rendering Layer Mask intersects this mask contribute to Surface Cache GI.");

        public override void OnEnable()
        {
            var o = new PropertyFetcher<SurfaceCacheGIVolumeOverride>(serializedObject);

            m_Enabled = Unpack(o.Find(obj => obj.enabled));
            m_Intensity = Unpack(o.Find(obj => obj.intensity));
            m_LightTransportMultiBounce = Unpack(o.Find(obj => obj.lightTransportMultiBounce));
            m_LightTransportBouncePatchAllocation = Unpack(o.Find(obj => obj.lightTransportBouncePatchAllocation));
            m_VolumeDistanceFallback = Unpack(o.Find(obj => obj.volumeDistanceFallback));
            m_LightTransportSampleCount = Unpack(o.Find(obj => obj.lightTransportSampleCount));
            m_PatchFilteringTemporalSmoothing = Unpack(o.Find(obj => obj.patchFilteringTemporalSmoothing));
            m_PatchFilteringSpatialFilterEnabled = Unpack(o.Find(obj => obj.patchFilteringSpatialFilterEnabled));
            m_PatchFilteringSpatialSampleCount = Unpack(o.Find(obj => obj.patchFilteringSpatialSampleCount));
            m_PatchFilteringSpatialRadius = Unpack(o.Find(obj => obj.patchFilteringSpatialRadius));
            m_PatchFilteringPostTemporalEnabled = Unpack(o.Find(obj => obj.patchFilteringPostTemporalEnabled));
            m_ScreenFilteringLookupSampleCount = Unpack(o.Find(obj => obj.screenFilteringLookupSampleCount));
            m_ScreenFilteringDenoisingPassCount = Unpack(o.Find(obj => obj.screenFilteringDenoisingPassCount));
            m_VolumeSize = Unpack(o.Find(obj => obj.volumeSize));
            m_VolumeResolution = Unpack(o.Find(obj => obj.volumeResolution));
            m_VolumeCascadeCount = Unpack(o.Find(obj => obj.volumeCascadeCount));
            m_AdvancedPatchWarpingEnabled = Unpack(o.Find(obj => obj.advancedPatchWarpingEnabled));
            m_AdvancedDefragCount = Unpack(o.Find(obj => obj.advancedDefragCount));
            m_AdvancedRenderingLayerMask = Unpack(o.Find(obj => obj.advancedRenderingLayerMask));

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
            PropertyField(m_Enabled, s_State);
            PropertyField(m_Intensity, s_Intensity);

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
            PropertyField(m_LightTransportMultiBounce, s_LightTransportMultiBounce);
            PropertyField(m_LightTransportBouncePatchAllocation, s_LightTransportBouncePatchAllocation);
            PropertyField(m_LightTransportSampleCount, s_LightTransportSampleCount);

            // Patch Filtering section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_PatchFilteringHeader, EditorStyles.boldLabel);
            PropertyField(m_PatchFilteringTemporalSmoothing, s_PatchFilteringTemporalSmoothing);
            PropertyField(m_PatchFilteringSpatialFilterEnabled, s_PatchFilteringSpatialFilterEnabled);
            using (new IndentLevelScope())
            {
                PropertyField(m_PatchFilteringSpatialSampleCount, s_PatchFilteringSpatialSampleCount);
                PropertyField(m_PatchFilteringSpatialRadius, s_PatchFilteringSpatialRadius);
            }
            PropertyField(m_PatchFilteringPostTemporalEnabled, s_PatchFilteringPostTemporalEnabled);

            // Screen Filtering section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_ScreenFilteringHeader, EditorStyles.boldLabel);
            PropertyField(m_ScreenFilteringLookupSampleCount, s_ScreenFilteringLookupSampleCount);
            PropertyField(m_ScreenFilteringDenoisingPassCount, s_ScreenFilteringDenoisingPassCount);

            // Volume Configuration section
            EditorGUILayout.Space();
            EditorGUILayout.LabelField(s_VolumeHeader, EditorStyles.boldLabel);
            PropertyField(m_VolumeSize, s_VolumeSize);
            PropertyField(m_VolumeResolution, s_VolumeResolution);
            PropertyField(m_VolumeCascadeCount, s_VolumeCascadeCount);
            DrawFocusTarget();
            PropertyField(m_VolumeDistanceFallback, s_VolumeDistanceFallback);

            if (showAdditionalProperties)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
                PropertyField(m_AdvancedRenderingLayerMask, s_RenderingLayerMask);
                PropertyField(m_AdvancedDefragCount, s_DefragCount);
                PropertyField(m_AdvancedPatchWarpingEnabled, s_PatchWarping);
            }
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
