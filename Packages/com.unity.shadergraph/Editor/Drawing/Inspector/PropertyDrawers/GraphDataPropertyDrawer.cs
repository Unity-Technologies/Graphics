using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditorInternal;
using UnityEditor.ShaderGraph.Serialization;
using System.Text.RegularExpressions;

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(GraphData))]
    public class GraphDataPropertyDrawer : IPropertyDrawer
    {
        public delegate void ChangeGraphDefaultPrecisionCallback(GraphPrecision newDefaultGraphPrecision);
        public delegate void PostTargetSettingsChangedCallback();
        internal delegate void GraphSettingsChangedCallback();

        GraphSettingsChangedCallback m_changeGraphSettingsCallback;
        PostTargetSettingsChangedCallback m_postChangeTargetSettingsCallback;
        ChangeGraphDefaultPrecisionCallback m_changeGraphDefaultPrecisionCallback;

        bool m_AdvancedSettingsFoldout;
        const string customPragmaTooltip = @"The following pragmas will be added in the given order, after other pragmas.
Enter what comes after '#pragma' (e.g.: enable_debug_symbols or use_dxc)";
        const string customDefineTooltip = @"The following defines will be added in the given order, after other graph defines.
Enter what comes after '#define' (e.g.: _DEBUG or MAX_STEPS 32 // your comments.)
Uncheck the checkbox to temporarily disable the define.";
        const string customIncludeTooltip = @"The following hlsl files will be included in the given order, prior to other graph includes (e.g.: Custom Function Nodes).";

        Dictionary<Target, bool> m_TargetFoldouts = new Dictionary<Target, bool>();
        Dictionary<AbstractShaderGraphDataExtension, bool> m_SubDataFoldouts = new Dictionary<AbstractShaderGraphDataExtension, bool>();

        internal void GetPropertyData(
            PostTargetSettingsChangedCallback postChangeValueCallback,
            GraphSettingsChangedCallback changeGraphSettingsCallback,
            ChangeGraphDefaultPrecisionCallback changeGraphDefaultPrecisionCallback)
        {
            m_postChangeTargetSettingsCallback = postChangeValueCallback;
            m_changeGraphSettingsCallback = changeGraphSettingsCallback;
            m_changeGraphDefaultPrecisionCallback = changeGraphDefaultPrecisionCallback;
        }

        VisualElement GetAdvancedSettings(GraphData graphData, Action onChange)
        {
            var element = new VisualElement() { name = "advGraphSettings" };

            void RegisterActionToUndo(string actionName)
            {
                graphData.owner.RegisterCompleteObjectUndo(actionName);
            }

            // Advanced Settings
            var advancedSettingsFoldout = new Foldout() { text = "Preprocessor Directives", value = m_AdvancedSettingsFoldout, name = "advGraphSettings" };
            advancedSettingsFoldout.style.unityFontStyleAndWeight = FontStyle.Bold;
            element.Add(advancedSettingsFoldout);
            advancedSettingsFoldout.AddToClassList("MainFoldout");
            advancedSettingsFoldout.RegisterValueChangedCallback(evt =>
            {
                m_AdvancedSettingsFoldout = evt.newValue;
                advancedSettingsFoldout.value = evt.newValue;
                onChange();
            });

            if (advancedSettingsFoldout.value)
            {
                var advancedSettingsElement = new VisualElement();
                element.Add(advancedSettingsElement);

                // Custom Pragmas
                var customPragmasList = new ReorderableListView<string>(
                    graphData.m_CustomPragmas,
                    new GUIContent("Pragmas", customPragmaTooltip));

                customPragmasList.OnBeforeChangeCallback +=
                    (ReorderableListView<string>.ListActionType changeType) =>
                    {
                        RegisterActionToUndo($"{changeType} Pragma");
                    };

                customPragmasList.OnChangeCallback +=
                    (ReorderableListView<string>.ListActionType changeType) =>
                    {
                        onChange();
                    };

                customPragmasList.DrawItemCallback += (Rect rect, int idx) =>
                {
                    EditorGUI.BeginChangeCheck();
                    var data = graphData.m_CustomPragmas[idx];
                    data = EditorGUI.DelayedTextField(rect, "", data);

                    if (EditorGUI.EndChangeCheck())
                    {
                        RegisterActionToUndo("Change Pragma");
                        data = ValidatePragma(data);
                        graphData.m_CustomPragmas[idx] = data;
                        onChange();
                    }
                };

                advancedSettingsElement.Add(customPragmasList);

                // Custom Defines
                var customDefineList = new ReorderableListView<GraphData.CustomDefineDescriptor>(
                    graphData.m_CustomDefines,
                    new GUIContent("Local Defines", customDefineTooltip));

                customDefineList.OnNewItemCallback += () => new GraphData.CustomDefineDescriptor("_DEFINE");

                customDefineList.OnBeforeChangeCallback +=
                    (ReorderableListView<GraphData.CustomDefineDescriptor>.ListActionType changeType) =>
                    {
                        RegisterActionToUndo($"{changeType} Local Define");
                    };

                customDefineList.OnChangeCallback +=
                    (ReorderableListView<GraphData.CustomDefineDescriptor>.ListActionType changeType) =>
                    {
                        onChange();
                    };

                customDefineList.DrawItemCallback += (Rect rect, int idx) =>
                {
                    Rect objectFieldRect = new Rect(rect.x, rect.y, rect.width - 24, rect.height);
                    Rect toggleRect = new Rect(objectFieldRect.max.x+8, rect.y, 24, rect.height);

                    var data = graphData.m_CustomDefines[idx];

                    EditorGUI.BeginChangeCheck();
                    data.Define = EditorGUI.DelayedTextField(objectFieldRect, new GUIContent("", ""), data.Define);
                    data.Enabled = EditorGUI.ToggleLeft(toggleRect, "", data.Enabled);

                    if (EditorGUI.EndChangeCheck())
                    {
                        RegisterActionToUndo("Change Local Define");
                        data.Define = ValidateDefine(data.Define);
                        graphData.m_CustomDefines[idx] = data;
                        onChange();
                    }
                };

                advancedSettingsElement.Add(customDefineList);

                // Custom Includes
                var customIncludeList = new ReorderableListView<GraphData.CustomIncludeDescriptor>(
                    graphData.m_CustomIncludes,
                    new GUIContent("Graph Includes", customIncludeTooltip));

                customIncludeList.OnNewItemCallback += () => new GraphData.CustomIncludeDescriptor();

                customIncludeList.OnBeforeChangeCallback +=
                    (ReorderableListView<GraphData.CustomIncludeDescriptor>.ListActionType changeType) =>
                    {
                        RegisterActionToUndo($"{changeType} Graph Include");
                    };

                customIncludeList.OnChangeCallback +=
                    (ReorderableListView<GraphData.CustomIncludeDescriptor>.ListActionType changeType) =>
                    {
                        onChange();
                    };

                customIncludeList.DrawItemCallback += (Rect rect, int idx) =>
                {
                    const float labelWidth = 85f;
                    Rect objectFieldRect = new Rect(rect.x, rect.y, rect.width - (labelWidth + 24), rect.height);
                    Rect toggleRect = new Rect(objectFieldRect.max.x + 8, rect.y, labelWidth + 24, rect.height);

                    EditorGUI.BeginChangeCheck();
                    var data = graphData.m_CustomIncludes[idx];
                    var previousLabelWidth = EditorGUIUtility.labelWidth;
                    EditorGUIUtility.labelWidth = labelWidth;
                    data.Include = (ShaderInclude)EditorGUI.ObjectField(objectFieldRect, "", data.Include, typeof(ShaderInclude), false);
                    data.IncludeWithPragmas = EditorGUI.Toggle(toggleRect, new GUIContent("with pragmas", "Should the pragma directives in the include also be included."), data.IncludeWithPragmas);
                    EditorGUIUtility.labelWidth = previousLabelWidth;
                    if (EditorGUI.EndChangeCheck())
                    {
                        RegisterActionToUndo("Change Graph Include");
                        graphData.m_CustomIncludes[idx] = data;
                        onChange();
                    }
                };

                advancedSettingsElement.Add(customIncludeList);
            }

            return element;
        }

        static string ValidatePragma(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            if (input.StartsWith("#pragma "))
                return input.Replace("#pragma ", "");

            return input;
        }

        static string ValidateDefine(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "";

            if (input.StartsWith("#define "))
                input = input.Replace("#define ", "");

            // Split comment
            string comment = "";
            var commentMatch = Regex.Match(input, @"//.*$");
            if (commentMatch.Success)
            {
                comment = commentMatch.Value;
                input = input.Substring(0, commentMatch.Index);
            }

            // Normalize whitespace
            input = Regex.Replace(input, @"\s+", " ").Trim();

            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 0)
                return "";

            // Clean NAME
            string name = Regex.Replace(parts[0], @"[^A-Za-z0-9_]", "");
            if (Regex.IsMatch(name, @"^[0-9]"))
                name = "_" + name;

            // Clean VALUE
            string value = "";
            if (parts.Length > 1)
                value = Regex.Replace(parts[1], @"[^A-Za-z0-9_]", "");

            // Rebuild
            string result = name;
            if (!string.IsNullOrEmpty(value))
                result += " " + value;

            if (!string.IsNullOrEmpty(comment))
                result += " " + comment;

            return result;
        }

        VisualElement GetSettings(GraphData graphData, Action onChange)
        {
            var element = new VisualElement() { name = "graphSettings" };

            if (graphData.isSubGraph)
                return element;

            void RegisterActionToUndo(string actionName)
            {
                graphData.owner.RegisterCompleteObjectUndo(actionName);
            }

            // Targets
            var targetSettingsLabel = new Label("Target Settings");
            targetSettingsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            element.Add(new PropertyRow(targetSettingsLabel));

            var targetList = new ReorderableTextListView<JsonData<Target>>(
                graphData.m_ActiveTargets,
                "Active Targets",
                false,      // disallow reordering (active list is sorted)
                target => target.value.displayName);

            var validTargets = graphData.GetValidTargets();
            targetList.GetAddMenuOptions = () => validTargets.Select(o => o.displayName).ToList();

            targetList.OnAddMenuItemCallback +=
                (list, addMenuOptionIndex, addMenuOption) =>
            {
                RegisterActionToUndo("Add Target");
                var target = validTargets.ElementAt(addMenuOptionIndex);
                graphData.SetTargetActive(target);
                m_postChangeTargetSettingsCallback();
            };

            targetList.RemoveItemCallback +=
                (list, itemIndex) =>
            {
                RegisterActionToUndo("Remove Target");
                graphData.SetTargetInactive(list[itemIndex].value);
                m_postChangeTargetSettingsCallback();
            };

            element.Add(targetList);

            // Iterate active TargetImplementations
            foreach (var target in graphData.activeTargets)
            {
                // Ensure enabled state is being tracked and get value
                bool foldoutActive;
                if (!m_TargetFoldouts.TryGetValue(target, out foldoutActive))
                {
                    foldoutActive = true;
                    m_TargetFoldouts.Add(target, foldoutActive);
                }

                // Create foldout
                var foldout = new Foldout() { text = target.displayName, value = foldoutActive, name = "foldout" };
                element.Add(foldout);
                foldout.AddToClassList("MainFoldout");
                foldout.RegisterValueChangedCallback(evt =>
                {
                    // Update foldout value and rebuild
                    m_TargetFoldouts[target] = evt.newValue;
                    foldout.value = evt.newValue;
                    onChange();
                });

                if (foldout.value)
                {
                    // Get settings for Target
                    var context = new TargetPropertyGUIContext(graphData.ValidateGraph);
                    // Indent the content of the foldout
                    context.globalIndentLevel++;
                    target.GetPropertiesGUI(ref context, onChange, RegisterActionToUndo);
                    context.globalIndentLevel--;
                    element.Add(context);
                }
            }


            // Data Extensions
            var validExtensions = AbstractShaderGraphDataExtension.ValidExtensions();
            if (validExtensions.Count() > 0 || graphData.SubDatas.Count() > 0)
            {
                var dataExtensionSettings = new Label("\nData Extension Settings");
                dataExtensionSettings.style.unityFontStyleAndWeight = FontStyle.Bold;
                element.Add(new PropertyRow(dataExtensionSettings));

                var extensionList = new ReorderableTextListView<JsonData<AbstractShaderGraphDataExtension>>(
                    graphData.m_SubDatas,
                    "Active Data Extensions",
                    false,
                    data => data.value.displayName);


                extensionList.GetAddMenuOptions = () => validExtensions.Select(o => o.displayName).ToList();

                extensionList.OnAddMenuItemCallback +=
                    (list, addMenuOptionIndex, addMenuOption) =>
                    {
                        RegisterActionToUndo("Add Data Extension");
                        graphData.m_SubDatas.Add(validExtensions[addMenuOptionIndex]);
                        onChange();
                    };

                extensionList.RemoveItemCallback +=
                    (list, itemIndex) =>
                    {
                        RegisterActionToUndo("Remove Data Extension");
                        graphData.m_SubDatas.RemoveAt(itemIndex);
                        onChange();
                    };

                element.Add(extensionList);
                foreach (var subData in graphData.SubDatas)
                {
                    if (subData != null) // I think we need to do this in case it didn't serialize correctly.
                    {
                        bool foldoutActive;
                        if (!m_SubDataFoldouts.TryGetValue(subData, out foldoutActive))
                        {
                            foldoutActive = true;
                            m_SubDataFoldouts.Add(subData, foldoutActive);
                        }
                        var foldout = new Foldout() { text = subData.displayName, value = foldoutActive, name = "foldout" };
                        element.Add(foldout);
                        foldout.AddToClassList("MainFoldout");
                        foldout.RegisterValueChangedCallback(evt =>
                        {
                            // Update foldout value and rebuild
                            m_SubDataFoldouts[subData] = evt.newValue;
                            foldout.value = evt.newValue;
                            onChange();
                        });

                        if (foldout.value)
                        {
                            var subDataElement = new VisualElement();
                            subData.OnPropertiesGUI(subDataElement, onChange, RegisterActionToUndo, graphData);
                            element.Add(subDataElement);
                        }
                    }
                }
            }
#if VFX_GRAPH_10_0_0_OR_NEWER
            // Inform the user that VFXTarget is deprecated, if they are using one.
            if (graphData.m_ActiveTargets.Any(t => t.value is VFXTarget)) //Use Old VFXTarget
            {
                var vfxWarning = new HelpBoxRow("The Visual Effect target is deprecated.\n" +
                    "Use the SRP target(s) instead, and enable 'Support VFX Graph' in the Graph Inspector.\n" +
                    "Then, you can remove the Visual Effect Target.", MessageType.Info);

                element.Add(vfxWarning);
            }
#endif

            return element;
        }

        // used to display UI to select GraphPrecision in the GraphData inspector
        enum UI_GraphPrecision
        {
            Single = GraphPrecision.Single,
            Half = GraphPrecision.Half,
        };

        enum UI_SubGraphPrecision
        {
            Single = GraphPrecision.Single,
            Half = GraphPrecision.Half,
            Switchable = GraphPrecision.Graph,
        };

        internal VisualElement CreateGUI(GraphData graphData)
        {
            var propertySheet = new VisualElement() { name = "graphSettings" };

            if (graphData == null)
            {
                Debug.Log("Attempting to draw something that isn't of type GraphData with a GraphDataPropertyDrawer");
                return propertySheet;
            }

            if (!graphData.isSubGraph)
            {
                // precision selector for shader graphs
                var enumPropertyDrawer = new EnumPropertyDrawer();
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue => { m_changeGraphDefaultPrecisionCallback((GraphPrecision)newValue); },
                    (UI_GraphPrecision)graphData.graphDefaultPrecision,
                    "Precision",
                    UI_GraphPrecision.Single,
                    out var propertyVisualElement));
            }

            if (graphData.isSubGraph)
            {
                {
                    var enum2PropertyDrawer = new EnumPropertyDrawer();
                    propertySheet.Add(enum2PropertyDrawer.CreateGUI(
                        newValue => { m_changeGraphDefaultPrecisionCallback((GraphPrecision)newValue); },
                        (UI_SubGraphPrecision)graphData.graphDefaultPrecision,
                        "Precision",
                        UI_SubGraphPrecision.Switchable,
                        out var propertyVisualElement2));
                }

                var enumPropertyDrawer = new EnumPropertyDrawer();
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        graphData.owner.RegisterCompleteObjectUndo("Change Preview Mode");
                        graphData.previewMode = (PreviewMode)newValue;
                    },
                    graphData.previewMode,
                    "Preview",
                    PreviewMode.Inherit,
                    out var propertyVisualElement));

                var deprecationToggle = new BoolPropertyDrawer();
                propertySheet.Add(deprecationToggle.CreateGUI(
                    newValue =>
                    {
                        graphData.owner.RegisterCompleteObjectUndo("Change Deprecation State");
                        graphData.m_DeprecateSubgraph = newValue;
                        this.m_postChangeTargetSettingsCallback();
                    },
                    graphData.m_DeprecateSubgraph,
                    "Deprecate",
                    out _
                    ));

                if (graphData.m_DeprecateSubgraph)
                {
                    var deprecationMsg = new TextPropertyDrawer();
                    propertySheet.Add(deprecationMsg.CreateGUI(
                        newValue =>
                        {
                            graphData.owner.RegisterCompleteObjectUndo("Change Deprecation Message");
                            graphData.m_DeprecateSubgraphMessage = newValue;
                        },
                        graphData.m_DeprecateSubgraphMessage,
                        "Deprecation Message"
                        ));
                }
            }

            propertySheet.Add(GetSettings(graphData, () => this.m_postChangeTargetSettingsCallback()));

            if (!graphData.isSubGraph)
                propertySheet.Add(GetAdvancedSettings(graphData, () => this.m_changeGraphSettingsCallback()));

            return propertySheet;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI((GraphData)actualObject);
        }

        void IPropertyDrawer.DisposePropertyDrawer() { }
    }
}
