using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Debug Display Settings Volume
    /// </summary>
    public class DebugDisplaySettingsVolume : IDebugDisplaySettingsData
    {
        /// <summary>Current volume debug settings.</summary>
        public IVolumeDebugSettings volumeDebugSettings { get; }

        /// <summary>
        /// Constructor with the settings
        /// </summary>
        /// <param name="volumeDebugSettings">The volume debug settings object used for configuration.</param>
        public DebugDisplaySettingsVolume(IVolumeDebugSettings volumeDebugSettings)
        {
            this.volumeDebugSettings = volumeDebugSettings;
        }

        internal int volumeComponentEnumIndex;

        internal Dictionary<string, VolumeComponent> debugState = new Dictionary<string, VolumeComponent>();

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
            public static readonly string debugViewNotSupported = "Debug view not supported";
            public static readonly string volumeInfo = "Volume Info";
            public static readonly string resultValue = "Result";
            public static readonly string resultValueTooltip = "The interpolated result value of the parameter. This value is used to render the camera.";
            public static readonly string globalDefaultValue = "Default";
            public static readonly string globalDefaultValueTooltip = "Default value for this parameter, defined by the Default Volume Profile in Global Settings.";
            public static readonly string qualityLevelValue = "SRP Asset";
            public static readonly string qualityLevelValueTooltip = "Override value for this parameter, defined by the Volume Profile in the current SRP Asset.";
            public static readonly string global = "Global";
            public static readonly string local = "Local";
        }

        const string k_PanelTitle = "Volume";

#if UNITY_EDITOR
        internal static void OpenInRenderingDebugger()
        {
            EditorApplication.ExecuteMenuItem("Window/Analysis/Rendering Debugger");
            var idx = DebugManager.instance.FindPanelIndex(k_PanelTitle);
            if (idx != -1)
                DebugManager.instance.RequestEditorWindowPanelIndex(idx);
        }
