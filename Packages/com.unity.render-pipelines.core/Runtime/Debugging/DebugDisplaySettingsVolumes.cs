using System;
using System.Collections.Generic;
using System.Reflection;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Debug Display Settings Volume
    /// </summary>
    public class DebugDisplaySettingsVolume : IDebugDisplaySettingsData
    {
        /// <summary>Current volume debug settings.</summary>
        [Obsolete("This property has been obsoleted and will be removed in a future version. #from(6000.2)", false)]
        public IVolumeDebugSettings volumeDebugSettings { get; }

        private int m_SelectedComponentIndex = -1;

        /// <summary>Current volume component to debug.</summary>
        public int selectedComponent
        {
            get => m_SelectedComponentIndex;
            set
            {
                if (value != m_SelectedComponentIndex)
                {
                    m_SelectedComponentIndex = value;
                    OnSelectionChanged();
                }
            }
        }

        private void DestroyVolumeInterpolatedResults()
        {
            if (m_VolumeInterpolatedResults != null)
                ScriptableObject.DestroyImmediate(m_VolumeInterpolatedResults);
        }

        /// <summary>Type of the current component to debug.</summary>
        public Type selectedComponentType
        {
            get => selectedComponent > 0 ? volumeComponentsPathAndType[selectedComponent - 1].Item2 : null;
            set
            {
                var index = volumeComponentsPathAndType.FindIndex(t => t.Item2 == value);
                if (index != -1)
                    selectedComponent = index + 1;
            }
        }

        /// <summary>List of Volume component types.</summary>
        public List<(string, Type)> volumeComponentsPathAndType => VolumeManager.instance.GetVolumeComponentsForDisplay(GraphicsSettings.currentRenderPipelineAssetType);

        private Camera m_SelectedCamera;

        /// <summary>Current camera to debug.</summary>
        public Camera selectedCamera
        {
            get
            {
#if UNITY_EDITOR
                // By default pick the one scene camera
                if (m_SelectedCamera == null && SceneView.lastActiveSceneView != null)
                {
                    var sceneCamera = SceneView.lastActiveSceneView.camera;
                    if (sceneCamera != null)
                        m_SelectedCamera = sceneCamera;
                }
#endif

                return m_SelectedCamera;
            }
            set
            {
                if (value != null && value != m_SelectedCamera)
                {
                    m_SelectedCamera = value;
                    OnSelectionChanged();
                }
            }
        }

        private void OnSelectionChanged()
        {
            ClearInterpolationData();
            DestroyVolumeInterpolatedResults();
        }

        VolumeComponent m_VolumeInterpolatedResults;
        private bool m_StoreStackInterpolatedValues;
        private ObservableList<Volume> m_InfluenceVolumes = new ();
        private List<(Volume volume, float weight)> m_VolumesWeights = new ();

        private void ClearInterpolationData()
        {
            m_VolumesWeights.Clear();
        }

        static bool AreVolumesChanged(ObservableList<Volume> influenceVolumes, List<(Volume volume, float weight)> volumesWeights)
        {
            // First, check if the lists have the same number of elements
            if (influenceVolumes.Count != volumesWeights.Count)
                return true;

            // Sequence Equals
            for (int i = 0; i < influenceVolumes.Count; i++)
            {
                if (influenceVolumes[i] != volumesWeights[i].volume)
                    return true;
            }

            // If all checks pass, the lists are the same (in terms of both content and order)
            return false;
        }

        private void OnBeginVolumeStackUpdate(VolumeStack stack, Camera camera)
        {
            if (camera == selectedCamera)
            {
                ClearInterpolationData();
                m_StoreStackInterpolatedValues = selectedCamera != null && selectedComponentType != null;
            }
        }

        private void OnEndVolumeStackUpdate(VolumeStack stack, Camera camera)
        {
            if (m_StoreStackInterpolatedValues)
            {
                if (AreVolumesChanged(m_InfluenceVolumes, m_VolumesWeights))
                {
                    m_InfluenceVolumes.Clear();
                    foreach (var pair in m_VolumesWeights)
                        m_InfluenceVolumes.Add(pair.volume);
                }

                // Copy the results of the interpolation into our resulVolumeComponent
                var componentInStack = stack.GetComponent(selectedComponentType);

                for (int i = 0; i < componentInStack.parameters.Count; ++i)
                {
                    resultVolumeComponent.parameters[i].SetValue(componentInStack.parameters[i]);
                }

                m_StoreStackInterpolatedValues = false;
            }
        }

        private void OnVolumeStackInterpolated(VolumeStack stack, Volume volume, float interpolationFactor)
        {
            if (m_StoreStackInterpolatedValues)
            {
                m_VolumesWeights.Add((volume, interpolationFactor));
            }
        }

        /// <summary>
        /// Obtains the volume weight
        /// </summary>
        /// <param name="volume"><see cref="Volume"/></param>
        /// <returns>The weight of the volume</returns>
        public float GetVolumeWeight(Volume volume)
        {
            // Try to get the weight associated with the volume
            // If the volume is not found, return a default value (e.g., 0.0f)
            if (m_VolumesWeights.Count == 0)
                return 0.0f;

            foreach (var pair in m_VolumesWeights)
            {
                if (volume == pair.volume)
                    return pair.weight;
            }

            return 0.0f;
        }

        /// <summary>
        /// Gets the Volumes List for the current camera and selected volume component
        /// </summary>
        /// <returns>The list of influenced volumes</returns>
        public ObservableList<Volume> GetVolumesList()
        {
            return m_InfluenceVolumes;
        }


        void IDebugDisplaySettingsData.Reset()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            VolumeManager.instance.overrideVolumeStackData -= OnVolumeStackInterpolated;
            VolumeManager.instance.beginVolumeStackUpdate -= OnBeginVolumeStackUpdate;
            VolumeManager.instance.endVolumeStackUpdate -= OnEndVolumeStackUpdate;
            VolumeManager.instance.renderingDebuggerAttached = false;
#endif

            ClearInterpolationData();
            DestroyVolumeInterpolatedResults();
        }

        /// <summary>
        /// Constructor with the settings
        /// </summary>
        /// <param name="volumeDebugSettings">The volume debug settings object used for configuration.</param>
        [Obsolete("This constructor has been obsoleted and will be removed in a future version. #from(6000.2)", false)]
        public DebugDisplaySettingsVolume(IVolumeDebugSettings volumeDebugSettings)
            : this()
        {
            this.volumeDebugSettings = volumeDebugSettings;
        }

        /// <summary>
        /// Constructor with the settings
        /// </summary>
        public DebugDisplaySettingsVolume()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            VolumeManager.instance.overrideVolumeStackData += OnVolumeStackInterpolated;
            VolumeManager.instance.beginVolumeStackUpdate += OnBeginVolumeStackUpdate;
            VolumeManager.instance.endVolumeStackUpdate += OnEndVolumeStackUpdate;
