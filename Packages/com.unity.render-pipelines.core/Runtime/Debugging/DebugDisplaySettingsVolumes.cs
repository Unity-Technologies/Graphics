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
            public static readonly string debugViewNotSupported = "Debug view not supported";
            public static readonly string volumeInfo = "Volume Info";
            public static readonly string interpolatedValue = "Interpolated Value";
            public static readonly string defaultValue = "Default Value";
            public static readonly string global = "Global";
            public static readonly string local = "Local";
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

            public static DebugUI.Table CreateVolumeTable(DebugDisplaySettingsVolume data)
            {
                var table = new DebugUI.Table()
                {
                    displayName = Strings.parameter,
                    isReadOnly = true
                };

                Type selectedType = data.volumeDebugSettings.selectedComponentType;
                if (selectedType == null)
                    return table;

                var stack = data.volumeDebugSettings.selectedCameraVolumeStack ?? VolumeManager.instance.stack;
                var stackComponent = stack.GetComponent(selectedType);
                if (stackComponent == null)
                    return table;

                var volumes = data.volumeDebugSettings.GetVolumes();

                var inst = (VolumeComponent)ScriptableObject.CreateInstance(selectedType);

                // First row for volume info
                var row = new DebugUI.Table.Row()
                {
                    displayName = Strings.volumeInfo,
                    opened = true, // Open by default for the in-game view
                    children =
                    {
                        new DebugUI.Value()
                        {
                            displayName = Strings.interpolatedValue,
                            getter = () => string.Empty
                        }
                    }
                };

                // Second row, links to volume gameobjects
                var row2 = new DebugUI.Table.Row()
                {
                    displayName = "GameObject",
                    children = { new DebugUI.Value() { getter = () => string.Empty } }
                };

                foreach (var volume in volumes)
                {
                    var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
                    row.children.Add(new DebugUI.Value()
                    {
                        displayName = profile.name,
                        getter = () =>
                        {
                            var scope = volume.isGlobal ? Strings.global : Strings.local;
                            var weight = data.volumeDebugSettings.GetVolumeWeight(volume);
                            return scope + " (" + (weight * 100f) + "%)";
                        }
                    });

                    row2.children.Add(new DebugUI.ObjectField()
                    {
                        displayName = profile.name,
                        getter = () => volume,
                    });
                }

                row.children.Add(new DebugUI.Value() { displayName = Strings.defaultValue, getter = () => string.Empty });
                table.children.Add(row);

                row2.children.Add(new DebugUI.Value() { getter = () => string.Empty });
                table.children.Add(row2);

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
                        row = new DebugUI.Table.Row()
                        {
                            displayName = fieldName,
                            children = { CreateVolumeParameterWidget(Strings.interpolatedValue, stackComponent.parameters[currentParam]) },
                        };

                        foreach (var volume in volumes)
                        {
                            VolumeParameter param = null;
                            var profile = volume.HasInstantiatedProfile() ? volume.profile : volume.sharedProfile;
                            if (profile.TryGet(selectedType, out VolumeComponent component))
                                param = component.parameters[currentParam];
                            row.children.Add(CreateVolumeParameterWidget(volume.name + " (" + profile.name + ")", param, () => !component.parameters[currentParam].overrideState));
                        }

                        row.children.Add(CreateVolumeParameterWidget(Strings.defaultValue, inst.parameters[currentParam]));
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
