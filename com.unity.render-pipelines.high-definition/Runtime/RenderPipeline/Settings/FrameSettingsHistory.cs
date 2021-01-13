using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Type of entity on which frame settings are applied.
    /// </summary>
    public enum FrameSettingsRenderType
    {
        /// <summary>Frame settings are applied to a camera.</summary>
        Camera,
        /// <summary>Frame settings are applied to a baked or custom reflection probe.</summary>
        CustomOrBakedReflection,
        /// <summary>Frame settings are applied to a realtime reflection.</summary>
        RealtimeReflection
    }

    internal interface IFrameSettingsHistoryContainer : IDebugData
    {
        FrameSettingsHistory frameSettingsHistory { get; set; }
        FrameSettingsOverrideMask frameSettingsMask { get; }
        FrameSettings frameSettings { get; }
        bool hasCustomFrameSettings { get; }
        string panelName { get; }
    }

    struct FrameSettingsHistory
    {
        internal static readonly string[] foldoutNames = { "Rendering", "Lighting", "Async Compute", "Light Loop" };
        static readonly string[] columnNames = { "Debug", "Sanitized", "Overridden", "Default" };
        static readonly Dictionary<FrameSettingsField, FrameSettingsFieldAttribute> attributes;
        static Dictionary<int, IOrderedEnumerable<KeyValuePair<FrameSettingsField, FrameSettingsFieldAttribute>>> attributesGroup = new Dictionary<int, IOrderedEnumerable<KeyValuePair<FrameSettingsField, FrameSettingsFieldAttribute>>>();

        // due to strange management of Scene view cameras, all camera of type Scene will share same FrameSettingsHistory
#if UNITY_EDITOR
        //internal static Camera sceneViewCamera;
        class MinimalHistoryContainer : IFrameSettingsHistoryContainer
        {
            FrameSettingsHistory m_FrameSettingsHistory = new FrameSettingsHistory();
            FrameSettingsHistory IFrameSettingsHistoryContainer.frameSettingsHistory
            {
                get => m_FrameSettingsHistory;
                set => m_FrameSettingsHistory = value;
            }

            // never used as hasCustomFrameSettings forced to false
            FrameSettingsOverrideMask IFrameSettingsHistoryContainer.frameSettingsMask
                => throw new NotImplementedException();

            // never used as hasCustomFrameSettings forced to false
            FrameSettings IFrameSettingsHistoryContainer.frameSettings
                => throw new NotImplementedException();

            // forced to false as there is no control on this object
            bool IFrameSettingsHistoryContainer.hasCustomFrameSettings
                => false;

            string IFrameSettingsHistoryContainer.panelName
                => "Scene Camera";

            public MinimalHistoryContainer()
                => m_FrameSettingsHistory.debug = HDRenderPipeline.defaultAsset?.GetDefaultFrameSettings(FrameSettingsRenderType.Camera) ?? FrameSettings.NewDefaultCamera();

            Action IDebugData.GetReset()
                //caution: we actually need to retrieve the
                //m_FrameSettingsHistory as it is a struct so no direct
                // => m_FrameSettingsHistory.TriggerReset
                => () => m_FrameSettingsHistory.TriggerReset();
        }
        internal static IFrameSettingsHistoryContainer sceneViewFrameSettingsContainer = new MinimalHistoryContainer();
#endif
        internal static HashSet<IFrameSettingsHistoryContainer> containers = new HashSet<IFrameSettingsHistoryContainer>();

        public FrameSettingsRenderType defaultType;
        public FrameSettings overridden;
        public FrameSettingsOverrideMask customMask;
        public FrameSettings sanitazed;
        public FrameSettings debug;
        bool hasDebug;

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
                        || (s_PossiblyInUse && (s_PossiblyInUse = containers.Any(history => history.frameSettingsHistory.hasDebug)));
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
        public static void AggregateFrameSettings(ref FrameSettings aggregatedFrameSettings, Camera camera, HDAdditionalCameraData additionalData, HDRenderPipelineAsset hdrpAsset, HDRenderPipelineAsset defaultHdrpAsset)
            => AggregateFrameSettings(
                ref aggregatedFrameSettings,
                camera,
#if UNITY_EDITOR
                camera.cameraType == CameraType.SceneView ? sceneViewFrameSettingsContainer :
#endif
                additionalData,
                ref defaultHdrpAsset.GetDefaultFrameSettings(additionalData?.defaultFrameSettings ?? FrameSettingsRenderType.Camera), //fallback on Camera for SceneCamera and PreviewCamera
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
        public static void AggregateFrameSettings(ref FrameSettings aggregatedFrameSettings, Camera camera, IFrameSettingsHistoryContainer historyContainer, ref FrameSettings defaultFrameSettings, RenderPipelineSettings supportedFeatures)
        {
            FrameSettingsHistory history = historyContainer.frameSettingsHistory;
            aggregatedFrameSettings = defaultFrameSettings;
            bool updatedComponent = false;

            if (historyContainer.hasCustomFrameSettings)
            {
                FrameSettings.Override(ref aggregatedFrameSettings, historyContainer.frameSettings, historyContainer.frameSettingsMask);
                updatedComponent = history.customMask.mask != historyContainer.frameSettingsMask.mask;
                history.customMask = historyContainer.frameSettingsMask;
            }
            history.overridden = aggregatedFrameSettings;
            FrameSettings.Sanitize(ref aggregatedFrameSettings, camera, supportedFeatures);

            history.hasDebug = history.debug != aggregatedFrameSettings;
            updatedComponent |= history.sanitazed != aggregatedFrameSettings;
            bool dirtyDebugData = !history.hasDebug || updatedComponent;

            history.sanitazed = aggregatedFrameSettings;
            if (dirtyDebugData)
            {
                // Reset debug data
                history.debug = history.sanitazed;
            }
            else
            {
                // Keep user modified debug data
                // Ensure user is not trying to activate unsupported settings in DebugMenu
                FrameSettings.Sanitize(ref history.debug, camera, supportedFeatures);
            }

            aggregatedFrameSettings = history.debug;
            historyContainer.frameSettingsHistory = history;
        }

        static DebugUI.HistoryBoolField GenerateHistoryBoolField(HDRenderPipelineAsset defaultHdrpAsset, IFrameSettingsHistoryContainer frameSettingsContainer, FrameSettingsField field, FrameSettingsFieldAttribute attribute)
        {
            string displayIndent = "";
            for (int indent = 0; indent < attribute.indentLevel; ++indent)
                displayIndent += "  ";
            return new DebugUI.HistoryBoolField
            {
                displayName = displayIndent + attribute.displayedName,
                getter = () => frameSettingsContainer.frameSettingsHistory.debug.IsEnabled(field),
                setter = value =>
                {
                    var tmp = frameSettingsContainer.frameSettingsHistory;
                    tmp.debug.SetEnabled(field, value);
                    frameSettingsContainer.frameSettingsHistory = tmp;
                },
                historyGetter = new Func<bool>[]
                {
                    () => frameSettingsContainer.frameSettingsHistory.sanitazed.IsEnabled(field),
                    () => frameSettingsContainer.frameSettingsHistory.overridden.IsEnabled(field),
                    () => defaultHdrpAsset.GetDefaultFrameSettings(frameSettingsContainer.frameSettingsHistory.defaultType).IsEnabled(field)
                }
            };
        }

        static DebugUI.HistoryEnumField GenerateHistoryEnumField(HDRenderPipelineAsset defaultHdrpAsset, IFrameSettingsHistoryContainer frameSettingsContainer, FrameSettingsField field, FrameSettingsFieldAttribute attribute, Type autoEnum)
        {
            string displayIndent = "";
            for (int indent = 0; indent < attribute.indentLevel; ++indent)
                displayIndent += "  ";
            return new DebugUI.HistoryEnumField
            {
                displayName = displayIndent + attribute.displayedName,
                getter = () => frameSettingsContainer.frameSettingsHistory.debug.IsEnabled(field) ? 1 : 0,
                setter = value =>
                {
                    var tmp = frameSettingsContainer.frameSettingsHistory; //indexer with struct will create a copy
                    tmp.debug.SetEnabled(field, value == 1);
                    frameSettingsContainer.frameSettingsHistory = tmp;
                },
                autoEnum = autoEnum,

                // Contrarily to other enum of DebugMenu, we do not need to stock index as
                // it can be computed again with data in the dedicated debug section of history
                getIndex = () => frameSettingsContainer.frameSettingsHistory.debug.IsEnabled(field) ? 1 : 0,
                setIndex = (int a) => { },

                historyIndexGetter = new Func<int>[]
                {
                    () => frameSettingsContainer.frameSettingsHistory.sanitazed.IsEnabled(field) ? 1 : 0,
                    () => frameSettingsContainer.frameSettingsHistory.overridden.IsEnabled(field) ? 1 : 0,
                    () => defaultHdrpAsset.GetDefaultFrameSettings(frameSettingsContainer.frameSettingsHistory.defaultType).IsEnabled(field) ? 1 : 0
                }
            };
        }

        static ObservableList<DebugUI.Widget> GenerateHistoryArea(HDRenderPipelineAsset defaultHdrpAsset, IFrameSettingsHistoryContainer frameSettingsContainer, int groupIndex)
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
                        area.Add(GenerateHistoryBoolField(
                            defaultHdrpAsset,
                            frameSettingsContainer,
                            field.Key,
                            field.Value));
                        break;
                    case FrameSettingsFieldAttribute.DisplayType.BoolAsEnumPopup:
                        area.Add(GenerateHistoryEnumField(
                            defaultHdrpAsset,
                            frameSettingsContainer,
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

        static DebugUI.Widget[] GenerateFrameSettingsPanelContent(HDRenderPipelineAsset defaultHdrpAsset, IFrameSettingsHistoryContainer frameSettingsContainer)
        {
            var panelContent = new DebugUI.Widget[foldoutNames.Length];
            for (int index = 0; index < foldoutNames.Length; ++index)
            {
                panelContent[index] = new DebugUI.Foldout(foldoutNames[index], GenerateHistoryArea(defaultHdrpAsset, frameSettingsContainer, index), columnNames);
            }
            return panelContent;
        }

        static void GenerateFrameSettingsPanel(string menuName, IFrameSettingsHistoryContainer frameSettingsContainer)
        {
            HDRenderPipelineAsset defaultHdrpAsset = HDRenderPipeline.defaultAsset;
            List<DebugUI.Widget> widgets = new List<DebugUI.Widget>();
            widgets.AddRange(GenerateFrameSettingsPanelContent(defaultHdrpAsset, frameSettingsContainer));
            var panel = DebugManager.instance.GetPanel(
                menuName,
                createIfNull: true,
#if UNITY_EDITOR
                frameSettingsContainer == sceneViewFrameSettingsContainer
                ? 1 : // Scene Camera
#endif
                2,    // Other Cameras (from Camera component)
                overrideIfExist: true);
            panel.children.Add(widgets.ToArray());
        }

        static Type RetrieveEnumTypeByField(FrameSettingsField field)
        {
            switch (field)
            {
                case FrameSettingsField.LitShaderMode: return typeof(LitShaderMode);
                default: throw new ArgumentException("Unknown enum type for this field");
            }
        }

        /// <summary>Register FrameSettingsHistory for DebugMenu</summary>
        public static IDebugData RegisterDebug(IFrameSettingsHistoryContainer frameSettingsContainer, bool sceneViewCamera = false)
        {
#if UNITY_EDITOR
            if (sceneViewCamera)
                frameSettingsContainer = sceneViewFrameSettingsContainer;
#endif

            GenerateFrameSettingsPanel(frameSettingsContainer.panelName, frameSettingsContainer);
            containers.Add(frameSettingsContainer);
            return frameSettingsContainer;
        }

        /// <summary>Unregister FrameSettingsHistory for DebugMenu</summary>
        public static void UnRegisterDebug(IFrameSettingsHistoryContainer container)
        {
            DebugManager.instance.RemovePanel(container.panelName);
            containers.Remove(container);
        }

        /// <summary>Check if a camera is registered.</summary>
        public static bool IsRegistered(IFrameSettingsHistoryContainer container, bool sceneViewCamera = false)
        {
            if (sceneViewCamera)
                return true;
            return containers.Contains(container);
        }

        internal void TriggerReset()
        {
            debug = sanitazed;
            hasDebug = false;
        }
    }
}
