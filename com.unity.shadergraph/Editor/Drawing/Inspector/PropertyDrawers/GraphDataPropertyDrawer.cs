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
        public delegate void ChangeConcretePrecisionCallback(ConcretePrecision newValue);
        public delegate void PostTargetSettingsChangedCallback();

        PostTargetSettingsChangedCallback m_postChangeTargetSettingsCallback;
        ChangeConcretePrecisionCallback m_postChangeConcretePrecisionCallback;

        Dictionary<Target, bool> m_TargetFoldouts = new Dictionary<Target, bool>();

        public void GetPropertyData(
            PostTargetSettingsChangedCallback postChangeValueCallback,
            ChangeConcretePrecisionCallback changeConcretePrecisionCallback)
        {
            m_postChangeTargetSettingsCallback = postChangeValueCallback;
            m_postChangeConcretePrecisionCallback = changeConcretePrecisionCallback;
        }

        VisualElement GetSettings(GraphData graphData, Action onChange)
        {
            var element = new VisualElement() { name = "graphSettings" };

            if(graphData.isSubGraph)
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
            foreach(var target in graphData.activeTargets)
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
                    target.GetPropertiesGUI(ref context, onChange, RegisterActionToUndo);
                    element.Add(context);
                }
            }

            return element;
        }

        internal VisualElement CreateGUI(GraphData graphData)
        {
            var propertySheet = new VisualElement() {name = "graphSettings"};

            if (graphData == null)
            {
                Debug.Log("Attempting to draw something that isn't of type GraphData with a GraphDataPropertyDrawer");
                return propertySheet;
            }

            {
                var enumPropertyDrawer = new EnumPropertyDrawer();
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue => { m_postChangeConcretePrecisionCallback((ConcretePrecision)newValue); },
                    graphData.concretePrecision,
                    "Precision",
                    ConcretePrecision.Single,
                    out var propertyVisualElement));
            }

            if (graphData.isSubGraph)
            {
                var enumPropertyDrawer = new EnumPropertyDrawer();
                propertySheet.Add(enumPropertyDrawer.CreateGUI(
                    newValue =>
                    {
                        graphData.owner.RegisterCompleteObjectUndo("Change Preview Mode");
                        graphData.previewMode = (PreviewMode) newValue;
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
