using System; //Type
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal; //ReorderableList
using UnityEngine; //ScriptableObject
using UnityEngine.Rendering; //CoreUtils.Destroy
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDRenderPipelineGlobalSettings
    {
        public SerializedObject serializedObject;

        public SerializedProperty renderPipelineResources;
        public SerializedProperty renderPipelineRayTracingResources;

        public SerializedProperty defaultVolumeProfile;
        public SerializedProperty lookDevVolumeProfile;

        public SerializedProperty defaultRenderingLayerMask;
        public SerializedProperty renderingLayerNames;
        internal ReorderableList renderingLayerNamesList;

        public SerializedProperty lensAttenuation;
        public SerializedProperty colorGradingSpace;
        public SerializedProperty specularFade;
        public SerializedProperty autoRegisterDiffusionProfiles;

        public SerializedProperty analyticDerivativeEmulation;
        public SerializedProperty analyticDerivativeDebugOutput;

        public SerializedProperty rendererListCulling;

        public SerializedProperty apvScenesData;

        internal ReorderableList uiBeforeTransparentCustomPostProcesses;
        internal ReorderableList uiBeforeTAACustomPostProcesses;
        internal ReorderableList uiBeforePostProcessCustomPostProcesses;
        internal ReorderableList uiAfterPostProcessBlursCustomPostProcesses;
        internal ReorderableList uiAfterPostProcessCustomPostProcesses;

        //RenderPipelineResources not always exist and thus cannot be serialized normally.
        bool? m_HasEditorResourceHasMultipleDifferentValues;
        public bool editorResourceHasMultipleDifferentValues
        {
            get
            {
                if (m_HasEditorResourceHasMultipleDifferentValues.HasValue)
                    return m_HasEditorResourceHasMultipleDifferentValues.Value;

                if (serializedObject.targetObjects.Length < 2)
                {
                    m_HasEditorResourceHasMultipleDifferentValues = false;
                }
                else
                {
                    m_HasEditorResourceHasMultipleDifferentValues = serializedSettings.Skip(1).Any(t => t.renderPipelineEditorResources != firstEditorResources);
                }

                return m_HasEditorResourceHasMultipleDifferentValues.Value;
            }
        }

        public HDRenderPipelineEditorResources firstEditorResources
            => serializedSettings[0]?.renderPipelineEditorResources;

        private List<HDRenderPipelineGlobalSettings> serializedSettings = new List<HDRenderPipelineGlobalSettings>();

        public SerializedHDRenderPipelineGlobalSettings(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            // do the cast only once
            foreach (var currentSetting in serializedObject.targetObjects)
            {
                if (currentSetting is HDRenderPipelineGlobalSettings hdrpSettings)
                    serializedSettings.Add(hdrpSettings);
                else
                    throw new Exception($"Target object has an invalid object, objects must be of type {typeof(HDRenderPipelineGlobalSettings)}");
            }

            renderPipelineResources = serializedObject.FindProperty("m_RenderPipelineResources");
            renderPipelineRayTracingResources = serializedObject.FindProperty("m_RenderPipelineRayTracingResources");

            var serializedRenderingPathProperty = serializedObject.FindProperty("m_RenderingPath");
            if (serializedRenderingPathProperty == null)
                throw new Exception($"Unable to find m_RenderingPath property on object {typeof(HDRenderPipelineGlobalSettings)}");

            InitializeCustomPostProcessesLists();

            defaultVolumeProfile = serializedObject.FindProperty("m_DefaultVolumeProfile");
            lookDevVolumeProfile = serializedObject.FindProperty("m_LookDevVolumeProfile");

            defaultRenderingLayerMask = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.defaultRenderingLayerMask);
            renderingLayerNames = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.renderingLayerNames);
            renderingLayerNamesList = new ReorderableList(serializedObject, renderingLayerNames, false, false, true, true)
            {
                drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
                {
                    rect.y += 2.5f;
                    SerializedProperty element = renderingLayerNames.GetArrayElementAtIndex(index);
                    EditorGUI.PropertyField(rect, element, EditorGUIUtility.TrTextContent($"Layer {index}"), true);

                    if (element.stringValue == "")
                    {
                        element.stringValue = HDRenderPipelineGlobalSettings.GetDefaultLayerName(index);
                        GUI.changed = true;
                    }
                },
                onCanRemoveCallback = (ReorderableList list) => list.IsSelected(list.count - 1) && !list.IsSelected(0),
                onCanAddCallback = (ReorderableList list) => list.count < 16,
                onAddCallback = (ReorderableList list) =>
                {
                    int index = list.count;
                    list.serializedProperty.arraySize = list.count + 1;
                    list.serializedProperty.GetArrayElementAtIndex(index).stringValue = HDRenderPipelineGlobalSettings.GetDefaultLayerName(index);
                },
            };

            // HDRP
            lensAttenuation = serializedObject.FindProperty("lensAttenuationMode");
            colorGradingSpace = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.colorGradingSpace);
            rendererListCulling = serializedObject.FindProperty("rendererListCulling");

            specularFade               = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.specularFade);
            autoRegisterDiffusionProfiles = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.autoRegisterDiffusionProfiles);

            analyticDerivativeEmulation = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.analyticDerivativeEmulation);
            analyticDerivativeDebugOutput = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.analyticDerivativeDebugOutput);

            apvScenesData = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.apvScenesData);
        }

        void InitializeCustomPostProcessesLists()
        {
            var ppVolumeTypeInjectionPoints = new Dictionary<Type, CustomPostProcessInjectionPoint>();

            var ppVolumeTypes = TypeCache.GetTypesDerivedFrom<CustomPostProcessVolumeComponent>();
            foreach (var ppVolumeType in ppVolumeTypes.Where(t => !t.IsAbstract))
            {
                var comp = ScriptableObject.CreateInstance(ppVolumeType) as CustomPostProcessVolumeComponent;
                ppVolumeTypeInjectionPoints[ppVolumeType] = comp.injectionPoint;
                CoreUtils.Destroy(comp);
            }

            var globalSettings = serializedObject.targetObject as HDRenderPipelineGlobalSettings;
            InitList(ref uiBeforeTransparentCustomPostProcesses, globalSettings.beforeTransparentCustomPostProcesses, "After Opaque And Sky", CustomPostProcessInjectionPoint.AfterOpaqueAndSky);
            InitList(ref uiBeforePostProcessCustomPostProcesses, globalSettings.beforePostProcessCustomPostProcesses, "Before Post Process", CustomPostProcessInjectionPoint.BeforePostProcess);
            InitList(ref uiAfterPostProcessBlursCustomPostProcesses, globalSettings.afterPostProcessBlursCustomPostProcesses, "After Post Process Blurs", CustomPostProcessInjectionPoint.AfterPostProcessBlurs);
            InitList(ref uiAfterPostProcessCustomPostProcesses, globalSettings.afterPostProcessCustomPostProcesses, "After Post Process", CustomPostProcessInjectionPoint.AfterPostProcess);
            InitList(ref uiBeforeTAACustomPostProcesses, globalSettings.beforeTAACustomPostProcesses, "Before TAA", CustomPostProcessInjectionPoint.BeforeTAA);

            void InitList(ref ReorderableList reorderableList, List<string> customPostProcessTypes, string headerName, CustomPostProcessInjectionPoint injectionPoint)
            {
                // Sanitize the list
                customPostProcessTypes.RemoveAll(s => Type.GetType(s) == null);

                reorderableList = new ReorderableList(customPostProcessTypes, typeof(string));
                reorderableList.drawHeaderCallback = (rect) =>
                    EditorGUI.LabelField(rect, headerName, EditorStyles.label);
                reorderableList.drawElementCallback = (rect, index, isActive, isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    var elemType = Type.GetType(customPostProcessTypes[index]);
                    EditorGUI.LabelField(rect, elemType.ToString(), EditorStyles.boldLabel);
                };
                reorderableList.onAddCallback = (list) =>
                {
                    var menu = new GenericMenu();

                    foreach (var kp in ppVolumeTypeInjectionPoints)
                    {
                        if (kp.Value == injectionPoint && !customPostProcessTypes.Contains(kp.Key.AssemblyQualifiedName))
                            menu.AddItem(new GUIContent(kp.Key.ToString()), false, () =>
                            {
                                Undo.RegisterCompleteObjectUndo(serializedObject.targetObject, $"Added {kp.Key.ToString()} Custom Post Process");
                                customPostProcessTypes.Add(kp.Key.AssemblyQualifiedName);
                            });
                    }

                    if (menu.GetItemCount() == 0)
                        menu.AddDisabledItem(new GUIContent("No Custom Post Process Available"));

                    menu.ShowAsContext();
                    EditorUtility.SetDirty(serializedObject.targetObject);
                };
                reorderableList.onRemoveCallback = (list) =>
                {
                    Undo.RegisterCompleteObjectUndo(serializedObject.targetObject, $"Removed {list.list[list.index].ToString()} Custom Post Process");
                    customPostProcessTypes.RemoveAt(list.index);
                    EditorUtility.SetDirty(serializedObject.targetObject);
                };
                reorderableList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                reorderableList.onReorderCallback = (list) =>
                {
                    EditorUtility.SetDirty(serializedObject.targetObject);
                };
            }
        }
    }
}
