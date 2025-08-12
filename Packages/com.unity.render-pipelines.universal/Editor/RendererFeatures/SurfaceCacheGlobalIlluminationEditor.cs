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

        private SerializedProperty _uniformEstimationSampleCount;
        private SerializedProperty _restirEstimationConfidenceCap;
        private SerializedProperty _restirEstimationSpatialSampleCount;
        private SerializedProperty _restirEstimationSpatialFilterSize;
        private SerializedProperty _restirEstimationValidationFrameInterval;
        private SerializedProperty _risEstimationCandidateCount;
        private SerializedProperty _risEstimationTargetFunctionUpdateWeight;
        private SerializedProperty _multiBounce;
        private SerializedProperty _estimationMethod;
        private SerializedProperty _temporalSmoothing;
        private SerializedProperty _spatialFilterEnabled;
        private SerializedProperty _spatialFilterSampleCount;
        private SerializedProperty _spatialFilterRadius;
        private SerializedProperty _temporalPostFilterEnabled;
        private SerializedProperty _lookupSampleCount;
        private SerializedProperty _upsamplingKernelSize;
        private SerializedProperty _upsamplingSampleCount;
        private SerializedProperty _gridSize;
        private SerializedProperty _voxelMinSize;
        private SerializedProperty _cascadeCount;
        private SerializedProperty _cascadeMovement;
        private SerializedProperty _debugEnabled;
        private SerializedProperty _debugViewMode;
        private SerializedProperty _debugShowSamplePosition;

        private struct TextContent
        {
            public static GUIContent UniformEstimationSampleCount = EditorGUIUtility.TrTextContent("Sample Count", "");
            public static GUIContent RestirEstimationConfidenceCap = EditorGUIUtility.TrTextContent("Confidence Cap", "");
            public static GUIContent RestirEstimationSpatialSampleCount = EditorGUIUtility.TrTextContent("Spatial Sample Count", "");
            public static GUIContent RestirEstimationSpatialFilterSize = EditorGUIUtility.TrTextContent("Spatial Filter Size", "");
            public static GUIContent RestirEstimationValidationFrameInterval = EditorGUIUtility.TrTextContent("Validation Frame Interval", "");
            public static GUIContent RisEstimationCandidateCount = EditorGUIUtility.TrTextContent("Candidate Count", "");
            public static GUIContent RisEstimationTargetFunctionUpdateWeight = EditorGUIUtility.TrTextContent("Target Function Update Weight", "");
            public static GUIContent MultiBounce = EditorGUIUtility.TrTextContent("Multi Bounce", "");
            public static GUIContent EstimationMethod = EditorGUIUtility.TrTextContent("Estimation Method", "");

            public static GUIContent TemporalSmoothing = EditorGUIUtility.TrTextContent("Temporal Smoothing", "");
            public static GUIContent SpatialFilterEnabled = EditorGUIUtility.TrTextContent("Spatial Filter Enabled", "");
            public static GUIContent SpatialFilterSampleCount = EditorGUIUtility.TrTextContent("Spatial Sample Count", "");
            public static GUIContent SpatialFilterRadius = EditorGUIUtility.TrTextContent("Spatial Radius", "");
            public static GUIContent TemporalPostFilterEnabled = EditorGUIUtility.TrTextContent("Temporal Post Filter Enabled", "");

            public static GUIContent LookupSampleCount = EditorGUIUtility.TrTextContent("Lookup Sample Count", "");
            public static GUIContent UpsamplingKernelSize = EditorGUIUtility.TrTextContent("Upsampling Kernel Size", "");
            public static GUIContent UpsamplingSampleCount = EditorGUIUtility.TrTextContent("Upsampling Sample Count", "");

            public static GUIContent GridSize = EditorGUIUtility.TrTextContent("Grid Size", "");
            public static GUIContent VoxelMinSize = EditorGUIUtility.TrTextContent("Voxel Min Size", "");
            public static GUIContent CascadeCount = EditorGUIUtility.TrTextContent("Cascade Count", "");
            public static GUIContent CascadeMovement = EditorGUIUtility.TrTextContent("Cascade Movement", "");

            public static GUIContent DebugEnabled = EditorGUIUtility.TrTextContent("Debug Enabled", "");
            public static GUIContent DebugViewMode = EditorGUIUtility.TrTextContent("Debug View Mode", "");
            public static GUIContent DebugShowSamplePosition = EditorGUIUtility.TrTextContent("Debug Show Sample Position", "");
        }

        private void Init()
        {
            SerializedProperty paramSet = serializedObject.FindProperty("_parameterSet");

            SerializedProperty uniformParamSet = paramSet.FindPropertyRelative("UniformEstimationParams");
            SerializedProperty restirParamSet = paramSet.FindPropertyRelative("RestirEstimationParams");
            SerializedProperty risParamSet = paramSet.FindPropertyRelative("RisEstimationParams");
            SerializedProperty patchFilteringParamSet = paramSet.FindPropertyRelative("PatchFilteringParams");
            SerializedProperty screenFilteringParamSet = paramSet.FindPropertyRelative("ScreenFilteringParams");
            SerializedProperty gridParamSet = paramSet.FindPropertyRelative("GridParams");

            _multiBounce = paramSet.FindPropertyRelative("MultiBounce");
            _estimationMethod = paramSet.FindPropertyRelative("EstimationMethod");

            _uniformEstimationSampleCount = uniformParamSet.FindPropertyRelative("SampleCount");
            _restirEstimationConfidenceCap = restirParamSet.FindPropertyRelative("ConfidenceCap");
            _restirEstimationSpatialSampleCount = restirParamSet.FindPropertyRelative("SpatialSampleCount");
            _restirEstimationSpatialFilterSize = restirParamSet.FindPropertyRelative("SpatialFilterSize");
            _restirEstimationValidationFrameInterval = restirParamSet.FindPropertyRelative("ValidationFrameInterval");
            _risEstimationCandidateCount = risParamSet.FindPropertyRelative("CandidateCount");
            _risEstimationTargetFunctionUpdateWeight = risParamSet.FindPropertyRelative("TargetFunctionUpdateWeight");

            _temporalSmoothing = patchFilteringParamSet.FindPropertyRelative("TemporalSmoothing");
            _spatialFilterEnabled = patchFilteringParamSet.FindPropertyRelative("SpatialFilterEnabled");
            _spatialFilterSampleCount = patchFilteringParamSet.FindPropertyRelative("SpatialFilterSampleCount");
            _spatialFilterRadius = patchFilteringParamSet.FindPropertyRelative("SpatialFilterRadius");
            _temporalPostFilterEnabled = patchFilteringParamSet.FindPropertyRelative("TemporalPostFilterEnabled");

            _lookupSampleCount = screenFilteringParamSet.FindPropertyRelative("LookupSampleCount");
            _upsamplingKernelSize = screenFilteringParamSet.FindPropertyRelative("UpsamplingKernelSize");
            _upsamplingSampleCount = screenFilteringParamSet.FindPropertyRelative("UpsamplingSampleCount");

            _gridSize = gridParamSet.FindPropertyRelative("GridSize");
            _voxelMinSize = gridParamSet.FindPropertyRelative("VoxelMinSize");
            _cascadeCount = gridParamSet.FindPropertyRelative("CascadeCount");
            _cascadeMovement = gridParamSet.FindPropertyRelative("CascadeMovement");

            _debugEnabled = paramSet.FindPropertyRelative("DebugEnabled");
            _debugViewMode = paramSet.FindPropertyRelative("DebugViewMode");
            _debugShowSamplePosition = paramSet.FindPropertyRelative("DebugShowSamplePosition");
        }

        public override void OnInspectorGUI()
        {
            if (!m_IsInitialized)
                Init();

            if (SceneView.lastActiveSceneView && !SceneView.lastActiveSceneView.sceneViewState.alwaysRefreshEnabled)
            {
                EditorGUILayout.HelpBox("Enable \"Always Refresh\" in the Scene View to see realtime updates in the Scene View.", MessageType.Info);
            }

            EditorGUILayout.LabelField("Light Transport", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_multiBounce, TextContent.MultiBounce);
            EditorGUILayout.PropertyField(_estimationMethod, TextContent.EstimationMethod);
            if (_estimationMethod.intValue == (int)SurfaceCacheEstimationMethod.Uniform)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Uniform Estimation", EditorStyles.boldLabel);
                EditorGUILayout.IntSlider(_uniformEstimationSampleCount, 1, 32, TextContent.UniformEstimationSampleCount);
            }
            else if (_estimationMethod.intValue == (int)SurfaceCacheEstimationMethod.Restir)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Restir Estimation", EditorStyles.boldLabel);
                EditorGUILayout.IntSlider(_restirEstimationConfidenceCap, 1, 64, TextContent.RestirEstimationConfidenceCap);
                EditorGUILayout.IntSlider(_restirEstimationSpatialSampleCount, 0, 8, TextContent.RestirEstimationSpatialSampleCount);
                EditorGUILayout.Slider(_restirEstimationSpatialFilterSize, 0.0f, 4.0f, TextContent.RestirEstimationSpatialFilterSize);
                EditorGUILayout.IntSlider(_restirEstimationValidationFrameInterval, 2, 8, TextContent.RestirEstimationValidationFrameInterval);
            }
            else if (_estimationMethod.intValue == (int)SurfaceCacheEstimationMethod.Ris)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("SH-Guided Ris Estimation", EditorStyles.boldLabel);
                EditorGUILayout.IntSlider(_risEstimationCandidateCount, 2, 64, TextContent.RisEstimationCandidateCount);
                EditorGUILayout.Slider(_risEstimationTargetFunctionUpdateWeight, 0.0f, 1.0f, TextContent.RisEstimationTargetFunctionUpdateWeight);
            }

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
            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            EditorGUILayout.IntSlider(_gridSize, 16, 64, TextContent.GridSize);
            EditorGUILayout.Slider(_voxelMinSize, 0.1f, 2.0f, TextContent.VoxelMinSize);
            EditorGUILayout.IntSlider(_cascadeCount, 1, (int)SurfaceCache.CascadeMax, TextContent.CascadeCount);
            EditorGUILayout.PropertyField(_cascadeMovement, TextContent.CascadeMovement);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debugging", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_debugEnabled, TextContent.DebugEnabled);
            EditorGUILayout.PropertyField(_debugViewMode, TextContent.DebugViewMode);
            EditorGUILayout.PropertyField(_debugShowSamplePosition, TextContent.DebugShowSamplePosition);
        }
    }
}

#endif
