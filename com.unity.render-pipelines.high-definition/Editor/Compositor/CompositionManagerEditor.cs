using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.HighDefinition.Attributes;
using UnityEngine.Rendering.HighDefinition.Compositor;
using UnityEngine.Video;

using UnityEditor;
using UnityEditorInternal;

namespace UnityEditor.Rendering.HighDefinition.Compositor
{
    // Responsible for drawing the inspector UI of the composition manager
    [CustomEditor(typeof(CompositionManager))]
    internal class CompositionManagerEditor : Editor
    {
        static partial class Styles
        {
            static public readonly GUIContent k_CompositionGraph = EditorGUIUtility.TrTextContent("Composition Graph", "Specifies the Shader Graph that will be used to produce the final composited output.");
            static public readonly GUIContent k_OutputCamera = EditorGUIUtility.TrTextContent("Output Camera", "Specifies the camera that will output the final composited image.");
            static public readonly GUIContent k_EnablePreview = EditorGUIUtility.TrTextContent("Enable Preview", "When enabled, the compositor will generate the final composed frame even in edit mode.");
            static public readonly GUIContent k_InputFilters = EditorGUIUtility.TrTextContent("Input Filters", "A list of color filters that will be executed before composing the frame.");
            static public readonly GUIContent k_Properties = EditorGUIUtility.TrTextContent("Properties", "The properties of a layer or sub-layer.");
            static public readonly GUIContent k_RenderSchedule = EditorGUIUtility.TrTextContent("Render Schedule", "A list of layers and sub-layers in the scene. Layers are drawn from top to bottom.");
            static public readonly string k_AlphaWarningPipeline = "The rendering pipeline was not configured to output an alpha channel. You can select a color buffer format that supports alpha in the HDRP quality settings.";
            static public readonly string k_AlphaWarningPost = "The post processing system was not configured to process the alpha channel. You can select a buffer format that supports alpha in the HDRP quality settings.";
            static public readonly string k_ShaderWarning = "You must specify a composition graph to see an output from the compositor.";
        }

        ReorderableList m_layerList;
        ReorderableList m_filterList;

        // Cached serialized properties
        SerializedCompositionManager m_SerializedProperties;
        List<SerializedCompositionLayer> m_SerializedLayerProperties;
        List<SerializedShaderProperty> m_SerializedShaderProperties;

        bool m_IsEditorDirty = true;
        bool m_EnablePreview;
        bool layerListChange;
        CompositionManager m_compositionManager;

        public bool isDirty => m_IsEditorDirty;

        public int defaultSelection = -1;
        public int selectionIndex => m_layerList != null ? m_layerList.index : -1;

        void AddLayerOfTypeCallback(object type)
        {
            Undo.RecordObject(m_compositionManager, "Add compositor sublayer");
            m_compositionManager.AddNewLayer(m_layerList.index + 1, (CompositorLayer.LayerType)type);
            m_SerializedProperties.layerList.serializedObject.Update();
            m_compositionManager.UpdateLayerSetup();
        }

        void AddFilterOfTypeCallback(object type)
        {
            Undo.RecordObject(m_compositionManager, "Add input filter");
            m_compositionManager.AddInputFilterAtLayer(CompositionFilter.Create((CompositionFilter.FilterType)type), m_layerList.index);
            m_SerializedProperties.layerList.serializedObject.Update();
            CacheSerializedObjects();
        }

        void DrawCompositionParameters()
        {
            ShaderPropertyUI.Draw(m_SerializedShaderProperties);
        }

        public bool CacheSerializedObjects()
        {
            try
            {
                m_SerializedProperties = new SerializedCompositionManager(serializedObject);
            }
            catch (Exception)
            {
                return false;
            }
            
            m_SerializedLayerProperties = new List<SerializedCompositionLayer>();
            m_SerializedShaderProperties = new List<SerializedShaderProperty>();

            var serializedLayerList = m_SerializedProperties.layerList;
            for (int layerIndex = 0; layerIndex < serializedLayerList.arraySize; layerIndex++)
            {
                var serializedLayer = serializedLayerList.GetArrayElementAtIndex(layerIndex);
                m_SerializedLayerProperties.Add(new SerializedCompositionLayer(serializedLayer));
            }

            var serializedPropertyList = m_SerializedProperties.shaderProperties;
            if (m_SerializedProperties.shaderProperties == null)
            {
                return false;
            }
            for (int pIndex = 0; pIndex < serializedPropertyList.arraySize; pIndex++)
            {
                var serializedProperty = serializedPropertyList.GetArrayElementAtIndex(pIndex);
                m_SerializedShaderProperties.Add(new SerializedShaderProperty(serializedProperty));
            }

            return true;
        }

        void OnEnable()
        {
            CacheSerializedObjects();
            m_IsEditorDirty = false;
        }

