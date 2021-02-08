using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.Rendering.HighDefinition.Compositor;

using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    internal class CompositionLayerUI
    {
        static partial class Styles
        {
            // main layer
            static public readonly GUIContent k_Resolution = EditorGUIUtility.TrTextContent("Resolution", "Specifies the resolution of this layer's render target. Lower resolution increases the performance at the expense of visual quality.");
            static public readonly GUIContent k_BufferFormat = EditorGUIUtility.TrTextContent("Format", "Specifies the color buffer format of this layer. ");
            static public readonly GUIContent k_OutputRenderer = EditorGUIUtility.TrTextContent("Output Renderer", "Redirects the output of this layer to the surface which is drawn by the selected mesh renderer. ");
            static public readonly GUIContent k_AOVs = EditorGUIUtility.TrTextContent("AOVs", "Specifies the Arbitrary Output Variable (AOV) that will be drawn on this layer. This option affects all cameras that are stacked on this layer.");

            // Sub layer
            static public readonly GUIContent k_NameContent = EditorGUIUtility.TrTextContent("Layer Name", "Specifies the name of this layer.");
            static public readonly GUIContent k_Camera = EditorGUIUtility.TrTextContent("Source Camera", "Specifies the camera of the scene that will provide the content for this sublayer.");
            static public readonly GUIContent k_Image = EditorGUIUtility.TrTextContent("Source Image", "Specifies the image that will provide the content for this sublayer.");
            static public readonly GUIContent k_Video = EditorGUIUtility.TrTextContent("Source Video", "Specifies the video that will provide the content for this sublayer.");
            static public readonly GUIContent k_ClearDepth = EditorGUIUtility.TrTextContent("Clear Depth", "If enabled, the depth buffer will be cleared before rendering this layer. Not clearing the depth can be useful when stacking sublayers. The first sublayer of a stack always clears the depth.");
            static public readonly GUIContent k_ClearAlpha = EditorGUIUtility.TrTextContent("Clear Alpha", "If enabled, the alpha channel will be cleared before rendering this layer. If enabled, post processing will affect only the objects of this layer");
            static public readonly GUIContent k_ClearMode = EditorGUIUtility.TrTextContent("Clear Color", "To override the clear mode of this layer, activate the option by clicking on the check-box and then select the desired value. This can be changed only on the first sub layer of a stack (stacked sublayers do not clear the color).");
            static public readonly GUIContent k_AAMode = EditorGUIUtility.TrTextContent("Post Anti-aliasing", "To override the postprocess Anti-aliasing mode, activate the option by clicking on the check-box and then select the desired value.");
            static public readonly GUIContent k_CullingMask = EditorGUIUtility.TrTextContent("Culling Mask", "To override the culling mask, activate the option by clicking on the check-box and then select the desired value.");
            static public readonly GUIContent k_VolumeMask = EditorGUIUtility.TrTextContent("Volume Mask", "To override the volume mask, activate the option by clicking on the check-box and then select the desired value.");
            static public readonly GUIContent k_AlphaRange = EditorGUIUtility.TrTextContent("Alpha Range", "The range of alpha values used when transitioning from post-processed to plain image regions. A smaller range will result in a steeper transition.");

            static public readonly string k_AlphaInfoPost = "The use of AOVs properties in a player requires to enable the Runtime AOV API support in the HDRP quality settings.";

            static public readonly string k_ShaderCompilationWarning = "The Unity Editor is compiling the AOV shaders for the first time. The output might not be correct until the compilation is over.";

            static public float infoBoxIconWidth = 100;
        }

        static bool s_AsyncCompileState = false;
        static bool s_HasStartedCompiling = false;

        public static void DrawItemInList(Rect rect, SerializedCompositionLayer serialized, RenderTexture thumbnail, float aspectRatio, bool isAlphaEnbaled)
        {
            bool isCameraStack = serialized.outTarget.intValue == (int)CompositorLayer.OutputTarget.CameraStack;

            // Compute the desired indentation
            {
                const float listBorder = 2.0f;
                rect.x = isCameraStack ? rect.x + CompositorStyle.k_ListItemStackPading + listBorder : rect.x + listBorder;
                rect.width = isCameraStack ? rect.width - CompositorStyle.k_ListItemStackPading - listBorder : rect.width - listBorder;
                rect.y += CompositorStyle.k_ListItemPading;
                rect.height = EditorGUIUtility.singleLineHeight;
            }

            if (thumbnail)
            {
                Rect newRect = rect;
                newRect.width = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(newRect, serialized.show, GUIContent.none);
                rect.x += CompositorStyle.k_CheckboxSpacing;
                Rect previewRect = rect;
                previewRect.width = CompositorStyle.k_ThumbnailSize * aspectRatio;
                previewRect.height = CompositorStyle.k_ThumbnailSize;
                EditorGUI.DrawPreviewTexture(previewRect, thumbnail);
                previewRect.x += previewRect.width + CompositorStyle.k_ThumbnailDivider;
                rect.x += previewRect.width + CompositorStyle.k_ThumbnailSpacing;
                rect.width -= previewRect.width + CompositorStyle.k_ThumbnailSpacing;

                if (isAlphaEnbaled
                    && (thumbnail.format == RenderTextureFormat.ARGBHalf
                        || thumbnail.format == RenderTextureFormat.ARGBFloat
                        || thumbnail.format == RenderTextureFormat.ARGB64))
                {
                    EditorGUI.DrawTextureAlpha(previewRect, thumbnail);
                    rect.x += previewRect.width + CompositorStyle.k_ThumbnailSpacing;
                    rect.width -= previewRect.width + CompositorStyle.k_ThumbnailSpacing;
                }

                rect.y += CompositorStyle.k_LabelVerticalOffset;
                EditorGUI.LabelField(rect, serialized.layerName.stringValue);
            }
            else
            {
                Rect newRect = rect;
                newRect.width = EditorGUIUtility.singleLineHeight;
                EditorGUI.PropertyField(newRect, serialized.show, GUIContent.none);
                newRect.x += CompositorStyle.k_CheckboxSpacing;
                if (isCameraStack)
                {
                    Rect iconRect = newRect;
                    iconRect.width = CompositorStyle.k_IconSize;
                    iconRect.height = CompositorStyle.k_IconSize;
                    iconRect.y -= CompositorStyle.k_IconVerticalOffset;
                    switch (serialized.inputLayerType.enumValueIndex)
                    {
                        case (int)CompositorLayer.LayerType.Camera:
                            GUI.DrawTexture(iconRect, EditorGUIUtility.ObjectContent(null, typeof(Camera)).image);
                            break;
                        case (int)CompositorLayer.LayerType.Video:
                            GUI.DrawTexture(iconRect, EditorGUIUtility.ObjectContent(null, typeof(UnityEngine.Video.VideoClip)).image);
                            break;
                        case (int)CompositorLayer.LayerType.Image:
                            GUI.DrawTexture(iconRect, EditorGUIUtility.ObjectContent(null, typeof(Texture)).image);
                            break;
                        default:
                            // This will happen if someone adds a new layer type and does not update this switch statement
                            Debug.Log("Unknown layer type: Please add code here to draw this type of layer.");
                            break;
                    }
                    newRect.x += CompositorStyle.k_IconSize + CompositorStyle.k_IconSpacing;
                }

                newRect.width = rect.width - newRect.x;
                EditorGUI.LabelField(newRect, serialized.layerName.stringValue);
            }
        }

        public static void DrawOutputLayerProperties(Rect rect, SerializedCompositionLayer serializedProperties, System.Action resetRenderTargetCallback)
        {
            rect.y += CompositorStyle.k_ListItemPading;
            rect.height = CompositorStyle.k_SingleLineHeight;

            EditorGUI.PropertyField(rect, serializedProperties.colorFormat, Styles.k_BufferFormat);
            rect.y += CompositorStyle.k_Spacing;

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(rect, serializedProperties.resolutionScale, Styles.k_Resolution);

            if (EditorGUI.EndChangeCheck())
            {
                // if the resolution changes, reset the RTs
                resetRenderTargetCallback();
            }
            rect.y += CompositorStyle.k_Spacing;

            EditorGUI.PropertyField(rect, serializedProperties.outputRenderer, Styles.k_OutputRenderer);
            rect.y += CompositorStyle.k_Spacing;

            EditorGUI.PropertyField(rect, serializedProperties.aovBitmask, Styles.k_AOVs);
            rect.y += CompositorStyle.k_Spacing;

            if (serializedProperties.aovBitmask.intValue != 0)
            {
                // [case 1288744] Enable async compile for the debug shaders to avoid editor freeze when the user selects AOVs
                s_AsyncCompileState = ShaderUtil.allowAsyncCompilation;
                ShaderUtil.allowAsyncCompilation = true;
                // Note: We cannot check immediately if "anythingCompiling", this has to be delayed for the next frame
                if (s_HasStartedCompiling && ShaderUtil.anythingCompiling)
                {
                    // Display a message while we are compiling the shaders
                    Rect infoRect = rect;
                    // Compute the height of the infobox based on the width of the window and the amount of text
                    GUIStyle.none.CalcMinMaxWidth(new GUIContent(Styles.k_ShaderCompilationWarning), out float minWidth, out float maxWidth);
                    float lines = Mathf.Max(2, Mathf.CeilToInt(maxWidth / (rect.width - Styles.infoBoxIconWidth)));
                    infoRect.height = lines * CompositorStyle.k_Spacing;
                    EditorGUI.HelpBox(infoRect, Styles.k_ShaderCompilationWarning, MessageType.Warning);
                    rect.y += infoRect.height + EditorGUIUtility.standardVerticalSpacing;
                }
                else
                {
                    // If the shaders have finished compiling, set the async compilation to the previous state
                    if (s_HasStartedCompiling)
                    {
                        ShaderUtil.allowAsyncCompilation = s_AsyncCompileState;
                        s_HasStartedCompiling = false;
                    }
                    else
                    {
                        s_HasStartedCompiling = true;
                    }
                }
            }
            else
            {
                // Check if the user switched off the AOV before the shaders were compiled
                if (s_HasStartedCompiling)
                {
                    ShaderUtil.allowAsyncCompilation = s_AsyncCompileState;
                    s_HasStartedCompiling = false;
                }
            }

            HDRenderPipelineAsset hdrp = HDRenderPipeline.currentAsset;
            if (serializedProperties.aovBitmask.intValue != 0 && hdrp && !hdrp.currentPlatformRenderPipelineSettings.supportRuntimeAOVAPI)
            {
                Rect infoRect = rect;
                // Compute the height of the infobox based on the width of the window and the amount of text
                GUIStyle.none.CalcMinMaxWidth(new GUIContent(Styles.k_AlphaInfoPost), out float minWidth, out float maxWidth);
                float lines = Mathf.Max(2, Mathf.CeilToInt(maxWidth / (rect.width - Styles.infoBoxIconWidth)));
                infoRect.height = lines * CompositorStyle.k_Spacing;
                EditorGUI.HelpBox(infoRect, Styles.k_AlphaInfoPost, MessageType.Info);
                rect.y += infoRect.height + EditorGUIUtility.standardVerticalSpacing;
            }
        }

        public static void DrawStackedLayerProperties(Rect rect, SerializedCompositionLayer serializedProperties, ReorderableList filterList)
        {
            rect.y += CompositorStyle.k_ListItemPading;
            rect.height = CompositorStyle.k_SingleLineHeight;

            EditorGUI.PropertyField(rect, serializedProperties.layerName, Styles.k_NameContent);
            rect.y += CompositorStyle.k_Spacing;

            switch (serializedProperties.inputLayerType.enumValueIndex)
            {
                case (int)CompositorLayer.LayerType.Camera:
                    EditorGUI.PropertyField(rect, serializedProperties.inputCamera, Styles.k_Camera);
                    break;
                case (int)CompositorLayer.LayerType.Video:
                    EditorGUI.PropertyField(rect, serializedProperties.inputVideo, Styles.k_Video);
                    break;
                case (int)CompositorLayer.LayerType.Image:
                    EditorGUI.PropertyField(rect, serializedProperties.inputTexture, Styles.k_Image);
                    rect.y += CompositorStyle.k_Spacing;
                    EditorGUI.PropertyField(rect, serializedProperties.fitType);
                    break;
                default:
                    // This will happen if someone adds a new layer type and does not update this switch statement
                    Debug.Log("Unknown layer type: Please add code here to handle this type of layer.");
                    break;
            }
            rect.y += 1.5f * CompositorStyle.k_Spacing;

            using (new EditorGUI.DisabledScope(serializedProperties.positionInStack.intValue == 0))
            {
                EditorGUI.PropertyField(rect, serializedProperties.clearDepth, Styles.k_ClearDepth);
                rect.y += CompositorStyle.k_Spacing;
            }

            EditorGUI.PropertyField(rect, serializedProperties.clearAlpha, Styles.k_ClearAlpha);
            rect.y += 1.0f * CompositorStyle.k_Spacing;

            // Draw a min/max slider for tha alpha range
            {
                const float spacing = 5;
                var labelRect = new Rect(rect.x, rect.y, EditorGUIUtility.labelWidth, rect.height);
                EditorGUI.PrefixLabel(labelRect, Styles.k_AlphaRange);

                var minLabelRect = rect;
                minLabelRect.x += EditorGUIUtility.labelWidth;
                minLabelRect.width = EditorGUIUtility.fieldWidth / 2;
                serializedProperties.alphaMin.floatValue = EditorGUI.FloatField(minLabelRect, serializedProperties.alphaMin.floatValue);

                GUI.SetNextControlName("AlphaMinMaxSlider");
                var sliderRect = rect;
                sliderRect.x += EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth / 2 + spacing;
                sliderRect.width -= (EditorGUIUtility.labelWidth + EditorGUIUtility.fieldWidth + 2 * spacing);
                float minVal = serializedProperties.alphaMin.floatValue;
                float maxVal = serializedProperties.alphaMax.floatValue;

                EditorGUI.MinMaxSlider(sliderRect, ref minVal, ref maxVal, 0, 1);
                if (serializedProperties.alphaMin.floatValue != minVal || serializedProperties.alphaMax.floatValue != maxVal)
                {
                    // Note: We need to move the focus when the slider changes, otherwise the textField will not update
                    GUI.FocusControl("AlphaMinMaxSlider");
                    serializedProperties.alphaMin.floatValue = minVal;
                    serializedProperties.alphaMax.floatValue = maxVal;
                }

                var maxLabelRect = rect;
                maxLabelRect.x = sliderRect.x + sliderRect.width + spacing;
                maxLabelRect.width = EditorGUIUtility.fieldWidth / 2;
                serializedProperties.alphaMax.floatValue = EditorGUI.FloatField(maxLabelRect, serializedProperties.alphaMax.floatValue);

                // sanity checks
                if (serializedProperties.alphaMax.floatValue < serializedProperties.alphaMin.floatValue)
                {
                    serializedProperties.alphaMax.floatValue = serializedProperties.alphaMin.floatValue;
                }
                if (serializedProperties.alphaMax.floatValue > 1)
                {
                    serializedProperties.alphaMax.floatValue = 1;
                }
                if (serializedProperties.alphaMin.floatValue > serializedProperties.alphaMax.floatValue)
                {
                    serializedProperties.alphaMin.floatValue = serializedProperties.alphaMax.floatValue;
                }
                if (serializedProperties.alphaMin.floatValue < 0)
                {
                    serializedProperties.alphaMin.floatValue = 0;
                }
            }
            rect.y += 1.5f * CompositorStyle.k_Spacing;

            // The clear mode should be visible / configurable only for the first layer in the stack. For the other layers we set a camera-stacking specific clear-mode .
            using (new EditorGUI.DisabledScope(serializedProperties.positionInStack.intValue != 0))
            {
                DrawPropertyHelper(rect, Styles.k_ClearMode, serializedProperties.overrideClearMode, serializedProperties.clearMode);
                rect.y += CompositorStyle.k_Spacing;
            }

            DrawPropertyHelper(rect, Styles.k_AAMode, serializedProperties.overrideAA, serializedProperties.aaMode);
            rect.y += CompositorStyle.k_Spacing;

            DrawPropertyHelper(rect, Styles.k_CullingMask, serializedProperties.overrideCulling, serializedProperties.cullingMaskProperty);
            rect.y += CompositorStyle.k_Spacing;

            DrawPropertyHelper(rect, Styles.k_VolumeMask, serializedProperties.overrideVolume, serializedProperties.volumeMask);
            rect.y += CompositorStyle.k_Spacing;

            Rect filterRect = rect;
            filterRect.y += 0.5f * CompositorStyle.k_Spacing;
            filterList.DoList(filterRect);
        }

        static void DrawPropertyHelper(Rect rect, GUIContent label, SerializedProperty checkBox, SerializedProperty serializedProperty)
        {
            Rect rectCopy = rect;
            EditorGUI.PropertyField(rectCopy, checkBox, GUIContent.none);

            rectCopy.x += EditorGUIUtility.singleLineHeight;
            rectCopy.width = EditorGUIUtility.labelWidth - EditorGUIUtility.singleLineHeight;
            EditorGUI.LabelField(rectCopy, label);

            using (new EditorGUI.DisabledScope(!checkBox.boolValue))
            {
                float pad = EditorGUIUtility.labelWidth;
                rect.x += pad;
                rect.width -= rect.x;
                EditorGUI.PropertyField(rect, serializedProperty, GUIContent.none);
            }
        }
    }
}
