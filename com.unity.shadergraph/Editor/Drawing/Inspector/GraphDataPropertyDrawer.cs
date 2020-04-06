using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Data.Interfaces;
using UnityEditor;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Drawing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;
using UnityEngine.UIElements;

namespace Drawing.Inspector
{
    [SGPropertyDrawer(typeof(GraphData))]
    public class GraphDataPropertyDrawer : IPropertyDrawer
    {
        public delegate void ChangeConcretePrecisionCallback(ConcretePrecision newValue);
        public delegate void PostTargetSettingsChangedCallback(bool updateInspector);

        private PostTargetSettingsChangedCallback m_postChangeTargetSettingsCallback;
        private ChangeConcretePrecisionCallback m_postChangeConcretePrecisionCallback;

        // Targets have persistent data that needs to be maintained between interactions
        // Hence we are storing a reference to this particular property drawer to continue using it
        private TargetPropertyDrawer m_TargetPropertyDrawer = new TargetPropertyDrawer();

        public void GetPropertyData(
            PostTargetSettingsChangedCallback postChangeValueCallback,
            ChangeConcretePrecisionCallback changeConcretePrecisionCallback)
        {
            m_postChangeTargetSettingsCallback = postChangeValueCallback;
            m_postChangeConcretePrecisionCallback = changeConcretePrecisionCallback;
        }

        internal VisualElement CreateGUIForField(GraphData graphData)
        {
            if (graphData == null)
                throw new InvalidCastException(
                    "Attempting to draw something that isn't of type GraphData with a GraphDataPropertyDrawer");

            var propertySheet = new VisualElement() {name = "graphSettings"};

            var enumPropertyDrawer = new EnumPropertyDrawer();
            propertySheet.Add(enumPropertyDrawer.CreateGUIForField(
                newValue => { m_postChangeConcretePrecisionCallback((ConcretePrecision) newValue); },
                graphData.concretePrecision,
                "Precision",
                ConcretePrecision.Float,
                out var propertyVisualElement));

            // Add Label
            var targetSettingsLabel = new Label("Target Settings");
            targetSettingsLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            propertySheet.Add(new PropertyRow(targetSettingsLabel));

            // Target Dropdown Field
            propertySheet.Add(new PropertyRow(new Label("Target")), (row) =>
            {
                row.Add(new IMGUIContainer(() =>
                {
                    EditorGUI.BeginChangeCheck();
                    graphData.activeTargetIndex = EditorGUILayout.Popup(graphData.activeTargetIndex,
                        graphData.generationTargets.Select(x => x.target.displayName).ToArray(), GUILayout.Width(100f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_postChangeTargetSettingsCallback(true);
                    }
                }));
            });

            // Add a space
            propertySheet.Add(new PropertyRow(new Label("")));

            graphData.activeGenerationTarget.SupplyDataToPropertyDrawer(m_TargetPropertyDrawer, null);
            propertySheet.Add(
                m_TargetPropertyDrawer.CreateGUIForField((updateInspector) =>
                {
                    m_postChangeTargetSettingsCallback(updateInspector);
                },
            graphData.activeGenerationTarget,
            out propertyVisualElement));

            return propertySheet;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            return this.CreateGUIForField((GraphData)actualObject);
        }
    }

    [SGPropertyDrawer(typeof(Target))]
    public class TargetPropertyDrawer : IPropertyDrawer
    {
        private Action m_postChangeActiveImplementationsCallback;

        string[] m_TargetNames { get; set; }

        Dictionary<Target, bool> m_TargetFoldouts = new Dictionary<Target, bool>();

        public void GetPropertyData(
            string[] targetNames,
            Action postChangeActiveImplementationsCallback)
        {
            m_TargetNames = targetNames;
            m_postChangeActiveImplementationsCallback = postChangeActiveImplementationsCallback;
        }

        public VisualElement DrawProperty(PropertyInfo propertyInfo, object actualObject, Inspectable attribute)
        {
            var target = (Target) actualObject;
            if (target == null)
                throw new InvalidCastException(
                    "Attempting to draw something that isn't of type Target with a TargetPropertyDrawer");

            return CreateGUIForField((updateInspector) =>
                {
                    propertyInfo.GetSetMethod(true).Invoke(actualObject, new object[] {updateInspector});
                },
                target,
                out var implementationPropertyVisualElement);
        }

        internal VisualElement CreateGUIForField(GraphDataPropertyDrawer.PostTargetSettingsChangedCallback postSettingsChangedCallback, Target target, out VisualElement element)
        {
            var visualElement = new VisualElement() {name = "implementationSettings"};

            // Title
            var title = new Label("Implementation Settings") {name = "titleLabel"};
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            visualElement.Add(new PropertyRow(title));

            // Implementation Dropdown Field
            visualElement.Add(new PropertyRow(new Label("Implementations")), (row) =>
            {
                row.Add(new IMGUIContainer(() =>
                {
                    EditorGUI.BeginChangeCheck();
                    target.activeImplementationBitmask = EditorGUILayout.MaskField(
                        target.activeImplementationBitmask, m_TargetNames, GUILayout.Width(100f));
                    if (EditorGUI.EndChangeCheck())
                    {
                        m_postChangeActiveImplementationsCallback();
                        postSettingsChangedCallback(true);
                    }
                }));
            });

            // Iterate active TargetImplementations
            foreach (var implementation in target.Implementations)
            {
                // Ensure enabled state is being tracked and get value
                bool foldoutActive = true;
                if (!m_TargetFoldouts.TryGetValue(implementation, out foldoutActive))
                {
                    m_TargetFoldouts.Add(implementation, foldoutActive);
                }

                // Create foldout
                var foldout = new Foldout() {text = implementation.displayName, value = foldoutActive};
                visualElement.Add(foldout);
                foldout.RegisterValueChangedCallback(evt =>
                {
                    // Update foldout value and rebuild
                    m_TargetFoldouts[implementation] = evt.newValue;
                    foldout.value = evt.newValue;
                    postSettingsChangedCallback(true);
                });

                if (foldout.value)
                {
                    // Get settings for TargetImplementation
                    var implementationSettings = implementation.GetSettings(() => postSettingsChangedCallback(true));

                    // Virtual method returns null
                    // Settings are only added if this is overriden
                    if (implementationSettings != null)
                    {
                        visualElement.Add(implementationSettings);
                    }
                }
            }

            element = visualElement;
            return element;
        }
    }
}

