using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public enum FrameSettingsRenderType
    {
        Camera,
        CustomOrBakedReflection,
        RealtimeReflection
    }

    public struct FrameSettingsHistory : IDebugData
    {
        static readonly string[] foldoutNames = { "Rendering", "Lighting", "Async Compute", "Light Loop" };
        static readonly string[] columnNames = { "Debug", "Sanitized", "Overridden", "Default" };
        static readonly Dictionary<FrameSettingsField, FrameSettingsFieldAttribute> attributes;
        static Dictionary<int, IOrderedEnumerable<KeyValuePair<FrameSettingsField, FrameSettingsFieldAttribute>>> attributesGroup = new Dictionary<int, IOrderedEnumerable<KeyValuePair<FrameSettingsField, FrameSettingsFieldAttribute>>>();

        // due to strange management of Scene view cameras, all camera of type Scene will share same FrameSettingsHistory
#if UNITY_EDITOR
        internal static Camera sceneViewCamera;
#endif
        internal static Dictionary<Camera, FrameSettingsHistory> frameSettingsHistory = new Dictionary<Camera, FrameSettingsHistory>();

        public FrameSettingsRenderType defaultType;
        public FrameSettings overridden;
        public FrameSettingsOverrideMask customMask;
        public FrameSettings sanitazed;
        public FrameSettings debug;
        Camera camera; //ref for DebugMenu retrieval only
        
        static bool s_PossiblyInUse;
        public static bool enabled 
        {
            get
            {
                // The feature is enabled when either DebugWindow or DebugRuntimeUI
                // are displayed. When none are displayed, the feature remain in use
                // as long as there is one renderer that have debug modification.
                // We use s_PossiblyInUse to perform the check on the FrameSettingsHistory
                // collection the less possible (only when we exited all the windows
                // as long as there is modification).

                if (!s_PossiblyInUse)
                    return s_PossiblyInUse = DebugManager.instance.displayEditorUI || DebugManager.instance.displayRuntimeUI;
                else
                    return DebugManager.instance.displayEditorUI
                        || DebugManager.instance.displayRuntimeUI
                        // a && (a = something) different than a &= something as if a is false something is not evaluated in second version
                        || (s_PossiblyInUse && (s_PossiblyInUse = frameSettingsHistory.Values.Any(history => history.debug == history.sanitazed)));
            }
        }

        /// <summary>Initialize data for FrameSettings panel of DebugMenu construction.</summary>
        static FrameSettingsHistory()
        {
            attributes = new Dictionary<FrameSettingsField, FrameSettingsFieldAttribute>();
            attributesGroup = new Dictionary<int, IOrderedEnumerable<KeyValuePair<FrameSettingsField, FrameSettingsFieldAttribute>>>();
            Type type = typeof(FrameSettingsField);
            foreach (FrameSettingsField value in Enum.GetValues(type))
            {
                attributes[value] = type.GetField(Enum.GetName(type, value)).GetCustomAttribute<FrameSettingsFieldAttribute>();
            }
        }
        /// <summary>Same than FrameSettings.AggregateFrameSettings but keep history of agregation in a collection for DebugMenu.
        /// Aggregation is default with override of the renderer then sanitazed depending on supported features of hdrpasset. Then the DebugMenu override occurs.</summary>
        /// <param name="aggregatedFrameSettings">The aggregated FrameSettings result.</param>
        /// <param name="camera">The camera rendering.</param>
        /// <param name="additionalData">Additional data of the camera rendering.</param>
        /// <param name="hdrpAsset">HDRenderPipelineAsset contening default FrameSettings.</param>
        public static void AggregateFrameSettings(ref FrameSettings aggregatedFrameSettings, Camera camera, HDAdditionalCameraData additionalData, HDRenderPipelineAsset hdrpAsset)
            => AggregateFrameSettings(
                ref aggregatedFrameSettings,
                camera,
                additionalData,
                ref hdrpAsset.GetDefaultFrameSettings(additionalData?.defaultFrameSettings ?? FrameSettingsRenderType.Camera), //fallback on Camera for SceneCamera and PreviewCamera
                hdrpAsset.currentPlatformRenderPipelineSettings
                );

        // Note: this version is the one tested as there is issue getting HDRenderPipelineAsset in batchmode in unit test framework currently.
        /// <summary>Same than FrameSettings.AggregateFrameSettings but keep history of agregation in a collection for DebugMenu.
        /// Aggregation is default with override of the renderer then sanitazed depending on supported features of hdrpasset. Then the DebugMenu override occurs.</summary>
        /// <param name="aggregatedFrameSettings">The aggregated FrameSettings result.</param>
        /// <param name="camera">The camera rendering.</param>
        /// <param name="additionalData">Additional data of the camera rendering.</param>
        /// <param name="defaultFrameSettings">Base framesettings to copy prior any override.</param>
        /// <param name="supportedFeatures">Currently supported feature for the sanitazation pass.</param>
        public static void AggregateFrameSettings(ref FrameSettings aggregatedFrameSettings, Camera camera, HDAdditionalCameraData additionalData, ref FrameSettings defaultFrameSettings, RenderPipelineSettings supportedFeatures)
        {
            FrameSettingsHistory history = new FrameSettingsHistory
            {
                camera = camera,
                defaultType = additionalData ? additionalData.defaultFrameSettings : FrameSettingsRenderType.Camera
            };
            aggregatedFrameSettings = defaultFrameSettings;
            if (additionalData && additionalData.customRenderingSettings)
            {
                FrameSettings.Override(ref aggregatedFrameSettings, additionalData.renderingPathCustomFrameSettings, additionalData.renderingPathCustomFrameSettingsOverrideMask);
                history.customMask = additionalData.renderingPathCustomFrameSettingsOverrideMask;
            }
            history.overridden = aggregatedFrameSettings;
            FrameSettings.Sanitize(ref aggregatedFrameSettings, camera, supportedFeatures);

            bool noHistory = !frameSettingsHistory.ContainsKey(camera);                   
            bool updatedComponent = !noHistory && frameSettingsHistory[camera].sanitazed != aggregatedFrameSettings;
            bool dirty = noHistory || updatedComponent;

            history.sanitazed = aggregatedFrameSettings;
            if (dirty)
                history.debug = history.sanitazed;
            else
            {
                history.debug = frameSettingsHistory[camera].debug;

                // Ensure user is not trying to activate unsupported settings in DebugMenu
                FrameSettings.Sanitize(ref history.debug, camera, supportedFeatures);
            }

            aggregatedFrameSettings = history.debug;
            frameSettingsHistory[camera] = history;
        }

        static DebugUI.HistoryBoolField GenerateHistoryBoolField(HDRenderPipelineAsset hdrpAsset, ref FrameSettingsHistory frameSettings, FrameSettingsField field, FrameSettingsFieldAttribute attribute)
        {
            Camera camera = frameSettings.camera;
            var renderType = frameSettings.defaultType;
            string displayIndent = "";
            for (int indent = 0; indent < attribute.indentLevel; ++indent)
                displayIndent += "  ";
            return new DebugUI.HistoryBoolField
            {
                displayName = displayIndent + attribute.displayedName,
                getter = () => frameSettingsHistory[camera].debug.IsEnabled(field),
                setter = value =>
                {
                    var tmp = frameSettingsHistory[camera]; //indexer with struct will create a copy
                    tmp.debug.SetEnabled(field, value);
                    frameSettingsHistory[camera] = tmp;
                },
                historyGetter = new Func<bool>[]
                {
                    () => frameSettingsHistory[camera].sanitazed.IsEnabled(field),
                    () => frameSettingsHistory[camera].overridden.IsEnabled(field),
                    () => hdrpAsset.GetDefaultFrameSettings(renderType).IsEnabled(field)
                }
            };
        }

        static DebugUI.HistoryEnumField GenerateHistoryEnumField(HDRenderPipelineAsset hdrpAsset, ref FrameSettingsHistory frameSettings, FrameSettingsField field, FrameSettingsFieldAttribute attribute, Type autoEnum)
        {
            Camera camera = frameSettings.camera;
            var renderType = frameSettings.defaultType;
            string displayIndent = "";
            for (int indent = 0; indent < attribute.indentLevel; ++indent)
                displayIndent += "  ";
            return new DebugUI.HistoryEnumField
            {
                displayName = displayIndent + attribute.displayedName,
                getter = () => frameSettingsHistory[camera].debug.IsEnabled(field) ? 1 : 0,
                setter = value =>
                {
                    var tmp = frameSettingsHistory[camera]; //indexer with struct will create a copy
                    tmp.debug.SetEnabled(field, value == 1);
                    frameSettingsHistory[camera] = tmp;
                },
                autoEnum = autoEnum,

                // Contrarily to other enum of DebugMenu, we do not need to stock index as 
                // it can be computed again with data in the dedicated debug section of history
                getIndex = () => frameSettingsHistory[camera].debug.IsEnabled(field) ? 1 : 0, 
                setIndex = (int a) => { },

                historyIndexGetter = new Func<int>[]
                {
                    () => frameSettingsHistory[camera].sanitazed.IsEnabled(field) ? 1 : 0,
                    () => frameSettingsHistory[camera].overridden.IsEnabled(field) ? 1 : 0,
                    () => hdrpAsset.GetDefaultFrameSettings(renderType).IsEnabled(field) ? 1 : 0
                }
            };
        }

        static ObservableList<DebugUI.Widget> GenerateHistoryArea(HDRenderPipelineAsset hdrpAsset, ref FrameSettingsHistory frameSettings, int groupIndex)
        {
            if (!attributesGroup.ContainsKey(groupIndex) || attributesGroup[groupIndex] == null)
                attributesGroup[groupIndex] = attributes?.Where(pair => pair.Value?.group == groupIndex)?.OrderBy(pair => pair.Value.orderInGroup);
            if (!attributesGroup.ContainsKey(groupIndex))
                throw new ArgumentException("Unknown groupIndex");
            
            var area = new ObservableList<DebugUI.Widget>();
            foreach (var field in attributesGroup[groupIndex])
            {
                switch (field.Value.type)
                {
                    case FrameSettingsFieldAttribute.DisplayType.BoolAsCheckbox:
                        area.Add(GenerateHistoryBoolField(hdrpAsset, ref frameSettings, field.Key, field.Value));
                        break;
                    case FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup:
                        area.Add(GenerateHistoryEnumField(
                            hdrpAsset,
                            ref frameSettings,
                            field.Key,
                            field.Value,
                            RetrieveEnumTypeByField(field.Key)
                            ));
                        break;
                    case FrameSettingsFieldAttribute.DisplayType.Others: // for now, skip other display settings. Add them if needed
                        break;
                }
            }
            return area;
        }

        static DebugUI.Widget[] GenerateFrameSettingsPanelContent(HDRenderPipelineAsset hdrpAsset, ref FrameSettingsHistory frameSettings)
        {
            var panelContent = new DebugUI.Widget[foldoutNames.Length];
            for (int index = 0; index < foldoutNames.Length; ++index)
            {
                panelContent[index] = new DebugUI.Foldout(foldoutNames[index], GenerateHistoryArea(hdrpAsset, ref frameSettings, index), columnNames);
            }
            return panelContent;
        }

        static void GenerateFrameSettingsPanel(string menuName, FrameSettingsHistory frameSettings)
        {
            HDRenderPipelineAsset hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            var camera = frameSettings.camera;
            List<DebugUI.Widget> widgets = new List<DebugUI.Widget>();
            widgets.AddRange(GenerateFrameSettingsPanelContent(hdrpAsset, ref frameSettings));
            var panel = DebugManager.instance.GetPanel(menuName, true, 1);
            panel.children.Add(widgets.ToArray());
        }

        static Type RetrieveEnumTypeByField(FrameSettingsField field)
        {
            switch (field)
            {
                case FrameSettingsField.LitShaderMode: return typeof(LitShaderMode);
                default: throw new ArgumentException("Unknow enum type for this field");
            }
        }

        /// <summary>Register FrameSettingsHistory for DebugMenu</summary>
        public static IDebugData RegisterDebug(Camera camera, HDAdditionalCameraData additionalCameraData)
        {
            HDRenderPipelineAsset hdrpAsset = GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset;
            Assertions.Assert.IsNotNull(hdrpAsset);

            // complete frame settings history is required for displaying debug menu.
            // AggregateFrameSettings will finish the registration if it is not yet registered
            FrameSettings registering = new FrameSettings();
            AggregateFrameSettings(ref registering, camera, additionalCameraData, hdrpAsset);
            GenerateFrameSettingsPanel(camera.name, frameSettingsHistory[camera]);
#if UNITY_EDITOR
            if (sceneViewCamera == null && camera.cameraType == CameraType.SceneView)
                sceneViewCamera = camera;
#endif
            return frameSettingsHistory[camera];
        }

        /// <summary>Unregister FrameSettingsHistory for DebugMenu</summary>
        public static void UnRegisterDebug(Camera camera)
        {
            DebugManager.instance.RemovePanel(camera.name);
            frameSettingsHistory.Remove(camera);
        }

#if UNITY_EDITOR
        /// <summary>Check if the common frameSettings for SceneViewCamera is already created.</summary>
        public static bool isRegisteredSceneViewCamera(Camera camera) =>
            camera.cameraType == CameraType.SceneView && sceneViewCamera != null && frameSettingsHistory.ContainsKey(sceneViewCamera);
#endif

        /// <summary>Check if a camera is registered.</summary>
        public static bool IsRegistered(Camera camera)
        {
            return frameSettingsHistory.ContainsKey(camera);
        }

        /// <summary>Return a copy of the persistently stored data.</summary>
        public static IDebugData GetPersistantDebugDataCopy(Camera camera) => frameSettingsHistory[camera];

        void TriggerReset()
        {
            var tmp = frameSettingsHistory[camera];
            tmp.debug = tmp.sanitazed;   //erase immediately debug data as camera could be not rendered if not enabled.
            frameSettingsHistory[camera] = this; //copy changed history to collection
        }
        Action IDebugData.GetReset() => TriggerReset;
    }
}