        public override void OnInspectorGUI()
        {
            m_compositionManager = (CompositionManager)target;

            if (m_compositionManager == null)
            {
                Debug.LogError("Compositor target was null");
                return;
            }

            var headerStyle = EditorStyles.helpBox;
            headerStyle.fontSize = 14;

            // Cache the serialized property fields
            if (m_IsEditorDirty || m_SerializedProperties == null)
            {
                if (CacheSerializedObjects())
                {
                    m_IsEditorDirty = false;
                }
                else
                {
                    return;
                }
            }
            m_SerializedProperties.Update();

            m_EnablePreview = EditorGUILayout.Toggle(Styles.k_EnablePreview, m_compositionManager.enableOutput);
            {
                m_compositionManager.enableOutput = m_EnablePreview;
            }

            bool cameraChange = false;
            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_SerializedProperties.outputCamera, Styles.k_OutputCamera);
            if (EditorGUI.EndChangeCheck())
            {
                cameraChange = true;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(m_SerializedProperties.compositionShader, Styles.k_CompositionGraph);

            bool shaderChange = false;
            if (EditorGUI.EndChangeCheck())
            {
                // Clear the existing shader (the new shader will be loaded in the next Update)
                m_IsEditorDirty = true;
                shaderChange = true;
            }
            if (m_compositionManager.shader == null)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(Styles.k_ShaderWarning, MessageType.Error);
            }

            EditorGUILayout.PropertyField(m_SerializedProperties.displayNumber);

