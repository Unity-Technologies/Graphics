using System.Collections.Generic;
using UnityEngine.Rendering.HighDefinition.Compositor;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class SerializedCompositionLayer
    {
        public SerializedProperty layerName;
        public SerializedProperty show;
        public SerializedProperty resolutionScale;
        public SerializedProperty expandLayer;
        public SerializedProperty outTarget;
        public SerializedProperty outputRenderer;
        public SerializedProperty clearDepth;
        public SerializedProperty clearAlpha;
        public SerializedProperty inputLayerType;
        public SerializedProperty inputCamera;
        public SerializedProperty inputVideo;
        public SerializedProperty inputTexture;
        public SerializedProperty fitType;
        public SerializedProperty colorFormat;
        public SerializedProperty overrideAA;
        public SerializedProperty aaMode;
        public SerializedProperty overrideClearMode;
        public SerializedProperty clearMode;
        public SerializedProperty overrideCulling;
        public SerializedProperty cullingMaskProperty;
        public SerializedProperty overrideVolume;
        public SerializedProperty volumeMask;
        public SerializedProperty aovBitmask;
        public SerializedProperty inputFilters;
        public SerializedProperty positionInStack;
        public SerializedProperty alphaMin;
        public SerializedProperty alphaMax;

        public List<SerializedCompositionFilter> filterList = new List<SerializedCompositionFilter>();

        public SerializedCompositionLayer(SerializedProperty root)
        {
            layerName = root.FindPropertyRelative("m_LayerName");
            show = root.FindPropertyRelative("m_Show");
            resolutionScale = root.FindPropertyRelative("m_ResolutionScale");
            expandLayer = root.FindPropertyRelative("m_ExpandLayer");
            outTarget = root.FindPropertyRelative("m_OutputTarget");
            clearDepth = root.FindPropertyRelative("m_ClearDepth");
            clearAlpha = root.FindPropertyRelative("m_ClearAlpha");
            outputRenderer = root.FindPropertyRelative("m_OutputRenderer");
            inputLayerType = root.FindPropertyRelative("m_Type");
            inputCamera = root.FindPropertyRelative("m_Camera");
            inputVideo = root.FindPropertyRelative("m_InputVideo");
            inputTexture = root.FindPropertyRelative("m_InputTexture");
            fitType = root.FindPropertyRelative("m_BackgroundFit");
            colorFormat = root.FindPropertyRelative("m_ColorBufferFormat");
            overrideClearMode = root.FindPropertyRelative("m_OverrideClearMode");
            clearMode = root.FindPropertyRelative("m_ClearMode");
            overrideAA = root.FindPropertyRelative("m_OverrideAntialiasing");
            aaMode = root.FindPropertyRelative("m_Antialiasing");
            overrideCulling = root.FindPropertyRelative("m_OverrideCullingMask");
            cullingMaskProperty = root.FindPropertyRelative("m_CullingMask");
            overrideVolume = root.FindPropertyRelative("m_OverrideVolumeMask");
            volumeMask = root.FindPropertyRelative("m_VolumeMask");
            aovBitmask = root.FindPropertyRelative("m_AOVBitmask");
            inputFilters = root.FindPropertyRelative("m_InputFilters");
            positionInStack = root.FindPropertyRelative("m_LayerPositionInStack");
            alphaMin = root.FindPropertyRelative("m_AlphaMin");
            alphaMax = root.FindPropertyRelative("m_AlphaMax");

            for (int index = 0; index < inputFilters.arraySize; index++)
            {
                var serializedFilter = inputFilters.GetArrayElementAtIndex(index);
                filterList.Add(new SerializedCompositionFilter(serializedFilter));
            }
        }

        public float GetPropertiesHeight()
        {
            if (outTarget.intValue != (int)CompositorLayer.OutputTarget.CameraStack)
            {
                return
                    EditorGUI.GetPropertyHeight(outputRenderer, null) +
                    EditorGUI.GetPropertyHeight(colorFormat, null) +
                    EditorGUI.GetPropertyHeight(aovBitmask, null) +
                    EditorGUI.GetPropertyHeight(resolutionScale, null) +
                    2 * EditorGUIUtility.singleLineHeight; //for the heading and pading
            }
            else
            {
                float height =
                    EditorGUI.GetPropertyHeight(layerName, null) +
                    EditorGUI.GetPropertyHeight(inputCamera, null) +
                    EditorGUI.GetPropertyHeight(clearDepth, null) +
                    EditorGUI.GetPropertyHeight(clearAlpha, null) +
                    EditorGUI.GetPropertyHeight(clearMode, null) +
                    EditorGUI.GetPropertyHeight(aaMode, null) +
                    EditorGUI.GetPropertyHeight(cullingMaskProperty, null) +
                    EditorGUI.GetPropertyHeight(volumeMask, null) +
                    EditorGUI.GetPropertyHeight(inputFilters, null) +
                    EditorGUI.GetPropertyHeight(alphaMin, null) +   // we use a min/max slider in the UI so it takes a sinle line, so we don't need to count the alphaMax
                    EditorGUIUtility.singleLineHeight * 7;          // for the heading and pading

                if (inputFilters.arraySize > 0)
                {
                    // add extra height for the list of filters
                    height += inputFilters.arraySize * EditorGUIUtility.singleLineHeight * 5;
                }

                return height;
            }
        }

        public float GetListItemHeight()
        {
            int pading = 10;
            if (outTarget.intValue != (int)CompositorLayer.OutputTarget.CameraStack)
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
