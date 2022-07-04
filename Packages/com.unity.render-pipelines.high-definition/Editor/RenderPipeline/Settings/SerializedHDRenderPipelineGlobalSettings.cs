using System; //Type
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal; //ReorderableList
using UnityEngine; //ScriptableObject
using UnityEngine.Rendering; //CoreUtils.Destroy
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDRenderPipelineGlobalSettings : ISerializedRenderPipelineGlobalSettings
    {
        #region ISerializedRenderPipelineGlobalSettings
        public SerializedObject serializedObject { get; }
        public SerializedProperty shaderVariantLogLevel { get; }
        public SerializedProperty exportShaderVariants { get; }
        #endregion

        public SerializedProperty renderPipelineResources;
        public SerializedProperty renderPipelineRayTracingResources;

        public SerializedFrameSettings defaultCameraFrameSettings;
        public SerializedFrameSettings defaultBakedOrCustomReflectionFrameSettings;
        public SerializedFrameSettings defaultRealtimeReflectionFrameSettings;

        public SerializedProperty defaultVolumeProfile;
        public SerializedProperty lookDevVolumeProfile;

        public SerializedProperty lightLayerName0;
        public SerializedProperty lightLayerName1;
        public SerializedProperty lightLayerName2;
        public SerializedProperty lightLayerName3;
        public SerializedProperty lightLayerName4;
        public SerializedProperty lightLayerName5;
        public SerializedProperty lightLayerName6;
        public SerializedProperty lightLayerName7;

        public SerializedProperty decalLayerName0;
        public SerializedProperty decalLayerName1;
        public SerializedProperty decalLayerName2;
        public SerializedProperty decalLayerName3;
        public SerializedProperty decalLayerName4;
        public SerializedProperty decalLayerName5;
        public SerializedProperty decalLayerName6;
        public SerializedProperty decalLayerName7;

        public SerializedProperty lensAttenuation;
        public SerializedProperty colorGradingSpace;
        public SerializedProperty supportRuntimeDebugDisplay;
        public SerializedProperty autoRegisterDiffusionProfiles;

        public SerializedProperty rendererListCulling;

        public SerializedProperty DLSSProjectId;
        public SerializedProperty useDLSSCustomProjectId;

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
            defaultCameraFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_RenderingPathDefaultCameraFrameSettings"), null); //no overrides in HDRPAsset
            defaultBakedOrCustomReflectionFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings"), null); //no overrides in HDRPAsset
            defaultRealtimeReflectionFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_RenderingPathDefaultRealtimeReflectionFrameSettings"), null); //no overrides in HDRPAsset

            InitializeCustomPostProcessesLists();

            defaultVolumeProfile = serializedObject.FindProperty("m_DefaultVolumeProfile");
            lookDevVolumeProfile = serializedObject.FindProperty("m_LookDevVolumeProfile");

            lightLayerName0 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.lightLayerName0);
            lightLayerName1 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.lightLayerName1);
            lightLayerName2 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.lightLayerName2);
            lightLayerName3 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.lightLayerName3);
            lightLayerName4 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.lightLayerName4);
            lightLayerName5 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.lightLayerName5);
            lightLayerName6 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.lightLayerName6);
            lightLayerName7 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.lightLayerName7);

            decalLayerName0 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.decalLayerName0);
            decalLayerName1 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.decalLayerName1);
            decalLayerName2 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.decalLayerName2);
            decalLayerName3 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.decalLayerName3);
            decalLayerName4 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.decalLayerName4);
            decalLayerName5 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.decalLayerName5);
            decalLayerName6 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.decalLayerName6);
            decalLayerName7 = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.decalLayerName7);

            shaderVariantLogLevel = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.shaderVariantLogLevel);
            exportShaderVariants = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.exportShaderVariants);

            lensAttenuation = serializedObject.FindProperty("lensAttenuationMode");
            colorGradingSpace = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.colorGradingSpace);
            rendererListCulling = serializedObject.FindProperty("rendererListCulling");

            supportRuntimeDebugDisplay = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.supportRuntimeDebugDisplay);
            autoRegisterDiffusionProfiles = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.autoRegisterDiffusionProfiles);

            DLSSProjectId = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.DLSSProjectId);
            useDLSSCustomProjectId = serializedObject.Find((HDRenderPipelineGlobalSettings s) => s.useDLSSCustomProjectId);

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