#endif

        internal static class WidgetFactory
        {
            public static DebugUI.EnumField CreateComponentSelector(SettingsPanel panel, Action<DebugUI.Field<int>, int> refresh)
            {
                int componentIndex = 0;
                var componentNames = new List<GUIContent>() { Styles.none };
                var componentValues = new List<int>() { componentIndex++ };

                var volumesAndTypes = VolumeManager.instance.GetVolumeComponentsForDisplay(GraphicsSettings.currentRenderPipelineAssetType);
                foreach (var type in volumesAndTypes)
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

            static DebugUI.Widget CreateVolumeParameterWidget(string name, VolumeParameter param, Func<bool> isHiddenCallback = null)
            {
                if (param == null)
                    return new DebugUI.Value() { displayName = name, getter = () => "-" };

                var parameterType = param.GetType();

                // Special overrides
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
                else if (parameterType == typeof(BoolParameter))
                {
                    var p = (BoolParameter)param;
                    return new DebugUI.BoolField()
                    {
                        displayName = name,
                        getter = () => p.value,
                        setter = value => p.value = value,
                        isHiddenCallback = isHiddenCallback
                    };
                }
                else
                {
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
                }

                // For parameters that do not override `ToString`
                var property = param.GetType().GetProperty("value");
                var toString = property.PropertyType.GetMethod("ToString", Type.EmptyTypes);
                if ((toString == null) || (toString.DeclaringType == typeof(object)) || (toString.DeclaringType == typeof(UnityEngine.Object)))
                {
                    // Check if the parameter has a name
                    var nameProp = property.PropertyType.GetProperty("name");
                    if (nameProp == null)
                        return new DebugUI.Value() { displayName = name, getter = () => Strings.debugViewNotSupported };

                    // Return the parameter name
                    return new DebugUI.Value()
                    {
                        displayName = name,
                        getter = () =>
                        {
                            var value = property.GetValue(param);
                            if (value == null || value.Equals(null))
                                return Strings.none;
                            var valueString = nameProp.GetValue(value);
                            return valueString ?? Strings.none;
                        },
                        isHiddenCallback = isHiddenCallback
                    };
                }

                // Call the ToString method
                return new DebugUI.Value()
                {
                    displayName = name,
                    getter = () =>
                    {
                        var value = property.GetValue(param);
                        return value == null ? Strings.none : value.ToString();
                    },
                    isHiddenCallback = isHiddenCallback
                };
            }

            static DebugUI.Value s_EmptyDebugUIValue = new DebugUI.Value { getter = () => string.Empty };

            public static DebugUI.Table CreateVolumeTable(DebugDisplaySettingsVolume data)
            {
                var table = new DebugUI.Table()
                {
                    displayName = Strings.parameter,
                    isReadOnly = true,
                    isHiddenCallback = () => data.volumeDebugSettings.selectedComponent == 0
                };

                Type selectedType = data.volumeDebugSettings.selectedComponentType;
                if (selectedType == null)
                    return table;

                var volumeManager = VolumeManager.instance;
                var stack = data.volumeDebugSettings.selectedCameraVolumeStack ?? volumeManager.stack;
                var stackComponent = stack.GetComponent(selectedType);
                if (stackComponent == null)
                    return table;

                var volumes = data.volumeDebugSettings.GetVolumes();

                // First row for volume info
                var row1 = new DebugUI.Table.Row()
                {
                    displayName = Strings.volumeInfo,
                    opened = true, // Open by default for the in-game view
                    children =
                    {
                        new DebugUI.Value()
                        {
                             displayName = Strings.resultValue,
                             tooltip = Strings.resultValueTooltip,
                             getter = () => string.Empty
                        }
                    }
                };

                // Second row, links to volume gameobjects
                var row2 = new DebugUI.Table.Row()
                {
                    displayName = "GameObject",
                    children = { s_EmptyDebugUIValue }
                };

                // Third row, links to volume profile assets
                var row3 = new DebugUI.Table.Row()
                {
                    displayName = "Volume Profile",
                    children = { s_EmptyDebugUIValue }
                };

                // Fourth row, empty (to separate from actual data)
                var row4 = new DebugUI.Table.Row()
                {
                    displayName =  string.Empty ,
                    children = { s_EmptyDebugUIValue }
                };

                foreach (var volume in volumes)
                {
                    var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
                    row1.children.Add(new DebugUI.Value()
                    {
                        displayName = profile.name,
                        tooltip = $"Override value for this parameter, defined by {profile.name}",
                        getter = () =>
                        {
                            var scope = volume.isGlobal ? Strings.global : Strings.local;
                            var weight = data.volumeDebugSettings.GetVolumeWeight(volume);
                            return scope + " (" + (weight * 100f) + "%)";
                        }
                    });
                    row2.children.Add(new DebugUI.ObjectField() { displayName = string.Empty, getter = () => volume });
                    row3.children.Add(new DebugUI.ObjectField() { displayName = string.Empty, getter = () => profile });
                    row4.children.Add(s_EmptyDebugUIValue);
                }

                // Default value profiles
                var globalDefaultComponent = GetSelectedVolumeComponent(volumeManager.globalDefaultProfile);
                var qualityDefaultComponent = GetSelectedVolumeComponent(volumeManager.qualityDefaultProfile);
                List<(VolumeProfile, VolumeComponent)> customDefaultComponents = new();
                if (volumeManager.customDefaultProfiles != null)
                {
                    foreach (var customProfile in volumeManager.customDefaultProfiles)
                    {
                        var customDefaultComponent = GetSelectedVolumeComponent(customProfile);
                        if (customDefaultComponent != null)
                            customDefaultComponents.Add((customProfile, customDefaultComponent));
                    }
                }

                foreach (var (customProfile, _) in customDefaultComponents)
                {
                    row1.children.Add(new DebugUI.Value() { displayName = customProfile.name, getter = () => string.Empty });
                    row2.children.Add(s_EmptyDebugUIValue);
                    row3.children.Add(new DebugUI.ObjectField() { displayName = string.Empty, getter = () => customProfile });
                    row4.children.Add(s_EmptyDebugUIValue);
                }

                row1.children.Add(new DebugUI.Value() { displayName = Strings.qualityLevelValue, tooltip = Strings.qualityLevelValueTooltip, getter = () => string.Empty });
                row2.children.Add(s_EmptyDebugUIValue);
                row3.children.Add(new DebugUI.ObjectField() { displayName = string.Empty, getter = () => volumeManager.qualityDefaultProfile });
                row4.children.Add(s_EmptyDebugUIValue);

                row1.children.Add(new DebugUI.Value() { displayName = Strings.globalDefaultValue, tooltip = Strings.globalDefaultValueTooltip, getter = () => string.Empty });
                row2.children.Add(s_EmptyDebugUIValue);
                row3.children.Add(new DebugUI.ObjectField() { displayName = string.Empty, getter = () => volumeManager.globalDefaultProfile });
                row4.children.Add(s_EmptyDebugUIValue);

                table.children.Add(row1);
                table.children.Add(row2);
                table.children.Add(row3);
                table.children.Add(row4);

                VolumeComponent GetSelectedVolumeComponent(VolumeProfile profile)
                {
                    if (profile != null)
                    {
                        foreach (var component in profile.components)
                            if (component.GetType() == selectedType)
                                return component;
                    }
                    return null;
                }

                // Build rows - recursively handles nested parameters
                var rows = new List<DebugUI.Table.Row>();
                int AddParameterRows(Type type, string baseName = null, int skip = 0)
                {
                    void AddRow(FieldInfo f, string prefix, int skip)
                    {
                        var fieldName = prefix + f.Name;
                        var attr = (DisplayInfoAttribute[])f.GetCustomAttributes(typeof(DisplayInfoAttribute), true);
                        if (attr.Length != 0)
                            fieldName = prefix + attr[0].name;
#if UNITY_EDITOR
                        // Would be nice to have the equivalent for the runtime debug.
                        else
                            fieldName = UnityEditor.ObjectNames.NicifyVariableName(fieldName);
#endif

                        int currentParam = rows.Count + skip;
                        DebugUI.Table.Row row = new DebugUI.Table.Row()
                        {
                            displayName = fieldName,
                            children = { CreateVolumeParameterWidget(Strings.resultValue, stackComponent.parameterList[currentParam]) },
                        };

                        foreach (var volume in volumes)
                        {
                            VolumeParameter param = null;
                            var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
                            if (profile.TryGet(selectedType, out VolumeComponent component))
                                param = component.parameterList[currentParam];
                            row.children.Add(CreateVolumeParameterWidget(volume.name + " (" + profile.name + ")", param, () => !component.parameterList[currentParam].overrideState));
                        }

                        foreach (var (customProfile, customComponent) in customDefaultComponents)
                            row.children.Add(CreateVolumeParameterWidget(customProfile.name,
                                customComponent != null ? customComponent.parameterList[currentParam] : null));

                        row.children.Add(CreateVolumeParameterWidget(Strings.qualityLevelValue,
                            qualityDefaultComponent != null ? qualityDefaultComponent.parameterList[currentParam] : null));

                        row.children.Add(CreateVolumeParameterWidget(Strings.globalDefaultValue,
                            globalDefaultComponent != null ? globalDefaultComponent.parameterList[currentParam] : null));

                        rows.Add(row);
                    }

                    var fields = type
                        .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
                        .OrderBy(t => t.MetadataToken);
                    foreach (var field in fields)
                    {
                        if (field.GetCustomAttributes(typeof(ObsoleteAttribute), false).Length != 0)
                        {
                            skip++;
                            continue;
                        }
                        var fieldType = field.FieldType;
                        if (fieldType.IsSubclassOf(typeof(VolumeParameter)))
                            AddRow(field, baseName ?? string.Empty, skip);
                        else if (!fieldType.IsArray && fieldType.IsClass)
                            skip += AddParameterRows(fieldType, baseName ?? (field.Name + " "), skip);
                    }
                    return skip;
                }

                AddParameterRows(selectedType);
                foreach (var r in rows.OrderBy(t => t.displayName))
                    table.children.Add(r);

                data.volumeDebugSettings.RefreshVolumes(volumes);
                for (int i = 0; i < volumes.Length; i++)
                    table.SetColumnVisibility(i + 1, data.volumeDebugSettings.VolumeHasInfluence(volumes[i]));

                float timer = 0.0f, refreshRate = 0.2f;
                table.isHiddenCallback = () =>
                {
                    timer += Time.deltaTime;
                    if (timer >= refreshRate)
                    {
                        if (data.volumeDebugSettings.selectedCamera != null)
                        {
                            var newVolumes = data.volumeDebugSettings.GetVolumes();
                            if (!data.volumeDebugSettings.RefreshVolumes(newVolumes))
                            {
                                for (int i = 0; i < newVolumes.Length; i++)
                                {
                                    var visible = data.volumeDebugSettings.VolumeHasInfluence(newVolumes[i]);
                                    table.SetColumnVisibility(i + 1, visible);
                                }
                            }

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
        }

        [DisplayInfo(name = k_PanelTitle, order = int.MaxValue)]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsVolume>
        {
            public SettingsPanel(DebugDisplaySettingsVolume data)
                : base(data)
            {
                AddWidget(WidgetFactory.CreateCameraSelector(this, (_, __) => Refresh()));
                AddWidget(WidgetFactory.CreateComponentSelector(this, (_, __) => Refresh()));
                m_VolumeTable = WidgetFactory.CreateVolumeTable(m_Data);
                AddWidget(m_VolumeTable);
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

        /// <inheritdoc/>
        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
