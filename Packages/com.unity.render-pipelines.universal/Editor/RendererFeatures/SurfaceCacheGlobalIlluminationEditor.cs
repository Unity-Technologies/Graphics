#if SURFACE_CACHE

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomEditor(typeof(SurfaceCacheGlobalIlluminationRendererFeature))]
    internal class SurfaceCacheGlobalIlluminationEditor : Editor
    {
        private bool m_IsInitialized;

        private SerializedProperty _estimationSampleCount;
        private SerializedProperty _multiBounce;
        private SerializedProperty _temporalSmoothing;
        private SerializedProperty _spatialFilterEnabled;
        private SerializedProperty _spatialFilterSampleCount;
        private SerializedProperty _spatialFilterRadius;
        private SerializedProperty _temporalPostFilterEnabled;
        private SerializedProperty _lookupSampleCount;
        private SerializedProperty _upsamplingKernelSize;
        private SerializedProperty _upsamplingSampleCount;
        private SerializedProperty _volumeSize;
        private SerializedProperty _volumeResolution;
        private SerializedProperty _volumeCascadeCount;
        private SerializedProperty _volumeMovement;
        private SerializedProperty _debugEnabled;
        private SerializedProperty _debugViewMode;
        private SerializedProperty _debugShowSamplePosition;
        private SerializedProperty _defragCount;

        private struct TextContent
        {
            public static GUIContent EstimationSampleCount = EditorGUIUtility.TrTextContent("Sample Count", "");
            public static GUIContent MultiBounce = EditorGUIUtility.TrTextContent("Multi Bounce", "");

            public static GUIContent TemporalSmoothing = EditorGUIUtility.TrTextContent("Temporal Smoothing", "");
            public static GUIContent SpatialFilterEnabled = EditorGUIUtility.TrTextContent("Spatial Filter Enabled", "");
            public static GUIContent SpatialFilterSampleCount = EditorGUIUtility.TrTextContent("Spatial Sample Count", "");
            public static GUIContent SpatialFilterRadius = EditorGUIUtility.TrTextContent("Spatial Radius", "");
            public static GUIContent TemporalPostFilterEnabled = EditorGUIUtility.TrTextContent("Temporal Post Filter Enabled", "");

            public static GUIContent LookupSampleCount = EditorGUIUtility.TrTextContent("Lookup Sample Count", "");
            public static GUIContent UpsamplingKernelSize = EditorGUIUtility.TrTextContent("Upsampling Kernel Size", "");
            public static GUIContent UpsamplingSampleCount = EditorGUIUtility.TrTextContent("Upsampling Sample Count", "");

            public static GUIContent VolumeSize = EditorGUIUtility.TrTextContent("Size", "");
            public static GUIContent VolumeResolution = EditorGUIUtility.TrTextContent("Resolution", "");
            public static GUIContent VolumeCascadeCount = EditorGUIUtility.TrTextContent("Cascade Count", "");
            public static GUIContent VolumeMovement = EditorGUIUtility.TrTextContent("Movement", "");

            public static GUIContent DefragCount = EditorGUIUtility.TrTextContent("Defragmentation Count", "");

            public static GUIContent DebugEnabled = EditorGUIUtility.TrTextContent("Debug Enabled", "");
            public static GUIContent DebugViewMode = EditorGUIUtility.TrTextContent("Debug View Mode", "");
            public static GUIContent DebugShowSamplePosition = EditorGUIUtility.TrTextContent("Debug Show Sample Position", "");
        }

        private void Init()
        {
            SerializedProperty paramSets = serializedObject.FindProperty("_parameterSet");

            SerializedProperty estimationParams = paramSets.FindPropertyRelative("EstimationParams");
            SerializedProperty patchFilteringParams = paramSets.FindPropertyRelative("PatchFilteringParams");
            SerializedProperty screenFilteringParams = paramSets.FindPropertyRelative("ScreenFilteringParams");
            SerializedProperty volumeParams = paramSets.FindPropertyRelative("VolumeParams");
            SerializedProperty advancedParams = paramSets.FindPropertyRelative("AdvancedParams");

            _multiBounce = paramSets.FindPropertyRelative("MultiBounce");

            _estimationSampleCount = estimationParams.FindPropertyRelative("SampleCount");

            _temporalSmoothing = patchFilteringParams.FindPropertyRelative("TemporalSmoothing");
            _spatialFilterEnabled = patchFilteringParams.FindPropertyRelative("SpatialFilterEnabled");
            _spatialFilterSampleCount = patchFilteringParams.FindPropertyRelative("SpatialFilterSampleCount");
            _spatialFilterRadius = patchFilteringParams.FindPropertyRelative("SpatialFilterRadius");
            _temporalPostFilterEnabled = patchFilteringParams.FindPropertyRelative("TemporalPostFilterEnabled");

            _lookupSampleCount = screenFilteringParams.FindPropertyRelative("LookupSampleCount");
            _upsamplingKernelSize = screenFilteringParams.FindPropertyRelative("UpsamplingKernelSize");
            _upsamplingSampleCount = screenFilteringParams.FindPropertyRelative("UpsamplingSampleCount");

            _volumeSize = volumeParams.FindPropertyRelative("Size");
            _volumeResolution = volumeParams.FindPropertyRelative("Resolution");
            _volumeCascadeCount = volumeParams.FindPropertyRelative("CascadeCount");
            _volumeMovement = volumeParams.FindPropertyRelative("Movement");

            _debugEnabled = paramSets.FindPropertyRelative("DebugEnabled");
            _debugViewMode = paramSets.FindPropertyRelative("DebugViewMode");
            _debugShowSamplePosition = paramSets.FindPropertyRelative("DebugShowSamplePosition");

            _defragCount = advancedParams.FindPropertyRelative("DefragCount");
        }

        public override void OnInspectorGUI()
        {
            if (!m_IsInitialized)
                Init();

            if (SceneView.lastActiveSceneView && !SceneView.lastActiveSceneView.sceneViewState.alwaysRefreshEnabled)
            {
                EditorGUILayout.HelpBox("Enable \"Always Refresh\" in the Scene View to see realtime updates in the Scene View.", MessageType.Info);
            }

            EditorGUILayout.LabelField("Sampling", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_multiBounce, TextContent.MultiBounce);
            EditorGUILayout.IntSlider(_estimationSampleCount, 1, 32, TextContent.EstimationSampleCount);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Patch Filtering", EditorStyles.boldLabel);
            EditorGUILayout.Slider(_temporalSmoothing, 0.0f, 1.0f, TextContent.TemporalSmoothing);
            EditorGUILayout.PropertyField(_spatialFilterEnabled, TextContent.SpatialFilterEnabled);
            EditorGUILayout.IntSlider(_spatialFilterSampleCount, 1, 8, TextContent.SpatialFilterSampleCount);
            EditorGUILayout.Slider(_spatialFilterRadius, 0.1f, 4.0f, TextContent.SpatialFilterRadius);
            EditorGUILayout.PropertyField(_temporalPostFilterEnabled, TextContent.TemporalPostFilterEnabled);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Screen Filtering", EditorStyles.boldLabel);
            EditorGUILayout.IntSlider(_lookupSampleCount, 0, 8, TextContent.LookupSampleCount);
            EditorGUILayout.Slider(_upsamplingKernelSize, 0.0f, 8.0f, TextContent.UpsamplingKernelSize);
            EditorGUILayout.IntSlider(_upsamplingSampleCount, 1, 16, TextContent.UpsamplingSampleCount);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Volume", EditorStyles.boldLabel);
            _volumeSize.floatValue = Mathf.Max(0.0f, EditorGUILayout.FloatField(TextContent.VolumeSize, _volumeSize.floatValue));
            EditorGUILayout.IntSlider(_volumeResolution, 16, 128, TextContent.VolumeResolution);
            EditorGUILayout.IntSlider(_volumeCascadeCount, 1, (int)SurfaceCache.CascadeMax, TextContent.VolumeCascadeCount);
            EditorGUILayout.PropertyField(_volumeMovement, TextContent.VolumeMovement);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Advanced", EditorStyles.boldLabel);
            EditorGUILayout.IntSlider(_defragCount, 1, 32, TextContent.DefragCount);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_debugEnabled, TextContent.DebugEnabled);
            EditorGUILayout.PropertyField(_debugViewMode, TextContent.DebugViewMode);
            EditorGUILayout.PropertyField(_debugShowSamplePosition, TextContent.DebugShowSamplePosition);
        }
    }
}

#endif
