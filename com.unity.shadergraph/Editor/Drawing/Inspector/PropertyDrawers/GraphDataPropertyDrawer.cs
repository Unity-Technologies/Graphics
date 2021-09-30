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

namespace UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers
{
    [SGPropertyDrawer(typeof(GraphData))]
    public class GraphDataPropertyDrawer : IPropertyDrawer
    {
        public delegate void ChangeGraphDefaultPrecisionCallback(GraphPrecision newDefaultGraphPrecision);
        public delegate void PostTargetSettingsChangedCallback();

        PostTargetSettingsChangedCallback m_postChangeTargetSettingsCallback;
        ChangeGraphDefaultPrecisionCallback m_changeGraphDefaultPrecisionCallback;

        Dictionary<Target, bool> m_TargetFoldouts = new Dictionary<Target, bool>();

        public void GetPropertyData(
            PostTargetSettingsChangedCallback postChangeValueCallback,
            ChangeGraphDefaultPrecisionCallback changeGraphDefaultPrecisionCallback)
        {
            m_postChangeTargetSettingsCallback = postChangeValueCallback;
            m_changeGraphDefaultPrecisionCallback = changeGraphDefaultPrecisionCallback;
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

            // Add Label
            var targetSettingsLabel = new Label("Target Settings");
            targetSettingsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            element.Add(new PropertyRow(targetSettingsLabel));

            var targetList = new ReorderableListView<JsonData<Target>>(
                graphData.m_ActiveTargets,
                "Active Targets",
                false,      // disallow reordering (active list is sorted)
                target => target.value.displayName);

            targetList.GetAddMenuOptions = () => graphData.GetPotentialTargetDisplayNames();

            targetList.OnAddMenuItemCallback +=
                (list, addMenuOptionIndex, addMenuOption) =>
            {
                RegisterActionToUndo("Add Target");
                graphData.SetTargetActive(addMenuOptionIndex);
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
                    var context = new TargetPropertyGUIContext();
                    // Indent the content of the foldout
                    context.globalIndentLevel++;
                    target.GetPropertiesGUI(ref context, onChange, RegisterActionToUndo);
                    context.globalIndentLevel--;
                    element.Add(context);
                }
            }

#if VFX_GRAPH_10_0_0_OR_NEWER
            // Inform the user that VFXTarget is deprecated, if they are using one.
            var activeTargetSRP = graphData.m_ActiveTargets.Where(t => !(t.value is VFXTarget));
            if (graphData.m_ActiveTargets.Any(t => t.value is VFXTarget) //Use Old VFXTarget
                && activeTargetSRP.Any()
                && activeTargetSRP.All(o => o.value.CanSupportVFX()))
            {
                var vfxWarning = new HelpBoxRow(MessageType.Info);

                var vfxWarningLabel = new Label("The Visual Effect target is deprecated.\n" +
                    "Use the SRP target(s) instead, and enable 'Support VFX Graph' in the Graph Inspector.\n" +
                    "Then, you can remove the Visual Effect Target.");

                vfxWarningLabel.style.color = new StyleColor(Color.white);
                vfxWarningLabel.style.whiteSpace = WhiteSpace.Normal;

                vfxWarning.Add(vfxWarningLabel);
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
            }

            propertySheet.Add(GetSettings(graphData, () => this.m_postChangeTargetSettingsCallback()));

            return propertySheet;
        }

        public Action inspectorUpdateDelegate { get; set; }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, InspectableAttribute attribute)
        {
            return this.CreateGUI((GraphData)actualObject);
        }
    }
}
