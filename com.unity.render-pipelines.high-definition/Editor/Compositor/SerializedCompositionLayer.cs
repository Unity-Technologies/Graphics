using System.Collections.Generic;

using UnityEditor;
using UnityEngine.Rendering.HighDefinition.Compositor;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class SerializedCompositionLayer
    {
        public SerializedProperty LayerName;
        public SerializedProperty Show;
        public SerializedProperty ResolutionScale;
        public SerializedProperty ExpandLayer;
        public SerializedProperty OutTarget;
        public SerializedProperty OutputRenderer;
        public SerializedProperty ClearDepth;
        public SerializedProperty ClearAlpha;
        public SerializedProperty InputLayerType;
        public SerializedProperty InputCamera;
        public SerializedProperty InputVideo;
        public SerializedProperty InputTexture;
        public SerializedProperty FitType;
        public SerializedProperty ColorFormat;
        public SerializedProperty OverrideAA;
        public SerializedProperty AAMode;
        public SerializedProperty OverrideClearMode;
        public SerializedProperty ClearMode;
        public SerializedProperty OverrideCulling;
        public SerializedProperty CullingMaskProperty;
        public SerializedProperty OverrideVolume;
        public SerializedProperty VolumeMask;
        public SerializedProperty AOVBitmask;
        public SerializedProperty InputFilters;
        public SerializedProperty PositionInStack;

        public List<SerializedCompositionFilter> FilterList = new List<SerializedCompositionFilter>();

        public SerializedCompositionLayer(SerializedProperty root)
        {
            LayerName = root.FindPropertyRelative("m_LayerName");
            Show = root.FindPropertyRelative("m_Show");
            ResolutionScale = root.FindPropertyRelative("m_ResolutionScale");
            ExpandLayer = root.FindPropertyRelative("m_ExpandLayer");
            OutTarget = root.FindPropertyRelative("m_OutputTarget");
            ClearDepth = root.FindPropertyRelative("m_ClearDepth");
            ClearAlpha = root.FindPropertyRelative("m_ClearAlpha");
            OutputRenderer = root.FindPropertyRelative("m_OutputRenderer");
            InputLayerType = root.FindPropertyRelative("m_Type");
            InputCamera = root.FindPropertyRelative("m_Camera");
            InputVideo = root.FindPropertyRelative("m_InputVideo");
            InputTexture = root.FindPropertyRelative("m_InputTexture");
            FitType = root.FindPropertyRelative("m_BackgroundFit");
            ColorFormat = root.FindPropertyRelative("m_ColorBufferFormat");
            OverrideClearMode = root.FindPropertyRelative("m_OverrideClearMode");
            ClearMode = root.FindPropertyRelative("m_ClearMode");
            OverrideAA = root.FindPropertyRelative("m_OverrideAntialiasing");
            AAMode = root.FindPropertyRelative("m_Antialiasing");
            OverrideCulling = root.FindPropertyRelative("m_OverrideCullingMask");
            CullingMaskProperty = root.FindPropertyRelative("m_CullingMask");
            OverrideVolume = root.FindPropertyRelative("m_OverrideVolumeMask");
            VolumeMask = root.FindPropertyRelative("m_VolumeMask");
            AOVBitmask = root.FindPropertyRelative("m_AOVBitmask");
            InputFilters = root.FindPropertyRelative("m_InputFilters");
            PositionInStack = root.FindPropertyRelative("m_LayerPositionInStack");

            for (int index = 0; index < InputFilters.arraySize; index++)
            {
                var serializedFilter = InputFilters.GetArrayElementAtIndex(index);
                FilterList.Add(new SerializedCompositionFilter(serializedFilter));
            }
        }

        public float GetPropertiesHeight()
        {
            if (OutTarget.intValue != (int)CompositorLayer.OutputTarget.CameraStack)
            {
                return 
                    EditorGUI.GetPropertyHeight(OutputRenderer, null) +
                    EditorGUI.GetPropertyHeight(ColorFormat, null) +
                    EditorGUI.GetPropertyHeight(AOVBitmask, null) +
                    EditorGUI.GetPropertyHeight(ResolutionScale, null) +
                    2 * EditorGUIUtility.singleLineHeight; //for the heading and pading
            }
            else
            {
                return EditorGUI.GetPropertyHeight(LayerName, null) +
                EditorGUI.GetPropertyHeight(InputCamera, null) +
                EditorGUI.GetPropertyHeight(ClearDepth, null) +
                EditorGUI.GetPropertyHeight(ClearAlpha, null) +
                EditorGUI.GetPropertyHeight(ClearMode, null) +
                EditorGUI.GetPropertyHeight(AAMode, null) +
                EditorGUI.GetPropertyHeight(CullingMaskProperty, null) +
                EditorGUI.GetPropertyHeight(VolumeMask, null) +
                EditorGUI.GetPropertyHeight(InputFilters, null) +
                7 * EditorGUIUtility.singleLineHeight; //for the heading and pading
            }
        }

        public float GetListItemHeight()
        {
                int pading = 10;
                if (OutTarget.intValue != (int)CompositorLayer.OutputTarget.CameraStack)
                {
                    return CompositorStyle.k_ThumbnailSize + pading;
                }
                else
                {
                    return EditorGUIUtility.singleLineHeight + pading;
                }
            }
        }
}
