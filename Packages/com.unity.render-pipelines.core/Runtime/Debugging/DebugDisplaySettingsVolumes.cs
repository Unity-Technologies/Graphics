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
        public IVolumeDebugSettings volumeDebugSettings { get; }

        /// <summary>
        /// Constructor with the settings
        /// </summary>
        /// <param name="volumeDebugSettings"></param>
        public DebugDisplaySettingsVolume(IVolumeDebugSettings volumeDebugSettings)
        {
            this.volumeDebugSettings = volumeDebugSettings;
        }

        internal int volumeComponentEnumIndex;
        internal int volumeCameraEnumIndex;

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
            public static DebugUI.EnumField CreateComponentSelector(DebugDisplaySettingsVolume data, Action<DebugUI.Field<int>, int> refresh)
            {
                int componentIndex = 0;
                var componentNames = new List<GUIContent>() { Styles.none };
                var componentValues = new List<int>() { componentIndex++ };

                foreach (var type in data.volumeDebugSettings.componentTypes)
                {
                    componentNames.Add(new GUIContent() { text = type.Item1 });
                    componentValues.Add(componentIndex++);
                }

                return new DebugUI.EnumField
                {
                    displayName = Strings.component,
                    getter = () => data.volumeDebugSettings.selectedComponent,
                    setter = value => data.volumeDebugSettings.selectedComponent = value,
                    enumNames = componentNames.ToArray(),
                    enumValues = componentValues.ToArray(),
                    getIndex = () => data.volumeComponentEnumIndex,
                    setIndex = value => { data.volumeComponentEnumIndex = value; },
                    onValueChanged = refresh
                };
            }

            public static DebugUI.EnumField CreateCameraSelector(DebugDisplaySettingsVolume data, Action<DebugUI.Field<int>, int> refresh)
            {
                int componentIndex = 0;
                var componentNames = new List<GUIContent>() { Styles.none };
                var componentValues = new List<int>() { componentIndex++ };

#if UNITY_EDITOR
                componentNames.Add(Styles.editorCamera);
                componentValues.Add(componentIndex++);
#endif

                foreach (var camera in data.volumeDebugSettings.cameras)
                {
                    componentNames.Add(new GUIContent() { text = camera.name });
                    componentValues.Add(componentIndex++);
                }

                return new DebugUI.EnumField
                {
                    displayName = Strings.camera,
                    getter = () => data.volumeDebugSettings.selectedCameraIndex,
                    setter = value => data.volumeDebugSettings.selectedCameraIndex = value,
                    enumNames = componentNames.ToArray(),
                    enumValues = componentValues.ToArray(),
                    getIndex = () => data.volumeCameraEnumIndex,
                    setIndex = value => { data.volumeCameraEnumIndex = value; },
                    isHiddenCallback = () => data.volumeComponentEnumIndex == 0,
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
                        setter = _ => { },
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
                        setter = _ => { },
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
                float timer = 0.0f, refreshRate = 0.2f;
                var row = new DebugUI.Table.Row()
                {
                    displayName = Strings.volumeInfo,
                    opened = true, // Open by default for the in-game view
                    children =
                    {
                        new DebugUI.Value()
                        {
                            displayName = Strings.interpolatedValue,
                            getter = () => {
                                // This getter is called first at each render
                                // It is used to update the volumes
                                if (Time.time - timer < refreshRate)
                                    return string.Empty;
                                timer = Time.deltaTime;
                                if (data.volumeDebugSettings.selectedCameraIndex != 0)
                                {
                                    var newVolumes = data.volumeDebugSettings.GetVolumes();
                                    if (!data.volumeDebugSettings.RefreshVolumes(newVolumes))
                                    {
                                        for (int i = 0; i < newVolumes.Length; i++)
                                        {
                                            var visible = data.volumeDebugSettings.VolumeHasInfluence(newVolumes[i]);
                                            table.SetColumnVisibility(i + 1, visible);
                                        }
                                        return string.Empty;
                                    }
                                }
                                DebugManager.instance.ReDrawOnScreenDebug();
                                return string.Empty;
                            }
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
                void AddParameterRows(Type type, string baseName = null)
                {
                    void AddRow(FieldInfo f, string prefix)
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

                        int currentParam = rows.Count;
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
                            continue;
                        var fieldType = field.FieldType;
                        if (fieldType.IsSubclassOf(typeof(VolumeParameter)))
                            AddRow(field, baseName ?? string.Empty);
                        else if (!fieldType.IsArray && fieldType.IsClass)
                            AddParameterRows(fieldType, baseName ?? (field.Name + " "));
                    }
                }

                AddParameterRows(selectedType);
                foreach (var r in rows.OrderBy(t => t.displayName))
                    table.children.Add(r);

                data.volumeDebugSettings.RefreshVolumes(volumes);
                for (int i = 0; i < volumes.Length; i++)
                    table.SetColumnVisibility(i + 1, data.volumeDebugSettings.VolumeHasInfluence(volumes[i]));

                return table;
            }
        }

        private class SettingsPanel : DebugDisplaySettingsPanel
        {
            readonly DebugDisplaySettingsVolume m_Data;

            public override string PanelName => "Volume";

            public SettingsPanel(DebugDisplaySettingsVolume data)
            {
                m_Data = data;
                AddWidget(WidgetFactory.CreateComponentSelector(m_Data, Refresh));
                AddWidget(WidgetFactory.CreateCameraSelector(m_Data, Refresh));
            }

            DebugUI.Table m_VolumeTable = null;
            void Refresh(DebugUI.Field<int> _, int __)
            {
                var panel = DebugManager.instance.GetPanel(PanelName);
                if (panel == null)
                    return;

                if (m_VolumeTable != null)
                    panel.children.Remove(m_VolumeTable);

                if (m_Data.volumeDebugSettings.selectedComponent > 0 && m_Data.volumeDebugSettings.selectedCameraIndex > 0)
                {
                    m_VolumeTable = WidgetFactory.CreateVolumeTable(m_Data);
                    AddWidget(m_VolumeTable);
                    panel.children.Add(m_VolumeTable);
                }

                DebugManager.instance.ReDrawOnScreenDebug();
            }
        }

        #region IDebugDisplaySettingsData
        /// <summary>
        /// If are any settings active
        /// </summary>
        public bool AreAnySettingsActive => volumeCameraEnumIndex > 0 || volumeComponentEnumIndex > 0;
        /// <summary>
        /// If post processing is allowed
        /// </summary>
        public bool IsPostProcessingAllowed => true;
        /// <summary>
        /// If lighting is active
        /// </summary>
        public bool IsLightingActive => true;
        /// <summary>
        /// Returns if the clear color can be returned
        /// </summary>
        /// <param name="color">[In/out]The color </param>
        /// <returns>True if the screen clear color can  be returned</returns>
        public bool TryGetScreenClearColor(ref Color color)
        {
            return false;
        }

        /// <summary>
        /// Creates the panel
        /// </summary>
        /// <returns>The panel tha is created</returns>
        public IDebugDisplaySettingsPanelDisposable CreatePanel()
        {
            return new SettingsPanel(this);
        }

        #endregion
    }
}