#endif
        }

        internal int volumeComponentEnumIndex;
        internal VolumeComponent resultVolumeComponent
        {
            get
            {
                if (m_VolumeInterpolatedResults == null)
                    m_VolumeInterpolatedResults = ScriptableObject.CreateInstance(selectedComponentType) as VolumeComponent;

                return m_VolumeInterpolatedResults;
            }
        }

        internal static string ExtractResult(VolumeParameter param)
        {
            if (param == null)
                return Strings.parameterNotCalculated;

            var paramType = param.GetType();

            var property = paramType.GetProperty("value");
            if (property == null)
                return "-";

            var value = property.GetValue(param);
            var propertyType = property.PropertyType;
            if (value == null || value.Equals(null))
                return Strings.none + $" ({propertyType.Name})";

            var toString = propertyType.GetMethod("ToString", Type.EmptyTypes);
            if ((toString == null) || (toString.DeclaringType == typeof(object)) || (toString.DeclaringType == typeof(Object)))
            {
                // Check if the parameter has a name
                var nameProp = property.PropertyType.GetProperty("name");
                if (nameProp == null)
                    return Strings.debugViewNotSupported;

                var valueString = $"{nameProp.GetValue(value)}";
                return valueString ?? Strings.none;
            }

            return value.ToString();
        }

        static class Styles
        {
            public static readonly GUIContent none = new GUIContent("None");
        }

        static class Strings
        {
            public static readonly string cameraNeedsRendering = "Values might not be fully updated if the camera you are inspecting is not rendered.";
            public static readonly string none = "None";
            public static readonly string parameter = "Parameter";
            public static readonly string component = "Component";
            public static readonly string debugViewNotSupported = "N/A";
            public static readonly string volumeInfo = "Volume Info";
            public static readonly string gameObject = "GameObject";
            public static readonly string priority = "Priority";
            public static readonly string resultValue = "Result";
            public static readonly string resultValueTooltip = "The interpolated result value of the parameter. This value is used to render the camera.";
            public static readonly string globalDefaultValue = "Graphics Settings";
            public static readonly string globalDefaultValueTooltip = "Default value for this parameter, defined by the Default Volume Profile in Global Settings.";
            public static readonly string qualityLevelValue = "Quality Settings";
            public static readonly string qualityLevelValueTooltip = "Override value for this parameter, defined by the Volume Profile in the current SRP Asset.";
            public static readonly string global = "Global";
            public static readonly string local = "Local";
            public static readonly string volumeProfile = "Volume Profile";
            public static readonly string parameterNotCalculated = "N/A";
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

                var volumesAndTypes = panel.data.volumeComponentsPathAndType;
                foreach (var type in volumesAndTypes)
                {
                    componentNames.Add(new GUIContent() { text = type.Item1 });
                    componentValues.Add(componentIndex++);
                }

                return new DebugUI.EnumField
                {
                    displayName = Strings.component,
                    getter = () => panel.data.selectedComponent,
                    setter = value => panel.data.selectedComponent = value,
                    enumNames = componentNames.ToArray(),
                    enumValues = componentValues.ToArray(),
                    getIndex = () => panel.data.volumeComponentEnumIndex,
                    setIndex = value => { panel.data.volumeComponentEnumIndex = value; },
                    onValueChanged = refresh
                };
            }

            public static DebugUI.ObjectPopupField CreateCameraSelector(SettingsPanel panel, Action<DebugUI.Field<Object>, Object> refresh)
            {
                return new DebugUI.CameraSelector()
                {
                    getter = () => panel.data.selectedCamera,
                    setter = value => panel.data.selectedCamera = value as Camera,
                    onValueChanged = refresh
                };
            }

            internal static DebugUI.Widget CreateVolumeParameterWidget(string name, bool isResultParameter, VolumeParameter param)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (param != null)
                {
                    Func<bool> isHiddenCallback = isResultParameter ?
                            () => false :
                            () => !param.overrideState;
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
                    else if (parameterType.BaseType.IsGenericType && parameterType.BaseType.GetGenericArguments().Length > 0)
                    {
                        // Get the generic type argument, e.g., <Texture> in VolumeParameter<Texture>
                        var genericArgument = parameterType.BaseType.GetGenericArguments()[0];

                        // Check if the argument is a UnityEngine.Object or derived type
                        if (typeof(Object).IsAssignableFrom(genericArgument))
                        {
                            return new DebugUI.ObjectField()
                            {
                                displayName = name,
                                getter = () =>
                                {
                                    var property = parameterType.GetProperty("value");
                                    if (property == null)
                                        return null;

                                    var value = property.GetValue(param);
                                    return value as Object;

                                },
                                isHiddenCallback = isHiddenCallback
                            };
                        }
                    }

                    var typeInfo = parameterType.GetTypeInfo();
                    var genericArguments = typeInfo.BaseType.GenericTypeArguments;
                    if (genericArguments.Length > 0 && genericArguments[0].IsArray)
                    {
                        return new DebugUI.ObjectListField()
                        {
                            displayName = name,
                            getter = () => (Object[])parameterType.GetProperty("value").GetValue(param, null),
                            type = parameterType,
                            isHiddenCallback = isHiddenCallback
                        };
                    }

                    return new DebugUI.Value()
                    {
                        displayName = name,
                        getter = () => ExtractResult(param),
                        isHiddenCallback = isHiddenCallback
                    };
                }
    #endif
                return new DebugUI.Value() { displayName = name, getter = () => Strings.parameterNotCalculated, };
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

                Type selectedType = data.selectedComponentType;
                if (data.selectedCamera == null || selectedType == null)
                    return chain;

                if (data.resultVolumeComponent == null)
                    return chain;

                var result = new VolumeParameterChain()
                {
                    nameAndTooltip = new DebugUI.Widget.NameAndTooltip()
                    {
                        name = Strings.resultValue,
                        tooltip = Strings.resultValueTooltip,
                    },
                    volumeComponent = data.resultVolumeComponent
                };

                chain.Add(result);

                // Add volume components that override the default values.
                // Iterate in reverse order to display the last interpolated Volume (most relevant) next to the result in the table view.
                var volumes = data.GetVolumesList();
                for (int i = volumes.Count - 1; i >= 0; i--)
                {
                    var volume = volumes[i];
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

                // Add custom default profiles
                if (VolumeManager.instance.customDefaultProfiles != null)
                {
                    foreach (var customProfile in VolumeManager.instance.customDefaultProfiles)
                    {
                        var customProfileComponent = GetSelectedVolumeComponent(customProfile, selectedType);
                        if (customProfileComponent != null)
                        {
                            var overrideVolume = new VolumeParameterChain()
                            {
                                nameAndTooltip = new DebugUI.Widget.NameAndTooltip()
                                {
                                    name = customProfile.name,
                                    tooltip = customProfile.name,
                                },
                                volumeProfile = customProfile,
                                volumeComponent = customProfileComponent,
                            };
                            chain.Add(overrideVolume);
                        }
                    }
                }

                // Add Quality Settings
                if (VolumeManager.instance.qualityDefaultProfile != null)
                {
                    var qualitySettingsComponent = GetSelectedVolumeComponent(VolumeManager.instance.qualityDefaultProfile, selectedType);
                    if (qualitySettingsComponent != null)
                    {
                        var overrideVolume = new VolumeParameterChain()
                        {
                            nameAndTooltip = new DebugUI.Widget.NameAndTooltip()
                            {
                                name = Strings.qualityLevelValue,
                                tooltip = Strings.qualityLevelValueTooltip,
                            },
                            volumeProfile = VolumeManager.instance.qualityDefaultProfile,
                            volumeComponent = qualitySettingsComponent,
                        };
                        chain.Add(overrideVolume);
                    }
                }

                // Add Graphics Settings
                if (VolumeManager.instance.globalDefaultProfile != null)
                {
                    var graphicsSettingsComponent = GetSelectedVolumeComponent(VolumeManager.instance.globalDefaultProfile, selectedType);
                    if (graphicsSettingsComponent != null)
                    {
                        var overrideVolume = new VolumeParameterChain()
                        {
                            nameAndTooltip = new DebugUI.Widget.NameAndTooltip()
                            {
                                name = Strings.globalDefaultValue,
                                tooltip = Strings.globalDefaultValueTooltip,
                            },
                            volumeProfile = VolumeManager.instance.globalDefaultProfile,
                            volumeComponent = graphicsSettingsComponent,
                        };
                        chain.Add(overrideVolume);
                    }
                }

                return chain;
            }

            public static DebugUI.Table CreateVolumeTable(DebugDisplaySettingsVolume data)
            {
                // Function for updating the attach state and also checking if the table should be visible
                Func<bool> hiddenCallback = () =>
                {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    VolumeManager.instance.renderingDebuggerAttached = data.selectedComponent > 0 && data.selectedCamera != null;
                    return !VolumeManager.instance.renderingDebuggerAttached;
#else
                    return true;
#endif
                };
                var table = new DebugUI.Table()
                {
                    displayName = Strings.parameter,
                    isReadOnly = true,
                    isHiddenCallback = hiddenCallback,
                };

                var resolutionChain = GetResolutionChain(data);
                if (resolutionChain.Count == 0)
                    return table;

                GenerateTableRows(table, resolutionChain);
                GenerateTableColumns(table, data, resolutionChain);

                return table;
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
                                var weight = data.GetVolumeWeight(chain.volume);
                                if (chain.volumeComponent.active)
                                    return $"{scope} ({(weight * 100f):F2}%)";
                                else
                                    return $"{scope} (disabled)";
                            },
                            refreshRate = 0.2f
                        });
                        ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(new DebugUI.ObjectField() { displayName = string.Empty, getter = () => chain.volume });
                        ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(new DebugUI.Value()
                        {
                            nameAndTooltip = chain.nameAndTooltip,
                            getter = () => chain.volume.priority
                        });
                    }
                    else
                    {
                        ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(new DebugUI.Value()
                        {
                            nameAndTooltip = chain.nameAndTooltip,
                            getter = () => string.Empty
                        });
                        ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(s_EmptyDebugUIValue);
                        ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(s_EmptyDebugUIValue);
                    }

                    ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(chain.volumeProfile != null ?
                        new DebugUI.ObjectField() { displayName = string.Empty, getter = () => chain.volumeProfile } :
                        s_EmptyDebugUIValue);

                    ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(s_EmptyDebugUIValue);

                    bool isResultParameter = i == 0;
                    for (int j = 0; j < chain.volumeComponent.parameterList.Length; ++j)
                    {
                        var parameter = chain.volumeComponent.parameterList[j];
                        ((DebugUI.Table.Row)table.children[++iRowIndex]).children.Add(
                            CreateVolumeParameterWidget(chain.nameAndTooltip.name, isResultParameter, parameter));
                    }
                }
            }

            private static void GenerateTableRows(DebugUI.Table table, List<VolumeParameterChain> resolutionChain)
            {
                var volumeInfoRow = new DebugUI.Table.Row()
                {
                    displayName = Strings.volumeInfo,
                    opened = true, // Open by default for the in-game view
                };

                table.children.Add(volumeInfoRow);

                var gameObjectRow = new DebugUI.Table.Row()
                {
                    displayName = Strings.gameObject,
                };

                table.children.Add(gameObjectRow);

                var priorityRow = new DebugUI.Table.Row()
                {
                    displayName = Strings.priority,
                };

                table.children.Add(priorityRow);

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
                for (int i = 0; i < results.parameterList.Length; ++i)
                {
                    var parameter = results.parameterList[i];
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    string displayName = VolumeDebugData.GetVolumeParameterDebugId(parameter);// In the development player, just the debug id
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

        [DisplayInfo(name = k_PanelTitle, order = int.MaxValue)]
        internal class SettingsPanel : DebugDisplaySettingsPanel<DebugDisplaySettingsVolume>
        {
            // When we are moving the scene camera, we want the editor window to be repainted too
            public override DebugUI.Flags Flags => DebugUI.Flags.EditorForceUpdate;

            public override void Dispose()
            {
                base.Dispose();

                data.GetVolumesList().ItemAdded -= OnVolumeInfluenceChanged;
                data.GetVolumesList().ItemRemoved -= OnVolumeInfluenceChanged;
            }

            public SettingsPanel(DebugDisplaySettingsVolume data)
                : base(data)
            {
                AddWidget(WidgetFactory.CreateCameraSelector(this, (_, __) => Refresh()));
                AddWidget(WidgetFactory.CreateComponentSelector(this, (_, __) => Refresh()));

                Func<bool> hiddenCallback = () => data.selectedCamera == null || data.selectedComponent <= 0;
                AddWidget(new DebugUI.MessageBox()
                {
                    displayName = Strings.cameraNeedsRendering,
                    style = DebugUI.MessageBox.Style.Warning,
                    isHiddenCallback = hiddenCallback,
                });
                m_VolumeTable = WidgetFactory.CreateVolumeTable(data);
                AddWidget(m_VolumeTable);
                data.GetVolumesList().ItemAdded += OnVolumeInfluenceChanged;
                data.GetVolumesList().ItemRemoved += OnVolumeInfluenceChanged;
            }

            private void OnVolumeInfluenceChanged(ObservableList<Volume> sender, ListChangedEventArgs<Volume> e)
            {
                Refresh();
                DebugManager.instance.ReDrawOnScreenDebug();
            }

            DebugUI.Table m_VolumeTable = null;
            void Refresh()
            {
                var panel = DebugManager.instance.GetPanel(PanelName);
                if (panel == null)
                    return;

                bool needsRefresh = false;
                if (m_Data.selectedComponent > 0 && m_Data.selectedCamera != null)
                {
                    needsRefresh = true;
                    var volumeTable = WidgetFactory.CreateVolumeTable(m_Data);

                    m_VolumeTable.children.Clear();
                    foreach (var row in volumeTable.children)
                    {
                        m_VolumeTable.children.Add(row);
                    }
                }

                if (needsRefresh)
                {
                    DebugManager.instance.ReDrawOnScreenDebug();
                }
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
