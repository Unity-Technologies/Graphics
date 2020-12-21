using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEditorInternal; //ReorderableList
using UnityEngine; //ScriptableObject
using UnityEngine.Rendering; //CoreUtils.Destroy
using System; //Type

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedHDDefaultSettings
    {
        public SerializedObject serializedObject;

        public SerializedProperty renderPipelineResources;
        public SerializedProperty renderPipelineRayTracingResources;

        public SerializedFrameSettings defaultFrameSettings;
        public SerializedFrameSettings defaultBakedOrCustomReflectionFrameSettings;
        public SerializedFrameSettings defaultRealtimeReflectionFrameSettings;

        public SerializedProperty volumeProfileDefault;
        public SerializedProperty volumeProfileLookDev;

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

        public SerializedProperty shaderVariantLogLevel;
        public SerializedProperty lensAttenuation;
        public SerializedProperty diffusionProfileSettingsList;

        //RenderPipelineResources not always exist and thus cannot be serialized normally.
        public bool editorResourceHasMultipleDifferentValues
        {
            get
            {
                var initialValue = firstEditorResources;
                for (int index = 1; index < serializedObject.targetObjects.Length; ++index)
                {
                    if (initialValue != (serializedObject.targetObjects[index] as HDDefaultSettings)?.renderPipelineEditorResources)
                        return true;
                }
                return false;
            }
        }

        public HDRenderPipelineEditorResources firstEditorResources
            => (serializedObject.targetObjects[0] as HDDefaultSettings)?.renderPipelineEditorResources;

        public SerializedHDDefaultSettings(SerializedObject serializedObject)
        {
            this.serializedObject = serializedObject;

            renderPipelineResources = serializedObject.FindProperty("m_RenderPipelineResources");
            renderPipelineRayTracingResources = serializedObject.FindProperty("m_RenderPipelineRayTracingResources");
            defaultFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_RenderingPathDefaultCameraFrameSettings"), null); //no overrides in HDRPAsset
            defaultBakedOrCustomReflectionFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings"), null); //no overrides in HDRPAsset
            defaultRealtimeReflectionFrameSettings = new SerializedFrameSettings(serializedObject.FindProperty("m_RenderingPathDefaultRealtimeReflectionFrameSettings"), null); //no overrides in HDRPAsset

            // We are using ReorderableList for the UI. Since the integration with SerializedProperty is still WIP, in the meantime we will not use one
            InitializeCustomPostProcessesLists();

            volumeProfileDefault  = serializedObject.FindProperty("m_VolumeProfileDefault");
            volumeProfileLookDev  = serializedObject.FindProperty("m_VolumeProfileLookDev");

            lightLayerName0 = serializedObject.Find((HDDefaultSettings s) => s.lightLayerName0);
            lightLayerName1 = serializedObject.Find((HDDefaultSettings s) => s.lightLayerName1);
            lightLayerName2 = serializedObject.Find((HDDefaultSettings s) => s.lightLayerName2);
            lightLayerName3 = serializedObject.Find((HDDefaultSettings s) => s.lightLayerName3);
            lightLayerName4 = serializedObject.Find((HDDefaultSettings s) => s.lightLayerName4);
            lightLayerName5 = serializedObject.Find((HDDefaultSettings s) => s.lightLayerName5);
            lightLayerName6 = serializedObject.Find((HDDefaultSettings s) => s.lightLayerName6);
            lightLayerName7 = serializedObject.Find((HDDefaultSettings s) => s.lightLayerName7);

            decalLayerName0 = serializedObject.Find((HDDefaultSettings s) => s.decalLayerName0);
            decalLayerName1 = serializedObject.Find((HDDefaultSettings s) => s.decalLayerName1);
            decalLayerName2 = serializedObject.Find((HDDefaultSettings s) => s.decalLayerName2);
            decalLayerName3 = serializedObject.Find((HDDefaultSettings s) => s.decalLayerName3);
            decalLayerName4 = serializedObject.Find((HDDefaultSettings s) => s.decalLayerName4);
            decalLayerName5 = serializedObject.Find((HDDefaultSettings s) => s.decalLayerName5);
            decalLayerName6 = serializedObject.Find((HDDefaultSettings s) => s.decalLayerName6);
            decalLayerName7 = serializedObject.Find((HDDefaultSettings s) => s.decalLayerName7);

            shaderVariantLogLevel = serializedObject.Find((HDDefaultSettings s) => s.shaderVariantLogLevel);

            lensAttenuation = serializedObject.FindProperty("m_LensAttenuation");
            diffusionProfileSettingsList = serializedObject.Find((HDDefaultSettings s) => s.diffusionProfileSettingsList);
        }

        internal ReorderableList uiBeforeTransparentCustomPostProcesses;
        internal ReorderableList uiBeforeTAACustomPostProcesses;
        internal ReorderableList uiBeforePostProcessCustomPostProcesses;
        internal ReorderableList uiAfterPostProcessCustomPostProcesses;

        void InitializeCustomPostProcessesLists()
        {
            if (uiBeforeTransparentCustomPostProcesses != null)
                return;

            var ppVolumeTypeInjectionPoints = new Dictionary<Type, CustomPostProcessInjectionPoint>();

            var ppVolumeTypes = TypeCache.GetTypesDerivedFrom<CustomPostProcessVolumeComponent>();
            foreach (var ppVolumeType in ppVolumeTypes)
            {
                if (ppVolumeType.IsAbstract)
                    continue;

                var comp = ScriptableObject.CreateInstance(ppVolumeType) as CustomPostProcessVolumeComponent;
                ppVolumeTypeInjectionPoints[ppVolumeType] = comp.injectionPoint;
                CoreUtils.Destroy(comp);
            }
            var defaultSettings = serializedObject.targetObject as HDDefaultSettings;
            InitList(ref uiBeforeTransparentCustomPostProcesses, defaultSettings.beforeTransparentCustomPostProcesses, "After Opaque And Sky", CustomPostProcessInjectionPoint.AfterOpaqueAndSky);
            InitList(ref uiBeforePostProcessCustomPostProcesses, defaultSettings.beforePostProcessCustomPostProcesses, "Before Post Process", CustomPostProcessInjectionPoint.BeforePostProcess);
            InitList(ref uiAfterPostProcessCustomPostProcesses, defaultSettings.afterPostProcessCustomPostProcesses, "After Post Process", CustomPostProcessInjectionPoint.AfterPostProcess);
            InitList(ref uiBeforeTAACustomPostProcesses, defaultSettings.beforeTAACustomPostProcesses, "Before TAA", CustomPostProcessInjectionPoint.BeforeTAA);

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
