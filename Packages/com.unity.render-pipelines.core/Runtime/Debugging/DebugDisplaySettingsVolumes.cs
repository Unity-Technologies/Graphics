using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Debug Dispaly Settings Volume
    /// </summary>
    public class DebugDisplaySettingsVolume : IDebugDisplaySettingsData
    {
        /// <summary>Current volume debug settings.</summary>
        public IVolumeDebugSettings2 volumeDebugSettings { get; }

        /// <summary>
        /// Constructor with the settings
        /// </summary>
        /// <param name="volumeDebugSettings"></param>
        public DebugDisplaySettingsVolume(IVolumeDebugSettings2 volumeDebugSettings)
        {
            this.volumeDebugSettings = volumeDebugSettings;
        }

        internal int volumeComponentEnumIndex;

        static class Styles
        {
            public static readonly GUIContent none = new GUIContent("None");
            public static readonly GUIContent editorCamera = new GUIContent("Editor Camera");
        }

        static class Strings
        {
            public static readonly string none = "None";
            public static readonly string camera = "Camera";
            public static readonly string parameter = "Parameter";
            public static readonly string component = "Component";
            public static readonly string debugViewNotSupported = "N/A";
            public static readonly string parameterNotOverrided = "-";
            public static readonly string volumeInfo = "Volume Info";
            public static readonly string gameObject = "GameObject";
            public static readonly string resultValue = "Result";
            public static readonly string resultValueTooltip = "The interpolated result value of the parameter. This value is used to render the camera.";
            public static readonly string globalDefaultValue = "Graphics Settings";
            public static readonly string globalDefaultValueTooltip = "Default value for this parameter, defined by the Default Volume Profile in Global Settings.";
            public static readonly string qualityLevelValue = "Quality Settings";
            public static readonly string qualityLevelValueTooltip = "Override value for this parameter, defined by the Volume Profile in the current SRP Asset.";
            public static readonly string global = "Global";
            public static readonly string local = "Local";
            public static readonly string volumeProfile = "Volume Profile";
        }

        internal static class WidgetFactory
        {
            public static DebugUI.EnumField CreateComponentSelector(SettingsPanel panel, Action<DebugUI.Field<int>, int> refresh)
            {
                int componentIndex = 0;
                var componentNames = new List<GUIContent>() { Styles.none };
                var componentValues = new List<int>() { componentIndex++ };

                foreach (var type in panel.data.volumeDebugSettings.volumeComponentsPathAndType)
                {
                    componentNames.Add(new GUIContent() { text = type.Item1 });
                    componentValues.Add(componentIndex++);
                }

                return new DebugUI.EnumField
                {
                    displayName = Strings.component,
                    getter = () => panel.data.volumeDebugSettings.selectedComponent,
                    setter = value => panel.data.volumeDebugSettings.selectedComponent = value,
                    enumNames = componentNames.ToArray(),
                    enumValues = componentValues.ToArray(),
                    getIndex = () => panel.data.volumeComponentEnumIndex,
                    setIndex = value => { panel.data.volumeComponentEnumIndex = value; },
                    onValueChanged = refresh
                };
            }

            public static DebugUI.ObjectPopupField CreateCameraSelector(SettingsPanel panel, Action<DebugUI.Field<Object>, Object> refresh)
            {
                return new DebugUI.ObjectPopupField
                {
                    displayName = Strings.camera,
                    getter = () => panel.data.volumeDebugSettings.selectedCamera,
                    setter = value =>
                    {
                        var c = panel.data.volumeDebugSettings.cameras.ToArray();
                        panel.data.volumeDebugSettings.selectedCameraIndex = Array.IndexOf(c, value as Camera);
                    },
                    getObjects = () => panel.data.volumeDebugSettings.cameras,
                    onValueChanged = refresh
                };
            }

            static DebugUI.Widget CreateVolumeParameterWidget(string name, bool isResultParameter, VolumeParameter param, Func<bool> isHiddenCallback = null)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (param != null)
                {
                    var parameterType = param.GetType();
                    if (parameterType == typeof(ColorParameter))
                    {
                        var p = (ColorParameter)param;
                        return new DebugUI.ColorField()
                        {
                            displayName = name,
                            hdr = p.hdr,
                            showAlpha = p.showAlpha,
                            getter = () => p.value,
                            setter = value => p.value = value,
                            isHiddenCallback = isHiddenCallback
                        };
                    }

                    var typeInfo = parameterType.GetTypeInfo();
                    var genericArguments = typeInfo.BaseType.GenericTypeArguments;
                    if (genericArguments.Length > 0 && genericArguments[0].IsArray)
                    {
                        return new DebugUI.ObjectListField()
                        {
                            displayName = name,
                            getter = () => (Object[])parameterType.GetProperty("value").GetValue(param, null),
                            type = parameterType
                        };
                    }

                    return new DebugUI.Value()
                    {
                        displayName = name,
                        getter = () =>
                        {
                            var property = param.GetType().GetProperty("value");
                            if (property == null)
                                return "-";

                            if (isResultParameter || param.overrideState)
                            {
                                var value = property.GetValue(param);
                                var propertyType = property.PropertyType;
                                if (value == null || value.Equals(null))
                                    return Strings.none + $" ({propertyType.Name})";

                                var toString = propertyType.GetMethod("ToString", Type.EmptyTypes);
                                if ((toString == null) || (toString.DeclaringType == typeof(object)) || (toString.DeclaringType == typeof(UnityEngine.Object)))
                                {
                                    // Check if the parameter has a name
                                    var nameProp = property.PropertyType.GetProperty("name");
                                    if (nameProp == null)
                                        return Strings.debugViewNotSupported;

                                    var valueString = nameProp.GetValue(value);
                                    return valueString ?? Strings.none;
                                }

                                return value.ToString();
                            }

                            return Strings.parameterNotOverrided;
                        },
                        isHiddenCallback = isHiddenCallback
                    };
                }
    #endif
                return new DebugUI.Value();
            }

            static DebugUI.Value s_EmptyDebugUIValue = new DebugUI.Value { getter = () => string.Empty };

            struct VolumeParameterChain
            {
                public DebugUI.Widget.NameAndTooltip nameAndTooltip;
                public VolumeProfile volumeProfile;
                public VolumeComponent volumeComponent;
                public Volume volume;
            }

            static VolumeComponent GetSelectedVolumeComponent(VolumeProfile profile, Type selectedType)
            {
                if (profile != null)
                {
                    foreach (var component in profile.components)
                        if (component.GetType() == selectedType)
                            return component;
                }
                return null;
            }

            static List<VolumeParameterChain> GetResolutionChain(DebugDisplaySettingsVolume data)
            {
                List<VolumeParameterChain> chain = new List<VolumeParameterChain>();

                Type selectedType = data.volumeDebugSettings.selectedComponentType;
                if (selectedType == null)
                    return chain;

                var volumeManager = VolumeManager.instance;
                var stack = data.volumeDebugSettings.selectedCameraVolumeStack ?? volumeManager.stack;
                var stackComponent = stack.GetComponent(selectedType);
                if (stackComponent == null)
                    return chain;

                var result = new VolumeParameterChain()
                {
                    nameAndTooltip = new DebugUI.Widget.NameAndTooltip()
                    {
                        name = Strings.resultValue,
                        tooltip = Strings.resultValueTooltip,
                    },
                    volumeComponent = stackComponent,
                };

                chain.Add(result);

                // Add volume components that override default values
                var volumes = data.volumeDebugSettings.GetVolumes();
                foreach (var volume in volumes)
                {
                    var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
                    var overrideComponent = GetSelectedVolumeComponent(profile, selectedType);
                    if (overrideComponent != null)
                    {
                        var overrideVolume = new VolumeParameterChain()
                        {
                            nameAndTooltip = new DebugUI.Widget.NameAndTooltip()
                            {
                                name = profile.name,
                                tooltip = profile.name,
                            },
                            volumeProfile = profile,
                            volumeComponent = overrideComponent,
                            volume = volume
                        };
                        chain.Add(overrideVolume);
                    }
                }

                return chain;
            }

            public static DebugUI.Table CreateVolumeTable(DebugDisplaySettingsVolume data)
            {
                var table = new DebugUI.Table()
                {
                    displayName = Strings.parameter,
                    isReadOnly = true
                };

                var resolutionChain = GetResolutionChain(data);
                if (resolutionChain.Count == 0)
                    return table;

                GenerateTableRows(table, resolutionChain);
                GenerateTableColumns(table, data, resolutionChain);

                float timer = 0.0f, refreshRate = 0.2f;
                var volumes = data.volumeDebugSettings.GetVolumes();
                table.isHiddenCallback = () =>
                {
                    timer += Time.deltaTime;
                    if (timer >= refreshRate)
                    {
                        if (data.volumeDebugSettings.selectedCamera != null)
                        {
                            SetTableColumnVisibility(data, table);

                            var newVolumes = data.volumeDebugSettings.GetVolumes();
                            if (!volumes.SequenceEqual(newVolumes))
                            {
                                volumes = newVolumes;
                                DebugManager.instance.ReDrawOnScreenDebug();
                            }
                        }

                        timer = 0.0f;
                    }
                    return false;
                };

                return table;
            }

            private static void SetTableColumnVisibility(DebugDisplaySettingsVolume data, DebugUI.Table table)
            {
                var newResolutionChain = GetResolutionChain(data);
                for (int i = 1; i < newResolutionChain.Count; i++) // We always skip the interpolated stack that is in index 0
                {
                    bool visible = true;
                    if (newResolutionChain[i].volume != null)
                    {
                        visible = data.volumeDebugSettings.VolumeHasInfluence(newResolutionChain[i].volume);
                    }
                    else
                    {
                        visible = newResolutionChain[i].volumeComponent.active;

                        if (visible)
                        {
                            bool atLeastOneParameterIsOverriden = false;
                            foreach (var parameter in newResolutionChain[i].volumeComponent.parameterList)
                            {
                                if (parameter.overrideState == true)
                                {
                                    atLeastOneParameterIsOverriden = true;
                                    break;
                                }
                            }

                            visible &= atLeastOneParameterIsOverriden;
                        }
                    }

                    table.SetColumnVisibility(i, visible);
                }
            }

            private static void GenerateTableColumns(DebugUI.Table table, DebugDisplaySettingsVolume data, List<VolumeParameterChain> resolutionChain)
            {
                for (int i = 0; i < resolutionChain.Count; ++i)
                {
                    var chain = resolutionChain[i];
                    int iRowIndex = -1;

                    if (chain.volume != null)
                    {
                        ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(new DebugUI.Value()
                        {
                            nameAndTooltip = chain.nameAndTooltip,
                            getter = () =>
                            {
                                var scope = chain.volume.isGlobal ? Strings.global : Strings.local;
                                var weight = data.volumeDebugSettings.GetVolumeWeight(chain.volume);
                                return scope + " (" + (weight * 100f) + "%)";
                            },
                            refreshRate = 0.2f
                        });
                        ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(new DebugUI.ObjectField() { displayName = string.Empty, getter = () => chain.volume });
                    }
                    else
                    {
                        ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(new DebugUI.Value()
                        {
                            nameAndTooltip = chain.nameAndTooltip,
                            getter = () => string.Empty
                        });
                        ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(s_EmptyDebugUIValue);
                    }

                    ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(chain.volumeProfile != null ? new DebugUI.ObjectField() { displayName = string.Empty, getter = () => chain.volumeProfile } :
                        s_EmptyDebugUIValue);

                    ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(s_EmptyDebugUIValue);

                    bool isResultParameter = i == 0;
                    for (int j = 0; j < chain.volumeComponent.parameterList.Count; ++j)
                    {
                        var parameter = chain.volumeComponent.parameterList[j];
                        ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(CreateVolumeParameterWidget(chain.nameAndTooltip.name, isResultParameter, parameter));
                    }
                }
            }

            private static void GenerateTableRows(DebugUI.Table table, List<VolumeParameterChain> resolutionChain)
            {
                // First row for volume info
                var volumeInfoRow = new DebugUI.Table.Row()
                {
                    displayName = Strings.volumeInfo,
                    opened = true, // Open by default for the in-game view
                };

                table.children.Add(volumeInfoRow);

                // Second row, links to volume gameobjects
                var gameObjectRow = new DebugUI.Table.Row()
                {
                    displayName = Strings.gameObject,
                };

                table.children.Add(gameObjectRow);

                // Third row, links to volume profile assets
                var volumeProfileRow = new DebugUI.Table.Row()
                {
                    displayName = Strings.volumeProfile,
                };
                table.children.Add(volumeProfileRow);

                var separatorRow = new DebugUI.Table.Row()
                {
                    displayName =  string.Empty ,
                };

                table.children.Add(separatorRow);

                var results = resolutionChain[0].volumeComponent;
                for (int i = 0; i < results.parameterList.Count; ++i)
                {
                    var parameter = results.parameterList[i];

#if UNITY_EDITOR
                    string displayName = UnityEditor.ObjectNames.NicifyVariableName(parameter.debugId); // In the editor, make the name more readable
#elif DEVELOPMENT_BUILD
                    string displayName = parameter.debugId; // In the development player, just the debug id
#else
                    string displayName = i.ToString(); // Everywhere else, just a dummy id ( TODO: The Volume panel code should be stripped completely in nom-development builds )
#endif

                    table.children.Add(new DebugUI.Table.Row()
                    {
                        displayName = displayName
                    });
                }
            }
        }

        [DisplayInfo(name = "Volume", order = int.MaxValue)]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsVolume>
        {
            public SettingsPanel(DebugDisplaySettingsVolume data)
                : base(data)
            {
                AddWidget(WidgetFactory.CreateComponentSelector(this, (_, __) => Refresh()));
                AddWidget(WidgetFactory.CreateCameraSelector(this, (_, __) => Refresh()));
            }

            DebugUI.Table m_VolumeTable = null;
            void Refresh()
            {
                var panel = DebugManager.instance.GetPanel(PanelName);
                if (panel == null)
                    return;

                bool needsRefresh = false;
                if (m_VolumeTable != null)
                {
                    needsRefresh = true;
                    panel.children.Remove(m_VolumeTable);
                }

                if (m_Data.volumeDebugSettings.selectedComponent > 0 && m_Data.volumeDebugSettings.selectedCamera != null)
                {
                    needsRefresh = true;
                    m_VolumeTable = WidgetFactory.CreateVolumeTable(m_Data);
                    AddWidget(m_VolumeTable);
                    panel.children.Add(m_VolumeTable);
                }

                if (needsRefresh)
                    DebugManager.instance.ReDrawOnScreenDebug();
            }
        }

        #region IDebugDisplaySettingsData
        /// <summary>
        /// Checks whether ANY of the debug settings are currently active.
        /// </summary>
        public bool AreAnySettingsActive => false; // Volume Debug Panel doesn't need to modify the renderer data, therefore this property returns false
        /// <summary>
        /// Checks whether the current state of these settings allows post-processing.
        /// </summary>
        public bool IsPostProcessingAllowed => true;
        /// <summary>
        /// Checks whether lighting is active for these settings.
        /// </summary>
        public bool IsLightingActive => true;

        /// <summary>
        /// Attempts to get the color used to clear the screen for this debug setting.
        /// </summary>
        /// <param name="color">A reference to the screen clear color to use.</param>
        /// <returns>"true" if we updated the color, "false" if we didn't change anything.</returns>
        public bool TryGetScreenClearColor(ref Color color)
        {
            return false;
        }

        /// <summary>
        /// Creates the panel
        /// </summary>
        /// <returns>The panel</returns>
        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