            // Draw some warnings in case alpha is not fully supported
            if (m_compositionManager.alphaSupport == CompositionManager.AlphaChannelSupport.None)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(Styles.k_AlphaWarningPipeline, MessageType.Warning);
            }
            else if (m_compositionManager.alphaSupport == CompositionManager.AlphaChannelSupport.Rendering)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.HelpBox(Styles.k_AlphaWarningPost, MessageType.Warning);
            }

            // Now draw the composition shader properties
            DrawCompositionParameters();

            // Now draw the list of layers
            EditorGUILayout.Separator();

            layerListChange = false;
            if (m_layerList == null)
            {
                var serializedLayerList = m_SerializedProperties.layerList;
                m_layerList = new ReorderableList(m_SerializedProperties.compositorSO, serializedLayerList, true, false, true, true);

                // Pre-select the "default" item in the list (used to remember the last selected item when re-creating the Editor) 
                if (defaultSelection >= 0)
                {
                    m_layerList.index = Math.Min(defaultSelection, m_layerList.count-1);
                }

                m_layerList.drawHeaderCallback = (Rect rect) =>
                {
                };

                m_layerList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    if (index < m_SerializedLayerProperties.Count)
                    {
                        var serializedProperties = m_SerializedLayerProperties[index];
                        CompositionLayerUI.DrawItemInList(rect, serializedProperties, m_compositionManager.GetRenderTarget(index), m_compositionManager.aspectRatio, m_compositionManager.alphaSupport != CompositionManager.AlphaChannelSupport.None);
                    }
                };

                m_layerList.onReorderCallbackWithDetails += (list, oldIndex, newIndex) =>
                {
                    layerListChange = true;
                    m_IsEditorDirty = true;

                    m_compositionManager.ReorderChildren(oldIndex, newIndex);
                    if (!m_compositionManager.ValidateLayerListOrder(oldIndex, newIndex))
                    {
                        // The new position is invalid, so set the currently selected layer to the old/starting position s
                        m_layerList.index = oldIndex; 
                    }
                };

                m_layerList.elementHeightCallback = (index) =>
                {
                    if (index < m_SerializedLayerProperties.Count)
                    {
                        return m_SerializedLayerProperties[index].GetListItemHeight();
                    }
                    return 0;
                };

                m_layerList.onAddCallback = (list) =>
                {
                    var menu = new GenericMenu();
                    menu.AddItem(new GUIContent("Image"), false, AddLayerOfTypeCallback, CompositorLayer.LayerType.Image);
                    menu.AddItem(new GUIContent("Video"), false, AddLayerOfTypeCallback, CompositorLayer.LayerType.Video);
                    menu.AddItem(new GUIContent("Camera"), false, AddLayerOfTypeCallback, CompositorLayer.LayerType.Camera);
                    menu.ShowAsContext();
                    m_IsEditorDirty = true;

                    m_SerializedProperties.layerList.serializedObject.Update();
                    EditorUtility.SetDirty(m_compositionManager.profile);
                };

                m_layerList.onRemoveCallback = (list) =>
                {
                    Undo.RecordObject(m_compositionManager, "Remove compositor sublayer");
                    m_compositionManager.RemoveLayerAtIndex(list.index);
                    m_IsEditorDirty = true;
                    EditorUtility.SetDirty(m_compositionManager.profile);
                };

                m_layerList.onSelectCallback = (index) =>
                {
                    m_filterList = null;
                };

                m_layerList.onCanRemoveCallback = (list) =>
                {
                    return !m_compositionManager.IsOutputLayer(list.index);
                };
                m_layerList.onCanAddCallback = (list) =>
                {
                    return list.index >= 0;
                };
                m_layerList.headerHeight = 0;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(Styles.k_RenderSchedule, headerStyle);
            m_layerList.DoLayoutList();
            EditorGUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck())
            {
                layerListChange = true;
            }

            float height = EditorGUIUtility.singleLineHeight;
            if (m_layerList.index >= 0)
            {
                height += m_SerializedLayerProperties[m_layerList.index].GetPropertiesHeight();
            }

            var rectangle = EditorGUILayout.BeginVertical(GUILayout.Height(height));

            EditorGUI.BeginChangeCheck();
            if (m_layerList.index >= 0)
            {
                EditorGUILayout.LabelField(Styles.k_Properties, headerStyle);

                rectangle.y += EditorGUIUtility.singleLineHeight * 1.5f;
                rectangle.x += 5;
                rectangle.width -= 10;
                var serializedProperties = m_SerializedLayerProperties[m_layerList.index];
                DrawLayerProperties(rectangle, serializedProperties, m_layerList.index, null);
            }
            EditorGUILayout.EndVertical();
            if (EditorGUI.EndChangeCheck())
            {
                // Also the layers might need to be re-initialized
                layerListChange = true;
            }

            if (m_SerializedProperties != null)
            {
                m_SerializedProperties.ApplyModifiedProperties();
            }

            if (shaderChange)
            {
                // This needs to run after m_SerializedProperties.ApplyModifiedProperties
                CompositionUtils.LoadOrCreateCompositionProfileAsset(m_compositionManager);
                m_compositionManager.SetupCompositionMaterial();
                CompositionUtils.SetDefaultLayers(m_compositionManager);
            }

            if (cameraChange)
            {
                m_compositionManager.DropCompositorCamera();
                m_compositionManager.Init();
            }

            if (layerListChange)
            {
                // Some properties were changed, mark the profile as dirty so it can be saved if the user saves the scene
                EditorUtility.SetDirty(m_compositionManager);
                EditorUtility.SetDirty(m_compositionManager.profile);
                m_compositionManager.DeleteLayerRTs();
                m_compositionManager.UpdateLayerSetup();
            }
        }
 
        void DrawLayerProperties(Rect rect, SerializedCompositionLayer serializedProperties, int layerIndex, RenderTexture preview = null)
        {
            if (serializedProperties == null)
            {
                return;
            }

            if (serializedProperties.outTarget.intValue != (int)CompositorLayer.OutputTarget.CameraStack)
            {
                CompositionLayerUI.DrawOutputLayerProperties(rect, serializedProperties, m_compositionManager.DeleteLayerRTs);
            }
            else
            {
                if (m_filterList == null)
                {
                    m_filterList = new ReorderableList(serializedProperties.inputFilters.serializedObject, serializedProperties.inputFilters, true, true, true, true);
                    m_filterList.onAddCallback = (list) =>
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Chroma Keying"), false, AddFilterOfTypeCallback, 0);
                        menu.AddItem(new GUIContent("Alpha Mask"), false, AddFilterOfTypeCallback, 1);
                        menu.ShowAsContext();

                        EditorUtility.SetDirty(m_compositionManager.profile);
                        EditorUtility.SetDirty(m_compositionManager);
                    };

                    m_filterList.drawElementCallback = (Rect r, int index, bool isActive, bool isFocused) =>
                    {
                        if (index < m_SerializedLayerProperties[m_layerList.index].filterList.Count)
                        { 
                            var serializedFilter = m_SerializedLayerProperties[m_layerList.index].filterList[index];
                            CompositionFilterUI.Draw(r, serializedFilter);
                        }
                    };

                    m_filterList.drawNoneElementCallback = (Rect r) =>
                    {
                        using (new EditorGUI.DisabledScope(true))
                        {
                            EditorGUI.LabelField(r, "List Is Empty", EditorStyles.label);
                        }
                    };

                    m_filterList.drawHeaderCallback = (Rect r) =>
                    {
                        EditorGUI.LabelField(r, Styles.k_InputFilters, EditorStyles.largeLabel);
                    };

                    m_filterList.elementHeightCallback = (index) =>
                    {
                        if (index < m_SerializedLayerProperties[m_layerList.index].filterList.Count)
                        {
                            var filter = m_SerializedLayerProperties[m_layerList.index].filterList[index];
                            return filter.GetHeight();
                        }
                        return CompositorStyle.k_Spacing;
                    };
                }

                CompositionLayerUI.DrawStackedLayerProperties(rect, serializedProperties, m_filterList);
            }
        }

    }
}


